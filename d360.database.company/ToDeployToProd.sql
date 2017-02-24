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
		set @type = 'Event';
		insert into #Recache
			SELECT	@type, O.ID, 'Rule', R.ID
			FROM	[Event] O
					INNER JOIN EventGroup G on G.ID = O.EventGroupID
					INNER JOIN [Rule] R on R.ID = G.RuleID;
	end;

	begin
		set @type = 'EventGroup';
		insert into #Recache
			SELECT	@type, ID, 'Rule', RuleID FROM EventGroup;
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
		INSERT INTO #Recache VALUES ('RuleType', 1, 'RuleType', 1)
		INSERT INTO #Recache VALUES ('RuleType', 2, 'RuleType', 2)
		INSERT INTO #Recache VALUES ('RuleType', 3, 'RuleType', 3)
		INSERT INTO #Recache VALUES ('RuleType', 4, 'RuleType', 4)

		set @type = 'Rule';
		insert into #Recache
			SELECT	@type, ID, 'RuleType', RuleType FROM [Rule];
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

alter procedure [dbo].[GetLineage]
--declare 
	@type varchar(50),
	@id int,
	@view int = 1

--set @type = 'Artifact'
--set @id = 2528--6381
--set @view = 3
as
begin
	declare @links table ([from] varchar(250), [to] varchar(250), category varchar(50))
	declare @nodes table (
		[key] varchar(250), 
		obj varchar(50), [objid] int, [type] varchar(50), typeName nvarchar(250), name nvarchar(500), 
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
					inner join IntersectDetail SI on SI.ID = MI.SourceIntersectID
					inner join IntersectDetail TI ON TI.ID = MI.TargetIntersectID
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

		declare @points table ( ID int, SourceIntersectID int, TargetIntersectID int )

		-- get all items directly tied to the focal object.
		insert into @points
			select	MI.ID, MI.SourceIntersectID, MI.TargetIntersectID
			from	MapItem MI
					inner join [Intersect] SI on SI.ID = MI.SourceIntersectID
					inner join [Intersect] TI ON TI.ID = MI.TargetIntersectID
					inner join @objects O on	( (SI.Subject = O.Type and SI.SubjectID = O.ID) OR (SI.Object = O.Type and SI.ObjectID = O.ID)  ) OR 
												( (TI.Subject = O.Type and TI.SubjectID = O.ID) OR (TI.Object = O.Type and TI.ObjectID = O.ID)  )

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
								) O on O.MapItemID = MI.ID;

		with cte as (
			select	ID,
					SourceIntersectID,
					TargetIntersectID,
					1 as [Level]
			from	@points
			union all
			select	S.ID,
					S.SourceIntersectID,
					S.TargetIntersectID,
					T.[Level] + 1 as [Level]
			from	MapItem S
					inner join cte T on T.SourceIntersectID = S.TargetIntersectID and S.ID <> T.ID
			where	T.[Level] <= 25
		)
		insert into @points
			select ID, SourceIntersectID, TargetIntersectID from cte where ID not in (select ID from @points)


		declare @items table (
			ID int,
			SourceIntersectID int, 
			SourceSubjectTypeName nvarchar(500), SourceSubjectName nvarchar(500), SourceSubject varchar(50), SourceSubjectID int, SourceSubjectIconBackColor varchar(7), SourceSubjectIconForeColor varchar(7), 
			SourceObjectTypeName nvarchar(500), SourceObjectName nvarchar(500), SourceObject varchar(50), SourceObjectID int, SourceObjectIconBackColor varchar(7), SourceObjectIconForeColor varchar(7),
			
			TargetIntersectID int, 
			TargetSubjectTypeName nvarchar(500), TargetSubjectName nvarchar(500), TargetSubject varchar(50), TargetSubjectID int, TargetSubjectIconBackColor varchar(7), TargetSubjectIconForeColor varchar(7), 
			TargetObjectTypeName nvarchar(500), TargetObjectName nvarchar(500), TargetObject varchar(50), TargetObjectID int, TargetObjectIconBackColor varchar(7), TargetObjectIconForeColor varchar(7),

			HasSourceRules bit
		)

		insert into @items
			select	O.ID,
				
					O.SourceIntersectID,
					SI.SubjectTypeName,
					SI.SubjectName,
					SI.Subject,
					SI.SubjectID,
					SI.SubjectIconBackColor,
					SI.SubjectIconForeColor,
					SI.ObjectTypeName,
					SI.ObjectName,
					SI.Object,
					SI.ObjectID,
					SI.ObjectIconBackColor,
					SI.ObjectIconForeColor,

					O.TargetIntersectID,
					TI.SubjectTypeName,
					TI.SubjectName,
					TI.Subject,
					TI.SubjectID,
					TI.SubjectIconBackColor,
					TI.SubjectIconForeColor,
					TI.ObjectTypeName,
					TI.ObjectName,
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
							I.TargetSubjectIconBackColor as back,
							I.TargetSubjectIconForeColor as fore,
							case 
								when I.TargetSubject = @type and I.TargetSubjectID = @id then 'Focal'
								else 'Normal'
							end as template,
							null as other,
							I.HasSourceRules
					from	@items I
					) S
			on		(T.[key] = S.[key])
			when	matched then
			update	set
					T.HasSourceRules = S.HasSourceRules
			when	not matched then
			insert	([key], obj, [objid], [type], typeName, name, back, fore, template, other, HasSourceRules)
			values	(S.[key], S.obj, S.[objid], S.[type], S.typeName, S.name, S.back, S.fore, S.template, S.other, S.HasSourceRules);
					--where	I.TargetSubject + '.' + cast(I.TargetSubjectID as varchar) not in (select [key] from @nodes)

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
							C.challenges,
							E.issues
					from	@nodes I
							cross apply (
											select count(1) as challenges   
											from Workflow W            			                          
											where W.WorkflowType = 4 and W.Data.exist('/fields/ArtifactID[text() = sql:column("I.objid")]') = 1 and W.DateCompleted is null   
										) C
							cross apply (
											select count(1) as issues   
											from Workflow W            			                          
											where W.WorkflowType = 3 and W.Data.exist('/fields/ArtifactID[text() = sql:column("I.objid")]') = 1 and W.DateCompleted is null   
										) E
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
			insert	([key], obj, [objid], [type], typeName, name, back, fore, template, other, HasSourceRules)
			values	(S.[key], S.obj, S.[objid], S.[type], S.typeName, S.name, S.back, S.fore, S.template, S.other, S.HasSourceRules);

			merge	@nodes as T
			using	(
					select	distinct
							TargetSubject + '.' + cast(TargetSubjectID as varchar) as [key],
							TargetSubject as [obj],
							TargetSubjectID as [objid], 
							TargetSubject as [type],
							TargetSubjectTypeName as typeName,
							TargetSubjectName as name,
							TargetSubjectIconBackColor as back,
							TargetSubjectIconForeColor as fore,
							case 
								when TargetSubject = @type and TargetSubjectID = @id then 'Focal'
								else 'Normal'
							end as template,
							null as other,
							HasSourceRules
					from	@items
					) S
			on		(T.[key] = S.[key])
			when	matched then
			update	set
					T.HasSourceRules = S.HasSourceRules
			when	not matched then
			insert	([key], obj, [objid], [type], typeName, name, back, fore, template, other, HasSourceRules)
			values	(S.[key], S.obj, S.[objid], S.[type], S.typeName, S.name, S.back, S.fore, S.template, S.other, S.HasSourceRules);

			--select	* from	@links
			--select	* from	@nodes

			select	(
					select	*
					from	@links O
					for json path			
					) as 'links',
					(
					select	I.*,
							C.challenges,
							E.issues
					from	@nodes I
							cross apply (
											select count(1) as challenges   
											from Workflow W            			                          
											where W.WorkflowType = 4 and W.Data.exist('/fields/ArtifactID[text() = sql:column("I.objid")]') = 1 and W.DateCompleted is null   
										) C
							cross apply (
											select count(1) as issues   
											from Workflow W            			                          
											where W.WorkflowType = 3 and W.Data.exist('/fields/ArtifactID[text() = sql:column("I.objid")]') = 1 and W.DateCompleted is null   
										) E
					for json path			
					) as 'nodes'
			for json path, WITHOUT_ARRAY_WRAPPER
		end --view 2
	end

	if @view = 3
	begin
		declare @tFusionPoints table ( ID int, MapItemID int, SourceFusionAttributeID int, TargetFusionAttributeID int )

		declare @tItems table (
			MapItemID int, --MapID int,

			SourceIntersectID int, 
			SourceSubjectTypeName nvarchar(500), SourceSubjectName nvarchar(500), SourceSubject varchar(50), SourceSubjectID int,
			SourceObjectTypeName nvarchar(500), SourceObjectName nvarchar(500), SourceObject varchar(50), SourceObjectID int, 
			
			TargetIntersectID int, 
			TargetSubjectTypeName nvarchar(500), TargetSubjectName nvarchar(500), TargetSubject varchar(50), TargetSubjectID int, 
			TargetObjectTypeName nvarchar(500), TargetObjectName nvarchar(500), TargetObject varchar(50), TargetObjectID int
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

				with cte as (
					select	ID,
							SourceFusionAttributeID,
							TargetFusionAttributeID,
							1 as [Level]
					from	@tFusionPoints
					union all
					select	S.ID,
							S.SourceFusionAttributeID,
							S.TargetFusionAttributeID,
							T.[Level] + 1 as [Level]
					from	MapRuleItem S
							inner join cte T on T.SourceFusionAttributeID = S.TargetFusionAttributeID and S.ID <> T.ID
					where	T.[Level] <= 25
				)
				insert into @tFusionPoints
					select ID, NULL, SourceFusionAttributeID, TargetFusionAttributeID from cte where ID not in (select ID from @tFusionPoints)

				-- get all items directly tied to the focal object.
				insert into @tItems
					select	MI.ID,
					
							MI.SourceIntersectID,
							SI.SubjectTypeName,
							SI.SubjectName,
							SI.Subject,
							SI.SubjectID,
							SI.ObjectTypeName,
							SI.ObjectName,
							SI.Object,
							SI.ObjectID,

							MI.TargetIntersectID,
							TI.SubjectTypeName,
							TI.SubjectName,
							TI.Subject,
							TI.SubjectID,
							TI.ObjectTypeName,
							TI.ObjectName,
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
							SI.Subject,
							SI.SubjectID,
							SI.ObjectTypeName,
							SI.ObjectName,
							SI.Object,
							SI.ObjectID,

							MI.TargetIntersectID,
							TI.SubjectTypeName,
							TI.SubjectName,
							TI.Subject,
							TI.SubjectID,
							TI.ObjectTypeName,
							TI.ObjectName,
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
							inner join IntersectDetail SI on SI.ID = MI.SourceIntersectID
							inner join IntersectDetail TI ON TI.ID = MI.TargetIntersectID
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
							SI.Subject,
							SI.SubjectID,
							SI.ObjectTypeName,
							SI.ObjectName,
							SI.Object,
							SI.ObjectID,

							O.TargetIntersectID,
							TI.SubjectTypeName,
							TI.SubjectName,
							TI.Subject,
							TI.SubjectID,
							TI.ObjectTypeName,
							TI.ObjectName,
							TI.Object,
							TI.ObjectID

					from	@tBusinessPoints O
							inner join IntersectDetail SI on SI.ID = O.SourceIntersectID
							inner join IntersectDetail TI ON TI.ID = O.TargetIntersectID

				insert into @tFusionPoints
					select	J.MapRuleItemID,
							J.MapItemID,
							T.SourceFusionAttributeID,
							T.TargetFusionAttributeID
					from	@tItems I
							inner join MapRuleItemMapItem J on J.MapItemID = I.MapItemID
							inner join MapRuleItem T on T.ID = J.MapRuleItemID
							inner join FusionAttribute SFA on SFA.ID = T.SourceFusionAttributeID and SFA.Deleted = 0
							inner join FusionAttribute TFA on TFA.ID = T.TargetFusionAttributeID and TFA.Deleted = 0
			end

			--Load tables we will return to caller.
			insert into @links
				select	distinct
						cast(S.SourceFusionAttributeID as varchar) + '.' + coalesce(B.SourceSubject, '0') + '.' + coalesce(cast(B.SourceSubjectID as varchar), '0') as [from],
						cast(S.TargetFusionAttributeID as varchar) + '.' + coalesce(B.TargetSubject, '0') + '.' + coalesce(cast(B.TargetSubjectID as varchar), '0') as [to],
						'' as category
				from	@tFusionPoints S
						left join MapRuleItemMapItem J on J.MapRuleItemID = S.ID
						left join @tItems B on B.MapItemID = J.MapItemID
			insert into @nodes
				select	distinct
						cast(S.SourceFusionAttributeID as varchar) + '.' + coalesce(B.SourceSubject, '0') + '.' + coalesce(cast(B.SourceSubjectID as varchar), '0') as [key],
						'FusionAttribute' as [obj],
						SourceFusionAttributeID as [objid], 
						'FusionAttribute' as [type],
						T.Name as typeName,
						A.TextPath as name,
						'#000' as back,
						'#fff' as fore,
						'Fusion' as template,
						B.SourceSubjectTypeName + ' : ' + B.SourceSubjectName as other,
						null
				from	@tFusionPoints S
						inner join FusionAttribute A on A.ID = S.SourceFusionAttributeID
						inner join FusionAttributeType T on T.ID = A.FusionAttributeTypeID
						left join MapRuleItemMapItem J on J.MapRuleItemID = S.ID
						left join @tItems B on B.MapItemID = J.MapItemID
			insert into @nodes
				select	distinct
						cast(S.TargetFusionAttributeID as varchar) + '.' + coalesce(B.TargetSubject, '0') + '.' + coalesce(cast(B.TargetSubjectID as varchar), '0') as [key],
						'FusionAttribute' as [obj],
						TargetFusionAttributeID as [objid], 
						'FusionAttribute' as [type],
						T.Name as typeName,
						A.TextPath as name,
						'#000' as back,
						'#fff' as fore,
						'Fusion' as template,
						B.TargetSubjectTypeName + ' : ' + B.TargetSubjectName as other,
						null
				from	@tFusionPoints S
						inner join FusionAttribute A on A.ID = S.TargetFusionAttributeID
						inner join FusionAttributeType T on T.ID = A.FusionAttributeTypeID
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

--select	* from	@links
--select	* from	@nodes

		select	(
				select	*
				from	@links O
				for json path			
				) as 'links',
				(
				select	*
				from	@nodes
				for json path			
				) as 'nodes'
		for json path, WITHOUT_ARRAY_WRAPPER
	end --view 3
end
GO


alter PROCEDURE [dbo].[GetRenderedTemplateBodyNg]-- 'Tooltip', 'Resource', 2, 'Preview'
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
			@t = fat.textpath,
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

		declare @workflowID uniqueidentifier,
				@dateCertifiedOn varchar(10),
				@certifiers nvarchar(2500),
				@status varchar(50),
				@certIconColor varchar(10)

		select	@dateCertifiedOn = CONVERT(VARCHAR(10), DateLastCertified, 101),
				@status = Status
		from	Artifact A
		where	A.ID = @ID

		SELECT	@workflowID = W.ID,
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
				inner join reporting.Global_Resource R on R.ResourceID = WR.ResourceID

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
					set @html = @html + '<div><a class=''btn btn-info'' routerLink=''/workflow/status/' + cast(@workflowID as varchar(50)) + '''>Go to this workflow status</a>.</div>'
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
						set @html = @html + '<div>Certifying Users: {Certifiers}</div>'
						if @workflowID is not null
						begin
							set @html = @html + '<div><a class=''btn btn-info'' routerLink=''/workflow/status/' + cast(@workflowID as varchar(50)) + '''>Go to this workflow status</a>.</div>'
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
		set @html = '<h3>{Name} <small style="right: 5px;">{Type}</small></h3><div>{Description}</div>'
		set @showIcon = 0

		if @Type = 'Artifact'
		begin
			insert into @tbl
			select	'Status', [Status]
			from	Artifact
			where	ID = @ID

			insert into @tbl
			select	'Path', TextPath
			from	Artifact
			where	ID = @ID

			insert into @tbl 
				select 'GoverningDomain', tt.name
				from
					artifact a
					inner join taxonomytype tt on (a.taxonomytypeid = tt.id and a.id = @ID)

			set @html = @html + '<div><b>' + @SubjectName + ':</b> {GoverningDomain}</div>'
			set @html = @html + '<div><b>Status:</b> {Status}</div>'
			set @html = @html + '<div><b>Path:</b> {Path}</div>'

			set @hasDynamicFields = 1
		end;

		if @Type = 'Event'
		begin
			declare @so nvarchar(250)
			select	@so = SourceID, 
					@s = [Status]
			from	[Event]
			where	ID = @ID

			insert into @tbl values ('Status', @s)
			insert into @tbl values ('SourceID', @so)

			set @html = @html + '<div><b>Status:</b> {Status}</div>'
			set @html = @html + '<div><b>SourceID:</b> {SourceID}</div>'

			set @hasDynamicFields = 1
		end;

		if @Type = 'EventGroup'
		begin
			insert into @tbl
				select	'Key', PublicID
				from	EventGroup
				where	ID = @ID

			-- BUILD EVENT LIST HTML -----------------------------------------
			declare @events table (ID int, SourceID nvarchar(250), Status varchar(25))
			insert into @events 
				select top 10 ID, SourceID, Status from [Event] where EventGroupID = @ID order by ID desc
		
			declare @eventFieldTypes table (ID int identity, Name nvarchar(250))
			insert into @eventFieldTypes
				select FriendlyName from FieldType where [Object] = 'Rule' and ObjectID = @typeID order by SortOrder asc
			insert into @eventFieldTypes values ('Source ID')
			insert into @eventFieldTypes values ('Status')

			declare @eventHtml nvarchar(max)

			set @eventHtml = '<table class="hoverable bordered striped" style="width:100%">'

			-- Loop through field name list ---------
			set @eventHtml = @eventHtml + '<thead>'
			set		@current = 1
			select	@max = max(ID) from @eventFieldTypes
			while @current <= @max
			begin
				select	@name = Name
				from	@eventFieldTypes
				where	ID = @current

				set @eventHtml = @eventHtml + '<th>' + @name  + '</th>'

				set @current = @current + 1
			end
			set @eventHtml = @eventHtml + '</thead>'
			-----------------------------------------

			set @eventHtml = @eventHtml + '<tbody>'

			-- Loop through event list --------------
			select	@current = min(ID) from @events
			select	@max = max(ID) from @events

			while @current <= @max
			begin
				set @eventHtml = @eventHtml + '<tr>'	-- Open row for selected event.

				declare @eventFields table (Name nvarchar(250), Value nvarchar(4000))
			
				insert into @eventFields
					select		FriendlyName,
								FormattedValue
					from		FieldWithRelation
					where		ObjectType = 'Event' 
								and ObjectID = @current

					-- Loop through each field for this selected event --
					declare @fCurrent int,
							@fMax int
					set		@fCurrent = 1
					select	@fMax = max(ID) from @eventFieldTypes
					while @fCurrent <= @fMax
					begin
						select	@name = Name from @eventFieldTypes where ID = @fCurrent

						select @eventHtml = @eventHtml + '<td>' + coalesce(Value, '') + '</td>' from @eventFields where Name = @name

						set @fCurrent = @fCurrent + 1
					end
					-----------------------------------------------------

					select @eventHtml = @eventHtml	+ 
										'<td>' + [SourceID] + '</td>' + 
										'<td>' + [Status] + '</td>' 
					from	@events 
					where	ID = @current

				delete @eventFields

				set @eventHtml = @eventHtml + '</tr>'	-- Close off row for selected event.

				set @current = @current + 1
			end
			-----------------------------------------

			set @eventHtml = @eventHtml + '</tbody>'

			set @eventHtml = @eventHtml + '</table>'

			insert into @tbl values ('Items', @eventHtml)

			set @html = @html + '<div><b>Key:</b> {Key}</div>'
			set @html = @html + '<div>Items: {Items}</div>'
			------------------------------------------------------------------
		end;

		if @Type = 'Intersect'
		begin
			insert into @tbl
				select	'Classification',
						case Classification
							when 1 then 'Critical'
							else 'Normal'
						end
				from	[Intersect]
				where	ID = @ID

			set @html = @html + '<div><b>Classification:</b> {Classification}</div>'
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
			--insert into @tbl
			--	select	'Status', Status
			--	from	[Rule] O
			--	where	ID = @ID

			--set @html = @html + '<div><b>Status:</b> {Status}</div>'

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
			insert into @tbl
				select	'TextPath', TextPath
				from	Taxonomy O
				where	ID = @ID

			set @html = @html + '<div><b>Path:</b> {TextPath}</div>'

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

	-- Return the properly formatted values.
	select	'' as Title,
			@html as Body;
END
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
									--sr.ID as srID,
									'FusionAttribute' as Subject,
									sr.StreamFusionAttributeID as SubjectID,
									'FusionAttribute' as Object,
									sr.FieldFusionAttributeID as ObjectID
							FROM	@StreamToFieldList sr							
						) s
				ON      (
						s.IntersectTypeID = d.IntersectTypeID 
						and s.Subject = d.Subject and s.SubjectID = d.SubjectID 
						and s.Object = d.Object and s.ObjectID = d.ObjectID
						)
				WHEN NOT MATCHED THEN
				INSERT  (IntersectTypeID, Classification, Description, Subject, SubjectID, Object, ObjectID)
				VALUES  (s.IntersectTypeID, s.class, NULL, s.Subject, s.SubjectID, s.Object, s.ObjectID);
				--OUTPUT  INSERTED.ID, s.srID into @IDList;
	end;
