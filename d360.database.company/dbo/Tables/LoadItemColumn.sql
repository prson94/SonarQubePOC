CREATE TABLE [dbo].[LoadItemColumn] (
    [LoadID]         INT             NOT NULL,
    [RowIndex]       INT             NOT NULL,
    [ColumnIndex]    INT             NOT NULL,
    [Value]          NVARCHAR (MAX)  NULL,
    [LookupObject]   VARCHAR (50)    NULL,
    [LookupObjectID] INT             NULL,
    [LookupValue]    NVARCHAR (2000) NULL,
    [Success]        BIT             NULL,
    CONSTRAINT [PK_LoadItemColumn] PRIMARY KEY CLUSTERED ([LoadID] ASC, [RowIndex] ASC, [ColumnIndex] ASC),
    CONSTRAINT [FK_LoadItemColumn_LoadColumn] FOREIGN KEY ([LoadID], [ColumnIndex]) REFERENCES [dbo].[LoadColumn] ([LoadID], [ColumnIndex]),
    CONSTRAINT [FK_LoadItemColumn_LoadItem] FOREIGN KEY ([LoadID], [RowIndex]) REFERENCES [dbo].[LoadItem] ([LoadID], [RowIndex]) ON DELETE CASCADE
);






GO
CREATE NONCLUSTERED INDEX [IX_LoadItemColumn_Load_ColumnIndex_LookupObjectID]
    ON [dbo].[LoadItemColumn]([LoadID] DESC, [ColumnIndex] ASC, [LookupObjectID] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_LoadItemColumn_Load_ColumnIndex]
    ON [dbo].[LoadItemColumn]([LoadID] DESC, [ColumnIndex] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_LoadItemColumn_Load_ColumnIndex_Value]
    ON [dbo].[LoadItemColumn]([LoadID] DESC, [ColumnIndex] ASC, [LookupValue] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_LoadItemColumn_Load]
    ON [dbo].[LoadItemColumn]([LoadID] DESC);

