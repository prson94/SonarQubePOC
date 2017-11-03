CREATE TABLE [dbo].[MapType] (
    [MapClass]    SMALLINT       CONSTRAINT [DF_MapType_MapClass] DEFAULT ((1)) NOT NULL,
    [Name]        NVARCHAR (250) NOT NULL,
    [Description] NVARCHAR (MAX) NULL,
    [CreatedOn]   DATETIME       CONSTRAINT [DF_MapType_CreatedOn] DEFAULT (getutcdate()) NULL,
    [CreatedBy]   INT            CONSTRAINT [DF_MapType_CreatedBy] DEFAULT ((0)) NULL,
    [UpdatedOn]   DATETIME       CONSTRAINT [DF_MapType_UpdatedOn] DEFAULT (getutcdate()) NULL,
    [UpdatedBy]   INT            CONSTRAINT [DF_MapType_UpdatedBy] DEFAULT ((0)) NULL,
    [ID]          INT            IDENTITY (1, 1) NOT NULL,
    CONSTRAINT [PK_MapType] PRIMARY KEY CLUSTERED ([ID] ASC)
);


GO


GO


GO


GO


GO


GO
CREATE TRIGGER [dbo].[MapType_AfterUpdate]
   ON  [dbo].[MapType] 
   AFTER UPDATE
AS 
	SET NOCOUNT ON
	--INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
	--	select 'Update', [queue].WriteIndexXml('', 'MapType', ID, coalesce(UpdatedBy, 0)), 'MapType', ID from inserted

	--merge	[cache].[Object] as T
	--using	(
	--		select	'MapType' as [Object],			ID as ObjectID,
	--				'MapType' as ObjectType,			0 as ObjectTypeID
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
			inner join inserted S on T.Object = 'MapType' and T.ObjectID = S.ID
GO
CREATE TRIGGER [dbo].[MapType_AfterInsert]
   ON  [dbo].[MapType] 
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
		select Name, Description, 7, '{Name}', 1, 0, 1, 'MapType', ID, coalesce(UpdatedOn, getutcdate()), UpdatedBy, coalesce(UpdatedOn, getutcdate()), UpdatedBy from inserted
GO
CREATE TRIGGER [dbo].[MapType_AfterDelete]
	ON [dbo].[MapType]
	AFTER DELETE
AS
	SET NOCOUNT ON
	--delete	T
	--from	[cache].[Object] T
	--		inner join deleted S on T.Object = 'MapType' and S.ID = T.ObjectID;
	update	AssetType
	set		[State] = 3
	where	Object = 'MapType' and ObjectID in (select ID from deleted)

	delete AssetType where Object = 'MapType' and ObjectID in (select ID from deleted)