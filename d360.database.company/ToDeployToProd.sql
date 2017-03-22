DROP TABLE [dbo].[WorkflowGlobalParameter]
GO

DROP TABLE [dbo].[WorkflowInbox]
GO

DROP TABLE [dbo].[WorkflowProcessInstance]
GO

DROP TABLE [dbo].[WorkflowProcessInstancePersistence] 
GO

DROP TABLE [dbo].[WorkflowProcessInstanceStatus]
GO

DROP TABLE [dbo].[WorkflowProcessScheme]
GO

DROP TABLE [dbo].[WorkflowProcessTimer]
GO

DROP TABLE [dbo].[WorkflowProcessTransitionHistory]
GO

DROP TABLE [dbo].[WorkflowRuntime]
GO

DROP TABLE [dbo].[WorkflowScheme]
GO

DROP PROCEDURE [DropWorkflowInbox]
GO

DROP PROCEDURE [DropWorkflowProcess]
GO

DROP PROCEDURE [DropWorkflowProcesses]
GO

DROP PROCEDURE [dbo].[GetRenderedTemplateBody]
GO

DROP PROCEDURE [spWorkflowProcessResetRunningStatus]
GO

DROP TYPE [dbo].[IdsTableType]
GO

CREATE NONCLUSTERED INDEX [IX_Artifact_ArtifactTypeID_TaxonomyTypeID]
    ON [dbo].[Artifact]([ArtifactTypeID] ASC, [TaxonomyTypeID] ASC)
    INCLUDE([ID], [TextPath]);
GO


drop INDEX [CIX_FusionAttribute] on FusionAttribute
GO


--add sequence to fusion attribute table

-- add new column
ALTER TABLE dbo.fusionattribute ADD IDNew int NULL
GO

-- set new columns value = to old id
UPDATE dbo.fusionattribute
SET [IDNew] = [ID]
GO

-- drop fk constraint
alter table dbo.ruleresult
drop constraint FK_RuleResult_FusionAttribute
go

ALTER TABLE [fusion].[RulePromotion] DROP CONSTRAINT [FK_FusionRulePromotion_FusionAttribute]
GO

-- drop the constraint
ALTER TABLE dbo.fusionattribute
DROP CONSTRAINT PK_FusionAttribute;
GO

-- drop index
DROP INDEX [dbo].fusionattribute.IX_FusionAttribute_FusionAttributeTypeID;
GO

--DROP INDEX [dbo].fusionattribute.CIX_FusionAttribute;
--GO

--drop identity column
ALTER TABLE dbo.fusionattribute
DROP COLUMN [ID] ;
GO


-- rename temp id to regular id
EXEC sp_rename 'dbo.fusionattribute.IDNew',
'ID', 'COLUMN';
GO

-- alter the column to be not null
ALTER TABLE dbo.fusionattribute ALTER COLUMN [ID] int NOT NULL ;
GO

-- add back constraint
ALTER TABLE dbo.fusionattribute ADD CONSTRAINT PK_FusionAttribute PRIMARY KEY CLUSTERED ( [ID] ASC);
GO

-- get max value from id column
SELECT MAX(ID) FROM dbo.fusionattribute ;
GO

-- THIS STEP NEEDS TO BE AUTOMATTED WITH PREVIOUS!!! -- 
-- create sequence with max plus one from above.
CREATE SEQUENCE dbo.FusionAttribute_Seq AS int 
START WITH 2047323 -- value from previous step + 1
INCREMENT BY 1;
GO

-- add constraint that uses sequence
ALTER TABLE dbo.FusionAttribute
ADD CONSTRAINT Const_FusionAttributeSeq DEFAULT (NEXT VALUE FOR dbo.FusionAttribute_Seq)
FOR ID;
GO

-- add fk back that we removed
ALTER TABLE [dbo].[RuleResult]  WITH CHECK ADD  CONSTRAINT [FK_RuleResult_FusionAttribute] FOREIGN KEY([FusionAttributeID])
REFERENCES [dbo].[FusionAttribute] ([ID])
GO

-- add back index we deleted IX_FusionAttribute_FusionAttributeTypeID

