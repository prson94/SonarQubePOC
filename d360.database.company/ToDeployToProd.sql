CREATE SCHEMA [analytics]
    AUTHORIZATION [dbo];
GO


DROP TABLE [dbo].[Event]
go
DROP TABLE [dbo].[EventGroup]
go
ALTER TABLE [dbo].[Map] DROP CONSTRAINT [FK_Map_IntersectRole]
GO
alter table Map drop column IntersectRoleID
go
DROP TABLE [dbo].[IntersectRole]
go
DROP TABLE [dbo].[RuleMap]
go
DROP TABLE [dbo].[Synonym]
go
drop procedure bulkload.UpdateIntersectRoleColumn
go
DROP PROCEDURE [dbo].[GetEventsByObject]
go
DROP FUNCTION [dbo].[EventsByObject]
go
DROP FUNCTION [dbo].[EventCountByObject]
go
DROP TYPE [dbo].[LineageEditorRows]
go

ALTER TABLE Artifact ADD [Visible] BIT CONSTRAINT [DF_Artifact_Visible] DEFAULT ((1)) NOT NULL;
go
CREATE NONCLUSTERED INDEX [IX_Artifact_Visible] ON [dbo].[Artifact]([Visible] ASC);
go
ALTER TABLE ArtifactType DROP COLUMN AllowRelatedArtifacts;
go

ALTER TABLE FusionQueryAttributeType ALTER COLUMN [Query] NVARCHAR (MAX) NOT NULL;
go
ALTER TABLE FusionQueryAttributeType ADD [DisplayFormat] NVARCHAR (250) CONSTRAINT [DF_FusionQueryAttributeType_DisplayFormat] DEFAULT ('{ID}') NOT NULL;
go

CREATE FUNCTION [utility].[GetFormattedFieldFusionQueryAttributeValue]
(
	@FusionQueryAttributeID int,
	@FusionQueryAttributeTypeID int	
)
RETURNS nvarchar(max)
AS
BEGIN
	declare @formattedValue nvarchar(max)
	declare @tokens table(ID int identity(1,1), Token nvarchar(100), Field nvarchar(100))
	declare @fieldValues table(Field nvarchar(100), Value nvarchar(max))
	declare @displayFormat nvarchar(250)

	set @displayFormat = (select displayformat from FusionQueryAttributeType where ID = @FusionQueryAttributeTypeID);

	set @formattedValue = @displayFormat

	while patindex('%{%',@formattedValue) > 0
	begin
		declare @txt nvarchar(100) = SUBSTRING(@formattedValue, patindex('%{%',@formattedValue), PATINDEX('%}%', @formattedValue))
		insert into @tokens Values (@txt, REPLACE(REPLACE(@txt,'{',''),'}',''))
		set @formattedValue = replace(@formattedValue, @txt, '')
	end

	insert into @fieldValues
		SELECT	Name,
				FormattedValue
		FROM	FieldWithRelation 
		WHERE	ObjectType = 'FusionQueryAttribute' 
				and ObjectID = @FusionQueryAttributeID

				
	insert into @fieldValues
		SELECT 'ID',
				ID
		FROM	FusionQueryAttribute
		WHERE	ID = @FusionQueryAttributeID

	declare @current int,
			@max int

	set @current = 1
	select @max = Max(ID) from @tokens
	set @formattedValue = @displayFormat

	while(@current <= @max)
	begin
		declare @currentToken nvarchar(100) = null,
				@currentField nvarchar(100) = null,
				@currentValue nvarchar(4000) = null,
				@lkpType nvarchar(250) = null, 
				@lkpID int = null, 
				@lkpFormat nvarchar(250) = null

		select	@currentField = Field, 
				@currentToken = Token 
		from	@tokens
		where	ID = @current

		select	@currentValue = Value
		from	@fieldValues 
		where	Field = @currentField

		if @currentValue is not null
		begin
			SET @formattedValue = REPLACE(@formattedValue, @currentToken, @currentValue)
		end
		else
		begin
			SET @formattedValue = REPLACE(@formattedValue, @currentToken, '')
		end

		SET @current = @current + 1
	end

	return @formattedValue
END
GO

CREATE FUNCTION [utility].[GetFormattedFieldFusionQueryAttributeValueWrapper]
(
	@FusionQueryAttributeID int,
	@FusionQueryAttributeTypeID int	
)
RETURNS nvarchar(max)
AS
BEGIN
	return utility.GetFormattedFieldFusionQueryAttributeValue(@FusionQueryAttributeID, @FusionQueryAttributeTypeID)
END
GO

CREATE FUNCTION [utility].[GetFormattedFieldLookupValueWrapper]
(
	@Type varchar(25),
	@DisplayFormat nvarchar(250),
	@LookupObjectType varchar(25),
	@LookupObjectID int,
	@Value nvarchar(max)	
)
RETURNS nvarchar(max)
AS
BEGIN
	RETURN utility.GetFormattedFieldLookupValue(@Type, @DisplayFormat, @LookupObjectType, @LookupObjectID, @Value)
END
GO

ALTER TABLE FusionQueryAttribute ADD [DisplayValue] AS ([utility].[GetFormattedFieldFusionQueryAttributeValueWrapper]([ID],[FusionQueryAttributeTypeID]));
go

ALTER TABLE FieldType ADD [IsDisplayable]         BIT             CONSTRAINT [DF_FieldType_IsDisplayable] DEFAULT ((1)) NOT NULL;
go
ALTER TABLE FieldType ADD [IsEditable]            BIT             CONSTRAINT [DF_FieldType_IsEditable] DEFAULT ((1)) NOT NULL;
go
ALTER TABLE FieldType ADD [DefaultValue]          NVARCHAR (MAX)  NULL;
go
ALTER TABLE FieldType ADD [DefaultFormattedValue] AS              ([utility].[GetFormattedFieldLookupValueWrapper]([Type],[LookupDisplayFormat],[LookupObjectType],[LookupObjectID],[DefaultValue]));
go
ALTER TABLE FieldType ADD [AllowAllValue]         BIT             CONSTRAINT [DF_FieldType_AllowAllValue] DEFAULT ((0)) NOT NULL;
go
ALTER TABLE FieldType ADD [AllowAllLabel]         NVARCHAR (250)  NULL;
go
ALTER TABLE FieldType ADD [IsPrimaryFilter]       BIT             CONSTRAINT [CK_FieldType_IsPrimaryFilter] DEFAULT ((0)) NOT NULL;
go



DROP TABLE FusionSchedule;
go

CREATE TABLE [dbo].[FusionSchedule] (
    [FusionID]    INT      NOT NULL,
    [Day]         INT      NOT NULL,
    [Time]        TIME (7) NOT NULL,
    [FullRefresh] BIT      CONSTRAINT [DF_FusionSchedule_FullRefresh] DEFAULT ((0)) NOT NULL,
    [CreatedOn]   DATETIME NULL,
    [CreatedBy]   INT      NULL,
    [UpdatedOn]   DATETIME NULL,
    [UpdatedBy]   INT      NULL,
    [ID]          INT      IDENTITY (1, 1) NOT NULL,
    CONSTRAINT [PK_FusionSchedule] PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [FK_FusionSchedule_Fusion] FOREIGN KEY ([FusionID]) REFERENCES [dbo].[Fusion] ([ID]) ON DELETE CASCADE,
    CONSTRAINT [Con_FusionScheduleUniqueFusionIDDayTime] UNIQUE NONCLUSTERED ([FusionID] ASC, [Day] ASC, [Time] ASC)
);
go

ALTER TABLE [dbo].[Intersect] DROP COLUMN Classification;
go
ALTER TABLE [dbo].[Intersect] DROP COLUMN Description;
go
ALTER TABLE [dbo].[Intersect] ADD [Visible] BIT CONSTRAINT [DF_Intersect_Visible] DEFAULT ((1)) NOT NULL;
go

CREATE NONCLUSTERED INDEX [IX_Intersect_Visible] ON [dbo].[Intersect]([Visible] ASC);
go

ALTER TABLE Issue ADD [CommentID] INT NULL;
go

CREATE TRIGGER [dbo].[Map_AfterDelete]
	ON [dbo].[Map]
	AFTER DELETE
AS
	SET NOCOUNT ON;
	delete	T
	from	[cache].[Object] T
			inner join deleted S on T.Object = 'Map' and S.ID = T.ObjectID;
GO

ALTER TABLE [dbo].[Map] add MapTypeID int CONSTRAINT [DF_Map_MapTypeID]  DEFAULT ((1)) not null
GO
ALTER TABLE [dbo].[Map] add Name nvarchar(2500) null
GO

CREATE TRIGGER [dbo].[Map_AfterUpsert]
   ON  [dbo].[Map] 
   AFTER INSERT, UPDATE
AS 
	SET NOCOUNT ON;
	declare @ot varchar(50) = 'Map'

	merge	[cache].[Object] as T
	using	(
			select	@ot as [Object],
					ID as ObjectID,
					'MapType' as ObjectType,
					MapTypeID as ObjectTypeID
			from	inserted
			) as S
	on		T.[Object] = S.[Object] and T.[ObjectID] = S.[ObjectID]
	when	matched then
			update set	T.[ObjectType] = S.[ObjectType],
						T.[ObjectTypeID] = S.[ObjectTypeID]
	when	not matched then
			insert	( [Object], [ObjectID], [ObjectType], [ObjectTypeID] )
			values	( S.[Object], S.[ObjectID], S.[ObjectType], S.[ObjectTypeID] );
GO

CREATE NONCLUSTERED INDEX [IX_MapRuleItem_SourceFusionAttributeID_TargetFusionAttributeID]
    ON [dbo].[MapRuleItem]([SourceFusionAttributeID] ASC, [TargetFusionAttributeID] ASC);
GO

ALTER TABLE Nym add [Visible] BIT CONSTRAINT DF_Nym_Visible DEFAULT ((1)) NOT NULL;
go

CREATE NONCLUSTERED INDEX [IX_Nym_Visible]
    ON [dbo].[Nym]([Visible] ASC);
GO

DROP TRIGGER [dbo].[ObjectStyle_AfterUpsert];
go

ALTER TABLE [Policy] ADD [Visible] BIT CONSTRAINT DF_Policy_Visible DEFAULT ((1)) NOT NULL;
go

CREATE NONCLUSTERED INDEX [IX_Policy_Visible]
    ON [dbo].[Policy]([Visible] ASC);
GO

ALTER TABLE [ReferenceItem] ADD [Visible] BIT CONSTRAINT DF_ReferenceItem_Visible DEFAULT ((1)) NOT NULL;
go

CREATE NONCLUSTERED INDEX [IX_ReferenceItem_Visible] ON [dbo].[ReferenceItem]([Visible] ASC);
go

CREATE TRIGGER [dbo].[ReferenceItem_AfterDelete]
	ON [dbo].[ReferenceItem]
	AFTER DELETE
AS
	SET NOCOUNT ON;
	delete	T
	from	[cache].[Object] T
			inner join deleted S on T.Object = 'ReferenceItem' and S.ID = T.ObjectID;
GO

CREATE TRIGGER [dbo].[ReferenceItem_AfterUpsert]
   ON  [dbo].[ReferenceItem] 
   AFTER INSERT, UPDATE
AS 
	SET NOCOUNT ON;
	merge	[cache].[Object] as T
	using	(
			select	'ReferenceItem' as [Object],
					ID as ObjectID,
					'ReferenceItemType' as ObjectType,
					ReferenceItemTypeID as ObjectTypeID
			from	inserted
			) as S
	on		T.[Object] = S.[Object] and T.[ObjectID] = S.[ObjectID]
	when	matched then
			update set	T.[ObjectType] = S.[ObjectType],
						T.[ObjectTypeID] = S.[ObjectTypeID]
	when	not matched then
			insert	( [Object], [ObjectID], [ObjectType], [ObjectTypeID] )
			values	( S.[Object], S.[ObjectID], S.[ObjectType], S.[ObjectTypeID] );
GO

ALTER TABLE [dbo].[Rule] DROP CONSTRAINT [FK_Rule_RuleType]
GO

EXEC sp_rename 'dbo.Rule.RuleType', 'RuleTypeID', 'COLUMN';
go

ALTER TABLE [Rule] ADD [Visible] BIT CONSTRAINT DF_Rule_Visible DEFAULT ((1)) NOT NULL;
go

CREATE NONCLUSTERED INDEX [IX_Rule_Visible] ON [dbo].[Rule]([Visible] ASC);
go

CREATE TABLE [dbo].[RuleType] (
    [ID]          INT            IDENTITY (1, 1) NOT NULL,
    [Name]        NVARCHAR (250) NOT NULL,
    [Description] NVARCHAR (MAX) NULL,
    [CreatedOn]   DATETIME       NULL,
    [CreatedBy]   INT            NULL,
    [UpdatedOn]   DATETIME       NULL,
    [UpdatedBy]   INT            NULL,
    CONSTRAINT [PK_RuleType] PRIMARY KEY CLUSTERED ([ID] ASC)
);
GO

CREATE TRIGGER [dbo].[RuleType_AfterDelete]
   ON  [dbo].[RuleType] 
   AFTER DELETE
AS 
	SET NOCOUNT ON;
	INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
		select 'Delete', [queue].WriteIndexXml('Removed', 'RuleType', ID, coalesce(UpdatedBy, 0)), 'RuleType', ID from deleted
GO

CREATE TRIGGER [dbo].[RuleType_AfterInsert]
   ON  [dbo].[RuleType] 
   AFTER INSERT
AS 
	SET NOCOUNT ON;
	INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
		select 'Add', [queue].WriteIndexXml('', 'RuleType', ID, coalesce(UpdatedBy, 0)), 'RuleType', ID from inserted

	merge	[cache].[Object] as T
	using	(
			select	'RuleType' as [Object],			ID as ObjectID,
					'RuleType' as ObjectType,			0 as ObjectTypeID
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

CREATE TRIGGER [dbo].[RuleType_AfterUpdate]
   ON  [dbo].[RuleType] 
   AFTER UPDATE
AS 
	SET NOCOUNT ON;
	INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
		select 'Update', [queue].WriteIndexXml('', 'RuleType', ID, coalesce(UpdatedBy, 0)), 'RuleType', ID from inserted

	merge	[cache].[Object] as T
	using	(
			select	'RuleType' as [Object],			ID as ObjectID,
					'RuleType' as ObjectType,			0 as ObjectTypeID
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


insert into RuleType values ('Informational', 'An informational rule such as a rule defining a data event.  This rule delivers events that are purely informational, and there is no need to perform any other steps.', getutcdate(), 0, getutcdate(), 0)
insert into RuleType values ('Quality Check', 'A quality check rule.', getutcdate(), 0, getutcdate(), 0)
insert into RuleType values ('Metric', 'A metric rule.  These rules can be included as part of scoring for a related item.', getutcdate(), 0, getutcdate(), 0)
insert into RuleType values ('Profile', 'A profile rule.', getutcdate(), 0, getutcdate(), 0)
go

ALTER TABLE [dbo].[Rule]  WITH CHECK ADD  CONSTRAINT [FK_Rule_RuleType] FOREIGN KEY([RuleTypeID]) REFERENCES [dbo].[RuleType] ([ID])
go
ALTER TABLE [dbo].[Rule] CHECK CONSTRAINT [FK_Rule_RuleType]
GO


ALTER TRIGGER [dbo].[Rule_AfterInsert]
   ON  [dbo].[Rule] 
   AFTER INSERT
AS 
	SET NOCOUNT ON;
	INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
		select 'Add', [queue].WriteIndexXml('', 'Rule', ID, coalesce(UpdatedBy, 0)), 'Rule', ID from inserted

	update	T
	set		T.CreatedOn = coalesce(S.CreatedOn, getutcdate()),
			T.UpdatedOn = coalesce(S.UpdatedOn, getutcdate())
	from	[Rule] T
			inner join inserted S on S.ID = T.ID;

	merge	[cache].[Object] as T
	using	(
			select	'Rule' as [Object],			ID as ObjectID,
					'RuleType' as ObjectType,	RuleTypeID as ObjectTypeID
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

ALTER TRIGGER [dbo].[Rule_AfterUpdate]
   ON  [dbo].[Rule] 
   AFTER UPDATE
AS 
	SET NOCOUNT ON;
	INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
		select 'Update', [queue].WriteIndexXml('', 'Rule', ID, coalesce(UpdatedBy, 0)), 'Rule', ID from inserted

	merge	[cache].[Object] as T
	using	(
			select	'Rule' as [Object],			ID as ObjectID,
					'RuleType' as ObjectType,	RuleTypeID as ObjectTypeID
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

alter table StatisticType add [ScoreTypeID] INT CONSTRAINT [DF_StatisticType_ScoreTypeID] DEFAULT ((1)) NOT NULL;
go

ALTER TABLE [Taxonomy] ADD [Visible] BIT CONSTRAINT DF_Taxonomy_Visible DEFAULT ((1)) NOT NULL;
go

CREATE NONCLUSTERED INDEX [IX_Taxonomy_Visible] ON [dbo].[Taxonomy]([Visible] ASC);
go

CREATE NONCLUSTERED INDEX [IX_ReportingDates_Date]
    ON [reporting].[Dates]([Date] ASC);
GO

DROP TABLE [dbo].[RuleResultQualifier]
Go

drop TABLE [dbo].[RuleResultQualifierType]
go

drop TABLE [dbo].[RuleResult]
go

CREATE TABLE [dbo].[RuleImplementation](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[RuleID] [int] NOT NULL,
	[SourceID] [varchar](250) NULL,
	[SourceUri] [varchar](2500) NULL,
	[Name] [nvarchar](250) NULL,
	[CreatedOn] [datetime] NULL,
	[CreatedBy] [int] NULL,
	[UpdatedOn] [datetime] NULL,
	[UpdatedBy] [int] NULL,
	CONSTRAINT [PK_RuleImplementation] PRIMARY KEY CLUSTERED ( [ID] ASC )
)
GO

ALTER TABLE [dbo].[RuleImplementation]  WITH CHECK ADD  CONSTRAINT [FK_RuleImplementation_Rule] FOREIGN KEY([RuleID]) REFERENCES [dbo].[Rule] ([ID])
GO

ALTER TABLE [dbo].[RuleImplementation] CHECK CONSTRAINT [FK_RuleImplementation_Rule]
GO

CREATE TABLE [dbo].[RuleResult] (
    [ID]                   INT      IDENTITY (1, 1) NOT NULL,
    [EffectiveDate]        DATETIME NOT NULL,
    [RowsPassed]           INT      NOT NULL,
    [RowsFailed]           INT      NOT NULL,
    [CreatedOn]            DATETIME CONSTRAINT [DF_RuleResult_CreatedOn] DEFAULT (getutcdate()) NULL,
    [CreatedBy]            INT      CONSTRAINT [DF_RuleResult_CreatedBy] DEFAULT ((0)) NULL,
    [RunDate]              DATETIME CONSTRAINT [DF_RuleResult_RunDate] DEFAULT (getutcdate()) NOT NULL,
    [RuleImplementationID] INT      CONSTRAINT [DF_RuleResult_RuleImplementationID] DEFAULT ((0)) NOT NULL,
    [PassFraction]         AS       (case when [RowsPassed]=(0) AND [RowsFailed]=(0) then (0) else CONVERT([decimal](4,3),CONVERT([decimal](18,3),[RowsPassed],(0))/(CONVERT([decimal](18,3),[RowsPassed],(0))+CONVERT([decimal](18,3),[RowsFailed],(0))),(0)) end),
    [FailFraction]         AS       (case when [RowsPassed]=(0) AND [RowsFailed]=(0) then (0) when [RowsPassed]=(0) AND [RowsFailed]<>(0) then (1) else CONVERT([decimal](4,3),CONVERT([decimal](3,3),CONVERT([decimal](18,3),(1),(0))-CONVERT([decimal](18,3),[RowsPassed],(0))/(CONVERT([decimal](18,3),[RowsPassed],(0))+CONVERT([decimal](18,3),[RowsFailed],(0))),(0)),(0)) end),
    [Passed]               AS       ([utility].[CalculatePassedWrapper](case when [RowsPassed]=(0) AND [RowsFailed]=(0) then (0) else CONVERT([decimal](4,3),CONVERT([decimal](18,3),[RowsPassed],(0))/(CONVERT([decimal](18,3),[RowsPassed],(0))+CONVERT([decimal](18,3),[RowsFailed],(0))),(0)) end,[RuleImplementationID])),
    CONSTRAINT [PK_RuleResult] PRIMARY KEY NONCLUSTERED ([ID] ASC),
    CONSTRAINT [FK_RuleResult_RuleImplementation] FOREIGN KEY ([RuleImplementationID]) REFERENCES [dbo].[RuleImplementation] ([ID])
);
GO

CREATE TABLE [dbo].[RuleResultQualifierType] (
    [ID]                      INT            IDENTITY (1, 1) NOT NULL,
    [Name]                    NVARCHAR (250) NOT NULL,
    [Order]                   INT            NOT NULL,
    [ResolutionObject]        VARCHAR (50)   NULL,
    [ResolutionObjectID]      INT            NULL,
    [ResolutionFieldTypeID]   INT            NULL,
    [ResolutionFieldTypeName] NVARCHAR (250) NULL,
    [RuleImplementationID]    INT            CONSTRAINT [DF_RuleResultQualifierType_RuleImplementationID] DEFAULT ((0)) NOT NULL,
    CONSTRAINT [PK_RuleResultQualifierType] PRIMARY KEY NONCLUSTERED ([ID] ASC),
    CONSTRAINT [FK_RuleResultQualifierType_RuleImplementation] FOREIGN KEY ([RuleImplementationID]) REFERENCES [dbo].[RuleImplementation] ([ID])
);
GO

CREATE TABLE [dbo].[RuleResultQualifier] (
    [RuleResultID]              INT             NOT NULL,
    [RuleResultQualifierTypeID] INT             NOT NULL,
    [Value]                     NVARCHAR (1000) NULL,
    [ResolvedObject]            VARCHAR (50)    NULL,
    [ResolvedObjectID]          INT             NULL,
    [EventNotificationSent]     BIT             CONSTRAINT [DF_RuleResultQualifier_EventNotificationSent] DEFAULT ((0)) NOT NULL,
    CONSTRAINT [PK_RuleResultQualifier] PRIMARY KEY NONCLUSTERED ([RuleResultID] ASC, [RuleResultQualifierTypeID] ASC),
    CONSTRAINT [FK_RuleResultQualifier_RuleResult] FOREIGN KEY ([RuleResultID]) REFERENCES [dbo].[RuleResult] ([ID]),
    CONSTRAINT [FK_RuleResultQualifier_RuleResultQualifierType] FOREIGN KEY ([RuleResultQualifierTypeID]) REFERENCES [dbo].[RuleResultQualifierType] ([ID])
);
GO

alter table workflow.EventRegistration add [Settings] XML NULL;
alter table workflow.EventRegistration add [LastExecuted] DATETIME     NULL;
alter table workflow.EventRegistration add [State] INT CONSTRAINT [DF_WorkflowEventRegistration_State] DEFAULT ((-1)) NOT NULL;
go

alter table workflow.Item add [NumberOfEvents] INT      CONSTRAINT DF_WorkflowItem_NumberofEvents    DEFAULT ((0)) NOT NULL;
go

CREATE NONCLUSTERED INDEX [IX_WorkflowItem_VersionObjectObjectID]
    ON [workflow].[Item]([VersionID] ASC, [Object] ASC, [ObjectID] ASC);
GO

ALTER table workflow.ItemStep add [ResourceObject]   VARCHAR (50) NULL;
ALTER table workflow.ItemStep add [ResourceObjectID] INT          NULL;
go

alter table workflow.[Type] add [PublishedVersionID] INT            NULL;
alter table workflow.[Type] add [Deleted]            BIT            CONSTRAINT [DF_WorkflowType_Deleted] DEFAULT ((0)) NOT NULL;
alter table workflow.[Type] add [State]              INT            CONSTRAINT [DF_WorkflowType_State] DEFAULT ((-1)) NOT NULL;
go

ALTER TABLE [workflow].[Type]  WITH CHECK ADD  CONSTRAINT [FK_WorkflowType_WorkflowVersion] FOREIGN KEY([PublishedVersionID]) REFERENCES [workflow].[Version] ([ID])
GO

ALTER TABLE [workflow].[Type] CHECK CONSTRAINT [FK_WorkflowType_WorkflowVersion]
GO

alter table workflow.[Version] add [Version]   INT CONSTRAINT DF_WorkflowVersion_Version DEFAULT ((1)) NOT NULL;
go

alter table workflow.VersionStep add [State] INT CONSTRAINT [DF_WorkflowVersionStep_State] DEFAULT ((-1)) NOT NULL;
go

alter TABLE [workflow].[VersionStepTransition] add [FromPortID]        VARCHAR (10)   NULL;
alter TABLE [workflow].[VersionStepTransition] add [ToPortID]          VARCHAR (10)   NULL;
alter TABLE [workflow].[VersionStepTransition] add [ID]                BIGINT         IDENTITY (1, 1) NOT NULL;
alter TABLE [workflow].[VersionStepTransition] add [Settings]          XML            NULL;
alter TABLE [workflow].[VersionStepTransition] add [State]             INT            CONSTRAINT [DF_WorkflowVersionStepTransition_State] DEFAULT ((-1)) NOT NULL;
alter TABLE [workflow].[VersionStepTransition] drop column LinkType;
go

ALTER TABLE [workflow].[VersionStepTransition] DROP CONSTRAINT [PK_WorkflowVersionStepTransition]
ALTER TABLE [workflow].[VersionStepTransition] ADD  CONSTRAINT [PK_WorkflowVersionStepTransition] PRIMARY KEY CLUSTERED ( [ID] ASC )
go

alter view [cache].[ObjectDetails]
as
	select	D.[Object],
			D.[ObjectID],
			coalesce(O1.Name, O2.Name, O5.Name, O6.Name, O7.Name, O8.Name, O9.Name, O10.Name, O11.Name, O12.Name, O13.Name, case when O14.ResourceID is not null then O14.FirstName + ' ' + O14.LastName else null end, O15.Name, O16.Name, O17.Name, O18.Name, O21.Name, O22.Name, O23.Name, O24.Name, O25.DisplayValue, O26.Name, O27.Name, O28.Name, O29.Name, null) as Name, --O4.Name, 
			coalesce(O1.TextPath, O2.TextPath, O5.Name, O6.Name, O7.Name, O8.Name, O9.Name, O10.Name, O11.Name, O12.Name, O13.TextPath, case when O14.ResourceID is not null then O14.FirstName + ' ' + O14.LastName else null end, O15.Name, O16.Name, O17.TextPath, O18.Name, O21.Name, O22.Name, O23.Name, O24.Name, O25.DisplayValue, O26.Name, O27.Name, O28.Name, O29.Name, '') as TextPath, --O4.TextPath, 
			coalesce(O1.Description, O2.Description, O6.Description, O7.Description, O8.Description, O9.Description, O10.Description, O12.Description, O13.Description, O26.Description, NULL) as Description,
			case D.[Object]
				when 'Lookup' then dbo.GenerateNgObjectUrl('Lookup', O20.LookupTypeID, O20.ID)
				when 'LookupType' then dbo.GenerateNgObjectUrl('LookupType', O21.ID, 0)
				when 'ReferenceItem' then dbo.GenerateNgObjectUrl('ReferenceItem', O25.ReferenceItemTypeID, O25.ID)
				when 'ReferenceItemType' then dbo.GenerateNgObjectUrl('ReferenceItemType', O26.ID, 0)
				else dbo.GenerateNgObjectUrl(D.[Object], D.[ObjectTypeID], D.ObjectID) 
			end as Url,
			case 
				when P1.ID is not null then 'Artifact'
				when P2.ID is not null then 'Taxonomy'
				--when P4.ID is not null then 'FusionAttribute'
				when P7.ID is not null then 'ArtifactType'
				when P10.ID is not null then 'AttributeType'
				when P13.ID is not null then 'PolicyType'
				when P17.ID is not null then 'FusionAttributeType'
				else NULL
			end as Parent,
			coalesce(O1.ParentID, O2.ParentID, O7.ParentID, O10.ParentID, O13.ParentID, O17.ParentID, NULL) as ParentID, --O4.ParentID, 
			coalesce(P1.Name, P2.Name, P7.Name, P10.Name, P13.Name, P17.Name, NULL) as ParentName,	--P4.Name, 
			D.[ObjectType],
			D.ObjectTypeID,
			coalesce(OT1.Name, OT2.Name, OT5.Name, OT12.Name, OT13.Name, OT14.Name, OT15.Name, OT20.Name, OT24.Name, NULL) as ObjectTypeName, --OT4.TextPath, 
			coalesce(S.IconBackColor, '#000') as IconBackColor,
			coalesce(S.IconForeColor, '#fff') as IconForeColor,
			coalesce(S.IconText, 'leaf') as IconText,
			case D.[Object]
				when 'Lookup' then dbo.GenerateNgObjectUrl('Lookup', O20.LookupTypeID, O20.ID)
				when 'LookupType' then dbo.GenerateNgObjectUrl('LookupType', O21.ID, 0)
				when 'ReferenceItem' then dbo.GenerateNgObjectUrl('ReferenceItem', O25.ReferenceItemTypeID, O25.ID)
				when 'ReferenceItemType' then dbo.GenerateNgObjectUrl('ReferenceItemType', O26.ID, 0)
				else dbo.GenerateNgObjectUrl(D.[Object], D.[ObjectTypeID], D.ObjectID) 
			end as NgUrl
	from	cache.[Object] D with(nolock)
			left join Artifact O1 with(nolock) on D.[Object] = 'Artifact' and O1.ID = D.ObjectID
			left join ArtifactType OT1 with(nolock) on D.[Object] = 'Artifact' and OT1.ID = O1.ArtifactTypeID
			left join Artifact P1 with(nolock) on D.[Object] = 'Artifact' and P1.ID = O1.ParentID

			left join Taxonomy O2 with(nolock) on D.[Object] = 'Taxonomy' and O2.ID = D.ObjectID
			left join TaxonomyType OT2 with(nolock) on D.[Object] = 'Taxonomy' and OT2.ID = O2.TaxonomyTypeID
			left join Taxonomy P2 with(nolock) on D.[Object] = 'Taxonomy' and P2.ID = O2.ParentID

			--left join FusionAttribute O4 with(nolock) on D.[Object] = 'FusionAttribute' and O4.ID = D.ObjectID
			--left join FusionAttributeType OT4 with(nolock) on D.[Object] = 'FusionAttribute' and OT4.ID = O4.FusionAttributeTypeID
			--left join FusionAttribute P4 with(nolock) on D.[Object] = 'FusionAttribute' and P4.ID = O4.ParentID

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
			left join RuleType OT12 with(nolock) on D.[Object] = 'Rule' and OT12.ID = O12.RuleTypeID

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

			left join RuleType O18 on D.[Object] = 'RuleType' and O18.ID = D.ObjectID

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

			left join ReferenceItem O25 with(nolock) on D.[Object] = 'ReferenceItem' and O25.ID = D.ObjectID
			left join ReferenceItemType OT25 with(nolock) on D.[Object] = 'ReferenceItem' and OT25.ID = O25.ReferenceItemTypeID

			left join ReferenceItemType O26 with(nolock) on D.[Object] = 'ReferenceItemType' and O26.ID = D.ObjectID

			left join FusionQueryAttributeType O27 with(nolock) on D.[Object] = 'FusionQueryAttributeType' and O27.ID = D.ObjectID

			left join IssueType O28 with(nolock) on D.[Object] = 'IssueType' and O28.ID = D.ObjectID

			left join	(
						select 0 as ID, 'Reference List' as Name						
			) O29 on D.[Object] = 'ReferenceItemType' and O29.ID = D.ObjectID
			
			left join ObjectStyle S with(nolock) on S.ObjectType = D.ObjectType and S.ObjectID = D.[ObjectTypeID]
GO

CREATE TABLE [dbo].[MapType] (
    [ID]          INT            IDENTITY (1, 1) NOT NULL,
    [MapClass]    INT            NOT NULL,
    [Name]        NVARCHAR (250) NOT NULL,
    [Description] NVARCHAR (MAX) NULL,
    [CreatedOn]   DATETIME       CONSTRAINT [DF_MapType_CreatedOn] DEFAULT (getutcdate()) NULL,
    [CreatedBy]   INT            NULL,
    [UpdatedOn]   DATETIME       CONSTRAINT [DF_MapType_UpdatedOn] DEFAULT (getutcdate()) NULL,
    [UpdatedBy]   INT            NULL,
    CONSTRAINT [PK_MapType] PRIMARY KEY CLUSTERED ([ID] ASC)
);
GO

CREATE TRIGGER [dbo].[MapType_AfterDelete]
	ON [dbo].[MapType]
	AFTER DELETE
AS
	SET NOCOUNT ON;
	delete	T
	from	[cache].[Object] T
			inner join deleted S on T.Object = 'MapType' and S.ID = T.ObjectID;
GO

CREATE TRIGGER [dbo].[MapType_AfterUpsert]
   ON  [dbo].[MapType] 
   AFTER INSERT, UPDATE
AS 
	SET NOCOUNT ON;
	declare @ot varchar(50) = 'MapType'

	merge	[cache].[Object] as T
	using	(
			select	@ot as [Object],
					ID as ObjectID,
					@ot as ObjectType,
					ID as ObjectTypeID
			from	inserted
			) as S
	on		T.[Object] = S.[Object] and T.[ObjectID] = S.[ObjectID]
	when	matched then
			update set	T.[ObjectType] = S.[ObjectType],
						T.[ObjectTypeID] = S.[ObjectTypeID]
	when	not matched then
			insert	( [Object], [ObjectID], [ObjectType], [ObjectTypeID] )
			values	( S.[Object], S.[ObjectID], S.[ObjectType], S.[ObjectTypeID] );
GO

alter view [dbo].[IntersectDetail]
as
	select	I.ID,
			I.IntersectTypeID,

			I.Subject,
			I.SubjectID,
			case I.Subject
				when 'Intersect' then utility.DeriveIntersectName(SI.ID)
				when 'Resource' then SRE.FirstName + ' ' + SRE.LastName
				else coalesce(SA.TextPath, SD.Name, SF.TextPath, SQF.DisplayValue, SG.Name, SM.Name, SP.TextPath, SR.Name, SRI.DisplayValue, ST.TextPath) 
			end as SubjectName,
			case I.Subject
				when 'Intersect' then utility.DeriveIntersectName(SI.ID)
				when 'Resource' then SRE.FirstName + ' ' + SRE.LastName
				else coalesce(SA.Name, SD.Name, SF.Name, SQF.DisplayValue, SG.Name, SM.Name, SP.Name, SR.Name, SRI.DisplayValue, ST.Name) 
			end as SubjectShortName,
			dbo.GenerateNgObjectUrl(
				I.Subject, 
				case I.Subject
					when 'Resource' then 1
					when 'Group' then 1
					when 'ReferenceItemType' then 0
					else coalesce(SA.ArtifactTypeID, SF.FusionAttributeTypeID, SQF.FusionQueryAttributeTypeID, SI.IntersectTypeID, SM.MapTypeID, SP.PolicyTypeID, SR.RuleTypeID, SRI.ReferenceItemTypeID, ST.TaxonomyTypeID) 
				end,
				I.SubjectID) as SubjectUrl,
			case I.Subject
				when 'Group' then 'GroupType'
				when 'Resource' then 'ResourceType'
				else I.Subject + 'Type'
			end as SubjectType,
			case I.Subject
				when 'Resource' then 1
				when 'Group' then 1
				when 'ReferenceItemType' then 0
				else coalesce(SA.ArtifactTypeID, SF.FusionAttributeTypeID, SQF.FusionQueryAttributeTypeID, SI.IntersectTypeID, SM.MapTypeID, SP.PolicyTypeID, SR.RuleTypeID, SRI.ReferenceItemTypeID, ST.TaxonomyTypeID) 
			end as SubjectTypeID,
			case 
				when I.Subject = 'ReferenceItemType' then 'Reference List'
				when I.Subject = 'Intersect' then utility.DeriveIntersectTypeName(SI.IntersectTypeID)
				else coalesce(SAT.Name, SFT.TextPath, SMT.Name, SPT.Name, SRT.Name, SRIT.Name, STT.Name) 
			end as SubjectTypeName,
			coalesce(SIcon.IconBackColor, '#000') as SubjectIconBackColor,
			coalesce(SIcon.IconForeColor, '#fff') as SubjectIconForeColor,
			coalesce(SIcon.IconText, substring(coalesce(SAT.Name, SD.Name, SFT.TextPath, SMT.Name, SPT.Name, SRT.Name, SRIT.Name, STT.Name, ''), 1, 2)) as SubjectIconText,

			I.Object,
			I.ObjectID,
			case I.Object
				when 'Intersect' then utility.DeriveIntersectName(OI.ID)
				when 'Resource' then ORE.FirstName + ' ' + ORE.LastName
				else coalesce(OA.TextPath, OD.Name, [OF].TextPath, OQF.DisplayValue, OG.Name, OM.Name, OP.TextPath, [OR].Name, ORI.DisplayValue, OT.TextPath)
			end as ObjectName,
			case I.Object
				when 'Intersect' then utility.DeriveIntersectName(OI.ID)
				when 'Resource' then ORE.FirstName + ' ' + ORE.LastName
				else coalesce(OA.Name, OD.Name, [OF].Name, OQF.DisplayValue, OG.Name, OM.Name, OP.Name, [OR].Name, ORI.DisplayValue, OT.Name)
			end as ObjectShortName,
			dbo.GenerateNgObjectUrl(
				I.Object, 
				case I.Object
					when 'Resource' then 1
					when 'Group' then 1
					when 'ReferenceItemType' then 0
					else coalesce(OA.ArtifactTypeID, OD.ID, [OF].FusionAttributeTypeID, [OQF].FusionQueryAttributeTypeID, OI.IntersectTypeID, OM.MapTypeID, OP.PolicyTypeID, [OR].RuleTypeID, ORI.ReferenceItemTypeID, OT.TaxonomyTypeID)
				end,
				I.ObjectID) as ObjectUrl,
			case I.Object
				when 'Artifact' then 'ArtifactType'
				when 'FusionAttribute' then 'FusionAttributeType'
				when 'FusionQueryAttribute' then 'FusionQueryAttributeType'
				when 'Intersect' then 'IntersectType'
				when 'Map' then 'MapType'
				when 'Policy' then 'PolicyType'
				when 'Rule' then 'RuleType'
				when 'Taxonomy' then 'TaxonomyType'
				else I.Object
			end as ObjectType,
			case I.Object
				when 'Resource' then 1
				when 'Group' then 1
				when 'ReferenceItemType' then 0
				else coalesce(OA.ArtifactTypeID, OD.ID, [OF].FusionAttributeTypeID, OQF.FusionQueryAttributeTypeID, OI.IntersectTypeID, OM.MapTypeID, OP.PolicyTypeID, [OR].RuleTypeID, ORI.ReferenceItemTypeID, OT.TaxonomyTypeID)
			end as ObjectTypeID,
			case
				when I.Object = 'ReferenceItemType' then 'Reference List'
				when I.Object = 'Intersect' then utility.DeriveIntersectTypeName(OI.IntersectTypeID)
				else coalesce(OAT.Name, OD.Name, OFT.TextPath, OMT.Name, OPT.Name, ORT.Name, ORIT.Name, OTT.Name) 
			end as ObjectTypeName,
			coalesce(OIcon.IconBackColor, '#000') as ObjectIconBackColor,
			coalesce(OIcon.IconForeColor, '#fff') as ObjectIconForeColor,
			--coalesce(OIcon.IconText, 'leaf') as ObjectIconText,
			coalesce(OIcon.IconText, substring(coalesce(OAT.Name, OD.Name, OFT.TextPath, OMT.Name, OPT.Name, ORT.Name, ORIT.Name, OTT.Name, ''), 1, 2)) as ObjectIconText,

			IT.PredicateID,
			P.Name as [PredicateName],
			P.Type as PredicateType
	from	dbo.[Intersect] I with(nolock)
			inner join dbo.[IntersectType] IT with(nolock) on IT.ID = I.IntersectTypeID and I.[Visible] = 1
			left join [Predicate] P with(nolock) on P.ID = IT.PredicateID 
			left join dbo.Artifact SA with(nolock) on I.Subject = 'Artifact' and SA.ID = I.SubjectID
			left join dbo.ArtifactType SAT with(nolock) on SAT.ID = SA.ArtifactTypeID
			left join dbo.ReferenceItemType SD with(nolock) on I.Subject = 'ReferenceItemType' and SD.ID = I.SubjectID
			left join dbo.FusionAttribute SF with(nolock) on I.Subject = 'FusionAttribute' and SF.ID = I.SubjectID
			left join dbo.FusionQueryAttribute [SQF] with(nolock) on I.Object = 'FusionQueryAttribute' and [SQF].ID = I.SubjectID
			left join dbo.FusionAttributeType SFT with(nolock) on SFT.ID = SF.FusionAttributeTypeID
			left join dbo.[Group] SG with(nolock) on I.Subject = 'Group' and SG.ID = I.SubjectID
			left join dbo.[Intersect] SI with(nolock) on I.Subject = 'Intersect' and SI.ID = I.SubjectID
			--left join dbo.[IntersectType] SIT with(nolock) on SIT.ID = SI.IntersectTypeID
			left join dbo.Map SM with(nolock) on I.Subject = 'Map' and SM.ID = I.SubjectID
			left join dbo.MapType SMT with(nolock) on SMT.ID = SM.MapTypeID
			left join dbo.[Policy] SP with(nolock) on I.Subject = 'Policy' and SP.ID = I.SubjectID
			left join dbo.PolicyType SPT with(nolock) on SPT.ID = SP.PolicyTypeID
			left join reporting.Global_Resource SRE with(nolock) on I.Subject = 'Resource' and SRE.ResourceID = I.SubjectID
			left join ReferenceItem SRI with(nolock) on I.Subject = 'ReferenceItem' and SRI.ID = I.SubjectID
			left join ReferenceItemType SRIT with(nolock) on SRIT.ID = SRI.ReferenceItemTypeID
			left join dbo.[Rule] SR with(nolock) on I.Subject = 'Rule' and SR.ID = I.SubjectID
			left join dbo.RuleType SRT with(nolock) on SRT.ID = [SR].RuleTypeID
			left join dbo.Taxonomy ST with(nolock) on I.Subject = 'Taxonomy' and ST.ID = I.SubjectID
			left join dbo.TaxonomyType STT with(nolock) on STT.ID = ST.TaxonomyTypeID

			left join dbo.Artifact OA with(nolock) on I.Object = 'Artifact' and OA.ID = I.ObjectID
			left join dbo.ArtifactType OAT with(nolock) on OAT.ID = OA.ArtifactTypeID
			left join dbo.ReferenceItemType OD with(nolock) on I.Object = 'ReferenceItemType' and OD.ID = I.ObjectID
			left join dbo.FusionAttribute [OF] with(nolock) on I.Object = 'FusionAttribute' and [OF].ID = I.ObjectID
			left join dbo.FusionQueryAttribute [OQF] with(nolock) on I.Object = 'FusionQueryAttribute' and [OQF].ID = I.ObjectID
			left join dbo.FusionAttributeType OFT with(nolock) on OFT.ID = [OF].FusionAttributeTypeID
			left join dbo.[Group] OG with(nolock) on I.Object = 'Group' and OG.ID = I.SubjectID
			left join dbo.[Intersect] OI with(nolock) on I.Subject = 'Intersect' and OI.ID = I.SubjectID
			--left join dbo.[IntersectType] OIT with(nolock) on OIT.ID = OI.IntersectTypeID
			left join dbo.Map OM with(nolock) on I.Object = 'Map' and OM.ID = I.ObjectID
			left join dbo.MapType OMT with(nolock) on OMT.ID = OM.MapTypeID
			left join dbo.[Policy] OP with(nolock) on I.Object = 'Policy' and OP.ID = I.ObjectID
			left join dbo.PolicyType OPT with(nolock) on OPT.ID = OP.PolicyTypeID
			left join reporting.Global_Resource ORE with(nolock) on I.Object = 'Resource' and ORE.ResourceID = I.ObjectID
			left join ReferenceItem ORI with(nolock) on I.Object = 'ReferenceItem' and ORI.ID = I.ObjectID
			left join ReferenceItemType ORIT with(nolock) on ORIT.ID = ORI.ReferenceItemTypeID
			left join dbo.[Rule] [OR] with(nolock) on I.Object = 'Rule' and [OR].ID = I.ObjectID
			left join dbo.RuleType ORT with(nolock) on ORT.ID = [OR].RuleTypeID
			left join dbo.Taxonomy OT with(nolock) on I.Object = 'Taxonomy' and OT.ID = I.ObjectID
			left join dbo.TaxonomyType OTT with(nolock) on OTT.ID = OT.TaxonomyTypeID

			left join ObjectStyle SIcon with(nolock) on SIcon.ObjectType =	case I.Subject
																				when 'Group' then 'GroupType'
																				when 'Resource' then 'ResourceType'
																				else I.Subject + 'Type'
																			end 
														and SIcon.ObjectID =	case I.Subject
																					when 'Resource' then 1
																					when 'Group' then 1
																					else coalesce(SA.ArtifactTypeID, SD.ID, SF.FusionAttributeTypeID, SQF.FusionQueryAttributeTypeID, SI.IntersectTypeID, SM.MapTypeID, SP.PolicyTypeID, SR.RuleTypeID, ST.TaxonomyTypeID) 
																				end
			left join ObjectStyle OIcon with(nolock) on OIcon.ObjectType =	case I.Object
																				when 'Group' then 'GroupType'
																				when 'Resource' then 'ResourceType'
																				else I.Object + 'Type'
																			end 
														and OIcon.ObjectID =	case I.Object
																					when 'Resource' then 1
																					when 'Group' then 1
																					else coalesce(OA.ArtifactTypeID, OD.ID, [OF].FusionAttributeTypeID, OQF.FusionQueryAttributeTypeID, OI.IntersectTypeID, OM.MapTypeID, OP.PolicyTypeID, [OR].RuleTypeID, OT.TaxonomyTypeID) 
																				end

	where	coalesce(SA.ID, SD.ID, SF.ID, SQF.ID, SG.ID, SI.ID, SM.ID, SP.ID, SR.ID, SRI.ID, SRE.ResourceID, ST.ID) is not null
			and coalesce(OA.ID, OD.ID, [OF].ID, OQF.ID, OG.ID, OI.ID, OM.ID, OP.ID, [OR].ID, ORI.ID, ORE.ResourceID, OT.ID) is not null
GO

alter view [cache].[Relationships]
as
	SELECT	IntersectTypeID,
			ID as IntersectID,
			0 as SourceIntersectTypeNodeID,
			0 as SourceIntersectNodeID,
			Subject as SourceObject,
			SubjectID as SourceObjectID,
			SubjectName as SourceObjectName,
			SubjectType as SourceType,
			SubjectTypeID as SourceTypeID,
			SubjectTypeName as SourceTypeName,
			0 as TargetIntersectTypeNodeID,
			0 as TargetIntersectNodeID,
			Object as TargetObject,
			ObjectID as TargetObjectID,
			ObjectName as TargetObjectName,
			ObjectType as TargetType,
			ObjectTypeID as TargetTypeID,
			ObjectTypeName as TargetTypeName,
			'' as [Role]
	FROM	[IntersectDetail]
union
	SELECT	IntersectTypeID,
			ID as IntersectID,
			0 as SourceIntersectTypeNodeID,
			0 as SourceIntersectNodeID,
			Object as SourceObject,
			ObjectID as SourceObjectID,
			ObjectName as SourceObjectName,
			ObjectType as SourceType,
			ObjectTypeID as SourceTypeID,
			ObjectTypeName as SourceTypeName,
			0 as TargetIntersectTypeNodeID,
			0 as TargetIntersectNodeID,
			Subject as TargetObject,
			SubjectID as TargetObjectID,
			SubjectName as TargetObjectName,
			SubjectType as TargetType,
			SubjectTypeID as TargetTypeID,
			SubjectTypeName as TargetTypeName,
			'' as [Role]
	FROM	[IntersectDetail]
GO

alter VIEW [dbo].[FieldTypeWithRelation]
AS
	SELECT	T.ID,
			T.Name,
			T.FriendlyName,
			T.Category,
			T.Description,
			T.DisplayDescription,
			T.FormDescription,
			T.ValidationDescription,
			T.Type,
			T.LookupObjectType,
			T.LookupObjectID ,
			T.LookupDisplayFormat,
			T.Length,
			T.MinimumLength,
			T.MaximumLength,
			T.Pattern,
			T.[Object],
			T.ObjectID,
			D.Name as ObjectName,
			T.IsDisplayable,
			T.IsEditable,
			T.IsListable,
			T.IsRequired,
			T.SortOrder,
			T.DefaultValue
	FROM	FieldType T
			inner join cache.ObjectDetails D on D.[Object] = T.[Object] and D.ObjectID = T.ObjectID
GO

alter VIEW [dbo].[FieldWithRelation]
AS
	SELECT	F.FieldTypeID,
			T.Name,
			T.FriendlyName,
			T.Category,
			T.Description,
			T.DisplayDescription,
			T.FormDescription,
			T.ValidationDescription,
			T.Type,
			T.LookupObjectType,
			T.LookupObjectID,
			T.LookupDisplayFormat,
			T.MinimumLength,
			T.MaximumLength,
			T.Length,
			T.Pattern,
			T.IsDisplayable,
			T.IsEditable,
			T.IsListable,
			T.IsRequired,
			T.SortOrder,
			F.ObjectType,
			F.ObjectID,
			coalesce(F.Value, T.DefaultValue) as Value,
			case
				when T.AllowAllValue = 1 and F.FormattedValue = '0' then T.AllowAllLabel
				when F.FormattedValue is not null then F.FormattedValue
				when T.DefaultFormattedValue is not null then T.DefaultFormattedValue
				else null
			end as FormattedValue,
			case T.LookupObjectType 
				when 'ReferenceItem' then [dbo].GenerateObjectUrl('ReferenceItemType', T.LookupObjectID, T.LookupObjectID)
				when 'Resource' then [dbo].GenerateObjectUrl('ResourceType', 0, T.LookupObjectID)
				else null
			end as LookupUrl
	FROM	FieldType T
			left join Field F on F.FieldTypeID = T.ID 
	WHERE	(F.Value is not null OR T.DefaultValue is not null)
GO

alter view [dbo].[IntersectTypeDetail]
as
	select	IT.ID,
			IT.Subject,
			IT.SubjectID,
			case IT.Subject
				when 'IntersectType' then utility.DeriveIntersectTypeName(SIT.ID)
				when 'GroupType' then 'Group'
				when 'ResourceType' then 'Resource'
				else coalesce(SAT.Name, SDT.Name, SFT.TextPath, SMT.Name, SPT.Name, SRT.Name, SRIT.Name, STT.Name, SFQT.Name) 
			end as SubjectName,
			coalesce(SIcon.IconBackColor, '#000') as SubjectIconBackColor,
			coalesce(SIcon.IconForeColor, '#fff') as SubjectIconForeColor,
			coalesce(SIcon.IconText, substring(coalesce(SAT.Name, SDT.Name, SFT.Name, SMT.Name, SPT.Name, SRT.Name, SRIT.Name, STT.Name, SFQT.Name, ''), 1, 2)) as SubjectIconText,
			
			IT.Object,
			IT.ObjectID,
			case IT.Object
				when 'IntersectType' then utility.DeriveIntersectTypeName(OIT.ID)
				when 'GroupType' then 'Group'
				when 'ResourceType' then 'Resource'
				else coalesce(OAT.Name, ODT.Name, OFT.TextPath, OMT.Name, OPT.Name, ORT.Name, ORIT.Name, OTT.Name, OFQT.Name) 
			end as ObjectName,
			coalesce(OIcon.IconBackColor, '#000') as ObjectIconBackColor,
			coalesce(OIcon.IconForeColor, '#fff') as ObjectIconForeColor,
			--coalesce(OIcon.IconText, 'leaf') as ObjectIconText,
			coalesce(OIcon.IconText, substring(coalesce(OAT.Name, ODT.Name, OFT.Name, OMT.Name, OPT.Name, ORT.Name, ORIT.Name, OTT.Name, OFQT.Name, ''), 1, 2)) as ObjectIconText,

			IT.PredicateID,
			P.Name as [PredicateName],
			P.Type as PredicateType,
			
			coalesce(IT.IsSystem, cast(0 as bit)) as IsSystem
	from	IntersectType IT with(nolock) 
			left join [Predicate] P with(nolock) on P.ID = IT.PredicateID 

			left join dbo.ArtifactType SAT with(nolock)		on IT.Subject = 'ArtifactType'			and SAT.ID = IT.SubjectID
			left join (
				select 1 as ID, 'Reference List' as Name
			) SDT											on IT.Subject = 'ReferenceItemType'		and IT.SubjectID = 0
			left join FusionAttributeType SFT with(nolock)	on IT.Subject = 'FusionAttributeType'	and SFT.ID = IT.SubjectID
			left join FusionQueryAttributeType SFQT with(nolock)	on IT.Object = 'FusionQueryAttributeType'	and SFQT.ID = IT.SubjectID
			left join IntersectType SIT with(nolock)		on IT.Subject = 'IntersectType'			and SIT.ID = IT.SubjectID
			left join MapType SMT with(nolock)				on IT.Subject = 'MapType'				and SMT.ID = IT.SubjectID
			left join PolicyType SPT with(nolock)			on IT.Subject = 'PolicyType'			and SPT.ID = IT.SubjectID
			left join ReferenceItemType SRIT with(nolock)	on IT.Subject = 'ReferenceItemType'		and SRIT.ID = IT.SubjectID
			left join RuleType SRT with(nolock)				on IT.Subject = 'RuleType'				and SRT.ID = IT.SubjectID
			left join dbo.TaxonomyType STT with(nolock)		on IT.Subject = 'TaxonomyType'			and STT.ID = IT.SubjectID
			left join (
				select 1 as ID, 'Resource' as Name				
			) SRET												on IT.[Subject] = 'ResourceType'

			left join dbo.ArtifactType OAT with(nolock)			on IT.Object = 'ArtifactType'			and OAT.ID = IT.ObjectID			
			left join dbo.FusionAttributeType OFT with(nolock)	on IT.Object = 'FusionAttributeType'	and OFT.ID = IT.ObjectID
			left join dbo.FusionQueryAttributeType OFQT with(nolock)	on IT.Object = 'FusionQueryAttributeType'	and OFQT.ID = IT.ObjectID
			left join dbo.IntersectType OIT with(nolock)		on IT.Object = 'IntersectType'			and OIT.ID = IT.ObjectID
			left join dbo.MapType OMT with(nolock)				on IT.Object = 'MapType'				and OMT.ID = IT.ObjectID
			left join dbo.PolicyType OPT with(nolock)			on IT.Object = 'PolicyType'				and OPT.ID = IT.ObjectID
			left join ReferenceItemType ORIT with(nolock)		on IT.Object = 'ReferenceItemType'		and ORIT.ID = IT.ObjectID
			left join RuleType ORT with(nolock)					on IT.Object = 'RuleType'				and ORT.ID = IT.ObjectID
			left join dbo.TaxonomyType OTT with(nolock)			on IT.Object = 'TaxonomyType'			and OTT.ID = IT.ObjectID
			left join (
				select 1 as ID, 'Resource' as Name				
			) ORET												on IT.[Object] = 'ResourceType'
			left join (
				select 0 as ID, 'Reference List' as Name
			) ODT 	on IT.Object = 'ReferenceItemType'		and IT.ObjectID = 0

			left join ObjectStyle SIcon with(nolock) on SIcon.ObjectType = IT.Subject and SIcon.ObjectID =	IT.SubjectID
			left join ObjectStyle OIcon with(nolock) on OIcon.ObjectType = IT.Object and OIcon.ObjectID = IT.ObjectID
	where	coalesce(SAT.ID, SDT.ID, SIT.ID, SFT.ID, SMT.ID, SPT.ID, SRT.ID, STT.ID, SRET.ID, SRIT.ID, SFQT.ID) is not null
			and coalesce(OAT.ID, ODT.ID, [OFT].ID, OMT.ID, OPT.ID, ORT.ID, OTT.ID, ORET.ID, ORIT.ID, OFQT.ID) is not null
GO


alter view [dbo].[Relationship]
as
	select	R.IntersectTypeID,
			R.ID as IntersectID,
			2 as Classification,
			null as Description,
			'' as [Role],
			0 as SourceIntersectTypeNodeID,
			R.Subject as SourceObjectType,
			R.SubjectID as SourceObjectID,
			R.SubjectName as SourceName, 
			'' as SourceParent,
			0 as SourceParentID,
			'' as SourceParentName,
			R.SubjectTypeID as SourceTypeID,
			R.SubjectType as SourceType,
			R.SubjectTypeName as SourceTypeName,
			R.[SubjectUrl] as SourceUrl,
			0 as TargetIntersectTypeNodeID,
			R.Object as TargetObjectType,
			R.ObjectID as TargetObjectID,
			R.ObjectName as TargetName,
			'' as TargetParent,
			0 as TargetParentID,
			'' as TargetParentName,
			R.ObjectTypeID as TargetTypeID,
			R.ObjectType as TargetType,
			R.ObjectTypeName as TargetTypeName,
			R.[ObjectUrl] as TargetUrl,
			TR.[Exists] as HasTechnicalRelationships
	from	IntersectDetail R
			cross apply (
						select	case 
									when count(1) > 0 then cast(1 as bit) 
									else cast(0 as bit) 
								end as [Exists]
						from	[Intersect]
						where	Subject = 'Intersect' and SubjectID = R.ID
						) TR
GO


alter VIEW [dbo].[WorkflowIssue]
AS
(
(select		W.ID as WorkflowID
			,-1 as WorkflowItemID
		    ,W.Data.value('(fields/CommentID)[1]', 'int') as CommentID
			,W.Data.value('(fields/ResourceID)[1]', 'int') as CreatingResourceID
			,W.DateStarted
			,W.DateCompleted	
			,W.Step
			,A.ObjectID
			,A.Name
			,A.[Object]
			,A.Url
			,R.FirstName + ' ' + R.LastName as RaisedBy			
			,case when w.dateCompleted is null then cast(0 as bit) else cast(1 as bit) end as IsCompleted	
			,ws.data.value('(fields/Comment)[1]','nvarchar(500)') as Notes			
			,C.Body as Comments		
			,case when W.Data.value('(fields/IssueType)[1]', 'int') is null then 0 else W.Data.value('(fields/IssueType)[1]', 'int') end as IssueType
			,case when W.Data.value('(fields/IssueType)[1]', 'int') = 0 then 'Business Data Incorrect' else 'Governance Information Incorrect' end as IssueTypeName
			,0 as IssueID
			,2 as Criticality
			,'Medium' as CriticalityName
			,case when W.DateCompleted is null then datediff(day,W.DateStarted,GetUtcDate()) else datediff(day, W.DateStarted, W.DateCompleted) end as EllapsedDays
from	    Workflow W		
			inner join Comment C on C.ID = W.Data.value('(fields/CommentID)[1]', 'int')
			left outer join CommentRelation CR on CR.CommentID = C.ID and CR.ObjectType not in ('Resource', 'Group')
			left outer join workflowstatus ws on w.id = ws.workflowid and ws.recordnumber = 7
			left outer join cache.ObjectDetails A on A.[Object] = CR.ObjectType and A.ObjectID = CR.ObjectID            		
			left outer join reporting.Global_Resource R on R.ResourceID = W.Data.value('(fields/ResourceID)[1]', 'int')			
            where  W.WorkflowType = 3 and W.Data.value('(fields/IssueID)[1]', 'int') is null)
)
union
(select		W.ID as WorkflowID
			,-1 as WorkflowItemID
		    ,W.Data.value('(fields/CommentID)[1]', 'int') as CommentID
			,W.Data.value('(fields/ResourceID)[1]', 'int') as CreatingResourceID
			,W.DateStarted
			,W.DateCompleted	
			,W.Step
			,A.ObjectID
			,A.Name
			,A.[Object]
			,A.Url
			,R.FirstName + ' ' + R.LastName as RaisedBy			
			,case when w.dateCompleted is null then cast(0 as bit) else cast(1 as bit) end as IsCompleted	
			,ws.data.value('(fields/Comment)[1]','nvarchar(500)') as Notes	
			,C.Body as Comments				
			,IT.ID as IssueType
            ,IT.Name as IssueTypeName
			,I.ID as IssueID
			,I.Criticality as Criticality
			,case when I.Criticality = 0 then 'Negligible' when I.Criticality = 1 then 'Low' when I.Criticality = 2 then 'Medium' when I.Criticality = 3 then 'High'  when I.Criticality = 4 then 'Critical' else 'N/A' end as CriticalityName
			,case when W.DateCompleted is null then datediff(day,W.DateStarted,GetUtcDate()) else datediff(day, W.DateStarted, W.DateCompleted) end as EllapsedDays
from	    Workflow W		
			inner join Comment C on C.ID = W.Data.value('(fields/CommentID)[1]', 'int')
			inner join Issue I on (I.ID = W.Data.value('(fields/IssueID)[1]', 'int'))
			inner join IssueType IT on (I.IssueTypeID = IT.ID)			
			left outer join CommentRelation CR on CR.CommentID = C.ID and CR.ObjectType not in ('Resource', 'Group')
			left outer join workflowstatus ws on w.id = ws.workflowid and ws.recordnumber = 7
			left outer join cache.ObjectDetails A on A.[Object] = CR.ObjectType and A.ObjectID = CR.ObjectID            		
			left outer join reporting.Global_Resource R on R.ResourceID = W.Data.value('(fields/ResourceID)[1]', 'int')			
            where  W.WorkflowType = 3
)
union
(select		null as WorkflowID
			,wi.ID as WorkflowItemID
		    ,I.CommentID as CommentID
			,I.CreatedBy as CreatingResourceID
			,wi.StartedOn
			,wi.CompletedOn
			,'' as Step
			,A.ObjectID
			,A.Name
			,A.[Object]
			,A.Url
			,R.FirstName + ' ' + R.LastName as RaisedBy			
			,case when wi.CompletedOn is null then cast(0 as bit) else cast(1 as bit) end as IsCompleted	
			,'' as Notes					
			,C.Body as Comments
			,IT.ID as IssueType
            ,IT.Name as IssueTypeName
			,I.ID as IssueID
			,I.Criticality as Criticality
			,case when I.Criticality = 0 then 'Negligible' when I.Criticality = 1 then 'Low' when I.Criticality = 2 then 'Medium' when I.Criticality = 3 then 'High'  when I.Criticality = 4 then 'Critical' else 'N/A' end as CriticalityName
			,case when wi.CompletedOn is null then datediff(day,wi.StartedOn,GetUtcDate()) else datediff(day, wi.StartedOn, wi.CompletedOn) end as EllapsedDays
from	    Issue I
			inner join [workflow].item wi on (wi.[object] = 'Issue' and wi.[objectid] = i.id)
			inner join IssueType IT on (I.IssueTypeID = IT.ID)						
			left outer join cache.ObjectDetails A on A.[Object] = I.[Object] and A.ObjectID = I.ObjectID            		
			left outer join reporting.Global_Resource R on R.ResourceID = I.CreatedBy
			left outer join Comment C on C.ID = I.CommentID
)
GO

alter procedure [bulkload].[BusinessLineage]
--declare
	@id int
--set @id = 237
as
begin
	set nocount on;

	declare @r int,
			@dt datetime = getutcdate(),
			@ActionColumn int = 1,
			@SourceIntersectTypeColumn int = 2,
			@SourceSubjectSubjectAreaColumn int = 3,
			@SourceSubjectColumn int = 4,
			@SourceObjectSubjectAreaColumn int = 5,
			@SourceObjectColumn int = 6,
			@SourceFusionConfigColumn int = 7,
			@SourceFusionAttributeColumn int = 8,
			@TargetIntersectTypeColumn int = 9,
			@TargetSubjectSubjectAreaColumn int = 10,
			@TargetSubjectColumn int = 11,
			@TargetObjectSubjectAreaColumn int = 12,
			@TargetObjectColumn int = 13,
			@TargetFusionConfigColumn int = 14,
			@TargetFusionAttributeColumn int = 15,
			@TransformationColumn int = 16

	select	@r = UpdatedBy from [Load] where ID = @id

	--Set the default Action to Add if blank or NULL.
	update	LoadItemColumn
	set		Value = 'Add'
	where	LoadID = @id and ColumnIndex = @ActionColumn and (Value is null or Value = '')

	exec bulkload.UpdateIntersectTypeColumn @id, @SourceIntersectTypeColumn																		-- source intersect type
	exec bulkload.UpdateIntersectTypeColumn @id, @TargetIntersectTypeColumn																		-- target intersect type

	exec bulkload.UpdateSubjectAreaColumn @id, @SourceSubjectSubjectAreaColumn																	-- source subject subject area
	exec bulkload.UpdateSubjectAreaColumn @id, @SourceObjectSubjectAreaColumn																	-- source object subject area
	exec bulkload.UpdateSubjectAreaColumn @id, @TargetSubjectSubjectAreaColumn																	-- target subject subject area
	exec bulkload.UpdateSubjectAreaColumn @id, @TargetObjectSubjectAreaColumn																	-- target object subject area

	exec bulkload.UpdateItemColumnByIntersectType @id, @SourceIntersectTypeColumn, 1, @SourceSubjectSubjectAreaColumn, @SourceSubjectColumn		-- source subject
	exec bulkload.UpdateItemColumnByIntersectType @id, @SourceIntersectTypeColumn, 0, @SourceObjectSubjectAreaColumn, @SourceObjectColumn		-- source object
	exec bulkload.UpdateItemColumnByIntersectType @id, @TargetIntersectTypeColumn, 1, @TargetSubjectSubjectAreaColumn, @TargetSubjectColumn		-- target subject
	exec bulkload.UpdateItemColumnByIntersectType @id, @TargetIntersectTypeColumn, 0, @TargetObjectSubjectAreaColumn, @TargetObjectColumn		-- target object

	exec bulkload.UpdateFusionConfigurationColumn @id, @SourceFusionConfigColumn																-- source fusion config
	exec bulkload.UpdateFusionConfigurationColumn @id, @TargetFusionConfigColumn																-- target fusion config

	exec bulkload.UpdateFusionAttributeColumn @id, @SourceFusionConfigColumn, @SourceFusionAttributeColumn										-- source fusion attribute
	exec bulkload.UpdateFusionAttributeColumn @id, @TargetFusionConfigColumn, @TargetFusionAttributeColumn										-- target fusion attribute

	drop table if exists #RemoveItems
	drop table if exists #AddItems
--select * from #RemoveItems
	BEGIN TRANSACTION [Tran1]

	BEGIN TRY
		-- HANDLE THE REMOVEs

		-- Load Temp table that we are going to work from
		select	SS.RowIndex,
		
				SIT.LookupObjectID as SourceIntersectTypeID,
				SS.LookupObject as SourceSubject,
				SS.LookupObjectID as SourceSubjectID,
				SO.LookupObject as SourceObject,
				SO.LookupObjectID as SourceObjectID,

				TIT.LookupObjectID as TargetIntersectTypeID,
				TS.LookupObject as TargetSubject,
				TS.LookupObjectID as TargetSubjectID,
				[TO].LookupObject as TargetObject,
				[TO].LookupObjectID as TargetObjectID,

				SI.ID as SourceIntersectID,
				TI.ID as TargetIntersectID,
				M.ID as MapItemID,

				MRI.ID as MapRuleItemID,

				cast(0 as bit) as Status,
				cast('' as nvarchar(500)) as StatusMessage,

				@r as ResourceID  --THE USER THAT ADDED THE LOAD
		into	#RemoveItems
		from	LoadItemColumn SS
				inner join LoadItemColumn SO	on SO.LoadID = SS.LoadID	and SO.RowIndex = SS.RowIndex 	and SS.ColumnIndex = @SourceSubjectColumn 	and SO.ColumnIndex = @SourceObjectColumn
				inner join LoadItemColumn SA	on SA.LoadID = SS.LoadID	and SA.RowIndex = SS.RowIndex 	and SA.ColumnIndex = @ActionColumn and SA.Value = 'Remove'
				inner join LoadItemColumn SIT	on SIT.LoadID = SS.LoadID	and SIT.RowIndex = SS.RowIndex 	and SIT.ColumnIndex = @SourceIntersectTypeColumn
				left join [Intersect] SI		on SIT.LookupObject = 'IntersectType' and SI.IntersectTypeID = SIT.LookupObjectID 
												and SI.Subject = SS.LookupObject and SI.SubjectID = SS.LookupObjectID 
												and SI.Object = SO.LookupObject and SI.ObjectID = SO.LookupObjectID

				inner join LoadItemColumn TS 	on TS.LoadID = SS.LoadID 	and TS.RowIndex = SS.RowIndex	and TS.ColumnIndex = @TargetSubjectColumn
				inner join LoadItemColumn [TO]	on [TO].LoadID = SS.LoadID	and [TO].RowIndex = SS.RowIndex	and [TO].ColumnIndex = @TargetObjectColumn
				inner join LoadItemColumn TIT	on TIT.LoadID = SS.LoadID	and TIT.RowIndex = SS.RowIndex 	and TIT.ColumnIndex = @TargetIntersectTypeColumn
				left join [Intersect] TI		on TIT.LookupObject = 'IntersectType' and TI.IntersectTypeID = TIT.LookupObjectID 
												and TI.Subject = TS.LookupObject and TI.SubjectID = TS.LookupObjectID 
												and TI.Object = [TO].LookupObject and TI.ObjectID = [TO].LookupObjectID

				left join MapItem M				on M.SourceIntersectID = SI.ID and M.TargetIntersectID = TI.ID

				left join LoadItemColumn SFA	on SFA.LoadID = SS.LoadID	and SFA.RowIndex = SS.RowIndex 	and SFA.ColumnIndex = @SourceFusionAttributeColumn
				left join LoadItemColumn TFA	on TFA.LoadID = SS.LoadID	and TFA.RowIndex = SS.RowIndex 	and TFA.ColumnIndex = @TargetFusionAttributeColumn
				left join MapRuleItem MRI		on	SFA.LookupObject = 'FusionAttribute' and MRI.SourceFusionAttributeID = SFA.LookupObjectID and
													TFA.LookupObject = 'FusionAttribute' and MRI.TargetFusionAttributeID = TFA.LookupObjectID

		where	SS.LoadID = @id


		-- Add indexes to temp table
		CREATE NONCLUSTERED INDEX [IX_TempRemoveItems_MapItem] ON #RemoveItems ( MapItemID ASC )
		CREATE NONCLUSTERED INDEX [IX_TempRemoveItems_MapRuleItem] ON #RemoveItems ( MapRuleItemID ASC )
		CREATE NONCLUSTERED INDEX [IX_TempRemoveItems_SourceIntersect] ON #RemoveItems ( SourceIntersectID ASC )
		CREATE NONCLUSTERED INDEX [IX_TempRemoveItems_TargetIntersect] ON #RemoveItems ( TargetIntersectID ASC )

		/*	BEGIN: REMOVE TECHNICAL MAPPINGS THAT ARE TIED TO FOUND MAP ITEMS */
		declare @mapRuleItems table(MapRuleItemID int, MapRuleID int)
		insert into @mapRuleItems
			select	T.MapRuleItemID,
					TJ.MapRuleID
			from	MapRuleItemMapItem T
					inner join #RemoveItems S on S.MapItemID = T.MapItemID
					left join MapRuleItemMapRule TJ on TJ.MapRuleItemID = T.MapRuleItemID

		delete	T
		from	MapRuleItemMapItem T
				inner join @mapRuleItems S on S.MapRuleItemID = T.MapRuleItemID

		delete	T
		from	MapRuleItemMapRule T
				inner join @mapRuleItems S on S.MapRuleItemID = T.MapRuleItemID

		delete	T
		from	MapRule T
				inner join @mapRuleItems S on S.MapRuleID = T.ID
				left join MapRuleItemMapRule NTJ on NTJ.MapRuleID = S.MapRuleID and NTJ.MapRuleItemID <> S.MapRuleItemID	--get all map rules that are used only once.
		where	NTJ.MapRuleID is null
		/*	END: REMOVE TECHNICAL MAPPINGS THAT ARE TIED TO FOUND MAP ITEMS */

		/*	BEGIN: REMOVE TECHNICAL MAPPING OPTIONALLY SPECIFIED IF NOT TIED ANYWHERE ELSE */
		declare @mapRuleItemIDs table(MapRuleItemID int)
		insert into @mapRuleItemIDs
			select	S.MapRuleItemID
			from	#RemoveItems S
					left join MapRuleItemMapItem J on J.MapRuleItemID = S.MapRuleItemID
			where	S.MapRuleItemID is not null;

		delete	T
		from	MapRuleItem T
				inner join @mapRuleItemIDs S on S.MapRuleItemID = T.ID;

		/*	END: REMOVE TECHNICAL MAPPING OPTIONALLY SPECIFIED IF NOT TIED ANYWHERE ELSE */

		/*	BEGIN: MAPPINGS FOUND MAP ITEMS */
		declare @mapItems table(MapItemID int, MapID int)
		insert into @mapItems
			select	S.MapItemID,
					J.MapID
			from	#RemoveItems S
					left join MapItemMap J on J.MapItemID = S.MapItemID;

		delete	T
		from	MapItemMap T
				inner join @mapItems S on S.MapItemID = T.MapItemID;

		delete	T
		from	MapSequence T
				inner join @mapItems S on S.MapItemID = T.MapItemID;

		delete	T
		from	MapItem T
				inner join @mapItems S on S.MapItemID = T.ID;

		delete	T
		from	MapRule T
				inner join @mapRuleItems S on S.MapRuleID = T.ID
				left join MapRuleItemMapRule NTJ on NTJ.MapRuleID = S.MapRuleID and NTJ.MapRuleItemID <> S.MapRuleItemID	--get all map rules that are used only once.
		where	NTJ.MapRuleID is null;
		/*	END: REMOVE FOUND MAP ITEMS */

		/*	BEGIN: REMOVE SOURCE AND TARGET INTERSECTS THAT ARE NOT REFERENCED ANYWHERE ELSE */
		delete	T
		from	[Intersect] T
				inner join #RemoveItems S on (S.SourceIntersectID = T.ID or S.TargetIntersectID = T.ID)
				left join IntersectGroup CG on CG.IntersectID = T.ID
				left join MapItem CSM on CSM.SourceIntersectID = T.ID
				left join MapItem CTM on CTM.TargetIntersectID = T.ID
				left join [Intersect] CI on (CI.Subject = 'Intersect' and CI.SubjectID = T.ID) or (CI.Object = 'Intersect' and CI.ObjectID = T.ID)
		where	CG.ID is null and
				CSM.ID is null and 
				CTM.ID is null and
				CI.ID is null;
		/*	BEGIN: REMOVE SOURCE INTERSECTS THAT ARE NOT REFERENCED ANYWHERE ELSE */

		-- update status & status message for Items table
		
		-- SUCCESS STATUS
		update	T
		set		T.Status = 1,
				T.StatusMessage = coalesce(T.StatusMessage,'') + 'Business map removed. '
		from	#RemoveItems T
				left join MapItem S on S.ID = T.MapItemID
		where	T.MapItemID is not null and S.ID is null;

		update	T
		set		T.StatusMessage = coalesce(T.StatusMessage,'') + 'Source relationship removed. '
		from	#RemoveItems T
				left join [Intersect] S on S.ID = T.SourceIntersectID
		where	T.SourceIntersectID is not null and S.ID is null;

		update	T
		set		T.StatusMessage = coalesce(T.StatusMessage,'') + 'Target relationship removed. '
		from	#RemoveItems T
				left join [Intersect] S on S.ID = T.TargetIntersectID
		where	T.TargetIntersectID is not null and S.ID is null;

		-- FAILED STATUS
		update	T
		set		T.Status = 0,
				T.StatusMessage = coalesce(T.StatusMessage,'') + 'Could not find source relationship. '
		from	#RemoveItems T
		where	SourceIntersectID is null;

		update	T
		set		T.Status = 0,
				T.StatusMessage = coalesce(T.StatusMessage,'') + 'Could not find target relationship. '
		from	#RemoveItems T
		where	TargetIntersectID is null;

		update	T
		set		T.Status = 0,
				T.StatusMessage = coalesce(T.StatusMessage,'') + 'Could not find business map. '
		from	#RemoveItems T
		where	MapItemID is null;


		-- Now update LoadItems on original Load with status and messages created above
		update	T
		set		T.Status = S.Status,
				T.StatusMessage = S.StatusMessage,
				T.Object = case S.Status
							when 1 then 'MapItem'
							else NULL
						   end,
				T.ObjectID = case S.Status
							when 1 then S.MapItemID
							else NULL
						   end
		from	LoadItem T
				inner join #RemoveItems S on T.LoadID = @id and S.RowIndex = T.RowIndex;



		-- NOW HANDLE THE ADDs ---------------------------------------------------------------------------

		-- Load Temp table that we are going to work from
		select	SS.RowIndex,
		
				SIT.LookupObjectID as SourceIntersectTypeID,
				SS.LookupObject as SourceSubject,
				SS.LookupObjectID as SourceSubjectID,
				SO.LookupObject as SourceObject,
				SO.LookupObjectID as SourceObjectID,

				TIT.LookupObjectID as TargetIntersectTypeID,
				TS.LookupObject as TargetSubject,
				TS.LookupObjectID as TargetSubjectID,
				[TO].LookupObject as TargetObject,
				[TO].LookupObjectID as TargetObjectID,

				SFA.LookupObjectID as SourceFusionAttributeID,
				SFA.Value as SourceFusionAttributeRaw,
				TFA.LookupObjectID as TargetFusionAttributeID,
				TFA.Value as TargetFusionAttributeRaw,

				SI.ID as SourceIntersectID,
				TI.ID as TargetIntersectID,
				M.ID as MapItemID,
				MRI.ID as MapRuleItemID,

				SIFT.ID as SourceFusionIntersectTypeID,
				TIFT.ID as TargetFusionIntersectTypeID,
				SIF.ID as SourceFusionIntersectID,
				TIF.ID as TargetFusionIntersectID,

				cast(null as bit) as Status,
				cast('' as nvarchar(500)) as StatusMessage,

				@r as ResourceID  --THE USER THAT ADDED THE LOAD
		into	#AddItems
		from	LoadItemColumn SS
				inner join LoadItemColumn SO	on SO.LoadID = SS.LoadID	and SO.RowIndex = SS.RowIndex 	and SS.ColumnIndex = @SourceSubjectColumn 	and SO.ColumnIndex = @SourceObjectColumn
				inner join LoadItemColumn SA	on SA.LoadID = SS.LoadID	and SA.RowIndex = SS.RowIndex 	and SA.ColumnIndex = @ActionColumn and SA.Value = 'Add'
				inner join LoadItemColumn SIT	on SIT.LoadID = SS.LoadID	and SIT.RowIndex = SS.RowIndex 	and SIT.ColumnIndex = @SourceIntersectTypeColumn
				left join [Intersect] SI		on SIT.LookupObject = 'IntersectType' and SI.IntersectTypeID = SIT.LookupObjectID 
												and SI.Subject = SS.LookupObject and SI.SubjectID = SS.LookupObjectID 
												and SI.Object = SO.LookupObject and SI.ObjectID = SO.LookupObjectID

				inner join LoadItemColumn TS 	on TS.LoadID = SS.LoadID 	and TS.RowIndex = SS.RowIndex	and TS.ColumnIndex = @TargetSubjectColumn
				inner join LoadItemColumn [TO]	on [TO].LoadID = SS.LoadID	and [TO].RowIndex = SS.RowIndex	and [TO].ColumnIndex = @TargetObjectColumn
				inner join LoadItemColumn TIT	on TIT.LoadID = SS.LoadID	and TIT.RowIndex = SS.RowIndex 	and TIT.ColumnIndex = @TargetIntersectTypeColumn
				left join [Intersect] TI		on TIT.LookupObject = 'IntersectType' and TI.IntersectTypeID = TIT.LookupObjectID 
												and TI.Subject = TS.LookupObject and TI.SubjectID = TS.LookupObjectID 
												and TI.Object = [TO].LookupObject and TI.ObjectID = [TO].LookupObjectID

				left join MapItem M				on M.SourceIntersectID = SI.ID and M.TargetIntersectID = TI.ID

				left join LoadItemColumn SFA	on SFA.LoadID = SS.LoadID	and SFA.RowIndex = SS.RowIndex 	and SFA.ColumnIndex = @SourceFusionAttributeColumn
				left join LoadItemColumn TFA	on TFA.LoadID = SS.LoadID	and TFA.RowIndex = SS.RowIndex 	and TFA.ColumnIndex = @TargetFusionAttributeColumn

				left join MapRuleItem MRI		on	SFA.LookupObject = 'FusionAttribute' and MRI.SourceFusionAttributeID = SFA.LookupObjectID and
													TFA.LookupObject = 'FusionAttribute' and MRI.TargetFusionAttributeID = TFA.LookupObjectID

				left join FusionAttribute SFAO	on SFA.LookupObject = 'FusionAttribute' and SFAO.ID = SFA.LookupObjectID 
				outer apply (
						SELECT  MIN(ID) as ID
						FROM    IntersectType
						WHERE   Subject = 'IntersectType' and SubjectID = SIT.LookupObjectID and Object = 'FusionAttributeType' and ObjectID = SFAO.FusionAttributeTypeID
				) SIFT
				left join [Intersect] SIF		on	SIF.IntersectTypeID = SIFT.ID 
													and SIF.Subject = 'Intersect' and SIF.SubjectID = SI.ID
													and SIF.Object = SFA.LookupObject and SIF.ObjectID = SFA.LookupObjectID

				left join FusionAttribute TFAO	on TFA.LookupObject = 'FusionAttribute' and TFAO.ID = TFA.LookupObjectID 
				outer apply (
						SELECT  MIN(ID) as ID
						FROM    IntersectType
						WHERE   Subject = 'IntersectType' and SubjectID = TIT.LookupObjectID and Object = 'FusionAttributeType' and ObjectID = TFAO.FusionAttributeTypeID
				) TIFT
				left join [Intersect] TIF		on	TIF.IntersectTypeID = TIFT.ID 
													and TIF.Subject = 'Intersect' and TIF.SubjectID = TI.ID
													and TIF.Object = TFA.LookupObject and TIF.ObjectID = TFA.LookupObjectID

		where	SS.LoadID = @id

		-- Add indexes to temp table
		CREATE NONCLUSTERED INDEX [IX_SourceBusinessIntersect] ON #AddItems ( SourceIntersectTypeID ASC, SourceSubject ASC, SourceSubjectID ASC, SourceObject ASC, SourceObjectID ASC )
/*
update LoadItemColumn set Value = 'Bloomberg LP/Back Office Data License' where LoadID =  270 and RowIndex = 2 and ColumnIndex = 4
select * from LoadItemColumn where LoadID = 270
select * from #AddItems
select * from LoadItem where LoadID = 270

select I.LoadID, I.RowIndex, case I.[Status] when 1 then 'Complete' when 0 then 'Failed' else 'Queued' end as [Status], I.StatusMessage
from LoadItem I
where I.LoadID = 270
order by I.RowIndex
*/
		-- ERROR OUT THE ROWS THAT DO NOT HAVE THE APPROPRIATE FUSION INTERSECT TYPE IDs.
		update	#AddItems
		set		Status = 0,
				StatusMessage = coalesce(StatusMessage,'') +
								IIF(SourceFusionIntersectTypeID is null, 'Could not find source fusion relationship type. ', '') + 
								IIF(SourceFusionAttributeID is null, 'Could not find source fusion path. ', '') + 
								IIF(TargetFusionIntersectTypeID is null, 'Could not find target fusion relationship type. ', '') + 
								IIF(TargetFusionAttributeID is null, 'Could not find target fusion path. ', '')
		where	(SourceFusionAttributeRaw is not null and SourceFusionIntersectTypeID is null) OR (TargetFusionAttributeRaw is not null and TargetFusionIntersectTypeID is null);

		-- ERROR OUT THE ROWS THAT DO NOT HAVE THE APPROPRIATE SOURCEs.
		update	#AddItems
		set		Status = 0,
				StatusMessage = coalesce(StatusMessage,'') +
								IIF(SourceSubjectID is null, 'Could not find source subject. ', '') + 
								IIF(SourceObjectID is null, 'Could not find source object. ', '')
		where	(SourceSubjectID is null) OR (SourceObjectID is null);

		-- ERROR OUT THE ROWS THAT DO NOT HAVE THE APPROPRIATE TARGETs.
		update	#AddItems
		set		Status = 0,
				StatusMessage = coalesce(StatusMessage,'') +
								IIF(TargetSubjectID is null, 'Could not find target subject. ', '') + 
								IIF(TargetObjectID is null, 'Could not find target object. ', '')
		where	(TargetSubjectID is null) OR (TargetObjectID is null);




		/*	BEGIN: SOURCE BUSINESS INTERSECT LOGIC */

		-- insert source business relationships
		insert into [Intersect] (IntersectTypeID, Subject, SubjectID, Object, ObjectID, Deleted, CreatedBy, CreatedOn, UpdatedBy, UpdatedOn)
			select	SourceIntersectTypeID, 
					SourceSubject, SourceSubjectID, 
					SourceObject, SourceObjectID,
					0, ResourceID, @dt, ResourceID, @dt
			from	(
					select		SourceIntersectTypeID, SourceSubject, SourceSubjectID, SourceObject, SourceObjectID, ResourceID
					from		#AddItems
					where		Status is null 
								and SourceIntersectID is null
					group by	SourceIntersectTypeID, SourceSubject, SourceSubjectID, SourceObject, SourceObjectID, ResourceID
					) O


		-- update rows with existing source business intersect
		update	T
		set		T.SourceIntersectID = S.ID,
				T.StatusMessage = coalesce(T.StatusMessage,'') + ' Source business relationship created.'
		from	#AddItems T
				inner join [Intersect] S on S.IntersectTypeID = T.SourceIntersectTypeID 
											and T.SourceSubject = S.Subject and T.SourceSubjectID = S.SubjectID 
											and T.SourceObject = S.Object and T.SourceObjectID = S.ObjectID
											and T.SourceIntersectID is null
											and T.Status is null;
		
		-- update rows with existing target business intersect
		update	T
		set		T.TargetIntersectID = S.ID
		from	#AddItems T
				inner join [Intersect] S on S.IntersectTypeID = T.TargetIntersectTypeID 
											and T.TargetSubject = S.Subject and T.TargetSubjectID = S.SubjectID 
											and T.TargetObject = S.Object and T.TargetObjectID = S.ObjectID
											and T.TargetIntersectID is null
											and T.Status is null;

		/*	END: SOURCE BUSINESS INTERSECT LOGIC */


		/*	BEGIN: TARGET BUSINESS INTERSECT LOGIC */

		-- insert target business relationships
		insert into [Intersect] (IntersectTypeID, Subject, SubjectID, Object, ObjectID, Deleted, CreatedBy, CreatedOn, UpdatedBy, UpdatedOn)
			select	TargetIntersectTypeID, 
					TargetSubject, TargetSubjectID, 
					TargetObject, TargetObjectID,
					0, ResourceID, @dt, ResourceID, @dt
			from	(
					select		TargetIntersectTypeID, TargetSubject, TargetSubjectID, TargetObject, TargetObjectID, ResourceID
					from		#AddItems
					where		Status is null 
								and TargetIntersectID is null
					group by	TargetIntersectTypeID, TargetSubject, TargetSubjectID, TargetObject, TargetObjectID, ResourceID
					) O

		-- update rows with existing target business intersect
		update	T
		set		T.TargetIntersectID = S.ID,
				T.StatusMessage = coalesce(T.StatusMessage,'') + ' Target business relationship created.'
		from	#AddItems T
				inner join [Intersect] S on S.IntersectTypeID = T.TargetIntersectTypeID 
											and T.TargetSubject = S.Subject and T.TargetSubjectID = S.SubjectID 
											and T.TargetObject = S.Object and T.TargetObjectID = S.ObjectID
											and T.TargetIntersectID is null
											and T.Status is null;

		/*	END: TARGET BUSINESS INTERSECT LOGIC */


		/*	BEGIN: SOURCE TECHNICAL INTERSECT LOGIC */

		-- insert source technical relationships
		insert into [Intersect] (IntersectTypeID, Subject, SubjectID, Object, ObjectID, Deleted, CreatedBy, CreatedOn, UpdatedBy, UpdatedOn)
			select	SourceFusionIntersectTypeID, 
					'Intersect', SourceIntersectID, 'FusionAttribute', SourceFusionAttributeID,
					0, ResourceID, @dt, ResourceID, @dt
			from	(
					select		SourceFusionIntersectTypeID, SourceIntersectID, SourceFusionAttributeID, ResourceID
					from		#AddItems
					where		Status is null
								and SourceFusionIntersectTypeID is not null
								and SourceFusionIntersectID is null
								and SourceIntersectID is not null
								and SourceFusionAttributeID is not null
					group by	SourceFusionIntersectTypeID, SourceIntersectID, SourceFusionAttributeID, ResourceID
					) O;

		-- update rows with new source technical intersect
		update	T
		set		T.SourceFusionIntersectID = S.ID,
				T.StatusMessage = coalesce(T.StatusMessage,'') + ' Source technical relationship created.'
		from	#AddItems T
				inner join [Intersect] S on S.IntersectTypeID = T.SourceFusionIntersectTypeID 
											and S.Subject = 'Intersect' and S.SubjectID = T.SourceIntersectID 
											and S.Object = 'FusionAttribute' and S.ObjectID = T.SourceFusionAttributeID
											and T.SourceFusionIntersectID is null 
											and T.Status is null;

		-- update rows with new target technical intersect
		update	T
		set		T.TargetFusionIntersectID = S.ID
		from	#AddItems T
				inner join [Intersect] S on S.IntersectTypeID = T.TargetFusionIntersectTypeID 
											and S.Subject = 'Intersect' and S.SubjectID = T.TargetIntersectID 
											and S.Object = 'FusionAttribute' and S.ObjectID = T.TargetFusionAttributeID
											and T.TargetFusionIntersectID is null 
											and T.Status is null;

		/*	END: SOURCE TECHNICAL INTERSECT LOGIC */


		/*	BEGIN: TARGET TECHNICAL INTERSECT LOGIC */
		
		-- insert target technical relationships
		insert into [Intersect] (IntersectTypeID, Subject, SubjectID, Object, ObjectID, Deleted, CreatedBy, CreatedOn, UpdatedBy, UpdatedOn)
			select	TargetFusionIntersectTypeID, 
					'Intersect', TargetIntersectID, 'FusionAttribute', TargetFusionAttributeID,
					0, ResourceID, @dt, ResourceID, @dt
			from	(
					select		TargetFusionIntersectTypeID, TargetIntersectID, TargetFusionAttributeID, ResourceID
					from		#AddItems
					where		Status is null
								and TargetFusionIntersectTypeID is not null
								and TargetFusionIntersectID is null
								and TargetIntersectID is not null
								and TargetFusionAttributeID is not null			
					group by	TargetFusionIntersectTypeID, TargetIntersectID, TargetFusionAttributeID, ResourceID
					) O;

		-- update rows with new target technical intersect
		update	T
		set		T.TargetFusionIntersectID = S.ID,
				T.StatusMessage = coalesce(T.StatusMessage,'') + ' Target technical relationship created.'
		from	#AddItems T
				inner join [Intersect] S on S.IntersectTypeID = T.TargetFusionIntersectTypeID 
											and S.Subject = 'Intersect' and S.SubjectID = T.TargetIntersectID 
											and S.Object = 'FusionAttribute' and S.ObjectID = T.TargetFusionAttributeID
											and T.TargetFusionIntersectID is null 
											and T.Status is null;

		/*	END: TARGET TECHNICAL INTERSECT LOGIC */

		-- insert new map items
		insert into MapItem (SourceIntersectID, TargetIntersectID, CreatedBy, CreatedOn, UpdatedBy, UpdatedOn)
			select	distinct
					SourceIntersectID, 
					TargetIntersectID,
					ResourceID,
					@dt, 
					ResourceID,
					@dt
			from	#AddItems
			where	SourceIntersectID is not null 
					and TargetIntersectID is not null 
					and MapItemID is null
					and Status is null;

		-- update source data with newly created map item IDs
		update	T
		set		T.MapItemID = S.ID,
				T.StatusMessage = coalesce(T.StatusMessage,'') + ' Business map created.'
		from	#AddItems T
				inner join [MapItem] S on	S.SourceIntersectID = T.SourceIntersectID 
											and S.TargetIntersectID = T.TargetIntersectID 
											and T.MapItemID is null 
											and T.Status is null;

		-- insert new map rule items
		insert into MapRuleItem (SourceFusionAttributeID, TargetFusionAttributeID, CreatedBy, CreatedOn, UpdatedBy, UpdatedOn)
			select	distinct
					SourceFusionAttributeID, 
					TargetFusionAttributeID,
					ResourceID,
					@dt, 
					ResourceID,
					@dt
			from	#AddItems
			where	SourceIntersectID is not null 
					and TargetIntersectID is not null
					and SourceFusionAttributeID is not null 
					and TargetFusionAttributeID is not null
					and Status is null;

		-- update source data with newly created map rule item IDs
		update	T
		set		T.MapRuleItemID = S.ID,
				T.StatusMessage = coalesce(T.StatusMessage,'') + ' Technical map created.'
		from	#AddItems T
				inner join [MapRuleItem] S on	S.SourceFusionAttributeID = T.SourceFusionAttributeID 
												and S.TargetFusionAttributeID = T.TargetFusionAttributeID 
												and T.MapRuleItemID is null 
												and Status is null;

		-- MERGE MapRuleItemMapItem with all the IDs above
		merge	MapRuleItemMapItem as T
		using	(
				select		MapItemID, 
							MapRuleItemID
				from		#AddItems
				where		MapItemID is not null
							and MapRuleItemID is not null
				group by	MapItemID, 
							MapRuleItemID
				) as S
		on		T.MapRuleItemID = S.MapRuleItemID and T.MapItemID = S.MapItemID
		when	not matched by target then
				insert (MapRuleItemID, MapItemID)
				values (S.MapRuleItemID, S.MapItemID);

		
		-- CALCULATE STATUS BASED ON POPULATED IDs
		update	#AddItems
		set		Status = 1
		where	MapItemID is not null 
				and (
					(SourceFusionAttributeRaw is not null and TargetFusionAttributeRaw is not null and MapRuleItemID is not null) 
					or 
					(SourceFusionAttributeRaw is null and TargetFusionAttributeRaw is null)
				);

		-- Now update LoadItems on original Load with status and messages created above
		update	T
		set		T.Status = S.Status,
				T.StatusMessage = S.StatusMessage,
				T.Object = case S.Status
							when 1 then 'MapItem'
							else NULL
						   end,
				T.ObjectID = case S.Status
							when 1 then S.MapItemID
							else NULL
						   end
		from	LoadItem T
				inner join #AddItems S on T.LoadID = @id and S.RowIndex = T.RowIndex;


--select *,  case [Status] when 1 then 'Complete' when 0 then 'Failed' else 'Queued' end as [Status] from LoadItem where LoadID = 270

		-- NOW, Close out the Load job ----------------------------------------------------------------------------------
		update	LoadItem
		set		Status = cast(0 as bit),
				StatusMessage = 'Incomplete : ' + coalesce(StatusMessage,''),
				Object = null,
				ObjectID = null
		where	LoadID = @id and Status is null;

		update	[Load]
		set		DateCompleted = getutcdate()
		where	ID = @id;

		COMMIT TRANSACTION [Tran1]
	END TRY
	BEGIN CATCH
		ROLLBACK TRANSACTION [Tran1]
		select ERROR_MESSAGE()
		update	[Load]
		set		Notes = Notes + '<br/> ' + ERROR_MESSAGE()
		where	ID = @id;
	END CATCH
end
GO

alter procedure [bulkload].[UpdateDynamicLookupFieldColumns]
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
									when ( (L_A.ID is not null) OR (F.AllowAllValue = 1 AND IC.Value = F.AllowAllLabel AND F.LookupObjectType in ('Artifact', 'ArtifactType')) ) then 'Artifact'
									when ( (L_D.ID is not null) OR (F.AllowAllValue = 1 AND IC.Value = F.AllowAllLabel AND F.LookupObjectType = 'ReferenceItemType') ) then 'ReferenceItemType'
									when ( (L_DI.ID is not null) OR (F.AllowAllValue = 1 AND IC.Value = F.AllowAllLabel AND F.LookupObjectType = 'ReferenceItem') ) then 'ReferenceItem'
									when ( (L_F.ID is not null) OR (F.AllowAllValue = 1 AND IC.Value = F.AllowAllLabel AND F.LookupObjectType = 'FusionAttributeType') ) then 'FusionAttribute'
									when ( (L_L.Value is not null) OR (F.AllowAllValue = 1 AND IC.Value = F.AllowAllLabel AND F.LookupObjectType = 'Lookup') ) then 'Lookup'
									when ( (L_T.ID is not null) OR (F.AllowAllValue = 1 AND IC.Value = F.AllowAllLabel AND F.LookupObjectType in ('Taxonomy', 'TaxonomyType')) ) then 'Taxonomy'
									else NULL
								end as LookupObject,
								case 
									when L_A.ID is not null then L_A.ID
									when F.AllowAllValue = 1 AND IC.Value = F.AllowAllLabel AND F.LookupObjectType in ('Artifact', 'ArtifactType') then 0

									when L_D.ID is not null then L_D.ID
									when F.AllowAllValue = 1 AND IC.Value = F.AllowAllLabel AND F.LookupObjectType = 'ReferenceItemType' then 0

									when L_DI.ID is not null then L_DI.ID
									when F.AllowAllValue = 1 AND IC.Value = F.AllowAllLabel AND F.LookupObjectType = 'ReferenceItem' then 0

									when L_F.ID is not null then L_F.ID
									when F.AllowAllValue = 1 AND IC.Value = F.AllowAllLabel AND F.LookupObjectType = 'FusionAttributeType' then 0

									when L_L.Value is not null then L_L.Value
									when F.AllowAllValue = 1 AND IC.Value = F.AllowAllLabel AND F.LookupObjectType = 'Lookup' then 0

									when L_T.ID is not null then L_T.ID
									when F.AllowAllValue = 1 AND IC.Value = F.AllowAllLabel AND F.LookupObjectType in ('Taxonomy', 'TaxonomyType') then 0

									else NULL
								end as LookupObjectID --coalesce(L_A.ID, L_D.ID, L_DI.ID, L_F.ID, L_L.Value, L_T.ID) as LookupObjectID -- L_I.ID,
						from	FieldType F
								inner join [Load] L on L.ID = @id and L.[Object] = F.[Object] and L.ObjectID = F.ObjectID and F.[Type] = 'Lookup'
								inner join [LoadColumn] C on C.LoadID = L.ID and F.Name = C.Name
								inner join [LoadItemColumn] IC on IC.LoadID = C.LoadID and IC.ColumnIndex = C.ColumnIndex
								
								left join Artifact L_A on F.LookupObjectType in ('Artifact', 'ArtifactType') and L_A.ArtifactTypeID = F.LookupObjectID and (L_A.[Name] = IC.Value OR L_A.TextPath = IC.Value)
								left join ReferenceItemType L_D on F.LookupObjectType = 'ReferenceItemType' and L_D.ID = F.LookupObjectID and L_D.[Name] = IC.Value
								left join ReferenceItem L_DI on F.LookupObjectType = 'ReferenceItem' and L_DI.ReferenceItemTypeID = F.LookupObjectID and L_DI.[Code] = IC.Value
								left join FusionAttribute L_F on F.LookupObjectType = 'FusionAttributeType' and L_F.FusionAttributeTypeID = F.LookupObjectID and (L_F.[Name] = IC.Value OR L_F.TextPath = IC.Value)
								left join [FieldLookupValue] L_L on F.ID = L_L.FieldTypeID and F.LookupObjectType = 'Lookup' and L_L.LookupObjectType = 'Lookup' and L_L.LookupObjectID = F.LookupObjectID and L_L.[Text] = IC.Value
								left join Taxonomy L_T on F.LookupObjectType in ('Taxonomy', 'TaxonomyType') and L_T.TaxonomyTypeID = F.LookupObjectID and (L_T.[Name] = IC.Value OR L_T.TextPath = IC.Value)
						where	C.ColumnIndex between @startColumnIndex and @endColumnIndex
						) S on S.LoadID = T.LoadID and S.RowIndex = T.RowIndex and S.ColumnIndex = T.ColumnIndex

	while @startColumnIndex <= @endColumnIndex
	begin
		update	T
		set		T.StatusMessage = coalesce(T.StatusMessage, '') + S.StatusMessage
		from	LoadItem T
				inner join	(
							select	IC.LoadID,
									IC.RowIndex,
									case 
										when IC.LookupObjectID is null and IC.Value is not null and IC.Value <> '' then ' ' + F.Name + ' does not contain a valid value.'
										else ''
									end StatusMessage
							from	FieldType F
									inner join [Load] L on L.ID = @id and L.[Object] = F.[Object] and L.ObjectID = F.ObjectID and F.[Type] = 'Lookup'
									inner join [LoadColumn] C on C.LoadID = L.ID and F.Name = C.Name
									inner join [LoadItemColumn] IC on IC.LoadID = C.LoadID and IC.ColumnIndex = C.ColumnIndex and IC.columnIndex = @startColumnIndex and IC.LookupObjectID is null
							) S on S.LoadID = T.LoadID and S.RowIndex = T.RowIndex
		set @startColumnIndex = @startColumnIndex + 1
	end
end
GO

alter procedure [bulkload].[UpdateItemColumn]
	@id int,
	@globalTypeColumn int, 
	@typeColumn int, 
	@subjectAreaColumn int, 
	@itemColumn int
as
begin
	set nocount on;
	update	T
	set		T.LookupObject = TTT.Value,
			T.LookupObjectID = coalesce(A.ID, D.ID, DI.ID, I.ID, M.ID, P.ID, R.ID, TA.ID)
	from	LoadItemColumn T
			inner join LoadItemColumn TT on TT.LoadID = T.LoadID and T.LoadID = @id and TT.RowIndex = T.RowIndex and TT.ColumnIndex = @typeColumn and T.ColumnIndex = @itemColumn
			inner join LoadItemColumn TS on TS.LoadID = T.LoadID and TS.RowIndex = T.RowIndex and TS.ColumnIndex = @subjectAreaColumn
			inner join LoadItemColumn TTT on TTT.LoadID = T.LoadID and TTT.RowIndex = T.RowIndex and TTT.ColumnIndex = @globalTypeColumn
			left join Artifact A on lower(A.TextPath) = lower(T.Value) and A.TaxonomyTypeID = TS.LookupObjectID and A.ArtifactTypeID = TT.LookupObjectID and TTT.Value = 'Artifact'
			left join ReferenceItemType D on lower(D.Name) = lower(T.Value) and TTT.Value = 'ReferenceItemType' and TT.LookupObjectID = 0
			left join ReferenceItem DI on lower(DI.DisplayValue) = lower(T.Value) and TTT.Value = 'ReferenceItem' and DI.ReferenceItemTypeID = TT.LookupObjectID
			left join [Intersect] I on lower(I.Name) = lower(T.Value) and I.IntersectTypeID = TT.LookupObjectID and TTT.Value = 'Intersect'
			left join [Map] M on lower(M.Name) = lower(T.Value) and M.MapTypeID = TT.LookupObjectID and TTT.Value = 'Map'
			left join [Policy] P on lower(P.TextPath) = lower(T.Value) and P.PolicyTypeID = TT.LookupObjectID and TTT.Value = 'Policy'
			left join [Rule] R on lower(R.Name) = lower(T.Value) and R.RuleTypeID = TT.LookupObjectID and TTT.Value = 'Rule'
			left join [Taxonomy] TA on lower(TA.TextPath) = lower(T.Value) and TA.TaxonomyTypeID = TT.LookupObjectID and TTT.Value = 'Taxonomy'
	where	coalesce(A.ID, D.ID, DI.ID, I.ID, M.ID, P.ID, R.ID, TA.ID) is not null
end
GO

alter procedure [bulkload].[UpdateItemColumnByIntersectType]
	@id int,
	@intersectTypeColumn int, 
	@isSubject bit,
	@subjectAreaColumn int, 
	@itemColumn int
as
begin
	set nocount on;
	update	T
	set		T.LookupObject = replace(case when @isSubject = 1 then IT.Subject else IT.Object end, 'Type', ''),
			T.LookupObjectID = coalesce(A.ID, D.ID, F.ID, I.ID, P.ID, R.ID, TA.ID)
	from	LoadItemColumn T
			inner join LoadItemColumn TI on TI.LoadID = T.LoadID and TI.RowIndex = T.RowIndex and TI.ColumnIndex = @intersectTypeColumn and T.ColumnIndex = @itemColumn
			inner join IntersectType IT on TI.LookupObject = 'IntersectType' and IT.ID = TI.LookupObjectID
			left join LoadItemColumn TS on TS.LoadID = T.LoadID and TS.RowIndex = T.RowIndex and TS.ColumnIndex = @subjectAreaColumn
			left join Artifact A on lower(A.TextPath) = lower(T.Value) and A.TaxonomyTypeID = TS.LookupObjectID and A.ArtifactTypeID = case when @isSubject = 1 then IT.SubjectID else IT.ObjectID end and 'ArtifactType' = case when @isSubject = 1 then IT.Subject else IT.Object end
			left join ReferenceItemType D on lower(D.Name) = lower(T.Value) and 'ReferenceItemType' = case when @isSubject = 1 then IT.Subject else IT.Object end
			left join FusionAttribute F on lower(F.TextPath) = lower(T.Value) and F.FusionAttributeTypeID = case when @isSubject = 1 then IT.SubjectID else IT.ObjectID end and 'FusionAttributeType' = case when @isSubject = 1 then IT.Subject else IT.Object end
			left join [Intersect] I on lower(I.Name) = lower(T.Value) and I.IntersectTypeID = case when @isSubject = 1 then IT.SubjectID else IT.ObjectID end and 'IntersectType' = case when @isSubject = 1 then IT.Subject else IT.Object end
			left join [Policy] P on lower(P.TextPath) = lower(T.Value) and P.PolicyTypeID = case when @isSubject = 1 then IT.SubjectID else IT.ObjectID end and 'PolicyType' = case when @isSubject = 1 then IT.Subject else IT.Object end
			left join [Rule] R on lower(R.Name) = lower(T.Value) and R.RuleTypeID = case when @isSubject = 1 then IT.SubjectID else IT.ObjectID end and 'RuleType' = case when @isSubject = 1 then IT.Subject else IT.Object end
			left join [Taxonomy] TA on lower(TA.TextPath) = lower(T.Value) and TA.TaxonomyTypeID = case when @isSubject = 1 then IT.SubjectID else IT.ObjectID end and 'TaxonomyType' = case when @isSubject = 1 then IT.Subject else IT.Object end
	where	T.LoadID = @id and coalesce(A.ID, D.ID, F.ID, I.ID, P.ID, R.ID, TA.ID) is not null
end
GO

alter procedure [bulkload].[UpdateItemColumnByType]
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
			T.LookupObjectID = coalesce(A.ID, D.ID, DI.ID, F.ID, I.ID, M.ID, P.ID, R.ID, TA.ID)
	from	LoadItemColumn T
			left join LoadItemColumn TS on TS.LoadID = T.LoadID and TS.RowIndex = T.RowIndex and TS.ColumnIndex = @subjectAreaColumn and T.ColumnIndex = @itemColumn
			left join Artifact A on lower(A.TextPath) = lower(T.Value) and A.TaxonomyTypeID = TS.LookupObjectID and A.ArtifactTypeID = @ObjectTypeID and @ObjectType = 'ArtifactType'
			left join ReferenceItemType D on lower(D.Name) = lower(T.Value) and @ObjectType = 'ReferenceItemType'
			left join ReferenceItem DI on lower(DI.DisplayValue) = lower(T.Value) and @ObjectType = 'ReferenceItemType' and DI.ReferenceItemTypeID = @ObjectTypeID
			left join FusionAttribute F on lower(F.TextPath) = lower(T.Value) and F.FusionAttributeTypeID = @ObjectTypeID and @ObjectType = 'FusionAttributeType'
			left join [Intersect] I on lower(I.Name) = lower(T.Value) and I.IntersectTypeID = @ObjectTypeID and @ObjectType = 'IntersectType'
			left join [Map] M on lower(M.Name) = lower(T.Value) and M.MapTypeID = @ObjectTypeID and @ObjectType = 'MapType'
			left join [Policy] P on lower(P.TextPath) = lower(T.Value) and P.PolicyTypeID = @ObjectTypeID and @ObjectType = 'PolicyType'
			left join [Rule] R on lower(R.Name) = lower(T.Value) and R.RuleTypeID = @ObjectTypeID and @ObjectType = 'RuleType'
			left join [Taxonomy] TA on lower(TA.TextPath) = lower(T.Value) and TA.TaxonomyTypeID = @ObjectTypeID and @ObjectType = 'TaxonomyType'
	where	T.LoadID = @id and coalesce(A.ID, D.ID, DI.ID, F.ID, I.ID, M.ID, P.ID, R.ID, TA.ID) is not null
end
GO


alter procedure [cache].[ReSynchronizeAllObjectDetails]
as
begin
	set nocount on;

	IF OBJECT_ID('tempdb..#Recache') IS NOT NULL
    DROP TABLE #Recache

	create table #Recache (
		[Object] varchar(50) not null,
		ObjectID int not null,
		ObjectType varchar(25) not null,
		ObjectTypeID int not null
	);

	declare @type varchar(50);
	
	begin
		set @type = 'Artifact'
		insert into #Recache
			SELECT	@type, ID, 'ArtifactType', ArtifactTypeID FROM Artifact;
	end;

	begin
		set @type = 'ArtifactType'
		insert into #Recache
			SELECT	@type, ID, @type, ID FROM ArtifactType;
	end;

	begin
		set @type = 'AttributeType';
		insert into #Recache
			SELECT	@type, ID, 'AttributeType', ID FROM AttributeType;
	end;

	begin
		set @type = 'Group';
		insert into #Recache
			SELECT	@type, ID, 'GroupType', 1 FROM [Group];
	end;

	begin
		set @type = 'Intersect';
		insert into #Recache
			SELECT	@type, ID, 'IntersectType', IntersectTypeID FROM [Intersect];
	end;

	begin
		set @type = 'IntersectType';
		insert into #Recache
			SELECT	@type, ID, @type, ID FROM IntersectType;
	end;

	begin
		set @type = 'Lookup';
		insert into #Recache
			SELECT	@type, ID, 'LookupType', LookupTypeID FROM [Lookup];
	end;

	begin
		set @type = 'LookupType';
		insert into #Recache
			SELECT	@type, ID, @type, ID FROM LookupType;
	end;

	begin
		set @type = 'Fusion';
		insert into #Recache
			SELECT	@type, ID, 'FusionType', FusionTypeID FROM Fusion;
	end;

	begin
		set @type = 'FusionType';
		insert into #Recache
			SELECT	@type, ID, @type, ID FROM FusionType;
	end;

/*	begin
		set @type = 'FusionAttribute';
		insert into #Recache
			SELECT	@type, ID, 'FusionAttributeType', FusionAttributeTypeID FROM FusionAttribute;
	end;*/
 
	begin
		set @type = 'FusionAttributeType';
		insert into #Recache
			SELECT	@type, ID, 'FusionType', FusionTypeID FROM FusionAttributeType;
	end;

	begin
		set @type = 'GroupType';
		insert into #Recache values (@type, 0, @type, 0);
		insert into #Recache values (@type, 1, @type, 0);
	end;

	begin
		set @type = 'Policy';
		insert into #Recache
			SELECT	@type, ID, 'PolicyType', PolicyTypeID FROM [Policy];
	end;

	begin
		set @type = 'PolicyType';
		insert into #Recache
			SELECT	@type, ID, 'PolicyType', ID FROM [PolicyType];
	end;

	begin
		set @type = 'ReferenceItemType';
		insert into #Recache
			SELECT	@type, ID, 'ReferenceItemType', ID FROM ReferenceItemType;
	end;

	begin
		set @type = 'Resource';
		insert into #Recache
			select	@type, ResourceID, 'ResourceType', 1 from reporting.Global_Resource;
	end;

	begin
		set @type = 'ResourceType';
		insert into #Recache values (@type, 0, @type, 0)
		insert into #Recache values (@type, 1, @type, 0)
	end;

	begin
		set @type = 'ResponsibilityType';
		insert into #Recache
			SELECT	@type, ID, @type, 0 FROM ResponsibilityType;
	end;

	begin
		set @type = 'Rule';
		insert into #Recache
			SELECT	@type, ID, 'RuleType', RuleTypeID FROM [Rule];
	end;

	begin
		set @type = 'RuleType';
		insert into #Recache
			SELECT	@type, ID, 'RuleType', ID FROM [RuleType];
	end;

	begin
		set @type = 'Taxonomy';
		insert into #Recache
			SELECT	@type, ID, 'TaxonomyType', TaxonomyTypeID FROM Taxonomy
	end;

	begin
		set @type = 'TaxonomyType';
		insert into #Recache
			SELECT	@type, ID, @type, ID FROM TaxonomyType;
	end;

	-- upsert the individual object into the cache table.
	merge	cache.[Object] as T
	using	(
			SELECT	*
			FROM	#Recache
			) as S
	on		(
			T.[Object] = S.[Object] and T.[ObjectID] = S.[ObjectID]
			)
	when matched then
			update	
			set		T.ObjectType = S.ObjectType,
					T.ObjectTypeID = S.ObjectTypeID
	when not matched then
			insert ( [Object], ObjectID, ObjectType, ObjectTypeID )
			values ( S.[Object], S.ObjectID, S.ObjectType, S.ObjectTypeID );
end
GO

alter procedure [cache].[SynchronizeObjectDetails]
--declare 
	@type varchar(50),
	@id int
--set @type = 'IntersectType'
--set @id = 27
as
begin
	set nocount on;

	declare @item table (
		[Object] varchar(50) not null,
		ObjectID int not null,
		ObjectType varchar(25) not null,
		ObjectTypeID int not null
	);

	if @type = 'Artifact'
	begin
		insert into @item ([Object], ObjectID, ObjectType, ObjectTypeID)
			SELECT	@type, O.ID, 'ArtifactType', O.ArtifactTypeID
			FROM	Artifact O
			WHERE	O.ID = @id
	end;

	if @type = 'ArtifactType'
	begin
		insert into @item ([Object], ObjectID, ObjectType, ObjectTypeID)
			SELECT	@type, O.ID, @type, 0
			FROM	ArtifactType O
			WHERE	O.ID = @id;
	end;

	if @type = 'AttributeType'
	begin
		insert into @item ([Object], ObjectID, ObjectType, ObjectTypeID)
			SELECT	@type, O.ID, 
					'AttributeType', 0
			FROM	AttributeType O
			WHERE	O.ID = @id;
	end;

	if @type = 'Group'
	begin
		insert into @item ([Object], ObjectID, ObjectType, ObjectTypeID)
			SELECT	@type, ID, 'GroupType', 0
			FROM	[Group]
			WHERE	ID = @id;
	end;

	if @type = 'GroupType'
	begin
		insert into @item ([Object], ObjectID, ObjectType, ObjectTypeID)
			select	@type, 0, @type, 0
	end;


	if @type = 'Intersect'
	begin
		insert into @item ([Object], ObjectID, ObjectType, ObjectTypeID)
			SELECT	@type, O.ID, 'IntersectType', O.IntersectTypeID
			FROM	[Intersect] O
			WHERE	O.ID = @id;
	end;

	if @type = 'IntersectType'
	begin
		insert into @item ([Object], ObjectID, ObjectType, ObjectTypeID)
			SELECT	@type, ID, @type, 0
			FROM	IntersectType
			WHERE	ID = @id;
	end;

	if @type = 'Lookup'
	begin
		insert into @item ([Object], ObjectID, ObjectType, ObjectTypeID)
			SELECT	@type, O.ID, 'LookupType', O.LookupTypeID
			FROM	[Lookup] O
					INNER JOIN LookupType T ON O.LookupTypeID = T.ID AND O.ID = @id;
	end;

	if @type = 'LookupType'
	begin
		insert into @item ([Object], ObjectID, ObjectType, ObjectTypeID)
			SELECT	@type, ID, @type, 0
			FROM	LookupType
			WHERE	ID = @id;
	end;

	if @type = 'Fusion'
	begin
		insert into @item ([Object], ObjectID, ObjectType, ObjectTypeID)
			SELECT	@type, O.ID, 'FusionType', O.FusionTypeID
			FROM	Fusion O
			WHERE	O.ID = @id;
	end;

	if @type = 'FusionType'
	begin
		insert into @item ([Object], ObjectID, ObjectType, ObjectTypeID)
			SELECT	@type, ID, @type, 0
			FROM	FusionType
			WHERE	ID = @id;
	end;

	if @type = 'FusionAttribute'
	begin
		insert into @item ([Object], ObjectID, ObjectType, ObjectTypeID)
			SELECT	@type, O.ID, 'FusionAttributeType', O.FusionAttributeTypeID
			FROM	FusionAttribute O
			WHERE	O.ID = @id;
	end;

	if @type = 'FusionAttributeType'
	begin
		insert into @item ([Object], ObjectID, ObjectType, ObjectTypeID)
			SELECT	@type, O.ID, 'FusionType', O.FusionTypeID
			FROM	FusionAttributeType O
			WHERE	O.ID = @id;
	end;

	if @type = 'Policy'
	begin
		insert into @item ([Object], ObjectID, ObjectType, ObjectTypeID)
			SELECT	@type, O.ID, 'PolicyType', O.PolicyTypeID
			FROM	[Policy] O
			WHERE	O.ID = @id;
	end;

	if @type = 'PolicyType'
	begin
		insert into @item ([Object], ObjectID, ObjectType, ObjectTypeID)
			SELECT	@type, T.ID, @type, T.PolicyTypeClassID
			FROM	PolicyType T
			WHERE	T.ID = @id;
	end;

	if @type = 'ReferenceItemType'
	begin
		insert into @item ([Object], ObjectID, ObjectType, ObjectTypeID)
			SELECT	@type, T.ID, @type, 0
			FROM	ReferenceItemType T
			WHERE	T.ID = @id;
	end;

	if @type = 'Resource'
	begin
		insert into @item ([Object], ObjectID, ObjectType, ObjectTypeID)
			select	@type, ResourceID, 'ResourceType', 1
			from	reporting.Global_Resource 
			where	ResourceID = @id;
	end;

	if @type = 'ResourceType'
	begin
		insert into @item ([Object], ObjectID, ObjectType, ObjectTypeID)
			select	@type, 1, @type, 0
	end;

	if @type = 'ResponsibilityType'
	begin
		insert into @item ([Object], ObjectID, ObjectType, ObjectTypeID)
			SELECT	@type, ID, @type, 0
			FROM	ResponsibilityType
			WHERE	ID = @id;

		--UPDATE	T
		--SET		T.ResponsibilityType = S.Name
		--FROM	cache.ResponsibilityItem T INNER JOIN @item S ON S.[Object] = @type and S.ObjectID = T.ResponsibilityTypeID
	end;

	if @type = 'Rule'
	begin
		insert into @item ([Object], ObjectID, ObjectType, ObjectTypeID)
			SELECT	@type, O.ID, 'RuleType', O.RuleTypeID
			FROM	[Rule] O
			WHERE	O.ID = @id;
	end;

	if @type = 'RuleType'
	begin
		insert into @item ([Object], ObjectID, ObjectType, ObjectTypeID)
			SELECT	@type, T.ID, @type, T.ID
			FROM	RuleType T
			WHERE	T.ID = @id;
	end;

	if @type = 'Taxonomy'
	begin
		insert into @item ([Object], ObjectID, ObjectType, ObjectTypeID)
			SELECT	@type, O.ID, 'TaxonomyType', O.TaxonomyTypeID
			FROM	Taxonomy O
			WHERE	O.ID = @id;
	end;

	if @type = 'TaxonomyType'
	begin
		insert into @item ([Object], ObjectID, ObjectType, ObjectTypeID)
			SELECT	@type, T.ID, @type, T.TaxonomyTypeClassID
			FROM	TaxonomyType T
			WHERE	T.ID = @id;
	end;

	-- upsert the individual object into the cache table.
	merge	cache.[Object] as T
	using	(
			SELECT	*
			FROM	@item
			) as S
	on		(
			T.[Object] = S.[Object] and T.[ObjectID] = S.[ObjectID]
			)
	when matched then
			update	
			set		T.ObjectType = S.ObjectType, 
					T.ObjectTypeID = S.ObjectTypeID
	when not matched then
			insert ( [Object], ObjectID, ObjectType, ObjectTypeID )
			values ( S.[Object], S.ObjectID, S.ObjectType, S.ObjectTypeID );
end
GO

ALTER PROCEDURE [dbo].[GetAllowedIntersectionTypes]
	@SourceType varchar(50),
	@SourceTypeID int,
	@IntersectID int = 0
AS
BEGIN
	SET NOCOUNT ON;

	declare @tbl table (IntersectTypeID int, TargetType varchar(50), TargetTypeID int, TargetName nvarchar(500), ParentIntersectID int, PredicateName nvarchar(100), SourceName nvarchar(500), SourceTypeID int, SourceType varchar(50));
	
	insert into @tbl
		(IntersectTypeID, TargetType, TargetTypeID, TargetName, ParentIntersectID, PredicateName, SourceName, SourceTypeID, SourceType)
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
				NULL,
				RT.PredicateName,
				case 
					when RT.Subject = @SourceType and RT.SubjectID = @SourceTypeID then RT.SubjectName
					else RT.ObjectName
				end AS SourceName,
				case 
					when RT.Subject = @SourceType and RT.SubjectID = @SourceTypeID then RT.SubjectID
					else RT.ObjectID
				end AS SourceTypeID,
				case 
					when RT.Subject = @SourceType and RT.SubjectID = @SourceTypeID then RT.Subject
					else RT.Object
				end AS SourceType
		FROM	IntersectTypeDetail RT
		WHERE	(RT.Subject = @SourceType and RT.SubjectID = @SourceTypeID) OR 
				(RT.Object = @SourceType and RT.ObjectID = @SourceTypeID)
				
	-- load any map types for this object
			insert into @tbl
			(IntersectTypeID, TargetType, TargetTypeID, TargetName, ParentIntersectID, PredicateName, SourceName, SourceTypeID, SourceType)
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
					NULL,
					RT.PredicateName,
					case 
						when RT.Subject = @SourceType and RT.SubjectID = @SourceTypeID then RT.SubjectName
						else RT.ObjectName
					end AS SourceName,
					case 
						when RT.Subject = @SourceType and RT.SubjectID = @SourceTypeID then RT.SubjectID
						else RT.ObjectID
					end AS SourceTypeID,
					case 
						when RT.Subject = @SourceType and RT.SubjectID = @SourceTypeID then RT.Subject
						else RT.Object
				end AS SourceType
			FROM	IntersectTypeDetail RT
					inner join @tbl t on (t.TargetType = 'MapType'  and (RT.Subject = 'MapType' and RT.SubjectID = t.TargetTypeID) );

	--delete the map type associated directly with this type					
	delete from @tbl where TargetType = 'MapType' and SourceTypeID = @SourceTypeID and SourceType = @SourceType;
	
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
					when 'Maptype' then 'Map: ' + SourceName
					else ''
				end + ' : ' + TargetName as TargetName, 
				ParentIntersectID,
				PredicateName
	from		@tbl 
	order by	case TargetType
					when 'TaxonomyType' then 'Model: '
					when 'DomainType' then 'Reference: '
					when 'FusionType' then 'Fusion: '
					when 'FusionAttributeType' then 'Fusion: '
					when 'ArtifactType' then 'Glossary: '
					when 'RuleType' then 'Rules: '
					when 'PolicyType' then 'Policies: '
					when 'Maptype' then 'Map: ' + SourceName
					else ''
				end + ' : ' + TargetName
END
GO

ALTER PROCEDURE [dbo].[GetAverageScoreByObjectType]
--declare
	@type varchar(50),-- = 'Artifact',
	@id int-- = 733
AS
begin
	declare 
			@oName nvarchar(250),
			@oTypeName nvarchar(250),
			@oType varchar(50),
			@oID int,
			@AveragePoints int,
			@MaxPoints int,
			@AverageScore int,
			@ObjectScore varchar(250)--int

	select	@oName = Name,
			@oTypeName = ObjectTypeName,
			@oType = ObjectType,
			@oID = ObjectTypeID
	from	cache.ObjectDetails 
	where	[Object] = @type and ObjectID = @id

	select	@ObjectScore = cast(round(avg(S.Value), 0) as int)	
	FROM	[Score] S
			inner join (
				select	max(ID) as ScoreID,
						Object,
						ObjectID,
						ScoreTypeID
				from	Score
				where		Object = @type and ObjectID = @id
				group by Object, ObjectID, ScoreTypeID
			) MS on MS.ScoreID = S.ID
	where	S.Object = @type and S.ObjectID = @id


  	select	@AverageScore = cast(round(avg(S.Value), 0) as int)	
	FROM	[Score] S
			inner join cache.Object C on C.Object = S.Object and C.ObjectID = S.ObjectID and C.ObjectType = @oType and C.ObjectTypeID = @oID
			inner join (
				select	max(SI.ID) as ScoreID,
						SI.Object,
						SI.ObjectID,
						SI.ScoreTypeID
				from	Score SI
						inner join cache.Object CI on CI.Object = SI.Object and CI.ObjectID = SI.ObjectID and CI.ObjectType = @oType and CI.ObjectTypeID = @oID
				group by SI.Object, SI.ObjectID, SI.ScoreTypeID
			) MS on MS.ScoreID = S.ID

	select	@type as [Object], @id as ObjectID, @oName as ObjectName, @ObjectScore as ObjectScore, 
			@oType as ObjectType, @oID as ObjectTypeID, @oTypeName as ObjectTypeName, @AverageScore as AverageScore 
end
GO

CREATE TYPE [dbo].[LineageTable] AS TABLE (
    [ID]                INT          NULL,
    [SourceIntersectID] INT          NULL,
    [SourceSubject]     VARCHAR (50) NULL,
    [SourceSubjectID]   INT          NULL,
    [SourceObject]      VARCHAR (50) NULL,
    [SourceObjectID]    INT          NULL,
    [TargetIntersectID] INT          NULL,
    [TargetSubject]     VARCHAR (50) NULL,
    [TargetSubjectID]   INT          NULL,
    [TargetObject]      VARCHAR (50) NULL,
    [TargetObjectID]    INT          NULL,
    [Deleting]          BIT          NULL,
    [Adding]            BIT          NULL);
GO

CREATE TYPE [dbo].[LineageTechnicalTable] AS TABLE (
    [ID]                      INT NULL,
    [MapItemID]               INT NULL,
    [SourceFusionAttributeID] INT NULL,
    [TargetFusionAttributeID] INT NULL,
    [Deleting]                BIT NULL,
    [Adding]                  BIT NULL);
GO


ALTER procedure [dbo].[GetLineage]
--declare 
	@type varchar(50),
	@id int,
	@view int = 1,
	@usageOnly bit = 0,
	@rows LineageTable readonly,
	@technicalRows LineageTechnicalTable readonly

--set @type = 'Artifact'
--set @id = 550
--set @view = 1
as
begin
	declare @links table ([from] varchar(250), [to] varchar(250), category varchar(50))
	declare @nodes table (
		[key] varchar(250), 
		obj varchar(50), [objid] int, [type] varchar(50), typeName nvarchar(250), name nvarchar(500), shortname nvarchar(500),
		back varchar(7), fore varchar(7), template varchar(50), other varchar(500),

		HasSourceRules bit
		)
	declare @objects table (Type varchar(50), ID int)

	if @view in (0, 1, 2)
	begin
		insert into @objects values (@type, @id)

		if not exists(
			select	MI.ID
			from	MapItem MI
					inner join [Intersect] SI on SI.ID = MI.SourceIntersectID
					inner join [Intersect] TI ON TI.ID = MI.TargetIntersectID
			where 	( (SI.Subject = @type and SI.SubjectID = @id) OR (SI.Object = @type and SI.ObjectID = @id)  )
					OR ( (TI.Subject = @type and TI.SubjectID = @id) OR (TI.Object = @type and TI.ObjectID = @id)  )
		)
		begin
			insert into @objects
				select	case 
							when I.Subject = @type and I.SubjectID = @id then I.Object
							else I.Subject
						end,
						case 
							when I.Subject = @type and I.SubjectID = @id then I.ObjectID 
							else I.SubjectID 
						end
				from	[Intersect] I
						inner join IntersectType T on T.ID = I.IntersectTypeID 
						inner join Predicate P on P.ID = T.PredicateID and P.Type = 6
				where	(I.Subject = @type and I.SubjectID = @id) or (I.Object = @type and I.ObjectID = @id)
		end

		declare @points table ( ID int, SourceIntersectID int, TargetIntersectID int );
		declare @forwardPoints table ( ID int, SourceIntersectID int, TargetIntersectID int );

		-- get all items directly tied to the focal object.
		insert into @points
			select	MI.ID, MI.SourceIntersectID, MI.TargetIntersectID
			from	MapItem MI
					inner join [Intersect] SI on SI.ID = MI.SourceIntersectID
					inner join [Intersect] TI ON TI.ID = MI.TargetIntersectID
					inner join @objects O on	( (SI.Subject = O.Type and SI.SubjectID = O.ID) OR (SI.Object = O.Type and SI.ObjectID = O.ID)  ) OR 
												( (TI.Subject = O.Type and TI.SubjectID = O.ID) OR (TI.Object = O.Type and TI.ObjectID = O.ID)  )
			where not exists (select 1 from @rows R where R.Deleting = 1 and R.ID = MI.ID)

		-- get all items not directly tied to the focal object, but still tied to maps involved above.
		insert into @points
			select	MI.ID, MI.SourceIntersectID, MI.TargetIntersectID
			from	MapItem MI
					inner join	(
								select	ID.MapItemID
								from	MapItemMap DM
										inner join @points D on D.ID = DM.MapItemID
										inner join MapItemMap ID on ID.MapID = DM.MapID and ID.MapItemID not in (
																												select ID from @points
																												)
								) O on O.MapItemID = MI.ID
			where not exists (select 1 from @rows R where R.Deleting = 1 and R.ID = MI.ID);

		insert into @forwardPoints
			select * from @points

			--join editor rows to any existing intersects
			if exists(select 1 from @rows)
			begin
				insert into @points
					select	R.ID,
							D1.ID as SourceIntersectID,
							D2.ID as TargetIntersectID
					from	@rows R
							inner join [Intersect] D1 on 
								R.SourceSubject = D1.[Subject] AND 
								R.SourceObject = D1.[Object] AND 
								R.SourceSubjectID = D1.SubjectID AND 
								R.SourceObjectID = D1.ObjectID
							inner join [Intersect] D2 on 
								R.TargetSubject = D2.[Subject] AND 
								R.TargetObject = D2.[Object] AND 
								R.TargetSubjectID = D2.SubjectID AND 
								R.TargetObjectID = D2.ObjectID
					where	R.Adding = 1 
			end;

		with cte as (
			select	ID,
					SourceIntersectID,
					TargetIntersectID,
					1 as [Level]
			from	@points P
			where not exists (select 1 from @rows R where R.Deleting = 1 and R.ID = P.ID)
			union all
			select	S.ID,
					S.SourceIntersectID,
					S.TargetIntersectID,
					T.[Level] + 1 as [Level]
			from	MapItem S
					inner join cte T on T.SourceIntersectID = S.TargetIntersectID and S.ID <> T.ID
			where	T.[Level] <= 10 and not exists (select 1 from @rows R where R.Deleting = 1 and R.ID = S.ID)
		)
		insert into @points
			select ID, SourceIntersectID, TargetIntersectID from cte where ID not in (select ID from @points);


		with cteF as (
			select	ID,
					SourceIntersectID,
					TargetIntersectID,
					1 as [Level]
			from	@forwardPoints P
			where not exists (select 1 from @rows R where R.Deleting = 1 and R.ID = P.ID)
			union all
			select	S.ID,
					S.SourceIntersectID,
					S.TargetIntersectID,
					T.[Level] + 1 as [Level]
			from	MapItem S
					inner join cteF T on T.TargetIntersectID = S.SourceIntersectID and S.ID <> T.ID
			where	T.[Level] <= 10 and not exists (select 1 from @rows R where R.Deleting = 1 and R.ID = S.ID)
		)
		insert into @points
			select ID, SourceIntersectID, TargetIntersectID from cteF where ID not in (select ID from @forwardPoints)

		declare @items table (
			ID int,
			SourceIntersectID int, 
			SourceSubjectTypeName nvarchar(500), SourceSubjectName nvarchar(500), SourceSubjectShortName nvarchar(500), SourceSubject varchar(50), SourceSubjectID int, SourceSubjectIconBackColor varchar(7), SourceSubjectIconForeColor varchar(7), 
			SourceObjectTypeName nvarchar(500), SourceObjectName nvarchar(500), SourceObjectShortName nvarchar(500), SourceObject varchar(50), SourceObjectID int, SourceObjectIconBackColor varchar(7), SourceObjectIconForeColor varchar(7),
			
			TargetIntersectID int, 
			TargetSubjectTypeName nvarchar(500), TargetSubjectName nvarchar(500), TargetSubjectShortName nvarchar(500), TargetSubject varchar(50), TargetSubjectID int, TargetSubjectIconBackColor varchar(7), TargetSubjectIconForeColor varchar(7), 
			TargetObjectTypeName nvarchar(500), TargetObjectName nvarchar(500), TargetObjectShortName nvarchar(500), TargetObject varchar(50), TargetObjectID int, TargetObjectIconBackColor varchar(7), TargetObjectIconForeColor varchar(7),

			HasSourceRules bit
		)

		insert into @items
			select	O.ID,				
					O.SourceIntersectID,
					SI.SubjectTypeName,
					SI.SubjectName,
					SI.SubjectShortName,
					SI.Subject,
					SI.SubjectID,
					SI.SubjectIconBackColor,
					SI.SubjectIconForeColor,
					SI.ObjectTypeName,
					SI.ObjectName,
					SI.ObjectShortName,
					SI.Object,
					SI.ObjectID,
					SI.ObjectIconBackColor,
					SI.ObjectIconForeColor,
					O.TargetIntersectID,
					TI.SubjectTypeName,
					TI.SubjectName,
					TI.SubjectShortName,
					TI.Subject,
					TI.SubjectID,
					TI.SubjectIconBackColor,
					TI.SubjectIconForeColor,
					TI.ObjectTypeName,
					TI.ObjectName,
					TI.ObjectShortName,
					TI.Object,
					TI.ObjectID,
					TI.ObjectIconBackColor,
					TI.ObjectIconForeColor,
					case 
						when HSR.C > 0 then cast(1 as bit)
						else cast(0 as bit)
					end as HasSourceRules
			from	@points O
					inner join IntersectDetail SI on SI.ID = O.SourceIntersectID
					inner join IntersectDetail TI ON TI.ID = O.TargetIntersectID
					cross apply (
								select	count(1) as C
								from	MapItem MI 
										inner join MapSequence MS on MS.MapItemID = MI.ID and MI.TargetIntersectID = TI.ID
								) HSR

		--if editor data is being passed
		if EXISTS (SELECT 1 FROM @rows)
		begin
			--remove deleting items
			delete I
			from @items I
			inner join @rows R on R.Deleting = 1  
				AND R.SourceSubjectID = I.SourceSubjectID 
				AND R.SourceObjectID = I.SourceObjectID
				AND R.TargetSubjectID = I.TargetSubjectID
				AND R.TargetObjectID = I.TargetObjectID;

			--insert adding items and fill in missing data
			insert into @items
			select
				R.ID,
				R.SourceIntersectID,
				SS.ObjectTypeName as SourceSubjectTypeName,
				coalesce(SS.TextPath, SS.Name) as SourceSubjectName,
				SS.Name as SourceSubjectShortName,
				R.SourceSubject,
				R.SourceSubjectID,
				SS.IconBackColor as SourceSubjectIconBackColor,
				SS.IconForeColor as SourceSubjectIconForeColor,
				SO.ObjectTypeName as SourceObjectTypeName,
				coalesce(SO.TextPath, SO.Name) as SourceObjectName,
				SO.Name as SourceObjectShortName,
				R.SourceObject,
				R.SourceObjectID,
				SO.IconBackColor as SourceObjectIconBackColor,
				SO.IconForeColor as SourceObjectIconForeColor,
				R.TargetIntersectID,
				TS.ObjectTypeName as TargetSubjectTypeName,
				coalesce(TS.TextPath, TS.Name) as TargetSubjectName,
				TS.Name as TargetSubjectShortName,
				R.TargetSubject,
				R.TargetSubjectID,
				TS.IconBackColor as TargetSubjectIconBackColor,
				TS.IconForeColor as TargetSubjectIconForeColor,
				TB.ObjectTypeName as TargetObjectTypeName,
				coalesce(TB.TextPath, TB.Name)  as TargetObjectName,
				TB.Name as TargetObjectShortName,
				R.TargetObject,
				R.TargetObjectID,
				TB.IconBackColor as TargetObjectIconBackColor,
				TB.IconForeColor as TargetObjectIconForeColor,
				0 as HasSourceRules
			from @rows R 
			inner join cache.ObjectDetails SS on SS.[Object] = R.SourceSubject AND SS.ObjectID = R.SourceSubjectID
			inner join cache.ObjectDetails SO on SO.[Object] = R.SourceObject AND SO.ObjectID = R.SourceObjectID
			inner join cache.ObjectDetails TS on TS.[Object] = R.TargetSubject AND TS.ObjectID = R.TargetSubjectID
			inner join cache.ObjectDetails TB on TB.[Object] = R.TargetObject AND TB.ObjectID = R.TargetObjectID
			where R.Adding = 1;
		end
		
		if @view = 0
		begin
			select (
					select distinct
					cast(SI.IntersectTypeID as varchar) + '.' 
					+ cast(I.SourceSubjectID as varchar) + '.'
					+ cast(I.SourceObjectID as varchar) as [sourcekey],
					cast(TI.IntersectTypeID as varchar) + '.' 
					+ cast(I.TargetSubjectID as varchar) + '.'
					+ cast(I.TargetObjectID as varchar) as [targetkey],
					I.*,
					SI.IntersectTypeID as SourceIntersectTypeID,
					SIT.[Name] as SourceIntersectTypeName,
					TI.IntersectTypeID as TargetIntersectTypeID,
					TIT.[Name] as TargetIntersectTypeName
				from @items I
				inner join [Intersect] SI on SI.ID = I.SourceIntersectID
				inner join IntersectType SIT on SIT.ID = SI.IntersectTypeID
				inner join [Intersect] TI on TI.ID = I.TargetIntersectID
				inner join IntersectType TIT on TIT.ID = TI.IntersectTypeID
				for json path
			) as 'items'
			for json path, WITHOUT_ARRAY_WRAPPER
		end

		if @view = 1
		begin

			insert into @links
					select	distinct
							S.SourceSubject + '.' + cast(S.SourceSubjectID as varchar) as [from],
							S.TargetSubject + '.' + cast(S.TargetSubjectID as varchar) as [to],
							'' as category
					from	@items S
			insert into @nodes
					select	distinct
							I.SourceSubject + '.' + cast(I.SourceSubjectID as varchar) as [key],
							I.SourceSubject as [obj],
							I.SourceSubjectID as [objid], 
							I.SourceSubject as [type],
							I.SourceSubjectTypeName as typeName,
							I.SourceSubjectName as name,
							I.SourceSubjectShortName as shortname,
							I.SourceSubjectIconBackColor as back,
							I.SourceSubjectIconForeColor as fore,
							case 
								when I.SourceSubject = @type and I.SourceSubjectID = @id then 'Focal'
								else 'Normal'
							end as template,
							null as other,
							0 as HasSourceRules--I.HasSourceRules
					from	@items I;

			--insert into @nodes
			merge	@nodes as T
			using	(
					select	distinct
							I.TargetSubject + '.' + cast(I.TargetSubjectID as varchar) as [key],
							I.TargetSubject as [obj],
							I.TargetSubjectID as [objid], 
							I.TargetSubject as [type],
							I.TargetSubjectTypeName as typeName,
							I.TargetSubjectName as name,
							I.TargetSubjectShortName as shortname,
							I.TargetSubjectIconBackColor as back,
							I.TargetSubjectIconForeColor as fore,
							case 
								when I.TargetSubject = @type and I.TargetSubjectID = @id then 'Focal'
								else 'Normal'
							end as template,
							null as other,
							I.HasSourceRules
					from	@items I
					where	I.TargetSubject + '.' + cast(I.TargetSubjectID as varchar) not in (select [key] from @nodes)
					) S
			on		(T.[key] = S.[key])
			when	matched then
			update	set
					T.HasSourceRules = S.HasSourceRules
			when	not matched then
			insert	([key], obj, [objid], [type], typeName, name, shortname, back, fore, template, other, HasSourceRules)
			values	(S.[key], S.obj, S.[objid], S.[type], S.typeName, S.name, S.shortname, S.back, S.fore, S.template, S.other, S.HasSourceRules);
					--where	I.TargetSubject + '.' + cast(I.TargetSubjectID as varchar) not in (select [key] from @nodes)

			if @usageOnly = 1 --Remove elements that are not tied to the current object via any Usage predicate type.
			begin
				delete	@nodes
				where	[key] not in 
					(
					--DIRECTLY related to an item via Usage relationship
					select	case 
								when (I.Subject = N.obj and I.SubjectID = N.objid and I.SubjectID <> @id and I.Object = @type and I.ObjectID = @id) then I.Subject + '.' + cast(I.SubjectID as varchar)
								else I.Object + '.' + cast(I.ObjectID as varchar)
							end as [key]
					from	[Intersect] I
							inner join @nodes N on	(
													(I.Subject = N.obj and I.SubjectID = N.objid and I.SubjectID <> @id and I.Object = @type and I.ObjectID = @id) OR
													(I.Object = N.obj and I.ObjectID = N.objid and I.ObjectID <> @id and I.Subject = @type and I.SubjectID = @id)
													)
							inner join IntersectType T on T.ID = I.IntersectTypeID and (I.Deleted = 0 or I.Deleted is null)
							inner join [Predicate] P on P.ID = T.PredicateID and P.[Type] = 10
					union
					--INDIRECTLY related to an item via Usage relationship
					select	N.obj + '.' + cast(N.objid as varchar) as [key]
					from	[Intersect] I1
							inner join [Intersect] I2 on I1.Subject = @type and I1.SubjectID = @id 
														and (
																--(I2.Subject = I1.Subject and I2.SubjectID = I1.SubjectID) --OR
																(I2.Object = I1.Object and I2.ObjectID = I1.ObjectID)
															)
														and I1.ID <> I2.ID
							inner join IntersectType T on T.ID = I2.IntersectTypeID 
														and (I1.Deleted = 0 or I1.Deleted is null)
														and (I2.Deleted = 0 or I2.Deleted is null) 
							inner join @nodes N on (I2.Subject = N.obj and I2.SubjectID = N.objid)
							inner join [Predicate] P on P.ID = T.PredicateID and P.[Type] = 10
					) and [key] <> @type + '.' + cast(@id as varchar)
			end

--select	* from	@links
--select	* from	@nodes

			select	(
					select	*
					from	@links O
					for json path			
					) as 'links',
					(
					select	I.*,
							A.actions
					from	@nodes I
							cross apply (
											select count(1) as actions   
											from Workflow W  
											left join issue S on S.ID = W.data.value('(/fields//IssueID/node())[1]', 'nvarchar(max)')          			                          
											where W.WorkflowType = 3 AND W.DateCompleted is null AND S.ObjectID = I.objid AND S.Object = I.obj  
										) A
					for json path			
					) as 'nodes'
			for json path, WITHOUT_ARRAY_WRAPPER
		end --view 1

		if @view = 2
		begin
			insert into @links
				select	distinct
						SourceSubject + '.' + cast(SourceSubjectID as varchar) as 'from',
						cast(SourceIntersectID as varchar) + '.S' as 'to',
						'Support' as category
				from	@items
				union
				select	distinct
						cast(SourceIntersectID as varchar) + '.S' as 'from',
						TargetSubject + '.' + cast(TargetSubjectID as varchar) as 'to',
						'' as category
				from	@items O
				where	(SourceObject + cast(SourceObjectID as varchar)) = (TargetObject + cast(TargetObjectID as varchar))
				union
				select	distinct
						cast(SourceIntersectID as varchar) + '.S' as 'from',
						cast(TargetIntersectID as varchar) + '.T' as 'to',
						'' as category
				from	@items O
				where	(SourceObject + cast(SourceObjectID as varchar)) <> (TargetObject + cast(TargetObjectID as varchar))
				--where	TargetIntersectID in (select SourceIntersectID from @items)
				union
				select	distinct
						cast(TargetIntersectID as varchar) + '.T' as 'from',
						TargetSubject + '.' + cast(TargetSubjectID as varchar) as 'to',
						'Support' as category
				from	@items
				where	(SourceObject + cast(SourceObjectID as varchar)) <> (TargetObject + cast(TargetObjectID as varchar))

			insert into @nodes
				select	distinct
						SourceSubject + '.' + cast(SourceSubjectID as varchar) as [key],
						SourceSubject as [obj],
						SourceSubjectID as [objid], 
						SourceSubject as [type],
						SourceSubjectTypeName as typeName,
						SourceSubjectName as name,
						SourceSubjectShortName as shortname,
						SourceSubjectIconBackColor as back,
						SourceSubjectIconForeColor as fore,
						case 
							when SourceSubject = @type and SourceSubjectID = @id then 'Focal'
							else 'Normal'
						end as template,
						null as other,
						0 as HasSourceRules
				from	@items 

			insert into @nodes
				select	distinct
						cast(SourceIntersectID as varchar) + '.S' as [key],
						SourceObject as [obj],
						SourceObjectID as [objid], 
						SourceObject as [type],
						SourceObjectTypeName as typeName,
						SourceObjectName as name,
						SourceObjectShortName as shortname,
						SourceObjectIconBackColor as back,
						SourceObjectIconForeColor as fore,
						case 
							when SourceObject = @type and SourceObjectID = @id then 'SupportFocal'
							else 'SupportNormal'
						end as template,
						null as other,
						0 as HasSourceRules
				from	@items

			merge	@nodes as T
			using	(
					select	distinct
							cast(TargetIntersectID as varchar) + '.T' as [key],
							TargetObject as [obj],
							TargetObjectID as [objid], 
							TargetObject as [type],
							TargetObjectTypeName as typeName,
							TargetObjectName as name,
							TargetObjectShortName as shortname,
							TargetObjectIconBackColor as back,
							TargetObjectIconForeColor as fore,
							case 
								when TargetObject = @type and TargetObjectID = @id then 'SupportFocal'
								else 'SupportNormal'
							end as template,
							null as other,
							HasSourceRules
					from	@items
					where	(SourceObject + cast(SourceObjectID as varchar)) <> (TargetObject + cast(TargetObjectID as varchar))
					) S
			on		(T.[key] = S.[key])
			when	matched then
			update	set
					T.HasSourceRules = S.HasSourceRules
			when	not matched then
			insert	([key], obj, [objid], [type], typeName, name, shortname, back, fore, template, other, HasSourceRules)
			values	(S.[key], S.obj, S.[objid], S.[type], S.typeName, S.name, S.shortname, S.back, S.fore, S.template, S.other, S.HasSourceRules);

			merge	@nodes as T
			using	(
					select	distinct
							TargetSubject + '.' + cast(TargetSubjectID as varchar) as [key],
							TargetSubject as [obj],
							TargetSubjectID as [objid], 
							TargetSubject as [type],
							TargetSubjectTypeName as typeName,
							TargetSubjectName as name,
							TargetSubjectShortName as shortname,
							TargetSubjectIconBackColor as back,
							TargetSubjectIconForeColor as fore,
							case 
								when TargetSubject = @type and TargetSubjectID = @id then 'Focal'
								else 'Normal'
							end as template,
							null as other,
							HasSourceRules
					from	@items
					where	TargetSubject + '.' + cast(TargetSubjectID as varchar) not in (select [key] from @nodes)
					) S
			on		(T.[key] = S.[key])
			when	matched then
			update	set
					T.HasSourceRules = S.HasSourceRules
			when	not matched then
			insert	([key], obj, [objid], [type], typeName, name, shortname, back, fore, template, other, HasSourceRules)
			values	(S.[key], S.obj, S.[objid], S.[type], S.typeName, S.name, S.shortname, S.back, S.fore, S.template, S.other, S.HasSourceRules);

--select	* from	@links
--select	* from	@nodes

			if @usageOnly = 1 --Remove elements that are not tied to the current object via any Usage predicate type.
			begin
				declare @usages table ([key] varchar(250))

				insert into @usages
					--DIRECTLY related to an item via Usage relationship
					select	--*,
							case 
								when (I.Subject = N.obj and I.SubjectID = N.objid and I.SubjectID <> @id and I.Object = @type and I.ObjectID = @id) then I.Subject + '.' + cast(I.SubjectID as varchar)
								else I.Object + '.' + cast(I.ObjectID as varchar)
							end as [key]
					from	[Intersect] I
							inner join @nodes N on	(
													(I.Subject = N.obj and I.SubjectID = N.objid and I.SubjectID <> @id and I.Object = @type and I.ObjectID = @id) OR
													(I.Object = N.obj and I.ObjectID = N.objid and I.ObjectID <> @id and I.Subject = @type and I.SubjectID = @id)
													)
							inner join IntersectType T on T.ID = I.IntersectTypeID and (I.Deleted = 0 or I.Deleted is null)
							inner join [Predicate] P on P.ID = T.PredicateID and P.[Type] = 10
					union
					--INDIRECTLY related to an item via Usage relationship
					select	N.obj + '.' + cast(N.objid as varchar) as [key]
					from	[Intersect] I1
							inner join [Intersect] I2 on I1.Subject = @type and I1.SubjectID = @id 
														and (
																--(I2.Subject = I1.Subject and I2.SubjectID = I1.SubjectID) --OR
																(I2.Object = I1.Object and I2.ObjectID = I1.ObjectID)
															)
														and I1.ID <> I2.ID
							inner join IntersectType T on T.ID = I2.IntersectTypeID 
														and (I1.Deleted = 0 or I1.Deleted is null)
														and (I2.Deleted = 0 or I2.Deleted is null) 
							inner join @nodes N on (I2.Subject = N.obj and I2.SubjectID = N.objid)
							inner join [Predicate] P on P.ID = T.PredicateID and P.[Type] = 10

				delete	@nodes
				where	[key] not in 
					(
					select	[key]
					from	@usages
					) 
					and [key] <> @type + '.' + cast(@id as varchar)
					and [template] not like '%Support%'

				delete	@links
				where	[from] not in (select [key] from @nodes)
						or [to] not in (select [key] from @nodes)
				
				delete	@nodes
				where	[template] like '%Support%'
						and [key] not in (
							select	[key]
							from	@nodes 
							where	[template] like '%Support%'
									and [key] in (select [from] from @links)
									and [key] in (select [to] from @links)
						)
			end

--select	* from	@items
--select	* from	@links
--select	* from	@nodes

			select	(
					select	*
					from	@links O
					for json path			
					) as 'links',
					(
					select	I.*,
							A.actions
					from	@nodes I
							cross apply (
											select count(1) as actions   
											from Workflow W  
											left join issue S on S.ID = W.data.value('(/fields//IssueID/node())[1]', 'nvarchar(max)')          			                          
											where W.WorkflowType = 3 AND W.DateCompleted is null AND S.ObjectID = I.objid AND S.Object = I.obj  
										) A
					for json path			
					) as 'nodes'
			for json path, WITHOUT_ARRAY_WRAPPER
		end --view 2
	end

	if @view in (3,4)
	begin
	
		declare @tFusionPoints table (	ID int, MapItemID int, SourceFusionAttributeID int, TargetFusionAttributeID int);
		declare @tItems table (
			MapItemID int, --MapID int,

			SourceIntersectID int, 
			SourceSubjectTypeName nvarchar(500), SourceSubjectName nvarchar(500), SourceSubjectShortName nvarchar(500), SourceSubject varchar(50), SourceSubjectID int,
			SourceObjectTypeName nvarchar(500), SourceObjectName nvarchar(500), SourceObjectShortName nvarchar(500), SourceObject varchar(50), SourceObjectID int, 
			
			TargetIntersectID int, 
			TargetSubjectTypeName nvarchar(500), TargetSubjectName nvarchar(500), TargetSubjectShortName nvarchar(500), TargetSubject varchar(50), TargetSubjectID int, 
			TargetObjectTypeName nvarchar(500), TargetObjectName nvarchar(500), TargetObjectShortName nvarchar(500), TargetObject varchar(50), TargetObjectID int
		)
	
		if @type = 'FusionAttribute'
			begin
				insert into @tFusionPoints
					select	I.ID,
							NULL,
							I.SourceFusionAttributeID,
							I.TargetFusionAttributeID
					from	MapRuleItem I
							inner join FusionAttribute SFA on SFA.ID = I.SourceFusionAttributeID and SFA.Deleted = 0
							inner join FusionAttribute TFA on TFA.ID = I.TargetFusionAttributeID and TFA.Deleted = 0
					where	I.SourceFusionAttributeID = @id or I.TargetFusionAttributeID = @id;

				--insert adding items if editor data is present
				if EXISTS (SELECT 1 FROM @technicalRows)
				begin
					insert into @tFusionPoints
					select
						ID,
						MapItemID,
						SourceFusionAttributeID,
						TargetFusionAttributeID
					from @technicalRows
					where Adding = 1;
				end;

				-- forward items
				with cte as (
					select	ID,
							SourceFusionAttributeID,
							TargetFusionAttributeID,
							1 as [Level]
					from	@tFusionPoints S
					where not exists (select 1 from @technicalRows R where Deleting = 1 and R.ID = S.ID)
					union all
					select	S.ID,
							S.SourceFusionAttributeID,
							S.TargetFusionAttributeID,
							T.[Level] + 1 as [Level]
					from	MapRuleItem S
							inner join cte T on T.TargetFusionAttributeID = S.SourceFusionAttributeID and S.ID <> T.ID
					where	T.[Level] <= 13 and not exists (select 1 from @technicalRows R where Deleting = 1 and R.ID = S.ID)
				)
				insert into @tFusionPoints
					select distinct	ID, 
							NULL, 
							SourceFusionAttributeID, 
							TargetFusionAttributeID
					from	cte 
					where	ID not in (select ID from @tFusionPoints);

				-- backward items
				with cte as (
					select		I.ID,                                           
                                I.SourceFusionAttributeID,
                                I.TargetFusionAttributeID,
                                1 as [Level]
                    from   MapRuleItem I
                                inner join FusionAttribute SFA on SFA.ID = I.SourceFusionAttributeID and SFA.Deleted = 0
                                inner join FusionAttribute TFA on TFA.ID = I.TargetFusionAttributeID and TFA.Deleted = 0
                    where  I.SourceFusionAttributeID = @id or I.TargetFusionAttributeID = @id 
						   and not exists (select 1 from @technicalRows R where Deleting = 1 and R.ID = I.ID)
					union all
					select	S.ID,
							S.SourceFusionAttributeID,
							S.TargetFusionAttributeID,
							T.[Level] + 1 as [Level]
					from	MapRuleItem S
							inner join cte T on T.SourceFusionAttributeID = S.TargetFusionAttributeID and S.ID <> T.ID
					where	T.[Level] <= 13 and not exists (select 1 from @technicalRows R where Deleting = 1 and R.ID = S.ID)
				)
				insert into @tFusionPoints
					select distinct	ID, 
							NULL, 
							SourceFusionAttributeID, 
							TargetFusionAttributeID
					from	cte 
					where	ID not in (select ID from @tFusionPoints);

				

				--remove deleting items if editor data is present
				if EXISTS (SELECT 1 FROM @technicalRows)
				begin
					
					delete I
					from @tFusionPoints I
					inner join @technicalRows R on R.Deleting = 1 AND R.ID = I.ID;
				end

				-- get all items directly tied to the focal object.
				insert into @tItems
					select	MI.ID,
					
							MI.SourceIntersectID,
							SI.SubjectTypeName,
							SI.SubjectName,
							SI.SubjectShortName,
							SI.Subject,
							SI.SubjectID,
							SI.ObjectTypeName,
							SI.ObjectName,
							SI.ObjectShortName,
							SI.Object,
							SI.ObjectID,

							MI.TargetIntersectID,
							TI.SubjectTypeName,
							TI.SubjectName,
							TI.SubjectShortName,
							TI.Subject,
							TI.SubjectID,
							TI.ObjectTypeName,
							TI.ObjectName,
							TI.ObjectShortName,
							TI.Object,
							TI.ObjectID

					from	@tFusionPoints F
							inner join MapRuleItemMapItem J on J.MapRuleItemID = F.ID
							inner join MapItem MI on MI.ID = J.MapItemID
							inner join IntersectDetail SI on SI.ID = MI.SourceIntersectID
							inner join IntersectDetail TI ON TI.ID = MI.TargetIntersectID
 

				-- get all items not directly tied to the focal object, but still tied to maps involved above.
				insert into @tItems
					select	MI.ID,
							--NULL,
					
							MI.SourceIntersectID,
							SI.SubjectTypeName,
							SI.SubjectName,
							SI.SubjectShortName,
							SI.Subject,
							SI.SubjectID,
							SI.ObjectTypeName,
							SI.ObjectName,
							SI.ObjectShortName,
							SI.Object,
							SI.ObjectID,

							MI.TargetIntersectID,
							TI.SubjectTypeName,
							TI.SubjectName,
							TI.SubjectShortName,
							TI.Subject,
							TI.SubjectID,
							TI.ObjectTypeName,
							TI.ObjectName,
							TI.ObjectShortName,
							TI.Object,
							TI.ObjectID

					from	MapItem MI
							inner join	(
										select	ID.MapItemID
										from	MapItemMap DM
												inner join @tItems D on D.MapItemID = DM.MapItemID
												inner join MapItemMap ID on ID.MapID = DM.MapID and ID.MapItemID not in (
																														select MapItemID from @tItems
																														)
										) O on O.MapItemID = MI.ID
							inner join [IntersectDetail] SI on SI.ID = MI.SourceIntersectID
							inner join [IntersectDetail] TI on TI.ID = MI.TargetIntersectID
			end
		else
			begin
				declare @tBusinessPoints table ( ID int, SourceIntersectID int, TargetIntersectID int )

				insert into @objects values (@type, @id)

				if not exists(
					select	MI.ID
					from	MapItem MI
							inner join [Intersect] SI on SI.ID = MI.SourceIntersectID --IntersectDetail
							inner join [Intersect] TI ON TI.ID = MI.TargetIntersectID --IntersectDetail
					where 	( (SI.Subject = @type and SI.SubjectID = @id) OR (SI.Object = @type and SI.ObjectID = @id)  )
							OR ( (TI.Subject = @type and TI.SubjectID = @id) OR (TI.Object = @type and TI.ObjectID = @id)  )
				)
				begin
					insert into @objects
						select	case 
									when I.Subject = @type and I.SubjectID = @id then I.Object
									else I.Subject
								end,
								case 
									when I.Subject = @type and I.SubjectID = @id then I.ObjectID 
									else I.SubjectID 
								end
						from	[Intersect] I
								inner join IntersectType T on T.ID = I.IntersectTypeID 
								inner join [Predicate] P on P.ID = T.PredicateID and P.Type = 6
						where	(I.Subject = @type and I.SubjectID = @id) or (I.Object = @type and I.ObjectID = @id)
				end

				-- get all items directly tied to the focal object.
				insert into @tBusinessPoints
					select	MI.ID, MI.SourceIntersectID, MI.TargetIntersectID
					from	MapItem MI
							inner join [Intersect] SI on SI.ID = MI.SourceIntersectID
							inner join [Intersect] TI ON TI.ID = MI.TargetIntersectID
							inner join @objects O on	( (SI.Subject = O.Type and SI.SubjectID = O.ID) OR (SI.Object = O.Type and SI.ObjectID = O.ID)  ) OR 
														( (TI.Subject = O.Type and TI.SubjectID = O.ID) OR (TI.Object = O.Type and TI.ObjectID = O.ID)  )

				-- get all items not directly tied to the focal object, but still tied to maps involved above.
				insert into @tBusinessPoints
					select	MI.ID, MI.SourceIntersectID, MI.TargetIntersectID
					from	MapItem MI
							inner join	(
										select	ID.MapItemID
										from	MapItemMap DM
												inner join @tBusinessPoints D on D.ID = DM.MapItemID
												inner join MapItemMap ID on ID.MapID = DM.MapID and ID.MapItemID not in (
																														select ID from @tBusinessPoints
																														)
										) O on O.MapItemID = MI.ID;

				with cte as (
					select	ID,
							SourceIntersectID,
							TargetIntersectID,
							1 as [Level]
					from	@tBusinessPoints
					union all
					select	S.ID,
							S.SourceIntersectID,
							S.TargetIntersectID,
							T.[Level] + 1 as [Level]
					from	MapItem S
							inner join cte T on T.SourceIntersectID = S.TargetIntersectID and S.ID <> T.ID
					where	T.[Level] <= 25
				)
				insert into @tBusinessPoints
					select ID, SourceIntersectID, TargetIntersectID from cte where ID not in (select ID from @tBusinessPoints)

				insert into @tItems
					select	O.ID,
							--NULL,
					
							O.SourceIntersectID,
							SI.SubjectTypeName,
							SI.SubjectName,
							SI.SubjectShortName,
							SI.Subject,
							SI.SubjectID,
							SI.ObjectTypeName,
							SI.ObjectName,
							SI.ObjectShortName,
							SI.Object,
							SI.ObjectID,

							O.TargetIntersectID,
							TI.SubjectTypeName,
							TI.SubjectName,
							TI.SubjectShortName,
							TI.Subject,
							TI.SubjectID,
							TI.ObjectTypeName,
							TI.ObjectName,
							TI.ObjectShortName,
							TI.Object,
							TI.ObjectID

					from	@tBusinessPoints O
							inner join IntersectDetail SI on SI.ID = O.SourceIntersectID
							inner join IntersectDetail TI ON TI.ID = O.TargetIntersectID

							--select * from @tItems;

				insert into @tFusionPoints
					select	J.MapRuleItemID,
							J.MapItemID,
							T.SourceFusionAttributeID,
							T.TargetFusionAttributeID
					from	@tItems I
							inner join MapRuleItemMapItem J on J.MapItemID = I.MapItemID
							inner join MapRuleItem T on T.ID = J.MapRuleItemID
							inner join FusionAttribute SFA on SFA.ID = T.SourceFusionAttributeID and SFA.Deleted = 0
							inner join FusionAttribute TFA on TFA.ID = T.TargetFusionAttributeID and TFA.Deleted = 0;

				
				--insert adding items if editor data is passed
				if EXISTS (SELECT 1 FROM @technicalRows)
				begin
					insert into @tFusionPoints
					select
						ID,
						MapItemID,
						SourceFusionAttributeID,
						TargetFusionAttributeID
					from @technicalRows
					where Adding = 1;
				end;


				
				with cteFusionForward as (
					select	ID, 
							MapItemID, 
							SourceFusionAttributeID, 
							TargetFusionAttributeID,
							1 as [Level]
					from	@tFusionPoints S
					where not exists (select 1 from @technicalRows R where R.Deleting = 1 and R.ID = S.ID)
					union all
					select	S.ID,
							T.MapItemID,
							S.SourceFusionAttributeID,
							S.TargetFusionAttributeID,
							T.[Level] + 1 as [Level]
					from	MapRuleItem S
							inner join cteFusionForward T on T.SourceFusionAttributeID = S.TargetFusionAttributeID and S.ID <> T.ID
							inner join FusionAttribute SFA on SFA.ID = S.SourceFusionAttributeID and SFA.Deleted = 0
							inner join FusionAttribute TFA on TFA.ID = S.TargetFusionAttributeID and TFA.Deleted = 0
					where	T.[Level] <= 25 and not exists (select 1 from @technicalRows R where R.Deleting = 1 AND R.ID = S.ID)
				)

				insert into @tFusionPoints
					select distinct	ID,
							NULL,
							SourceFusionAttributeID,
							TargetFusionAttributeID
					from	cteFusionForward
					where	ID not in (select ID from @tFusionPoints)
					OPTION (MAXRECURSION 200) ;

				with cteFusionBackward as (
					select	ID, 
							MapItemID, 
							SourceFusionAttributeID, 
							TargetFusionAttributeID,
							1 as [Level]
					from	@tFusionPoints S
					where not exists (select 1 from @technicalRows R where R.Deleting = 1 AND R.ID = S.ID)
					union all
					select	S.ID,
							T.MapItemID,
							S.SourceFusionAttributeID,
							S.TargetFusionAttributeID,
							T.[Level] + 1 as [Level]
					from	MapRuleItem S
							inner join cteFusionBackward T on T.TargetFusionAttributeID = S.SourceFusionAttributeID and S.ID <> T.ID
							inner join FusionAttribute SFA on SFA.ID = S.SourceFusionAttributeID and SFA.Deleted = 0
							inner join FusionAttribute TFA on TFA.ID = S.TargetFusionAttributeID and TFA.Deleted = 0
					where	T.[Level] <= 25 and not exists (select 1 from @technicalRows R where R.Deleting = 1 AND R.ID = S.ID)
				)

				insert into @tFusionPoints
					select distinct	ID,
							NULL,
							SourceFusionAttributeID,
							TargetFusionAttributeID
					from	cteFusionBackward
					where	ID not in (select ID from @tFusionPoints)
					OPTION (MAXRECURSION 200) ;

				--remove deleting items if editor data is present
				if EXISTS (SELECT 1 FROM @technicalRows)
				begin
					delete I
					from @tFusionPoints I
					inner join @technicalRows R on R.Deleting = 1 AND R.ID = I.ID;
				end;
			end

		if @view = 3
		begin
		--Load tables we will return to caller.
		insert into @links
			select	distinct
					cast(S.SourceFusionAttributeID as varchar),-- + '.' + coalesce(B.SourceSubject, '0') + '.' + coalesce(cast(B.SourceSubjectID as varchar), '0') as [from],
					cast(S.TargetFusionAttributeID as varchar),-- + '.' + coalesce(B.TargetSubject, '0') + '.' + coalesce(cast(B.TargetSubjectID as varchar), '0') as [to],
					'' as category
			from	@tFusionPoints S
					left join MapRuleItemMapItem J on J.MapRuleItemID = S.ID
					left join @tItems B on B.MapItemID = J.MapItemID
		insert into @nodes
			select	distinct
					cast(S.SourceFusionAttributeID as varchar),-- + '.' + coalesce(B.SourceSubject, '0') + '.' + coalesce(cast(B.SourceSubjectID as varchar), '0') as [key],
					'FusionAttribute' as [obj],
					SourceFusionAttributeID as [objid], 
					'FusionAttribute' as [type],
					T.Name as typeName,
					A.TextPath as name,
					A.Name as shortname,
					COALESCE(ST.IconBackColor, '#000') as back,
					COALESCE(ST.IconForeColor, '#fff') as fore,
					'Fusion' as template,
					B.SourceSubjectTypeName + ' : ' + B.SourceSubjectName as other,
					null
			from	@tFusionPoints S
					inner join FusionAttribute A on A.ID = S.SourceFusionAttributeID
					inner join FusionAttributeType T on T.ID = A.FusionAttributeTypeID
					left join ObjectStyle ST on ST.ObjectType = 'FusionAttributeType' and ST.ObjectID = T.ID
					left join MapRuleItemMapItem J on J.MapRuleItemID = S.ID
					left join @tItems B on B.MapItemID = J.MapItemID
		insert into @nodes
			select	distinct
					cast(S.TargetFusionAttributeID as varchar),-- + '.' + coalesce(B.TargetSubject, '0') + '.' + coalesce(cast(B.TargetSubjectID as varchar), '0') as [key],
					'FusionAttribute' as [obj],
					TargetFusionAttributeID as [objid], 
					'FusionAttribute' as [type],
					T.Name as typeName,
					A.TextPath as name,
					A.Name as shortname,
					COALESCE(ST.IconBackColor, '#000') as back,
					COALESCE(ST.IconForeColor, '#fff') as fore,
					'Fusion' as template,
					B.TargetSubjectTypeName + ' : ' + B.TargetSubjectName as other,
					null
			from	@tFusionPoints S
					inner join FusionAttribute A on A.ID = S.TargetFusionAttributeID
					inner join FusionAttributeType T on T.ID = A.FusionAttributeTypeID
					left join ObjectStyle ST on ST.ObjectType = 'FusionAttributeType' and ST.ObjectID = T.ID
					left join MapRuleItemMapItem J on J.MapRuleItemID = S.ID
					left join @tItems B on B.MapItemID = J.MapItemID
			where	cast(S.TargetFusionAttributeID as varchar) + '.' + coalesce(B.TargetSubject, '0') + '.' + coalesce(cast(B.TargetSubjectID as varchar), '0') not in (select [key] from @nodes)

			--gets rid of dupes
			delete	@nodes 
			where	other is null 
					and (obj + cast([objid] as varchar)) in (
															select	(obj + cast([objid] as varchar))
															from	@nodes 
															where	other is not null
															)
			delete	T
			from	@links T
					left join @nodes S on S.[key] = T.[from] or S.[key] = T.[to]
			where	S.[key] is null
			
			select	(
					select	*
					from	@links O
					for json path			
					) as 'links',
					(
					select	distinct
							*
					from	@nodes
					for json path			
					) as 'nodes'
			for json path, WITHOUT_ARRAY_WRAPPER
		end --view 3

		if @view = 4
		begin
			select (
				select distinct
					F.ID,
					I.MapItemID,
					F.SourceFusionAttributeID,
					FS.TextPath as SourceFusionAttributeName,
					F.TargetFusionAttributeID,
					FT.TextPath as TargetFusionAttributeName 
				from @tFusionPoints F
				left join @tItems I on I.MapItemID = F.MapItemID
				inner join FusionAttribute FS on FS.ID = F.SourceFusionAttributeID
				inner join FusionAttribute FT on FT.ID = F.TargetFusionAttributeID
				for json path
				) as 'items'
			for json path, WITHOUT_ARRAY_WRAPPER
		end --view 4
	end
end
GO

ALTER PROCEDURE [dbo].[GetReferenceItemValues]	
	@listid int	
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;
	
	-- load the fields for this item
	select id, 'Field' + cast(id as varchar(100)) as [Name] into #fieldtypes from fieldtype where object = 'ReferenceItemType' and objectid = @listid order by sortorder
	
	DECLARE @tsqlSelect nvarchar(max);
	DECLARE @tsqlFrom nvarchar(max);
	DECLARE @tsqlWhere nvarchar(max);
	DECLARE @tsql nvarchar(max);

	set @tsqlSelect = 'select ri.id as [ID] ,ri.code as [Code]';
	set @tsqlFrom = ' from [dbo].[referenceitem] ri';
	set @tsqlWhere = ' where ri.visible = 1 and ri.referenceitemtypeid = ' + cast(@listid as nvarchar(20));
	

	DECLARE @id int;
	DECLARE @index int = 0;
	DECLARE @name nvarchar(250);

	-- generate dynamic sql for each field
	DECLARE cur CURSOR FOR SELECT id, name FROM #fieldtypes
	OPEN cur

	FETCH NEXT FROM cur INTO @id, @name

	WHILE @@FETCH_STATUS = 0 BEGIN
		
		SET @tsqlSelect = @tsqlSelect + ',f'+ cast(@index as nvarchar(10)) + '.formattedvalue as [' + @name + ']';
		SET @tsqlFrom = @tsqlFrom + ' left outer join [dbo].[field] f' + cast(@index as nvarchar(10)) + ' on (ri.id = f' + cast(@index as nvarchar(10)) + '.objectid and f' + cast(@index as nvarchar(10)) + '.[objecttype] = ''ReferenceItem'' and f' + cast(@index as nvarchar(10)) + '.fieldtypeid = ' + cast(@id as nvarchar(20)) + ')';

		SET @index = @index + 1;
		FETCH NEXT FROM cur INTO @id, @name
	END

	CLOSE cur    
	DEALLOCATE cur

	SET @tsql = @tsqlSelect + @tsqlFrom + @tsqlWhere;
	--print @tsql
	EXEC sp_executesql @tsql;

END
GO

ALTER PROCEDURE [dbo].[GetRenderedTemplateBodyNg]-- 'Tooltip', 'Resource', 2, 'Preview'
--declare
	@TemplateType varchar(25),
	@Type varchar(50),
	@ID int,
	@Action varchar(50),
	@SubjectName VARCHAR (200) = 'Governing Domain'
--set @TemplateType = 'Lookup'
--set @Type = 'Artifact'
--set @ID = 7004--16435
--set @Action = 'Preview'--'Certificate'
AS
BEGIN
	SET NOCOUNT ON;

	declare @html nvarchar(max),
			@link nvarchar(2500),
			@icon nvarchar(250),
			@hasDynamicFields bit = 0,
			@hasStats bit = 0,
			@typeID int,

			@showIcon bit = 1,

			@current int,
			@max int,
			@name nvarchar(250),
			@value nvarchar(max);

	declare @tbl table (ID int identity, Name nvarchar(250), Value nvarchar(max));

	if @TemplateType = 'Email'
	begin
		select	@html = TemplateBody
		from	EmailTemplate
		where	Name = @Type
				and [Action] = @Action
	end

	if @TemplateType = 'Tooltip'
	begin
		select	@html = TemplateBody
		from	TooltipTemplate
		where	Name = @Type
				and [Action] = @Action
	end

	-- Get the static tokens, depending on the type.
	declare @n nvarchar(250), @t nvarchar(250), @s nvarchar(25), @v int, @dc datetime, @du datetime, @d nvarchar(4000);
		
	-- Get common fields
	select	@typeID = ObjectTypeID,
			@icon = '<div title=''' + ObjectTypeName + ''' class=''tooltip-icon'' style=''background-color: ' + IconBackColor + '; color: ' + IconForeColor + '''><i class=''fa fa-' + IconText + '''></i></div>',
			@n = Name,
			@t = ObjectTypeName,
			@d = Description,
			@link = NgUrl
	from	cache.ObjectDetails
	where	[Object] = @Type
			and ObjectID = @ID;

	--fusion attributes arent in cache
	if @Type = 'FusionAttribute'
	begin		
		select 
			@typeID = fa.fusionattributetypeid,
			@n = fa.name,
			@t = fat.Name,
			@link = dbo.GenerateNgObjectUrl('FusionAttribute', fat.id, fa.id) 
		from fusionattribute fa 
			inner join fusionattributetype fat on (fa.fusionattributetypeid = fat.id) 
		where fa.id = @ID
	end

	if @n is not null
	begin
		if @link is null
		begin
			insert into @tbl values ('Name', @n)
		end
		else
		begin
			insert into @tbl values ('Name', '<a routerLink="/' + @link + '">' + @n + '</a>')
		end
		insert into @tbl values ('Description', @d)
	end
	insert into @tbl values ('Type', @t)

	if @Action = 'AssigningItemPreview'
	begin
		set @html = '<h3>{Name}</h3>'
	end

	if @Action = 'Certificate'
	begin
		set @html = '<h3>{Name}</h3>'

		declare @workflowID bigint,
				@dateCertifiedOn varchar(10),
				@certifiers nvarchar(2500),
				@status varchar(50),
				@certIconColor varchar(10)

		select	@dateCertifiedOn = CONVERT(VARCHAR(10), DateLastCertified, 101),
				@status = Status
		from	Artifact A
		where	A.ID = @ID

		/*SELECT	@workflowID = W.ID,
				@certifiers = COALESCE(@certifiers + ', ', '') + R.FirstName + ' ' + R.LastName 
		from	(
				select		top 1
							ID,
							Data.value('(/fields/ArtifactID)[1]', 'int') as ArtifactID,
							DateCompleted
				from		Workflow
				where		WorkflowType = 2
							and Data.exist('/fields/ArtifactID[text() = sql:variable("@ID")]') = 1
				order by	DateCompleted desc
				) W
				inner join WorkflowResource WR on WR.WorkflowID = W.ID
				inner join reporting.Global_Resource R on R.ResourceID = WR.ResourceID*/
		select 
			@workflowID = wi.id
		from 
			workflow.eventregistration we
			inner join workflow.type wt on we.typeid = wt.id
			inner join workflow.version wv on wt.id = wv.typeid
			inner join workflow.item wi on wi.versionid = wv.id and (wi.[object] = 'Artifact' and wi.objectid = @ID )
		where
			we.changetype = 8;

		if @dateCertifiedOn is null and @status != 'Certified'
			begin
				set @showIcon = 0

				set @html = @html + '<div><b>Not yet certified</b></div>'
				if @certifiers is not null
				begin
					set @html = @html + '<div>Certifying Users: {Certifiers}</div>'
				end
				if @workflowID is not null
				begin
					set @html = @html + '<div><a class=''btn btn-info'' routerLink=''/workflow/details/' + cast(@workflowID as varchar(50)) + '''>Go to this workflow status</a>.</div>'
				end
			end
		else
			begin
				if @status = 'Certified'
					begin
						set @certIconColor = '#EFC43D'
					end
				else 
					begin
						set @certIconColor = '#FFE183'
					end
				select	@icon = '<div style="background-color: transparent; color: ' + @certIconColor + '"><i class="fa fa-2x fa-certificate"></i></div>'
				set @html = @html + '<div>Last Certified On: ';
				if @dateCertifiedOn is null
					begin
						set @html = @html + 'Manually Certified';
					end
				else
					begin
						set @html = @html + '{CertifiedOn}';
					end
				set @html = @html + '</div>';
				if @status = 'Certified'
					begin
						if @Certifiers is not null
						begin
							set @html = @html + '<div>Certified By: {Certifiers}</div>'
						end
					end
				else 
					begin
						set @html = @html + '<div>Currently Under Certification Review</div>'
						--set @html = @html + '<div>Certifying Users: {Certifiers}</div>'
						if @workflowID is not null
						begin
							set @html = @html + '<div><a class=''btn btn-info'' routerLink=''/workflow/details/' + cast(@workflowID as varchar(50)) + '''>Go to this workflow status</a>.</div>'
						end
					end
			end

		insert into @tbl values ('CertifiedOn', @dateCertifiedOn)
		insert into @tbl values ('Certifiers', @certifiers)
	end
	if @Action = 'JoinRequest'
	begin
		set @html = ''
	end
	if @Action = 'LookupPreview'
	begin
		set @html = '{Items}'
		
		if @Type = 'FusionAttribute'
		begin
			-- BUILD LIST HTML -----------------------------------------
			declare @fusionAttributeItemsHtml nvarchar(max)

			set @fusionAttributeItemsHtml = '<div style="height: 200px; overflow-y: scroll"><table class="hoverable bordered striped" style="width:100%"><thead>'
			set @fusionAttributeItemsHtml = @fusionAttributeItemsHtml + '<th style="margin-right: 15px">Name</th>'
			set @fusionAttributeItemsHtml = @fusionAttributeItemsHtml + '</thead><tbody>'

			select		--top 10 
						@fusionAttributeItemsHtml = @fusionAttributeItemsHtml + '<tr>' 
											+ '<td>' + Name + '</td>'
											+ '</tr>'
			from		FusionAttribute
			where		ParentID = @ID
			order by	Name asc

			set @fusionAttributeItemsHtml = @fusionAttributeItemsHtml + '</tbody>'
			set @fusionAttributeItemsHtml = @fusionAttributeItemsHtml + '</table></div>'
 
			insert into @tbl values ('Items', @fusionAttributeItemsHtml)
			------------------------------------------------------------------
		end;

		if @Type = 'LookupType' OR @Type = 'Lookup'
		begin
			-- BUILD LOOKUP LIST HTML -----------------------------------------
			declare @lookups table (RowID int identity, ID int)

			declare @MyLookupTypeID int
			if @Type = 'Lookup'
				begin
					select @MyLookupTypeID = LookupTypeID from [Lookup] where ID = @ID 
				end
			else
				begin
					set @MyLookupTypeID = @ID
				end

			insert into @lookups 
				select top 10 ID from [Lookup] where LookupTypeID = @MyLookupTypeID order by ID desc
		
			declare @lookupFieldTypes table (ID int identity, Name nvarchar(250))
			insert into @lookupFieldTypes
				select FriendlyName from FieldType where [Object] = 'LookupType' and ObjectID = @MyLookupTypeID order by SortOrder asc

			declare @lookupHtml nvarchar(max)

			set @lookupHtml = '<table class="hoverable bordered striped" style="width:100%">'

			-- Loop through field name list ---------
			set @lookupHtml = @lookupHtml + '<thead>'
			set		@current = 1
			select	@max = max(ID) from @lookupFieldTypes
			while @current <= @max
			begin
				select	@name = Name
				from	@lookupFieldTypes
				where	ID = @current

				set @lookupHtml = @lookupHtml + '<th style="margin-right: 15px">' + @name  + '</th>'

				set @current = @current + 1
			end
			set @lookupHtml = @lookupHtml + '</thead>'
			-----------------------------------------

			set @lookupHtml = @lookupHtml + '<tbody>'

			-- Loop through event list --------------
			select	@current = min(RowID) from @lookups
			select	@max = max(RowID) from @lookups

			while @current <= @max
			begin
				set @lookupHtml = @lookupHtml + '<tr>'	-- Open row for selected event.

				declare @lookupFields table (Name nvarchar(250), Value nvarchar(4000))
			
				declare @lookupID int

				select	@lookupID = ID from @lookups where RowID = @current

				insert into @lookupFields
					select		FriendlyName,
								FormattedValue
					from		FieldWithRelation
					where		ObjectType = 'Lookup' 
								and ObjectID = @lookupID

					-- Loop through each field for this selected event --
					declare @lfCurrent int,
							@lfMax int
					set		@lfCurrent = 1
					select	@lfMax = max(ID) from @lookupFieldTypes
					while @lfCurrent <= @lfMax
					begin
						select	@name = Name from @lookupFieldTypes where ID = @lfCurrent

						select @lookupHtml = @lookupHtml + '<td>' + coalesce(Value, '') + '</td>' from @lookupFields where Name = @name

						set @lfCurrent = @lfCurrent + 1
					end
					-----------------------------------------------------

				delete @lookupFields

				set @lookupHtml = @lookupHtml + '</tr>'	-- Close off row for selected lookup.

				set @current = @current + 1
			end
			-----------------------------------------

			set @lookupHtml = @lookupHtml + '</tbody>'

			set @lookupHtml = @lookupHtml + '</table>'

			insert into @tbl values ('Items', @lookupHtml)
			------------------------------------------------------------------
		end;

		if @Type = 'ReferenceItemType' OR @Type = 'ReferenceItem'
		begin
			-- BUILD LOOKUP LIST HTML -----------------------------------------
			declare @refs table (RowID int identity, ID int)

			declare @MyRefTypeID int
			if @Type = 'ReferenceItem'
				begin
					select @MyRefTypeID = ReferenceItemTypeID from ReferenceItem where ID = @ID 
				end
			else
				begin
					set @MyRefTypeID = @ID
				end

			insert into @refs 
				select top 50 ID from [ReferenceItem] where ReferenceItemTypeID = @MyRefTypeID order by DisplayValue desc
		
			declare @refFieldTypes table (ID int identity, Name nvarchar(250))
			insert into @refFieldTypes values ('Code')
			insert into @refFieldTypes
				select FriendlyName from FieldType where [Object] = 'ReferenceItemType' and ObjectID = @MyRefTypeID order by SortOrder asc

			declare @refHtml nvarchar(max)

			set @refHtml = '<table class="hoverable bordered striped" style="width:100%; min-width: 400px">'

			-- Loop through field name list ---------
			set @refHtml = @refHtml + '<thead>'
			set		@current = 1
			select	@max = max(ID) from @refFieldTypes
			while @current <= @max
			begin
				select	@name = Name
				from	@refFieldTypes
				where	ID = @current

				set @refHtml = @refHtml + '<th style="margin-right: 15px">' + @name  + '</th>'

				set @current = @current + 1
			end
			set @refHtml = @refHtml + '</thead>'
			-----------------------------------------

			set @refHtml = @refHtml + '<tbody>'

			-- Loop through event list --------------
			select	@current = min(RowID) from @refs
			select	@max = max(RowID) from @refs

			while @current <= @max
			begin
				set @refHtml = @refHtml + '<tr>'	-- Open row for selected event.

				declare @refFields table (Name nvarchar(250), Value nvarchar(4000))
			
				declare @refID int

				select	@refID = ID from @refs where RowID = @current

				insert into @refFields
					select	'Code', Code from ReferenceItem where ID = @refID

				insert into @refFields
					select		FriendlyName,
								FormattedValue
					from		FieldWithRelation
					where		ObjectType = 'ReferenceItem' 
								and ObjectID = @refID

					-- Loop through each field for this selected event --
					declare @rfCurrent int,
							@rfMax int,
							@rfCurrentVal nvarchar(max);

					set		@rfCurrent = 1
					select	@rfMax = max(ID) from @refFieldTypes
					while @rfCurrent <= @rfMax
					begin
						select	@name = Name from @refFieldTypes where ID = @rfCurrent

						if exists (select 1 from @refFields where Name = @name)
						begin
							select @refHtml = @refHtml + '<td>' + coalesce(Value, '1') + '</td>' from @refFields where Name = @name;
						end
						else
						begin
							set @refHtml = @refHtml + '<td>&nbsp;</td>';
						end

						set @rfCurrent = @rfCurrent + 1
					end
					-----------------------------------------------------

				delete @refFields

				set @refHtml = @refHtml + '</tr>'	-- Close off row for selected lookup.

				set @current = @current + 1
			end
			-----------------------------------------

			set @refHtml = @refHtml + '</tbody>'

			set @refHtml = @refHtml + '</table>'

			insert into @tbl values ('Items', @refHtml)
			------------------------------------------------------------------
		end;

		if @Type = 'Resource' OR @Type = 'ResourceType'
		begin
			-- BUILD Resource LIST HTML -----------------------------------------
			declare @resourceItemsHtml nvarchar(max)

			set @resourceItemsHtml = '<table class="hoverable bordered striped" style="width:100%"><thead>'
			set @resourceItemsHtml = @resourceItemsHtml + '<th style="margin-right: 15px">First Name</th><th style="margin-right: 15px">Last Name</th><th>Email</th>'
			set @resourceItemsHtml = @resourceItemsHtml + '</thead><tbody>'

			select		top 10 
						@resourceItemsHtml = @resourceItemsHtml + '<tr>' + 
											'<td>' + FirstName + '</td>' + 
											'<td>' + LastName + '</td>' + 
											'<td>' + Email + '</td>'
											+ '</tr>'
			from		reporting.Global_Resource
			order by	LastName, FirstName asc

			set @resourceItemsHtml = @resourceItemsHtml + '</tbody>'
			set @resourceItemsHtml = @resourceItemsHtml + '</table>'
 
			insert into @tbl values ('Items', @resourceItemsHtml)
			------------------------------------------------------------------
		end;

		if @Type = 'ReferenceItem'
		begin

			declare @myReferenceListID int

			select	@myReferenceListID = ReferenceItemTypeID from ReferenceItem where ID = @ID
			-- BUILD LIST HTML -----------------------------------------
			declare @referenceItemHtml nvarchar(max)

			set @referenceItemHtml = '<table class="hoverable bordered striped" style="width:100%">'
			set @referenceItemHtml = @referenceItemHtml + '<thead><th style="margin-right: 15px">Name</th></thead>'
			set @referenceItemHtml = @referenceItemHtml + '<tbody>'



			select		top 10 
						@referenceItemHtml = @referenceItemHtml + '<tr>' + '<td>' + DisplayValue + '</td>' + '</tr>'             
			from		ReferenceItem
			where		ReferenceItemTypeID = @myReferenceListID
			order by	DisplayValue desc

			set @referenceItemHtml = @referenceItemHtml + '</tbody>'
			set @referenceItemHtml = @referenceItemHtml + '</table>'
 
			insert into @tbl values ('Items', @referenceItemHtml)
			------------------------------------------------------------------
		end;

		if @Type = 'ReferenceItemType'
		begin

		--	declare @myReferenceListID int

			--select	@myReferenceListID = ReferenceItemTypeID from ReferenceItem where ID = @ID
			-- BUILD LIST HTML -----------------------------------------
			declare @referenceItemTypeHtml nvarchar(max)

			set @referenceItemTypeHtml = '<table class="hoverable bordered striped" style="width:100%">'
			set @referenceItemTypeHtml = @referenceItemTypeHtml + '<thead><th style="margin-right: 15px">Display Value</th></thead>'
			set @referenceItemTypeHtml = @referenceItemTypeHtml + '<tbody>'



			select		top 10 
						@referenceItemTypeHtml = @referenceItemTypeHtml + '<tr>' + '<td>' + DisplayValue + '</td>' + '</tr>'             
			from		ReferenceItem
			where		ReferenceItemTypeID = @ID
			order by	DisplayValue desc

			set @referenceItemTypeHtml = @referenceItemTypeHtml + '</tbody>'
			set @referenceItemTypeHtml = @referenceItemTypeHtml + '</table>'
 
			insert into @tbl values ('Items', @referenceItemTypeHtml)
			------------------------------------------------------------------
		end;

	end
	
	if @Action = 'None'
	begin
		set @html = '<h3>{Name}</h3><div>'
	end

	if @Action = 'Preview'
	begin
		set @html = '<h3 style="positon: relative">{Name} <small style="background-color: #fff; float:right;font-size:65%;">{Type}</small></h3><div>{Description}</div>'
		set @showIcon = 0

		if @Type = 'Artifact'
		begin
			declare @artifactPathHtml nvarchar(2500) = '<table>';
			declare @artLevelResult table(ID int identity, LevelName nvarchar(250), Name nvarchar(250), Url varchar(1000));

			with ap as (
				select	O.ID,
						O.ParentID,
						O.Name,
						L.Name as LevelName,
						dbo.GenerateNgObjectUrl('Artifact', L.ID, O.ID) as Url,
						1 as [Level]
				from	Artifact O
						inner join ArtifactType L on L.ID = O.ArtifactTypeID
				where	O.ID = @ID
				union all
				select	O.ID,
						O.ParentID,
						O.Name,
						L.Name as LevelName,
						dbo.GenerateNgObjectUrl('Artifact', L.ID, O.ID) as Url,
						C.[Level] + 1 as [Level]
				from	Artifact O
						inner join ArtifactType L on L.ID = O.ArtifactTypeID
						inner join ap as C on C.ParentID = O.ID
			)

			insert into @artLevelResult
				select LevelName, Name, Url from ap order by [Level] desc

			select		@artifactPathHtml = coalesce(@artifactPathHtml + '', '') + '<tr><td style="width: 15px">' +  cast([ID] as varchar) + '</td><td>' +  LevelName + '</td><td><b><a href="' + Url + '">' + Name + '</a></b>' + '</td></tr>'
			from		@artLevelResult
			
			set @artifactPathHtml =  @artifactPathHtml + '</table>'

			insert into @tbl
			select	'Status', [Status]
			from	Artifact
			where	ID = @ID

			set @html = @html + '<div><b>Path:</b></div><div>' + coalesce(@artifactPathHtml,'') + '</div>'

			insert into @tbl 
				select 'GoverningDomain', tt.name
				from
					artifact a
					inner join taxonomytype tt on (a.taxonomytypeid = tt.id and a.id = @ID)

			set @html = @html + '<div><b>' + @SubjectName + ':</b> {GoverningDomain}</div>'
			set @html = @html + '<div><b>Status:</b> {Status}</div>'
			--set @html = @html + '<div><b>Path:</b> {Path}</div>'

			set @hasDynamicFields = 1
		end;

		if @Type = 'FusionAttribute'
		begin
			declare @faPathHtml nvarchar(2500) = '<table>';
			declare @faLevelResult table(ID int identity, LevelName nvarchar(250), Name nvarchar(250));

			with fap as (
				select	O.ID,
						O.ParentID,
						O.Name,
						L.Name as LevelName,
						1 as [Level]
				from	FusionAttribute O
						inner join FusionAttributeType L on L.ID = O.FusionAttributeTypeID
				where	O.ID = @ID
				union all
				select	O.ID,
						O.ParentID,
						O.Name,
						L.Name as LevelName,
						C.[Level] + 1 as [Level]
				from	FusionAttribute O
						inner join FusionAttributeType L on L.ID = O.FusionAttributeTypeID
						inner join fap as C on C.ParentID = O.ID
			)

			insert into @faLevelResult
				select LevelName, Name from fap order by [Level] desc

			select		@faPathHtml = @faPathHtml + 
						'<tr><td colspan="2">Configuration</td><td><b><a href="/fusion/' + cast(F.ID as nvarchar) + '">' + coalesce(F.Name,'') + '</a></b></td></tr>' 
			from		Fusion F 
						inner join FusionAttribute A on A.FusionID = F.ID and A.ID = @ID

			select		@faPathHtml = coalesce(@faPathHtml + '', '') + '<tr><td style="width: 15px">' +  cast(ID as varchar) + '</td><td>' +  LevelName + '</td><td><b>' + Name + '</b>' + '</td></tr>'
			from		@faLevelResult

			set @faPathHtml =  @faPathHtml + '</table>'

			set @html = @html + '<div><b>Path:</b></div><div>' + coalesce(@faPathHtml,'') + '</div>'

			set @hasDynamicFields = 1
		end

		if @Type = 'Intersect'
		begin
			set @hasDynamicFields = 1
		end;

		
		if @Type = 'Issue'
		begin
			insert into @tbl values('Name', '')
			insert into @tbl values('Description', '')
					
			if exists (select id from issue where id = @ID)
			begin			
				set @html = @html + '<div><b>Issue Type:</b> {IssueType}</div>'
				set @html = @html + '<div><b>Criticality:</b> {Criticality}</div>'
						
				insert into @tbl 
					select 'IssueType', it.name 
					from issuetype it inner join issue i on(i.issuetypeid = it.id) 
					where i.id = @ID

				insert into @tbl 
					select 'Criticality', case when i.Criticality = 0 then 'Negligible' when i.Criticality = 1 then 'Low' when i.Criticality = 2 then 'Medium' when i.Criticality = 3 then 'High'  when i.Criticality = 4 then 'Critical' else 'N/A' end
					from issuetype it inner join issue i on(i.issuetypeid = it.id) 
					where i.id = @ID

				set @hasDynamicFields = 1
			end			
		end;

		if @Type = 'Responsibility'
		begin
			select	@n = T.Name, 
					@t = T.Name,
					@d = T.[Description]
			from	Responsibility O
					inner join ResponsibilityType T on T.ID = O.ResponsibilityTypeID
			where	O.ID = @ID
			
			declare @contextsHtml nvarchar(max)

			set @contextsHtml = '<table class="hoverable bordered striped" style="width:100%">' + 
								'<thead><th>List</th><th>Code</th></thead>' + 
								'<tbody>' + 
								(
								select		(select D.Name as 'td' for xml path(''), type),
											(select I.Code as 'td' for xml path(''), type)
								from		ResponsibilityContextItem R
											inner join ReferenceItem I on R.ResponsibilityID = @ID and R.ObjectType = 'ReferenceItem' and I.ID = R.ObjectID
											inner join ReferenceItemType D on D.ID = I.ReferenceItemTypeID
								FOR XML RAW('tr'), ELEMENTS
								) +
								'</tbody>' + 
								'</table>'

			insert into @tbl values ('Name', @n)
			insert into @tbl values ('Type', @t)
			insert into @tbl values ('Description', @d)
			insert into @tbl values ('Contexts', @contextsHtml)

			set @html = @html + '<div><b>Contexts:</b> {Contexts}</div>'
		end;

		if @Type = 'Resource'
		begin
			--declare @e nvarchar(500), @fn nvarchar(250), @ln nvarchar(250)
			--select	@e = Email, @fn = FirstName, @ln = LastName
			--from	reporting.Global_Resource
			--where	ResourceID = @ID

			--insert into @tbl values ('Email', @e)
			--insert into @tbl values ('FirstName', @fn)
			--insert into @tbl values ('LastName', @ln)
			--insert into @tbl values ('Role', '')

			--set @html = @html + '<div><b>Email:</b> {Email}</div>'
			--set @html = @html + '<div><b>First Name:</b> {FirstName}</div>'
			--set @html = @html + '<div><b>Last Name:</b> {LastName}</div>'

			set @hasDynamicFields = 1
		end;

		if @Type = 'Rule'
		begin
			insert into @tbl
				select	'Name', Name
				from	[Rule] O
				where	ID = @ID
			insert into @tbl
				select	'Description', Description
				from	[Rule] O
				where	ID = @ID

			set @hasDynamicFields = 1
		end;

		if @Type = 'RuleDimension'
		begin
			insert into @tbl
				select	'Description', [Description]
				from	RuleDimension
				where	ID = @ID
			insert into @tbl
				select	'Name', [Name]
				from	RuleDimension
				where	ID = @ID

			--set @html = @html + '<div><b>Path:</b> {Description}</div>'
						
		end;

		if @Type = 'Taxonomy'
		begin
			declare @taxonomyPathHtml nvarchar(2500) = '<table>';

			with tp as (
				select	O.ID,
						O.ParentID,
						O.Name,
						coalesce(L.Name, 'Level ' + cast(O.[Level] as varchar)) as LevelName,
						O.[Level]
				from	[Taxonomy] O
						left join TaxonomyTypeLevel L on L.TaxonomyTypeID = O.TaxonomyTypeID and L.[Level] = O.[Level]
				where	O.ID = @ID
				union all
				select	O.ID,
						O.ParentID,
						O.Name,
						coalesce(L.Name, 'Level ' + cast(O.[Level] as varchar)) as LevelName,
						O.[Level]
				from	[Taxonomy] O
						outer apply (
									select Name from TaxonomyTypeLevel where TaxonomyTypeID = O.TaxonomyTypeID and [Level] = O.[Level]
									) L
						--left join TaxonomyTypeLevel L on L.TaxonomyTypeID = O.TaxonomyTypeID and L.[Level] = O.[Level]
						inner join tp as C on C.ParentID = O.ID
			)

			select		@taxonomyPathHtml = coalesce(@taxonomyPathHtml + '', '') + '<tr><td style="width: 15px">' +  cast([Level] as varchar) + '</td><td>' +  LevelName + '</td><td><b>' + Name + '</b>' + '</td></tr>'
			from		tp
			order by	[Level]
			
			set @taxonomyPathHtml =  @taxonomyPathHtml + '</table>'
			--insert into @tbl
			--	select	'TextPath', TextPath
			--	from	Taxonomy O
			--	where	ID = @ID

			--set @html = @html + '<div><b>Path:</b> {TextPath}</div>'
			set @html = @html + '<div><b>Path:</b></div><div>' + coalesce(@taxonomyPathHtml,'') + '</div>'

			set @hasDynamicFields = 1
		end;

		if @Type = 'TaxonomyType'
		begin
			insert into @tbl
				select	'Name', Name
				from	TaxonomyType O
				where	ID = @ID

			set @hasDynamicFields = 1
		end;
		
		-- If required, get dynamic fields to add to list.
		if @hasDynamicFields = 1
		begin
			select	@html = @html + '<div><b>' + FriendlyName + '</b>: ' + '{' + Name + '}' + '</div>' 
			from	FieldWithRelation
			where	ObjectType = @Type
					and ObjectID = @ID
					and Name not in (select Name from @tbl)

			insert into @tbl
				select	Name,
						FormattedValue
				from	FieldWithRelation
				where	ObjectType = @Type
						and ObjectID = @ID
						and Name not in (select Name from @tbl)
		end;
	end

	if @Action = 'Statistics'
	begin
		set @html = '<h3>{Name}</h3><div>{Statistics}</div>'

		set @hasStats = case @Type
							when 'Artifact' then 1
							when 'Taxonomy' then 1
							else 0
						end

		-- If required, build statistics table
		if @hasStats = 1
		begin
			-- BUILD STATS LIST HTML -----------------------------------------
			declare @statsHtml nvarchar(max)

			declare @stats table (ID int identity, Name nvarchar(250), Score int)
			insert into @stats 
				select	T.Name,
						coalesce(S.SCore, 0) as Score
				from	StatisticType T
						outer apply (
									select	top 1
											*
									from	Statistic
									where	StatisticTypeID = T.ID
											and ObjectType = @Type
											and ObjectID = @ID
									order by DateStart desc
									) S
				where	T.[Object] = @Type + 'Type' 
						and T.ObjectID = @typeID
						and T.PartOfScore = 1

			set @statsHtml = '<table class="hoverable bordered striped" style="width:100%">'

			-- Loop through field name list ---------
			set @statsHtml = @statsHtml + '<tbody>'
			set		@current = 1
			select	@max = max(ID) from @stats
			while @current <= @max
			begin
				select	@statsHtml = @statsHtml + '<tr><td>' + Name  + '</td>' + '<td>' + cast(Score as varchar(5))  + ' Points</td></tr>'
				from	@stats
				where	ID = @current

				set @current = @current + 1
			end
			set @statsHtml = @statsHtml + '</tbody>'
			-----------------------------------------

			insert into @tbl values ('Statistics', @statsHtml)

			------------------------------------------------------------------
		end;
	end

	-- Replace the fields in the template with the appropriate text value.
	set		@current = 1
	select	@max = max(ID) from @tbl

	while @current <= @max
	begin
		select	@name = '{' + Name + '}',
				@value = COALESCE(Value, '')
		from	@tbl 
		where	ID = @current

		if @showIcon = 1
		begin
			if @name = '{Name}' and @icon is not null
			begin
				update	@tbl 
				set		Value = '<div class="pull-left" style="width: 30px">' + @icon + '</div>' + '<div class="pull-right">' + @value + '</div>'
				where	ID = @current
				--set @usedIconAlready = 1
			end
		end

		set @html = REPLACE(@html, @name, @value)

		set @current = @current + 1
	end

	--if @showIcon = 1 and @icon is not null
	--begin
	--	set @html = @icon + '<br/>' + @html
	--end

	set @html = '<div style="max-height: 500px; min-width: 400px; overflow-y: auto">' + @html + '</div>'

	-- Return the properly formatted values.
	select	'' as Title,
			@html as Body;
END
GO

ALTER PROCEDURE [dbo].[GetScoreHistoryByObject]-- 'Artifact', 733
--declare
	@type varchar(50),
	@id int
AS
begin
	declare @DateStart date, 
			@DateEnd date

	select	@DateEnd = max(Date),
			@DateStart = DATEADD(d, -30, max(Date))
	from	Score
	where	Object = @type 
			and ObjectID = @id
			and ScoreTypeID = 1
	
	select	Date,
			Value as Score
	from	Score
	where	Object = @type 
			and ObjectID = @id
			and ScoreTypeID = 1
			and Date between @DateStart and @DateEnd
end
GO

ALTER PROCEDURE [dbo].[GetSiteNavigation]
AS
BEGIN
	SET NOCOUNT ON;

SELECT	n.Name as MenuID,
		n.SortOrder,
		0 as Feature,
		n.Icon as Icon,
		n.Title as Title,
		NULL AS Items	
FROM SiteNav n
WHERE n.Name = '#Monitor'
UNION ALL

SELECT	n.Name as MenuID,
		n.SortOrder,
		0 as Feature,
		n.Icon as Icon,
		n.Title as Title,
		NULL AS Items		
FROM SiteNav n
WHERE n.Name = '#Home'
UNION ALL

SELECT	n.Name as MenuID,
		n.SortOrder,
		0 as Feature,
		n.Icon as Icon,
		n.Title as Title,
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
					ORDER BY	a.name
					) BG
					FOR XML PATH('nav'), TYPE
		) AS Items
FROM SiteNav n
WHERE n.Name = '#Glossary'

UNION ALL

SELECT	n.Name as MenuID,
		n.SortOrder,
		0 as Feature,
		n.Icon as Icon,
		n.Title as Title,
		(
		SELECT	ft.name, 
				'model/classification/' + ft.name As url,
				0 as feature,
				(

				SELECT	t.name, 
						dbo.GenerateNgObjectUrl('TaxonomyType', 0, t.ID)  As url,
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
		0 as Feature,
		n.Icon as Icon,
		n.Title as Title,
		(
		SELECT	ft.name, 				
				'policy/classification/' + cast(ft.id as varchar(15)) As url,
				0 as feature,
				(
				SELECT	t.name, 
						dbo.GenerateNgObjectUrl('PolicyType', t.ID, 0)  As url,
						0 as feature
				FROM	PolicyType t
				LEFT JOIN SiteNav v on v.ObjectID = t.ID and v.Object = 'PolicyType'
				WHERE	PolicyTypeClassID = FT.ID and v.ObjectID is null
				FOR XML PATH('nav'), TYPE
				) AS items	
		FROM	(
                select top 100 percent ID, name from PolicyTypeClass C where exists(select 1 from PolicyType where PolicyTypeClassID = C.ID) order by name
				) FT
		LEFT JOIN SiteNav v on v.ObjectID = FT.ID and v.Object ='PolicyTypeClass'
		WHERE v.ObjectID IS NULL
		FOR XML PATH('nav'), TYPE
		) AS Items
		FROM SiteNav n
WHERE n.Name = '#Policy'
		
UNION ALL

SELECT	n.Name as MenuID,
		n.SortOrder,
		0 as Feature,
		n.Icon as Icon,
		n.Title as Title,
		null AS Items
FROM SiteNav n
WHERE n.Name = '#Reference'

UNION ALL

SELECT	n.Name as MenuID,
		n.SortOrder,
		2 as Feature,
		n.Icon as Icon,
		n.Title as Title,
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
		n.Icon as Icon,
		n.Title as Title,
		(
        SELECT	'People' AS name, --'#People' as MenuID,
                'community/groups' AS url, 		        
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
		'fa-cogs' as Icon,
		'Administration' as Title,
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
									SELECT	'Scoring' AS name, 
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

	SELECT	n.Name as MenuID,
		n.SortOrder,
		0 as Feature,
		n.Icon as Icon,
		n.Title as Title,
		(
		SELECT	RT.name, 				
				dbo.GenerateNgObjectUrl('RuleType', RT.ID, RT.ID) As url,
				0 as feature,
				null AS items	
		FROM	RuleType RT
				LEFT JOIN SiteNav v on v.ObjectID = RT.ID and v.Object ='RuleType'
		WHERE	v.ObjectID IS NULL
		FOR XML PATH('nav'), TYPE
		) AS Items
	FROM SiteNav n
	WHERE n.Name = '#Data Quality'

	UNION ALL

	SELECT 
		'~' + Name AS MenuID,
		s.SortOrder,
		0 AS Feature,
		s.Icon as Icon,
		s.Title as Title,
		dbo.CustomSiteNavigation(ID) AS Items
	from SiteNav s
	where ParentID IS NULL and Name not like '#%'

	order by sortorder
END
GO

ALTER procedure [fusion].[GenerateMarkitMapLineageData]
	@fusionID int
as
begin
	SET NOCOUNT, ANSI_PADDING ON;
	SET ANSI_WARNINGS ON;

	declare @databaseName varchar(100);
	declare @sourceFieldTypeID int;
	declare @targetFieldTypeID int;		
	declare @mapFusionAttributeTypeID int = 710; -- this is fixed for all clients
	declare @viewColumnFusionAttributeTypeID int = 715; -- this is fixed for all clients
	
	-- load the field ids for the source / target from mappings
	select @sourceFieldTypeID = id from fieldtype where [object] = 'FusionAttributeType' and [objectid] = @mapFusionAttributeTypeID and name = 'source';
	select @targetFieldTypeID = id from fieldtype where [object] = 'FusionAttributeType' and [objectid] = @mapFusionAttributeTypeID and name = 'target';
	
	IF @sourceFieldTypeID IS NULL
	begin		
		raiserror('ERROR - Cannot find the Markit Fusion Map Source Field.  Please make sure the latest markit fusion attribute types have been pushed to this environment', 16, -1);
		return;
	end

	IF @targetFieldTypeID IS NULL
	begin		
		raiserror('ERROR - Cannot find the Markit Fusion Map Target Field.  Please make sure the latest markit fusion attribute types have been pushed to this environment', 16, -1);
		return;
	end

	-- determine the database name
	select top 1 @databaseName = replace(sourceid, name,'') from fusionattribute where fusionid = @fusionID and fusionattributetypeid = 711;

	if @databaseName is null
	begin
		raiserror('ERROR - Cannot determine the database name to strip from markit fusion attribute data', 16, -1);
		return;
	end

	-- dont run if this is not a markit fusion
	declare @fusionTypeId int;
	select @fusionTypeId = FusionTypeID from [dbo].[Fusion] where ID = @fusionID;
	if @fusionTypeId != 13
	begin
		raiserror('ERROR - The fusion lineage generation process may only be run for the Markit Fusion Type', 16, -1);
		return;
	end

	-- dont run if no map records exist for this fusion
	if not exists( select 1 from fusionattribute where fusionid = @fusionID and fusionattributetypeid = @mapFusionAttributeTypeID )
	begin
		raiserror('ERROR - No Markit Fusion Map records exist for the specified Fusion ID', 16, -1);
		return;
	end

	-- figure out the database prefix from some markit data

	-- some logging
	declare @fusionName nvarchar(250);
	select @fusionName = name from [dbo].[fusion] where id = @fusionID;

	begin
		print 'Running For Fusion:' + @fusionName;
		print 'Using Target Field ID:' + cast(@targetFieldTypeID as varchar(100));
		print 'Using Source Field ID:' + cast(@sourceFieldTypeID as varchar(100));
		print 'Using Database prefix:' + @databaseName;
	end
	-- end logging

	-- get the intersecttypeid for view -> table intersects
	declare @viewTableIntersectTypeId int;
	select @viewTableIntersectTypeId = id from intersecttype where [object] = 'FusionAttributeType' and [Subject] = 'FusionAttributeType' and [subjectid] = 714 and [objectid] = 712
	if @viewTableIntersectTypeId is null
	begin
		raiserror('ERROR - Cannot identify the intersecttypeid for markit view/table relations', 16, -1);
		return;
	end

	-- get the intersecttypeid for view -> view intersects
	declare @viewViewIntersectTypeId int;
	select @viewViewIntersectTypeId = id from intersecttype where [object] = 'FusionAttributeType' and [Subject] = 'FusionAttributeType' and [subjectid] = 714 and [objectid] = 714
	if @viewViewIntersectTypeId is null
	begin
		raiserror('ERROR - Cannot identify the intersecttypeid for markit view/view relations', 16, -1);
		return;
	end

	IF OBJECT_ID('tempdb..#maps') IS NOT NULL
		DROP TABLE #maps;

	create table #maps (	
		ID int identity primary key,			
		MapRuleItemID int,
		[ParentID] int,
		[UltimateParentID] int,
		[Level] int,
		SourceFusionAttributeID int,
		SourceFusionAttributeTypeID int,
		SourceObject nvarchar(500),		
		SourceParentObject nvarchar(max),
		SourceParentObjectFusionAttributeID int,
		SourceParentObjectFusionAttributeTypeID int,
		TargetFusionAttributeID int,
		TargetFusionAttributeTypeID int,
		TargetObject nvarchar(500),
		TargetParentObject nvarchar(max),
		TargetParentObjectFusionAttributeID int,
		TargetParentObjectFusionAttributeTypeID int,					
		[Source] varchar(50),
		[SourceID] int,	
		[Target] varchar(50),
		[TargetID] int,
	);

	CREATE NONCLUSTERED INDEX [CIX_TempMaps] ON #maps ( SourceFusionAttributeID ASC, TargetFusionAttributeID ASC );

	IF OBJECT_ID('tempdb..#objectmap') IS NOT NULL
		DROP TABLE #objectmap;

	create table #objectmap (
		MapID int,
		MapItemID int,
		[Object] varchar(50),
		[ObjectID] int,	
		[SourceIntersectID] int,		
		[TargetIntersectID] int		
	)

	CREATE NONCLUSTERED INDEX [CIX_TempObjectMap] ON #objectmap ( MapID ASC, [Object] ASC, [ObjectID] ASC );
	
	insert into #maps
		(SourceObject, TargetObject)
		select distinct
			replace(cast(F_source.formattedValue as nvarchar(500)), @databaseName, '') as SourceObject						
			, replace(cast(F_target.formattedValue as nvarchar(500)), @databaseName, '') as TargetObject			
		from 
			FusionAttribute FA
			inner join Field F_source on F_source.ObjectType = 'FusionAttribute' and F_source.ObjectID = FA.ID and F_source.FieldTypeID = @sourceFieldTypeID -- MAP SOURCE FIELD VALUE
			inner join Field F_target on F_target.ObjectType = 'FusionAttribute' and F_target.ObjectID = FA.ID and F_target.FieldTypeID = @targetFieldTypeID -- TARGET SOURCE FIELD VALUE
		where 
			FA.FusionID = @fusionID
				and
			FA.FusionAttributeTypeID = @mapFusionAttributeTypeID
			--	and
			--F_source.formattedValue like '%.cusip' or F_source.formattedValue like '%.ticker' or F_source.formattedValue like '%.cntry_of%' -- **for testing to limit to just cusip**;
	
	-- check how many map records we have
	declare @mapRecordCount int;
	select @mapRecordCount = count(1) from #maps
	if @fusionTypeId > 0
		begin
			print 'Loaded [' + cast(@mapRecordCount as varchar) + '] map records';			
		end
	else
		begin
			raiserror('ERROR - Could not load any map records this is most likely because there are no corresponding fusionattributes for the markit source/target mappings.', 16, -1);
			return;
		end

			
	--set the Source objects 
	update	T
	set		T.SourceFusionAttributeID = S.ID, T.SourceFusionAttributeTypeID = S.FusionAttributeTypeID
	from	#maps T			
			inner join fusionattribute S on (S.TextPath = T.SourceObject and S.FusionID = @fusionID)

	--set the Target Objects
	update	T
	set		T.TargetFusionAttributeID = S.ID, T.TargetFusionAttributeTypeID = S.FusionAttributeTypeID
	from	#maps T			
			inner join fusionattribute S on (S.TextPath = T.TargetObject and S.FusionID = @fusionID)

	--remove any source objects that we cant find the fusion attribute for
	delete from #maps where SourceFusionAttributeID is null or TargetFusionAttributeID is null		
	
	--set the source parent objects
	update T
	set T.SourceParentObject = FA_p.TextPath, T.SourceParentObjectFusionAttributeID = FA_p.ID, T.SourceParentObjectFusionAttributeTypeID = FA_p.FusionAttributeTypeID
	from #maps T
		inner join fusionattribute FA on (FA.ID = T.SourceFusionAttributeID)
		inner join fusionattribute FA_p on (FA_p.ID = FA.ParentID)

	--set the target parent objects
	update T
	set T.TargetParentObject = FA_p.TextPath, T.TargetParentObjectFusionAttributeID = FA_p.ID, T.TargetParentObjectFusionAttributeTypeID = FA_p.FusionAttributeTypeID
	from #maps T
		inner join fusionattribute FA on (FA.ID = T.TargetFusionAttributeID)
		inner join fusionattribute FA_p on (FA_p.ID = FA.ParentID)

	-- remove any maps that reference same fusionattribute both sides
	delete from #maps where SourceFusionAttributeID = TargetFusionAttributeID;
	
	--this query adds in the view to table mapings
	-- add in any view column to table column records
	-- table / view maps for targets that are missing connection
	insert into #maps
		(SourceFusionAttributeID, SourceFusionAttributeTypeID, SourceObject, SourceParentObjectFusionAttributeID, SourceParentObject, SourceParentObjectFusionAttributeTypeID, TargetFusionAttributeID, TargetFusionAttributeTypeID, TargetObject, TargetParentObjectFusionAttributeID, TargetParentObject, TargetParentObjectFusionAttributeTypeID)
		select 	distinct
			m.TargetFusionAttributeID as SourceFusionAttributeID,
			m.TargetFusionAttributeTypeID as SourceFusionAttributeTypeID,
			m.TargetObject as SourceObject,
			m.TargetParentObjectFusionAttributeID as SourceParentObjectFusionAttributeID,
			m.TargetParentObject as SourceParentObject,
			m.TargetParentObjectFusionAttributeTypeID as SourceParentObjectFusionAttributeTypeID,
			T.id as TargetFusionAttributeID,
			T.fusionattributetypeid as TargetFusionAttributeTypeID,
			T.textpath as TargetObject,
			i.objectid as TargetParentObjectFusionAttributeID,
			T_p.name as TargetParentObject,
			T_p.fusionattributetypeid as TargetParentObjectFusionAttributeTypeID			
		 from 
			#maps m			
			inner join [intersect] i on (i.subjectid = m.TargetParentObjectFusionAttributeID and i.[subject] = 'FusionAttribute')	
			inner join fusionattribute T_p on (T_p.id = i.objectid)
			inner join fusionattribute T on(T.parentid = T_p.id and T.Textpath = T_p.TextPath + replace(m.TargetObject,m.TargetParentObject,'')) -- we are doing this to avoid messing with the name column that doesnt have an index
		where 
			m.TargetFusionAttributeTypeID = @viewColumnFusionAttributeTypeID
				and
			i.intersecttypeid = @viewTableIntersectTypeId
				and
			m.id not in(select m_2.id from #maps m_2 where (m_2.SourceFusionAttributeID = m.TargetFusionAttributeID and m_2.TargetFusionAttributeID = T.Id) or (m_2.TargetFusionAttributeID = m.TargetFusionAttributeID and m_2.SourceFusionAttributeID = T.id)) -- dont insert duplicates
	
	-- table / view maps for sources that are missing connection
	insert into #maps
		(TargetFusionAttributeID, TargetFusionAttributeTypeID, TargetObject, TargetParentObjectFusionAttributeID, TargetParentObject, TargetParentObjectFusionAttributeTypeID, SourceFusionAttributeID, SourceFusionAttributeTypeID, SourceObject, SourceParentObjectFusionAttributeID, SourceParentObject, SourceParentObjectFusionAttributeTypeID)
		select 	distinct
			m.SourceFusionAttributeID as TargetFusionAttributeID,
			m.SourceFusionAttributeTypeID as TargetFusionAttributeTypeID,
			m.SourceObject as TargetObject,
			m.SourceParentObjectFusionAttributeID as TargetParentObjectFusionAttributeID,
			m.SourceParentObject as TargetParentObject,
			m.SourceParentObjectFusionAttributeTypeID as TargetParentObjectFusionAttributeTypeID,
			T.id as SourceFusionAttributeID,
			T.fusionattributetypeid as SourceFusionAttributeTypeID,
			T.textpath as SourceObject,
			i.objectid as SourceParentObjectFusionAttributeID,
			T_p.name as SourceParentObject,
			T_p.fusionattributetypeid as SourceParentObjectFusionAttributeTypeID			
		 from 
			#maps m			
			inner join [intersect] i on (i.subjectid = m.SourceParentObjectFusionAttributeID and i.[subject] = 'FusionAttribute')	
			inner join fusionattribute T_p on (T_p.id = i.objectid)
			inner join fusionattribute T on(T.parentid = T_p.id and T.Textpath = T_p.TextPath + replace(m.SourceObject,m.SourceParentObject,'')) -- we are doing this to avoid messing with the name column that doesnt have an index
		where 
			m.SourceFusionAttributeTypeID = @viewColumnFusionAttributeTypeID
				and
			i.intersecttypeid = @viewTableIntersectTypeId
				and
			m.id not in(select m_2.id from #maps m_2 where (m_2.SourceFusionAttributeID = T.Id and m_2.TargetFusionAttributeID = m.SourceFusionAttributeID) or (m_2.TargetFusionAttributeID = T.id  and m_2.SourceFusionAttributeID = m.SourceFusionAttributeID)) -- dont insert duplicates
					
	-- end table / view maps

	

	--this query adds in the view to view mapings
	-- add in any view column to view column records
	-- view / view maps for targets that are missing connection
	/*insert into #maps
		(SourceFusionAttributeID, SourceFusionAttributeTypeID, SourceObject, SourceParentObjectFusionAttributeID, SourceParentObject, SourceParentObjectFusionAttributeTypeID, TargetFusionAttributeID, TargetFusionAttributeTypeID, TargetObject, TargetParentObjectFusionAttributeID, TargetParentObject, TargetParentObjectFusionAttributeTypeID)
		select 	distinct
			m.TargetFusionAttributeID as SourceFusionAttributeID,
			m.TargetFusionAttributeTypeID as SourceFusionAttributeTypeID,
			m.TargetObject as SourceObject,
			m.TargetParentObjectFusionAttributeID as SourceParentObjectFusionAttributeID,
			m.TargetParentObject as SourceParentObject,
			m.TargetParentObjectFusionAttributeTypeID as SourceParentObjectFusionAttributeTypeID,
			T.id as TargetFusionAttributeID,
			T.fusionattributetypeid as TargetFusionAttributeTypeID,
			T.textpath as TargetObject,
			i.objectid as TargetParentObjectFusionAttributeID,
			T_p.name as TargetParentObject,
			T_p.fusionattributetypeid as TargetParentObjectFusionAttributeTypeID			
		 from 
			#maps m			
			inner join [intersect] i on (i.subjectid = m.TargetParentObjectFusionAttributeID and i.[subject] = 'FusionAttribute')	
			inner join fusionattribute T_p on (T_p.id = i.objectid)
			inner join fusionattribute T on(T.parentid = T_p.id and T.Textpath = T_p.TextPath + replace(m.TargetObject,m.TargetParentObject,'')) -- we are doing this to avoid messing with the name column that doesnt have an index
		where 
			m.TargetFusionAttributeTypeID = @viewColumnFusionAttributeTypeID
				and
			i.intersecttypeid = @viewViewIntersectTypeId
				and
			m.id not in(select m_2.id from #maps m_2 where (m_2.SourceFusionAttributeID = m.TargetFusionAttributeID and m_2.TargetFusionAttributeID = T.Id) or (m_2.TargetFusionAttributeID = m.TargetFusionAttributeID and m_2.SourceFusionAttributeID = T.id)) -- dont insert duplicates
	*/
	insert into #maps
		(SourceFusionAttributeID, SourceFusionAttributeTypeID, SourceObject, SourceParentObjectFusionAttributeID, SourceParentObject, SourceParentObjectFusionAttributeTypeID, TargetFusionAttributeID, TargetFusionAttributeTypeID, TargetObject, TargetParentObjectFusionAttributeID, TargetParentObject, TargetParentObjectFusionAttributeTypeID)
		select 	distinct
			m.TargetFusionAttributeID as SourceFusionAttributeID,
			m.TargetFusionAttributeTypeID as SourceFusionAttributeTypeID,
			m.TargetObject as SourceObject,
			m.TargetParentObjectFusionAttributeID as SourceParentObjectFusionAttributeID,
			m.TargetParentObject as SourceParentObject,
			m.TargetParentObjectFusionAttributeTypeID as SourceParentObjectFusionAttributeTypeID,
			T.id as TargetFusionAttributeID,
			T.fusionattributetypeid as TargetFusionAttributeTypeID,
			T.textpath as TargetObject,
			i.objectid as TargetParentObjectFusionAttributeID,
			T_p.name as TargetParentObject,
			T_p.fusionattributetypeid as TargetParentObjectFusionAttributeTypeID			
		 from 
			#maps m			
			inner join [intersect] i on (i.objectid = m.TargetParentObjectFusionAttributeID and i.[object] = 'FusionAttribute')	
			inner join fusionattribute T_p on (T_p.id = i.subjectid)
			inner join fusionattribute T on(T.FusionId = @fusionId and T.deleted = 0 and T.parentid = T_p.id and T.Textpath = T_p.TextPath + replace(m.TargetObject,m.TargetParentObject,'')) -- we are doing this to avoid messing with the name column that doesnt have an index
		where 
			m.TargetFusionAttributeTypeID = @viewColumnFusionAttributeTypeID
				and
			i.intersecttypeid = @viewViewIntersectTypeId
				and
			m.id not in(select m_2.id from #maps m_2 where (m_2.SourceFusionAttributeID = m.TargetFusionAttributeID and m_2.TargetFusionAttributeID = T.Id) or (m_2.TargetFusionAttributeID = m.TargetFusionAttributeID and m_2.SourceFusionAttributeID = T.id)) -- dont insert duplicates

	-- view / view maps for sources that are missing connection
	insert into #maps
		(TargetFusionAttributeID, TargetFusionAttributeTypeID, TargetObject, TargetParentObjectFusionAttributeID, TargetParentObject, TargetParentObjectFusionAttributeTypeID, SourceFusionAttributeID, SourceFusionAttributeTypeID, SourceObject, SourceParentObjectFusionAttributeID, SourceParentObject, SourceParentObjectFusionAttributeTypeID)
		select 	distinct
			m.SourceFusionAttributeID as TargetFusionAttributeID,
			m.SourceFusionAttributeTypeID as TargetFusionAttributeTypeID,
			m.SourceObject as TargetObject,
			m.SourceParentObjectFusionAttributeID as TargetParentObjectFusionAttributeID,
			m.SourceParentObject as TargetParentObject,
			m.SourceParentObjectFusionAttributeTypeID as TargetParentObjectFusionAttributeTypeID,
			T.id as SourceFusionAttributeID,
			T.fusionattributetypeid as SourceFusionAttributeTypeID,
			T.textpath as SourceObject,
			i.objectid as SourceParentObjectFusionAttributeID,
			T_p.name as SourceParentObject,
			T_p.fusionattributetypeid as SourceParentObjectFusionAttributeTypeID			
		 from 
			#maps m			
			inner join [intersect] i on (i.subjectid = m.SourceParentObjectFusionAttributeID and i.[subject] = 'FusionAttribute')	
			inner join fusionattribute T_p on (T_p.id = i.objectid)
			inner join fusionattribute T on(T.FusionId = @fusionId and T.deleted = 0 and T.parentid = T_p.id and T.Textpath = T_p.TextPath + replace(m.SourceObject,m.SourceParentObject,'')) -- we are doing this to avoid messing with the name column that doesnt have an index
		where 
			m.SourceFusionAttributeTypeID = @viewColumnFusionAttributeTypeID
				and
			i.intersecttypeid = @viewViewIntersectTypeId
				and
			m.id not in(select m_2.id from #maps m_2 where (m_2.SourceFusionAttributeID = T.Id and m_2.TargetFusionAttributeID = m.SourceFusionAttributeID) or (m_2.TargetFusionAttributeID = T.id  and m_2.SourceFusionAttributeID = m.SourceFusionAttributeID)) -- dont insert duplicates

	/*	insert into #maps
		(TargetFusionAttributeID, TargetFusionAttributeTypeID, TargetObject, TargetParentObjectFusionAttributeID, TargetParentObject, TargetParentObjectFusionAttributeTypeID, SourceFusionAttributeID, SourceFusionAttributeTypeID, SourceObject, SourceParentObjectFusionAttributeID, SourceParentObject, SourceParentObjectFusionAttributeTypeID)
		select 	distinct
			m.SourceFusionAttributeID as TargetFusionAttributeID,
			m.SourceFusionAttributeTypeID as TargetFusionAttributeTypeID,
			m.SourceObject as TargetObject,
			m.SourceParentObjectFusionAttributeID as TargetParentObjectFusionAttributeID,
			m.SourceParentObject as TargetParentObject,
			m.SourceParentObjectFusionAttributeTypeID as TargetParentObjectFusionAttributeTypeID,
			T.id as SourceFusionAttributeID,
			T.fusionattributetypeid as SourceFusionAttributeTypeID,
			T.textpath as SourceObject,
			i.objectid as SourceParentObjectFusionAttributeID,
			T_p.name as SourceParentObject,
			T_p.fusionattributetypeid as SourceParentObjectFusionAttributeTypeID			
		 from 
			#maps m			
			inner join [intersect] i on (i.objectid = m.SourceParentObjectFusionAttributeID and i.[object] = 'FusionAttribute')	
			inner join fusionattribute T_p on (T_p.id = i.subjectid)
			inner join fusionattribute T on(T.parentid = T_p.id and T.Textpath = T_p.TextPath + replace(m.SourceObject,m.SourceParentObject,'')) -- we are doing this to avoid messing with the name column that doesnt have an index
		where 
			m.SourceFusionAttributeTypeID = @viewColumnFusionAttributeTypeID
				and
			i.intersecttypeid = @viewViewIntersectTypeId
				and
			m.id not in(select m_2.id from #maps m_2 where (m_2.SourceFusionAttributeID = T.Id and m_2.TargetFusionAttributeID = m.SourceFusionAttributeID) or (m_2.TargetFusionAttributeID = T.id  and m_2.SourceFusionAttributeID = m.SourceFusionAttributeID)) -- dont insert duplicates
		*/				
	-- end view / view maps


	-- populate the previous step id this also duplicates items that have multiple paths and is very important
	update m_S
	set m_S.ParentID = m_T.ID
	from #maps m_T
	left outer join #maps m_S on (m_T.TargetFusionAttributeID = m_S.SourceFusionAttributeID)

	IF OBJECT_ID('tempdb..#levelMap') IS NOT NULL
		DROP TABLE #levelMap;
	
	;with C as
			(
			  select
				ID,
				SourceFusionAttributeID as SourceID,
				TargetFusionAttributeID as TargetID,
				ID as [UltimateParentID],
				0 as [level] 
			  from 
					#maps
			  where ParentID is null
			  union all
			  select 
					T.ID,
					T.SourceFusionAttributeID as SourceID,			 
					 T.TargetFusionAttributeID as TargetID,
					 C.[UltimateParentID] as [UltimateParentID],
					 C.[level] + 1
			  from #maps as T
				inner join C  
					on T.ParentID = C.ID				  
			)
			select C.ID, C.[level], C.[UltimateParentID]
			into #levelMap
			from C
			OPTION (MAXRECURSION 25) 

	update T
	set T.[level] = S.[level], T.[UltimateParentID] = S.[UltimateParentID]
	from #maps T
	inner join #levelMap S on S.ID = T.ID;
	
	--remove any that we cant find the level for
	--delete from #maps where [level] is null		


	-- find any object related to column as the object	
	insert into #objectmap (MapID, [Object], [ObjectID])
		select T.ID, OI.[subject], OI.[subjectid]
		from #maps T
		inner join [IntersectDetail] OI on OI.Subject <> 'FusionAttribute' and OI.Object = 'FusionAttribute' and OI.ObjectID in (T.SourceFusionAttributeID, T.TargetFusionAttributeID)  and OI.PredicateType = 8-- look for relation between non fusion object and source/target column

	-- find any business terms related to source
	update T
	set T.[source] = OI.[subject], T.[sourceid] = OI.[subjectid]--, T.sourceintersectid = OI.ID
	from #maps T
		inner join [IntersectDetail] OI on OI.Subject <> 'FusionAttribute' and OI.Object = 'FusionAttribute' and OI.ObjectID = T.SourceParentObjectFusionAttributeID  and OI.PredicateType = 8 

	
	-- find any business terms related to target
	update T
	set T.[target] = OI.[subject], T.[targetid] = OI.[subjectid]--, T.targetintersectid = OI.ID
	from #maps T
		inner join [IntersectDetail] OI on OI.Subject <> 'FusionAttribute' and OI.Object = 'FusionAttribute' and OI.ObjectID = T.TargetParentObjectFusionAttributeID and OI.PredicateType = 8
		
	-- update the objects for each path to be the same	
	insert into #objectmap (MapID, [Object], [ObjectID])
		select T.ID, SO.[object], SO.[objectID]
		from #maps T		
		inner join #maps S on T.UltimateParentID = S.UltimateParentID
		inner join #objectmap SO on S.ID = SO.MapID
		left join #objectmap T_O on (T.ID = T_O.MapID and T_O.[object] is null);
	
	
	--take any sources with null targets find the next target

	WITH hierarchy (id, [target], [targetid], [source], [sourceid]) AS
	(
		SELECT id, [target], [targetid], [source], [sourceid]
		FROM #maps
		WHERE [parentid] is null

		UNION ALL

		SELECT mc.id, coalesce(mc.[target], mc.[source], gps.[target]) as [target], coalesce(mc.targetid, mc.sourceid, gps.targetid) as [targetid], coalesce(mc.[source], gps.[target], gps.[source]) as [source], coalesce(mc.sourceid, gps.targetid, gps.sourceid) as [targetid]
		FROM #maps mc
		JOIN hierarchy gps ON gps.id = mc.parentid
	)
	UPDATE T
	set T.[target] = cte.[target], T.[targetid] = cte.[targetid], T.[source] = cte.[source], T.[sourceid] = cte.[sourceid]
	from #maps T
	inner join 
		hierarchy cte
	on cte.id = T.id
	OPTION (MAXRECURSION 50)
			
	-- generate relationships for each unique object / source that dont exist

	update T
	set T.[sourceintersectid] = OI.ID
	from #objectmap T
		inner join #maps M on (T.MapID = M.ID)
		inner join [IntersectDetail] OI on OI.[Subject] = M.[Source] and OI.SubjectID = M.[SourceID] and OI.[Object] = T.[Object] and OI.[ObjectID] = T.[ObjectID];

	update T
	set T.[sourceintersectid] = OI.ID
	from #objectmap T
		inner join #maps M on (T.MapID = M.ID)
		inner join [IntersectDetail] OI on OI.[Object] = M.[Source] and OI.ObjectID = M.[SourceID] and OI.[Subject] = T.[Object] and OI.[SubjectID] = T.[ObjectID] and T.sourceintersectid is null
	
	-- add any missing relations to source / object
	insert into [intersect] (IntersectTypeID, [Subject], SubjectID, [Object], ObjectID, CreatedBy, CreatedOn, UpdatedBy, UpdatedOn, [Owner])
		select distinct
			(select top 1 i_t.ID from [intersecttype] i_t where (i_t.[object] = c_s.objecttype and i_t.[subject] = c_t.objecttype and i_t.objectid = c_s.objecttypeid and i_t.subjectid = c_t.objecttypeid))				
			,T.[Source]
			,T.[SourceID]
			,OM.[Object]
			,OM.[ObjectID]
			,0,getutcdate(),0,getutcdate(),'MARKIT LINEAGE'
		from #maps T
		inner join #objectmap OM on (T.ID = OM.MapID)
		inner join [cache].[objectdetails] c_s on (c_s.[object] = OM.[object] and c_s.[objectid] = OM.[objectid])
		inner join [cache].[objectdetails] c_t on (c_t.[object] = T.[source] and c_t.[objectid] = T.[sourceid])		
		where OM.sourceIntersectID is null;
	
	update OM
	set OM.[sourceintersectid] = OI.ID
	from #objectmap OM
		inner join #maps T on (OM.MapID = T.ID)		
		inner join [IntersectDetail] OI on OI.[Subject] = T.[Source] and OI.SubjectID = T.[SourceID] and OI.[Object] = OM.[Object] and OI.[ObjectID] = OM.[ObjectID] and OM.sourceintersectid is null;

	
	-- generate relationships for each unique object / target that dont exist	
	update OM
	set OM.[targetintersectid] = OI.ID
	from #objectmap OM
		inner join #maps T on (OM.MapID = T.ID)
		inner join [IntersectDetail] OI on OI.[Subject] = T.[Target] and OI.SubjectID = T.[TargetID] and OI.[Object] = OM.[Object] and OI.[ObjectID] = OM.[ObjectID]
		
	update OM
	set OM.[targetintersectid] = OI.ID
	from #objectmap OM
		inner join #maps T on (OM.MapID = T.ID)
		inner join [IntersectDetail] OI on OI.[Object] = T.[Target] and OI.ObjectID = T.[TargetID] and OI.[Subject] = OM.[Object] and OI.[SubjectID] = OM.[ObjectID] and OM.targetintersectid is null;

	-- add any missing relations to source / object
	insert into [intersect] (IntersectTypeID, [Subject], SubjectID, [Object], ObjectID, CreatedBy, CreatedOn, UpdatedBy, UpdatedOn, [Owner])
		select distinct
			(select top 1 i_t.ID from [intersecttype] i_t where (i_t.[object] = c_s.objecttype and i_t.[subject] = c_t.objecttype and i_t.objectid = c_s.objecttypeid and i_t.subjectid = c_t.objecttypeid))			
			,T.[target]
			,T.[targetID]
			,OM.[Object]
			,OM.[ObjectID]
			,0,getutcdate(),0,getutcdate(),'MARKIT LINEAGE'
		from #maps T
		inner join #objectmap OM on (T.ID = OM.MapID)
		inner join [cache].[objectdetails] c_s on (c_s.[object] = OM.[object] and c_s.[objectid] = OM.[objectid])
		inner join [cache].[objectdetails] c_t on (c_t.[object] = T.[target] and c_t.[objectid] = T.[targetid])		
		where OM.targetintersectid is null;
		
	update OM
	set OM.[targetintersectid] = OI.ID
	from #objectmap OM
		inner join #maps T on (OM.MapID = T.ID)
		inner join [IntersectDetail] OI on OI.[Subject] = T.[Target] and OI.SubjectID = T.[TargetID] and OI.[Object] = OM.[Object] and OI.[ObjectID] = OM.[ObjectID] and OM.targetintersectid is null;
	

	/*testing only!!*/			
--	select * from #maps order by [ultimateparentid], [level]
	/*end testing only*/

	print 'Removing any prior generated Markit Lineage map records';

	-- clear any previous values from map rule item map item table
	--delete from mapitem where [owner] = 'MARKIT LINEAGE';
	--delete from mapruleitem where [owner] = 'MARKIT LINEAGE';
	delete from mapruleitemmapitem where [owner] = 'MARKIT LINEAGE';

	print 'Inserting new map records';
	-- insert mapping data
	
	Declare @MapItemIDList Table(MapItemID int, sourceintersectid int, targetintersectid int);
	Declare @MapRuleItemIDList Table(MapRuleItemID int, MapID Int);
	
	-- load any existing map item instances
	update T
	set T.MapItemID = mi.ID
	from #objectmap T
		inner join mapitem mi on(T.sourceintersectid = mi.SourceIntersectID and T.targetintersectid = mi.TargetIntersectID and mi.[Owner] = 'MARKIT LINEAGE'); 

	-- insert map records
	MERGE
	INTO    mapitem mi
	USING   (			
			select distinct sourceintersectid, targetintersectid FROM #objectmap where (sourceintersectid is not null and targetintersectid is not null) and sourceintersectid != targetintersectid and mapitemid is null
			) S
	ON      (1 = 0)
	WHEN NOT MATCHED THEN
	INSERT  (SourceIntersectID, TargetIntersectID, CreatedBy, CreatedOn, UpdatedBy, UpdatedOn, [Owner])
	VALUES  (S.sourceintersectid, S.targetintersectid, 0, getutcdate(), 0, getutcdate(), 'MARKIT LINEAGE')
	OUTPUT  INSERTED.ID, S.sourceintersectid, S.targetintersectid into @MapItemIDList;

	--update map item id from main temp table
	update T
	set T.mapitemid = MI.MapItemID
	from #objectmap T
		inner join @MapItemIDList MI on (MI.sourceintersectid = T.sourceintersectid and MI.targetintersectid = T.targetintersectid)
		
	-- delete any mapitem records that are not in objectmap that are markit lineage
	delete from mapitem where [owner] = 'MARKIT LINEAGE' and id not in (select mapitemid from #objectmap);
	
	-- load id's of existing mapruleitems
	update T
	set T.mapruleitemid = S.id
	from #maps T
		inner join [dbo].[mapruleitem] S on (S.[owner] = 'MARKIT LINEAGE' and S.SourceFusionAttributeID = T.SourceFusionAttributeID and S.TargetFusionAttributeID = T.TargetFusionAttributeID);
	
	-- insert the mapruleitem records
	MERGE
	INTO    mapruleitem mri
	USING   (
			select SourceFusionAttributeID, TargetFusionAttributeID, ID from #maps where mapruleitemid is null
			) S
	ON      (1 = 0)
	WHEN NOT MATCHED THEN
	INSERT  (SourceFusionAttributeID, TargetFusionAttributeID, CreatedBy, CreatedOn, UpdatedBy, UpdatedOn, [Owner])
	VALUES  (S.SourceFusionAttributeID, S.TargetFusionAttributeID, 0, getutcdate(), 0, getutcdate(), 'MARKIT LINEAGE')
	OUTPUT  INSERTED.ID, S.ID into @MapRuleItemIDList;
	
	--update map rule item id from main temp table
	update T
	set T.MapRuleItemID = MI.MapRuleItemID
	from #maps T
		inner join @MapRuleItemIDList MI on (MI.MapID = T.ID);

	-- delete any mapitem records that are not in objectmap that are markit lineage
	delete from mapruleitem where [owner] = 'MARKIT LINEAGE' and id not in (select MapRuleItemID from #maps);
			
	--insert mapruleitemmapitem records
	insert into mapruleitemmapitem 
		(MapRuleItemID, MapItemID, [Owner])
		SELECT distinct M.MapRuleItemID, OM.MapItemID , 'MARKIT LINEAGE'
		FROM #maps M 
		inner join #objectmap OM on(M.ID = OM.MapID)
		where M.MapRuleItemID is not null and OM.MapItemID is not null;	

	declare @mapruleitemmapitemCount int;
	select @mapruleitemmapitemCount = count(1) from mapruleitemmapitem where [owner] = 'MARKIT LINEAGE'
	print 'Inserted [' + cast(@mapruleitemmapitemCount as varchar) + '] mapruleitemmapitem records';			

	declare @mapruleitemCount int;
	select @mapruleitemCount = count(1) from mapruleitem where [owner] = 'MARKIT LINEAGE'
	print 'Inserted [' + cast(@mapruleitemCount as varchar) + '] mapruleitem records';			

	declare @mapitemCount int;
	select @mapitemCount = count(1) from mapitem where [owner] = 'MARKIT LINEAGE'
	print 'Inserted [' + cast(@mapitemCount as varchar) + '] mapitem records';
			
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
		Declare @BBToFieldList Table(FieldFusionAttributeID int, StreamFusionAttributeID int, IntersectTypeID int);
		
		-- load the intersect id's for message stream to bb mnemonic	

		select	@fieldToBBIntersectTypeID = ID
			from	[IntersectType]
			where	Subject = 'FusionAttributeType' and 
					Object = 'FusionAttributeType' and 
					(
						( SubjectID = 205 and ObjectID = 301 ) 
					)

		if @fieldToBBIntersectTypeID is null
		begin
			raiserror('ERROR : UNABLE TO LOCATE INTERSECT TYPE IDS FOR EAGLE TO BLOOMBERG INTERSECT', 15, 1);
			return;
		end

		-- load into memory the id's that we need to add intersects for
		insert into @BBToFieldList
			select distinct	fa.id as 'fieldID', faBB.id as 'bbID', @fieldToBBIntersectTypeID
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
													--( I.SubjectID = faBB.ID and I.ObjectID = fa.ID ) OR
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
									--ID,
									'FusionAttribute' as Subject,									
									FieldFusionAttributeID as SubjectID,
									'FusionAttribute' as Object,
									StreamFusionAttributeID as ObjectID									
							FROM	@BBToFieldList
						) s
				ON      (
						s.IntersectTypeID = d.IntersectTypeID 
						and s.Subject = d.Subject and s.SubjectID = d.SubjectID 
						and s.Object = d.Object and s.ObjectID = d.ObjectID
						)
				WHEN NOT MATCHED THEN
				INSERT  (IntersectTypeID, Subject, SubjectID, Object, ObjectID, [Owner])
				VALUES  (s.IntersectTypeID, 'FusionAttribute', s.SubjectID, 'FusionAttribute', s.ObjectID, 'BB TO EAGLE');
			
	end;
END
GO

ALTER PROCEDURE [fusion].[Rules] 
AS
BEGIN
	SET NOCOUNT ON;

	declare @d datetime = getutcdate(),
			@r int = 0,
			@RuleID int,
			@FusionID int,
			@AttributeID int,
			@ParentAttributeID int,
			@ExecutionID int,
			@NumberOfRules int,			
			@NumberOfNewTaxonomies int,
			@NumberOfNewReferenceItems int,
			@NumberOfNewReferences int,
			@NumberOfNewArtifacts int,
			@NumberOfAttributesTotal int,
			@NumberOfNewRelations int
	
	set	@NumberOfRules = 0;	
	set @NumberOfNewTaxonomies = 0;
	set @NumberOfNewReferenceItems = 0;
	set @NumberOfNewReferences = 0;
	set @NumberOfNewArtifacts = 0;


	-- Get list of rules that should be processed -----------------------------------------------------------
	declare @applicableRules table (ID int)
	declare @lastPromotionRun datetime
	select	@lastPromotionRun = max(DateStarted) from fusion.RuleLog
		
	if(@lastPromotionRun is null)
	begin
	 set @lastPromotionRun = '1970-01-01';
	end;

	-- Rules engine should not run if there is a current job out there that has not completed and this job was started within the last day.
	if not exists (select 1 from fusion.RuleLog where DateCompleted is null and DateStarted > DATEADD(day,-1,CURRENT_TIMESTAMP))
	begin
		-- Get rules that should be processed based on latest changes.
		insert into @applicableRules
			select	distinct
					R.ID
			from	fusion.Execution E 
					inner join [fusion].[Rule] R on R.FusionID = E.FusionID
			where	R.[Enabled] = 1 
					and E.DateCompleted > @lastPromotionRun
					and (E.Adds + E.Updates + E.Deletes) > 0
		
		-- Get rules that have been modified or added since last run of rules engine.
		insert into @applicableRules
			select	ID 
			from	fusion.[Rule] 
			where	(
					UpdatedOn > @lastPromotionRun 
					and [Enabled] = 1 
					and ID not in (select ID from @applicableRules)
					)
	end
	--select * from @applicableRules
	---------------------------------------------------------------------------------------------------------
	
	if exists (select 1 from @applicableRules)
	begin
		--Log this run get a new id from the fusion.promotion table
		insert into [fusion].[RuleLog] ( DateStarted ) values ( CURRENT_TIMESTAMP)
		select @ExecutionID =  SCOPE_IDENTITY()

		IF OBJECT_ID('tempdb..#rules') IS NOT NULL
			DROP TABLE #rules;

		create table #rules (
			ID int identity,
			RuleID int,
			FusionID int,
			ObjectType varchar(50),
			ObjectID int
		);

		IF OBJECT_ID('tempdb..#attributes') IS NOT NULL
			DROP TABLE #attributes;

		create table #attributes (
			ID int identity,
			RuleID int,
			RuleStepID int,
			[Action] varchar(25),
			Object varchar(50),
			ObjectID int
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
			Value nvarchar(max)
		);
	
		CREATE NONCLUSTERED INDEX [CIX_TempFields] ON #fields ( ID ASC, RuleID ASC );

		IF OBJECT_ID('tempdb..#fieldValues') IS NOT NULL
			DROP TABLE #fieldValues;

		create table #fieldValues (
			ObjectType varchar(50), 
			ObjectID int, 
			FieldTypeID int, 
			Value nvarchar(max)
		);

		CREATE UNIQUE CLUSTERED INDEX PK_tempfieldValues ON #fieldValues ([ObjectType] ASC,[ObjectID] ASC,[FieldTypeID] ASC);

		insert into #rules
			select	R.ID,
					R.FusionID,
					R.ObjectType,
					R.ObjectID
			from	[fusion].[Rule] R
					inner join @applicableRules AR on AR.ID = R.ID

		declare	@currentID int,
				@maxID int

		set		@currentID = 1
		select	@maxID = MAX(ID) from #rules

		select @NumberOfRules = count(1) from #rules;

		--BEGIN: Determine the target fusion attributes to promote.
		declare @filters table (RowID int identity, [Sql] nvarchar(max))
		insert into @filters
			select	replace(RF.[Sql], 'select A.ID', 'select ' + cast(RF.RuleID as nvarchar) + ' as RuleID, ''' + replace(R.ObjectType, 'Type', '') + ''' as Object, A.ID as ObjectID') as [Sql]
			from	fusion.RuleFilter RF --inner join fusion.[Rule] R on R.ID = RF.RuleID
					inner join #rules R on R.RuleID = RF.RuleID

		DECLARE @attributes AS TABLE (RuleID int, Object VARCHAR(50), ObjectID int) 
		declare @i int = 1, @m int, @Sql nvarchar(max)
		select @m = max(RowID) from @filters
		while @i <= @m
		begin
			select @Sql = [Sql] from @filters where RowID = @i
			INSERT into @attributes EXECUTE  sp_executesql @Sql
			set @i = @i + 1
		end

		insert into #attributes
			select	distinct
					A.RuleID,
					S.ID as RuleStepID,
					S.[Action],
					A.Object,
					A.ObjectID
			from	@attributes A
					inner join fusion.RuleStep S on S.RuleID = A.RuleID
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
					inner join [fusion].[RuleStep] RS on M.RuleStepID = RS.ID and (M.SourceFieldName in ('ID', 'Name', 'TextPath') OR M.IsConstantValue = 1)
					inner join #attributes A on A.RuleID = RS.RuleID
					inner join FusionAttribute FA on FA.ID = A.ObjectID and A.Object = 'FusionAttribute' and FA.Deleted = 0

		insert into #fields
			select	A.ID,
					RS.RuleID,
					M.RuleStepID,
					M.SourceFieldName,
					M.SourceFieldTypeID,
					M.TargetFieldName,
					M.TargetFieldTypeID,
					F.FormattedValue
			from	[fusion].[RuleStepMapping] M
					inner join [fusion].[RuleStep] RS on M.RuleStepID = RS.ID and (M.SourceFieldName not in ('ID', 'Name', 'TextPath') AND M.IsConstantValue = 0)
					inner join #attributes A on A.RuleID = RS.RuleID and A.Object = 'FusionAttribute' --and A.AttributeID = M.SourceFieldTypeID
					left join FusionAttribute FA on FA.ID = A.ObjectID
					inner join FieldType FT on FT.ID = M.SourceFieldTypeID --and M.SourceFieldName = FT.Name
					inner join Field F on F.FieldTypeID = FT.ID and F.ObjectType = 'FusionAttribute' and (F.ObjectID = A.ObjectID OR F.ObjectID = FA.ParentID)


		--insert fusion query attribute fields
		insert into #fields
			select	A.ID,
					RS.RuleID,
					M.RuleStepID,
					M.SourceFieldName,
					M.SourceFieldTypeID,
					M.TargetFieldName,
					M.TargetFieldTypeID,
					case 
						when M.SourceFieldName = 'ID' then cast(A.ObjectID as nvarchar)
						when M.IsConstantValue = 1 then M.ConstantValue
					end
			from	[fusion].[RuleStepMapping] M
					inner join [fusion].[RuleStep] RS on M.RuleStepID = RS.ID and (M.SourceFieldName = 'ID' OR M.IsConstantValue = 1)
					inner join #attributes A on A.RuleID = RS.RuleID and A.Object = 'FusionQueryAttribute'

		insert into #fields
			select	A.ID,
					RS.RuleID,
					M.RuleStepID,
					M.SourceFieldName,
					M.SourceFieldTypeID,
					M.TargetFieldName,
					M.TargetFieldTypeID,
					F.FormattedValue
			from	[fusion].[RuleStepMapping] M
					inner join [fusion].[RuleStep] RS on M.RuleStepID = RS.ID and (M.SourceFieldName <> 'ID' AND M.IsConstantValue = 0)
					inner join #attributes A on A.RuleID = RS.RuleID and A.Object = 'FusionQueryAttribute' --and A.AttributeID = M.SourceFieldTypeID
					inner join Field F on F.ObjectType = 'FusionQueryAttribute' and F.ObjectID = A.ObjectID
					inner join FieldType FT on FT.ID = F.FieldTypeID and M.SourceFieldName = FT.Name

	--BEGIN: TESTING ---------------------------------------
	/*
	select * from #rules;
	select * from #attributes order by ID;
	select * from #fields order by ID;
	*/
	--drop table #attributes;
	--drop table #fields;
	--drop table #rules;

	--END: TESTING ------------------------------------------

		set		@currentID = 1
		select	@maxID = MAX(ID) from #attributes

		set @NumberOfAttributesTotal = @maxID;
	
		while (@currentID <= @maxID)
		begin
			begin try

				declare @AttributeTypeID int = null,
						@AttributeType varchar(25) = null,
						@RuleStepID int = null,
						@Action varchar(25) = null,
						@ResultObject varchar(50) = null,
						@ResultObjectID int = null

				declare @fields table (SourceFieldName nvarchar(250), SourceFieldTypeID int, TargetFieldName nvarchar(250), TargetFieldTypeID int, Value nvarchar(max))
				declare @settings table (Name nvarchar(100), Value nvarchar(250))
			

				select	@RuleID = R.RuleID,
						@RuleStepID = A.RuleStepID,
						@Action = A.[Action],
						@FusionID = R.FusionID,
						@AttributeTypeID = R.ObjectID,
						@AttributeID = A.ObjectID,
						@AttributeType = A.Object,
						@ResultObject = P.ObjectType,
						@ResultObjectID = P.ObjectID
				from	#rules R
						inner join #attributes A on A.RuleID = R.RuleID and A.ID = @currentID
						left join [Fusion].RulePromotion P on P.AttributeID = A.ObjectID and P.AttributeType = A.Object and P.RuleID = R.RuleID and P.RuleStepID = A.RuleStepID

				delete from @fields -- clear out previous fields
				--Load fields were are working with for this loop instance.
				insert into @fields
					select SourceFieldName, SourceFieldTypeID, TargetFieldName, TargetFieldTypeID, Value from #fields where ID = @currentID and RuleStepID = @RuleStepID

				delete from @settings -- clear out previous settings

				--Load settings were are working with for this loop instance.
				insert into @settings
					select Name, Value from [fusion].[RuleStepSetting] RSS inner join [fusion].[RuleStep] RS on (RSS.RuleStepID = RS.ID) where RS.RuleID = @RuleID and RS.ID = @RuleStepID
			
				--BEGIN: Promote action
				if lower(@Action) = 'promote'
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

					if @ObjectTypeToPromoteTo = 'ReferenceItemType' OR @ObjectTypeToPromoteTo = 'ReferenceItem'
					begin
						if exists(select 1 from @fields where TargetFieldName = 'Code')
						begin
							declare @code nvarchar(50) = null

							select @code = Value from @fields where TargetFieldName = 'Code'

							-- You are promoting Reference items to a specific Reference (list)
							set @ResultObject = 'ReferenceItem'

							if (@ResultObject is null and @ResultObjectID is null) or not exists(select 1 from ReferenceItem where ID = @ResultObjectID)
							begin
								select	@ResultObjectID = ID
								from	ReferenceItem
								where	ReferenceItemTypeID = @ObjectTypeIDToPromoteTo
										and lower(Code) = lower(@code)

								if not exists(select 1 from ReferenceItem where ID = @ResultObjectID)
								begin
									set @ResultObjectID = null
								end
							end
 
							if @ResultObjectID is null
							begin
								insert into ReferenceItem ( ReferenceItemTypeID, Code, CreatedOn, CreatedBy, UpdatedOn, UpdatedBy )
								values ( @ObjectTypeIDToPromoteTo, @code, getutcdate(), 0, getutcdate(), 0 )

								select @ResultObjectID =  SCOPE_IDENTITY()

								set @NumberOfNewReferenceItems = @NumberOfNewReferenceItems +1;
							end
						end -- END check if Code is a TargetField
					end	--END: IF ReferenceType
					else
					begin
						if exists(select 1 from @fields where TargetFieldName = 'Name')
						begin
							declare @name nvarchar(250) = null,
									@description nvarchar(max) = null

							select @name = Value from @fields where TargetFieldName = 'Name'
							select @description = coalesce(Value, '') from @fields where TargetFieldName = 'Description'

							--BEGIN: Find parent based on search type
							if @ParentObjectSearchType = 'Direct'
							begin
								set @ParentObject = @ParentSearchObject
								set @ParentObjectID = @ParentSearchObjectID
							end

							if @ParentObjectSearchType = 'FusionOwner'	--acts similarly to Direct
							begin
								set	@ParentObject = @ParentSearchObject
								set @ParentObjectID = @ParentSearchObjectID
							end

							if @ParentObjectSearchType = 'ResultFromStep'
							begin
								select	@ParentObject = ObjectType,
										@ParentObjectID = ObjectID
								from	[fusion].[RulePromotion]
								where	@ParentSearchObject = 'Step'
										and RuleID = @RuleID
										and RuleStepID = @ParentSearchObjectID
										and AttributeID = @AttributeID
										and AttributeType = @AttributeType
							end
							--END: Find parent based on search type

							--BEGIN: Determine object type to promote as
							if @ObjectTypeToPromoteTo = 'ArtifactType'
							begin
								set @ResultObject = 'Artifact'

								if (@ResultObjectID is null) or not exists(select 1 from Artifact where ID = @ResultObjectID)
								begin
									select	@ResultObjectID = ID
									from	Artifact
									where	ArtifactTypeID = @ObjectTypeIDToPromoteTo
											and lower(Name) = lower(@name)

									if not exists(select 1 from Artifact where ID = @ResultObjectID)
									begin
										set @ResultObjectID = null
									end
								end

								declare @modelTypeID int = null
								declare @taxonomyTypeValue nvarchar(250)

								select @taxonomyTypeValue = Value from @fields where TargetFieldName = 'TaxonomyTypeID'

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

												--DEBUGGING------------------------
												--select 
												--	@ParentObjectID as ParentObjectID,
												--	@ObjectTypeIDToPromoteTo as ObjectTypeIDToPromoteTo,
												--	@modelTypeID as modelTypeID, 
												--	@name as [name], 
												--	@description as [description], 
												--	@ResultObject as ResultObject, 
												--	@ResultObjectID as ResultObjectID,
												--    @RuleID as RuleID, @RuleStepID as RuleStepID;

												-- select * from @fields;
												------------------------------------

												if @ParentObjectSearchType is not null 
													begin
														if @ParentObject is not null and @ParentObjectID is not null
															begin
																insert into Artifact ( ParentID, ArtifactTypeID, TaxonomyTypeID, Name, Description, Status, UpdatedOn, UpdatedBy, CreatedOn )
																values ( @ParentObjectID, @ObjectTypeIDToPromoteTo, @modelTypeID, @name, @description, 'Draft', getutcdate(), 0, getutcdate() )

																select @ResultObjectID =  SCOPE_IDENTITY()
																set @NumberOfNewArtifacts = @NumberOfNewArtifacts +1;
															end
														--else
														--	begin
														--		--write error
														--	end
													end
												else
													begin
														insert into Artifact ( ArtifactTypeID, TaxonomyTypeID, Name, Description, Status, UpdatedOn, UpdatedBy, CreatedOn )
														values ( @ObjectTypeIDToPromoteTo, @modelTypeID, @name, @description, 'Draft', getutcdate(), 0, getutcdate() )

														select @ResultObjectID =  SCOPE_IDENTITY()
														set @NumberOfNewArtifacts = @NumberOfNewArtifacts +1;
													end
											end
									end
								else
									begin
										declare @testArtifactName nvarchar(250) = null,
												@testArtifactDescription nvarchar(max) = null,
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
													OR (@testArtifactParentID is null and @ParentObjectID is not null)
													OR (@testArtifactParentID is not null and @ParentObjectID is null)
													OR (@testArtifactTaxonomyTypeID <> @modelTypeID)
												begin
													update	Artifact
													set		Name = @name,
															Description = @description,
															ParentID = @ParentObjectID,
															TaxonomyTypeID = @modelTypeID,
															UpdatedOn = getutcdate(),
															UpdatedBy = 0
													where	ID = @ResultObjectID
												end
											end
									end
							end
							--END: IF ArtifactType

							if @ObjectTypeToPromoteTo = 'TaxonomyType'
							begin
								set @ResultObject = 'Taxonomy'

								if (@ResultObjectID is null) or not exists(select 1 from Taxonomy where ID = @ResultObjectID)
								begin
									select	@ResultObjectID = ID
									from	Taxonomy
									where	TaxonomyTypeID = @ObjectTypeIDToPromoteTo
											and ParentID = @ParentObjectID
											and lower(Name) = lower(@name)

									if not exists(select 1 from Taxonomy where ID = @ResultObjectID)
									begin
										set @ResultObjectID = null
									end
								end

								if @ResultObjectID is null
								begin
									insert into Taxonomy	( ParentID, TaxonomyTypeID, Name, Description, UpdatedOn, UpdatedBy )
									values					( @ParentObjectID, @ObjectTypeIDToPromoteTo, @name, @description, getutcdate(), 0 )

									select @ResultObjectID =  SCOPE_IDENTITY()

									set @NumberOfNewTaxonomies = @NumberOfNewTaxonomies +1;
								end
								else
								begin
									update	Taxonomy
									set		Name = @Name,
											Description = @Description,
											UpdatedOn = getutcdate(),
											UpdatedBy = 0--,
											--ParentID = @PromotionParentObjectID
									where	ID = @ResultObjectID
 								end
							end
							--END: IF TaxonomyType

							--END: Determine object type to promote as

						end -- END: Check to see if Target Field called NAME is present

					end --End of the ELSE that checks to see if referencelist, or not.

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
								if not exists(select 1 from @fields where SourceFieldTypeID = @FindFilterField)
									begin
										select	@FindFilterFieldValue = Value
										from	FieldWithRelation
										where	FieldTypeID = @FindFilterField
												and ObjectType = @AttributeType
												and ObjectID = @AttributeID
									end
								else
									begin
										select	@FindFilterFieldValue = Value
										from	@fields
										where	SourceFieldTypeID = @FindFilterField
									end
							end
						else
							begin
								if not exists(select 1 from @fields where SourceFieldName = 'Name')
									begin
										if @AttributeType = 'FusionQueryAttribute'
											begin
												select @FindFilterFieldValue = [Name]
												from FieldType FT
												where FT.ID = @AttributeID
											end
										else
											begin
												select	@FindFilterFieldValue = TextPath
												from	FusionAttribute
												where	ID = @AttributeID
											end
									end
								else
									begin
										select	@FindFilterFieldValue = Value
										from	@fields
										where	SourceFieldName = 'Name'
									end
							end
					
						if @FindFilterFieldValue is not null
						begin
							if @AttributeType = 'FusionQueryAttribute'
								begin
									select top 1 
											@ResultObject = 'FusionQueryAttribute',
											@ResultObjectID = ID
									from	FieldType
									where	@FindSearchObject = 'FusionQueryAttributeType'
											and ObjectID = @FindSearchObjectID
											and [Object] = 'FusionQueryAttributeType'
											and Name = @FindFilterFieldValue
								end
							else
								begin
									select	top 1
											@ResultObject = 'FusionAttribute',
											@ResultObjectID = ID
									from	FusionAttribute
									where	@FindSearchObject = 'FusionAttributeType'
											and FusionAttributeTypeID = @FindSearchObjectID
											and (SourceID = @FindFilterFieldValue or TextPath = @FindFilterFieldValue or Name = @FindFilterFieldValue)
								end


						end
					end

					--BEGIN: Find based on search type
					if @FindSearchType = 'FusionOwner'
					begin
						set	@ResultObject = 'Artifact'
						set @ResultObjectID = @FindSearchObjectID
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
							if @FindFilterField = -2
								begin
									select	@FindFilterFieldValue = Name
									from	FusionAttribute
									where	ID = @ParentAttributeID
								end
							else
								begin
									select	@FindFilterFieldValue = Value
									from	@fields
									where	SourceFieldName = 'Name'	
								end
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
								and rp.AttributeID = @AttributeID
								and rp.AttributeType = @AttributeType

					end

					if @FindSearchType = 'ResultFromStep' and @FindParent is null
					begin
						select	@ResultObject = ObjectType,
								@ResultObjectID = ObjectID
						from	[fusion].[RulePromotion]
						where	@FindSearchObject = 'Step'
								and RuleID = @RuleID
								and RuleStepID = @FindSearchObjectID
								and AttributeID = @AttributeID
								and AttributeType = @AttributeType
					end

					if @FindSearchType = 'Promotion' and @FindTargetField is null --by parent
					begin
						select	@ResultObject = ObjectType,
								@ResultObjectID = ObjectID
						from	[fusion].[RulePromotion]
						join	FusionAttribute A on A.ID = @AttributeID
						join	FusionAttribute AP on AP.ID = A.ParentID
						where	RuleStepID = @PromotionRuleStepID
								and AttributeID = AP.ID and AttributeType = 'FusionAttribute'
					end

					if @FindSearchType = 'Promotion' and @FindTargetField is not null -- by field
					begin
						select	@ResultObject = R.ObjectType, 
								@ResultObjectID = R.ObjectID 
						from	[fusion].[RulePromotion] R
						join	FusionAttribute SA on SA.ID = R.AttributeID
						join	Field SF on SF.ObjectType = 'FusionAttribute' 
								and SF.ObjectID = SA.ID 
								and SF.FieldTypeID = @FindFilterField
						join	FusionAttribute TA on TA.ID = @AttributeID
						join	Field TF on TF.ObjectType = 'FusionAttribute' 
								and TF.ObjectID = TA.ID 
								and TF.FieldTypeID = @FindTargetField
						where	R.RuleStepID = @PromotionRuleStepID 
								and SF.Value = TF.Value
								and R.AttributeType = 'FusionAttribute'
					end

					--END: Find based on search type
				end --END: Find Action


				--BEGIN: FindRelation Action
				if @Action = 'FindRelation'
				begin
					declare @IntersectTypeID		int = null,
							@SearchType				nvarchar(250) = null,
							@FindRelationObject		varchar(50) = null,
							@FindRelationObjectID	int = null

					select	@IntersectTypeID		= Value from @settings where Name = 'IntersectType'
					select	@SearchType				= Value from @settings where Name = 'Search'
					select	@FindSearchObjectID		= Value from @settings where Name = 'ID'

					--BEGIN: Find based on search type

					if @SearchType = 'ResultFromStep'
					begin
						select	@FindRelationObject = ObjectType,
								@FindRelationObjectID = ObjectID
						from	[fusion].[RulePromotion]
						where	RuleID = @RuleID
								and RuleStepID = @FindSearchObjectID
								and AttributeID = @AttributeID
								and AttributeType = @AttributeType
					end

					if @SearchType = 'Self'
					begin
						set @FindRelationObject = 'FusionAttribute'
						set @FindRelationObjectID = @AttributeID
					end

					if @FindRelationObject is not null and @FindRelationObjectID is not null
					begin
						select	top 1
								@ResultObject = case 
													when (Subject = @FindRelationObject and SubjectID = @FindRelationObjectID) then Object
													else Subject
												end,
								@ResultObjectID = case 
													when (Subject = @FindRelationObject and SubjectID = @FindRelationObjectID) then ObjectID
													else SubjectID
												end
						from	[Intersect]
						where	IntersectTypeID = @IntersectTypeID
								and (
										(Subject = @FindRelationObject and SubjectID = @FindRelationObjectID) 
										OR (Object = @FindRelationObject and ObjectID = @FindRelationObjectID)
									)
					end

					--END: Find based on search type

				end --END: FindRelation Action

				--BEGIN: Update Action
				if @Action = 'Update'
				begin

					declare @FromRuleStepID int;
					select @FromRuleStepID = Value from @settings where Name = 'SubjectID';

					if (@AttributeType is not null and @AttributeTypeID is not null)
					begin
						select
							@ResultObject = ObjectType,
							@ResultObjectID = ObjectID
						from
							fusion.RulePromotion
						where
							AttributeID = @AttributeID and AttributeType = @AttributeType and RuleStepID = @FromRuleStepID
					end

						--special fields
					if (@ResultObject is not null and @ResultObjectID is not null)
					begin
						--handle special fields
						if exists(select 1 from @fields where TargetFieldTypeID = 0)
						begin

							if @ResultObject = 'Artifact'
							begin
								update A
								set [Name] = f.Value
								from Artifact A
								join @fields F on F.TargetFieldTypeID = 0 and F.TargetFieldName = 'Name'
								where A.ID = @ResultObjectID;

								update A
								set [Description] = f.Value
								from Artifact A
								join @fields F on F.TargetFieldTypeID = 0 and F.TargetFieldName = 'Description'
								where A.ID = @ResultObjectID;

								update A
								set TextPath = f.Value
								from Artifact A
								join @fields F on F.TargetFieldTypeID = 0 and F.TargetFieldName = 'TextPath'
								where A.ID = @ResultObjectID;
							end 

							if @ResultObject = 'Taxonomy'
							begin
								update T
								set [Name] = f.Value
								from Taxonomy T
								join @fields F on F.TargetFieldTypeID = 0 and F.TargetFieldName = 'Name'
								where T.ID = @ResultObjectID;

								update T
								set [Description] = f.Value
								from Taxonomy T
								join @fields F on F.TargetFieldTypeID = 0 and F.TargetFieldName = 'Description'
								where T.ID = @ResultObjectID;

								update T
								set TextPath = f.Value
								from Taxonomy T
								join @fields F on F.TargetFieldTypeID = 0 and F.TargetFieldName = 'TextPath'
								where T.ID = @ResultObjectID;
							end
						end
					end --END: if result object not null

				end --END: Update Action


				--BEGIN: Lineage Action
				if @Action = 'Lineage'
				begin
					declare @SubjectSearchID int = null,
							@ObjectSearchID int = null,
							@Subject varchar(50) = null,
							@SubjectID int = null,
							@Object varchar(50) = null,
							@ObjectID int = null,

							@TechnicalSubjectSearchID int = null,
							@TechnicalObjectSearchID int = null,
							@RoleID int = null,

							@TechnicalSubject varchar(50) = null,
							@TechnicalSubjectID int  = null,
							@TechnicalObject varchar(50) = null,
							@TechnicalObjectID int  = null

					select	@SubjectSearchID			= Value from @settings where Name = 'SubjectID'
					select	@ObjectSearchID				= Value from @settings where Name = 'ObjectID'

					select	@TechnicalSubjectSearchID	= Value from @settings where Name = 'TechnicalSubjectID'
					select	@TechnicalObjectSearchID	= Value from @settings where Name = 'TechnicalObjectID'

					select	@RoleID						= Value from @settings where Name = 'Role'
				
					--BEGIN: Find subject based on search type, ALWAYS ResultFromStep
					select	@Subject = ObjectType,
							@SubjectID = ObjectID
					from	[Fusion].[RulePromotion]
					where	RuleID = @RuleID
							and RuleStepID = @SubjectSearchID
							and AttributeID = @AttributeID
							and AttributeType = @AttributeType
					--END: Find subject based on search type

					--BEGIN: Find object based on search type
					select	@Object = ObjectType,
							@ObjectID = ObjectID
					from	[fusion].[RulePromotion]
					where	RuleID = @RuleID
							and RuleStepID = @ObjectSearchID
							and AttributeID = @AttributeID
							and AttributeType = @AttributeType
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
					if @TechnicalSubjectSearchID is not null and @TechnicalObjectSearchID is not null
					begin
						select	@TechnicalSubject = ObjectType,
								@TechnicalSubjectID = ObjectID
						from	[Fusion].[RulePromotion]
						where	RuleID = @RuleID
								and RuleStepID = @TechnicalSubjectSearchID
								and AttributeID = @AttributeID
								and AttributeType = @AttributeType

						select	@TechnicalObject = ObjectType,
								@TechnicalObjectID = ObjectID
						from	[fusion].[RulePromotion]
						where	RuleID = @RuleID
								and RuleStepID = @TechnicalObjectSearchID
								and AttributeID = @AttributeID
								and AttributeType = @AttributeType
					end
					--END: Find object based on search type

					declare @MapRule table (ID int)

					--BEGIN: Add Map
					if	@TechnicalSubject = 'FusionAttribute' and @TechnicalSubjectID is not null 
						and @TechnicalObject = 'FusionAttribute' and @TechnicalObjectID is not null
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
						set	@R_Subject = 'Artifact'
						set @R_SubjectID = @R_ObjectSearchObjectID
					end

					if @R_SubjectSearchType = 'ResultFromStep'
					begin
						select	@R_Subject = ObjectType,
								@R_SubjectID = ObjectID
						from	[fusion].RulePromotion
						where	@R_SubjectSearchObject = 'Step'
								and RuleID = @RuleID
								and RuleStepID = @R_SubjectSearchObjectID
								and AttributeID = @AttributeID
								and AttributeType = @AttributeType
					end

					if @R_SubjectSearchType = 'Self'
					begin
						set @R_Subject = @AttributeType
						set @R_SubjectID = @AttributeID
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
						set	@R_Object = 'Artifact'
						set @R_ObjectID = @R_ObjectSearchObjectID
					end

					if @R_ObjectSearchType = 'ResultFromStep'
					begin
						select	@R_Object = ObjectType,
								@R_ObjectID = ObjectID
						from	[Fusion].[RulePromotion]
						where	@R_ObjectSearchObject = 'Step'
								and RuleID = @RuleID
								and RuleStepID = @R_ObjectSearchObjectID
								and AttributeID = @AttributeID
								and AttributeType = @AttributeType
					end

					if @R_ObjectSearchType = 'Self'
					begin
						set @R_Object = @AttributeType
						set @R_ObjectID = @AttributeID

					end
					--END: Find object based on search type


					--Check to see if we have all the required data to create the relationship.
					if @R_IntersectTypeID is not null and @R_Subject is not null and @R_SubjectID is not null and @R_Object is not null and @R_ObjectID is not null
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
										insert into [Intersect] (IntersectTypeID, Subject, SubjectID, Object, ObjectID, Deleted, CreatedBy, CreatedOn, UpdatedBy, UpdatedOn)
										values					(@R_IntersectTypeID, @R_Subject, @R_SubjectID, @R_Object, @R_ObjectID, 0, @r, @d, @r, @d)  

										select @R_IntersectID = SCOPE_IDENTITY()

										--cache logic
										insert into cache.[Object] ( [Object], [ObjectID], [ObjectType], [ObjectTypeID] ) values	( 'Intersect', @R_IntersectID, 'IntersectType', @R_IntersectTypeID );

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
							SELECT	@AttributeID as AttributeID,
									@AttributeType as AttributeType, 
									@ResultObject as ObjectType, 
									@ResultObjectID as ObjectID, 
									@RuleID as RuleID,
									0 as PromotedObjectTypeID,
									@RuleStepID as RuleStepID
							) as S
					ON		T.RuleID = S.RuleID
							and T.RuleStepID = S.RuleStepID 
							and T.AttributeID = S.AttributeID 
							and T.AttributeType = S.AttributeType
							and T.ObjectType = S.ObjectType 
							and T.ObjectID = S.ObjectID
					WHEN	MATCHED THEN
							UPDATE SET	T.RuleID = S.RuleID, 
										T.ObjectTypeID = S.PromotedObjectTypeID,
										T.UpdatedOn = getutcdate()
					WHEN	NOT MATCHED THEN
							INSERT (AttributeID, AttributeType, ObjectType, ObjectID, RuleID, RuleStepID, ObjectTypeID, CreatedOn, UpdatedOn) 
							VALUES (S.AttributeID, S.AttributeType, S.ObjectType, S.ObjectID, S.RuleID, S.RuleStepID, S.PromotedObjectTypeID, getutcdate(), getutcdate());


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
								@fieldValue nvarchar(max),
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
							if @lookupObjectType = 'ReferenceItemType'
								begin
									select	top 1
											@objectResultID = ID
									from	ReferenceItem
									where	ReferenceItemTypeID = @lookupObjectID and Code = @fieldValue
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
									set @fieldValue = cast(@objectResultID as nvarchar(max))
								end
						end									
						else
						begin
							-- This is a text value, so just insert it into the Field table for the promoted object.
							set @shouldInsert = 1
						end

						if @shouldInsert = 1
						begin
							--If not EXISTS (SELECT 1 FROM #fieldValues where ObjectType = @ResultObject and ObjectID = @ResultObjectID and FieldTypeID = @targetFieldTypeID) --avoid duplicates this happens in gmo
							begin
								begin try
									insert into #fieldValues (ObjectType, ObjectID, FieldTypeID, Value) values(@ResultObject, @ResultObjectID, @targetFieldTypeID, @fieldValue)
								end try
								Begin Catch
								--	print 'duplicate field value'
								End Catch
							end
						end
						
						-- Delete the field we just finished processing.
						delete @fields where TargetFieldTypeID = @targetFieldTypeID
					end --END: while

				end --END: IF when checking for promotiontype


			end try
			begin catch
				SELECT 
					ERROR_LINE() as ErrorLine
					,ERROR_NUMBER() AS ErrorNumber
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

		--Remove the previously promoted items that are no longer valid.
		delete	T
		from	[fusion].[RulePromotion] T
				left join #attributes S on S.RuleID = T.RuleID and S.RuleStepID = T.RuleStepID and S.Object = T.AttributeType and S.ObjectID = T.AttributeID
		where	S.ID is null;

		--Log this run done
		update	[fusion].[RuleLog]
		set		DateCompleted = CURRENT_TIMESTAMP, 
				[PromotedTaxonomies] = @NumberOfNewTaxonomies, 
				[PromotedDomainItems] = @NumberOfNewReferenceItems,  
				[PromotedDomains] = @NumberOfNewReferences,
				[PromotedArtifacts] = @NumberOfNewArtifacts,
				[TotalNewPromotions] = (@NumberOfNewTaxonomies + @NumberOfNewReferenceItems + @NumberOfNewReferences + @NumberOfNewArtifacts),
				[AttributesConsidered]= @NumberOfAttributesTotal,
				[NumberOfRules] = @NumberOfRules ,
				[RelationshipsAdded] = @NumberOfNewRelations
		where	ID = @ExecutionID;

	end -- close the IF check for any applicableRules rows in the temp table.
END
GO

alter procedure [tile].[GetObjectStatistics]
	@type varchar(50),
	@id int
AS
BEGIN
	declare @table table (Name nvarchar(250), Value varchar(250), [Group] varchar(25), Url varchar(250), MostRecent datetime, TypeID int)
	
	declare @ObjectScore varchar(250)

	insert into @table
		select NULL, count(1), 'Followers', '/overlays/' + @type + '/' + cast(@id as varchar(10)) + '/followers', max(datecreated),null
		from	Follow F
		inner join reporting.Global_Resource R on R.ResourceID = F.ResourceID
		where	F.ObjectType = @type and F.ObjectID = @id
	
	insert into @table
		select	NULL, count(1), 'Comments', '/overlays/' + @type + '/' + cast(@id as varchar(10)) + '/comments', max(datecreated),null
		from	Comment C
				inner join CommentRelation R	on R.CommentID = C.ID and C.ParentID is null
												and R.ObjectType = @type and R.ObjectID = @id
                                                and C.ParentID is null
												and C.IsDeleted = 0

	select	@ObjectScore = cast(round(avg(S.Value), 0) as int)	
	FROM	[Score] S
			inner join (
				select	max(ID) as ScoreID,
						Object,
						ObjectID,
						ScoreTypeID
				from	Score
				where		Object = @type and ObjectID = @id
				group by Object, ObjectID, ScoreTypeID
			) MS on MS.ScoreID = S.ID
	where	S.Object = @type and S.ObjectID = @id

	insert into @table values (null, @ObjectScore, 'Score', '/overlays/' + @type + '/' + cast(@id as varchar(10)) + '/score', null, null)

	if @type = 'Artifact'
	begin
		insert into @table 
			select		lower(T.Name),
						count(1),
						'Children',
						'/overlays/' + cast(@id as varchar(10)) + '/' + cast(T.ID as varchar(10)) + '/ChildArtifacts',
						max(A.createdon),
						T.ID
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
				max(w.datestarted),
				null
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

alter procedure [utility].[AddAuditEntry]
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
	if @Object = 'ReferenceItemType'	begin		select @objectName = Name from ReferenceItemType where ID = @ObjectID		end
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
	end
	
	-- Relevant ONLY to: Artifact, Fusion, FusionAttribute, Intersect, Taxonomy
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

	-- Relevant ONLY to: Artifact, FusionAttribute, Intersect, Taxonomy, Policy, Rule
	if @ActionObject = 'Intersect'
	begin
		select	@actionObjectTypeName = T.Name,
				@actionObjectName = O.Name 
		from	[Intersect] O
				inner join [IntersectType] T on T.ID = O.IntersectTypeID
		where	O.ID = @ActionObjectID
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

	-- Relevant ONLY to: ReferenceItem
	if @ActionObject = 'ReferenceItem'
	begin
		select	@actionObjectTypeName = T.Name,
				@actionObjectName = O.Code 
		from	ReferenceItem O
				inner join ReferenceItemType T on T.ID = O.ReferenceItemTypeID
		where	O.ID = @ActionObjectID

		insert into @tbl  select 0, 'Code', Code, 0, 0 from ReferenceItem where ID = @ActionObjectID
	end

	-- Relevant ONLY to: ReferenceItemType
	if @ActionObject = 'ReferenceItemType'
	begin
		select	@actionObjectTypeName = 'Reference Item Type',
				@actionObjectName = O.Name
		from	ReferenceItemType O
		where	O.ID = @ActionObjectID

		insert into @tbl  select 0, 'Name', Name, 0, 0 from ReferenceItemType where ID = @ActionObjectID
		insert into @tbl  select 0, 'Description', Description, 0, 0 from ReferenceItemType where ID = @ActionObjectID
		insert into @tbl  select 0, 'DisplayFormat', DisplayFormat, 0, 0 from ReferenceItemType where ID = @ActionObjectID
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

	-- Relevant ONLY to: Artifact, ArtifactType, Intersect, Policy, Rule, Taxonomy, TaxonomyType, Vocabulary
	if @ActionObject = 'Responsibility'
	begin
		select	@actionObjectTypeName = 'Responsibility',
				@actionObjectName = T.Name 
		from	Responsibility O
				inner join ResponsibilityType T on T.ID = O.ResponsibilityTypeID

		where	O.ID = @ActionObjectID

		insert into @tbl  select 0, 'Context', (
				select	D.Name + ': ' + I.Code + '; '
				from	ResponsibilityContextItem C
						inner join ReferenceItem I on C.ObjectType = 'ReferenceItem' and C.ObjectID = I.ID
						inner join ReferenceItemType D on D.ID = I.ReferenceItemTypeID
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
		insert into @tbl  select 0, 'Threshold', Threshold, 0, 0 from [Rule] where ID = @ActionObjectID
		insert into @tbl  select 0, 'Purpose', Purpose, 0, 0 from [Rule] where ID = @ActionObjectID
		insert into @tbl  select 0, 'Measurement', Measurement, 0, 0 from [Rule] where ID = @ActionObjectID
		insert into @tbl  select 0, 'Resolution', Resolution, 0, 0 from [Rule] where ID = @ActionObjectID
		insert into @tbl  select 0, 'Dimension', D.Name, 0, 0 from [Rule] R inner join RuleDimension D on D.ID = R.RuleDimensionID and R.ID = @ActionObjectID
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
	if @ActionObject in ('Artifact', 'Attribute', 'Event', 'Fusion', 'FusionAttribute', 'Lookup', 'Resource', 'Rule', 'Policy', 'Taxonomy') 
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

alter procedure [utility].[CalculateStatistics]
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
		

		---- EVENT METRIC CHECK
		--if (@CheckType = 9)
		--begin
		--	declare @ValidField nvarchar(250),-- = 'ValidCount',
		--			@InvalidField nvarchar(250),-- = 'InvalidCount',
		--			@Threshold decimal(9,2),-- = 0.10,
		--			@TotalValid float,
		--			@TotalInvalid float

		--	select	@ValidField = f.value('(ValidField/text())[1]', 'nvarchar(250)'),
		--			@InvalidField = f.value('(InvalidField/text())[1]', 'nvarchar(250)'),
		--			@Threshold = f.value('(Threshold/text())[1]', 'decimal(9,2)')
		--	from	@Configuration.nodes('/fields') as F(f)


		--	select	@TotalValid = sum(cast(V.ValidCount as int)),
		--			@TotalInvalid = sum(cast(I.InvalidCount as int))
		--	from	[Intersect] REL
		--			inner join [Rule] R on ((R.ID = REL.ObjectID and REL.Object = 'Rule') OR (R.ID = REL.SubjectID and REL.Subject = 'Rule')) --and R.RuleTypeID in (3,4)
		--			inner join EventGroup EG on EG.RuleID = R.ID
		--			inner join [Event] E on E.EventGroupID = EG.ID 
		--			inner join (
		--						select	R.ID,
		--								max(E.Date) as [Date]
		--						from	[Intersect] REL
		--								inner join [Rule] R on ((R.ID = REL.ObjectID and REL.Object = 'Rule') OR (R.ID = REL.SubjectID and REL.Subject = 'Rule')) --and R.RuleType in (3,4)
		--								inner join EventGroup EG on EG.RuleID = R.ID
		--								inner join [Event] E on E.EventGroupID = EG.ID
		--						group by R.ID					
		--						) F on F.ID = R.ID and F.[Date] = E.[Date]
		--			cross apply (
		--						select	Value as ValidCount
		--						from	FieldWithRelation
		--						where	ObjectType = 'Event' and ObjectID = E.ID and Name = @ValidField
		--						) V
		--			cross apply (
		--						select	Value as InvalidCount
		--						from	FieldWithRelation
		--						where	ObjectType = 'Event' and ObjectID = E.ID and Name = @InvalidField
		--						) I

		--	insert into #Statistics
		--		select	@StatisticTypeID as StatisticTypeID,
		--				R.[Object],
		--				R.ObjectID,
		--				case 
		--					when cast(@TotalInvalid / @TotalValid as decimal(9,2)) < @Threshold then @Score
		--					else 0
		--				end as Score
		--		from	@relations R
		--end

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

alter FUNCTION [utility].[GetIntersectTypesByType]
(	
	@type varchar(50),
	@id int
)
RETURNS TABLE 
AS
RETURN 
(
	select	'I' as type,
			cast(I.ID as varchar) + '|' +
			case 
				when (Subject = @type and SubjectID = @id) then I.Object + '|' + cast(I.ObjectID as varchar)
				else I.Subject + '|' + cast(I.SubjectID as varchar)
			end as value,
			case 
				when (Subject = @type and SubjectID = @id) then I.ObjectName + ' [' + coalesce(P.Name, 'relates') + '] ' + I.SubjectName
				else I.SubjectName + ' [' + coalesce(P.Inverse, 'related') + '] ' + I.ObjectName
			end as title
	from	IntersectTypeDetail I
			left join [Predicate] P on P.ID = I.PredicateID
	where	(Subject = @type and SubjectID = @id) or 
			(Object = @type and ObjectID = @id)
)
GO

alter FUNCTION [utility].[ObjectDetail]
(
--declare
	@type varchar(50), 
	@id int
--set @type = 'Domain'
--set @id = 1
)
RETURNS @tbl TABLE 
(
	ID int,
	Name nvarchar(250),
	TextPath nvarchar(2500),
	Description nvarchar(max),
	ParentID int null,
	ParentType nvarchar(250),
	Url nvarchar(2500),
	TypeID int,
	[Type] varchar(25),
	[TypeName] nvarchar(250),
	IconBackColor varchar(15),
	IconForeColor varchar(15),
	IconText varchar(15),
	Status nvarchar(25) null
) 
AS
BEGIN
	if @type = 'Artifact'
	begin
		insert into @tbl (	ID,		Name,	TextPath,	[Description],	ParentID,	ParentType, Url,													TypeID,				[Type],			TypeName, Status)
			SELECT			O.ID,	O.Name,	O.TextPath,	O.Description,	O.ParentID,	@type,		dbo.GenerateObjectUrl(@type, O.ArtifactTypeID, O.ID),	O.ArtifactTypeID,	'ArtifactType',	T.Name, O.Status
			FROM	Artifact O
					INNER JOIN ArtifactType T ON O.ArtifactTypeID = T.ID and O.ID = @id
	end

	if @type = 'ArtifactType'
	begin
		insert into @tbl (	ID,		Name,	TextPath,	[Description],	ParentID,	ParentType, Url,									TypeID, [Type], TypeName)
			SELECT			ID,		Name,	Name,		Description,	NULL,		NULL,		dbo.GenerateObjectUrl(@type, 0, ID),	ID,		@type,	'Artifact Type'
			FROM	ArtifactType O
			WHERE	ID = @id
	end

	if @type = 'Attribute'
	begin
		insert into @tbl (	ID,		Name,	TextPath,	[Description],	ParentID,	ParentType, Url,													TypeID,				[Type],				TypeName)
			SELECT			O.ID,	'',		'',			'',				O.ParentID,	@type,		D.Url,	O.AttributeTypeID,	'AttributeType',	T.Name
			FROM	[Attribute] O
					INNER JOIN AttributeType T ON O.AttributeTypeID = T.ID and O.ID = @id
					cross apply  utility.ObjectDetail(O.ObjectType, O.ObjectID) D
	end

	if @type = 'AttributeType'
	begin
		insert into @tbl (	ID,		Name,	TextPath,	[Description],	ParentID,	ParentType, Url,									TypeID, [Type], TypeName)
			SELECT			ID,		Name,	Name,		Description,	ParentID,	@type,		dbo.GenerateObjectUrl(@type, 0, ID),	ID,		@type,	'Attribute Type'
			FROM	AttributeType
			WHERE	ID = @id
	end

	if @type = 'Group'
	begin
		insert into @tbl (	ID,		Name,	TextPath,	[Description],	ParentID,	ParentType, Url,									TypeID, [Type], TypeName)
			SELECT			ID,		Name,	Name,		Description,	NULL,		NULL,		dbo.GenerateObjectUrl(@type, 0, ID),	0,		@type,	'Group'
			FROM	[Group]
			WHERE	ID = @id
	end

	if @type = 'Intersect'
	begin
		insert into @tbl (	ID,		Name,	TextPath,	[Description],	ParentID,	ParentType, Url,													TypeID,				[Type],				TypeName)
			SELECT			O.ID,	O.Name,	O.Name,		'',				NULL,		@type,		dbo.GenerateObjectUrl(@type, O.IntersectTypeID, O.ID),	O.IntersectTypeID,	'IntersectType',	T.Name
			FROM	[Intersect] O
					INNER JOIN IntersectType T ON O.IntersectTypeID = T.ID and O.ID = @id
	end

	if @type = 'IntersectType'
	begin
		insert into @tbl (	ID,		Name,	TextPath,	[Description],	ParentID,	ParentType, Url,									TypeID, [Type], TypeName)
			SELECT			ID,		Name,	Name,		'',				NULL,		NULL,		dbo.GenerateObjectUrl(@type, 0, ID),	ID,		@type,	'Intersect Type'
			FROM	IntersectType
			WHERE	ID = @id
	end

	if @type = 'Lookup'
	begin
		insert into @tbl (	ID,		Name,				TextPath,	[Description],	ParentID,	ParentType, Url,												TypeID,			[Type],			TypeName)
			SELECT			O.ID,	T.Name + ' Item',	T.Name,		'',				NULL,		NULL,		dbo.GenerateObjectUrl(@type, O.LookupTypeID, O.ID),	O.LookupTypeID,	'LookupType',	T.Name
			FROM	[Lookup] O
					INNER JOIN LookupType T ON O.LookupTypeID = T.ID AND O.ID = @id
	end

	if @type = 'LookupType'
	begin
		insert into @tbl (	ID,		Name,	TextPath,	[Description],	ParentID,	ParentType, Url,									TypeID, [Type], TypeName)
			SELECT			ID,		Name,	Name,		'',				0,			@type,		dbo.GenerateObjectUrl(@type, ID, 0),	ID,		@type,	'Lookup Type'
			FROM	LookupType O
			WHERE	ID = @id
	end

	if @type = 'Fusion'
	begin
		insert into @tbl (	ID,		Name,	TextPath,	[Description],	ParentID,	ParentType, Url,												TypeID,			[Type],			TypeName)
			SELECT			O.ID,	O.Name,	O.Name,		'',				NULL,		@type,		dbo.GenerateObjectUrl(@type, O.FusionTypeID, O.ID),	O.FusionTypeID,	'FusionType',	T.Name
			FROM	Fusion O
					INNER JOIN FusionType T ON O.FusionTypeID = T.ID and O.ID = @id
	end

	if @type = 'FusionType'
	begin
		insert into @tbl (	ID,		Name,	TextPath,	[Description],	ParentID,	ParentType, Url,									TypeID, [Type], TypeName)
			SELECT			ID,		Name,	Name,		'',				NULL,		NULL,		dbo.GenerateObjectUrl(@type, 0, ID),	ID,		@type,	'Fusion Type'
			FROM	FusionType O
			WHERE	ID = @id
	end

	if @type = 'FusionAttribute'
	begin
		insert into @tbl (	ID,		Name,		TextPath,	[Description],	ParentID,	ParentType, Url,	TypeID,						[Type],					TypeName)
			SELECT			O.ID,	coalesce(O.TextPath, O.Name),	O.TextPath,	'',				O.ParentID,	@type,		dbo.GenerateObjectUrl(@type, FT.ID, O.ID),
																											O.FusionAttributeTypeID,	'FusionAttributeType',	T.Name
			FROM	FusionAttribute O
					INNER JOIN FusionAttributeType T ON O.FusionAttributeTypeID = T.ID and O.ID = @id
					INNER JOIN FusionType FT ON T.FusionTypeID = FT.ID
	end

	if @type = 'FusionAttributeType'
	begin
		insert into @tbl (	ID, Name,		TextPath,	[Description],	ParentID,	ParentType, Url,									TypeID, [Type], TypeName)
			SELECT			ID,	O.Name,	O.TextPath,	'',				NULL,		NULL,		dbo.GenerateObjectUrl(@type, 0, ID),	ID,		@type,	'Fusion Attribute Type'
			FROM	FusionAttributeType O
			WHERE	ID = @id	
	end

	if @type = 'FusionQueryAttribute'
	begin
		insert into @tbl (	ID,		Name,		TextPath,	[Description],	ParentID,	ParentType, Url,	TypeID,						[Type],					TypeName)
			SELECT			O.ID,	O.DisplayValue,	O.DisplayValue,	'',				NULL,	@type,		dbo.GenerateObjectUrl(@type, 0, O.ID),
																											O.FusionQueryAttributeTypeID,	'FusionQueryAttributeType',	T.Name
			FROM	FusionQueryAttribute O
					INNER JOIN FusionQueryAttributeType T ON O.FusionQueryAttributeTypeID = T.ID and O.ID = @id					
	end
	
	if @type = 'FusionQueryAttributeType'
	begin
		insert into @tbl (	ID, Name,		TextPath,	[Description],	ParentID,	ParentType, Url,									TypeID, [Type], TypeName)
			SELECT			ID,	O.Name,	O.Name,	'',				NULL,		NULL,		dbo.GenerateObjectUrl(@type, 0, ID),	ID,		@type,	'Fusion Query Attribute Type'
			FROM	FusionQueryAttributeType O
			WHERE	ID = @id
	end

	if @type = 'Map'
	begin
		insert into @tbl (	ID,		Name,	TextPath,	[Description],	ParentID,	ParentType, Url,													TypeID,				[Type],			TypeName, Status)
			SELECT			O.ID,	O.Name,	O.Name,	NULL,	NULL,	NULL,		dbo.GenerateObjectUrl(@type, O.MapTypeID, O.ID),	O.MapTypeID,	'MapType',	T.Name, NULL
			FROM	Map O
					INNER JOIN MapType T ON O.MapTypeID = T.ID and O.ID = @id
	end

	if @type = 'MapType'
	begin
		insert into @tbl (	ID,		Name,	TextPath,	[Description],	ParentID,	ParentType, Url,													TypeID,				[Type],			TypeName, Status)
			SELECT			O.ID,	O.Name,	O.Name,	O.Description,	NULL,	NULL,		dbo.GenerateObjectUrl(@type, O.ID, O.ID),	O.ID,	'MapType',	Name, NULL
			FROM	MapType O
	end

	if @type = 'Policy'
	begin
		insert into @tbl (	ID,		Name,	TextPath,	[Description],	ParentID,	ParentType, Url,	TypeID,				[Type],			TypeName)
			SELECT			O.ID,	O.Name,	O.TextPath,	O.Description,	NULL,		@type,		dbo.GenerateObjectUrl(@type, T.ID, O.ID),	T.ID,	'PolicyType',	T.Name
			FROM	[Policy] O
					INNER JOIN PolicyType T ON O.PolicyTypeID = T.ID AND O.ID = @id
	end

	if @type = 'PolicyType'
	begin
		insert into @tbl (	ID,		Name,	TextPath,	[Description],	ParentID,	ParentType, Url,									TypeID,	[Type],	TypeName)
			SELECT			O.ID,	O.Name,	O.Name,		O.Description,	NULL,		NULL,		dbo.GenerateObjectUrl(@type, O.ID, O.ID),	C.ID,	@type,	C.Name
			FROM	PolicyType O
					inner join PolicyTypeClass C on C.ID = O.PolicyTypeClassID
			WHERE	O.ID = @id
	end

	if @type = 'ReferenceItem'
	begin
		insert into @tbl (	ID,	
							Name, TextPath, [Description],	
							ParentID, ParentType, 
							Url, 
							TypeID, [Type], TypeName)
			SELECT			O.ID,		
							O.DisplayValue, O.DisplayValue, NULL,
							NULL, NULL, 
							dbo.GenerateObjectUrl(@type, T.ID, O.ID),
							T.ID, 'ReferenceItemType', T.Name
			FROM	ReferenceItem O
					inner join ReferenceItemType T on T.ID = O.ReferenceItemTypeID and O.ID = @id
	end

	if @type = 'ReferenceItemType'
	begin
		insert into @tbl (	ID,		Name,	TextPath,	[Description],	ParentID,	ParentType, Url,									TypeID, [Type], TypeName)
			SELECT			ID,		Name,	Name,		Description,	NULL,		NULL,		dbo.GenerateObjectUrl(@type, 0, ID),	0,		@type,	'Reference Item Type'
			FROM	ReferenceItemType
			WHERE	ID = @id
	end

	if @type = 'Report'
	begin
		insert into @tbl (	ID,		Name,	TextPath,	[Description],	ParentID,	ParentType, Url,	TypeID,				[Type],			TypeName)
			SELECT			O.ID,	O.Name,	O.Name,	O.Description,	NULL,		@type,		'#',	0,	'Report',	'Report'
			FROM	Report O
			WHERE	O.ID = @id
	end

	if @type = 'Resource'
	begin
		insert into @tbl (ID, Name, Url, TypeID, [Type], TypeName)
			select	ResourceID, FirstName + ' ' + LastName, dbo.GenerateObjectUrl(@type, 1, @id), 1, 'ResourceType', 'Employee'
			from	reporting.Global_Resource 
			where	ResourceID = @id
	end

	if @type = 'ResponsibilityType'
	begin
		insert into @tbl (	ID, Name,	TextPath,	[Description],	ParentID,	ParentType, Url,									TypeID, [Type], TypeName)
			SELECT			ID,	O.Name,	NULL,		Description,	NULL,		NULL,		dbo.GenerateObjectUrl(@type, 0, ID),	ID,		@type,	'Responsibility Type'
			FROM	ResponsibilityType O
			WHERE	ID = @id
	end

	if @type = 'ResourceType'
	begin
		insert into @tbl (ID, Name, Url, TypeID, [Type], TypeName)
		values			(@id, 'Resource Type', '#/resources/administration', @id, @type, 'Resource Type')
	end

	if @type = 'Rule'
	begin
		insert into @tbl (	ID,		Name,	TextPath,	[Description],	ParentID,	ParentType, Url,	TypeID,				[Type],			TypeName, Status)
			SELECT			O.ID,	O.Name,	O.Name,	O.Description,	NULL,		@type,		dbo.GenerateObjectUrl(@type, 0, O.ID),	O.RuleTypeID,	'RuleType',	T.Name, case O.Status when 1 then 'Draft' when 2 then 'Active' else 'Inactive' end
			FROM	[Rule] O
					inner join RuleType T on T.ID = O.RuleTypeID
			WHERE	O.ID = @id
	end

	if @type = 'RuleImplementation'
	begin
		insert into @tbl (	ID,		Name,	TextPath,	[Description],	ParentID,	ParentType, Url,	TypeID,				[Type],			TypeName, Status)
			SELECT			O.ID,	coalesce(O.Name,'Implementation ' + cast(o.id as nvarchar)) ,	coalesce(O.Name,'Implementation ' + cast(o.id as nvarchar)),	null,	T.ID,		'Rule',		dbo.GenerateObjectUrl(@type, T.ID, O.ID),	T.RuleTypeID,	'RuleType',	T.Name, 'Active'
			FROM	[RuleImplementation] O
					inner join [Rule] T on T.ID = O.RuleID
			WHERE	O.ID = @id
	end

	if @type = 'RuleType'
	begin
		insert into @tbl (	ID,		Name,	TextPath,	[Description],	ParentID,	ParentType, Url,									TypeID,	[Type],	TypeName)
			SELECT			O.ID,	O.Name,	O.Name,		O.Description,	NULL,		NULL,		dbo.GenerateObjectUrl(@type, O.ID, O.ID),	O.ID,	@type,	O.Name
			FROM	RuleType O
			WHERE	O.ID = @id
	end

	if @type = 'StatisticType'
	begin
		insert into @tbl (	ID,		Name,	TextPath,	[Description],	ParentID,	ParentType, Url,									TypeID, [Type], TypeName)
			SELECT			ID,		Name,	Name,		Description,	NULL,		NULL,		dbo.GenerateObjectUrl(@type, 0, ID),	ID,		@type,	'Analytic Type'
			FROM	StatisticType O
			WHERE	ID = @id
	end

	if @type = 'Synonym'
	begin
		insert into @tbl (	ID,		Name,	TextPath,	[Description],	ParentID,	ParentType, Url,	TypeID,			[Type],		TypeName)
			SELECT			O.ID,	O.Name,	D.TextPath,	D.TypeName,		O.ObjectID,	O.Object,	D.Url,	O.PredicateID,	'Synonym',	P.Name
			FROM	[Synonym] O
					INNER JOIN [Predicate] P ON O.PredicateID = P.ID and O.ID = @id
					cross apply  utility.ObjectDetail(O.[Object], O.ObjectID) D
	end

	if @type = 'Taxonomy'
	begin
		insert into @tbl (	ID,		Name,	TextPath,	[Description],	ParentID,	ParentType, Url,													TypeID,				[Type],			TypeName)
			SELECT			O.ID,	O.Name,	O.TextPath,	O.Description,	O.ParentID,	@type,		dbo.GenerateObjectUrl(@type, O.TaxonomyTypeID, O.ID),	O.TaxonomyTypeID,	'TaxonomyType',	C.Name + ' Model'
			FROM	Taxonomy O
					INNER JOIN TaxonomyType T ON O.TaxonomyTypeID = T.ID AND O.ID = @id
					inner join TaxonomyTypeClass C on C.ID = T.TaxonomyTypeClassID
	end

	if @type = 'TaxonomyType'
	begin
		insert into @tbl (	ID,		Name,	TextPath,	[Description],	ParentID,	ParentType, Url,									TypeID,	[Type],	TypeName)
			SELECT			O.ID,	O.Name,	O.Name,		O.Description,	NULL,		NULL,		dbo.GenerateObjectUrl(@type, 0, O.ID),	C.ID,	@type,	C.Name
			FROM	TaxonomyType O
					inner join TaxonomyTypeClass C on C.ID = O.TaxonomyTypeClassID
			WHERE	O.ID = @id
	end

	update	T
	set		T.IconBackColor = coalesce(S.IconBackColor, '#000000'),
			T.IconForeColor = coalesce(S.IconForeColor, '#ffffff'),
			T.IconText =	--case @type
							--	when 'Taxonomy' then 'IM'
							--	when 'TaxonomyType' then 'IM'
								--else 
								COALESCE(S.IconText, 'leaf') 
							--end
	from	@tbl T
			left join ObjectStyle S ON S.ObjectType = T.[Type] and S.ObjectID = T.TypeID

	RETURN
END
GO

alter FUNCTION [dbo].[GenerateNgObjectUrl] 
(
	@Type varchar(50),
	@TypeID int,
	@ObjectID int = 0
)
RETURNS varchar(500)
AS
BEGIN
	DECLARE @Prefix varchar(5) = ''--'a/'
	DECLARE @Url varchar(500)
	SET @Url = @Prefix

	SET @Url = CASE @Type
		WHEN 'Artifact' THEN 'artifact/' +  + CAST(@TypeID as varchar) + '/' + CAST(@ObjectID as varchar)
		WHEN 'ArtifactType' THEN 'artifact/' + CAST(@TypeID as varchar)
		WHEN 'Domain' THEN 'domain/' +  + CAST(@TypeID as varchar) + '/' +  + CAST(@ObjectID as varchar)
		WHEN 'DomainType' THEN 'domain/' + CAST(@TypeID as varchar)
		WHEN 'ReferenceItem' THEN 'reference/' +  + CAST(@TypeID as varchar)-- + '/' +  + CAST(@ObjectID as varchar)
		WHEN 'ReferenceItemType' THEN 'reference/' + CAST(@TypeID as varchar)
		WHEN 'FusionAttribute' THEN 'fusion/fusionattribute/' + CAST(@TypeID as varchar) + '/' + CAST(@ObjectID as varchar)		
		WHEN 'Fusion' THEN 'fusion/' + CAST(@TypeID as varchar) + '/' + + CAST(@ObjectID as varchar)
		WHEN 'FusionType' THEN 'fusion/' + CAST(@TypeID as varchar)
		WHEN 'Group' THEN 'groups/' + CAST(@ObjectID as varchar)	
		WHEN 'Lookup' THEN 'admin/lookups/' + CAST(@TypeID as varchar) + '/' + + CAST(@ObjectID as varchar)
		WHEN 'LookupType' THEN 'admin/lookups/' + CAST(@TypeID as varchar)
		WHEN 'Policy' THEN 'policy/' + CAST(@TypeID as varchar(15)) + '/id/' + CAST(@ObjectID as varchar)
		WHEN 'PolicyType' THEN 'policy/' + CAST(@TypeID as varchar) + '/structure'		
		WHEN 'Resource' THEN 'resource/' + CAST(@ObjectID as varchar)
		WHEN 'ResourceType' THEN 'resource/list/' + CAST(@TypeID as varchar)
		WHEN 'Rule' THEN 'quality/rule/' + CAST(@TypeID as varchar) + '/' + CAST(@ObjectID as varchar)
		WHEN 'RuleType' THEN 'quality/rule/' + CAST(@TypeID as varchar)	
		WHEN 'Taxonomy' THEN 'model/' + CAST(@TypeID as varchar) + '/id/' + CAST(@ObjectID as varchar)
		WHEN 'TaxonomyType' THEN 'model/' + CAST(@ObjectID as varchar) + '/structure'		
	END

	SET @Url = @Prefix + @Url

	RETURN @Url
END
GO

alter FUNCTION [dbo].[GenerateObjectUrl] 
(
	@Type varchar(50),
	@TypeID int,
	@ObjectID int = 0
)
RETURNS varchar(500)
AS
BEGIN
	DECLARE @Prefix varchar(5) = ''--'a/'
	DECLARE @Url varchar(500)
	SET @Url = @Prefix

	SET @Url = CASE @Type
		WHEN 'Artifact' THEN 'artifact/' +  + CAST(@TypeID as varchar) + '/' + CAST(@ObjectID as varchar)
		WHEN 'ArtifactType' THEN 'artifact/' + CAST(@TypeID as varchar)
		WHEN 'Domain' THEN 'domain/' +  + CAST(@TypeID as varchar) + '/' +  + CAST(@ObjectID as varchar)
		WHEN 'DomainType' THEN 'domain/' + CAST(@TypeID as varchar)
		WHEN 'ReferenceItem' THEN 'reference/' +  + CAST(@TypeID as varchar)-- + '/' +  + CAST(@ObjectID as varchar)
		WHEN 'ReferenceItemType' THEN 'reference/' + CAST(@TypeID as varchar)
		WHEN 'FusionAttribute' THEN 'fusion/fusionattribute/' + CAST(@TypeID as varchar) + '/' + CAST(@ObjectID as varchar)		
		WHEN 'Fusion' THEN 'fusion/' + CAST(@TypeID as varchar) + '/' + + CAST(@ObjectID as varchar)
		WHEN 'FusionType' THEN 'fusion/' + CAST(@TypeID as varchar)
		WHEN 'Group' THEN 'groups/' + CAST(@ObjectID as varchar)	
		WHEN 'Lookup' THEN 'admin/lookups/' + CAST(@TypeID as varchar) + '/' + + CAST(@ObjectID as varchar)
		WHEN 'LookupType' THEN 'admin/lookups/' + CAST(@TypeID as varchar)
		WHEN 'Policy' THEN 'policy/' + CAST(@TypeID as varchar(15)) + '/id/' + CAST(@ObjectID as varchar)
		WHEN 'PolicyType' THEN 'policy/' + CAST(@TypeID as varchar) + '/structure'		
		WHEN 'Resource' THEN 'resource/' + CAST(@ObjectID as varchar)
		WHEN 'ResourceType' THEN 'resource/list/' + CAST(@TypeID as varchar)
		WHEN 'Rule' THEN 'quality/rule/' + CAST(@TypeID as varchar) + '/' + CAST(@ObjectID as varchar)
		WHEN 'RuleType' THEN 'quality/rule/' + CAST(@TypeID as varchar)	
		WHEN 'Taxonomy' THEN 'model/' + CAST(@TypeID as varchar) + '/id/' + CAST(@ObjectID as varchar)
		WHEN 'TaxonomyType' THEN 'model/' + CAST(@ObjectID as varchar) + '/structure'		
	END

	SET @Url = @Prefix + @Url

	RETURN @Url
END
GO

alter FUNCTION [utility].[CalculatePassed]
(
	@PassFraction decimal(4,3),
	@RuleImplementationID int
)
RETURNS bit
AS
BEGIN
	DECLARE @Passed bit

	select	top 1
			@Passed = case 
						when @PassFraction >= R.Threshold then cast(1 as bit)
						else cast(0 as bit)
					end
	from	RuleImplementation I
			inner join [Rule] R on I.ID = @RuleImplementationID and I.RuleID = R.ID

	RETURN @Passed
END
GO

alter FUNCTION [utility].[DeriveIntersectName] 
(	
	@id int
)
RETURNS nvarchar(500)
AS
BEGIN
	DECLARE @result nvarchar(500)

	SET @result =	(
					SELECT	COALESCE(SA.TextPath, SD.Name, SF.TextPath, SM.Name, SP.TextPath, SR.Name, ST.TextPath, SI.Name, '') + ' / ' + COALESCE(OA.TextPath, OD.Name, [OF].TextPath, OM.Name, OP.TextPath, [OR].Name, OT.TextPath, '')
					FROM	[Intersect] I
							left join Artifact SA on I.Subject = 'Artifact' and SA.ID = I.SubjectID
							left join Artifact OA on I.Object = 'Artifact' and OA.ID = I.ObjectID

							left join ReferenceItemType SD on I.Subject = 'ReferenceItemType' and SD.ID = I.SubjectID
							left join ReferenceItemType OD on I.Object = 'ReferenceItemType' and OD.ID = I.ObjectID

							left join [FusionAttribute] SF on I.Subject = 'FusionAttribute' and SF.ID = I.SubjectID
							left join [FusionAttribute] [OF] on I.Object = 'FusionAttribute' and [OF].ID = I.ObjectID


							left join [Intersect] SI on I.Subject = 'Intersect' and SI.ID = I.SubjectID

							left join [Map] SM on I.Subject = 'Map' and SM.ID = I.SubjectID
							left join [Map] OM on I.Object = 'Map' and OM.ID = I.ObjectID

							left join [Policy] SP on I.Subject = 'Policy' and SP.ID = I.SubjectID
							left join [Policy] OP on I.Object = 'Policy' and OP.ID = I.ObjectID

							left join [Rule] SR on I.Subject = 'Rule' and SR.ID = I.SubjectID
							left join [Rule] [OR] on I.Object = 'Rule' and [OR].ID = I.ObjectID

							left join [Taxonomy] ST on I.Subject = 'Taxonomy' and ST.ID = I.SubjectID
							left join [Taxonomy] OT on I.Object = 'Taxonomy' and OT.ID = I.ObjectID

					WHERE	I.ID = @id
					FOR XML PATH('')
					)

	RETURN @result
END
GO

alter  FUNCTION [utility].[DeriveIntersectTypeName] 
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
					SELECT	COALESCE(SA.Name, SD.Name, SF.TextPath, SM.Name, SP.Name, SR.Name, ST.Name, SI.Name, SQF.Name, '') + 
							' ' + coalesce(P.Name,'/') + ' ' + 
							COALESCE(OA.Name, OD.Name, [OF].TextPath, OM.Name, OP.Name, [OR].Name, OT.Name, OQF.Name, '')
					FROM	[IntersectType] I
							left join ArtifactType SA on I.Subject = 'ArtifactType' and SA.ID = I.SubjectID
							left join ArtifactType OA on I.Object = 'ArtifactType' and OA.ID = I.ObjectID

							left join ReferenceItemType SD on I.Subject = 'ReferenceItemType' and SD.ID = I.SubjectID
							left join ReferenceItemType OD on I.Object = 'ReferenceItemType' and OD.ID = I.ObjectID

							left join [FusionAttributeType] SF on I.Subject = 'FusionAttributeType' and SF.ID = I.SubjectID
							left join [FusionAttributeType] [OF] on I.Object = 'FusionAttributeType' and [OF].ID = I.ObjectID

							left join [FusionQueryAttributeType] SQF on I.Subject = 'FusionQueryAttributeType' and SQF.ID = I.SubjectID
							left join [FusionQueryAttributeType] [OQF] on I.Object = 'FusionQueryAttributeType' and [OQF].ID = I.ObjectID

							left join [IntersectType] SI on I.Subject = 'IntersectType' and SI.ID = I.SubjectID

							left join [MapType] SM on I.Subject = 'MapType' and SM.ID = I.SubjectID
							left join [MapType] OM on I.Object = 'MapType' and OM.ID = I.ObjectID

							left join [PolicyType] SP on I.Subject = 'PolicyType' and SP.ID = I.SubjectID
							left join [PolicyType] OP on I.Object = 'PolicyType' and OP.ID = I.ObjectID

							left join [RuleType] SR on I.Subject = 'RuleType' and SR.ID = I.SubjectID
							left join [RuleType] [OR] on I.Object = 'RuleType' and [OR].ID = I.ObjectID

							left join [TaxonomyType] ST on I.Subject = 'TaxonomyType' and ST.ID = I.SubjectID
							left join [TaxonomyType] OT on I.Object = 'TaxonomyType' and OT.ID = I.ObjectID

							left join [Predicate] P on P.ID = I.PredicateID
					WHERE	I.ID = @id
					FOR XML PATH('')
					)

	RETURN @result
END
GO

alter FUNCTION [utility].[GetFormattedFieldLookupValue]
(
	@Type varchar(25),
	@DisplayFormat nvarchar(250),
	@LookupObjectType varchar(25),
	@LookupObjectID int,
	@Value nvarchar(max)	
)
RETURNS nvarchar(max)
AS
BEGIN
	declare @formattedValue nvarchar(max)
	
	if @Value is null
	begin
		return null
	end

	if @LookupObjectType is null
	begin
		set @formattedValue  = @Value

		if @Type = 'Link' OR @Type = 'UncLink'
		begin
			declare @linkName nvarchar(max),
					@linkUrl nvarchar(max)

			if charindex('|', @Value, 1) > 1
				begin
					SELECT @linkName = SUBSTRING(@Value, 1, PATINDEX('%|%', @Value)-1)
					SELECT @linkUrl = SUBSTRING(@Value, PATINDEX('%|%', @Value)+1, LEN(@Value))

					set @formattedValue = '<a href="' + @linkUrl + '" target="_blank">' + @linkName + '</a>'
				end
			else
				begin
					if @Value <> '' AND @Value <> '|' AND @Value IS NOT NULL
						begin
							if LEFT(@Value, 1) = '|'
								begin
									--no name, default to url
									set @formattedValue = '<a href="' + SUBSTRING(@Value,2, LEN(@Value)) + '" target="_blank">' + SUBSTRING(@Value,2, LEN(@Value)) + '</a>'
								end
							else
								begin
									set @formattedValue = '<a href="' + @Value + '" target="_blank">' + @Value + '</a>'
								end
						end
					else
						begin
							set @formattedValue = null
						end
				end
		end

	end	
	else
	begin
		if @LookupObjectType = 'ReferenceItemType'
		begin
			select @formattedValue = Name from ReferenceItemType where id = @Value;		
		end
		else
		begin
			declare @tokens table(ID int identity(1,1), Token nvarchar(100), Field nvarchar(100))
			declare @fieldValues table(Field nvarchar(100), Value nvarchar(max), LookupObjectType nvarchar(250), LookupObjectID int, LookupDisplayFormat nvarchar(250))

			set @formattedValue = @DisplayFormat
	
			while patindex('%{%',@formattedValue) > 0
			 begin
				declare @txt nvarchar(100) = SUBSTRING(@formattedValue, patindex('%{%',@formattedValue), PATINDEX('%}%', @formattedValue))
				insert into @tokens Values (@txt, REPLACE(REPLACE(@txt,'{',''),'}',''))
				set @formattedValue = replace(@formattedValue, @txt, '')
			end

			insert into @fieldValues
				select	distinct
						V.Name,
						V.Value,
						V.LookupObjectType,
						V.LookupObjectID,
						V.LookupDisplayFormat
				from	(
						SELECT	ID,
								Name,
								'Artifact' as ObjectType
						FROM	ArtifactType
						WHERE	@LookupObjectType = 'Artifact' and ID = @LookupObjectID
						UNION
						SELECT	ID,
								Name,
								'Lookup' as ObjectType
						FROM	[LookupType]
						WHERE	@LookupObjectType = 'Lookup' and ID = @LookupObjectID
						UNION
						SELECT	ID,
								Name,
								'ReferenceItem' as ObjectType
						FROM	[ReferenceItemType]
						WHERE	@LookupObjectType = 'ReferenceItem' and ID = @LookupObjectID
						UNION										
						SELECT	1 as ID,
								'User' as Name,
								'Resource' as ObjectType
						WHERE	@LookupObjectType = 'Resource'-- and ID = @LookupObjectID
						UNION
						SELECT	ID,
								Name,
								'Taxonomy' as ObjectType
						FROM	TaxonomyType
						WHERE	@LookupObjectType = 'Taxonomy' and ID = @LookupObjectID
						) L
						outer apply (

									SELECT	IT.Name,
											[IF].Value,
											[IT].LookupObjectType,
											COALESCE([IT].LookupObjectID, 0) as LookupObjectID,
											[IT].LookupDisplayFormat
									FROM	Field [IF]
											inner join FieldType IT ON [IF].FieldTypeID = IT.ID 
																	and [IF].ObjectType = L.ObjectType
																	and [IF].ObjectID = case 
																							when dbo.IsInteger(@Value) = 1 then @Value
																							else 0
																						end
								
									UNION

									SELECT	P.FieldName as Name,
											p.FieldValue as Value,
											NULL as LookupObjectType,
											NULL as LookupObjectID,
											NULL as LookupDisplayFormat
									FROM	(
											SELECT	ID as AID,
													CAST(ID as nvarchar(max)) as ID,
													CAST(Name as nvarchar(max)) as Name,
													CAST(Description as nvarchar(max)) as Description,
													CAST(TextPath as nvarchar(max)) as TextPath
											FROM	Artifact A
											WHERE	A.ID = CAST(@Value as int)
													and L.ObjectType = 'Artifact'
											) A
											unpivot	(
													FieldValue for FieldName in (ID, Name, Description, TextPath)
													) p

									UNION

									SELECT	P.FieldName as Name,
											p.FieldValue as Value,
											NULL as LookupObjectType,
											NULL as LookupObjectID,
											NULL as LookupDisplayFormat
									FROM	(
											SELECT	ID,
													CAST(Name as nvarchar(max)) as Name,
													CAST(Description as nvarchar(max)) as Description,
													CAST(TextPath as nvarchar(max)) as TextPath
											FROM	Taxonomy A
											WHERE	A.ID = CAST(@Value as int)
													and L.ObjectType = 'Taxonomy'
											) A
											unpivot	(
													FieldValue for FieldName in (Name, Description, TextPath)
													) p

									UNION

									SELECT	P.FieldName as Name,
											p.FieldValue as Value,
											NULL as LookupObjectType,
											NULL as LookupObjectID,
											NULL as LookupDisplayFormat
									FROM	(
											SELECT	ID,
													CAST(Code as nvarchar(max)) as Code
											FROM	ReferenceItem A
											WHERE	A.ID = @Value
													and L.ObjectType = 'ReferenceItem'
											) A
											unpivot	(
													FieldValue for FieldName in (Code)
													) p

									UNION

									SELECT	P.FieldName as Name,
											p.FieldValue as Value,
											NULL as LookupObjectType,
											NULL as LookupObjectID,
											NULL as LookupDisplayFormat
									FROM	(
											SELECT	ID,
													CAST(Name as nvarchar(max)) as Name,
													CAST(Description as nvarchar(max)) as Description
											FROM	ReferenceItemType A
											WHERE	A.ID = @Value
													and L.ObjectType = 'ReferenceItemType'
											) A
											unpivot	(
													FieldValue for FieldName in (Name, Description)
													) p

									UNION

									SELECT	P.FieldName as Name,
											p.FieldValue as Value,
											NULL as LookupObjectType,
											NULL as LookupObjectID,
											NULL as LookupDisplayFormat
									FROM	(
											SELECT	ResourceID as ID,
													CAST(FirstName as nvarchar(max)) as FirstName,
													CAST(LastName as nvarchar(max)) as LastName,
													CAST(Email as nvarchar(max)) as Email
											FROM	reporting.Global_Resource A
											WHERE	A.ResourceID = @Value
													and L.ObjectType = 'Resource'
											) A
											unpivot	(
													FieldValue for FieldName in (FirstName, LastName, Email)
													) p
									) V

			declare @current int,
					@max int

			set @current = 1
			select @max = Max(ID) from @tokens

			set @formattedValue = @DisplayFormat

			while(@current <= @max)
			begin
				declare @currentToken nvarchar(100) = null,
						@currentField nvarchar(100) = null,
						@currentValue nvarchar(max) = null,
						@lkpType nvarchar(250) = null, 
						@lkpID int = null, 
						@lkpFormat nvarchar(250) = null

				select	@currentField = Field, 
						@currentToken = Token 
				from	@tokens
				where	ID = @current

				select	@currentValue = Value,
						@lkpType = LookupObjectType,
						@lkpID = LookupObjectID,
						@lkpFormat = LookupDisplayFormat
				from	@fieldValues 
				where	Field = @currentField

				if @currentValue is not null
				begin
					if @lookupObjectType is not null and @lkpID is not null
					begin
						select @currentValue = utility.GetFormattedFieldLookupValue(@Type, @lkpFormat, @lkpType, @lkpID, @currentValue)
					end

					SET @formattedValue = REPLACE(@formattedValue, @currentToken, @currentValue)
				end
				else
				begin
					SET @formattedValue = REPLACE(@formattedValue, @currentToken, '')
				end

				SET @current = @current + 1
			end
		end
	end

	return @formattedValue
END
GO

CREATE TABLE [analytics].[Action] (
    [ID]    INT          IDENTITY (1, 1) NOT NULL,
    [Value] VARCHAR (50) NOT NULL,
    CONSTRAINT [PK_Analytics_Action] PRIMARY KEY NONCLUSTERED ([ID] ASC)
);
GO

CREATE CLUSTERED INDEX [CIX_Analytics_Action]
    ON [analytics].[Action]([Value] ASC);
GO

CREATE TABLE [analytics].[BrowserLanguage] (
    [ID]    INT           IDENTITY (1, 1) NOT NULL,
    [Value] VARCHAR (500) NOT NULL,
    CONSTRAINT [PK_Analytics_BrowserLanguage] PRIMARY KEY NONCLUSTERED ([ID] ASC)
);
GO

CREATE CLUSTERED INDEX [CIX_Analytics_BrowserLanguage]
    ON [analytics].[BrowserLanguage]([Value] ASC);
GO

CREATE TABLE [analytics].[Host] (
    [ID]    INT          IDENTITY (1, 1) NOT NULL,
    [Value] VARCHAR (50) NOT NULL,
    CONSTRAINT [PK_Analytics_Host] PRIMARY KEY NONCLUSTERED ([ID] ASC)
);
GO

CREATE CLUSTERED INDEX [CIX_Analytics_Host]
    ON [analytics].[Host]([Value] ASC);
GO

CREATE TABLE [analytics].[Ip] (
    [ID]    INT           IDENTITY (1, 1) NOT NULL,
    [Value] VARCHAR (100) NOT NULL,
    CONSTRAINT [PK_Analytics_Ip] PRIMARY KEY NONCLUSTERED ([ID] ASC)
);
GO

CREATE CLUSTERED INDEX [CIX_Analytics_Ip]
    ON [analytics].[Ip]([Value] ASC);
GO

CREATE TABLE [analytics].[Object] (
    [ID]    INT          IDENTITY (1, 1) NOT NULL,
    [Value] VARCHAR (50) NOT NULL,
    CONSTRAINT [PK_Analytics_Object] PRIMARY KEY NONCLUSTERED ([ID] ASC)
);
GO

CREATE CLUSTERED INDEX [CIX_Analytics_Object]
    ON [analytics].[Object]([Value] ASC);
GO

CREATE TABLE [analytics].[Statistic] (
    [ID]                UNIQUEIDENTIFIER CONSTRAINT [DF_Analytics_Statistic_ID] DEFAULT (newid()) NOT NULL,
    [Object]            INT              NOT NULL,
    [ObjectID]          INT              NOT NULL,
    [IpID]              INT              NOT NULL,
    [UserAgentID]       INT              NOT NULL,
    [HostID]            INT              NOT NULL,
    [BrowserLanguageID] INT              NOT NULL,
    [ActionID]          SMALLINT         NOT NULL,
    [ResourceID]        INT              CONSTRAINT [DF_Analytics_Statistic_ResourceID] DEFAULT ((0)) NOT NULL,
    [Timestamp]         DATETIME         NOT NULL,
    CONSTRAINT [PK_Analytics_Statistic] PRIMARY KEY NONCLUSTERED ([ID] ASC)
);
GO

CREATE TABLE [analytics].[UserAgent] (
    [ID]    INT            IDENTITY (1, 1) NOT NULL,
    [Value] NVARCHAR (250) NULL,
    CONSTRAINT [PK_Analytics_UserAgent] PRIMARY KEY NONCLUSTERED ([ID] ASC)
);
GO

CREATE CLUSTERED INDEX [CIX_Analytics_UserAgent]
    ON [analytics].[UserAgent]([Value] ASC);
GO


CREATE TABLE [dbo].[LineageDefault] (
    [Object]   VARCHAR (50) NOT NULL,
    [ObjectID] INT          NOT NULL,
    [UsageOn]  BIT          NOT NULL,
    CONSTRAINT [PK_LineageDefault] PRIMARY KEY CLUSTERED ([Object] ASC, [ObjectID] ASC)
);
GO

CREATE TABLE [dbo].[Organization] (
    [ID]   INT            IDENTITY (1, 1) NOT NULL,
    [Name] NVARCHAR (250) NOT NULL,
    CONSTRAINT [PK_Organization] PRIMARY KEY CLUSTERED ([ID] ASC)
);
GO

CREATE TABLE [dbo].[Contract] (
    [ID]             INT            IDENTITY (1, 1) NOT NULL,
    [ContractType]   INT            NOT NULL,
    [OrganizationID] INT            NULL,
    [Title]          NVARCHAR (250) NOT NULL,
    [Body]           NVARCHAR (MAX) NOT NULL,
    CONSTRAINT [PK_Contract] PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [FK_Contract_Organization] FOREIGN KEY ([OrganizationID]) REFERENCES [dbo].[Organization] ([ID])
);
GO


CREATE TABLE [dbo].[OrganizationDomain] (
    [ID]             INT            IDENTITY (1, 1) NOT NULL,
    [OrganizationID] INT            NOT NULL,
    [Domain]         NVARCHAR (500) NOT NULL,
    [Accepted]       BIT            NULL,
    [AcceptedBy]     INT            NULL,
    [DateAccepted]   DATETIME       NULL,
    CONSTRAINT [PK_OrganizationDomain] PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [FK_OrganizationDomain_Organization] FOREIGN KEY ([OrganizationID]) REFERENCES [dbo].[Organization] ([ID])
);
GO

CREATE TABLE [dbo].[OrganizationInvitation] (
    [ID]             INT            IDENTITY (1, 1) NOT NULL,
    [OrganizationID] INT            NOT NULL,
    [Email]          NVARCHAR (500) NOT NULL,
    CONSTRAINT [PK_OrganizationInvitation] PRIMARY KEY NONCLUSTERED ([ID] ASC),
    CONSTRAINT [FK_OrganizationInvitation_Organization] FOREIGN KEY ([OrganizationID]) REFERENCES [dbo].[Organization] ([ID])
);
GO

CREATE TABLE [dbo].[OrganizationResource] (
    [OrganizationID] INT      NOT NULL,
    [ResourceID]     INT      NOT NULL,
    [Accepted]       BIT      NULL,
    [DateAccepted]   DATETIME NULL,
    CONSTRAINT [PK_OrganizationResource] PRIMARY KEY CLUSTERED ([OrganizationID] ASC, [ResourceID] ASC),
    CONSTRAINT [FK_OrganizationResource_Organization] FOREIGN KEY ([OrganizationID]) REFERENCES [dbo].[Organization] ([ID])
);
GO

CREATE TABLE [dbo].[ReportResponsibility] (
    [ID]                   INT IDENTITY (1, 1) NOT NULL,
    [ReportID]             INT NOT NULL,
    [ResponsibilityTypeID] INT NOT NULL,
    PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [FK_ReportResponsibility_Report] FOREIGN KEY ([ReportID]) REFERENCES [dbo].[Report] ([ID]) ON DELETE CASCADE
);
GO

CREATE TABLE [dbo].[ResourcePasswordReset] (
    [ID]         UNIQUEIDENTIFIER DEFAULT (newid()) NOT NULL,
    [ResourceID] INT              NOT NULL,
    [CreateDate] DATETIME         NOT NULL,
    PRIMARY KEY CLUSTERED ([ID] ASC)
);
GO

CREATE TABLE [dbo].[RuleImplementation] (
    [ID]        INT            IDENTITY (1, 1) NOT NULL,
    [RuleID]    INT            NOT NULL,
    [SourceID]  VARCHAR (250)  NULL,
    [SourceUri] VARCHAR (2500) NULL,
    [Name]      NVARCHAR (250) NULL,
    [CreatedOn] DATETIME       NULL,
    [CreatedBy] INT            NULL,
    [UpdatedOn] DATETIME       NULL,
    [UpdatedBy] INT            NULL,
    CONSTRAINT [PK_RuleImplementation] PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [FK_RuleImplementation_Rule] FOREIGN KEY ([RuleID]) REFERENCES [dbo].[Rule] ([ID])
);
GO

CREATE TABLE [dbo].[RuleResultFusionAttribute] (
    [ID]                BIGINT          IDENTITY (1, 1) NOT NULL,
    [RuleResultID]      INT             NOT NULL,
    [FusionAttribute]   NVARCHAR (2500) NOT NULL,
    [FusionAttributeID] INT             NULL,
    CONSTRAINT [PK_RuleResultFusionAttribute] PRIMARY KEY NONCLUSTERED ([ID] ASC),
    CONSTRAINT [FK_RuleResultFusionAttribute_RuleResult] FOREIGN KEY ([RuleResultID]) REFERENCES [dbo].[RuleResult] ([ID])
);
GO

CREATE TABLE [dbo].[ScoreType] (
    [ID]          INT             IDENTITY (1, 1) NOT NULL,
    [Name]        NVARCHAR (250)  NOT NULL,
    [Description] NVARCHAR (4000) NULL,
    [CreatedOn]   DATETIME        CONSTRAINT [DF_ScoreType_CreatedOn] DEFAULT (getutcdate()) NULL,
    [CreatedBy]   INT             CONSTRAINT [DF_ScoreType_CreatedBy] DEFAULT ((0)) NULL,
    [UpdatedOn]   DATETIME        CONSTRAINT [DF_ScoreType_UpdatedOn] DEFAULT (getutcdate()) NULL,
    [UpdatedBy]   INT             CONSTRAINT [DF_ScoreType_UpdatedBy] DEFAULT ((0)) NULL,
    CONSTRAINT [PK_ScoreType] PRIMARY KEY CLUSTERED ([ID] ASC)
);
GO

insert into ScoreType (Name) values ('Governance Score')
go

CREATE TABLE [dbo].[ScoreTypeMetric] (
    [ID]            INT             IDENTITY (1, 1) NOT NULL,
    [ScoreTypeID]   INT             NOT NULL,
    [Object]        VARCHAR (50)    NULL,
    [ObjectID]      INT             NULL,
    [Name]          NVARCHAR (250)  NOT NULL,
    [Description]   NVARCHAR (4000) NULL,
    [CheckType]     INT             NOT NULL,
    [Configuration] XML             NULL,
    [CreatedOn]     DATETIME        NULL,
    [CreatedBy]     INT             NULL,
    [UpdatedOn]     DATETIME        NULL,
    [UpdatedBy]     INT             NULL,
    [MaximumScore]  INT             NOT NULL,
    [Deleted]       BIT             CONSTRAINT [DF_ScoreTypeMetric_Deleted] DEFAULT ((0)) NOT NULL,
    CONSTRAINT [PK_ScoreTypeMetric] PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [CK_ScoreTypeMetric_MaximumScore] CHECK ([MaximumScore]>=(0) AND [MaximumScore]<=(999)),
    CONSTRAINT [FK_ScoreTypeMetric_ScoreType] FOREIGN KEY ([ScoreTypeID]) REFERENCES [dbo].[ScoreType] ([ID])
);
GO

alter table ScoreTypeMetric add OldID int
--delete Statistic where StatisticTypeID in (
--delete 
insert into ScoreTypeMetric
	select	1 as ScoreTypeID,
			Object, ObjectID, Name, Description, CheckType,
			Configuration, 
			UpdatedOn as CreatedOn, UpdatedBy as CreatedBy,
			UpdatedOn, UpdatedBy,
			Score as MaximumScore,
			0 as Deleted,
			ID as OldID
	from	StatisticType
--where	CheckType = 9
--)

update	T
set		T.OldID = S.ID,
		T.Deleted = 0
from	ScoreTypeMetric T
		inner join StatisticType S on S.Object = T.Object and S.ObjectID = T.ObjectID and S.Name = T.Name
		

CREATE TABLE [dbo].[ScoreTypeMetricVersion] (
    [ID]                INT             IDENTITY (1, 1) NOT NULL,
    [ScoreTypeMetricID] INT             NOT NULL,
    [Name]              NVARCHAR (250)  NOT NULL,
    [Description]       NVARCHAR (4000) NULL,
    [CheckType]         INT             NOT NULL,
    [Configuration]     XML             NULL,
    [CreatedOn]         DATETIME        NULL,
    [CreatedBy]         INT             NULL,
    [UpdatedOn]         DATETIME        NULL,
    [UpdatedBy]         INT             NULL,
    [MaximumScore]      INT             NOT NULL,
    CONSTRAINT [PK_ScoreTypeMetricVersion] PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [CK_ScoreTypeMetricVersion_MaximumScore] CHECK ([MaximumScore]>=(0) AND [MaximumScore]<=(999)),
    CONSTRAINT [FK_ScoreTypeMetricVersion_ScoreTypeMetric] FOREIGN KEY ([ScoreTypeMetricID]) REFERENCES [dbo].[ScoreTypeMetric] ([ID])
);
GO

CREATE TABLE [dbo].[Score] (
    [ID]          BIGINT       IDENTITY (1, 1) NOT NULL,
    [Object]      VARCHAR (50) NOT NULL,
    [ObjectID]    INT          NOT NULL,
    [ScoreTypeID] INT          NOT NULL,
    [Date]        DATE         NOT NULL,
    [Value]       INT          NOT NULL,
    CONSTRAINT [PK_Score] PRIMARY KEY NONCLUSTERED ([ID] ASC),
    CONSTRAINT [CK_Score_Value] CHECK ([Value]>=(0) AND [Value]<=(100)),
    CONSTRAINT [FK_Score_ScoreType] FOREIGN KEY ([ScoreTypeID]) REFERENCES [dbo].[ScoreType] ([ID])
);
GO

CREATE CLUSTERED INDEX [CIX_Score] ON [dbo].[Score]([Object] ASC, [ObjectID] ASC, [ScoreTypeID] ASC, [Date] DESC);
GO


CREATE TABLE [dbo].[ScoreMetric] (
    [ScoreID]                  BIGINT         NOT NULL,
    [ScoreTypeMetricVersionID] INT            NOT NULL,
    [Value]                    DECIMAL (6, 3) NOT NULL,
    CONSTRAINT [PK_ScoreMetric] PRIMARY KEY CLUSTERED ([ScoreID] ASC, [ScoreTypeMetricVersionID] ASC),
    CONSTRAINT [FK_ScoreMetric_Score] FOREIGN KEY ([ScoreID]) REFERENCES [dbo].[Score] ([ID]),
    CONSTRAINT [FK_ScoreMetric_ScoreTypeMetricVersion] FOREIGN KEY ([ScoreTypeMetricVersionID]) REFERENCES [dbo].[ScoreTypeMetricVersion] ([ID])
);
GO


CREATE TABLE [dbo].[TestExternalMetric] (
    [Object]          VARCHAR (50)   NOT NULL,
    [ObjectID]        INT            NOT NULL,
    [MetricName]      NVARCHAR (250) NOT NULL,
    [MetricVersionID] INT            NULL,
    [Score]           DECIMAL (6, 3) NOT NULL
);
GO


create table #scores (
	DateStart date not null,
	DateEnd date not null,
	[Object] varchar(50) NOT NULL,
	[ObjectID] [int] NOT NULL,
)
go

insert into #scores
	select		min(cast(DateStart as date)) as DateStart,
				max(cast(DateEnd as date)) as DateEnd,
				ObjectType as Object,
				ObjectID
	from		[Statistic] S
	group by	ObjectType, ObjectID
go

CREATE NONCLUSTERED INDEX [IX_TempScore_DateStart] ON #scores ( DateStart ASC )
GO
CREATE NONCLUSTERED INDEX [IX_TempScore_DateEnd] ON #scores ( DateEnd ASC )
GO


insert into [Score]
	select	S.Object,
			S.ObjectID,
			1 as ScoreTypeID,
			D.Date,
			0 as Value
	from	#scores S
			inner join [reporting].[Dates] D on D.Date between DateStart and DateEnd --and S.Object = 'Artifact' and S.ObjectID = 1202
	order by D.Date

insert into ScoreMetric
	select		S.ID as ScoreID,
				M.ID as ScoreTypeMetricID,
				A.Score as Value
	from		Score S
				inner join Statistic A on A.ObjectType = S.Object and A.ObjectID = S.ObjectID
				inner join ScoreTypeMetric M on M.OldID = A.StatisticTypeID and S.Date between A.DateStart and A.DateEnd
	--where		S.Object = 'Artifact' 
	--			and S.ObjectID = 1202
	--order by	S.Date
go

alter table ScoreTypeMetric drop column OldID
go

INSERT INTO [dbo].[ScoreTypeMetricVersion]
           ([ScoreTypeMetricID]
           ,[Name]
           ,[Description]
           ,[CheckType]
           ,[Configuration]
           ,[CreatedOn]
           ,[CreatedBy]
           ,[UpdatedOn]
           ,[UpdatedBy]
           ,[MaximumScore])
select	ID, Name, Description, CheckType, Configuration, CreatedOn, CreatedBy, UpdatedOn, UpdatedBy, MaximumScore 
from	ScoreTypeMetric
go

update	T
set		T.Value = case 
					when S.Value > 100 then 100
					else S.Value
				end
from	Score T
inner join	(
			select		CAST(ROUND( (SUM(Value) / SUM(V.MaximumScore)) * 100, 0) as int) as Value,
						ScoreID
			from		ScoreMetric SM
						inner join ScoreTypeMetricVersion V on V.ID = SM.ScoreTypeMetricVersionID
			group by	ScoreID
			) S on S.ScoreID = T.ID
go
	
	
CREATE TABLE [fusion].[RuleFilter] (
    [ID]     INT            IDENTITY (1, 1) NOT NULL,
    [RuleID] INT            NULL,
    [Name]   NVARCHAR (250) NOT NULL,
    [Fields] XML            NULL,
    [Sql]    NVARCHAR (MAX) NULL,
    [All]    BIT            CONSTRAINT [DF_FusionRuleFilter_All] DEFAULT ((0)) NOT NULL,
    CONSTRAINT [PK_RuleFilter] PRIMARY KEY NONCLUSTERED ([ID] ASC),
    CONSTRAINT [FK_FusionRuleFilter_FusionRule] FOREIGN KEY ([RuleID]) REFERENCES [fusion].[Rule] ([ID]) ON DELETE CASCADE
);
GO

CREATE TABLE [workflow].[ItemAssignment] (
    [ID]               BIGINT       IDENTITY (1, 1) NOT NULL,
    [ItemID]           BIGINT       NOT NULL,
    [ResourceObject]   VARCHAR (50) NOT NULL,
    [ResourceObjectID] INT          NOT NULL,
    [CreatedBy]        INT          NOT NULL,
    [CreatedOn]        DATETIME     NOT NULL,
    [UpdatedBy]        INT          NOT NULL,
    [UpdatedOn]        DATETIME     NOT NULL,
    CONSTRAINT [PK_WorkflowItemAssignment] PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [FK_WorkflowItemAssignment_WorkflowItem] FOREIGN KEY ([ItemID]) REFERENCES [workflow].[Item] ([ID]) ON DELETE CASCADE
);
GO

CREATE TABLE [workflow].[TaskProcedure] (
    [ID]             INT            IDENTITY (1, 1) NOT NULL,
    [Name]           NVARCHAR (250) NOT NULL,
    [Procedure]      VARCHAR (1000) NOT NULL,
    [PassObjectInfo] BIT            NOT NULL,
    [UpdatedBy]      INT            NOT NULL,
    [UpdatedOn]      DATETIME       NOT NULL
);
GO

CREATE view analytics.StatisticDetail
as
select	S.ID,
		S.ResourceID,
		RE.FirstName + ' ' + RE.LastName as ResourceName,
		'/resource/' + cast(RE.ResourceID as varchar) as ResourceUrl,
		S.[Timestamp],
		A.Value as [Action],
		B.Value as BrowserLanguage,
		H.Value as [Host],
		I.Value as [Ip],
		O.Value as [Object],
		S.ObjectID,
		coalesce(OA.TextPath, OAT.Name, ORE.Name) as ObjectName,
		U.Value as UserAgent
from	[analytics].Statistic S
		inner join [analytics].[Action] A on A.ID = S.ActionID
		inner join [analytics].[BrowserLanguage] B on B.ID = S.BrowserLanguageID
		inner join [analytics].[Host] H on H.ID = S.HostID
		inner join [analytics].[Ip] I on I.ID = S.IpID
		inner join [analytics].[Object] O on O.ID = S.Object
		inner join [analytics].[UserAgent] U on U.ID = S.UserAgentID
		left join reporting.Global_Resource RE on S.ResourceID = RE.ResourceID
		left join Artifact OA on O.Value = 'Artifact' and OA.ID = S.ObjectID
		left join ArtifactType OAT on O.Value = 'ArtifactType' and OAT.ID = S.ObjectID
		left join Report ORE on O.Value = 'Report' and ORE.ID = S.ObjectID
GO

CREATE view OrganizationDomainDetail
as
select	O.ID,
		O.OrganizationID,
		coalesce(ORG.Name, 'Global') as OrganizationName,
		O.Domain,
		O.Accepted,
		O.AcceptedBy,
		R.FirstName as AcceptedByFirstName,
		R.LastName as AcceptedByLastName,
		R.DateLastLoggedIn as AcceptedByDateLastLoggedIn,
		R.Email as AcceptedByEmail,
		R.Status as AcceptedByStatus,
		O.DateAccepted
from	OrganizationDomain O
		left join Organization ORG on ORG.ID = O.OrganizationID
		left join reporting.Global_Resource R on R.ResourceID = O.AcceptedBy
GO

CREATE view OrganizationInvitationDetail
as
select	O.ID,
		O.OrganizationID,
		coalesce(ORG.Name, 'Global') as OrganizationName,
		O.Email
from	OrganizationInvitation O
		left join Organization ORG on ORG.ID = O.OrganizationID
GO

create view OrganizationResourceDetail
as
select	O.OrganizationID,
		coalesce(ORG.Name, 'Global') as OrganizationName,
		O.ResourceID,
		R.FirstName,
		R.LastName,
		R.DateLastLoggedIn,
		R.Email,
		R.Status,
		O.Accepted,
		O.DateAccepted
from	OrganizationResource O
		left join Organization ORG on ORG.ID = O.OrganizationID
		inner join reporting.Global_Resource R on R.ResourceID = O.ResourceID
GO

drop table #scores
go

CREATE PROCEDURE [analytics].AddStatistic 
	@Object varchar(50),
	@ObjectID int,
	@Ip varchar(100),
	@UserAgent varchar(250),
	@Host varchar(50),
	@BrowserLanguage varchar(500),
	@Action varchar(50),
	@ResourceID int,
	@Timestamp datetime
AS
BEGIN
	SET NOCOUNT ON;

	declare @IpLookupID int,
			@UserAgentLookupID int,
			@ObjectLookupID int,
			@HostLookupID int,
			@BrowserLanguageLookupID int,
			@ActionLookupID int

	select	@ActionLookupID = ID			from [analytics].[Action]			where [Value] = @Action
	select	@BrowserLanguageLookupID = ID	from [analytics].[BrowserLanguage]	where [Value] = @BrowserLanguage
	select	@HostLookupID = ID				from [analytics].[Host]				where [Value] = @Host
	select	@IpLookupID = ID				from [analytics].[Ip]				where [Value] = @Ip
	select	@ObjectLookupID = ID			from [analytics].[Object]			where [Value] = @Object
	select	@UserAgentLookupID = ID			from [analytics].[UserAgent]		where [Value] = @UserAgent

	if @ActionLookupID is null
	begin
		insert into [analytics].[Action] values (@Action)
		set @ActionLookupID = SCOPE_IDENTITY()
	end

	if @BrowserLanguageLookupID is null
	begin
		insert into [analytics].[BrowserLanguage] values (@BrowserLanguage)
		set @BrowserLanguageLookupID = SCOPE_IDENTITY()
	end

	if @HostLookupID is null
	begin
		insert into [analytics].[Host] values (@Host)
		set @HostLookupID = SCOPE_IDENTITY()
	end

	if @IpLookupID is null
	begin
		insert into [analytics].[Ip] values (@Ip)
		set @IpLookupID = SCOPE_IDENTITY()
	end

	if @ObjectLookupID is null
	begin
		insert into [analytics].[Object] values (@Object)
		set @ObjectLookupID = SCOPE_IDENTITY()
	end

	if @UserAgentLookupID is null
	begin
		insert into [analytics].[UserAgent] values (@UserAgent)
		set @UserAgentLookupID = SCOPE_IDENTITY()
	end

	INSERT INTO [analytics].Statistic values (
		newid(), 
		@ObjectLookupID,
		@ObjectID,
		@IpLookupID,
		@UserAgentLookupID,
		@HostLookupID,
		@BrowserLanguageLookupID,
		@ActionLookupID,
		@ResourceID,
		@Timestamp
	)
END
GO

create procedure [fusion].[ClearMarkitMapLineageData]
as
begin
	delete from mapitem where [owner] = 'MARKIT LINEAGE';
	delete from mapruleitem where [owner] = 'MARKIT LINEAGE';
	delete from mapruleitemmapitem where [owner] = 'MARKIT LINEAGE';
	delete from [intersect] where [owner] = 'MARKIT LINEAGE';
end
GO

CREATE procedure [fusion].[GenerateEagleBusinessLineageData]
	@eagleOwnerArtifact int = 974209
as
begin
	SET NOCOUNT, ANSI_PADDING ON;
	SET ANSI_WARNINGS ON;

	declare @bloombergFusionTypeId int = 8;
	--declare @eagleOwnerArtifact int = 974209;

	IF OBJECT_ID('tempdb..#maps') IS NOT NULL
		DROP TABLE #maps;

	create table #maps (	
		ID int identity primary key,
		MapItemID int,	
		MapRuleItemID int,		
		FusionAttributeID int,
		FusionAttributeTypeID int,
		Object varchar(50),
		ObjectID int,
		ObjectArtifactTypeID int,
		ObjectFusionID int,
		ObjectOwnerArtifactID int,
		SourceIntersectID int,
		TargetIntersectID int,
		IsRelatedToBloomberg int
	);


	insert into #maps
	(FusionAttributeID, ObjectID, Object, ObjectFusionID, FusionAttributeTypeID, MapRuleItemID)
	select 
		mri.SourceFusionAttributeID as 'FusionAttributeID', 
		i.ObjectID as 'ObjectID',
		i.Object as 'Object'
		,f.FusionID
		,f.FusionAttributeTypeID
		,mri.id
	from 
		mapruleitem  mri
		inner join [intersect] i on (mri.SourceFusionAttributeID = i.SubjectID and i.Subject = 'FusionAttribute' and i.Object = 'Artifact')		
		inner join [dbo].[fusionattribute] f on f.id = mri.SourceFusionAttributeID and f.deleted = 0
	where 
		mri.[owner] = 'EAGLE LINEAGE'
	union
	select 
		mri.SourceFusionAttributeID as 'FusionAttributeID', 
		i.SubjectID as 'ObjectID',
		i.Subject as 'Object'
		,f.FusionID
		,f.FusionAttributeTypeID
		,mri.id
	from 
		mapruleitem  mri
		inner join [intersect] i on (mri.SourceFusionAttributeID = i.ObjectID and i.Object = 'FusionAttribute' and i.Subject = 'Artifact')
		inner join [dbo].[fusionattribute] f on f.id = mri.SourceFusionAttributeID and f.deleted = 0
	where 
		mri.[owner] = 'EAGLE LINEAGE'
	union
	select 
		mri.TargetFusionAttributeID as 'FusionAttributeID', 
		i.ObjectID as 'objectID',
		i.Object as 'Object'
		,f.FusionID
		,f.FusionAttributeTypeID
		,mri.id
	from 
		mapruleitem  mri
		inner join [intersect] i on (mri.TargetFusionAttributeID = i.SubjectID and i.Subject = 'FusionAttribute' and i.Object = 'Artifact')
		inner join [dbo].[fusionattribute] f on f.id = mri.TargetFusionAttributeID and f.deleted = 0
	where 
		mri.[owner] = 'EAGLE LINEAGE'
	union
	select 
		mri.TargetFusionAttributeID as 'FusionAttributeID', 
		i.SubjectID as 'ObjectID',
		i.Subject as 'Object'
		,f.FusionID
		,f.FusionAttributeTypeID
		,mri.id
	from 
		mapruleitem  mri
		inner join [intersect] i on (mri.TargetFusionAttributeID = i.ObjectID and i.Object = 'FusionAttribute' and i.Subject = 'Artifact')
		inner join [dbo].[fusionattribute] f on f.id = mri.TargetFusionAttributeID and f.deleted = 0
	where 
		mri.[owner] = 'EAGLE LINEAGE';

	
	-- set the owner artifact of the fusion ids
	update T
	 set T.ObjectOwnerArtifactID = f.ArtifactID
	 from #maps T
		inner join [dbo].[fusionowner] f on f.fusionid = T.objectfusionid


	update #maps set IsRelatedToBloomberg = 1 where fusionattributetypeid = 301;
	--delete the items that start with bloomberg
	--delete from #maps where fusionattributetypeid = 301;
	--update #maps set IsRelatedToBloomberg = 1 where fusionattributetypeid = 301;
	-- for owners objects that are not fusionattributetypeid 301 we need to see if they connect to bloomberg
	-- use source to target until we find end or 301

	declare @tFusionPoints table (	ID int, IsBB int);
	-- backward items
				with cte as (
					select		m.ID,                                           
                                I.SourceFusionAttributeID,
                                I.TargetFusionAttributeID,
                                1 as [Level],
								1 as SourceFusionAttributeTypeID,
								1 as TargetFusionAttributeTypeID								
                    from   MapRuleItem I                                
								inner join #maps m on (m.FusionAttributeID = I.SourceFusionAttributeID or m.FusionAttributeID = I.TargetFusionAttributeID) and m.FusionAttributeTypeID != 301
                    						   
					union all
					select	T.ID,
							S.SourceFusionAttributeID,
							S.TargetFusionAttributeID,
							T.[Level] + 1 as [Level],							
							SFA.FusionAttributeTypeID as SourceFusionAttributeTypeID,
							TFA.FusionAttributeTypeID as TargetFusionAttributeTypeID
					from	MapRuleItem S
							inner join cte T on T.SourceFusionAttributeID = S.TargetFusionAttributeID and S.ID <> T.ID
							inner join FusionAttribute SFA on SFA.ID = S.SourceFusionAttributeID and SFA.Deleted = 0
                            inner join FusionAttribute TFA on TFA.ID = S.TargetFusionAttributeID and TFA.Deleted = 0
					where	T.[Level] <= 25
				)
				insert into @tFusionPoints
					select distinct	ID, 							
							1
					from	cte 
					where	cte.SourceFusionAttributeTypeID = 301 or cte.TargetFusionAttributeTypeID = 301;

	update T
		 set T.IsRelatedToBloomberg = 1
		 from #maps T
			inner join @tFusionPoints f on f.ID = T.ID;

	update T
		set T.ObjectArtifactTypeID = A.ArtifactTypeID
		from #maps T
		inner join artifact A on (A.id = T.ObjectID);

	declare @bloombergOwnerArtifactId int = 0;

	select 
		top 1 @bloombergOwnerArtifactId = fo.artifactid
	from
		fusion f
		inner join fusionowner fo on (f.fusiontypeid = @bloombergFusionTypeId and fo.fusionid = f.id);


	--------------------------------------------------------------------------
	-- Eagle -> Bloomberg - Source is bloomberg Target is eagle
	--------------------------------------------------------------------------

	update T
	set T.[targetintersectid] = OI.ID
	from #maps T		
		inner join [IntersectDetail] OI on (OI.[Object] = T.[Object] and OI.ObjectID = T.[ObjectID] and OI.[Subject] = 'Artifact' and OI.[SubjectID] = T.ObjectOwnerArtifactID and T.TargetIntersectID is null  and T.fusionAttributeTypeId != 301);

	-- add any missing relations to source / object
	insert into [intersect] (IntersectTypeID, Classification, [Subject], SubjectID, [Object], ObjectID, CreatedBy, CreatedOn, UpdatedBy, UpdatedOn, [Owner])
		select distinct
			(select top 1 i_t.ID from [intersecttype] i_t where (i_t.[object] = c_s.objecttype and i_t.[subject] = c_t.objecttype and i_t.objectid = c_s.objecttypeid and i_t.subjectid = c_t.objecttypeid) /*or 
				(i_t.[subject] = c_s.objecttype and i_t.[object] = c_t.objecttype and i_t.subjectid = c_s.objecttypeid and i_t.objectid = c_t.objecttypeid)*/)
			,2			
			,'Artifact'
			,T.ObjectOwnerArtifactID
			,T.[object]
			,T.[objectID]			
			,0,getutcdate(),0,getutcdate(),'EAGLE BUSINESS LINEAGE'
		from #maps T		
		inner join [cache].[objectdetails] c_s on (c_s.[object] = T.[object] and c_s.[objectid] = T.[objectid])
		inner join [cache].[objectdetails] c_t on (c_t.[object] = 'Artifact' and c_t.[objectid] = T.ObjectOwnerArtifactID)
		where T.targetIntersectID is null  and T.fusionAttributeTypeId != 301;

	update T
	set T.[targetintersectid] = OI.ID
	from #maps T		
		inner join [IntersectDetail] OI on (OI.[Object] = T.[Object] and OI.ObjectID = T.[ObjectID] and OI.[Subject] = 'Artifact' and OI.[SubjectID] = T.ObjectOwnerArtifactID and T.TargetIntersectID is null  and T.fusionAttributeTypeId != 301);


	-- source intersects for eagle use bloomberg default
	update T
	set T.[sourceintersectid] = OI.ID
	from #maps T		
		inner join [IntersectDetail] OI on (OI.[Object] = T.[Object] and OI.ObjectID = T.[ObjectID] and OI.[Subject] = 'Artifact' and OI.[SubjectID] = @bloombergOwnerArtifactId and T.sourceIntersectID is null and T.fusionAttributeTypeId != 301);

	-- add any missing relations to source / object
	insert into [intersect] (IntersectTypeID, Classification, [Subject], SubjectID, [Object], ObjectID, CreatedBy, CreatedOn, UpdatedBy, UpdatedOn, [Owner])
		select distinct
			(select top 1 i_t.ID from [intersecttype] i_t where (i_t.[object] = c_s.objecttype and i_t.[subject] = c_t.objecttype and i_t.objectid = c_s.objecttypeid and i_t.subjectid = c_t.objecttypeid))
			,2			
			,'Artifact'
			,@bloombergOwnerArtifactId
			,T.[object]
			,T.[objectID]			
			,0,getutcdate(),0,getutcdate(),'EAGLE BUSINESS LINEAGE'
		from #maps T		
		inner join [cache].[objectdetails] c_s on (c_s.[object] = T.[object] and c_s.[objectid] = T.[objectid])
		inner join [cache].[objectdetails] c_t on (c_t.[object] = 'Artifact' and c_t.[objectid] = @bloombergOwnerArtifactId)
		where T.sourceIntersectID is null and T.IsRelatedToBloomberg = 1 and T.fusionAttributeTypeId != 301;

	update T
	set T.[sourceintersectid] = OI.ID
	from #maps T		
		inner join [IntersectDetail] OI on (OI.[Object] = T.[Object] and OI.ObjectID = T.[ObjectID] and OI.[Subject] = 'Artifact' and OI.[SubjectID] = @bloombergOwnerArtifactId and T.sourceIntersectID is null and T.fusionAttributeTypeId != 301);
		
	--------------------------------------------------------------------------
	-- Bloomberg -> Eagle - Source is bloomberg Target is eagle
	--------------------------------------------------------------------------
	
	update T
	set T.[sourceintersectid] = OI.ID
	from #maps T		
		inner join [IntersectDetail] OI on (OI.[Object] = T.[Object] and OI.ObjectID = T.[ObjectID] and OI.[Subject] = 'Artifact' and OI.[SubjectID] = T.ObjectOwnerArtifactID and T.sourceIntersectID is null and T.fusionAttributeTypeId = 301);

	-- add any missing relations to source / object
	insert into [intersect] (IntersectTypeID, Classification, [Subject], SubjectID, [Object], ObjectID, CreatedBy, CreatedOn, UpdatedBy, UpdatedOn, [Owner])
		select distinct
			(select top 1 i_t.ID from [intersecttype] i_t where (i_t.[object] = c_s.objecttype and i_t.[subject] = c_t.objecttype and i_t.objectid = c_s.objecttypeid and i_t.subjectid = c_t.objecttypeid))
			,2			
			,'Artifact'
			,T.ObjectOwnerArtifactID
			,T.[object]
			,T.[objectID]			
			,0,getutcdate(),0,getutcdate(),'EAGLE BUSINESS LINEAGE'
		from #maps T		
		inner join [cache].[objectdetails] c_s on (c_s.[object] = T.[object] and c_s.[objectid] = T.[objectid])
		inner join [cache].[objectdetails] c_t on (c_t.[object] = 'Artifact' and c_t.[objectid] = T.ObjectOwnerArtifactID)
		where T.sourceIntersectID is null and T.IsRelatedToBloomberg = 1 and T.fusionAttributeTypeId = 301;

	update T
	set T.[sourceintersectid] = OI.ID
	from #maps T		
		inner join [IntersectDetail] OI on (OI.[Object] = T.[Object] and OI.ObjectID = T.[ObjectID] and OI.[Subject] = 'Artifact' and OI.[SubjectID] = T.ObjectOwnerArtifactID and T.sourceIntersectID is null and T.fusionAttributeTypeId != 301);

	--target 

	update T
	set T.[targetintersectid] = OI.ID
	from #maps T		
		inner join [IntersectDetail] OI on (OI.[Object] = T.[Object] and OI.ObjectID = T.[ObjectID] and OI.[Subject] = 'Artifact' and OI.[SubjectID] = @eagleOwnerArtifact and T.TargetIntersectID is null  and T.fusionAttributeTypeId = 301);
		

	-- add any missing relations to source / object
	insert into [intersect] (IntersectTypeID, Classification, [Subject], SubjectID, [Object], ObjectID, CreatedBy, CreatedOn, UpdatedBy, UpdatedOn, [Owner])
		select distinct
			(select top 1 i_t.ID from [intersecttype] i_t where (i_t.[object] = c_s.objecttype and i_t.[subject] = c_t.objecttype and i_t.objectid = c_s.objecttypeid and i_t.subjectid = c_t.objecttypeid) /*or 
				(i_t.[subject] = c_s.objecttype and i_t.[object] = c_t.objecttype and i_t.subjectid = c_s.objecttypeid and i_t.objectid = c_t.objecttypeid)*/)
			,2			
			,'Artifact'
			,@eagleOwnerArtifact
			,T.[object]
			,T.[objectID]			
			,0,getutcdate(),0,getutcdate(),'EAGLE BUSINESS LINEAGE'
		from #maps T		
		inner join [cache].[objectdetails] c_s on (c_s.[object] = T.[object] and c_s.[objectid] = T.[objectid])
		inner join [cache].[objectdetails] c_t on (c_t.[object] = 'Artifact' and c_t.[objectid] = @eagleOwnerArtifact)
		where T.targetIntersectID is null  and T.fusionAttributeTypeId = 301;

	update T
	set T.[targetintersectid] = OI.ID
	from #maps T		
		inner join [IntersectDetail] OI on (OI.[Object] = T.[Object] and OI.ObjectID = T.[ObjectID] and OI.[Subject] = 'Artifact' and OI.[SubjectID] = @eagleOwnerArtifact and T.TargetIntersectID is null  and T.fusionAttributeTypeId = 301);

		
	---------------------------------------------------------------------------------------------------------------
	-- Insert Piece
	---------------------------------------------------------------------------------------------------------------

	Declare @MapItemIDList Table(MapItemID int, sourceintersectid int, targetintersectid int);
	Declare @MapRuleItemIDList Table(MapRuleItemID int, MapID Int);

	-- insert the map item records 
	-- load any existing map item instances
	update T
	set T.MapItemID = mi.ID
	from #maps T
		inner join mapitem mi on(T.sourceintersectid = mi.SourceIntersectID and T.targetintersectid = mi.TargetIntersectID and mi.[Owner] = 'EAGLE BUSINESS LINEAGE'); 

	-- insert map records
	MERGE
	INTO    mapitem mi
	USING   (			
			select distinct sourceintersectid, targetintersectid FROM #maps where (sourceintersectid is not null and targetintersectid is not null) and sourceintersectid != targetintersectid and mapitemid is null
			) S
	ON      (1 = 0)
	WHEN NOT MATCHED THEN
	INSERT  (SourceIntersectID, TargetIntersectID, CreatedBy, CreatedOn, UpdatedBy, UpdatedOn, [Owner])
	VALUES  (S.sourceintersectid, S.targetintersectid, 0, getutcdate(), 0, getutcdate(), 'EAGLE BUSINESS LINEAGE')
	OUTPUT  INSERTED.ID, S.sourceintersectid, S.targetintersectid into @MapItemIDList;

	--update map item id from main temp table
	update T
	set T.mapitemid = MI.MapItemID
	from #maps T
		inner join @MapItemIDList MI on (MI.sourceintersectid = T.sourceintersectid and MI.targetintersectid = T.targetintersectid)
		
	-- delete any mapitem records that are not in objectmap that are markit lineage
	delete from mapitem where [owner] = 'EAGLE BUSINESS LINEAGE' and id not in (select mapitemid from #maps);


	delete from mapruleitemmapitem where [owner] = 'EAGLE BUSINESS LINEAGE';

	insert into mapruleitemmapitem
		(mapruleitemid, mapitemid, [owner])
		select MapRuleItemID, MapItemID, 'EAGLE BUSINESS LINEAGE' from #maps;
	
	--select * from #maps;


end
GO

CREATE procedure [fusion].[GenerateEagleLineageData]
	@fusionId int,
	@includeEagleToBloomberg bit
as
begin
	SET NOCOUNT, ANSI_PADDING ON;
	SET ANSI_WARNINGS ON;

	declare @intersectTypeId int;
	--bloomberg type ids
	declare @bloombergMnemonicTypeId int = 301;

	--eagle type ids
	declare @eagleReportProfileTypeId int = 191;
	declare @eaglePortalQueryTypeId int = 192;
	declare @eagleDatamartFieldTypeId int = 193;
	declare @eagleDatamartModelTypeId int = 194;
	declare @eagleMessageStreamTypeId int = 196;
	declare @eagleReportRuleTypeId int = 197;
	declare @eagleFieldAttributeTypeId int = 201;
	declare @eagleInventoryOfFieldTypeId int = 205;
	declare @eagleFieldRuleTypeId int = 206;
	declare @eagleSourceRuleTypeId int = 208;
	declare @eagleSourceRuleItemTypeId int = 209;
	declare @eagleGroupingRuleTypeId int = 210;
	declare @eagleReferenceDataCenterStrategyTypeId int = 215;
	declare @eagleReferenceDataCenterValidationTypeId int = 216;
	declare @eagleReferenceDataCenterFieldGroupTypeId int = 218;
	declare @eagleReferenceDataCenterGoldCopyTypeId int = 217;

	-- validate the provided fusion id that its of fusiontype id 16	
	declare @fusionTypeId int;
	select @fusionTypeId = FusionTypeID from [dbo].[Fusion] where ID = @fusionId;
	if @fusionTypeId != 16
	begin
		raiserror('ERROR - The eagle fusion lineage generation process may only be run for the Eagle DB Fusion Type', 16, -1);
		return;
	end
	
	IF OBJECT_ID('tempdb..#maps') IS NOT NULL
		DROP TABLE #maps;

	create table #maps (	
		ID int identity primary key,			
		MapRuleItemID int,		
		SourceFusionAttributeID int,
		SourceFusionAttributeTypeID int,
		SourceObject nvarchar(500),		
		TargetFusionAttributeID int,
		TargetFusionAttributeTypeID int,
		TargetObject nvarchar(500)		
	);

	CREATE NONCLUSTERED INDEX [CIX_TempMaps] ON #maps ( SourceFusionAttributeID ASC, TargetFusionAttributeID ASC );


	if ( @includeEagleToBloomberg = 1 )
	begin
		----------------------------------------------------------
		-- BLOOMBERG MNEMNONIC TO EAGLE INVENTORY OF FIELD
		----------------------------------------------------------	
		select @intersectTypeId = id from intersecttype where subjectid = @eagleInventoryOfFieldTypeId and [subject] = 'FusionAttributeType' and objectid = @bloombergMnemonicTypeId and [object] = 'FusionAttributeType';	
		if @intersectTypeId is null
		begin
			raiserror('ERROR - Cannot identify the intersecttypeid for bloomberg mnemonic/ eagle db column relations', 16, -1);
			return;
		end

		insert into #maps 
			(SourceFusionAttributeID, SourceFusionAttributeTypeID, SourceObject, TargetFusionAttributeID, TargetFusionAttributeTypeID, TargetObject)
			select
				FA_s.ID as SourceFusionAttributeID,
				FA_s.FusionAttributeTypeID as SourceFusionAttributeTypeID,
				FA_s.Name as SourceObject,
				FA_t.ID as TargetFusionAttributeID,
				FA_t.FusionAttributeTypeID as TargetFusionAttributeTypeID,
				FA_t.Name as TargetObject
			from
				[dbo].[intersect] I
				inner join [dbo].FusionAttribute FA_t on (FA_t.ID = I.subjectID and I.intersecttypeid = @intersectTypeId and FA_t.FusionID = @fusionId)
				inner join [dbo].FusionAttribute FA_s on (FA_s.ID = I.objectID and I.intersecttypeid = @intersectTypeId);
	
		set @intersectTypeId = null;

		----------------------------------------------------------
		-- BLOOMBERG MNEMONIC TO EAGLE MESSAGE STREAM	
		----------------------------------------------------------
	
		select @intersectTypeId = id from intersecttype where subjectid = @eagleMessageStreamTypeId and [subject] = 'FusionAttributeType' and objectid = @bloombergMnemonicTypeId and [object] = 'FusionAttributeType';
		if @intersectTypeId is null
		begin
			raiserror('ERROR - Cannot identify the intersecttypeid for bloomberg mnemonic/ eagle message stream relations', 16, -1);
			return;
		end

		insert into #maps 
			(SourceFusionAttributeID, SourceFusionAttributeTypeID, SourceObject, TargetFusionAttributeID, TargetFusionAttributeTypeID, TargetObject)
			select
				FA_s.ID as SourceFusionAttributeID,
				FA_s.FusionAttributeTypeID as SourceFusionAttributeTypeID,
				FA_s.Name as SourceObject,
				FA_t.ID as TargetFusionAttributeID,
				FA_t.FusionAttributeTypeID as TargetFusionAttributeTypeID,
				FA_t.Name as TargetObject
			from
				[dbo].[intersect] I
				inner join [dbo].FusionAttribute FA_t on (FA_t.ID = I.subjectID and I.intersecttypeid = @intersectTypeId and FA_t.FusionID = @fusionId)
				inner join [dbo].FusionAttribute FA_s on (FA_s.ID = I.objectID and I.intersecttypeid = @intersectTypeId);
	
		set @intersectTypeId = null;						
	end
	----------------------------------------------------------
	-- INVENTORY OF FIELDS TO FIELD ATTRIBUTE	
	----------------------------------------------------------
	
	select @intersectTypeId = id from intersecttype where subjectid = @eagleFieldAttributeTypeId and [subject] = 'FusionAttributeType' and objectid = @eagleInventoryOfFieldTypeId and [object] = 'FusionAttributeType';
	if @intersectTypeId is null
	begin
		raiserror('ERROR - Cannot identify the intersecttypeid for eagle field attribute/ eagle db column relations', 16, -1);
		return;
	end

	insert into #maps 
		(SourceFusionAttributeID, SourceFusionAttributeTypeID, SourceObject, TargetFusionAttributeID, TargetFusionAttributeTypeID, TargetObject)
		select
			FA_s.ID as SourceFusionAttributeID,
			FA_s.FusionAttributeTypeID as SourceFusionAttributeTypeID,
			FA_s.Name as SourceObject,
			FA_t.ID as TargetFusionAttributeID,
			FA_t.FusionAttributeTypeID as TargetFusionAttributeTypeID,
			FA_t.Name as TargetObject
		from
			[dbo].[intersect] I
			inner join [dbo].FusionAttribute FA_t on (FA_t.ID = I.subjectID and I.intersecttypeid = @intersectTypeId and FA_t.FusionID = @fusionId)
			inner join [dbo].FusionAttribute FA_s on (FA_s.ID = I.objectID and I.intersecttypeid = @intersectTypeId and FA_s.FusionID = @fusionId);
	set @intersectTypeId = null;	
	
	----------------------------------------------------------
	-- FIELD ATTRIBUTE TO FIELD RULES	
	----------------------------------------------------------
	
	select @intersectTypeId = id from intersecttype where subjectid = @eagleFieldRuleTypeId and [subject] = 'FusionAttributeType' and objectid = @eagleFieldAttributeTypeId and [object] = 'FusionAttributeType';
	if @intersectTypeId is null
	begin
		raiserror('ERROR - Cannot identify the intersecttypeid for eagle field attribute/ eagle field rule', 16, -1);
		return;
	end

	insert into #maps 
		(SourceFusionAttributeID, SourceFusionAttributeTypeID, SourceObject, TargetFusionAttributeID, TargetFusionAttributeTypeID, TargetObject)
		select
			FA_s.ID as SourceFusionAttributeID,
			FA_s.FusionAttributeTypeID as SourceFusionAttributeTypeID,
			FA_s.Name as SourceObject,
			FA_t.ID as TargetFusionAttributeID,
			FA_t.FusionAttributeTypeID as TargetFusionAttributeTypeID,
			FA_t.Name as TargetObject
		from
			[dbo].[intersect] I
			inner join [dbo].FusionAttribute FA_t on (FA_t.ID = I.subjectID and I.intersecttypeid = @intersectTypeId and FA_t.FusionID = @fusionId)
			inner join [dbo].FusionAttribute FA_s on (FA_s.ID = I.objectID and I.intersecttypeid = @intersectTypeId and FA_s.FusionID = @fusionId);
	set @intersectTypeId = null;

	----------------------------------------------------------
	-- FIELD ATTRIBUTE TO FIELD ATTRIBUTE - This is for computed fields ie fields which use other fields
	----------------------------------------------------------
	
	select @intersectTypeId = id from intersecttype where subjectid = @eagleFieldAttributeTypeId and [subject] = 'FusionAttributeType' and objectid = @eagleFieldAttributeTypeId and [object] = 'FusionAttributeType';
	if @intersectTypeId is null
	begin
		raiserror('ERROR - Cannot identify the intersecttypeid for eagle field attribute/ eagle field attribute', 16, -1);
		return;
	end

	insert into #maps 
		(SourceFusionAttributeID, SourceFusionAttributeTypeID, SourceObject, TargetFusionAttributeID, TargetFusionAttributeTypeID, TargetObject)
		select
			FA_s.ID as SourceFusionAttributeID,
			FA_s.FusionAttributeTypeID as SourceFusionAttributeTypeID,
			FA_s.Name as SourceObject,
			FA_t.ID as TargetFusionAttributeID,
			FA_t.FusionAttributeTypeID as TargetFusionAttributeTypeID,
			FA_t.Name as TargetObject
		from
			[dbo].[intersect] I
			inner join [dbo].FusionAttribute FA_s on (FA_s.ID = I.subjectID and I.intersecttypeid = @intersectTypeId and FA_s.FusionID = @fusionId)
			inner join [dbo].FusionAttribute FA_t on (FA_t.ID = I.objectID and I.intersecttypeid = @intersectTypeId and FA_t.FusionID = @fusionId);
	set @intersectTypeId = null;

	----------------------------------------------------------
	-- FIELD ATTRIBUTE TO GROUPING RULES	
	----------------------------------------------------------
	
	select @intersectTypeId = id from intersecttype where subjectid = @eagleGroupingRuleTypeId and [subject] = 'FusionAttributeType' and objectid = @eagleFieldAttributeTypeId and [object] = 'FusionAttributeType';
	if @intersectTypeId is null
	begin
		raiserror('ERROR - Cannot identify the intersecttypeid for eagle field attribute/ eagle grouping rule', 16, -1);
		return;
	end

	insert into #maps 
		(SourceFusionAttributeID, SourceFusionAttributeTypeID, SourceObject, TargetFusionAttributeID, TargetFusionAttributeTypeID, TargetObject)
		select
			FA_s.ID as SourceFusionAttributeID,
			FA_s.FusionAttributeTypeID as SourceFusionAttributeTypeID,
			FA_s.Name as SourceObject,
			FA_t.ID as TargetFusionAttributeID,
			FA_t.FusionAttributeTypeID as TargetFusionAttributeTypeID,
			FA_t.Name as TargetObject
		from
			[dbo].[intersect] I
			inner join [dbo].FusionAttribute FA_t on (FA_t.ID = I.subjectID and I.intersecttypeid = @intersectTypeId and FA_t.FusionID = @fusionId)
			inner join [dbo].FusionAttribute FA_s on (FA_s.ID = I.objectID and I.intersecttypeid = @intersectTypeId and FA_s.FusionID = @fusionId);
	
	set @intersectTypeId = null;
	----------------------------------------------------------
	-- FIELD RULE TO REPORT PROFILE
	----------------------------------------------------------
	
	select @intersectTypeId = id from intersecttype where subjectid = @eagleReportRuleTypeId and [subject] = 'FusionAttributeType' and objectid = @eagleFieldRuleTypeId and [object] = 'FusionAttributeType';
	if @intersectTypeId is null
	begin
		raiserror('ERROR - Cannot identify the intersecttypeid for eagle field rule/ eagle report rule', 16, -1);
		return;
	end

	insert into #maps 
		(SourceFusionAttributeID, SourceFusionAttributeTypeID, SourceObject, TargetFusionAttributeID, TargetFusionAttributeTypeID, TargetObject)
		select
			FA_s.ID as SourceFusionAttributeID,
			FA_s.FusionAttributeTypeID as SourceFusionAttributeTypeID,
			FA_s.Name as SourceObject,
			FA_t.ID as TargetFusionAttributeID,
			FA_t.FusionAttributeTypeID as TargetFusionAttributeTypeID,
			FA_t.Name as TargetObject
		from
			[dbo].[intersect] I
			inner join [dbo].FusionAttribute FA_t on (FA_t.ID = I.subjectID and I.intersecttypeid = @intersectTypeId and FA_t.FusionID = @fusionId)
			inner join [dbo].FusionAttribute FA_s on (FA_s.ID = I.objectID and I.intersecttypeid = @intersectTypeId and FA_s.FusionID = @fusionId);
	
	set @intersectTypeId = null;
	----------------------------------------------------------
	-- FIELD RULE to PORTAL QUERY
	----------------------------------------------------------	
	select @intersectTypeId = id from intersecttype where subjectid = @eaglePortalQueryTypeId and [subject] = 'FusionAttributeType' and objectid = @eagleFieldRuleTypeId and [object] = 'FusionAttributeType';
	if @intersectTypeId is null
	begin
		raiserror('ERROR - Cannot identify the intersecttypeid for eagle field rule/ eagle portal query', 16, -1);
		return;
	end

	insert into #maps 
		(SourceFusionAttributeID, SourceFusionAttributeTypeID, SourceObject, TargetFusionAttributeID, TargetFusionAttributeTypeID, TargetObject)
		select
			FA_s.ID as SourceFusionAttributeID,
			FA_s.FusionAttributeTypeID as SourceFusionAttributeTypeID,
			FA_s.Name as SourceObject,
			FA_t.ID as TargetFusionAttributeID,
			FA_t.FusionAttributeTypeID as TargetFusionAttributeTypeID,
			FA_t.Name as TargetObject
		from
			[dbo].[intersect] I
			inner join [dbo].FusionAttribute FA_t on (FA_t.ID = I.subjectID and I.intersecttypeid = @intersectTypeId and FA_t.FusionID = @fusionId)
			inner join [dbo].FusionAttribute FA_s on (FA_s.ID = I.objectID and I.intersecttypeid = @intersectTypeId and FA_s.FusionID = @fusionId);
	
	set @intersectTypeId = null;
	----------------------------------------------------------
	-- RDC Validation to Field Attribute
	----------------------------------------------------------	
	select @intersectTypeId = id from intersecttype where subjectid = @eagleReferenceDataCenterValidationTypeId and [subject] = 'FusionAttributeType' and objectid = @eagleFieldAttributeTypeId and [object] = 'FusionAttributeType';
	if @intersectTypeId is null
	begin
		raiserror('ERROR - Cannot identify the intersecttypeid for eagle field attribute/ rdc validation', 16, -1);
		return;
	end

	insert into #maps 
		(SourceFusionAttributeID, SourceFusionAttributeTypeID, SourceObject, TargetFusionAttributeID, TargetFusionAttributeTypeID, TargetObject)
		select
			FA_s.ID as SourceFusionAttributeID,
			FA_s.FusionAttributeTypeID as SourceFusionAttributeTypeID,
			FA_s.Name as SourceObject,
			FA_t.ID as TargetFusionAttributeID,
			FA_t.FusionAttributeTypeID as TargetFusionAttributeTypeID,
			FA_t.Name as TargetObject
		from
			[dbo].[intersect] I
			inner join [dbo].FusionAttribute FA_t on (FA_t.ID = I.subjectID and I.intersecttypeid = @intersectTypeId and FA_t.FusionID = @fusionId)
			inner join [dbo].FusionAttribute FA_s on (FA_s.ID = I.objectID and I.intersecttypeid = @intersectTypeId and FA_s.FusionID = @fusionId);
	
	set @intersectTypeId = null;
	----------------------------------------------------------
	-- RDC Data Strategy to Field Attribute
	----------------------------------------------------------	
	select @intersectTypeId = id from intersecttype where subjectid = @eagleReferenceDataCenterStrategyTypeId and [subject] = 'FusionAttributeType' and objectid = @eagleFieldAttributeTypeId and [object] = 'FusionAttributeType';
	if @intersectTypeId is null
	begin
		raiserror('ERROR - Cannot identify the intersecttypeid for eagle field attribute/ rdc data strategy', 16, -1);
		return;
	end

	insert into #maps 
		(SourceFusionAttributeID, SourceFusionAttributeTypeID, SourceObject, TargetFusionAttributeID, TargetFusionAttributeTypeID, TargetObject)
		select
			FA_s.ID as SourceFusionAttributeID,
			FA_s.FusionAttributeTypeID as SourceFusionAttributeTypeID,
			FA_s.Name as SourceObject,
			FA_t.ID as TargetFusionAttributeID,
			FA_t.FusionAttributeTypeID as TargetFusionAttributeTypeID,
			FA_t.Name as TargetObject
		from
			[dbo].[intersect] I
			inner join [dbo].FusionAttribute FA_t on (FA_t.ID = I.subjectID and I.intersecttypeid = @intersectTypeId and FA_t.FusionID = @fusionId)
			inner join [dbo].FusionAttribute FA_s on (FA_s.ID = I.objectID and I.intersecttypeid = @intersectTypeId and FA_s.FusionID = @fusionId);
	
	set @intersectTypeId = null;
	----------------------------------------------------------
	-- RDC Data Strategy to RDC Gold Copy
	----------------------------------------------------------	
	select @intersectTypeId = id from intersecttype where subjectid = @eagleReferenceDataCenterGoldCopyTypeId and [subject] = 'FusionAttributeType' and objectid = @eagleReferenceDataCenterStrategyTypeId and [object] = 'FusionAttributeType';
	if @intersectTypeId is null
	begin
		raiserror('ERROR - Cannot identify the intersecttypeid for eagle rdc gold copy / rdc data strategy', 16, -1);
		return;
	end

	insert into #maps 
		(SourceFusionAttributeID, SourceFusionAttributeTypeID, SourceObject, TargetFusionAttributeID, TargetFusionAttributeTypeID, TargetObject)
		select
			FA_s.ID as SourceFusionAttributeID,
			FA_s.FusionAttributeTypeID as SourceFusionAttributeTypeID,
			FA_s.Name as SourceObject,
			FA_t.ID as TargetFusionAttributeID,
			FA_t.FusionAttributeTypeID as TargetFusionAttributeTypeID,
			FA_t.Name as TargetObject
		from
			[dbo].[intersect] I
			inner join [dbo].FusionAttribute FA_t on (FA_t.ID = I.subjectID and I.intersecttypeid = @intersectTypeId and FA_t.FusionID = @fusionId)
			inner join [dbo].FusionAttribute FA_s on (FA_s.ID = I.objectID and I.intersecttypeid = @intersectTypeId and FA_s.FusionID = @fusionId);
	
	set @intersectTypeId = null;
	----------------------------------------------------------
	-- DataMart Measure to Field Attribute
	----------------------------------------------------------	
	select @intersectTypeId = id from intersecttype where subjectid = @eagleDatamartFieldTypeId and [subject] = 'FusionAttributeType' and objectid = @eagleFieldAttributeTypeId and [object] = 'FusionAttributeType';
	if @intersectTypeId is null
	begin
		raiserror('ERROR - Cannot identify the intersecttypeid for eagle field attribute/ datamart field', 16, -1);
		return;
	end

	insert into #maps 
		(SourceFusionAttributeID, SourceFusionAttributeTypeID, SourceObject, TargetFusionAttributeID, TargetFusionAttributeTypeID, TargetObject)
		select
			FA_s.ID as SourceFusionAttributeID,
			FA_s.FusionAttributeTypeID as SourceFusionAttributeTypeID,
			FA_s.Name as SourceObject,
			FA_t.ID as TargetFusionAttributeID,
			FA_t.FusionAttributeTypeID as TargetFusionAttributeTypeID,
			FA_t.Name as TargetObject
		from
			[dbo].[intersect] I
			inner join [dbo].FusionAttribute FA_t on (FA_t.ID = I.subjectID and I.intersecttypeid = @intersectTypeId and FA_t.FusionID = @fusionId)
			inner join [dbo].FusionAttribute FA_s on (FA_s.ID = I.objectID and I.intersecttypeid = @intersectTypeId and FA_s.FusionID = @fusionId);
	
	set @intersectTypeId = null;
	----------------------------------------------------------
	-- DataMart Model to DataMart Measure - uses parent child relation from fusion...
	----------------------------------------------------------	
	
	insert into #maps 
		(SourceFusionAttributeID, SourceFusionAttributeTypeID, SourceObject, TargetFusionAttributeID, TargetFusionAttributeTypeID, TargetObject)
		select
			FA_s.ID as SourceFusionAttributeID,
			FA_s.FusionAttributeTypeID as SourceFusionAttributeTypeID,
			FA_s.Name as SourceObject,
			FA_t.ID as TargetFusionAttributeID,
			FA_t.FusionAttributeTypeID as TargetFusionAttributeTypeID,
			FA_t.Name as TargetObject
		from
			[dbo].FusionAttribute FA_s
			inner join [dbo].FusionAttribute FA_t on (FA_t.ParentID = FA_s.ID and FA_t.FusionAttributeTypeId = @eagleDatamartFieldTypeId)
		where
			FA_s.FusionAttributeTypeId = @eagleDatamartModelTypeId and FA_s.FusionID = @fusionId
	
	set @intersectTypeId = null;
	----------------------------------------------------------
	-- Report Profile to Report Rule
	----------------------------------------------------------	
	select @intersectTypeId = id from intersecttype where subjectid = @eagleReportProfileTypeId  and [subject] = 'FusionAttributeType' and objectid = @eagleReportRuleTypeId and [object] = 'FusionAttributeType';
	if @intersectTypeId is null
	begin
		raiserror('ERROR - Cannot identify the intersecttypeid for eagle report profile/ report rule', 16, -1);
		return;
	end

	insert into #maps 
		(SourceFusionAttributeID, SourceFusionAttributeTypeID, SourceObject, TargetFusionAttributeID, TargetFusionAttributeTypeID, TargetObject)
		select
			FA_s.ID as SourceFusionAttributeID,
			FA_s.FusionAttributeTypeID as SourceFusionAttributeTypeID,
			FA_s.Name as SourceObject,
			FA_t.ID as TargetFusionAttributeID,
			FA_t.FusionAttributeTypeID as TargetFusionAttributeTypeID,
			FA_t.Name as TargetObject
		from
			[dbo].[intersect] I
			inner join [dbo].FusionAttribute FA_t on (FA_t.ID = I.subjectID and I.intersecttypeid = @intersectTypeId and FA_t.FusionID = @fusionId)
			inner join [dbo].FusionAttribute FA_s on (FA_s.ID = I.objectID and I.intersecttypeid = @intersectTypeId and FA_s.FusionID = @fusionId);
	
	set @intersectTypeId = null;
	----------------------------------------------------------
	-- Report Rule to Source Rule
	----------------------------------------------------------	
	select @intersectTypeId = id from intersecttype where subjectid = @eagleReportRuleTypeId  and [subject] = 'FusionAttributeType' and objectid = @eagleSourceRuleTypeId and [object] = 'FusionAttributeType';
	if @intersectTypeId is null
	begin
		raiserror('ERROR - Cannot identify the intersecttypeid for eagle report rule / source rule', 16, -1);
		return;
	end

	insert into #maps 
		(SourceFusionAttributeID, SourceFusionAttributeTypeID, SourceObject, TargetFusionAttributeID, TargetFusionAttributeTypeID, TargetObject)
		select
			FA_s.ID as SourceFusionAttributeID,
			FA_s.FusionAttributeTypeID as SourceFusionAttributeTypeID,
			FA_s.Name as SourceObject,
			FA_t.ID as TargetFusionAttributeID,
			FA_t.FusionAttributeTypeID as TargetFusionAttributeTypeID,
			FA_t.Name as TargetObject
		from
			[dbo].[intersect] I
			inner join [dbo].FusionAttribute FA_t on (FA_t.ID = I.subjectID and I.intersecttypeid = @intersectTypeId and FA_t.FusionID = @fusionId)
			inner join [dbo].FusionAttribute FA_s on (FA_s.ID = I.objectID and I.intersecttypeid = @intersectTypeId and FA_s.FusionID = @fusionId);
	
	set @intersectTypeId = null;
	----------------------------------------------------------
	-- Reference Data Center Data Strategy to Source Rule
	----------------------------------------------------------	
	select @intersectTypeId = id from intersecttype where subjectid = @eagleReferenceDataCenterStrategyTypeId  and [subject] = 'FusionAttributeType' and objectid = @eagleSourceRuleTypeId and [object] = 'FusionAttributeType';
	if @intersectTypeId is null
	begin
		raiserror('ERROR - Cannot identify the intersecttypeid for eagle data strategy / source rule', 16, -1);
		return;
	end

	insert into #maps 
		(SourceFusionAttributeID, SourceFusionAttributeTypeID, SourceObject, TargetFusionAttributeID, TargetFusionAttributeTypeID, TargetObject)
		select
			FA_s.ID as SourceFusionAttributeID,
			FA_s.FusionAttributeTypeID as SourceFusionAttributeTypeID,
			FA_s.Name as SourceObject,
			FA_t.ID as TargetFusionAttributeID,
			FA_t.FusionAttributeTypeID as TargetFusionAttributeTypeID,
			FA_t.Name as TargetObject
		from
			[dbo].[intersect] I
			inner join [dbo].FusionAttribute FA_t on (FA_t.ID = I.subjectID and I.intersecttypeid = @intersectTypeId and FA_t.FusionID = @fusionId)
			inner join [dbo].FusionAttribute FA_s on (FA_s.ID = I.objectID and I.intersecttypeid = @intersectTypeId and FA_s.FusionID = @fusionId);
	
	set @intersectTypeId = null;
	----------------------------------------------------------
	-- Datamart model to Source Rule
	----------------------------------------------------------	
	select @intersectTypeId = id from intersecttype where subjectid = @eagleDatamartModelTypeId  and [subject] = 'FusionAttributeType' and objectid = @eagleSourceRuleTypeId and [object] = 'FusionAttributeType';
	if @intersectTypeId is null
	begin
		raiserror('ERROR - Cannot identify the intersecttypeid for eagle datamart model / source rule', 16, -1);
		return;
	end

	insert into #maps 
		(SourceFusionAttributeID, SourceFusionAttributeTypeID, SourceObject, TargetFusionAttributeID, TargetFusionAttributeTypeID, TargetObject)
		select
			FA_s.ID as SourceFusionAttributeID,
			FA_s.FusionAttributeTypeID as SourceFusionAttributeTypeID,
			FA_s.Name as SourceObject,
			FA_t.ID as TargetFusionAttributeID,
			FA_t.FusionAttributeTypeID as TargetFusionAttributeTypeID,
			FA_t.Name as TargetObject
		from
			[dbo].[intersect] I
			inner join [dbo].FusionAttribute FA_t on (FA_t.ID = I.subjectID and I.intersecttypeid = @intersectTypeId and FA_t.FusionID = @fusionId)
			inner join [dbo].FusionAttribute FA_s on (FA_s.ID = I.objectID and I.intersecttypeid = @intersectTypeId and FA_s.FusionID = @fusionId);
	
	set @intersectTypeId = null;	
	----------------------------------------------------------
	-- Portal Query to Report Profile 
	----------------------------------------------------------	
	select @intersectTypeId = id from intersecttype where subjectid = @eaglePortalQueryTypeId and [subject] = 'FusionAttributeType' and objectid = @eagleReportProfileTypeId and [object] = 'FusionAttributeType';
	if @intersectTypeId is null
	begin
		raiserror('ERROR - Cannot identify the intersecttypeid for eagle portal query/ report profile', 16, -1);
		return;
	end

	insert into #maps 
		(SourceFusionAttributeID, SourceFusionAttributeTypeID, SourceObject, TargetFusionAttributeID, TargetFusionAttributeTypeID, TargetObject)
		select
			FA_s.ID as SourceFusionAttributeID,
			FA_s.FusionAttributeTypeID as SourceFusionAttributeTypeID,
			FA_s.Name as SourceObject,
			FA_t.ID as TargetFusionAttributeID,
			FA_t.FusionAttributeTypeID as TargetFusionAttributeTypeID,
			FA_t.Name as TargetObject
		from
			[dbo].[intersect] I
			inner join [dbo].FusionAttribute FA_t on (FA_t.ID = I.subjectID and I.intersecttypeid = @intersectTypeId and FA_t.FusionID = @fusionId)
			inner join [dbo].FusionAttribute FA_s on (FA_s.ID = I.objectID and I.intersecttypeid = @intersectTypeId and FA_s.FusionID = @fusionId);
	
	set @intersectTypeId = null;	
	----------------------------------------------------------
	-- Source Rule to Source Interface
	----------------------------------------------------------
	insert into #maps 
		(SourceFusionAttributeID, SourceFusionAttributeTypeID, SourceObject, TargetFusionAttributeID, TargetFusionAttributeTypeID, TargetObject)
			select
					FA_s.ID as SourceFusionAttributeID,
					FA_s.FusionAttributeTypeID as SourceFusionAttributeTypeID,
					FA_s.Name as SourceObject,
					FA_t.ID as TargetFusionAttributeID,
					FA_t.FusionAttributeTypeID as TargetFusionAttributeTypeID,
					FA_t.Name as TargetObject
				from
					[dbo].FusionAttribute FA_t
					inner join [dbo].FusionAttribute FA_s on (FA_s.ParentID = FA_t.ID and FA_s.FusionAttributeTypeId = @eagleSourceRuleItemTypeId)
				where
					FA_t.FusionAttributeTypeId = @eagleSourceRuleTypeId and FA_t.FusionID = @fusionId;

	----------------------------------------------------------
	-- INSERT 
	-- update the map rule item id's of already inserted items
	----------------------------------------------------------
	update T
			set T.mapruleitemid = S.id
			from #maps T
				inner join [dbo].[mapruleitem] S on (S.[owner] = 'EAGLE LINEAGE' and S.SourceFusionAttributeID = T.SourceFusionAttributeID and S.TargetFusionAttributeID = T.TargetFusionAttributeID);
	
	-- insert the mapruleitem records
	MERGE
		INTO    mapruleitem mri
		USING   (
				select SourceFusionAttributeID, TargetFusionAttributeID from #maps where mapruleitemid is null
				) S
		ON      (1 = 0)
		WHEN NOT MATCHED THEN
		INSERT  (SourceFusionAttributeID, TargetFusionAttributeID, CreatedBy, CreatedOn, UpdatedBy, UpdatedOn, [Owner])
		VALUES  (S.SourceFusionAttributeID, S.TargetFusionAttributeID, 0, getutcdate(), 0, getutcdate(), 'EAGLE LINEAGE');
		
		--delete any maprule item records that are not in the map

	delete from mapruleitem where [owner] = 'EAGLE LINEAGE' and id not in(select m.mapruleitemid from #maps m);

	--testing / debug
	--select * from #maps;
	-- end testing / debug
end
GO

CREATE procedure [fusion].[GenerateMarkitMapLineageDataV2]
	@fusionID int
as
begin
	SET NOCOUNT, ANSI_PADDING ON;
	SET ANSI_WARNINGS ON;
		
	declare @databaseName varchar(100);
	declare @sourceFieldTypeID int;
	declare @targetFieldTypeID int;		
	declare @mapFusionAttributeTypeID int = 710; -- this is fixed for all clients
	declare @viewColumnFusionAttributeTypeID int = 715; -- this is fixed for all clients
	
	-- load the field ids for the source / target from mappings
	select @sourceFieldTypeID = id from fieldtype where [object] = 'FusionAttributeType' and [objectid] = @mapFusionAttributeTypeID and name = 'source';
	select @targetFieldTypeID = id from fieldtype where [object] = 'FusionAttributeType' and [objectid] = @mapFusionAttributeTypeID and name = 'target';
	
	IF @sourceFieldTypeID IS NULL
	begin		
		raiserror('ERROR - Cannot find the Markit Fusion Map Source Field.  Please make sure the latest markit fusion attribute types have been pushed to this environment', 16, -1);
		return;
	end

	IF @targetFieldTypeID IS NULL
	begin		
		raiserror('ERROR - Cannot find the Markit Fusion Map Target Field.  Please make sure the latest markit fusion attribute types have been pushed to this environment', 16, -1);
		return;
	end

	-- determine the database name
	select top 1 @databaseName = replace(sourceid, name,'') from fusionattribute where fusionid = @fusionID and fusionattributetypeid = 711;

	if @databaseName is null
	begin
		raiserror('ERROR - Cannot determine the database name to strip from markit fusion attribute data', 16, -1);
		return;
	end

	-- dont run if this is not a markit fusion
	declare @fusionTypeId int;
	select @fusionTypeId = FusionTypeID from [dbo].[Fusion] where ID = @fusionID;
	if @fusionTypeId != 13
	begin
		raiserror('ERROR - The fusion lineage generation process may only be run for the Markit Fusion Type', 16, -1);
		return;
	end

	-- dont run if no map records exist for this fusion
	if not exists( select 1 from fusionattribute where fusionid = @fusionID and fusionattributetypeid = @mapFusionAttributeTypeID )
	begin
		raiserror('ERROR - No Markit Fusion Map records exist for the specified Fusion ID', 16, -1);
		return;
	end

	-- figure out the database prefix from some markit data

	-- some logging
	declare @fusionName nvarchar(250);
	select @fusionName = name from [dbo].[fusion] where id = @fusionID;

	begin
		print 'Running For Fusion:' + @fusionName;
		print 'Using Target Field ID:' + cast(@targetFieldTypeID as varchar(100));
		print 'Using Source Field ID:' + cast(@sourceFieldTypeID as varchar(100));
		print 'Using Database prefix:' + @databaseName;
	end
	-- end logging

	-- get the intersecttypeid for view -> table intersects
	declare @viewTableIntersectTypeId int;
	select @viewTableIntersectTypeId = id from intersecttype where [object] = 'FusionAttributeType' and [Subject] = 'FusionAttributeType' and [subjectid] = 714 and [objectid] = 712
	if @viewTableIntersectTypeId is null
	begin
		raiserror('ERROR - Cannot identify the intersecttypeid for markit view/table relations', 16, -1);
		return;
	end

	-- get the intersecttypeid for view -> view intersects
	declare @viewViewIntersectTypeId int;
	select @viewViewIntersectTypeId = id from intersecttype where [object] = 'FusionAttributeType' and [Subject] = 'FusionAttributeType' and [subjectid] = 714 and [objectid] = 714
	if @viewViewIntersectTypeId is null
	begin
		raiserror('ERROR - Cannot identify the intersecttypeid for markit view/view relations', 16, -1);
		return;
	end

	IF OBJECT_ID('tempdb..#maps') IS NOT NULL
		DROP TABLE #maps;

	create table #maps (	
		ID int identity primary key,			
		MapRuleItemID int,		
		SourceFusionAttributeID int,
		SourceFusionAttributeTypeID int,
		SourceObject nvarchar(500),		
		SourceParentObject nvarchar(max),
		SourceParentObjectFusionAttributeID int,
		SourceParentObjectFusionAttributeTypeID int,
		TargetFusionAttributeID int,
		TargetFusionAttributeTypeID int,
		TargetObject nvarchar(500),
		TargetParentObject nvarchar(max),
		TargetParentObjectFusionAttributeID int,
		TargetParentObjectFusionAttributeTypeID int,							
	);

	CREATE NONCLUSTERED INDEX [CIX_TempMaps] ON #maps ( SourceFusionAttributeID ASC, TargetFusionAttributeID ASC );

	IF OBJECT_ID('tempdb..#objectmap') IS NOT NULL
		DROP TABLE #objectmap;

	create table #objectmap (
		MapID int,
		MapItemID int,
		[Object] varchar(50),
		[ObjectID] int,	
		[SourceIntersectID] int,		
		[TargetIntersectID] int		
	)

	CREATE NONCLUSTERED INDEX [CIX_TempObjectMap] ON #objectmap ( MapID ASC, [Object] ASC, [ObjectID] ASC );

	IF OBJECT_ID('tempdb..#mapRelation') IS NOT NULL
		DROP TABLE #mapRelation;

	create table #mapRelation (
		MapID int,
		[ParentID] int,
		[UltimateParentID] int,	
		[Source] varchar(50),
		[SourceID] int,	
		[Target] varchar(50),
		[TargetID] int,
	)

	CREATE NONCLUSTERED INDEX [CIX_TempMapRelation] ON #mapRelation ( MapID ASC, [UltimateParentID] ASC, [ParentID] ASC );
	
	insert into #maps
		(SourceObject, TargetObject)
		select distinct
			replace(cast(F_source.formattedValue as nvarchar(500)), @databaseName, '') as SourceObject						
			, replace(cast(F_target.formattedValue as nvarchar(500)), @databaseName, '') as TargetObject			
		from 
			FusionAttribute FA
			inner join Field F_source on F_source.ObjectType = 'FusionAttribute' and F_source.ObjectID = FA.ID and F_source.FieldTypeID = @sourceFieldTypeID -- MAP SOURCE FIELD VALUE
			inner join Field F_target on F_target.ObjectType = 'FusionAttribute' and F_target.ObjectID = FA.ID and F_target.FieldTypeID = @targetFieldTypeID -- TARGET SOURCE FIELD VALUE
		where 
			FA.FusionID = @fusionID
				and
			FA.FusionAttributeTypeID = @mapFusionAttributeTypeID
			--	and
		--	F_source.formattedValue like '%.cusip' --or F_source.formattedValue like '%.ticker' or F_source.formattedValue like '%.cntry_of%' -- **for testing to limit to just cusip**;
	
	-- check how many map records we have
	declare @mapRecordCount int;
	select @mapRecordCount = count(1) from #maps
	if @fusionTypeId > 0
		begin
			print 'Loaded [' + cast(@mapRecordCount as varchar) + '] map records';			
		end
	else
		begin
			raiserror('ERROR - Could not load any map records this is most likely because there are no corresponding fusionattributes for the markit source/target mappings.', 16, -1);
			return;
		end

			
	--set the Source objects 
	update	T
	set		T.SourceFusionAttributeID = S.ID, T.SourceFusionAttributeTypeID = S.FusionAttributeTypeID
	from	#maps T			
			inner join fusionattribute S on (S.TextPath = T.SourceObject and S.FusionID = @fusionID)

	--set the Target Objects
	update	T
	set		T.TargetFusionAttributeID = S.ID, T.TargetFusionAttributeTypeID = S.FusionAttributeTypeID
	from	#maps T			
			inner join fusionattribute S on (S.TextPath = T.TargetObject and S.FusionID = @fusionID)

	--remove any source objects that we cant find the fusion attribute for
	delete from #maps where SourceFusionAttributeID is null or TargetFusionAttributeID is null		
	
	--set the source parent objects
	update T
	set T.SourceParentObject = FA_p.TextPath, T.SourceParentObjectFusionAttributeID = FA_p.ID, T.SourceParentObjectFusionAttributeTypeID = FA_p.FusionAttributeTypeID
	from #maps T
		inner join fusionattribute FA on (FA.ID = T.SourceFusionAttributeID)
		inner join fusionattribute FA_p on (FA_p.ID = FA.ParentID)

	--set the target parent objects
	update T
	set T.TargetParentObject = FA_p.TextPath, T.TargetParentObjectFusionAttributeID = FA_p.ID, T.TargetParentObjectFusionAttributeTypeID = FA_p.FusionAttributeTypeID
	from #maps T
		inner join fusionattribute FA on (FA.ID = T.TargetFusionAttributeID)
		inner join fusionattribute FA_p on (FA_p.ID = FA.ParentID)

	-- remove any maps that reference same fusionattribute both sides
	delete from #maps where SourceFusionAttributeID = TargetFusionAttributeID;
	
	--this query adds in the view to table mapings
	-- add in any view column to table column records
	-- table / view maps for targets that are missing connection
	insert into #maps
		(SourceFusionAttributeID, SourceFusionAttributeTypeID, SourceObject, SourceParentObjectFusionAttributeID, SourceParentObject, SourceParentObjectFusionAttributeTypeID, TargetFusionAttributeID, TargetFusionAttributeTypeID, TargetObject, TargetParentObjectFusionAttributeID, TargetParentObject, TargetParentObjectFusionAttributeTypeID)
		select 	distinct
			m.TargetFusionAttributeID as SourceFusionAttributeID,
			m.TargetFusionAttributeTypeID as SourceFusionAttributeTypeID,
			m.TargetObject as SourceObject,
			m.TargetParentObjectFusionAttributeID as SourceParentObjectFusionAttributeID,
			m.TargetParentObject as SourceParentObject,
			m.TargetParentObjectFusionAttributeTypeID as SourceParentObjectFusionAttributeTypeID,
			T.id as TargetFusionAttributeID,
			T.fusionattributetypeid as TargetFusionAttributeTypeID,
			T.textpath as TargetObject,
			i.objectid as TargetParentObjectFusionAttributeID,
			T_p.name as TargetParentObject,
			T_p.fusionattributetypeid as TargetParentObjectFusionAttributeTypeID			
		 from 
			#maps m			
			inner join [intersect] i on (i.subjectid = m.TargetParentObjectFusionAttributeID and i.[subject] = 'FusionAttribute')	
			inner join fusionattribute T_p on (T_p.id = i.objectid)
			inner join fusionattribute T on(T.parentid = T_p.id and T.Textpath = T_p.TextPath + replace(m.TargetObject,m.TargetParentObject,'')) -- we are doing this to avoid messing with the name column that doesnt have an index
		where 
			m.TargetFusionAttributeTypeID = @viewColumnFusionAttributeTypeID
				and
			i.intersecttypeid = @viewTableIntersectTypeId
				and
			m.id not in(select m_2.id from #maps m_2 where (m_2.SourceFusionAttributeID = m.TargetFusionAttributeID and m_2.TargetFusionAttributeID = T.Id) or (m_2.TargetFusionAttributeID = m.TargetFusionAttributeID and m_2.SourceFusionAttributeID = T.id)) -- dont insert duplicates
	
	-- table / view maps for sources that are missing connection
	insert into #maps
		(TargetFusionAttributeID, TargetFusionAttributeTypeID, TargetObject, TargetParentObjectFusionAttributeID, TargetParentObject, TargetParentObjectFusionAttributeTypeID, SourceFusionAttributeID, SourceFusionAttributeTypeID, SourceObject, SourceParentObjectFusionAttributeID, SourceParentObject, SourceParentObjectFusionAttributeTypeID)
		select 	distinct
			m.SourceFusionAttributeID as TargetFusionAttributeID,
			m.SourceFusionAttributeTypeID as TargetFusionAttributeTypeID,
			m.SourceObject as TargetObject,
			m.SourceParentObjectFusionAttributeID as TargetParentObjectFusionAttributeID,
			m.SourceParentObject as TargetParentObject,
			m.SourceParentObjectFusionAttributeTypeID as TargetParentObjectFusionAttributeTypeID,
			T.id as SourceFusionAttributeID,
			T.fusionattributetypeid as SourceFusionAttributeTypeID,
			T.textpath as SourceObject,
			i.objectid as SourceParentObjectFusionAttributeID,
			T_p.name as SourceParentObject,
			T_p.fusionattributetypeid as SourceParentObjectFusionAttributeTypeID			
		 from 
			#maps m			
			inner join [intersect] i on (i.subjectid = m.SourceParentObjectFusionAttributeID and i.[subject] = 'FusionAttribute')	
			inner join fusionattribute T_p on (T_p.id = i.objectid)
			inner join fusionattribute T on(T.parentid = T_p.id and T.Textpath = T_p.TextPath + replace(m.SourceObject,m.SourceParentObject,'')) -- we are doing this to avoid messing with the name column that doesnt have an index
		where 
			m.SourceFusionAttributeTypeID = @viewColumnFusionAttributeTypeID
				and
			i.intersecttypeid = @viewTableIntersectTypeId
				and
			m.id not in(select m_2.id from #maps m_2 where (m_2.SourceFusionAttributeID = T.Id and m_2.TargetFusionAttributeID = m.SourceFusionAttributeID) or (m_2.TargetFusionAttributeID = T.id  and m_2.SourceFusionAttributeID = m.SourceFusionAttributeID)) -- dont insert duplicates
					
	-- end table / view maps

	

	--this query adds in the view to view mapings
	-- add in any view column to view column records
	-- view / view maps for targets that are missing connection

	insert into #maps
		(SourceFusionAttributeID, SourceFusionAttributeTypeID, SourceObject, SourceParentObjectFusionAttributeID, SourceParentObject, SourceParentObjectFusionAttributeTypeID, TargetFusionAttributeID, TargetFusionAttributeTypeID, TargetObject, TargetParentObjectFusionAttributeID, TargetParentObject, TargetParentObjectFusionAttributeTypeID)
		select 	distinct
			m.TargetFusionAttributeID as SourceFusionAttributeID,
			m.TargetFusionAttributeTypeID as SourceFusionAttributeTypeID,
			m.TargetObject as SourceObject,
			m.TargetParentObjectFusionAttributeID as SourceParentObjectFusionAttributeID,
			m.TargetParentObject as SourceParentObject,
			m.TargetParentObjectFusionAttributeTypeID as SourceParentObjectFusionAttributeTypeID,
			T.id as TargetFusionAttributeID,
			T.fusionattributetypeid as TargetFusionAttributeTypeID,
			T.textpath as TargetObject,
			i.objectid as TargetParentObjectFusionAttributeID,
			T_p.name as TargetParentObject,
			T_p.fusionattributetypeid as TargetParentObjectFusionAttributeTypeID			
		 from 
			#maps m			
			inner join [intersect] i on (i.objectid = m.TargetParentObjectFusionAttributeID and i.[object] = 'FusionAttribute')	
			inner join fusionattribute T_p on (T_p.id = i.subjectid)
			inner join fusionattribute T on(T.FusionId = @fusionId and T.deleted = 0 and T.parentid = T_p.id and T.Textpath = T_p.TextPath + replace(m.TargetObject,m.TargetParentObject,'')) -- we are doing this to avoid messing with the name column that doesnt have an index
		where 
			m.TargetFusionAttributeTypeID = @viewColumnFusionAttributeTypeID
				and
			i.intersecttypeid = @viewViewIntersectTypeId
				and
			m.id not in(select m_2.id from #maps m_2 where (m_2.SourceFusionAttributeID = m.TargetFusionAttributeID and m_2.TargetFusionAttributeID = T.Id) or (m_2.TargetFusionAttributeID = m.TargetFusionAttributeID and m_2.SourceFusionAttributeID = T.id)) -- dont insert duplicates

	-- view / view maps for sources that are missing connection
	insert into #maps
		(TargetFusionAttributeID, TargetFusionAttributeTypeID, TargetObject, TargetParentObjectFusionAttributeID, TargetParentObject, TargetParentObjectFusionAttributeTypeID, SourceFusionAttributeID, SourceFusionAttributeTypeID, SourceObject, SourceParentObjectFusionAttributeID, SourceParentObject, SourceParentObjectFusionAttributeTypeID)
		select 	distinct
			m.SourceFusionAttributeID as TargetFusionAttributeID,
			m.SourceFusionAttributeTypeID as TargetFusionAttributeTypeID,
			m.SourceObject as TargetObject,
			m.SourceParentObjectFusionAttributeID as TargetParentObjectFusionAttributeID,
			m.SourceParentObject as TargetParentObject,
			m.SourceParentObjectFusionAttributeTypeID as TargetParentObjectFusionAttributeTypeID,
			T.id as SourceFusionAttributeID,
			T.fusionattributetypeid as SourceFusionAttributeTypeID,
			T.textpath as SourceObject,
			i.objectid as SourceParentObjectFusionAttributeID,
			T_p.name as SourceParentObject,
			T_p.fusionattributetypeid as SourceParentObjectFusionAttributeTypeID			
		 from 
			#maps m			
			inner join [intersect] i on (i.subjectid = m.SourceParentObjectFusionAttributeID and i.[subject] = 'FusionAttribute')	
			inner join fusionattribute T_p on (T_p.id = i.objectid)
			inner join fusionattribute T on(T.FusionId = @fusionId and T.deleted = 0 and T.parentid = T_p.id and T.Textpath = T_p.TextPath + replace(m.SourceObject,m.SourceParentObject,'')) -- we are doing this to avoid messing with the name column that doesnt have an index
		where 
			m.SourceFusionAttributeTypeID = @viewColumnFusionAttributeTypeID
				and
			i.intersecttypeid = @viewViewIntersectTypeId
				and
			m.id not in(select m_2.id from #maps m_2 where (m_2.SourceFusionAttributeID = T.Id and m_2.TargetFusionAttributeID = m.SourceFusionAttributeID) or (m_2.TargetFusionAttributeID = T.id  and m_2.SourceFusionAttributeID = m.SourceFusionAttributeID)) -- dont insert duplicates


	-- end view / view maps


	-- populate the previous step id this also duplicates items that have multiple paths and is very important	

	insert into #mapRelation (MapID, ParentID)
		select M.ID, m_T.ID 
		from 
			#maps M
			left outer join #maps m_T on m_T.TargetFusionAttributeID = M.SourceFusionAttributeID;



	IF OBJECT_ID('tempdb..#levelMap') IS NOT NULL
		DROP TABLE #levelMap;
	

	;with C as
			(
			  select
				m.ID,			
				m.ID as [UltimateParentID],								
				'/' + convert(varchar(max), rtrim(m.ID)) + '/' [path]
			  from 
					#mapRelation mR
					inner join #maps m on (mR.MapID = m.ID)
			  where ParentID is null
			  union all
			  select 
					T.ID,					
					 C.[UltimateParentID] as [UltimateParentID],					 					 
					 C.[path] + cast(T.ID as varchar) + '/'
			  from 
				#mapRelation mR
				inner join #maps as T on (mR.MapID = T.ID)
				inner join C  
					on mR.ParentID = C.ID						
				where   C.path not like '%/' + cast(T.ID as varchar) + '/%'
			)
			select C.ID, C.[UltimateParentID]
			into #levelMap
			from C
			OPTION (MAXRECURSION 40) 


	update T
	set T.[UltimateParentID] = S.[UltimateParentID]
	from #mapRelation T	
	inner join #levelMap S on S.ID = T.MapID;
	

	-- find any object related to column as the object	
	insert into #objectmap (MapID, [Object], [ObjectID])
		select T.ID, OI.[subject], OI.[subjectid]
		from #maps T
		inner join [IntersectDetail] OI on OI.Subject <> 'FusionAttribute' and OI.Object = 'FusionAttribute' and OI.ObjectID in (T.SourceFusionAttributeID, T.TargetFusionAttributeID)  and OI.PredicateType = 8-- look for relation between non fusion object and source/target column

	-- find any business terms related to source
	update mR
	set mR.[source] = OI.[subject], mR.[sourceid] = OI.[subjectid]--, T.sourceintersectid = OI.ID
	from
		#mapRelation mR 
		inner join #maps T on mR.MapID = T.ID
		inner join [IntersectDetail] OI on OI.Subject <> 'FusionAttribute' and OI.Object = 'FusionAttribute' and OI.ObjectID = T.SourceParentObjectFusionAttributeID  and OI.PredicateType = 8 

	
	-- find any business terms related to target
	update mR
	set mR.[target] = OI.[subject], mR.[targetid] = OI.[subjectid]--, T.targetintersectid = OI.ID
	from
		#mapRelation mR 
		inner join #maps T on mR.MapID = T.ID
		inner join [IntersectDetail] OI on OI.Subject <> 'FusionAttribute' and OI.Object = 'FusionAttribute' and OI.ObjectID = T.TargetParentObjectFusionAttributeID and OI.PredicateType = 8
		
	-- update the objects for each path to be the same	
	insert into #objectmap (MapID, [Object], [ObjectID])
		select T.MapID, SO.[object], SO.[objectID]
		from #mapRelation T
		inner join #mapRelation S on T.UltimateParentID = S.UltimateParentID
		inner join #objectmap SO on S.MapID = SO.MapID
		left join #objectmap T_O on (T.MapID = T_O.MapID and T_O.[object] is null);
	
	
	--take any sources with null targets find the next target

	WITH hierarchy (id, [target], [targetid], [source], [sourceid], [Path]) AS
	(
		SELECT 
			mapid, [target], [targetid], [source], [sourceid],			
			'/' + convert(varchar(max), rtrim(mapid)) + '/' [path]
		FROM #mapRelation
		WHERE [parentid] is null

		UNION ALL

		SELECT 
			mc.mapid, 
			coalesce(mc.[target], mc.[source], gps.[target]) as [target], 
			coalesce(mc.targetid, mc.sourceid, gps.targetid) as [targetid], 
			coalesce(mc.[source], gps.[target], gps.[source]) as [source], 
			coalesce(mc.sourceid, gps.targetid, gps.sourceid) as [targetid],			
			gps.[path] + rtrim(mc.mapid) + '/'
		FROM #mapRelation mc
		JOIN hierarchy gps ON gps.id = mc.parentid		
		where   gps.path not like '%/' + cast(mc.mapID as varchar) + '/%'	  
	)
	UPDATE T
	set T.[target] = cte.[target], T.[targetid] = cte.[targetid], T.[source] = cte.[source], T.[sourceid] = cte.[sourceid]
	from #mapRelation T
	inner join 
		hierarchy cte
	on cte.id = T.mapid
	OPTION (MAXRECURSION 50)
			
	-- generate relationships for each unique object / source that dont exist

	update T
	set T.[sourceintersectid] = OI.ID
	from #objectmap T
		inner join #maprelation M on (T.MapID = M.MapID)
		inner join [IntersectDetail] OI on OI.[Subject] = M.[Source] and OI.SubjectID = M.[SourceID] and OI.[Object] = T.[Object] and OI.[ObjectID] = T.[ObjectID];

	update T
	set T.[sourceintersectid] = OI.ID
	from #objectmap T
		inner join #maprelation M on (T.MapID = M.MapID)
		inner join [IntersectDetail] OI on OI.[Object] = M.[Source] and OI.ObjectID = M.[SourceID] and OI.[Subject] = T.[Object] and OI.[SubjectID] = T.[ObjectID] and T.sourceintersectid is null
	
	-- add any missing relations to source / object
	insert into [intersect] (IntersectTypeID, [Subject], SubjectID, [Object], ObjectID, CreatedBy, CreatedOn, UpdatedBy, UpdatedOn, [Owner])
		select distinct
			(select top 1 i_t.ID from [intersecttype] i_t where (i_t.[object] = c_s.objecttype and i_t.[subject] = c_t.objecttype and i_t.objectid = c_s.objecttypeid and i_t.subjectid = c_t.objecttypeid))			
			,T.[Source]
			,T.[SourceID]
			,OM.[Object]
			,OM.[ObjectID]
			,0,getutcdate(),0,getutcdate(),'MARKIT LINEAGE'
		from #maprelation T
		inner join #objectmap OM on (T.MapID = OM.MapID)
		inner join [cache].[objectdetails] c_s on (c_s.[object] = OM.[object] and c_s.[objectid] = OM.[objectid])
		inner join [cache].[objectdetails] c_t on (c_t.[object] = T.[source] and c_t.[objectid] = T.[sourceid])		
		where OM.sourceIntersectID is null;
	
	update OM
	set OM.[sourceintersectid] = OI.ID
	from #objectmap OM
		inner join #maprelation T on (OM.MapID = T.MapID)		
		inner join [IntersectDetail] OI on OI.[Subject] = T.[Source] and OI.SubjectID = T.[SourceID] and OI.[Object] = OM.[Object] and OI.[ObjectID] = OM.[ObjectID] and OM.sourceintersectid is null;

	
	-- generate relationships for each unique object / target that dont exist	
	update OM
	set OM.[targetintersectid] = OI.ID
	from #objectmap OM
		inner join #maprelation T on (OM.MapID = T.MapID)
		inner join [IntersectDetail] OI on OI.[Subject] = T.[Target] and OI.SubjectID = T.[TargetID] and OI.[Object] = OM.[Object] and OI.[ObjectID] = OM.[ObjectID]
		
	update OM
	set OM.[targetintersectid] = OI.ID
	from #objectmap OM
		inner join #maprelation T on (OM.MapID = T.MapID)
		inner join [IntersectDetail] OI on OI.[Object] = T.[Target] and OI.ObjectID = T.[TargetID] and OI.[Subject] = OM.[Object] and OI.[SubjectID] = OM.[ObjectID] and OM.targetintersectid is null;

	-- add any missing relations to source / object
	insert into [intersect] (IntersectTypeID, [Subject], SubjectID, [Object], ObjectID, CreatedBy, CreatedOn, UpdatedBy, UpdatedOn, [Owner])
		select distinct
			(select top 1 i_t.ID from [intersecttype] i_t where (i_t.[object] = c_s.objecttype and i_t.[subject] = c_t.objecttype and i_t.objectid = c_s.objecttypeid and i_t.subjectid = c_t.objecttypeid))				
			,T.[target]
			,T.[targetID]
			,OM.[Object]
			,OM.[ObjectID]
			,0,getutcdate(),0,getutcdate(),'MARKIT LINEAGE'
		from #maprelation T
		inner join #objectmap OM on (T.MapID = OM.MapID)
		inner join [cache].[objectdetails] c_s on (c_s.[object] = OM.[object] and c_s.[objectid] = OM.[objectid])
		inner join [cache].[objectdetails] c_t on (c_t.[object] = T.[target] and c_t.[objectid] = T.[targetid])		
		where OM.targetintersectid is null;
		
	update OM
	set OM.[targetintersectid] = OI.ID
	from #objectmap OM
		inner join #maprelation T on (OM.MapID = T.MapID)
		inner join [IntersectDetail] OI on OI.[Subject] = T.[Target] and OI.SubjectID = T.[TargetID] and OI.[Object] = OM.[Object] and OI.[ObjectID] = OM.[ObjectID] and OM.targetintersectid is null;
	

	/*testing only!!*/			
--	select * from #maps
--	select * from #mapRelation
--	SELECT * FROM #OBJECTMAP
--	return
	/*end testing only*/
	

	print 'Removing any prior generated Markit Lineage map records';

	-- clear any previous values from map rule item map item table	
	delete from mapruleitemmapitem where [owner] = 'MARKIT LINEAGE';

	print 'Inserting new map records';
	-- insert mapping data
	
	Declare @MapItemIDList Table(MapItemID int, sourceintersectid int, targetintersectid int);
	Declare @MapRuleItemIDList Table(MapRuleItemID int, MapID Int);
	
	-- load any existing map item instances
	update T
	set T.MapItemID = mi.ID
	from #objectmap T
		inner join mapitem mi on(T.sourceintersectid = mi.SourceIntersectID and T.targetintersectid = mi.TargetIntersectID and mi.[Owner] = 'MARKIT LINEAGE'); 

	-- insert map records
	MERGE
	INTO    mapitem mi
	USING   (			
			select distinct sourceintersectid, targetintersectid FROM #objectmap where (sourceintersectid is not null and targetintersectid is not null) and sourceintersectid != targetintersectid and mapitemid is null
			) S
	ON      (1 = 0)
	WHEN NOT MATCHED THEN
	INSERT  (SourceIntersectID, TargetIntersectID, CreatedBy, CreatedOn, UpdatedBy, UpdatedOn, [Owner])
	VALUES  (S.sourceintersectid, S.targetintersectid, 0, getutcdate(), 0, getutcdate(), 'MARKIT LINEAGE')
	OUTPUT  INSERTED.ID, S.sourceintersectid, S.targetintersectid into @MapItemIDList;

	--update map item id from main temp table
	update T
	set T.mapitemid = MI.MapItemID
	from #objectmap T
		inner join @MapItemIDList MI on (MI.sourceintersectid = T.sourceintersectid and MI.targetintersectid = T.targetintersectid)
		
	-- delete any mapitem records that are not in objectmap that are markit lineage
	delete from mapitem where [owner] = 'MARKIT LINEAGE' and id not in (select mapitemid from #objectmap);
	
	-- load id's of existing mapruleitems
	update T
	set T.mapruleitemid = S.id
	from #maps T
		inner join [dbo].[mapruleitem] S on (S.[owner] = 'MARKIT LINEAGE' and S.SourceFusionAttributeID = T.SourceFusionAttributeID and S.TargetFusionAttributeID = T.TargetFusionAttributeID);
	
	-- insert the mapruleitem records
	MERGE
	INTO    mapruleitem mri
	USING   (
			select SourceFusionAttributeID, TargetFusionAttributeID, ID from #maps where mapruleitemid is null
			) S
	ON      (1 = 0)
	WHEN NOT MATCHED THEN
	INSERT  (SourceFusionAttributeID, TargetFusionAttributeID, CreatedBy, CreatedOn, UpdatedBy, UpdatedOn, [Owner])
	VALUES  (S.SourceFusionAttributeID, S.TargetFusionAttributeID, 0, getutcdate(), 0, getutcdate(), 'MARKIT LINEAGE')
	OUTPUT  INSERTED.ID, S.ID into @MapRuleItemIDList;
	
	--update map rule item id from main temp table
	update T
	set T.MapRuleItemID = MI.MapRuleItemID
	from #maps T
		inner join @MapRuleItemIDList MI on (MI.MapID = T.ID);

	-- delete any mapitem records that are not in objectmap that are markit lineage
	delete from mapruleitem where [owner] = 'MARKIT LINEAGE' and id not in (select MapRuleItemID from #maps);
			
	--insert mapruleitemmapitem records
	insert into mapruleitemmapitem 
		(MapRuleItemID, MapItemID, [Owner])
		SELECT distinct M.MapRuleItemID, OM.MapItemID , 'MARKIT LINEAGE'
		FROM #maps M 
		inner join #objectmap OM on(M.ID = OM.MapID)
		where M.MapRuleItemID is not null and OM.MapItemID is not null;	

	declare @mapruleitemmapitemCount int;
	select @mapruleitemmapitemCount = count(1) from mapruleitemmapitem where [owner] = 'MARKIT LINEAGE'
	print 'Inserted [' + cast(@mapruleitemmapitemCount as varchar) + '] mapruleitemmapitem records';			

	declare @mapruleitemCount int;
	select @mapruleitemCount = count(1) from mapruleitem where [owner] = 'MARKIT LINEAGE'
	print 'Inserted [' + cast(@mapruleitemCount as varchar) + '] mapruleitem records';			

	declare @mapitemCount int;
	select @mapitemCount = count(1) from mapitem where [owner] = 'MARKIT LINEAGE'
	print 'Inserted [' + cast(@mapitemCount as varchar) + '] mapitem records';
			
end
GO

CREATE procedure [utility].[CalculateScores]
--declare
	@Object varchar(50) = NULL,
	@ObjectID int = NULL,
	@Date date = null--'04/17/2017'
--set @Object = 'Artifact'
--set @ObjectID = 16437 --select * from Artifact where ID = 16437
as
begin
	SET NOCOUNT ON;

	if @Date is null
	begin
		set @Date = cast(getutcdate() as Date)
	end

	DROP TABLE IF EXISTS #MetricTypes

	create table #MetricTypes (
		ScoreTypeID int,
		ScoreTypeMetricID int,
		ScoreTypeMetricVersionID int,
		ObjectType varchar(50),
		ObjectTypeID int,
		CheckType int,
		Configuration xml,
		MaximumScore int,
		Object varchar(50),
		ObjectID int
	)
	insert into #MetricTypes
		select	M.ScoreTypeID,
				M.ID as ScoreTypeMetricID,
				V.ID as ScoreTypeMetricVersionID,
				M.Object as ObjectType,
				M.ObjectID as ObjectTypeID,
				M.CheckType,
				M.Configuration,
				M.MaximumScore,
				O.Object,
				O.ObjectID
		from	ScoreType T
				inner join ScoreTypeMetric M on M.ScoreTypeID = T.ID  and M.Deleted = 0
				inner join	(
							select		ScoreTypeMetricID,
										max(IV.ID) as ID,
										max(IV.UpdatedOn) as UpdatedOn
							from		ScoreTypeMetricVersion IV
							group by	IV.ScoreTypeMetricID
							) V on V.ScoreTypeMetricID = M.ID
				inner join cache.[Object] O on O.ObjectType = M.Object and O.ObjectTypeID = M.ObjectID and ( (O.Object = @Object and O.ObjectID = @ObjectID) OR @ObjectID is null)

	DROP TABLE IF EXISTS #ScoreMetrics
	create table #ScoreMetrics (
		ScoreID bigint null,
		Object varchar(50),
		ObjectID int,
		ScoreTypeID int,
		[Date] date,
		ScoreTypeMetricVersionID int,
		MetricValue decimal(6,3),
	)

	insert into #ScoreMetrics
		select	NULL,
				T.Object,
				T.ObjectID,
				T.ScoreTypeID,
				@Date,
				T.ScoreTypeMetricVersionID,
				case 
					when T.CheckType = 1 and f.value('(ObjectType/text())[1]', 'varchar(50)') = 'AttributeType' and C1_A.ValueExists <> 0 then T.MaximumScore
					when T.CheckType = 1 and f.value('(ObjectType/text())[1]', 'varchar(50)') = 'ResponsibilityType' and C1_R.ValueExists <> 0 then T.MaximumScore
					when T.CheckType = 2 then C2.Multiplier * T.MaximumScore
					when T.CheckType = 3 and T.ObjectType = 'ArtifactType' and C3_S.ValueExists <> 0 then T.MaximumScore
					when T.CheckType = 3 and f.value('(PropertyName/text())[1]', 'varchar(250)') <> 'Status' and C3_F.ValueExists <> 0 then T.MaximumScore
					when T.CheckType = 4 and f.value('(PropertyName/text())[1]', 'varchar(250)') = 'Description' and C4_D.ValueExists <> 0 then T.MaximumScore
					when T.CheckType = 4 and f.value('(PropertyName/text())[1]', 'varchar(250)') <> 'Description' and C4_F.ValueExists <> 0 then T.MaximumScore
					when T.CheckType = 5 and C5_R.ValueExists <> 0 then T.MaximumScore
					when T.CheckType = 6 and C6_F.ValueExists <> 0 then T.MaximumScore
					when T.CheckType = 7 and C7_R.AverageScore is not null then (C7_R.AverageScore / 100) * T.MaximumScore
					when T.CheckType = 8 and C8_O.AverageScore is not null then (C8_O.AverageScore / 100) * T.MaximumScore
					when T.CheckType = 10 and C10_P.ValueExists <> 0 then T.MaximumScore
					else 0
				end as MetricValue
		from	#MetricTypes T
				cross apply Configuration.nodes('/fields') as F(f)
				outer apply (
							select		coalesce(M.Score, 0) as Multiplier
							from		TestExternalMetric M
							where		M.Object = T.[Object]
										and M.ObjectID = T.ObjectID 
										and M.MetricVersionID = T.ScoreTypeMetricVersionID
										and T.CheckType = 2
							) C2
				outer apply (
							select		ISNULL(AttributeTypeID, 0) as ValueExists
							from		Attribute 
							where		ObjectType = T.[Object] 
										and ObjectID = T.ObjectID 
										and AttributeTypeID = f.value('(ObjectID/text())[1]', 'int')
										and f.value('(ObjectType/text())[1]', 'varchar(50)') = 'AttributeType'
										and T.CheckType = 1
							group by	AttributeTypeID, ObjectType, ObjectID
							) C1_A
				outer apply (
							select		ISNULL(ResponsibilityTypeID, 0) as ValueExists
							from		[cache].[ResponsibilityItem]
							where		[Object] = T.[Object] 
										and ObjectID = T.ObjectID 
										and ResponsibilityTypeID = f.value('(ObjectID/text())[1]', 'int')
										and f.value('(ObjectType/text())[1]', 'varchar(50)') = 'ResponsibilityType'
										and T.CheckType = 1
							group by	ResponsibilityTypeID, [Object], ObjectID
							) C1_R
				outer apply (
							select		CASE 
											when [Status] = f.value('(PropertyValue/text())[1]', 'nvarchar(4000)') then 1
											else 0
										END as ValueExists
							from		Artifact
							where		T.ObjectType = 'ArtifactType'
										and ID = T.ObjectID
										and f.value('(PropertyName/text())[1]', 'varchar(250)') = 'Status'
										and T.CheckType = 3
							) C3_S
				outer apply (
							select		CASE 
											when F.FormattedValue = f.value('(PropertyValue/text())[1]', 'nvarchar(4000)') then 1
											else 0
										END as ValueExists									
							from		Field F
										inner join FieldType FT on FT.[Object] = T.ObjectType and FT.ObjectID = T.ObjectTypeID 
																and F.[ObjectType] = T.[Object] and F.ObjectID = T.ObjectID
																and FT.Name = f.value('(PropertyName/text())[1]', 'varchar(250)') 
																and T.CheckType = 3
							) C3_F
				outer apply (
							select		case 
											when Description is null then 0
											when LEN(Description) < 25 then 0
											else 1
										end as ValueExists
							from		cache.ObjectDetails
							where		[Object] = T.[Object] and ObjectID = T.ObjectID
										and f.value('(PropertyName/text())[1]', 'varchar(250)') = 'Description'
										and T.CheckType = 4
							) C4_D
				outer apply (
							select		CASE 
											when F.FormattedValue is not null then 1
											else 0
										END as ValueExists									
							from		Field F
										inner join FieldType FT on FT.[Object] = T.ObjectType and FT.ObjectID = T.ObjectTypeID 
																and F.[ObjectType] = T.[Object] and F.ObjectID = T.ObjectID
																and FT.Name = f.value('(PropertyName/text())[1]', 'varchar(250)') 
																and T.CheckType = 4
							) C4_F
				outer apply (
							select		case 
											when COUNT(1) > 0 then 1
											else 0
										end as ValueExists
							from		[Intersect] IR
										inner join IntersectType IRT on IRT.ID = IR.IntersectTypeID and (
																										(IR.Subject = T.Object and IR.SubjectID = T.ObjectID) OR 
																										(IR.Object = T.Object and IR.ObjectID = T.ObjectID)
																										)
										cross apply T.Configuration.nodes('/fields/CheckObjects') as R(r) 
							where		r.value('(Object/Type/text())[1]', 'varchar(50)') = case 
											when (IR.Subject = T.Object and IR.SubjectID = T.ObjectID) then IRT.Object 
											else IRT.Subject
										end
										and r.value('(Object/ID/text())[1]', 'int') = case 
											when (IR.Subject = T.Object and IR.SubjectID = T.ObjectID) then IRT.ObjectID
											else IRT.SubjectID
										end
										and T.CheckType = 5
							) C5_R
				outer apply (
							select		ISNULL(ArtifactID, 0) as ValueExists
							from		FusionOwner
							where		ArtifactID = T.ObjectID
										and T.CheckType = 6
							group by	ArtifactID
							) C6_F
				outer apply (
							select	(sum(S.Value) / Count(1)) as [AverageScore],
									max(S.Date) as [Date] 
							from	[Intersect] I
									inner join IntersectType IT on	IT.ID = I.IntersectTypeID
																	and (
																		(I.Subject = T.[Object] and I.SubjectID = T.ObjectID) OR
																		(I.Object = T.[Object] and I.ObjectID = T.ObjectID)
																		)
																	and (
																		f.value('(ObjectType/text())[1]', 'varchar(25)') = case 
																			when (I.Subject = T.[Object] and I.SubjectID = T.ObjectID) then IT.Object
																			else IT.Subject
																		end 
																		and f.value('(ObjectID/text())[1]', 'int') = case 
																			when (I.Subject = T.[Object] and I.SubjectID = T.ObjectID) then IT.ObjectID
																			else IT.SubjectID
																		end
																		)
									left join Score S on	S.Object =	case 
																			when I.Subject = T.Object and I.SubjectID = T.ObjectID then I.Object
																			else I.Subject
																		end 
															and S.ObjectID =	case 
																					when I.Subject = T.Object and I.SubjectID = T.ObjectID then I.ObjectID
																					else I.SubjectID
																				end
															and S.ScoreTypeID = f.value('(ScoreTypeID/text())[1]', 'int')
							where	T.CheckType = 7
							) C7_R	-- ROLLUP VIA RELATIONSHIPS
				outer apply (
							select	(sum(S.Value) / Count(1)) as [AverageScore],
									max(S.Date) as [Date] 
							from	[cache].[Responsibilities] R
									left join Score S on S.Object = R.Object and S.ObjectID = R.ObjectID --and S.ScoreTypeID = f.value('(ScoreTypeID/text())[1]', 'int')
							where	T.CheckType = 8
									and R.ResponsibleObject = T.[Object] 
									and R.ResponsibleObjectID = T.ObjectID
									and R.ObjectType = f.value('(ObjectType/text())[1]', 'varchar(25)') 
									and R.ObjectTypeID = f.value('(ObjectID/text())[1]', 'int')
							) C8_O	-- ROLLUP VIA OWNERSHIP
				outer apply (
							select	case 
										when COUNT(1) > 0 then 1
										else 0
									end as ValueExists
							from	[Intersect] I
									inner join IntersectType IT on IT.ID = I.IntersectTypeID and 
																IT.PredicateID = f.value('(Predicate/text())[1]', 'int') and 
																(
																(I.Subject = T.Object and I.SubjectID = T.ObjectID) OR
																(I.Object = T.Object and I.ObjectID = T.ObjectID)
																)
							where	T.CheckType = 10
							) C10_P	-- PREDICATE CHECK

--drop table #ScoreMetrics
--select * from #ScoreMetrics

	-- Gets results from merge statement below (OUTPUT)
	DROP TABLE IF EXISTS #Scores
	create table #Scores (ScoreID bigint, Object varchar(50), ObjectID int, ScoreTypeID int, Date date, [Action] varchar(15), CurrentScore int not null, NewScore int null)

	MERGE	Score AS T
	USING	(
			select		Object,
						ObjectID,
						ScoreTypeID,
						Date
			from		#ScoreMetrics
			group by	Object,
						ObjectID,
						ScoreTypeID,
						Date
			) AS S
	ON		(
			T.ScoreTypeID = S.ScoreTypeID
			and T.Object = S.Object
			and T.ObjectID = S.ObjectID
			and T.Date = S.Date
			)
	WHEN MATCHED THEN
		UPDATE	
		SET		T.Date = S.Date
	WHEN NOT MATCHED THEN	
		INSERT	
		VALUES	(S.Object, S.ObjectID, S.ScoreTypeID, S.Date, 0)
	OUTPUT inserted.ID, S.Object, S.ObjectID, S.ScoreTypeID, S.Date, $Action, inserted.Value, null into #Scores;

	--update the ScoreID column based on merge above.
	update	T
	set		T.ScoreID = S.ScoreID
	from	#ScoreMetrics T
			inner join #Scores S on S.Object = T.Object and S.ObjectID = T.ObjectID and S.ScoreTypeID = T.ScoreTypeID and S.Date = T.Date; 

	-- merge the results into the ScoreMetric table.
	MERGE	ScoreMetric AS T
	USING	(
			select	distinct
					ScoreID,
					ScoreTypeMetricVersionID,
					MetricValue
			from	#ScoreMetrics
			) AS S
	ON		(
			T.ScoreID = S.ScoreID
			and T.ScoreTypeMetricVersionID = S.ScoreTypeMetricVersionID
			)
	WHEN MATCHED THEN
		UPDATE	
		SET		T.Value = coalesce(S.MetricValue, 0)
	WHEN NOT MATCHED THEN	
		INSERT	
		VALUES	(S.ScoreID, S.ScoreTypeMetricVersionID, coalesce(S.MetricValue, 0));

	update	T
	set		T.Value = coalesce(S.Value, 0)
	from	Score T
	inner join	(
				select		CAST(ROUND( (SUM(MetricValue) / SUM(V.MaximumScore)) * 100, 0) as int) as Value,
							ScoreID
				from		#ScoreMetrics SM
							inner join ScoreTypeMetricVersion V on V.ID = SM.ScoreTypeMetricVersionID
				group by	ScoreID
				) S on S.ScoreID = T.ID;

	-- Now get which scores changed. 
	update	T
	set		T.NewScore = NS.Value
	from	#Scores T
			OUTER APPLY	(
						SELECT		TOP 1 
									*
						FROM		[Score]
						WHERE		Object = T.Object and ObjectID = T.ObjectID and ScoreTypeID = T.ScoreTypeID
						ORDER BY	[Date] DESC
						) NS;

	insert into [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
        select	'EventTopicNotification', 
				'<fields><ChangeType>ScoreUpdate</ChangeType><ObjectType>' + O.ObjectType + '</ObjectType><ObjectTypeID>' + cast(O.ObjectTypeID as varchar) + '</ObjectTypeID><Score>' + cast(S.NewScore as varchar) + '</Score></fields>',
				S.Object, 
				S.ObjectID
		from	#Scores S
				inner join cache.Object O on O.Object = S.Object and O.ObjectID = S.ObjectID
		where	S.CurrentScore <> S.NewScore
				and S.[Action] = 'UPDATE';
end
GO

CREATE procedure [utility].[GetOwnersForWorkflowV2]
	@workflowID int,
	@workflowStepID int = 0
as
begin
	declare @objectId int,			
			@objectType varchar(50),
			@responsibilityTypeID int;

	declare @tbl table (ResourceID int, FirstName nvarchar(250), LastName nvarchar(250), Email nvarchar(500), Username nvarchar(500), DateLastLoggedIn datetime null, ResourceTypeID int, Status nvarchar(25))

	select @objectType = object, @objectId = objectid from [workflow].[eventregistration] where typeid = @workflowID;
	
	--get the responsibility for this step from the settings of the step
	select @responsibilityTypeID = settings.value('(/settings/ResponsibilityTypeID)[1]', 'int') from [workflow].[VersionStep] where id = @workflowStepID
	
		--1. Check for vocabulary owners
	insert into @tbl
		select	R.ResourceID, R.FirstName, R.LastName, R.Email, R.Email, R.DateLastLoggedIn, 1 as ResourceTypeID, R.Status 
		from	ResponsibilityDetail RD 					
					inner join reporting.Global_Resource R 
						on RD.ObjectType = @objectType
						and RD.ObjectID = @objectId
						and RD.ResponsibilityTypeID = @responsibilityTypeID
						and	(
								(RD.ResponsibleObjectType = 'Group' and R.ResourceID = RD.PrimaryOwnerResourceID) or 
								(RD.ResponsibleObjectType = 'Resource' and R.ResourceID = RD.ResponsibleObjectID)
							)
						and R.Email not like '%?subject=%' and R.Status = 'Active';

	
	
	-- if noone found email admins
	if not exists (select 1 from @tbl)
		begin
			insert into @tbl
				select 
					R.ResourceID, R.FirstName, R.LastName, R.Email, R.Email, R.DateLastLoggedIn, 1 as ResourceTypeID, R.Status 
				from 
					reporting.Global_Resource R where isadministrator = 1 and status = 'Active'
		end
	

	select * from @tbl
end
GO

CREATE FUNCTION [workflow].[ConditionToPlainText] 
(
	@ConditionXml xml	
)
RETURNS varchar(500)
AS
BEGIN	
	DECLARE @PlainText varchar(500) = '';
	DECLARE @Value varchar(500) = '';
	DECLARE @Operator varchar(500) = '';
	DECLARE @FieldName varchar(500) = '';
	DECLARE @FieldTypeID int;

	SELECT 
		@FieldTypeID = Child.value('(Condition[1]/@FieldTypeID)', 'int'),
		@Operator = Child.value('(Condition[1])/@Operator', 'Varchar(50)'),
		@Value = Child.value('(Condition[1])/@Value', 'Varchar(50)')
	FROM
		@ConditionXml.nodes('/Conditions') AS N(Child);


	if (@FieldTypeID > 0)
	begin
		select @FieldName = FriendlyName from fieldtype where id = @FieldTypeID;
	end

	RETURN @FieldName + ' ' +  @Operator + ' ' + @Value;
END


/*
<Conditions>
  <Condition FieldTypeID="53026" Operator="&lt;=" Value="15" ValueType="D" />
</Conditions>
*/
GO

CREATE CLUSTERED INDEX [CIX_Analytics_Statistic]
    ON [analytics].[Statistic]([Object] ASC, [ObjectID] ASC);
GO

CREATE NONCLUSTERED INDEX [IX_Analytics_Statistic_Object]
    ON [analytics].[Statistic]([Object] ASC, [ObjectID] ASC);
GO

CREATE NONCLUSTERED INDEX [IX_Analytics_Statistic_Timestamp]
    ON [analytics].[Statistic]([Timestamp] ASC);
GO


update FieldType set IsEditable = 0 where Object = 'FusionAttributeType'
GO

ALTER TABLE [dbo].[Map]  WITH CHECK ADD  CONSTRAINT [FK_Map_MapType] FOREIGN KEY([MapTypeID])
REFERENCES [dbo].[MapType] ([ID])
GO

ALTER TABLE [dbo].[Map] CHECK CONSTRAINT [FK_Map_MapType]
GO





