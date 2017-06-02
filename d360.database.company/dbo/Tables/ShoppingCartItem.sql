CREATE TABLE [dbo].[ShoppingCartItem](
	[ShoppingCartID] [int] NOT NULL,
	[Object] [varchar](250) NOT NULL,
	[ObjectID] [int] NOT NULL,
	[AddedOn] [datetime] NOT NULL,
 CONSTRAINT [PK_ShoppingCartItem] PRIMARY KEY CLUSTERED 
(
	[ShoppingCartID] ASC,
	[Object] ASC,
	[ObjectID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)
)

GO

ALTER TABLE [dbo].[ShoppingCartItem] ADD  CONSTRAINT [DF_ShoppingCartItem_AddedOn]  DEFAULT (getutcdate()) FOR [AddedOn]
GO

ALTER TABLE [dbo].[ShoppingCartItem]  WITH CHECK ADD  CONSTRAINT [FK_ShoppingCartItem_ShoppingCart] FOREIGN KEY([ShoppingCartID])
REFERENCES [dbo].[ShoppingCart] ([ID])
GO

ALTER TABLE [dbo].[ShoppingCartItem] CHECK CONSTRAINT [FK_ShoppingCartItem_ShoppingCart]
GO


