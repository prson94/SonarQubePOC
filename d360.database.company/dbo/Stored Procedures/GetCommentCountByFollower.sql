CREATE PROCEDURE [dbo].[GetCommentCountByFollower]
	@resourceID int,
	@dateStart datetime = null,
	@dateEnd datetime = null,
	@searchPhrase varchar(100) = ''
AS
BEGIN

	with p as
	(
	select c.*,
	case when c.creatingresourceid = @resourceID then
		1
	when c.visibilityid = 2  then
		1
	when c.visibilityid = 3 and f.objectid is not null then
		1
	when coalesce(c.visibilityid,4) = 4  then
		1
	else
		0
	end as IsVisible
	from comment c
	left join FollowWithChildren f on f.objectid = c.ownerobjectid and f.objecttype = c.ownerobjecttype and f.resourceId = c.creatingResourceId
	where c.ID in 
	(
		select commentid as id from FollowWithChildren f
		join commentrelation cr on cr.objectid = f.objectid and cr.objecttype = f.objecttype
		where f.resourceid = @resourceId
		union all
		select id from comment where creatingresourceid = @resourceid
		union all
		select id from comment c2
		join 
		(
			select r.[Object], r.ObjectID from resourcegroup rg 
			join cache.responsibilities r on rg.GroupID = r.ResponsibleObjectID and r.ResponsibleObject = 'Group'
			where rg.resourceid = @resourceID and rg.isOwner = 1
		) o on o.object = c2.ownerobjecttype and o.objectid = c2.ownerobjectid
		union all
		select id from comment c3 where ownerobjecttype = 'Artifact' and ownerobjectid in
		(
			select objectid from followWithChildren where followtypeid = 1 and resourceid = @resourceID
			union all
			select a.id as objectid from follow l
			join artifacttype at on at.id = l.objectid
			join artifact a on a.artifacttypeid = at.id
			where l.resourceid = @resourceID and l.followtypeid = 2
		)
	)
	AND C.isdeleted = 0
	AND (
			(C.DateCreated between @dateStart and @dateEnd and @dateStart is not null and @dateEnd is not null) or
			(@dateStart is null and @dateEnd is null)
		)
	AND C.ParentID is null
	AND (coalesce(ltrim(rtrim(@searchPhrase)),'')='' or (lower(Body) like lower('%'+@searchPhrase+'%')))
	)
	
		SELECT
		i.CommentType, 
		u.[Count], 
		u.CommentTypeName 
	FROM
	(
		SELECT
			count(*) as [All],
			sum(case when a.commenttypeid = 2 then 1 else 0 end) as [Discussions],
			sum(case when a.commenttypeid = 5 then 1 else 0 end) as Issues,
			sum(case when a.commenttypeid = 6 then 1 else 0 end) as Tasks,
			sum(case when a.commenttypeid = 7 then 1 else 0 end) as [Red Flags],
			sum(case when a.commenttypeid = 8 then 1 else 0 end) as [Data Events],
			sum(case when a.commenttypeid = 9 then 1 else 0 end) as  Questions
		FROM
		(
			select * from p
			union all
			select r.*,1 as IsVisible from comment r
			join p on r.parentid = p.id
		) a
		left join reporting.Global_Resource R on R.ResourceID = a.CreatingResourceID
		left join cache.ObjectDetails D on D.[Object] = a.OwnerObjectType and D.ObjectID = a.OwnerObjectID
		where isvisible = 1
		) t
		UNPIVOT
			(
				[Count]
				for [CommentTypeName] in ([All], Discussions, Issues, Tasks, [Red Flags], [Data Events], Questions)
			) u
			
			join
			(
			select * from 
			(
				select 
					0 as [All],
					2 as Discussions,
					5 as Issues,
					6 as Tasks,
					7 as [Red Flags],
					8 as [Data Events],
					9 as Questions
					) t2
				unpivot
					(
						CommentType
						for CommentTypeName in ([All], Discussions, Issues, Tasks, [Red Flags], [Data Events], Questions)
					) u2
		) i on i.CommentTypeName = u.CommentTypeName

END
