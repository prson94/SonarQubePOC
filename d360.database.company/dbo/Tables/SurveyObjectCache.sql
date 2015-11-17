CREATE TABLE [dbo].[SurveyObjectCache] (
    [SurveyTypeID] INT          NOT NULL,
    [ObjectType]   VARCHAR (25) NOT NULL,
    [ObjectID]     INT          NOT NULL,
    [ReportCache]  AS           ([dbo].[SurveyReportGeneratorWrapper]([SurveyTypeID],[ObjectType],[ObjectID])),
    CONSTRAINT [PK_SurveyObjectCache] PRIMARY KEY CLUSTERED ([SurveyTypeID] ASC, [ObjectType] ASC, [ObjectID] ASC),
    CONSTRAINT [FK_SurveyObjectCache_SurveyType] FOREIGN KEY ([SurveyTypeID]) REFERENCES [dbo].[SurveyType] ([ID]) ON DELETE CASCADE
);

