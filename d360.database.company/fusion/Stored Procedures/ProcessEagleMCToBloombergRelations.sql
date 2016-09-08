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
	declare		@fieldToBBIntersectTypeID int,
				@fieldSourceIntersectTypeNodeID int,
				@fieldTargetIntersectTypeNodeID int

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
		Declare @BBToFieldList Table(FieldFusionAttributeID int, StreamFusionAttributeID int,IntersectTypeID int, ID int);
		
		-- load the intersect id's for message stream to bb mnemonic
		select	@fieldToBBIntersectTypeID = IntersectTypeID,
				@fieldSourceIntersectTypeNodeID = SourceIntersectTypeNodeID,
				@fieldTargetIntersectTypeNodeID = TargetIntersectTypeNodeID
		 from	utility.RelationshipTypes
				where	SourceObjectType = 'FusionAttributeType' and SourceObjectID = 301					
					and TargetObjectType = 'FusionAttributeType' and TargetObjectID = 205;

		if @fieldToBBIntersectTypeID is null or @fieldSourceIntersectTypeNodeID is null or @fieldTargetIntersectTypeNodeID is null
		begin
			raiserror('ERROR : UNABLE TO LOCATE INTERSECT TYPE IDS FOR EAGLE TO BLOOMBERG INTERSECT', 15, 1);
			return;
		end;

		-- load into memory the id's that we need to add intersects for
		insert into @BBToFieldList
			select	fa.id as 'fieldID', faBB.id as 'bbID', @fieldToBBIntersectTypeID, ROW_NUMBER() OVER (Order by sfi.id) AS 'RowNumber'
			from	field f 
					inner join fusionAttribute fa on (f.ObjectID = fa.ID)
					inner join fieldtype ft on (f.fieldtypeid = ft.id)
					inner join [fusion].[StagingFileItem] sfi on (sfi.tag = f.value)				
					inner join [fusion].[StagingFile] sf on (sfi.stagingfileid = sf.id)						
					inner join fusionAttribute faBB on (faBB.Name = sfi.value and faBB.fusionattributetypeid = 301)						
					left join	(
								select	srcINode.ObjectID as SourceObjectID,
										tgtINode.ObjectID as TargetObjectID,
										1 as hasExisting
								from	[Intersect] isect 
										inner join intersectnode srcINode on	(
																				isect.intersecttypeid = @fieldToBBIntersectTypeID and 
																				isect.id = srcINode.IntersectID and 
																				srcINode.IntersectTypeNodeID = @fieldSourceIntersectTypeNodeID
																				)
										inner join intersectnode tgtINode on	(
																				isect.intersecttypeid = @fieldToBBIntersectTypeID and 
																				isect.id = tgtINode.IntersectID and 
																				tgtINode.IntersectTypeNodeID = @fieldTargetIntersectTypeNodeID
																				)
								) existing on existing.SourceObjectID = faBB.ID and existing.TargetObjectID = fa.id
			where	fa.fusionattributetypeid = 205 and 
					ft.name = 'startag' and 
					sfi.stagingfileid = @StagingFileID and 
					existing.hasExisting is null;


			--insert intersect records and save there id's
			-- trick is to use merge to keep the sequence id and staging row ids
			-- http://stackoverflow.com/questions/15614261/using-output-clause-to-insert-value-not-in-inserted
			MERGE
				INTO    [Intersect] d
				USING   (
							SELECT	sr.IntersectTypeID, 
									2 as class,
									sr.ID as srID,
									'FusionAttribute' as Subject,
									sr.StreamFusionAttributeID as SubjectID,
									'FusionAttribute' as Object,
									sr.FieldFusionAttributeID as ObjectID
							FROM	@BBToFieldList sr							
						) s
				ON      (1 = 0)
				WHEN NOT MATCHED THEN
				INSERT  (IntersectTypeID, Classification, Description, Subject, SubjectID, Object, ObjectID)
				VALUES  (s.IntersectTypeID, s.class, NULL, s.Subject, s.SubjectID, s.Object, s.ObjectID)
				OUTPUT  INSERTED.ID, s.srID into @IDList;

			--insert start records into intersect node
			INSERT INTO IntersectNode	(IntersectTypeNodeID, IntersectID, ObjectType, ObjectID)
				select	@fieldSourceIntersectTypeNodeID,
						il.IntersectID, 
						'FusionAttribute',
						sr.StreamFusionAttributeID 
				from	@BBToFieldList sr 
						inner join @IDList il on sr.ID = il.StageID;

			--insert end records into intersect node
			INSERT INTO IntersectNode	(IntersectTypeNodeID, IntersectID, ObjectType, ObjectID)
				select	@fieldTargetIntersectTypeNodeID, 
						il.IntersectID, 
						'FusionAttribute',
						sr.FieldFusionAttributeID 
				from	@BBToFieldList sr 
						inner join @IDList il on sr.ID = il.StageID;
										
			insert into @Intersects select idl.intersectid from @IDList idl;
						
			select @IntersectCount = count(1) from @Intersects
			if @IntersectCount > 0 
			begin
				EXEC cache.SynchronizeRelationships @Intersects
			end

	end;
END
GO
