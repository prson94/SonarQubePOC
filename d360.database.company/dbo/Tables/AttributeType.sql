CREATE TABLE [dbo].[AttributeType] (
    [ID]                      INT            IDENTITY (50000, 1) NOT NULL,
    [ParentID]                INT            NULL,
    [Name]                    NVARCHAR (250) NOT NULL,
    [Description]             NVARCHAR (MAX) NULL,
    [TextFormatString]        NVARCHAR (250) NOT NULL,
    [AttributeTypeCategoryID] INT            NULL,
    [UpdatedOn]               DATETIME       NULL,
    [UpdatedBy]               INT            NULL,
    [ShowNameInTree]          BIT            DEFAULT ((1)) NOT NULL,
    CONSTRAINT [PK_AttributeType] PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [FK_AttributeType_AttributeTypeCategory] FOREIGN KEY ([AttributeTypeCategoryID]) REFERENCES [dbo].[AttributeTypeCategory] ([ID]),
    CONSTRAINT [FK_AttributeType_ParentAttributeType] FOREIGN KEY ([ParentID]) REFERENCES [dbo].[AttributeType] ([ID])
);








GO
CREATE NONCLUSTERED INDEX [IX_Attribute_ParentID]
    ON [dbo].[AttributeType]([ParentID] ASC);


GO

CREATE TRIGGER [dbo].[AttributeType_AfterDelete]
   ON  [dbo].[AttributeType] 
   AFTER DELETE
AS 
	SET NOCOUNT ON;
	DECLARE @Count int
	SET @Count = 0;

	IF 0 < (
		SELECT count(*) FROM deleted atts JOIN  statisticType statType on statType.Configuration.exist ('/fields[ObjectID=sql:column("atts.ID") and ObjectType="AttributeType"]') = 1
	)
	BEGIN
		RAISERROR('You cannot delete an attribute if it is being used in an Analytic.  Please delete the analytic before deleting the attribute.',16,1)
		ROLLBACK TRANSACTION
		RETURN;
	END
	ELSE
	BEGIN
		INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
			select 'Delete', [queue].WriteIndexXml('Removed', 'AttributeType', ID, coalesce(UpdatedBy, 0)), 'AttributeType', ID from deleted

		delete	T
		from	[cache].[Object] T
				inner join deleted D on T.[Object] = 'AttributeType' and D.ID = T.ObjectID;
	END


GO

CREATE TRIGGER [dbo].[AttributeType_AfterInsert]
   ON  [dbo].[AttributeType] 
   AFTER INSERT
AS 
	SET NOCOUNT ON;
	INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
        select 'Add', [queue].WriteIndexXml('', 'AttributeType', ID, coalesce(UpdatedBy, 0)), 'AttributeType', ID from inserted

	merge	[cache].[Object] as T
	using	(
			select	'AttributeType' as [Object],			ID as ObjectID,
					'AttributeType' as ObjectType,			0 as ObjectTypeID
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


CREATE TRIGGER [dbo].[AttributeType_AfterUpdate]
   ON  [dbo].[AttributeType] 
   AFTER UPDATE
AS 
	SET NOCOUNT ON;
	INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
        select 'Update', [queue].WriteIndexXml('', 'AttributeType', ID, coalesce(UpdatedBy, 0)), 'AttributeType', ID from inserted

	merge	[cache].[Object] as T
	using	(
			select	'AttributeType' as [Object],			ID as ObjectID,
					'AttributeType' as ObjectType,			0 as ObjectTypeID
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

