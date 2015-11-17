CREATE TABLE [dbo].[TagRelation] (
    [ID]        INT          IDENTITY (1, 1) NOT NULL,
    [TagID]     INT          NOT NULL,
    [Object]    VARCHAR (50) NOT NULL,
    [ObjectID]  INT          NOT NULL,
    [UpdatedOn] DATETIME     NULL,
    [UpdatedBy] INT          NULL,
    CONSTRAINT [PK_TagRelation] PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [FK_TagRelation_Tag] FOREIGN KEY ([TagID]) REFERENCES [dbo].[Tag] ([ID]) ON DELETE CASCADE
);


GO
CREATE NONCLUSTERED INDEX [IX_TagRelation_Object]
    ON [dbo].[TagRelation]([Object] ASC, [ObjectID] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_TagRelation_Tag]
    ON [dbo].[TagRelation]([TagID] ASC);

