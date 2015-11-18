/*
 Pre-Deployment Script Template							
--------------------------------------------------------------------------------------
 This file contains a list of SQL files that need to be executed when releasing 
 to production in the next cycle.
--------------------------------------------------------------------------------------
\dbo\Stored Procedures\GetNonIntersections.sql
*/


SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
ALTER procedure [dbo].[GetEnvironmentDetailsDiagramData]
--declare
	@ObjectType varchar(50),
	@ObjectID int
--set @ObjectType = 'Artifact'
--set @ObjectID = 11808
as
begin
	declare @tbl table (ID int, ParentID int, RtID int, ParentRtID int, TargetResponsibilityID int, ResponsibleObjectType varchar(50), ResponsibleObjectID int, AssigningItemType varchar(50), AssigningItemID int, [Role] nvarchar(250))

	insert into @tbl
		select	--distinct
				ResponsibilityID,
				coalesce(TargetResponsibilityID, 0),
				ResponsibilityTypeID,
				NULL,
				TargetResponsibilityID,
				ResponsibleObject as ResponsibleObjectType,
				ResponsibleObjectID,
				AssigningItem as AssigningItemType, 
				AssigningItemID,
				ResponsibilityType as [Role]
		from	cache.Responsibilities S--SourcingResponsibilityDetail S
		where	S.[Object] = @ObjectType and S.ObjectID = @ObjectID and S.[ResponsibilityTypeGroup] = 2

	update	T
	set		T.ParentRtID = h.ParentID
	from	@tbl T
			INNER JOIN ResponsibilityTypeHierarchy h on h.ID = T.RtID

	update	T
	set		ParentID = P.ID
	from	@tbl T
			inner join @tbl P on T.ParentRtID = P.RtID and T.ParentID = 0

	select	0 as ID,
			NULL as ParentID,
			null as AssigningItemType, 
			null as AssigningItemID,
			@ObjectType as ObjectType,
			@ObjectID as ObjectID,
			Name,
			ObjectTypeName as [Type],
			IconBackColor as BackColor,
			IconForeColor as ForeColor,
			Url,
			NULL TechnicalRelationships,
			NULL as Contexts,
			NULL as Transformations,
			'' as [Role]
	from	cache.ObjectDetails 
	where	[Object] = @ObjectType and ObjectID = @ObjectID --utility.ObjectDetail(@ObjectType, @ObjectID)
	union
	select	R.ID, 
			R.ParentID, 
			R.AssigningItemType, 
			R.AssigningItemID,
			R.ResponsibleObjectType as ObjectType,
			R.ResponsibleObjectID as ObjectID,
			D.Name,
			D.ObjectTypeName as [Type],
			D.IconBackColor as BackColor,
			D.IconForeColor as ForeColor,
			D.Url,
			T.TechnicalRelationships,
			C.Contexts,
			X.Transformations,
			R.[Role]
	from	@tbl R
			inner join cache.ObjectDetails D on D.[Object] = R.ResponsibleObjectType and D.ObjectID = R.ResponsibleObjectID--cross apply utility.ObjectDetail(R.ResponsibleObjectType, R.ResponsibleObjectID) D
			outer apply (
						select (
								select	TN.ObjectType as "@type",
										TN.ObjectID as "@id",
										FT.Name as "@attribute",
										coalesce(F.Name, '') "@fusion",
										coalesce(FA.TextPath, FA.Name) as "@name",
										'#/fusion/' + CAST(FT.FusionTypeID as varchar(15)) + '/' + + CAST(FA.FusionID as varchar(15)) as "@url"
								from	IntersectNode SN
										inner join IntersectNode TN on 
																	TN.IntersectID = SN.IntersectID and TN.ID <> SN.ID
																	and SN.ObjectType = R.ResponsibleObjectType and SN.ObjectID = R.ResponsibleObjectID 
																	and TN.ObjectType = @ObjectType and TN.ObjectID = @ObjectID
										inner join IntersectNode SFN on 
																	SFN.ObjectType = 'Intersect' and SFN.ObjectID = TN.IntersectID
										inner join IntersectNode TFN on 
																	TFN.IntersectID = SFN.IntersectID and TFN.ID <> SFN.ID
																	and SFN.ObjectType = 'Intersect' and SFN.ObjectID = TN.IntersectID
																	and TFN.ObjectType = 'FusionAttribute'
										inner join FusionAttribute FA on FA.ID = TFN.ObjectID
										inner join Fusion F on F.ID = FA.FusionID
										inner join FusionAttributeType FT on FT.ID = FA.FusionAttributeTypeID
								for xml path('relationship'), root('relationships')
							) as TechnicalRelationships
						) T
			outer apply (
						select (
								select	case ResponsibilityTransformationType
											when 1 then 'Business Transformation'
											else 'Technical Transformation'
										end as "@type",
										ID as "@id",
										Description as "description"
								from	ResponsibilityTransformation
								where	ResponsibilityID = R.ID
								for xml path('transformation'), root('transformations')
							) as Transformations
						) X
			outer apply (
						select (
								select	LT.Name as "@lookup",
										L.Name as "@name",
										L.Code as "@code"
								from	ResponsibilityContextItem RCI
										inner join DomainItem L on RCI.ObjectType = 'DomainItem' and L.ID = RCI.ObjectID and RCI.ResponsibilityID = R.ID
										inner join Domain LT on LT.ID = L.DomainID
								for xml path('context'), root('contexts')
							) as Contexts
						) C
