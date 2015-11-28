CREATE TABLE [dbo].[Map] (
    [ID]   INT            IDENTITY (1, 1) NOT NULL,
    [Name] NVARCHAR (250) NOT NULL,
    [Type] INT            DEFAULT ((1)) NOT NULL,
    CONSTRAINT [PK_Map] PRIMARY KEY CLUSTERED ([ID] ASC)
);