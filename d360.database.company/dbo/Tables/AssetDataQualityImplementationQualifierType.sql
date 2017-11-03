CREATE TABLE [dbo].[AssetDataQualityImplementationQualifierType] (
    [ID]                               BIGINT                                      IDENTITY (1, 1) NOT NULL,
    [AssetDataQualityImplementationID] BIGINT                                      NOT NULL,
    [Name]                             NVARCHAR (250)                              NOT NULL,
    [Order]                            INT                                         NOT NULL,
    [SourceID]                         INT                                         NOT NULL,
    [ResolutionAssetTypeID]            INT                                         NULL,
    [ResolutionFieldTypeID]            INT                                         NULL,
    [CreatedOn]                        DATETIME                                    NULL,
    [CreatedBy]                        INT                                         NULL,
    [UpdatedOn]                        DATETIME                                    NULL,
    [UpdatedBy]                        INT                                         NULL,
    [EffectiveStartDate]               DATETIME2 (0) GENERATED ALWAYS AS ROW START NOT NULL,
    [EffectiveEndDate]                 DATETIME2 (0) GENERATED ALWAYS AS ROW END   NOT NULL,
    CONSTRAINT [PK_AssetDataQualityImplementationQualifierType] PRIMARY KEY NONCLUSTERED ([ID] ASC),
    CONSTRAINT [FK_AssetDataQualityImplementationQualifierType_AssetDataQualityImplementation] FOREIGN KEY ([AssetDataQualityImplementationID]) REFERENCES [dbo].[AssetDataQualityImplementation] ([ID]) ON DELETE CASCADE,
    PERIOD FOR SYSTEM_TIME ([EffectiveStartDate], [EffectiveEndDate])
)
WITH (SYSTEM_VERSIONING = ON (HISTORY_TABLE=[dbo].[AssetDataQualityImplementationQualifierType_History], DATA_CONSISTENCY_CHECK=ON));


GO
CREATE NONCLUSTERED INDEX [CIX_AssetDataQualityImplementationQualifierType]
    ON [dbo].[AssetDataQualityImplementationQualifierType]([AssetDataQualityImplementationID] ASC);

