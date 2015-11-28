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

	with i (owner1, owner2) 
	as
	(
		select primaryownerresourceid as owner1,secondaryownerresourceid as owner2 from responsibility r
		join responsibilitytype rt on rt.id = r.responsibilitytypeid
		join [group] g on g.id = rt.responsibilitytypegroup
		where r.objecttype = @type and r.objectid = @id
	),
	 P
	AS
	(
		SELECT		C.*,
					CASE WHEN (select count(*) from i where owner1 = C.CreatingResourceID) > 0  THEN
						1
					WHEN (select count(*) from i where owner2 = C.CreatingResourceID) > 0  THEN
						1
					ELSE
						0
					END as CreatorIsOwner,
					coalesce(C.OwnerObjectType, CR.ObjectType) as ObjectType,
					coalesce(C.OwnerObjectID, CR.ObjectID) as ObjectID,
					(
					select	CRD.Object,
							CRD.ObjectID,
							CRD.TextPath,
							CRD.ObjectTypeName,
							CRD.Url,
							CRD.IconForeColor,
							CRD.IconBackColor
					from	CommentRelation CR
							inner join cache.ObjectDetails CRD on CR.CommentID = C.ID and CR.ObjectType = CRD.[Object] and CR.ObjectID = CRD.ObjectID
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
			D.Name as ObjectName,
			D.Url as ObjectUrl,
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
			left join cache.ObjectDetails D on D.[Object] = P.ObjectType and D.ObjectID = P.ObjectID
	where
		isdeleted = 0;
END
GO