end
go


ALTER PROCEDURE [dbo].[GetNonIntersections]
--declare
	@SourceID int,
	@TargetTypeID int,
	@SourceType varchar(250),
	@TargetType varchar(250),
	@Prefix varchar(250),
	@IntersectTypeID int
--set @SourceID = 261537
--set @TargetTypeID = 155
--set @SourceType = 'Intersect'
--set @TargetType = 'FusionAttribute'
--set @Prefix = ''
--set @IntersectTypeID = 72
AS
BEGIN
	SET NOCOUNT ON;

	declare @owners table (ObjectType varchar(25), ObjectID int, FusionAttributeID int)

	DECLARE @IDs TABLE (
						ID int
						)

	DECLARE @tbl TABLE (
						TargetUrl nvarchar(2500), 
						TargetID int, 
						TargetName nvarchar(2500), 
						TargetType nvarchar(250)
						)

	IF @TargetType = 'Event'
	BEGIN
		SET @TargetType = 'EventType'
	END

	INSERT INTO @IDs
		select TargetObjectID from cache.Relationships where SourceObject = @SourceType and SourceObjectID = @SourceID and TargetObject = @TargetType
		
	IF (@TargetType = 'Artifact')
	BEGIN
		INSERT INTO @tbl
			SELECT	dbo.GenerateObjectUrl(@TargetType, O.ArtifactTypeID, O.ID),
					O.ID,
					coalesce(O.TextPath, O.Name),
					T.Name
			FROM	Artifact O
					INNER JOIN ArtifactType T ON	O.ArtifactTypeID = T.ID
													AND O.ArtifactTypeID = @TargetTypeID
													AND O.Name LIKE '%' + @Prefix + '%'
													AND O.ID NOT IN (SELECT	ID FROM	@IDs)
	END

	IF (@TargetType = 'Domain')
	BEGIN
		INSERT INTO @tbl
			SELECT	dbo.GenerateObjectUrl(@TargetType, D.DomainTypeID, D.ID),
					D.ID,
					D.Name,
					T.Name
			FROM	Domain D
					INNER JOIN DomainType T ON	T.ID = D.DomainTypeID
													AND D.DomainTypeID = @TargetTypeID
													AND D.Name LIKE '%' + @Prefix + '%'
													AND D.ID NOT IN (SELECT	ID FROM	@IDs)
	END

	IF (@TargetType = 'FusionAttribute')
	BEGIN
		declare @OwnerSourceType varchar(50),
				@OwnerSourceID int
		IF @SourceType = 'Intersect'
		BEGIN
			select	top 1
					@OwnerSourceType = ObjectType,
					@OwnerSourceID = ObjectID
			from	IntersectNode N
					inner join Artifact A on A.ID = N.ObjectID and N.ObjectType = 'Artifact' and N.IntersectID = @SourceID
					inner join ArtifactType AT on AT.ID = A.ArtifactTypeID and AT.CanOwnFusion = 1
		END
		ELSE
		BEGIN
			set @OwnerSourceType = @SourceType
			set @OwnerSourceID = @SourceID
		END

		declare @h table (ID int);

		if @OwnerSourceType = 'Artifact'
			begin
				with h as	(
							select	ID,
									ParentID
							from	Artifact
							where	ID = @OwnerSourceID
							union all
							select	P.ID,
									P.ParentID
							from	Artifact P
									inner join h as C on C.ParentID = P.ID
							)
				insert into @h
					select ID from h;
			end
		else
			begin
				insert into @h values (@OwnerSourceID)
			end;

		with fa as	(
					select	A.ID,
							A.ParentID,
							A.FusionAttributeTypeID
					from	FusionAttributeOwnerRule R
							inner join FusionAttributeOwnerRuleItem RI on RI.FusionAttributeOwnerRuleID = R.ID and R.RelationshipOwnerObjectType = 'Artifact'
							inner join @h H on H.ID = R.RelationshipOwnerObjectID
							inner join FusionAttribute A on (
															(RI.FusionAttributeID is not null and A.ID = RI.FusionAttributeID) OR 
															(RI.FusionAttributeID is null and A.FusionAttributeTypeID = R.ObjectID)
															)
															AND A.FusionID = R.FusionID
					union all
					select	C.ID,
							C.ParentID,
							C.FusionAttributeTypeID
					from	FusionAttribute C
							inner join fa P on C.ParentID = P.ID --and P.ID <> C.ID
					)

		INSERT INTO @tbl
			SELECT	dbo.GenerateObjectUrl(@TargetType, B.FusionAttributeTypeID, B.ID),
					B.ID,
					B.TextPath,
					C.Name
			FROM	FusionAttribute B
					INNER JOIN FusionAttributeType C ON	C.ID = B.FusionAttributeTypeID
													AND B.FusionAttributeTypeID = @TargetTypeID
													AND B.ID NOT IN (SELECT	ID FROM	@IDs)
					INNER JOIN fa on fa.ID = B.ID and fa.FusionAttributeTypeID = @TargetTypeID
	END

	IF (@TargetType = 'Group')
	BEGIN
		INSERT INTO @tbl
			SELECT	dbo.GenerateObjectUrl(@TargetType, ID, ID),
					ID,
					Name,
					Name
			FROM	[Group]
			WHERE	Name LIKE '%' + @Prefix + '%' 
					 AND ID NOT IN (SELECT	ID FROM	@IDs)
	END

	IF (@TargetType = 'Intersect')
	BEGIN
		IF @SourceType = 'FusionAttribute'
		BEGIN
			declare @fusionID int
			select @fusionID = FusionID from FusionAttribute where ID = @SourceID
			insert into @owners
				select	RelationshipOwnerObjectType, 
						RelationshipOwnerObjectID, 
						FusionAttributeID 
				from	GetFusionOwnershipHierarchy(@fusionID, '', 0)
		END

		INSERT INTO @tbl
			SELECT	dbo.GenerateObjectUrl(@TargetType, O.IntersectTypeID, O.ID),
					O.ID,
					O.Name,
					T.Name
			FROM	[Intersect] O
					INNER JOIN IntersectType T ON	O.IntersectTypeID = T.ID
													AND T.ID = @TargetTypeID
													AND T.Name LIKE '%' + @Prefix + '%'
													AND O.ID NOT IN (SELECT	ID FROM	@IDs)
			WHERE	@SourceType <> 'FusionAttribute'
					OR	(
						@SourceType = 'FusionAttribute' and
						O.ID in (
								SELECT	I.ID
								FROM	[Intersect]	I
										INNER JOIN IntersectNode N on N.IntersectID = I.ID
										INNER JOIN @owners FO on FO.ObjectType = N.ObjectType and FO.ObjectID = N.ObjectID and FO.FusionAttributeID = @SourceID
								)
						)
	END

	IF (@TargetType = 'Policy')
	BEGIN
		INSERT INTO @tbl
			SELECT	dbo.GenerateObjectUrl(@TargetType, ID, ID),
					ID,
					TextPath,
					Name
			FROM	[Policy]
			WHERE	Name LIKE '%' + @Prefix + '%' 
					 AND ID NOT IN (SELECT	ID FROM	@IDs)
	END

	IF (@TargetType = 'Resource')
	BEGIN
		INSERT INTO @tbl
			SELECT	dbo.GenerateObjectUrl('ResourceType', ResourceID, ResourceID),
					ResourceID,
					LastName + ', ' + FirstName,
					LastName + ', ' + FirstName
			FROM	reporting.[Global_Resource]
			WHERE	ResourceID > 0
					AND LastName LIKE '%' + @Prefix + '%' 
					AND ResourceID NOT IN (SELECT	ID FROM	@IDs)
	END

	IF (@TargetType = 'Rule')
	BEGIN
		INSERT INTO @tbl
			SELECT	dbo.GenerateObjectUrl(@TargetType, ID, ID),
					ID,
					Name,
					Name
			FROM	[Rule]
			WHERE	Name LIKE '%' + @Prefix + '%' 
					 AND ID NOT IN (SELECT	ID FROM	@IDs)
	END

	IF (@TargetType = 'Taxonomy')
	BEGIN
		INSERT INTO @tbl
			SELECT	dbo.GenerateObjectUrl(@TargetType, B.TaxonomyTypeID, B.ID),
					B.ID,
					B.TextPath,
					C.Name
			FROM	Taxonomy B
					INNER JOIN TaxonomyType C ON	C.ID = B.TaxonomyTypeID
													AND B.TaxonomyTypeID = @TargetTypeID
													AND B.Name LIKE '%' + @Prefix + '%'
													AND B.ID NOT IN (SELECT	ID FROM	@IDs)
	END

	SELECT * FROM @tbl
