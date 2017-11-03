CREATE TABLE [dbo].[ResponsibilityTypeRelationItem_History] (
    [RuleID]               INT           NOT NULL,
    [ResponsibilityTypeID] INT           NOT NULL,
    [AssetID]              BIGINT        NOT NULL,
    [SecurityAsset]        CHAR (1)      NOT NULL,
    [SecurityAssetID]      INT           NOT NULL,
    [Overriden]            BIT           NOT NULL,
    [EffectiveStartDate]   DATETIME2 (0) NOT NULL,
    [EffectiveEndDate]     DATETIME2 (0) NOT NULL,
    [OverrideItemID]       BIGINT        NULL
);


GO
CREATE CLUSTERED INDEX [ix_ResponsibilityTypeRelationItem_History]
    ON [dbo].[ResponsibilityTypeRelationItem_History]([EffectiveEndDate] ASC, [EffectiveStartDate] ASC) WITH (DATA_COMPRESSION = PAGE);

