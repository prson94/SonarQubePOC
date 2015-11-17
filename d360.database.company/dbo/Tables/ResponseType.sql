CREATE TABLE [dbo].[ResponseType] (
    [ID]                 INT            IDENTITY (50000, 1) NOT NULL,
    [Name]               NVARCHAR (250) NOT NULL,
    [AllowOptions]       BIT            NOT NULL,
    [AllowValueOverride] BIT            NOT NULL,
    CONSTRAINT [PK_ResponseType] PRIMARY KEY CLUSTERED ([ID] ASC)
);