END
go


UPDATE COMMENT SET VisibilityID = 4 WHERE VisibilityID = 1;
go
ALTER TABLE Follow ADD FollowTypeID INT DEFAULT 1;
ALTER TABLE Follow DROP CONSTRAINT pk_follow;
ALTER TABLE Follow ADD ID INT IDENTITY CONSTRAINT PK_Follow PRIMARY KEY;
go

UPDATE follow SET  followtypeid = 1;
GO

create table FollowChild
(
	ParentObjectType varchar(50),
	ParentObjectID int,
	ObjectID int not null,
	ObjectType varchar(50) not null,
	DateCreated datetime,
	FollowTypeID int,
)
GO

CREATE TABLE [queue].[FollowUpdate](
	[ID] [uniqueidentifier] NOT NULL CONSTRAINT [DF_FollowUpdate]  DEFAULT (newid()),
	[ObjectID] [int] NOT NULL,
	[ObjectType] varchar(50) NOT NULL,
	[MachineAssigned] [varchar](250) NULL,
	[HasError] [bit] NULL CONSTRAINT [DF_FollowUpdate_HasError]  DEFAULT ((0)),
	[ErrorMessage] [nvarchar](max) NULL,
	[NumberOfRetries] [int] NOT NULL CONSTRAINT [DF_FollowUpdate_NumberOfRetries]  DEFAULT ((0)),
	CONSTRAINT [PK_FollowUpdate] PRIMARY KEY CLUSTERED ( [ID] ASC )
)
GO

