CREATE TABLE [dbo].[FieldType] (
    [ID]                      INT                                         IDENTITY (1, 1) NOT NULL,
    [Name]                    NVARCHAR (250)                              NOT NULL,
    [FriendlyName]            NVARCHAR (250)                              NOT NULL,
    [Description]             NVARCHAR (4000)                             NULL,
    [DisplayDescription]      NVARCHAR (4000)                             NULL,
    [FormDescription]         NVARCHAR (4000)                             NULL,
    [Type]                    VARCHAR (25)                                NOT NULL,
    [LookupObjectType]        VARCHAR (25)                                NULL,
    [LookupObjectID]          INT                                         NULL,
    [LookupDisplayFormat]     NVARCHAR (250)                              NULL,
    [MinimumLength]           INT                                         NULL,
    [MaximumLength]           INT                                         NULL,
    [Length]                  INT                                         NULL,
    [Pattern]                 VARCHAR (1000)                              NULL,
    [Object]                  VARCHAR (50)                                CONSTRAINT [CK_FieldType_Object] DEFAULT ('') NOT NULL,
    [ObjectID]                INT                                         CONSTRAINT [CK_FieldType_ObjectID] DEFAULT ((0)) NOT NULL,
    [SortOrder]               INT                                         CONSTRAINT [CK_FieldType_SortOrder] DEFAULT ((0)) NOT NULL,
    [IsRequired]              BIT                                         CONSTRAINT [CK_FieldType_IsRequired] DEFAULT ((0)) NOT NULL,
    [IsListable]              BIT                                         CONSTRAINT [CK_FieldType_IsListable] DEFAULT ((0)) NOT NULL,
    [ValidationDescription]   NVARCHAR (500)                              NULL,
    [Category]                NVARCHAR (250)                              NULL,
    [IsDisplayable]           BIT                                         CONSTRAINT [DF_FieldType_IsDisplayable] DEFAULT ((1)) NOT NULL,
    [IsEditable]              BIT                                         CONSTRAINT [DF_FieldType_IsEditable] DEFAULT ((1)) NOT NULL,
    [DefaultValue]            NVARCHAR (MAX)                              NULL,
    [DefaultFormattedValue]   NVARCHAR (MAX)                              NULL,
    [AllowAllValue]           BIT                                         CONSTRAINT [DF_FieldType_AllowAllValue] DEFAULT ((0)) NOT NULL,
    [AllowAllLabel]           NVARCHAR (250)                              NULL,
    [IsPrimaryFilter]         BIT                                         CONSTRAINT [CK_FieldType_IsPrimaryFilter] DEFAULT ((0)) NOT NULL,
    [LookupEditFormat]        NVARCHAR (250)                              NULL,
    [IsPartOfKey]             BIT                                         CONSTRAINT [DF_FieldType_IsPartOfKey] DEFAULT ((0)) NOT NULL,
    [ColumnOrder]             INT                                         CONSTRAINT [DF_FieldType_ColumnOrder] DEFAULT ((1)) NOT NULL,
    [ColumnWidth]             INT                                         NULL,
    [LookupObjectFieldTypeID] INT                                         NULL,
    [AllowMultipleValues]     BIT                                         CONSTRAINT [DF_FieldType_AllowMultipleValues] DEFAULT ((0)) NOT NULL,
    [EffectiveStartDate]      DATETIME2 (0) GENERATED ALWAYS AS ROW START NOT NULL,
    [EffectiveEndDate]        DATETIME2 (0) GENERATED ALWAYS AS ROW END   NOT NULL,
    CONSTRAINT [PK_FieldTypeNew] PRIMARY KEY CLUSTERED ([ID] ASC),
    PERIOD FOR SYSTEM_TIME ([EffectiveStartDate], [EffectiveEndDate])
)
WITH (SYSTEM_VERSIONING = ON (HISTORY_TABLE=[dbo].[FieldType_History], DATA_CONSISTENCY_CHECK=ON));








GO



GO


CREATE TRIGGER [dbo].[FieldType_AfterUpsert]
   ON  [dbo].[FieldType] 
   AFTER INSERT, UPDATE
AS 
	UPDATE	F
	set		F.FormattedValue = utility.GetFormattedFieldLookupValueWithMultiple(FT.Type, FT.LookupDisplayFormat, FT.LookupObjectType, FT.LookupObjectID, F.Value, FT.AllowMultipleValues)
	FROM	Field F
			inner join inserted FT on FT.ID = F.FieldTypeID

GO
CREATE NONCLUSTERED INDEX [IX_FieldType_Object-ObjectID]
    ON [dbo].[FieldType]([Object] ASC, [ObjectID] ASC);

