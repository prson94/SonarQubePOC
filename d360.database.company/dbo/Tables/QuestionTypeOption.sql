CREATE TABLE [dbo].[QuestionTypeOption] (
    [ID]             INT            IDENTITY (1, 1) NOT NULL,
    [QuestionTypeID] INT            NOT NULL,
    [Name]           NVARCHAR (500) NOT NULL,
    [Value]          INT            NOT NULL,
    CONSTRAINT [PK_QuestionTypeOption] PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [FK_QuestionTypeOption_QuestionType] FOREIGN KEY ([QuestionTypeID]) REFERENCES [dbo].[QuestionType] ([ID]) ON DELETE CASCADE
);

