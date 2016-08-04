CREATE TABLE [dbo].[SurveyType] (
    [ID]           INT            IDENTITY (50000, 1) NOT NULL,
    [Name]         NVARCHAR (250) NOT NULL,
    [Description]  NVARCHAR (MAX) NULL,
    [Object]       VARCHAR (50)   NOT NULL,
    [ObjectID]     INT            NOT NULL,
    [ValidForDays] INT            NOT NULL,
    [CreatedOn]    DATETIME       NULL,
    [CreatedBy]    INT            NULL,
    [UpdatedOn]    DATETIME       NULL,
    [UpdatedBy]    INT            NULL,
    CONSTRAINT [PK_SurveyType] PRIMARY KEY CLUSTERED ([ID] ASC)
);






GO
CREATE NONCLUSTERED INDEX [IX_SurveyType_ObjectType-ObjectID]
    ON [dbo].[SurveyType]([Object] ASC, [ObjectID] ASC);




GO

CREATE TRIGGER [dbo].[SurveyType_AfterDelete]
   ON  [dbo].[SurveyType] 
   AFTER DELETE
AS 
	SET NOCOUNT ON;
	INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
		select 'Delete', [queue].WriteIndexXml('Removed', 'SurveyType', ID, coalesce(UpdatedBy, 0)), 'SurveyType', ID from deleted

GO

CREATE TRIGGER [dbo].[SurveyType_AfterInsert]
   ON  [dbo].[SurveyType] 
   AFTER INSERT
AS 
	SET NOCOUNT ON;
	INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
		select 'Add', [queue].WriteIndexXml('', 'SurveyType', ID, coalesce(UpdatedBy, 0)), 'SurveyType', ID from inserted

GO

CREATE TRIGGER [dbo].[SurveyType_AfterUpdate]
   ON  [dbo].[SurveyType] 
   AFTER UPDATE
AS 
	SET NOCOUNT ON;
	INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
		select 'Update', [queue].WriteIndexXml('', 'SurveyType', ID, coalesce(UpdatedBy, 0)), 'SurveyType', ID from inserted
