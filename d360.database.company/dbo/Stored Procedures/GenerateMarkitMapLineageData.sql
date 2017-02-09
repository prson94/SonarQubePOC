create procedure [fusion].[GenerateMarkitMapLineageData]
	@fusionID int
as
begin
	SET NOCOUNT, ANSI_PADDING ON;
	SET ANSI_WARNINGS ON;

	declare @databaseName varchar(100);
	declare @sourceFieldTypeID int;
	declare @targetFieldTypeID int;		
	declare @mapFusionAttributeTypeID int = 710; -- this is fixed for all clients
	
	-- load the field ids for the source / target from mappings
	select @sourceFieldTypeID = id from fieldtype where [object] = 'FusionAttributeType' and [objectid] = @mapFusionAttributeTypeID and name = 'source';
	select @targetFieldTypeID = id from fieldtype where [object] = 'FusionAttributeType' and [objectid] = @mapFusionAttributeTypeID and name = 'target';
	
	IF @sourceFieldTypeID IS NULL
	begin		
		raiserror('ERROR - Cannot find the Markit Fusion Map Source Field.  Please make sure the latest markit fusion attribute types have been pushed to this environment', 16, -1);
		return;
	end

	IF @targetFieldTypeID IS NULL
	begin		
		raiserror('ERROR - Cannot find the Markit Fusion Map Target Field.  Please make sure the latest markit fusion attribute types have been pushed to this environment', 16, -1);
		return;
	end

	-- determine the database name
	select top 1 @databaseName = replace(sourceid, name,'') from fusionattribute where fusionid = 58 and fusionattributetypeid = 711;

	if @databaseName is null
	begin
		raiserror('ERROR - Cannot determine the database name to strip from markit fusion attribute data', 16, -1);
		return;
	end

	-- dont run if this is not a markit fusion
	declare @fusionTypeId int;
	select @fusionTypeId = FusionTypeID from [dbo].[Fusion] where ID = @fusionID;
	if @fusionTypeId != 13
	begin
		raiserror('ERROR - The fusion lineage generation process may only be run for the Markit Fusion Type', 16, -1);
		return;
	end

	-- dont run if no map records exist for this fusion
	if not exists( select 1 from fusionattribute where fusionid = @fusionID and fusionattributetypeid = @mapFusionAttributeTypeID )
	begin
		raiserror('ERROR - No Markit Fusion Map records exist for the specified Fusion ID', 16, -1);
		return;
	end

	-- figure out the database prefix from some markit data

	-- some logging
	declare @fusionName nvarchar(250);
	select @fusionName = name from [dbo].[fusion] where id = @fusionID;

	print 'Running For Fusion:' + @fusionName;
	print 'Using Target Field ID:' + cast(@targetFieldTypeID as varchar(100));
	print 'Using Source Field ID:' + cast(@sourceFieldTypeID as varchar(100));
	print 'Using Database prefix:' + @databaseName;
	-- end logging

	IF OBJECT_ID('tempdb..#maps') IS NOT NULL
		DROP TABLE #maps;

	create table #maps (	
		ID int identity primary key,			
		MapRuleItemID int,
		[ParentID] int,
		[UltimateParentID] int,
		[Level] int,
		SourceFusionAttributeID int,
		SourceFusionAttributeTypeID int,
		SourceObject nvarchar(500),		
		SourceParentObject nvarchar(max),
		SourceParentObjectFusionAttributeID int,
		SourceParentObjectFusionAttributeTypeID int,
		TargetFusionAttributeID int,
		TargetFusionAttributeTypeID int,
		TargetObject nvarchar(500),
		TargetParentObject nvarchar(max),
		TargetParentObjectFusionAttributeID int,
		TargetParentObjectFusionAttributeTypeID int,					
		[Source] varchar(50),
		[SourceID] int,	
		[Target] varchar(50),
		[TargetID] int,
	);

	CREATE NONCLUSTERED INDEX [CIX_TempMaps] ON #maps ( SourceFusionAttributeID ASC, TargetFusionAttributeID ASC );

	IF OBJECT_ID('tempdb..#objectmap') IS NOT NULL
		DROP TABLE #objectmap;

	create table #objectmap (
		MapID int,
		MapItemID int,
		[Object] varchar(50),
		[ObjectID] int,	
		[SourceIntersectID] int,		
		[TargetIntersectID] int		
	)

	CREATE NONCLUSTERED INDEX [CIX_TempObjectMap] ON #objectmap ( MapID ASC, [Object] ASC, [ObjectID] ASC );
	
	insert into #maps
		(SourceObject, TargetObject)
		select 
			replace(cast(F_source.formattedValue as nvarchar(500)), @databaseName, '') as SourceObject						
			, replace(cast(F_target.formattedValue as nvarchar(500)), @databaseName, '') as TargetObject			
		from 
			FusionAttribute FA
			inner join Field F_source on F_source.ObjectType = 'FusionAttribute' and F_source.ObjectID = FA.ID and F_source.FieldTypeID = @sourceFieldTypeID -- MAP SOURCE FIELD VALUE
			inner join Field F_target on F_target.ObjectType = 'FusionAttribute' and F_target.ObjectID = FA.ID and F_target.FieldTypeID = @targetFieldTypeID -- TARGET SOURCE FIELD VALUE
		where 
			FA.FusionID = @fusionID
				and
			FA.FusionAttributeTypeID = @mapFusionAttributeTypeID
			--	and
			--F_source.formattedValue like '%.cusip' or F_source.formattedValue like '%.ticker' or F_source.formattedValue like '%.cntry_of%' -- **for testing to limit to just cusip**;
	
	-- check how many map records we have
	declare @mapRecordCount int;
	select @mapRecordCount = count(1) from #maps
	if @fusionTypeId > 0
		begin
			print 'Loaded [' + cast(@mapRecordCount as varchar) + '] map records';			
		end
	else
		begin
			raiserror('ERROR - Could not load any map records this is most likely because there are no corresponding fusionattributes for the markit source/target mappings.', 16, -1);
			return;
		end

			
	--set the Source objects 
	update	T
	set		T.SourceFusionAttributeID = S.ID, T.SourceFusionAttributeTypeID = S.FusionAttributeTypeID
	from	#maps T			
			inner join fusionattribute S on (S.TextPath = T.SourceObject and S.FusionID = @fusionID)

	--set the Target Objects
	update	T
	set		T.TargetFusionAttributeID = S.ID, T.TargetFusionAttributeTypeID = S.FusionAttributeTypeID
	from	#maps T			
			inner join fusionattribute S on (S.TextPath = T.TargetObject and S.FusionID = @fusionID)

	--remove any source objects that we cant find the fusion attribute for
	delete from #maps where SourceFusionAttributeID is null or TargetFusionAttributeID is null		
	
	--set the source parent objects
	update T
	set T.SourceParentObject = FA_p.TextPath, T.SourceParentObjectFusionAttributeID = FA_p.ID, T.SourceParentObjectFusionAttributeTypeID = FA_p.FusionAttributeTypeID
	from #maps T
		inner join fusionattribute FA on (FA.ID = T.SourceFusionAttributeID)
		inner join fusionattribute FA_p on (FA_p.ID = FA.ParentID)

	--set the target parent objects
	update T
	set T.TargetParentObject = FA_p.TextPath, T.TargetParentObjectFusionAttributeID = FA_p.ID, T.TargetParentObjectFusionAttributeTypeID = FA_p.FusionAttributeTypeID
	from #maps T
		inner join fusionattribute FA on (FA.ID = T.TargetFusionAttributeID)
		inner join fusionattribute FA_p on (FA_p.ID = FA.ParentID)


	-- populate the previous step id this also duplicates items that have multiple paths and is very important
	update m_S
	set m_S.ParentID = m_T.ID
	from #maps m_T
	left outer join #maps m_S on (m_T.TargetFusionAttributeID = m_S.SourceFusionAttributeID)

	IF OBJECT_ID('tempdb..#levelMap') IS NOT NULL
		DROP TABLE #levelMap;
	
	;with C as
			(
			  select
				ID,
				SourceFusionAttributeID as SourceID,
				TargetFusionAttributeID as TargetID,
				ID as [UltimateParentID],
				0 as [level] 
			  from 
					#maps
			  where ParentID is null
			  union all
			  select 
					T.ID,
					T.SourceFusionAttributeID as SourceID,			 
					 T.TargetFusionAttributeID as TargetID,
					 C.[UltimateParentID] as [UltimateParentID],
					 C.[level] + 1
			  from #maps as T
				inner join C  
					on T.ParentID = C.ID				  
			)
			select C.ID, C.[level], C.[UltimateParentID]
			into #levelMap
			from C
			OPTION (MAXRECURSION 10) 

	update T
	set T.[level] = S.[level], T.[UltimateParentID] = S.[UltimateParentID]
	from #maps T
	inner join #levelMap S on S.ID = T.ID;
	
	--remove any that we cant find the level for
	delete from #maps where [level] is null		


	-- find any object related to column as the object	
	insert into #objectmap (MapID, [Object], [ObjectID])
		select T.ID, OI.[subject], OI.[subjectid]
		from #maps T
		inner join [IntersectDetail] OI on OI.Subject <> 'FusionAttribute' and OI.Object = 'FusionAttribute' and OI.ObjectID in (T.SourceFusionAttributeID, T.TargetFusionAttributeID)  and OI.PredicateType = 8-- look for relation between non fusion object and source/target column

	-- find any business terms related to source
	update T
	set T.[source] = OI.[subject], T.[sourceid] = OI.[subjectid]--, T.sourceintersectid = OI.ID
	from #maps T
		inner join [IntersectDetail] OI on OI.Subject <> 'FusionAttribute' and OI.Object = 'FusionAttribute' and OI.ObjectID = T.SourceParentObjectFusionAttributeID  and OI.PredicateType = 8 

	
	-- find any business terms related to target
	update T
	set T.[target] = OI.[subject], T.[targetid] = OI.[subjectid]--, T.targetintersectid = OI.ID
	from #maps T
		inner join [IntersectDetail] OI on OI.Subject <> 'FusionAttribute' and OI.Object = 'FusionAttribute' and OI.ObjectID = T.TargetParentObjectFusionAttributeID and OI.PredicateType = 8
		
	-- update the objects for each path to be the same	
	insert into #objectmap (MapID, [Object], [ObjectID])
		select T.ID, SO.[object], SO.[objectID]
		from #maps T		
		inner join #maps S on T.UltimateParentID = S.UltimateParentID
		inner join #objectmap SO on S.ID = SO.MapID
		left join #objectmap T_O on (T.ID = T_O.MapID and T_O.[object] is null);
	
	
	--take any sources with null targets find the next target

	WITH hierarchy (id, [target], [targetid], [source], [sourceid]) AS
	(
		SELECT id, [target], [targetid], [source], [sourceid]
		FROM #maps
		WHERE [parentid] is null

		UNION ALL

		SELECT mc.id, coalesce(mc.[target], mc.[source], gps.[target]) as [target], coalesce(mc.targetid, mc.sourceid, gps.targetid) as [targetid], coalesce(mc.[source], gps.[target], gps.[source]) as [source], coalesce(mc.sourceid, gps.targetid, gps.sourceid) as [targetid]
		FROM #maps mc
		JOIN hierarchy gps ON gps.id = mc.parentid
	)
	UPDATE T
	set T.[target] = cte.[target], T.[targetid] = cte.[targetid], T.[source] = cte.[source], T.[sourceid] = cte.[sourceid]
	from #maps T
	inner join 
		hierarchy cte
	on cte.id = T.id
	OPTION (MAXRECURSION 50)
			
	-- generate relationships for each unique object / source that dont exist

	update T
	set T.[sourceintersectid] = OI.ID
	from #objectmap T
		inner join #maps M on (T.MapID = M.ID)
		inner join [IntersectDetail] OI on OI.[Subject] = M.[Source] and OI.SubjectID = M.[SourceID] and OI.[Object] = T.[Object] and OI.[ObjectID] = T.[ObjectID];

	update T
	set T.[sourceintersectid] = OI.ID
	from #objectmap T
		inner join #maps M on (T.MapID = M.ID)
		inner join [IntersectDetail] OI on OI.[Object] = M.[Source] and OI.ObjectID = M.[SourceID] and OI.[Subject] = T.[Object] and OI.[SubjectID] = T.[ObjectID] and T.sourceintersectid is null
	
	-- add any missing relations to source / object
	insert into [intersect] (IntersectTypeID, Classification, [Subject], SubjectID, [Object], ObjectID, CreatedBy, CreatedOn, UpdatedBy, UpdatedOn, [Owner])
		select distinct
			(select top 1 i_t.ID from [intersecttype] i_t where (i_t.[object] = c_s.objecttype and i_t.[subject] = c_t.objecttype and i_t.objectid = c_s.objecttypeid and i_t.subjectid = c_t.objecttypeid))
			,2			
			,T.[Source]
			,T.[SourceID]
			,OM.[Object]
			,OM.[ObjectID]
			,0,getutcdate(),0,getutcdate(),'MARKIT LINEAGE'
		from #maps T
		inner join #objectmap OM on (T.ID = OM.MapID)
		inner join [cache].[objectdetails] c_s on (c_s.[object] = OM.[object] and c_s.[objectid] = OM.[objectid])
		inner join [cache].[objectdetails] c_t on (c_t.[object] = T.[source] and c_t.[objectid] = T.[sourceid])		
		where OM.sourceIntersectID is null;
	
	update OM
	set OM.[sourceintersectid] = OI.ID
	from #objectmap OM
		inner join #maps T on (OM.MapID = T.ID)		
		inner join [IntersectDetail] OI on OI.[Subject] = T.[Source] and OI.SubjectID = T.[SourceID] and OI.[Object] = OM.[Object] and OI.[ObjectID] = OM.[ObjectID] and OM.sourceintersectid is null;

	
	-- generate relationships for each unique object / target that dont exist	
	update OM
	set OM.[targetintersectid] = OI.ID
	from #objectmap OM
		inner join #maps T on (OM.MapID = T.ID)
		inner join [IntersectDetail] OI on OI.[Subject] = T.[Target] and OI.SubjectID = T.[TargetID] and OI.[Object] = OM.[Object] and OI.[ObjectID] = OM.[ObjectID]
		
	update OM
	set OM.[targetintersectid] = OI.ID
	from #objectmap OM
		inner join #maps T on (OM.MapID = T.ID)
		inner join [IntersectDetail] OI on OI.[Object] = T.[Target] and OI.ObjectID = T.[TargetID] and OI.[Subject] = OM.[Object] and OI.[SubjectID] = OM.[ObjectID] and OM.targetintersectid is null;

	-- add any missing relations to source / object
	insert into [intersect] (IntersectTypeID, Classification, [Subject], SubjectID, [Object], ObjectID, CreatedBy, CreatedOn, UpdatedBy, UpdatedOn, [Owner])
		select distinct
			(select top 1 i_t.ID from [intersecttype] i_t where (i_t.[object] = c_s.objecttype and i_t.[subject] = c_t.objecttype and i_t.objectid = c_s.objecttypeid and i_t.subjectid = c_t.objecttypeid))
			,2			
			,T.[target]
			,T.[targetID]
			,OM.[Object]
			,OM.[ObjectID]
			,0,getutcdate(),0,getutcdate(),'MARKIT LINEAGE'
		from #maps T
		inner join #objectmap OM on (T.ID = OM.MapID)
		inner join [cache].[objectdetails] c_s on (c_s.[object] = OM.[object] and c_s.[objectid] = OM.[objectid])
		inner join [cache].[objectdetails] c_t on (c_t.[object] = T.[target] and c_t.[objectid] = T.[targetid])		
		where OM.targetintersectid is null;
		
	update OM
	set OM.[targetintersectid] = OI.ID
	from #objectmap OM
		inner join #maps T on (OM.MapID = T.ID)
		inner join [IntersectDetail] OI on OI.[Subject] = T.[Target] and OI.SubjectID = T.[TargetID] and OI.[Object] = OM.[Object] and OI.[ObjectID] = OM.[ObjectID] and OM.targetintersectid is null;
			
	/*testing only!!*/			
