CREATE TABLE [dbo].[LoadColumn] (
    [LoadID]      INT            NOT NULL,
    [ColumnIndex] INT            NOT NULL,
    [Name]        NVARCHAR (250) NOT NULL,
    [IsDynamic]   BIT            CONSTRAINT [DF_LoadColumn_IsDynamic] DEFAULT ((0)) NOT NULL,
    CONSTRAINT [PK_LoadColumn] PRIMARY KEY CLUSTERED ([LoadID] ASC, [ColumnIndex] ASC),
    CONSTRAINT [FK_LoadColumn_Load] FOREIGN KEY ([LoadID]) REFERENCES [dbo].[Load] ([ID]) ON DELETE CASCADE
);




GO
CREATE NONCLUSTERED INDEX [IX_LoadColumn_Load]
    ON [dbo].[LoadColumn]([LoadID] DESC);

