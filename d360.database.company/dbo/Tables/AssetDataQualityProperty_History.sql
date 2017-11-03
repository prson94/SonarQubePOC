CREATE TABLE [dbo].[AssetDataQualityProperty_History] (
    [ID]                          BIGINT         NOT NULL,
    [AssetDataQualityDimensionID] INT            NULL,
    [Threshold]                   DECIMAL (4, 3) NULL,
    [EffectiveStartDate]          DATETIME2 (0)  NOT NULL,
    [EffectiveEndDate]            DATETIME2 (0)  NOT NULL
);


GO
CREATE CLUSTERED INDEX [ix_AssetDataQualityProperty_History]
    ON [dbo].[AssetDataQualityProperty_History]([EffectiveEndDate] ASC, [EffectiveStartDate] ASC) WITH (DATA_COMPRESSION = PAGE);

