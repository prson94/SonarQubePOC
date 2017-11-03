CREATE TABLE [dbo].[ReferenceItem] (
    [ID]                  INT            IDENTITY (1, 1) NOT NULL,
    [ReferenceItemTypeID] INT            NOT NULL,
    [CreatedOn]           DATETIME       NULL,
    [CreatedBy]           INT            NULL,
    [UpdatedOn]           DATETIME       NULL,
    [UpdatedBy]           INT            NULL,
    [Code]                NVARCHAR (250) NULL,
    [Visible]             BIT            CONSTRAINT [DF_ReferenceItem_Visible] DEFAULT ((1)) NOT NULL,
    [DisplayValue]        AS             ([utility].[GetObjectDisplayValueWrapper]('ReferenceItem',[ID],[ReferenceItemTypeID])),
    [KeyHash]             AS             ([utility].[GetObjectHashWrapper]('ReferenceItem',[ID],[ReferenceItemTypeID],(1))),
    [FieldHash]           AS             ([utility].[GetObjectHashWrapper]('ReferenceItem',[ID],[ReferenceItemTypeID],(0))),
    CONSTRAINT [PK_ReferenceItem] PRIMARY KEY NONCLUSTERED ([ID] ASC),
    CONSTRAINT [FK_ReferenceItem_ReferenceItemType] FOREIGN KEY ([ReferenceItemTypeID]) REFERENCES [dbo].[ReferenceItemType] ([ID]) ON DELETE CASCADE
);



go

-- add index on visible column to reference item table
CREATE NONCLUSTERED INDEX [IX_ReferenceItem_Visible] ON [dbo].[ReferenceItem] ( Visible ASC );
go
CREATE TRIGGER [dbo].[ReferenceItem_AfterUpdate]
   ON  [dbo].[ReferenceItem] 
   AFTER UPDATE
AS 
	SET NOCOUNT ON
	update	T
	set		T.UpdatedBy = S.UpdatedBy,
			T.UpdatedOn = S.UpdatedOn
	from	Asset T
			inner join inserted S on T.Object = 'ReferenceItem' and T.ObjectID = S.ID
GO
CREATE TRIGGER [dbo].[ReferenceItem_AfterInsert]
   ON  [dbo].[ReferenceItem] 
   AFTER INSERT
AS 
	SET NOCOUNT ON
	INSERT INTO [dbo].[Asset] ([AssetTypeID],[State],[Object],[ObjectID],[CreatedOn],[CreatedBy],[UpdatedOn],[UpdatedBy])
		SELECT	T.ID, 1, 'ReferenceItem', O.ID, O.[CreatedOn], O.[CreatedBy], O.[UpdatedOn], O.[UpdatedBy]
		FROM	inserted O inner join  AssetType T on T.Object = 'ReferenceItemType' and T.ObjectID = O.ReferenceItemTypeID
GO
CREATE TRIGGER [dbo].[ReferenceItem_AfterDelete]
   ON  [dbo].[ReferenceItem] 
   AFTER DELETE
AS 
	SET NOCOUNT ON
	update	Asset
	set		[State] = 3
	where	Object = 'ReferenceItem' and ObjectID in (select ID from deleted)

	delete Asset where Object = 'ReferenceItem' and ObjectID in (select ID from deleted)