CREATE PROCEDURE [utility].[PromoteFusionAttributesRelations]
	 @numberNewRelations int = 0 output
AS
BEGIN
	SET NOCOUNT ON;


	IF OBJECT_ID('tempdb..#relations') IS NOT NULL
		DROP TABLE #relations;

	create table #relations (
		ID int identity,
		StartFusionAttributeID int,
		StartPromotedObjectType varchar(25),
		StartPromotedObjectID int,
		StartIntersectTypeNodeID int,
		StartPromotedObjectTypeID int,
		EndFusionAttributeID int,
		EndPromotedObjectType varchar(25),
		EndPromotedObjectID int,
		EndIntersectTypeNodeID int,
		EndPromotedObjectTypeID int,
		IntersectTypeID int,
		IntersectID int
	);

	--insert existing relations between promoted items into temp table
	insert into #relations
		select
			fap.fusionattributeid as StartFusionAttributeID,
			fap.ObjectType as StartPromotedObjectType,
			fap.ObjectID as StartPromotedObjectID,
			-1 as StartIntersectTypeNodeID,
			fap.ObjectTypeID as StartPromotedObjectTypeID,
			fap2.fusionattributeid as EndFusionAttributeID,
			fap2.ObjectType as EndPromotedObjectType,
			fap2.ObjectID as EndPromotedObjectID,		
			-1 as EndIntersectTypeNodeID,
			fap2.ObjectTypeID as EndPromotedObjectTypeID,
			-1 as IntersectTypeID,
			inter.id as IntersectID
		from 
			dbo.fusionattributepromotion fap 
			inner join intersectnode inod on (inod.objectid = fap.fusionattributeid and inod.objecttype = 'FusionAttribute' and fap.objecttype != 'Intersect')	
			inner join intersectnode inod2 on (inod2.intersectid = inod.intersectid and inod2.objectid != inod.objectid and inod2.objecttype = 'FusionAttribute')
			inner join dbo.fusionattributepromotion fap2 on (inod2.objectid = fap2.fusionattributeid and fap2.objecttype != 'Intersect')
			inner join dbo.[intersect] inter on ( inter.id = inod2.intersectid)	
		where not exists
			( select 1 from dbo.fusionattributepromotion fapEx
				inner join intersectnode inodEx on (fapEx.ObjectID = inodEx.IntersectID and fapEx.ObjectType = 'Intersect' and inodEx.ObjectID = fap.ObjectID and inodEx.ObjectType = fap.ObjectType)
				inner join intersectnode inodEx2 on (inodEx.intersectID = inodEx2.intersectID and inodEx2.ObjectID = fap2.ObjectID and inodEx2.ObjectType = fap2.ObjectType)			
			)
			and fap.ObjectID != fap2.ObjectID;

	-- delete any objects we cant figure out the objecttypeid of 
	delete from #relations where EndPromotedObjectTypeID < 0 or StartPromotedObjectTypeID < 0;

	-- there will be two relations for each intersect on with either field starting .  Take just one.
	delete from #relations where ID in (
						select 
							a.ID 
						from 
							#relations a 
							inner join( select distinct intersectid, min(id) as id from #relations group by intersectid ) as b on (a.id = b.id) ) ;

	-- delete any duplicated relations if there are any
	delete from #relations where ID in (
						select 
							a.ID 
						from 
							#relations a 
							inner join( select
												StartFusionAttributeID,
												StartPromotedObjectType,
												StartPromotedObjectID,
												StartIntersectTypeNodeID,
												EndFusionAttributeID,
												EndPromotedObjectType,
												EndPromotedObjectID,
												EndIntersectTypeNodeID,
												IntersectTypeID, min(id) as id from #relations group by StartFusionAttributeID, StartPromotedObjectType, StartPromotedObjectID, StartIntersectTypeNodeID, EndFusionAttributeID, EndPromotedObjectType, EndPromotedObjectID, EndIntersectTypeNodeID, IntersectTypeID  having count(1) > 1) as b on (a.id = b.id) ) ;

	--load the intersect info for the promoted types
	
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
	
	-- rerun query to set the start end id's etc for newly created
	update R
	set
		R.StartIntersectTypeNodeID = RelTypes.SourceIntersectTypeNodeID, 
		R.EndIntersectTypeNodeID = RelTypes.TargetIntersectTypeNodeID,
		R.IntersectTypeID = RelTypes.IntersectTypeID
	from #relations as R
	inner join utility.RelationshipTypes RelTypes on (RelTypes.SourceObjectType = R.StartPromotedObjectType + 'Type' and RelTypes.TargetObjectType = R.EndPromotedObjectType + 'Type' and RelTypes.SourceObjectID = R.StartPromotedObjectTypeID and RelTypes.TargetObjectID = R.EndPromotedObjectTypeID)
		

	select @numberNewRelations = count(1) from #relations
	

	-- add new relations for promoted items
	If EXISTS (SELECT 1 FROM #relations)		
	begin
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
				select sr.StartIntersectTypeNodeID, il.IntersectID, sr.StartPromotedObjectType,sr.StartPromotedObjectID from #relations sr inner join @IDList il on (sr.ID = il.RelID);
					
		--insert end records into intersect node
		INSERT INTO IntersectNode	(IntersectTypeNodeID, IntersectID, ObjectType, ObjectID)
				select sr.EndIntersectTypeNodeID, il.IntersectID, sr.EndPromotedObjectType,sr.EndPromotedObjectID from #relations sr inner join @IDList il on (sr.ID = il.RelID);
				

		declare @Intersects IDTable;
		insert into @Intersects select idl.intersectid from @IDList idl;
			
		declare @IntersectCount int
		select @IntersectCount = count(1) from @Intersects
		if @IntersectCount > 0 
		begin
			EXEC cache.SynchronizeRelationships @Intersects
		end
	
		-- log the relations into the fusionattributepromotion table so  they dont get readded and we know we added them

		--start fusion id
		insert into dbo.fusionattributepromotion select r.StartFusionAttributeID as FusionAttributeID, 'Intersect', il.IntersectID,null,-1  from #relations r inner join @IDLIst il on (r.ID = il.RelID)
		-- end fusion id
		insert into dbo.fusionattributepromotion select r.EndFusionAttributeID as FusionAttributeID, 'Intersect', il.IntersectID,null,-1  from #relations r inner join @IDLIst il on (r.ID = il.RelID)
	end


	IF OBJECT_ID('tempdb..#relations') IS NOT NULL
		DROP TABLE #relations;
	
END
