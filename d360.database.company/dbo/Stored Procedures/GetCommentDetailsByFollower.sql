CREATE PROCEDURE [dbo].[GetCommentDetailsByFollower]
--declare
	@resourceID int,
	@skip int,
	@take int,
	@dateStart datetime = null,
	@dateEnd datetime = null,
	@commentTypeID int = 0,
	@searchPhrase varchar(100) = ''
--set @resourceID = 1
--set @skip = 0
--set @take = 200
AS
BEGIN
	set nocount on;

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
	left join FollowWithChildren f on f.objectid = c.ownerobjectid and f.objecttype = c.ownerobjecttype and f.ResourceID = c.CreatingResourceID
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
			select a.id as objectid from followWithChildren l
			join artifacttype at on at.id = l.objectid
			join artifact a on a.artifacttypeid = at.id
			where l.resourceid = @resourceID and l.followtypeid = 2
		)
	)
	AND C.isdeleted = 0
	AND (
			coalesce(@commentTypeID,0) = 0 OR (C.CommentTypeID = @commentTypeID)
		) 
	AND (
			(C.DateCreated between @dateStart and @dateEnd and @dateStart is not null and @dateEnd is not null) or
			(@dateStart is null and @dateEnd is null)
		)
	AND C.ParentID is null
	AND (coalesce(ltrim(rtrim(@searchPhrase)),'')='' or (lower(Body) like lower('%'+@searchPhrase+'%')))
	order by c.datecreated DESC
	OFFSET		@skip ROWS 
	FETCH NEXT	@take ROWS ONLY
	)

	select a.*,
		a.OwnerObjectType as ObjectType,
		a.OwnerObjectId as ObjectId,
		R.FirstName + ' ' + R.LastName as ResourceName,
		R.Email as ResourceEmail,
		D.Name as ObjectName,
		D.Url as ObjectUrl,
		(
		select	CRD.Object,
				CRD.ObjectID,
				CRD.TextPath,
				CRD.ObjectTypeName,
				CRD.Url,
				CRD.IconBackColor,
				CRD.IconForeColor
		from	CommentRelation CR
				inner join cache.ObjectDetails CRD on CR.CommentID = a.ID and a.ParentID is null and CR.ObjectType = CRD.[Object] and CR.ObjectID = CRD.ObjectID
		for xml path('tag'), root('tags'), type
		) as TagsXml,
					(
			select CommentID,
					ResourceID,
					vote as VoteValue
			from commentvote
			where commentid = a.ID
				for xml path('vote'), root('votes'), type
		) as VotesXML,
		0 as CreatorIsOwner
	from
	(
		select * from p
		union all
		select r.*,1 as IsVisible from comment r
		join p on r.parentid = p.id
	) a
	left join reporting.Global_Resource R on R.ResourceID = a.CreatingResourceID
	left join cache.ObjectDetails D on D.[Object] = a.OwnerObjectType and D.ObjectID = a.OwnerObjectID
	where isvisible = 1;

END
