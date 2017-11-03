CREATE TABLE [dbo].[AssetDataQualityImplementationResultFusion] (
    [AssetDataQualityImplementationResultID] BIGINT         NOT NULL,
    [Value]                                  NVARCHAR (500) NOT NULL,
    [FusionAssetID]                          BIGINT         NULL,
    CONSTRAINT [PK_AssetDataQualityImplementationResultFusion] PRIMARY KEY NONCLUSTERED ([AssetDataQualityImplementationResultID] ASC, [Value] ASC)
);

