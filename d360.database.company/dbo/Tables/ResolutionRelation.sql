CREATE TABLE [dbo].[ResolutionRelation] (
    [ResolutionID] INT          NOT NULL,
    [ObjectType]   VARCHAR (50) NOT NULL,
    [ObjectID]     INT          NOT NULL,
    CONSTRAINT [PK_ResolutionRelation] PRIMARY KEY CLUSTERED ([ResolutionID] ASC, [ObjectType] ASC, [ObjectID] ASC),
    CONSTRAINT [FK_ResolutionRelation_Resolution] FOREIGN KEY ([ResolutionID]) REFERENCES [dbo].[Resolution] ([ID]) ON DELETE CASCADE
);

