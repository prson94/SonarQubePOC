CREATE TABLE [dbo].[AssetType_History] (
    [ID]                       INT            NOT NULL,
    [Name]                     NVARCHAR (250) NOT NULL,
    [Description]              NVARCHAR (MAX) NULL,
    [Class]                    SMALLINT       NOT NULL,
    [DisplayFormat]            NVARCHAR (250) NOT NULL,
    [State]                    SMALLINT       NOT NULL,
    [Hierarchical]             BIT            NOT NULL,
    [HierarchyPredicateID]     INT            NULL,
    [HierarchyIntersectTypeID] INT            NULL,
    [HierarchyMaximumDepth]    INT            NOT NULL,
    [Object]                   VARCHAR (50)   NOT NULL,
    [ObjectID]                 INT            NOT NULL,
    [CreatedOn]                DATETIME       NULL,
    [CreatedBy]                INT            NULL,
    [UpdatedOn]                DATETIME       NULL,
    [UpdatedBy]                INT            NULL,
    [EffectiveStartDate]       DATETIME2 (0)  NOT NULL,
    [EffectiveEndDate]         DATETIME2 (0)  NOT NULL
);


GO
CREATE CLUSTERED INDEX [ix_AssetType_History]
    ON [dbo].[AssetType_History]([EffectiveEndDate] ASC, [EffectiveStartDate] ASC) WITH (DATA_COMPRESSION = PAGE);

