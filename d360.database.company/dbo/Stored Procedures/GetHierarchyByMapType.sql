create procedure [dbo].[GetHierarchyByMapType]
	@type varchar(50),
	@id int,
	@mapType int
as
begin

declare @results table (ID int, [Subject] varchar(150), SubjectID int, [Object] varchar(150),
 ObjectID int, ParentID varchar(500), Name varchar(250), Path varchar(250), Url varchar(50),
  ObjectTypeName varchar(100), [Level] int, PredicateID int, [Type] int, [UID] varchar(500));

  declare @results2 table (ID int, [Subject] varchar(150), SubjectID int, [Object] varchar(150),
 ObjectID int, ParentID varchar(500), Name varchar(250), Path varchar(250), Url varchar(50),
  ObjectTypeName varchar(100), [Level] int, PredicateID int, [Type] int, [UID] varchar(500));


with u as
(
	select  
		m.id as ID,
		n1.objecttype as [Subject], 
		n1.objectid as [SubjectID],
		n2.objecttype as [Object],
		n2.objectid as [ObjectID],
		cast('0' as varchar(500)) as ParentID,
		d.Name,
		cast(d.name as varchar(500)) as [Path],
		d.url as Url,
		d.objecttypename as ObjectTypeName, 
		-1 as [Level],
		m.PredicateID as PredicateID,
		m.[Type] as [Type],
		cast((n1.objecttype + cast(n1.objectid as varchar(10))) as varchar(500)) as [UID]
	from intersectmap m
	join intersectnode n1 on n1.id = m.subjectintersectnodeid
	join intersectnode n2 on n2.id = m.objectintersectnodeid
	join cache.objectdetails d on d.object = n1.objecttype and d.objectid = n1.objectid
	where n2.objecttype = @type and n2.objectid = @id and m.[type] = @mapType

	union all

	select 
		m.id as ID,
		n1.objecttype as [Subject], 
		n1.objectid as [SubjectID],
		n2.objecttype as [Object],
		n2.objectid as [ObjectID],
		u.UID as ParentID,
		d.Name, 
		cast(d.name + '/' + u.[Path] as varchar(500)) as [Path],
		d.url as Url,
		d.objecttypename as ObjectTypeName, 
		u.[Level]-1 as [Level],
		m.PredicateID as PredicateID,
		m.[Type] as [Type],
		cast((n1.objecttype + cast(n1.objectid as varchar(10))) as varchar(500)) as [UID]
	from intersectmap m
	join intersectnode n1 on n1.id = m.subjectintersectnodeid
	join intersectnode n2 on n2.id = m.objectintersectnodeid
	join cache.objectdetails d on d.object = n1.objecttype and d.objectid = n1.objectid
	join u on u.[Subject] = n2.objecttype and u.[SubjectID] = n2.objectid and (u.[subject] + cast(u.[subjectid] as varchar(10))) != (u.[object] + cast(u.[objectid] as varchar(10)))
	where m.[type] = @mapType
)
insert into @results
select distinct * from u order by u.uid asc;

declare @UID varchar(500);
select top 1 @UID = r.[UID] from @results r
join @results c on c.ParentID = r.[UID] 
where r.ParentID = '0' and c.[UID] != r.[UID] and r.[Level] < 0;

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
		cast(r.[UID] as varchar(500)) as ParentID,
		d.Name,
		cast(d.name as varchar(500)) as [Path],
		d.url as Url,
		d.objecttypename as ObjectTypeName, 
		0 as [Level],
		r.PredicateID as PredicateID,
		t.mapType as [Type],
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
		cast(null as varchar(500)) as ParentID,
		d.Name,
		cast(d.name as varchar(500)) as [Path],
		d.url as Url,
		d.objecttypename as ObjectTypeName, 
		0 as [Level],
		r.PredicateID as PredicateID,
		t.mapType as [Type],
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
		cast(null as varchar(500)) as ParentID,
		d.Name,
		cast(d.name as varchar(500)) as [Path],
		d.url as Url,
		d.objecttypename as ObjectTypeName, 
		0 as [Level],
		null as PredicateID,
		t.mapType as [Type],
		'root' as [UID]
	from (select @type as [type], @id as [id], @mapType as mapType) t
	join cache.objectdetails d on d.[object] = t.[type] and d.objectid = t.id;

end;

 with z as
(
	select 
		m.id as ID,
		n1.objecttype as [Subject], 
		n1.objectid as SubjectID,
		n2.objecttype as [Object],
		n2.objectid as [ObjectID],
		cast(r.[UID] as varchar(500)) as ParentID,
		d.Name,
		cast(d.name as varchar(500)) as [Path],
		d.url as Url,
		d.objecttypename as ObjectTypeName, 
		1 as [Level],
		m.PredicateID as PredicateID,
		m.[Type] as [Type],
		cast((n2.objecttype + cast(n2.objectid as varchar(10))) as varchar(500)) as [UID]
	from intersectmap m
	join intersectnode n1 on n1.id = m.subjectintersectnodeid
	join intersectnode n2 on n2.id = m.objectintersectnodeid
	join cache.objectdetails d on d.object = n2.objecttype and d.objectid = n2.objectid
	join @results r on r.[subject] = n1.objecttype and r.subjectid = n1.objectid and r.[Level] = (select min([Level]) from @results)
	where m.[type] = @mapType
	
	union all
	
	select 
		m.id as ID,
		n1.objecttype as [Subject], 
		n1.objectid as [SubjectID],
		n2.objecttype as [Object],
		n2.objectid as [ObjectID],
		z.[UID] as ParentID,
		d.Name, 
		cast(z.[path] + '/' + d.name as varchar(500)) as [Path],
		d.url as Url,
		d.objecttypename as ObjectTypeName, 
		z.[Level]+1 as [Level],
		m.PredicateID as PredicateID,
		m.[Type] as [Type],
		cast((n2.objecttype + cast(n2.objectid as varchar(10))) as varchar(500)) as [UID]
	from intersectmap m
	join intersectnode n1 on n1.id = m.subjectintersectnodeid
	join intersectnode n2 on n2.id = m.objectintersectnodeid
	join cache.objectdetails d on d.object = n2.objecttype and d.objectid = n2.objectid
	join z on z.[Object] = n1.objecttype and z.[ObjectID] = n1.objectid
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
	null as [ParentID],
	r.[name],
	r.[path],
	r.[url],
	r.[objecttypename],
	0 as [level],
	r.[predicateid],
	r.[type],
	r.[uid]
from @results r
where r.[Level] = (select min([Level]) from @results);

select * from @results2 order by [level] asc;

end
