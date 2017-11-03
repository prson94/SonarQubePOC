CREATE TABLE [dbo].[ScoreTypeMetricVersion] (
    [ID]                INT             IDENTITY (1, 1) NOT NULL,
    [ScoreTypeMetricID] INT             NOT NULL,
    [Name]              NVARCHAR (250)  NOT NULL,
    [Description]       NVARCHAR (4000) NULL,
    [CheckType]         INT             NOT NULL,
    [Configuration]     XML             NULL,
    [CreatedOn]         DATETIME        NULL,
    [CreatedBy]         INT             NULL,
    [UpdatedOn]         DATETIME        NULL,
    [UpdatedBy]         INT             NULL,
    [MaximumScore]      INT             NOT NULL,
    [Weight]            DECIMAL (3, 2)  NULL,
    CONSTRAINT [PK_ScoreTypeMetricVersion] PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [CK_ScoreTypeMetricVersion_MaximumScore] CHECK ([MaximumScore]>=(0) AND [MaximumScore]<=(999)),
    CONSTRAINT [FK_ScoreTypeMetricVersion_ScoreTypeMetric] FOREIGN KEY ([ScoreTypeMetricID]) REFERENCES [dbo].[ScoreTypeMetric] ([ID])
);

