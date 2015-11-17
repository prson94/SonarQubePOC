CREATE TABLE [dbo].[ResponsibilityContextItem] (
    [ResponsibilityID] INT          NOT NULL,
    [ObjectType]       VARCHAR (25) NOT NULL,
    [ObjectID]         INT          NOT NULL,
    CONSTRAINT [PK_ResponsibilityContextItem] PRIMARY KEY CLUSTERED ([ResponsibilityID] ASC, [ObjectType] ASC, [ObjectID] ASC),
    CONSTRAINT [FK_ResponsibilityContextItem_Responsibility] FOREIGN KEY ([ResponsibilityID]) REFERENCES [dbo].[Responsibility] ([ID]) ON DELETE CASCADE
);

