create procedure [dbo].[GetHierarchyByMapType]
	@type varchar(50),
	@id int,
	@mapType int
as
begin

 declare @results table (ID int, [Subject] varchar(150), SubjectID int, [Object] varchar(150),
	 ObjectID int, ObjectType varchar(150), ObjectTypeID int,
	 ParentID varchar(max), Name varchar(250), Path varchar(250), Url varchar(50),
	 ObjectTypeName varchar(100), [Level] int, PredicateID int, PredicatePhrase varchar(350), [Type] int, GroupNumber int, [UID] varchar(max));

 declare @results2 table (ID int, [Subject] varchar(150), SubjectID int, [Object] varchar(150),
	 ObjectID int, ObjectType varchar(150), ObjectTypeID int,
	 ParentID varchar(max), Name varchar(250), Path varchar(250), Url varchar(50),
	 ObjectTypeName varchar(100), [Level] int, PredicateID int, PredicatePhrase varchar(350), [Type] int, GroupNumber int, [UID] varchar(max));


with u as
(
	select  
		m.id as ID,
		n1.objecttype as [Subject], 
		n1.objectid as [SubjectID],
		n2.objecttype as [Object],
		n2.objectid as [ObjectID],
		d.ObjectType as ObjectType,
		d.ObjectTypeID as ObjectTypeID,
		cast('0' as varchar(max)) as ParentID,
		d.Name,
		cast(d.name as varchar(500)) as [Path],
		d.url as Url,
		d.objecttypename as ObjectTypeName, 
		-1 as [Level],
		m.PredicateID as PredicateID,
		coalesce(p.Name,'') + '/' + coalesce(p.Inverse,'') as PredicatePhrase,
		m.[Type] as [Type],
		coalesce(g.GroupNumber,-1) as GroupNumber,
		cast((n1.objecttype + cast(n1.objectid as varchar(10))) as varchar(max)) as [UID]
	from intersectmap m
	join intersectnode n1 on n1.id = m.subjectintersectnodeid
	join intersectnode n2 on n2.id = m.objectintersectnodeid
	join cache.objectdetails d on d.object = n1.objecttype and d.objectid = n1.objectid
	join predicate p on p.id = m.predicateid
	left join intersectmapgroup g on @mapType = 4 and g.intersectmapid = m.id
	where n2.objecttype = @type and n2.objectid = @id and m.[type] = @mapType

	union all

	select 
		m.id as ID,
		n1.objecttype as [Subject], 
		n1.objectid as [SubjectID],
		n2.objecttype as [Object],
		n2.objectid as [ObjectID],
		d.ObjectType as ObjectType,
		d.ObjectTypeID as ObjectTypeID,
		u.UID as ParentID,
		d.Name, 
		cast(d.name + '/' + u.[Path] as varchar(500)) as [Path],
		d.url as Url,
		d.objecttypename as ObjectTypeName, 
		u.[Level]-1 as [Level],
		m.PredicateID as PredicateID,
		coalesce(p.Name,'') + '/' + coalesce(p.Inverse,'') as PredicatePhrase,
		m.[Type] as [Type],
		u.GroupNumber,
		cast((n1.objecttype + cast(n1.objectid as varchar(10))) as varchar(max)) as [UID]
	from intersectmap m
	join intersectnode n1 on n1.id = m.subjectintersectnodeid
	join intersectnode n2 on n2.id = m.objectintersectnodeid
	join cache.objectdetails d on d.object = n1.objecttype and d.objectid = n1.objectid
	join predicate p on p.id = m.predicateid
	join u on u.[Subject] = n2.objecttype and u.[SubjectID] = n2.objectid and (u.[subject] + cast(u.[subjectid] as varchar(10))) != (u.[object] + cast(u.[objectid] as varchar(10)))
	join (
		select -1 as groupnumber, -1 as intersectmapid
		union all
		select groupnumber,intersectmapid from intersectmapgroup
	) g on (u.groupnumber = -1 and g.groupnumber = u.groupnumber) or (@mapType = 4 and g.groupnumber = u.groupnumber and g.intersectmapid = m.id)
	where m.[type] = @mapType
)
insert into @results
select distinct * from u order by u.uid asc;


