CREATE TABLE [dbo].[Question] (
    [ID]       INT             IDENTITY (1, 1) NOT NULL,
    [SurveyID] INT             NOT NULL,
    [Comment]  NVARCHAR (4000) NULL,
    CONSTRAINT [PK_Question] PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [FK_Question_Survey] FOREIGN KEY ([SurveyID]) REFERENCES [dbo].[Survey] ([ID])
);




GO


