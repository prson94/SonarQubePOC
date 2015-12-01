-- =============================================
-- Create date: <12/1/2015>
-- Description:	<Given the input Stage File instance the proc looks up Eagle DB Columns and adds relations to Given BB mnemonic>
-- =============================================
Create PROCEDURE [Fusion].[ProcessEagleMCToBloombergRelations]	
	@StagingFileID int,
	@FusionID int
AS
BEGIN	
	SET NOCOUNT ON;

	declare		@eagleStreamID int,
				@streamToFieldIntersectTypeID int,
				@fieldToBBIntersectTypeID int,
				@streamSourceIntersectTypeNodeID int,
				@streamTargetIntersectTypeNodeID int
	-- load the panel that we want to add relations ships for
    
	select @eagleStreamID = fusionattributeid from [fusion].[stagingfile] where id = @StagingFileID and fusionID = @FusionID

	
	if @eagleStreamID is not null
	begin
		-- add relationships for Stream (171) to Eagle DB Columns (205)
		-- using star tag field that is a field for for fusionattribute type 205 lookup fields to add rels for

			Declare @StreamToFieldList Table(IntersectTypeID int, ID int, FieldFusionAttributeID int, StreamFusionAttributeID int);
			Declare @IDList Table(IntersectID int,StageID Int);
			declare @Intersects IDTable
			
			-- load the intersect type ids
			select	@streamToFieldIntersectTypeID = IntersectTypeID,
					@streamSourceIntersectTypeNodeID = SourceIntersectTypeNodeID,
					@streamTargetIntersectTypeNodeID = TargetIntersectTypeNodeID
				 from	utility.RelationshipTypes
				where	SourceObjectType = 'FusionAttributeType' and SourceObjectID = 171
					and TargetObjectType = 'FusionAttributeType' and TargetObjectID = 205

			-- insert into in memory table variable the values we want to add intersects for
			insert into @StreamToFieldList
				select @streamToFieldIntersectTypeID, sfi.id, fa.id, sf.FusionAttributeID
					from 
						field f 
						inner join fusionAttribute fa on (f.ObjectID = fa.ID)
						inner join fieldtype ft on (f.fieldtypeid = ft.id)
						inner join [fusion].[StagingFileItem] sfi on (sfi.tag = f.value)				
						inner join [fusion].[StagingFile] sf on (sfi.stagingfileid = sf.id)
						left outer join cache.Relationships cr on (cr.SourceObject = 'FusionAttribute' and cr.TargetObject = 'FusionAttribute' and cr.SourceObjectID = sf.FusionAttributeID and cr.TargetObjectID = fa.ID)
					where fa.fusionattributetypeid = 205 and ft.name = 'startag' and sfi.stagingfileid = @StagingFileID and cr.IntersectID is null

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
			

		-- add relations for Eagle Field (205) to Bloomberg mnemonic (301)
		select	@fieldToBBIntersectTypeID = IntersectTypeID from	utility.RelationshipTypes
				where	SourceObjectType = 'FusionAttributeType' and SourceObjectID = 205
					and TargetObjectType = 'FusionAttributeType' and TargetObjectID = 301
	end;
END
GO
