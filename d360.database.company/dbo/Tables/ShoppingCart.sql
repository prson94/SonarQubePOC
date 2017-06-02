CREATE TABLE [dbo].[ShoppingCart](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[ShoppingCartTypeID] [int] NOT NULL,
	[ResourceID] [int] NOT NULL,
	[CreatedOn] [datetime] NOT NULL,
	[RequestedOn] [datetime] NULL,
	[Request] [nvarchar](max) NULL,
 CONSTRAINT [PK_ShoppingCart] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)
)

GO

ALTER TABLE [dbo].[ShoppingCart] ADD  CONSTRAINT [DF_ShoppingCart_CreatedOn]  DEFAULT (getutcdate()) FOR [CreatedOn]
GO

ALTER TABLE [dbo].[ShoppingCart]  WITH CHECK ADD  CONSTRAINT [FK_ShoppingCart_ShoppingCartType] FOREIGN KEY([ShoppingCartTypeID])
REFERENCES [dbo].[ShoppingCartType] ([ID])
GO

ALTER TABLE [dbo].[ShoppingCart] CHECK CONSTRAINT [FK_ShoppingCart_ShoppingCartType]
GO


