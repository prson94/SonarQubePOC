CREATE TABLE [dbo].[StatisticTypeRelation] (
    [StatisticTypeID] INT          NOT NULL,
    [ObjectType]      VARCHAR (50) NOT NULL,
    [ObjectID]        INT          NOT NULL,
    [Score]           INT          NOT NULL,
    CONSTRAINT [PK_StatisticTypeRelation] PRIMARY KEY CLUSTERED ([StatisticTypeID] ASC, [ObjectType] ASC, [ObjectID] ASC),
    CONSTRAINT [FK_StatisticTypeRelation_StatisticType] FOREIGN KEY ([StatisticTypeID]) REFERENCES [dbo].[StatisticType] ([ID]) ON DELETE CASCADE
);




GO

CREATE TRIGGER [dbo].[StatisticTypeRelation_AfterUpsert]
   ON  [dbo].[StatisticTypeRelation] 
   AFTER INSERT, UPDATE
AS 
	SET NOCOUNT ON;
	INSERT INTO [queue].[Task] ([Action], [Object], [ObjectID])
		select 'Analytic', 'StatisticTypeRelation', StatisticTypeID from inserted

GO

CREATE TRIGGER [dbo].[StatisticTypeRelation_AfterDelete]
   ON  [dbo].[StatisticTypeRelation] 
   AFTER DELETE
AS 
	SET NOCOUNT ON;
	INSERT INTO [queue].[Task] ([Action], [Object], [ObjectID])
		select 'Analytic', 'StatisticTypeRelation', StatisticTypeID from deleted