ALTER TABLE [fusion].[RulePromotion]  WITH CHECK ADD  CONSTRAINT [FK_FusionRulePromotion_FusionAttribute] FOREIGN KEY([FusionAttributeID])
REFERENCES [dbo].[FusionAttribute] ([ID])
ON DELETE CASCADE
GO

ALTER TABLE [fusion].[RulePromotion] CHECK CONSTRAINT [FK_FusionRulePromotion_FusionAttribute]
GO

CREATE NONCLUSTERED INDEX [IX_FusionAttribute_FusionAttributeTypeID]
    ON [dbo].[FusionAttribute]([FusionAttributeTypeID] ASC)
    INCLUDE([ID], [Name]);
go

CREATE NONCLUSTERED INDEX [IX_FusionAttribute_FusionID_Deleted_ParentID]
    ON [dbo].[FusionAttribute]([FusionID] ASC, [Deleted] ASC, [ParentID] ASC);
GO

delete from [cache].[object] where [object] = 'FusionAttribute';
go

ALTER TABLE FusionStatusLog ADD [FullRefresh]     BIT              CONSTRAINT [DF_FusionStatusLog_FullRefresh] DEFAULT ((0)) NOT NULL
GO

CREATE NONCLUSTERED INDEX [IX_Intersect_IntersectTypeID_Subject_Object]
    ON [dbo].[Intersect]([IntersectTypeID] ASC, [Subject] ASC, [SubjectID] ASC, [Object] ASC, [ObjectID] ASC);
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
        select 'Add', [queue].WriteIndexXml('', Subject, SubjectID, UpdatedBy), 'Intersect', ID from inserted;

	INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
        select 'Add', [queue].WriteIndexXml('', Object, ObjectID, UpdatedBy), 'Intersect', ID from inserted;
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
        select 'Update', [queue].WriteIndexXml('', Subject, SubjectID, UpdatedBy), 'Intersect', ID from inserted;

	INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
        select 'Update', [queue].WriteIndexXml('', Object, ObjectID, UpdatedBy), 'Intersect', ID from inserted;
END
GO

alter table MapItem add [Owner]             VARCHAR (100) NULL
go

alter table MapRuleItem add [Owner]             VARCHAR (100) NULL
go

alter table MapRuleItemMapItem add [Owner]             VARCHAR (100) NULL
go

CREATE FUNCTION dbo.GetWorkflowArtifactID(@Data XML)
RETURNS INT
WITH SCHEMABINDING
AS BEGIN
  DECLARE @ArtifactID INT

  SELECT  
    @ArtifactID = @Data.value('(fields/ArtifactID/text())[1]', 'int')

  RETURN @ArtifactID
END
GO

CREATE FUNCTION dbo.GetWorkflowStartDate(@Data XML)
RETURNS varchar(33) 
WITH SCHEMABINDING
AS BEGIN
  DECLARE @StartDate varchar(33)

  SELECT  
    @StartDate = @Data.value('(fields/StartDate/text())[1]', 'varchar(33)')

  RETURN @StartDate
END
GO

alter table Workflow add [ArtifactID] AS ([dbo].[GetWorkflowArtifactID]([Data])) PERSISTED
GO

CREATE XML INDEX [IXXML_Workflow_Data_Property]
    ON [dbo].[Workflow]([Data])
    USING XML INDEX [IXXML_Workflow_Data] FOR PROPERTY
    WITH (PAD_INDEX = OFF);
GO

CREATE PRIMARY XML INDEX [IXXML_WorkflowTypeRelation_Fields]
    ON [dbo].[WorkflowTypeRelation]([Fields])
    WITH (PAD_INDEX = OFF);
GO

CREATE XML INDEX [IXXML_WorkflowTypeRelation_Fields_Property]
    ON [dbo].[WorkflowTypeRelation]([Fields])
    USING XML INDEX [IXXML_WorkflowTypeRelation_Fields] FOR PROPERTY
    WITH (PAD_INDEX = OFF);
GO

