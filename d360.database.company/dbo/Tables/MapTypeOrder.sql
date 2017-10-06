CREATE TABLE [dbo].[MapTypeOrder](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[MapTypeID] [int] NOT NULL,
	[IntersectTypeID] [int] NOT NULL,
	[Order] [int] NOT NULL,
 CONSTRAINT [PK_MapTypeObject] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)
)
GO

ALTER TABLE [dbo].[MapTypeOrder]  WITH NOCHECK ADD  CONSTRAINT [FK_MapTypeObject_MapType] FOREIGN KEY([MapTypeID])
REFERENCES [dbo].[MapType] ([ID])
GO

ALTER TABLE [dbo].[MapTypeOrder] CHECK CONSTRAINT [FK_MapTypeObject_MapType]
GO