CREATE TABLE [dbo].[IntersectMapExclusion](
	[IntersectMapIDToExclude] [int] NOT NULL,
	[MapID] [int] NOT NULL,
 CONSTRAINT [PK_IntersectMapExclusion] PRIMARY KEY CLUSTERED 
(
	[MapID] ASC,
	[IntersectMapIDToExclude] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)
)

GO

ALTER TABLE [dbo].[IntersectMapExclusion]  WITH CHECK ADD  CONSTRAINT [FK_IntersectMapExclusion_IntersectMap_ToExclude] FOREIGN KEY([IntersectMapIDToExclude])
REFERENCES [dbo].[IntersectMap] ([ID])
GO

ALTER TABLE [dbo].[IntersectMapExclusion] CHECK CONSTRAINT [FK_IntersectMapExclusion_IntersectMap_ToExclude]
GO

ALTER TABLE [dbo].[IntersectMapExclusion]  WITH CHECK ADD  CONSTRAINT [FK_IntersectMapExclusion_MapID] FOREIGN KEY([MapID])
REFERENCES [dbo].[Map] ([ID])
GO

ALTER TABLE [dbo].[IntersectMapExclusion] CHECK CONSTRAINT [FK_IntersectMapExclusion_MapID]
GO