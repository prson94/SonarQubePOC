CREATE TABLE [dbo].[QuestionOption] (
    [QuestionID]           INT NOT NULL,
    [QuestionTypeOptionID] INT NOT NULL,
    CONSTRAINT [PK_QuestionOption] PRIMARY KEY CLUSTERED ([QuestionID] ASC, [QuestionTypeOptionID] ASC),
    CONSTRAINT [FK_QuestionOption_Question] FOREIGN KEY ([QuestionID]) REFERENCES [dbo].[Question] ([ID]) ON DELETE CASCADE,
    CONSTRAINT [FK_QuestionOption_QuestionTypeOption] FOREIGN KEY ([QuestionTypeOptionID]) REFERENCES [dbo].[QuestionTypeOption] ([ID]) ON DELETE CASCADE
);

