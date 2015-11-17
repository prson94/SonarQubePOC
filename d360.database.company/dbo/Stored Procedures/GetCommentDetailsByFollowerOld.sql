
CREATE PROCEDURE [dbo].[GetCommentDetailsByFollowerOld]
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

	IF OBJECT_ID('tempdb..#comments') IS NOT NULL
		DROP TABLE #comments

	create table #comments (ID int not null, ParentID int null, CommentTypeID int not null, Body nvarchar(max), DateCreated datetime, CreatingResourceID int, ObjectType varchar(50), ObjectID int, IsDeleted bit, DateEdited datetime);
	create nonclustered index IX_TempComments_ID ON #comments (ID asc);
	create nonclustered index IX_TempComments_ParentID ON #comments (ParentID asc);
	declare @IDs table (ID int);

	with f as	(
				select	ObjectType, 
						ObjectID 
				from	Follow 
				where	ResourceID = @resourceID				
				),
		rg as	(
				select	ObjectType,
						ObjectID
				from	cache.Responsibilities rd
						inner join ResourceGroup rg on rd.ResponsibleObject = 'Group' and rg.GroupID = rd.ResponsibleObjectID and rg.ResourceID = @resourceID
				),
		r as	(
				select	ObjectType,
						ObjectID
				from	cache.Responsibilities
				where	ResponsibleObject = 'Resource'
						and ResponsibleObjectID = @resourceID
				)

	insert into @IDs
		SELECT		C.ID
		FROM		Comment C
					LEFT JOIN	(
								select	* from f
								union
								select	* from r
								union
								select	* from rg
								union select 'Resource' as ObjectType, @resourceID as ObjectID 
								) F ON	F.ObjectType = C.OwnerObjectType
										AND F.ObjectID = C.OwnerObjectID
		WHERE		(
						coalesce(@commentTypeID,0) = 0 OR (C.CommentTypeID = @commentTypeID)
					) 
					AND (
							(C.DateCreated between @dateStart and @dateEnd and @dateStart is not null and @dateEnd is not null) or
							(@dateStart is null and @dateEnd is null)
						)
					AND C.ParentID is null
					AND (F.ObjectType is not null OR C.ID in (select CommentID from CommentRelation where ObjectType = 'Resource' and ObjectID = @resourceID))
					AND (coalesce(ltrim(rtrim(@searchPhrase)),'')='' or (lower(Body) like lower('%'+@searchPhrase+'%')))
		ORDER BY	C.DateCreated DESC
		OFFSET		@skip ROWS 
		FETCH NEXT	@take ROWS ONLY;

	insert into #comments
		SELECT		distinct	
					C.ID,
					C.ParentID,
					C.CommentTypeID,
					C.Body,
					C.DateCreated,
					C.CreatingResourceID,
					C.OwnerObjectType,
					C.OwnerObjectID,
					C.IsDeleted,
					C.DateEdited
		FROM		Comment C
					INNER JOIN @IDs I on I.ID = C.ID
					--INNER JOIN CommentRelation CR on CR.CommentID = C.ID
		ORDER BY	C.DateCreated DESC;

	insert into #comments
		SELECT	C.ID,
				C.ParentID,
				C.CommentTypeID,
				C.Body,
				C.DateCreated,
				C.CreatingResourceID, 
				cast('Resource' as varchar(50)),
				C.CreatingResourceID,
				C.IsDeleted,
				C.DateEdited
		FROM	#comments P
				INNER JOIN Comment C on C.ParentID = P.ID

	select	P.*,
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
					inner join cache.ObjectDetails CRD on CR.CommentID = P.ID and P.ParentID is null and CR.ObjectType = CRD.[Object] and CR.ObjectID = CRD.ObjectID
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
			0 as CreatorIsOwner
	from	#comments P
			left join reporting.Global_Resource R on R.ResourceID = P.CreatingResourceID
			left join cache.ObjectDetails D on D.[Object] = P.ObjectType and D.ObjectID = P.ObjectID
	where
		1=1 
END