CREATE XML INDEX [IXXML_WorkflowTypeRelation_Secondary_PATH]
    ON [dbo].[WorkflowTypeRelation]([Fields])
    USING XML INDEX [IXXML_WorkflowTypeRelation_Fields] FOR PATH
    WITH (PAD_INDEX = OFF);
GO

CREATE XML INDEX [IXXML_WorkflowTypeRelation_Secondary_VALUE]
    ON [dbo].[WorkflowTypeRelation]([Fields])
    USING XML INDEX [IXXML_WorkflowTypeRelation_Fields] FOR VALUE
    WITH (PAD_INDEX = OFF);
GO

alter table Report add [FileName]         VARCHAR (260)   NULL
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
					'RuleType' as ObjectType,	RuleType as ObjectTypeID
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
					'RuleType' as ObjectType,	RuleType as ObjectTypeID
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

alter table RuleResult drop column [PassFraction]
go
alter table RuleResult drop column [FailFraction]
go
alter table RuleResult drop column [Passed]
go
alter table RuleResult add [PassFraction]      AS       (case when [RowsPassed]=(0) AND [RowsFailed]=(0) then (0) else CONVERT([decimal](4,3),CONVERT([decimal](18,3),[RowsPassed],(0))/(CONVERT([decimal](18,3),[RowsPassed],(0))+CONVERT([decimal](18,3),[RowsFailed],(0))),(0)) end)
go
alter table RuleResult add [FailFraction]      AS       (case when [RowsPassed]=(0) AND [RowsFailed]=(0) then (0) else CONVERT([decimal](4,3),CONVERT([decimal](3,3),CONVERT([decimal](18,3),(1),(0))-CONVERT([decimal](18,3),[RowsPassed],(0))/(CONVERT([decimal](18,3),[RowsPassed],(0))+CONVERT([decimal](18,3),[RowsFailed],(0))),(0)),(0)) end)
go
alter table RuleResult add [Passed]            AS       ([utility].[CalculatePassedWrapper](case when [RowsPassed]=(0) AND [RowsFailed]=(0) then (0) else CONVERT([decimal](4,3),CONVERT([decimal](18,3),[RowsPassed],(0))/(CONVERT([decimal](18,3),[RowsPassed],(0))+CONVERT([decimal](18,3),[RowsFailed],(0))),(0)) end,[RuleID]))
go
alter table RuleResult add [RunDate]           DATETIME CONSTRAINT [DF_RuleResult_RunDate] DEFAULT (getutcdate()) NOT NULL
go

ALTER TABLE [fusion].[result] DROP CONSTRAINT DF_FusionResult_ID;
GO
ALTER TABLE [fusion].[Result] DROP CONSTRAINT [PK_FusionResult]
GO
alter table fusion.Result alter column [ID] UNIQUEIDENTIFIER NULL
go


--update old fieldtype html records to have no MaximumLength (Task 1789)
update FieldType
set MinimumLength = 1,
	MaximumLength = NULL
where [Type] = 'Html' AND IsRequired = 1;
go

update FieldType
set MinimumLength = NULL, MaximumLength = NULL
where [Type] = 'Html' AND IsRequired = 0;
go

--update RuleItem to use objectid/type instead of FusionAttributeID
sp_RENAME 'fusion.RuleItem.FusionAttributeID' , 'ObjectID', 'COLUMN'

alter table fusion.RuleItem add ObjectType nvarchar(250);
go

update fusion.RuleItem
set ObjectType = 'FusionAttribute' where ObjectType is null;
go

--add attribute type column to RulePromotion
sp_RENAME 'fusion.RulePromotion.FusionAttributeID' , 'AttributeID', 'COLUMN';

alter table fusion.RulePromotion drop constraint FK_FusionRulePromotion_FusionAttribute;
go
alter table fusion.RulePromotion add AttributeType varchar(25);
go
update fusion.RulePromotion set AttributeType = 'FusionAttribute' where AttributeType is null;
go
alter table fusion.RulePromotion alter column AttributeType varchar(25) not null;
go


