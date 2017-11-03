CREATE TABLE [dbo].[CommentVote] (
    [ID]         INT IDENTITY (1, 1) NOT NULL,
    [CommentID]  INT NOT NULL,
    [ResourceID] INT NOT NULL,
    [Vote]       INT DEFAULT ((0)) NOT NULL,
    CONSTRAINT [PK_CommentVote] PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [FK_CommentVote_Comment] FOREIGN KEY ([CommentID]) REFERENCES [dbo].[Comment] ([ID])
);



