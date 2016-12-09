CREATE TABLE [dbo].[Field] (
    [ObjectType]     VARCHAR (50)   NOT NULL,
    [ObjectID]       INT            NOT NULL,
    [FieldTypeID]    INT            NOT NULL,
    [Value]          NVARCHAR (MAX) NULL,
    [FormattedValue] NVARCHAR (MAX) NULL,
    CONSTRAINT [PK_Field] PRIMARY KEY CLUSTERED ([ObjectType] ASC, [ObjectID] ASC, [FieldTypeID] ASC),
    CONSTRAINT [FK_Field_FieldType] FOREIGN KEY ([FieldTypeID]) REFERENCES [dbo].[FieldType] ([ID]) ON DELETE CASCADE
);










GO
CREATE NONCLUSTERED INDEX [IX_Field_ObjectType-ObjectID]
    ON [dbo].[Field]([ObjectType] ASC, [ObjectID] ASC);


GO



GO
CREATE TRIGGER [dbo].[Field_AfterUpsert]
	ON [dbo].[Field]
	FOR INSERT, UPDATE
AS
	SET NOCOUNT ON;

	
	UPDATE	T
	SET		T.FormattedValue = utility.GetFormattedFieldLookupValue(FT.Type, FT.LookupDisplayFormat, FT.LookupObjectType, FT.LookupObjectID, F.Value)
	FROM	Field T 
			inner join inserted F on F.FieldTypeID = T.FieldTypeID and F.ObjectType = T.ObjectType and F.ObjectID = T.ObjectID
			INNER JOIN FieldType FT ON FT.ID = T.FieldTypeID


	UPDATE	TF
	SET		TF.FormattedValue = utility.GetFormattedFieldLookupValue(FT.Type, FT.LookupDisplayFormat, FT.LookupObjectType, FT.LookupObjectID, TF.Value)
	from	Field TF
			inner join FieldType FT on FT.ID = TF.FieldTypeID
			inner join	inserted SF on FT.LookupObjectType = SF.ObjectType and TF.Value = cast(SF.ObjectID as varchar(50))
GO
CREATE NONCLUSTERED INDEX [IX_Field_FieldTypeID]
    ON [dbo].[Field]([FieldTypeID] ASC)
    INCLUDE([Value]);