CREATE TABLE [dbo].[IssueType] (
    [ID]          INT            IDENTITY (1, 1) NOT NULL,
    [Name]        NVARCHAR (250) NOT NULL,
    [Description] NVARCHAR (MAX) NULL,
    [IsSystem]    BIT            NOT NULL,
    [UpdatedOn]   DATETIME       NULL,
    [UpdatedBy]   INT            NULL,
    CONSTRAINT [PK_IssueType] PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [CONST_IssueType_Name] UNIQUE NONCLUSTERED ([Name] ASC)
);
GO

CREATE TRIGGER [dbo].[IssueType_AfterDelete]
   ON  [dbo].[IssueType] 
   AFTER DELETE
AS 
	SET NOCOUNT ON;
	INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
		select 'Delete', [queue].WriteIndexXml('Removed', 'IssueType', ID, coalesce(UpdatedBy, 0)), 'IssueType', ID from deleted
GO

CREATE TRIGGER [dbo].[IssueType_AfterInsert]
   ON  [dbo].[IssueType]
   AFTER INSERT
AS 
	SET NOCOUNT ON;
	INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
        select 'Add', [queue].WriteIndexXml('', 'IssueType', ID, coalesce(UpdatedBy, 0)), 'IssueType', ID from inserted

	merge	[cache].[Object] as T
	using	(
			select	'IssueType' as [Object],			ID as ObjectID,
					'IssueType' as ObjectType,			0 as ObjectTypeID
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

CREATE TRIGGER [dbo].[IssueType_AfterUpdate]
   ON  [dbo].[IssueType] 
   AFTER UPDATE
AS 
	SET NOCOUNT ON;
	INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
        select 'Update', [queue].WriteIndexXml('', 'IssueType', ID, coalesce(UpdatedBy, 0)), 'IssueType', ID from inserted

	merge	[cache].[Object] as T
	using	(
			select	'IssueType' as [Object],			ID as ObjectID,
					'IssueType' as ObjectType,			0 as ObjectTypeID
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

CREATE TABLE [dbo].[Issue] (
    [ID]           INT          IDENTITY (1, 1) NOT NULL,
    [IssueTypeID]  INT          NOT NULL,
    [Object]       VARCHAR (50) NOT NULL,
    [ObjectID]     INT          NOT NULL,
    [ObjectType]   VARCHAR (25) NOT NULL,
    [ObjectTypeID] INT          NOT NULL,
    [CreatedOn]    DATETIME     NOT NULL,
    [CreatedBy]    INT          NOT NULL,
    [UpdatedOn]    DATETIME     DEFAULT (getutcdate()) NOT NULL,
    [UpdatedBy]    INT          NULL,
    [Criticality]  INT          DEFAULT ((0)) NOT NULL,
    CONSTRAINT [PK_Issue] PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [FK_Issue_IssueType] FOREIGN KEY ([IssueTypeID]) REFERENCES [dbo].[IssueType] ([ID]) ON DELETE CASCADE
);
GO

CREATE TABLE [dbo].[NymRelation] (
    [ID]          INT          IDENTITY (1, 1) NOT NULL,
    [PredicateID] INT          NOT NULL,
    [Object]      VARCHAR (25) NOT NULL,
    [ObjectID]    INT          NOT NULL,
    [UpdatedOn]   DATETIME     NOT NULL,
    [UpdatedBy]   INT          NOT NULL,
    CONSTRAINT [PK_NymRelation] PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [FK_NymRelation_PredicateType] FOREIGN KEY ([PredicateID]) REFERENCES [dbo].[Predicate] ([ID]) ON DELETE CASCADE,
    CONSTRAINT [CONST_NymRelation_Name] UNIQUE NONCLUSTERED ([PredicateID] ASC, [Object] ASC, [ObjectID] ASC)
);
GO


CREATE TABLE [dbo].[Nym] (
    [ID]          INT            IDENTITY (1, 1) NOT NULL,
    [Object]      VARCHAR (25)   NOT NULL,
    [ObjectID]    INT            NOT NULL,
    [Name]        NVARCHAR (250) NULL,
    [PredicateID] INT            NOT NULL,
    [UpdatedOn]   DATETIME       DEFAULT (getutcdate()) NULL,
    [UpdatedBy]   INT            NULL,
    [CreatedOn]   DATETIME       DEFAULT (getutcdate()) NOT NULL,
    [CreatedBy]   INT            NOT NULL,
    CONSTRAINT [PK_Nym] PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [FK_Nym_Predicate] FOREIGN KEY ([PredicateID]) REFERENCES [dbo].[Predicate] ([ID]) ON DELETE CASCADE
);
GO

CREATE TRIGGER [dbo].[Nym_AfterDelete]
   ON  [dbo].[Nym] 
   AFTER DELETE
AS 
	SET NOCOUNT ON;
	declare @ot varchar(50) = 'Synonym'

	INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
        select	'ObjectIndex',
				'D', 
				@ot, 
				ID
		from	deleted;
GO

CREATE TRIGGER [dbo].[Nym_AfterUpsert]
   ON  [dbo].[Nym] 
   AFTER INSERT, UPDATE
AS 
	SET NOCOUNT ON;
	declare @ot varchar(50) = 'Synonym'

	INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
        select	'ObjectIndex',
				case 
					when D.ID is not null then 'U'
					else 'A'
				end, 
				@ot, 
				I.ID
		from	inserted I
				left join deleted D on D.ID = I.ID;
GO

CREATE SCHEMA [workflow]
    AUTHORIZATION [dbo];
GO

CREATE TABLE [workflow].[Type] (
    [ID]        INT            IDENTITY (1, 1) NOT NULL,
    [Name]      NVARCHAR (500) NOT NULL,
    [CreatedBy] INT            NOT NULL,
    [CreatedOn] DATETIME       NOT NULL,
    [UpdatedBy] INT            NOT NULL,
    [UpdatedOn] DATETIME       NOT NULL,
    CONSTRAINT [PK_WorkflowType] PRIMARY KEY CLUSTERED ([ID] ASC)
);
GO

CREATE TABLE [workflow].[Version] (
    [ID]        INT      IDENTITY (1, 1) NOT NULL,
    [TypeID]    INT      NOT NULL,
    [CreatedBy] INT      NOT NULL,
    [CreatedOn] DATETIME NOT NULL,
    [UpdatedBy] INT      NOT NULL,
    [UpdatedOn] DATETIME NOT NULL,
    CONSTRAINT [PK_WorkflowVersion] PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [FK_WorkflowVersion_WorkflowType] FOREIGN KEY ([TypeID]) REFERENCES [workflow].[Type] ([ID]) ON DELETE CASCADE
);
GO

CREATE TABLE [workflow].[VersionStep] (
    [ID]           INT            IDENTITY (1, 1) NOT NULL,
    [ParentID]     INT            NULL,
    [VersionID]    INT            NOT NULL,
    [Name]         NVARCHAR (500) NOT NULL,
    [StepType]     INT            NOT NULL,
    [ActivityType] INT            NOT NULL,
    [Settings]     XML            NULL,
    [Fields]       XML            NULL,
    [XPosition]    INT            NOT NULL,
    [YPosition]    INT            NOT NULL,
    CONSTRAINT [PK_WorkflowVersionStep] PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [FK_WorkflowVersionStep_Parent] FOREIGN KEY ([ParentID]) REFERENCES [workflow].[VersionStep] ([ID]),
    CONSTRAINT [FK_WorkflowVersionStep_WorkflowVersion] FOREIGN KEY ([VersionID]) REFERENCES [workflow].[Version] ([ID]) ON DELETE CASCADE
);
GO

CREATE TABLE [workflow].[VersionStepTransition] (
    [FromVersionStepID] INT            NOT NULL,
    [ToVersionStepID]   INT            NOT NULL,
    [Name]              NVARCHAR (500) NOT NULL,
    [TransitionType]    INT            NOT NULL,
    [Condition]         XML            NULL,
    [LinkType]          INT            NOT NULL,
    CONSTRAINT [PK_WorkflowVersionStepTransition] PRIMARY KEY CLUSTERED ([FromVersionStepID] ASC, [ToVersionStepID] ASC),
    CONSTRAINT [FK_WorkflowVersionStepTransition_FromVersionStep] FOREIGN KEY ([FromVersionStepID]) REFERENCES [workflow].[VersionStep] ([ID]),
    CONSTRAINT [FK_WorkflowVersionStepTransition_ToVersionStep] FOREIGN KEY ([ToVersionStepID]) REFERENCES [workflow].[VersionStep] ([ID])
);
GO

CREATE TABLE [workflow].[EventRegistration] (
    [ID]         INT          IDENTITY (1, 1) NOT NULL,
    [TypeID]     INT          NOT NULL,
    [Object]     VARCHAR (50) NOT NULL,
    [ObjectID]   INT          NOT NULL,
    [ChangeType] INT          NOT NULL,
    [Condition]  XML          NULL,
    CONSTRAINT [PK_WorkflowEventRegistration] PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [FK_WorkflowEventRegistration_WorkflowType] FOREIGN KEY ([TypeID]) REFERENCES [workflow].[Type] ([ID]) ON DELETE CASCADE
);
GO


CREATE TABLE [workflow].[Item] (
    [ID]          BIGINT       IDENTITY (1, 1) NOT NULL,
    [VersionID]   INT          NOT NULL,
    [Active]      BIT          NOT NULL,
    [Object]      VARCHAR (50) NOT NULL,
    [ObjectID]    INT          NOT NULL,
    [StartedBy]   INT          NOT NULL,
    [StartedOn]   DATETIME     NOT NULL,
    [UpdatedBy]   INT          NOT NULL,
    [UpdatedOn]   DATETIME     NOT NULL,
    [CompletedBy] INT          NULL,
    [CompletedOn] DATETIME     NULL,
    [IsTest]      BIT          CONSTRAINT [DF_WorkflowItem_IsTest] DEFAULT ((0)) NOT NULL,
    CONSTRAINT [PK_WorkflowItem] PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [FK_WorkflowItem_WorkflowVersion] FOREIGN KEY ([VersionID]) REFERENCES [workflow].[Version] ([ID]) ON DELETE CASCADE
);
GO

CREATE TABLE [workflow].[ItemStep] (
    [ID]          BIGINT   IDENTITY (1, 1) NOT NULL,
    [ItemID]      BIGINT   NOT NULL,
    [StepID]      INT      NOT NULL,
    [Settings]    XML      NULL,
    [Fields]      XML      NULL,
    [StartedBy]   INT      NOT NULL,
    [StartedOn]   DATETIME NOT NULL,
    [CompletedBy] INT      NULL,
    [CompletedOn] DATETIME NULL,
    CONSTRAINT [PK_WorkflowItemStep] PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [FK_WorkflowItemStep_WorkflowItem] FOREIGN KEY ([ItemID]) REFERENCES [workflow].[Item] ([ID]),
    CONSTRAINT [FK_WorkflowItemStep_WorkflowVersionStep] FOREIGN KEY ([StepID]) REFERENCES [workflow].[VersionStep] ([ID]) ON DELETE CASCADE
);
GO

CREATE TABLE [workflow].[ItemStepTransition] (
    [FromItemStepID] BIGINT   NOT NULL,
    [ToItemStepID]   BIGINT   NOT NULL,
    [Condition]      XML      NULL,
    [Date]           DATETIME NOT NULL,
    CONSTRAINT [PK_WorkflowItemStepTransition] PRIMARY KEY CLUSTERED ([FromItemStepID] ASC, [ToItemStepID] ASC),
    CONSTRAINT [FK_WorkflowItemStepTransition_FromItemStep] FOREIGN KEY ([FromItemStepID]) REFERENCES [workflow].[ItemStep] ([ID]),
    CONSTRAINT [FK_WorkflowItemStepTransition_ToItemStep] FOREIGN KEY ([ToItemStepID]) REFERENCES [workflow].[ItemStep] ([ID])
);
GO

CREATE TABLE [dbo].[FusionSchedule] (
    [FusionID]    INT      NOT NULL,
    [Day]         INT      NOT NULL,
    [Time]        TIME (7) NOT NULL,
    [FullRefresh] BIT      CONSTRAINT [DF_FusionSchedule_FullRefresh] DEFAULT ((0)) NOT NULL,
    [CreatedOn]   DATETIME NULL,
    [CreatedBy]   INT      NULL,
    [UpdatedOn]   DATETIME NULL,
    [UpdatedBy]   INT      NULL,
    CONSTRAINT [PK_FusionSchedule] PRIMARY KEY CLUSTERED ([FusionID] ASC, [Day] ASC, [Time] ASC),
    CONSTRAINT [FK_FusionSchedule_Fusion] FOREIGN KEY ([FusionID]) REFERENCES [dbo].[Fusion] ([ID]) ON DELETE CASCADE
);
GO

alter table fusion.RulePromotion add CreatedOn datetime CONSTRAINT [DF_RulePromotion_CreatedOn]  DEFAULT (getutcdate()) not null
go

alter table fusion.RulePromotion add UpdatedOn datetime CONSTRAINT [DF_RulePromotion_UpdatedOn]  DEFAULT (getutcdate()) not null
go


update predicate set name = 'Synonym', inverse = 'Synonym' where issystem = 1 and name = 'synonym of' and [type]  = 6
go

insert into cache.[object] ([object],[objectid],[objecttype],[objecttypeid]) values('ReferenceItemType',0,'ReferenceItemType',0)
go


--add owner columns used by markit lineage
alter table mapruleitem add [Owner] varchar(100) null;
go

alter table mapitem add [Owner] varchar(100) null;
go

alter table mapruleitemmapitem add [Owner] varchar(100) null;
go

alter table [intersect] add [Owner] varchar(100) null;
go

CREATE INDEX IX_MapRuleItem_SourceFusionAttributeID_TargetFusionAttributeID ON [dbo].[MapRuleItem] (SourceFusionAttributeID, TargetFusionAttributeID); 
go



-- add id column to fusion schedule table
alter table fusionschedule add ID INT IDENTITY (1, 1) NOT NULL;
go

-- drop the constraint
ALTER TABLE dbo.fusionschedule
DROP CONSTRAINT PK_FusionSchedule;
GO

-- add back constraint
ALTER TABLE dbo.fusionschedule
ADD CONSTRAINT PK_FusionSchedule PRIMARY KEY CLUSTERED
([ID] ASC) ;
GO


-- add constraint
ALTER TABLE dbo.fusionschedule ADD CONSTRAINT Con_FusionScheduleUniqueFusionIDDayTime UNIQUE (FusionID,Day,Time);
go



-- ADDED: Mike P -- 3/1/17
DROP TABLE [dbo].[FusionSchedule]
GO

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
GO

CREATE TABLE [dbo].[MapType](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[MapClass] [int] NOT NULL,
	[Name] [nvarchar](250) NOT NULL,
	[Description] [nvarchar](max) NULL,
	[CreatedOn] [datetime] NULL,
	[CreatedBy] [int] NULL,
	[UpdatedOn] [datetime] NULL,
	[UpdatedBy] [int] NULL,
	CONSTRAINT [PK_MapType] PRIMARY KEY CLUSTERED ( [ID] ASC )
)
GO

ALTER TABLE [dbo].[MapType] ADD  CONSTRAINT [DF_MapType_CreatedOn]  DEFAULT (getutcdate()) FOR [CreatedOn]
GO

ALTER TABLE [dbo].[MapType] ADD  CONSTRAINT [DF_MapType_UpdatedOn]  DEFAULT (getutcdate()) FOR [UpdatedOn]
GO

ALTER TABLE [Map] ADD MapTypeID int NOT NULL CONSTRAINT DF_Map_MapTypeID DEFAULT(1)
GO

ALTER TABLE [Map] ADD Name nvarchar(2500) NULL
GO

INSERT INTO [dbo].[MapType]	([MapClass] ,[Name] ,[Description] ,[CreatedBy] ,[UpdatedBy])
VALUES						(1, 'Source To Target', 'Outlines the source to target maps that can contain a variety of objects as sources and targets.', 0, 0)
GO

ALTER TABLE [dbo].[Map]  WITH CHECK ADD  CONSTRAINT [FK_Map_MapType] FOREIGN KEY([MapTypeID]) REFERENCES [dbo].[MapType] ([ID])
GO

ALTER TABLE [dbo].[Map] CHECK CONSTRAINT [FK_Map_MapType]
GO

ALTER TABLE [dbo].[Report] ADD  CONSTRAINT [DF_Report_ReportType]  DEFAULT ('legacy') FOR [ReportType]
GO

CREATE TYPE [dbo].[LineageTechnicalTable] AS TABLE (
    [ID]                      INT NULL,
    [MapItemID]               INT NULL,
    [SourceFusionAttributeID] INT NULL,
    [TargetFusionAttributeID] INT NULL,
    [Deleting]                BIT NULL,
    [Adding]                  BIT NULL);
GO

CREATE TRIGGER [dbo].[Map_AfterDelete]
	ON [dbo].[Map]
	AFTER DELETE
AS
	SET NOCOUNT ON;
	delete	T
	from	[cache].[Object] T
			inner join deleted S on T.Object = 'Map' and S.ID = T.ObjectID;
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



CREATE TABLE [dbo].[RuleType] (
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](250) NOT NULL,
	[Description] [nvarchar](max) NULL,
	[CreatedOn] [datetime] NULL,
	[CreatedBy] [int] NULL,
	[UpdatedOn] [datetime] NULL,
	[UpdatedBy] [int] NULL,
	CONSTRAINT [PK_RuleType] PRIMARY KEY CLUSTERED ( [ID] ASC )
)
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

EXEC sp_rename 'dbo.Rule.RuleType', 'RuleTypeID', 'COLUMN';  
GO 

ALTER TABLE [dbo].[Rule]  WITH CHECK ADD  CONSTRAINT [FK_Rule_RuleType] FOREIGN KEY([RuleTypeID]) REFERENCES [dbo].[RuleType] ([ID])
GO
ALTER TABLE [dbo].[Rule] CHECK CONSTRAINT [FK_Rule_RuleType]
GO

alter table FusionQueryAttributeType alter column Query nvarchar(max) not null
go


ALTER TABLE [dbo].[Map] DROP CONSTRAINT [FK_Map_IntersectRole]
GO
alter table Map drop column IntersectRoleID
go
drop table IntersectRole
go

alter table [Intersect] drop column [Classification]
go
alter table [Intersect] drop column [Description]
go


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

CREATE TRIGGER [dbo].[ReferenceItem_AfterDelete]
	ON [dbo].[ReferenceItem]
	AFTER DELETE
AS
	SET NOCOUNT ON;
	delete	T
	from	[cache].[Object] T
			inner join deleted S on T.Object = 'ReferenceItem' and S.ID = T.ObjectID;
GO

--select * from cache.Object where Object = 'ReferenceItem'

update ReferenceItem set UpdatedOn = getutcdate()


--add visible column to artifact table
alter table artifact add [Visible] bit not null default(1)
go

-- add index on visible to artifact
CREATE NONCLUSTERED INDEX [IX_Artifact_Visible] ON [dbo].Artifact ( Visible ASC );
go

-- add visible column to taxonomy table
alter table Taxonomy add [Visible] bit not null default(1);
go

-- add index on visible to taxonomy table
CREATE NONCLUSTERED INDEX [IX_Taxonomy_Visible] ON [dbo].Taxonomy ( Visible ASC );
go

-- add visible column to policy table
alter table [dbo].[Policy] add [Visible] bit not null default(1);
go

-- add index on visible to policy table
CREATE NONCLUSTERED INDEX [IX_Policy_Visible] ON [dbo].[Policy] ( Visible ASC );
go


-- add visible column to rule table
alter table [dbo].[Rule] add [Visible] bit not null default(1);
go

-- add index on visible column to rule table
CREATE NONCLUSTERED INDEX [IX_Rule_Visible] ON [dbo].[Rule] ( Visible ASC );
go

-- add visible column to reference item table
alter table [dbo].[ReferenceItem] add [Visible] bit not null default(1)
go

-- add index on visible column to reference item table
CREATE NONCLUSTERED INDEX [IX_ReferenceItem_Visible] ON [dbo].[ReferenceItem] ( Visible ASC );
go