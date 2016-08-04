CREATE TABLE [dbo].[QuestionType] (
    [ID]           INT             IDENTITY (50000, 1) NOT NULL,
    [SurveyTypeID] INT             NOT NULL,
    [Name]         NVARCHAR (500)  NOT NULL,
    [Description]  NVARCHAR (2000) NULL,
    [DisplayStyle] INT             NOT NULL,
    [CreatedOn]    DATETIME        NULL,
    [CreatedBy]    INT             NULL,
    [UpdatedOn]    DATETIME        NULL,
    [UpdatedBy]    INT             NULL,
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
	--INSERT INTO [queue].[Task] ([Action], [Object], [ObjectID])
	--	select 'Analytic', @ot, ID from inserted
	INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
		select 'Delete', [queue].WriteIndexXml('Removed', 'SurveyType', SurveyTypeID, coalesce(UpdatedBy, 0)), 'QuestionType', ID from deleted

GO

CREATE TRIGGER [dbo].[QuestionType_AfterInsert]
   ON  [dbo].[QuestionType] 
   AFTER INSERT
AS 
	SET NOCOUNT ON;
	INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
		select 'Add', [queue].WriteIndexXml('', 'SurveyType', SurveyTypeID, coalesce(UpdatedBy, 0)), 'QuestionType', ID from inserted

GO

CREATE TRIGGER [dbo].[QuestionType_AfterUpdate]
   ON  [dbo].[QuestionType] 
   AFTER UPDATE
AS 
	SET NOCOUNT ON;
	INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
		select 'Update', [queue].WriteIndexXml('', 'SurveyType', SurveyTypeID, coalesce(UpdatedBy, 0)), 'QuestionType', ID from inserted