end
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
									--ID,
									'FusionAttribute' as Subject,
									StreamFusionAttributeID as SubjectID,
									'FusionAttribute' as Object,
									FieldFusionAttributeID as ObjectID
							FROM	@StreamToFieldList							
						) s
				ON      (
						s.IntersectTypeID = d.IntersectTypeID 
						and s.Subject = d.Subject and s.SubjectID = d.SubjectID 
						and s.Object = d.Object and s.ObjectID = d.ObjectID
						)
				WHEN NOT MATCHED THEN
					INSERT  (IntersectTypeID, Classification, Description, Subject, SubjectID, Object, ObjectID)
					VALUES  (s.IntersectTypeID, 2, NULL, s.Subject, s.SubjectID, s.Object, s.ObjectID);
				--OUTPUT  INSERTED.ID, s.ID into @IDList;
										
			--insert into @Intersects 
			--	select idl.intersectid from @IDList idl;
			
			--declare @IntersectCount int
			--select @IntersectCount = count(1) from @Intersects
			
			--if @IntersectCount > 0 
			--begin				
			--	EXEC cache.SynchronizeRelationships @Intersects
			--end
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
									--ID,
									'FusionAttribute' as Subject,
									StreamFusionAttributeID as SubjectID,
									'FusionAttribute' as Object,
									FieldFusionAttributeID as ObjectID
							FROM	@BBToFieldList
						) s
				ON      (
						s.IntersectTypeID = d.IntersectTypeID 
						and s.Subject = d.Subject and s.SubjectID = d.SubjectID 
						and s.Object = d.Object and s.ObjectID = d.ObjectID
						)
				WHEN NOT MATCHED THEN
				INSERT  (IntersectTypeID, Classification, Subject, SubjectID, Object, ObjectID)
				VALUES  (s.IntersectTypeID, 2, 'FusionAttribute', s.SubjectID, 'FusionAttribute', s.ObjectID);
				--OUTPUT  INSERTED.ID, s.ID into @IDList;										

			--insert into @Intersects 
			--	select idl.intersectid from @IDList idl;
						
			--select @IntersectCount = count(1) from @Intersects
			--if @IntersectCount > 0 
			--begin
			--	EXEC cache.SynchronizeRelationships @Intersects
			--end
	end;
