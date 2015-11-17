CREATE TABLE [dbo].[Question] (
    [ID]                   INT             IDENTITY (1, 1) NOT NULL,
    [SurveyID]             INT             NOT NULL,
    [QuestionTypeID]       INT             NOT NULL,
    [ResponseTypeOptionID] INT             NULL,
    [ResponseValue]        NVARCHAR (250)  NULL,
    [Comment]              NVARCHAR (4000) NULL,
    CONSTRAINT [PK_Question] PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [FK_Question_QuestionType] FOREIGN KEY ([QuestionTypeID]) REFERENCES [dbo].[QuestionType] ([ID]),
    CONSTRAINT [FK_Question_ResponseTypeOption] FOREIGN KEY ([ResponseTypeOptionID]) REFERENCES [dbo].[ResponseTypeOption] ([ID]) ON DELETE CASCADE,
    CONSTRAINT [FK_Question_Survey] FOREIGN KEY ([SurveyID]) REFERENCES [dbo].[Survey] ([ID]) ON DELETE CASCADE
);


GO
CREATE NONCLUSTERED INDEX [IX_Question_SurveyID]
    ON [dbo].[Question]([SurveyID] ASC);

