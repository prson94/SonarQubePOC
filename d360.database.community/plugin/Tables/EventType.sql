CREATE TABLE [plugin].[EventType] (
    [ID]                    INT             NOT NULL,
    [ParentID]              INT             NULL,
    [Name]                  NVARCHAR (250)  NOT NULL,
    [Description]           NVARCHAR (4000) NULL,
    [MarkAsResolvedOnSynch] BIT             NULL,
    CONSTRAINT [PK_Plugin_EventType] PRIMARY KEY CLUSTERED ([ID] ASC)
);

