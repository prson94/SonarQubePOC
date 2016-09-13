CREATE PROCEDURE [fusion].[ProcessEagleMCToBloombergRelations]	
	@StagingFileID int,
	@FusionID int
AS
BEGIN	
	SET NOCOUNT ON;
	
	
	declare		@eagleStreamID int;				
	declare		@IntersectCount int;
	Declare		@IDList Table(IntersectID int,StageID Int);
	declare		@Intersects IDTable;
	declare		@fieldToBBIntersectTypeID int;

	-- load the panel that we want to add relations ships for
    
	select @eagleStreamID = fusionattributeid from [fusion].[stagingfile] where id = @StagingFileID and fusionID = @FusionID
	
	if @eagleStreamID is null
	begin
		raiserror('ERROR : UNABLE TO LOCATE SPECIFIED STREAM INFORMATION FOR INPUT FUSION ID / STAGING ID', 15, 1);
		return;
	end;
			
	exec ProcessEagleMCToEagleFieldRelations @StagingFileID, @FusionID

	exec [fusion].[ProcessEagleMCToBBMnemonic] @StagingFileID, @FusionID


	-- add relations for Eagle Field (205) to Bloomberg mnemonic (301)
	if @eagleStreamID is not null
	begin
		Declare @BBToFieldList Table(FieldFusionAttributeID int, StreamFusionAttributeID int, IntersectTypeID int, ID int);
		
		-- load the intersect id's for message stream to bb mnemonic	

		select	@fieldToBBIntersectTypeID = ID
			from	[IntersectType]
			where	Subject = 'FusionAttributeType' and 
					Object = 'FusionAttributeType' and 
					(
						( SubjectID = 205 and ObjectID = 301 ) OR
						( SubjectID = 301 and ObjectID = 205 )
					)

		if @fieldToBBIntersectTypeID is null
		begin
			raiserror('ERROR : UNABLE TO LOCATE INTERSECT TYPE IDS FOR EAGLE TO BLOOMBERG INTERSECT', 15, 1);
			return;
		end

		-- load into memory the id's that we need to add intersects for
		insert into @BBToFieldList
			select	fa.id as 'fieldID', faBB.id as 'bbID', @fieldToBBIntersectTypeID, ROW_NUMBER() OVER (Order by sfi.id) AS 'RowNumber'
			from	field f 
					inner join fusionAttribute fa on (f.ObjectID = fa.ID)
					inner join fieldtype ft on (f.fieldtypeid = ft.id)
					inner join [fusion].[StagingFileItem] sfi on (sfi.tag = f.value)				
					inner join [fusion].[StagingFile] sf on (sfi.stagingfileid = sf.id)						
					inner join fusionAttribute faBB on (faBB.Name = sfi.value and faBB.fusionattributetypeid = 301)		
					left join [Intersect] I on	I.IntersectTypeID = @fieldToBBIntersectTypeID and 
												I.Subject = 'FusionAttribute' and 
												I.Object ='FusionAttribute' and
												(
													( I.SubjectID = faBB.ID and I.ObjectID = fa.ID ) OR
													( I.SubjectID = fa.ID and I.ObjectID = faBB.ID )
												)
			where	fa.fusionattributetypeid = 205 and 
					ft.name = 'startag' and 
					sfi.stagingfileid = @StagingFileID and 
					I.ID is null;

			MERGE
				INTO    [Intersect] d
				USING   (
							SELECT	IntersectTypeID, 
									ID,
									StreamFusionAttributeID as SubjectID,
									FieldFusionAttributeID as ObjectID
							FROM	@BBToFieldList
						) s
				ON      (1 = 0)
				WHEN NOT MATCHED THEN
				INSERT  (IntersectTypeID, Classification, Subject, SubjectID, Object, ObjectID)
				VALUES  (s.IntersectTypeID, 2, 'FusionAttribute', s.SubjectID, 'FusionAttribute', s.ObjectID)
				OUTPUT  INSERTED.ID, s.ID into @IDList;										

			insert into @Intersects 
				select idl.intersectid from @IDList idl;
						
			select @IntersectCount = count(1) from @Intersects
			if @IntersectCount > 0 
			begin
				EXEC cache.SynchronizeRelationships @Intersects
			end
	end;
END
GO