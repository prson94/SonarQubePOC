CREATE PROCEDURE [dbo].[GetCommentDetailsByType]
--declare
	@type varchar(50), 
	@id int,
	@skip int,
	@take int,
	@dateStart datetime = null,
	@dateEnd datetime = null,
	@commentTypeID int = 0,
	@searchPhrase varchar(100) = ''
--set @type = 'Artifact'
--set @id = 733
--set @skip = 0
--set @take = 100
AS
BEGIN
	SET NOCOUNT ON;

	with i (ResourceID) 
	as
	(
		select	r.ResourceID
		from	ResponsibilityDetails r
				inner join Comment c on c.OwnerObjectType = r.Object and c.OwnerObjectID = r.ObjectID and c.ID = @id
	),
	 P
	AS
	(
		SELECT		C.*,
					CASE WHEN (select count(*) from i where ResourceID = C.CreatingResourceID) > 0  THEN
						1
					WHEN (select count(*) from i where ResourceID = C.CreatingResourceID) > 0  THEN
						1
					ELSE
						0
					END as CreatorIsOwner,
					coalesce(C.OwnerObjectType, CR.ObjectType) as ObjectType,
					coalesce(C.OwnerObjectID, CR.ObjectID) as ObjectID,
					(
					select	a.[Object],
							a.ObjectID,
							utility.getassetdisplayvalue(a.id) as TextPath,
							ast.Name as ObjectTypeName,							
							os.IconForeColor,
							os.IconBackColor,
							dbo.generatengobjecturl(a.[object],ast.[objectid],a.objectid) as Url
					from	CommentRelation CR
							inner join asset a on (CR.CommentID = C.ID and a.[object] = CR.[ObjectType] and a.objectid = CR.ObjectID)
							inner join assettype ast on ( a.assettypeid = ast.id)
							inner join objectstyle os on (ast.[object] = os.[objecttype] and ast.[objectid] = os.[objectid])							
					for xml path('tag'), root('tags'), type
					) as TagsXml
		FROM		Comment C
					INNER JOIN CommentRelation CR	ON C.ID = CR.CommentID
													AND (
														coalesce(@commentTypeID,0) = 0 OR (C.CommentTypeID = @commentTypeID)
														) --in (1,2,3,7)
													AND CR.ObjectType = @type 
													AND CR.ObjectID = @id
													AND (
														(C.DateCreated between @dateStart and @dateEnd and @dateStart is not null and @dateEnd is not null) or
														(@dateStart is null and @dateEnd is null)
														)
													AND C.ParentID IS NULL	
													and c.isdeleted = 0			
		WHERE
			coalesce(ltrim(rtrim(@searchPhrase)),'')='' or (lower(Body) like lower('%'+@searchPhrase+'%')) 
		ORDER BY	C.DateCreated DESC
		OFFSET  @skip ROWS 
		FETCH NEXT @take ROWS ONLY 

		UNION ALL

		SELECT	C.*,
				0 as CreatorIsOwner, 
				cast('Resource' as varchar(50)) as ObjectType,
				C.CreatingResourceID as ObjectID,
				NULL as TagsXml
		FROM	P
				INNER JOIN Comment C ON C.ParentID = P.ID
	)

	select	P.*,
			R.FirstName + ' ' + R.LastName as ResourceName,
			R.Email as ResourceEmail,
			utility.getassetdisplayvalue(a.id),
			dbo.generatengobjecturl(a.[object],ast.[objectid],a.objectid) as ObjectUrl,
			(
				select CommentID,
						ResourceID,
						vote as VoteValue
				from commentvote
				where commentid = p.ID
					for xml path('vote'), root('votes'), type
			) as VotesXML
	from	P
			left join reporting.Global_Resource R on R.ResourceID = P.CreatingResourceID			
			left join asset a on a.[object] = p.objecttype and a.objectid = p.objectid
			left join assettype ast on a.assettypeid = ast.id
	where
		isdeleted = 0;
END


