CREATE view FollowWithChildren
as
	select ResourceID, ObjectType, ObjectID, DateCreated, FollowTypeID, ID from follow where followtypeid in (1,3)
	union all
	select ch.ResourceID, c.ObjectType, c.ObjectID, c.DateCreated, c.FollowTypeID, ch.ID  from follow ch
	join followchild c on c.parentobjecttype = ch.objecttype and c.parentobjectid = ch.objectid and c.followtypeid = 5
	where  ch.followtypeid = 3
	union all
	select ty.ResourceID, o.[object] as ObjectType, o.ObjectID, ty.DateCreated,ty.FollowTypeID,ty.ID from follow ty
	join cache.ObjectDetails o on o.ObjectType = ty.ObjectType and o.ObjectTypeID = ty.ObjectID
	where  ty.followtypeid = 2