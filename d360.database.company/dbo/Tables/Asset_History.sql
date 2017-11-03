CREATE TABLE [dbo].[Asset_History] (
    [ID]                 BIGINT         NOT NULL,
    [AssetTypeID]        INT            NOT NULL,
    [State]              INT            NOT NULL,
    [Object]             VARCHAR (50)   NOT NULL,
    [ObjectID]           INT            NOT NULL,
    [SourceID]           NVARCHAR (500) NULL,
    [CreatedOn]          DATETIME       NULL,
    [CreatedBy]          INT            NULL,
    [UpdatedOn]          DATETIME       NULL,
    [UpdatedBy]          INT            NULL,
    [EffectiveStartDate] DATETIME2 (0)  NOT NULL,
    [EffectiveEndDate]   DATETIME2 (0)  NOT NULL,
    [KeyHash]            VARCHAR (50)   NULL,
    [FieldHash]          VARCHAR (50)   NULL
);


GO
CREATE CLUSTERED INDEX [ix_Asset_History]
    ON [dbo].[Asset_History]([EffectiveEndDate] ASC, [EffectiveStartDate] ASC) WITH (DATA_COMPRESSION = PAGE);

