CREATE TABLE [dbo].[ResponsibilityTypeRelation] (
    [ResponsibilityTypeID] INT          NOT NULL,
    [ObjectType]           VARCHAR (50) NOT NULL,
    [ObjectID]             INT          NOT NULL,
    [ReadObject]           BIT          CONSTRAINT [DF_ResponsibilityTypeRelation_ReadObject] DEFAULT ((1)) NOT NULL,
    [ReadAttributes]       BIT          CONSTRAINT [DF_ResponsibilityTypeRelation_ReadAttributes] DEFAULT ((1)) NOT NULL,
    [ReadAudit]            BIT          CONSTRAINT [DF_ResponsibilityTypeRelation_ReadAudit] DEFAULT ((1)) NOT NULL,
    [ReadDashboards]       BIT          CONSTRAINT [DF_ResponsibilityTypeRelation_ReadDashboards] DEFAULT ((1)) NOT NULL,
    [ReadRelationships]    BIT          CONSTRAINT [DF_ResponsibilityTypeRelation_ReadRelationships] DEFAULT ((1)) NOT NULL,
    [ReadSocial]           BIT          CONSTRAINT [DF_ResponsibilityTypeRelation_ReadSocial] DEFAULT ((1)) NOT NULL,
    [ModifyObject]         BIT          CONSTRAINT [DF_ResponsibilityTypeRelation_ModifyObject] DEFAULT ((0)) NOT NULL,
    [ModifyAttributes]     BIT          CONSTRAINT [DF_ResponsibilityTypeRelation_ModifyAttributes] DEFAULT ((0)) NOT NULL,
    [ModifyRelationships]  BIT          CONSTRAINT [DF_ResponsibilityTypeRelation_ModifyRelationships] DEFAULT ((0)) NOT NULL,
    [ModifySocial]         BIT          CONSTRAINT [DF_ResponsibilityTypeRelation_ModifySocial] DEFAULT ((0)) NOT NULL,
    [DeleteObject]         BIT          CONSTRAINT [DF_ResponsibilityTypeRelation_DeleteObject] DEFAULT ((0)) NOT NULL,
    [DeleteAttributes]     BIT          CONSTRAINT [DF_ResponsibilityTypeRelation_DeleteAttributes] DEFAULT ((0)) NOT NULL,
    [DeleteRelationships]  BIT          CONSTRAINT [DF_ResponsibilityTypeRelation_DeleteRelationships] DEFAULT ((0)) NOT NULL,
    [DeleteSocial]         BIT          CONSTRAINT [DF_ResponsibilityTypeRelation_DeleteSocial] DEFAULT ((0)) NOT NULL,
    CONSTRAINT [PK_ResponsibilityTypeRelation] PRIMARY KEY CLUSTERED ([ResponsibilityTypeID] ASC, [ObjectType] ASC, [ObjectID] ASC)
);




GO
CREATE NONCLUSTERED INDEX [IX_ResponsibilityTypeRelation_Object]
    ON [dbo].[ResponsibilityTypeRelation]([ObjectType] ASC, [ObjectID] ASC)
    INCLUDE([ResponsibilityTypeID]);

