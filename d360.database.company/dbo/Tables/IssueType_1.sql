CREATE TABLE [dbo].[IssueType] (
    [ID]          INT            IDENTITY (1, 1) NOT NULL,
    [Name]        NVARCHAR (250) NOT NULL,
    [Description] NVARCHAR (MAX) NULL,
    [IsSystem]    BIT            NOT NULL,
    [UpdatedOn]   DATETIME       NULL,
    [UpdatedBy]   INT            NULL,
    CONSTRAINT [PK_IssueType] PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [CONST_IssueType_Name] UNIQUE NONCLUSTERED ([Name] ASC)
);


GO

CREATE TRIGGER [dbo].[IssueType_AfterUpdate]
   ON  [dbo].[IssueType] 
   AFTER UPDATE
AS 
	SET NOCOUNT ON;
	INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
        select 'Update', [queue].WriteIndexXml('', 'IssueType', ID, coalesce(UpdatedBy, 0)), 'IssueType', ID from inserted

	merge	[cache].[Object] as T
	using	(
			select	'IssueType' as [Object],			ID as ObjectID,
					'IssueType' as ObjectType,			0 as ObjectTypeID
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
CREATE TRIGGER [dbo].[IssueType_AfterDelete]
   ON  [dbo].[IssueType] 
   AFTER DELETE
AS 
	SET NOCOUNT ON;
	INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
		select 'Delete', [queue].WriteIndexXml('Removed', 'IssueType', ID, coalesce(UpdatedBy, 0)), 'IssueType', ID from deleted
GO




CREATE TRIGGER [dbo].[IssueType_AfterInsert]
   ON  [dbo].[IssueType]
   AFTER INSERT
AS 
	SET NOCOUNT ON;
	INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
        select 'Add', [queue].WriteIndexXml('', 'IssueType', ID, coalesce(UpdatedBy, 0)), 'IssueType', ID from inserted

	merge	[cache].[Object] as T
	using	(
			select	'IssueType' as [Object],			ID as ObjectID,
					'IssueType' as ObjectType,			0 as ObjectTypeID
			from	inserted
			) as S
	on		T.[Object] = S.[Object] and T.[ObjectID] = S.[ObjectID]
	when	matched then
			update set	T.[ObjectType] = S.[ObjectType],
						T.[ObjectTypeID] = S.[ObjectTypeID]
	when	not matched then
			insert	( [Object],		[ObjectID],		[ObjectType],	[ObjectTypeID]		)
			values	( S.[Object],	S.[ObjectID],	S.[ObjectType], S.[ObjectTypeID]	);