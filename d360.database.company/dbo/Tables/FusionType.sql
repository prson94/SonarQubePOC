CREATE TABLE [dbo].[FusionType] (
    [ID]          INT            IDENTITY (50000, 1) NOT NULL,
    [Name]        NVARCHAR (250) NOT NULL,
    [Description] NVARCHAR (MAX) NULL,
    [UpdatedOn]   DATETIME       NULL,
    [UpdatedBy]   INT            NULL,
    CONSTRAINT [PK_FusionType] PRIMARY KEY CLUSTERED ([ID] ASC)
);










GO
CREATE TRIGGER [dbo].[FusionType_AfterUpdate]
   ON  [dbo].[FusionType] 
   AFTER UPDATE
AS 
	SET NOCOUNT ON
	--INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
 --       select 'Update', [queue].WriteIndexXml('', 'FusionType', ID, coalesce(UpdatedBy, 0)), 'FusionType', ID from inserted

	--merge	[cache].[Object] as T
	--using	(
	--		select	'FusionType' as [Object],			ID as ObjectID,
	--				'FusionType' as ObjectType,			0 as ObjectTypeID
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
			T.UpdatedBy = S.UpdatedBy,
			T.UpdatedOn = S.UpdatedOn
	from	AssetType T
			inner join inserted S on T.Object = 'FusionType' and T.ObjectID = S.ID

GO
CREATE TRIGGER [dbo].[FusionType_AfterDelete]
   ON  [dbo].[FusionType] 
   AFTER DELETE
AS 
	SET NOCOUNT ON
	--INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
	--	select 'Delete', [queue].WriteIndexXml('Removed', 'FusionType', ID, coalesce(UpdatedBy, 0)), 'FusionType', ID from deleted
	update	AssetType
	set		[State] = 3
	where	Object = 'FusionType' and ObjectID in (select ID from deleted)

	delete AssetType where Object = 'FusionType' and ObjectID in (select ID from deleted)

GO
CREATE TRIGGER [dbo].[FusionType_AfterInsert]
   ON  [dbo].[FusionType] 
   AFTER INSERT
AS 
	SET NOCOUNT ON
	--INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
 --       select 'Add', [queue].WriteIndexXml('', 'FusionType', ID, coalesce(UpdatedBy, 0)), 'FusionType', ID from inserted

	--merge	[cache].[Object] as T
	--using	(
	--		select	'FusionType' as [Object],			ID as ObjectID,
	--				'FusionType' as ObjectType,			0 as ObjectTypeID
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
		select Name, Description, 3, '{Name}', 1, 0, 1, 'FusionType', ID, coalesce(UpdatedOn, getutcdate()), UpdatedBy, coalesce(UpdatedOn, getutcdate()), UpdatedBy from inserted
