CREATE TABLE [dbo].[Score] (
    [ID]          BIGINT       IDENTITY (1, 1) NOT NULL,
    [Object]      VARCHAR (50) NOT NULL,
    [ObjectID]    INT          NOT NULL,
    [ScoreTypeID] INT          NOT NULL,
    [Date]        DATE         NOT NULL,
    [Value]       INT          NOT NULL,
    CONSTRAINT [PK_Score] PRIMARY KEY NONCLUSTERED ([ID] ASC),
    CONSTRAINT [CK_Score_Value] CHECK ([Value]>=(0) AND [Value]<=(100)),
    CONSTRAINT [FK_Score_ScoreType] FOREIGN KEY ([ScoreTypeID]) REFERENCES [dbo].[ScoreType] ([ID])
);


GO
CREATE CLUSTERED INDEX [CIX_Score]
    ON [dbo].[Score]([Object] ASC, [ObjectID] ASC, [ScoreTypeID] ASC, [Date] DESC);

