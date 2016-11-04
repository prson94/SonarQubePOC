CREATE TABLE [dbo].[FieldTypeFilteredLookupDefinition](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[FieldTypeID] [int] NOT NULL,
	[Object] [varchar](50) NOT NULL,
	[ObjectID] [int] NOT NULL,
	[HideHeader] [bit] NOT NULL,
	[HideFooter] [bit] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)
)

GO

ALTER TABLE [dbo].[FieldTypeFilteredLookupDefinition] ADD  CONSTRAINT [DF_FieldTypeFilteredLookupDefinition_HideHeader]  DEFAULT ((1)) FOR [HideHeader]
GO

ALTER TABLE [dbo].[FieldTypeFilteredLookupDefinition] ADD  CONSTRAINT [DF_FieldTypeFilteredLookupDefinition_HideFooter]  DEFAULT ((1)) FOR [HideFooter]
GO

ALTER TABLE [dbo].[FieldTypeFilteredLookupDefinition]  WITH CHECK ADD  CONSTRAINT [FK_FieldTypeFilteredLookupDefinition_FieldType] FOREIGN KEY([FieldTypeID])
REFERENCES [dbo].[FieldType] ([ID])
ON DELETE CASCADE
GO

ALTER TABLE [dbo].[FieldTypeFilteredLookupDefinition] CHECK CONSTRAINT [FK_FieldTypeFilteredLookupDefinition_FieldType]
GO

CREATE TABLE [dbo].[FieldTypeFilteredLookupDisplayField](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[FieldTypeFilteredLookupDefinitionID] [int] NOT NULL,
	[FieldTypeID] [int] NOT NULL,
	[FieldTypeName] [nvarchar](250) NULL,
	[Show] [bit] NOT NULL,
	[SortOrder] [int] NULL,
	[Filter] [bit] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)
)

GO

ALTER TABLE [dbo].[FieldTypeFilteredLookupDisplayField] ADD  CONSTRAINT [DF_FieldTypeFilteredLookupDisplayField_Show]  DEFAULT ((1)) FOR [Show]
GO

ALTER TABLE [dbo].[FieldTypeFilteredLookupDisplayField]  WITH CHECK ADD  CONSTRAINT [FK_FieldTypeFilteredLookupDisplayField_FieldTypeFilteredLookupDefinition] FOREIGN KEY([FieldTypeFilteredLookupDefinitionID])
REFERENCES [dbo].[FieldTypeFilteredLookupDefinition] ([ID])
ON DELETE CASCADE
GO

ALTER TABLE [dbo].[FieldTypeFilteredLookupDisplayField] CHECK CONSTRAINT [FK_FieldTypeFilteredLookupDisplayField_FieldTypeFilteredLookupDefinition]
GO


alter table [dbo].[FieldTypeFusionLookupDisplayField] add [Show] [bit] NOT NULL constraint DF_FieldTypeFusionLookupDisplayField_Show default(1)
go
alter table [dbo].[FieldTypeFusionLookupDisplayField] add [SortOrder] [int] NULL
go
alter table [dbo].[FieldTypeFusionLookupDisplayField] add [FilterValue] [nvarchar](250) NULL
go

--ALTER TABLE [Rule] ADD [Definition] [nvarchar](max) NULL
ALTER TABLE [Rule] ADD [Status] [int] NOT NULL CONSTRAINT [DF_Rule_Status] DEFAULT (1)
ALTER TABLE [Rule] ADD [Threshold] [decimal](3, 3) NOT NULL CONSTRAINT [DF_Rule_Threshold] DEFAULT (0)
ALTER TABLE [Rule] ADD [Purpose] [nvarchar](max) NULL
ALTER TABLE [Rule] ADD [Measurement] [nvarchar](max) NULL
ALTER TABLE [Rule] ADD [Resolution] [nvarchar](max) NULL
ALTER TABLE [Rule] ADD [CreatedOn] [datetime] NULL
ALTER TABLE [Rule] ADD [CreatedBy] [int] NULL
ALTER TABLE [Rule] DROP COLUMN SourceID
GO

