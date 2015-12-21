CREATE TABLE [dbo].[Event] (
    [ID]           INT            IDENTITY (1, 1) NOT NULL,
    [EventGroupID] INT            NULL,
    [SourceID]     NVARCHAR (250) NULL,
    [Status]       VARCHAR (50)   CONSTRAINT [DF_Event_Status] DEFAULT ('Open') NOT NULL,
    [Date]         DATETIME       CONSTRAINT [DF_Event_Date] DEFAULT (getutcdate()) NOT NULL,
    [Criticality]  INT            NULL,
    CONSTRAINT [PK_Event] PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [FK_Event_EventGroup] FOREIGN KEY ([EventGroupID]) REFERENCES [dbo].[EventGroup] ([ID])
);




GO
CREATE NONCLUSTERED INDEX [IX_Event_EventGroupID]
    ON [dbo].[Event]([EventGroupID] ASC);


GO

CREATE TRIGGER [dbo].[Event_AfterDelete]
	ON [dbo].[Event]
	AFTER DELETE
AS
	SET NOCOUNT ON;
		
	INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
		select 'Delete', [queue].WriteIndexXml('Removed', 'Event', ID, 0), 'Event', ID from deleted



GO

CREATE TRIGGER [dbo].[Event_AfterInsert]
	ON [dbo].[Event]
	FOR INSERT
AS
	SET NOCOUNT ON;
	INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
        select 'Add', [queue].WriteIndexXml('', 'Event', ID, 0), 'Event', ID from inserted

GO

CREATE TRIGGER [dbo].[Event_AfterUpdate]
	ON [dbo].[Event]
	FOR UPDATE
AS
BEGIN
	SET NOCOUNT ON;
	INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
        select 'Update', [queue].WriteIndexXml('', 'Event', ID, 0), 'Event', ID from inserted
END
