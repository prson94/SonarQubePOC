CREATE PROCEDURE [dbo].[ProcessEagleMCToEagleFieldRelations]
	@StagingFileID int,
	@FusionID int
AS
BEGIN	
	SET NOCOUNT ON;
		
	declare	@eagleStreamID int,
			@streamToFieldIntersectTypeID int,				
			@currentEagleFusionId int;

	declare	@IDList Table(IntersectID int,StageID Int);

	declare	@Intersects IDTable;

	declare	@MessageStreamFussionAttributeID int,
			@EagleFieldFusionAttributeID int;

	select	@MessageStreamFussionAttributeID = 196;
	select	@EagleFieldFusionAttributeID = 205;

	-- load the stream that we want to add relations ships for    
	select	@eagleStreamID = fusionattributeid 
	from	[fusion].[stagingfile] 
	where	id = @StagingFileID and 
			fusionID = @FusionID;
			
	if @eagleStreamID is null
	begin
		raiserror('ERROR : UNABLE TO LOCATE SPECIFIED STREAM INFORMATION FOR INPUT FUSION ID / STAGING ID', 15, 1);
		return;
	end;

	select @currentEagleFusionId = FusionID from [dbo].[fusionattribute] where id = @eagleStreamID

	-- add relationships for Stream (196) to Eagle DB Columns (205)
	-- using star tag field that is a field for for fusionattribute type 205 lookup fields to add rels for
	-- todo pull to separate proc
	if @eagleStreamID is not null
	begin
			Declare @StreamToFieldList Table(FieldFusionAttributeID int, StreamFusionAttributeID int,IntersectTypeID int, ID int);
			
			-- load the intersect type ids
			select	@streamToFieldIntersectTypeID = ID
			from	IntersectType
			where	(Subject = 'FusionAttributeType' and Object = 'FusionAttributeType') 
					and	( 
						(SubjectID = @MessageStreamFussionAttributeID and ObjectID = @EagleFieldFusionAttributeID) OR
						(SubjectID = @EagleFieldFusionAttributeID and ObjectID = @MessageStreamFussionAttributeID)
						)

			if @streamToFieldIntersectTypeID is null
			begin
				raiserror('ERROR : UNABLE TO LOCATE INTERSECT TYPE IDS FOR EAGLE TO EAGLE MESSAGE STREAMS', 15, 1);
				return;
			end;

			-- insert into in memory table variable the values we want to add intersects for
			insert into @StreamToFieldList
				select		fa.id, 
							sf.FusionAttributeID, 
							@streamToFieldIntersectTypeID, 
							ROW_NUMBER() OVER (Order by fa.id) AS 'RowNumber'
				from		field f 
							inner join FusionAttribute fa on f.ObjectID = fa.ID and fa.fusionid = @currentEagleFusionId
							inner join FieldType ft on f.fieldtypeid = ft.id
							inner join fusion.StagingFileItem sfi on sfi.tag = f.value				
							inner join fusion.StagingFile sf on sfi.stagingfileid = sf.id
							left join	(
										select	SubjectID,
												ObjectID,
												1 as hasExisting
										from	[Intersect]
										where	Subject = 'FusionAttribute' and Object= 'FusionAttribute'
										) existing on ( (existing.SubjectID = sf.FusionAttributeID and existing.ObjectID = fa.ID) OR (existing.SubjectID = fa.ID and existing.ObjectID = sf.FusionAttributeID) )
				where		fa.fusionattributetypeid = @EagleFieldFusionAttributeID and 
							ft.name = 'startag' and 
							sfi.stagingfileid = @StagingFileID and 
							existing.hasExisting is null
				group by	fa.id, sf.FusionAttributeID  -- grouping is used to eliminate duplicate star tag relations

			--insert intersect records and save there id's
			-- trick is to use merge to keep the sequence id and staging row ids
			-- http://stackoverflow.com/questions/15614261/using-output-clause-to-insert-value-not-in-inserted
			MERGE
				INTO    [Intersect] d
				USING   (
							SELECT	sr.IntersectTypeID, 
									2 as class,
									--sr.ID as srID,
									'FusionAttribute' as Subject,
									sr.StreamFusionAttributeID as SubjectID,
									'FusionAttribute' as Object,
									sr.FieldFusionAttributeID as ObjectID
							FROM	@StreamToFieldList sr							
						) s
				ON      (
						s.IntersectTypeID = d.IntersectTypeID 
						and s.Subject = d.Subject and s.SubjectID = d.SubjectID 
						and s.Object = d.Object and s.ObjectID = d.ObjectID
						)
				WHEN NOT MATCHED THEN
				INSERT  (IntersectTypeID, Classification, Description, Subject, SubjectID, Object, ObjectID)
				VALUES  (s.IntersectTypeID, s.class, NULL, s.Subject, s.SubjectID, s.Object, s.ObjectID);
				--OUTPUT  INSERTED.ID, s.srID into @IDList;
	end;
end