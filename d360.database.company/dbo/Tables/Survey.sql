CREATE TABLE [dbo].[Survey] (
    [ID]           INT          IDENTITY (1, 1) NOT NULL,
    [SurveyTypeID] INT          NOT NULL,
    [Object]       VARCHAR (50) NOT NULL,
    [ObjectID]     INT          NOT NULL,
    [ResourceID]   INT          NOT NULL,
    [CreatedOn]    DATETIME     NOT NULL,
    CONSTRAINT [PK_Survey] PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [FK_Survey_SurveyType] FOREIGN KEY ([SurveyTypeID]) REFERENCES [dbo].[SurveyType] ([ID]) ON DELETE CASCADE
);






GO
CREATE NONCLUSTERED INDEX [IX_Survey_ObjectType-ObjectID]
    ON [dbo].[Survey]([Object] ASC, [ObjectID] ASC);




GO