END
GO


alter procedure [fusion].[ProcessFusionCacheInQueue]
--declare
	@FusionID int
--set @FusionID = 15
as
begin
	SET NOCOUNT, ANSI_PADDING ON;
	SET ANSI_WARNINGS ON;

	UPDATE  FusionAttribute
	SET		TextPath = utility.GetBreadcrumbStringWrapper('FusionAttribute', ID, '.')
	FROM	FusionAttribute 
	WHERE	FusionID = @FusionID and deleted = 0

end
GO

alter procedure [utility].[GetArtifactsUpForCertification]
as
begin
	set nocount on;
	declare @artifactTypes table (RowID int identity, ID int)
	declare @subjectAreas table (RowID int identity, ID int)

	-- loop control variables
	declare @current int,
			@max int

	-- certification loop instance variables
	declare @wt int = 2,
			@id int,
			@start datetime,
			@end datetime,
			@months int,
			@days int,
			@calculationDate datetime,
			@difMonths int,
			@calculationDateMinusDaysBefore date,
			@lastStartDate datetime,
			@minDate datetime = '1900-01-01 00:00:00.000',
			@DateFieldExists bit = 0,
			@currentDate datetime = CAST(CAST(year(getutcdate()) AS varchar) + '-' + CAST(month(getutcdate()) AS varchar) + '-' + CAST(day(getutcdate()) AS varchar) AS DATETIME)

	-- 1. CHECK ARTIFACT TYPES -------------------------------------
	-- get the artifact types that need to be checked
	insert into @artifactTypes
		select	T.ID
		from	ArtifactType T
				inner join WorkflowTypeRelation R on R.[Object] = 'ArtifactType' and R.ObjectID = T.ID and R.WorkflowType = @wt and R.[Enabled] = 1

--select * from @artifactTypes

	set @current = 1
	select @max = MAX(RowID) from @artifactTypes
	while @current <= @max
	begin
		-- set to default
		set @start = null
		set @end = null
		set @months = null
		set @days = null

		select @id = ID from @artifactTypes where RowID = @current

		select	@start = Fields.value('(/fields/CertificationStartDate)[1]', 'datetime'),
				@end = Fields.value('(/fields/CertificationEndDate)[1]', 'datetime'),
				@months = Fields.value('(/fields/MonthsUntilCertification)[1]', 'int'),
				@days = Fields.value('(/fields/DaysGivenToCompleteCertification)[1]', 'int'),
				@calculationDate = Fields.value('(/fields/DateForScheduleCalculation)[1]', 'datetime'),
				@lastStartDate = Fields.value('(/fields/CertificationStartDate)[1]', 'datetime')
		from	WorkflowTypeRelation
		where	[Object] = 'ArtifactType' and ObjectID = @id and WorkflowType = @wt

		if @end is null
		begin
			set @end = @minDate
		end

--select DATEADD(d, -60, '2015-07-31 00:00:00.000')

		set @calculationDateMinusDaysBefore = DATEADD(d, -@days, @calculationDate)
		select @difMonths = DATEDIFF(mm, @calculationDateMinusDaysBefore, getutcdate())

--select	@id as ArtifactTypeID,
--		@calculationDate as CalculationDate,
--		@calculationDateMinusDaysBefore as CalculationDateMinusDaysBefore,
--		@difMonths as NumMonthsSinceLastCertification,
--		@months as NumMonthsBetweenCertifications,
--		@lastStartDate as LastStartDate

		if ((@difMonths >= @months) and (DATEDIFF(mm, @end, getutcdate()) >= @months) OR @lastStartDate is null) --or (@difMonths % @months = 0)
		begin
			set @start = CAST(CAST(year(getutcdate()) AS varchar) + '-' + CAST(month(getutcdate()) AS varchar) + '-' + CAST(day(@calculationDateMinusDaysBefore) AS varchar) AS DATETIME) --CONVERT(date, getutcdate())
			set @end = CAST(CAST(year(getutcdate()) AS varchar) + '-' + CAST(month(getutcdate()) AS varchar) + '-' + CAST(day(@calculationDate) AS varchar) AS DATETIME) --DATEADD(d, @days, CONVERT(date, getutcdate()))
--select @start, @end, DATEDIFF(d, @start, @end)

			if DATEDIFF(d, @start, @end) < @days
			begin
				set @start = @currentDate
				set @end = DATEADD(d, @days, @currentDate)
			end

			select	@DateFieldExists = Fields.exist('fields/CertificationStartDate')
			from	WorkflowTypeRelation
			where	[Object] = 'ArtifactType' and ObjectID = @id and WorkflowType = @wt

--select @start, @end
--select @DateFieldExists as DateFieldExists

			if @DateFieldExists = 1
			begin
				update	WorkflowTypeRelation
				set		Fields.modify('delete (/fields/CertificationStartDate)')
				where	[Object] = 'ArtifactType' and ObjectID = @id and WorkflowType = @wt
			end

			update	WorkflowTypeRelation
			set		Fields.modify('insert <CertificationStartDate>{sql:variable("@start")}</CertificationStartDate> into (/fields)[1]')
			where	[Object] = 'ArtifactType' and ObjectID = @id and WorkflowType = @wt

			select	@DateFieldExists = Fields.exist('fields/CertificationEndDate')
			from	WorkflowTypeRelation
			where	[Object] = 'ArtifactType' and ObjectID = @id and WorkflowType = @wt

			if @DateFieldExists = 1
			begin
				update	WorkflowTypeRelation
				set		Fields.modify('delete (/fields/CertificationEndDate)')
				where	[Object] = 'ArtifactType' and ObjectID = @id and WorkflowType = @wt
			end

			update	WorkflowTypeRelation
			set		Fields.modify('insert <CertificationEndDate>{sql:variable("@end")}</CertificationEndDate> into (/fields)[1]')
			where	[Object] = 'ArtifactType' and ObjectID = @id and WorkflowType = @wt
		end

		-- Increment
		set @current = @current + 1
	end

	-- 2. CHECK VOCABULARIES ---------------------------------------
	-- get the vocabularies that need to be checked
	insert into @subjectAreas
		select	T.ID
		from	TaxonomyType T
				inner join WorkflowTypeRelation R on R.[Object] = 'TaxonomyType' and R.ObjectID = T.ID and R.WorkflowType = @wt and R.[Enabled] = 1

	set @current = 1
	select @max = MAX(RowID) from @subjectAreas
	while @current <= @max
	begin
	--	-- set to default
		set @start = null
		set @end = null
		set @months = null
		set @days = null
	
		select @id = ID from @subjectAreas where RowID = @current

		select	@start = Fields.value('(/fields/CertificationStartDate)[1]', 'datetime'),
				@end = Fields.value('(/fields/CertificationEndDate)[1]', 'datetime'),
				@months = Fields.value('(/fields/MonthsUntilCertification)[1]', 'int'),
				@days = Fields.value('(/fields/DaysGivenToCompleteCertification)[1]', 'int'),
				@calculationDate = Fields.value('(/fields/DateForScheduleCalculation)[1]', 'datetime'),
				@lastStartDate = Fields.value('(/fields/CertificationStartDate)[1]', 'datetime')
		from	WorkflowTypeRelation
		where	[Object] = 'TaxonomyType' and ObjectID = @id and WorkflowType = @wt
	
		if @months is not null and @days is not null
		begin
			if @end is null
			begin
				set @end = @minDate
			end

		set @calculationDateMinusDaysBefore = DATEADD(d, -@days, @calculationDate)
		select @difMonths = DATEDIFF(mm, @calculationDateMinusDaysBefore, getutcdate())
		
		if ((@difMonths >= @months) and (DATEDIFF(mm, @end, getutcdate()) >= @months) OR @lastStartDate is null)
			begin
				set @start = CAST(CAST(year(getutcdate()) AS varchar) + '-' + CAST(month(getutcdate()) AS varchar) + '-' + CAST(day(@calculationDateMinusDaysBefore) AS varchar) AS DATETIME)
				set @end = CAST(CAST(year(getutcdate()) AS varchar) + '-' + CAST(month(getutcdate()) AS varchar) + '-' + CAST(day(@calculationDate) AS varchar) AS DATETIME)

				if DATEDIFF(d, @start, @end) < @days
				begin
					set @start = @currentDate
					set @end = DATEADD(d, @days, @currentDate)
				end

				select	@DateFieldExists = Fields.exist('fields/CertificationStartDate')
				from	WorkflowTypeRelation
				where	[Object] = 'TaxonomyType' and ObjectID = @id and WorkflowType = @wt

				if @DateFieldExists = 1
				begin
					update	WorkflowTypeRelation
					set		Fields.modify('delete (/fields/CertificationStartDate)')
					where	[Object] = 'TaxonomyType' and ObjectID = @id and WorkflowType = @wt
				end

				update	WorkflowTypeRelation
				set		Fields.modify('insert <CertificationStartDate>{sql:variable("@start")}</CertificationStartDate> into (/fields)[1]')
				where	[Object] = 'TaxonomyType' and ObjectID = @id and WorkflowType = @wt

				select	@DateFieldExists = Fields.exist('fields/CertificationEndDate')
				from	WorkflowTypeRelation
				where	[Object] = 'TaxonomyType' and ObjectID = @id and WorkflowType = @wt

				if @DateFieldExists = 1
				begin
					update	WorkflowTypeRelation
					set		Fields.modify('delete (/fields/CertificationEndDate)')
					where	[Object] = 'TaxonomyType' and ObjectID = @id and WorkflowType = @wt
				end

				update	WorkflowTypeRelation
				set		Fields.modify('insert <CertificationEndDate>{sql:variable("@end")}</CertificationEndDate> into (/fields)[1]')
				where	[Object] = 'TaxonomyType' and ObjectID = @id and WorkflowType = @wt
			end
		end

		-- Increment
		set @current = @current + 1
	end

	-- 3. CHECK ARTIFACTS ------------------------------------------