DROP TABLE [quality].[RuleResult]
GO
DROP TABLE [quality].[RuleMap]
GO
DROP TABLE [quality].[Rule]
GO
DROP TABLE [quality].[Dimension]
GO
DROP FUNCTION [quality].[CalculatePassedWrapper]
GO
DROP FUNCTION [quality].[CalculatePassed]
GO
drop table IntersectNode
go
drop table IntersectTypeNode
go
drop table cache.Relationship
go


CREATE FUNCTION [utility].[CalculatePassed]
(
	@PassFraction decimal(4,3),
	@RuleID int
)
RETURNS bit
AS
BEGIN
	DECLARE @Passed bit

	select	top 1
			@Passed = case 
						when @PassFraction >= Threshold then cast(1 as bit)
						else cast(0 as bit)
					end
	from	[Rule] 
	where	ID = @RuleID

	RETURN @Passed
END
GO

create FUNCTION [utility].[CalculatePassedWrapper]
(
	@PassFraction decimal(4,3),
	@RuleID int
)
RETURNS bit
AS
BEGIN
	RETURN [utility].CalculatePassed(@PassFraction, @RuleID)
END
GO



CREATE TABLE [dbo].[RuleResult](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[RuleID] [int] NULL,
	[EffectiveDate] [datetime] NOT NULL,
	[RowsPassed] [int] NOT NULL,
	[RowsFailed] [int] NOT NULL,
	[PassFraction]  AS (CONVERT([decimal](4,3),CONVERT([decimal](18,3),[RowsPassed],(0))/(CONVERT([decimal](18,3),[RowsPassed],(0))+CONVERT([decimal](18,3),[RowsFailed],(0))),(0))),
	[FailFraction]  AS (CONVERT([decimal](4,3),CONVERT([decimal](3,3),CONVERT([decimal](18,3),(1),(0))-CONVERT([decimal](18,3),[RowsPassed],(0))/(CONVERT([decimal](18,3),[RowsPassed],(0))+CONVERT([decimal](18,3),[RowsFailed],(0))),(0)),(0))),
	[Passed]  AS ([utility].[CalculatePassedWrapper](CONVERT([decimal](4,3),CONVERT([decimal](18,3),[RowsPassed],(0))/(CONVERT([decimal](18,3),[RowsPassed],(0))+CONVERT([decimal](18,3),[RowsFailed],(0))),(0)),[RuleID])),
	[CreatedOn] [datetime] NULL CONSTRAINT [DF_RuleResult_CreatedOn] DEFAULT (getutcdate()),
	[CreatedBy] [int] NULL CONSTRAINT [DF_RuleResult_CreatedBy] DEFAULT (0),
	[FusionAttributeID] [int] NULL,
	CONSTRAINT [PK_RuleResult] PRIMARY KEY NONCLUSTERED ( [ID] ASC )
)
GO

ALTER TABLE [dbo].[RuleResult]  WITH CHECK ADD  CONSTRAINT [FK_RuleResult_FusionAttribute] FOREIGN KEY([FusionAttributeID]) REFERENCES [dbo].[FusionAttribute] ([ID])
GO

ALTER TABLE [dbo].[RuleResult] CHECK CONSTRAINT [FK_RuleResult_FusionAttribute]
GO

ALTER TABLE [dbo].[RuleResult]  WITH CHECK ADD  CONSTRAINT [FK_RuleResult_Rule] FOREIGN KEY([RuleID]) REFERENCES [dbo].[Rule] ([ID])
GO

ALTER TABLE [dbo].[RuleResult] CHECK CONSTRAINT [FK_RuleResult_Rule]
GO

