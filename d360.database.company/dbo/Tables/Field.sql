CREATE TABLE [dbo].[Field] (
    [AssetID]            BIGINT                                      NULL,
    [ObjectType]         VARCHAR (50)                                NOT NULL,
    [ObjectID]           INT                                         NOT NULL,
    [FieldTypeID]        INT                                         NOT NULL,
    [Value]              NVARCHAR (MAX)                              NULL,
    [FormattedValue]     NVARCHAR (MAX)                              NULL,
    [EffectiveStartDate] DATETIME2 (0) GENERATED ALWAYS AS ROW START NOT NULL,
    [EffectiveEndDate]   DATETIME2 (0) GENERATED ALWAYS AS ROW END   NOT NULL,
    CONSTRAINT [PK_FieldNew] PRIMARY KEY NONCLUSTERED ([ObjectType] ASC, [ObjectID] ASC, [FieldTypeID] ASC),
    CONSTRAINT [FK_Field_FieldType] FOREIGN KEY ([FieldTypeID]) REFERENCES [dbo].[FieldType] ([ID]) ON DELETE CASCADE,
    PERIOD FOR SYSTEM_TIME ([EffectiveStartDate], [EffectiveEndDate])
)
WITH (SYSTEM_VERSIONING = ON (HISTORY_TABLE=[dbo].[Field_History], DATA_CONSISTENCY_CHECK=ON));














GO



GO



GO

CREATE TRIGGER [dbo].[Field_AfterUpsert]
	ON [dbo].[Field]
	FOR INSERT, UPDATE
AS
	SET NOCOUNT ON;

	
	UPDATE	T
	SET		T.FormattedValue = utility.GetFormattedFieldLookupValueWithMultiple(FT.Type, FT.LookupDisplayFormat, FT.LookupObjectType, FT.LookupObjectID, F.Value, FT.AllowMultipleValues)
	FROM	Field T 
			inner join inserted F on F.FieldTypeID = T.FieldTypeID and F.ObjectType = T.ObjectType and F.ObjectID = T.ObjectID and F.ObjectType <> 'FusionAttribute' and F.ObjectType <> 'FusionQueryAttribute'
			INNER JOIN FieldType FT ON FT.ID = T.FieldTypeID


	UPDATE	TF
	SET		TF.FormattedValue = utility.GetFormattedFieldLookupValueWithMultiple(FT.Type, FT.LookupDisplayFormat, FT.LookupObjectType, FT.LookupObjectID, TF.Value, FT.AllowMultipleValues)
	from	Field TF
			inner join FieldType FT on FT.ID = TF.FieldTypeID
			inner join	inserted SF on FT.LookupObjectType = SF.ObjectType and TF.Value = cast(SF.ObjectID as varchar(50)) and SF.ObjectType <> 'FusionAttribute' and SF.ObjectType <> 'FusionQueryAttribute'

GO
CREATE NONCLUSTERED INDEX [IX_Field_FieldTypeID]
    ON [dbo].[Field]([FieldTypeID] ASC)
    INCLUDE([ObjectType], [ObjectID]);




GO
CREATE CLUSTERED INDEX [CIX_Field]
    ON [dbo].[Field]([ObjectType] ASC, [ObjectID] ASC);


GO
CREATE TRIGGER [dbo].[Field_AfterInsert]
	ON [dbo].[Field]
	FOR INSERT
AS
	SET NOCOUNT ON;

	UPDATE	T
	SET		T.AssetID = A.ID
	FROM	Field T 
			inner join inserted F on F.FieldTypeID = T.FieldTypeID and F.ObjectType = T.ObjectType and F.ObjectID = T.ObjectID and T.AssetID is null
			left join Asset A on A.Object = F.ObjectType and A.ObjectID = F.ObjectID