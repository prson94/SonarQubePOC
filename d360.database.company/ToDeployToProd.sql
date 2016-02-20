/*
--------------------------------------------------------------------------------------
 This file contains a list of SQL files that need to be executed when releasing 
 to production in the next cycle.
--------------------------------------------------------------------------------------
*/

DROP TABLE [fusion].[StagingError]
GO
DROP TABLE [fusion].[StagingItem]
GO
DROP TABLE [fusion].[StagingItemArchive]
GO
DROP TABLE [fusion].[StagingRelationArchive]
GO
DROP TABLE [fusion].[StagingRelationMapping]
GO
DROP TABLE [fusion].[StagingStatistic]
GO
DROP TABLE [fusion].[StepStatistic]
GO

DROP procedure [fusion].[ProcessFusionInQueue]
go

alter table [Load] add [UpdatedBy] INT NOT NULL CONSTRAINT [CK_Load_UpdatedBy] DEFAULT ((0))
go

CREATE TABLE [dbo].[IntersectMapGroup] (
    [IntersectMapID] INT NOT NULL,
    [GroupNumber]    INT NOT NULL,
    [ID]             INT IDENTITY (1, 1) NOT NULL
)
GO


CREATE procedure [dbo].[GetGroupHierarchy]
	@type varchar(50),
	@id int
as
begin


declare @mapType int;
set @mapType = 4;

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
		cast((g.groupnumber * -1) as varchar(max)) as ParentID,
		d.Name,
		cast(d.name as varchar(500)) as [Path],
		d.url as Url,
		d.objecttypename as ObjectTypeName, 
		-1 as [Level],
		m.PredicateID as PredicateID,
		coalesce(p.Name,'') + '/' + coalesce(p.Inverse,'') as PredicatePhrase,
		m.[Type] as [Type],
		coalesce(g.GroupNumber,-1) as GroupNumber,
		cast((n1.objecttype + cast(n1.objectid as varchar(10)) + '_' + cast(g.groupnumber as varchar(10))) as varchar(max)) as [UID]
	from intersectmap m
	join intersectnode n1 on n1.id = m.subjectintersectnodeid
	join intersectnode n2 on n2.id = m.objectintersectnodeid
	join cache.objectdetails d on d.object = n1.objecttype and d.objectid = n1.objectid
	join predicate p on p.id = m.predicateid
	join intersectmapgroup g on g.intersectmapid = m.id
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
		cast((n1.objecttype + cast(n1.objectid as varchar(10)) + '_' + cast(g.groupnumber as varchar(10))) as varchar(max)) as [UID]
	from intersectmap m
	join intersectnode n1 on n1.id = m.subjectintersectnodeid
	join intersectnode n2 on n2.id = m.objectintersectnodeid
	join cache.objectdetails d on d.object = n1.objecttype and d.objectid = n1.objectid
	join predicate p on p.id = m.predicateid
	join u on u.[Subject] = n2.objecttype and u.[SubjectID] = n2.objectid and (u.[subject] + cast(u.[subjectid] as varchar(10))) != (u.[object] + cast(u.[objectid] as varchar(10)))
	join intersectmapgroup g on g.intersectmapid = m.id and g.groupnumber = u.groupnumber
	where m.[type] = @mapType
)
insert into @results
select distinct * from u order by u.uid asc;


declare @UID varchar(500);
select top 1 @UID = r.[UID] from @results r
join @results c on c.ParentID = r.[UID] 
where r.ParentID like '-%' and c.[UID] != r.[UID] and r.[Level] < 0;

--select * from @results;


while (@UID is not null)
begin

	update @results
	set ParentID = (select top 1 [UID] from @results r where r.ParentID = @UID)
	where [UID] = @UID;

	update @results
	set ParentID = cast((groupnumber * -1) as varchar(max))
	where [UID] = (select ParentID from @results where [UID] = @UID and [Level] < 0);

	if (select count(*) from @results r
		join @results c on c.ParentID = @UID
		where r.ParentID like '-%' and c.[UID] != r.[UID] and r.[Level] < 0) > 0
	begin
		select top 1 @UID = r.[UID] from @results r
		join @results c on c.ParentID = r.[UID] 
		where r.ParentID like '-%' and c.[UID] != r.[UID] and r.[Level] < 0;
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
		cast('-0' as varchar(max)) as ParentID,
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
	join @results r on r.[subject] = n1.objecttype and r.subjectid = n1.objectid and coalesce(r.ParentID,'0') like '-%'
	join intersectmapgroup g on g.intersectmapid = m.id
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
	join intersectmapgroup g on g.intersectmapid = m.id and g.groupnumber = z.groupnumber
	where m.[type] = @mapType
)
insert into @results2
select distinct * from z;

--select * from @results2;

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
where r.ParentID like '-%';

update r
set r.GroupNumber = p.GroupNumber
from @results2 r
join @results2 p on p.parentid = r.[uid]
where r.id = 0;

update @results2
set predicatephrase = reverse(stuff(reverse(predicatephrase),1,1,''))
where reverse(predicatephrase) like '/%';



select * from @results2 
--where (groupnumber > -1 and (select count(*) from @results2) > 1) or
--(groupnumber = -1 and (select count(*) from @results2) = 1)
--order by [level] asc;


/*


--select * from @results;


 */
end
GO



