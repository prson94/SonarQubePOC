CREATE TABLE [dbo].[ReferenceItemType] (
    [ID]            INT            IDENTITY (1, 1) NOT NULL,
    [Name]          NVARCHAR (250) NOT NULL,
    [DisplayFormat] NVARCHAR (250) NOT NULL,
    [Description]   NVARCHAR (MAX) NULL,
    [CreatedOn]     DATETIME       NULL,
    [CreatedBy]     INT            NULL,
    [UpdatedOn]     DATETIME       NULL,
    [UpdatedBy]     INT            NULL,
    CONSTRAINT [PK_ReferenceItemType] PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [CONST_Reference_Item_Type_Name] UNIQUE NONCLUSTERED ([Name] ASC)
);




GO
CREATE TRIGGER [dbo].[ReferenceItemType_AfterInsert]
   ON  [dbo].[ReferenceItemType]
   AFTER INSERT
AS 
	SET NOCOUNT ON
	--INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
 --       select 'Add', [queue].WriteIndexXml('', 'ReferenceItemType', ID, coalesce(UpdatedBy, 0)), 'ReferenceItemType', ID from inserted

	--merge	[cache].[Object] as T
	--using	(
	--		select	'ReferenceItemType' as [Object],			ID as ObjectID,
	--				'ReferenceItemType' as ObjectType,			0 as ObjectTypeID
	--		from	inserted
	--		) as S
	--on		T.[Object] = S.[Object] and T.[ObjectID] = S.[ObjectID]
	--when	matched then
	--		update set	T.[ObjectType] = S.[ObjectType],
	--					T.[ObjectTypeID] = S.[ObjectTypeID]
	--when	not matched then
	--		insert	( [Object],		[ObjectID],		[ObjectType],	[ObjectTypeID]		)
	--		values	( S.[Object],	S.[ObjectID],	S.[ObjectType], S.[ObjectTypeID]	);
	insert into AssetType (Name, Description, Class, DisplayFormat, [State],  [Hierarchical], [HierarchyMaximumDepth], [Object], [ObjectID], [CreatedOn], [CreatedBy], [UpdatedOn], [UpdatedBy])
		select Name, Description, 9, coalesce(DisplayFormat, '{Name}'), 1, 0, 1, 'ReferenceItemType', ID, coalesce(UpdatedOn, getutcdate()), UpdatedBy, coalesce(UpdatedOn, getutcdate()), UpdatedBy from inserted
GO
CREATE TRIGGER [dbo].[ReferenceItemType_AfterDelete]
   ON  [dbo].[ReferenceItemType] 
   AFTER DELETE
AS 
	SET NOCOUNT ON
	--INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
	--	select 'Delete', [queue].WriteIndexXml('Removed', 'ReferenceItemType', ID, coalesce(UpdatedBy, 0)), 'ReferenceItemType', ID from deleted
	update	AssetType
	set		[State] = 3
	where	Object = 'ReferenceItemType' and ObjectID in (select ID from deleted)

	delete AssetType where Object = 'ReferenceItemType' and ObjectID in (select ID from deleted)
GO
CREATE TRIGGER [dbo].[ReferenceItemType_AfterUpdate]
   ON  [dbo].[ReferenceItemType] 
   AFTER UPDATE
AS 
	SET NOCOUNT ON
	--INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
 --       select 'Update', [queue].WriteIndexXml('', 'ReferenceItemType', ID, coalesce(UpdatedBy, 0)), 'ReferenceItemType', ID from inserted

	--merge	[cache].[Object] as T
	--using	(
	--		select	'ReferenceItemType' as [Object],			ID as ObjectID,
	--				'ReferenceItemType' as ObjectType,			0 as ObjectTypeID
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
			inner join inserted S on T.Object = 'ReferenceItemType' and T.ObjectID = S.ID