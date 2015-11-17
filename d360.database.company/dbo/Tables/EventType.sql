CREATE TABLE [dbo].[EventType] (
    [ID]                    INT             IDENTITY (50000, 1) NOT NULL,
    [ParentID]              INT             NULL,
    [Name]                  NVARCHAR (250)  NOT NULL,
    [Description]           NVARCHAR (4000) NULL,
    [MarkAsResolvedOnSynch] BIT             NOT NULL,
    CONSTRAINT [PK_EventType] PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [FK_EventType_Parent] FOREIGN KEY ([ParentID]) REFERENCES [dbo].[EventType] ([ID])
);


GO
CREATE NONCLUSTERED INDEX [IX_EventType_ParentID]
    ON [dbo].[EventType]([ParentID] ASC);

