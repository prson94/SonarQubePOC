CREATE TABLE [dbo].[FusionAttributeType] (
    [ID]           INT            IDENTITY (50000, 1) NOT NULL,
    [ParentID]     INT            NULL,
    [FusionTypeID] INT            NOT NULL,
    [Name]         NVARCHAR (500) NOT NULL,
    [Path]         AS             ([utility].[GetBreadcrumbWrapper]('FusionAttributeType',[ID])),
    [TextPath]     AS             ([utility].[GetBreadcrumbStringWrapper]('FusionAttributeType',[ID],'.')),
    [Tab]          NVARCHAR (250) NULL,
    [Assignable]   BIT            CONSTRAINT [DF_FusionAttributeType_Assignable] DEFAULT ((0)) NOT NULL,
    [UpdatedOn]    DATETIME       NULL,
    [UpdatedBy]    INT            NULL,
    CONSTRAINT [PK_FusionAttributeType] PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [FK_FusionAttributeType_Parent] FOREIGN KEY ([ParentID]) REFERENCES [dbo].[FusionAttributeType] ([ID])
);






GO
CREATE NONCLUSTERED INDEX [IX_FusionAttributeType_FusionTypeID]
    ON [dbo].[FusionAttributeType]([FusionTypeID] ASC);


GO

CREATE TRIGGER [dbo].[FusionAttributeType_AfterDelete]
   ON  [dbo].[FusionAttributeType] 
   AFTER DELETE
AS 
	SET NOCOUNT ON;
	INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
		select 'Delete', [queue].WriteIndexXml('Removed', 'FusionType', FusionTypeID, coalesce(UpdatedBy, 0)), 'FusionAttributeType', ID from deleted

GO

CREATE TRIGGER [dbo].[FusionAttributeType_AfterInsert]
   ON  [dbo].[FusionAttributeType] 
   AFTER INSERT
AS 
	SET NOCOUNT ON;
	INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
        select 'Add', [queue].WriteIndexXml('', 'FusionType', FusionTypeID, coalesce(UpdatedBy, 0)), 'FusionAttributeType', ID from inserted

	merge	[cache].[Object] as T
	using	(
			select	'FusionAttributeType' as [Object],			ID as ObjectID,
					'FusionType' as ObjectType,					FusionTypeID as ObjectTypeID
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

CREATE TRIGGER [dbo].[FusionAttributeType_AfterUpdate]
   ON  [dbo].[FusionAttributeType] 
   AFTER UPDATE
AS 
	SET NOCOUNT ON;
	INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
        select 'Update', [queue].WriteIndexXml('', 'FusionType', FusionTypeID, coalesce(UpdatedBy, 0)), 'FusionAttributeType', ID from inserted

	merge	[cache].[Object] as T
	using	(
			select	'FusionAttributeType' as [Object],			ID as ObjectID,
					'FusionType' as ObjectType,					FusionTypeID as ObjectTypeID
			from	inserted
			) as S
	on		T.[Object] = S.[Object] and T.[ObjectID] = S.[ObjectID]
	when	matched then
			update set	T.[ObjectType] = S.[ObjectType],
						T.[ObjectTypeID] = S.[ObjectTypeID]
	when	not matched then
			insert	( [Object],		[ObjectID],		[ObjectType],	[ObjectTypeID]		)
			values	( S.[Object],	S.[ObjectID],	S.[ObjectType], S.[ObjectTypeID]	);
