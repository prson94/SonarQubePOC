CREATE TABLE [dbo].[ScoreTypeMetric] (
    [ID]            INT             IDENTITY (1, 1) NOT NULL,
    [ScoreTypeID]   INT             NOT NULL,
    [Object]        VARCHAR (50)    NULL,
    [ObjectID]      INT             NULL,
    [Name]          NVARCHAR (250)  NOT NULL,
    [Description]   NVARCHAR (4000) NULL,
    [CheckType]     INT             NOT NULL,
    [Configuration] XML             NULL,
    [CreatedOn]     DATETIME        NULL,
    [CreatedBy]     INT             NULL,
    [UpdatedOn]     DATETIME        NULL,
    [UpdatedBy]     INT             NULL,
    [MaximumScore]  INT             NOT NULL,
    [Deleted]       BIT             CONSTRAINT [DF_ScoreTypeMetric_Deleted] DEFAULT ((0)) NOT NULL,
    [Weight]        DECIMAL (3, 2)  NULL,
    CONSTRAINT [PK_ScoreTypeMetric] PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [CK_ScoreTypeMetric_MaximumScore] CHECK ([MaximumScore]>=(0) AND [MaximumScore]<=(999)),
    CONSTRAINT [FK_ScoreTypeMetric_ScoreType] FOREIGN KEY ([ScoreTypeID]) REFERENCES [dbo].[ScoreType] ([ID])
);

