CREATE TABLE [dbo].[CommentVote] (
    [ID]         INT IDENTITY (1, 1) NOT NULL,
    [CommentID]  INT NOT NULL,
    [ResourceID] INT NOT NULL,
    [Vote]       INT DEFAULT ((0)) NOT NULL,
    PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [FK_Comment_ID] FOREIGN KEY ([CommentID]) REFERENCES [dbo].[Comment] ([ID])
);

