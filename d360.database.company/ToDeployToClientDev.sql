alter table FusionQueryAttributeType alter column [Query] nvarchar(max) not null
go

delete from [cache].[object] where [object] = 'FusionAttribute';
go

drop table [dbo].[SiteNavOrder]
go

CREATE NONCLUSTERED INDEX [IX_FusionAttribute_FusionID_Deleted_ParentID]
    ON [dbo].[FusionAttribute]([FusionID] ASC, [Deleted] ASC, [ParentID] ASC);
GO

DROP INDEX [IX_FusionID_ParentID] on Fusion
GO

ALTER TABLE FusionStatusLog ADD [FullRefresh]     BIT              CONSTRAINT [DF_FusionStatusLog_FullRefresh] DEFAULT ((0)) NOT NULL
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
)
GO

--add owner columns used by markit lineage
alter table mapruleitem add [Owner] varchar(100) null;
go

alter table mapitem add [Owner] varchar(100) null;
go

alter table mapruleitemmapitem add [Owner] varchar(100) null;
go

alter table [intersect] add [Owner] varchar(100) null;
go


-- Remove the unused xml nullable column path from fusionattribute table, its not used anywhere and just makes the tables rows bigger
ALTER TABLE fusionattribute DROP COLUMN [path]
go


CREATE INDEX IX_MapRuleItem_SourceFusionAttributeID_TargetFusionAttributeID ON [dbo].[MapRuleItem] (SourceFusionAttributeID asc, TargetFusionAttributeID asc)
go

-- add id column to fusion schedule table
alter table fusionschedule add ID INT IDENTITY (1, 1) NOT NULL
go

-- drop the constraint
ALTER TABLE dbo.fusionschedule DROP CONSTRAINT PK_FusionSchedule
GO

-- add back constraint
ALTER TABLE dbo.fusionschedule ADD CONSTRAINT PK_FusionSchedule PRIMARY KEY CLUSTERED ([ID] ASC)
GO

-- add constraint
ALTER TABLE dbo.fusionschedule ADD CONSTRAINT Con_FusionScheduleUniqueFusionIDDayTime UNIQUE (FusionID,Day,Time)
go

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


-- add visible column to intersect table
alter table [dbo].[Intersect] add [Visible] bit not null default(1)
go

-- add index on visible column to intersect table
CREATE NONCLUSTERED INDEX [IX_Intersect_Visible] ON [dbo].[Intersect] ( Visible ASC );
go


-- add visible column to intersect table
alter table [dbo].[Intersect] add [Visible] bit not null default(1)
go

-- add index on visible column to intersect table
CREATE NONCLUSTERED INDEX [IX_Intersect_Visible] ON [dbo].[Intersect] ( Visible ASC );
go


-- add visible column to nym table
alter table [dbo].[Nym] add [Visible] bit not null default(1)
go

-- add index on visible column to nym table
CREATE NONCLUSTERED INDEX [IX_Nym_Visible] ON [dbo].[Nym] ( Visible ASC );
go