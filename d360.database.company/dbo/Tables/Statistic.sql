CREATE TABLE [dbo].[Statistic] (
    [StatisticTypeID] INT          NOT NULL,
    [ObjectType]      VARCHAR (50) NOT NULL,
    [ObjectID]        INT          NOT NULL,
    [DateStart]       DATETIME     CONSTRAINT [DF_Statistic_DateStart] DEFAULT (getutcdate()) NOT NULL,
    [DateEnd]         DATETIME     CONSTRAINT [DF_Statistic_DateEnd] DEFAULT (getutcdate()) NOT NULL,
    [Score]           INT          NULL,
    CONSTRAINT [PK_Statistic] PRIMARY KEY CLUSTERED ([StatisticTypeID] ASC, [ObjectType] ASC, [ObjectID] ASC, [DateStart] ASC, [DateEnd] ASC),
    CONSTRAINT [FK_Statistic_StatisticType] FOREIGN KEY ([StatisticTypeID]) REFERENCES [dbo].[StatisticType] ([ID]) ON DELETE CASCADE
);

