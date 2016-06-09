CREATE TABLE [dbo].[MapSequence] (
    [ID]          INT             IDENTITY (1, 1) NOT NULL,
    [MapID]       INT             NOT NULL,
    [Sequence]    INT             NOT NULL,
    [Description] NVARCHAR (4000) NULL,
    [CreatedBy]   INT             NOT NULL,
    [CreatedOn]   DATETIME        NOT NULL,
    [UpdatedBy]   INT             NOT NULL,
    [UpdatedOn]   DATETIME        NOT NULL,
    CONSTRAINT [PK_MapSequence] PRIMARY KEY NONCLUSTERED ([ID] ASC),
    CONSTRAINT [FK_MapSequence_Map] FOREIGN KEY ([MapID]) REFERENCES [dbo].[Map] ([ID]) ON DELETE CASCADE
);

