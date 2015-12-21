CREATE TABLE [dbo].[Lookup] (
    [ID]           INT      IDENTITY (1, 1) NOT NULL,
    [LookupTypeID] INT      NOT NULL,
    [UpdatedOn]    DATETIME NULL,
    [UpdatedBy]    INT      NULL,
    CONSTRAINT [PK_Lookup] PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [FK_Lookup_LookupType] FOREIGN KEY ([LookupTypeID]) REFERENCES [dbo].[LookupType] ([ID]) ON DELETE CASCADE
);




GO
CREATE NONCLUSTERED INDEX [IX_Lookup_LookupTypeID]
    ON [dbo].[Lookup]([LookupTypeID] ASC);


GO

CREATE TRIGGER [dbo].[Lookup_AfterDelete]
   ON  [dbo].[Lookup] 
   AFTER DELETE
AS 
	SET NOCOUNT ON;
	INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
		select 'Delete', [queue].WriteIndexXml('Removed', 'LookupType', LookupTypeID, coalesce(UpdatedBy, 0)), 'Lookup', ID from deleted

GO

CREATE TRIGGER [dbo].[Lookup_AfterInsert]
   ON  [dbo].[Lookup] 
   AFTER INSERT
AS 
	SET NOCOUNT ON;
	INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
        select 'Add', [queue].WriteIndexXml('', 'LookupType', LookupTypeID, coalesce(UpdatedBy, 0)), 'Lookup', ID from inserted

	UPDATE	F
	set		F.FormattedValue = utility.GetFormattedFieldLookupValue(FT.Type, FT.LookupDisplayFormat, FT.LookupObjectType, FT.LookupObjectID, F.Value)
	FROM	Field F
			inner join FieldType FT on FT.ID = F.FieldTypeID and FT.LookupObjectType = 'Lookup' 
			inner join inserted A on A.LookupTypeID = FT.LookupObjectID and cast(A.ID as nvarchar(15)) = F.Value

GO

CREATE TRIGGER [dbo].[Lookup_AfterUpdate]
   ON  [dbo].[Lookup] 
   AFTER UPDATE
AS 
	SET NOCOUNT ON;
	INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
        select 'Update', [queue].WriteIndexXml('', 'LookupType', LookupTypeID, coalesce(UpdatedBy, 0)), 'Lookup', ID from inserted

	UPDATE	F
	set		F.FormattedValue = utility.GetFormattedFieldLookupValue(FT.Type, FT.LookupDisplayFormat, FT.LookupObjectType, FT.LookupObjectID, F.Value)
	FROM	Field F
			inner join FieldType FT on FT.ID = F.FieldTypeID and FT.LookupObjectType = 'Lookup' 
			inner join inserted A on A.LookupTypeID = FT.LookupObjectID and cast(A.ID as nvarchar(15)) = F.Value
