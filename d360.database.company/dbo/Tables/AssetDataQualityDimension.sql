CREATE TABLE [dbo].[AssetDataQualityDimension] (
    [ID]                 INT                                         IDENTITY (1, 1) NOT NULL,
    [Name]               NVARCHAR (250)                              NOT NULL,
    [Description]        NVARCHAR (MAX)                              NULL,
    [State]              SMALLINT                                    NOT NULL,
    [SourceID]           INT                                         NULL,
    [CreatedOn]          DATETIME                                    NULL,
    [CreatedBy]          INT                                         NULL,
    [UpdatedOn]          DATETIME                                    NULL,
    [UpdatedBy]          INT                                         NULL,
    [EffectiveStartDate] DATETIME2 (0) GENERATED ALWAYS AS ROW START NOT NULL,
    [EffectiveEndDate]   DATETIME2 (0) GENERATED ALWAYS AS ROW END   NOT NULL,
    CONSTRAINT [PK_AssetDataQualityDimension] PRIMARY KEY NONCLUSTERED ([ID] ASC),
    PERIOD FOR SYSTEM_TIME ([EffectiveStartDate], [EffectiveEndDate])
)
WITH (SYSTEM_VERSIONING = ON (HISTORY_TABLE=[dbo].[AssetDataQualityDimension_History], DATA_CONSISTENCY_CHECK=ON));

