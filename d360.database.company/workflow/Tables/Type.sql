CREATE TABLE [workflow].[Type](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](500) NOT NULL,
	[CreatedBy] [int] NOT NULL,
	[CreatedOn] [datetime] NOT NULL,
	[UpdatedBy] [int] NOT NULL,
	[UpdatedOn] [datetime] NOT NULL,
	[PublishedVersionID] [int] NULL,
	[Deleted] [bit] NOT NULL,
 CONSTRAINT [PK_WorkflowType] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)
)

GO

ALTER TABLE [workflow].[Type] ADD  CONSTRAINT [DF_WorkflowType_Deleted]  DEFAULT ((0)) FOR [Deleted]
GO

ALTER TABLE [workflow].[Type]  WITH CHECK ADD  CONSTRAINT [FK_WorkflowType_WorkflowVersion] FOREIGN KEY([PublishedVersionID])
REFERENCES [workflow].[Version] ([ID])
GO

ALTER TABLE [workflow].[Type] CHECK CONSTRAINT [FK_WorkflowType_WorkflowVersion]
GO