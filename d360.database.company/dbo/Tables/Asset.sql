CREATE TABLE [dbo].[Asset] (
    [ID]                 BIGINT                                      IDENTITY (1, 1) NOT NULL,
    [AssetTypeID]        INT                                         NOT NULL,
    [State]              INT                                         CONSTRAINT [DF_Asset_State] DEFAULT ((1)) NOT NULL,
    [Object]             VARCHAR (50)                                NOT NULL,
    [ObjectID]           INT                                         NOT NULL,
    [SourceID]           NVARCHAR (500)                              NULL,
    [CreatedOn]          DATETIME                                    NULL,
    [CreatedBy]          INT                                         NULL,
    [UpdatedOn]          DATETIME                                    NULL,
    [UpdatedBy]          INT                                         NULL,
    [EffectiveStartDate] DATETIME2 (0) GENERATED ALWAYS AS ROW START NOT NULL,
    [EffectiveEndDate]   DATETIME2 (0) GENERATED ALWAYS AS ROW END   NOT NULL,
    [KeyHash]            VARCHAR (50)                                NULL,
    [FieldHash]          VARCHAR (50)                                NULL,
    CONSTRAINT [PK_Asset] PRIMARY KEY NONCLUSTERED ([ID] ASC),
    CONSTRAINT [FK_Asset_AssetType] FOREIGN KEY ([AssetTypeID]) REFERENCES [dbo].[AssetType] ([ID]),
    PERIOD FOR SYSTEM_TIME ([EffectiveStartDate], [EffectiveEndDate])
)
WITH (SYSTEM_VERSIONING = ON (HISTORY_TABLE=[dbo].[Asset_History], DATA_CONSISTENCY_CHECK=ON));


GO
CREATE NONCLUSTERED INDEX [IX_Asset_AssetTYpeID_Include]
    ON [dbo].[Asset]([AssetTypeID] ASC)
    INCLUDE([ID], [Object], [ObjectID]);


GO
CREATE NONCLUSTERED INDEX [IX_Asset_AssetType_KeyHash_Include]
    ON [dbo].[Asset]([AssetTypeID] ASC, [KeyHash] ASC)
    INCLUDE([ID]);


GO
CREATE NONCLUSTERED INDEX [IX_Asset_Object_ObjectID_Include]
    ON [dbo].[Asset]([Object] ASC, [ObjectID] ASC)
    INCLUDE([ID], [AssetTypeID]);


GO
CREATE NONCLUSTERED INDEX [IX_Asset_Object_Include]
    ON [dbo].[Asset]([Object] ASC)
    INCLUDE([ID], [AssetTypeID], [ObjectID]);

