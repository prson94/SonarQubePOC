CREATE TABLE [dbo].[Rule] (
    [ID]              INT            IDENTITY (1, 1) NOT NULL,
    [Name]            NVARCHAR (250) NOT NULL,
    [Description]     NVARCHAR (MAX) NULL,
    [RuleType]        INT            NOT NULL,
    [UpdatedOn]       DATETIME       NULL,
    [UpdatedBy]       INT            NULL,
    [RuleDimensionID] INT            NULL,
    [Status]          INT            CONSTRAINT [DF_Rule_Status] DEFAULT ((1)) NOT NULL,
    [Threshold]       DECIMAL (4, 3) CONSTRAINT [DF_Rule_Threshold] DEFAULT ((0)) NULL,
    [Purpose]         NVARCHAR (MAX) NULL,
    [Measurement]     NVARCHAR (MAX) NULL,
    [Resolution]      NVARCHAR (MAX) NULL,
    [CreatedOn]       DATETIME       NULL,
    [CreatedBy]       INT            NULL,
    CONSTRAINT [PK_Rule] PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [FK_Rule_RuleDimension] FOREIGN KEY ([RuleDimensionID]) REFERENCES [dbo].[RuleDimension] ([ID])
);
















GO

CREATE TRIGGER [dbo].[Rule_AfterUpdate]
   ON  [dbo].[Rule] 
   AFTER UPDATE
AS 
	SET NOCOUNT ON;
	INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
		select 'Update', [queue].WriteIndexXml('', 'Rule', ID, coalesce(UpdatedBy, 0)), 'Rule', ID from inserted

	merge	[cache].[Object] as T
	using	(
			select	'Rule' as [Object],			ID as ObjectID,
					'RuleType' as ObjectType,	RuleType as ObjectTypeID
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
CREATE TRIGGER [dbo].[Rule_AfterInsert]
   ON  [dbo].[Rule] 
   AFTER INSERT
AS 
	SET NOCOUNT ON;
	INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
		select 'Add', [queue].WriteIndexXml('', 'Rule', ID, coalesce(UpdatedBy, 0)), 'Rule', ID from inserted

	update	T
	set		T.CreatedOn = coalesce(S.CreatedOn, getutcdate()),
			T.UpdatedOn = coalesce(S.UpdatedOn, getutcdate())
	from	[Rule] T
			inner join inserted S on S.ID = T.ID;

	merge	[cache].[Object] as T
	using	(
			select	'Rule' as [Object],			ID as ObjectID,
					'RuleType' as ObjectType,	RuleType as ObjectTypeID
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

CREATE TRIGGER [dbo].[Rule_AfterDelete]
   ON  [dbo].[Rule] 
   AFTER DELETE
AS 
	SET NOCOUNT ON;
	INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
		select 'Delete', [queue].WriteIndexXml('Removed', 'Rule', ID, coalesce(UpdatedBy, 0)), 'Rule', ID from deleted

	delete	T
	from	[cache].[Object] T
			inner join deleted D on T.[Object] = 'Rule' and D.ID = T.ObjectID;