--declare @wt int =2
	select	A.ID as ArtifactID,
--A.ArtifactTypeID,
--W.DateStarted,
			coalesce(V.Fields.value('(/fields/CertificationStartDate/text())[1]', 'datetime'), T.Fields.value('(/fields/CertificationStartDate/text())[1]', 'datetime')) as CertificationStartDate,
			coalesce(V.Fields.value('(/fields/CertificationEndDate/text())[1]', 'datetime'), T.Fields.value('(/fields/CertificationEndDate/text())[1]', 'datetime')) as CertificationEndDate
	from	Artifact A
			left join WorkflowTypeRelation T on T.[Object] = 'ArtifactType' and T.ObjectID = A.ArtifactTypeID and T.WorkflowType = @wt and T.[Enabled] = 1  and T.Parent is null and T.ParentID is null
			left join WorkflowTypeRelation V on V.[Object] = 'ArtifactType' and V.ObjectID = A.ArtifactTypeID and V.WorkflowType = @wt and V.[Enabled] = 1 and V.Parent = 'TaxonomyType' and V.ParentID = A.TaxonomyTypeID
			outer apply (
						select	max(DateStarted) as DateStarted
						from	Workflow
						where	artifactID = A.ID
								--and DateCompleted is null
						) W
	where	(
				W.DateStarted is null
				or
				(
					W.DateStarted is not null 
					and
					DATEDIFF(m, W.DateStarted, 
						coalesce(V.Fields.value('(/fields/CertificationStartDate/text())[1]', 'datetime'), T.Fields.value('(/fields/CertificationStartDate/text())[1]', 'datetime'))
					) > 0
				)
			)
			and
			(
				A.DateLastCertified is null 
				--or A.DateLastCertified < coalesce(V.Fields.value('(/fields/CertificationStartDate)[1]', 'datetime'), T.Fields.value('(/fields/CertificationStartDate)[1]', 'datetime'))
				or 
				(
					A.DateLastCertified is not null
					and DATEDIFF(m, 
						A.DateLastCertified, 
						coalesce(V.Fields.value('(/fields/CertificationStartDate/text())[1]', 'datetime'), T.Fields.value('(/fields/CertificationStartDate/text())[1]', 'datetime'))
					) > coalesce(V.Fields.value('(/fields/MonthsUntilCertification/text())[1]', 'int'), T.Fields.value('(/fields/MonthsUntilCertification/text())[1]', 'int'))
					and A.Status = 'Certified'
				)
				or A.Status <> 'Certified'
			)
			and A.Status <> 'Archived'
			and coalesce(V.Fields.value('(/fields/CertificationStartDate/text())[1]', 'datetime'), T.Fields.value('(/fields/CertificationStartDate/text())[1]', 'datetime')) is not null
			and A.ID not in (
							select	artifactid
							from	Workflow
							where	WorkflowType = @wt 
									and Data.value('(/fields/StartDate/text())[1]', 'datetime') between 
											coalesce(V.Fields.value('(/fields/CertificationStartDate/text())[1]', 'datetime'), T.Fields.value('(/fields/CertificationStartDate/text())[1]', 'datetime'))
											and coalesce(V.Fields.value('(/fields/CertificationEndDate/text())[1]', 'datetime'), T.Fields.value('(/fields/CertificationEndDate/text())[1]', 'datetime'))
							)
			and A.ID not in (
							select	ArtifactID
							from	Workflow
							where	WorkflowType = @wt 
									and DateCompleted is null
							)
			and A.ID not in (
							select	ArtifactID
							from	Workflow
							where	WorkflowType = @wt 
									and DATEDIFF(m, 
											DateStarted, 
											coalesce(V.Fields.value('(/fields/CertificationStartDate/text())[1]', 'datetime'), T.Fields.value('(/fields/CertificationStartDate/text())[1]', 'datetime'))
										) > coalesce(V.Fields.value('(/fields/MonthsUntilCertification/text())[1]', 'int'), T.Fields.value('(/fields/MonthsUntilCertification/text())[1]', 'int'))
							)
			and A.ID in (
						select	RD.ObjectID 
						from	[cache].[Responsibilities] RD
								left join WorkflowTypeRelation WTR_V on WTR_V.[Object] = 'ArtifactType' and WTR_V.ObjectID = RD.ObjectTypeID and WTR_V.WorkflowType = @wt and WTR_V.[Enabled] = 1 and WTR_V.ResponsibilityTypeID = RD.ResponsibilityTypeID and WTR_V.Parent = 'TaxonomyType' and WTR_V.ParentID = A.TaxonomyTypeID
								left join WorkflowTypeRelation WTR_T on WTR_T.[Object] = 'ArtifactType' and WTR_T.ObjectID = RD.ObjectTypeID and WTR_T.WorkflowType = @wt and WTR_T.[Enabled] = 1 and WTR_T.ResponsibilityTypeID = RD.ResponsibilityTypeID and WTR_T.Parent is null and WTR_T.ParentID is null
						where	RD.[Object] = 'Artifact' 
								and coalesce(WTR_V.ID, WTR_T.ID) is not null
						)

end
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

CREATE PROCEDURE GetReferenceItemValues	
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
	set @tsqlWhere = ' where ri.referenceitemtypeid = ' + cast(@listid as nvarchar(20));
	

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

create procedure [fusion].[GenerateMarkitMapLineageData]
	@fusionID int
as
begin
	SET NOCOUNT, ANSI_PADDING ON;
	SET ANSI_WARNINGS ON;

	declare @databaseName varchar(100) = 'INTRODUCTION_V95.';
	declare @sourceFieldTypeID int = 52275;
	declare @targetFieldTypeID int = 52276;
	--declare @fusionID int = 58;
	declare @mapFusionAttributeTypeID int = 710;


	IF OBJECT_ID('tempdb..#maps') IS NOT NULL
		DROP TABLE #maps;

	create table #maps (	
		ID int identity,	
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
		[Object] varchar(50),
		[ObjectID] int,
		[Source] varchar(50),
		[SourceID] int,
		[Target] varchar(50),
		[TargetID] int,
	);

	
	insert into #maps
		(SourceObject, TargetObject)
		select 
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
				and
			F_source.formattedValue like '%.cusip' -- **for testing to limit to just cusip**;
			
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

/*	IF OBJECT_ID('tempdb..#levelMap') IS NOT NULL
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
			  where SourceFusionAttributeID not in(
					select 
						m_s.SourceFusionAttributeID
					from 
						#maps m_s
						inner join #maps m_t on(m_s.SourceFusionAttributeID = m_t.TargetFusionAttributeID)
						)	  	  
			  union all
			  select 
					T.ID,
					T.SourceFusionAttributeID as SourceID,			 
					 T.TargetFusionAttributeID as TargetID,
					 C.[UltimateParentID] as [UltimateParentID],
					 C.[level] + 1
			  from #maps as T
				inner join C  
				  on T.SourceFusionAttributeID = C.TargetID and T.SourceFusionAttributeID != T.TargetFusionAttributeID
			)
			select C.ID, C.[level], C.[UltimateParentID]
			into #levelMap
			from C
			OPTION (MAXRECURSION 10) 

	update T
	set T.[level] = S.[level], T.[UltimateParentID] = S.[UltimateParentID]
	from #maps T
	inner join #levelMap S on S.ID = T.ID;
	
	--remove any that we cant find the level for
	delete from #maps where [level] is null		*/


	-- find any object related to column as the object
	update T
	set T.[object] = OI.[subject], T.[objectid] = OI.[subjectid]
	from #maps T
		inner join [IntersectDetail] OI on OI.Subject <> 'FusionAttribute' and OI.Object = 'FusionAttribute' and OI.ObjectID in (T.SourceFusionAttributeID, T.TargetFusionAttributeID) -- look for relation between non fusion object and source/target column

	-- find any business terms related to source
	update T
	set T.[source] = OI.[subject], T.[sourceid] = OI.[subjectid]
	from #maps T
		inner join [IntersectDetail] OI on OI.Subject <> 'FusionAttribute' and OI.Object = 'FusionAttribute' and OI.ObjectID = T.SourceParentObjectFusionAttributeID 


	-- find any business terms related to target
	update T
	set T.[target] = OI.[subject], T.[targetid] = OI.[subjectid]
	from #maps T
		inner join [IntersectDetail] OI on OI.Subject <> 'FusionAttribute' and OI.Object = 'FusionAttribute' and OI.ObjectID = T.TargetParentObjectFusionAttributeID 

	/*testing only!!*/
	--delete from #maps where ultimateparent not in (select ultimateparent from #maps where sourceobject = 'Back office sec.txt.cusip') -- for testing clear out some noise
		
	select * from #maps --order by [Level]
	/*end testing only*/
	
	/*delete from mapruleitem where [owner] = 'MARKIT LINEAGE'

	insert into [dbo].[MapRuleItem]
		(SourceFusionAttributeID, TargetFusionAttributeID, CreatedBy, CreatedOn, UpdatedBy, UpdatedOn, [Owner])
		select
			m.SourceFusionAttributeID,
			m.TargetFusionAttributeID,
			0,
			getutcdate(),
			0,
			getutcdate(),
			'MARKIT LINEAGE'
		from #maps m*/
