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

	delete	T
	from	[cache].[Object] T
			inner join deleted D on T.[Object] = 'Event' and D.ID = T.ObjectID;



GO

CREATE TRIGGER [dbo].[Event_AfterInsert]
	ON [dbo].[Event]
	FOR INSERT
AS
	SET NOCOUNT ON;
	INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
        select 'Add', [queue].WriteIndexXml('', 'Event', ID, 0), 'Event', ID from inserted

	merge	[cache].[Object] as T
	using	(
			select	'Event' as [Object],	E.ID as ObjectID,
					'Rule' as ObjectType,	G.RuleID as ObjectTypeID
			from	inserted E  
					inner join EventGroup G on G.ID = E.EventGroupID
			) as S
	on		T.[Object] = S.[Object] and T.[ObjectID] = S.[ObjectID]
	when	matched then
			update set	T.[ObjectType] = S.[ObjectType],
						T.[ObjectTypeID] = S.[ObjectTypeID]
	when	not matched then
			insert	( [Object],		[ObjectID],		[ObjectType],	[ObjectTypeID]		)
			values	( S.[Object],	S.[ObjectID],	S.[ObjectType], S.[ObjectTypeID]	);

GO

CREATE TRIGGER [dbo].[Event_AfterUpdate]
	ON [dbo].[Event]
	FOR UPDATE
AS
BEGIN
	SET NOCOUNT ON;
	INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
        select 'Update', [queue].WriteIndexXml('', 'Event', ID, 0), 'Event', ID from inserted

	merge	[cache].[Object] as T
	using	(
			select	'Event' as [Object],	E.ID as ObjectID,
					'Rule' as ObjectType,	G.RuleID as ObjectTypeID
			from	inserted E  
					inner join EventGroup G on G.ID = E.EventGroupID
			) as S
	on		T.[Object] = S.[Object] and T.[ObjectID] = S.[ObjectID]
	when	matched then
			update set	T.[ObjectType] = S.[ObjectType],
						T.[ObjectTypeID] = S.[ObjectTypeID]
	when	not matched then
			insert	( [Object],		[ObjectID],		[ObjectType],	[ObjectTypeID]		)
			values	( S.[Object],	S.[ObjectID],	S.[ObjectType], S.[ObjectTypeID]	);
END
