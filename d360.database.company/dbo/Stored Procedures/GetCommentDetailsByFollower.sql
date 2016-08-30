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
	select	c.*,
			case 
				when c.CreatingResourceID = @resourceID then 1
				when c.VisibilityID = 2 then 1
				when c.VisibilityID = 3 then 1
				when coalesce(c.VisibilityID, 4) = 4  then 1
				else 0
			end as IsVisible
	from	Comment c
	where	c.ID in	(
					select	CommentID as ID
					from	FollowDetail f
							inner join CommentRelation cr on cr.ObjectID = f.ObjectID and cr.ObjectType = f.ObjectType
					where	f.ResourceID = @resourceId
					union all
					select	ID 
					from	Comment 
					where	CreatingResourceID = @resourceid
					union all
					select	ID 
					from	comment c2
							inner join	(
										select	r.[Object], r.ObjectID 
										from	ResourceGroup rg 
												inner join cache.ResponsibilityItem r on rg.GroupID = r.ResponsibleObjectID and r.ResponsibleObject = 'Group' and rg.ResourceID = @resourceID
										union
										select	[Object], ObjectID 
										from	cache.ResponsibilityItem
										where	ResponsibleObject = 'Resource' 
												and ResponsibleObjectID = @resourceID
										) o on o.[Object] = c2.OwnerObjectType and o.ObjectID = c2.OwnerObjectid
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
			AND (
				coalesce(ltrim(rtrim(@searchPhrase)),'')='' or 
				lower(Body) like lower('%'+@searchPhrase+'%')
				)
	order by c.datecreated DESC
	OFFSET		@skip ROWS 
	FETCH NEXT	@take ROWS ONLY
	)

	select	a.*,
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
					CRD.IconForeColor,
					CRD.NgUrl
			from	CommentRelation CR
					inner join cache.ObjectDetails CRD on CR.CommentID = a.ID and a.ParentID is null and CR.ObjectType = CRD.[Object] and CR.ObjectID = CRD.ObjectID
			for xml path('tag'), root('tags'), type
			) as TagsXml,
			(
			select	CommentID,
					ResourceID,
					vote as VoteValue
			from	commentvote
			where	commentid = a.ID
			for		xml path('vote'), root('votes'), type
			) as VotesXML,
			0 as CreatorIsOwner
	from	(
			select	* 
			from	p
			union all
			select	r.*,
					1 as IsVisible 
			from	Comment r
					inner join p on r.ParentID = p.ID
			) a
			left join reporting.Global_Resource R on R.ResourceID = a.CreatingResourceID
			left join cache.ObjectDetails D on D.[Object] = a.OwnerObjectType and D.ObjectID = a.OwnerObjectID
	where	IsVisible = 1;
END