end
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
				else coalesce(SAT.Name, SDT.Name, SFT.TextPath, SPT.Name, SRT.Name, STT.Name) 
			end as SubjectName,
			coalesce(SIcon.IconBackColor, '#000') as SubjectIconBackColor,
			coalesce(SIcon.IconForeColor, '#fff') as SubjectIconForeColor,
			coalesce(SIcon.IconText, substring(coalesce(SAT.Name, SDT.Name, SFT.Name, SPT.Name, SRT.Name, STT.Name, ''), 1, 2)) as SubjectIconText,
			
			IT.Object,
			IT.ObjectID,
			case IT.Object
				when 'IntersectType' then utility.DeriveIntersectTypeName(OIT.ID)
				when 'GroupType' then 'Group'
				when 'ResourceType' then 'Resource'
				else coalesce(OAT.Name, ODT.Name, OFT.TextPath, OPT.Name, ORT.Name, OTT.Name) 
			end as ObjectName,
			coalesce(OIcon.IconBackColor, '#000') as ObjectIconBackColor,
			coalesce(OIcon.IconForeColor, '#fff') as ObjectIconForeColor,
			--coalesce(OIcon.IconText, 'leaf') as ObjectIconText,
			coalesce(OIcon.IconText, substring(coalesce(OAT.Name, ODT.Name, OFT.Name, OPT.Name, ORT.Name, OTT.Name, ''), 1, 2)) as ObjectIconText,

			IT.PredicateID,
			P.Name as [PredicateName],
			P.Type as PredicateType,
			
			coalesce(IT.IsSystem, cast(0 as bit)) as IsSystem
	from	IntersectType IT with(nolock) 
			left join [Predicate] P with(nolock) on P.ID = IT.PredicateID 

			left join dbo.ArtifactType SAT with(nolock)			on IT.Subject = 'ArtifactType'			and SAT.ID = IT.SubjectID
			left join (
				select 1 as ID, 'Reference List' as Name
			) SDT												on IT.Subject = 'ReferenceItemType'		and IT.SubjectID = 0
			left join dbo.FusionAttributeType SFT with(nolock)	on IT.Subject = 'FusionAttributeType'	and SFT.ID = IT.SubjectID
			left join dbo.IntersectType SIT with(nolock)		on IT.Subject = 'IntersectType'			and SIT.ID = IT.SubjectID
			left join dbo.PolicyType SPT with(nolock)			on IT.Subject = 'PolicyType'			and SPT.ID = IT.SubjectID
			left join (
				select 1 as ID, 'Informational' as Name
				union
				select 2 as ID, 'Quality Check' as Name
				union
				select 3 as ID, 'Metric' as Name
				union
				select 4 as ID, 'Profile' as Name
			) SRT												on IT.Subject = 'RuleType'				and SRT.ID = IT.SubjectID 
			left join dbo.TaxonomyType STT with(nolock)			on IT.Subject = 'TaxonomyType'			and STT.ID = IT.SubjectID
			left join (
				select 1 as ID, 'Resource' as Name				
			) SRET												on IT.[Subject] = 'ResourceType'

			left join dbo.ArtifactType OAT with(nolock)			on IT.Object = 'ArtifactType'			and OAT.ID = IT.ObjectID			
			left join dbo.FusionAttributeType OFT with(nolock)	on IT.Object = 'FusionAttributeType'	and OFT.ID = IT.ObjectID
			left join dbo.IntersectType OIT with(nolock)		on IT.Object = 'IntersectType'			and OIT.ID = IT.ObjectID
			left join dbo.PolicyType OPT with(nolock)			on IT.Object = 'PolicyType'				and OPT.ID = IT.ObjectID
			left join (
				select 1 as ID, 'Informational' as Name
				union
				select 2 as ID, 'Quality Check' as Name
				union
				select 3 as ID, 'Metric' as Name
				union
				select 4 as ID, 'Profile' as Name
			) ORT												on IT.Object = 'RuleType'				and ORT.ID = IT.ObjectID
			left join dbo.TaxonomyType OTT with(nolock)			on IT.Object = 'TaxonomyType'			and OTT.ID = IT.ObjectID
			left join (
				select 1 as ID, 'Resource' as Name				
			) ORET												on IT.[Object] = 'ResourceType'
			left join (
				select 1 as ID, 'Reference List' as Name
			) ODT 	on IT.Object = 'ReferenceItemType'		and IT.ObjectID = 0

			left join ObjectStyle SIcon with(nolock) on SIcon.ObjectType = IT.Subject and SIcon.ObjectID =	IT.SubjectID
			left join ObjectStyle OIcon with(nolock) on OIcon.ObjectType = IT.Object and OIcon.ObjectID = IT.ObjectID
	where	coalesce(SAT.ID, SDT.ID, SIT.ID, SFT.ID, SPT.ID, SRT.ID, STT.ID, SRET.ID) is not null
			and coalesce(OAT.ID, ODT.ID, [OFT].ID, OPT.ID, ORT.ID, OTT.ID, ORET.ID) is not null
GO

