CREATE TABLE [dbo].[ResponsibilityTypeRelationItem] (
    [RuleID]               INT                                         NOT NULL,
    [ResponsibilityTypeID] INT                                         NOT NULL,
    [AssetID]              BIGINT                                      NOT NULL,
    [SecurityAsset]        CHAR (1)                                    NOT NULL,
    [SecurityAssetID]      INT                                         NOT NULL,
    [Overriden]            BIT                                         CONSTRAINT [DF_ResponsibilityTypeRelationItem_Overriden] DEFAULT ((0)) NOT NULL,
    [EffectiveStartDate]   DATETIME2 (0) GENERATED ALWAYS AS ROW START NOT NULL,
    [EffectiveEndDate]     DATETIME2 (0) GENERATED ALWAYS AS ROW END   NOT NULL,
    [OverrideItemID]       BIGINT                                      NULL,
    CONSTRAINT [PK_ResponsibilityTypeRelationItem] PRIMARY KEY NONCLUSTERED ([RuleID] ASC, [ResponsibilityTypeID] ASC, [AssetID] ASC, [SecurityAsset] ASC, [SecurityAssetID] ASC),
    PERIOD FOR SYSTEM_TIME ([EffectiveStartDate], [EffectiveEndDate])
)
WITH (SYSTEM_VERSIONING = ON (HISTORY_TABLE=[dbo].[ResponsibilityTypeRelationItem_History], DATA_CONSISTENCY_CHECK=ON));