--select * from [Rule]
--select * from quality.[Rule]
--insert into RuleResult (RuleID, EffectiveDate,
--		RowsPassed,
--		RowsFailed,
--		CreatedOn,
--		CreatedBy,
--		FusionAttributeID)
--select	case QualityRuleID
--			when 2 then 50
--			when 3 then 51
--			when 5 then 52
--		end,
--		EffectiveDate,
--		RowsPassed,
--		RowsFailed,
--		CreatedOn,
--		CreatedBy,
--		FusionAttributeID
--from	quality.RuleResult


CREATE TABLE [dbo].[RuleMap](
	[RuleID] [int] NOT NULL,
	[SourceID] [varchar](50) NOT NULL,
	[SourceName] [varchar](250) NULL,
	[SourceURI] [varchar](1000) NULL,
	CONSTRAINT [PK_RuleMap] PRIMARY KEY CLUSTERED ( [RuleID] ASC, [SourceID] ASC )
)
GO

ALTER TABLE [dbo].[RuleMap]  WITH CHECK ADD  CONSTRAINT [FK_RuleMap_Rule] FOREIGN KEY([RuleID]) REFERENCES [dbo].[Rule] ([ID])
GO

ALTER TABLE [dbo].[RuleMap] CHECK CONSTRAINT [FK_RuleMap_Rule]
GO




ALTER TABLE [Intersect] ADD CONSTRAINT [DF_Intersect_CreatedOn] DEFAULT (getutcdate()) FOR [CreatedOn];
ALTER TABLE [Intersect] ADD CONSTRAINT [DF_Intersect_UpdatedOn] DEFAULT (getutcdate()) FOR [UpdatedOn];
ALTER TABLE [Intersect] ADD CONSTRAINT [DF_Intersect_CreatedBy] DEFAULT (0) FOR [CreatedBy];
ALTER TABLE [Intersect] ADD CONSTRAINT [DF_Intersect_UpdatedBy] DEFAULT (0) FOR [UpdatedBy];
ALTER TABLE [Intersect] ADD CONSTRAINT [DF_Intersect_Deleted] DEFAULT (0) FOR [Deleted];
GO

CREATE TRIGGER [dbo].[FusionQueryAttributeType_AfterUpsert]
   ON  [dbo].[FusionQueryAttributeType] 
   AFTER INSERT, UPDATE
AS 
	SET NOCOUNT ON;
	merge	[cache].[Object] as T
	using	(
			select	'FusionQueryAttributeType' as [Object],			ID as ObjectID,
					'Fusion' as ObjectType,					FusionID as ObjectTypeID
			from	inserted
			) as S
	on		T.[Object] = S.[Object] and T.[ObjectID] = S.[ObjectID]
	when	matched then
			update set	T.[ObjectType] = S.[ObjectType],
						T.[ObjectTypeID] = S.[ObjectTypeID]
	when	not matched then
			insert	( [Object],		[ObjectID],		[ObjectType],	[ObjectTypeID]		)
			values	( S.[Object],	S.[ObjectID],	S.[ObjectType], S.[ObjectTypeID]	);

GO

CREATE TRIGGER [dbo].[FusionQueryAttributeType_AfterDelete]
   ON  [dbo].[FusionQueryAttributeType] 
   AFTER DELETE
AS 
	SET NOCOUNT ON;
	delete	T
	from	[cache].[Object] T
			inner join deleted S on T.Object = 'FusionQueryAttributeType' and T.ObjectID = S.ID
GO


CREATE NONCLUSTERED INDEX [IX_Field_FieldTypeID] 
ON [dbo].[Field] ([FieldTypeID]) INCLUDE ([Value]) 
WITH (ONLINE = ON)
GO


CREATE NONCLUSTERED INDEX [IX_FusionRulePromotion_FusionAttribute_Rule_RuleStep_Object] 
ON [fusion].[RulePromotion] ([FusionAttributeID], [RuleID], [RuleStepID], [ObjectID], [ObjectType]) 
WITH (ONLINE = ON)
GO


