CREATE TABLE [dbo].[AssetDataQualityDimension_History] (
    [ID]                 INT            NOT NULL,
    [Name]               NVARCHAR (250) NOT NULL,
    [Description]        NVARCHAR (MAX) NULL,
    [State]              SMALLINT       NOT NULL,
    [SourceID]           INT            NULL,
    [CreatedOn]          DATETIME       NULL,
    [CreatedBy]          INT            NULL,
    [UpdatedOn]          DATETIME       NULL,
    [UpdatedBy]          INT            NULL,
    [EffectiveStartDate] DATETIME2 (0)  NOT NULL,
    [EffectiveEndDate]   DATETIME2 (0)  NOT NULL
);


GO
CREATE CLUSTERED INDEX [ix_AssetDataQualityDimension_History]
    ON [dbo].[AssetDataQualityDimension_History]([EffectiveEndDate] ASC, [EffectiveStartDate] ASC) WITH (DATA_COMPRESSION = PAGE);

