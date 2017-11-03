CREATE TABLE [dbo].[LoadItem] (
    [LoadID]        INT            NOT NULL,
    [RowIndex]      INT            NOT NULL,
    [Object]        VARCHAR (50)   NULL,
    [ObjectID]      INT            NULL,
    [Status]        BIT            NULL,
    [StatusMessage] NVARCHAR (500) NULL,
    [KeyHash]       VARCHAR (250)  NULL,
    [FieldHash]     VARCHAR (250)  NULL,
    CONSTRAINT [PK_LoadItem] PRIMARY KEY CLUSTERED ([LoadID] ASC, [RowIndex] ASC),
    CONSTRAINT [FK_LoadItem_Load] FOREIGN KEY ([LoadID]) REFERENCES [dbo].[Load] ([ID]) ON DELETE CASCADE
);




GO
CREATE NONCLUSTERED INDEX [IX_LoadItem_Load]
    ON [dbo].[LoadItem]([LoadID] DESC);

