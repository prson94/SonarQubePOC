CREATE TABLE [dbo].[ShoppingCartItem] (
    [ShoppingCartID] INT           NOT NULL,
    [Object]         VARCHAR (250) NOT NULL,
    [ObjectID]       INT           NOT NULL,
    [AddedOn]        DATETIME      CONSTRAINT [DF_ShoppingCartItem_AddedOn] DEFAULT (getutcdate()) NOT NULL,
    CONSTRAINT [PK_ShoppingCartItem] PRIMARY KEY CLUSTERED ([ShoppingCartID] ASC, [Object] ASC, [ObjectID] ASC),
    CONSTRAINT [FK_ShoppingCartItem_ShoppingCart] FOREIGN KEY ([ShoppingCartID]) REFERENCES [dbo].[ShoppingCart] ([ID])
);