--	select * from #maps order by [ultimateparentid], [level]
	/*end testing only*/

	print 'Removing any prior generated Markit Lineage map records';

	-- clear any previous values from map rule item map item table
	--delete from mapitem where [owner] = 'MARKIT LINEAGE';
	--delete from mapruleitem where [owner] = 'MARKIT LINEAGE';
	delete from mapruleitemmapitem where [owner] = 'MARKIT LINEAGE';

	print 'Inserting new map records';
	-- insert mapping data
	
	Declare @MapItemIDList Table(MapItemID int, sourceintersectid int, targetintersectid int);
	Declare @MapRuleItemIDList Table(MapRuleItemID int, MapID Int);
	
	-- load any existing map item instances
	update T
	set T.MapItemID = mi.ID
	from #objectmap T
		inner join mapitem mi on(T.sourceintersectid = mi.SourceIntersectID and T.targetintersectid = mi.TargetIntersectID and mi.[Owner] = 'MARKIT LINEAGE'); 

	-- insert map records
	MERGE
	INTO    mapitem mi
	USING   (			
			select distinct sourceintersectid, targetintersectid FROM #objectmap where (sourceintersectid is not null and targetintersectid is not null) and sourceintersectid != targetintersectid and mapitemid is null
			) S
	ON      (1 = 0)
	WHEN NOT MATCHED THEN
	INSERT  (SourceIntersectID, TargetIntersectID, CreatedBy, CreatedOn, UpdatedBy, UpdatedOn, [Owner])
	VALUES  (S.sourceintersectid, S.targetintersectid, 0, getutcdate(), 0, getutcdate(), 'MARKIT LINEAGE')
	OUTPUT  INSERTED.ID, S.sourceintersectid, S.targetintersectid into @MapItemIDList;

	--update map item id from main temp table
	update T
	set T.mapitemid = MI.MapItemID
	from #objectmap T
		inner join @MapItemIDList MI on (MI.sourceintersectid = T.sourceintersectid and MI.targetintersectid = T.targetintersectid)
		
	-- delete any mapitem records that are not in objectmap that are markit lineage
	delete from mapitem where [owner] = 'MARKIT LINEAGE' and id not in (select mapitemid from #objectmap);
	
	-- load id's of existing mapruleitems
	update T
	set T.mapruleitemid = S.id
	from #maps T
		inner join [dbo].[mapruleitem] S on (S.[owner] = 'MARKIT LINEAGE' and S.SourceFusionAttributeID = T.SourceFusionAttributeID and S.TargetFusionAttributeID = T.TargetFusionAttributeID);
	
	-- insert the mapruleitem records
	MERGE
	INTO    mapruleitem mri
	USING   (
			select SourceFusionAttributeID, TargetFusionAttributeID, ID from #maps where mapruleitemid is null
			) S
	ON      (1 = 0)
	WHEN NOT MATCHED THEN
	INSERT  (SourceFusionAttributeID, TargetFusionAttributeID, CreatedBy, CreatedOn, UpdatedBy, UpdatedOn, [Owner])
	VALUES  (S.SourceFusionAttributeID, S.TargetFusionAttributeID, 0, getutcdate(), 0, getutcdate(), 'MARKIT LINEAGE')
	OUTPUT  INSERTED.ID, S.ID into @MapRuleItemIDList;
	
	--update map rule item id from main temp table
	update T
	set T.MapRuleItemID = MI.MapRuleItemID
	from #maps T
		inner join @MapRuleItemIDList MI on (MI.MapID = T.ID);

	-- delete any mapitem records that are not in objectmap that are markit lineage
	delete from mapruleitem where [owner] = 'MARKIT LINEAGE' and id not in (select MapRuleItemID from #maps);
			
	--insert mapruleitemmapitem records
	insert into mapruleitemmapitem 
		(MapRuleItemID, MapItemID, [Owner])
		SELECT distinct M.MapRuleItemID, OM.MapItemID , 'MARKIT LINEAGE'
		FROM #maps M 
		inner join #objectmap OM on(M.ID = OM.MapID)
		where M.MapRuleItemID is not null and OM.MapItemID is not null;	

	declare @mapruleitemmapitemCount int;
	select @mapruleitemmapitemCount = count(1) from mapruleitemmapitem where [owner] = 'MARKIT LINEAGE'
	print 'Inserted [' + cast(@mapruleitemmapitemCount as varchar) + '] mapruleitemmapitem records';			

	declare @mapruleitemCount int;
	select @mapruleitemCount = count(1) from mapruleitem where [owner] = 'MARKIT LINEAGE'
	print 'Inserted [' + cast(@mapruleitemCount as varchar) + '] mapruleitem records';			

	declare @mapitemCount int;
	select @mapitemCount = count(1) from mapitem where [owner] = 'MARKIT LINEAGE'
	print 'Inserted [' + cast(@mapitemCount as varchar) + '] mapitem records';			
end
