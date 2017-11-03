CREATE TABLE [dbo].[AssetDataQualityImplementation] (
    [ID]                 BIGINT                                      IDENTITY (1, 1) NOT NULL,
    [AssetID]            BIGINT                                      NOT NULL,
    [State]              SMALLINT                                    NOT NULL,
    [Name]               NVARCHAR (250)                              NULL,
    [SourceID]           VARCHAR (2500)                              NULL,
    [SourceUri]          VARCHAR (2500)                              NULL,
    [CreatedOn]          DATETIME                                    NULL,
    [CreatedBy]          INT                                         NULL,
    [UpdatedOn]          DATETIME                                    NULL,
    [UpdatedBy]          INT                                         NULL,
    [EffectiveStartDate] DATETIME2 (0) GENERATED ALWAYS AS ROW START NOT NULL,
    [EffectiveEndDate]   DATETIME2 (0) GENERATED ALWAYS AS ROW END   NOT NULL,
    CONSTRAINT [PK_AssetDataQualityImplementation] PRIMARY KEY NONCLUSTERED ([ID] ASC),
    PERIOD FOR SYSTEM_TIME ([EffectiveStartDate], [EffectiveEndDate])
)
WITH (SYSTEM_VERSIONING = ON (HISTORY_TABLE=[dbo].[AssetDataQualityImplementation_History], DATA_CONSISTENCY_CHECK=ON));


GO
CREATE NONCLUSTERED INDEX [CIX_AssetDataQualityImplementation]
    ON [dbo].[AssetDataQualityImplementation]([AssetID] ASC);

