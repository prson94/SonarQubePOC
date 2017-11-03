CREATE TABLE [dbo].[MapGroupItem] (
    [MapGroupID] INT          NOT NULL,
    [Object]     VARCHAR (50) NULL,
    [ObjectID]   INT          NULL,
    [ID]         INT          IDENTITY (1, 1) NOT NULL,
    CONSTRAINT [PK_MapGroupItem] PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [FK_MapGroupItem_MapGroup] FOREIGN KEY ([MapGroupID]) REFERENCES [dbo].[MapGroup] ([ID])
);


GO


GO


GO
CREATE NONCLUSTERED INDEX [IX_MapGroupItem_MapGroupID]
    ON [dbo].[MapGroupItem]([MapGroupID] ASC);

