CREATE TABLE [dbo].[TaxonomyType] (
    [ID]                  INT            IDENTITY (50000, 1) NOT NULL,
    [Name]                NVARCHAR (250) NOT NULL,
    [Description]         NVARCHAR (MAX) NULL,
    [MaximumDepth]        INT            NULL,
    [TaxonomyTypeClassID] INT            NULL,
    [UpdatedOn]           DATETIME       NULL,
    [UpdatedBy]           INT            NULL,
    [DisplayFormat]       NVARCHAR (250) CONSTRAINT [DF_TaxonomyType_DisplayFormat] DEFAULT ('{Name}') NULL,
    CONSTRAINT [PK_TaxonomyType] PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [FK_TaxonomyType_TaxonomyTypeClass] FOREIGN KEY ([TaxonomyTypeClassID]) REFERENCES [dbo].[TaxonomyTypeClass] ([ID])
);










GO
CREATE TRIGGER [dbo].[TaxonomyType_AfterInsert]
   ON  [dbo].[TaxonomyType] 
   AFTER INSERT
AS 
	SET NOCOUNT ON
	--INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
	--	select 'Add', [queue].WriteIndexXml('', 'TaxonomyType', ID, coalesce(UpdatedBy, 0)), 'TaxonomyType', ID from inserted

	--merge	[cache].[Object] as T
	--using	(
	--		select	'TaxonomyType' as [Object],			ID as ObjectID,
	--				'TaxonomyType' as ObjectType,			0 as ObjectTypeID
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
		select Name, Description, 2, DisplayFormat, 1, 1, MaximumDepth, 'TaxonomyType', ID, coalesce(UpdatedOn, getutcdate()), UpdatedBy, coalesce(UpdatedOn, getutcdate()), UpdatedBy from inserted

GO
CREATE TRIGGER [dbo].[TaxonomyType_AfterUpdate]
   ON  [dbo].[TaxonomyType] 
   AFTER UPDATE
AS 
	SET NOCOUNT ON
	--INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
	--	select 'Update', [queue].WriteIndexXml('', 'TaxonomyType', ID, coalesce(UpdatedBy, 0)), 'TaxonomyType', ID from inserted

	--update	T
	--set		T.TextPath = utility.GetBreadcrumbStringWrapper('Taxonomy', S.ID, '/')
	--from	Taxonomy T
	--		inner join inserted S on S.ID = T.TaxonomyTypeID

	--merge	[cache].[Object] as T
	--using	(
	--		select	'TaxonomyType' as [Object],			ID as ObjectID,
	--				'TaxonomyType' as ObjectType,			0 as ObjectTypeID
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
			T.HierarchyMaximumDepth = S.MaximumDepth,
			T.UpdatedBy = S.UpdatedBy,
			T.UpdatedOn = S.UpdatedOn
	from	AssetType T
			inner join inserted S on T.Object = 'TaxonomyType' and T.ObjectID = S.ID;

GO
CREATE TRIGGER [dbo].[TaxonomyType_AfterDelete]
   ON  [dbo].[TaxonomyType] 
   AFTER DELETE
AS 
BEGIN
	SET NOCOUNT ON
	--INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
	--	select 'Delete', [queue].WriteIndexXml('Removed', 'TaxonomyType', ID, coalesce(UpdatedBy, 0)), 'TaxonomyType', ID from deleted
	update	AssetType
	set		[State] = 3
	where	Object = 'TaxonomyType' and ObjectID in (select ID from deleted)

	delete AssetType where Object = 'TaxonomyType' and ObjectID in (select ID from deleted)
END