declare @UID varchar(500);
select top 1 @UID = r.[UID] from @results r
join @results c on c.ParentID = r.[UID] 
where r.ParentID = '0' and c.[UID] != r.[UID] and r.[Level] < 0;

--select * from @results;


while (@UID is not null)
begin

	update @results
	set ParentID = (select top 1 [UID] from @results r where r.ParentID = @UID)
	where [UID] = @UID;

	update @results
	set ParentID = '0'
	where [UID] = (select ParentID from @results where [UID] = @UID and [Level] < 0);

	if (select count(*) from @results r
		join @results c on c.ParentID = @UID
		where r.ParentID = '0' and c.[UID] != r.[UID] and r.[Level] < 0) > 0
	begin
		select top 1 @UID = r.[UID] from @results r
		join @results c on c.ParentID = r.[UID] 
		where r.ParentID = '0' and c.[UID] != r.[UID] and r.[Level] < 0;
	end
	else
	begin
		select @UID = null;
	end

end

insert into @results
	select 
		r.ID as ID,
		t.[type] as [Subject], 
		t.[id] as SubjectID,
		t.[type]as [Object],
		t.[id] as [ObjectID],
		d.ObjectType as ObjectType,
		d.ObjectTypeID as ObjectTypeID,
		cast(r.[UID] as varchar(500)) as ParentID,
		d.Name,
		cast(d.name as varchar(500)) as [Path],
		d.url as Url,
		d.objecttypename as ObjectTypeName, 
		0 as [Level],
		r.PredicateID as PredicateID,
		r.PredicatePhrase as PredicatePhrase,
		t.mapType as [Type],
		r.GroupNumber,
		'root' + r.[UID] as [UID]
	from @results r 
	join (select @type as [type], @id as [id], @mapType as mapType) t on 1=1
	join cache.objectdetails d on d.[object] = t.[type] and d.objectid = t.id
	where r.[Level] = -1;

insert into @results
	select 
		r.ID as ID,
		t.[type] as [Subject], 
		t.[id] as SubjectID,
		t.[type]as [Object],
		t.[id] as [ObjectID],
		d.ObjectType as ObjectType,
		d.ObjectTypeID as ObjectTypeID,
		cast(null as varchar(max)) as ParentID,
		d.Name,
		cast(d.name as varchar(500)) as [Path],
		d.url as Url,
		d.objecttypename as ObjectTypeName, 
		0 as [Level],
		r.PredicateID as PredicateID,
		r.PredicatePhrase as PredicatePhrase,
		t.mapType as [Type],
		r.GroupNumber,
		'root' as [UID]
	from @results r 
	join (select @type as [type], @id as [id], @mapType as mapType) t on 1=1
	join cache.objectdetails d on d.[object] = t.[type] and d.objectid = t.id
	where r.[Level] = 1;

if (select count(*) from @results) < 1
begin
	insert into @results
	select 
		0 as ID,
		t.[type] as [Subject], 
		t.[id] as SubjectID,
		t.[type]as [Object],
		t.[id] as [ObjectID],
		d.ObjectType as ObjectType,
		d.ObjectTypeID as ObjectTypeID,
		cast(0 as varchar(max)) as ParentID,
		d.Name,
		cast(d.name as varchar(500)) as [Path],
		d.url as Url,
		d.objecttypename as ObjectTypeName, 
		0 as [Level],
		null as PredicateID,
		null as PredicatePhrase,
		t.mapType as [Type],
		-1 as GroupNumber,
		'root' as [UID]
	from (select @type as [type], @id as [id], @mapType as mapType) t
	join cache.objectdetails d on d.[object] = t.[type] and d.objectid = t.id;

end;

declare @parent int;
select @parent = min([Level]) from @results;

--select * from @results;

 with z as
