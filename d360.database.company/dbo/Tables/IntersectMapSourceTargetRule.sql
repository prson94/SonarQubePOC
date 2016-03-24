CREATE TABLE [dbo].[IntersectMapSourceTargetRule](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[RuleID] [int] NOT NULL,
	[IntersectMapID] [int] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)
)

GO

ALTER TABLE [dbo].[IntersectMapSourceTargetRule]  WITH CHECK ADD  CONSTRAINT [FK_IntersectMap_ID] FOREIGN KEY([IntersectMapID])
REFERENCES [dbo].[IntersectMap] ([ID])
GO

ALTER TABLE [dbo].[IntersectMapSourceTargetRule] CHECK CONSTRAINT [FK_IntersectMap_ID]
GO

ALTER TABLE [dbo].[IntersectMapSourceTargetRule]  WITH CHECK ADD  CONSTRAINT [FK_SourceTargetRule_ID] FOREIGN KEY([RuleID])
REFERENCES [dbo].[SourceTargetRule] ([ID])
GO

ALTER TABLE [dbo].[IntersectMapSourceTargetRule] CHECK CONSTRAINT [FK_SourceTargetRule_ID]
GO