alter procedure [dbo].[GetHierarchyByMapType]
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
		-1 as GroupNumber,
		cast((n1.objecttype + cast(n1.objectid as varchar(10))) as varchar(max)) as [UID]
	from intersectmap m
	join intersectnode n1 on n1.id = m.subjectintersectnodeid
	join intersectnode n2 on n2.id = m.objectintersectnodeid
	join cache.objectdetails d on d.object = n1.objecttype and d.objectid = n1.objectid
	join predicate p on p.id = m.predicateid
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
		-1 as GroupNumber,
		cast((n1.objecttype + cast(n1.objectid as varchar(10))) as varchar(max)) as [UID]
	from intersectmap m
	join intersectnode n1 on n1.id = m.subjectintersectnodeid
	join intersectnode n2 on n2.id = m.objectintersectnodeid
	join cache.objectdetails d on d.object = n1.objecttype and d.objectid = n1.objectid
	join predicate p on p.id = m.predicateid
	join u on u.[Subject] = n2.objecttype and u.[SubjectID] = n2.objectid and (u.[subject] + cast(u.[subjectid] as varchar(10))) != (u.[object] + cast(u.[objectid] as varchar(10)))
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
		-1 as GroupNumber,
		cast((n1.objecttype + cast(n1.objectid as varchar(10)) + n2.objecttype + cast(n2.objectid as varchar(10))) as varchar(max)) as [UID]
	from intersectmap m
	join intersectnode n1 on n1.id = m.subjectintersectnodeid
	join intersectnode n2 on n2.id = m.objectintersectnodeid
	join cache.objectdetails d on d.object = n2.objecttype and d.objectid = n2.objectid
	join predicate p on p.id = m.predicateid
	join @results r on r.[subject] = n1.objecttype and r.subjectid = n1.objectid and r.[Level] = @parent
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
		-1 as GroupNumber,
		cast((z.UID + n2.objecttype + cast(n2.objectid as varchar(10))) as varchar(max)) as [UID]
	from intersectmap m
	join intersectnode n1 on n1.id = m.subjectintersectnodeid
	join intersectnode n2 on n2.id = m.objectintersectnodeid
	join cache.objectdetails d on d.object = n2.objecttype and d.objectid = n2.objectid
	join predicate p on p.id = m.predicateid
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
where r.[Level] = @parent;

update @results2
set predicatephrase = reverse(stuff(reverse(predicatephrase),1,1,''))
where reverse(predicatephrase) like '/%';


select * from @results2 
order by [level] asc;

end
GO

alter procedure [dbo].[ProcessBulkLoad]
--declare
	@LoadID int