update	[Rule]
set CreatedOn = coalesce(CreatedOn, getutcdate()),
	CreatedBy = coalesce(CreatedBy, 0),
	UpdatedOn = coalesce(UpdatedOn, getutcdate()),
	UpdatedBy = coalesce(UpdatedBy, 0)






--ALTER TRIGGER [dbo].[Intersect_AfterInsert]
--	ON [dbo].[Intersect]
--	FOR INSERT
--AS
--BEGIN
--	SET NOCOUNT ON;

--	--declare @tbl table(ID int identity, IntersectID int, ResourceID int, Subject varchar(50), SubjectID int, Object varchar(50), ObjectID int)
--	--insert into @tbl
--	--	select ID, UpdatedBy, Subject, SubjectID, Object, ObjectID from inserted;

--	--declare @current int = 1,
--	--		@max int,
--	--		@id int,
--	--		@r int,
--	--		@s varchar(50),
--	--		@sid int,
--	--		@o varchar(50),
--	--		@oid int,
--	--		@date datetime = getutcdate()

--	--select @max =max(ID) from @tbl

--	--while @current <= @max
--	--begin
--	--	select	@id = IntersectID,
--	--			@r = ResourceID,
--	--			@s = coalesce(Subject, 'Intersect'),
--	--			@sid = coalesce(SubjectID, IntersectID),
--	--			@o = coalesce(Object, 'Intersect'),
--	--			@oid = coalesce(ObjectID, IntersectID)
--	--	from	@tbl
--	--	where	ID = @current

--	--	exec [cache].[SynchronizeObjectDetails] 'Intersect', @id
--	--	exec [utility].[AddAuditEntry] @s, @sid, @r, @date, 'Created', 'Intersect', @id
--	--	exec [utility].[AddAuditEntry] @o, @oid, @r, @date, 'Created', 'Intersect', @id

--	--	exec cache.SynchronizeResponsibilitiesForObject @s, @sid
--	--	--exec cache.SynchronizeResponsibilitiesForObject @o, @oid

--	--	merge cache.Relationship as T
--	--	using (
--	--			select	distinct
--	--					S.IntersectID,
--	--					S.IntersectTypeNodeID as SourceIntersectTypeNodeID, 
--	--					S.ID as SourceIntersectNodeID,
--	--					S.ObjectType as SourceObject,
--	--					S.ObjectID as SourceObjectID,
--	--					T.IntersectTypeNodeID as TargetIntersectTypeNodeID,
--	--					T.ID as TargetIntersectNodeID,
--	--					T.ObjectType as TargetObject,
--	--					T.ObjectID as TargetObjectID
--	--			from	dbo.IntersectNode S
--	--					inner join dbo.IntersectNode T on T.IntersectID = S.IntersectID and T.ID <> S.ID
--	--			where	S.IntersectID = @id
--	--			) as S (
--	--				IntersectID, 
--	--				SourceIntersectTypeNodeID, SourceIntersectNodeID, SourceObject, SourceObjectID, 
--	--				TargetIntersectTypeNodeID, TargetIntersectNodeID, TargetObject, TargetObjectID
--	--				)
--	--	on    (T.IntersectID = S.IntersectID and T.SourceObject = S.SourceObject and T.SourceObjectID = S.SourceObjectID)
--	--	when not matched then
--	--		insert (
--	--				IntersectID, 
--	--				SourceIntersectTypeNodeID, SourceIntersectNodeID, SourceObject, SourceObjectID, 
--	--				TargetIntersectTypeNodeID, TargetIntersectNodeID, TargetObject, TargetObjectID
--	--				)
--	--		values (
--	--				S.IntersectID, 
--	--				S.SourceIntersectTypeNodeID, S.SourceIntersectNodeID, S.SourceObject, S.SourceObjectID, 
--	--				S.TargetIntersectTypeNodeID, S.TargetIntersectNodeID, S.TargetObject, S.TargetObjectID
--	--				);

--	--	set @current = @current +1
--	--end;
--END
--GO

