CREATE TABLE [dbo].[MapTypeTemplateItem](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[MapTypeTemplateID] [int] NOT NULL,
	[IntersectTypeID] [int] NOT NULL,
	[IsRequired] [bit] NOT NULL,
 CONSTRAINT [PK_MapTypeTemplateItem] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)
)
GO

ALTER TABLE [dbo].[MapTypeTemplateItem] ADD  CONSTRAINT [DF_MapTypeTemplateItem_IsRequired]  DEFAULT ((0)) FOR [IsRequired]
GO

ALTER TABLE [dbo].[MapTypeTemplateItem]  WITH CHECK ADD  CONSTRAINT [FK_MapTypeTemplateItem_MapTypeTemplateID] FOREIGN KEY([MapTypeTemplateID])
REFERENCES [dbo].[MapTypeTemplate] ([ID])
GO

ALTER TABLE [dbo].[MapTypeTemplateItem] CHECK CONSTRAINT [FK_MapTypeTemplateItem_MapTypeTemplateID]
GO


