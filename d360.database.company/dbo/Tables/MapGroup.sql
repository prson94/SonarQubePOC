CREATE TABLE [dbo].[MapGroup](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[MapID] [int] NOT NULL,
	[BusinessTransformation] [nvarchar](max) NULL,
	[TechnicalTransformation] [nvarchar](max) NULL,
 CONSTRAINT [PK_MapGroup] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)
)
GO

ALTER TABLE [dbo].[MapGroup]  WITH CHECK ADD  CONSTRAINT [FK_MapGroup_Map] FOREIGN KEY([MapID])
REFERENCES [dbo].[Map] ([ID])
GO

ALTER TABLE [dbo].[MapGroup] CHECK CONSTRAINT [FK_MapGroup_Map]
GO
