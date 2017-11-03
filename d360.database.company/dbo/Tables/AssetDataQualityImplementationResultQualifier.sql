CREATE TABLE [dbo].[AssetDataQualityImplementationResultQualifier] (
    [AssetDataQualityImplementationQualifierTypeID] BIGINT          NOT NULL,
    [AssetDataQualityImplementationResultID]        BIGINT          NOT NULL,
    [Value]                                         NVARCHAR (1000) NOT NULL,
    [ResolvedAssetID]                               BIGINT          NULL,
    [EventNotificationSent]                         BIT             NOT NULL,
    CONSTRAINT [PK_AssetDataQualityImplementationResultQualifier] PRIMARY KEY NONCLUSTERED ([AssetDataQualityImplementationQualifierTypeID] ASC, [AssetDataQualityImplementationResultID] ASC),
    CONSTRAINT [FK_AssetDataQualityImplementationResultQualifier_AssetDataQualityImplementationQualifierType] FOREIGN KEY ([AssetDataQualityImplementationQualifierTypeID]) REFERENCES [dbo].[AssetDataQualityImplementationQualifierType] ([ID]),
    CONSTRAINT [FK_AssetDataQualityImplementationResultQualifier_AssetDataQualityImplementationResult] FOREIGN KEY ([AssetDataQualityImplementationResultID]) REFERENCES [dbo].[AssetDataQualityImplementationResult] ([ID])
);

