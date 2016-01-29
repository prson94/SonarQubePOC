CREATE TABLE [dbo].[LookupType] (
    [ID]        INT            IDENTITY (1, 1) NOT NULL,
    [Name]      NVARCHAR (250) NOT NULL,
    [UpdatedOn] DATETIME       NULL,
    [UpdatedBy] INT            NULL,
    CONSTRAINT [PK_LookupType] PRIMARY KEY CLUSTERED ([ID] ASC)
);






GO

CREATE TRIGGER [dbo].[LookupType_AfterDelete]
   ON  [dbo].[LookupType] 
   AFTER DELETE
AS 
	SET NOCOUNT ON;
	INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
		select 'Delete', [queue].WriteIndexXml('Removed', 'LookupType', ID, coalesce(UpdatedBy, 0)), 'LookupType', ID from deleted

GO

CREATE TRIGGER [dbo].[LookupType_AfterInsert]
   ON  [dbo].[LookupType] 
   AFTER INSERT
AS 
	SET NOCOUNT ON;
	INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
        select 'Add', [queue].WriteIndexXml('', 'LookupType', ID, coalesce(UpdatedBy, 0)), 'LookupType', ID from inserted

	merge	[cache].[Object] as T
	using	(
			select	'LookupType' as [Object],			ID as ObjectID,
					'LookupType' as ObjectType,			0 as ObjectTypeID
			from	inserted
			) as S
	on		T.[Object] = S.[Object] and T.[ObjectID] = S.[ObjectID]
	when	matched then
			update set	T.[ObjectType] = S.[ObjectType],
						T.[ObjectTypeID] = S.[ObjectTypeID]
	when	not matched then
			insert	( [Object],		[ObjectID],		[ObjectType],	[ObjectTypeID]		)
			values	( S.[Object],	S.[ObjectID],	S.[ObjectType], S.[ObjectTypeID]	);

GO

CREATE TRIGGER [dbo].[LookupType_AfterUpdate]
   ON  [dbo].[LookupType] 
   AFTER UPDATE
AS 
	SET NOCOUNT ON;
	INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
        select 'Update', [queue].WriteIndexXml('', 'LookupType', ID, coalesce(UpdatedBy, 0)), 'LookupType', ID from inserted

	merge	[cache].[Object] as T
	using	(
			select	'LookupType' as [Object],			ID as ObjectID,
					'LookupType' as ObjectType,			0 as ObjectTypeID
			from	inserted
			) as S
	on		T.[Object] = S.[Object] and T.[ObjectID] = S.[ObjectID]
	when	matched then
			update set	T.[ObjectType] = S.[ObjectType],
						T.[ObjectTypeID] = S.[ObjectTypeID]
	when	not matched then
			insert	( [Object],		[ObjectID],		[ObjectType],	[ObjectTypeID]		)
			values	( S.[Object],	S.[ObjectID],	S.[ObjectType], S.[ObjectTypeID]	);
