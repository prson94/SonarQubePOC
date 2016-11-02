drop TABLE [dbo].[AlertFlag]
go
drop TABLE [dbo].[BusinessTransformationRule]
go
drop TABLE [dbo].[FollowChild]
GO
drop TABLE [dbo].[FusionAttributeOwnerRuleItem]
go
drop TABLE [dbo].[FusionAttributeOwnerRule]
go
drop TABLE [dbo].[FusionAttributePromotionLogSummary]
go
drop TABLE [dbo].[FusionAttributePromotionRuleItem]
go
drop TABLE [dbo].[FusionAttributePromotionRuleMapping]
go
drop TABLE [dbo].[FusionAttributePromotion]
go

DROP TABLE [dbo].[FusionAttributePromotionRuleStepSetting]
GO
DROP TABLE [dbo].[FusionAttributePromotionRuleStepMapping]
GO
DROP TABLE [dbo].[FusionAttributePromotionRuleStep]
GO
drop TABLE [dbo].[FusionAttributePromotionRule]
go
drop TABLE [dbo].[FusionAttributeSynchronizationStatus]
go
drop TABLE [dbo].[FusionJobHistory]
go
drop TABLE [dbo].[FusionJobSchedule]
go

drop TABLE [dbo].[IntersectMapGroup]
go
drop TABLE [dbo].[IntersectMapSourceRuleContext]
go
drop TABLE [dbo].[IntersectMapSourceRule]
go
drop TABLE [dbo].[IntersectMapSourceTargetRule]
go

drop TABLE [dbo].[IntersectMapTemplateLogSummary]
go
drop TABLE [dbo].[IntersectMapTemplate]
go

drop TABLE [dbo].[IntersectMap]
go

drop TABLE [dbo].[Relation]
go
drop TABLE [dbo].[RelationType]
go

drop TABLE [dbo].[SourceRuleContext]
go

drop TABLE [dbo].[SourceRule]
go

drop TABLE [dbo].[SourceTargetRule]
go

drop TABLE [queue].[BulkLoad]
go

drop view FollowWithChildren
go

drop view [fusion].[AttributeOwner]
go

drop view [fusion].[AttributePromotion]
go

drop procedure [dbo].[GetGroupHierarchy]
go

drop procedure [dbo].[GetHierarchyByMapType]
go

drop PROCEDURE [dbo].[GetIntersectTypeOptions]
go

drop procedure GetLineageDiagram
go

drop PROCEDURE [dbo].[GetNonIntersections]
go

drop PROCEDURE [dbo].[GetRandomSurveyQuestionForUser]
go

drop PROCEDURE [dbo].[GetRelatedObjectsByEventTypeWrapper]
go

drop PROCEDURE [dbo].[SetChildrenByFollowID]
go

drop procedure [fusion].[GetPromotionOptions]
go

drop PROCEDURE [utility].[AddOrUpdateFieldType]
go

drop procedure [utility].[AddRelationDiagramRelations]
go

drop PROCEDURE [utility].[ProcessIntersectTemplates]
go

drop PROCEDURE [validate].[IntersectType]
go

drop FUNCTION [dbo].[GetFusionOwnershipHierarchy]
go

drop FUNCTION [dbo].[GetFusionPromotionsHierarchy]
go


CREATE SCHEMA [quality]
    AUTHORIZATION [dbo];
go

ALTER TABLE [dbo].[Artifact] DROP COLUMN [Path];
GO

ALTER TABLE [dbo].[AttributeType] ALTER COLUMN [Description] NVARCHAR (MAX) NULL;
GO

ALTER TABLE [dbo].[Domain] DROP COLUMN [Path];
GO


ALTER TABLE [dbo].[Taxonomy] DROP COLUMN [Path];
GO

ALTER TABLE [fusion].[RuleItem] ALTER COLUMN [RuleID] INT NULL;
GO

ALTER TABLE [fusion].[StagingRelation] DROP COLUMN [EndIntersectTypeNodeID], COLUMN [StartIntersectTypeNodeID];
GO

CREATE TABLE [quality].[Rule] (
    [ID]                 INT            IDENTITY (1, 1) NOT NULL,
    [Name]               NVARCHAR (250) NOT NULL,
    [Definition]         NVARCHAR (MAX) NULL,
    [Status]             INT            NULL,
    [QualityDimensionID] INT            NULL,
    [Threshold]          DECIMAL (3, 3) NOT NULL,
    [WhatIsWrong]        NVARCHAR (MAX) NULL,
    [WhatIsRight]        NVARCHAR (MAX) NULL,
    [HowToMeasure]       NVARCHAR (MAX) NULL,
    [HowToResolve]       NVARCHAR (MAX) NULL,
    [CreatedOn]          DATETIME       NULL,
    [CreatedBy]          INT            NULL,
    [UpdatedOn]          DATETIME       NULL,
    [UpdatedBy]          INT            NULL,
    CONSTRAINT [PK_QualityRule] PRIMARY KEY CLUSTERED ([ID] ASC)
);
GO

CREATE TABLE [quality].[RuleMap] (
    [QualityRuleID] INT          NOT NULL,
    [SourceID]      VARCHAR (50) NOT NULL,
    CONSTRAINT [PK_QualityRuleMap] PRIMARY KEY CLUSTERED ([QualityRuleID] ASC, [SourceID] ASC)
);
GO

CREATE TABLE [quality].[Dimension] (
    [ID]              INT            IDENTITY (1, 1) NOT NULL,
    [Name]            NVARCHAR (250) NOT NULL,
    [Description]     NVARCHAR (MAX) NULL,
    [IsSystemDefined] BIT            NOT NULL,
    [Weight]          DECIMAL (2, 2) NULL,
    [UpdatedOn]       DATETIME       NOT NULL,
    [UpdatedBy]       INT            NOT NULL,
    CONSTRAINT [PK_RuleDimension] PRIMARY KEY CLUSTERED ([ID] ASC)
);
GO

CREATE TABLE [dbo].[Favorite] (
    [ID]         INT           IDENTITY (1, 1) NOT NULL,
    [ResourceID] INT           NOT NULL,
    [Route]      VARCHAR (250) NULL,
    [Name]       VARCHAR (250) NOT NULL,
    [SortOrder]  INT           NULL,
    [IsOverride] BIT           NOT NULL,
    CONSTRAINT [PK_Favorite_ID] PRIMARY KEY CLUSTERED ([ID] ASC)
);
GO

CREATE TABLE [dbo].[SiteNav] (
    [ID]        INT           IDENTITY (1, 1) NOT NULL,
    [ParentID]  INT           NULL,
    [Name]      VARCHAR (250) NULL,
    [Route]     VARCHAR (250) NULL,
    [SortOrder] INT           NULL
);
GO

ALTER TABLE [quality].[Dimension]
    ADD CONSTRAINT [DF__Dimension__IsSys__34EA6C10] DEFAULT ((0)) FOR [IsSystemDefined];
GO

ALTER TABLE [quality].[Dimension]
    ADD CONSTRAINT [DF__Dimension__Updat__35DE9049] DEFAULT (getutcdate()) FOR [UpdatedOn];
GO

ALTER TABLE [cache].[Relationship]
    ADD CONSTRAINT [DF_CacheRelationship_SourceIntersectNodeID] DEFAULT ((0)) FOR [SourceIntersectNodeID];
GO

ALTER TABLE [cache].[Relationship]
    ADD CONSTRAINT [DF_CacheRelationship_SourceIntersectTypeNodeID] DEFAULT ((0)) FOR [SourceIntersectTypeNodeID];
GO

ALTER TABLE [cache].[Relationship]
    ADD CONSTRAINT [DF_CacheRelationship_TargetIntersectNodeID] DEFAULT ((0)) FOR [TargetIntersectNodeID];
GO

ALTER TABLE [cache].[Relationship]
    ADD CONSTRAINT [DF_CacheRelationship_TargetIntersectTypeNodeID] DEFAULT ((0)) FOR [TargetIntersectTypeNodeID];
GO

ALTER TABLE [dbo].[Favorite]
    ADD CONSTRAINT [DF_Favorite_IsOverride] DEFAULT ((0)) FOR [IsOverride];
GO

ALTER TABLE [quality].[Rule] WITH NOCHECK
    ADD CONSTRAINT [FK_QualityRule_QualityDimension] FOREIGN KEY ([QualityDimensionID]) REFERENCES [quality].[Dimension] ([ID]);
GO

ALTER TABLE [quality].[RuleMap] WITH NOCHECK
    ADD CONSTRAINT [FK_QualityRuleMap_QualityRule] FOREIGN KEY ([QualityRuleID]) REFERENCES [quality].[Rule] ([ID]);
GO

CREATE TRIGGER [dbo].[Favorite_AfterInsert]
	ON [dbo].[Favorite]
	FOR INSERT
AS

	--update sortorder
	update f
	set f.sortorder = s.sortorder 
	from
		favorite f
		join (
		select 
			id, 
			resourceid, 
			row_number() over (partition by resourceid order by sortorder) as sortorder
		from favorite) s on s.id = f.id
		join inserted i on i.ResourceID = s.ResourceID;
GO

CREATE TRIGGER [dbo].[Favorite_AfterDelete]
	ON [dbo].[Favorite]
	FOR DELETE
AS

	--update sortorder
	update f
	set f.sortorder = s.sortorder 
	from
		favorite f
		join (
		select 
			id, 
			resourceid, 
			row_number() over (partition by resourceid order by sortorder) as sortorder
		from favorite) s on s.id = f.id
		join deleted i on i.ResourceID = s.ResourceID;
GO

CREATE FUNCTION [quality].[CalculatePassed]
(
	@PassFraction decimal(4,3),
	@QualityRuleID int
)
RETURNS bit
AS
BEGIN
	DECLARE @Passed bit--,
			--@Threshold decimal(3,3)

	--SELECT @Threshold = Threshold from quality.[Rule] where ID = @QualityRuleID

	select	top 1
			@Passed = case 
						when @PassFraction >= Threshold then cast(1 as bit)
						else cast(0 as bit)
					end
	from	quality.[Rule] 
	where	ID = @QualityRuleID

	RETURN @Passed
END
GO

create FUNCTION [quality].[CalculatePassedWrapper]
(
	@PassFraction decimal(4,3),
	@QualityRuleID int
)
RETURNS bit
AS
BEGIN
	RETURN [quality].CalculatePassed(@PassFraction, @QualityRuleID)
END
GO

CREATE FUNCTION [dbo].[CustomSiteNavigation]
(
	@id int
)
RETURNS XML
WITH RETURNS NULL ON NULL INPUT
AS
BEGIN
	 RETURN 
    (
        SELECT  name
                , [Route] AS url
				, 0 as feature
                , [dbo].CustomSiteNavigation(id)
        FROM    dbo.SiteNav
        WHERE   ParentID = @id
        FOR XML PATH('nav'),TYPE
    )
END
GO

CREATE TABLE [quality].[RuleResult] (
    [ID]                INT      IDENTITY (1, 1) NOT NULL,
    [QualityRuleID]     INT      NULL,
    [EffectiveDate]     DATETIME NOT NULL,
    [RowsPassed]        INT      NOT NULL,
    [RowsFailed]        INT      NOT NULL,
    [PassFraction]      AS       (CONVERT (DECIMAL (4, 3), CONVERT (DECIMAL (18, 3), [RowsPassed], (0)) / (CONVERT (DECIMAL (18, 3), [RowsPassed], (0)) + CONVERT (DECIMAL (18, 3), [RowsFailed], (0))), (0))),
    [FailFraction]      AS       (CONVERT (DECIMAL (4, 3), CONVERT (DECIMAL (3, 3), CONVERT (DECIMAL (18, 3), (1), (0)) - CONVERT (DECIMAL (18, 3), [RowsPassed], (0)) / (CONVERT (DECIMAL (18, 3), [RowsPassed], (0)) + CONVERT (DECIMAL (18, 3), [RowsFailed], (0))), (0)), (0))),
    [Passed]            AS       ([quality].[CalculatePassedWrapper](CONVERT (DECIMAL (4, 3), CONVERT (DECIMAL (18, 3), [RowsPassed], (0)) / (CONVERT (DECIMAL (18, 3), [RowsPassed], (0)) + CONVERT (DECIMAL (18, 3), [RowsFailed], (0))), (0)), [QualityRuleID])),
    [CreatedOn]         DATETIME NULL,
    [CreatedBy]         INT      NULL,
    [FusionAttributeID] INT      NULL,
    CONSTRAINT [PK_QualityRuleResult] PRIMARY KEY CLUSTERED ([ID] ASC)
);
GO

ALTER TABLE [quality].[RuleResult] WITH NOCHECK
    ADD CONSTRAINT [FK_QualityRuleResult_QualityRule] FOREIGN KEY ([QualityRuleID]) REFERENCES [quality].[Rule] ([ID]);
GO

ALTER TABLE [quality].[RuleResult] WITH NOCHECK
    ADD CONSTRAINT [FK_QualityRuleResult_FusionAttribute] FOREIGN KEY ([FusionAttributeID]) REFERENCES [dbo].[FusionAttribute] ([ID]);
GO

ALTER TABLE [quality].[RuleResult]
    ADD CONSTRAINT [DF_QualityRuleResult_CreatedOn] DEFAULT (getutcdate()) FOR [CreatedOn];
GO

ALTER view [cache].[ObjectDetails]
as
	select	D.[Object],
			D.[ObjectID],
			coalesce(O1.Name, O2.Name, O3.Name, O4.Name, O5.Name, O6.Name, O7.Name, O8.Name, O9.Name, O10.Name, O11.Name, O12.Name, O13.Name, case when O14.ResourceID is not null then O14.FirstName + ' ' + O14.LastName else null end, O15.Name, O16.Name, O17.Name, O18.Name, O19.Name, O21.Name, O22.Name, O23.Name, O24.Name, null) as Name,
			coalesce(O1.TextPath, O2.TextPath, O3.Name, O4.TextPath, O5.Name, O6.Name, O7.Name, O8.Name, O9.Name, O10.Name, O11.Name, O12.Name, O13.TextPath, case when O14.ResourceID is not null then O14.FirstName + ' ' + O14.LastName else null end, O15.Name, O16.Name, O17.TextPath, O18.Name, O19.Name, O21.Name, O22.Name, O23.Name, O24.Name, '') as TextPath,
			coalesce(O1.Description, O2.Description, O6.Description, O7.Description, O8.Description, O9.Description, O10.Description, O12.Description, O13.Description, O19.Description,  NULL) as Description,
			case D.[Object]
				when 'Lookup' then dbo.GenerateObjectUrl('Lookup', O20.LookupTypeID, O20.ID)
				when 'LookupType' then dbo.GenerateObjectUrl('LookupType', O21.ID, 0)
				else dbo.GenerateObjectUrl(D.[Object], D.[ObjectTypeID], D.ObjectID) 
			end as Url,
			case 
				when P1.ID is not null then 'Artifact'
				when P2.ID is not null then 'Taxonomy'
				when P3.ID is not null then 'DomainGroup'
				when P4.ID is not null then 'FusionAttribute'
				when P4.ID is not null then 'FusionAttribute'
				when P7.ID is not null then 'ArtifactType'
				when P10.ID is not null then 'AttributeType'
				when P13.ID is not null then 'PolicyType'
				when P17.ID is not null then 'FusionAttributeType'
				else NULL
			end as Parent,
			coalesce(O1.ParentID, O2.ParentID, O3.ParentID, O4.ParentID, O7.ParentID, O10.ParentID, O13.ParentID, O17.ParentID, NULL) as ParentID,
			coalesce(P1.Name, P2.Name, P3.Name, P4.Name, P7.Name, P10.Name, P13.Name, P17.Name, NULL) as ParentName,
			D.[ObjectType],
			D.ObjectTypeID,
			coalesce(OT1.Name, OT2.Name, OT3.Name, OT4.TextPath, OT5.Name, OT12.Name, OT13.Name, OT14.Name, OT15.Name, OT20.Name, OT24.Name, NULL) as ObjectTypeName,
			coalesce(S.IconBackColor, '#000') as IconBackColor,
			coalesce(S.IconForeColor, '#fff') as IconForeColor,
			coalesce(S.IconText, 'leaf') as IconText,
			case D.[Object]
				when 'Lookup' then dbo.GenerateNgObjectUrl('Lookup', O20.LookupTypeID, O20.ID)
				when 'LookupType' then dbo.GenerateNgObjectUrl('LookupType', O21.ID, 0)
				else dbo.GenerateNgObjectUrl(D.[Object], D.[ObjectTypeID], D.ObjectID) 
			end as NgUrl
	from	cache.[Object] D with(nolock)
			left join Artifact O1 with(nolock) on D.[Object] = 'Artifact' and O1.ID = D.ObjectID
			left join ArtifactType OT1 with(nolock) on D.[Object] = 'Artifact' and OT1.ID = O1.ArtifactTypeID
			left join Artifact P1 with(nolock) on D.[Object] = 'Artifact' and P1.ID = O1.ParentID

			left join Taxonomy O2 with(nolock) on D.[Object] = 'Taxonomy' and O2.ID = D.ObjectID
			left join TaxonomyType OT2 with(nolock) on D.[Object] = 'Taxonomy' and OT2.ID = O2.TaxonomyTypeID
			left join Taxonomy P2 with(nolock) on D.[Object] = 'Taxonomy' and P2.ID = O2.ParentID

			left join Domain O3 with(nolock) on D.[Object] = 'Domain' and O3.ID = D.ObjectID
			left join DomainType OT3 with(nolock) on D.[Object] = 'Domain' and OT3.ID = O3.DomainTypeID
			left join DomainGroup P3 with(nolock) on D.[Object] = 'Domain' and P3.ID = O3.DomainGroupID

			left join FusionAttribute O4 with(nolock) on D.[Object] = 'FusionAttribute' and O4.ID = D.ObjectID
			left join FusionAttributeType OT4 with(nolock) on D.[Object] = 'FusionAttribute' and OT4.ID = O4.FusionAttributeTypeID
			left join FusionAttribute P4 with(nolock) on D.[Object] = 'FusionAttribute' and P4.ID = O4.ParentID

			left join Fusion O5 with(nolock) on D.[Object] = 'Fusion' and O5.ID = D.ObjectID
			left join FusionType OT5 with(nolock) on D.[Object] = 'Fusion' and OT5.ID = O5.FusionTypeID

			left join FusionType O6 with(nolock) on D.[Object] = 'FusionType' and O6.ID = D.ObjectID

			left join ArtifactType O7 with(nolock) on D.[Object] = 'ArtifactType' and O7.ID = D.ObjectID
			left join ArtifactType P7 with(nolock) on D.[Object] = 'ArtifactType' and P7.ID = O7.ParentID

			left join TaxonomyType O8 with(nolock) on D.[Object] = 'TaxonomyType' and O8.ID = D.ObjectID

			left join ResponsibilityType O9 with(nolock) on D.[Object] = 'ResponsibilityType' and O9.ID = D.ObjectID

			left join AttributeType O10 with(nolock) on D.[Object] = 'AttributeType' and O10.ID = D.ObjectID
			left join AttributeType P10 with(nolock) on D.[Object] = 'AttributeType' and P10.ID = O10.ParentID

			left join IntersectType O11 with(nolock) on D.[Object] = 'IntersectType' and O11.ID = D.ObjectID

			left join [Rule] O12 with(nolock) on D.[Object] = 'Rule' and O12.ID = D.ObjectID
			left join	(
						select 1 as ID, 'Informational Rule' as Name
						union
						select 2 as ID, 'Quality Check Rule' as Name
						union
						select 3 as ID, 'Metric Rule' as Name
						union
						select 4 as ID, 'Profile Rule' as Name
						) OT12 on D.[Object] = 'Rule' and OT12.ID = O12.RuleType

			left join [Policy] O13 with(nolock) on D.[Object] = 'Policy' and O13.ID = D.ObjectID
			left join PolicyType OT13 with(nolock) on D.[Object] = 'Policy' and OT13.ID = O13.PolicyTypeID
			left join [Policy] P13 with(nolock) on D.[Object] = 'Policy' and P13.ID = O13.ParentID

			left join reporting.Global_Resource O14 with(nolock) on D.[Object] = 'Resource' and O14.ResourceID = D.ObjectID --and O14.Status = 'Active'
			left join (select 1 as ID, 'User' as Name) OT14 on D.[Object] = 'Resource' and OT14.ID = D.ObjectTypeID

			left join [Group] O15 with(nolock) on D.[Object] = 'Group' and O15.ID = D.ObjectID
			left join (
						select 0 as ID, 'Group' as Name
						union
						select 1 as ID, 'Group' as Name
					  ) OT15 on D.[Object] = 'Group' and OT15.ID = D.ObjectTypeID

			left join PolicyType O16 with(nolock) on D.[Object] = 'PolicyType' and O16.ID = D.ObjectID

			left join FusionAttributeType O17 with(nolock) on D.[Object] = 'FusionAttributeType' and O17.ID = D.ObjectID
			left join FusionAttributeType P17 with(nolock) on D.[Object] = 'FusionAttributeType' and P17.ID = O17.ParentID

			left join	(
						select 1 as ID, 'Informational Rule' as Name
						union
						select 2 as ID, 'Quality Check Rule' as Name
						union
						select 3 as ID, 'Metric Rule' as Name
						union
						select 4 as ID, 'Profile Rule' as Name
						) O18 on D.[Object] = 'RuleType' and O18.ID = D.ObjectID

			left join DomainType O19 with(nolock) on D.[Object] = 'DomainType' and O19.ID = D.ObjectID

			left join [Lookup] O20 with(nolock) on D.[Object] = 'Lookup' and O20.ID = D.ObjectID
			left join LookupType OT20 with(nolock) on D.[Object] = 'Lookup' and OT20.ID = O20.LookupTypeID

			left join [LookupType] O21 with(nolock) on D.[Object] = 'LookupType' and O21.ID = D.ObjectID

			left join	(
						select 0 as ID, 'User' as Name
						union
						select 1 as ID, 'User' as Name
						) O22 on D.[Object] = 'ResourceType' and O22.ID = D.ObjectID

			left join	(
						select 0 as ID, 'Group' as Name
						union
						select 1 as ID, 'Group' as Name
						) O23 on D.[Object] = 'GroupType' and O22.ID = D.ObjectID

			left join [Intersect] O24 with(nolock) on D.[Object] = 'Intersect' and O24.ID = D.ObjectID
			left join IntersectType OT24 with(nolock) on D.[Object] = 'Intersect' and OT24.ID = O24.IntersectTypeID

			left join ObjectStyle S with(nolock) on S.ObjectType = D.ObjectType and S.ObjectID = D.[ObjectTypeID]
GO

ALTER view [cache].[Relationships]
as
	SELECT	I.[IntersectTypeID]
			,R.[IntersectID]
			,I.[Classification]
			,I.[Description]
			,R.[SourceIntersectTypeNodeID]
			,R.[SourceIntersectNodeID]
			,R.[SourceObject]
			,R.[SourceObjectID]
			,SD.[TextPath] as [SourceObjectName]
			,SD.[ObjectType] as [SourceType]
			,SD.[ObjectTypeID] as [SourceTypeID]
			,SD.ObjectTypeName as [SourceTypeName]
			,R.[TargetIntersectTypeNodeID]
			,R.[TargetIntersectNodeID]
			,R.[TargetObject]
			,R.[TargetObjectID]
			,TD.TextPath as [TargetObjectName]
			,TD.ObjectType as [TargetType]
			,TD.ObjectTypeID as [TargetTypeID]
			,TD.ObjectTypeName as [TargetTypeName]
			--,substring((
			--			select	', ' + P.Name as [text()]
			--			from	IntersectMap IM
			--					inner join Predicate P on	P.ID = IM.PredicateID	
			--												and (
			--													(IM.SubjectIntersectNodeID = R.[SourceIntersectNodeID] and IM.ObjectIntersectNodeID = R.[TargetIntersectNodeID]) or
			--													(IM.SubjectIntersectNodeID = R.[TargetIntersectNodeID] and IM.ObjectIntersectNodeID = R.[SourceIntersectNodeID])
			--													)
			--			for xml path('')
			--			), 3, 1000) as [Role]
			, '' as [Role]
	FROM	cache.Relationship R
			inner join cache.ObjectDetails SD on SD.[Object] = R.[SourceObject] and SD.[ObjectID] = R.[SourceObjectID]
			inner join cache.ObjectDetails TD on TD.[Object] = R.[TargetObject] and TD.[ObjectID] = R.[TargetObjectID]
			inner join [Intersect] I on I.ID = R.IntersectID
GO


ALTER procedure [tile].[GetObjectStatistics]
	@type varchar(50),
	@id int
AS
BEGIN
declare @table table (Name nvarchar(250), Value varchar(250), [Group] varchar(25), Url varchar(250), MostRecent datetime)
	
	insert into @table
		select NULL, count(1), 'Followers', '/overlays/' + @type + '/' + cast(@id as varchar(10)) + '/followers', max(datecreated)
		from	Follow F
		inner join reporting.Global_Resource R on R.ResourceID = F.ResourceID
		where	F.ObjectType = @type and F.ObjectID = @id
	
	insert into @table
		select	NULL, count(1), 'Comments', '/overlays/' + @type + '/' + cast(@id as varchar(10)) + '/comments', max(datecreated)
		from	Comment C
				inner join CommentRelation R	on R.CommentID = C.ID and C.ParentID is null
												and R.ObjectType = @type and R.ObjectID = @id
                                                and C.ParentID is null
												and C.IsDeleted = 0
	insert into @table
		select NULL, count(1), 'Events', '/overlays/' + @type + '/' + cast(@id as varchar(10)) + '/events', max([date])
			FROM	    [Event] E
					    INNER JOIN EventGroup G ON E.EventGroupID = G.ID and E.Status in ('Active', 'Open')
					    INNER JOIN [Rule] R on R.ID = G.RuleID
					    inner join [Intersect] CR on (
														(CR.Subject = @type and CR.SubjectID = @id and CR.Object = 'Rule' and CR.ObjectID = R.ID) OR
														(CR.Object = @type and CR.ObjectID = @id and CR.Subject = 'Rule' and CR.SubjectID = R.ID)
													 )

	insert into @table values (null, dbo.[GetObjectStatisticScore](@type, @id) * 100, 'Score', '/overlays/' + @type + '/' + cast(@id as varchar(10)) + '/score', null)

	if @type = 'Artifact'
	begin
		insert into @table 
			select		lower(T.Name),
						count(1),
						'Children',
						'/overlays/' + cast(@id as varchar(10)) + '/' + cast(T.ID as varchar(10)) + '/ChildArtifacts',
						max(A.createdon)
			from		Artifact A
						inner join ArtifactType T on T.ID = A.ArtifactTypeID and A.ParentID = @id
			group by	T.Name,
						T.ID
			order by	T.Name


		insert into @table
			select	
				'Issue',
				count(1),
				'Issues',
				'/overlays/Artifact/' + cast(@id as varchar(10)) + '/Issues',
				max(w.datestarted)
			from	
					workflow w
					inner join Comment C on C.ID = w.data.value('(fields/CommentID)[1]', 'int')
					inner join CommentRelation CR on CR.CommentID = C.ID and CR.ObjectType = 'Artifact'
					inner join Artifact A on w.workflowtype = 3 and w.datecompleted is null and A.ID = cr.objectid
			where 
				a.id = @id			
	end


	select * from @table

END
GO

ALTER procedure [utility].[CalculateStatistics]
--declare
	@Type varchar(50) = NULL,
	@ID int = NULL,
	@TargetStatisticTypeID int = NULL
as
begin
	SET NOCOUNT ON;

	declare @current int, @max int
	declare @relations table (ID int identity, [Object] varchar(50), ObjectID int)

	IF OBJECT_ID('tempdb..#StatisticTypes') IS NOT NULL
	BEGIN
		DROP TABLE #StatisticTypes
	END
	create table #StatisticTypes (ID int identity, StatisticTypeID int)

	insert into #StatisticTypes
		select ID from StatisticType where (@TargetStatisticTypeID is not null and ID = @TargetStatisticTypeID) OR @TargetStatisticTypeID is null order by ID

	set		@current	= 1
	select	@max		= MAX(ID) from #StatisticTypes

	IF OBJECT_ID('tempdb..#Statistics') IS NOT NULL
	BEGIN
		DROP TABLE #Statistics
	END
	create table #Statistics (StatisticTypeID int, ObjectType varchar(50), ObjectID int, Score int)

--select * from #StatisticTypes

	while @current <= @max
	begin
		declare @StatisticTypeID int,
				@CheckType int,
				@CheckObjectType varchar(25),
				@CheckObjectID int,
				@Object varchar(25),
				@ObjectID int,
				@Score int,
				@PropertyName varchar(250),
				@Value nvarchar(4000),
				@PredicateID int,
				@Configuration xml

		select	@StatisticTypeID = S.ID,
				@CheckType = S.CheckType,
				@Configuration = S.Configuration,
				@Object = [Object],
				@ObjectID = ObjectID,
				@Score = Score 
		from	#StatisticTypes T
				inner join StatisticType S on S.ID = T.StatisticTypeID
		where	T.ID = @current
				
		delete @relations
		
		insert into @relations
			select	[Object],
					ObjectID
			from	cache.[Object]
			where	ObjectType = @Object
					and ObjectTypeID = @ObjectID
					and (
						(@Type is not null and [Object] = @Type and ObjectID = @ID) OR (@Type is null) 
						)
		
		
		-- EXISTENCE
		if (@CheckType = 1)
		begin
			select	@CheckObjectType = f.value('(ObjectType/text())[1]', 'varchar(25)'),
					@CheckObjectID = f.value('(ObjectID/text())[1]', 'int')
			from	@Configuration.nodes('/fields') as F(f)

			if @CheckObjectType = 'AttributeType'
			begin
				insert into #Statistics
					select	@StatisticTypeID as StatisticTypeID,
							R.[Object],
							R.ObjectID,
							case 
								when O.ValueExists <> 0 then @Score
								else 0
							end as Score
					from	@relations R
							outer apply (
										select		ISNULL(AttributeTypeID, 0) as ValueExists
										from		Attribute 
										where		ObjectType = R.[Object] and ObjectID = R.ObjectID and AttributeTypeID = @CheckObjectID
										group by	AttributeTypeID, ObjectType, ObjectID
										) O
			end

			if @CheckObjectType = 'ResponsibilityType'
			begin
				insert into #Statistics
					select	@StatisticTypeID as StatisticTypeID,
							R.[Object],
							R.ObjectID,
							case 
								when P.ValueExists <> 0 then @Score
								else 0
							end as Score
					from	@relations R
							outer apply (
										select		ISNULL(ResponsibilityTypeID, 0) as ValueExists
										from		[cache].[ResponsibilityItem]
										where		[Object] = R.[Object] and ObjectID = R.ObjectID and ResponsibilityTypeID = @CheckObjectID
										group by	ResponsibilityTypeID, [Object], ObjectID
										) P
			end
		end

		-- COUNT (instead of score)
		if (@CheckType = 2)	--COUNT
		begin
			select	@CheckObjectType = f.value('(ObjectType/text())[1]', 'varchar(25)'),
					@CheckObjectID = f.value('(ObjectID/text())[1]', 'int')
			from	@Configuration.nodes('/fields') as F(f)

			if @CheckObjectType = 'AttributeType'
			begin
				insert into #Statistics
					select	@StatisticTypeID as StatisticTypeID,
							R.[Object],
							R.ObjectID,
							COALESCE(O.Score, 0) as Score
					from	@relations R
							outer apply (
										select		COUNT(1) as Score
										from		Attribute 
										where		ObjectType = R.[Object] and ObjectID = R.ObjectID and AttributeTypeID = @CheckObjectID
										group by	AttributeTypeID, ObjectType, ObjectID
										) O
			end

			if @CheckObjectType = 'ResponsibilityType'
			begin
				insert into #Statistics
					select	@StatisticTypeID as StatisticTypeID,
							R.[Object],
							R.ObjectID,
							COALESCE(O.Score, 0) as Score
					from	@relations R
							outer apply (
										select		COUNT(1) as Score
										from		[cache].[ResponsibilityItem]
										where		[Object] = R.[Object] and ObjectID = R.ObjectID and ResponsibilityTypeID = @CheckObjectID
										group by	ResponsibilityTypeID, [Object], ObjectID
										) O
			end

			-- This does a count on relationships
			if @CheckObjectType <> 'AttributeType' and @CheckObjectType <> 'ResponsibilityType'
			begin
				insert into #Statistics
					select	@StatisticTypeID as StatisticTypeID,
							R.[Object],
							R.ObjectID,
							COALESCE(O.Score, 0) as Score
					from	@relations R
							outer apply (
										select		COUNT(1) as Score
										from		[Intersect] I
													inner join IntersectType IT on	IT.ID = I.IntersectTypeID
																					and (
																						(I.Subject = R.[Object] and I.SubjectID = R.ObjectID) OR
																						(I.Object = R.[Object] and I.ObjectID = R.ObjectID)
																						)
																					and (
																							@CheckObjectType = case 
																											when (I.Subject = R.[Object] and I.SubjectID = R.ObjectID) then IT.Object
																											else IT.Subject
																											end and
																							@CheckObjectID = case 
																											when (I.Subject = R.[Object] and I.SubjectID = R.ObjectID) then IT.ObjectID
																											else IT.SubjectID
																											end																				
																						)
										--group by	ID.ObjectType, ID.ObjectTypeID
										) O
			end
		end

		-- PROPERTY VALUE CHECK
		if (@CheckType = 3)
		begin
			select	@PropertyName = f.value('(PropertyName/text())[1]', 'varchar(250)'),
					@Value = f.value('(PropertyValue/text())[1]', 'nvarchar(4000)')
			from	@Configuration.nodes('/fields') as F(f)

			if @Object = 'ArtifactType' and @PropertyName = 'Status'
				begin
					insert into #Statistics
						select	@StatisticTypeID as StatisticTypeID,
								R.[Object],
								R.ObjectID,
								case 
									when O.ValueExists <> 0 then @Score
									else 0
								end as Score
						from	@relations R
								outer apply (
											select		CASE 
															when [Status] = @Value then 1
															else 0
														END as ValueExists
											from		Artifact
											where		R.[Object] = 'Artifact' and ID = R.ObjectID
											) O
				end
			else
				begin
					-- A dynamic field to check.
					insert into #Statistics
						select	@StatisticTypeID as StatisticTypeID,
								R.[Object],
								R.ObjectID,
								case 
									when O.ValueExists <> 0 then @Score
									else 0
								end as Score
						from	@relations R
								outer apply (
											select	CASE 
														when F.FormattedValue = @Value then 1
														else 0
													END as ValueExists									
											from	Field F
													inner join FieldType FT on FT.[Object] = @Object and FT.ObjectID = @ObjectID 
																			and F.[ObjectType] = R.[Object] and F.ObjectID = R.ObjectID
																			and FT.Name = @PropertyName 
											) O
				end
		end

		-- PROPERTY POPULATED
		if (@CheckType = 4)
		begin
			select	@PropertyName = f.value('(PropertyName/text())[1]', 'varchar(250)')
			from	@Configuration.nodes('/fields') as F(f)

			if @PropertyName = 'Description'
				begin
					insert into #Statistics
						select	@StatisticTypeID as StatisticTypeID,
								R.[Object],
								R.ObjectID,
								case 
									when D.Description is null then 0
									when LEN(D.Description) < 25 then 0
									else @Score
								end as Score
						from	@relations R
								left join cache.ObjectDetails D on D.[Object] = R.[Object] and D.ObjectID = R.ObjectID
				end
			else
				begin
					-- A dynamic field to check.
					insert into #Statistics
						select	@StatisticTypeID as StatisticTypeID,
								R.[Object],
								R.ObjectID,
								case 
									when O.ValueExists <> 0 then @Score
									else 0
								end as Score
						from	@relations R
								outer apply (
											select	case
														when F.FormattedValue is not null then 1
														else 0
													END as ValueExists
											from	Field F
													inner join FieldType FT on FT.[Object] = @Object and FT.ObjectID = @ObjectID 
																			and F.[ObjectType] = R.[Object] and F.ObjectID = R.ObjectID
																			and FT.Name = @PropertyName 
											) O
				end
		end

		-- RELATIONSHIP
		if (@CheckType = 5)
		begin
			declare @checkRelationshipObjects table (Object varchar(50), ObjectID int)

			-- first, check legacy format
			select	@CheckObjectType = f.value('(ObjectType/text())[1]', 'varchar(25)'),
					@CheckObjectID = f.value('(ObjectID/text())[1]', 'int')
			from	@Configuration.nodes('/fields') as F(f)

			if @CheckObjectType is not null and @CheckObjectID is not null
				begin
					insert into @checkRelationshipObjects values (@CheckObjectType, @CheckObjectID)
				end
			else
				begin
					--check new format of multiple options
					insert into @checkRelationshipObjects
						select	f.value('(Object/Type/text())[1]', 'varchar(50)'),
								f.value('(Object/ID/text())[1]', 'int')
						from	@Configuration.nodes('/fields/CheckObjects') as F(f)
				end


			insert into #Statistics
				select	@StatisticTypeID as StatisticTypeID,
						R.[Object],
						R.ObjectID,
						case 
							when O.[Count] > 0 then @Score
							else 0
						end as Score
				from	@relations R
						outer apply (
									select		COUNT(1) as [Count]
									from		[Intersect] IR
												inner join IntersectType IRT on IRT.ID = IR.IntersectTypeID and (
																												(IR.Subject = R.Object and IR.SubjectID = R.ObjectID) OR 
																												(IR.Object = R.Object and IR.ObjectID = R.ObjectID)
																												)
												inner join @checkRelationshipObjects TT on TT.[Object] = case 
																											when (IR.Subject = R.Object and IR.SubjectID = R.ObjectID) then IRT.Object 
																											else IRT.Subject
																										 end
																						and TT.ObjectID = case 
																											when (IR.Subject = R.Object and IR.SubjectID = R.ObjectID) then IRT.ObjectID
																											else IRT.SubjectID
																										 end
									) O

		end

		-- FUSION OWNERSHIP
		if (@CheckType = 6)
		begin
			--select	@CheckObjectType = f.value('(ObjectType/text())[1]', 'varchar(25)'),
			--		@CheckObjectID = f.value('(ObjectID/text())[1]', 'int')
			--from	@Configuration.nodes('/fields') as F(f)

			insert into #Statistics
				select	@StatisticTypeID as StatisticTypeID,
						R.[Object],
						R.ObjectID,
						case 
							when O.ValueExists <> 0 then @Score
							else 0
						end as Score
				from	@relations R
						outer apply (
									select		ISNULL(ArtifactID, 0) as ValueExists
									from		FusionOwner
									where		ArtifactID = R.ObjectID
									group by	ArtifactID
									) O
		end

		-- ROLLUP VIA RELATIONSHIPS
		if (@CheckType = 7)
		begin
			select	@CheckObjectType = f.value('(ObjectType/text())[1]', 'varchar(25)'),
					@CheckObjectID = f.value('(ObjectID/text())[1]', 'int')
			from	@Configuration.nodes('/fields') as F(f)

			insert into #Statistics
				select	@StatisticTypeID as StatisticTypeID,
						R.[Object],
						R.ObjectID,
						round((T.Total/C.[Count]) * @Score, 0) Score
				from	@relations R
						cross apply (
									select	count(1) as [Count] 
									from	[Intersect] I
											inner join IntersectType IT on	IT.ID = I.IntersectTypeID
																			and (
																				(I.Subject = R.[Object] and I.SubjectID = R.ObjectID) OR
																				(I.Object = R.[Object] and I.ObjectID = R.ObjectID)
																				)
																			and (
																					@CheckObjectType = case 
																									when (I.Subject = R.[Object] and I.SubjectID = R.ObjectID) then IT.Object
																									else IT.Subject
																								 end and
																					@CheckObjectID = case 
																									when (I.Subject = R.[Object] and I.SubjectID = R.ObjectID) then IT.ObjectID
																									else IT.SubjectID
																								 end																				
																				)
									) C
						outer apply (
									select	sum(dbo.GetObjectStatisticScore(O, OID)) as Total
									from	(
											select	case 
														when (I.Subject = R.[Object] and I.SubjectID = R.ObjectID) then I.Object
														else I.Subject
													end as O, 
													case 
														when (I.Subject = R.[Object] and I.SubjectID = R.ObjectID) then I.ObjectID
														else I.SubjectID
													end as OID
											from	[Intersect] I
													inner join IntersectType IT on	IT.ID = I.IntersectTypeID
																					and (
																						(I.Subject = R.[Object] and I.SubjectID = R.ObjectID) OR
																						(I.Object = R.[Object] and I.ObjectID = R.ObjectID)
																						)
																					and (
																							@CheckObjectType = case 
																											when (I.Subject = R.[Object] and I.SubjectID = R.ObjectID) then IT.Object
																											else IT.Subject
																										 end and
																							@CheckObjectID = case 
																											when (I.Subject = R.[Object] and I.SubjectID = R.ObjectID) then IT.ObjectID
																											else IT.SubjectID
																										 end																				
																						)
										) I
									) T
				where C.[Count] > 0
		end

		-- ROLLUP VIA OWNERSHIP
		if (@CheckType = 8)
		begin
			select	@CheckObjectType = f.value('(ObjectType/text())[1]', 'varchar(25)'),
					@CheckObjectID = f.value('(ObjectID/text())[1]', 'int')
			from	@Configuration.nodes('/fields') as F(f)

			insert into #Statistics
				select	@StatisticTypeID as StatisticTypeID,
						R.[Object],
						R.ObjectID,
						round((T.Total/C.[Count]) * @Score, 0) Score
				from	@relations R
						cross apply (
									select	count(1) as [Count] 
									from	cache.Responsibilities
									where	ResponsibleObject = R.[Object] and ResponsibleObjectID = R.ObjectID
											and ObjectType = @CheckObjectType and ObjectTypeID = @CheckObjectID
									) C
						outer apply (
									select	sum(dbo.GetObjectStatisticScore([Object], ObjectID)) as Total
									from	cache.Responsibilities 
									where	ResponsibleObject = R.[Object] and ResponsibleObjectID = R.ObjectID 
											and ObjectType = @CheckObjectType and ObjectTypeID = @CheckObjectID
									) T
				where C.[Count] > 0
		end
		

		-- EVENT METRIC CHECK
		if (@CheckType = 9)
		begin
			declare @ValidField nvarchar(250),-- = 'ValidCount',
					@InvalidField nvarchar(250),-- = 'InvalidCount',
					@Threshold decimal(9,2),-- = 0.10,
					@TotalValid float,
					@TotalInvalid float

			select	@ValidField = f.value('(ValidField/text())[1]', 'nvarchar(250)'),
					@InvalidField = f.value('(InvalidField/text())[1]', 'nvarchar(250)'),
					@Threshold = f.value('(Threshold/text())[1]', 'decimal(9,2)')
			from	@Configuration.nodes('/fields') as F(f)


			select	@TotalValid = sum(cast(V.ValidCount as int)),
					@TotalInvalid = sum(cast(I.InvalidCount as int))
			from	[Intersect] REL
					inner join [Rule] R on ((R.ID = REL.ObjectID and REL.Object = 'Rule') OR (R.ID = REL.SubjectID and REL.Subject = 'Rule')) and R.RuleType in (3,4)
					inner join EventGroup EG on EG.RuleID = R.ID
					inner join [Event] E on E.EventGroupID = EG.ID 
					inner join (
								select	R.ID,
										max(E.Date) as [Date]
								from	[Intersect] REL
										inner join [Rule] R on ((R.ID = REL.ObjectID and REL.Object = 'Rule') OR (R.ID = REL.SubjectID and REL.Subject = 'Rule')) and R.RuleType in (3,4)
										inner join EventGroup EG on EG.RuleID = R.ID
										inner join [Event] E on E.EventGroupID = EG.ID
								group by R.ID					
								) F on F.ID = R.ID and F.[Date] = E.[Date]
					cross apply (
								select	Value as ValidCount
								from	FieldWithRelation
								where	ObjectType = 'Event' and ObjectID = E.ID and Name = @ValidField
								) V
					cross apply (
								select	Value as InvalidCount
								from	FieldWithRelation
								where	ObjectType = 'Event' and ObjectID = E.ID and Name = @InvalidField
								) I

			insert into #Statistics
				select	@StatisticTypeID as StatisticTypeID,
						R.[Object],
						R.ObjectID,
						case 
							when cast(@TotalInvalid / @TotalValid as decimal(9,2)) < @Threshold then @Score
							else 0
						end as Score
				from	@relations R
		end

		-- PREDICATE CHECK
		if (@CheckType = 10)
		begin
			select	@PredicateID = f.value('(Predicate/text())[1]', 'int')
			from	@Configuration.nodes('/fields') as F(f)

			insert into #Statistics
				select	@StatisticTypeID as StatisticTypeID,
						R.[Object],
						R.ObjectID,
						case 
							when O.[Count] > 0 then @Score
							else 0
						end as Score
				from	@relations R
						outer apply (
									select	count(1) as [Count]
									from	[Intersect] I
											inner join IntersectType IT on IT.ID = I.IntersectTypeID and 
																		IT.PredicateID = @PredicateID and 
																		(
																		(I.Subject = R.Object and I.SubjectID = R.ObjectID) OR
																		(I.Object = R.Object and I.ObjectID = R.ObjectID)
																		)
									) O
		end

		set @current = @current + 1
	end

	
	-- now merge the Statistics table
	MERGE	Statistic AS T
	USING	(
			select	distinct
					S.*,
					MS.DateStart
			from	#Statistics S
					outer apply (
								select		StatisticTypeID,
											ObjectType,
											ObjectID,
											MAX(DateStart) as DateStart
								from		Statistic
								where		StatisticTypeID = S.StatisticTypeID
											and ObjectType = S.ObjectType
											and ObjectID = S.ObjectID
								group by	StatisticTypeID,
											ObjectType,
											ObjectID
								) MS
			) AS S
	ON		(
			T.StatisticTypeID = S.StatisticTypeID
			and T.ObjectType = S.ObjectType
			and T.ObjectID = S.ObjectID
			and T.DateStart = S.DateStart
			and T.Score = S.Score
			)
		WHEN MATCHED THEN 
			UPDATE SET T.DateEnd = getutcdate()
		WHEN NOT MATCHED THEN	
			INSERT	
			VALUES	(
					S.StatisticTypeID, 
					S.ObjectType, 
					S.ObjectID,
					getutcdate(), 
					getutcdate(), 
					S.Score
					);
	
end
GO

ALTER procedure [dbo].[DeleteObject]
	@Obj varchar(50),
	@ObjectID int,
	@ResourceID int
as
begin
	set nocount on;
	
	declare @Object varchar(50) = @Obj,
			@trans varchar(25) = 'Trans',
			@current int = 1,
			@max int

	begin try
		begin transaction @trans

		INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
        VALUES (
				'ObjectVersion', 
				'<fields>
				 <Action>Removed</Action>
				 <ActionObject>' + @Obj + '</ActionObject>
				 <ActionObjectID>' + cast(@ObjectID as varchar) + '</ActionObjectID>
				 <ResourceID>' + cast(@ResourceID as varchar) + '</ResourceID>
				</fields>', 
				@Obj, 
				@ObjectID)

		INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
		values ('ObjectIndex', 'D', @Obj, @ObjectID)

		--COMMON
		delete CommentRelation					where ObjectType = @Object and ObjectID = @ObjectID
		delete Field							where ObjectType = @Object and ObjectID = @ObjectID
		delete Follow							where ObjectType = @Object and ObjectID = @ObjectID
		delete Responsibility					where ObjectType = @Object and ObjectID = @ObjectID
		--delete SurveyObjectCache				where ObjectType = @Object and ObjectID = @ObjectID
		delete cache.[Object]					where [Object] = @Object and ObjectID = @ObjectID

		if charindex('Type', @Object) > 0
		begin
			delete AttributeTypeRelation			where ObjectType = @Object AND ObjectID = @ObjectID
			delete FieldType						where [Object] = @Object AND ObjectID = @ObjectID
			delete ResponsibilityTypeRelation		where ObjectType = @Object and ObjectID = @ObjectID
			delete ResponsibilityTypeObjectClaim	where ObjectType = @Object and ObjectID = @ObjectID
			delete StatisticType					where [Object] = @Object and [ObjectID] = @ObjectID
			delete WorkflowTypeRelation				where [Object] = @Object and ObjectID = @ObjectID

			if @Object = 'ArtifactType'
			begin
				declare @ah table (ID int);
				with ah as	(
							select	ID, 
									ParentID
							from	Artifact
							where	ArtifactTypeID = @ObjectID
							union all
							select	C.ID,
									C.ParentID
							from	Artifact C
									inner join ah P on P.ID = C.ParentID
							)
				insert into @ah 
					select ID from ah
			
				delete Artifact where ID in (select ID from @ah)
			end

			if @Object = 'AttributeType'
			begin
				delete AttributeTypeRelation		where AttributeTypeID = @ObjectID
			
				declare @ath table (RowID int identity, ID int, ParentID int null, [Level] int);
				with ath as	(
							select	ID,
									ParentID,
									1 as [Level]
							from	AttributeType
							where	ID = @ObjectID
							union all
							select	C.ID,
									C.ParentID,
									P.[Level] + 1 as [Level]
							from	AttributeType C
									inner join ath P on P.ID = C.ParentID
							)
				insert into @ath 
					select ID, ParentID, [Level] from ath order by [Level] desc

				select @max = max(RowID) from @ath

				while @current <= @max
				begin
					declare @attributeTypeID int
					select @attributeTypeID = ID from @ath where RowID = @current
					delete Attribute where AttributeTypeID = @attributeTypeID
					delete AttributeType where ID = @attributeTypeID
					set @current = @current + 1
				end
			end

			if @Object = 'DomainType'
			begin
				delete DomainItem where DomainID in (select ID from Domain where DomainTypeID = @ObjectID)
				delete Domain where DomainTypeID = @ObjectID
				delete DomainGroup where DomainTypeID = @ObjectID
			end

			if @Object = 'FieldType'
			begin
				delete Field where FieldTypeID = @ObjectID
				delete FieldType where ID = @ObjectID
			end

			if @Object = 'FusionAttributeType'
			begin
				declare @fath table (RowID int identity, ID int, ParentID int null, [Level] int);
				with fath as	(
							select	ID,
									ParentID,
									1 as [Level]
							from	FusionAttributeType
							where	ID = @ObjectID
							union all
							select	C.ID,
									C.ParentID,
									P.[Level] + 1 as [Level]
							from	FusionAttributeType C
									inner join fath P on P.ID = C.ParentID
							)
				insert into @fath 
					select ID, ParentID, [Level] from fath order by [Level] desc

				select @max = max(RowID) from @fath

				while @current <= @max
				begin
					declare @fusionAttributeTypeID int
					select @fusionAttributeTypeID = ID from @fath where RowID = @current
					delete FusionAttribute where FusionAttributeTypeID = @fusionAttributeTypeID
					delete FusionAttributeType where ID = @fusionAttributeTypeID
					set @current = @current + 1
				end
			end

			if @Object = 'FusionType'
			begin
				declare @fth table (RowID int identity, ID int, ParentID int null, [Level] int);
				with fth as	(
							select	ID,
									ParentID,
									1 as [Level]
							from	FusionAttributeType
							where	FusionTypeID = @ObjectID and ParentID is null
							union all
							select	C.ID,
									C.ParentID,
									P.[Level] + 1 as [Level]
							from	FusionAttributeType C
									inner join fth P on P.ID = C.ParentID
							)
				insert into @fth 
					select ID, ParentID, [Level] from fth order by [Level] desc

				select @max = max(RowID) from @fth

				while @current <= @max
				begin
					declare @fattributeTypeID int
					select @fattributeTypeID = ID from @fth where RowID = @current
					delete FusionAttribute where FusionAttributeTypeID = @fattributeTypeID
					delete FusionAttributeType where ID = @fattributeTypeID
					set @current = @current + 1
				end
				delete FusionType where ID = @ObjectID
			end

			if @Object = 'IntersectType'
			begin
				-- Stores the sources we have identified through the loop below.
				declare @tblRelationshipIDs table (ID int)

				--Seed initial tables values
				insert into @tblRelationshipIDs
					select	R.ID 
					from	Responsibility R
							inner join [Intersect] I on I.IntersectTypeID = 2 and R.ObjectType = 'Intersect' and R.ObjectID = I.ID 

				-- follow trail all the way back.
				while exists(
						select	1 
						from	Responsibility
						where	TargetResponsibilityID in (select ID from @tblRelationshipIDs)
								and ID not in (select ID from @tblRelationshipIDs)
				)
				begin
					insert into @tblRelationshipIDs
						select	ID
						from	Responsibility
						where	TargetResponsibilityID in (select ID from @tblRelationshipIDs)
								and ID not in (select ID from @tblRelationshipIDs)
				end

				delete Responsibility where ID in (select ID from @tblRelationshipIDs)

				delete [Intersect] where IntersectTypeID = @ObjectID
				delete IntersectType where ID = @ObjectID
			end

			if @Object = 'LookupType'
			begin
				delete [Lookup] where LookupTypeID = @ObjectID
			end

			if @Object = 'PolicyType'
			begin
				delete Policy where PolicyTypeID = @ObjectID
				delete PolicyTypeLevel where PolicyTypeID = @ObjectID
			end

			if @Object = 'ResponsibilityType'
			begin
				delete Responsibility where ResponsibilityTypeID = @ObjectID
				delete ResponsibilityType where ID = @ObjectID
			end

			--if @Object = 'StatisticType'
			--begin
			--	delete [Statistic] where StatisticTypeID = @ObjectID
			--end

			if @Object = 'SurveyType'
			begin
				--delete SurveyObjectCache where SurveyTypeID = @ObjectID
				delete Survey where SurveyTypeID = @ObjectID
				delete SurveyType where ID = @ObjectID
			end

			if @Object = 'TaxonomyType'
			begin
				delete Taxonomy where TaxonomyTypeID = @ObjectID
				delete TaxonomyTypeLevel where TaxonomyTypeID = @ObjectID
				delete TaxonomyType where ID = @ObjectID
			end

		end
		else
		begin
			delete Attribute							where ObjectType = @Object and ObjectID = @ObjectID
			delete cache.Relationship					where [SourceObject] = @Object and SourceObjectID = @ObjectID
			delete cache.Relationship					where [TargetObject] = @Object and TargetObjectID = @ObjectID

			BEGIN TRY
				DECLARE @tblIntersectIDs table (ID int)

				INSERT INTO @tblIntersectIDs
					SELECT	IntersectID
					FROM	IntersectNode
					WHERE	ObjectType = @Object and ObjectID = @ObjectID

				delete	[Intersect] where ID in (select ID from @tblIntersectIDs)
				delete	MapItem 
				where	SourceIntersectID in (select ID from @tblIntersectIDs) OR
						TargetIntersectID in (select ID from @tblIntersectIDs)
			END TRY
			BEGIN CATCH

			END CATCH

			if @Object = 'Artifact'
			begin
				delete	RelatedArtifact where ArtifactID = @ObjectID
			end

			if @Object = 'Domain'
			begin
				delete DomainItem where DomainID = @ObjectID
			end

			if @Object = 'Taxonomy'
			begin
				declare @th table (ID int);
				with th as	(
							select	ID, 
									ParentID
							from	Taxonomy
							where	ID = @ObjectID
							union all
							select	C.ID,
									C.ParentID
							from	Taxonomy C
									inner join th P on P.ID = C.ParentID
							)
				insert into @th 
					select ID from th
			
				delete Taxonomy where ID in (select ID from @th)
			end
		end
		
		commit transaction @trans
	end try
	begin catch
		 DECLARE @ErrorMessage NVARCHAR(4000);
    DECLARE @ErrorSeverity INT;
    DECLARE @ErrorState INT;

    SELECT 
        @ErrorMessage = ERROR_MESSAGE(),
        @ErrorSeverity = ERROR_SEVERITY(),
        @ErrorState = ERROR_STATE();

    -- Use RAISERROR inside the CATCH block to return error
    -- information about the original error that caused
    -- execution to jump to the CATCH block.
    RAISERROR (@ErrorMessage, -- Message text.
               @ErrorSeverity, -- Severity.
               @ErrorState -- State.
               );

		rollback transaction @trans
	end catch
end
GO

ALTER procedure [utility].[AddAuditEntry]
--declare
	@Object varchar(50),
	@ObjectID int,
	@ResourceID int,
	@Date datetime,
	@Action varchar(15),
	@ActionObject varchar(50),
	@ActionObjectID int
--set @Object = 'Taxonomy'--'Artifact'
--set @ObjectID = 229--733
--set @ResourceID = 1
--set @Action = 'Updated'
--set @ActionObject = 'Taxonomy' --'Artifact'
--set @ActionObjectID = 229 --733
as
begin
	set nocount on;
	declare @objectName nvarchar(250),
			@actionObjectTypeName nvarchar(250),
			@actionObjectName nvarchar(250),
			@actionDescription nvarchar(max)
	
	declare @tbl table (ID int identity, FieldTypeID int, FieldName nvarchar(250), NewValue nvarchar(max), MostRecentVersion int, Updated bit)

	-- Object Resolution --------------------------------------------------
	if @Object = 'Artifact'				begin		select @objectName = Name from Artifact where ID = @ObjectID				end
	if @Object = 'ArtifactType'			begin		select @objectName = Name from ArtifactType where ID = @ObjectID			end
	if @Object = 'AttributeType'		begin		select @objectName = Name from AttributeType where ID = @ObjectID			end
	if @Object = 'Domain'				begin		select @objectName = Name from Domain where ID = @ObjectID					end
	if @Object = 'DomainGroup'			begin		select @objectName = Name from DomainGroup where ID = @ObjectID				end
	if @Object = 'DomainType'			begin		select @objectName = Name from DomainType where ID = @ObjectID				end
	if @Object = 'Fusion'				begin		select @objectName = Name from Fusion where ID = @ObjectID					end
	if @Object = 'FusionAttribute'		begin		select @objectName = TextPath from FusionAttribute where ID = @ObjectID		end
	if @Object = 'FusionAttributeType'	begin		select @objectName = Name from FusionAttributeType where ID = @ObjectID		end
	if @Object = 'FusionType'			begin		select @objectName = Name from FusionType where ID = @ObjectID				end
	if @Object = 'Group'				begin		select @objectName = Name from [Group] where ID = @ObjectID					end
	if @Object = 'Intersect'			begin		select @objectName = Name from [Intersect] where ID = @ObjectID				end
	if @Object = 'IntersectType'		begin		select @objectName = Name from IntersectType where ID = @ObjectID			end
	if @Object = 'LoadType'				begin		select @objectName = Name from LoadType where ID = @ObjectID				end
	if @Object = 'LookupType'			begin		select @objectName = Name from LookupType where ID = @ObjectID				end
	if @Object = 'Policy'				begin		select @objectName = Name from Policy where ID = @ObjectID					end
	if @Object = 'Report'				begin		select @objectName = Name from Report where ID = @ObjectID					end
	if @Object = 'ResponsibilityType'	begin		select @objectName = Name from ResponsibilityType where ID = @ObjectID		end
	if @Object = 'Rule'					begin		select @objectName = Name from [Rule] where ID = @ObjectID					end
	if @Object = 'StatisticType'		begin		select @objectName = Name from StatisticType where ID = @ObjectID			end
	if @Object = 'SurveyType'			begin		select @objectName = Name from SurveyType where ID = @ObjectID				end
	if @Object = 'Taxonomy'				begin		select @objectName = Name from Taxonomy where ID = @ObjectID				end
	if @Object = 'TaxonomyType'			begin		select @objectName = Name from TaxonomyType where ID = @ObjectID			end
	----------------------------------------------------------------------

	-- Action Object Resolution ------------------------------------------

	-- Relevant ONLY to: Artifact, ArtifactType
	if @ActionObject = 'Artifact'
	begin
		select	@actionObjectTypeName = T.Name,
				@actionObjectName = O.TextPath
		from	Artifact O
				inner join ArtifactType T on T.ID = O.ArtifactTypeID
		where	O.ID = @ActionObjectID

		insert into @tbl  select 0, 'Name', Name, 0, 0 from Artifact where ID = @ActionObjectID
		insert into @tbl  select 0, 'ParentID', ParentID, 0, 0 from Artifact where ID = @ActionObjectID
		insert into @tbl  select 0, 'Description', Description, 0, 0 from Artifact where ID = @ActionObjectID
		insert into @tbl  select 0, 'TaxonomyTypeID', TaxonomyTypeID, 0, 0 from Artifact where ID = @ActionObjectID
		insert into @tbl  select 0, 'Status', Status, 0, 0 from Artifact where ID = @ActionObjectID
		insert into @tbl  select 0, 'DateLastCertified', DateLastCertified, 0, 0 from Artifact where ID = @ActionObjectID
	end

	-- Relevant ONLY to: ArtifactType
	if @ActionObject = 'ArtifactType'
	begin
		select	@actionObjectTypeName = 'Artifact Type',
				@actionObjectName = O.Name 
		from	ArtifactType O
		where	O.ID = @ActionObjectID

		insert into @tbl  select 0, 'Name', Name, 0, 0 from ArtifactType where ID = @ActionObjectID
		insert into @tbl  select 0, 'ParentID', ParentID, 0, 0 from ArtifactType where ID = @ActionObjectID
		insert into @tbl  select 0, 'Description', Description, 0, 0 from ArtifactType where ID = @ActionObjectID
		insert into @tbl  select 0, 'CanOwnFusion', CanOwnFusion, 0, 0 from ArtifactType where ID = @ActionObjectID
		--insert into @tbl  select 0, 'SourcingApplies', SourcingApplies, 0, 0 from ArtifactType where ID = @ActionObjectID
		insert into @tbl  select 0, 'AllowRelatedArtifacts', AllowRelatedArtifacts, 0, 0 from ArtifactType where ID = @ActionObjectID
	end
	
	-- Relevant ONLY to: Artifact, Domain, Fusion, FusionAttribute, Intersect, Taxonomy
	if @ActionObject = 'Attribute'
	begin
		select	@actionObjectTypeName = T.Name,
				@actionObjectName = T.Name + ' Attribute ' + cast(O.ID as nvarchar(15)) 
		from	Attribute O
				inner join AttributeType T on T.ID = O.AttributeTypeID
		where	O.ID = @ActionObjectID
	end

	-- Relevant ONLY to: AttributeType
	if @ActionObject = 'AttributeType'
	begin
		select	@actionObjectTypeName = 'Attribute Type',
				@actionObjectName = O.Name
		from	AttributeType O
		where	O.ID = @ActionObjectID

		insert into @tbl  select 0, 'Name', Name, 0, 0 from AttributeType where ID = @ActionObjectID
		insert into @tbl  select 0, 'ParentID', ParentID, 0, 0 from AttributeType where ID = @ActionObjectID
		insert into @tbl  select 0, 'Description', Description, 0, 0 from AttributeType where ID = @ActionObjectID
		insert into @tbl  select 0, 'TextFormatString', TextFormatString, 0, 0 from AttributeType where ID = @ActionObjectID
		insert into @tbl  select 0, 'AttributeTypeCategoryID', AttributeTypeCategoryID, 0, 0 from AttributeType where ID = @ActionObjectID
	end

	-- Relevant ONLY to: Domain
	if @ActionObject = 'DomainItem'
	begin
		select	@actionObjectTypeName = T.Name,
				@actionObjectName = O.Name 
		from	DomainItem O
				inner join Domain T on T.ID = O.DomainID
		where	O.ID = @ActionObjectID

		insert into @tbl  select 0, 'Name', Name, 0, 0 from DomainItem where ID = @ActionObjectID
		insert into @tbl  select 0, 'Code', Code, 0, 0 from DomainItem where ID = @ActionObjectID
		insert into @tbl  select 0, 'Description', Description, 0, 0 from DomainItem where ID = @ActionObjectID
	end

	-- Relevant ONLY to: Domain, DomainGroup, DomainType
	if @ActionObject = 'Domain'
	begin
		select	@actionObjectTypeName = T.Name,
				@actionObjectName = O.Name 
		from	Domain O
				inner join DomainType T on T.ID = O.DomainTypeID
		where	O.ID = @ActionObjectID

		insert into @tbl  select 0, 'Name', Name, 0, 0 from Domain where ID = @ActionObjectID
		--insert into @tbl  select 0, 'ParentID', ParentID, 0, 0 from Domain where ID = @ActionObjectID
		insert into @tbl  select 0, 'Description', Description, 0, 0 from Domain where ID = @ActionObjectID
		insert into @tbl  select 0, 'DomainGroupID', DomainGroupID, 0, 0 from Domain where ID = @ActionObjectID
	end

	-- Relevant ONLY to: DomainGroup, DomainType
	if @ActionObject = 'DomainGroup'
	begin
		select	@actionObjectTypeName = T.Name,
				@actionObjectName = O.Name 
		from	DomainGroup O
				inner join DomainType T on T.ID = O.DomainTypeID
		where	O.ID = @ActionObjectID

		insert into @tbl  select 0, 'Name', Name, 0, 0 from DomainGroup where ID = @ActionObjectID
		insert into @tbl  select 0, 'MasterListID', MasterListID, 0, 0 from DomainGroup where ID = @ActionObjectID
		insert into @tbl  select 0, 'Description', Description, 0, 0 from DomainGroup where ID = @ActionObjectID
	end

	-- Relevant ONLY to: DomainType
	if @ActionObject = 'DomainType'
	begin
		select	@actionObjectTypeName = 'Domain Type',
				@actionObjectName = O.Name
		from	DomainType O
		where	O.ID = @ActionObjectID

		insert into @tbl  select 0, 'Name', Name, 0, 0 from DomainType where ID = @ActionObjectID
		insert into @tbl  select 0, 'Description', Description, 0, 0 from DomainType where ID = @ActionObjectID
	end
	
	-- Relevant ONLY to: Rule
	if @ActionObject = 'EventGroup'
	begin
		select	@actionObjectTypeName = T.Name,
				@actionObjectName = O.Name 
		from	EventGroup O
				inner join [Rule] T on T.ID = O.RuleID
		where	O.ID = @ActionObjectID

		insert into @tbl  select 0, 'Name', Name, 0, 0 from EventGroup where ID = @ActionObjectID
		insert into @tbl  select 0, 'PublicID', PublicID, 0, 0 from EventGroup where ID = @ActionObjectID
	end

	-- Relevant ONLY to: Fusion
	if @ActionObject = 'Fusion'
	begin
		select	@actionObjectTypeName = T.Name,
				@actionObjectName = O.Name 
		from	Fusion O
				inner join FusionType T on T.ID = O.FusionTypeID
		where	O.ID = @ActionObjectID

		insert into @tbl  select 0, 'Name', Name, 0, 0 from Fusion where ID = @ActionObjectID
		insert into @tbl  select 0, 'Description', Description, 0, 0 from Fusion where ID = @ActionObjectID
		insert into @tbl  select 0, 'Enabled', Enabled, 0, 0 from Fusion where ID = @ActionObjectID
		insert into @tbl  select 0, 'Manual', Manual, 0, 0 from Fusion where ID = @ActionObjectID
		insert into @tbl  select 0, 'LockPromotedItems', LockPromotedItems, 0, 0 from Fusion where ID = @ActionObjectID
		insert into @tbl  select 0, 'IntervalType', IntervalType, 0, 0 from Fusion where ID = @ActionObjectID
		insert into @tbl  select 0, 'Interval', Interval, 0, 0 from Fusion where ID = @ActionObjectID
		insert into @tbl  select 0, 'ForceRefresh', ForceRefresh, 0, 0 from Fusion where ID = @ActionObjectID
	end

	-- Relevant ONLY to: FusionAttributeType, FusionType
	if @ActionObject = 'FusionAttributeType'
	begin
		select	@actionObjectTypeName = T.Name,
				@actionObjectName = O.Name 
		from	FusionAttributeType O
				inner join FusionType T on T.ID = O.FusionTypeID
		where	O.ID = @ActionObjectID

		insert into @tbl  select 0, 'Name', Name, 0, 0 from FusionAttributeType where ID = @ActionObjectID
		insert into @tbl  select 0, 'Assignable', Assignable, 0, 0 from FusionAttributeType where ID = @ActionObjectID
	end

	-- Relevant ONLY to: FusionType
	if @ActionObject = 'FusionType'
	begin
		select	@actionObjectTypeName = 'Fusion Type',
				@actionObjectName = O.Name 
		from	FusionType O
		where	O.ID = @ActionObjectID

		insert into @tbl  select 0, 'Name', Name, 0, 0 from FusionType where ID = @ActionObjectID
		insert into @tbl  select 0, 'Description', Description, 0, 0 from FusionType where ID = @ActionObjectID
	end

	-- Relevant ONLY to: Group
	if @ActionObject = 'Group'
	begin
		select	@actionObjectTypeName = 'Group',
				@actionObjectName = O.Name 
		from	[Group] O
		where	O.ID = @ActionObjectID

		insert into @tbl  select 0, 'Name', Name, 0, 0 from [Group] where ID = @ActionObjectID
		insert into @tbl  select 0, 'Description', Description, 0, 0 from [Group] where ID = @ActionObjectID
		insert into @tbl  select 0, 'PrimaryOwnerResourceID', PrimaryOwnerResourceID, 0, 0 from [Group] where ID = @ActionObjectID
		insert into @tbl  select 0, 'SecondaryOwnerResourceID', SecondaryOwnerResourceID, 0, 0 from [Group] where ID = @ActionObjectID
	end

	-- Relevant ONLY to: Artifact, Domain, FusionAttribute, Intersect, Taxonomy, Policy, Rule
	if @ActionObject = 'Intersect'
	begin
		select	@actionObjectTypeName = T.Name,
				@actionObjectName = O.Name 
		from	[Intersect] O
				inner join [IntersectType] T on T.ID = O.IntersectTypeID
		where	O.ID = @ActionObjectID

		insert into @tbl  select 0, 'Classification', Classification, 0, 0 from [Intersect] where ID = @ActionObjectID
		insert into @tbl  select 0, 'Description', Description, 0, 0 from [Intersect] where ID = @ActionObjectID
	end

	-- Relevant ONLY to: IntersectType
	if @ActionObject = 'IntersectType'
	begin
		select	@actionObjectTypeName = 'Intersect Type',
				@actionObjectName = O.Name 
		from	IntersectType O
		where	O.ID = @ActionObjectID

		--insert into @tbl  select 0, 'ReadOnly', [ReadOnly], 0, 0 from IntersectType where ID = @ActionObjectID
		--insert into @tbl  select 0, 'IsTechnical', IsTechnical, 0, 0 from IntersectType where ID = @ActionObjectID
		--insert into @tbl  select 0, 'AllowContext', AllowContext, 0, 0 from IntersectType where ID = @ActionObjectID
	end

	-- Relevant ONLY to: LoadType
	if @ActionObject = 'LoadType'
	begin
		select	@actionObjectTypeName = 'Load Type',
				@actionObjectName = O.Name 
		from	LoadType O
		where	O.ID = @ActionObjectID

		insert into @tbl  select 0, 'Name', Name, 0, 0 from LoadType where ID = @ActionObjectID
	end

	-- Relevant ONLY to: LoadType
	if @ActionObject = 'LoadTypeField'
	begin
		select	@actionObjectTypeName = T.Name,
				@actionObjectName = O.Name 
		from	LoadTypeField O
				inner join LoadType T on T.ID = O.LoadTypeID
		where	O.ID = @ActionObjectID

		insert into @tbl  select 0, 'Name', Name, 0, 0 from LoadTypeField where ID = @ActionObjectID
		insert into @tbl  select 0, 'SortOrder', SortOrder, 0, 0 from LoadTypeField where ID = @ActionObjectID
		insert into @tbl  select 0, 'LookupObjectType', LookupObjectType, 0, 0 from LoadTypeField where ID = @ActionObjectID
		insert into @tbl  select 0, 'LookupObjectID', LookupObjectID, 0, 0 from LoadTypeField where ID = @ActionObjectID
		insert into @tbl  select 0, 'LookupFieldName', LookupFieldName, 0, 0 from LoadTypeField where ID = @ActionObjectID
	end

	-- Relevant ONLY to: LoadType
	if @ActionObject = 'LoadTypeRule'
	begin
		select	@actionObjectTypeName = T.Name,
				@actionObjectName = T.Name + ' Rule ' + cast(O.ID as nvarchar(15))
		from	LoadTypeRule O
				inner join LoadType T on T.ID = O.LoadTypeID
		where	O.ID = @ActionObjectID

		insert into @tbl  select 0, 'LoadTypeRuleGroup', case LoadTypeRuleGroup when 1 then 'Promotion' when 2 then 'Relation' else 'Unknown' end, 0, 0 from LoadTypeRule where ID = @ActionObjectID
		insert into @tbl  select 0, 'SortOrder', SortOrder, 0, 0 from LoadTypeRule where ID = @ActionObjectID
		insert into @tbl  select 0, 'ObjectType', ObjectType, 0, 0 from LoadTypeRule where ID = @ActionObjectID
		insert into @tbl  select 0, 'ObjectID', ObjectID, 0, 0 from LoadTypeRule where ID = @ActionObjectID
		insert into @tbl  select 0, 'UniqueLoadTypeFieldID', UniqueLoadTypeFieldID, 0, 0 from LoadTypeRule where ID = @ActionObjectID
	end

	-- Relevant ONLY to: LoadType
	if @ActionObject = 'LoadTypeRuleItem'
	begin
		select	@actionObjectTypeName = T.Name,
				@actionObjectName = 'Rule Field ' + cast(O.ID as nvarchar(15))
		from	LoadTypeRuleItem O
				inner join LoadTypeRule R on R.ID = O.LoadTypeRuleID
				inner join LoadType T on T.ID = R.LoadTypeID
		where	O.ID = @ActionObjectID

		insert into @tbl  select 0, 'SourceLoadTypeFieldID', SourceLoadTypeFieldID, 0, 0 from LoadTypeRuleItem where ID = @ActionObjectID
		insert into @tbl  select 0, 'TargetFieldName', TargetFieldName, 0, 0 from LoadTypeRuleItem where ID = @ActionObjectID
		insert into @tbl  select 0, 'IsCustomField', IsCustomField, 0, 0 from LoadTypeRuleItem where ID = @ActionObjectID
	end

	-- Relevant ONLY to: LookupType
	if @ActionObject = 'Lookup'
	begin
		select	@actionObjectTypeName = T.Name,
				@actionObjectName = T.Name + ' Lookup ' + cast(O.ID as nvarchar(15))
		from	[Lookup] O
				inner join LookupType T on T.ID = O.LookupTypeID
		where	O.ID = @ActionObjectID
	end

	-- Relevant ONLY to: LookupType
	if @ActionObject = 'LookupType'
	begin
		select	@actionObjectTypeName = 'Lookup Type',
				@actionObjectName = O.Name 
		from	LookupType O
		where	O.ID = @ActionObjectID

		insert into @tbl  select 0, 'Name', Name, 0, 0 from LookupType where ID = @ActionObjectID
	end

	-- Relevant ONLY to: Policy
	if @ActionObject = 'Policy'
	begin
		select	@actionObjectTypeName = 'Policy',
				@actionObjectName = O.Name 
		from	[Policy] O
		where	O.ID = @ActionObjectID

		insert into @tbl  select 0, 'Name', Name, 0, 0 from [Policy] where ID = @ActionObjectID
		insert into @tbl  select 0, 'ParentID', ParentID, 0, 0 from [Policy] where ID = @ActionObjectID
		insert into @tbl  select 0, 'Description', Description, 0, 0 from [Policy] where ID = @ActionObjectID
	end

	-- Relevant ONLY to: SurveyType
	if @ActionObject = 'QuestionType'
	begin
		select	@actionObjectTypeName = T.Name,
				@actionObjectName = O.Name 
		from	QuestionType O
				inner join SurveyType T on T.ID = O.SurveyTypeID
		where	O.ID = @ActionObjectID

		insert into @tbl  select 0, 'Name', Name, 0, 0 from QuestionType where ID = @ActionObjectID
		insert into @tbl  select 0, 'DisplayStyle', DisplayStyle, 0, 0 from QuestionType where ID = @ActionObjectID
		insert into @tbl  select 0, 'Description', Description, 0, 0 from QuestionType where ID = @ActionObjectID
	end

	-- Relevant ONLY to: Report
	if @ActionObject = 'Report'
	begin
		select	@actionObjectTypeName = 'Report',
				@actionObjectName = O.Name
		from	Report O
		where	O.ID = @ActionObjectID

		insert into @tbl  select 0, 'Name', Name, 0, 0 from Report where ID = @ActionObjectID
		insert into @tbl  select 0, 'Description', Description, 0, 0 from Report where ID = @ActionObjectID
		insert into @tbl  select 0, 'ObjectType', ObjectType, 0, 0 from Report where ID = @ActionObjectID
		insert into @tbl  select 0, 'ObjectID', ObjectID, 0, 0 from Report where ID = @ActionObjectID
		insert into @tbl  select 0, 'ReportLayoutID', ReportLayoutID, 0, 0 from Report where ID = @ActionObjectID
	end

	-- Relevant ONLY to: Report
	if @ActionObject = 'ReportTile'
	begin
		select	@actionObjectTypeName = T.Name,
				@actionObjectName = O.Name 
		from	ReportTile O
				inner join Report T on T.ID = O.ReportID
		where	O.ID = @ActionObjectID

		insert into @tbl  select 0, 'Name', Name, 0, 0 from ReportTile where ID = @ActionObjectID
		insert into @tbl  select 0, 'ReportTileType', ReportTileType, 0, 0 from ReportTile where ID = @ActionObjectID
		insert into @tbl  select 0, 'ContentAreaNumber', ContentAreaNumber, 0, 0 from ReportTile where ID = @ActionObjectID
		insert into @tbl  select 0, 'CommandText', CommandText, 0, 0 from ReportTile where ID = @ActionObjectID
		insert into @tbl  select 0, 'Settings', cast(Settings as nvarchar(max)), 0, 0 from ReportTile where ID = @ActionObjectID
	end

	-- Relevant ONLY to: Artifact, ArtifactType, DomainType, Intersect, Policy, Rule, Taxonomy, TaxonomyType, Vocabulary
	if @ActionObject = 'Responsibility'
	begin
		select	@actionObjectTypeName = 'Responsibility',
				@actionObjectName = T.Name 
		from	Responsibility O
				inner join ResponsibilityType T on T.ID = O.ResponsibilityTypeID

		where	O.ID = @ActionObjectID

		insert into @tbl  select 0, 'Context', (
				select	D.Name + ': ' + I.Code + ' - ' + I.Name + '; '
				from	ResponsibilityContextItem C
						inner join DomainItem I on C.ObjectType = 'DomainItem' and C.ObjectID = I.ID
						inner join Domain D on D.ID = I.DomainID
				where	ResponsibilityID = @ActionObjectID
				for xml path ('')--, root('items')
				), 0, 0 from Responsibility where ID = @ActionObjectID
		insert into @tbl  select 0, 'ObjectType', ObjectType, 0, 0 from Responsibility where ID = @ActionObjectID
		insert into @tbl  select 0, 'ObjectID', ObjectID, 0, 0 from Responsibility where ID = @ActionObjectID
		insert into @tbl  select 0, 'ResponsibleObjectType', ResponsibleObjectType, 0, 0 from Responsibility where ID = @ActionObjectID
		insert into @tbl  select 0, 'ResponsibleObjectID', ResponsibleObjectID, 0, 0 from Responsibility where ID = @ActionObjectID
	end

	-- Relevant ONLY to: ResponsibilityType
	if @ActionObject = 'ResponsibilityType'
	begin
		select	@actionObjectTypeName = 'Responsibility Type',
				@actionObjectName = O.Name 
		from	ResponsibilityType O
		where	O.ID = @ActionObjectID

		insert into @tbl  select 0, 'Name', Name, 0, 0 from ResponsibilityType where ID = @ActionObjectID
		insert into @tbl  select 0, 'ResponsibilityTypeGroup', ResponsibilityTypeGroup, 0, 0 from ResponsibilityType where ID = @ActionObjectID
		insert into @tbl  select 0, 'Description', Description, 0, 0 from ResponsibilityType where ID = @ActionObjectID
	end

	-- Relevant ONLY to: Rule
	if @ActionObject = 'Rule'
	begin
		select	@actionObjectTypeName = 'Rule',
				@actionObjectName = O.Name 
		from	[Rule] O
		where	O.ID = @ActionObjectID

		insert into @tbl  select 0, 'Name', Name, 0, 0 from [Rule] where ID = @ActionObjectID
		insert into @tbl  select 0, 'Description', Description, 0, 0 from [Rule] where ID = @ActionObjectID
		insert into @tbl  select 0, 'RuleType', RuleType, 0, 0 from [Rule] where ID = @ActionObjectID
	end

	-- Relevant ONLY to: StatisticType
	if @ActionObject = 'StatisticType'
	begin
		select	@actionObjectTypeName = 'Statistic Type',
				@actionObjectName = O.Name 
		from	StatisticType O
		where	O.ID = @ActionObjectID

		insert into @tbl  select 0, 'Name', Name, 0, 0 from StatisticType where ID = @ActionObjectID
		insert into @tbl  select 0, 'CheckType', CheckType, 0, 0 from StatisticType where ID = @ActionObjectID
		insert into @tbl  select 0, 'Description', Description, 0, 0 from StatisticType where ID = @ActionObjectID
		insert into @tbl  select 0, 'PartOfScore', PartOfScore, 0, 0 from StatisticType where ID = @ActionObjectID
		insert into @tbl  select 0, 'Configuration', cast(Configuration as nvarchar(max)), 0, 0 from StatisticType where ID = @ActionObjectID
	end

	-- Relevant ONLY to: SurveyType
	if @ActionObject = 'SurveyType'
	begin
		select	@actionObjectTypeName = 'Survey Type',
				@actionObjectName = O.Name 
		from	SurveyType O
		where	O.ID = @ActionObjectID

		insert into @tbl  select 0, 'Name', Name, 0, 0 from SurveyType where ID = @ActionObjectID
		insert into @tbl  select 0, 'Object', Object, 0, 0 from SurveyType where ID = @ActionObjectID
		insert into @tbl  select 0, 'ObjectID', ObjectID, 0, 0 from SurveyType where ID = @ActionObjectID
	end

	-- Relevant ONLY to: Taxonomy, TaxonomyType
	if @ActionObject = 'Taxonomy'
	begin
		select	@actionObjectTypeName = T.Name + ' model',
				@actionObjectName = O.TextPath
		from	Taxonomy O
				inner join TaxonomyType T on T.ID = O.TaxonomyTypeID
		where	O.ID = @ActionObjectID

		insert into @tbl  select 0, 'Name', Name, 0, 0 from Taxonomy where ID = @ActionObjectID
		insert into @tbl  select 0, 'ParentID', ParentID, 0, 0 from Taxonomy where ID = @ActionObjectID
		insert into @tbl  select 0, 'Description', Description, 0, 0 from Taxonomy where ID = @ActionObjectID
		insert into @tbl  select 0, 'Level', [Level], 0, 0 from Taxonomy where ID = @ActionObjectID
	end

	-- Relevant ONLY to: TaxonomyType
	if @ActionObject = 'TaxonomyType'
	begin
		select	@actionObjectTypeName = 'Model Type',
				@actionObjectName = O.Name
		from	TaxonomyType O
		where	O.ID = @ActionObjectID

		insert into @tbl  select 0, 'Name', Name, 0, 0 from TaxonomyType where ID = @ActionObjectID
		insert into @tbl  select 0, 'Description', Description, 0, 0 from TaxonomyType where ID = @ActionObjectID
		insert into @tbl  select 0, 'MaximumDepth', MaximumDepth, 0, 0 from TaxonomyType where ID = @ActionObjectID
		--insert into @tbl  select 0, 'Class', Class, 0, 0 from TaxonomyType where ID = @ActionObjectID
	end

	-- Get the dynamic fields for the actional object, if available for this type.
	if @ActionObject in ('Artifact', 'Attribute', 'Event', 'Fusion', 'FusionAttribute', 'Lookup', 'Resource', 'Taxonomy') 
	begin
		insert into @tbl  
			select	FieldTypeID, 
					FriendlyName, 
					FormattedValue, 
					0, 
					0 
			from	FieldWithRelation
			where	ObjectType = @ActionObject 
					and ObjectID = @ActionObjectID
	end
	----------------------------------------------------------------------


	-- Now, determine the description, and whether to create audit row ---

	update	T
	set		T.MostRecentVersion = coalesce(S.[Version], 0),
			T.Updated = case 
							when T.NewValue = S.Value then 0
							when T.NewValue is null and S.Value is null then 0
							else 1
						end
	from	@tbl T
			left join (
						select	V.*,
								F.Value
						from	reporting.Global_Audit A
								inner join reporting.Global_FieldAudit F on F.AuditID = A.ID and A.[Object] = @Object and A.ObjectID = @ObjectID and A.ActionObject = @ActionObject and A.ActionObjectID = @ActionObjectID
								inner join (
											select		F.FieldTypeID,
														F.FieldName,
														max([Version]) as [Version]
											from		reporting.Global_Audit A
														inner join reporting.Global_FieldAudit F on F.AuditID = A.ID and A.[Object] = @Object and A.ObjectID = @ObjectID and A.ActionObject = @ActionObject and A.ActionObjectID = @ActionObjectID
											group by	F.FieldTypeID,
														F.FieldName
											) V on V.FieldTypeID = F.FieldTypeID and V.FieldName = F.FieldNAme and V.[Version] = F.[Version]
						) S on (S.FieldTypeID = 0 and S.FieldTypeID = T.FieldTypeID and S.FieldName = T.FieldName) or (S.FieldTypeID > 0 and S.FieldTypeID = T.FieldTypeID)

	declare	@auditID bigint,
			@current int = 1, 
			@max int,
			@fieldTypeID int,
			@fieldName nvarchar(250),
			@version int,
			@value nvarchar(max),
			@updated bit
	select	@max = max(ID) from @tbl

	if @Action = 'Created'
		begin
			set @actionDescription = @actionObjectTypeName + ' created.'
		end
	else
		begin
			while @current <= @max
			begin
				select	@fieldTypeID = FieldTypeID,
						@fieldName = FieldName,
						@version = MostRecentVersion,
						@value = NewValue,
						@updated = Updated
				from	@tbl
				where	ID = @current

				if @updated = 1
				begin
					set @actionDescription = coalesce(@actionDescription + ', ', '') + @fieldName + case when @version > 0 then ' updated' else ' added' end
				end

				set @current = @current + 1
			end
		end
	
	--select @Object, @ObjectID, @ObjectName, @ResourceID, @Date, @Action, @ActionObject, @ActionObjectID, @actionObjectTypeName, @actionObjectName, @actionDescription

	if @actionDescription is not null and @objectName is not null
	begin
		set @actionDescription = @actionDescription + '.'

		insert into [reporting].[Global_Audit] values (@Object, @ObjectID, @objectName, coalesce(@ResourceID, 0), @Date, @Action, @ActionObject, @ActionObjectID, @actionObjectTypeName, @actionObjectName, @actionDescription)
		select @auditID = SCOPE_IDENTITY()

		set @current = 1
		while @current <= @max
		begin
			select	@fieldTypeID = FieldTypeID,
					@fieldName = FieldName,
					@version = MostRecentVersion,
					@value = NewValue,
					@updated = Updated
			from	@tbl
			where	ID = @current

			if @updated = 1
			begin
				insert into [reporting].[Global_FieldAudit] 
				values	(
						@auditID, 
						@fieldTypeID, 
						@fieldName, 
						@version + 1, 
						@value --case FormattedValue when '' then 'EMPTY' else coalesce(FormattedValue, 'NULL') end
						) 		
			end

			set @current = @current + 1
		end
	end
	----------------------------------------------------------------------
end
GO

ALTER PROCEDURE [dbo].[GetScoreHistoryByObject]
	@type varchar(50),
	@id int
AS
begin
	declare @Points int = 40,
			@DateStart datetime, @DateEnd datetime, @DateCurrent datetime,
			@Increment int, @CurrentPoints int, @MaxPoints int, @current int,
			@oType varchar(25), @oTypeID int, @score float

	declare @dates table ([Date] datetime, Score float)

	set @DateEnd = DATEADD(dd, 0, DATEDIFF(dd, 0, GETUTCDATE()))
	select	@DateStart = coalesce(min(Date), DATEADD(d, -30, @DateEnd)) 
	from	reporting.Global_Audit
	where	Object = @type 
			and ObjectID = @id
	
	select @Increment = DATEDIFF(hh, @DateStart, @DateEnd) / @Points
	insert into @dates values (@DateEnd, dbo.GetObjectStatisticScore(@type, @id)*100)

	select	@oType = Type,
			@oTypeID = TypeID
	from	utility.ObjectDetail(@type, @id)

	select	@MaxPoints = SUM(Score)
	from	StatisticType
	where	[Object] = @oType
			and ObjectID = @oTypeID
			and PartOfScore = 1

	set @current = 1
	while @current <= @Points
	begin
		set @DateCurrent = DATEADD(hh, -(@current * @Increment), @DateEnd)


		select	@CurrentPoints = SUM(S.Score)
		from	Statistic S
				inner join StatisticType T on S.StatisticTypeID = T.ID and T.PartOfScore = 1
				inner join	(
							select		StatisticTypeID,
										Max(DateStart) D
							from		Statistic S
										inner join StatisticType T on S.StatisticTypeID = T.ID and T.PartOfScore = 1
							where		S.ObjectType = @type
										and S.ObjectID = @id
										and @DateCurrent between S.DateStart and S.DateEnd
							group by	StatisticTypeID
							) M on M.StatisticTypeID = S.StatisticTypeID and M.D = S.DateStart
		where	S.ObjectType = @type
				and S.ObjectID = @id

		select	@score = round(cast(@CurrentPoints as float) / cast(@MaxPoints as float), 2)
		insert into @dates values (@DateCurrent, @score*100)
		set @current = @current + 1
	end
	select * from  @dates order by [Date]
end
GO

ALTER procedure [dbo].[AddRelationships]
--declare
	@ResourceID int,
	@Date datetime,
	@Type varchar(50),				-- The start object type.
	@ID int,						-- The start object ID.
	@Classification int,
	@IntersectRole int,
	@Description nvarchar(4000),
	@Objects ObjectsTable READONLY
	
--set @ResourceID = 1
--set @Date = getutcdate()
--set @Type = 'Artifact'
--set @ID = 3
--set @Classification = 1
--set @IntersectRole = NULL
--set @Description = ''
--insert into @Objects VALUES ('Artifact', 2)
as
begin
	set nocount on;

	declare @current int,
			@max int
	
	declare @Relations table (
		ID int identity, 
			
		Subject varchar(50), SubjectID int, SubjectType varchar(50), SubjectTypeID int, 
		Object varchar(50), ObjectID int, ObjectType varchar(50), ObjectTypeID int, 

		IntersectTypeID int, IntersectID int, [Action] varchar(1),
		
		IsReversed bit
	)

	insert into @Relations
		select	distinct 
				SD.Object, SD.ObjectID, SD.ObjectType, SD.ObjectTypeID, 
				OD.Object, OD.ObjectID, OD.ObjectType, OD.ObjectTypeID, 
				RT.ID, R.ID, CASE WHEN R.ID IS NULL THEN 'C' ELSE 'U' END,
				case
					when (RT.Subject = SD.ObjectType and RT.SubjectID = SD.ObjectTypeID and RT.Object = OD.ObjectType and RT.ObjectID = OD.ObjectTypeID) then cast(1 as bit)
					else cast(0 as bit)
				end
		from	@Objects O
				inner join cache.Object SD on SD.[Object] = @Type and SD.ObjectID = @ID
				inner join cache.Object OD on OD.[Object] = O.ObjectType and OD.ObjectID = O.ObjectID
				inner join [IntersectType] RT on	(
													(RT.Subject = SD.ObjectType and RT.SubjectID = SD.ObjectTypeID and RT.Object = OD.ObjectType and RT.ObjectID = OD.ObjectTypeID) OR
													(RT.Object = SD.ObjectType and RT.ObjectID = SD.ObjectTypeID and RT.Subject = OD.ObjectType and RT.SubjectID = OD.ObjectTypeID)
													)
				left join [Intersect] R on	R.IntersectTypeID = RT.ID and 
											(
												(R.Subject = SD.Object and R.SubjectID = SD.ObjectID and R.Object = OD.Object and R.ObjectID = OD.ObjectID) OR
												(R.Object = SD.Object and R.ObjectID = SD.ObjectID and R.Subject = OD.Object and R.SubjectID = OD.ObjectID)
											)

	set @current = 1
	select @max = MAX(ID) from @Relations
	while @current <= @max
	begin
		declare @Subject varchar(50),		@SubjectID int, 
				@Object varchar(50),		@ObjectID int,	
				@Action varchar(1),			@IsReversed bit,
				@IntersectTypeID int,		@IntersectID int,
				@s varchar(50),				@o varchar(50),
				@sid int,					@oid int
		
		set		@IntersectID = null	--reset here

		select	@Subject = Subject,
				@SubjectID = SubjectID,

				@Object = Object,
				@ObjectID = ObjectID,	

				@IntersectTypeID = IntersectTypeID, 
				@IntersectID = IntersectID, 
				@Action = [Action],
				@IsReversed = IsReversed
		from	@Relations
		where	ID = @current

		if @IntersectID is null
		begin
			-- Relationship does not yet exist, so CREATE.
			if @IntersectID is null
				begin
					if @IsReversed = 1
					begin
						set @s = @Object
						set @sid = @ObjectID
						set @o = @Subject
						set @oid = @SubjectID 
					end
					else
					begin
						set @o = @Object
						set @oid = @ObjectID
						set @s = @Subject
						set @sid = @SubjectID
					end

					INSERT INTO [Intersect] (
						IntersectTypeID, Classification, [Description],
						[Subject], SubjectID, [Object], ObjectID,
						CreatedBy, CreatedOn, UpdatedBy, UpdatedOn		
					) 
					VALUES (
						@IntersectTypeID,  @Classification,  @Description,
						@s, @sid, @o, @oid,
						@ResourceID, @Date, @ResourceID, @Date
					)

					SELECT @IntersectID = SCOPE_IDENTITY()

					insert into cache.[Object] ( [Object], [ObjectID], [ObjectType], [ObjectTypeID] )
					values	( 'Intersect', @IntersectID, 'IntersectType', @IntersectTypeID );

					insert into cache.Relationship ( IntersectID, SourceObject, SourceObjectID, TargetObject, TargetObjectID )
					values	( @IntersectID, @s, @sid, @o, @oid );
					insert into cache.Relationship ( IntersectID, SourceObject, SourceObjectID, TargetObject, TargetObjectID )
					values	( @IntersectID, @o, @oid, @s, @sid );

					--Update the responsibilities of the object that should inherit form the other (Taxonomy can push relationships down to artifact)
					if ( (@s = 'Taxonomy' and @o = 'Artifact') OR (@s = 'Artifact' and @o = 'Taxonomy') )
					begin
						if @s = 'Artifact'
						begin
							exec [cache].[SynchronizeResponsibilitiesForObject] @s, @sid
						end
						if @o = 'Artifact'
						begin
							exec [cache].[SynchronizeResponsibilitiesForObject] @o, @oid
						end
					end

					--exec utility.AddAuditEntry @StartObject, @StartObjectID, @ResourceID, @Date, 'Created', 'Intersect', @IntersectID
					--exec utility.AddAuditEntry @EndObject, @EndObjectID, @ResourceID, @Date, 'Created', 'Intersect', @IntersectID
				end
			else
				begin
					-- Update the Classification and Description only if the relationship already exists.
					if @IntersectID is not null
					begin
						update	[Intersect]
						set		Classification = @Classification,
								Description = @Description
						where	ID = @IntersectID

						--exec utility.AddAuditEntry 'Intersect', @IntersectID, @ResourceID, @Date, 'Updated', 'Intersect', @IntersectID
					end
				end
		end

		set @current = @current + 1
	end
end
GO


ALTER PROCEDURE [dbo].[GetCommentDetailByID]
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
GO

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
GO

ALTER PROCEDURE [dbo].[GetCommentDetailsByType]
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
							CRD.IconBackColor,
							CRD.NgUrl
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

/*
select * from Comment where ownerobjectid = 971882 


select	*, 'Comments', '/overlays/Artifact/' + cast(971882 as varchar(10)) + '/comments'
		from	Comment C
				inner join CommentRelation R	on R.CommentID = C.ID and C.ParentID is null
												and R.ObjectType = 'Artifact' and R.ObjectID = 971882
                                                and C.ParentID is null
												and C.IsDeleted = 0


exec [GetCommentDetailsByType] 'Artifact',971882,0,5*/
GO

ALTER procedure [dbo].[AddSingleIntersect]
	@ResourceID int,
	@IntersectTypeID int,
	@Subject varchar(50),			-- The start object type.
	@SubjectID int,					-- The start object ID.
	@Object varchar(50),			-- The end object type.
	@ObjectID int,					-- The end object ID.	
	@Classification int,
	@Description nvarchar(4000)
as
begin
	set nocount on;

	declare @Date datetime = getutcdate(),
			@ErrorMessage nvarchar(2500),
			@IntersectID int,
			@SubjectIntersectTypeNodeID int,
			@SubjectIntersectNodeID int,
			@ObjectIntersectTypeNodeID int,
			@ObjectIntersectNodeID int

	select	@IntersectID = I.ID,
			@SubjectIntersectTypeNodeID = N1.IntersectTypeNodeID,	@SubjectIntersectNodeID = N1.ID,
			@ObjectIntersectTypeNodeID = N2.IntersectTypeNodeID,	@ObjectIntersectNodeID = N2.ID
	from	[Intersect] I
			inner join IntersectNode N1 on N1.IntersectID = I.ID and N1.ObjectType = @Subject and N1.ObjectID = @SubjectID
			inner join IntersectNode N2 on N2.IntersectID = I.ID and N2.ObjectType = @Object and N2.ObjectID = @ObjectID

	if @IntersectID is not null and @IntersectID > 0
		begin
			-- Update

			update	[Intersect]
			set		Classification = @Classification,
					Description = @Description
			where	ID = @IntersectID

			--exec utility.AddAuditEntry 'Intersect', @IntersectID, @ResourceID, @Date, 'Updated', 'Intersect', @IntersectID
		end
	else
		begin
			-- Create

			declare @SubjectType varchar(50),
					@SubjectTypeID int,
					@ObjectType varchar(50),
					@ObjectTypeID int

			select	@SubjectType = ObjectType, @SubjectTypeID = ObjectTypeID	from cache.[Object] where [Object] = @Subject and ObjectID = @SubjectID 
			select	@ObjectType = ObjectType, @ObjectTypeID = ObjectTypeID		from cache.[Object] where [Object] = @Object and ObjectID = @ObjectID 

			select	distinct 
					@SubjectIntersectTypeNodeID = SourceIntersectTypeNodeID, 
					@ObjectIntersectTypeNodeID = TargetIntersectTypeNodeID
			from	utility.RelationshipTypes R 
			where	SourceObjectType = @SubjectType and SourceObjectID = @SubjectTypeID 
					and TargetObjectType = @ObjectType and TargetObjectID = @ObjectTypeID

			if @SubjectIntersectTypeNodeID is not null and @ObjectIntersectTypeNodeID is not null
				begin
					INSERT INTO [Intersect] (
						IntersectTypeID, 
						Classification, 
						[Description],
						[Subject], SubjectID,
						[Object], ObjectID,
						CreatedBy, CreatedOn,
						UpdatedBy, UpdatedOn				
					) 
					VALUES (
						@IntersectTypeID, 
						@Classification, 
						@Description,
						@Subject, @SubjectID,
						@Object, @ObjectID,
						@ResourceID, @Date,
						@ResourceID, @Date
					)

					SELECT @IntersectID = SCOPE_IDENTITY()

					insert into cache.[Object] ( [Object], [ObjectID], [ObjectType], [ObjectTypeID] )
					values	( 'Intersect', @IntersectID, 'IntersectType', @IntersectTypeID );

					insert into cache.Relationship ( IntersectID, SourceIntersectTypeNodeID, SourceIntersectNodeID, SourceObject, SourceObjectID, TargetIntersectTypeNodeID, TargetIntersectNodeID, TargetObject, TargetObjectID )
					values	( @IntersectID, 0, 0, @Subject, @SubjectID, 0, 0, @Object, @ObjectID );
					insert into cache.Relationship ( IntersectID, SourceIntersectTypeNodeID, SourceIntersectNodeID, SourceObject, SourceObjectID, TargetIntersectTypeNodeID, TargetIntersectNodeID, TargetObject, TargetObjectID )
					values	( @IntersectID, 0, 0, @Object, @ObjectID, 0, 0, @Subject, @SubjectID );

					--Update the responsibilities of the object that should inherit form the other (Taxonomy can push relationships down to artifact)
					if ( (@Subject = 'Taxonomy' and @Object = 'Artifact') OR (@Subject = 'Artifact' and @Object = 'Taxonomy') )
						begin
							if @Subject = 'Artifact'
							begin
								exec [cache].[SynchronizeResponsibilitiesForObject] @Subject, @SubjectID
							end
							if @Object = 'Artifact'
							begin
								exec [cache].[SynchronizeResponsibilitiesForObject] @Object, @ObjectID
							end
						end

					--exec utility.AddAuditEntry @Subject, @SubjectID, @ResourceID, @Date, 'Created', 'Intersect', @IntersectID
					--exec utility.AddAuditEntry @Object, @ObjectID, @ResourceID, @Date, 'Created', 'Intersect', @IntersectID
				end
		end

	select * from [Intersect] where ID = @IntersectID
end
GO

ALTER PROCEDURE [dbo].[GetEventsByObject] --'Policy', 1-- 'Rule', 7
	@Type varchar(250),
	@ID int,
	@Status varchar(25) = NULL
AS
BEGIN
	if @Type = 'Policy'
		begin
			with PH as	(
						select	ID,
								ParentID
						from	Policy
						where	ID = @ID
						union all
						select	C.ID,
								C.ParentID
						from	Policy C 
								inner join PH on C.ParentID = PH.ID
						)

			SELECT	E.ID AS EventID,
					G.RuleID,
					R.Name as [Rule],
					G.Name as EventName,
					E.EventGroupID,
					E.SourceID,
					E.Status,
					E.Date
			FROM	[Event] E
					INNER JOIN EventGroup G ON E.EventGroupID = G.ID 
											AND (E.Status = @Status OR 1=1)
					INNER JOIN [Rule] R on R.ID = G.RuleID
			where	R.ID in (
							select	distinct
									CR.TargetObjectID
							from	PH
									inner join cache.Relationships CR on CR.SourceObject = 'Policy' and CR.SourceObjectID = PH.ID and CR.TargetObject = 'Rule'
							)
		end

	if @Type = 'Rule'
		begin
			SELECT	E.ID AS EventID,
					G.RuleID,
					R.Name as [Rule],
					G.Name as EventName,
					E.EventGroupID,
					E.SourceID,
					E.Status,
					E.Date
			FROM	[Event] E
					INNER JOIN EventGroup G ON E.EventGroupID = G.ID 
											AND (E.Status = @Status OR 1=1)
					INNER JOIN [Rule] R on R.ID = G.RuleID and R.ID = @ID
		end

	if @Type = 'EventGroup'
		begin
			SELECT	E.ID AS EventID,
					G.RuleID,
					R.Name as [Rule],
					G.Name as EventName,
					E.EventGroupID,
					E.SourceID,
					E.Status,
					E.Date
			FROM	[Event] E
					INNER JOIN EventGroup G ON E.EventGroupID = G.ID 
											AND E.EventGroupID = @ID
											AND (E.Status = @Status OR 1=1)
					INNER JOIN [Rule] R on R.ID = G.RuleID
		end

	if @Type <> 'EventGroup' and @Type <> 'Policy' 
		begin
			
			SELECT	E.ID AS EventID,
					G.RuleID,
					R.Name as [Rule],
					G.Name as EventName,
					E.EventGroupID,
					E.SourceID,
					E.Status,
					E.Date
			FROM	[Event] E
					INNER JOIN EventGroup G ON E.EventGroupID = G.ID 
											and (E.Status = @Status OR 1=1)
					INNER JOIN [Rule] R on R.ID = G.RuleID
					inner join [Intersect] CR on	(
													(CR.Subject = @Type and CR.SubjectID = @ID and CR.Object = 'Rule' and CR.ObjectID = R.ID) OR 
													(CR.Object = @Type and CR.ObjectID = @ID and CR.Subject = 'Rule' and CR.SubjectID = R.ID)
													)
		end
END
GO

ALTER procedure [dbo].[GetTechnicalRelationshipsByIntersect]
	@IntersectID int
as
begin
	select	distinct 
			I.Object as [Type],
			FT.Name as Attribute,
			coalesce(F.Name, '') Fusion,
			coalesce(FA.TextPath, FA.Name) as Name,
			'#/fusion/' + CAST(FT.FusionTypeID as varchar(15)) + '/' + + CAST(FA.FusionID as varchar(15)) as URL
	from	[Intersect] I
			inner join FusionAttribute FA on I.Subject = 'Intersect' and I.Object = 'FusionAttribute' and I.SubjectID = @IntersectID and FA.ID = I.ObjectID
			inner join Fusion F on F.ID = FA.FusionID
			inner join FusionAttributeType FT on FT.ID = FA.FusionAttributeTypeID;
end
GO

ALTER PROCEDURE [fusion].[FindEagleToDBRelationships]
as
begin
	set NOCOUNT, ANSI_PADDING ON;
	SET ANSI_WARNINGS ON;

	Declare @RelationshipList Table(StartID int,EndID Int);
	Declare @StartID int;
	Declare @EndID int;

	-- Eagle Inventory of Table to SQL Server DB Table
	insert into [fusion].[StagingRelationUnresolved]
		select	f.SOURCEID as 'StartID',
				f2.SOURCEID as 'EndID',
				CURRENT_TIMESTAMP
		from	fusionattribute f
				inner join fusionattribute f2 on ( f.name = f2.name and f.fusionattributetypeid = 2 and f2.fusionattributetypeid = 204)
				inner join fusionattribute fparent on (f.parentid = fparent.id)
				inner join fusionattribute f2parent on (f2.parentid = f2parent.id)
		where	f2parent.sourceid + '.DBO' = fparent.sourceid and 
				not exists	(
							select	1
							from	[Intersect] I
									inner join FusionAttribute sfa on I.Subject = 'FusionAttribute' and I.SubjectID = sfa.ID and sfa.FusionAttributeTypeID = 2 and sfa.SourceID = f.SourceID
									inner join FusionAttribute sfa2 on I.Object = 'FusionAttribute' and I.ObjectID = sfa2.ID and sfa2.FusionAttributeTypeID = 204 and sfa2.SourceID = F2.SourceID
							) and 
				not exists	(
							select	1
							from	fusion.stagingrelationunresolved sru
							where	sru.startid = f.sourceid and sru.endid = f2.sourceid
							)

	-- Eagle Field Attribute to SQL Server DB Column field attribute type = 201, sql server column type = 3
	insert into [fusion].[StagingRelationUnresolved]
		select	fa.sourceid as 'StartID',
				faSQLCol.sourceid as 'EndID',
				CURRENT_TIMESTAMP
		from	[Intersect] i
				inner join fusionattribute fa on I.Subject = 'FusionAttribute' and I.SubjectID = fa.ID
				inner join fusionattribute fa2 on I.Object = 'FusionAttribute' and I.ObjectID = fa2.ID -- the inventory of field
				inner join fusionattribute faTbl on (fa2.parentid = faTbl.id) -- the table
				inner join fusionattribute faDB on (faTbl.parentid = faDB.id) -- the db
				inner join fusionattribute faSQLCol on (faSQLCol.Name = fa2.Name and faSQLCol.fusionattributetypeid = 3)
				inner join fusionattribute faSQLTbl on (faSQLCol.ParentID = faSQLTbl.ID and faSQLTbl.Name = faTbl.Name)
				inner join fusionattribute faSQLSchema on (faSQLTbl.ParentID = faSQLSchema.ID and faSQLSchema.SourceID  = faDB.sourceid +'.DBO' )--and faSQLDb.Name = faDB.Name)	
		where	fa.fusionattributetypeid = 201	and 
				fa2.fusionattributetypeid = 205 and 
				not exists	(
							select	1
							from	[Intersect] I
									inner join FusionAttribute sfa on I.Subject = 'FusionAttribute' and I.SubjectID = sfa.ID and sfa.FusionAttributeTypeID = 201 and sfa.SourceID = fa.SourceID
									inner join FusionAttribute sfa2 on I.Object = 'FusionAttribute' and I.ObjectID = sfa2.ID and sfa2.FusionAttributeTypeID = 3 and sfa2.SourceID = faSQLCol.SourceID
							) and 
				not exists	(
							select	1 
							from	fusion.stagingrelationunresolved sru
							where	sru.startid = fa.sourceid and sru.endid = faSQLCol.sourceid
							)

end
GO

ALTER procedure [dbo].[GetTechnicalRelationshipsByObject]
	@ResponsibleObjectType varchar(50),
	@ResponsibleObjectID int,
	@ObjectType varchar(50),
	@ObjectID int
as
begin
	declare @IntersectID int;

	select	@IntersectID = ID
	from	[Intersect]
	where	(Subject = @ResponsibleObjectType and SubjectID = @ResponsibleObjectID and Object = @ObjectType and ObjectID = @ObjectID) OR
			(Object = @ResponsibleObjectType and ObjectID = @ResponsibleObjectID and Subject = @ObjectType and SubjectID = @ObjectID);

	EXEC GetTechnicalRelationshipsByIntersect @IntersectID;
end
GO

ALTER procedure [dbo].[AddRelationshipTypesBulk]
	@unresolvedrelations RelationshipTypeTable readonly
as
begin
	set nocount on;

	if exists(select 1 from @unresolvedrelations)
	begin
			
			-- Relationship does not yet exist, so CREATE.
			Declare @UnResIDList Table(IntersectTypeID int,UnresID Int);
			
			MERGE
				INTO    [IntersectType] d
				USING   (
						SELECT distinct ur.startpromotedobjecttype, ur.startpromotedobjecttypeid, ur.endpromotedobjecttype, ur.endpromotedobjecttypeid ,ur.ID as srID
							FROM @unresolvedrelations ur							
						) s
				ON      (1 = 0)
				WHEN NOT MATCHED THEN
				INSERT  (UpdatedOn, UpdatedBy)
				VALUES  (getutcdate(),0)
				OUTPUT  INSERTED.ID, s.srID into @UnResIDList;
				
	end

end
GO

ALTER procedure [cache].[SynchronizeRelationships]
	@Intersects IDTable READONLY
as
begin
	declare @count int
	select @count = count(1) from @Intersects

	if @count = 0
	begin
		--REFRESH ENTIRE TABLE
		merge cache.Relationship as T
		using (
				select	distinct
						ID as IntersectID,
						Subject,
						SubjectID,
						Object,
						ObjectID
				from	[Intersect]
				union
				select	distinct
						ID as IntersectID,
						Object as Subject,
						ObjectID as SubjectID,
						Subject as Object,
						SubjectID as ObjectID
				from	[Intersect]
			  ) as S
		on    (T.IntersectID = S.IntersectID and T.SourceObject = S.Subject and T.SourceObjectID = S.SubjectID)
		when not matched then
			insert (IntersectID, SourceObject, SourceObjectID, TargetObject, TargetObjectID)
			values (S.IntersectID, S.Subject, S.SubjectID, S.Object, S.ObjectID);
	end
	else
	begin
		--REFRESH SINGLE INTERSECT ENTRIES (2)
		merge cache.Relationship as T
		using (
				select	distinct
						I.ID as IntersectID,
						I.Subject,
						I.SubjectID,
						I.Object,
						I.ObjectID
				from	[Intersect] I
						inner join @Intersects C on C.ObjectID = I.ID
				union
				select	distinct
						I.ID as IntersectID,
						I.Object as Subject,
						I.ObjectID as SubjectID,
						I.Subject as Object,
						I.SubjectID as ObjectID
				from	[Intersect] I
						inner join @Intersects C on C.ObjectID = I.ID
			  ) as S
		on    (T.IntersectID = S.IntersectID and T.SourceObject = S.Subject and T.SourceObjectID = S.SubjectID)
		when not matched then
			insert (IntersectID, SourceObject, SourceObjectID, TargetObject, TargetObjectID)
			values (S.IntersectID, S.Subject, S.SubjectID, S.Object, S.ObjectID);
	end
end
GO

CREATE procedure [bulkload].[MergeDynamicLookupFields]
--declare
	@id int,
	@startColumnIndex int,
	@endColumnIndex int
--set @id = 252
--set @startColumnIndex = 5
--set @endColumnIndex = 7
as
begin
	set nocount on;
	-- Load custom fields for the inserted/updated objects.
	merge	Field T
	using	(
			select	distinct
					FT.ID as FieldTypeID,
					I.[Object],
					I.ObjectID,
					IC.LookupObjectID
			from	LoadItem I
					inner join [Load] L on L.ID = I.LoadID and I.LoadID = @id and I.ObjectID is not null
					inner join LoadColumn C on C.LoadID = I.LoadID and C.ColumnIndex between @startColumnIndex and @endColumnIndex
					inner join LoadItemColumn IC on IC.LoadID = C.LoadID and I.RowIndex = IC.RowIndex and IC.ColumnIndex = C.ColumnIndex and IC.LookupObjectID is not null
					inner join FieldType FT on FT.[Object] = L.Object and FT.ObjectID = L.ObjectID and FT.Name = C.Name
			) S
	on		(T.FieldTypeID = S.FieldTypeID and T.ObjectType = S.[Object] and T.ObjectID = S.ObjectID)
	when	matched then
			update	set Value = S.LookupObjectID
	when	not matched then
			insert (ObjectType, ObjectID, FieldTypeID, Value)
			values (S.[Object], S.ObjectID, S.FieldTypeID, S.LookupObjectID);

	merge	Field T
	using	(
			select	distinct
					FT.ID as FieldTypeID,
					I.[Object],
					I.ObjectID,
					case 
						when FT.[Type] = 'Boolean' and LOWER(IC.Value) in ('y', 'yes', 'true', 't', '1') then 'true'
						when FT.[Type] = 'Boolean' and LOWER(IC.Value) not in ('y', 'yes', 'true', 't', '1') then 'false'
						else IC.Value
					end as Value
			from	LoadItem I
					inner join [Load] L on L.ID = I.LoadID and I.LoadID = @id and I.ObjectID is not null
					inner join LoadColumn C on C.LoadID = I.LoadID and C.ColumnIndex between @startColumnIndex and @endColumnIndex
					inner join LoadItemColumn IC on IC.LoadID = C.LoadID and I.RowIndex = IC.RowIndex and IC.ColumnIndex = C.ColumnIndex and IC.LookupObjectID is null
					inner join FieldType FT on FT.[Object] = L.Object and FT.ObjectID = L.ObjectID and FT.Name = C.Name and FT.[Type] <> 'Lookup'
			) S
	on		(T.FieldTypeID = S.FieldTypeID and T.ObjectType = S.[Object] and T.ObjectID = S.ObjectID)
	when	matched then
			update	set Value = S.Value
	when	not matched then
			insert (ObjectType, ObjectID, FieldTypeID, Value)
			values (S.[Object], S.ObjectID, S.FieldTypeID, S.Value);
end
GO

create procedure [bulkload].[UpdateDynamicLookupFieldColumns]
	@id int,
	@startColumnIndex int,
	@endColumnIndex int
as
begin
	set nocount on;
	update	T
	set		T.LookupObject = S.LookupObject,
			T.LookupObjectID = S.LookupObjectID
	from	LoadItemColumn T
			inner join	(
						select	IC.LoadID,
								IC.RowIndex,
								IC.ColumnIndex,
								case 
									when L_A.ID is not null then 'Artifact'
									when L_D.ID is not null then 'Domain'
									when L_DI.ID is not null then 'DomainItem'
									when L_F.ID is not null then 'FusionAttribute'
									when L_I.ID is not null then 'Intersect'
									when L_L.Value is not null then 'Lookup'
									when L_T.ID is not null then 'Taxonomy'
									else NULL
								end as LookupObject,
								coalesce(L_A.ID, L_D.ID, L_DI.ID, L_F.ID, L_I.ID, L_L.Value, L_T.ID) as LookupObjectID
						from	FieldType F
								inner join [Load] L on L.ID = @id and L.[Object] = F.[Object] and L.ObjectID = F.ObjectID and F.[Type] = 'Lookup'
								inner join [LoadColumn] C on C.LoadID = L.ID and F.Name = C.Name
								inner join [LoadItemColumn] IC on IC.LoadID = C.LoadID and IC.ColumnIndex = C.ColumnIndex
								
								left join Artifact L_A on F.LookupObjectType in ('Artifact', 'ArtifactType') and L_A.ArtifactTypeID = F.LookupObjectID and (L_A.[Name] = IC.Value OR L_A.TextPath = IC.Value)
								left join Domain L_D on F.LookupObjectType in ('Domain', 'DomainType') and L_D.DomainTypeID = F.LookupObjectID and L_D.[Name] = IC.Value
								left join DomainItem L_DI on F.LookupObjectType = 'DomainItem' and L_DI.DomainID = F.LookupObjectID and L_DI.[Name] = IC.Value
								left join FusionAttribute L_F on F.LookupObjectType = 'FusionAttributeType' and L_F.FusionAttributeTypeID = F.LookupObjectID and (L_F.[Name] = IC.Value OR L_F.TextPath = IC.Value)
								left join [Intersect] L_I on F.LookupObjectType = 'IntersectType' and L_I.IntersectTypeID = F.LookupObjectID and L_I.[Name] = IC.Value
								left join [FieldLookupValue] L_L on F.ID = L_L.FieldTypeID and F.LookupObjectType = 'Lookup' and L_L.LookupObjectID = F.LookupObjectID and L_L.[Text] = IC.Value
								left join Taxonomy L_T on F.LookupObjectType in ('Taxonomy', 'TaxonomyType') and L_T.TaxonomyTypeID = F.LookupObjectID and (L_T.[Name] = IC.Value OR L_T.TextPath = IC.Value)
						where	F.[Type] = 'Lookup'
								and C.ColumnIndex between @startColumnIndex and @endColumnIndex
						) S on S.LoadID = T.LoadID and S.RowIndex = T.RowIndex and S.ColumnIndex = T.ColumnIndex
end
GO

create procedure [bulkload].[UpdateItemColumnByType]
	@id int,
	@ObjectType varchar(50), 
	@ObjectTypeID int,
	@subjectAreaColumn int, 
	@itemColumn int
as
begin
	set nocount on;
	update	T
	set		T.LookupObject = replace(@ObjectType, 'Type', ''),
			T.LookupObjectID = coalesce(A.ID, D.ID, P.ID, R.ID, TA.ID)
	from	LoadItemColumn T
			left join LoadItemColumn TS on TS.LoadID = T.LoadID and TS.RowIndex = T.RowIndex and TS.ColumnIndex = @subjectAreaColumn and T.ColumnIndex = @itemColumn
			left join Artifact A on lower(A.TextPath) = lower(T.Value) and A.TaxonomyTypeID = TS.LookupObjectID and A.ArtifactTypeID = @ObjectTypeID and @ObjectType = 'ArtifactType'
			left join Domain D on lower(D.Name) = lower(T.Value) and D.DomainTypeID = @ObjectTypeID and @ObjectType = 'DomainType'
			left join [Policy] P on lower(P.TextPath) = lower(T.Value) and P.PolicyTypeID = @ObjectTypeID and @ObjectType = 'PolicyType'
			left join [Rule] R on lower(R.Name) = lower(T.Value) and R.RuleType = @ObjectTypeID and @ObjectType = 'RuleType'
			left join [Taxonomy] TA on lower(TA.TextPath) = lower(T.Value) and TA.TaxonomyTypeID = @ObjectTypeID and @ObjectType = 'TaxonomyType'
	where	coalesce(A.ID, D.ID, P.ID, R.ID, TA.ID) is not null
end
GO


ALTER PROCEDURE [fusion].[Rules] 
AS
BEGIN
	SET NOCOUNT ON;

	declare @d datetime = getutcdate(),
			@r int = 0,
			@RuleID int,
			@FusionID int,
			@FusionAttributeID int,
			@ExecutionID int,
			@NumberOfRules int,			
			@NumberOfNewTaxonomies int,
			@NumberOfNewDomainItems int,
			@NumberOfNewDomains int,
			@NumberOfNewArtifacts int,
			@NumberOfAttributesTotal int,
			@NumberOfNewRelations int,
			@promotionNeedsToRun bit
	
	set	@NumberOfRules = 0;	
	set @NumberOfNewTaxonomies = 0;
	set @NumberOfNewDomainItems = 0;
	set @NumberOfNewDomains = 0;
	set @NumberOfNewArtifacts = 0;
	set @promotionNeedsToRun = 1;

	--First check if there is anything to do
	EXEC @promotionNeedsToRun = [utility].[ShouldPromotionRun]

	if(@promotionNeedsToRun <= 0)
	BEGIN
		PRINT 'NO REASON TO RUN THE PROMOTION RULES WAS DETECTED';
		return;
	END;

	--Log this run get a new id from the fusion.promotion table
	insert into [fusion].[RuleLog] ( DateStarted ) values ( CURRENT_TIMESTAMP)
	select @ExecutionID =  SCOPE_IDENTITY()

	IF OBJECT_ID('tempdb..#rules') IS NOT NULL
		DROP TABLE #rules;

	create table #rules (
		ID int identity,
		RuleID int,
		FusionID int,
		ObjectType varchar(25),
		ObjectID int,
		FilterFusionAttributeID int,
		FilterFusionAttributeTypeID int
	);

	IF OBJECT_ID('tempdb..#attributes') IS NOT NULL
		DROP TABLE #attributes;

	create table #attributes (
		ID int identity,
		RuleID int,
		RuleStepID int,
		[Action] varchar(25),
		FusionAttributeID int
	);

	IF OBJECT_ID('tempdb..#fields') IS NOT NULL
		DROP TABLE #fields;

	create table #fields (
		ID int, 
		RuleID int,
		RuleStepID int,
		SourceFieldName nvarchar(250), 
		SourceFieldTypeID int, 
		TargetFieldName nvarchar(250), 
		TargetFieldTypeID int, 
		Value nvarchar(4000)
	);

	IF OBJECT_ID('tempdb..#fieldValues') IS NOT NULL
		DROP TABLE #fieldValues;

	create table #fieldValues (
		ObjectType varchar(50), 
		ObjectID int, 
		FieldTypeID int, 
		Value nvarchar(4000)
	);
	
	insert into #rules
		select	R.ID,
				R.FusionID,
				R.ObjectType,
				R.ObjectID,
				I.FusionAttributeID as FilterFusionAttributeID,
				coalesce(A.FusionAttributeTypeID, R.ObjectID) as FilterFusionAttributeTypeID
		from	[fusion].[Rule] R
				inner join [fusion].[RuleItem] I on I.RuleID = R.ID and R.[Enabled] = 1
				left join FusionAttribute A on A.ID = I.FusionAttributeID


	
	declare	@currentID int,
			@maxID int

	set		@currentID = 1
	select	@maxID = MAX(ID) from #rules

	select @NumberOfRules = count(1) from #rules;

	--BEGIN: Determine the target fusion attributes to promote.
	while (@currentID <= @maxID)
	begin
		declare @FusionObjectType varchar(25),
				@FusionObjectID int,
				@FilterFusionAttributeID int,
				@FilterFusionAttributeTypeID int


		select	@RuleID = RuleID,
				@FusionObjectType = ObjectType,
				@FusionObjectID = ObjectID,
				@FusionID = FusionID,
				@FilterFusionAttributeID = FilterFusionAttributeID,
				@FilterFusionAttributeTypeID = FilterFusionAttributeTypeID
		from	#rules
		where	ID = @currentID

		if @FusionObjectID = @FilterFusionAttributeTypeID AND @FilterFusionAttributeID is not null
			begin
				-- You are on a specific nodes of same type.  Just copy to target table.
				insert into #attributes 
					select	@RuleID, 
							S.ID,
							S.[Action],
							@FilterFusionAttributeID
					from	[fusion].[RuleStep] S
					where	S.RuleID = @RuleID
					order by S.Step
			end
		else
			begin
				-- You are on an attribute higher up in hierarchy.
				if @FilterFusionAttributeID is null
					begin
						--  If there is NO filtered attribute ID, then you need to get every attribute in system for the partiular fusion instance.
						insert into #attributes
							select	@RuleID, 
									S.ID,
									S.[Action],
									FA.ID
							from	FusionAttribute FA 
									inner join [fusion].[RuleStep] S on S.RuleID = @RuleID and FA.FusionID = @FusionID and FA.FusionAttributeTypeID = @FusionObjectID
									left join #attributes A on A.FusionAttributeID = FA.ID and A.RuleID = S.RuleID and A.ID is null
							order by FA.ID, S.Step
					end
				else
					begin
						-- If there is a filter attribute ID, then traverse the hierarchy and get all attributes of the specified type.
						with FA as	(
									select	ID,
											ParentID,
											FusionAttributeTypeID
									from	FusionAttribute
									where	ID = @FilterFusionAttributeID
											and FusionID = @FusionID
									union all
									select	C.ID,
											C.ParentID,
											C.FusionAttributeTypeID
									from	FusionAttribute C
											inner join fa P on C.ParentID = P.ID --and P.ID <> C.ID
									)
	
						insert into #attributes
							select	@RuleID, 
									S.ID,
									S.[Action],
									FA.ID
							from	FA 
									inner join [fusion].[RuleStep] S on S.RuleID = @RuleID and FA.FusionAttributeTypeID = @FusionObjectID
									left join #attributes A on A.FusionAttributeID = FA.ID and A.RuleID = S.RuleID and A.ID is null
							where	FA.FusionAttributeTypeID = @FusionObjectID
							order by FA.ID, S.Step
					end
			end

		set @currentID = @currentID + 1
	end --end while loop
	--END: Determine the target fusion attributes to promote.

	-- Load field values we are working with, first starting with the Name.
	insert into #fields
		select	A.ID,
				RS.RuleID,
				M.RuleStepID,
				M.SourceFieldName,
				M.SourceFieldTypeID,
				M.TargetFieldName,
				M.TargetFieldTypeID,
				case 
					when M.SourceFieldName = 'ID' then cast(FA.ID as nvarchar)
					when M.SourceFieldName = 'Name' then FA.Name
					when M.SourceFieldName = 'TextPath' then FA.TextPath
					when M.IsConstantValue = 1 then M.ConstantValue
				end				
		from	[fusion].[RuleStepMapping] M
				inner join [fusion].[RuleStep] RS on M.RuleStepID = RS.ID
				inner join #attributes A on A.RuleID = RS.RuleID
				inner join FusionAttribute FA on FA.ID = A.FusionAttributeID 

	
	-- Update the fields table above with values for all dynamic fields.
	update	T
	set		T.Value = S.Value
	from	#fields T
			inner join #attributes A on A.ID = T.ID
			inner join Field S on S.ObjectType = 'FusionAttribute' and S.ObjectID = A.FusionAttributeID and S.FieldTypeID = T.SourceFieldTypeID 

--BEGIN: TESTING ---------------------------------------
/*
select * from #rules
select * from #attributes
select * from #fields
select * from FusionAttributePromotion where RuleID = 6

select * from IntersectMap where ID = 1424
select * from IntersectNode where ID = 720728
select * from [Intersect] where ID = 362728
delete FusionAttributePromotion where RuleID = 34
select	A.ID,
		R.RuleID,
		R.FusionID,
		R.ObjectID as FusionAttributeTypeID,
		R.PromotionObjectType,
		R.PromotionObjectID,
		R.PromotionParentObjectType,
		R.PromotionParentObjectID,
		A.FusionAttributeID
from	#rules R
		inner join #attributes A on A.RuleID = R.RuleID
*/
--END: TESTING ------------------------------------------

	set		@currentID = 1
	select	@maxID = MAX(ID) from #attributes

	set @NumberOfAttributesTotal = @maxID;
	
	while (@currentID <= @maxID)
	begin
		begin try

			declare @FusionAttributeTypeID int = null,
					@RuleStepID int = null,
					@Action varchar(25) = null,
					@ResultObject varchar(50) = null,
					@ResultObjectID int = null

			declare @fields table (SourceFieldName nvarchar(250), SourceFieldTypeID int, TargetFieldName nvarchar(250), TargetFieldTypeID int, Value nvarchar(4000))
			declare @settings table (Name nvarchar(100), Value nvarchar(250))
			
			select	@RuleID = R.RuleID,
					@RuleStepID = A.RuleStepID,
					@Action = A.[Action],
					@FusionID = R.FusionID,
					@FusionAttributeTypeID = R.ObjectID,
					@FusionAttributeID = A.FusionAttributeID,
					@ResultObject = P.ObjectType,
					@ResultObjectID = P.ObjectID
			from	#rules R
					inner join #attributes A on A.RuleID = R.RuleID and A.ID = @currentID
					left join [Fusion].RulePromotion P on P.FusionAttributeID = A.FusionAttributeID and P.RuleID = R.RuleID and P.RuleStepID = A.RuleStepID

			delete from @fields -- clear out previous fields
			--Load fields were are working with for this loop instance.
			insert into @fields
				select SourceFieldName, SourceFieldTypeID, TargetFieldName, TargetFieldTypeID, Value from #fields where ID = @currentID and RuleStepID = @RuleStepID

			delete from @settings -- clear out previous settings
			--Load settings were are working with for this loop instance.
			insert into @settings
				select Name, Value from [fusion].[RuleStepSetting] RSS inner join [fusion].[RuleStep] RS on (RSS.RuleStepID = RS.ID) where RS.RuleID = @RuleID and RS.ID = @RuleStepID
				
			--BEGIN: Promote action
			if @Action = 'Promote'
			begin
				declare @ObjectTypeToPromoteTo varchar(50) = null,
						@ObjectTypeIDToPromoteTo int = null,
						@ParentObjectSearchType nvarchar(250) = null,
						@ParentSearchObject varchar(50) = null,
						@ParentSearchObjectID int = null,
						@ParentObject varchar(50) = null,
						@ParentObjectID int = null

				select	@ObjectTypeToPromoteTo		= Value from @settings where Name = 'Object'
				select	@ObjectTypeIDToPromoteTo	= Value from @settings where Name = 'ObjectID'
				select	@ParentObjectSearchType		= Value from @settings where Name = 'ParentObjectSearch'
				select	@ParentSearchObject			= Value from @settings where Name = 'ParentObject'
				select	@ParentSearchObjectID		= Value from @settings where Name = 'ParentObjectID'

				if exists(select 1 from @fields where TargetFieldName = 'Name')
				begin
					declare @code nvarchar(50) = null,
							@name nvarchar(250) = null,
							@description nvarchar(4000) = null

					select @code = Value from @fields where TargetFieldName = 'Code'
					select @name = Value from @fields where TargetFieldName = 'Name'
					select @description = coalesce(Value, '') from @fields where TargetFieldName = 'Description'

					--BEGIN: Find parent based on search type
					if @ParentObjectSearchType = 'Direct'
					begin
						set @ParentObject = @ParentSearchObject
						set @ParentObjectID = @ParentSearchObjectID
					end

					if @ParentObjectSearchType = 'FusionOwner'
					begin
						select	@ParentObject = 'Artifact',
								@ParentObjectID = ArtifactID
						from	FusionOwner
						where	@ParentSearchObject = 'Owner'
								and FusionID = @FusionID
								--and ID = @ParentSearchObjectID
					end

					if @ParentObjectSearchType = 'ResultFromStep'
					begin
						select	@ParentObject = ObjectType,
								@ParentObjectID = ObjectID
						from	[fusion].[RulePromotion]
						where	@ParentSearchObject = 'Step'
								and RuleID = @RuleID
								and RuleStepID = @ParentSearchObjectID
								and FusionAttributeID = @FusionAttributeID
					end
					--END: Find parent based on search type

					print @ParentObject
					print @ParentObjectID

					--BEGIN: Determine object type to promote as
					if @ObjectTypeToPromoteTo = 'ArtifactType'
					begin
						set @ResultObject = 'Artifact'

						if @ResultObjectID is null
						begin
							select	@ResultObjectID = ID
							from	Artifact
							where	ArtifactTypeID = @ObjectTypeIDToPromoteTo
									and lower(Name) = lower(@name)
						end

						declare @modelTypeID int = null
						declare @taxonomyTypeValue nvarchar(250)

						select @taxonomyTypeValue = Value from @fields where TargetFieldName = 'TaxonomyTypeID'

--fusion.Rules
						if (@taxonomyTypeValue <> '' and @taxonomyTypeValue is not null)
						begin
							select @modelTypeID = ID from TaxonomyType where Name = ltrim(rtrim(@taxonomyTypeValue))
						end

						if @taxonomyTypeValue is null
						begin
							select @modelTypeID = min(ID) from TaxonomyType
						end

						if @ResultObjectID is null
						begin
							if @ParentObjectID = 0
							begin
								set @ParentObjectID = null
							end

							if @modelTypeID is not null
								begin
									insert into Artifact ( ParentID, ArtifactTypeID, TaxonomyTypeID, Name, Description, Status, UpdatedOn, UpdatedBy )
									values ( @ParentObjectID, @ObjectTypeIDToPromoteTo, @modelTypeID, @name, @description, 'Draft', getutcdate(), 0 )

									select @ResultObjectID =  SCOPE_IDENTITY()
									set @NumberOfNewArtifacts = @NumberOfNewArtifacts +1;
								end
						end
						else
						begin
							declare @testArtifactName nvarchar(250) = null,
									@testArtifactDescription nvarchar(4000) = null,
									@testArtifactParentID int = null,
									@testArtifactTaxonomyTypeID int = null

							select	@testArtifactName = Name,
									@testArtifactDescription = Description,
									@testArtifactParentID = ParentID,
									@testArtifactTaxonomyTypeID = TaxonomyTypeID
							from	Artifact
							where	ID = @ResultObjectID

							if @modelTypeID is not null
								begin
									if (@testArtifactName <> @name) 
										OR (@testArtifactDescription <> @description) 
										OR (@testArtifactParentID <> @ParentObjectID) 
										OR (@testArtifactTaxonomyTypeID <> @modelTypeID)
									begin
										update	Artifact
										set		Name = @name,
												Description = @description,
												ParentID = @ParentObjectID,
												TaxonomyTypeID = @modelTypeID
										where	ID = @ResultObjectID
									end
								end
						end
					end
					--END: IF ArtifactType

					if @ObjectTypeToPromoteTo = 'DomainType'
					begin
						if @ParentObject is null and @ParentObjectID is null
							begin
								set @ResultObject = 'Domain'
									
								-- You are promoting to a Domain (creating a list)
								if @ResultObjectID is null
									begin
										select	@ResultObjectID = ID
										from	Domain
										where	DomainTypeID = @ObjectTypeIDToPromoteTo
												and lower(Name) = lower(@name)
									end
 
								if @ResultObjectID is null
									begin
										insert into Domain  ( DomainTypeID, Name, Description ) 
										values ( @ObjectTypeIDToPromoteTo, @name, @description )

										select @ResultObjectID =  SCOPE_IDENTITY()

										set @NumberOfNewDomains = @NumberOfNewDomains +1;
									end
								else
									begin
										update	Domain
										set		Name = @name,
												Description = @description
										where	ID = @ResultObjectID
									end
							end
						else
							begin
								-- You are promoting domain items to a specific domain (list)
								set @ResultObject = 'DomainItem'

								if @ResultObject is null and @ResultObjectID is null
									begin
										select	@ResultObjectID = ID
										from	DomainItem
										where	DomainID = @ParentObjectID
												and lower(Code) = lower(@code)
									end
 
								if @ResultObjectID is not null
									begin
										update	DomainItem
										set		Name = @name,
												Code = coalesce(@code, @name),
												Description = @description
										where	ID = @ResultObjectID
									end
								else
									begin
										insert into DomainItem ( DomainID, Name, Code, Description )
										values ( @ParentObject, @name, coalesce(@code, @name), @description )

										select @ResultObjectID =  SCOPE_IDENTITY()

										set @NumberOfNewDomainItems = @NumberOfNewDomainItems +1;
									end
							end
					end
					--END: IF DomainType

					if @ObjectTypeToPromoteTo = 'TaxonomyType'
					begin
						set @ResultObject = 'Taxonomy'

						if @ResultObjectID is null
							begin
								select	@ResultObjectID = ID
								from	Taxonomy
								where	TaxonomyTypeID = @ObjectTypeIDToPromoteTo
										and ParentID = @ParentObjectID
										and lower(Name) = lower(@name)
							end

						if @ResultObjectID is null
							begin
								insert into Taxonomy	( ParentID, TaxonomyTypeID, Name, Description )
								values					( @ParentObjectID, @ObjectTypeIDToPromoteTo, @name, @description )

								select @ResultObjectID =  SCOPE_IDENTITY()

								set @NumberOfNewTaxonomies = @NumberOfNewTaxonomies +1;
							end
						else
							begin
								update	Taxonomy
								set		Name = @Name,
										Description = @Description--,
										--ParentID = @PromotionParentObjectID
								where	ID = @ResultObjectID
 							end
					end
					--END: IF TaxonomyType

					--END: Determine object type to promote as

				end -- END: Check to see if Target Field called NAME is present

			end --END: Promote action

			--BEGIN: Find Action
			if @Action = 'Find'
			begin
				declare @FindSearchType nvarchar(250) = null,
						@FindSearchObject varchar(50) = null,
						@FindSearchObjectID int = null,
						@FindFilterField int = null,
						@FindFilterFieldValue nvarchar(250) = null,
						@FindTargetField int = null,
						@FindParent int = null,
						@PromotionRuleStepID int = null

				select	@FindSearchType			= Value from @settings where Name = 'ObjectSearch'
				select	@FindSearchObject		= Value from @settings where Name = 'Object'
				select	@FindSearchObjectID		= Value from @settings where Name = 'ObjectID'
				select	@FindFilterField		= Value from @settings where Name = 'FilterField'
				select	@FindTargetField		= Value from @settings where Name = 'TargetField'
				select	@FindParent				= Value from @settings where Name = 'FindParent'
																
				if @FindSearchType = 'Fusion'
				begin					
					if @FindFilterField > 0
					begin
						select	@FindFilterFieldValue = Value
						from	@fields
						where	SourceFieldTypeID = @FindFilterField
					end
					else
					begin
						select	@FindFilterFieldValue = Value
						from	@fields
						where	SourceFieldName = 'Name'
					end
					
					if @FindFilterFieldValue is not null
					begin
						select	top 1
								@ResultObject = 'FusionAttribute',
								@ResultObjectID = ID
						from	FusionAttribute
						where	@FindSearchObject = 'FusionAttributeType'
								and FusionAttributeTypeID = @FindSearchObjectID
								and (TextPath = @FindFilterFieldValue or Name = @FindFilterFieldValue)
					end

				end

				--BEGIN: Find based on search type
				if @FindSearchType = 'FusionOwner'
				begin
					select	@ResultObject = 'Artifact',
							@ResultObjectID = ArtifactID
					from	FusionOwner
					where	@FindSearchObject = 'Owner'
							and FusionID = @FusionID
							--and ID = @FindSearchObjectID
				end

				if @FindSearchType = 'Glossary'					
				begin									
					if @FindFilterField > 0
					begin
						select	@FindFilterFieldValue = Value
						from	@fields
						where	SourceFieldTypeID = @FindFilterField
					end
					else
					begin
						select	@FindFilterFieldValue = Value
						from	@fields
						where	SourceFieldName = 'Name'	
						
											
					end
									

					if @FindFilterFieldValue is not null
					begin
						if @FindSearchObject = 'ArtifactType' and  ( @FindTargetField is null or @FindTargetField <= 0)
						begin							
							select	top 1
									@ResultObject = 'Artifact',
									@ResultObjectID = ID
							from	Artifact
							where	ArtifactTypeID = @FindSearchObjectID
									and (TextPath = @FindFilterFieldValue or Name = @FindFilterFieldValue)
						end

						if @FindSearchObject = 'ArtifactType' and @FindTargetField > 0
						begin							
							select	top 1
									@ResultObject = 'Artifact',
									@ResultObjectID = a.ID
							from	Artifact a
									inner join field f on(a.ID = f.ObjectID and f.Objecttype = 'Artifact' and f.fieldtypeid = @FindTargetField)
							where	a.ArtifactTypeID = @FindSearchObjectID									
									and (f.FormattedValue = @FindFilterFieldValue)
						end

						if @FindSearchObject = 'TaxonomyType'
						begin
							select	top 1
									@ResultObject = 'Taxonomy',
									@ResultObjectID = ID
							from	Taxonomy
							where	TaxonomyTypeID = @FindSearchObjectID
									and (TextPath = @FindFilterFieldValue or Name = @FindFilterFieldValue)
						end
					end

--select @ResultObjectID
				end

				if @FindSearchType = 'ResultFromStep' and @FindParent is not null
				begin
					select	@ResultObject = co.parent,
							@ResultObjectID = co.parentid
					from	[fusion].[RulePromotion] rp
						inner join [cache].[objectdetails] co on(co.[object] = rp.objecttype and co.objectid = rp.objectid)
					where	@FindSearchObject = 'Step'
							and rp.RuleID = @RuleID
							and rp.RuleStepID = @FindSearchObjectID
							and rp.FusionAttributeID = @FusionAttributeID
				end

				if @FindSearchType = 'ResultFromStep' and @FindParent is null
				begin
					select	@ResultObject = ObjectType,
							@ResultObjectID = ObjectID
					from	[fusion].[RulePromotion]
					where	@FindSearchObject = 'Step'
							and RuleID = @RuleID
							and RuleStepID = @FindSearchObjectID
							and FusionAttributeID = @FusionAttributeID
				end

				if @FindSearchType = 'Promotion' and @FindTargetField is null --by parent
				begin
					select	@ResultObject = ObjectType,
						    @ResultObjectID = ObjectID
					from	[fusion].[RulePromotion]
					join	FusionAttribute A on A.ID = @FusionAttributeID
					join	FusionAttribute AP on AP.ID = A.ParentID
					where	RuleStepID = @PromotionRuleStepID
							and FusionAttributeID = AP.ID
				end

				if @FindSearchType = 'Promotion' and @FindTargetField is not null -- by field
				begin
					select	@ResultObject = R.ObjectType, 
							@ResultObjectID = R.ObjectID 
					from	[fusion].[RulePromotion] R
					join	FusionAttribute SA on SA.ID = R.FusionAttributeID
					join	Field SF on SF.ObjectType = 'FusionAttribute' 
							and SF.ObjectID = SA.ID 
							and SF.FieldTypeID = @FindFilterField
					join	FusionAttribute TA on TA.ID = @FusionAttributeID
					join	Field TF on TF.ObjectType = 'FusionAttribute' 
							and TF.ObjectID = TA.ID 
							and TF.FieldTypeID = @FindTargetField
					where	R.RuleStepID = @PromotionRuleStepID 
							and SF.Value = TF.Value
				end

				--END: Find based on search type
			end --END: Find Action
			
			--BEGIN: Lineage Action
			if @Action = 'Lineage'
			begin
				declare @SubjectSearchType nvarchar(250) = null,
						@SubjectSearchObject varchar(50) = null,
						@SubjectSearchObjectID int = null,
						@Subject varchar(50) = null,
						@SubjectID int = null,
						@ObjectSearchType nvarchar(250) = null,
						@ObjectSearchObject varchar(50) = null,
						@ObjectSearchObjectID int = null,
						@Object varchar(50) = null,
						@ObjectID int = null,

						@TechnicalSubjectSearchType nvarchar(250) = null,
						@TechnicalSubjectSearchObject varchar(50) = null,
						@TechnicalSubjectSearchObjectID int = null,
						@TechnicalSubject varchar(50) = null,
						@TechnicalSubjectID int = null,
						@TechnicalObjectSearchType nvarchar(250) = null,
						@TechnicalObjectSearchObject varchar(50) = null,
						@TechnicalObjectSearchObjectID int = null,
						@TechnicalObject varchar(50) = null,
						@TechnicalObjectID int = null,

						@RoleID int = null

				select	@SubjectSearchType				= Value from @settings where Name = 'SubjectSearch'
				select	@SubjectSearchObject			= Value from @settings where Name = 'Subject'
				select	@SubjectSearchObjectID			= Value from @settings where Name = 'SubjectID'
				select	@ObjectSearchType				= Value from @settings where Name = 'ObjectSearch'
				select	@ObjectSearchObject				= Value from @settings where Name = 'Object'
				select	@ObjectSearchObjectID			= Value from @settings where Name = 'ObjectID'

				select	@TechnicalSubjectSearchType		= Value from @settings where Name = 'TechnicalSubjectSearch'
				select	@TechnicalSubjectSearchObject	= Value from @settings where Name = 'TechnicalSubject'
				select	@TechnicalSubjectSearchObjectID	= Value from @settings where Name = 'TechnicalSubjectID'
				select	@TechnicalObjectSearchType		= Value from @settings where Name = 'TechnicalObjectSearch'
				select	@TechnicalObjectSearchObject	= Value from @settings where Name = 'TechnicalObject'
				select	@TechnicalObjectSearchObjectID	= Value from @settings where Name = 'TechnicalObjectID'

				select	@RoleID							= Value from @settings where Name = 'Role'
				
				--BEGIN: Find subject based on search type, ALWAYS ResultFromStep
				if @SubjectSearchType = 'ResultFromStep'
				begin
					select	@Subject = ObjectType,
							@SubjectID = ObjectID
					from	[Fusion].[RulePromotion]
					where	@SubjectSearchObject = 'Step'
							and RuleID = @RuleID
							and RuleStepID = @SubjectSearchObjectID
							and FusionAttributeID = @FusionAttributeID
				end
				--END: Find subject based on search type

				--BEGIN: Find object based on search type
				if @ObjectSearchType = 'ResultFromStep' --ALWAYS ResultFromStep
				begin
					select	@Object = ObjectType,
							@ObjectID = ObjectID
					from	[fusion].[RulePromotion]
					where	@ObjectSearchObject = 'Step'
							and RuleID = @RuleID
							and RuleStepID = @ObjectSearchObjectID
							and FusionAttributeID = @FusionAttributeID
				end
				--END: Find object based on search type

				declare @Map table (ID int)

				--BEGIN: Add Map
				if @Subject = 'Intersect' and @SubjectID is not null and @Object = 'Intersect' and @ObjectID is not null
				begin
					MERGE	MapItem AS T
					USING	(
							SELECT	@SubjectID as SourceIntersectID, 
									@ObjectID as TargetIntersectID
							) as S
					ON		T.SourceIntersectID = S.SourceIntersectID
							and T.TargetIntersectID = S.TargetIntersectID 
					WHEN	NOT MATCHED THEN
							INSERT (SourceIntersectID, TargetIntersectID, CreatedBy, CreatedOn, UpdatedBy, UpdatedOn) 
							VALUES (S.SourceIntersectID, S.TargetIntersectID, 0, getutcdate(), 0, getutcdate())
					OUTPUT inserted.ID into @Map;
					
					set @ResultObject = 'MapItem'
					select top 1 @ResultObjectID = ID from @Map
				end
				--END: Add Map

				--BEGIN: Find subject based on search type, ALWAYS ResultFromStep
				if @TechnicalSubjectSearchType = 'ResultFromStep'
				begin
					select	@TechnicalSubject = ObjectType,
							@TechnicalSubjectID = ObjectID
					from	[Fusion].[RulePromotion]
					where	@TechnicalSubjectSearchObject = 'Step'
							and RuleID = @RuleID
							and RuleStepID = @TechnicalSubjectSearchObjectID
							and FusionAttributeID = @FusionAttributeID
				end
				--END: Find subject based on search type

				--BEGIN: Find object based on search type
				if @TechnicalObjectSearchType = 'ResultFromStep' --ALWAYS ResultFromStep
				begin
					select	@TechnicalObject = ObjectType,
							@TechnicalObjectID = ObjectID
					from	[fusion].[RulePromotion]
					where	@TechnicalObjectSearchObject = 'Step'
							and RuleID = @RuleID
							and RuleStepID = @TechnicalObjectSearchObjectID
							and FusionAttributeID = @FusionAttributeID
				end
				--END: Find object based on search type

				declare @MapRule table (ID int)

				--BEGIN: Add Map
				if @TechnicalSubject = 'FusionAttribute' and @TechnicalSubjectID is not null and @TechnicalObject = 'FusionAttribute' and @TechnicalObjectID is not null
				begin
					MERGE	MapRuleItem AS T
					USING	(
							SELECT	@TechnicalSubjectID as SourceFusionAttributeID, 
									@TechnicalObjectID as TargetFusionAttributeID
							) as S
					ON		T.SourceFusionAttributeID = S.SourceFusionAttributeID
							and T.TargetFusionAttributeID = S.TargetFusionAttributeID 
					WHEN	NOT MATCHED THEN
							INSERT (SourceFusionAttributeID, TargetFusionAttributeID, CreatedBy, CreatedOn, UpdatedBy, UpdatedOn) 
							VALUES (S.SourceFusionAttributeID, S.TargetFusionAttributeID, 0, getutcdate(), 0, getutcdate())
					OUTPUT inserted.ID into @Map;
					
					set @ResultObject = 'MapRuleItem'
					select top 1 @ResultObjectID = ID from @MapRule
				end
				--END: Add Map

				if exists(select ID from @Map) and exists(select ID from @MapRule)
				begin
					merge	MapRuleItemMapItem as T
					using	(
							select	B.ID as MapItemID,
									T.ID as MapRuleItemID
							from	@Map B
									inner join @MapRule T on 1=1
							) as S
					on		T.MapRuleItemID = S.MapRuleItemID and T.MapItemID = S.MapItemID
					when	not matched then
							insert (MapRuleItemID, MapItemID)
							values (S.MapRuleItemID, S.MapItemID);

					delete from @Map
					delete from @MapRule
				end

			end --END: Lineage Action

			--BEGIN: Relate Action
			if @Action = 'Relate'
			begin
				declare @R_IntersectTypeID int = null,
						@R_SubjectSearchType nvarchar(250) = null,
						@R_SubjectSearchObject varchar(50) = null,
						@R_SubjectSearchObjectID int = null,
						@R_Subject varchar(50) = null,
						@R_SubjectID int = null,
						@R_ObjectSearchType nvarchar(250) = null,
						@R_ObjectSearchObject varchar(50) = null,
						@R_ObjectSearchObjectID int = null,
						@R_Object varchar(50) = null,
						@R_ObjectID int = null,
						@R_IntersectID int = null

				select	@R_SubjectSearchType		= Value from @settings where Name = 'SubjectSearch'
				select	@R_SubjectSearchObject		= Value from @settings where Name = 'Subject'
				select	@R_SubjectSearchObjectID	= Value from @settings where Name = 'SubjectID'
				select	@R_ObjectSearchType			= Value from @settings where Name = 'ObjectSearch'
				select	@R_ObjectSearchObject		= Value from @settings where Name = 'Object'
				select	@R_ObjectSearchObjectID		= Value from @settings where Name = 'ObjectID'
				select	@R_IntersectTypeID			= Value from @settings where Name = 'IntersectType'


				--BEGIN: Find subject based on search type
				if @R_SubjectSearchType = 'Direct'
				begin
					set @R_Subject = @R_SubjectSearchObject
					set @R_SubjectID = @R_SubjectSearchObjectID
				end

				if @R_SubjectSearchType = 'FusionOwner'
				begin
					select	@R_Subject = 'Artifact',
							@R_SubjectID = ArtifactID
					from	FusionOwner
					where	@R_SubjectSearchObject = 'Owner'
							and FusionID = @FusionID
							--and ID = @R_SubjectSearchObjectID
				end

				if @R_SubjectSearchType = 'ResultFromStep'
				begin
					select	@R_Subject = ObjectType,
							@R_SubjectID = ObjectID
					from	[fusion].RulePromotion
					where	@R_SubjectSearchObject = 'Step'
							and RuleID = @RuleID
							and RuleStepID = @R_SubjectSearchObjectID
							and FusionAttributeID = @FusionAttributeID

--select @R_Subject, @R_SubjectID
				end

				if @R_SubjectSearchType = 'Self'
				begin
					set @R_Subject = 'FusionAttribute'
					set @R_SubjectID = @FusionAttributeID
				end
				--END: Find subject based on search type
				
				--BEGIN: Find object based on search type
				if @R_ObjectSearchType = 'Direct'
				begin
					set @R_Object = @R_ObjectSearchObject
					set @R_ObjectID = @R_ObjectSearchObjectID
				end

				if @R_ObjectSearchType = 'FusionOwner'
				begin
					select	@R_Object = 'Artifact',
							@R_ObjectID = ArtifactID
					from	FusionOwner
					where	@R_ObjectSearchObject = 'Owner'
							and FusionID = @FusionID
							--and ID = @R_ObjectSearchObjectID
				end

				if @R_ObjectSearchType = 'ResultFromStep'
				begin
					select	@R_Object = ObjectType,
							@R_ObjectID = ObjectID
					from	[Fusion].[RulePromotion]
					where	@R_ObjectSearchObject = 'Step'
							and RuleID = @RuleID
							and RuleStepID = @R_ObjectSearchObjectID
							and FusionAttributeID = @FusionAttributeID
				end

				if @R_ObjectSearchType = 'Self'
				begin
					set @R_Object = 'FusionAttribute'
					set @R_ObjectID = @FusionAttributeID

				end
				--END: Find object based on search type


				--Check to see if we have all the required data to create the relationship.
				if @R_IntersectTypeID is not null and @R_subject is not null and @R_SubjectID is not null and @R_Object is not null and @R_ObjectID is not null
				begin
					-- Validate that intersect type exists.
					if exists(select 1 from IntersectType where ID = @R_IntersectTypeID)
					begin
						set @ResultObject = 'Intersect'
--select @Subject, @SubjectID, @Object, @ObjectID
						select	@R_IntersectID = ID
						from	[Intersect]
						where	Subject = @R_Subject 
								and SubjectID = @R_SubjectID 
								and Object = @R_Object 
								and ObjectID = @R_ObjectID
								and IntersectTypeID = @R_IntersectTypeID

						if @R_IntersectID is null
						begin
							declare @R_SubjectType varchar(50) = null,
									@R_SubjectTypeID int = null,
									@R_SubjectIntersectTypeNodeID int = null,
									@R_ObjectType varchar(50) = null,
									@R_ObjectTypeID int = null,
									@R_ObjectIntersectTypeNodeID int = null

							select	@R_SubjectType = ObjectType, @R_SubjectTypeID = ObjectTypeID from cache.[object] where Object = @R_Subject and ObjectID = @R_SubjectID
							select	@R_ObjectType = ObjectType, @R_ObjectTypeID = ObjectTypeID from cache.[object] where Object = @R_Object and ObjectID = @R_ObjectID

							select	@R_IntersectTypeID = ID
							from	[IntersectType] R 
							where	Subject = @R_SubjectType and SubjectID = @R_SubjectTypeID 
									and Object = @R_ObjectType and ObjectID = @R_ObjectTypeID;


							if @R_IntersectTypeID is not null
							begin
								begin try
									insert into [Intersect] (IntersectTypeID, Classification, Subject, SubjectID, Object, ObjectID, Deleted, CreatedBy, CreatedOn, UpdatedBy, UpdatedOn)
									values					(@R_IntersectTypeID, 2, @R_Subject, @R_SubjectID, @R_Object, @R_ObjectID, 0, @r, @d, @r, @d)  

									select @R_IntersectID = SCOPE_IDENTITY()

									--cache logic
									insert into cache.[Object] ( [Object], [ObjectID], [ObjectType], [ObjectTypeID] ) values	( 'Intersect', @R_IntersectID, 'IntersectType', @R_IntersectTypeID );
									insert into cache.Relationship ( IntersectID, SourceIntersectTypeNodeID, SourceIntersectNodeID, SourceObject, SourceObjectID, TargetIntersectTypeNodeID, TargetIntersectNodeID, TargetObject, TargetObjectID )
									values	( @R_IntersectID, 0, 0, @R_Subject, @R_SubjectID, 0, 0, @R_Object, @R_ObjectID );
									insert into cache.Relationship ( IntersectID, SourceIntersectTypeNodeID, SourceIntersectNodeID, SourceObject, SourceObjectID, TargetIntersectTypeNodeID, TargetIntersectNodeID, TargetObject, TargetObjectID )
									values	( @R_IntersectID, 0, 0, @R_Object, @R_ObjectID, 0, 0, @R_Subject, @R_SubjectID );

									--Update the responsibilities of the object that should inherit form the other (Taxonomy can push relationships down to artifact)
									if ( (@R_Subject = 'Taxonomy' and @R_Object = 'Artifact') OR (@R_Subject = 'Artifact' and @R_Object = 'Taxonomy') )
									begin
										if @R_Subject = 'Artifact'
										begin
											exec [cache].[SynchronizeResponsibilitiesForObject] @R_Subject, @R_SubjectID
										end
										if @R_Object = 'Artifact'
										begin
											exec [cache].[SynchronizeResponsibilitiesForObject] @R_Object, @R_ObjectID
										end
									end

									exec utility.AddAuditEntry @R_Subject, @R_SubjectID, @r, @d, 'Created', 'Intersect', @R_IntersectID
									exec utility.AddAuditEntry @R_Object, @R_ObjectID, @r, @d, 'Created', 'Intersect', @R_IntersectID

									set @NumberOfNewRelations = @NumberOfNewRelations + 1

									set @ResultObjectID = @R_IntersectID
								end try
								begin catch
									select ERROR_MESSAGE()
								end catch

							end
						end
						else
						begin
							set @ResultObjectID = @R_IntersectID
						end
					end
				end


			end --END: Relate Action


			-- Add/Update the promotion record to keep track of the auto-promotions
			if @ResultObject is not null and @ResultObjectID is not null
			begin
				-- Insert/Update the FusionAttributePromotion table to keep track of previously promoted objects.
				MERGE	[fusion].[RulePromotion] AS T
				USING	(
						SELECT	@FusionAttributeID as FusionAttributeID, 
								@ResultObject as ObjectType, 
								@ResultObjectID as ObjectID, 
								@RuleID as RuleID,
								0 as PromotedObjectTypeID,
								@RuleStepID as RuleStepID
						) as S
				ON		T.RuleID = S.RuleID
						and T.RuleStepID = S.RuleStepID 
						and T.FusionAttributeID = S.FusionAttributeID 
						and T.ObjectType = S.ObjectType 
						and T.ObjectID = S.ObjectID
				WHEN	MATCHED THEN
						UPDATE SET	T.RuleID = S.RuleID, 
									T.ObjectTypeID = S.PromotedObjectTypeID
				WHEN	NOT MATCHED THEN
						INSERT (FusionAttributeID, ObjectType, ObjectID, RuleID, RuleStepID, ObjectTypeID) 
						VALUES (S.FusionAttributeID, S.ObjectType, S.ObjectID, S.RuleID, S.RuleStepID, S.PromotedObjectTypeID);


				-- Add/Update the dynamic fields involved.

				-- First, clean up fields table variable of static fields to prepare for dynamic field work below.
				delete @fields where TargetFieldTypeID = 0

				-- Now insert the dynamic fields
				while exists (select 1 from @fields)
				begin
					declare @targetFieldTypeID int,
							@field_Type varchar(25),
							@lookupObjectType varchar(25),
							@lookupObjectID int,
							@fieldValue nvarchar(4000),
							@shouldInsert bit = 0

					select	top 1 
							@targetFieldTypeID = TargetFieldTypeID,
							@fieldValue = Value
					from	@fields
									
					select	@field_Type = [Type],
							@lookupObjectType = LookupObjectType,
							@lookupObjectID = LookupObjectID									
						from	FieldType 
						where	ID = @targetFieldTypeID

					if @field_Type = 'Lookup'
					begin
						declare @objectResultID int

						if @lookupObjectType = 'Artifact'
							begin
								select	top 1
										@objectResultID = ID
								from	Artifact
								where	ArtifactTypeID = @lookupObjectID and Name = @fieldValue
							end
						if @lookupObjectType = 'Domain'
							begin
								select	top 1
										@objectResultID = ID
								from	DomainItem
								where	DomainID = @lookupObjectID and Name = @fieldValue
							end
						if @lookupObjectType = 'Lookup'
							begin
								select	top 1
										@objectResultID = L.ID
								from	[Lookup] L
										inner join Field F on F.ObjectType = @lookupObjectType and F.ObjectID = L.ID and L.LookupTypeID = @lookupObjectID and F.FieldTypeID = @targetFieldTypeID and F.FormattedValue = @fieldValue
							end
											
						if @ResultObjectID is not null and @objectResultID is not null
							begin
								-- Lookup values properly resolved, so you can now insert the Field record.
													
								set @shouldInsert = 1
								set @fieldValue = cast(@objectResultID as nvarchar(4000))
							end
					end									
					else
					begin
						-- This is a text value, so just insert it into the Field table for the promoted object.
						set @shouldInsert = 1
					end

					if @shouldInsert = 1
					begin
						If not EXISTS (SELECT 1 FROM #fieldValues where ObjectType = @ResultObject and ObjectID = @ResultObjectID and FieldTypeID = @targetFieldTypeID) --avoid duplicates this happens in gmo
						begin
							insert into #fieldValues (ObjectType, ObjectID, FieldTypeID, Value) values(@ResultObject, @ResultObjectID, @targetFieldTypeID, @fieldValue)
						end
					end
						
					-- Delete the field we just finished processing.
					delete @fields where TargetFieldTypeID = @targetFieldTypeID
				end --END: while

			end --END: IF when checking for promotiontype


		end try
		begin catch
			SELECT 
				ERROR_NUMBER() AS ErrorNumber
				,ERROR_MESSAGE() AS ErrorMessage;
		end catch

		set @currentID = @currentID + 1
	end


	-- write the field values from the temp table to the field table
	-- the field table has a trigger doing this once outside the loop causes the trigger to only fire this one time.
	If EXISTS (SELECT 1 FROM #fieldValues)
	begin
		--debug shows values 
		--select * from #fieldValues

		merge	Field as T
		using	(
				select	f.ObjectType as ObjectType,
						f.ObjectID as ObjectID,
						f.FieldTypeID as FieldTypeID,
						f.Value as Value
				from	#fieldValues f 
						inner join dbo.FieldType ft on (ft.ID = f.FieldTypeID)
				) as S
		on		T.ObjectType = S.ObjectType and T.ObjectID = S.ObjectID and T.FieldTypeID = S.FieldTypeID
		when	matched then
				update set T.Value = S.Value
		when	not matched then
				insert (ObjectType, ObjectID, FieldTypeID, Value) values (S.ObjectType, S.ObjectID, S.FieldTypeID, S.Value);
	end

	---- Add new relations as needed
	--exec [utility].[PromoteFusionAttributesRelations] @NumberOfNewRelations output

	---- Handle any fusionlookup fields
	--exec [utility].[PromoteFusionAttributeLookups]


	----Log this run done
	update	[fusion].[RuleLog]
	set		DateCompleted = CURRENT_TIMESTAMP, 
			[PromotedTaxonomies] = @NumberOfNewTaxonomies, 
			[PromotedDomainItems] = @NumberOfNewDomainItems,  
			[PromotedDomains] = @NumberOfNewDomains,
			[PromotedArtifacts] = @NumberOfNewArtifacts,
			[TotalNewPromotions] = (@NumberOfNewTaxonomies + @NumberOfNewDomainItems + @NumberOfNewDomains + @NumberOfNewArtifacts),
			[AttributesConsidered]= @NumberOfAttributesTotal,
			[NumberOfRules] = @NumberOfRules ,
			[RelationshipsAdded] = @NumberOfNewRelations
	where	ID = @ExecutionID;
END
GO

ALTER PROCEDURE [fusion].[ProcessEagleMCToBBMnemonic]
	@StagingFileID int,
	@FusionID int
AS
BEGIN	
	SET NOCOUNT ON;
		
	declare		@eagleStreamID int,
				@streamToFieldIntersectTypeID int;

	declare		@IDList Table(IntersectID int,StageID Int);

	declare		@Intersects IDTable;

	declare		@MessageStreamFussionAttributeID int = 196,
				@BloombergMnemonicFusionID int = 301;
				
	-- load the stream that we want to add relations ships for    
	select @eagleStreamID = fusionattributeid from [fusion].[stagingfile] where id = @StagingFileID and fusionID = @FusionID
		
	if @eagleStreamID is null
	begin
		raiserror('ERROR : UNABLE TO LOCATE SPECIFIED STREAM INFORMATION FOR INPUT FUSION ID / STAGING ID', 15, 1);
		return;
	end;

	-- add relationships for Stream (196) to Eagle DB Columns (205)
	-- using star tag field that is a field for for fusionattribute type 205 lookup fields to add rels for
	-- todo pull to separate proc
	if @eagleStreamID is not null
	begin
			Declare @StreamToFieldList Table(FieldFusionAttributeID int, StreamFusionAttributeID int, IntersectTypeID int, ID int);
			
			-- load the intersect type ids
			select	@streamToFieldIntersectTypeID = ID
			from	[IntersectType]
			where	Subject = 'FusionAttributeType' and 
					Object = 'FusionAttributeType' and 
					(
						( SubjectID = @MessageStreamFussionAttributeID and ObjectID = @BloombergMnemonicFusionID ) OR
						( SubjectID = @BloombergMnemonicFusionID and ObjectID = @MessageStreamFussionAttributeID )
					)

			if @streamToFieldIntersectTypeID is null
			begin
				raiserror('ERROR : UNABLE TO LOCATE INTERSECT TYPE IDS FOR EAGLE TO EAGLE MESSAGE STREAMS', 15, 1);
				return;
			end;

			-- insert into in memory table variable the values we want to add intersects for
			insert into @StreamToFieldList
				select		fa.id, 
							sf.FusionAttributeID, 
							@streamToFieldIntersectTypeID, 
							ROW_NUMBER() OVER (Order by fa.id) AS 'RowNumber'
				from		fusionAttribute fa
							inner join [fusion].[StagingFileItem] sfi on (sfi.value = fa.name)				
							inner join [fusion].[StagingFile] sf on (sfi.stagingfileid = sf.id)
							left join [Intersect] I on	I.IntersectTypeID = @streamToFieldIntersectTypeID and 
														I.Subject = 'FusionAttribute' and 
														I.Object ='FusionAttribute' and
														(
															( SubjectID = sf.FusionAttributeID and ObjectID = fa.ID ) OR
															( SubjectID = fa.ID and ObjectID = sf.FusionAttributeID )
														)
					where		fa.fusionattributetypeid = @BloombergMnemonicFusionID and 
								sfi.stagingfileid = @StagingFileID and 
								I.ID is null
					group by	fa.id, sf.FusionAttributeID  -- grouping is used to eliminate duplicate star tag relations

			MERGE
				INTO    [Intersect] d
				USING   (
							SELECT	IntersectTypeID, 
									ID,
									StreamFusionAttributeID as SubjectID,
									FieldFusionAttributeID as ObjectID
							FROM	@StreamToFieldList							
						) s
				ON      (1 = 0)
				WHEN NOT MATCHED THEN
				INSERT  (IntersectTypeID, Classification, Description, Subject, SubjectID, Object, ObjectID)
				VALUES  (s.IntersectTypeID, 2, NULL, 'FusionAttribute', s.SubjectID, 'FusionAttribute', s.ObjectID)
				OUTPUT  INSERTED.ID, s.ID into @IDList;
										
			insert into @Intersects 
				select idl.intersectid from @IDList idl;
			
			declare @IntersectCount int
			select @IntersectCount = count(1) from @Intersects
			
			if @IntersectCount > 0 
			begin				
				EXEC cache.SynchronizeRelationships @Intersects
			end
	end;
end
GO

ALTER PROCEDURE [fusion].[ProcessEagleMCToBloombergRelations]	
	@StagingFileID int,
	@FusionID int
AS
BEGIN	
	SET NOCOUNT ON;
	
	
	declare		@eagleStreamID int;				
	declare		@IntersectCount int;
	Declare		@IDList Table(IntersectID int,StageID Int);
	declare		@Intersects IDTable;
	declare		@fieldToBBIntersectTypeID int;

	-- load the panel that we want to add relations ships for
    
	select @eagleStreamID = fusionattributeid from [fusion].[stagingfile] where id = @StagingFileID and fusionID = @FusionID
	
	if @eagleStreamID is null
	begin
		raiserror('ERROR : UNABLE TO LOCATE SPECIFIED STREAM INFORMATION FOR INPUT FUSION ID / STAGING ID', 15, 1);
		return;
	end;
			
	exec ProcessEagleMCToEagleFieldRelations @StagingFileID, @FusionID

	exec [fusion].[ProcessEagleMCToBBMnemonic] @StagingFileID, @FusionID


	-- add relations for Eagle Field (205) to Bloomberg mnemonic (301)
	if @eagleStreamID is not null
	begin
		Declare @BBToFieldList Table(FieldFusionAttributeID int, StreamFusionAttributeID int, IntersectTypeID int, ID int);
		
		-- load the intersect id's for message stream to bb mnemonic
		select	@fieldToBBIntersectTypeID = ID
		from	[IntersectType]
		where	Subject = 'FusionAttributeType' 
				and SubjectID = 301					
				and Object = 'FusionAttributeType' 
				and ObjectID = 205;

		if @fieldToBBIntersectTypeID is null
		begin
			raiserror('ERROR : UNABLE TO LOCATE INTERSECT TYPE IDS FOR EAGLE TO BLOOMBERG INTERSECT', 15, 1);
			return;
		end

		-- load into memory the id's that we need to add intersects for
		insert into @BBToFieldList
			select	fa.id as 'fieldID', faBB.id as 'bbID', @fieldToBBIntersectTypeID, ROW_NUMBER() OVER (Order by sfi.id) AS 'RowNumber'
			from	field f 
					inner join fusionAttribute fa on (f.ObjectID = fa.ID)
					inner join fieldtype ft on (f.fieldtypeid = ft.id)
					inner join [fusion].[StagingFileItem] sfi on (sfi.tag = f.value)				
					inner join [fusion].[StagingFile] sf on (sfi.stagingfileid = sf.id)						
					inner join fusionAttribute faBB on (faBB.Name = sfi.value and faBB.fusionattributetypeid = 301)		
					left join [Intersect] I on	I.IntersectTypeID = @fieldToBBIntersectTypeID and 
												I.Subject = 'FusionAttribute' and 
												I.Object ='FusionAttribute' and
												(
													( I.SubjectID = faBB.ID and I.ObjectID = fa.ID ) OR
													( I.SubjectID = fa.ID and I.ObjectID = faBB.ID )
												)
			where	fa.fusionattributetypeid = 205 and 
					ft.name = 'startag' and 
					sfi.stagingfileid = @StagingFileID and 
					I.ID is null;

			MERGE
				INTO    [Intersect] d
				USING   (
							SELECT	IntersectTypeID, 
									ID,
									StreamFusionAttributeID as SubjectID,
									FieldFusionAttributeID as ObjectID
							FROM	@BBToFieldList
						) s
				ON      (1 = 0)
				WHEN NOT MATCHED THEN
				INSERT  (IntersectTypeID, Classification, Subject, SubjectID, Object, ObjectID)
				VALUES  (s.IntersectTypeID, 2, 'FusionAttribute', s.SubjectID, 'FusionAttribute', s.ObjectID)
				OUTPUT  INSERTED.ID, s.ID into @IDList;										

			insert into @Intersects 
				select idl.intersectid from @IDList idl;
						
			select @IntersectCount = count(1) from @Intersects
			if @IntersectCount > 0 
			begin
				EXEC cache.SynchronizeRelationships @Intersects
			end
	end;
END
GO

ALTER PROCEDURE [fusion].[ProcessFusionRelationships]
	@executionID int	
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	set NOCOUNT, ANSI_PADDING ON;
	SET ANSI_WARNINGS ON;

	declare @Intersects IDTable;
	declare @objectType varchar(50) = 'FusionAttribute';

    -- delete any relations we already have that was already added from stagingrelation table so we dont duplicate
	delete	T
	from	fusion.StagingRelation T
			left join [Intersect] S on	S.Subject = @objectType and 
										S.Object = @objectType and
										(
											( S.SubjectID = T.StartFusionAttributeID and S.ObjectID = T.EndFusionAttributeID ) OR
											( S.SubjectID = T.EndFusionAttributeID and S.ObjectID = T.StartFusionAttributeID )
										)
	where	ExecutionID = @executionID and
			S.ID is null;
					
	Declare @IDList Table(IntersectID int, StageID Int);
			
	MERGE
		INTO    [Intersect] d
		USING   (
				SELECT	IntersectTypeID, 
						ID,
						StartFusionAttributeID,
						EndFusionAttributeID
				FROM	[fusion].stagingrelation
				where	ExecutionID = @executionID 
						and IntersectID is null
				) S
		ON      (1 = 0)
		WHEN NOT MATCHED THEN
		INSERT  (IntersectTypeID, Classification, Description, Subject, SubjectID, Object, ObjectID)
		VALUES  (S.IntersectTypeID, 2, NULL, @objectType, StartFusionAttributeID, @objectType, EndFusionAttributeID)
		OUTPUT  INSERTED.ID, S.ID into @IDList;
	
	--update StagingRelation to have the id's we used in intersect table.
	UPDATE	T
	SET		T.IntersectID = S.IntersectID
	from	[fusion].[StagingRelation] T
			inner join @IDList S on T.ExecutionID = @executionID and T.ID = S.StageID;

	insert into @Intersects 
		select	IntersectID 
		from	@IDList;
			
	declare @IntersectCount int
	select @IntersectCount = count(1) from @Intersects
	if @IntersectCount > 0 
	begin
		EXEC cache.SynchronizeRelationships @Intersects
	end
END
GO

CREATE procedure [bulkload].[Relationships]
--declare
	@id int
--set @id = 252
as
begin
	set nocount on;

	declare @r int,
			@intersectTypeID int,
			@subjectHasSubjectArea bit,
			@subject varchar(50),
			@subjectID int,
			@objectHasSubjectArea bit,
			@object varchar(50),
			@objectID int,
			@dt datetime = getutcdate(),
			@columnCount int,
			@startDynamicFieldColumnIndex int

	select	@r = UpdatedBy,
			@intersectTypeID = ObjectID
	from	[Load] 
	where	[Action] = 'R' 
			and ID = @id

	select	@columnCount = count(1) from LoadColumn where LoadID = @id

	select	@subject = Subject,
			@subjectID = SubjectID,
			@object = Object,
			@objectID = ObjectID
	from	IntersectType
	where	ID = @intersectTypeID

	if @subject = 'ArtifactType'
		begin
			set @subjectHasSubjectArea = 1
			exec bulkload.UpdateSubjectAreaColumn @id, 1							-- subject subject area
			exec bulkload.UpdateItemColumnByType @id, @subject, @subjectID, 1, 2	-- subject
		end
	else
		begin
			set @subjectHasSubjectArea = 0
			exec bulkload.UpdateItemColumnByType @id, @subject, @subjectID, 0, 1	-- subject
		end

	if @object = 'ArtifactType'
		begin
			set @objectHasSubjectArea = 1

			if @subjectHasSubjectArea = 1
				begin
					exec bulkload.UpdateSubjectAreaColumn @id, 3							-- object subject area
					exec bulkload.UpdateItemColumnByType @id, @object, @objectID, 3, 4		-- object
				end
			else
				begin 
					exec bulkload.UpdateSubjectAreaColumn @id, 1							-- object subject area
					exec bulkload.UpdateItemColumnByType @id, @object, @objectID, 2, 3		-- object
				end
		end
	else
		begin
			set @objectHasSubjectArea = 0

			if @subjectHasSubjectArea = 1
				begin
					exec bulkload.UpdateItemColumnByType @id, @object, @objectID, 0, 3		-- object
				end
			else
				begin 
					exec bulkload.UpdateItemColumnByType @id, @object, @objectID, 0, 2		-- object
				end
		end

	select @startDynamicFieldColumnIndex =	case
												when @subjectHasSubjectArea = 1 and @objectHasSubjectArea = 1 then 4
												when @subjectHasSubjectArea = 1 and @objectHasSubjectArea = 0 then 3
												when @subjectHasSubjectArea = 0 and @objectHasSubjectArea = 1 then 3
												else 2
											end
	set @startDynamicFieldColumnIndex = @startDynamicFieldColumnIndex + 1

	select @startDynamicFieldColumnIndex, @columnCount

	drop table if exists #Items

	BEGIN TRANSACTION [Tran1]

	BEGIN TRY
		-- Load Temp table that we are going to work from
		select	S.RowIndex,
		
				S.LookupObject as Subject,
				S.LookupObjectID as SubjectID,

				O.LookupObject as Object,
				O.LookupObjectID as ObjectID,
				
				cast(0 as int) as IntersectID,
				cast('' as char(1)) as IntersectChangeType,

				case 
					when @startDynamicFieldColumnIndex <= @columnCount then cast(0 as bit)
					else cast(1 as bit)
				end as DynamicFieldsAreValid,

				cast(0 as bit) as Status,
				cast('' as nvarchar(500)) as StatusMessage,

				@r as ResourceID  --THE USER THAT ADDED THE LOAD
		into	#Items
		from	LoadItemColumn S
				inner join LoadItemColumn O on O.LoadID = S.LoadID 
											and O.RowIndex = S.RowIndex 
											and O.ColumnIndex = @startDynamicFieldColumnIndex-1
		where	S.LoadID = @id
				and S.ColumnIndex = case 
										when @subjectHasSubjectArea = 1 then 2
										else 1
									end

		-- Add indexes to temp table
		CREATE NONCLUSTERED INDEX [IX_Intersect] ON #Items ( Subject ASC, SubjectID ASC, Object ASC, ObjectID ASC )
--select * from #Items
		if @startDynamicFieldColumnIndex <= @columnCount	--has dynamic fields
		begin
			--DynamicFieldsAreValid

			-- PARSE any dynamic fields that are specifically lookups.
			exec [bulkload].[UpdateDynamicLookupFieldColumns] @id, @startDynamicFieldColumnIndex, @columnCount

			update	T
			set		T.DynamicFieldsAreValid = case
												when S.InvalidCount = 0 then cast(1 as bit)
												else cast(0 as bit)
											end
			from	#Items T
					inner join	(
								select	I.LoadID,
										I.RowIndex,
										C.InvalidCount
								from	[Load] L
										inner join [LoadItem] I on I.LoadID = L.ID
										cross apply (
													select	count(1) as InvalidCount
													from	[LoadItemColumn] IC
															inner join FieldType F on L.[Object] = F.[Object] and L.ObjectID = F.ObjectID and F.[Type] = 'Lookup'
															inner join [LoadColumn] C on C.LoadID = IC.LoadID and F.Name = C.Name and C.ColumnIndex = IC.ColumnIndex and C.ColumnIndex between @startDynamicFieldColumnIndex and @columnCount
													where	IC.LoadID = @id 
															and IC.RowIndex = I.RowIndex
															and IC.LookupObject is null and IC.LookupObjectID is null
													) C
								where	L.ID = @id
								) S on S.RowIndex = T.RowIndex
		end

		-- update rows with existing intersects
		update	T
		set		T.IntersectID = S.ID,
				T.IntersectChangeType = 'U'
		from	#Items T
				inner join [Intersect] S on S.IntersectTypeID = @intersectTypeID 
										and T.Subject = S.Subject 
										and T.SubjectID = S.SubjectID 
										and T.Object = S.Object 
										and T.ObjectID = S.ObjectID
										and DynamicFieldsAreValid = 0

		-- insert relationships
		insert into [Intersect] (IntersectTypeID, Subject, SubjectID, Object, ObjectID, Deleted, CreatedBy, CreatedOn, UpdatedBy, UpdatedOn)
			select	distinct
					@intersectTypeID, 
					Subject, SubjectID, Object, ObjectID,
					0, ResourceID, @dt, ResourceID, @dt
			from	#Items
			where	IntersectID = 0
					and IntersectChangeType <> 'U'
					and Subject is not null and SubjectID is not null
					and Object is not null and ObjectID is not null
					and DynamicFieldsAreValid = 1;

		-- update rows with new intersect
		update	T
		set		T.IntersectID = S.ID,
				T.IntersectChangeType = 'A'
		from	#Items T
				inner join [Intersect] S on S.IntersectTypeID = @intersectTypeID and T.Subject = S.Subject and T.SubjectID = S.SubjectID and T.Object = S.Object and T.ObjectID = S.ObjectID
				and T.IntersectChangeType <> 'U';


		-- merge the dynamic fields involved with this load into the Fields table.
		exec [bulkload].MergeDynamicLookupFields @id, @startDynamicFieldColumnIndex, @columnCount

		-- update status & status message for Items table
		
		-- SUCCESS STATUS
		update	#Items
		set		Status = 1,
				StatusMessage = case IntersectChangeType
									when 'A' then 'Relationship created. '
									when 'U' then 'Relationship updated. '
								end
		where	IntersectID > 0;

		-- FAILED STATUS
		update	T
		set		T.Status = 0,
				T.StatusMessage = T.StatusMessage +
								'Relationship could not be created nor updated. ' + 
								IIF(T.SubjectID is null, 'Could not find subject. ', '') + 
								IIF(T.ObjectID is null, 'Could not find object. ', '') + 
								IIF(T.DynamicFieldsAreValid = 0, 'One or more dynamic lookup fields is invalid. ', '') 
		from	#Items T
		where	IntersectID = 0;

		-- Now update LoadItems on original Load with status and messages created above
		update	T
		set		T.Status = S.Status,
				T.StatusMessage = S.StatusMessage,
				T.Object = case S.Status
							when 1 then 'Intersect'
							else NULL
						   end,
				T.ObjectID = case S.Status
							when 1 then S.IntersectID
							else NULL
						   end
		from	LoadItem T
				inner join #Items S on T.LoadID = @id and S.RowIndex = T.RowIndex;

		-- Now perform audit
		declare @current int = 2,
				@max int,
				@s varchar(50),
				@sid int,
				@o varchar(50),
				@oid int,
				@intersect int,
				@ct varchar(25)
		select	@max = max(Rowindex) from #Items

		while @current <= @max
		begin
			select	@s = Subject,
					@sid = SubjectID,
					@o = Object,
					@oid = ObjectID,
					@intersect = IntersectID,
					@ct = case IntersectChangeType
							when 'A' then 'Created'
							else 'Updated'
						end
			from	#items
			where	RowIndex = @current

			if @intersect > 0
			begin
				exec utility.AddAuditEntry @s, @sid, @r, @dt, @ct, 'Intersect', @intersect
				exec utility.AddAuditEntry @o, @oid, @r, @dt, @ct, 'Intersect', @intersect
			end

			set @current = @current + 1
		end

		-- Close out the Load job
		update	[Load]
		set		DateCompleted = getutcdate()
		where	ID = @id;

		COMMIT TRANSACTION [Tran1]
	END TRY
	BEGIN CATCH
		ROLLBACK TRANSACTION [Tran1]
	END CATCH
end
GO

ALTER procedure [fusion].[ProcessUnprocessedRelations]
as
begin
	set NOCOUNT, ANSI_PADDING ON;
	SET ANSI_WARNINGS ON;

	declare @unprocessedRelationsExeId int;
	
	set @unprocessedRelationsExeId = -99;

	-- delete any unprocessed relations older than 3 days
	delete from [fusion].[StagingRelationUnresolved] where DATEDIFF(day,getdate(),CreatedOn) < -3

	-- delete any unprocessed relations from any prior run that may be hanging around
	delete from [fusion].[StagingRelation] where executionid = @unprocessedRelationsExeId
			
	-- load the unprocessed relations for now across all fusion types /ids

	insert into [fusion].[StagingRelation]
				select	distinct 
						@unprocessedRelationsExeId,
						R.StartID,
						R.EndID,
						S.ID,
						E.ID,
						S.FusionAttributeTypeID,
						E.FusionAttributeTypeID,
						IT.ID,
						null
				from	(
						select	srm.StartID,
								srm.EndID
						from	[fusion].[StagingRelationUnresolved] srm													
						) R
						inner join FusionAttribute S on S.SourceID = R.StartID
						inner join FusionAttribute E on E.SourceID = R.EndID
						inner join IntersectType IT on	IT.Subject = 'FusionAttributeType' and 
														IT.Object = 'FusionAttributeType' and 
														(
															( IT.SubjectID = S.FusionAttributeTypeID and IT.ObjectID = E.FusionAttributeTypeID ) OR
															( IT.SubjectID = E.FusionAttributeTypeID and IT.ObjectID = S.FusionAttributeTypeID )
														)
				where	NOT EXISTS	(
									select	* 
									from	[Intersect] I
									where	I.Subject = 'FusionAttribute' and 
											I.Object = 'FusionAttribute' and
											(
												(I.SubjectID = S.ID and I.ObjectID = E.ID ) OR
												(I.SubjectID = E.ID and I.ObjectID = S.ID )
											)
									)

	-- process these relations as regular relations
	exec [fusion].[ProcessFusionRelationships] @unprocessedRelationsExeId

	--clean up

	-- delete any unprocessed relations that were processed from unprocessed table
	DELETE sru
		FROM [fusion].[StagingRelationUnresolved] sru 
		INNER JOIN [fusion].[StagingRelation] sr
		  ON sru.startid = sr.startid and sru.endid = sr.endid and sr.executionid = @unprocessedRelationsExeId
		
	-- delete from staging relation any relations added
	delete from [fusion].[StagingRelation] where executionid = @unprocessedRelationsExeId

end
GO

alter TRIGGER [dbo].[Domain_AfterInsert]
   ON  [dbo].[Domain] 
   AFTER INSERT
AS
	SET NOCOUNT ON;
	INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
        select 'Add', [queue].WriteIndexXml('', 'Domain', ID, coalesce(UpdatedBy, 0)), 'Domain', ID from inserted
GO

ALTER PROCEDURE [dbo].[GetAllowedIntersectionTypes]
--declare 
	@SourceType varchar(250),
	@SourceTypeID int,
	@IntersectID int = 0
--set @SourceType = 'ArtifactType'--'FusionAttributeType'
--set @SourceTypeID = 1--213
--set @IntersectID = 40859
AS
BEGIN
	SET NOCOUNT ON;

	declare @tbl table (IntersectTypeID int, TargetType varchar(50), TargetTypeID int, TargetName nvarchar(500), ParentIntersectID int);

	insert into @tbl
		SELECT	RT.ID,
				case 
					when RT.Subject = @SourceType and RT.SubjectID = @SourceTypeID then RT.Object 
					else RT.Subject
				end AS TargetType,
				case 
					when RT.Subject = @SourceType and RT.SubjectID = @SourceTypeID then RT.ObjectID
					else RT.SubjectID
				end AS TargetTypeID,
				case 
					when RT.Subject = @SourceType and RT.SubjectID = @SourceTypeID then RT.ObjectName
					else RT.SubjectName
				end AS TargetName,
				NULL
		FROM	IntersectTypeDetail RT
		WHERE	(RT.Subject = @SourceType and RT.SubjectID = @SourceTypeID) OR 
				(RT.Object = @SourceType and RT.ObjectID = @SourceTypeID)

	if @IntersectID > 0
	begin
		select	@SourceType = 'IntersectType',
				@SourceTypeID = IntersectTypeID
		from	[Intersect]
		where	ID = @IntersectID;

		insert into @tbl
			SELECT		RT.ID,
						case 
							when RT.Subject = @SourceType and RT.SubjectID = @SourceTypeID then RT.Object 
							else RT.Subject
						end AS TargetType,
						case 
							when RT.Subject = @SourceType and RT.SubjectID = @SourceTypeID then RT.ObjectID
							else RT.SubjectID
						end AS TargetTypeID,
						case 
							when RT.Subject = @SourceType and RT.SubjectID = @SourceTypeID then RT.ObjectName
							else RT.SubjectName
						end AS TargetName,
						NULL
			FROM		IntersectTypeDetail RT
			WHERE	(RT.Subject = @SourceType and RT.SubjectID = @SourceTypeID) OR 
					(RT.Object = @SourceType and RT.ObjectID = @SourceTypeID)

		-- Now figure out if we need to remove any fusion relationship types based on ownership.
		select	top 1
				@SourceType = 'Artifact',
				@SourceTypeID = A.ID
		from	[Intersect] I
				inner join Artifact A on ( (A.ID = I.SubjectID and I.Subject = 'Artifact') OR (A.ID = I.ObjectID and I.Object = 'Artifact') ) and I.ID = @IntersectID
				inner join ArtifactType AT on AT.ID = A.ArtifactTypeID and AT.CanOwnFusion = 1

		delete	@tbl
		where	TargetType = 'FusionAttributeType'
				and TargetTypeID not in (
										select	T.ID
										from	FusionOwner FO
												inner join Fusion F on F.ID = FO.FusionID
												inner join FusionAttributeType T on T.FusionTypeID = F.FusionTypeID
										where	@SourceType = 'Artifact' and FO.ArtifactID = @SourceTypeID
										)
	end

	select		distinct
				IntersectTypeID, 
				TargetType, 
				TargetTypeID, 
				case TargetType
					when 'TaxonomyType' then 'Model: '
					when 'DomainType' then 'Reference: '
					when 'FusionType' then 'Fusion: '
					when 'FusionAttributeType' then 'Fusion: '
					when 'ArtifactType' then 'Glossary: '
					when 'RuleType' then 'Rules: '
					when 'PolicyType' then 'Policies: '
					else ''
				end + ' : ' + TargetName as TargetName, 
				ParentIntersectID
	from		@tbl 
	order by	case TargetType
					when 'TaxonomyType' then 'Model: '
					when 'DomainType' then 'Reference: '
					when 'FusionType' then 'Fusion: '
					when 'FusionAttributeType' then 'Fusion: '
					when 'ArtifactType' then 'Glossary: '
					when 'RuleType' then 'Rules: '
					when 'PolicyType' then 'Policies: '
					else ''
				end + ' : ' + TargetName
END
GO

alter procedure [dbo].[ProcessBulkLoad]
--declare
	@LoadID int
--set @LoadID = 29
as
begin
	set nocount on;

	declare @Object varchar(50),
			@ObjectID int,
			@Action varchar(1),
			@UpdatedBy int = 0

	select	@Object = [Object],
			@ObjectID = ObjectID,
			@Action = [Action],
			@UpdatedBy = UpdatedBy
	from	[Load]
	where	ID = @LoadID

	-- PARSE any dynamic fields that are specifically lookups.
	update	T
	set		T.LookupObject = S.LookupObject,
			T.LookupObjectID = S.LookupObjectID
	from	LoadItemColumn T
			inner join	(
						select	IC.LoadID,
								IC.RowIndex,
								IC.ColumnIndex,
								case 
									when L_A.ID is not null then 'Artifact'
									when L_D.ID is not null then 'Domain'
									when L_DI.ID is not null then 'DomainItem'
									when L_F.ID is not null then 'FusionAttribute'
									when L_I.ID is not null then 'Intersect'
									when L_L.Value is not null then 'Lookup'
									when L_T.ID is not null then 'Taxonomy'
									else NULL
								end as LookupObject,
								coalesce(L_A.ID, L_D.ID, L_DI.ID, L_F.ID, L_I.ID, L_L.Value, L_T.ID) as LookupObjectID
						from	FieldType F
								inner join [Load] L on L.ID = @LoadID and L.[Object] = F.[Object] and L.ObjectID = F.ObjectID and F.[Type] = 'Lookup'
								inner join [LoadColumn] C on C.LoadID = L.ID and F.Name = C.Name
								inner join [LoadItemColumn] IC on IC.LoadID = C.LoadID and IC.ColumnIndex = C.ColumnIndex
								
								left join Artifact L_A on F.LookupObjectType in ('Artifact', 'ArtifactType') and L_A.ArtifactTypeID = F.LookupObjectID and (L_A.[Name] = IC.Value OR L_A.TextPath = IC.Value)
								left join Domain L_D on F.LookupObjectType in ('Domain', 'DomainType') and L_D.DomainTypeID = F.LookupObjectID and L_D.[Name] = IC.Value
								left join DomainItem L_DI on F.LookupObjectType = 'DomainItem' and L_DI.DomainID = F.LookupObjectID and L_DI.[Name] = IC.Value
								left join FusionAttribute L_F on F.LookupObjectType = 'FusionAttributeType' and L_F.FusionAttributeTypeID = F.LookupObjectID and (L_F.[Name] = IC.Value OR L_F.TextPath = IC.Value)
								left join [Intersect] L_I on F.LookupObjectType = 'IntersectType' and L_I.IntersectTypeID = F.LookupObjectID and L_I.[Name] = IC.Value
								left join [FieldLookupValue] L_L on F.ID = L_L.FieldTypeID and F.LookupObjectType = 'Lookup' and L_L.LookupObjectID = F.LookupObjectID and L_L.[Text] = IC.Value
								left join Taxonomy L_T on F.LookupObjectType in ('Taxonomy', 'TaxonomyType') and L_T.TaxonomyTypeID = F.LookupObjectID and (L_T.[Name] = IC.Value OR L_T.TextPath = IC.Value)
						where	F.[Type] = 'Lookup'
						) S on S.LoadID = T.LoadID and S.RowIndex = T.RowIndex and S.ColumnIndex = T.ColumnIndex

		-- PARSE any Subject AREA fields.  This is only in the case of artifacts.
		update	T
		set		T.LookupObject = S.LookupObject,
				T.LookupObjectID = S.LookupObjectID
		from	LoadItemColumn T
				inner join	(
							select	IC.LoadID,
									IC.RowIndex,
									IC.ColumnIndex,
									'TaxonomyType' as LookupObject,
									T.ID as LookupObjectID
							from	[Load] L 
									inner join [LoadColumn] C on L.ID = @LoadID and L.[Object] = 'ArtifactType' and C.LoadID = L.ID and C.Name = 'Subject Area'
									inner join [LoadItemColumn] IC on IC.LoadID = C.LoadID and IC.ColumnIndex = C.ColumnIndex
									inner join TaxonomyType T on T.[Name] = IC.Value
							) S on S.LoadID = T.LoadID and S.RowIndex = T.RowIndex and S.ColumnIndex = T.ColumnIndex

		-- PARSE any Domain Group fields.  This is only in the case of domains.
		update	T
		set		T.LookupObject = S.LookupObject,
				T.LookupObjectID = S.LookupObjectID
		from	LoadItemColumn T
				inner join	(
							select	IC.LoadID,
									IC.RowIndex,
									IC.ColumnIndex,
									'DomainGroup' as LookupObject,
									T.ID as LookupObjectID
							from	[Load] L 
									inner join [LoadColumn] C on L.ID = @LoadID and L.[Object] = 'DomainType' and C.LoadID = L.ID and C.Name = 'Domain Group'
									inner join [LoadItemColumn] IC on IC.LoadID = C.LoadID and IC.ColumnIndex = C.ColumnIndex
									inner join DomainGroup T on T.[Name] = IC.Value and T.DomainTypeID = @ObjectID
							) S on S.LoadID = T.LoadID and S.RowIndex = T.RowIndex and S.ColumnIndex = T.ColumnIndex

		-- PARSE any Parent Artifact fields.  This is only in the case of artifacts.
		update	T
		set		T.LookupObject = S.LookupObject,
				T.LookupObjectID = S.LookupObjectID
		from	LoadItemColumn T
				inner join	(
							select	IC.LoadID,
									IC.RowIndex,
									IC.ColumnIndex,
									'Artifact' as LookupObject,
									P.ID as LookupObjectID
							from	[Load] L 
									inner join ArtifactType T on L.ID = @LoadID and L.[Object] = 'ArtifactType' and L.ObjectID = T.ID
									inner join ArtifactType PT on PT.ID = T.ParentID
									inner join [LoadColumn] C on C.LoadID = L.ID and C.Name = 'Parent ' + PT.Name
									inner join [LoadItemColumn] IC on IC.LoadID = C.LoadID and IC.ColumnIndex = C.ColumnIndex
									inner join Artifact P on P.ArtifactTypeID = PT.ID and (P.[TextPath] = IC.Value or P.[Name] = IC.Value)
							) S on S.LoadID = T.LoadID and S.RowIndex = T.RowIndex and S.ColumnIndex = T.ColumnIndex


	if @Action = 'P'	--PROMOTION
	begin
		if @Object = 'AttributeType'
		begin
			-- Clean Owner Type field.
			update	LoadItemColumn
			set		Value = case when charindex('Type', Value) > 0 then Value else Value + 'Type' end
			where	LoadID = @LoadID and ColumnIndex = 1

			-- PARSE Owner Type fields.
			update	T
			set		T.LookupObject = S.LookupObject,
					T.LookupObjectID = S.LookupObjectID
			from	LoadItemColumn T
					inner join	(
								select	LI.LoadID,
										LI.RowIndex,
										C2.ColumnIndex,
										D.[Object] as LookupObject,
										D.ObjectID as LookupObjectID
								from	[Load] L
										inner join LoadItem LI on LI.LoadID = L.ID and L.ID = @LoadID
										inner join [LoadItemColumn] C1 on C1.LoadID = LI.LoadID and C1.RowIndex = LI.RowIndex and C1.ColumnIndex = 1 --'Owner Type' 
										inner join [LoadItemColumn] C2 on C2.LoadID = LI.LoadID and C2.RowIndex = LI.RowIndex and C2.ColumnIndex = 2 --'Owner Type Name'
										inner join cache.ObjectDetails D on D.[Object] = C1.Value and D.[Name] = C2.Value
								) S on S.LoadID = T.LoadID and S.RowIndex = T.RowIndex and S.ColumnIndex = T.ColumnIndex

			-- PARSE Owner fields.
			update	T
			set		T.LookupObject = S.LookupObject,
					T.LookupObjectID = S.LookupObjectID
			from	LoadItemColumn T
					inner join	(
								select	LI.LoadID,
										LI.RowIndex,
										C3.ColumnIndex,
										D.[Object] as LookupObject,
										D.ObjectID as LookupObjectID
								from	[Load] L
										inner join LoadItem LI on LI.LoadID = L.ID and L.ID = @LoadID
										--inner join [LoadItemColumn] C1 on	C1.LoadID = LI.LoadID	and C1.RowIndex = LI.RowIndex	and C1.ColumnIndex = 1 --'Owner Type' 
										inner join [LoadItemColumn] C2 on C2.LoadID = LI.LoadID and C2.RowIndex = LI.RowIndex and C2.ColumnIndex = 2 --'Owner Type Name'
										inner join [LoadItemColumn] C3 on C3.LoadID = LI.LoadID	and C3.RowIndex = LI.RowIndex and C3.ColumnIndex = 3 --'Owner Name'
										inner join cache.ObjectDetails D on D.[ObjectType] = C2.[LookupObject] and D.ObjectTypeID = C2.LookupObjectID and D.[Name] = C3.Value
								) S on S.LoadID = T.LoadID and S.RowIndex = T.RowIndex and S.ColumnIndex = T.ColumnIndex
		end

		declare @ResolvedObjects table ([Object] varchar(50), ObjectID int, [Action] varchar(25), LoadID int, RowIndex int)	--This captures the INSERTED/UPDATED objects from the merge statements below.

		if @Object = 'ArtifactType'
		begin
			declare @RequiresParent bit
			select	@RequiresParent =		case
												when ParentID is null then cast(0 as bit)
												else cast(1 as bit)
											end
									  from	ArtifactType 
									  where	ID = @ObjectID

			merge	Artifact T
			using	(
					select	O.LoadID,
							O.RowIndex,
							O.ArtifactTypeID,
							O.Name,
							D.Description,
							O.ParentID,
							O.TaxonomyTypeID
					from	(
							select	LI.LoadID,
									MIN(LI.RowIndex) as RowIndex,
									@ObjectID as ArtifactTypeID,
									IC_N.Value as Name,
									P.ParentID,
									IC_T.LookupObjectID as TaxonomyTypeID
							from	[LoadItem] LI
									inner join [LoadItemColumn] IC_N on IC_N.LoadID = LI.LoadID and IC_N.RowIndex = LI.RowIndex inner join LoadColumn C_N on C_N.LoadID = LI.LoadID and C_N.ColumnIndex = IC_N.ColumnIndex and C_N.Name = 'Name'
									inner join [LoadItemColumn] IC_T on IC_T.LoadID = LI.LoadID and IC_T.RowIndex = LI.RowIndex inner join LoadColumn C_T on C_T.LoadID = LI.LoadID and C_T.ColumnIndex = IC_T.ColumnIndex and C_T.Name = 'Subject Area' and IC_T.LookupObjectID is not null
									outer apply (
												select	I.LookupObjectID as ParentID
												from	[LoadItemColumn] I
														inner join LoadColumn C on I.LoadID = LI.LoadID and I.RowIndex = LI.RowIndex 
																						and C.LoadID = LI.LoadID and C.ColumnIndex = I.ColumnIndex and C.Name like 'Parent %'
												) P
							where	LI.LoadID = @LoadID
									and (
											(@RequiresParent = 1 and P.ParentID is not null) or
											@RequiresParent = 0
										)
							group by LI.LoadID,
									IC_N.Value,
									P.ParentID,
									IC_T.LookupObjectID
							) O
							outer apply (
								select	I.Value as Description
								from	[LoadItemColumn] I
										inner join LoadColumn C on I.LoadID = O.LoadID and I.RowIndex = O.RowIndex 
																		and C.LoadID = O.LoadID and C.ColumnIndex = I.ColumnIndex and C.Name = 'Description'
							) D
					) S
			on		(T.ArtifactTypeID = S.ArtifactTypeID and T.TaxonomyTypeID = S.TaxonomyTypeID and ((T.ParentID = S.ParentID and S.ParentID is not null) or (T.ParentID is null and S.ParentID is null)) and T.Name = S.Name)
			when	matched then
					update	set T.[Description] = IsNull(S.[Description], T.[Description]),
								T.[ParentID] = S.[ParentID],
								T.[Status] = 'Draft',
								T.TaxonomyTypeID = S.TaxonomyTypeID,
								T.UpdatedBy = @UpdatedBy,
								T.UpdatedOn = getutcdate()
			when	not matched then
					insert (ArtifactTypeID, TaxonomyTypeID, ParentID, Name, [Description], [Status], UpdatedOn, UpdatedBy)
					values (S.ArtifactTypeID, S.TaxonomyTypeID, S.ParentID, S.Name, S.[Description], 'Draft', getutcdate(), @UpdatedBy)
			output	'Artifact', inserted.ID, $action, S.LoadID, S.RowIndex into @ResolvedObjects;

			--update	T
			--set		T.Name = T.Name
			--from	Artifact T
			--		inner join @ResolvedObjects S on S.ObjectID = T.ID and S.[Action] = 'INSERT';

			if @RequiresParent = 1
			begin
				-- Update the LoadItem table with the IDs we recieved in the merge statements above.
				update	T
				set		T.StatusMessage = 'Parent could not be found.'
				from	LoadItem T
						left join @ResolvedObjects S on S.LoadID = T.LoadID and S.RowIndex = T.RowIndex
				where	S.ObjectID is null
			end

		end
		else if @Object = 'AttributeType'
		begin
			merge	[Attribute] T
			using	(
					select	I.LoadID,
							I.RowIndex,
							@ObjectID as AttributeTypeID,
							C.LookupObject as [Object],
							C.LookupObjectID as ObjectID
					from	[LoadItem] I
							inner join [LoadItemColumn] C on I.LoadID = @LoadID and C.LoadID = I.LoadID and C.RowIndex = I.RowIndex and C.ColumnIndex = 3
							and C.LookupObject is not null
							and C.LookupObjectID is not null
					) S
			on		(T.AttributeTypeID = S.AttributeTypeID and T.[ObjectType] = S.[Object] and T.[ObjectID] = S.[ObjectID] and T.ParentID = NULL)-- and T.Name = S.Name)
			when	matched then
					update	set T.[UpdatedOn] = getutcdate(),
								T.UpdatedBy = @UpdatedBy
			when	not matched then
					insert (AttributeTypeID, ObjectType, ObjectID, UpdatedOn, UpdatedBy)
					values (S.AttributeTypeID, S.[Object], S.ObjectID, getutcdate(), @UpdatedBy)
			output	'Attribute', inserted.ID, $action, S.LoadID, S.RowIndex into @ResolvedObjects;		
		end
		else if @Object = 'Domain'
		begin
			merge	DomainItem T
			using	(
					select	distinct
							LI.LoadID,
							LI.RowIndex,
							@ObjectID as DomainID,
							IC_C.Value as Code,
							IC_N.Value as Name,
							D.[Description]
					from	[LoadItem] LI
							inner join [LoadItemColumn] IC_C on IC_C.LoadID = LI.LoadID and IC_C.RowIndex = LI.RowIndex inner join LoadColumn C_C on C_C.LoadID = LI.LoadID and C_C.ColumnIndex = IC_C.ColumnIndex and C_C.Name = 'Code'
							inner join [LoadItemColumn] IC_N on IC_N.LoadID = LI.LoadID and IC_N.RowIndex = LI.RowIndex inner join LoadColumn C_N on C_N.LoadID = LI.LoadID and C_N.ColumnIndex = IC_N.ColumnIndex and C_N.Name = 'Name'
							outer apply (
										select	I.Value as Description
										from	[LoadItemColumn] I
												inner join LoadColumn C on I.LoadID = LI.LoadID and I.RowIndex = LI.RowIndex 
																			 and C.LoadID = LI.LoadID and C.ColumnIndex = I.ColumnIndex and C.Name = 'Description'
										) D
					where	LI.LoadID = @LoadID
					) S
			on		(T.DomainID = S.DomainID and T.Code = S.Code)
			when	matched then
					update	set T.[Name] = S.[Name],
								T.[Description] = IsNull(S.[Description],T.[Description]),
								T.[DomainID] = S.[DomainID],
								T.UpdatedBy = @UpdatedBy,
								T.UpdatedOn = getutcdate()
			when	not matched then
					insert (DomainID, Code, Name, [Description], UpdatedOn, UpdatedBy)
					values (S.DomainID, S.Code, S.Name, S.[Description], getutcdate(), @UpdatedBy)
			output	'DomainItem', inserted.ID, $action, S.LoadID, S.RowIndex into @ResolvedObjects;
		end
		else if @Object = 'DomainType'
		begin
			merge	Domain T
			using	(
					select	distinct
							LI.LoadID,
							LI.RowIndex,
							@ObjectID as DomainTypeID,
							IC_N.Value as Name,
							D.[Description],
							IC_G.LookupObjectID as DomainGroupID
					from	[LoadItem] LI
							inner join [LoadItemColumn] IC_N on IC_N.LoadID = LI.LoadID and IC_N.RowIndex = LI.RowIndex inner join LoadColumn C_N on C_N.LoadID = LI.LoadID and C_N.ColumnIndex = IC_N.ColumnIndex and C_N.Name = 'Name'
							outer apply (
										select	I.Value as Description
										from	[LoadItemColumn] I
												inner join LoadColumn C on I.LoadID = LI.LoadID and I.RowIndex = LI.RowIndex 
																			 and C.LoadID = LI.LoadID and C.ColumnIndex = I.ColumnIndex and C.Name = 'Description'
										) D
							inner join [LoadItemColumn] IC_G on IC_G.LoadID = LI.LoadID and IC_G.RowIndex = LI.RowIndex inner join LoadColumn C_G on C_G.LoadID = LI.LoadID and C_G.ColumnIndex = IC_G.ColumnIndex and C_G.Name = 'Domain Group'
					where	LI.LoadID = @LoadID
					) S
			on		(T.DomainTypeID = S.DomainTypeID and T.Name = S.Name)
			when	matched then
					update	set T.[Description] = IsNull(S.[Description],T.[Description]),
								T.[DomainGroupID] = S.[DomainGroupID],
								T.UpdatedOn = getutcdate(),
								T.UpdatedBy = @UpdatedBy
			when	not matched then
					insert (DomainTypeID, DomainGroupID, Name, [Description], UpdatedOn, UpdatedBy)
					values (S.DomainTypeID, S.DomainGroupID, S.Name, S.[Description], getutcdate(), @UpdatedBy)
			output	'Domain', inserted.ID, $action, S.LoadID, S.RowIndex into @ResolvedObjects;
		end
		else if @Object = 'FusionAttributeType'
		begin
			select 1;
		end
		else if @Object = 'TaxonomyType'
		begin
		--begin tran

			declare @currentLevel int,
			@maxLevel int,
			@rowCount int,
			@rowCurr int;

			select 
				@currentLevel = 0
				,@maxLevel = max(
					case when isnumeric(replace(Name,'Level','')) = 1 then
						replace(Name,'Level','') 
					else 
						0 
					end) 
			from 
				LoadColumn 
			where 
				LoadID = @LoadID and Name like 'Level%';
			

			declare @levels table (id int, ColumnIndex int, RowIndex int, [Level] varchar(50), Value varchar(250),MaxLevel int, TaxonomyID int, ParentID int, [Status] varchar(50));
			with v as
			(
				select L.ID, L.Object, L.ObjectID, LC.Name, LC.ColumnIndex, IC.RowIndex, IC.Value, replace(LC.Name,'Level','') as [Level], T.ID as TaxonomyID from [Load] L
				join LoadColumn LC on LC.LoadID = L.ID
				join LoadItemColumn IC on IC.LoadID = LC.LoadID AND IC.ColumnIndex = LC.ColumnIndex
				left join Taxonomy T on T.TaxonomyTypeID = L.ObjectID and T.[Level] = replace(LC.Name,'Level','') and T.Name = IC.Value
				where L.ID = @LoadID AND ltrim(rtrim(IC.Value)) != '' and LC.Name like 'Level%'  
			)
			insert into @levels
			select distinct
				row_number() over (partition by 1 order by v.[Level]) as ID,
				v.ColumnIndex
				,v.RowIndex
				,v.[Level]
				,v.Value
				,m.[Level] as MaxLevel
				,v.TaxonomyID
				,p.TaxonomyID as ParentID 
				,'UPDATE' as [Status]
			from v
			left join v p 
				on p.RowIndex = v.RowIndex and v.TaxonomyID is null and p.ColumnIndex = (v.ColumnIndex - 1)
			inner join v m on m.RowIndex = v.RowIndex and m.[Level] = (select max([Level]) from v where RowIndex = m.RowIndex)
			order by v.[Level] asc;

			--calculate hierarchy
			while @currentLevel <= @maxLevel
			begin
				set @currentLevel = @currentLevel + 1;
				
				update LV
				set LV.ParentID = P.ID
				from @levels LV
				left join @levels P on P.[Level] = (LV.[Level] - 1) AND LV.RowIndex = P.RowIndex
				where LV.[Level] = @currentLevel;
			end 

			--delete records that have a level > 1 and no parentid, missing info
			--delete from @levels where parentid is null and level > 1;

			select @rowCurr = 0, @rowCount = count(*) from @levels;

			while @rowCurr <= @rowCount
			begin
				set @rowCurr = @rowCurr + 1;

				--parent does not exist or leading columns were not filled
				if (select ParentID from @levels where id = @rowCurr) IS NULL AND (select Level from @levels where id = @rowCurr) > 1
				begin
					update @levels set [Status] = 'ERROR' where rowIndex = (select rowindex from @levels where id = @rowCurr);
					continue;
				end


				--update the TaxonomyID for records that do not yet have it
				if (select level from @levels where id = @rowCurr) = 1
				begin
					update LV
					set TaxonomyID = T.ID
					from @levels LV
					join Load L on L.ID = @LoadID
					join Taxonomy T on T.Name = LV.Value and T.ParentID is NULL and T.Level = LV.Level and T.TaxonomyTypeID = L.ObjectID
					where LV.ID = @rowCurr;
				end
				else
				begin
					update LV
					set TaxonomyID = T.ID
					from @levels LV
					left join @levels P on P.ID = LV.ParentID
					join Taxonomy T on T.Name = LV.Value and T.ParentID = P.TaxonomyID and T.Level = LV.Level
					where LV.ID = @rowCurr;
				end

				if (select TaxonomyID from @levels where id = @rowCurr) IS NULL
				begin
					--insert the new taxonomy
					insert into Taxonomy (TaxonomyTypeID, ParentID, Name, [Description], UpdatedOn, UpdatedBy)
					select	distinct
							L.ObjectID as TaxonomyTypeID
						,LVP.TaxonomyID as ParentID
						,LV.Value as Name
						,case when LV.Level = LV.MaxLevel then
							LI.Value
						else
							''
						END as Description
						,getdate() as UpdatedOn
						,@UpdatedBy as UpdatedBy
					from 
						@levels LV
					left join @levels LVP on LVP.ID = LV.ParentID
					join [Load] L on L.ID = @LoadID
					inner join LoadColumn LC on LC.Name = 'Description' and LC.LoadID = @LoadID
					inner join LoadItemColumn LI on LI.RowIndex = LV.RowIndex AND LI.ColumnIndex = LC.ColumnIndex AND LI.LoadID = @LoadID
					where
						LV.ID = @rowCurr

					update @levels set [Status] = 'INSERT' where id = @rowCurr;

					--set the levels taxonomy id after insert
					update LV
					set TaxonomyID = T.ID
					from @levels LV
					left join @levels P on P.ID = LV.ParentID
					join Taxonomy T on T.Name = LV.Value and coalesce(T.ParentID,-1) = coalesce(P.TaxonomyID,-1) and T.Level = LV.Level
					where LV.ID = @rowCurr;
				end
				
				--if level = max, update the description
				if (select level from @levels where id = @rowCurr) = (select maxlevel from @levels where id = @rowCurr)
				begin
					update	T
					set		T.Description = case when LI.Value = '' then T.Description else LI.Value end,
							T.UpdatedOn = getutcdate(),
							T.UpdatedBy = @UpdatedBy
					from	Taxonomy T
							join @levels LV on LV.ID = @rowCurr and T.ID = LV.TaxonomyID
							inner join LoadColumn LC on LC.Name = 'Description' and LC.LoadID = @LoadID
							inner join LoadItemColumn LI on LI.RowIndex = LV.RowIndex AND LI.ColumnIndex = LC.ColumnIndex AND LI.LoadID = @LoadID;

				end
			end --end while
			

			--remove error rows
			delete from @levels
			where rowindex in (select rowindex from @levels where status is null or status = 'ERROR');

						--insert object statuses
			insert into @ResolvedObjects ([Object], ObjectID, [Action], LoadID, RowIndex)
			select
				'Taxonomy',
				TaxonomyID,
				[Status],
				@LoadID,
				RowIndex
			from 
			@levels;

		end

		-- Update the LoadItem table with the IDs we recieved in the merge statements above.
		update	T
		set		T.[Object] = S.[Object],
				T.ObjectID = S.ObjectID,
				T.[Status] = 1,
				T.StatusMessage = case S.[Action]
									when 'INSERT' then 'Added item'
									when 'UPDATE' then 'Updated item'
									else NULL
									end
		from	LoadItem T
				inner join	@ResolvedObjects S on S.LoadID = T.LoadID and S.RowIndex = T.RowIndex

		-- Update the LoadItems that were not successfully added or updated.
		update	LoadItem
		set		[Status] = 0,
				[StatusMessage] = coalesce([StatusMessage], '') + ' Item could not be added nor updated.'
		where	LoadID = @LoadID
				and [ObjectID] is null
	end
	else
	begin
		-- This is for actions: R, U, L
		declare @current int,
				@max int,
				@sourceObject varchar(50),
				@sourceObjectID int,
				@targetObject varchar(50),
				@targetObjectID int,
				@intersectID int = null,
				@date datetime = getutcdate()

		declare @Intersects IDTable

		declare @sourceObjectTypeName nvarchar(1000),
				@sourceSubject nvarchar(500),
				@sourceName nvarchar(500),
					
				@targetObjectTypeName nvarchar(1000),
				@targetSubject nvarchar(500),
				@targetName nvarchar(500),
				
				@predicateID int,
				@rundate datetime = CURRENT_TIMESTAMP

		if @Action = 'L' -- LINEAGE (create lineage from input spreadsheet)
		begin
			declare @focalObject varchar(50),
					@focalObjectID int,
					@focalObjectTypeName nvarchar(1000),
					@focalName nvarchar(500),
					@intersectPredicate varchar(50),
					@focalIntersectID int,
					@focalSubject nvarchar(500),
					@lineageErrorDetailMessage varchar(200)
			
			select	@current = min(I.RowIndex),
					@max = max(I.RowIndex)
			from	LoadItem I
					inner join LoadItemColumn FT on FT.LoadID = I.LoadID and FT.RowIndex = I.RowIndex and FT.ColumnIndex = 1  --focal point object type
						inner join LoadItemColumn FTN on FTN.LoadID = I.LoadID and FTN.RowIndex = I.RowIndex and FTN.ColumnIndex = 2   --focal point object type name
						inner join LoadItemColumn F on F.LoadID = I.LoadID and F.RowIndex = I.RowIndex and F.ColumnIndex = 4--focal point name		
						inner join LoadItemColumn ST on ST.LoadID = I.LoadID and ST.RowIndex = I.RowIndex and St.ColumnIndex = 5 --source object type
						inner join LoadItemColumn STN on STN.LoadID = I.LoadID and STN.RowIndex = I.RowIndex and StN.ColumnIndex = 6 --source object type name
						inner join LoadItemColumn S on S.LoadID = I.LoadID and S.RowIndex = I.RowIndex and S.ColumnIndex = 8 --source object name
						inner join LoadItemColumn TT on TT.LoadID = I.LoadID and TT.RowIndex = I.RowIndex and TT.ColumnIndex = 9 --target object type
						inner join LoadItemColumn TTN on TTN.LoadID = I.LoadID and TTN.RowIndex = I.RowIndex and TTN.ColumnIndex = 10 --target object type name
						inner join LoadItemColumn T on T.LoadID = I.LoadID and T.RowIndex = I.RowIndex and T.ColumnIndex = 12 --source object name
						inner join LoadItemColumn P on P.LoadID = I.LoadID and P.RowIndex = I.RowIndex and P.ColumnIndex = 13 --predicate
			where	I.LoadID = @LoadID
			
			-- go row by row
			while @current <= @max
			begin
				--load the objects / id's for the focal, source, and target objects
				select	@focalObject = FT.Value,
						@focalObjectTypeName = FTN.Value,
						@focalName = F.Value,
						@focalSubject = FS.Value,
						@sourceObject = ST.Value,
						@sourceObjectTypeName = STN.Value,
						@sourceName = S.Value,
						@sourceSubject = SS.Value,
						@targetObject = TT.Value,
						@targetObjectTypeName = TTN.Value,
						@targetName = T.Value,
						@targetSubject = TS.Value,
						@intersectPredicate = P.Value
				from	LoadItem I
						inner join LoadItemColumn FT on FT.LoadID = I.LoadID and FT.RowIndex = I.RowIndex and FT.ColumnIndex = 1  --focal point object type
						inner join LoadItemColumn FTN on FTN.LoadID = I.LoadID and FTN.RowIndex = I.RowIndex and FTN.ColumnIndex = 2  --focal point object type name
						inner join LoadItemColumn FS on FS.LoadID = I.LoadID and FS.RowIndex = I.RowIndex and FS.ColumnIndex = 3 --focal point subject area		
						inner join LoadItemColumn F on F.LoadID = I.LoadID and F.RowIndex = I.RowIndex and F.ColumnIndex = 4 --focal point name		
						inner join LoadItemColumn ST on ST.LoadID = I.LoadID and ST.RowIndex = I.RowIndex and St.ColumnIndex = 5 --source object type
						inner join LoadItemColumn STN on STN.LoadID = I.LoadID and STN.RowIndex = I.RowIndex and StN.ColumnIndex = 6 --source object type name
						inner join LoadItemColumn SS on SS.LoadID = I.LoadID and SS.RowIndex = I.RowIndex and SS.ColumnIndex = 7 --source object subject
						inner join LoadItemColumn S on S.LoadID = I.LoadID and S.RowIndex = I.RowIndex and S.ColumnIndex = 8 --source object name
						inner join LoadItemColumn TT on TT.LoadID = I.LoadID and TT.RowIndex = I.RowIndex and TT.ColumnIndex = 9 --target object type
						inner join LoadItemColumn TTN on TTN.LoadID = I.LoadID and TTN.RowIndex = I.RowIndex and TTN.ColumnIndex = 10 --target object type name
						inner join LoadItemColumn TS on TS.LoadID = I.LoadID and TS.RowIndex = I.RowIndex and TS.ColumnIndex = 11 --target object subject
						inner join LoadItemColumn T on T.LoadID = I.LoadID and T.RowIndex = I.RowIndex and T.ColumnIndex = 12 --source object name
						inner join LoadItemColumn P on P.LoadID = I.LoadID and P.RowIndex = I.RowIndex and P.ColumnIndex = 13 --predicate
				where	I.LoadID = @LoadID and I.RowIndex = @current

				select @focalObjectID = 0, @sourceObjectID = 0, @targetObjectID = 0, @predicateID = 0;

				select @predicateID = id from predicate where name = @intersectPredicate;				

				-- load focal object
				if @focalObject = 'Artifact'
				begin
					select top 1
						@focalObjectID = cod.objectid										
					from 
						[cache].objectdetails cod
						inner join artifact a on (cod.objectid = a.id)
						inner join taxonomytype t on (a.taxonomytypeid = t.id)
					where 
						cod.[object] = @focalObject and cod.textpath = @focalName and cod.objecttypename = @focalObjectTypeName and t.Name = @focalSubject
				end
				else
				begin
					select top 1
							@focalObjectID = cod.objectid										
					from 
						[cache].objectdetails cod
					where 
						cod.[object] = @focalObject and cod.textpath = @focalName and cod.objecttypename = @focalObjectTypeName
				end

				if @sourceObject = 'Artifact'
				begin
					select top 1
						@sourceObjectID = cod.objectid										
					from 
						[cache].objectdetails cod
						inner join artifact a on (cod.objectid = a.id)
						inner join taxonomytype t on (a.taxonomytypeid = t.id)
					where 
						cod.[object] = @sourceObject and cod.textpath = @sourceName and cod.objecttypename = @sourceObjectTypeName and t.Name = @sourceSubject
				end
				else
				begin
					-- load source object
					select top 1
							@sourceObjectID = cod.objectid						
					from 
						[cache].objectdetails cod
					where 
						cod.[object] = @sourceObject and cod.textpath = @sourceName and cod.objecttypename = @sourceObjectTypeName
				end

				if @targetObject = 'Artifact'
				begin
					-- load target object
					select top 1
							@targetObjectID = cod.objectid												
					from 
						[cache].objectdetails cod
						inner join artifact a on (cod.objectid = a.id)
						inner join taxonomytype t on (a.taxonomytypeid = t.id)
					where 
						cod.[object] = @targetObject and cod.textpath = @targetName and cod.objecttypename = @targetObjectTypeName and t.Name = @targetSubject
				end
				else
				begin
					-- load target object
					select top 1
							@targetObjectID = cod.objectid												
					from 
						[cache].objectdetails cod
					where 
						cod.[object] = @targetObject and cod.textpath = @targetName and cod.objecttypename = @targetObjectTypeName
				end

				--debug 
				--select @focalObjectID, @focalObject, @sourceObjectID, @sourceObject, @targetObjectID, @targetObject, @predicateID

				--if all are provided we are good otherwise error
				if @focalObjectID > 0 and @sourceObjectID > 0 and @targetObjectID > 0 and @predicateID > 0
					begin

					-- add intersect between focal object and source if one doesnt exist					
					exec [dbo].[AddRelationship] @UpdatedBy,@rundate,@focalObject,@focalObjectID,2,null,null,@sourceObject,@sourceObjectID;

					-- add intersect between focal object and target if one doesnt exist
					exec [dbo].[AddRelationship] @UpdatedBy,@rundate,@focalObject,@focalObjectID,2,null,null,@targetObject,@targetObjectID;
					
					-- add intersect between source / target if one doesnt exist
					exec [dbo].[AddRelationship] @UpdatedBy,@rundate,@sourceObject,@sourceObjectID,2,null,null,@targetObject,@targetObjectID;

					-- add intersect map between source / target if one doesnt exist for source to target intersect
					if not exists (select 1 from intersectmap map
							inner join intersectnode node1 on ( map.subjectintersectnodeid = node1.id and node1.objectid = @sourceObjectID and node1.objecttype = @sourceObject)
							inner join intersectnode node2 on ( map.objectintersectnodeid = node2.id and node2.objectid = @targetObjectID and node2.objecttype = @targetObject)
						where map.[type] = 1)
						begin							
							insert into intersectmap
								select 
									node1.ID as SubjectIntersectNode,
									node2.ID as ObjectIntersectNode,
									@predicateID as PredicateID,
									1 as [Type]
								from						
									intersectnode node1 
									inner join intersectnode node2 on (node1.objectid = @sourceObjectID and node1.objecttype =@sourceObject and node2.objectid = @targetObjectID and node2.objecttype = @targetObject and node1.intersectid = node2.intersectid);
						end


						update	LoadItem
						set		[Status] = 1,
								StatusMessage = 'Successfully added item to lineage'
						where	LoadID = @LoadID
								and RowIndex = @current
					end -- if valid
				else
					begin
						set @lineageErrorDetailMessage = '';

						if @focalObjectID = 0
						begin
							set @lineageErrorDetailMessage = '  Focal point is invalid.';
						end

						if @sourceObjectID = 0
						begin
							set @lineageErrorDetailMessage = @lineageErrorDetailMessage + '  Source object is invalid.';
						end

						if @targetObjectID = 0
						begin
							set @lineageErrorDetailMessage = @lineageErrorDetailMessage + '  Target object is invalid.';
						end

						update	LoadItem
						set		[Status] = 0,
								StatusMessage = 'Failed to add item to lineage.' + @lineageErrorDetailMessage + ' [focal id:' + convert(varchar(10), @focalObjectID) + ' type:' + @focalObject + '] [source id:' + convert(varchar(10),@sourceObjectID) + ' type:' + @sourceObject +'] [target id:' + convert(varchar(10), @targetObjectID) + ' type:' + @targetObject + ']'
						where	LoadID = @LoadID
								and RowIndex = @current
					end -- else not valid
				
				set @current = @current + 1
			end

		end

		if @Action = 'S' -- SYNONYM (create synonyms from input spreadsheet)
		begin
			declare @synonymErrorDetailMessage varchar(200)
			
			select	@current = min(I.RowIndex),
					@max = max(I.RowIndex)
			from	LoadItem I
					inner join LoadItemColumn ST on ST.LoadID = I.LoadID and ST.RowIndex = I.RowIndex and St.ColumnIndex = 1			-- source object type
					inner join LoadItemColumn STN on STN.LoadID = I.LoadID and STN.RowIndex = I.RowIndex and StN.ColumnIndex = 2		-- source object type name
					inner join LoadItemColumn S on S.LoadID = I.LoadID and S.RowIndex = I.RowIndex and S.ColumnIndex = 4				-- source object name
					inner join LoadItemColumn TT on TT.LoadID = I.LoadID and TT.RowIndex = I.RowIndex and TT.ColumnIndex = 5			-- target object type
					inner join LoadItemColumn TTN on TTN.LoadID = I.LoadID and TTN.RowIndex = I.RowIndex and TTN.ColumnIndex = 6		-- target object type name
					inner join LoadItemColumn T on T.LoadID = I.LoadID and T.RowIndex = I.RowIndex and T.ColumnIndex = 8				-- target object name
			where	I.LoadID = @LoadID
			
			-- go row by row
			while @current <= @max
			begin
				--load the objects / id's for the focal, source, and target objects
				select	@sourceObject = ST.Value,
						@sourceObjectTypeName = STN.Value,
						@sourceName = S.Value,
						@sourceSubject = SS.Value,
						
						@targetObject = TT.Value,
						@targetObjectTypeName = TTN.Value,
						@targetName = T.Value,
						@targetSubject = TS.Value
				from	LoadItem I
						inner join LoadItemColumn ST on ST.LoadID = I.LoadID and ST.RowIndex = I.RowIndex and St.ColumnIndex = 1		-- source object type
						inner join LoadItemColumn STN on STN.LoadID = I.LoadID and STN.RowIndex = I.RowIndex and StN.ColumnIndex = 2	-- source object type name
						inner join LoadItemColumn SS on SS.LoadID = I.LoadID and SS.RowIndex = I.RowIndex and SS.ColumnIndex = 3		-- source object subject
						inner join LoadItemColumn S on S.LoadID = I.LoadID and S.RowIndex = I.RowIndex and S.ColumnIndex = 4			-- source object name
						inner join LoadItemColumn TT on TT.LoadID = I.LoadID and TT.RowIndex = I.RowIndex and TT.ColumnIndex = 5		-- target object type
						inner join LoadItemColumn TTN on TTN.LoadID = I.LoadID and TTN.RowIndex = I.RowIndex and TTN.ColumnIndex = 6	-- target object type name
						inner join LoadItemColumn TS on TS.LoadID = I.LoadID and TS.RowIndex = I.RowIndex and TS.ColumnIndex = 7		-- target object subject
						inner join LoadItemColumn T on T.LoadID = I.LoadID and T.RowIndex = I.RowIndex and T.ColumnIndex = 8			-- target object name
				where	I.LoadID = @LoadID and I.RowIndex = @current

				select @sourceObjectID = 0, @targetObjectID = 0, @predicateID = 0;

				select @predicateID = min(ID) from [Predicate] where [Type] = 6;				

				if @sourceObject = 'Artifact'
				begin
					select	top 1
							@sourceObjectID = cod.objectid										
					from	[cache].objectdetails cod
							inner join artifact a on (cod.objectid = a.id)
							inner join taxonomytype t on (a.taxonomytypeid = t.id)
					where	cod.[object] = @sourceObject and cod.textpath = @sourceName and cod.objecttypename = @sourceObjectTypeName and t.Name = @sourceSubject
				end
				else
				begin
					-- load source object
					select	top 1
							@sourceObjectID = cod.objectid						
					from	[cache].objectdetails cod
					where	cod.[object] = @sourceObject and cod.textpath = @sourceName and cod.objecttypename = @sourceObjectTypeName
				end

				if @targetObject = 'Artifact'
				begin
					-- load target object
					select	top 1
							@targetObjectID = cod.objectid												
					from	[cache].objectdetails cod
							inner join artifact a on (cod.objectid = a.id)
							inner join taxonomytype t on (a.taxonomytypeid = t.id)
					where	cod.[object] = @targetObject and cod.textpath = @targetName and cod.objecttypename = @targetObjectTypeName and t.Name = @targetSubject
				end
				else
				begin
					-- load target object
					select	top 1
							@targetObjectID = cod.objectid												
					from	[cache].objectdetails cod
					where	cod.[object] = @targetObject and cod.textpath = @targetName and cod.objecttypename = @targetObjectTypeName
				end

				--debug 
				--select @sourceObjectID, @sourceObject, @targetObjectID, @targetObject, @predicateID

				--if all are provided we are good otherwise error
				if @sourceObjectID > 0 and @targetObjectID > 0 and @predicateID > 0
					begin

					-- add intersect between source / target if one doesnt exist
					exec [dbo].[AddRelationship] @UpdatedBy, @rundate, @sourceObject, @sourceObjectID, 2, null, null, @targetObject, @targetObjectID;

					-- add intersect map between source / target if one doesnt exist for source to target intersect
					if not exists (
							select	1 
							from	intersectmap map
									inner join intersectnode node1 on ( map.subjectintersectnodeid = node1.id and node1.objectid = @sourceObjectID and node1.objecttype = @sourceObject)
									inner join intersectnode node2 on ( map.objectintersectnodeid = node2.id and node2.objectid = @targetObjectID and node2.objecttype = @targetObject)
						where map.[type] = 6)
						begin							
							insert into intersectmap
								select 
									node1.ID as SubjectIntersectNode,
									node2.ID as ObjectIntersectNode,
									@predicateID as PredicateID,
									6 as [Type]
								from						
									intersectnode node1 
									inner join intersectnode node2 on (node1.objectid = @sourceObjectID and node1.objecttype =@sourceObject and node2.objectid = @targetObjectID and node2.objecttype = @targetObject and node1.intersectid = node2.intersectid);
						end

						update	LoadItem
						set		[Status] = 1,
								StatusMessage = 'Successfully added synonym'
						where	LoadID = @LoadID
								and RowIndex = @current
					end -- if valid
				else
					begin
						set @synonymErrorDetailMessage = '';

						if @sourceObjectID = 0
						begin
							set @synonymErrorDetailMessage = @synonymErrorDetailMessage + '  Source object is invalid.';
						end

						if @targetObjectID = 0
						begin
							set @synonymErrorDetailMessage = @synonymErrorDetailMessage + '  Target object is invalid.';
						end

						if @predicateID = 0
						begin
							set @synonymErrorDetailMessage = @synonymErrorDetailMessage + '  No predicate of type synonym.';
						end

						update	LoadItem
						set		[Status] = 0,
								StatusMessage = 'Failed to add synonym. ' + @synonymErrorDetailMessage + ' [source id:' + convert(varchar(10),@sourceObjectID) + ' type:' + @sourceObject +'] [target id:' + convert(varchar(10), @targetObjectID) + ' type:' + @targetObject + ']'
						where	LoadID = @LoadID
								and RowIndex = @current
					end -- else not valid
				
				set @current = @current + 1
			end

		end

		if @Action = 'R' OR @Action = 'U'	--UNRELATION (Remove existing relation)
		begin
			-- PARSE both sides.
			update	T
			set		T.LookupObject = S.LookupObject,
					T.LookupObjectID = S.LookupObjectID
			from	LoadItemColumn T
					inner join	(
								select	IC.LoadID,
										IC.RowIndex,
										IC.ColumnIndex,
										T.[Object] as LookupObject,
										T.ObjectID as LookupObjectID
								from	[Load] L
										inner join [LoadColumn] C on C.LoadID = L.ID and L.ID = @LoadID
										inner join [LoadItemColumn] IC on IC.LoadID = C.LoadID and IC.ColumnIndex = C.ColumnIndex
										inner join IntersectTypeNode IT on IT.IntersectTypeID = @ObjectID and IT.[Order] = IC.[ColumnIndex]
										inner join cache.ObjectDetails T on (T.[TextPath] = IC.Value or T.Name = IC.Value) and T.[ObjectType] = IT.[ObjectType] and T.ObjectTypeID = IT.ObjectID
								) S on S.LoadID = T.LoadID and S.RowIndex = T.RowIndex and S.ColumnIndex = T.ColumnIndex
			update	T
			set		T.[Status] = 0,
					T.StatusMessage =	REPLACE(REPLACE(
											STUFF(
											(
											select	LIC.Value + ' could not be located in the <a href="' + T.Url + '">' + T.Name + '</a> list, '
											from	[Load] L
													inner join [IntersectTypeNode] ITN on ITN.IntersectTypeID = L.ObjectID and L.ID = @LoadID
													inner join [LoadItemColumn] LIC on LIC.LoadID = L.ID and LIC.ColumnIndex = ITN.[Order] and LIC.ColumnIndex = IC.ColumnIndex and LIC.RowIndex = IC.RowIndex and LIC.LookupObject is null
													inner join cache.ObjectDetails T on T.[Object] = ITN.[ObjectType] and T.ObjectID = ITN.ObjectID
											for xml path('')
											), 1, 0, ''),
										'&lt;', '<'), '&gt;', '>')
			from	[LoadItem] T
					inner join [LoadItemColumn] IC on T.LoadID = @LoadID and IC.LoadID = T.LoadID and IC.RowIndex = T.RowIndex and IC.LookupObject IS NULL and IC.LookupObjectID is null

			select	@current = min(I.RowIndex),
					@max = max(I.RowIndex)
			from	LoadItem I
					inner join LoadItemColumn S on S.LoadID = I.LoadID and S.RowIndex = I.RowIndex and S.ColumnIndex = 1 and S.LookupObject is not null
					inner join LoadItemColumn T on T.LoadID = I.LoadID and T.RowIndex = I.RowIndex and T.ColumnIndex = 2 and T.LookupObject is not null
			where	I.LoadID = @LoadID



		end

		while @current <= @max
		begin
			select	@sourceObject = S.LookupObject,
					@sourceObjectID = S.LookupObjectID,
					@targetObject = T.LookupObject,
					@targetObjectID = T.LookupObjectID
			from	LoadItem I
					inner join LoadItemColumn S on S.LoadID = I.LoadID and S.RowIndex = I.RowIndex and S.ColumnIndex = 1 and S.LookupObject is not null
					inner join LoadItemColumn T on T.LoadID = I.LoadID and T.RowIndex = I.RowIndex and T.ColumnIndex = 2 and T.LookupObject is not null
			where	I.LoadID = @LoadID and I.RowIndex = @current

			set		@intersectID = null

			select	@IntersectID = ID 
			from	[Intersect]
			where	(Subject = @sourceObject and SubjectID = @sourceObjectID and Object = @targetObject and ObjectID = @targetObjectID) OR
					(Object = @sourceObject and ObjectID = @sourceObjectID and Subject = @targetObject and SubjectID = @targetObjectID)

			if @Action = 'R'	--RELATION
			begin
				if @intersectID is null
				begin
					insert into [Intersect] (IntersectTypeID, Classification, Subject, SubjectID, Object, ObjectID, CreatedBy, CreatedOn, UpdatedBy, UpdatedOn) 
					values		(@ObjectID, 2, @sourceObject, @sourceObjectID, @targetObject, @targetObjectID, 0, @date, 0, @date)

					set @intersectID = SCOPE_IDENTITY()

					exec utility.AddAuditEntry @sourceObject, @sourceObjectID, 0, @date, 'Created', 'Intersect', @intersectID
					exec utility.AddAuditEntry @targetObject, @targetObjectID, 0, @date, 'Created', 'Intersect', @intersectID
				end

				if @intersectID is not null
				begin
					update	LoadItem
					set		[Object] = 'Intersect',
							ObjectID = @intersectID,
							[Status] = 1,
							StatusMessage = 'Successfully created/updated relationship'
					where	LoadID = @LoadID
							and RowIndex = @current
				end
				else
				begin
					update	LoadItem
					set		[Status] = 0,
							StatusMessage = 'Failed to create relationship'
					where	LoadID = @LoadID
							and RowIndex = @current
				end
			end --end R

			if @Action = 'U'	--UNRELATION
			begin
				if @intersectID is not null
				begin
					begin try
						if exists(	select 1 
									from	MapItem
									where	SourceIntersectID = @intersectID or TargetIntersectID = @intersectID
								 )
						begin
							update	LoadItem
							set		[Object] = 'Intersect',
									ObjectID = @intersectID,
									[Status] = 0,
									StatusMessage = 'Unable to remove relationship as it is involved in lineage.'
							where	LoadID = @LoadID
									and RowIndex = @current
						end
						else
						begin
							delete [Intersect] where ID = @intersectID

							update	LoadItem
							set		[Object] = 'Intersect',
									ObjectID = @intersectID,
									[Status] = 1,
									StatusMessage = 'Successfully removed relationship'
							where	LoadID = @LoadID
									and RowIndex = @current
						end
					end try
					begin catch
							update	LoadItem
							set		[Object] = 'Intersect',
									ObjectID = @intersectID,
									[Status] = 0,
									StatusMessage = 'Unable to remove relationship due to the following error: ' + ERROR_MESSAGE()
							where	LoadID = @LoadID
									and RowIndex = @current
					end catch
				end
				else
				begin
					update	LoadItem
					set		[Object] = 'Intersect',
							ObjectID = NULL,
							[Status] = 0,
							StatusMessage = 'Relationship not found'
					where	LoadID = @LoadID
							and RowIndex = @current
				end
			end --end U

			insert into @Intersects values (@intersectID)

			set @current = @current + 1
		end

		if @Action = 'R'
		begin
			exec cache.SynchronizeRelationships @Intersects
		end

	end --end IF statement to check if action = P or NOT

	if @Action = 'P' or @Action = 'R'
	begin
		-- Load custom fields for the inserted/updated object above.
		merge	Field T
		using	(
				select	distinct
						FT.ID as FieldTypeID,
						L.[Object],
						L.ObjectID,
						IC.LookupObjectID--max(IC.LookupObjectID) as LookupObjectID
				from	LoadItem L
						inner join LoadColumn C on C.LoadID = L.LoadID
						inner join LoadItemColumn IC on IC.LoadID = C.LoadID and L.RowIndex = IC.RowIndex and IC.ColumnIndex = C.ColumnIndex and IC.LookupObjectID is not null
						inner join FieldType FT on FT.[Object] = @Object and FT.ObjectID = @ObjectID and FT.Name = C.Name
				where	L.ObjectID is not null
						and L.LoadID = @LoadID
				--group by	FT.ID,
				--			L.[Object],
				--			L.ObjectID
				) S
		on		(T.FieldTypeID = S.FieldTypeID and T.ObjectType = S.[Object] and T.ObjectID = S.ObjectID)
		when	matched then
				update	set Value = S.LookupObjectID
		when	not matched then
				insert (ObjectType, ObjectID, FieldTypeID, Value)
				values (S.[Object], S.ObjectID, S.FieldTypeID, S.LookupObjectID);

		merge	Field T
		using	(
				select	distinct
						FT.ID as FieldTypeID,
						L.[Object],
						L.ObjectID,
						case 
							when FT.[Type] = 'Boolean' and LOWER(IC.Value) in ('y', 'yes', 'true', 't', '1') then 'true'
							when FT.[Type] = 'Boolean' and LOWER(IC.Value) not in ('y', 'yes', 'true', 't', '1') then 'false'
							else IC.Value
						end as Value
				from	LoadItem L
						inner join LoadColumn C on C.LoadID = L.LoadID
						inner join LoadItemColumn IC on IC.LoadID = C.LoadID and L.RowIndex = IC.RowIndex and IC.ColumnIndex = C.ColumnIndex and IC.LookupObjectID is null
						inner join FieldType FT on FT.[Object] = @Object and FT.ObjectID = @ObjectID and FT.Name = C.Name and FT.[Type] <> 'Lookup'
				where	L.ObjectID is not null
						and L.LoadID = @LoadID
				) S
		on		(T.FieldTypeID = S.FieldTypeID and T.ObjectType = S.[Object] and T.ObjectID = S.ObjectID)
		when	matched then
				update	set Value = S.Value
		when	not matched then
				insert (ObjectType, ObjectID, FieldTypeID, Value)
				values (S.[Object], S.ObjectID, S.FieldTypeID, S.Value);
	end

	update	[Load] 
	set		DateCompleted = getutcdate()
	where	ID = @LoadID
end
GO


ALTER PROCEDURE [dbo].[ProcessEagleMCToEagleFieldRelations]
	@StagingFileID int,
	@FusionID int
AS
BEGIN	
	SET NOCOUNT ON;
		
	declare	@eagleStreamID int,
			@streamToFieldIntersectTypeID int,				
			@streamSourceIntersectTypeNodeID int,
			@streamTargetIntersectTypeNodeID int,
			@currentEagleFusionId int;

	declare	@IDList Table(IntersectID int,StageID Int);

	declare	@Intersects IDTable;

	declare	@MessageStreamFussionAttributeID int,
			@EagleFieldFusionAttributeID int;

	select	@MessageStreamFussionAttributeID = 196;
	select	@EagleFieldFusionAttributeID = 205;

	-- load the stream that we want to add relations ships for    
	select	@eagleStreamID = fusionattributeid 
	from	[fusion].[stagingfile] 
	where	id = @StagingFileID and 
			fusionID = @FusionID;
			
	if @eagleStreamID is null
	begin
		raiserror('ERROR : UNABLE TO LOCATE SPECIFIED STREAM INFORMATION FOR INPUT FUSION ID / STAGING ID', 15, 1);
		return;
	end;

	select @currentEagleFusionId = FusionID from [dbo].[fusionattribute] where id = @eagleStreamID

	-- add relationships for Stream (196) to Eagle DB Columns (205)
	-- using star tag field that is a field for for fusionattribute type 205 lookup fields to add rels for
	-- todo pull to separate proc
	if @eagleStreamID is not null
	begin
			Declare @StreamToFieldList Table(FieldFusionAttributeID int, StreamFusionAttributeID int,IntersectTypeID int, ID int);
			
			-- load the intersect type ids
			select	@streamToFieldIntersectTypeID = IntersectTypeID,
					@streamSourceIntersectTypeNodeID = SourceIntersectTypeNodeID,
					@streamTargetIntersectTypeNodeID = TargetIntersectTypeNodeID
			from	utility.RelationshipTypes
			where	SourceObjectType = 'FusionAttributeType' and 
					SourceObjectID = @MessageStreamFussionAttributeID and 
					TargetObjectType = 'FusionAttributeType' and 
					TargetObjectID = @EagleFieldFusionAttributeID

			if @streamToFieldIntersectTypeID is null or @streamSourceIntersectTypeNodeID is null or @streamTargetIntersectTypeNodeID is null
			begin
				raiserror('ERROR : UNABLE TO LOCATE INTERSECT TYPE IDS FOR EAGLE TO EAGLE MESSAGE STREAMS', 15, 1);
				return;
			end;

			-- insert into in memory table variable the values we want to add intersects for
			insert into @StreamToFieldList
				select		fa.id, 
							sf.FusionAttributeID, 
							@streamToFieldIntersectTypeID, 
							ROW_NUMBER() OVER (Order by fa.id) AS 'RowNumber'
				from		field f 
							inner join fusionAttribute fa on (f.ObjectID = fa.ID and fa.fusionid = @currentEagleFusionId)
							inner join fieldtype ft on (f.fieldtypeid = ft.id)
							inner join [fusion].[StagingFileItem] sfi on (sfi.tag = f.value)				
							inner join [fusion].[StagingFile] sf on (sfi.stagingfileid = sf.id)
							left join	(
										select	srcINode.ObjectID as SourceObjectID,
												tgtINode.ObjectID as TargetObjectID,
												1 as hasExisting
										from	[Intersect] isect 
												inner join intersectnode srcINode on	(
																						isect.intersecttypeid = @streamToFieldIntersectTypeID and 
																						isect.id = srcINode.IntersectID and 
																						srcINode.IntersectTypeNodeID = @streamSourceIntersectTypeNodeID
																						)
												inner join intersectnode tgtINode on	(
																						isect.intersecttypeid = @streamToFieldIntersectTypeID and 
																						isect.id = tgtINode.IntersectID and 
																						tgtINode.IntersectTypeNodeID = @streamTargetIntersectTypeNodeID
																						)
										) existing on existing.SourceObjectID = sf.FusionAttributeID and existing.TargetObjectID = fa.ID
				where		fa.fusionattributetypeid = @EagleFieldFusionAttributeID and 
							ft.name = 'startag' and 
							sfi.stagingfileid = @StagingFileID and 
							existing.hasExisting is null
				group by	fa.id, sf.FusionAttributeID  -- grouping is used to eliminate duplicate star tag relations

			--insert intersect records and save there id's
			-- trick is to use merge to keep the sequence id and staging row ids
			-- http://stackoverflow.com/questions/15614261/using-output-clause-to-insert-value-not-in-inserted
			MERGE
				INTO    [Intersect] d
				USING   (
							SELECT	sr.IntersectTypeID, 
									2 as class,
									sr.ID as srID,
									'FusionAttribute' as Subject,
									sr.StreamFusionAttributeID as SubjectID,
									'FusionAttribute' as Object,
									sr.FieldFusionAttributeID as ObjectID
							FROM	@StreamToFieldList sr							
						) s
				ON      (1 = 0)
				WHEN NOT MATCHED THEN
				INSERT  (IntersectTypeID, Classification, Description, Subject, SubjectID, Object, ObjectID)
				VALUES  (s.IntersectTypeID, s.class, NULL, s.Subject, s.SubjectID, s.Object, s.ObjectID)
				OUTPUT  INSERTED.ID, s.srID into @IDList;

			--insert start records into intersect node
			INSERT INTO IntersectNode	(IntersectTypeNodeID, IntersectID, ObjectType, ObjectID)
				select	@streamSourceIntersectTypeNodeID, 
						il.IntersectID, 
						'FusionAttribute',
						sr.StreamFusionAttributeID 
				from	@StreamToFieldList sr 
						inner join @IDList il on sr.ID = il.StageID;

			--insert end records into intersect node
			INSERT INTO IntersectNode	(IntersectTypeNodeID, IntersectID, ObjectType, ObjectID)
				select	@streamTargetIntersectTypeNodeID, 
						il.IntersectID, 
						'FusionAttribute',
						sr.FieldFusionAttributeID 
				from	@StreamToFieldList sr 
						inner join @IDList il on sr.ID = il.StageID;

			insert into @Intersects select idl.intersectid from @IDList idl;
			
			declare @IntersectCount int
			select @IntersectCount = count(1) from @Intersects
			
			if @IntersectCount > 0 
			begin				
				EXEC cache.SynchronizeRelationships @Intersects
			end
	end;
end
GO

ALTER FUNCTION [dbo].[GetFusionAttributesByOwningArtifact]
(
	@ArtifactID int
)
RETURNS 
@tbl TABLE 
(
	ID int
)
AS
BEGIN
		declare @h table (ID int);

		with h as	(
					select	ID,
							ParentID
					from	Artifact
					where	ID = @ArtifactID
					union all
					select	P.ID,
							P.ParentID
					from	Artifact P
							inner join h as C on C.ParentID = P.ID
					)
		insert into @h
			select ID from h;
	
		with f as	(
					select	R.FusionID
					from	FusionOwner R
							inner join @h H on H.ID = R.ArtifactID
					)

		INSERT INTO @tbl
			SELECT	distinct
					FusionID
			FROM	f
	RETURN 
END
GO


ALTER FUNCTION [utility].[DeriveIntersectTypeName] 
(
--declare
	@id int
--set @id = 17
)
RETURNS nvarchar(500)
AS
BEGIN
	DECLARE @result nvarchar(500)

	SET @result =	(
					SELECT	COALESCE(SA.Name, SD.Name, SF.TextPath, SP.Name, ST.Name, SI.Name, case I.Subject when 'RuleType' then 'Rule' else '' end) + 
							' / ' + 
							COALESCE(OA.Name, OD.Name, [OF].TextPath, OP.Name, OT.Name, case I.Object when 'RuleType' then 'Rule' else '' end)
					FROM	[IntersectType] I
							left join ArtifactType SA on I.Subject = 'ArtifactType' and SA.ID = I.SubjectID
							left join ArtifactType OA on I.Object = 'ArtifactType' and OA.ID = I.ObjectID

							left join DomainType SD on I.Subject = 'DomainType' and SD.ID = I.SubjectID
							left join DomainType OD on I.Object = 'DomainType' and OD.ID = I.ObjectID

							left join [FusionAttributeType] SF on I.Subject = 'FusionAttributeType' and SF.ID = I.SubjectID
							left join [FusionAttributeType] [OF] on I.Object = 'FusionAttributeType' and [OF].ID = I.ObjectID


							left join [IntersectType] SI on I.Subject = 'IntersectType' and SI.ID = I.SubjectID

							left join [PolicyType] SP on I.Subject = 'PolicyType' and SP.ID = I.SubjectID
							left join [PolicyType] OP on I.Object = 'PolicyType' and OP.ID = I.ObjectID

							left join [TaxonomyType] ST on I.Subject = 'TaxonomyType' and ST.ID = I.SubjectID
							left join [TaxonomyType] OT on I.Object = 'TaxonomyType' and OT.ID = I.ObjectID
					WHERE	I.ID = @id
					FOR XML PATH('')
					)

	RETURN @result
END
GO

ALTER view [dbo].[Relationship]
as
	select	I.IntersectTypeID,
			R.IntersectID,
			case I.Classification
				when 0 then 2
				else I.Classification
			end as Classification,
			I.Description,
			'' as [Role],
			--R.[Role],
			R.SourceIntersectTypeNodeID,
			R.SourceObject as SourceObjectType,
			R.SourceObjectID,
			coalesce(S.TextPath, S.Name) as SourceName, 
			S.Parent as SourceParent,
			S.ParentID as SourceParentID,
			S.ParentName as SourceParentName,
			S.ObjectTypeID as SourceTypeID,
			S.ObjectType as SourceType,
			S.ObjectTypeName as SourceTypeName,
			S.[Url] as SourceUrl,
			R.TargetIntersectTypeNodeID,
			T.Object as TargetObjectType,
			T.ObjectID as TargetObjectID,
			coalesce(T.TextPath, T.Name) as TargetName,
			T.Parent as TargetParent,
			T.ParentID as TargetParentID,
			T.ParentName as TargetParentName,
			T.ObjectTypeID as TargetTypeID,
			T.ObjectType as TargetType,
			T.ObjectTypeName as TargetTypeName,
			T.[Url] as TargetUrl,
			TR.[Exists] as HasTechnicalRelationships
	from	cache.Relationship R
			inner join [Intersect] I on I.ID = R.IntersectID
			left join [cache].[ObjectDetails] S on S.[Object] = R.SourceObject and S.ObjectID = R.SourceObjectID
			left join [cache].[ObjectDetails] T on T.[Object] = R.TargetObject and T.ObjectID = R.TargetObjectID
			cross apply (
						select	case 
									when count(1) > 0 then cast(1 as bit) 
									else cast(0 as bit) 
								end as [Exists]
						from	cache.Relationships
						where	SourceObject = 'Intersect' and SourceObjectID = R.IntersectID
						) TR
GO

ALTER TRIGGER [dbo].[TaxonomyType_AfterUpdate]
   ON  [dbo].[TaxonomyType] 
   AFTER UPDATE
AS 
	SET NOCOUNT ON;
	INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
		select 'Update', [queue].WriteIndexXml('', 'TaxonomyType', ID, coalesce(UpdatedBy, 0)), 'TaxonomyType', ID from inserted

	update	T
	set		T.TextPath = utility.GetBreadcrumbStringWrapper('Taxonomy', S.ID, '/')
	from	Taxonomy T
			inner join inserted S on S.ID = T.TaxonomyTypeID

	merge	[cache].[Object] as T
	using	(
			select	'TaxonomyType' as [Object],			ID as ObjectID,
					'TaxonomyType' as ObjectType,			0 as ObjectTypeID
			from	inserted
			) as S
	on		T.[Object] = S.[Object] and T.[ObjectID] = S.[ObjectID]
	when	matched then
			update set	T.[ObjectType] = S.[ObjectType],
						T.[ObjectTypeID] = S.[ObjectTypeID]
	when	not matched then
			insert	( [Object],		[ObjectID],		[ObjectType],	[ObjectTypeID]		)
			values	( S.[Object],	S.[ObjectID],	S.[ObjectType], S.[ObjectTypeID]	);
go


alter table FusionAttribute  add CONSTRAINT [DF_FusionAttribute_Deleted] DEFAULT ((0)) for Deleted
go

alter table quality.RuleResult  add CONSTRAINT [DF_QualityRuleResult_CreatedOn] DEFAULT ((0)) for [CreatedOn]
go


CREATE NONCLUSTERED INDEX [IX_QueueTask_MachineAssignedNumRetries]
    ON [queue].[Task]([MachineAssigned] ASC, [NumberOfRetries] ASC)
    INCLUDE([Date], [ID], [Priority]);
GO


DROP FUNCTION [dbo].[GetRelatedObjectsByEventType]
go

ALTER TRIGGER [dbo].[Domain_AfterUpdate]
   ON  [dbo].[Domain] 
   AFTER UPDATE
AS 
	SET NOCOUNT ON;
	INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
        select 'Update', [queue].WriteIndexXml('', 'Domain', ID, coalesce(UpdatedBy, 0)), 'Domain', ID from inserted
GO

ALTER TRIGGER [dbo].[Intersect_AfterInsert]
	ON [dbo].[Intersect]
	FOR INSERT
AS
BEGIN
	SET NOCOUNT ON;

	merge cache.Object as T
	using (
			select	'Intersect' as Object,
					ID as ObjectID,
					'IntersectType' as ObjectType,
					IntersectTypeID as ObjectTypeID
			from	inserted
			) as S
	on    (T.Object = S.Object and T.ObjectID = S.ObjectID)
	when not matched then
		insert (Object, ObjectID, ObjectType, ObjectTypeID)
		values (S.Object, S.ObjectID, S.ObjectType, S.ObjectTypeID);

	INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
        select 'Add', [queue].WriteIndexXml('', Subject, SubjectID, UpdatedBy), 'Intersect', ID from inserted

	INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
        select 'Add', [queue].WriteIndexXml('', Object, ObjectID, UpdatedBy), 'Intersect', ID from inserted

	--declare @tbl table(ID int identity, IntersectID int, ResourceID int, Subject varchar(50), SubjectID int, Object varchar(50), ObjectID int)
	--insert into @tbl
	--	select ID, UpdatedBy, Subject, SubjectID, Object, ObjectID from inserted;

	--declare @current int = 1,
	--		@max int,
	--		@id int,
	--		@r int,
	--		@s varchar(50),
	--		@sid int,
	--		@o varchar(50),
	--		@oid int,
	--		@date datetime = getutcdate()

	--select @max =max(ID) from @tbl

	--while @current <= @max
	--begin
	--	select	@id = IntersectID,
	--			@r = ResourceID,
	--			@s = coalesce(Subject, 'Intersect'),
	--			@sid = coalesce(SubjectID, IntersectID),
	--			@o = coalesce(Object, 'Intersect'),
	--			@oid = coalesce(ObjectID, IntersectID)
	--	from	@tbl
	--	where	ID = @current

	--	exec [utility].[AddAuditEntry] @s, @sid, @r, @date, 'Created', 'Intersect', @id
	--	exec [utility].[AddAuditEntry] @o, @oid, @r, @date, 'Created', 'Intersect', @id

	--	exec cache.SynchronizeResponsibilitiesForObject @s, @sid

	--	set @current = @current +1
	--end;
END
GO

ALTER TRIGGER [dbo].[Intersect_AfterUpdate]
	ON [dbo].[Intersect]
	FOR UPDATE
AS
BEGIN
	SET NOCOUNT ON;

	merge cache.Object as T
	using (
			select	'Intersect' as Object,
					ID as ObjectID,
					'IntersectType' as ObjectType,
					IntersectTypeID as ObjectTypeID
			from	inserted
			) as S
	on    (T.Object = S.Object and T.ObjectID = S.ObjectID)
	when not matched then
		insert (Object, ObjectID, ObjectType, ObjectTypeID)
		values (S.Object, S.ObjectID, S.ObjectType, S.ObjectTypeID);

	INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
        select 'Update', [queue].WriteIndexXml('', Subject, SubjectID, UpdatedBy), 'Intersect', ID from inserted

	INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
        select 'Update', [queue].WriteIndexXml('', Object, ObjectID, UpdatedBy), 'Intersect', ID from inserted

	--declare @tbl table(ID int identity, IntersectID int, ResourceID int, Subject varchar(50), SubjectID int, Object varchar(50), ObjectID int)
	--insert into @tbl
	--	select ID, UpdatedBy, Subject, SubjectID, Object, ObjectID from inserted;

	--declare @current int = 1,
	--		@max int,
	--		@id int,
	--		@r int,
	--		@s varchar(50),
	--		@sid int,
	--		@o varchar(50),
	--		@oid int,
	--		@date datetime = getutcdate()

	--select @max =max(ID) from @tbl

	--while @current <= @max
	--begin
	--	select	@id = IntersectID,
	--			@r = ResourceID,
	--			@s = coalesce(Subject, 'Intersect'),
	--			@sid = coalesce(SubjectID, IntersectID),
	--			@o = coalesce(Object, 'Intersect'),
	--			@oid = coalesce(ObjectID, IntersectID)
	--	from	@tbl
	--	where	ID = @current

	--	exec [cache].[SynchronizeObjectDetails] 'Intersect', @id
	--	exec [utility].[AddAuditEntry] @s, @sid, @r, @date, 'Updated', 'Intersect', @id
	--	exec [utility].[AddAuditEntry] @o, @oid, @r, @date, 'Updated', 'Intersect', @id

	--	merge cache.Relationship as T
	--	using (
	--			select	distinct
	--					S.IntersectID,
	--					S.IntersectTypeNodeID as SourceIntersectTypeNodeID, 
	--					S.ID as SourceIntersectNodeID,
	--					S.ObjectType as SourceObject,
	--					S.ObjectID as SourceObjectID,
	--					T.IntersectTypeNodeID as TargetIntersectTypeNodeID,
	--					T.ID as TargetIntersectNodeID,
	--					T.ObjectType as TargetObject,
	--					T.ObjectID as TargetObjectID
	--			from	dbo.IntersectNode S
	--					inner join dbo.IntersectNode T on T.IntersectID = S.IntersectID and T.ID <> S.ID
	--			where	S.IntersectID = @id
	--			) as S (
	--				IntersectID, 
	--				SourceIntersectTypeNodeID, SourceIntersectNodeID, SourceObject, SourceObjectID, 
	--				TargetIntersectTypeNodeID, TargetIntersectNodeID, TargetObject, TargetObjectID
	--				)
	--	on    (T.IntersectID = S.IntersectID and T.SourceObject = S.SourceObject and T.SourceObjectID = S.SourceObjectID)
	--	when not matched then
	--		insert (
	--				IntersectID, 
	--				SourceIntersectTypeNodeID, SourceIntersectNodeID, SourceObject, SourceObjectID, 
	--				TargetIntersectTypeNodeID, TargetIntersectNodeID, TargetObject, TargetObjectID
	--				)
	--		values (
	--				S.IntersectID, 
	--				S.SourceIntersectTypeNodeID, S.SourceIntersectNodeID, S.SourceObject, S.SourceObjectID, 
	--				S.TargetIntersectTypeNodeID, S.TargetIntersectNodeID, S.TargetObject, S.TargetObjectID
	--				);

	--	set @current = @current +1
	--end;
END
GO

ALTER TABLE [dbo].[IntersectNode] DROP CONSTRAINT [FK_IntersectNode_Intersect]
GO

ALTER TABLE [dbo].[IntersectNode] DROP CONSTRAINT [FK_IntersectNode_IntersectTypeNode]
GO

ALTER TABLE [dbo].[IntersectTypeNode] DROP CONSTRAINT [FK_IntersectTypeNode_IntersectType]
GO

ALTER TABLE SiteNav add [ObjectID] INT NULL
go
ALTER TABLE SiteNav add  [Object] VARCHAR (50) NULL
go

CREATE TRIGGER [dbo].[SiteNav_AfterDelete]
	ON [dbo].[SiteNav]
	FOR DELETE
AS

	--update sortorder
	update n
	set n.sortorder = s.sortorder 
	from
		sitenav n
		join (
		select 
			id,
			row_number() over (partition by parentid order by sortorder) as sortorder
		from sitenav where parentid is null) s on s.id = n.id;
GO

CREATE TRIGGER [dbo].[SiteNav_AfterInsert]
	ON [dbo].[SiteNav]
	FOR INSERT
AS

	--update sortorder
	update n
	set n.sortorder = s.sortorder 
	from
		sitenav n
		join (
		select 
			id,
			row_number() over (partition by parentid order by sortorder) as sortorder
		from sitenav where parentid is null) s on s.id = n.id;
GO

/*
ALTER TABLE [fusion].[RuleItem] DROP CONSTRAINT [FK_FusionRuleItem_FusionRule]
GO

ALTER TABLE [fusion].[RuleItem]  WITH CHECK ADD  CONSTRAINT [FK_FusionRuleItem_FusionRule] FOREIGN KEY([RuleID])
REFERENCES [fusion].[Rule] ([ID])
ON DELETE CASCADE
GO

ALTER TABLE [fusion].[RuleItem] CHECK CONSTRAINT [FK_FusionRuleItem_FusionRule]
GO
*/

alter procedure [dbo].[AddSingleIntersect]
	@ResourceID int,
	@IntersectTypeID int,
	@Subject varchar(50),			-- The start object type.
	@SubjectID int,					-- The start object ID.
	@Object varchar(50),			-- The end object type.
	@ObjectID int,					-- The end object ID.	
	@Classification int,
	@Description nvarchar(4000)
as
begin
	set nocount on;

	declare @Date datetime = getutcdate(),
			@ErrorMessage nvarchar(2500),
			@IntersectID int,
			@Reversed bit = 0

	select	@IntersectID = ID,
			@Reversed = case
				when (Subject = @Subject and SubjectID = @SubjectID and Object = @Object and ObjectID = @ObjectID) then 0
				else 1
			end
	from	[Intersect]
	where	(
			(Subject = @Subject and SubjectID = @SubjectID and Object = @Object and ObjectID = @ObjectID) OR 
			(Subject = @Object and SubjectID = @ObjectID and Object = @Subject and ObjectID = @SubjectID)
			)

	if @IntersectID is not null and @IntersectID > 0
		begin
			-- Update
			update	[Intersect]
			set		Classification = @Classification,
					Description = @Description
			where	ID = @IntersectID
		end
	else
		begin
			-- Create
			declare @SubjectType varchar(50),
					@SubjectTypeID int,
					@ObjectType varchar(50),
					@ObjectTypeID int

			select	@SubjectType = ObjectType, @SubjectTypeID = ObjectTypeID	from cache.[Object] where [Object] = @Subject and ObjectID = @SubjectID 
			select	@ObjectType = ObjectType, @ObjectTypeID = ObjectTypeID		from cache.[Object] where [Object] = @Object and ObjectID = @ObjectID 

			select	distinct 
					@IntersectTypeID = ID,
					@Reversed = case
						when (Subject = @SubjectType and SubjectID = @SubjectTypeID and Object = @ObjectType and ObjectID = @ObjectTypeID) then 0
						else 1
					end
			from	IntersectType 
			where	(
						(Subject = @SubjectType and SubjectID = @SubjectTypeID and Object = @ObjectType and ObjectID = @ObjectTypeID) OR
						(Subject = @ObjectType and SubjectID = @ObjectTypeID and Object = @SubjectType and ObjectID = @SubjectTypeID)
					)

			if @IntersectTypeID is not null
				begin
					INSERT INTO [Intersect] (
						IntersectTypeID, 
						Classification, 
						[Description],
						[Subject], SubjectID,
						[Object], ObjectID,
						CreatedBy, CreatedOn,
						UpdatedBy, UpdatedOn				
					) 
					VALUES (
						@IntersectTypeID, 
						@Classification, 
						@Description,
						case @Reversed when 0 then @Subject else @Object end, 
						case @Reversed when 0 then @SubjectID else @ObjectID end,
						case @Reversed when 0 then @Object else @Subject end, 
						case @Reversed when 0 then @ObjectID else @SubjectID end,
						@ResourceID, @Date,
						@ResourceID, @Date
					)

					SELECT @IntersectID = SCOPE_IDENTITY()

					insert into cache.[Object] ( [Object], [ObjectID], [ObjectType], [ObjectTypeID] )
					values	( 'Intersect', @IntersectID, 'IntersectType', @IntersectTypeID );

					--Update the responsibilities of the object that should inherit form the other (Taxonomy can push relationships down to artifact)
					if ( (@Subject = 'Taxonomy' and @Object = 'Artifact') OR (@Subject = 'Artifact' and @Object = 'Taxonomy') )
						begin
							if @Subject = 'Artifact'
							begin
								exec [cache].[SynchronizeResponsibilitiesForObject] @Subject, @SubjectID
							end
							if @Object = 'Artifact'
							begin
								exec [cache].[SynchronizeResponsibilitiesForObject] @Object, @ObjectID
							end
						end
				end
		end

	select * from [Intersect] where ID = @IntersectID
end
GO

alter procedure [dbo].[AsyncAddObject]
	@Object varchar(50),
	@ObjectID int,
	@ParentObject varchar(50),
	@ParentObjectID int,
	@ResourceID int
as
begin
	set nocount on;
	declare @trans varchar(25) = 'Trans',
			@current int = 1,
			@max int,
			@date datetime = getutcdate()

	begin try
		begin transaction @trans
		
		exec [cache].[SynchronizeObjectDetails] @Object, @ObjectID

		exec [utility].[AddAuditEntry] @ParentObject, @ParentObjectID, @ResourceID, @date, 'Created', @Object, @ObjectID

		if @Object in ('AttributeTypeRelation', 'AttributeTypeRelation', 'ResponsibilityTypeRelation', 'ResponsibilityType')
		begin
			exec utility.CalculateStatistics
		end
		else
		begin
			exec utility.CalculateStatistics @Object, @ObjectID
		end

		if @Object = 'Intersect'
		begin
			exec cache.SynchronizeResponsibilitiesForObject @ParentObject, @ParentObjectID 
		end

		if @Object = 'Responsibility'
		begin
			exec cache.SynchronizeResponsibilitiesForObject @ParentObject, @ParentObjectID 
		end

		if @Object = 'Artifact'
		begin
			exec cache.SynchronizeResponsibilitiesForObject @Object, @ObjectID 
		end

		commit transaction @trans
	end try
	begin catch
		DECLARE @ErrorMessage NVARCHAR(4000);
		DECLARE @ErrorSeverity INT;
		DECLARE @ErrorState INT;

		SELECT 
			@ErrorMessage = ERROR_MESSAGE(),
			@ErrorSeverity = ERROR_SEVERITY(),
			@ErrorState = ERROR_STATE();

		-- Use RAISERROR inside the CATCH block to return error
		-- information about the original error that caused
		-- execution to jump to the CATCH block.
		RAISERROR (@ErrorMessage, -- Message text.
				   @ErrorSeverity, -- Severity.
				   @ErrorState -- State.
				   );

		rollback transaction @trans
	end catch
end
GO

alter procedure [dbo].[AsyncDeleteObject]
	@Object varchar(50),
	@ObjectID int,
	@ParentObject varchar(50),
	@ParentObjectID int,
	@ResourceID int
as
begin
	set nocount on;
	
	declare @trans varchar(25) = 'Trans',
			@current int = 1,
			@max int,
			@date datetime = getutcdate()

	begin try
		exec [utility].[AddAuditEntry] @ParentObject, @ParentObjectID, @ResourceID, @date, 'Removed', @Object, @ObjectID
	end try
	begin catch

	end catch

	begin try
		begin transaction @trans

		--INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID], [Priority])
		--values ('ObjectIndex', 'D', @Object, @ObjectID, 4)

		--COMMON
		delete CommentRelation					where ObjectType = @Object and ObjectID = @ObjectID
		delete Field							where ObjectType = @Object and ObjectID = @ObjectID
		delete Follow							where ObjectType = @Object and ObjectID = @ObjectID
		delete Responsibility					where ObjectType = @Object and ObjectID = @ObjectID
		--delete SurveyObjectCache				where ObjectType = @Object and ObjectID = @ObjectID
		delete cache.[Object]					where [Object] = @Object and ObjectID = @ObjectID

		if charindex('Type', @Object) > 0
		begin
			delete AttributeTypeRelation			where ObjectType = @Object AND ObjectID = @ObjectID
			delete FieldType						where [Object] = @Object AND ObjectID = @ObjectID
			delete ResponsibilityTypeRelation		where ObjectType = @Object and ObjectID = @ObjectID
			delete ResponsibilityTypeObjectClaim	where ObjectType = @Object and ObjectID = @ObjectID
			delete StatisticType					where [Object] = @Object and ObjectID = @ObjectID
			delete WorkflowTypeRelation				where [Object] = @Object and ObjectID = @ObjectID

			if @Object in ('AttributeTypeRelation', 'AttributeTypeRelation', 'ResponsibilityTypeRelation', 'ResponsibilityType')
			begin
				exec utility.CalculateStatistics
			end

			if @Object = 'ArtifactType'
			begin
				declare @ah table (ID int);
				with ah as	(
							select	ID, 
									ParentID
							from	Artifact
							where	ArtifactTypeID = @ObjectID
							union all
							select	C.ID,
									C.ParentID
							from	Artifact C
									inner join ah P on P.ID = C.ParentID
							)
				insert into @ah 
					select ID from ah
			
				delete Artifact where ID in (select ID from @ah)
			end

			if @Object = 'AttributeType'
			begin
				delete AttributeTypeRelation		where AttributeTypeID = @ObjectID
			
				declare @ath table (RowID int identity, ID int, ParentID int null, [Level] int);
				with ath as	(
							select	ID,
									ParentID,
									1 as [Level]
							from	AttributeType
							where	ID = @ObjectID
							union all
							select	C.ID,
									C.ParentID,
									P.[Level] + 1 as [Level]
							from	AttributeType C
									inner join ath P on P.ID = C.ParentID
							)
				insert into @ath 
					select ID, ParentID, [Level] from ath order by [Level] desc

				select @max = max(RowID) from @ath

				while @current <= @max
				begin
					declare @attributeTypeID int
					select @attributeTypeID = ID from @ath where RowID = @current
					delete Attribute where AttributeTypeID = @attributeTypeID
					delete AttributeType where ID = @attributeTypeID
					set @current = @current + 1
				end
			end

			if @Object = 'DomainType'
			begin
				delete DomainItem where DomainID in (select ID from Domain where DomainTypeID = @ObjectID)
				delete Domain where DomainTypeID = @ObjectID
				delete DomainGroup where DomainTypeID = @ObjectID
			end

			if @Object = 'FieldType'
			begin
				delete Field where FieldTypeID = @ObjectID
				delete FieldTypeFusionLookupDisplayField where FieldTypeID = @ObjectID
				delete FieldTypeRelationLookupDisplayField where FieldTypeID = @ObjectID
				delete FieldType where ID = @ObjectID
			end

			if @Object = 'FusionAttributeType'
			begin
				declare @fath table (RowID int identity, ID int, ParentID int null, [Level] int);
				with fath as	(
							select	ID,
									ParentID,
									1 as [Level]
							from	FusionAttributeType
							where	ID = @ObjectID
							union all
							select	C.ID,
									C.ParentID,
									P.[Level] + 1 as [Level]
							from	FusionAttributeType C
									inner join fath P on P.ID = C.ParentID
							)
				insert into @fath 
					select ID, ParentID, [Level] from fath order by [Level] desc

				select @max = max(RowID) from @fath

				while @current <= @max
				begin
					declare @fusionAttributeTypeID int
					select @fusionAttributeTypeID = ID from @fath where RowID = @current
					delete FusionAttribute where FusionAttributeTypeID = @fusionAttributeTypeID
					delete FusionAttributeType where ID = @fusionAttributeTypeID
					set @current = @current + 1
				end
			end

			if @Object = 'FusionType'
			begin
				declare @fth table (RowID int identity, ID int, ParentID int null, [Level] int);
				with fth as	(
							select	ID,
									ParentID,
									1 as [Level]
							from	FusionAttributeType
							where	FusionTypeID = @ObjectID and ParentID is null
							union all
							select	C.ID,
									C.ParentID,
									P.[Level] + 1 as [Level]
							from	FusionAttributeType C
									inner join fth P on P.ID = C.ParentID
							)
				insert into @fth 
					select ID, ParentID, [Level] from fth order by [Level] desc

				select @max = max(RowID) from @fth

				while @current <= @max
				begin
					declare @fattributeTypeID int
					select @fattributeTypeID = ID from @fth where RowID = @current
					delete FusionAttribute where FusionAttributeTypeID = @fattributeTypeID
					delete FusionAttributeType where ID = @fattributeTypeID
					set @current = @current + 1
				end
				delete FusionType where ID = @ObjectID
			end

			if @Object = 'IntersectType'
			begin
				delete [Intersect] where IntersectTypeID = @ObjectID
				delete IntersectType where ID = @ObjectID
			end

			if @Object = 'LookupType'
			begin
				delete [Lookup] where LookupTypeID = @ObjectID
			end

			if @Object = 'PolicyType'
			begin
				delete Policy where PolicyTypeID = @ObjectID
				delete PolicyTypeLevel where PolicyTypeID = @ObjectID
			end

			if @Object = 'ResponsibilityType'
			begin
				delete Responsibility where ResponsibilityTypeID = @ObjectID
				delete ResponsibilityType where ID = @ObjectID
			end

			if @Object = 'SurveyType'
			begin
				--delete SurveyObjectCache where SurveyTypeID = @ObjectID
				delete Survey where SurveyTypeID = @ObjectID
				delete SurveyType where ID = @ObjectID
			end

			if @Object = 'TaxonomyType'
			begin
				delete Taxonomy where TaxonomyTypeID = @ObjectID
				delete TaxonomyTypeLevel where TaxonomyTypeID = @ObjectID
				delete TaxonomyType where ID = @ObjectID
			end

		end
		else
		begin
			delete Attribute							where ObjectType = @Object and ObjectID = @ObjectID

			BEGIN TRY
				DECLARE @tblIntersectIDs table (ID int)

				INSERT INTO @tblIntersectIDs
					SELECT	ID
					FROM	[Intersect]
					WHERE	(Subject = @Object and SubjectID = @ObjectID) OR (Object = @Object and ObjectID = @ObjectID)

				delete	MapItem 
				where	SourceIntersectID in (select ID from @tblIntersectIDs) OR
						TargetIntersectID in (select ID from @tblIntersectIDs)

				delete [Intersect] where ID in (select ID from @tblIntersectIDs)
			END TRY
			BEGIN CATCH

			END CATCH

			if @Object = 'Artifact'
			begin
				delete	RelatedArtifact where ArtifactID = @ObjectID
			end

			if @Object = 'Domain'
			begin
				delete DomainItem where DomainID = @ObjectID
			end

			if @Object = 'Responsibility'
			begin
				exec cache.SynchronizeResponsibilitiesForObject @ParentObject, @ParentObjectID 
			end

			if @Object = 'Taxonomy'
			begin
				declare @th table (ID int);
				with th as	(
							select	ID, 
									ParentID
							from	Taxonomy
							where	ID = @ObjectID
							union all
							select	C.ID,
									C.ParentID
							from	Taxonomy C
									inner join th P on P.ID = C.ParentID
							)
				insert into @th 
					select ID from th
			
				delete Taxonomy where ID in (select ID from @th)

				exec cache.SynchronizeResponsibilities
			end
		end
		
		commit transaction @trans
	end try
	begin catch
		DECLARE @ErrorMessage NVARCHAR(4000);
		DECLARE @ErrorSeverity INT;
		DECLARE @ErrorState INT;

		SELECT 
			@ErrorMessage = ERROR_MESSAGE(),
			@ErrorSeverity = ERROR_SEVERITY(),
			@ErrorState = ERROR_STATE();

		-- Use RAISERROR inside the CATCH block to return error
		-- information about the original error that caused
		-- execution to jump to the CATCH block.
		RAISERROR (@ErrorMessage, -- Message text.
				   @ErrorSeverity, -- Severity.
				   @ErrorState -- State.
				   );

		rollback transaction @trans
	end catch
end
GO

ALTER PROCEDURE [dbo].[DeleteIntersect]
	@ID int,
	@ResourceID int
AS
BEGIN
	SET NOCOUNT ON;
	declare @trancount int;
    set @trancount = @@trancount;	
	
	BEGIN TRY
		if @trancount = 0
            begin transaction
        else
			save transaction DeleteIntersect

		IF NOT EXISTS(select 1 from [Intersect] where ID = @ID)
		BEGIN
			RAISERROR('Item does not exist.', 16, 1);
		END

		IF EXISTS(select 1 from [Intersect] where (Subject = 'Intersect' and SubjectID = @ID) OR (Object = 'Intersect' and ObjectID = @ID) )
		BEGIN
			RAISERROR('Item is used in other relationships.', 16, 1);
		END

		IF EXISTS(
			select	I.ID
			from	[Intersect] I
					inner join MapItem MI on MI.SourceIntersectID = I.ID or MI.TargetIntersectID = I.ID and I.ID = @ID
		)
		BEGIN
			RAISERROR('Relationship is a source for other relationships.  You must first remove those consuming relationships before deleting this one.', 16, 1);
		END

		if exists(select 1 from [Attribute] where ObjectType = 'Intersect' and ObjectID = @ID)
		begin
			DELETE	[Attribute]
			WHERE	ObjectType = 'Intersect' and ObjectID = @ID
		end

		declare @oNodeID int,
				@date datetime,
				@Subject varchar(50),
				@SubjectID int,
				@Object varchar(50),
				@ObjectID int

		set @date = getutcdate()

		select	@Subject = Subject,
				@SubjectID = SubjectID,
				@Object = Object,
				@ObjectID = ObjectID
		from	[Intersect]
		where	ID = @ID

		exec utility.AddAuditEntry @Subject, @SubjectID, @ResourceID, @date, 'Removed', 'Intersect', @ID
		exec utility.AddAuditEntry @Object, @ObjectID, @ResourceID, @date, 'Removed', 'Intersect', @ID

		-- Now delete the actual record.
		delete	[Intersect]
		where	ID = @ID

		--Update the responsibilities of the object that should inherit form the other (Taxonomy can push relationships down to artifact)
		if ( (@Subject = 'Taxonomy' and @Object = 'Artifact') OR (@Subject = 'Artifact' and @Object = 'Taxonomy') )
		begin
			if @Subject = 'Artifact'
			begin
				exec [cache].[SynchronizeResponsibilitiesForObject] @Subject, @SubjectID
			end
			if @Object = 'Artifact'
			begin
				exec [cache].[SynchronizeResponsibilitiesForObject] @Object, @ObjectID
			end
		end

		if @trancount = 0
			commit;
	END TRY
	BEGIN CATCH
		declare @message varchar(4000), @xstate int;
        select @message = ERROR_MESSAGE(), @xstate = XACT_STATE();
        if @xstate = -1
            rollback;
        if @xstate = 1 and @trancount = 0
            rollback
        if @xstate = 1 and @trancount > 0
            rollback transaction DeleteIntersect;

        raiserror ('Unable to remove relationship: %s', 16, 1, @message);
	END CATCH
END
GO

alter procedure [dbo].[DeleteObject]
	@Obj varchar(50),
	@ObjectID int,
	@ResourceID int
as
begin
	set nocount on;
	
	declare @Object varchar(50) = @Obj,
			@trans varchar(25) = 'Trans',
			@current int = 1,
			@max int

	begin try
		begin transaction @trans

		INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
        VALUES (
				'ObjectVersion', 
				'<fields>
				 <Action>Removed</Action>
				 <ActionObject>' + @Obj + '</ActionObject>
				 <ActionObjectID>' + cast(@ObjectID as varchar) + '</ActionObjectID>
				 <ResourceID>' + cast(@ResourceID as varchar) + '</ResourceID>
				</fields>', 
				@Obj, 
				@ObjectID)

		INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
		values ('ObjectIndex', 'D', @Obj, @ObjectID)

		--COMMON
		delete CommentRelation					where ObjectType = @Object and ObjectID = @ObjectID
		delete Field							where ObjectType = @Object and ObjectID = @ObjectID
		delete Follow							where ObjectType = @Object and ObjectID = @ObjectID
		delete Responsibility					where ObjectType = @Object and ObjectID = @ObjectID
		--delete SurveyObjectCache				where ObjectType = @Object and ObjectID = @ObjectID
		delete cache.[Object]					where [Object] = @Object and ObjectID = @ObjectID

		if charindex('Type', @Object) > 0
		begin
			delete AttributeTypeRelation			where ObjectType = @Object AND ObjectID = @ObjectID
			delete FieldType						where [Object] = @Object AND ObjectID = @ObjectID
			delete ResponsibilityTypeRelation		where ObjectType = @Object and ObjectID = @ObjectID
			delete ResponsibilityTypeObjectClaim	where ObjectType = @Object and ObjectID = @ObjectID
			delete StatisticType					where [Object] = @Object and [ObjectID] = @ObjectID
			delete WorkflowTypeRelation				where [Object] = @Object and ObjectID = @ObjectID

			if @Object = 'ArtifactType'
			begin
				declare @ah table (ID int);
				with ah as	(
							select	ID, 
									ParentID
							from	Artifact
							where	ArtifactTypeID = @ObjectID
							union all
							select	C.ID,
									C.ParentID
							from	Artifact C
									inner join ah P on P.ID = C.ParentID
							)
				insert into @ah 
					select ID from ah
			
				delete Artifact where ID in (select ID from @ah)
			end

			if @Object = 'AttributeType'
			begin
				delete AttributeTypeRelation		where AttributeTypeID = @ObjectID
			
				declare @ath table (RowID int identity, ID int, ParentID int null, [Level] int);
				with ath as	(
							select	ID,
									ParentID,
									1 as [Level]
							from	AttributeType
							where	ID = @ObjectID
							union all
							select	C.ID,
									C.ParentID,
									P.[Level] + 1 as [Level]
							from	AttributeType C
									inner join ath P on P.ID = C.ParentID
							)
				insert into @ath 
					select ID, ParentID, [Level] from ath order by [Level] desc

				select @max = max(RowID) from @ath

				while @current <= @max
				begin
					declare @attributeTypeID int
					select @attributeTypeID = ID from @ath where RowID = @current
					delete Attribute where AttributeTypeID = @attributeTypeID
					delete AttributeType where ID = @attributeTypeID
					set @current = @current + 1
				end
			end

			if @Object = 'DomainType'
			begin
				delete DomainItem where DomainID in (select ID from Domain where DomainTypeID = @ObjectID)
				delete Domain where DomainTypeID = @ObjectID
				delete DomainGroup where DomainTypeID = @ObjectID
			end

			if @Object = 'FieldType'
			begin
				delete Field where FieldTypeID = @ObjectID
				delete FieldType where ID = @ObjectID
			end

			if @Object = 'FusionAttributeType'
			begin
				declare @fath table (RowID int identity, ID int, ParentID int null, [Level] int);
				with fath as	(
							select	ID,
									ParentID,
									1 as [Level]
							from	FusionAttributeType
							where	ID = @ObjectID
							union all
							select	C.ID,
									C.ParentID,
									P.[Level] + 1 as [Level]
							from	FusionAttributeType C
									inner join fath P on P.ID = C.ParentID
							)
				insert into @fath 
					select ID, ParentID, [Level] from fath order by [Level] desc

				select @max = max(RowID) from @fath

				while @current <= @max
				begin
					declare @fusionAttributeTypeID int
					select @fusionAttributeTypeID = ID from @fath where RowID = @current
					delete FusionAttribute where FusionAttributeTypeID = @fusionAttributeTypeID
					delete FusionAttributeType where ID = @fusionAttributeTypeID
					set @current = @current + 1
				end
			end

			if @Object = 'FusionType'
			begin
				declare @fth table (RowID int identity, ID int, ParentID int null, [Level] int);
				with fth as	(
							select	ID,
									ParentID,
									1 as [Level]
							from	FusionAttributeType
							where	FusionTypeID = @ObjectID and ParentID is null
							union all
							select	C.ID,
									C.ParentID,
									P.[Level] + 1 as [Level]
							from	FusionAttributeType C
									inner join fth P on P.ID = C.ParentID
							)
				insert into @fth 
					select ID, ParentID, [Level] from fth order by [Level] desc

				select @max = max(RowID) from @fth

				while @current <= @max
				begin
					declare @fattributeTypeID int
					select @fattributeTypeID = ID from @fth where RowID = @current
					delete FusionAttribute where FusionAttributeTypeID = @fattributeTypeID
					delete FusionAttributeType where ID = @fattributeTypeID
					set @current = @current + 1
				end
				delete FusionType where ID = @ObjectID
			end

			if @Object = 'IntersectType'
			begin
				delete [Intersect] where IntersectTypeID = @ObjectID
				delete IntersectType where ID = @ObjectID
			end

			if @Object = 'LookupType'
			begin
				delete [Lookup] where LookupTypeID = @ObjectID
			end

			if @Object = 'PolicyType'
			begin
				delete Policy where PolicyTypeID = @ObjectID
				delete PolicyTypeLevel where PolicyTypeID = @ObjectID
			end

			if @Object = 'ResponsibilityType'
			begin
				delete Responsibility where ResponsibilityTypeID = @ObjectID
				delete ResponsibilityType where ID = @ObjectID
			end

			--if @Object = 'StatisticType'
			--begin
			--	delete [Statistic] where StatisticTypeID = @ObjectID
			--end

			if @Object = 'SurveyType'
			begin
				--delete SurveyObjectCache where SurveyTypeID = @ObjectID
				delete Survey where SurveyTypeID = @ObjectID
				delete SurveyType where ID = @ObjectID
			end

			if @Object = 'TaxonomyType'
			begin
				delete Taxonomy where TaxonomyTypeID = @ObjectID
				delete TaxonomyTypeLevel where TaxonomyTypeID = @ObjectID
				delete TaxonomyType where ID = @ObjectID
			end

		end
		else
		begin
			delete Attribute							where ObjectType = @Object and ObjectID = @ObjectID
			delete cache.Relationship					where [SourceObject] = @Object and SourceObjectID = @ObjectID
			delete cache.Relationship					where [TargetObject] = @Object and TargetObjectID = @ObjectID

			BEGIN TRY
				DECLARE @tblIntersectIDs table (ID int)

				INSERT INTO @tblIntersectIDs
					SELECT	ID
					FROM	[Intersect]
					WHERE	(Subject = @Object and SubjectID = @ObjectID) OR (Object = @Object and ObjectID = @ObjectID)

				delete	MapItem 
				where	SourceIntersectID in (select ID from @tblIntersectIDs) OR
						TargetIntersectID in (select ID from @tblIntersectIDs)

				delete [Intersect] where ID in (select ID from @tblIntersectIDs)
			END TRY
			BEGIN CATCH

			END CATCH

			if @Object = 'Artifact'
			begin
				delete	RelatedArtifact where ArtifactID = @ObjectID
			end

			if @Object = 'Domain'
			begin
				delete DomainItem where DomainID = @ObjectID
			end

			if @Object = 'Taxonomy'
			begin
				declare @th table (ID int);
				with th as	(
							select	ID, 
									ParentID
							from	Taxonomy
							where	ID = @ObjectID
							union all
							select	C.ID,
									C.ParentID
							from	Taxonomy C
									inner join th P on P.ID = C.ParentID
							)
				insert into @th 
					select ID from th
			
				delete Taxonomy where ID in (select ID from @th)
			end
		end
		
		commit transaction @trans
	end try
	begin catch
		 DECLARE @ErrorMessage NVARCHAR(4000);
    DECLARE @ErrorSeverity INT;
    DECLARE @ErrorState INT;

    SELECT 
        @ErrorMessage = ERROR_MESSAGE(),
        @ErrorSeverity = ERROR_SEVERITY(),
        @ErrorState = ERROR_STATE();

    -- Use RAISERROR inside the CATCH block to return error
    -- information about the original error that caused
    -- execution to jump to the CATCH block.
    RAISERROR (@ErrorMessage, -- Message text.
               @ErrorSeverity, -- Severity.
               @ErrorState -- State.
               );

		rollback transaction @trans
	end catch
end
GO

alter procedure [dbo].[ProcessBulkLoad]
--declare
	@LoadID int
--set @LoadID = 29
as
begin
	set nocount on;

	declare @Object varchar(50),
			@ObjectID int,
			@Action varchar(1),
			@UpdatedBy int = 0

	select	@Object = [Object],
			@ObjectID = ObjectID,
			@Action = [Action],
			@UpdatedBy = UpdatedBy
	from	[Load]
	where	ID = @LoadID

	-- PARSE any dynamic fields that are specifically lookups.
	update	T
	set		T.LookupObject = S.LookupObject,
			T.LookupObjectID = S.LookupObjectID
	from	LoadItemColumn T
			inner join	(
						select	IC.LoadID,
								IC.RowIndex,
								IC.ColumnIndex,
								case 
									when L_A.ID is not null then 'Artifact'
									when L_D.ID is not null then 'Domain'
									when L_DI.ID is not null then 'DomainItem'
									when L_F.ID is not null then 'FusionAttribute'
									when L_I.ID is not null then 'Intersect'
									when L_L.Value is not null then 'Lookup'
									when L_T.ID is not null then 'Taxonomy'
									else NULL
								end as LookupObject,
								coalesce(L_A.ID, L_D.ID, L_DI.ID, L_F.ID, L_I.ID, L_L.Value, L_T.ID) as LookupObjectID
						from	FieldType F
								inner join [Load] L on L.ID = @LoadID and L.[Object] = F.[Object] and L.ObjectID = F.ObjectID and F.[Type] = 'Lookup'
								inner join [LoadColumn] C on C.LoadID = L.ID and F.Name = C.Name
								inner join [LoadItemColumn] IC on IC.LoadID = C.LoadID and IC.ColumnIndex = C.ColumnIndex
								
								left join Artifact L_A on F.LookupObjectType in ('Artifact', 'ArtifactType') and L_A.ArtifactTypeID = F.LookupObjectID and (L_A.[Name] = IC.Value OR L_A.TextPath = IC.Value)
								left join Domain L_D on F.LookupObjectType in ('Domain', 'DomainType') and L_D.DomainTypeID = F.LookupObjectID and L_D.[Name] = IC.Value
								left join DomainItem L_DI on F.LookupObjectType = 'DomainItem' and L_DI.DomainID = F.LookupObjectID and L_DI.[Name] = IC.Value
								left join FusionAttribute L_F on F.LookupObjectType = 'FusionAttributeType' and L_F.FusionAttributeTypeID = F.LookupObjectID and (L_F.[Name] = IC.Value OR L_F.TextPath = IC.Value)
								left join [Intersect] L_I on F.LookupObjectType = 'IntersectType' and L_I.IntersectTypeID = F.LookupObjectID and L_I.[Name] = IC.Value
								left join [FieldLookupValue] L_L on F.ID = L_L.FieldTypeID and F.LookupObjectType = 'Lookup' and L_L.LookupObjectID = F.LookupObjectID and L_L.[Text] = IC.Value
								left join Taxonomy L_T on F.LookupObjectType in ('Taxonomy', 'TaxonomyType') and L_T.TaxonomyTypeID = F.LookupObjectID and (L_T.[Name] = IC.Value OR L_T.TextPath = IC.Value)
						where	F.[Type] = 'Lookup'
						) S on S.LoadID = T.LoadID and S.RowIndex = T.RowIndex and S.ColumnIndex = T.ColumnIndex

		-- PARSE any Subject AREA fields.  This is only in the case of artifacts.
		update	T
		set		T.LookupObject = S.LookupObject,
				T.LookupObjectID = S.LookupObjectID
		from	LoadItemColumn T
				inner join	(
							select	IC.LoadID,
									IC.RowIndex,
									IC.ColumnIndex,
									'TaxonomyType' as LookupObject,
									T.ID as LookupObjectID
							from	[Load] L 
									inner join [LoadColumn] C on L.ID = @LoadID and L.[Object] = 'ArtifactType' and C.LoadID = L.ID and C.Name = 'Subject Area'
									inner join [LoadItemColumn] IC on IC.LoadID = C.LoadID and IC.ColumnIndex = C.ColumnIndex
									inner join TaxonomyType T on T.[Name] = IC.Value
							) S on S.LoadID = T.LoadID and S.RowIndex = T.RowIndex and S.ColumnIndex = T.ColumnIndex

		-- PARSE any Domain Group fields.  This is only in the case of domains.
		update	T
		set		T.LookupObject = S.LookupObject,
				T.LookupObjectID = S.LookupObjectID
		from	LoadItemColumn T
				inner join	(
							select	IC.LoadID,
									IC.RowIndex,
									IC.ColumnIndex,
									'DomainGroup' as LookupObject,
									T.ID as LookupObjectID
							from	[Load] L 
									inner join [LoadColumn] C on L.ID = @LoadID and L.[Object] = 'DomainType' and C.LoadID = L.ID and C.Name = 'Domain Group'
									inner join [LoadItemColumn] IC on IC.LoadID = C.LoadID and IC.ColumnIndex = C.ColumnIndex
									inner join DomainGroup T on T.[Name] = IC.Value and T.DomainTypeID = @ObjectID
							) S on S.LoadID = T.LoadID and S.RowIndex = T.RowIndex and S.ColumnIndex = T.ColumnIndex

		-- PARSE any Parent Artifact fields.  This is only in the case of artifacts.
		update	T
		set		T.LookupObject = S.LookupObject,
				T.LookupObjectID = S.LookupObjectID
		from	LoadItemColumn T
				inner join	(
							select	IC.LoadID,
									IC.RowIndex,
									IC.ColumnIndex,
									'Artifact' as LookupObject,
									P.ID as LookupObjectID
							from	[Load] L 
									inner join ArtifactType T on L.ID = @LoadID and L.[Object] = 'ArtifactType' and L.ObjectID = T.ID
									inner join ArtifactType PT on PT.ID = T.ParentID
									inner join [LoadColumn] C on C.LoadID = L.ID and C.Name = 'Parent ' + PT.Name
									inner join [LoadItemColumn] IC on IC.LoadID = C.LoadID and IC.ColumnIndex = C.ColumnIndex
									inner join Artifact P on P.ArtifactTypeID = PT.ID and (P.[TextPath] = IC.Value or P.[Name] = IC.Value)
							) S on S.LoadID = T.LoadID and S.RowIndex = T.RowIndex and S.ColumnIndex = T.ColumnIndex


	if @Action = 'P'	--PROMOTION
	begin
		if @Object = 'AttributeType'
		begin
			-- Clean Owner Type field.
			update	LoadItemColumn
			set		Value = case when charindex('Type', Value) > 0 then Value else Value + 'Type' end
			where	LoadID = @LoadID and ColumnIndex = 1

			-- PARSE Owner Type fields.
			update	T
			set		T.LookupObject = S.LookupObject,
					T.LookupObjectID = S.LookupObjectID
			from	LoadItemColumn T
					inner join	(
								select	LI.LoadID,
										LI.RowIndex,
										C2.ColumnIndex,
										D.[Object] as LookupObject,
										D.ObjectID as LookupObjectID
								from	[Load] L
										inner join LoadItem LI on LI.LoadID = L.ID and L.ID = @LoadID
										inner join [LoadItemColumn] C1 on C1.LoadID = LI.LoadID and C1.RowIndex = LI.RowIndex and C1.ColumnIndex = 1 --'Owner Type' 
										inner join [LoadItemColumn] C2 on C2.LoadID = LI.LoadID and C2.RowIndex = LI.RowIndex and C2.ColumnIndex = 2 --'Owner Type Name'
										inner join cache.ObjectDetails D on D.[Object] = C1.Value and D.[Name] = C2.Value
								) S on S.LoadID = T.LoadID and S.RowIndex = T.RowIndex and S.ColumnIndex = T.ColumnIndex

			-- PARSE Owner fields.
			update	T
			set		T.LookupObject = S.LookupObject,
					T.LookupObjectID = S.LookupObjectID
			from	LoadItemColumn T
					inner join	(
								select	LI.LoadID,
										LI.RowIndex,
										C3.ColumnIndex,
										D.[Object] as LookupObject,
										D.ObjectID as LookupObjectID
								from	[Load] L
										inner join LoadItem LI on LI.LoadID = L.ID and L.ID = @LoadID
										--inner join [LoadItemColumn] C1 on	C1.LoadID = LI.LoadID	and C1.RowIndex = LI.RowIndex	and C1.ColumnIndex = 1 --'Owner Type' 
										inner join [LoadItemColumn] C2 on C2.LoadID = LI.LoadID and C2.RowIndex = LI.RowIndex and C2.ColumnIndex = 2 --'Owner Type Name'
										inner join [LoadItemColumn] C3 on C3.LoadID = LI.LoadID	and C3.RowIndex = LI.RowIndex and C3.ColumnIndex = 3 --'Owner Name'
										inner join cache.ObjectDetails D on D.[ObjectType] = C2.[LookupObject] and D.ObjectTypeID = C2.LookupObjectID and D.[Name] = C3.Value
								) S on S.LoadID = T.LoadID and S.RowIndex = T.RowIndex and S.ColumnIndex = T.ColumnIndex
		end

		declare @ResolvedObjects table ([Object] varchar(50), ObjectID int, [Action] varchar(25), LoadID int, RowIndex int)	--This captures the INSERTED/UPDATED objects from the merge statements below.

		if @Object = 'ArtifactType'
		begin
			declare @RequiresParent bit
			select	@RequiresParent =		case
												when ParentID is null then cast(0 as bit)
												else cast(1 as bit)
											end
									  from	ArtifactType 
									  where	ID = @ObjectID

			merge	Artifact T
			using	(
					select	O.LoadID,
							O.RowIndex,
							O.ArtifactTypeID,
							O.Name,
							D.Description,
							O.ParentID,
							O.TaxonomyTypeID
					from	(
							select	LI.LoadID,
									MIN(LI.RowIndex) as RowIndex,
									@ObjectID as ArtifactTypeID,
									IC_N.Value as Name,
									P.ParentID,
									IC_T.LookupObjectID as TaxonomyTypeID
							from	[LoadItem] LI
									inner join [LoadItemColumn] IC_N on IC_N.LoadID = LI.LoadID and IC_N.RowIndex = LI.RowIndex inner join LoadColumn C_N on C_N.LoadID = LI.LoadID and C_N.ColumnIndex = IC_N.ColumnIndex and C_N.Name = 'Name'
									inner join [LoadItemColumn] IC_T on IC_T.LoadID = LI.LoadID and IC_T.RowIndex = LI.RowIndex inner join LoadColumn C_T on C_T.LoadID = LI.LoadID and C_T.ColumnIndex = IC_T.ColumnIndex and C_T.Name = 'Subject Area' and IC_T.LookupObjectID is not null
									outer apply (
												select	I.LookupObjectID as ParentID
												from	[LoadItemColumn] I
														inner join LoadColumn C on I.LoadID = LI.LoadID and I.RowIndex = LI.RowIndex 
																						and C.LoadID = LI.LoadID and C.ColumnIndex = I.ColumnIndex and C.Name like 'Parent %'
												) P
							where	LI.LoadID = @LoadID
									and (
											(@RequiresParent = 1 and P.ParentID is not null) or
											@RequiresParent = 0
										)
							group by LI.LoadID,
									IC_N.Value,
									P.ParentID,
									IC_T.LookupObjectID
							) O
							outer apply (
								select	I.Value as Description
								from	[LoadItemColumn] I
										inner join LoadColumn C on I.LoadID = O.LoadID and I.RowIndex = O.RowIndex 
																		and C.LoadID = O.LoadID and C.ColumnIndex = I.ColumnIndex and C.Name = 'Description'
							) D
					) S
			on		(T.ArtifactTypeID = S.ArtifactTypeID and T.TaxonomyTypeID = S.TaxonomyTypeID and ((T.ParentID = S.ParentID and S.ParentID is not null) or (T.ParentID is null and S.ParentID is null)) and T.Name = S.Name)
			when	matched then
					update	set T.[Description] = IsNull(S.[Description], T.[Description]),
								T.[ParentID] = S.[ParentID],
								T.[Status] = 'Draft',
								T.TaxonomyTypeID = S.TaxonomyTypeID,
								T.UpdatedBy = @UpdatedBy,
								T.UpdatedOn = getutcdate()
			when	not matched then
					insert (ArtifactTypeID, TaxonomyTypeID, ParentID, Name, [Description], [Status], UpdatedOn, UpdatedBy)
					values (S.ArtifactTypeID, S.TaxonomyTypeID, S.ParentID, S.Name, S.[Description], 'Draft', getutcdate(), @UpdatedBy)
			output	'Artifact', inserted.ID, $action, S.LoadID, S.RowIndex into @ResolvedObjects;

			--update	T
			--set		T.Name = T.Name
			--from	Artifact T
			--		inner join @ResolvedObjects S on S.ObjectID = T.ID and S.[Action] = 'INSERT';

			if @RequiresParent = 1
			begin
				-- Update the LoadItem table with the IDs we recieved in the merge statements above.
				update	T
				set		T.StatusMessage = 'Parent could not be found.'
				from	LoadItem T
						left join @ResolvedObjects S on S.LoadID = T.LoadID and S.RowIndex = T.RowIndex
				where	S.ObjectID is null
			end

		end
		else if @Object = 'AttributeType'
		begin
			merge	[Attribute] T
			using	(
					select	I.LoadID,
							I.RowIndex,
							@ObjectID as AttributeTypeID,
							C.LookupObject as [Object],
							C.LookupObjectID as ObjectID
					from	[LoadItem] I
							inner join [LoadItemColumn] C on I.LoadID = @LoadID and C.LoadID = I.LoadID and C.RowIndex = I.RowIndex and C.ColumnIndex = 3
							and C.LookupObject is not null
							and C.LookupObjectID is not null
					) S
			on		(T.AttributeTypeID = S.AttributeTypeID and T.[ObjectType] = S.[Object] and T.[ObjectID] = S.[ObjectID] and T.ParentID = NULL)-- and T.Name = S.Name)
			when	matched then
					update	set T.[UpdatedOn] = getutcdate(),
								T.UpdatedBy = @UpdatedBy
			when	not matched then
					insert (AttributeTypeID, ObjectType, ObjectID, UpdatedOn, UpdatedBy)
					values (S.AttributeTypeID, S.[Object], S.ObjectID, getutcdate(), @UpdatedBy)
			output	'Attribute', inserted.ID, $action, S.LoadID, S.RowIndex into @ResolvedObjects;		
		end
		else if @Object = 'Domain'
		begin
			merge	DomainItem T
			using	(
					select	distinct
							LI.LoadID,
							LI.RowIndex,
							@ObjectID as DomainID,
							IC_C.Value as Code,
							IC_N.Value as Name,
							D.[Description]
					from	[LoadItem] LI
							inner join [LoadItemColumn] IC_C on IC_C.LoadID = LI.LoadID and IC_C.RowIndex = LI.RowIndex inner join LoadColumn C_C on C_C.LoadID = LI.LoadID and C_C.ColumnIndex = IC_C.ColumnIndex and C_C.Name = 'Code'
							inner join [LoadItemColumn] IC_N on IC_N.LoadID = LI.LoadID and IC_N.RowIndex = LI.RowIndex inner join LoadColumn C_N on C_N.LoadID = LI.LoadID and C_N.ColumnIndex = IC_N.ColumnIndex and C_N.Name = 'Name'
							outer apply (
										select	I.Value as Description
										from	[LoadItemColumn] I
												inner join LoadColumn C on I.LoadID = LI.LoadID and I.RowIndex = LI.RowIndex 
																			 and C.LoadID = LI.LoadID and C.ColumnIndex = I.ColumnIndex and C.Name = 'Description'
										) D
					where	LI.LoadID = @LoadID
					) S
			on		(T.DomainID = S.DomainID and T.Code = S.Code)
			when	matched then
					update	set T.[Name] = S.[Name],
								T.[Description] = IsNull(S.[Description],T.[Description]),
								T.[DomainID] = S.[DomainID],
								T.UpdatedBy = @UpdatedBy,
								T.UpdatedOn = getutcdate()
			when	not matched then
					insert (DomainID, Code, Name, [Description], UpdatedOn, UpdatedBy)
					values (S.DomainID, S.Code, S.Name, S.[Description], getutcdate(), @UpdatedBy)
			output	'DomainItem', inserted.ID, $action, S.LoadID, S.RowIndex into @ResolvedObjects;
		end
		else if @Object = 'DomainType'
		begin
			merge	Domain T
			using	(
					select	distinct
							LI.LoadID,
							LI.RowIndex,
							@ObjectID as DomainTypeID,
							IC_N.Value as Name,
							D.[Description],
							IC_G.LookupObjectID as DomainGroupID
					from	[LoadItem] LI
							inner join [LoadItemColumn] IC_N on IC_N.LoadID = LI.LoadID and IC_N.RowIndex = LI.RowIndex inner join LoadColumn C_N on C_N.LoadID = LI.LoadID and C_N.ColumnIndex = IC_N.ColumnIndex and C_N.Name = 'Name'
							outer apply (
										select	I.Value as Description
										from	[LoadItemColumn] I
												inner join LoadColumn C on I.LoadID = LI.LoadID and I.RowIndex = LI.RowIndex 
																			 and C.LoadID = LI.LoadID and C.ColumnIndex = I.ColumnIndex and C.Name = 'Description'
										) D
							inner join [LoadItemColumn] IC_G on IC_G.LoadID = LI.LoadID and IC_G.RowIndex = LI.RowIndex inner join LoadColumn C_G on C_G.LoadID = LI.LoadID and C_G.ColumnIndex = IC_G.ColumnIndex and C_G.Name = 'Domain Group'
					where	LI.LoadID = @LoadID
					) S
			on		(T.DomainTypeID = S.DomainTypeID and T.Name = S.Name)
			when	matched then
					update	set T.[Description] = IsNull(S.[Description],T.[Description]),
								T.[DomainGroupID] = S.[DomainGroupID],
								T.UpdatedOn = getutcdate(),
								T.UpdatedBy = @UpdatedBy
			when	not matched then
					insert (DomainTypeID, DomainGroupID, Name, [Description], UpdatedOn, UpdatedBy)
					values (S.DomainTypeID, S.DomainGroupID, S.Name, S.[Description], getutcdate(), @UpdatedBy)
			output	'Domain', inserted.ID, $action, S.LoadID, S.RowIndex into @ResolvedObjects;
		end
		else if @Object = 'FusionAttributeType'
		begin
			select 1;
		end
		else if @Object = 'TaxonomyType'
		begin
		--begin tran

			declare @currentLevel int,
			@maxLevel int,
			@rowCount int,
			@rowCurr int;

			select 
				@currentLevel = 0
				,@maxLevel = max(
					case when isnumeric(replace(Name,'Level','')) = 1 then
						replace(Name,'Level','') 
					else 
						0 
					end) 
			from 
				LoadColumn 
			where 
				LoadID = @LoadID and Name like 'Level%';
			

			declare @levels table (id int, ColumnIndex int, RowIndex int, [Level] varchar(50), Value varchar(250),MaxLevel int, TaxonomyID int, ParentID int, [Status] varchar(50));
			with v as
			(
				select L.ID, L.Object, L.ObjectID, LC.Name, LC.ColumnIndex, IC.RowIndex, IC.Value, replace(LC.Name,'Level','') as [Level], T.ID as TaxonomyID from [Load] L
				join LoadColumn LC on LC.LoadID = L.ID
				join LoadItemColumn IC on IC.LoadID = LC.LoadID AND IC.ColumnIndex = LC.ColumnIndex
				left join Taxonomy T on T.TaxonomyTypeID = L.ObjectID and T.[Level] = replace(LC.Name,'Level','') and T.Name = IC.Value
				where L.ID = @LoadID AND ltrim(rtrim(IC.Value)) != '' and LC.Name like 'Level%'  
			)
			insert into @levels
			select distinct
				row_number() over (partition by 1 order by v.[Level]) as ID,
				v.ColumnIndex
				,v.RowIndex
				,v.[Level]
				,v.Value
				,m.[Level] as MaxLevel
				,v.TaxonomyID
				,p.TaxonomyID as ParentID 
				,'UPDATE' as [Status]
			from v
			left join v p 
				on p.RowIndex = v.RowIndex and v.TaxonomyID is null and p.ColumnIndex = (v.ColumnIndex - 1)
			inner join v m on m.RowIndex = v.RowIndex and m.[Level] = (select max([Level]) from v where RowIndex = m.RowIndex)
			order by v.[Level] asc;

			--calculate hierarchy
			while @currentLevel <= @maxLevel
			begin
				set @currentLevel = @currentLevel + 1;
				
				update LV
				set LV.ParentID = P.ID
				from @levels LV
				left join @levels P on P.[Level] = (LV.[Level] - 1) AND LV.RowIndex = P.RowIndex
				where LV.[Level] = @currentLevel;
			end 

			--delete records that have a level > 1 and no parentid, missing info
			--delete from @levels where parentid is null and level > 1;

			select @rowCurr = 0, @rowCount = count(*) from @levels;

			while @rowCurr <= @rowCount
			begin
				set @rowCurr = @rowCurr + 1;

				--parent does not exist or leading columns were not filled
				if (select ParentID from @levels where id = @rowCurr) IS NULL AND (select Level from @levels where id = @rowCurr) > 1
				begin
					update @levels set [Status] = 'ERROR' where rowIndex = (select rowindex from @levels where id = @rowCurr);
					continue;
				end


				--update the TaxonomyID for records that do not yet have it
				if (select level from @levels where id = @rowCurr) = 1
				begin
					update LV
					set TaxonomyID = T.ID
					from @levels LV
					join Load L on L.ID = @LoadID
					join Taxonomy T on T.Name = LV.Value and T.ParentID is NULL and T.Level = LV.Level and T.TaxonomyTypeID = L.ObjectID
					where LV.ID = @rowCurr;
				end
				else
				begin
					update LV
					set TaxonomyID = T.ID
					from @levels LV
					left join @levels P on P.ID = LV.ParentID
					join Taxonomy T on T.Name = LV.Value and T.ParentID = P.TaxonomyID and T.Level = LV.Level
					where LV.ID = @rowCurr;
				end

				if (select TaxonomyID from @levels where id = @rowCurr) IS NULL
				begin
					--insert the new taxonomy
					insert into Taxonomy (TaxonomyTypeID, ParentID, Name, [Description], UpdatedOn, UpdatedBy)
					select	distinct
							L.ObjectID as TaxonomyTypeID
						,LVP.TaxonomyID as ParentID
						,LV.Value as Name
						,case when LV.Level = LV.MaxLevel then
							LI.Value
						else
							''
						END as Description
						,getdate() as UpdatedOn
						,@UpdatedBy as UpdatedBy
					from 
						@levels LV
					left join @levels LVP on LVP.ID = LV.ParentID
					join [Load] L on L.ID = @LoadID
					inner join LoadColumn LC on LC.Name = 'Description' and LC.LoadID = @LoadID
					inner join LoadItemColumn LI on LI.RowIndex = LV.RowIndex AND LI.ColumnIndex = LC.ColumnIndex AND LI.LoadID = @LoadID
					where
						LV.ID = @rowCurr

					update @levels set [Status] = 'INSERT' where id = @rowCurr;

					--set the levels taxonomy id after insert
					update LV
					set TaxonomyID = T.ID
					from @levels LV
					left join @levels P on P.ID = LV.ParentID
					join Taxonomy T on T.Name = LV.Value and coalesce(T.ParentID,-1) = coalesce(P.TaxonomyID,-1) and T.Level = LV.Level
					where LV.ID = @rowCurr;
				end
				
				--if level = max, update the description
				if (select level from @levels where id = @rowCurr) = (select maxlevel from @levels where id = @rowCurr)
				begin
					update	T
					set		T.Description = case when LI.Value = '' then T.Description else LI.Value end,
							T.UpdatedOn = getutcdate(),
							T.UpdatedBy = @UpdatedBy
					from	Taxonomy T
							join @levels LV on LV.ID = @rowCurr and T.ID = LV.TaxonomyID
							inner join LoadColumn LC on LC.Name = 'Description' and LC.LoadID = @LoadID
							inner join LoadItemColumn LI on LI.RowIndex = LV.RowIndex AND LI.ColumnIndex = LC.ColumnIndex AND LI.LoadID = @LoadID;

				end
			end --end while
			

			--remove error rows
			delete from @levels
			where rowindex in (select rowindex from @levels where status is null or status = 'ERROR');

						--insert object statuses
			insert into @ResolvedObjects ([Object], ObjectID, [Action], LoadID, RowIndex)
			select
				'Taxonomy',
				TaxonomyID,
				[Status],
				@LoadID,
				RowIndex
			from 
			@levels;

		end

		-- Update the LoadItem table with the IDs we recieved in the merge statements above.
		update	T
		set		T.[Object] = S.[Object],
				T.ObjectID = S.ObjectID,
				T.[Status] = 1,
				T.StatusMessage = case S.[Action]
									when 'INSERT' then 'Added item'
									when 'UPDATE' then 'Updated item'
									else NULL
									end
		from	LoadItem T
				inner join	@ResolvedObjects S on S.LoadID = T.LoadID and S.RowIndex = T.RowIndex

		-- Update the LoadItems that were not successfully added or updated.
		update	LoadItem
		set		[Status] = 0,
				[StatusMessage] = coalesce([StatusMessage], '') + ' Item could not be added nor updated.'
		where	LoadID = @LoadID
				and [ObjectID] is null
	end
	else
	begin
		-- This is for actions: R, U, S
		declare @current int,
				@max int,
				@sourceObject varchar(50),
				@sourceObjectID int,
				@targetObject varchar(50),
				@targetObjectID int,
				@intersectID int = null,
				@date datetime = getutcdate()

		declare @Intersects IDTable

		declare @sourceObjectTypeName nvarchar(1000),
				@sourceSubject nvarchar(500),
				@sourceName nvarchar(500),
					
				@targetObjectTypeName nvarchar(1000),
				@targetSubject nvarchar(500),
				@targetName nvarchar(500),
				
				@predicateID int,
				@rundate datetime = CURRENT_TIMESTAMP

		if @Action = 'S' -- SYNONYM (create synonyms from input spreadsheet)
		begin
			declare @synonymErrorDetailMessage varchar(200)
			
			select	@current = min(I.RowIndex),
					@max = max(I.RowIndex)
			from	LoadItem I
					inner join LoadItemColumn ST on ST.LoadID = I.LoadID and ST.RowIndex = I.RowIndex and St.ColumnIndex = 1			-- source object type
					inner join LoadItemColumn STN on STN.LoadID = I.LoadID and STN.RowIndex = I.RowIndex and StN.ColumnIndex = 2		-- source object type name
					inner join LoadItemColumn S on S.LoadID = I.LoadID and S.RowIndex = I.RowIndex and S.ColumnIndex = 4				-- source object name
					inner join LoadItemColumn TT on TT.LoadID = I.LoadID and TT.RowIndex = I.RowIndex and TT.ColumnIndex = 5			-- target object type
					inner join LoadItemColumn TTN on TTN.LoadID = I.LoadID and TTN.RowIndex = I.RowIndex and TTN.ColumnIndex = 6		-- target object type name
					inner join LoadItemColumn T on T.LoadID = I.LoadID and T.RowIndex = I.RowIndex and T.ColumnIndex = 8				-- target object name
			where	I.LoadID = @LoadID
			
			-- go row by row
			while @current <= @max
			begin
				--load the objects / id's for the focal, source, and target objects
				select	@sourceObject = ST.Value,
						@sourceObjectTypeName = STN.Value,
						@sourceName = S.Value,
						@sourceSubject = SS.Value,
						
						@targetObject = TT.Value,
						@targetObjectTypeName = TTN.Value,
						@targetName = T.Value,
						@targetSubject = TS.Value
				from	LoadItem I
						inner join LoadItemColumn ST on ST.LoadID = I.LoadID and ST.RowIndex = I.RowIndex and St.ColumnIndex = 1		-- source object type
						inner join LoadItemColumn STN on STN.LoadID = I.LoadID and STN.RowIndex = I.RowIndex and StN.ColumnIndex = 2	-- source object type name
						inner join LoadItemColumn SS on SS.LoadID = I.LoadID and SS.RowIndex = I.RowIndex and SS.ColumnIndex = 3		-- source object subject
						inner join LoadItemColumn S on S.LoadID = I.LoadID and S.RowIndex = I.RowIndex and S.ColumnIndex = 4			-- source object name
						inner join LoadItemColumn TT on TT.LoadID = I.LoadID and TT.RowIndex = I.RowIndex and TT.ColumnIndex = 5		-- target object type
						inner join LoadItemColumn TTN on TTN.LoadID = I.LoadID and TTN.RowIndex = I.RowIndex and TTN.ColumnIndex = 6	-- target object type name
						inner join LoadItemColumn TS on TS.LoadID = I.LoadID and TS.RowIndex = I.RowIndex and TS.ColumnIndex = 7		-- target object subject
						inner join LoadItemColumn T on T.LoadID = I.LoadID and T.RowIndex = I.RowIndex and T.ColumnIndex = 8			-- target object name
				where	I.LoadID = @LoadID and I.RowIndex = @current

				select @sourceObjectID = 0, @targetObjectID = 0, @predicateID = 0;

				select @predicateID = min(ID) from [Predicate] where [Type] = 6;				

				if @sourceObject = 'Artifact'
				begin
					select	top 1
							@sourceObjectID = cod.objectid										
					from	[cache].objectdetails cod
							inner join artifact a on (cod.objectid = a.id)
							inner join taxonomytype t on (a.taxonomytypeid = t.id)
					where	cod.[object] = @sourceObject and cod.textpath = @sourceName and cod.objecttypename = @sourceObjectTypeName and t.Name = @sourceSubject
				end
				else
				begin
					-- load source object
					select	top 1
							@sourceObjectID = cod.objectid						
					from	[cache].objectdetails cod
					where	cod.[object] = @sourceObject and cod.textpath = @sourceName and cod.objecttypename = @sourceObjectTypeName
				end

				if @targetObject = 'Artifact'
				begin
					-- load target object
					select	top 1
							@targetObjectID = cod.objectid												
					from	[cache].objectdetails cod
							inner join artifact a on (cod.objectid = a.id)
							inner join taxonomytype t on (a.taxonomytypeid = t.id)
					where	cod.[object] = @targetObject and cod.textpath = @targetName and cod.objecttypename = @targetObjectTypeName and t.Name = @targetSubject
				end
				else
				begin
					-- load target object
					select	top 1
							@targetObjectID = cod.objectid												
					from	[cache].objectdetails cod
					where	cod.[object] = @targetObject and cod.textpath = @targetName and cod.objecttypename = @targetObjectTypeName
				end

				--debug 
				--select @sourceObjectID, @sourceObject, @targetObjectID, @targetObject, @predicateID

				--if all are provided we are good otherwise error
				if @sourceObjectID > 0 and @targetObjectID > 0 and @predicateID > 0
					begin
						-- add intersect between source / target if one doesn't exist
						exec [dbo].[AddRelationship] @UpdatedBy, @rundate, @sourceObject, @sourceObjectID, 2, null, null, @targetObject, @targetObjectID;

						update	LoadItem
						set		[Status] = 1,
								StatusMessage = 'Successfully added synonym'
						where	LoadID = @LoadID
								and RowIndex = @current
					end -- if valid
				else
					begin
						set @synonymErrorDetailMessage = '';

						if @sourceObjectID = 0
						begin
							set @synonymErrorDetailMessage = @synonymErrorDetailMessage + '  Source object is invalid.';
						end

						if @targetObjectID = 0
						begin
							set @synonymErrorDetailMessage = @synonymErrorDetailMessage + '  Target object is invalid.';
						end

						if @predicateID = 0
						begin
							set @synonymErrorDetailMessage = @synonymErrorDetailMessage + '  No predicate of type synonym.';
						end

						update	LoadItem
						set		[Status] = 0,
								StatusMessage = 'Failed to add synonym. ' + @synonymErrorDetailMessage + ' [source id:' + convert(varchar(10),@sourceObjectID) + ' type:' + @sourceObject +'] [target id:' + convert(varchar(10), @targetObjectID) + ' type:' + @targetObject + ']'
						where	LoadID = @LoadID
								and RowIndex = @current
					end -- else not valid
				
				set @current = @current + 1
			end

		end

		if @Action = 'R' OR @Action = 'U'	--UNRELATION (Remove existing relation)
		begin
			-- PARSE both sides.
			update	T
			set		T.LookupObject = S.LookupObject,
					T.LookupObjectID = S.LookupObjectID
			from	LoadItemColumn T
					inner join	(
								select	IC.LoadID,
										IC.RowIndex,
										IC.ColumnIndex,
										T.[Object] as LookupObject,
										T.ObjectID as LookupObjectID
								from	[Load] L
										inner join [LoadColumn] C on C.LoadID = L.ID and L.ID = @LoadID
										inner join [LoadItemColumn] IC on IC.LoadID = C.LoadID and IC.ColumnIndex = C.ColumnIndex
										inner join IntersectType IT on IT.ID = @ObjectID
										inner join cache.ObjectDetails T on (T.[TextPath] = IC.Value or T.Name = IC.Value) and ( (T.[ObjectType] = IT.Subject and T.ObjectTypeID = IT.SubjectID) OR (T.[ObjectType] = IT.Object and T.ObjectTypeID = IT.ObjectID) )
								) S on S.LoadID = T.LoadID and S.RowIndex = T.RowIndex and S.ColumnIndex = T.ColumnIndex
			update	T
			set		T.[Status] = 0,
					T.StatusMessage =	REPLACE(REPLACE(
											STUFF(
											(
											select	LIC.Value + ' could not be located in the <a href="' + T.Url + '">' + T.Name + '</a> list, '
											from	[Load] L
													inner join IntersectType IT on IT.ID = L.ObjectID and L.ID = @LoadID
													inner join [LoadItemColumn] LIC on LIC.LoadID = L.ID and LIC.ColumnIndex = IC.ColumnIndex and LIC.RowIndex = IC.RowIndex and LIC.LookupObject is null
													inner join cache.ObjectDetails T on (T.[Object] = IT.[Subject] and T.ObjectID = IT.SubjectID) OR (T.[Object] = IT.[Object] and T.ObjectID = IT.ObjectID)
											for xml path('')
											), 1, 0, ''),
										'&lt;', '<'), '&gt;', '>')
			from	[LoadItem] T
					inner join [LoadItemColumn] IC on T.LoadID = @LoadID and IC.LoadID = T.LoadID and IC.RowIndex = T.RowIndex and IC.LookupObject IS NULL and IC.LookupObjectID is null

			select	@current = min(I.RowIndex),
					@max = max(I.RowIndex)
			from	LoadItem I
					inner join LoadItemColumn S on S.LoadID = I.LoadID and S.RowIndex = I.RowIndex and S.ColumnIndex = 1 and S.LookupObject is not null
					inner join LoadItemColumn T on T.LoadID = I.LoadID and T.RowIndex = I.RowIndex and T.ColumnIndex = 2 and T.LookupObject is not null
			where	I.LoadID = @LoadID



		end

		while @current <= @max
		begin
			select	@sourceObject = S.LookupObject,
					@sourceObjectID = S.LookupObjectID,
					@targetObject = T.LookupObject,
					@targetObjectID = T.LookupObjectID
			from	LoadItem I
					inner join LoadItemColumn S on S.LoadID = I.LoadID and S.RowIndex = I.RowIndex and S.ColumnIndex = 1 and S.LookupObject is not null
					inner join LoadItemColumn T on T.LoadID = I.LoadID and T.RowIndex = I.RowIndex and T.ColumnIndex = 2 and T.LookupObject is not null
			where	I.LoadID = @LoadID and I.RowIndex = @current

			set		@intersectID = null

			select	@IntersectID = ID 
			from	[Intersect]
			where	(Subject = @sourceObject and SubjectID = @sourceObjectID and Object = @targetObject and ObjectID = @targetObjectID) OR
					(Object = @sourceObject and ObjectID = @sourceObjectID and Subject = @targetObject and SubjectID = @targetObjectID)

			if @Action = 'R'	--RELATION
			begin
				if @intersectID is null
				begin
					insert into [Intersect] (IntersectTypeID, Classification, Subject, SubjectID, Object, ObjectID, CreatedBy, CreatedOn, UpdatedBy, UpdatedOn) 
					values		(@ObjectID, 2, @sourceObject, @sourceObjectID, @targetObject, @targetObjectID, 0, @date, 0, @date)

					set @intersectID = SCOPE_IDENTITY()
				end

				if @intersectID is not null
				begin
					update	LoadItem
					set		[Object] = 'Intersect',
							ObjectID = @intersectID,
							[Status] = 1,
							StatusMessage = 'Successfully created/updated relationship'
					where	LoadID = @LoadID
							and RowIndex = @current
				end
				else
				begin
					update	LoadItem
					set		[Status] = 0,
							StatusMessage = 'Failed to create relationship'
					where	LoadID = @LoadID
							and RowIndex = @current
				end
			end --end R

			if @Action = 'U'	--UNRELATION
			begin
				if @intersectID is not null
				begin
					begin try
						if exists(	select 1 
									from	MapItem
									where	SourceIntersectID = @intersectID or TargetIntersectID = @intersectID
								 )
						begin
							update	LoadItem
							set		[Object] = 'Intersect',
									ObjectID = @intersectID,
									[Status] = 0,
									StatusMessage = 'Unable to remove relationship as it is involved in lineage.'
							where	LoadID = @LoadID
									and RowIndex = @current
						end
						else
						begin
							delete [Intersect] where ID = @intersectID

							update	LoadItem
							set		[Object] = 'Intersect',
									ObjectID = @intersectID,
									[Status] = 1,
									StatusMessage = 'Successfully removed relationship'
							where	LoadID = @LoadID
									and RowIndex = @current
						end
					end try
					begin catch
							update	LoadItem
							set		[Object] = 'Intersect',
									ObjectID = @intersectID,
									[Status] = 0,
									StatusMessage = 'Unable to remove relationship due to the following error: ' + ERROR_MESSAGE()
							where	LoadID = @LoadID
									and RowIndex = @current
					end catch
				end
				else
				begin
					update	LoadItem
					set		[Object] = 'Intersect',
							ObjectID = NULL,
							[Status] = 0,
							StatusMessage = 'Relationship not found'
					where	LoadID = @LoadID
							and RowIndex = @current
				end
			end --end U

			insert into @Intersects values (@intersectID)

			set @current = @current + 1
		end

		if @Action = 'R'
		begin
			exec cache.SynchronizeRelationships @Intersects
		end

	end --end IF statement to check if action = P or NOT

	if @Action = 'P' or @Action = 'R'
	begin
		-- Load custom fields for the inserted/updated object above.
		merge	Field T
		using	(
				select	distinct
						FT.ID as FieldTypeID,
						L.[Object],
						L.ObjectID,
						IC.LookupObjectID--max(IC.LookupObjectID) as LookupObjectID
				from	LoadItem L
						inner join LoadColumn C on C.LoadID = L.LoadID
						inner join LoadItemColumn IC on IC.LoadID = C.LoadID and L.RowIndex = IC.RowIndex and IC.ColumnIndex = C.ColumnIndex and IC.LookupObjectID is not null
						inner join FieldType FT on FT.[Object] = @Object and FT.ObjectID = @ObjectID and FT.Name = C.Name
				where	L.ObjectID is not null
						and L.LoadID = @LoadID
				--group by	FT.ID,
				--			L.[Object],
				--			L.ObjectID
				) S
		on		(T.FieldTypeID = S.FieldTypeID and T.ObjectType = S.[Object] and T.ObjectID = S.ObjectID)
		when	matched then
				update	set Value = S.LookupObjectID
		when	not matched then
				insert (ObjectType, ObjectID, FieldTypeID, Value)
				values (S.[Object], S.ObjectID, S.FieldTypeID, S.LookupObjectID);

		merge	Field T
		using	(
				select	distinct
						FT.ID as FieldTypeID,
						L.[Object],
						L.ObjectID,
						case 
							when FT.[Type] = 'Boolean' and LOWER(IC.Value) in ('y', 'yes', 'true', 't', '1') then 'true'
							when FT.[Type] = 'Boolean' and LOWER(IC.Value) not in ('y', 'yes', 'true', 't', '1') then 'false'
							else IC.Value
						end as Value
				from	LoadItem L
						inner join LoadColumn C on C.LoadID = L.LoadID
						inner join LoadItemColumn IC on IC.LoadID = C.LoadID and L.RowIndex = IC.RowIndex and IC.ColumnIndex = C.ColumnIndex and IC.LookupObjectID is null
						inner join FieldType FT on FT.[Object] = @Object and FT.ObjectID = @ObjectID and FT.Name = C.Name and FT.[Type] <> 'Lookup'
				where	L.ObjectID is not null
						and L.LoadID = @LoadID
				) S
		on		(T.FieldTypeID = S.FieldTypeID and T.ObjectType = S.[Object] and T.ObjectID = S.ObjectID)
		when	matched then
				update	set Value = S.Value
		when	not matched then
				insert (ObjectType, ObjectID, FieldTypeID, Value)
				values (S.[Object], S.ObjectID, S.FieldTypeID, S.Value);
	end

	update	[Load] 
	set		DateCompleted = getutcdate()
	where	ID = @LoadID
end
GO

ALTER PROCEDURE [dbo].[ProcessEagleMCToEagleFieldRelations]
	@StagingFileID int,
	@FusionID int
AS
BEGIN	
	SET NOCOUNT ON;
		
	declare	@eagleStreamID int,
			@streamToFieldIntersectTypeID int,				
			@currentEagleFusionId int;

	declare	@IDList Table(IntersectID int,StageID Int);

	declare	@Intersects IDTable;

	declare	@MessageStreamFussionAttributeID int,
			@EagleFieldFusionAttributeID int;

	select	@MessageStreamFussionAttributeID = 196;
	select	@EagleFieldFusionAttributeID = 205;

	-- load the stream that we want to add relations ships for    
	select	@eagleStreamID = fusionattributeid 
	from	[fusion].[stagingfile] 
	where	id = @StagingFileID and 
			fusionID = @FusionID;
			
	if @eagleStreamID is null
	begin
		raiserror('ERROR : UNABLE TO LOCATE SPECIFIED STREAM INFORMATION FOR INPUT FUSION ID / STAGING ID', 15, 1);
		return;
	end;

	select @currentEagleFusionId = FusionID from [dbo].[fusionattribute] where id = @eagleStreamID

	-- add relationships for Stream (196) to Eagle DB Columns (205)
	-- using star tag field that is a field for for fusionattribute type 205 lookup fields to add rels for
	-- todo pull to separate proc
	if @eagleStreamID is not null
	begin
			Declare @StreamToFieldList Table(FieldFusionAttributeID int, StreamFusionAttributeID int,IntersectTypeID int, ID int);
			
			-- load the intersect type ids
			select	@streamToFieldIntersectTypeID = ID
			from	IntersectType
			where	(Subject = 'FusionAttributeType' and Object = 'FusionAttributeType') 
					and	( 
						(SubjectID = @MessageStreamFussionAttributeID and ObjectID = @EagleFieldFusionAttributeID) OR
						(SubjectID = @EagleFieldFusionAttributeID and ObjectID = @MessageStreamFussionAttributeID)
						)

			if @streamToFieldIntersectTypeID is null
			begin
				raiserror('ERROR : UNABLE TO LOCATE INTERSECT TYPE IDS FOR EAGLE TO EAGLE MESSAGE STREAMS', 15, 1);
				return;
			end;

			-- insert into in memory table variable the values we want to add intersects for
			insert into @StreamToFieldList
				select		fa.id, 
							sf.FusionAttributeID, 
							@streamToFieldIntersectTypeID, 
							ROW_NUMBER() OVER (Order by fa.id) AS 'RowNumber'
				from		field f 
							inner join FusionAttribute fa on f.ObjectID = fa.ID and fa.fusionid = @currentEagleFusionId
							inner join FieldType ft on f.fieldtypeid = ft.id
							inner join fusion.StagingFileItem sfi on sfi.tag = f.value				
							inner join fusion.StagingFile sf on sfi.stagingfileid = sf.id
							left join	(
										select	SubjectID,
												ObjectID,
												1 as hasExisting
										from	[Intersect]
										where	Subject = 'FusionAttribute' and Object= 'FusionAttribute'
										) existing on ( (existing.SubjectID = sf.FusionAttributeID and existing.ObjectID = fa.ID) OR (existing.SubjectID = fa.ID and existing.ObjectID = sf.FusionAttributeID) )
				where		fa.fusionattributetypeid = @EagleFieldFusionAttributeID and 
							ft.name = 'startag' and 
							sfi.stagingfileid = @StagingFileID and 
							existing.hasExisting is null
				group by	fa.id, sf.FusionAttributeID  -- grouping is used to eliminate duplicate star tag relations

			--insert intersect records and save there id's
			-- trick is to use merge to keep the sequence id and staging row ids
			-- http://stackoverflow.com/questions/15614261/using-output-clause-to-insert-value-not-in-inserted
			MERGE
				INTO    [Intersect] d
				USING   (
							SELECT	sr.IntersectTypeID, 
									2 as class,
									sr.ID as srID,
									'FusionAttribute' as Subject,
									sr.StreamFusionAttributeID as SubjectID,
									'FusionAttribute' as Object,
									sr.FieldFusionAttributeID as ObjectID
							FROM	@StreamToFieldList sr							
						) s
				ON      (1 = 0)
				WHEN NOT MATCHED THEN
				INSERT  (IntersectTypeID, Classification, Description, Subject, SubjectID, Object, ObjectID)
				VALUES  (s.IntersectTypeID, s.class, NULL, s.Subject, s.SubjectID, s.Object, s.ObjectID)
				OUTPUT  INSERTED.ID, s.srID into @IDList;
	end;
end
GO

ALTER PROCEDURE [fusion].[ProcessEagleMCToBloombergRelations]	
	@StagingFileID int,
	@FusionID int
AS
BEGIN	
	SET NOCOUNT ON;
	
	
	declare		@eagleStreamID int;				
	declare		@IntersectCount int;
	Declare		@IDList Table(IntersectID int,StageID Int);
	declare		@Intersects IDTable;
	declare		@fieldToBBIntersectTypeID int;

	-- load the panel that we want to add relations ships for
    
	select @eagleStreamID = fusionattributeid from [fusion].[stagingfile] where id = @StagingFileID and fusionID = @FusionID
	
	if @eagleStreamID is null
	begin
		raiserror('ERROR : UNABLE TO LOCATE SPECIFIED STREAM INFORMATION FOR INPUT FUSION ID / STAGING ID', 15, 1);
		return;
	end;
			
	exec ProcessEagleMCToEagleFieldRelations @StagingFileID, @FusionID

	exec [fusion].[ProcessEagleMCToBBMnemonic] @StagingFileID, @FusionID


	-- add relations for Eagle Field (205) to Bloomberg mnemonic (301)
	if @eagleStreamID is not null
	begin
		Declare @BBToFieldList Table(FieldFusionAttributeID int, StreamFusionAttributeID int, IntersectTypeID int, ID int);
		
		-- load the intersect id's for message stream to bb mnemonic	

		select	@fieldToBBIntersectTypeID = ID
			from	[IntersectType]
			where	Subject = 'FusionAttributeType' and 
					Object = 'FusionAttributeType' and 
					(
						( SubjectID = 205 and ObjectID = 301 ) OR
						( SubjectID = 301 and ObjectID = 205 )
					)

		if @fieldToBBIntersectTypeID is null
		begin
			raiserror('ERROR : UNABLE TO LOCATE INTERSECT TYPE IDS FOR EAGLE TO BLOOMBERG INTERSECT', 15, 1);
			return;
		end

		-- load into memory the id's that we need to add intersects for
		insert into @BBToFieldList
			select	fa.id as 'fieldID', faBB.id as 'bbID', @fieldToBBIntersectTypeID, ROW_NUMBER() OVER (Order by sfi.id) AS 'RowNumber'
			from	field f 
					inner join fusionAttribute fa on (f.ObjectID = fa.ID)
					inner join fieldtype ft on (f.fieldtypeid = ft.id)
					inner join [fusion].[StagingFileItem] sfi on (sfi.tag = f.value)				
					inner join [fusion].[StagingFile] sf on (sfi.stagingfileid = sf.id)						
					inner join fusionAttribute faBB on (faBB.Name = sfi.value and faBB.fusionattributetypeid = 301)		
					left join [Intersect] I on	I.IntersectTypeID = @fieldToBBIntersectTypeID and 
												I.Subject = 'FusionAttribute' and 
												I.Object ='FusionAttribute' and
												(
													( I.SubjectID = faBB.ID and I.ObjectID = fa.ID ) OR
													( I.SubjectID = fa.ID and I.ObjectID = faBB.ID )
												)
			where	fa.fusionattributetypeid = 205 and 
					ft.name = 'startag' and 
					sfi.stagingfileid = @StagingFileID and 
					I.ID is null;

			MERGE
				INTO    [Intersect] d
				USING   (
							SELECT	IntersectTypeID, 
									ID,
									StreamFusionAttributeID as SubjectID,
									FieldFusionAttributeID as ObjectID
							FROM	@BBToFieldList
						) s
				ON      (1 = 0)
				WHEN NOT MATCHED THEN
				INSERT  (IntersectTypeID, Classification, Subject, SubjectID, Object, ObjectID)
				VALUES  (s.IntersectTypeID, 2, 'FusionAttribute', s.SubjectID, 'FusionAttribute', s.ObjectID)
				OUTPUT  INSERTED.ID, s.ID into @IDList;										

			insert into @Intersects 
				select idl.intersectid from @IDList idl;
						
			select @IntersectCount = count(1) from @Intersects
			if @IntersectCount > 0 
			begin
				EXEC cache.SynchronizeRelationships @Intersects
			end
	end;
END
GO

ALTER FUNCTION [dbo].[ArtifactNgSiteNavigation](@id int)
RETURNS XML
WITH RETURNS NULL ON NULL INPUT
BEGIN 
	RETURN 
	(
	SELECT	name,
			url,
			'Menu_AT' + cast(id as varchar(15)) as menuID,
			0 as feature,
			dbo.ArtifactNgSiteNavigation(id) as items
	FROM	(
			--SELECT	A.name,
			--		A.url,
			--		NULL AS items
			--FROM	(
					SELECT		TOP 1000
								a.id,
								a.name,
								dbo.GenerateNgObjecturl('ArtifactType', a.ID, 0) As url
					FROM		ArtifactType a
					LEFT JOIN SiteNav v on v.ObjectID = a.ID and v.Object = 'ArtifactType'
					WHERE		a.ParentID = @id AND v.ObjectID IS NULL
					ORDER BY	name
			--		) A
			) BG
			FOR XML PATH('nav'), TYPE
	)
END
GO

ALTER FUNCTION [dbo].[CustomSiteNavigation]
(
	@id int
)
RETURNS XML
WITH RETURNS NULL ON NULL INPUT
AS
BEGIN
	 RETURN 
    (
        SELECT  v.name
                , v.[Route] AS url
				, 0 as feature,
				case when v.Object = 'ArtifactType' then
					dbo.ArtifactNgSiteNavigation(a.id)
				when v.Object = 'PolicyTypeClass' then
				        (SELECT	name, 
						        dbo.GenerateNgObjectUrl('PolicyType', ID, 0)  As url,
						        0 as feature
				        FROM	PolicyType
				        WHERE	PolicyTypeClassID = pc.ID
				        FOR XML PATH('nav'), TYPE)
				when v.Object = 'TaxonomyTypeClass' then
					(SELECT name, 
							dbo.GenerateNgObjectUrl('TaxonomyType', id, 0)  As url,
							0 as feature
					FROM	TaxonomyType
					WHERE	TaxonomyTypeClassID = tc.ID
					FOR XML PATH('nav'), TYPE)
				when v.Object = 'TaxonomyType' or v.Object = 'PolicyType' then
					null
				else
					[dbo].CustomSiteNavigation(v.id)
				end as items
        FROM    dbo.SiteNav v
		left join artifacttype a on a.id = v.objectID and v.Object = 'ArtifactType'
		left join policytypeclass pc on pc.id = v.objectID and v.Object = 'PolicyTypeClass'
		left join taxonomytypeclass tc on tc.id = v.objectid and v.object = 'TaxonomyTypeClass'
        WHERE   v.ParentID = @id
        FOR XML PATH('nav'),TYPE
    )
END
GO

create view SiteNavAvailable as
	select
		u.ID as ObjectID,
		u.Name,
		u.url as Route,
		u.Object,
		null as SortOrder,
		u.ParentID as ParentID
	from
	(
		select
		ID,
		ParentID,
		Name,
		dbo.GenerateNgObjectUrl('ArtifactType', ID, 0) As url,
		'ArtifactType' as [Object]
		FROM ArtifactType
		
		UNION ALL
		
		SELECT
		ID,
		null as ParentID,
		Name,
		'a/model/classification/' + name as url,
		'TaxonomyTypeClass' as [Object]
		from TaxonomyTypeClass

		UNION ALL
		
		SELECT
		ID,
		TaxonomyTypeClassID as ParentID,
		Name,
		dbo.GenerateNgObjectUrl('TaxonomyType', ID, 0)  As url,
		'TaxonomyType' as [Object]
		FROM TaxonomyType
		
		UNION ALL
		
		SELECT
		ID,
		null as ParentID,
		Name,
		'a/home' as url,
		'PolicyTypeClass' as [Object]
		from PolicyTypeClass
		
		UNION ALL
		
		SELECT
		ID,
		PolicyTypeClassID as ParentID,
		Name,
		dbo.GenerateNgObjectUrl('PolicyType', ID, 0)  As url,
		'PolicyType' as [Object]
		FROM PolicyType
	) u
	left join SiteNav v on v.Object = u.Object and v.ObjectID = u.ID
	where v.ObjectID is null
GO


create view SiteNavFlat as

	select
		u.ID as ObjectID,
		u.Name,
		u.url as Route,
		u.Object,
		null as SortOrder,
		u.ParentID as ParentID
	from
	(
		select
		ID,
		ParentID,
		Name,
		dbo.GenerateNgObjectUrl('ArtifactType', ID, 0) As url,
		'ArtifactType' as [Object]
		FROM ArtifactType
		
		UNION ALL
		
		SELECT
		ID,
		null as ParentID,
		Name,
		'a/model/classification/' + name as url,
		'TaxonomyTypeClass' as [Object]
		from TaxonomyTypeClass

		UNION ALL
		
		SELECT
		ID,
		TaxonomyTypeClassID as ParentID,
		Name,
		dbo.GenerateNgObjectUrl('TaxonomyType', ID, 0)  As url,
		'TaxonomyType' as [Object]
		FROM TaxonomyType
		
		UNION ALL
		
		SELECT
		ID,
		null as ParentID,
		Name,
		'a/home' as url,
		'PolicyTypeClass' as [Object]
		from PolicyTypeClass
		
		UNION ALL
		
		SELECT
		ID,
		PolicyTypeClassID as ParentID,
		Name,
		dbo.GenerateNgObjectUrl('PolicyType', ID, 0)  As url,
		'PolicyType' as [Object]
		FROM PolicyType
	) u
GO

CREATE PROCEDURE GetAvailableSiteNavigation
AS
BEGIN
	SET NOCOUNT ON;

	select
		u.ID as ObjectID,
		u.Name,
		u.url as Route,
		u.Object,
		null as SortOrder,
		null as ParentID
	from
	(
		select
		ID,
		Name,
		dbo.GenerateNgObjectUrl('ArtifactType', ID, 0) As url,
		'ArtifactType' as [Object]
		FROM ArtifactType
		
		UNION ALL
		
		SELECT
		ID,
		Name,
		'a/model/classification/' + name as url,
		'TaxonomyTypeClass' as [Object]
		from TaxonomyTypeClass

		UNION ALL

		SELECT
		ID,
		Name,
		dbo.GenerateNgObjectUrl('TaxonomyType', ID, 0)  As url,
		'TaxonomyType' as [Object]
		FROM TaxonomyType
		
		UNION ALL
		
		SELECT
		ID,
		Name,
		'a/home' as url,
		'PolicyTypeClass' as [Object]
		from PolicyTypeClass
		
		UNION ALL
		
		SELECT
		ID,
		Name,
		dbo.GenerateNgObjectUrl('PolicyType', ID, 0)  As url,
		'PolicyType' as [Object]
		FROM PolicyType
	) u
	left join SiteNav v on v.Object = u.Object and v.ObjectID = u.ID
	where v.ObjectID is null 
END
GO

CREATE PROCEDURE [dbo].[GetSiteNavigation]
AS
BEGIN
	SET NOCOUNT ON;

SELECT	n.Name as MenuID,
		n.SortOrder,
		0 as Feature,
		NULL AS Items
FROM SiteNav n
WHERE n.Name = '#Monitor'
UNION ALL

SELECT	n.Name as MenuID,
		n.SortOrder,
		0 as Feature,
		NULL AS Items
FROM SiteNav n
WHERE n.Name = '#Home'
UNION ALL

SELECT	n.Name as MenuID,
		n.SortOrder,
		0 as Feature,
		(
			SELECT	name,
					url,
					0 as feature,
					dbo.ArtifactNgSiteNavigation(id) as items
			FROM	(
					SELECT		TOP 1000
								a.id,
								a.name,
								dbo.GenerateNgObjectUrl('ArtifactType', a.ID, 0) As url
					FROM		ArtifactType a
					left join SiteNav v on v.ObjectID = a.ID and v.Object = 'ArtifactType'
					WHERE		a.ParentID IS NULL and v.ObjectID is null
					ORDER BY	name
					) BG
					FOR XML PATH('nav'), TYPE
		) AS Items
FROM SiteNav n
WHERE n.Name = '#Glossary'

UNION ALL

SELECT	n.Name as MenuID,
		n.SortOrder,
		0 as Feature,
		(
		SELECT	ft.name, 
				'a/model/classification/' + ft.name As url,
				0 as feature,
				(
				SELECT	t.name, 
						dbo.GenerateNgObjectUrl('TaxonomyType', t.ID, 0)  As url,
						0 as feature
				FROM	TaxonomyType t
				LEFT JOIN SiteNav v on v.ObjectID = t.ID and v.Object = 'TaxonomyType'
				WHERE	TaxonomyTypeClassID = FT.ID and v.ObjectID is null
				FOR XML PATH('nav'), TYPE
				) AS items	
		FROM	(
                select top 100 percent ID, name from TaxonomyTypeClass C where exists(select 1 from TaxonomyType where TaxonomyTypeClassID = C.ID) order by name
				) FT
		LEFT JOIN SiteNav v on v.ObjectID = FT.ID and v.Object ='TaxonomyTypeClass'
		WHERE v.ObjectID IS NULL
		FOR XML PATH('nav'), TYPE
		) AS Items
FROM SiteNav n
WHERE n.Name = '#Models'

UNION ALL

		
SELECT	n.Name as MenuID,
		n.SortOrder,
		1 as Feature, 
		(
        select  *
        from    (
		        SELECT	ft.name, 
				        'a/' As url,
				        0 as feature,
				        (
				        SELECT	p.name, 
						        dbo.GenerateNgObjectUrl('PolicyType', p.ID, 0)  As url,
						        0 as feature
				        FROM	PolicyType p
						LEFT JOIN SiteNav v on v.ObjectID = p.id and v.Object = 'PolicyType'
				        WHERE	PolicyTypeClassID = FT.ID and v.ObjectID is null
				        FOR XML PATH('nav'), TYPE
				        ) AS items	
		        FROM	(
                        select top 100 percent ID, name from PolicyTypeClass C where exists(select 1 from PolicyType where PolicyTypeClassID = C.ID) order by name
				        ) FT
				LEFT JOIN SiteNav v on v.ObjectID = ft.ID and v.Object = 'PolicyTypeClass'
				WHERE v.ObjectID is null
				union all
				SELECT	'Rules' AS name, 
						'a/rule' AS url, 
						0 as feature,
						NULL AS items
                ) as mo
		FOR XML PATH('nav'), TYPE
		) AS Items
FROM SiteNav n
WHERE n.Name = '#Policy'

UNION ALL

SELECT	n.Name as MenuID,
		n.SortOrder,
		0 as Feature,
		(
		SELECT	name, 
				dbo.GenerateNgObjectUrl('DomainType', ID, 0)  As url,
				0 as feature
		FROM	DomainType
		FOR XML PATH('nav'), TYPE				
		) AS Items
FROM SiteNav n
WHERE n.Name = '#Reference'

UNION ALL

SELECT	n.Name as MenuID,
		n.SortOrder,
		2 as Feature,
		(
		SELECT		name, 
					dbo.GenerateNgObjectUrl('FusionType', FT.ID, 0)  As url,
					2 as feature,
					(
					SELECT		name, 
								dbo.GenerateObjectUrl('Fusion', FT.ID, Fusion.ID)  As url,
								'F' + cast(Fusion.ID as varchar(15)) as menuID,
								2 as feature
					FROM		Fusion
					WHERE		Fusion.FusionTypeID = FT.ID
					ORDER BY	name
					FOR XML PATH('nav'), TYPE
					) AS items	
		FROM		FusionType FT
		ORDER BY	name
		FOR XML PATH('nav'), TYPE
		) AS Items	
	FROM SiteNav n
WHERE n.Name = '#Fusion'
		
UNION ALL

SELECT	n.Name as MenuID, 
		n.SortOrder,
		4 as Feature,
		(
        SELECT	'People' AS name, --'#People' as MenuID,
                'a/community/groups' AS url, 		        
                0 as feature,
		        NULL AS Items
        FOR XML PATH('nav'), TYPE
        ) AS Items
FROM SiteNav n
WHERE n.Name = '#Community'
UNION ALL

SELECT	'#Admin' as MenuID,
		999 as SortOrder,
		0 as Feature,
		(
			select	*
			from	(
					SELECT	'Security' AS name, 
							'#/' AS url, 
							0 as feature,
							(
							select	*
							from	(
									SELECT	'Groups' AS name, 
											'#/groups/administration' AS url, 
											--'Menu_A_S_G' as menuID,
											0 as feature,
											NULL AS items
									union all
									SELECT	'Users' AS name, 
											'#/resources/administration' AS url, 
											--'Menu_A_S_R' as menuID,
											0 as feature,
											NULL AS items
									union all
									SELECT	'Responsibilities' AS name, 
											'#/governance/administration' AS url, 
											0 as feature,
											NULL AS items
                            ) bg
							FOR XML PATH('nav'), TYPE
							) AS items
						
					union all

					SELECT	'MetaModel' AS name, 
							'#/' AS url,
							0 as feature, 
							(
							select	*
							from	(
									SELECT	'Artifacts' AS name, 
											'#/artifacts/administration' AS url, 
											0 as feature,
											NULL AS items
									union all
									SELECT	'Attributes' AS name, 
											'#/attributes/administration' AS url, 
											0 as feature,
											NULL AS items
									union all
									SELECT	'Lookups' AS name, 
											'#/lookups/administration' AS url, 
											0 as feature,
											NULL AS items
									union all
									SELECT	'Models' AS name, 
											'#/catalogs/administration' AS url, 
											0 as feature,
											NULL AS items
                                    union all
									SELECT	'Policies' AS name, 
											'#/policies/administration' AS url, 
											1 as feature,
											NULL AS items
                                    union all
									SELECT	'Relationships' AS name, 
											'#/relations/administration' AS url, 
											0 as feature,
											NULL AS items
                                    union all
                                    SELECT	'Rules' AS name, 
											'#/rules/administration' AS url, 
											0 as feature,
											NULL AS items
									) bg
							FOR XML PATH('nav'), TYPE
							) AS items
						
					union all

					SELECT	'Metrics' AS name, 
							'#/' AS url,
							0 as feature, 
							(
							select	*
							from	(
									SELECT	'Analytics' AS name, 
											'#/analytics/administration' AS url, 
											5 as feature,
											NULL AS items
									union all
					                SELECT	'Dashboards' AS name, 
							                '#/reporting/administration' AS url, 
							                0 as feature,
							                NULL AS items
                                    union all
					                SELECT	'Surveys' AS name, 
							                '#/surveys/administration' AS url, 
							                7 as feature,
							                (
							                SELECT	'Response Types' AS name, 
									                '#/surveyresponsetypes/administration' AS url, 
									                7 as feature,
									                NULL AS items
							                FOR XML PATH('nav'), TYPE
							                ) AS items
									) bg
							FOR XML PATH('nav'), TYPE
							) AS items
						
					union all

					SELECT	'Reference' AS name, 
							'#/domains/administration' AS url, 
							0 as feature,
							NULL AS items

					union all

					SELECT	'Workflow' AS name, 
							'#/workflow/administration' AS url, 
							0 as feature,
							NULL AS items

                    union all

                    SELECT	'Templates' AS name, 
							'#/templates/administration' AS url, 
							0 as feature,
							NULL AS items

					union all

					SELECT	'Integration' AS name, 
							'#/' AS url, 
							0 as feature,
							(
							select	*
							from	(
									SELECT	'Bulk Loader' AS name, 
											'#/load' AS url, 
											0 as feature,
											NULL AS items
									union all
									SELECT	'Fusion' AS name, 
											'#/fusion/administration' AS url, 
											2 as feature,
											NULL AS items
									union all
									SELECT	'API' AS name, 
											'/swagger' AS url, 
											0 as feature,
											NULL AS items
									) bg
							FOR XML PATH('nav'), TYPE
							) AS items

                    union all

                    SELECT	'Settings' AS name, 
							'#/settings' AS url, 
							0 as feature,
							NULL AS items
            ) bg
			for xml path('nav'), type
		) as Items

	where 1 = 1

	UNION ALL

	SELECT 
		'~' + Name AS MenuID,
		s.SortOrder,
		0 AS Feature,
		dbo.CustomSiteNavigation(ID) AS Items
	from SiteNav s
	where ParentID IS NULL and Name not like '#%'

	order by sortorder
END
GO





-- columns also added to create scripts for sitenav table 
-- but for existing...
alter table sitenav add [Icon] [varchar](100) NULL
alter table sitenav add [Title] [nvarchar](250) NULL

update sitenav set Icon = 'fa-pie-chart', Title = 'Data Quality' where name = '#Data Quality'
update sitenav set Icon = 'fa-dashboard', Title = 'Monitor' where name = '#Monitor'
update sitenav set Icon = 'fa-university', Title = 'Policies' where name = '#Policy'
update sitenav set Icon = 'fa-cubes', Title = 'Reference' where name = '#Reference'
update sitenav set Icon = 'fa-sitemap', Title = 'Models' where name = '#Models'
update sitenav set Icon = 'fa-book', Title = 'Glossary' where name = '#Glossary'
update sitenav set Icon = 'fa-database', Title = 'Fusion' where name = '#Fusion'
update sitenav set Icon = 'fa-group', Title = 'Community' where name = '#Community'

-- delete unused home that has code written to hide it...
delete from sitenav where name = '#Home'
