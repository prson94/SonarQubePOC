CREATE PROCEDURE [dbo].[ProcessEagleMCToEagleFieldRelations]
	@StagingFileID int,
	@FusionID int
AS
BEGIN	
	SET NOCOUNT ON;
		
	declare		@eagleStreamID int,
				@streamToFieldIntersectTypeID int,				
				@streamSourceIntersectTypeNodeID int,
				@streamTargetIntersectTypeNodeID int;

	declare		@IDList Table(IntersectID int,StageID Int);

	declare		@Intersects IDTable;

	declare		@MessageStreamFussionAttributeID int,
				@EagleFieldFusionAttributeID int;

	select @MessageStreamFussionAttributeID = 196;
	select @EagleFieldFusionAttributeID = 205;

	-- load the stream that we want to add relations ships for    
	select @eagleStreamID = fusionattributeid from [fusion].[stagingfile] where id = @StagingFileID and fusionID = @FusionID

	if @eagleStreamID is null
	begin
		raiserror('ERROR : UNABLE TO LOCATE SPECIFIED STREAM INFORMATION FOR INPUT FUSION ID / STAGING ID', 15, 1);
		return;
	end;
		
	-- add relationships for Stream (196) to Eagle DB Columns (205)
	-- using star tag field that is a field for for fusionattribute type 205 lookup fields to add rels for
	-- todo pull to separate proc
	if @eagleStreamID is not null
	begin
			Declare @StreamToFieldList Table(FieldFusionAttributeID int, StreamFusionAttributeID int,IntersectTypeID int, ID int);
			
			-- load the intersect type ids
			select	@streamToFieldIntersectTypeID = IntersectTypeID,
					@streamSourceIntersectTypeNodeID = SourceIntersectTypeNodeID,
					@streamTargetIntersectTypeNodeID = TargetIntersectTypeNodeID
				 from	utility.RelationshipTypes
				where	SourceObjectType = 'FusionAttributeType' and SourceObjectID = @MessageStreamFussionAttributeID
					and TargetObjectType = 'FusionAttributeType' and TargetObjectID = @EagleFieldFusionAttributeID

			if @streamToFieldIntersectTypeID is null or @streamSourceIntersectTypeNodeID is null or @streamTargetIntersectTypeNodeID is null
			begin
				raiserror('ERROR : UNABLE TO LOCATE INTERSECT TYPE IDS FOR EAGLE TO EAGLE MESSAGE STREAMS', 15, 1);
				return;
			end;

			-- insert into in memory table variable the values we want to add intersects for
			insert into @StreamToFieldList
				select fa.id, sf.FusionAttributeID, @streamToFieldIntersectTypeID, ROW_NUMBER() OVER (Order by fa.id) AS 'RowNumber'
					from 
						field f 
						inner join fusionAttribute fa on (f.ObjectID = fa.ID)
						inner join fieldtype ft on (f.fieldtypeid = ft.id)
						inner join [fusion].[StagingFileItem] sfi on (sfi.tag = f.value)				
						inner join [fusion].[StagingFile] sf on (sfi.stagingfileid = sf.id)
						left join (select srcINode.ObjectID as SourceObjectID,
								   tgtINode.ObjectID as TargetObjectID,
								   1 as hasExisting
							from 
								[dbo].[intersect] isect inner join intersectnode srcINode on (isect.intersecttypeid = @streamToFieldIntersectTypeID and isect.id = srcINode.IntersectID and srcINode.IntersectTypeNodeID = @streamSourceIntersectTypeNodeID)
								inner join intersectnode tgtINode on(isect.intersecttypeid = @streamToFieldIntersectTypeID and isect.id = tgtINode.IntersectID and tgtINode.IntersectTypeNodeID = @streamTargetIntersectTypeNodeID)) existing
								on existing.SourceObjectID = sf.FusionAttributeID and existing.TargetObjectID = fa.ID
					where fa.fusionattributetypeid = @EagleFieldFusionAttributeID and ft.name = 'startag'  and sfi.stagingfileid = @StagingFileID and existing.hasExisting is null
					group by fa.id, sf.FusionAttributeID  -- grouping is used to eliminate duplicate star tag relations

			--insert intersect records and save there id's
			-- trick is to use merge to keep the sequence id and staging row ids
			-- http://stackoverflow.com/questions/15614261/using-output-clause-to-insert-value-not-in-inserted
			MERGE
				INTO    [Intersect] d
				USING   (
							SELECT sr.IntersectTypeID isectid , 2 as class,sr.ID as srID
							FROM @StreamToFieldList sr							
						) s
				ON      (1 = 0)
				WHEN NOT MATCHED THEN
				INSERT  (IntersectTypeID, Classification, Description)
				VALUES  (isectid, class, NULL)
				OUTPUT  INSERTED.ID, s.srID into @IDList;

			--insert start records into intersect node
			INSERT INTO IntersectNode	(IntersectTypeNodeID, IntersectID, ObjectType, ObjectID)
					select @streamSourceIntersectTypeNodeID, il.IntersectID, 'FusionAttribute',sr.StreamFusionAttributeID from @StreamToFieldList sr inner join @IDList il on (sr.ID = il.StageID);
						

			--insert end records into intersect node
			INSERT INTO IntersectNode	(IntersectTypeNodeID, IntersectID, ObjectType, ObjectID)
					select @streamTargetIntersectTypeNodeID, il.IntersectID, 'FusionAttribute',sr.FieldFusionAttributeID from @StreamToFieldList sr inner join @IDList il on (sr.ID = il.StageID);
					
	
										
			insert into @Intersects select idl.intersectid from @IDList idl;
			
			declare @IntersectCount int
			select @IntersectCount = count(1) from @Intersects
			
			if @IntersectCount > 0 
			begin				
				EXEC cache.SynchronizeRelationships @Intersects
			end
	end;
end;