alter VIEW [dbo].[WorkflowIssue]
AS
(
(select		W.ID as WorkflowID
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
			,ws.data.value('(fields/Comment)[1]','nvarchar(500)') as Comments					
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
			,ws.data.value('(fields/Comment)[1]','nvarchar(500)') as Comments					
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
GO

alter procedure [bulkload].[Promotions]
--declare
	@id int
--set @id = 231
as
begin
	set nocount on;

	declare @Object varchar(50),
			@ObjectID int,
			@Action varchar(1),
			@UpdatedOn datetime = getutcdate(),
			@UpdatedBy int = 0,
			@startDynamicFieldColumnIndex int,
			@columnCount int

	declare @ResolvedObjects table ([Object] varchar(50), ObjectID int, [Action] varchar(25), LoadID int, RowIndex int)	--This captures the INSERTED/UPDATED objects from the merge statements below.

	drop table if exists #FieldValidationRows

	create table #FieldValidationRows (
		RowIndex int,
		Valid bit
	)

	select	@Object = [Object],
			@ObjectID = ObjectID,
			@Action = [Action],
			@UpdatedBy = UpdatedBy
	from	[Load]
	where	ID = @id;

	update	LoadItem
	set		Status = null,
			StatusMessage = null
	where	LoadID = @id;

	select	@columnCount = count(1) from LoadColumn where LoadID = @id;

	declare @ParentID int = null,	--Artifact
			@currentLevel int,		--Taxonomy
			@maxLevel int			--Taxonomy

	if @Object = 'ArtifactType'
	begin
		select	@ParentID = ParentID 
		from	ArtifactType 
		where	ID = @ObjectID

		if @ParentID is not null
			begin
				set @startDynamicFieldColumnIndex = 5
			end
		else
			begin
				set @startDynamicFieldColumnIndex = 4
			end
	end
	else if @Object = 'AttributeType'
	begin
		set @startDynamicFieldColumnIndex = 4
	end
	else if @Object = 'ReferenceItemType'
	begin
		set @startDynamicFieldColumnIndex = 2
	end
	else if @Object = 'TaxonomyType'
	begin
		select	@currentLevel = 0,
				@maxLevel = max(
								case when isnumeric(replace(Name,'Level','')) = 1 then
									replace(Name,'Level','') 
								else 
									0 
								end) 
		from	LoadColumn 
		where	LoadID = @id 
				and Name like 'Level%';

		set @startDynamicFieldColumnIndex = @maxLevel + 1 + 1 -- the first 1 is for description.  the second 1 is to move to the start column of the dynamic fields, if any.
	end

	-- PARSE any dynamic fields that are specifically lookups.
	exec [bulkload].[UpdateDynamicLookupFieldColumns] @id, @startDynamicFieldColumnIndex, @columnCount

	--Note the dynamic field status for all load items.
	insert into #FieldValidationRows
		select	I.RowIndex,
				case
					when S.InvalidCount = 0 then cast(1 as bit)
					else cast(0 as bit)
				end
		from	LoadItem I
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
														and IC.Value is not null and IC.Value <> ''
												) C
							where	L.ID = @id
							) S on I.LoadID = S.LoadID and S.RowIndex = I.RowIndex

	if @Object = 'ArtifactType'
	begin
		exec bulkload.UpdateSubjectAreaColumn @id, 3

		-- Mark the rows with invalid subject areas.
		update	I
		set		I.StatusMessage = I.StatusMessage + ' Subject area could not be found.'
		from	LoadItem I
				inner join LoadItemColumn S on I.LoadID = @id and S.LoadID = I.LoadID and S.RowIndex = I.RowIndex and S.ColumnIndex = 3 and S.LookupObjectID is null

		-- Mark the rows with invalid names.
		update	I
		set		I.StatusMessage = I.StatusMessage + ' Name cannot be empty.'
		from	LoadItem I
				inner join LoadItemColumn N on I.LoadID = @id and N.LoadID = I.LoadID and N.RowIndex = I.RowIndex and N.ColumnIndex = 1 and N.Value is null

		if @ParentID is not null
			begin
				-- Parse the parents, if any.
				exec bulkload.UpdateItemColumnByType @id, 'ArtifactType', @ParentID, 3, 4

				drop table if exists #ParentArtifacts

				select	I.LoadID,
						I.RowIndex,
						I.LookupObjectID as ID,
						@ObjectID as ArtifactTypeID,
						I.Value as Name,
						D.Value as Description,
						S.LookupObjectID as TaxonomyTypeID,
						P.LookupObjectID as ParentID,
						T.ID as ExistingArtifactID
				into	#ParentArtifacts
				from	LoadItemColumn I
						inner join LoadItemColumn S on S.LoadID = I.LoadID and S.RowIndex = I.RowIndex and S.ColumnIndex = 3 and S.LookupObjectID is not null
						inner join LoadItemColumn P on P.LoadID = I.LoadID and P.RowIndex = I.RowIndex and P.ColumnIndex = 4 and P.LookupObjectID is not null
						inner join LoadItemColumn D on D.LoadID = I.LoadID and D.RowIndex = I.RowIndex and D.ColumnIndex = 2
						inner join #FieldValidationRows V on V.RowIndex = I.RowIndex and V.Valid = 1
						left join Artifact T on T.ArtifactTypeID = @ObjectID and T.TaxonomyTypeID = S.LookupObjectID and T.ParentID = P.LookupObjectID and T.Name = I.Value
				where	I.LoadID = @id and I.ColumnIndex = 1 AND I.Value IS NOT NULL;

				update	T
				set		--T.ParentID = null,
						T.[Description] = S.[Description],
						T.[Status] = 
						( CASE  
							WHEN (T.[Status] = 'Certified') THEN 'Draft' 							
							ELSE  (T.[Status])
							END 
						),
						T.UpdatedBy = @UpdatedBy,
						T.UpdatedOn = @UpdatedOn
				from	Artifact T
						inner join #ParentArtifacts S on S.ExistingArtifactID = T.ID;

				insert into @ResolvedObjects
					select 'Artifact', ExistingArtifactID, 'UPDATE', LoadID, RowIndex from #ParentArtifacts where ExistingArtifactID is not null

				insert into Artifact (ArtifactTypeID, TaxonomyTypeID, ParentID, Name, [Description], [Status], CreatedOn, UpdatedOn, UpdatedBy)
					select	ArtifactTypeID, TaxonomyTypeID, ParentID, Name, [Description], 'Draft', @UpdatedOn, @UpdatedOn, @UpdatedBy
					from	#ParentArtifacts 
					where	ExistingArtifactID is null

				insert into @ResolvedObjects
					select	'Artifact', A.ID, 'INSERT', I.LoadID, I.RowIndex 
					from	#ParentArtifacts I
							inner join Artifact A on A.ArtifactTypeID = I.ArtifactTypeID and A.TaxonomyTypeID = I.TaxonomyTypeID and A.ParentID = I.ParentID and A.Name = I.Name and I.ExistingArtifactID is null

				-- Mark the rows with invalid parents.
				update	I
				set		I.StatusMessage = 'Parent could not be found.'
				from	LoadItem I
						inner join LoadItemColumn P on I.LoadID = @id and P.LoadID = I.LoadID and P.RowIndex = I.RowIndex and P.ColumnIndex = 4 and P.LookupObjectID is null
			end
		else
			begin
				drop table if exists #NoParentArtifacts

				select	I.LoadID,
						I.RowIndex,
						I.LookupObjectID as ID,
						I.Value as Name,
						D.Value as Description,
						@ObjectID as ArtifactTypeID,
						S.LookupObjectID as TaxonomyTypeID,
						T.ID as ExistingArtifactID
				into	#NoParentArtifacts
				from	LoadItemColumn I
						inner join LoadItemColumn S on S.LoadID = I.LoadID and S.RowIndex = I.RowIndex and S.ColumnIndex = 3 and S.LookupObjectID is not null
						inner join LoadItemColumn D on D.LoadID = I.LoadID and D.RowIndex = I.RowIndex and D.ColumnIndex = 2
						inner join #FieldValidationRows V on V.RowIndex = I.RowIndex and V.Valid = 1
						left join Artifact T on T.ArtifactTypeID = @ObjectID and T.TaxonomyTypeID = S.LookupObjectID and T.Name = I.Value
				where	I.LoadID = @id and I.ColumnIndex = 1 AND I.Value IS NOT NULL
				
				update	T
				set		T.ParentID = null,
						T.[Description] = S.[Description],						
						T.[Status] = 
						( CASE  
							WHEN (T.[Status] = 'Certified') THEN 'Draft' 							
							ELSE  (T.[Status])
						  END 
						),
						T.UpdatedBy = @UpdatedBy,
						T.UpdatedOn = @UpdatedOn
				from	Artifact T
						inner join #NoParentArtifacts S on S.ExistingArtifactID = T.ID;

				insert into @ResolvedObjects
					select 'Artifact', ExistingArtifactID, 'UPDATE', LoadID, RowIndex from #NoParentArtifacts where ExistingArtifactID is not null

				insert into Artifact (ArtifactTypeID, TaxonomyTypeID, Name, [Description], [Status], CreatedOn, UpdatedOn, UpdatedBy)
					select	ArtifactTypeID, TaxonomyTypeID, Name, [Description], 'Draft', @UpdatedOn, @UpdatedOn, @UpdatedBy
					from	#NoParentArtifacts 
					where	ExistingArtifactID is null

				insert into @ResolvedObjects
					select	'Artifact', A.ID, 'INSERT', I.LoadID, I.RowIndex 
					from	#NoParentArtifacts I
							inner join Artifact A on A.ArtifactTypeID = I.ArtifactTypeID and A.TaxonomyTypeID = I.TaxonomyTypeID and A.Name = I.Name and I.ExistingArtifactID is null
			end
	end
	else if @Object = 'AttributeType'
	begin
		-- Clean Owner Type field.
		update	LoadItemColumn
		set		Value = case when charindex('Type', Value) > 0 then Value else Value + 'Type' end
		where	LoadID = @id and ColumnIndex = 1;

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
									inner join LoadItem LI on LI.LoadID = L.ID and L.ID = @id
									inner join [LoadItemColumn] C1 on C1.LoadID = LI.LoadID and C1.RowIndex = LI.RowIndex and C1.ColumnIndex = 1 --'Owner Type' 
									inner join [LoadItemColumn] C2 on C2.LoadID = LI.LoadID and C2.RowIndex = LI.RowIndex and C2.ColumnIndex = 2 --'Owner Type Name'
									inner join cache.ObjectDetails D on D.[Object] = C1.Value and D.[Name] = C2.Value
							) S on S.LoadID = T.LoadID and S.RowIndex = T.RowIndex and S.ColumnIndex = T.ColumnIndex;

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
									inner join LoadItem LI on LI.LoadID = L.ID and L.ID = @id
									--inner join [LoadItemColumn] C1 on	C1.LoadID = LI.LoadID	and C1.RowIndex = LI.RowIndex	and C1.ColumnIndex = 1 --'Owner Type' 
									inner join [LoadItemColumn] C2 on C2.LoadID = LI.LoadID and C2.RowIndex = LI.RowIndex and C2.ColumnIndex = 2 --'Owner Type Name'
									inner join [LoadItemColumn] C3 on C3.LoadID = LI.LoadID	and C3.RowIndex = LI.RowIndex and C3.ColumnIndex = 3 --'Owner Name'
									inner join cache.ObjectDetails D on D.[ObjectType] = C2.[LookupObject] and D.ObjectTypeID = C2.LookupObjectID and D.[Name] = C3.Value
							) S on S.LoadID = T.LoadID and S.RowIndex = T.RowIndex and S.ColumnIndex = T.ColumnIndex;

		merge	[Attribute] T
		using	(
				select	I.LoadID,
						I.RowIndex,
						@ObjectID as AttributeTypeID,
						C.LookupObject as [Object],
						C.LookupObjectID as ObjectID
				from	[LoadItem] I
						inner join [LoadItemColumn] C on I.LoadID = @id and C.LoadID = I.LoadID and C.RowIndex = I.RowIndex and C.ColumnIndex = 3
						and C.LookupObject is not null
						and C.LookupObjectID is not null
						inner join #FieldValidationRows V on V.RowIndex = I.RowIndex and V.Valid = 1
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
	else if @Object = 'ReferenceItemType'
	begin
		-- Mark the rows with invalid codes.
		update	I
		set		I.StatusMessage = I.StatusMessage + ' Code cannot be empty.'
		from	LoadItem I
				inner join LoadItemColumn N on I.LoadID = @id and N.LoadID = I.LoadID and N.RowIndex = I.RowIndex and N.ColumnIndex = 1 and N.Value is null

		merge	ReferenceItem T
		using	(
				select	I.LoadID,
						I.RowIndex,
						@ObjectID as ReferenceItemTypeID,
						C.Value as Code
				from	[LoadItem] I
						inner join [LoadItemColumn] C on C.LoadID = I.LoadID and C.RowIndex = I.RowIndex and C.ColumnIndex = 1 and C.Value is not null
						inner join #FieldValidationRows V on V.RowIndex = I.RowIndex and V.Valid = 1
				where	I.LoadID = @id
				) S
		on		(T.ReferenceItemTypeID = S.ReferenceItemTypeID and T.Code = S.Code)
		when	matched then
				update	set T.[Code] = S.[Code],
							T.UpdatedBy = @UpdatedBy,
							T.UpdatedOn = @UpdatedOn
		when	not matched then
				insert (ReferenceItemTypeID, Code, UpdatedOn, UpdatedBy)
				values (S.ReferenceItemTypeID, S.Code, @UpdatedOn, @UpdatedBy)
		output	'ReferenceItem', inserted.ID, $action, S.LoadID, S.RowIndex into @ResolvedObjects;
	end
	else if @Object = 'TaxonomyType'
	begin
		declare @rowCount int,
				@rowCurr int;

		declare @levels table (id int, ColumnIndex int, RowIndex int, [Level] varchar(50), Value varchar(250),MaxLevel int, TaxonomyID int, ParentID int, [Status] varchar(50));

		with v as
		(
			select	L.ID, 
					L.Object, 
					L.ObjectID, 
					LC.Name, 
					LC.ColumnIndex, 
					IC.RowIndex, 
					IC.Value, 
					replace(LC.Name,'Level','') as [Level], 
					T.ID as TaxonomyID 
			from	[Load] L
					join LoadColumn LC on LC.LoadID = L.ID
					join LoadItemColumn IC on IC.LoadID = LC.LoadID AND IC.ColumnIndex = LC.ColumnIndex
					left join Taxonomy T on T.TaxonomyTypeID = L.ObjectID and T.[Level] = replace(LC.Name,'Level','') and T.Name = IC.Value
			where	L.ID = @id 
					AND ltrim(rtrim(IC.Value)) != '' 
					and LC.Name like 'Level%'  
		)

		insert into @levels
			select		distinct
						row_number() over (partition by 1 order by v.[Level]) as ID,
						v.ColumnIndex,
						v.RowIndex,
						v.[Level],
						v.Value,
						m.[Level] as MaxLevel,
						v.TaxonomyID,
						p.TaxonomyID as ParentID,
						'UPDATE' as [Status]
			from		v	
						left join v p on p.RowIndex = v.RowIndex and v.TaxonomyID is null and p.ColumnIndex = (v.ColumnIndex - 1)
						inner join v m on m.RowIndex = v.RowIndex and m.[Level] = (select max([Level]) from v where RowIndex = m.RowIndex)
			order by	v.[Level] asc;

		-- calculate hierarchy
		while @currentLevel <= @maxLevel
		begin
			set @currentLevel = @currentLevel + 1;
				
			update	LV
			set		LV.ParentID = P.ID
			from	@levels LV
					left join @levels P on P.[Level] = (LV.[Level] - 1) AND LV.RowIndex = P.RowIndex
			where	LV.[Level] = @currentLevel;
		end

		select @rowCurr = 0, @rowCount = count(*) from @levels;

		while @rowCurr <= @rowCount
		begin
			set @rowCurr = @rowCurr + 1;

			--parent does not exist or leading columns were not filled
			if (select ParentID from @levels where id = @rowCurr) IS NULL AND (select Level from @levels where id = @rowCurr) > 1
			begin
				update	@levels 
				set		[Status] = 'ERROR' 
				where	rowIndex = (select rowindex from @levels where id = @rowCurr);
				continue;
			end

			--update the TaxonomyID for records that do not yet have it
			if (select level from @levels where id = @rowCurr) = 1
			begin
				update	LV
				set		TaxonomyID = T.ID
				from	@levels LV
						join Load L on L.ID = @id
						join Taxonomy T on T.Name = LV.Value and T.ParentID is NULL and T.Level = LV.Level and T.TaxonomyTypeID = L.ObjectID
				where	LV.ID = @rowCurr;
			end
			else
			begin
				update	LV
				set		TaxonomyID = T.ID
				from	@levels LV
						left join @levels P on P.ID = LV.ParentID
						join Taxonomy T on T.Name = LV.Value and T.ParentID = P.TaxonomyID and T.Level = LV.Level
				where	LV.ID = @rowCurr;
			end

			if (select TaxonomyID from @levels where id = @rowCurr) IS NULL
			begin
				--insert the new taxonomy
				insert into Taxonomy (TaxonomyTypeID, ParentID, Name, [Description], UpdatedOn, UpdatedBy)
					select	distinct
							L.ObjectID as TaxonomyTypeID,
							LVP.TaxonomyID as ParentID,
							LV.Value as Name,
							case 
								when LV.Level = LV.MaxLevel then LI.Value
								else ''
							end as Description,
							@UpdatedOn as UpdatedOn,
							@UpdatedBy as UpdatedBy
					from	@levels LV
							left join @levels LVP on LVP.ID = LV.ParentID
							join [Load] L on L.ID = @id
							inner join LoadColumn LC on LC.Name = 'Description' and LC.LoadID = @id
							inner join LoadItemColumn LI on LI.RowIndex = LV.RowIndex AND LI.ColumnIndex = LC.ColumnIndex AND LI.LoadID = @id
							inner join #FieldValidationRows V on V.RowIndex = LI.RowIndex and V.Valid = 1
					where	LV.ID = @rowCurr

				update	@levels 
				set		[Status] = 'INSERT' 
				where	id = @rowCurr;

				--set the levels taxonomy id after insert
				update	LV
				set		TaxonomyID = T.ID
				from	@levels LV
						left join @levels P on P.ID = LV.ParentID
						join Taxonomy T on T.Name = LV.Value and coalesce(T.ParentID,-1) = coalesce(P.TaxonomyID,-1) and T.Level = LV.Level
				where	LV.ID = @rowCurr;
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
						inner join LoadColumn LC on LC.Name = 'Description' and LC.LoadID = @id
						inner join LoadItemColumn LI on LI.RowIndex = LV.RowIndex AND LI.ColumnIndex = LC.ColumnIndex AND LI.LoadID = @id
						inner join #FieldValidationRows V on V.RowIndex = LI.RowIndex and V.Valid = 1;
			end
		end --end while
			

		--remove error rows
		delete from @levels
		where rowindex in (select rowindex from @levels where status is null or status = 'ERROR');

					--insert object statuses
		insert into @ResolvedObjects ([Object], ObjectID, [Action], LoadID, RowIndex)
			select	'Taxonomy',
					TaxonomyID,
					[Status],
					@id,
					RowIndex
			from	@levels;
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
			inner join	@ResolvedObjects S on S.LoadID = T.LoadID and S.RowIndex = T.RowIndex;

	-- Update the LoadItems that were not successfully added or updated.
	update	LoadItem
	set		[Status] = 0,
			[StatusMessage] = coalesce([StatusMessage], 'Item could not be added nor updated.')
	where	LoadID = @id
			and [ObjectID] is null

	update	LoadItem
	set		[Status] = 0,
			[StatusMessage] = coalesce([StatusMessage], 'Item could not be added nor updated.')
	where	LoadID = @id
			and RowIndex in (select RowIndex from #FieldValidationRows where Valid = 0)
			and [ObjectID] is null

	-- merge the dynamic fields involved with this load into the Fields table.  Needs to be here as this proc looks at the LaodItem table for the Object and ObjectID.
	exec [bulkload].MergeDynamicLookupFields @id, @startDynamicFieldColumnIndex, @columnCount

	--Finally, close out the Load.
	update	[Load] 
	set		DateCompleted = getutcdate()
	where	ID = @id
end
GO

alter PROCEDURE [dbo].[GetCommentDetailByID]
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
	/*	UNION ALL
		SELECT	C.ID, 
				C.ParentID
		FROM	Comment C
				INNER JOIN P PAR ON PAR.ID = C.ParentID*/
	)

	SELECT		C.*,
				C.CreatingResourceID,
				O.Name as ObjectName,				
				O.Url as ObjectUrl,
				case
					WHEN C.ParentID IS NULL THEN C.OwnerObjectType
					ELSE 'Resource'
				end as ObjectType,
				O.Name as ResourceName,
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

alter table fusion.RulePromotion add CreatedOn datetime CONSTRAINT [DF_RulePromotion_CreatedOn]  DEFAULT (getutcdate()) not null
go

alter table fusion.RulePromotion add UpdatedOn datetime CONSTRAINT [DF_RulePromotion_UpdatedOn]  DEFAULT (getutcdate()) not null
go

alter PROCEDURE [fusion].[Rules] 
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
			@NumberOfNewRelations int,
			@promotionNeedsToRun bit
	
	set	@NumberOfRules = 0;	
	set @NumberOfNewTaxonomies = 0;
	set @NumberOfNewReferenceItems = 0;
	set @NumberOfNewReferences = 0;
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
		FilterAttributeID int,
		FilterAttributeTypeID int
	);

	IF OBJECT_ID('tempdb..#attributes') IS NOT NULL
		DROP TABLE #attributes;

	create table #attributes (
		ID int identity,
		RuleID int,
		RuleStepID int,
		[Action] varchar(25),
		AttributeID int,
		ParentAttributeID int null,
		AttributeType varchar(25)
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
				R.ObjectID,
				I.ObjectID as FilterAttributeID,
				coalesce(A.FusionAttributeTypeID, Q.ID, R.ObjectID) as FilterAttributeTypeID--coalesce(A.FusionAttributeTypeID, F.ObjectID, Q.ID, R.ObjectID) as FilterAttributeTypeID
		from	[fusion].[Rule] R
				inner join [fusion].[RuleItem] I on I.RuleID = R.ID and R.[Enabled] = 1
				left join FusionAttribute A on A.ID = I.ObjectID AND I.ObjectType = 'FusionAttribute'
				left join FusionQueryAttributeType Q on Q.ID = R.ObjectID and R.ObjectType = 'FusionQueryAttribute'
				--left join FieldType F on F.ID = I.ObjectID and I.ObjectType = 'FusionQueryAttribute'

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
				@FilterAttributeID int,
				@FilterAttributeTypeID int

		select	@RuleID = RuleID,
				@FusionObjectType = ObjectType,
				@FusionObjectID = ObjectID,
				@FusionID = FusionID,
				@FilterAttributeID = FilterAttributeID,
				@FilterAttributeTypeID = FilterAttributeTypeID
		from	#rules
		where	ID = @currentID

		if @FusionObjectID = @FilterAttributeTypeID AND @FilterAttributeID is not null
			begin
				-- You are on a specific nodes of same type.  Just copy to target table.
				insert into #attributes 
					select	@RuleID, 
							S.ID,
							S.[Action],
							@FilterAttributeID,
							A.ParentID,
							@FusionObjectType
					from	[fusion].[RuleStep] S
							inner join FusionAttribute A on A.ID = @FilterAttributeID and A.Deleted = 0
					where	S.RuleID = @RuleID
					order by S.Step
			end
		else
			begin
				if @FusionObjectType = 'FusionQueryAttributeType'
					begin
						--take all query attributes
						if @FilterAttributeID is null
							begin
								insert into #attributes
									select	@RuleID,
											S.ID,
											S.[Action],
											FT.ID,
											NULL,
											@FusionObjectType
									from	FusionQueryAttribute FT
											inner join fusion.RuleStep S on S.RuleID = @RuleID and FT.FusionQueryAttributeTypeID = @FusionObjectID and FT.Deleted = 0
							end
						else
							--take specific query attribute
							begin
								insert into #attributes
									select	@RuleID,
											S.ID,
											S.[Action],
											FT.ID,
											NULL,
											@FusionObjectType
									from	FusionQueryAttribute FT
											inner join fusion.RuleStep S on S.RuleID = @RuleID and FT.FusionQueryAttributeTypeID = @FusionObjectID and FT.ID = @FilterAttributeID and FT.Deleted = 0
							end
					end
				else
					begin
						-- You are on an attribute higher up in hierarchy.	
						if @FilterAttributeID is null
						begin
							--  If there is NO filtered attribute ID, then you need to get every attribute in system for the partiular fusion instance.
							insert into #attributes
								select	@RuleID, 
										S.ID,
										S.[Action],
										FA.ID,
										FA.ParentID,
										@FusionObjectType
								from	FusionAttribute FA 
										inner join [fusion].[RuleStep] S on S.RuleID = @RuleID and FA.FusionID = @FusionID and FA.FusionAttributeTypeID = @FusionObjectID
										left join #attributes A on A.AttributeID = FA.ID and A.AttributeType = 'FusionAttributeType' and A.RuleID = S.RuleID and A.ID is null
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
										where	ID = @FilterAttributeID
												and FusionID = @FusionID
												and Deleted = 0
										union all
										select	C.ID,
												C.ParentID,
												C.FusionAttributeTypeID
										from	FusionAttribute C
												inner join fa P on C.ParentID = P.ID and C.Deleted = 0 --and P.ID <> C.ID
										)
	
							insert into #attributes
								select	@RuleID, 
										S.ID,
										S.[Action],
										FA.ID,
										FA.ParentID,
										@FusionObjectType
								from	FA 
										inner join [fusion].[RuleStep] S on S.RuleID = @RuleID and FA.FusionAttributeTypeID = @FusionObjectID
										left join #attributes A on A.AttributeID = FA.ID and A.AttributeType = 'FusionAttributeType' and A.RuleID = S.RuleID and A.ID is null
								where	FA.FusionAttributeTypeID = @FusionObjectID
								order by FA.ID, S.Step
						end
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
				inner join [fusion].[RuleStep] RS on M.RuleStepID = RS.ID and (M.SourceFieldName in ('ID', 'Name', 'TextPath') OR M.IsConstantValue = 1)
				inner join #attributes A on A.RuleID = RS.RuleID
				inner join FusionAttribute FA on FA.ID = A.AttributeID and A.AttributeType = 'FusionAttributeType' and FA.Deleted = 0

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
				inner join #attributes A on A.RuleID = RS.RuleID and A.AttributeType = 'FusionAttributeType' --and A.AttributeID = M.SourceFieldTypeID
				inner join Field F on F.ObjectType = 'FusionAttribute' and F.ObjectID = A.AttributeID
				inner join FieldType FT on FT.ID = F.FieldTypeID and M.SourceFieldName = FT.Name


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
					when M.SourceFieldName = 'ID' then cast(A.AttributeID as nvarchar)
					when M.IsConstantValue = 1 then M.ConstantValue
				end
		from	[fusion].[RuleStepMapping] M
				inner join [fusion].[RuleStep] RS on M.RuleStepID = RS.ID and (M.SourceFieldName = 'ID' OR M.IsConstantValue = 1)
				inner join #attributes A on A.RuleID = RS.RuleID and A.AttributeType = 'FusionQueryAttributeType'

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
				inner join #attributes A on A.RuleID = RS.RuleID and A.AttributeType = 'FusionQueryAttributeType' --and A.AttributeID = M.SourceFieldTypeID
				inner join Field F on F.ObjectType = 'FusionQueryAttribute' and F.ObjectID = A.AttributeID
				inner join FieldType FT on FT.ID = F.FieldTypeID and M.SourceFieldName = FT.Name

	-- Update the fields table above with values for all dynamic fields.
	--update	T
	--set		T.Value = S.Value
	--from	#fields T
	--		inner join #attributes A on A.ID = T.ID and A.AttributeType = 'FusionQueryAttributeType'
	--		inner join Field S on S.ObjectType = 'FusionQueryAttribute' and S.ObjectID = A.AttributeID;

	--update	T
	--set		T.Value = S.Value
	--from	#fields T
	--		inner join #attributes A on A.ID = T.ID and A.AttributeType = 'FusionAttributeType'
	--		inner join Field S on S.ObjectType = 'FusionAttribute' and S.ObjectID = A.AttributeID and S.FieldTypeID = T.SourceFieldTypeID