--ALTER TRIGGER [dbo].[Intersect_AfterUpdate]
--	ON [dbo].[Intersect]
--	FOR UPDATE
--AS
--BEGIN
--	SET NOCOUNT ON;

--	--declare @tbl table(ID int identity, IntersectID int, ResourceID int, Subject varchar(50), SubjectID int, Object varchar(50), ObjectID int)
--	--insert into @tbl
--	--	select ID, UpdatedBy, Subject, SubjectID, Object, ObjectID from inserted;

--	--declare @current int = 1,
--	--		@max int,
--	--		@id int,
--	--		@r int,
--	--		@s varchar(50),
--	--		@sid int,
--	--		@o varchar(50),
--	--		@oid int,
--	--		@date datetime = getutcdate()

--	--select @max =max(ID) from @tbl

--	--while @current <= @max
--	--begin
--	--	select	@id = IntersectID,
--	--			@r = ResourceID,
--	--			@s = coalesce(Subject, 'Intersect'),
--	--			@sid = coalesce(SubjectID, IntersectID),
--	--			@o = coalesce(Object, 'Intersect'),
--	--			@oid = coalesce(ObjectID, IntersectID)
--	--	from	@tbl
--	--	where	ID = @current

--	--	exec [cache].[SynchronizeObjectDetails] 'Intersect', @id
--	--	exec [utility].[AddAuditEntry] @s, @sid, @r, @date, 'Updated', 'Intersect', @id
--	--	exec [utility].[AddAuditEntry] @o, @oid, @r, @date, 'Updated', 'Intersect', @id

--	--	merge cache.Relationship as T
--	--	using (
--	--			select	distinct
--	--					S.IntersectID,
--	--					S.IntersectTypeNodeID as SourceIntersectTypeNodeID, 
--	--					S.ID as SourceIntersectNodeID,
--	--					S.ObjectType as SourceObject,
--	--					S.ObjectID as SourceObjectID,
--	--					T.IntersectTypeNodeID as TargetIntersectTypeNodeID,
--	--					T.ID as TargetIntersectNodeID,
--	--					T.ObjectType as TargetObject,
--	--					T.ObjectID as TargetObjectID
--	--			from	dbo.IntersectNode S
--	--					inner join dbo.IntersectNode T on T.IntersectID = S.IntersectID and T.ID <> S.ID
--	--			where	S.IntersectID = @id
--	--			) as S (
--	--				IntersectID, 
--	--				SourceIntersectTypeNodeID, SourceIntersectNodeID, SourceObject, SourceObjectID, 
--	--				TargetIntersectTypeNodeID, TargetIntersectNodeID, TargetObject, TargetObjectID
--	--				)
--	--	on    (T.IntersectID = S.IntersectID and T.SourceObject = S.SourceObject and T.SourceObjectID = S.SourceObjectID)
--	--	when not matched then
--	--		insert (
--	--				IntersectID, 
--	--				SourceIntersectTypeNodeID, SourceIntersectNodeID, SourceObject, SourceObjectID, 
--	--				TargetIntersectTypeNodeID, TargetIntersectNodeID, TargetObject, TargetObjectID
--	--				)
--	--		values (
--	--				S.IntersectID, 
--	--				S.SourceIntersectTypeNodeID, S.SourceIntersectNodeID, S.SourceObject, S.SourceObjectID, 
--	--				S.TargetIntersectTypeNodeID, S.TargetIntersectNodeID, S.TargetObject, S.TargetObjectID
--	--				);

--	--	set @current = @current +1
--	--end;
--END
--GO

/*
Pull the following object updates:
	[dbo].[AddRelationships]
	[dbo].[DeleteObject]
	[dbo].[EventCountByObject]
	[dbo].[EventsByObject]
	[dbo].[GetRenderedTemplateBodyNg]
	[dbo].[Relationship]
	[cache].[ObjectDetails]
	[cache].[Relationships]
	[fusion].[Rules] 
	[utility].[GetHierarchyAssignedResponsibilityList]
	[utility].[ObjectDetail]
*/