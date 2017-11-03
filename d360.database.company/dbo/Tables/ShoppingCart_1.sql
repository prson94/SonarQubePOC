CREATE TABLE [dbo].[ShoppingCart] (
    [ID]                 INT            IDENTITY (1, 1) NOT NULL,
    [ShoppingCartTypeID] INT            NOT NULL,
    [ResourceID]         INT            NOT NULL,
    [CreatedOn]          DATETIME       CONSTRAINT [DF_ShoppingCart_CreatedOn] DEFAULT (getutcdate()) NOT NULL,
    [RequestedOn]        DATETIME       NULL,
    [Request]            NVARCHAR (MAX) NULL,
    CONSTRAINT [PK_ShoppingCart] PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [FK_ShoppingCart_ShoppingCartType] FOREIGN KEY ([ShoppingCartTypeID]) REFERENCES [dbo].[ShoppingCartType] ([ID])
);

