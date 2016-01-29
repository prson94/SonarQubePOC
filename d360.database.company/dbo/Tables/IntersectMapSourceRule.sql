CREATE TABLE [dbo].[IntersectMapSourceRule] (
    [ID]             INT             IDENTITY (1, 1) NOT NULL,
    [IntersectMapID] INT             NOT NULL,
    [SourceRuleID]   INT             NOT NULL,
    [Description]    NVARCHAR (4000) NOT NULL,
    [SortOrder]      INT             DEFAULT ((1)) NOT NULL,
    CONSTRAINT [PK_IntersectMapSourceRule] PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [FK_IntersectMapSourceRule_IntersectMap] FOREIGN KEY ([IntersectMapID]) REFERENCES [dbo].[IntersectMap] ([ID]),
    CONSTRAINT [FK_IntersectMapSourceRule_SourceRule] FOREIGN KEY ([SourceRuleID]) REFERENCES [dbo].[SourceRule] ([ID])
);

