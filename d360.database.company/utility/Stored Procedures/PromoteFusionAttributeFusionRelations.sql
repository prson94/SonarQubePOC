CREATE PROCEDURE [utility].[PromoteFusionAttributeFusionRelations]	 
AS
BEGIN
	SET NOCOUNT ON;
	
	IF OBJECT_ID('tempdb..#relations') IS NOT NULL
		DROP TABLE #relations;

	create table #relations (
		ID int identity,
		StartPromotedObjectID int,
		StartPromotedObjectType varchar(25),		
		StartIntersectTypeNodeID int,
		StartPromotedObjectTypeID int,
		EndFusionAttributeID int,
		EndPromotedObjectType varchar(25),		
		EndIntersectTypeNodeID int,
		EndPromotedObjectTypeID int,
		IntersectTypeID int		
	);

	--Load any promoted fusionattributes that dont already have relationships back to the original object
	insert into #relations
		select
			fap.ObjectID as StartPromotedObjectID,
			fap.ObjectType as StartPromotedObjectType,			
			-1 as StartIntersectTypeNodeID,
			fap.ObjectTypeID as StartPromotedObjectTypeID,
			fap.fusionattributeid as EndFusionAttributeID,
			'FusionAttribute' as EndPromotedObjectType,			
			-1 as EndIntersectTypeNodeID,
			fa.fusionattributetypeid as EndPromotedObjectTypeID,
			-1 as IntersectTypeID			
		from 
			dbo.fusionattributepromotion fap	
			inner join dbo.fusionattribute fa on (fap.fusionattributeid = fa.id)		
		where
			not exists ( select 1 from dbo.[intersectnode] inode 
							inner join dbo.[intersectnode] inode2 on(inode.intersectid = inode2.intersectid)
						where inode.objecttype = 'FusionAttribute' and inode.objectid = fap.fusionattributeid
							and inode2.objecttype = fap.objecttype and inode2.objectid = fap.objectid)
			and fap.objecttype != 'Intersect'
			and fap.ObjectTypeID > 0
			and fa.fusionattributetypeid > 0
				
	--Load the relationship types 
	update R
	set
		R.StartIntersectTypeNodeID = RelTypes.SourceIntersectTypeNodeID, 
		R.EndIntersectTypeNodeID = RelTypes.TargetIntersectTypeNodeID,
		R.IntersectTypeID = RelTypes.IntersectTypeID
	from #relations as R
	inner join utility.RelationshipTypes RelTypes on (RelTypes.SourceObjectType = R.StartPromotedObjectType + 'Type' and RelTypes.TargetObjectType = R.EndPromotedObjectType + 'Type' and RelTypes.SourceObjectID = R.StartPromotedObjectTypeID and RelTypes.TargetObjectID = R.EndPromotedObjectTypeID)
		
	

	-- create an relations that we still have -1 start / end type node ids
	declare @unresolvedrelations RelationshipTypeTable;

	insert into @unresolvedrelations select distinct startpromotedobjecttype, startpromotedobjecttypeid, endpromotedobjecttype, endpromotedobjecttypeid from #relations;
		

	-- create any new relations as needed
	exec [dbo].[AddRelationshipTypesBulk] @unresolvedrelations
	
	update R
	set
		R.StartIntersectTypeNodeID = RelTypes.SourceIntersectTypeNodeID, 
		R.EndIntersectTypeNodeID = RelTypes.TargetIntersectTypeNodeID,
		R.IntersectTypeID = RelTypes.IntersectTypeID
	from #relations as R
	inner join utility.RelationshipTypes RelTypes on (RelTypes.SourceObjectType = R.StartPromotedObjectType + 'Type' and RelTypes.TargetObjectType = R.EndPromotedObjectType + 'Type' and RelTypes.SourceObjectID = R.StartPromotedObjectTypeID and RelTypes.TargetObjectID = R.EndPromotedObjectTypeID)
	
		
	
	-- add new relations for FUSION TO OBJECT
	If EXISTS (SELECT 1 FROM #relations)		
	begin

		BEGIN TRAN

		BEGIN TRY
			Declare @IDList Table(IntersectID int,RelID Int);
			--insert intersect records and save there id's
			-- trick is to use merge to keep the sequence id and staging row ids
			-- http://stackoverflow.com/questions/15614261/using-output-clause-to-insert-value-not-in-inserted
			MERGE
						INTO    [Intersect] d
						USING   (
									select rel.IntersectTypeID as isectid, 2 as class, rel.ID as srID from #relations rel						
								) s
						ON      (1 = 0)
						WHEN NOT MATCHED THEN
						INSERT  (IntersectTypeID, Classification, Description)
						VALUES  (isectid, class, NULL)
						OUTPUT  INSERTED.ID, s.srID into @IDList;

			--insert start records into intersect node
			INSERT INTO IntersectNode	(IntersectTypeNodeID, IntersectID, ObjectType, ObjectID)
					select sr.StartIntersectTypeNodeID, il.IntersectID, 'Intersect' /*sr.StartPromotedObjectType*/,sr.StartPromotedObjectID from #relations sr inner join @IDList il on (sr.ID = il.RelID);
					
			--insert end records into intersect node
			INSERT INTO IntersectNode	(IntersectTypeNodeID, IntersectID, ObjectType, ObjectID)
					select sr.EndIntersectTypeNodeID, il.IntersectID, sr.EndPromotedObjectType,sr.EndFusionAttributeID from #relations sr inner join @IDList il on (sr.ID = il.RelID);
				
			COMMIT TRAN

			declare @Intersects IDTable;
			insert into @Intersects select idl.intersectid from @IDList idl;
			
			declare @IntersectCount int
			select @IntersectCount = count(1) from @Intersects
			if @IntersectCount > 0 
			begin
				EXEC cache.SynchronizeRelationships @Intersects
			end

		END TRY
		BEGIN CATCH
			PRINT 'ERROR'
		  ROLLBACK TRAN
		END CATCH  
	end
	
	IF OBJECT_ID('tempdb..#relations') IS NOT NULL
		DROP TABLE #relations;	
END