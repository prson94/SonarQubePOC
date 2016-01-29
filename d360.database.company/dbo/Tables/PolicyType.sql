CREATE TABLE [dbo].[PolicyType] (
    [ID]                INT             IDENTITY (50000, 1) NOT NULL,
    [Name]              NVARCHAR (250)  NOT NULL,
    [Description]       NVARCHAR (4000) NULL,
    [PolicyTypeClassID] INT             NULL,
    [UpdatedOn]         DATETIME        NULL,
    [UpdatedBy]         INT             NULL,
    [MaximumDepth]      INT             NULL,
    CONSTRAINT [PK_PolicyType] PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [FK_PolicyType_PolicyTypeClass] FOREIGN KEY ([PolicyTypeClassID]) REFERENCES [dbo].[PolicyTypeClass] ([ID])
);






GO

CREATE TRIGGER [dbo].[PolicyType_AfterInsert]
   ON  [dbo].[PolicyType] 
   AFTER INSERT
AS 
	SET NOCOUNT ON;
	INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
		select 'Add', [queue].WriteIndexXml('', 'PolicyType', ID, coalesce(UpdatedBy, 0)), 'PolicyType', ID from inserted

	merge	[cache].[Object] as T
	using	(
			select	'PolicyType' as [Object],			ID as ObjectID,
					'PolicyType' as ObjectType,			0 as ObjectTypeID
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

CREATE TRIGGER [dbo].[PolicyType_AfterUpdate]
   ON  [dbo].[PolicyType] 
   AFTER UPDATE
AS 
	SET NOCOUNT ON;
	INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
		select 'Update', [queue].WriteIndexXml('', 'PolicyType', ID, coalesce(UpdatedBy, 0)), 'PolicyType', ID from inserted

	update	T
	set		T.TextPath = utility.GetBreadcrumbStringWrapper('Policy', T.ID, '/')
	from	Policy T
			inner join inserted S on S.ID = T.PolicyTypeID

	merge	[cache].[Object] as T
	using	(
			select	'PolicyType' as [Object],			ID as ObjectID,
					'PolicyType' as ObjectType,			0 as ObjectTypeID
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

CREATE TRIGGER [dbo].[PolicyType_AfterDelete]
   ON  [dbo].[PolicyType] 
   AFTER DELETE
AS 
	SET NOCOUNT ON;
	INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
		select 'Delete', [queue].WriteIndexXml('Removed', 'PolicyType', ID, coalesce(UpdatedBy, 0)), 'PolicyType', ID from deleted
