CREATE TABLE [dbo].[DomainType] (
    [ID]          INT            IDENTITY (1, 1) NOT NULL,
    [Name]        NVARCHAR (250) NOT NULL,
    [Description] NVARCHAR (MAX) NULL,
    [UpdatedOn]   DATETIME       NULL,
    [UpdatedBy]   INT            NULL,
    CONSTRAINT [PK_DomainType] PRIMARY KEY CLUSTERED ([ID] ASC)
);








GO

CREATE TRIGGER [dbo].[DomainType_AfterDelete]
	ON [dbo].[DomainType]
	AFTER DELETE
	AS
BEGIN
	SET NOCOUNT ON;
	INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
		select 'Delete', [queue].WriteIndexXml('Removed', 'DomainType', ID, coalesce(UpdatedBy, 0)), 'DomainType', ID from deleted
END

GO



CREATE TRIGGER [dbo].[DomainType_AfterInsert]
   ON  [dbo].[DomainType] 
   AFTER INSERT
AS 
	SET NOCOUNT ON;
	INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
        select 'Add', [queue].WriteIndexXml('', 'DomainType', ID, coalesce(UpdatedBy, 0)), 'DomainType', ID from inserted

	merge	[cache].[Object] as T
	using	(
			select	'DomainType' as [Object],			ID as ObjectID,
					'DomainType' as ObjectType,			0 as ObjectTypeID
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

CREATE TRIGGER [dbo].[DomainType_AfterUpdate]
   ON  [dbo].[DomainType] 
   AFTER UPDATE
AS 
	SET NOCOUNT ON;
	INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
        select 'Update', [queue].WriteIndexXml('', 'DomainType', ID, coalesce(UpdatedBy, 0)), 'DomainType', ID from inserted

	merge	[cache].[Object] as T
	using	(
			select	'DomainType' as [Object],			ID as ObjectID,
					'DomainType' as ObjectType,			0 as ObjectTypeID
			from	inserted
			) as S
	on		T.[Object] = S.[Object] and T.[ObjectID] = S.[ObjectID]
	when	matched then
			update set	T.[ObjectType] = S.[ObjectType],
						T.[ObjectTypeID] = S.[ObjectTypeID]
	when	not matched then
			insert	( [Object],		[ObjectID],		[ObjectType],	[ObjectTypeID]		)
			values	( S.[Object],	S.[ObjectID],	S.[ObjectType], S.[ObjectTypeID]	);
