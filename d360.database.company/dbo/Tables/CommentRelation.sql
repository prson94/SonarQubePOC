CREATE TABLE [dbo].[CommentRelation] (
    [CommentID]  INT          NOT NULL,
    [ObjectType] VARCHAR (50) NOT NULL,
    [ObjectID]   INT          NOT NULL,
    [Date]       DATETIME     CONSTRAINT [DF_CommentRelation_Date] DEFAULT (getutcdate()) NOT NULL,
    CONSTRAINT [PK_CommentRelation] PRIMARY KEY CLUSTERED ([CommentID] ASC, [ObjectType] ASC, [ObjectID] ASC),
    CONSTRAINT [FK_CommentRelation_Comment] FOREIGN KEY ([CommentID]) REFERENCES [dbo].[Comment] ([ID]) ON DELETE CASCADE
);


GO
CREATE NONCLUSTERED INDEX [IX_CommentRelation_ObjectType-ObjectID]
    ON [dbo].[CommentRelation]([ObjectType] ASC, [ObjectID] ASC);

