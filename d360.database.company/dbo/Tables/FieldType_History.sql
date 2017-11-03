CREATE TABLE [dbo].[FieldType_History] (
    [ID]                      INT             NOT NULL,
    [Name]                    NVARCHAR (250)  NOT NULL,
    [FriendlyName]            NVARCHAR (250)  NOT NULL,
    [Description]             NVARCHAR (4000) NULL,
    [DisplayDescription]      NVARCHAR (4000) NULL,
    [FormDescription]         NVARCHAR (4000) NULL,
    [Type]                    VARCHAR (25)    NOT NULL,
    [LookupObjectType]        VARCHAR (25)    NULL,
    [LookupObjectID]          INT             NULL,
    [LookupDisplayFormat]     NVARCHAR (250)  NULL,
    [MinimumLength]           INT             NULL,
    [MaximumLength]           INT             NULL,
    [Length]                  INT             NULL,
    [Pattern]                 VARCHAR (1000)  NULL,
    [Object]                  VARCHAR (50)    NOT NULL,
    [ObjectID]                INT             NOT NULL,
    [SortOrder]               INT             NOT NULL,
    [IsRequired]              BIT             NOT NULL,
    [IsListable]              BIT             NOT NULL,
    [ValidationDescription]   NVARCHAR (500)  NULL,
    [Category]                NVARCHAR (250)  NULL,
    [IsDisplayable]           BIT             NOT NULL,
    [IsEditable]              BIT             NOT NULL,
    [DefaultValue]            NVARCHAR (MAX)  NULL,
    [DefaultFormattedValue]   NVARCHAR (MAX)  NULL,
    [AllowAllValue]           BIT             NOT NULL,
    [AllowAllLabel]           NVARCHAR (250)  NULL,
    [IsPrimaryFilter]         BIT             NOT NULL,
    [LookupEditFormat]        NVARCHAR (250)  NULL,
    [IsPartOfKey]             BIT             NOT NULL,
    [ColumnOrder]             INT             NOT NULL,
    [ColumnWidth]             INT             NULL,
    [LookupObjectFieldTypeID] INT             NULL,
    [AllowMultipleValues]     BIT             NOT NULL,
    [EffectiveStartDate]      DATETIME2 (0)   NOT NULL,
    [EffectiveEndDate]        DATETIME2 (0)   NOT NULL
);


GO
CREATE CLUSTERED INDEX [ix_FieldTypeNew_History]
    ON [dbo].[FieldType_History]([EffectiveEndDate] ASC, [EffectiveStartDate] ASC) WITH (DATA_COMPRESSION = PAGE);

