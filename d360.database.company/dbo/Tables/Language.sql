CREATE TABLE [dbo].[Language] (
    [ID]      INT            IDENTITY (1, 1) NOT NULL,
    [Name]    NVARCHAR (250) NOT NULL,
    [Alpha2]  VARCHAR (2)    NOT NULL,
    [Alpha3b] VARCHAR (3)    NOT NULL
);