create view FollowWithChildren
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
GO

ALTER PROCEDURE [dbo].[GetCommentCountByFollower]
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
	left join FollowWithChildren f on f.objectid = c.ownerobjectid and f.objecttype = c.ownerobjecttype
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
go

ALTER PROCEDURE [dbo].[GetCommentDetailsByFollower]
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
	left join FollowWithChildren f on f.objectid = c.ownerobjectid and f.objecttype = c.ownerobjecttype
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
go

CREATE PROCEDURE SetChildrenByFollowID
	@followId int
AS
BEGIN

declare @id int;
declare @type varchar(50);
declare @resourceID int;

select	@id = ObjectId,
		@type = ObjectType,
		@resourceId = ResourceID 
from	follow 
where	id = @followId;

with d as
(
	select	[Object] as ObjectType,
			ObjectID,
			null as IntersectID,
			null as TargetObjectID 
	from	cache.ObjectDetails d 
	where	d.ObjectID = @id and d.[Object] = @type
	union all
	select	d2.[Object] as ObjectType,
			d2.ObjectID,
			null as IntersectID,
			null as TargetObjectID 
	from	d
			inner join cache.ObjectDetails d2 on d2.parentid = d.Objectid 
)
,r as
(
	select	s.SourceObject as ObjectType,
			s.SourceObjectID as ObjectID,
			s.IntersectID,
			s.TargetObjectID 
	from	cache.Relationships s 
	join	d on s.SourceObject = @type and s.SourceObjectID = d.ObjectID
	union all
	select	r2.TargetObject as ObjectType,
			r2.TargetObjectID as ObjectID,
			r.IntersectID,
			null as TargetObjectID 
	from	r
			join cache.Relationships r2 on r2.TargetObject = @type 
										and r2.SourceObjectId = r.TargetObjectID
										and r2.TargetObjectID != r.ObjectID  
										and r2.SourceObjectID = r.ObjectID 
										and r2.SourceObject != r.ObjectType
)

