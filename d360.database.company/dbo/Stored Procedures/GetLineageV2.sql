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

	--select @objectType = 'Artifact', @objectId = 733;
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
		[group] varchar(150)
	);

	--links
	insert into @links
	select i.id as intersectId, i.subject + '|' + cast(i.subjectid as varchar) as [from], i.object + '|' + cast(i.objectid as varchar) as 'to' from [intersect] i
	inner join ( select distinct mapId from #maps) m on m.mapId = i.subjectId
	where i.subject = 'Map' and i.object != 'Map'
	union
	select i.id as intersectId, i.subject + '|' + cast(i.subjectid as varchar) as [from], i.object + '|' + cast(i.objectid as varchar) as 'to'  from #maps m
	inner join [intersect] i on i.id = m.id;


	--nodes
	insert into @nodes
	select 
		 i.subject + '|' + cast(i.subjectId as varchar) as [key]
		,i.subject as [object]
		,i.subjectid as [objectid]
		,d.ObjectType as objectType
		,d.ObjectTypeID as objectTypeId
		,d.ObjectTypeName as objectTypeName
		,d.IconBackColor as backColor
		,d.IconForeColor as foreColor
		,d.[Name] as [name]
		,0 as isGroup
		,null as [group]
	from [intersect] i
	inner join @links l on l.intersectId = i.id
	left join cache.ObjectDetails d on d.[object] = i.[subject] and d.objectid = i.subjectid
	union
	select 
		 i.object + '|' + cast(i.objectid as varchar) as [key]
		,i.object as [object]
		,i.objectid as [objectid] 
		,d.ObjectType as objectType
		,d.ObjectTypeID as objectTypeId
		,d.ObjectTypeName as objectTypeName
		,d.IconBackColor as backColor
		,d.IconForeColor as foreColor
		,d.[Name] as [name]
		,0 as isGroup
		,case when i.object != 'Map' and i.subject = 'Map' then
			'Map|' + cast(i.subjectid as varchar)
		else
			null
		end as [group]
	from [intersect] i
	inner join @links l on l.intersectId = i.id
	left join cache.ObjectDetails d on d.[object] = i.object and d.objectid = i.objectid;

	delete l
	from @links l
	inner join [intersect] i on i.id = l.intersectId
	where i.subject = 'Map' and i.object != 'Map';

	update n
	set 
		n.[name] = m.name,
		n.backColor = m.backColor,
		n.foreColor = m.foreColor,
		n.isGroup = 1
	from @nodes n
	cross apply (
		select top 1 * from @nodes n2
		where n2.[group] = n.[key]
		) m
	where n.[object] = 'Map';

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
