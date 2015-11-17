CREATE TABLE [dbo].[QuestionType] (
    [ID]             INT            IDENTITY (50000, 1) NOT NULL,
    [SurveyTypeID]   INT            NOT NULL,
    [Name]           NVARCHAR (250) NOT NULL,
    [Description]    NVARCHAR (500) NULL,
    [ResponseTypeID] INT            NOT NULL,
    [UpdatedOn]      DATETIME       NULL,
    [UpdatedBy]      INT            NULL,
    CONSTRAINT [PK_QuestionType] PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [FK_QuestionType_SurveyType] FOREIGN KEY ([SurveyTypeID]) REFERENCES [dbo].[SurveyType] ([ID]) ON DELETE CASCADE
);


GO
CREATE NONCLUSTERED INDEX [IX_QuestionType_SurveyTypeID]
    ON [dbo].[QuestionType]([SurveyTypeID] ASC);


GO
CREATE TRIGGER [dbo].[QuestionType_AfterDelete]
   ON  [dbo].[QuestionType] 
   AFTER DELETE
AS 
	SET NOCOUNT ON;
	insert into [queue].[ObjectVersion] ([Object], ObjectID, ResourceID, [Date], [Action], ActionObject, ActionObjectID)
		select 'SurveyType', SurveyTypeID, coalesce(UpdatedBy, 0), coalesce(UpdatedOn, getutcdate()), 'Removed', 'QuestionType', ID from inserted

GO
CREATE TRIGGER [dbo].[QuestionType_AfterInsert]
   ON  [dbo].[QuestionType] 
   AFTER INSERT
AS 
	SET NOCOUNT ON;
	insert into [queue].[ObjectVersion] ([Object], ObjectID, ResourceID, [Date], [Action], ActionObject, ActionObjectID)
		select 'SurveyType', SurveyTypeID, coalesce(UpdatedBy, 0), coalesce(UpdatedOn, getutcdate()), 'Created', 'QuestionType', ID from inserted

GO
CREATE TRIGGER [dbo].[QuestionType_AfterUpdate]
   ON  [dbo].[QuestionType] 
   AFTER UPDATE
AS 
	SET NOCOUNT ON;
	insert into [queue].[ObjectVersion] ([Object], ObjectID, ResourceID, [Date], [Action], ActionObject, ActionObjectID)
		select 'SurveyType', SurveyTypeID, coalesce(UpdatedBy, 0), coalesce(UpdatedOn, getutcdate()), 'Updated', 'QuestionType', ID from inserted