insert into FollowChild (ObjectID, ObjectType, DateCreated, FollowTypeID, ParentObjectType, ParentObjectID)
select	c.ObjectID,
		c.ObjectType,
		getdate() as DateCreated,
		5 as FollowTypeID,
		@type,
		@id
from	(
		select distinct * 
		from 
		(
			select ObjectID,ObjectType from d where ObjectType = @type
			union all
			select ObjectID,ObjectType from r where ObjectType = @type
		) c1
	) c
where	c.objectid != @id and not exists (select * from FollowChild l where l.ObjectID = c.ObjectID and l.ObjectType = c.ObjectType and l.ParentObjectID = @id and l.ParentObjectType = @type)


	--when I follow a parent I need to unfollow any Parent records which are children of the new parent
	delete 
	from	Follow 
	where	ID in	(
					select	f.id 
					from	followchild c
							join follow f on f.resourceId = @resourceID and f.followtypeid = 3 and f.objectid = c.objectid and f.objecttype = c.objecttype
					where	c.parentobjecttype = @type and c.parentobjectid = @id
					);
END
GO


ALTER TRIGGER [dbo].[Taxonomy_AfterInsert]
   ON  [dbo].[Taxonomy] 
   AFTER INSERT
AS 
	SET NOCOUNT ON;
	insert into [queue].[ObjectVersion] ([Object], ObjectID, ResourceID, [Date], [Action], ActionObject, ActionObjectID)
		select 'Taxonomy', ID, coalesce(UpdatedBy, 0), coalesce(UpdatedOn, getutcdate()), 'Created', 'Taxonomy', ID from inserted

	insert into [queue].[ObjectIndex] ([Object], ObjectID, ResourceID, [Date], [Action], ActionObject, ActionObjectID)
		select 'Taxonomy', ID, coalesce(UpdatedBy, 0), coalesce(UpdatedOn, getutcdate()), 'A', 'Taxonomy', ID from inserted

	declare @tblCache table (RowID int identity, ID int)
	insert into @tblCache 
		select ID from inserted

	declare @current int = 1,
			@max int,
			@thisID int
	select @max = max(RowID) from @tblCache

	while @current <= @max
	begin
		select @thisID = ID from @tblCache where RowID = @current
		exec [cache].[SynchronizeObjectDetails] 'Taxonomy', @thisID
		set @current = @current + 1
	end

	declare @tbl table (ID int);

	with d AS
	(
		SELECT	ParentID, 
				ID
		FROM	inserted	
		UNION ALL
		SELECT	C.ParentID, 
				C.ID
		FROM	Taxonomy	C
				INNER JOIN d AS P ON P.ID = C.ParentID
	)

	insert into @tbl
		select ID from d

	update	T
	set		T.TextPath = utility.GetBreadcrumbStringWrapper('Taxonomy', S.ID, '/'),
			T.[Path] = utility.GetBreadcrumbWrapper('Taxonomy', S.ID),
			T.[Level] = utility.GetObjectLevelWrapper('Taxonomy', S.ID)
	from	Taxonomy T
			inner join @tbl S on S.ID = T.ID
	

	insert into [queue].FollowUpdate (ObjectID, ObjectType) 
		select	id as objectid,
				'Taxonomy' as objecttype
		from	inserted
		where	parentid is not null;
GO