--set @LoadID = 4
as
begin
	set nocount on;

	declare @Object varchar(50),
			@ObjectID int,
			@Action varchar(1),
			@UpdatedBy int = 0

	select	@Object = [Object],
			@ObjectID = ObjectID,
			@Action = [Action],
			@UpdatedBy = UpdatedBy
	from	[Load]
	where	ID = @LoadID

	if @Action = 'P'	--PROMOTION
	begin
		-- PARSE any dynamic fields that are specifically lookups.
		update	T
		set		T.LookupObject = S.LookupObject,
				T.LookupObjectID = S.LookupObjectID
		from	LoadItemColumn T
				inner join	(
							select	IC.LoadID,
									IC.RowIndex,
									IC.ColumnIndex,
									case 
										when L_A.ID is not null then 'Artifact'
										when L_D.ID is not null then 'Domain'
										when L_DI.ID is not null then 'DomainItem'
										when L_F.ID is not null then 'FusionAttribute'
										when L_I.ID is not null then 'Intersect'
										when L_L.Value is not null then 'Lookup'
										when L_T.ID is not null then 'Taxonomy'
										else NULL
									end as LookupObject,
									coalesce(L_A.ID, L_D.ID, L_DI.ID, L_F.ID, L_I.ID, L_L.Value, L_T.ID) as LookupObjectID
							from	FieldType F
									inner join [Load] L on L.ID = @LoadID and L.[Object] = F.[Object] and L.ObjectID = F.ObjectID and F.[Type] = 'Lookup'
									inner join [LoadColumn] C on C.LoadID = L.ID and F.Name = C.Name
									inner join [LoadItemColumn] IC on IC.LoadID = C.LoadID and IC.ColumnIndex = C.ColumnIndex
								
									left join Artifact L_A on F.LookupObjectType in ('Artifact', 'ArtifactType') and L_A.ArtifactTypeID = F.LookupObjectID and (L_A.[Name] = IC.Value OR L_A.TextPath = IC.Value)
									left join Domain L_D on F.LookupObjectType in ('Domain', 'DomainType') and L_D.DomainTypeID = F.LookupObjectID and L_D.[Name] = IC.Value
									left join DomainItem L_DI on F.LookupObjectType = 'DomainItem' and L_DI.DomainID = F.LookupObjectID and L_DI.[Name] = IC.Value
									left join FusionAttribute L_F on F.LookupObjectType = 'FusionAttributeType' and L_F.FusionAttributeTypeID = F.LookupObjectID and (L_F.[Name] = IC.Value OR L_F.TextPath = IC.Value)
									left join [Intersect] L_I on F.LookupObjectType = 'IntersectType' and L_I.IntersectTypeID = F.LookupObjectID and L_I.[Name] = IC.Value
									left join [FieldLookupValue] L_L on F.ID = L_L.FieldTypeID and F.LookupObjectType = 'Lookup' and L_L.LookupObjectID = F.LookupObjectID and L_L.[Text] = IC.Value
									left join Taxonomy L_T on F.LookupObjectType in ('Taxonomy', 'TaxonomyType') and L_T.TaxonomyTypeID = F.LookupObjectID and (L_T.[Name] = IC.Value OR L_T.TextPath = IC.Value)
							where	F.[Type] = 'Lookup'
							) S on S.LoadID = T.LoadID and S.RowIndex = T.RowIndex and S.ColumnIndex = T.ColumnIndex

		-- PARSE any Subject AREA fields.  This is only in the case of artifacts.
		update	T
		set		T.LookupObject = S.LookupObject,
				T.LookupObjectID = S.LookupObjectID
		from	LoadItemColumn T
				inner join	(
							select	IC.LoadID,
									IC.RowIndex,
									IC.ColumnIndex,
									'TaxonomyType' as LookupObject,
									T.ID as LookupObjectID
							from	[Load] L 
									inner join [LoadColumn] C on L.ID = @LoadID and L.[Object] = 'ArtifactType' and C.LoadID = L.ID and C.Name = 'Subject Area'
									inner join [LoadItemColumn] IC on IC.LoadID = C.LoadID and IC.ColumnIndex = C.ColumnIndex
									inner join TaxonomyType T on T.[Name] = IC.Value
							) S on S.LoadID = T.LoadID and S.RowIndex = T.RowIndex and S.ColumnIndex = T.ColumnIndex

		-- PARSE any Domain Group fields.  This is only in the case of domains.
		update	T
		set		T.LookupObject = S.LookupObject,
				T.LookupObjectID = S.LookupObjectID
		from	LoadItemColumn T
				inner join	(
							select	IC.LoadID,
									IC.RowIndex,
									IC.ColumnIndex,
									'DomainGroup' as LookupObject,
									T.ID as LookupObjectID
							from	[Load] L 
									inner join [LoadColumn] C on L.ID = @LoadID and L.[Object] = 'DomainType' and C.LoadID = L.ID and C.Name = 'Domain Group'
									inner join [LoadItemColumn] IC on IC.LoadID = C.LoadID and IC.ColumnIndex = C.ColumnIndex
									inner join DomainGroup T on T.[Name] = IC.Value and T.DomainTypeID = @ObjectID
							) S on S.LoadID = T.LoadID and S.RowIndex = T.RowIndex and S.ColumnIndex = T.ColumnIndex

		-- PARSE any Parent Artifact fields.  This is only in the case of artifacts.
		update	T
		set		T.LookupObject = S.LookupObject,
				T.LookupObjectID = S.LookupObjectID
		from	LoadItemColumn T
				inner join	(
							select	IC.LoadID,
									IC.RowIndex,
									IC.ColumnIndex,
									'Artifact' as LookupObject,
									P.ID as LookupObjectID
							from	[Load] L 
									inner join ArtifactType T on L.ID = @LoadID and L.[Object] = 'ArtifactType' and L.ObjectID = T.ID
									inner join ArtifactType PT on PT.ID = T.ParentID
									inner join [LoadColumn] C on C.LoadID = L.ID and C.Name = 'Parent ' + PT.Name
									inner join [LoadItemColumn] IC on IC.LoadID = C.LoadID and IC.ColumnIndex = C.ColumnIndex
									inner join Artifact P on P.ArtifactTypeID = PT.ID and P.[Name] = IC.Value
							) S on S.LoadID = T.LoadID and S.RowIndex = T.RowIndex and S.ColumnIndex = T.ColumnIndex

		if @Object = 'AttributeType'
		begin
			-- Clean Owner Type field.
			update	LoadItemColumn
			set		Value = case when charindex('Type', Value) > 0 then Value else Value + 'Type' end
			where	LoadID = @LoadID and ColumnIndex = 1

			-- PARSE Owner Type fields.
			update	T
			set		T.LookupObject = S.LookupObject,
					T.LookupObjectID = S.LookupObjectID
			from	LoadItemColumn T
					inner join	(
								select	LI.LoadID,
										LI.RowIndex,
										C2.ColumnIndex,
										D.[Object] as LookupObject,
										D.ObjectID as LookupObjectID
								from	[Load] L
										inner join LoadItem LI on LI.LoadID = L.ID and L.ID = @LoadID
										inner join [LoadItemColumn] C1 on C1.LoadID = LI.LoadID and C1.RowIndex = LI.RowIndex and C1.ColumnIndex = 1 --'Owner Type' 
										inner join [LoadItemColumn] C2 on C2.LoadID = LI.LoadID and C2.RowIndex = LI.RowIndex and C2.ColumnIndex = 2 --'Owner Type Name'
										inner join cache.ObjectDetails D on D.[Object] = C1.Value and D.[Name] = C2.Value
								) S on S.LoadID = T.LoadID and S.RowIndex = T.RowIndex and S.ColumnIndex = T.ColumnIndex

			-- PARSE Owner fields.
			update	T
			set		T.LookupObject = S.LookupObject,
					T.LookupObjectID = S.LookupObjectID
			from	LoadItemColumn T
					inner join	(
								select	LI.LoadID,
										LI.RowIndex,
										C3.ColumnIndex,
										D.[Object] as LookupObject,
										D.ObjectID as LookupObjectID
								from	[Load] L
										inner join LoadItem LI on LI.LoadID = L.ID and L.ID = @LoadID
										--inner join [LoadItemColumn] C1 on	C1.LoadID = LI.LoadID	and C1.RowIndex = LI.RowIndex	and C1.ColumnIndex = 1 --'Owner Type' 
										inner join [LoadItemColumn] C2 on C2.LoadID = LI.LoadID and C2.RowIndex = LI.RowIndex and C2.ColumnIndex = 2 --'Owner Type Name'
										inner join [LoadItemColumn] C3 on C3.LoadID = LI.LoadID	and C3.RowIndex = LI.RowIndex and C3.ColumnIndex = 3 --'Owner Name'
										inner join cache.ObjectDetails D on D.[ObjectType] = C2.[LookupObject] and D.ObjectTypeID = C2.LookupObjectID and D.[Name] = C3.Value
								) S on S.LoadID = T.LoadID and S.RowIndex = T.RowIndex and S.ColumnIndex = T.ColumnIndex
		end

		declare @ResolvedObjects table ([Object] varchar(50), ObjectID int, [Action] varchar(25), LoadID int, RowIndex int)	--This captures the INSERTED/UPDATED objects from the merge statements below.

		if @Object = 'ArtifactType'
		begin
			merge	Artifact T
			using	(
					select	O.LoadID,
							O.RowIndex,
							O.ArtifactTypeID,
							O.Name,
							D.Description,
							O.ParentID,
							O.TaxonomyTypeID
					from	(
							select	LI.LoadID,
									MIN(LI.RowIndex) as RowIndex,
									@ObjectID as ArtifactTypeID,
									IC_N.Value as Name,
									P.ParentID,
									IC_T.LookupObjectID as TaxonomyTypeID
							from	[LoadItem] LI
									inner join [LoadItemColumn] IC_N on IC_N.LoadID = LI.LoadID and IC_N.RowIndex = LI.RowIndex inner join LoadColumn C_N on C_N.LoadID = LI.LoadID and C_N.ColumnIndex = IC_N.ColumnIndex and C_N.Name = 'Name'
									inner join [LoadItemColumn] IC_T on IC_T.LoadID = LI.LoadID and IC_T.RowIndex = LI.RowIndex inner join LoadColumn C_T on C_T.LoadID = LI.LoadID and C_T.ColumnIndex = IC_T.ColumnIndex and C_T.Name = 'Subject Area' and IC_T.LookupObjectID is not null
									outer apply (
												select	I.LookupObjectID as ParentID
												from	[LoadItemColumn] I
														inner join LoadColumn C on I.LoadID = LI.LoadID and I.RowIndex = LI.RowIndex 
																						and C.LoadID = LI.LoadID and C.ColumnIndex = I.ColumnIndex and C.Name like 'Parent %'
												) P
							where	LI.LoadID = @LoadID
							group by LI.LoadID,
									IC_N.Value,
									P.ParentID,
									IC_T.LookupObjectID
							) O
							outer apply (
								select	I.Value as Description
								from	[LoadItemColumn] I
										inner join LoadColumn C on I.LoadID = O.LoadID and I.RowIndex = O.RowIndex 
																		and C.LoadID = O.LoadID and C.ColumnIndex = I.ColumnIndex and C.Name = 'Description'
							) D
					) S
			on		(T.ArtifactTypeID = S.ArtifactTypeID and T.TaxonomyTypeID = S.TaxonomyTypeID and T.ParentID = S.ParentID and T.Name = S.Name)
			when	matched then
					update	set T.[Description] = S.[Description],
								T.[ParentID] = S.[ParentID],
								T.[Status] = 'Draft',
								T.TaxonomyTypeID = S.TaxonomyTypeID,
								T.UpdatedBy = @UpdatedBy,
								T.UpdatedOn = getutcdate()
			when	not matched then
					insert (ArtifactTypeID, TaxonomyTypeID, ParentID, Name, [Description], [Status], UpdatedOn, UpdatedBy)
					values (S.ArtifactTypeID, S.TaxonomyTypeID, S.ParentID, S.Name, S.[Description], 'Draft', getutcdate(), @UpdatedBy)
			output	'Artifact', inserted.ID, $action, S.LoadID, S.RowIndex into @ResolvedObjects;
		end
		else if @Object = 'AttributeType'
		begin
			merge	[Attribute] T
			using	(
					select	I.LoadID,
							I.RowIndex,
							@ObjectID as AttributeTypeID,
							C.LookupObject as [Object],
							C.LookupObjectID as ObjectID
					from	[LoadItem] I
							inner join [LoadItemColumn] C on I.LoadID = @LoadID and C.LoadID = I.LoadID and C.RowIndex = I.RowIndex and C.ColumnIndex = 3
							and C.LookupObject is not null
							and C.LookupObjectID is not null
					) S
			on		(T.AttributeTypeID = S.AttributeTypeID and T.[ObjectType] = S.[Object] and T.[ObjectID] = S.[ObjectID] and T.ParentID = NULL)-- and T.Name = S.Name)
			when	matched then
					update	set T.[UpdatedOn] = getutcdate(),
								T.UpdatedBy = @UpdatedBy
			when	not matched then
					insert (AttributeTypeID, ObjectType, ObjectID, UpdatedOn, UpdatedBy)
					values (S.AttributeTypeID, S.[Object], S.ObjectID, getutcdate(), @UpdatedBy)
			output	'Attribute', inserted.ID, $action, S.LoadID, S.RowIndex into @ResolvedObjects;		
		end
		else if @Object = 'Domain'
		begin
			merge	DomainItem T
			using	(
					select	distinct
							LI.LoadID,
							LI.RowIndex,
							@ObjectID as DomainID,
							IC_C.Value as Code,
							IC_N.Value as Name,
							D.[Description]
					from	[LoadItem] LI
							inner join [LoadItemColumn] IC_C on IC_C.LoadID = LI.LoadID and IC_C.RowIndex = LI.RowIndex inner join LoadColumn C_C on C_C.LoadID = LI.LoadID and C_C.ColumnIndex = IC_C.ColumnIndex and C_C.Name = 'Code'
							inner join [LoadItemColumn] IC_N on IC_N.LoadID = LI.LoadID and IC_N.RowIndex = LI.RowIndex inner join LoadColumn C_N on C_N.LoadID = LI.LoadID and C_N.ColumnIndex = IC_N.ColumnIndex and C_N.Name = 'Name'
							outer apply (
										select	I.Value as Description
										from	[LoadItemColumn] I
												inner join LoadColumn C on I.LoadID = LI.LoadID and I.RowIndex = LI.RowIndex 
																			 and C.LoadID = LI.LoadID and C.ColumnIndex = I.ColumnIndex and C.Name = 'Description'
										) D
					where	LI.LoadID = @LoadID
					) S
			on		(T.DomainID = S.DomainID and T.Code = S.Code)
			when	matched then
					update	set T.[Name] = S.[Name],
								T.[Description] = S.[Description],
								T.[DomainID] = S.[DomainID],
								T.UpdatedBy = @UpdatedBy,
								T.UpdatedOn = getutcdate()
			when	not matched then
					insert (DomainID, Code, Name, [Description], UpdatedOn, UpdatedBy)
					values (S.DomainID, S.Code, S.Name, S.[Description], getutcdate(), @UpdatedBy)
			output	'DomainItem', inserted.ID, $action, S.LoadID, S.RowIndex into @ResolvedObjects;
		end
		else if @Object = 'DomainType'
		begin
			merge	Domain T
			using	(
					select	distinct
							LI.LoadID,
							LI.RowIndex,
							@ObjectID as DomainTypeID,
							IC_N.Value as Name,
							D.[Description],
							IC_G.LookupObjectID as DomainGroupID
					from	[LoadItem] LI
							inner join [LoadItemColumn] IC_N on IC_N.LoadID = LI.LoadID and IC_N.RowIndex = LI.RowIndex inner join LoadColumn C_N on C_N.LoadID = LI.LoadID and C_N.ColumnIndex = IC_N.ColumnIndex and C_N.Name = 'Name'
							outer apply (
										select	I.Value as Description
										from	[LoadItemColumn] I
												inner join LoadColumn C on I.LoadID = LI.LoadID and I.RowIndex = LI.RowIndex 
																			 and C.LoadID = LI.LoadID and C.ColumnIndex = I.ColumnIndex and C.Name = 'Description'
										) D
							inner join [LoadItemColumn] IC_G on IC_G.LoadID = LI.LoadID and IC_G.RowIndex = LI.RowIndex inner join LoadColumn C_G on C_G.LoadID = LI.LoadID and C_G.ColumnIndex = IC_G.ColumnIndex and C_G.Name = 'Domain Group'
					where	LI.LoadID = @LoadID
					) S
			on		(T.DomainTypeID = S.DomainTypeID and T.Name = S.Name)
			when	matched then
					update	set T.[Description] = S.[Description],
								T.[DomainGroupID] = S.[DomainGroupID],
								T.UpdatedOn = getutcdate(),
								T.UpdatedBy = @UpdatedBy
			when	not matched then
					insert (DomainTypeID, DomainGroupID, Name, [Description], UpdatedOn, UpdatedBy)
					values (S.DomainTypeID, S.DomainGroupID, S.Name, S.[Description], getutcdate(), @UpdatedBy)
			output	'Domain', inserted.ID, $action, S.LoadID, S.RowIndex into @ResolvedObjects;
		end
		else if @Object = 'FusionAttributeType'
		begin
			select 1;
		end
		else if @Object = 'TaxonomyType'
		begin
		--begin tran

			declare @currentLevel int,
			@maxLevel int,
			@rowCount int,
			@rowCurr int;

			select 
				@currentLevel = 0
				,@maxLevel = max(
					case when isnumeric(replace(Name,'Level','')) = 1 then
						replace(Name,'Level','') 
					else 
						0 
					end) 
			from 
				LoadColumn 
			where 
				LoadID = @LoadID and Name like 'Level%';
			

			declare @levels table (id int, ColumnIndex int, RowIndex int, [Level] varchar(50), Value varchar(250),MaxLevel int, TaxonomyID int, ParentID int, [Status] varchar(50));
			with v as
			(
				select L.ID, L.Object, L.ObjectID, LC.Name, LC.ColumnIndex, IC.RowIndex, IC.Value, replace(LC.Name,'Level','') as [Level], T.ID as TaxonomyID from [Load] L
				join LoadColumn LC on LC.LoadID = L.ID
				join LoadItemColumn IC on IC.LoadID = LC.LoadID AND IC.ColumnIndex = LC.ColumnIndex
				left join Taxonomy T on T.TaxonomyTypeID = L.ObjectID and T.[Level] = replace(LC.Name,'Level','') and T.Name = IC.Value
				where L.ID = @LoadID AND ltrim(rtrim(IC.Value)) != '' and LC.Name like 'Level%'  
			)
			insert into @levels
			select distinct
				row_number() over (partition by 1 order by v.[Level]) as ID,
				v.ColumnIndex
				,v.RowIndex
				,v.[Level]
				,v.Value
				,m.[Level] as MaxLevel
				,v.TaxonomyID
				,p.TaxonomyID as ParentID 
				,'UPDATE' as [Status]
			from v
			left join v p 
				on p.RowIndex = v.RowIndex and v.TaxonomyID is null and p.ColumnIndex = (v.ColumnIndex - 1)
			inner join v m on m.RowIndex = v.RowIndex and m.[Level] = (select max([Level]) from v where RowIndex = m.RowIndex)
			order by v.[Level] asc;

			--calculate hierarchy
			while @currentLevel <= @maxLevel
			begin
				set @currentLevel = @currentLevel + 1;
				
				update LV
				set LV.ParentID = P.ID
				from @levels LV
				left join @levels P on P.[Level] = (LV.[Level] - 1) AND LV.RowIndex = P.RowIndex
				where LV.[Level] = @currentLevel;
			end 

			--delete records that have a level > 1 and no parentid, missing info
			--delete from @levels where parentid is null and level > 1;

			select @rowCurr = 0, @rowCount = count(*) from @levels;

			while @rowCurr <= @rowCount
			begin
				set @rowCurr = @rowCurr + 1;

				--parent does not exist or leading columns were not filled
				if (select ParentID from @levels where id = @rowCurr) IS NULL AND (select Level from @levels where id = @rowCurr) > 1
				begin
					update @levels set [Status] = 'ERROR' where rowIndex = (select rowindex from @levels where id = @rowCurr);
					continue;
				end


				--update the TaxonomyID for records that do not yet have it
				if (select level from @levels where id = @rowCurr) = 1
				begin
					update LV
					set TaxonomyID = T.ID
					from @levels LV
					join Load L on L.ID = @LoadID
					join Taxonomy T on T.Name = LV.Value and T.ParentID is NULL and T.Level = LV.Level and T.TaxonomyTypeID = L.ObjectID
					where LV.ID = @rowCurr;
				end
				else
				begin
					update LV
					set TaxonomyID = T.ID
					from @levels LV
					left join @levels P on P.ID = LV.ParentID
					join Taxonomy T on T.Name = LV.Value and T.ParentID = P.TaxonomyID and T.Level = LV.Level
					where LV.ID = @rowCurr;
				end

				if (select TaxonomyID from @levels where id = @rowCurr) IS NULL
				begin
					--insert the new taxonomy
					insert into Taxonomy (TaxonomyTypeID, ParentID, Name, [Description], UpdatedOn, UpdatedBy)
					select	distinct
							L.ObjectID as TaxonomyTypeID
						,LVP.TaxonomyID as ParentID
						,LV.Value as Name
						,case when LV.Level = LV.MaxLevel then
							LI.Value
						else
							''
						END as Description
						,getdate() as UpdatedOn
						,@UpdatedBy as UpdatedBy
					from 
						@levels LV
					left join @levels LVP on LVP.ID = LV.ParentID
					join [Load] L on L.ID = @LoadID
					inner join LoadColumn LC on LC.Name = 'Description' and LC.LoadID = @LoadID
					inner join LoadItemColumn LI on LI.RowIndex = LV.RowIndex AND LI.ColumnIndex = LC.ColumnIndex AND LI.LoadID = @LoadID
					where
						LV.ID = @rowCurr

					update @levels set [Status] = 'INSERT' where id = @rowCurr;

					--set the levels taxonomy id after insert
					update LV
					set TaxonomyID = T.ID
					from @levels LV
					left join @levels P on P.ID = LV.ParentID
					join Taxonomy T on T.Name = LV.Value and coalesce(T.ParentID,-1) = coalesce(P.TaxonomyID,-1) and T.Level = LV.Level
					where LV.ID = @rowCurr;
				end
				
				--if level = max, update the description
				if (select level from @levels where id = @rowCurr) = (select maxlevel from @levels where id = @rowCurr)
				begin
					update	T
					set		T.Description = case when LI.Value = '' then T.Description else LI.Value end,
							T.UpdatedOn = getutcdate(),
							T.UpdatedBy = @UpdatedBy
					from	Taxonomy T
							join @levels LV on LV.ID = @rowCurr and T.ID = LV.TaxonomyID
							inner join LoadColumn LC on LC.Name = 'Description' and LC.LoadID = @LoadID
							inner join LoadItemColumn LI on LI.RowIndex = LV.RowIndex AND LI.ColumnIndex = LC.ColumnIndex AND LI.LoadID = @LoadID;

				end
			end --end while
			

			--remove error rows
			delete from @levels
			where rowindex in (select rowindex from @levels where status is null or status = 'ERROR');

						--insert object statuses
			insert into @ResolvedObjects ([Object], ObjectID, [Action], LoadID, RowIndex)
			select
				'Taxonomy',
				TaxonomyID,
				[Status],
				@LoadID,
				RowIndex
			from 
			@levels;

		end

		-- Update the LoadItem table with the IDs we recieved in the merge statements above.
		update	T
		set		T.[Object] = S.[Object],
				T.ObjectID = S.ObjectID,
				T.[Status] = 1,
				T.StatusMessage = case S.[Action]
									when 'INSERT' then 'Added item'
									when 'UPDATE' then 'Updated item'
									else NULL
									end
		from	LoadItem T
				inner join	@ResolvedObjects S on S.LoadID = T.LoadID and S.RowIndex = T.RowIndex

		-- Update the LoadItems that were not successfully added or updated.
		update	LoadItem
		set		[Status] = 0,
				[StatusMessage] = 'Item could not be added nor updated.'
		where	[ObjectID] is null
		
		-- Load custom fields for the inserted/updated object above.
		merge	Field T
		using	(
				select	distinct
						FT.ID as FieldTypeID,
						L.[Object],
						L.ObjectID,
						IC.LookupObjectID--max(IC.LookupObjectID) as LookupObjectID
				from	LoadItem L
						inner join LoadColumn C on C.LoadID = L.LoadID
						inner join LoadItemColumn IC on IC.LoadID = C.LoadID and L.RowIndex = IC.RowIndex and IC.ColumnIndex = C.ColumnIndex and IC.LookupObjectID is not null
						inner join FieldType FT on FT.[Object] = @Object and FT.ObjectID = @ObjectID and FT.Name = C.Name
				where	L.ObjectID is not null
						and L.LoadID = @LoadID
				--group by	FT.ID,
				--			L.[Object],
				--			L.ObjectID
				) S
		on		(T.FieldTypeID = S.FieldTypeID and T.ObjectType = S.[Object] and T.ObjectID = S.ObjectID)
		when	matched then
				update	set Value = S.LookupObjectID
		when	not matched then
				insert (ObjectType, ObjectID, FieldTypeID, Value)
				values (S.[Object], S.ObjectID, S.FieldTypeID, S.LookupObjectID);

		merge	Field T
		using	(
				select	distinct
						FT.ID as FieldTypeID,
						L.[Object],
						L.ObjectID,
						case 
							when FT.[Type] = 'Boolean' and LOWER(IC.Value) in ('y', 'yes', 'true', 't', '1') then 'true'
							when FT.[Type] = 'Boolean' and LOWER(IC.Value) not in ('y', 'yes', 'true', 't', '1') then 'false'
							else IC.Value
						end as Value
				from	LoadItem L
						inner join LoadColumn C on C.LoadID = L.LoadID
						inner join LoadItemColumn IC on IC.LoadID = C.LoadID and L.RowIndex = IC.RowIndex and IC.ColumnIndex = C.ColumnIndex and IC.LookupObjectID is null
						inner join FieldType FT on FT.[Object] = @Object and FT.ObjectID = @ObjectID and FT.Name = C.Name and FT.[Type] <> 'Lookup'
				where	L.ObjectID is not null
						and L.LoadID = @LoadID
				) S
		on		(T.FieldTypeID = S.FieldTypeID and T.ObjectType = S.[Object] and T.ObjectID = S.ObjectID)
		when	matched then
				update	set Value = S.Value
		when	not matched then
				insert (ObjectType, ObjectID, FieldTypeID, Value)
				values (S.[Object], S.ObjectID, S.FieldTypeID, S.Value);
	end
	else
	begin
		-- This is for actions: R, U
		declare @current int,
				@max int,
				@sourceObject varchar(50),
				@sourceObjectID int,
				@sourceIntersectTypeNodeID int,
				@targetObject varchar(50),
				@targetObjectID int,
				@targetIntersectTypeNodeID int,
				@intersectID int = null,
				@date datetime = getutcdate()

		declare @Intersects IDTable

		if @Action = 'R' OR @Action = 'U'	--UNRELATION (Remove existing relation)
		begin
			-- PARSE both sides.
			update	T
			set		T.LookupObject = S.LookupObject,
					T.LookupObjectID = S.LookupObjectID
			from	LoadItemColumn T
					inner join	(
								select	IC.LoadID,
										IC.RowIndex,
										IC.ColumnIndex,
										T.[Object] as LookupObject,
										T.ObjectID as LookupObjectID
								from	[Load] L
										inner join [LoadColumn] C on C.LoadID = L.ID and L.ID = @LoadID
										inner join [LoadItemColumn] IC on IC.LoadID = C.LoadID and IC.ColumnIndex = C.ColumnIndex
										inner join IntersectTypeNode IT on IT.IntersectTypeID = @ObjectID and IT.[Order] = IC.[ColumnIndex]
										inner join cache.ObjectDetails T on T.[TextPath] = IC.Value and T.[ObjectType] = IT.[ObjectType] and T.ObjectTypeID = IT.ObjectID
								) S on S.LoadID = T.LoadID and S.RowIndex = T.RowIndex and S.ColumnIndex = T.ColumnIndex
			update	T
			set		T.[Status] = 0,
					T.StatusMessage =	REPLACE(REPLACE(
											STUFF(
											(
											select	LIC.Value + ' could not be located in the <a href="' + T.Url + '">' + T.Name + '</a> list, '
											from	[Load] L
													inner join [IntersectTypeNode] ITN on ITN.IntersectTypeID = L.ObjectID and L.ID = @LoadID
													inner join [LoadItemColumn] LIC on LIC.LoadID = L.ID and LIC.ColumnIndex = ITN.[Order] and LIC.ColumnIndex = IC.ColumnIndex and LIC.RowIndex = IC.RowIndex and LIC.LookupObject is null
													inner join cache.ObjectDetails T on T.[Object] = ITN.[ObjectType] and T.ObjectID = ITN.ObjectID
											for xml path('')
											), 1, 0, ''),
										'&lt;', '<'), '&gt;', '>')
			from	[LoadItem] T
					inner join [LoadItemColumn] IC on T.LoadID = @LoadID and IC.LoadID = T.LoadID and IC.RowIndex = T.RowIndex and IC.LookupObject IS NULL and IC.LookupObjectID is null

			select	@current = min(I.RowIndex),
					@max = max(I.RowIndex)
			from	LoadItem I
					inner join LoadItemColumn S on S.LoadID = I.LoadID and S.RowIndex = I.RowIndex and S.ColumnIndex = 1 and S.LookupObject is not null
					inner join LoadItemColumn T on T.LoadID = I.LoadID and T.RowIndex = I.RowIndex and T.ColumnIndex = 2 and T.LookupObject is not null
			where	I.LoadID = @LoadID



		end

		while @current <= @max
		begin
			select	@sourceObject = S.LookupObject,
					@sourceObjectID = S.LookupObjectID,
					@targetObject = T.LookupObject,
					@targetObjectID = T.LookupObjectID
			from	LoadItem I
					inner join LoadItemColumn S on S.LoadID = I.LoadID and S.RowIndex = I.RowIndex and S.ColumnIndex = 1 and S.LookupObject is not null
					inner join LoadItemColumn T on T.LoadID = I.LoadID and T.RowIndex = I.RowIndex and T.ColumnIndex = 2 and T.LookupObject is not null
			where	I.LoadID = @LoadID and I.RowIndex = @current

			set		@intersectID = null

			select	@IntersectID = SN.IntersectID 
			from	[IntersectNode] SN 
					inner join IntersectNode TN on	SN.IntersectID = TN.IntersectID 
													and SN.ID <> TN.ID 
													and SN.ObjectType = @sourceObject 
													and SN.ObjectID = @sourceObjectID 
													and TN.ObjectType = @targetObject 
													and TN.ObjectID = @targetObjectID
			if @Action = 'R'	--RELATION
			begin
				if @intersectID is null
				begin
					-- Get the node type IDs
					select	@sourceIntersectTypeNodeID = S.ID,
							@targetIntersectTypeNodeID = T.ID
					from	IntersectTypeNode S 
							inner join IntersectTypeNode T on S.IntersectTypeID = T.IntersectTypeID and S.[Order] = 1 and T.[Order] = 2 and S.ID <> T.ID and S.IntersectTypeID = @ObjectID

					insert into [Intersect] (IntersectTypeID, Classification) values (@ObjectID, 2)
					set @intersectID = SCOPE_IDENTITY()

					insert into [IntersectNode] (IntersectTypeNodeID, IntersectID, ObjectType, ObjectID) 
					values						(@sourceIntersectTypeNodeID, @intersectID, @sourceObject, @sourceObjectID)
					insert into [IntersectNode] (IntersectTypeNodeID, IntersectID, ObjectType, ObjectID) 
					values						(@targetIntersectTypeNodeID, @intersectID, @targetObject, @targetObjectID)

					exec utility.AddAuditEntry @sourceObject, @sourceObjectID, 0, @date, 'Created', 'Intersect', @intersectID
					exec utility.AddAuditEntry @targetObject, @targetObjectID, 0, @date, 'Created', 'Intersect', @intersectID
				end

				if @intersectID is not null
				begin
					update	LoadItem
					set		[Object] = 'Intersect',
							ObjectID = @intersectID,
							[Status] = 1,
							StatusMessage = 'Successfully created/updated relationship'
					where	LoadID = @LoadID
							and RowIndex = @current
				end
				else
				begin
					update	LoadItem
					set		[Status] = 0,
							StatusMessage = 'Failed to create relationship'
					where	LoadID = @LoadID
							and RowIndex = @current
				end
			end --end R

			if @Action = 'U'	--UNRELATION
			begin
				if @intersectID is not null
				begin
					begin try
						if exists(	select 1 
									from	[cache].[Relationships] SR
											inner join Responsibility RE on RE.ResponsibleObjectType = SR.SourceObject and RE.ResponsibleObjectID = SR.SourceObjectID
											inner join [cache].[Relationships] TR on RE.ObjectType = 'Intersect' and RE.ObjectID = TR.IntersectID and TR.TargetObject = SR.TargetObject and TR.TargetObjectID = SR.TargetObjectID
									where	SR.IntersectID = @intersectID
								 )
						begin
							DECLARE @Targets VARCHAR(8000) 
							SELECT	@Targets = COALESCE(@Targets + ', ', '') + TR.SourceObjectName 
							from	[cache].[Relationships] SR
									inner join Responsibility RE on RE.ResponsibleObjectType = SR.SourceObject and RE.ResponsibleObjectID = SR.SourceObjectID
									inner join [cache].[Relationships] TR on RE.ObjectType = 'Intersect' and RE.ObjectID = TR.IntersectID and TR.TargetObject = SR.TargetObject and TR.TargetObjectID = SR.TargetObjectID
							where	SR.IntersectID = @intersectID

							update	LoadItem
							set		[Object] = 'Intersect',
									ObjectID = @intersectID,
									[Status] = 0,
									StatusMessage = 'Unable to remove relationship as it acts as a source for: ' + @Targets
							where	LoadID = @LoadID
									and RowIndex = @current
						end
						else
						begin
							delete [Intersect] where ID = @intersectID

							update	LoadItem
							set		[Object] = 'Intersect',
									ObjectID = @intersectID,
									[Status] = 1,
									StatusMessage = 'Successfully removed relationship'
							where	LoadID = @LoadID
									and RowIndex = @current
						end
					end try
					begin catch
							update	LoadItem
							set		[Object] = 'Intersect',
									ObjectID = @intersectID,
									[Status] = 0,
									StatusMessage = 'Unable to remove relationship due to the following error: ' + ERROR_MESSAGE()
							where	LoadID = @LoadID
									and RowIndex = @current
					end catch
				end
				else
				begin
					update	LoadItem
					set		[Object] = 'Intersect',
							ObjectID = NULL,
							[Status] = 0,
							StatusMessage = 'Relationship not found'
					where	LoadID = @LoadID
							and RowIndex = @current
				end
			end --end U

			insert into @Intersects values (@intersectID)

			set @current = @current + 1
		end

		if @Action = 'R'
		begin
			exec cache.SynchronizeRelationships @Intersects
		end

	end --end IF statement to check if action = P or NOT

	update	[Load] 
	set		DateCompleted = getutcdate()
	where	ID = @LoadID
end
GO


