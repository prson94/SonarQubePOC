CREATE TABLE [workflow].[Type](
	ID int IDENTITY NOT NULL,
	Name nvarchar(500) NOT NULL,
	TriggerEvent int NOT NULL,
	Object varchar(50) NOT NULL,
	ObjectID int NOT NULL,
	CreatedBy int NOT NULL,
	CreatedOn datetime NOT NULL,
	UpdatedBy int NOT NULL,
	UpdatedOn datetime NOT NULL,
	CONSTRAINT [PK_WorkflowType] PRIMARY KEY CLUSTERED ( [ID] ASC )
)
GO

CREATE TABLE [workflow].[Version](
	ID int IDENTITY NOT NULL,
	TypeID int NOT NULL,
	CreatedBy int NOT NULL,
	CreatedOn datetime NOT NULL,
	UpdatedBy int NOT NULL,
	UpdatedOn datetime NOT NULL,
	CONSTRAINT [PK_WorkflowVersion] PRIMARY KEY CLUSTERED ( [ID] ASC )
)
GO

ALTER TABLE [workflow].[Version]  WITH CHECK ADD  CONSTRAINT [FK_WorkflowVersion_WorkflowType] FOREIGN KEY([TypeID]) REFERENCES [workflow].[Type] ([ID]) ON DELETE CASCADE
GO

ALTER TABLE [workflow].[Version] CHECK CONSTRAINT [FK_WorkflowVersion_WorkflowType]
GO

CREATE TABLE [workflow].[VersionStep](
	ID int IDENTITY NOT NULL,
	ParentID int NULL,
	VersionID int NOT NULL,
	Name nvarchar(500) NOT NULL,
	Condition xml null,
	Settings xml null,
	[Type] int NOT NULL,
	CONSTRAINT [PK_WorkflowVersionStep] PRIMARY KEY CLUSTERED ( [ID] ASC )
)
GO

ALTER TABLE [workflow].[VersionStep]  WITH CHECK ADD  CONSTRAINT [FK_WorkflowVersionStep_WorkflowVersion] FOREIGN KEY([VersionID]) REFERENCES [workflow].[Version] ([ID]) ON DELETE CASCADE
GO

ALTER TABLE [workflow].[VersionStep] CHECK CONSTRAINT [FK_WorkflowVersionStep_WorkflowVersion]
GO

ALTER TABLE [workflow].[VersionStep]  WITH CHECK ADD  CONSTRAINT [FK_WorkflowVersionStep_Parent] FOREIGN KEY([ParentID]) REFERENCES [workflow].[VersionStep] ([ID]) ON DELETE NO ACTION
GO

ALTER TABLE [workflow].[VersionStep] CHECK CONSTRAINT [FK_WorkflowVersionStep_Parent]
GO


CREATE TABLE [workflow].[Item](
	ID bigint IDENTITY NOT NULL,
	VersionID int NOT NULL,
	CreatedBy int NOT NULL,
	CreatedOn datetime NOT NULL,
	UpdatedBy int NOT NULL,
	UpdatedOn datetime NOT NULL,
	CONSTRAINT [PK_WorkflowItem] PRIMARY KEY CLUSTERED ( [ID] ASC )
)
GO

ALTER TABLE [workflow].[Item]  WITH CHECK ADD  CONSTRAINT [FK_WorkflowItem_WorkflowVersion] FOREIGN KEY([VersionID]) REFERENCES [workflow].[Version] ([ID]) ON DELETE CASCADE
GO

ALTER TABLE [workflow].[Item] CHECK CONSTRAINT [FK_WorkflowItem_WorkflowVersion]
GO

CREATE TABLE [workflow].[ItemStep](
	ID bigint IDENTITY NOT NULL,
	ItemID bigint NOT NULL,
	StepID int NOT NULL,
	Settings xml null,
	CONSTRAINT [PK_WorkflowItemStep] PRIMARY KEY CLUSTERED ( [ID] ASC )
)
GO

ALTER TABLE [workflow].[ItemStep]  WITH CHECK ADD  CONSTRAINT [FK_WorkflowItemStep_WorkflowItem] FOREIGN KEY(ItemID) REFERENCES [workflow].[Item] ([ID]) ON DELETE NO ACTION
GO

ALTER TABLE [workflow].[ItemStep] CHECK CONSTRAINT [FK_WorkflowItemStep_WorkflowItem]
GO

ALTER TABLE [workflow].[ItemStep]  WITH CHECK ADD  CONSTRAINT [FK_WorkflowItemStep_WorkflowVersionStep] FOREIGN KEY([StepID]) REFERENCES [workflow].[VersionStep] ([ID]) ON DELETE CASCADE
GO

ALTER TABLE [workflow].[ItemStep] CHECK CONSTRAINT [FK_WorkflowItemStep_WorkflowVersionStep]
GO


--change size of decimal to support values of 1 on rule threshold
ALTER TABLE [dbo].[rule] ALTER COLUMN [Threshold] decimal(4,3)

-- add filename column to the report table
alter table report add [FileName] varchar(260) null