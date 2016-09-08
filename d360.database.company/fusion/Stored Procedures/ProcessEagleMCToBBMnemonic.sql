CREATE PROCEDURE [fusion].[ProcessEagleMCToBBMnemonic]
	@StagingFileID int,
	@FusionID int
AS
BEGIN	
	SET NOCOUNT ON;
		
	declare		@eagleStreamID int,
				@streamToFieldIntersectTypeID int;

	declare		@IDList Table(IntersectID int,StageID Int);

	declare		@Intersects IDTable;

	declare		@MessageStreamFussionAttributeID int = 196,
				@BloombergMnemonicFusionID int = 301;
				
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
			Declare @StreamToFieldList Table(FieldFusionAttributeID int, StreamFusionAttributeID int, IntersectTypeID int, ID int);
			
			-- load the intersect type ids
			select	@streamToFieldIntersectTypeID = ID
			from	[IntersectType]
			where	Subject = 'FusionAttributeType' and 
					Object = 'FusionAttributeType' and 
					(
						( SubjectID = @MessageStreamFussionAttributeID and ObjectID = @BloombergMnemonicFusionID ) OR
						( SubjectID = @BloombergMnemonicFusionID and ObjectID = @MessageStreamFussionAttributeID )
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
				from		fusionAttribute fa
							inner join [fusion].[StagingFileItem] sfi on (sfi.value = fa.name)				
							inner join [fusion].[StagingFile] sf on (sfi.stagingfileid = sf.id)
							left join [Intersect] I on	I.IntersectTypeID = @streamToFieldIntersectTypeID and 
														I.Subject = 'FusionAttribute' and 
														I.Object ='FusionAttribute' and
														(
															( SubjectID = sf.FusionAttributeID and ObjectID = fa.ID ) OR
															( SubjectID = fa.ID and ObjectID = sf.FusionAttributeID )
														)
					where		fa.fusionattributetypeid = @BloombergMnemonicFusionID and 
								sfi.stagingfileid = @StagingFileID and 
								I.ID is null
					group by	fa.id, sf.FusionAttributeID  -- grouping is used to eliminate duplicate star tag relations

			MERGE
				INTO    [Intersect] d
				USING   (
							SELECT	IntersectTypeID, 
									ID,
									StreamFusionAttributeID as SubjectID,
									FieldFusionAttributeID as ObjectID
							FROM	@StreamToFieldList							
						) s
				ON      (1 = 0)
				WHEN NOT MATCHED THEN
				INSERT  (IntersectTypeID, Classification, Description, Subject, SubjectID, Object, ObjectID)
				VALUES  (s.IntersectTypeID, 2, NULL, 'FusionAttribute', s.SubjectID, 'FusionAttribute', s.ObjectID)
				OUTPUT  INSERTED.ID, s.ID into @IDList;
										
			insert into @Intersects 
				select idl.intersectid from @IDList idl;
			
			declare @IntersectCount int
			select @IntersectCount = count(1) from @Intersects
			
			if @IntersectCount > 0 
			begin				
				EXEC cache.SynchronizeRelationships @Intersects
			end
	end;
end
GO

