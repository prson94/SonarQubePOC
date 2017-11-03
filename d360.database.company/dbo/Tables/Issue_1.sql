CREATE TABLE [dbo].[Issue] (
    [ID]           INT          IDENTITY (1, 1) NOT NULL,
    [IssueTypeID]  INT          NOT NULL,
    [Object]       VARCHAR (50) NOT NULL,
    [ObjectID]     INT          NOT NULL,
    [ObjectType]   VARCHAR (25) NOT NULL,
    [ObjectTypeID] INT          NOT NULL,
    [CreatedOn]    DATETIME     NOT NULL,
    [CreatedBy]    INT          NOT NULL,
    [UpdatedOn]    DATETIME     DEFAULT (getutcdate()) NOT NULL,
    [UpdatedBy]    INT          NULL,
    [Criticality]  INT          DEFAULT ((0)) NOT NULL,
    [CommentID]    INT          NULL,
    CONSTRAINT [PK_Issue] PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [FK_Issue_IssueType] FOREIGN KEY ([IssueTypeID]) REFERENCES [dbo].[IssueType] ([ID]) ON DELETE CASCADE
);



