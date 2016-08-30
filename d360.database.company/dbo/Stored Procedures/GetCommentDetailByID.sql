CREATE PROCEDURE [dbo].[GetCommentDetailByID]
	@id int
AS
BEGIN
	with i (owner1, owner2) 
	as
	(
		select primaryownerresourceid as owner1,secondaryownerresourceid as owner2 from responsibility r
		join responsibilitytype rt on rt.id = r.responsibilitytypeid
		join [group] g on g.id = rt.responsibilitytypegroup
		where r.objecttype = (select ownerobjecttype from comment where id = @id)
		and r.objectid = (select ownerobjectid from comment where id = @id)
	),
	P (ID, ParentID)
	AS
	(
		SELECT		C.ID,
					C.ParentID
		FROM		Comment C
		WHERE		ID = @id
		UNION ALL
		SELECT	C.ID, 
				C.ParentID
		FROM	Comment C
				INNER JOIN P PAR ON PAR.ID = C.ParentID
	)

	SELECT		C.*,
				C.CreatingResourceID,
				O.Name as ObjectName,
				O.Url as ObjectUrl,
				case
					WHEN C.ParentID IS NULL THEN C.OwnerObjectType
					ELSE 'Resource'
				end as ObjectType,
				case 
					WHEN C.ParentID IS NULL THEN C.OwnerObjectID
					ELSE C.CreatingResourceID
				end as ObjectID,
				(
				select	CRD.Object,
						CRD.ObjectID,
						CRD.TextPath,
						CRD.ObjectTypeName,
						CRD.Url,
						CRD.IconForeColor,
						CRD.IconBackColor,
						CRD.NgUrl
				from	CommentRelation CR
						inner join cache.ObjectDetails CRD on CR.CommentID = C.ID and CR.ObjectType = CRD.[Object] and CR.ObjectID = CRD.ObjectID
				for xml path('tag'), root('tags'), type
				) as TagsXml,
										(
				select CommentID,
						ResourceID,
						vote as VoteValue
				from commentvote
				where commentid = p.ID
					for xml path('vote'), root('votes'), type
			) as VotesXML,
			CASE WHEN (select count(*) from i where owner1 = C.CreatingResourceID) > 0  THEN
				cast(1 as bit)
			WHEN (select count(*) from i where owner2 = C.CreatingResourceID) > 0  THEN
				cast(1 as bit)
			ELSE
				cast(0 as bit)
			END as CreatorIsOwner
	FROM		Comment C
				--INNER JOIN CommentRelation CR ON CR.CommentID = C.ID
				left join cache.ObjectDetails O on O.[Object] = C.OwnerObjectType and O.ObjectID = C.OwnerObjectID
				INNER JOIN P ON C.ID = P.ID
	ORDER BY	C.ParentID, C.DateCreated DESC
END

