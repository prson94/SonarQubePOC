CREATE TABLE [dbo].[AssetDataQualityImplementation_History] (
    [ID]                 BIGINT         NOT NULL,
    [AssetID]            BIGINT         NOT NULL,
    [State]              SMALLINT       NOT NULL,
    [Name]               NVARCHAR (250) NULL,
    [SourceID]           VARCHAR (2500) NULL,
    [SourceUri]          VARCHAR (2500) NULL,
    [CreatedOn]          DATETIME       NULL,
    [CreatedBy]          INT            NULL,
    [UpdatedOn]          DATETIME       NULL,
    [UpdatedBy]          INT            NULL,
    [EffectiveStartDate] DATETIME2 (0)  NOT NULL,
    [EffectiveEndDate]   DATETIME2 (0)  NOT NULL
);


GO
CREATE CLUSTERED INDEX [ix_AssetDataQualityImplementation_History]
    ON [dbo].[AssetDataQualityImplementation_History]([EffectiveEndDate] ASC, [EffectiveStartDate] ASC) WITH (DATA_COMPRESSION = PAGE);

