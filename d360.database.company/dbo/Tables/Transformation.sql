CREATE TABLE [dbo].[Transformation] (
    [ID]                 INT            IDENTITY (1, 1) NOT NULL,
    [TransformationType] INT            NOT NULL,
    [Body]               NVARCHAR (MAX) NOT NULL,
    CONSTRAINT [PK_Transformation] PRIMARY KEY CLUSTERED ([ID] ASC)
);

