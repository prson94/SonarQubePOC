CREATE TABLE [dbo].[ShoppingCartType] (
    [ID]   INT           IDENTITY (1, 1) NOT NULL,
    [Name] VARCHAR (250) NULL,
    CONSTRAINT [PK_ShoppingCartType] PRIMARY KEY CLUSTERED ([ID] ASC)
);