--BEGIN: TESTING ---------------------------------------

--select * from #rules;
--select * from #attributes order by ID;
--select * from #fields order by ID;

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

			declare @fields table (SourceFieldName nvarchar(250), SourceFieldTypeID int, TargetFieldName nvarchar(250), TargetFieldTypeID int, Value nvarchar(4000))
			declare @settings table (Name nvarchar(100), Value nvarchar(250))
			

			select	@RuleID = R.RuleID,
					@RuleStepID = A.RuleStepID,
					@Action = A.[Action],
					@FusionID = R.FusionID,
					@AttributeTypeID = R.ObjectID,
					@AttributeID = A.AttributeID,
					@AttributeType = replace(A.AttributeType,'Type',''),
					@ResultObject = P.ObjectType,
					@ResultObjectID = P.ObjectID
			from	#rules R
					inner join #attributes A on A.RuleID = R.RuleID and A.ID = @currentID
					left join [Fusion].RulePromotion P on P.AttributeID = A.AttributeID and P.AttributeType = replace(A.AttributeType, 'Type','') and P.RuleID = R.RuleID and P.RuleStepID = A.RuleStepID

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
								@description nvarchar(4000) = null

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

											insert into Artifact ( ParentID, ArtifactTypeID, TaxonomyTypeID, Name, Description, Status, UpdatedOn, UpdatedBy, CreatedOn )
											values ( @ParentObjectID, @ObjectTypeIDToPromoteTo, @modelTypeID, @name, @description, 'Draft', getutcdate(), 0, getutcdate() )

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
									insert into [Intersect] (IntersectTypeID, Classification, Subject, SubjectID, Object, ObjectID, Deleted, CreatedBy, CreatedOn, UpdatedBy, UpdatedOn)
									values					(@R_IntersectTypeID, 2, @R_Subject, @R_SubjectID, @R_Object, @R_ObjectID, 0, @r, @d, @r, @d)  

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

	---- Add new relations as needed
	--exec [utility].[PromoteFusionAttributesRelations] @NumberOfNewRelations output

	---- Handle any fusionlookup fields
	--exec [utility].[PromoteFusionAttributeLookups]


	----Log this run done
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
END
GO

