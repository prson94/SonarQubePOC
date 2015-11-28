CREATE TABLE [dbo].[MapTransformation] (
    [MapID]              INT            NOT NULL,
    [TransformationType] INT            NOT NULL,
    [Body]               NVARCHAR (MAX) NOT NULL,
    CONSTRAINT [PK_MapTransformation] PRIMARY KEY CLUSTERED ([MapID] ASC, [TransformationType] ASC),
    CONSTRAINT [FK_MapTransformation_Map] FOREIGN KEY ([MapID]) REFERENCES [dbo].[Map] ([ID])
);
GO

CREATE NONCLUSTERED INDEX [IX_MapTransformation_Map]
    ON [dbo].[MapTransformation]([MapID] ASC);
GO