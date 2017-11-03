CREATE TABLE [dbo].[AssetType] (
    [ID]                       INT                                         IDENTITY (1, 1) NOT NULL,
    [Name]                     NVARCHAR (250)                              NOT NULL,
    [Description]              NVARCHAR (MAX)                              NULL,
    [Class]                    SMALLINT                                    CONSTRAINT [DF_AssetType_Class] DEFAULT ((1)) NOT NULL,
    [DisplayFormat]            NVARCHAR (250)                              CONSTRAINT [DF_AssetType_DisplayFormat] DEFAULT ('{ID}') NOT NULL,
    [State]                    SMALLINT                                    CONSTRAINT [DF_AssetType_State] DEFAULT ((1)) NOT NULL,
    [Hierarchical]             BIT                                         CONSTRAINT [DF_AssetType_Hierarchical] DEFAULT ((0)) NOT NULL,
    [HierarchyPredicateID]     INT                                         NULL,
    [HierarchyIntersectTypeID] INT                                         NULL,
    [HierarchyMaximumDepth]    INT                                         CONSTRAINT [DF_AssetType_HierarchyMaximumDepth] DEFAULT ((0)) NOT NULL,
    [Object]                   VARCHAR (50)                                NOT NULL,
    [ObjectID]                 INT                                         NOT NULL,
    [CreatedOn]                DATETIME                                    NULL,
    [CreatedBy]                INT                                         NULL,
    [UpdatedOn]                DATETIME                                    NULL,
    [UpdatedBy]                INT                                         NULL,
    [EffectiveStartDate]       DATETIME2 (0) GENERATED ALWAYS AS ROW START NOT NULL,
    [EffectiveEndDate]         DATETIME2 (0) GENERATED ALWAYS AS ROW END   NOT NULL,
    CONSTRAINT [PK_AssetType] PRIMARY KEY NONCLUSTERED ([ID] ASC),
    PERIOD FOR SYSTEM_TIME ([EffectiveStartDate], [EffectiveEndDate])
)
WITH (SYSTEM_VERSIONING = ON (HISTORY_TABLE=[dbo].[AssetType_History], DATA_CONSISTENCY_CHECK=ON));


GO
CREATE NONCLUSTERED INDEX [IX_AssetType_Object_ObjectID_Include]
    ON [dbo].[AssetType]([Object] ASC, [ObjectID] ASC)
    INCLUDE([ID]);

