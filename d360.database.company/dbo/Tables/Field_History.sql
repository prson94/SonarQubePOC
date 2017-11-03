CREATE TABLE [dbo].[Field_History] (
    [AssetID]            BIGINT         NULL,
    [ObjectType]         VARCHAR (50)   NOT NULL,
    [ObjectID]           INT            NOT NULL,
    [FieldTypeID]        INT            NOT NULL,
    [Value]              NVARCHAR (MAX) NULL,
    [FormattedValue]     NVARCHAR (MAX) NULL,
    [EffectiveStartDate] DATETIME2 (0)  NOT NULL,
    [EffectiveEndDate]   DATETIME2 (0)  NOT NULL
);


GO
CREATE CLUSTERED INDEX [ix_FieldNew_History]
    ON [dbo].[Field_History]([EffectiveEndDate] ASC, [EffectiveStartDate] ASC) WITH (DATA_COMPRESSION = PAGE);

