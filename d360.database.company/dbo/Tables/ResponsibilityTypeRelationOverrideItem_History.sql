CREATE TABLE [dbo].[ResponsibilityTypeRelationOverrideItem_History] (
    [ID]                   BIGINT        NOT NULL,
    [ResponsibilityTypeID] INT           NOT NULL,
    [AssetID]              BIGINT        NOT NULL,
    [SecurityAsset]        CHAR (1)      NOT NULL,
    [SecurityAssetID]      INT           NOT NULL,
    [EffectiveStartDate]   DATETIME2 (0) NOT NULL,
    [EffectiveEndDate]     DATETIME2 (0) NOT NULL
);


GO
CREATE CLUSTERED INDEX [ix_ResponsibilityTypeRelationOverrideItem_History]
    ON [dbo].[ResponsibilityTypeRelationOverrideItem_History]([EffectiveEndDate] ASC, [EffectiveStartDate] ASC) WITH (DATA_COMPRESSION = PAGE);