ALTER  FUNCTION [utility].[GetDirectlyAssignedResponsibilityList]
(
	@Object varchar(50),
	@ObjectID int,
	@Priority int
)
RETURNS 
@tbl TABLE 
(
	[Source] varchar(50), 
	Visible bit,
	ResponsibilityID int,
	ResponsibilityTypeID int,
	AssigningItem varchar(50),
	AssigningItemID int,
	[Object] varchar(50),
	ObjectID int,
	ContextHash varchar(50),
	[Priority] int
)
AS
BEGIN

	if @Object = 'Artifact'
		begin
			insert into @tbl
				select	'Artifact Direct' as [Source],
						R.Visible,
						R.ID,
						R.ResponsibilityTypeID,
						@Object as AssigningItemType,
						T.ID as AssigningItemID,
						@Object as ObjectType,
						T.ID as ObjectID,
						utility.GetResponsibilityContextHash(R.ID),
						@Priority as [Priority]
				from	Artifact T 
						inner join Responsibility R on R.ObjectType = @Object and R.ObjectID = T.ID 
							and (
									(T.ID = @ObjectID and @ObjectID is not null)
									or (@ObjectID is null)
								);
		end
	if @Object = 'ArtifactType'
		begin
			insert into @tbl
				select	'Artifact Type Direct' as [Source],
						R.Visible,
						R.ID,
						R.ResponsibilityTypeID,
						@Object as AssigningItemType,
						T.ID as AssigningItemID,
						@Object as ObjectType,
						T.ID as ObjectID,
						utility.GetResponsibilityContextHash(R.ID),
						@Priority as [Priority]
				from	ArtifactType T 
						inner join Responsibility R on R.ObjectType = @Object and R.ObjectID = T.ID 
							and (
									(T.ID = @ObjectID and @ObjectID is not null)
									or (@ObjectID is null)
								);
		end
	if @Object = 'Fusion'
		begin
			insert into @tbl
				select	'Fusion Direct' as [Source],
						R.Visible,
						R.ID,
						R.ResponsibilityTypeID,
						@Object as AssigningItemType,
						T.ID as AssigningItemID,
						@Object as ObjectType,
						T.ID as ObjectID,
						utility.GetResponsibilityContextHash(R.ID),
						@Priority as [Priority]
				from	Fusion T 
						inner join Responsibility R on R.ObjectType = @Object and R.ObjectID = T.ID
							and (
									(T.ID = @ObjectID and @ObjectID is not null)
									or (@ObjectID is null)
								);
		end
	if @Object = 'FusionType'
		begin
			insert into @tbl
				select	'Fusion Type Direct' as [Source],
						R.Visible,
						R.ID,
						R.ResponsibilityTypeID,
						@Object as AssigningItemType,
						T.ID as AssigningItemID,
						@Object as ObjectType,
						T.ID as ObjectID,
						utility.GetResponsibilityContextHash(R.ID),
						@Priority as [Priority]
				from	FusionType T 
						inner join Responsibility R on R.ObjectType = @Object and R.ObjectID = T.ID
							and (
									(T.ID = @ObjectID and @ObjectID is not null)
									or (@ObjectID is null)
								);
		end
	if @Object = 'Rule'
		begin
			insert into @tbl
				select	'Rule Direct' as [Source],
						R.Visible,
						R.ID,
						R.ResponsibilityTypeID,
						@Object as AssigningItemType,
						RU.ID as AssigningItemID,
						@Object as ObjectType,
						RU.ID as ObjectID,
						utility.GetResponsibilityContextHash(R.ID),
						@Priority as [Priority]
				from	[Rule] RU 
						inner join Responsibility R on R.ObjectType = @Object and R.ObjectID = RU.ID
							and (
								(RU.ID = @ObjectID and @ObjectID is not null) or (@ObjectID is null)
								);
		end
	if @Object = 'RuleType'
		begin
			insert into @tbl
				select	'Rule Type Direct' as [Source],
						R.Visible,
						R.ID,
						R.ResponsibilityTypeID,
						@Object as AssigningItemType,
						@ObjectID as AssigningItemID,
						@Object as ObjectType,
						@ObjectID as ObjectID,
						utility.GetResponsibilityContextHash(R.ID),
						@Priority as [Priority]
				from	Responsibility R where R.ObjectType = @Object and R.ObjectID = @ObjectID;				
		end
	if @Object = 'Taxonomy'
		begin
			insert into @tbl
				select	'Taxonomy Direct' as [Source],
						R.Visible,
						R.ID,
						R.ResponsibilityTypeID,
						@Object as AssigningItemType,
						T.ID as AssigningItemID,
						@Object as ObjectType,
						T.ID as ObjectID,
						utility.GetResponsibilityContextHash(R.ID),
						@Priority as [Priority]
				from	Taxonomy T 
						inner join Responsibility R on R.ObjectType = @Object and R.ObjectID = T.ID
							and (
								(T.ID = @ObjectID and @ObjectID is not null) or (@ObjectID is null)
								)
		end
	if @Object = 'TaxonomyType'
		begin
			insert into @tbl
				select	'Taxonomy Type Direct' as [Source],
						R.Visible,
						R.ID,
						R.ResponsibilityTypeID,
						@Object as AssigningItemType,
						T.ID as AssigningItemID,
						@Object as ObjectType,
						T.ID as ObjectID,
						utility.GetResponsibilityContextHash(R.ID),
						@Priority as [Priority]
				from	TaxonomyType T 
						inner join Responsibility R on R.ObjectType = @Object and R.ObjectID = T.ID
							and (
								(T.ID = @ObjectID and @ObjectID is not null) or (@ObjectID is null)
								)
		end
		if @Object = 'ReferenceItemType'
		begin
			insert into @tbl
				select	'Reference Item Type Direct' as [Source],
						R.Visible,
						R.ID,
						R.ResponsibilityTypeID,
						@Object as AssigningItemType,
						T.ID as AssigningItemID,
						@Object as ObjectType,
						T.ID as ObjectID,
						utility.GetResponsibilityContextHash(R.ID),
						@Priority as [Priority]
				from	ReferenceItemType T 
						inner join Responsibility R on R.ObjectType = @Object and R.ObjectID = T.ID
							and (
									(T.ID = @ObjectID and @ObjectID is not null)
									or (@ObjectID is null)
								);
		end
		if @Object = 'PolicyType'
		begin
			insert into @tbl
				select	'Policy Type Direct' as [Source],
						R.Visible,
						R.ID,
						R.ResponsibilityTypeID,
						@Object as AssigningItemType,
						T.ID as AssigningItemID,
						@Object as ObjectType,
						T.ID as ObjectID,
						utility.GetResponsibilityContextHash(R.ID),
						@Priority as [Priority]
				from	PolicyType T 
						inner join Responsibility R on R.ObjectType = @Object and R.ObjectID = T.ID
							and (
								(T.ID = @ObjectID and @ObjectID is not null) or (@ObjectID is null)
								)
		end
	RETURN 
END
GO

ALTER FUNCTION [dbo].[GenerateObjectUrl] 
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
		WHEN 'Rule' THEN 'quality/rule/' + CAST(@ObjectID as varchar)
		WHEN 'Taxonomy' THEN 'model/' + CAST(@TypeID as varchar) + '/id/' + CAST(@ObjectID as varchar)
		WHEN 'TaxonomyType' THEN 'model/' + CAST(@ObjectID as varchar) + '/structure'		
	END

	SET @Url = @Prefix + @Url

	RETURN @Url
END
GO

ALTER FUNCTION [utility].[GetFormattedFieldLookupValue]
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

				SET @current = @current + 1
			end
		end
	end

	return @formattedValue
END
GO

ALTER FUNCTION [utility].[ShouldPromotionRun]
(
)
RETURNS bit
AS
BEGIN	
	DECLARE @lastPromotionRun datetime;
	
	-- if there are no enabled rules then say no
	if not exists(select 1 from [fusion].[Rule] where [Enabled] = 1)
	begin
		return 0;
	end;

	-- GET LAST RUN OF THE PROMOTON PROCESS FROM DBO.FUSIONATTRIBUTEPROMOTIONLOGSUMMARY
	select @lastPromotionRun = max(DateStarted) from fusion.RuleLog
		
	if(@lastPromotionRun is null)
	begin
	 set @lastPromotionRun = '1970-01-01';
	end;

	-- promotion should not run if there is a current job out there that has not completed and this job was started within the last day
	if exists (select 1 from fusion.RuleLog where DateCompleted is null and DateStarted > DATEADD(day,-1,CURRENT_TIMESTAMP))
	begin
		return 0; --should not run already running 
	end;

	--PROMOTION ONLY NEEDS TO RUN IF FUSION HAS COMPLETED ON A FUSION ID THAT HAS RULES SETUP AGAINST IT.
	if exists	(
				select	1 
				from	fusion.Execution E 
						inner join [fusion].[Rule] R on R.fusionid = E.fusionid	
				where	R.[enabled] = 1 
						and E.datecompleted > @lastPromotionRun
						and (E.Adds + E.Updates + E.Deletes) > 0
				)

	begin		
		RETURN 1;		
	end;

	-- OR THE PROMOTION RULES HAVE BEEN MODIFIED, ADDED OR DELETED SINCE LAST RUN OF PROMOTION	
	if exists (select 1 from fusion.[Rule] where UpdatedOn > @lastPromotionRun)
	begin
		return 1;
	end;
		
	RETURN 0;
END
GO

CREATE procedure [fusion].[UpdateFusionTextPaths]
	@FusionID int
as
begin
	set nocount on;

	WITH hierarchy (id, itempath) AS
	(
		SELECT id, cast(name as nvarchar(2500))
		FROM fusionattribute
		WHERE fusionid = @FusionID and parentid is null

		UNION ALL

		SELECT gp.id, cast(gps.itempath + '.' + gp.name as nvarchar(2500))
		FROM fusionattribute gp
		JOIN hierarchy gps ON gps.id = gp.parentid
	)
	UPDATE T
	set T.textpath = cte.itempath
	from fusionattribute T
	inner join 
		hierarchy cte
	on cte.id = T.id
	OPTION (MAXRECURSION 10)
end
GO

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