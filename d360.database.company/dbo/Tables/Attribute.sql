CREATE TABLE [dbo].[Attribute] (
    [ID]                    INT          IDENTITY (1, 1) NOT NULL,
    [ParentID]              INT          NULL,
    [AttributeTypeID]       INT          NOT NULL,
    [ObjectType]            VARCHAR (50) NOT NULL,
    [ObjectID]              INT          NOT NULL,
    [InheritanceObjectType] VARCHAR (50) NULL,
    [InheritanceObjectID]   INT          NULL,
    [UpdatedOn]             DATETIME     NULL,
    [UpdatedBy]             INT          NULL,
    [DisplayValue]          AS           ([utility].[GetObjectDisplayValueWrapper]('Attribute',[ID],[AttributeTypeID])),
    CONSTRAINT [PK_Attribute] PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [FK_Attribute_AttributeType] FOREIGN KEY ([AttributeTypeID]) REFERENCES [dbo].[AttributeType] ([ID]) ON DELETE CASCADE,
    CONSTRAINT [FK_Attribute_ParentAttribute] FOREIGN KEY ([ParentID]) REFERENCES [dbo].[Attribute] ([ID])
);






GO
CREATE NONCLUSTERED INDEX [IX_Attribute_ObjectType-ObjectID]
    ON [dbo].[Attribute]([ObjectType] ASC, [ObjectID] ASC);


GO
CREATE TRIGGER [dbo].[Attribute_AfterDelete]
   ON  [dbo].[Attribute] 
   AFTER DELETE
AS 
	SET NOCOUNT ON
	update	Asset
	set		[State] = 3
	where	Object = 'Attribute' and ObjectID in (select ID from deleted)

	delete Asset where Object = 'Attribute' and ObjectID in (select ID from deleted)

GO
CREATE TRIGGER [dbo].[Attribute_AfterInsert]
   ON  [dbo].[Attribute] 
   AFTER INSERT
AS 
	SET NOCOUNT ON
	INSERT INTO [dbo].[Asset] ([AssetTypeID],[State],[Object],[ObjectID],[CreatedOn],[CreatedBy],[UpdatedOn],[UpdatedBy])
		SELECT	T.ID, 1, 'Attribute', O.ID, O.[UpdatedOn], O.[UpdatedBy], O.[UpdatedOn], O.[UpdatedBy]
		FROM	inserted O inner join  AssetType T on T.Object = 'AttributeType' and T.ObjectID = O.AttributeTypeID

GO
CREATE TRIGGER [dbo].[Attribute_AfterUpdate]
   ON  [dbo].[Attribute] 
   AFTER UPDATE
AS 
	SET NOCOUNT ON
	update	T
	set		T.UpdatedBy = S.UpdatedBy,
			T.UpdatedOn = S.UpdatedOn
	from	Asset T
			inner join inserted S on T.Object = 'Attribute' and T.ObjectID = S.ID
