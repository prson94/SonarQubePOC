CREATE TABLE [dbo].[RuleType] (
    [ID]            INT            IDENTITY (1, 1) NOT NULL,
    [Name]          NVARCHAR (250) NOT NULL,
    [Description]   NVARCHAR (MAX) NULL,
    [CreatedOn]     DATETIME       NULL,
    [CreatedBy]     INT            NULL,
    [UpdatedOn]     DATETIME       NULL,
    [UpdatedBy]     INT            NULL,
    [DisplayFormat] NVARCHAR (250) CONSTRAINT [DF_RuleType_DisplayFormat] DEFAULT ('{Name}') NULL,
    CONSTRAINT [PK_RuleType] PRIMARY KEY CLUSTERED ([ID] ASC)
);


GO
CREATE TRIGGER [dbo].[RuleType_AfterUpdate]
   ON  [dbo].[RuleType] 
   AFTER UPDATE
AS 
	SET NOCOUNT ON
	--INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
	--	select 'Update', [queue].WriteIndexXml('', 'RuleType', ID, coalesce(UpdatedBy, 0)), 'RuleType', ID from inserted

	--merge	[cache].[Object] as T
	--using	(
	--		select	'RuleType' as [Object],			ID as ObjectID,
	--				'RuleType' as ObjectType,			0 as ObjectTypeID
	--		from	inserted
	--		) as S
	--on		T.[Object] = S.[Object] and T.[ObjectID] = S.[ObjectID]
	--when	matched then
	--		update set	T.[ObjectType] = S.[ObjectType],
	--					T.[ObjectTypeID] = S.[ObjectTypeID]
	--when	not matched then
	--		insert	( [Object],		[ObjectID],		[ObjectType],	[ObjectTypeID]		)
	--		values	( S.[Object],	S.[ObjectID],	S.[ObjectType], S.[ObjectTypeID]	);
	update	T
	set		T.Name = S.Name,
			T.Description = S.Description,
			T.DisplayFormat = S.DisplayFormat,
			T.UpdatedBy = S.UpdatedBy,
			T.UpdatedOn = S.UpdatedOn
	from	AssetType T
			inner join inserted S on T.Object = 'RuleType' and T.ObjectID = S.ID
GO
CREATE TRIGGER [dbo].[RuleType_AfterInsert]
   ON  [dbo].[RuleType] 
   AFTER INSERT
AS 
	SET NOCOUNT ON
	--INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
	--	select 'Add', [queue].WriteIndexXml('', 'RuleType', ID, coalesce(UpdatedBy, 0)), 'RuleType', ID from inserted

	--merge	[cache].[Object] as T
	--using	(
	--		select	'RuleType' as [Object],			ID as ObjectID,
	--				'RuleType' as ObjectType,			0 as ObjectTypeID
	--		from	inserted
	--		) as S
	--on		T.[Object] = S.[Object] and T.[ObjectID] = S.[ObjectID]
	--when	matched then
	--		update set	T.[ObjectType] = S.[ObjectType],
	--					T.[ObjectTypeID] = S.[ObjectTypeID]
	--when	not matched then
	--		insert	( [Object],		[ObjectID],		[ObjectType],	[ObjectTypeID]		)
	--		values	( S.[Object],	S.[ObjectID],	S.[ObjectType], S.[ObjectTypeID]	);
	insert into AssetType (Name, Description, Class, DisplayFormat, [State], [Hierarchical], [HierarchyMaximumDepth], [Object], [ObjectID], [CreatedOn], [CreatedBy], [UpdatedOn], [UpdatedBy])
		select Name, Description, 7, coalesce(DisplayFormat, '{Name}'), 1, 0, 1, 'RuleType', ID, coalesce(UpdatedOn, getutcdate()), UpdatedBy, coalesce(UpdatedOn, getutcdate()), UpdatedBy from inserted
GO
CREATE TRIGGER [dbo].[RuleType_AfterDelete]
   ON  [dbo].[RuleType] 
   AFTER DELETE
AS 
	SET NOCOUNT ON
	--INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
	--	select 'Delete', [queue].WriteIndexXml('Removed', 'RuleType', ID, coalesce(UpdatedBy, 0)), 'RuleType', ID from deleted
	update	AssetType
	set		[State] = 3
	where	Object = 'RuleType' and ObjectID in (select ID from deleted)

	delete AssetType where Object = 'RuleType' and ObjectID in (select ID from deleted)