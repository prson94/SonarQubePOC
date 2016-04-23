CREATE TABLE [dbo].[IntersectMapSourceRule] (
    [ID]             INT             IDENTITY (1, 1) NOT NULL,
    [IntersectMapID] INT             NOT NULL,
    [SourceRuleID]   INT             NOT NULL,
    [Description]    NVARCHAR (4000) NOT NULL,
    [SortOrder]      INT             DEFAULT ((1)) NOT NULL,
    CONSTRAINT [PK_IntersectMapSourceRule] PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [FK_IntersectMapSourceRule_IntersectMap] FOREIGN KEY ([IntersectMapID]) REFERENCES [dbo].[IntersectMap] ([ID]) ON DELETE CASCADE,
    CONSTRAINT [FK_IntersectMapSourceRule_SourceRule] FOREIGN KEY ([SourceRuleID]) REFERENCES [dbo].[SourceRule] ([ID]) ON DELETE CASCADE
);




GO
CREATE NONCLUSTERED INDEX [IX_IntersectMapSourceRule_SourceRule]
    ON [dbo].[IntersectMapSourceRule]([SourceRuleID] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_IntersectMapSourceRule_IntersectMap]
    ON [dbo].[IntersectMapSourceRule]([IntersectMapID] ASC);

