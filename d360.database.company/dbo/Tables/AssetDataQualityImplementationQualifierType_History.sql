CREATE TABLE [dbo].[AssetDataQualityImplementationQualifierType_History] (
    [ID]                               BIGINT         NOT NULL,
    [AssetDataQualityImplementationID] BIGINT         NOT NULL,
    [Name]                             NVARCHAR (250) NOT NULL,
    [Order]                            INT            NOT NULL,
    [SourceID]                         INT            NOT NULL,
    [ResolutionAssetTypeID]            INT            NULL,
    [ResolutionFieldTypeID]            INT            NULL,
    [CreatedOn]                        DATETIME       NULL,
    [CreatedBy]                        INT            NULL,
    [UpdatedOn]                        DATETIME       NULL,
    [UpdatedBy]                        INT            NULL,
    [EffectiveStartDate]               DATETIME2 (0)  NOT NULL,
    [EffectiveEndDate]                 DATETIME2 (0)  NOT NULL
);


GO
CREATE CLUSTERED INDEX [ix_AssetDataQualityImplementationQualifierType_History]
    ON [dbo].[AssetDataQualityImplementationQualifierType_History]([EffectiveEndDate] ASC, [EffectiveStartDate] ASC) WITH (DATA_COMPRESSION = PAGE);

