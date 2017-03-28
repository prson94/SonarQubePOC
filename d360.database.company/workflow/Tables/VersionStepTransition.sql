CREATE TABLE [workflow].[VersionStepTransition](
	[FromVersionStepID] [int] NOT NULL,
	[ToVersionStepID] [int] NOT NULL,
	[Name] [nvarchar](500) NOT NULL,
	[TransitionType] [int] NOT NULL,
	[Condition] [xml] NULL,
	[FromPortID] [varchar](10) NULL,
	[ToPortID] [varchar](10) NULL,
	[ID] [bigint] IDENTITY(1,1) NOT NULL,
 CONSTRAINT [PK_WorkflowVersionStepTransition_ID] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)
)

GO

ALTER TABLE [workflow].[VersionStepTransition]  WITH CHECK ADD  CONSTRAINT [FK_WorkflowVersionStepTransition_FromVersionStep] FOREIGN KEY([FromVersionStepID])
REFERENCES [workflow].[VersionStep] ([ID])
GO

ALTER TABLE [workflow].[VersionStepTransition] CHECK CONSTRAINT [FK_WorkflowVersionStepTransition_FromVersionStep]
GO

ALTER TABLE [workflow].[VersionStepTransition]  WITH CHECK ADD  CONSTRAINT [FK_WorkflowVersionStepTransition_ToVersionStep] FOREIGN KEY([ToVersionStepID])
REFERENCES [workflow].[VersionStep] ([ID])
GO

ALTER TABLE [workflow].[VersionStepTransition] CHECK CONSTRAINT [FK_WorkflowVersionStepTransition_ToVersionStep]
GO