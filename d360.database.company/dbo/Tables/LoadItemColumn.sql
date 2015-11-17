CREATE TABLE [dbo].[LoadItemColumn] (
    [LoadID]         INT            NOT NULL,
    [RowIndex]       INT            NOT NULL,
    [ColumnIndex]    INT            NOT NULL,
    [Value]          NVARCHAR (MAX) NULL,
    [LookupObject]   VARCHAR (50)   NULL,
    [LookupObjectID] INT            NULL,
    CONSTRAINT [PK_LoadItemColumn] PRIMARY KEY CLUSTERED ([LoadID] ASC, [RowIndex] ASC, [ColumnIndex] ASC),
    CONSTRAINT [FK_LoadItemColumn_LoadColumn] FOREIGN KEY ([LoadID], [ColumnIndex]) REFERENCES [dbo].[LoadColumn] ([LoadID], [ColumnIndex]),
    CONSTRAINT [FK_LoadItemColumn_LoadItem] FOREIGN KEY ([LoadID], [RowIndex]) REFERENCES [dbo].[LoadItem] ([LoadID], [RowIndex]) ON DELETE CASCADE
);

