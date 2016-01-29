CREATE TABLE [dbo].[IntersectType] (
    [ID]        INT      IDENTITY (1, 1) NOT NULL,
    [Name]      AS       ([utility].[DeriveIntersectTypeNameWrapper]([ID])),
    [UpdatedOn] DATETIME NULL,
    [UpdatedBy] INT      NULL,
    CONSTRAINT [PK_IntersectType] PRIMARY KEY CLUSTERED ([ID] ASC)
);






GO

CREATE TRIGGER [dbo].[IntersectType_AfterUpdate]
   ON  [dbo].[IntersectType] 
   AFTER UPDATE
AS 
	SET NOCOUNT ON;
	INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
        select 'Update', [queue].WriteIndexXml('', 'IntersectType', ID, coalesce(UpdatedBy, 0)), 'IntersectType', ID from inserted

	merge	[cache].[Object] as T
	using	(
			select	'IntersectType' as [Object],			ID as ObjectID,
					'IntersectType' as ObjectType,			0 as ObjectTypeID
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

CREATE TRIGGER [dbo].[IntersectType_AfterDelete]
   ON  [dbo].[IntersectType] 
   AFTER DELETE
AS 
	SET NOCOUNT ON;
	INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
		select 'Delete', [queue].WriteIndexXml('Removed', 'IntersectType', ID, coalesce(UpdatedBy, 0)), 'IntersectType', ID from deleted

GO

CREATE TRIGGER [dbo].[IntersectType_AfterInsert]
   ON  [dbo].[IntersectType] 
   AFTER INSERT
AS 
	SET NOCOUNT ON;
	INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
        select 'Add', [queue].WriteIndexXml('', 'IntersectType', ID, coalesce(UpdatedBy, 0)), 'IntersectType', ID from inserted

	merge	[cache].[Object] as T
	using	(
			select	'IntersectType' as [Object],			ID as ObjectID,
					'IntersectType' as ObjectType,			0 as ObjectTypeID
			from	inserted
			) as S
	on		T.[Object] = S.[Object] and T.[ObjectID] = S.[ObjectID]
	when	matched then
			update set	T.[ObjectType] = S.[ObjectType],
						T.[ObjectTypeID] = S.[ObjectTypeID]
	when	not matched then
			insert	( [Object],		[ObjectID],		[ObjectType],	[ObjectTypeID]		)
			values	( S.[Object],	S.[ObjectID],	S.[ObjectType], S.[ObjectTypeID]	);
