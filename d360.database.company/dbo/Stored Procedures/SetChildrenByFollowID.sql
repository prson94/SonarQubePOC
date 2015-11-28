CREATE PROCEDURE [dbo].[SetChildrenByFollowID]
	@followId int,
	@includeChildren bit = 0
AS
BEGIN

declare @id int;
declare @type varchar(50);
declare @resourceID int;

select 
	@id = ObjectId 
	,@type = ObjectType 
	,@resourceId = ResourceID 
from 
	follow 
where 
	id = @followId;

IF @id = 0 OR @includeChildren = 0
BEGIN

	IF @id != 0 --follow everything of this type
	BEGIN
		insert into FollowChild (ObjectID, ObjectType, DateCreated, FollowTypeID, ParentObjectType, ParentObjectID)
		select 
			ObjectID,
			[Object],
			getdate(),
			5, --Child
			@type,
			@id
		from
			cache.ObjectDetails d
		where
			ObjectType = @type 
			and ObjectTypeId = @id
			and not exists (select * from FollowChild where ObjectID = d.ObjectID and ObjectType = d.ObjectType and ParentObjectType = @type and ParentObjectID = @id);

	END
	ELSE --follow everything which is this type
	BEGIN
		insert into FollowChild (ObjectID, ObjectType, DateCreated, FollowTypeID, ParentObjectType, ParentObjectID)
	select 
		ObjectID,
		[Object],
		getdate(),
		5, --Child
		@type,
		@id
	from
		cache.ObjectDetails d
	where
		[Object] = @type
		and not exists (select * from FollowChild where ObjectID = d.ObjectID and ObjectType = d.ObjectType and ParentObjectType = @type and ParentObjectID = @id);

	END


END
ELSE
BEGIN
	with d as
	(
		select 
			[Object] as ObjectType
			,ObjectID
			,null as IntersectID
			,null as TargetObjectID 
		from 
			cache.ObjectDetails d 
		where 
			d.ObjectID = @id 
			and d.[Object] = @type

		union all

		select 
			d2.[Object] as ObjectType
			,d2.ObjectID
			,null as IntersectID
			,null as TargetObjectID 
		from 
			d
		inner join 
			cache.ObjectDetails d2 on d2.parentid = d.Objectid 
	)
	,r as
	(
		select 
			 s.SourceObject as ObjectType
			,s.SourceObjectID as ObjectID
			,s.IntersectID
			,s.TargetObjectID 
		from 
			cache.Relationships s 
		join 
			d on s.SourceObject = @type 
				and s.SourceObjectID = d.ObjectID

		union all

		select 
			 r2.TargetObject as ObjectType
			,r2.TargetObjectID as ObjectID
			,r.IntersectID
			,null as TargetObjectID 
		from 
			r
		join 
			cache.Relationships r2 on r2.TargetObject = @type 
				and r2.SourceObjectId = r.TargetObjectID
				and r2.TargetObjectID != r.ObjectID  
				and r2.SourceObjectID = r.ObjectID 
				and r2.SourceObject != r.ObjectType
	)

	insert into FollowChild (ObjectID, ObjectType, DateCreated, FollowTypeID, ParentObjectType, ParentObjectID)
	select 
		 c.ObjectID
		,c.ObjectType
		,getdate() as DateCreated
		,5 as FollowTypeID
		,@type
		,@id
	from
		(
		select distinct * from 
			(
				select ObjectID,ObjectType from d where ObjectType = @type
				union all
				select ObjectID,ObjectType from r where ObjectType = @type
			) c1
		) c
	where 
		c.objectid != @id
		and not exists (select * from FollowChild l where l.ObjectID = c.ObjectID and l.ObjectType = c.ObjectType and l.ParentObjectID = @id and l.ParentObjectType = @type)

END



--insert all child records into the child table
--select * from followchild


--when i follow a parent i need to unfollow any Parent records which are children of the new parent
delete from follow where
id in (
select f.id from followchild c
join follow f on f.resourceId = @resourceID and f.followtypeid = 3 and f.objectid = c.objectid and f.objecttype = c.objecttype
 where c.parentobjecttype = @type and c.parentobjectid = @id
 );

--select * from follow where followtypeid = 3 and resourceId = 3243



--delete any existing follow records or records that are children of existing records which have been inserted as part of the above
--with e as
--(
--	select f1.id 
--	from 
--		follow f1
--	join 
--		follow f2 on f2.parentid = @followid 
--		and f2.objecttype = f1.objecttype 
--		and f2.objectid = f1.objectid 
--		and f1.resourceId = 3243 
--		and f1.parentid is null
--	union all
--	select 
--		f3.id 
--	from 
--		e
--	join 
--		follow f3 on f3.parentid = e.id
--)
--delete from follow
--where 
--	id in (select id from e);


--with d as
--(
--	select * from cache.ObjectDetails d where d.ObjectID = @id and d.Object = @type
--	union all
--	select d2.* from d
--	inner join cache.ObjectDetails d2 on d2.parentid = d.Objectid 
--)
--insert into follow (ResourceID, ObjectType, ObjectID, DateCreated, FollowTypeID, ParentID)
--select @resourceId as ResourceID, r.targetobject as ObjectType,r.targetobjectid as ObjectId, getdate() as DateCreated, 5 as FollowTypeId, @followId as ParentID from d
--join cache.relationships r on r.SourceObject = d.Object and r.SourceObjectID = d.ObjectID
--where  r.targetobject = @type and not exists (select * from follow f where f.followtypeid = 5 and f.resourceId = @resourceID and f.ObjectType = r.targetobject and f.objectid = r.targetobjectid);

--select * from cache.relationships

--select * from follow order by datecreated desc

--delete from follow where id > 2500

--select objectid, count(objectid) from follow where  Parentid = 1741
--group by objectid order by 2 desc


END
GO