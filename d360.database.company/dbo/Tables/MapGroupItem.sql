CREATE TABLE [dbo].[MapGroupItem](
	[MapGroupID] [int] NOT NULL,
	[Object] [varchar](50) NULL,
	[ObjectID] [int] NULL,
	[ID] [int] IDENTITY(1,1) NOT NULL,
 CONSTRAINT [PK_MapGroupItem] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)
)
GO

ALTER TABLE [dbo].[MapGroupItem]  WITH CHECK ADD  CONSTRAINT [FK_MapGroupItem_MapGroup] FOREIGN KEY([MapGroupID])
REFERENCES [dbo].[MapGroup] ([ID])
GO

ALTER TABLE [dbo].[MapGroupItem] CHECK CONSTRAINT [FK_MapGroupItem_MapGroup]
GO