(
	select 
		m.id as ID,
		n1.objecttype as [Subject], 
		n1.objectid as SubjectID,
		n2.objecttype as [Object],
		n2.objectid as [ObjectID],
		d.ObjectType as ObjectType,
		d.ObjectTypeID as ObjectTypeID,
		cast(r.[UID] as varchar(max)) as ParentID,
		d.Name,
		cast(d.name as varchar(500)) as [Path],
		d.url as Url,
		d.objecttypename as ObjectTypeName, 
		1 as [Level],
		m.PredicateID as PredicateID,
		coalesce(p.Name,'') + '/' + coalesce(p.Inverse,'') as PredicatePhrase,
		m.[Type] as [Type],
		coalesce(g.GroupNumber,-1) as GroupNumber,
		cast((n1.objecttype + cast(n1.objectid as varchar(10)) + n2.objecttype + cast(n2.objectid as varchar(10))) as varchar(max)) as [UID]
	from intersectmap m
	join intersectnode n1 on n1.id = m.subjectintersectnodeid
	join intersectnode n2 on n2.id = m.objectintersectnodeid
	join cache.objectdetails d on d.object = n2.objecttype and d.objectid = n2.objectid
	join predicate p on p.id = m.predicateid
	join @results r on r.[subject] = n1.objecttype and r.subjectid = n1.objectid and ((@mapType != 4 and r.[Level] = @parent) or (@mapType = 4 and r.ParentID = '0'))
	left join intersectmapgroup g on @mapType = 4 and ((r.groupnumber = -1) or (g.groupnumber = r.groupnumber)) and g.intersectmapid = m.id
	where m.[type] = @mapType
	
	union all
	
	select 
		m.id as ID,
		n1.objecttype as [Subject], 
		n1.objectid as [SubjectID],
		n2.objecttype as [Object],
		n2.objectid as [ObjectID],
		d.ObjectType as ObjectType,
		d.ObjectTypeID as ObjectTypeID,
		z.[UID] as ParentID,
		d.Name, 
		cast(z.[path] + '/' + d.name as varchar(500)) as [Path],
		d.url as Url,
		d.objecttypename as ObjectTypeName, 
		z.[Level]+1 as [Level],
		m.PredicateID as PredicateID,
		coalesce(p.Name,'') + '/' + coalesce(p.Inverse,'') as PredicatePhrase,
		m.[Type] as [Type],
		z.GroupNumber,
		cast((z.UID + n2.objecttype + cast(n2.objectid as varchar(10))) as varchar(max)) as [UID]
	from intersectmap m
	join intersectnode n1 on n1.id = m.subjectintersectnodeid
	join intersectnode n2 on n2.id = m.objectintersectnodeid
	join cache.objectdetails d on d.object = n2.objecttype and d.objectid = n2.objectid
	join predicate p on p.id = m.predicateid
	join z on z.[Object] = n1.objecttype and z.[ObjectID] = n1.objectid
	join (
		select -1 as groupnumber, -1 as intersectmapid
		union all
		select groupnumber,intersectmapid from intersectmapgroup
	) g on (z.groupnumber = -1 and g.groupnumber = z.groupnumber) or (@mapType = 4 and g.groupnumber = z.groupnumber and g.intersectmapid = m.id)
	where m.[type] = @mapType
)
insert into @results2
select distinct * from z;

insert into @results2
select 
	r.[id],
	r.[subject],
	r.[subjectid],
	r.[object],
	r.[objectid],
	r.[objecttype],
	r.[objecttypeid],
	null as [ParentID],
	r.[name],
	r.[path],
	r.[url],
	r.[objecttypename],
	0 as [level],
	r.[predicateid],
	r.[predicatephrase],
	r.[type],
	r.[groupnumber],
	r.[uid]
from @results r
where ((@mapType != 4 and r.[Level] = @parent) or (@mapType = 4 and r.ParentID = '0'));

update r
set r.GroupNumber = p.GroupNumber
from @results2 r
join @results2 p on p.parentid = r.[uid]
where @mapType = 4 and r.id = 0;

update @results2
set predicatephrase = reverse(stuff(reverse(predicatephrase),1,1,''))
where reverse(predicatephrase) like '/%';



select * from @results2 
where (@mapType = 4 and groupnumber > -1 and (select count(*) from @results2) > 1) or
(@mapType = 4 and groupnumber = -1 and (select count(*) from @results2) = 1) or
(@mapType != 4)
order by [level] asc;

end
