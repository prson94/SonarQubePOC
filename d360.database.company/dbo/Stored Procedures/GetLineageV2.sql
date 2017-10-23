CREATE PROCEDURE [dbo].[GetLineageV2]
(
	@objectType varchar(50),
	@objectId int
)
AS
BEGIN

	if OBJECT_ID('tempdb..#maps') IS NOT NULL DROP TABLE #maps;

	--TESTING--------
	--declare @objectType varchar(50);
	--declare @objectId int;

	--select @objectType = 'Artifact', @objectId = 972074;
	-----------------

	create table #maps
	(
		id int not null,
		mapId int not null,
		visited bit not null default 0
	);

	--get any maps related directly to the object
	insert into #maps (id, mapId)
	select id, subjectid 
	from [intersect] where [object] = @objectType and objectId = @objectId and [subject] = 'Map';

	declare @i int;
	select @i = count(*) from #maps where visited = 0;

	--find all map-map relationships based on the initial maps above
	--iterative approach here makes cycle checks much easier but not sure about performance
	while @i != 0
	begin
		declare @rowId int;
		declare @mapId int;
		select top 1 @rowId = id, @mapId = mapId from #maps where visited = 0; 

		update #maps
		set visited = 1
		where id = @rowId;

		insert into #maps (id, mapId)
		select id, objectId from [intersect] where subject = 'Map' and subjectid = @mapId and object = 'Map' and id not in (select id from #maps);

		insert into #maps (id, mapId)
		select id, subjectid from [intersect] where object = 'Map' and objectid = @mapId and subject = 'Map' and id not in (select id from #maps);
		

		select @i = count(*) from #maps where visited = 0;
	end

	--now that we have all relevant maps, we can form our link/node data
	declare @links table 
	(
		intersectId int, 
		[from] varchar(150), 
		[to] varchar(150)
	);

	declare @nodes table
	(
		[key] varchar(150),
		[object] varchar(50),
		objectId int,
		objectType varchar(50),
		objectTypeId int,
		objectTypeName varchar(150),
		backColor  varchar(10),
		foreColor varchar(10),
		[name] varchar(150),
		isGroup bit,
		[group] varchar(150),
		[order] int,
		intersectTypeId int,
		businessTransformation nvarchar(max),
		technicalTransformation nvarchar(max),
		category varchar(50)
	);

	--links
	insert into @links
	select i.id as intersectId, i.subject + '|' + cast(i.subjectid as varchar) as [from], i.object + '|' + cast(i.objectid as varchar) as 'to' from [intersect] i
	inner join ( select distinct mapId from #maps) m on m.mapId = i.subjectId
	where i.subject = 'Map' and i.object != 'Map'
	union
	select i.id as intersectId, i.subject + '|' + cast(i.subjectid as varchar) as [from], i.object + '|' + cast(i.objectid as varchar) as 'to'  from #maps m
	inner join [intersect] i on i.id = m.id;


	--insert nodes for the transformations
	insert into @nodes
	select
		'MapGroup|' + cast(g.ID as varchar) as [key]
		,'MapGroup' as object
		,g.ID as objectId
		,null as objectType
		,null as objectTypeId
		,null as objectTypeName
		,null as backColor
		,null as foreColor
		,coalesce(g.BusinessTransformation, g.TechnicalTransformation) as name
		,1 as isGroup
		,null as [group]
		,null as [order]
		,null as intersectTypeId
		,g.BusinessTransformation as businessTransformation
		,g.TechnicalTransformation as technicalTransformation
		,'transform' as category 
	from 
		MapGroup g
	inner join MapGroupItem i on i.MapGroupID = g.ID
	inner join #maps m on m.MapID = i.ObjectID

	--nodes
	insert into @nodes
	select 
		 i.subject + '|' + cast(i.subjectId as varchar) as [key]
		,i.subject as [object]
		,i.subjectid as [objectid]
		,case when i.subject = 'Map' then 'MapType' else ta.Object end as objectType
		,case when i.subject = 'Map' then T.SubjectID else ta.ObjectID end as objectTypeId
		,ta.Name as objectTypeName
		,s.IconBackColor as backColor
		,s.IconForeColor as foreColor
		,utility.GetAssetDisplayValue(a.ID) as [name]
		,case when i.subject = 'Map' then 1 else 0 end as isGroup
		,null as [group]
		,case when i.subject = 'Map' then null else coalesce(o.[Order],99999) end as [order]
		,case when i.subject = 'Map' then null else t.ID end as intersectTypeId
		,null as businessTransformation
		,null as technicalTransformation
		,case when i.subject = 'Map' then 'map' else 'object' end as category
	from [intersect] i
	inner join @links l on l.intersectId = i.id
	left join Asset a on a.[object] = i.subject and a.objectId = i.subjectid
	left join AssetType ta on ta.ID = a.AssetTypeID
	left join ObjectStyle s on s.[objecttype] = ta.[object] and s.objectid = ta.objectid
	left join IntersectType t on t.ID = i.IntersectTypeID
	left join MapTypeOrder o on o.IntersectTypeID = t.ID
	union
	select 
		 i.object + '|' + cast(i.objectid as varchar) as [key]
		,i.object as [object]
		,i.objectid as [objectid] 
		,case when i.object = 'Map' then 'MapType' else ta.Object end as objectType
		,case when i.object = 'Map' then T.ObjectID else ta.ObjectID end as objectTypeId
		,ta.Name as objectTypeName
		,s.IconBackColor as backColor
		,s.IconForeColor as foreColor
		,utility.GetAssetDisplayValue(a.ID) as [name]
		,case when i.object = 'Map' then 1 else 0 end as isGroup
		,case when i.object != 'Map' and i.subject = 'Map' then
			'Map|' + cast(i.subjectid as varchar)
		else
			null
		end as [group]
		,case when i.object = 'Map' then null else coalesce(o.[Order],99999) end as [order]
		,case when i.object = 'Map' then null else t.ID end  as intersectTypeId
		,null as businessTransformation
		,null as technicalTransformation
		,case when i.[object] = 'Map' then 'map' else 'object' end as category
	from [intersect] i
	inner join @links l on l.intersectId = i.id
	left join Asset a on a.[object] = i.[object] and a.objectId = i.objectId
	left join AssetType ta on ta.ID = a.AssetTypeID
	left join ObjectStyle s on s.[objecttype] = ta.[object] and s.objectid = ta.objectid
	left join IntersectType t on t.ID = i.IntersectTypeID
	left join MapTypeOrder o on o.IntersectTypeID = t.ID;

	--we don't need links to the individual objects since they live in the maps
	delete l
	from @links l
	inner join [intersect] i on i.id = l.intersectId
	where i.subject = 'Map' and i.object != 'Map';

	--top 1 node in each map becomes the map's title object
	update n
		set
		n.[name] = coalesce(t.[name],''),
		n.backColor = t.backColor,
		n.foreColor = t.foreColor
	from @nodes n
	cross apply (
		select top 1 
			min(coalesce(n2.[order],99999)) as ord, 
			n2.[group] 
		from 
			@nodes n2
		where 
			n2.category != 'transform' and n2.[group] = n.[key]
		group by 
			[group]
		) r
	left join @nodes t on t.[key] = (select top 1 n3.[key] from @nodes n3 where n3.[group] = r.[group] and n3.[order] = r.ord and n3.category = 'object')
		and t.[group] = n.[key]
	where n.[object] = 'Map';

	--associate nodes with their transformations
	update n
	set
		n.[group] = n2.[key]
	from @nodes n
	inner join MapGroupItem i on i.ObjectID = n.objectId
	inner join @nodes n2 on n2.category = 'transform' and n2.[key] = 'MapGroup|' + cast(i.MapGroupID as varchar)
	where n.category = 'map';


	--set focal nodes
	update @nodes
	set category = 'focal'
	where category = 'object' and [object] = @objectType and objectId = @objectId

	--TESTING------------
	--select * from @links;
	--select * from @nodes;
	---------------------

	--return the results as a json object
	select	(
					select	*
					from	@links
					for json path			
					) as 'links',
					(
					select	distinct
							*
					from	@nodes
					for json path			
					) as 'nodes'
			for json path, WITHOUT_ARRAY_WRAPPER;

END

