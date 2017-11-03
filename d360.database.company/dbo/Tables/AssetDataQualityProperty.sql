CREATE TABLE [dbo].[AssetDataQualityProperty] (
    [ID]                          BIGINT                                      NOT NULL,
    [AssetDataQualityDimensionID] INT                                         NULL,
    [Threshold]                   DECIMAL (4, 3)                              NULL,
    [EffectiveStartDate]          DATETIME2 (0) GENERATED ALWAYS AS ROW START NOT NULL,
    [EffectiveEndDate]            DATETIME2 (0) GENERATED ALWAYS AS ROW END   NOT NULL,
    CONSTRAINT [PK_AssetDataQualityProperty] PRIMARY KEY NONCLUSTERED ([ID] ASC),
    CONSTRAINT [FK_AssetCalculated_AssetDataQualityDimension] FOREIGN KEY ([AssetDataQualityDimensionID]) REFERENCES [dbo].[AssetDataQualityDimension] ([ID]),
    CONSTRAINT [FK_AssetDataQualityProperty_Asset] FOREIGN KEY ([ID]) REFERENCES [dbo].[Asset] ([ID]),
    PERIOD FOR SYSTEM_TIME ([EffectiveStartDate], [EffectiveEndDate])
)
WITH (SYSTEM_VERSIONING = ON (HISTORY_TABLE=[dbo].[AssetDataQualityProperty_History], DATA_CONSISTENCY_CHECK=ON));

