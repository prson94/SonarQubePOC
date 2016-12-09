CREATE TABLE [cache].[ResponsibilityItem] (
    [ResponsibilityID]        INT            NOT NULL,
    [ResponsibilityTypeID]    INT            NOT NULL,
    [ResponsibilityType]      NVARCHAR (250) NULL,
    [AssigningItem]           VARCHAR (50)   NOT NULL,
    [AssigningItemID]         INT            NOT NULL,
    [Object]                  VARCHAR (50)   NOT NULL,
    [ObjectID]                INT            NOT NULL,
    [ResponsibleObject]       VARCHAR (50)   NOT NULL,
    [ResponsibleObjectID]     INT            NOT NULL,
    [ContextHash]             VARCHAR (50)   NOT NULL,
    [ResponsibilityTypeGroup] INT            NOT NULL,
    [Visible]                 BIT            CONSTRAINT [DF_CacheResponsibilityItem_Visible] DEFAULT ((1)) NOT NULL,
    [TargetResponsibilityID]  INT            NULL,
    CONSTRAINT [PK_CacheResponsibilityItem] PRIMARY KEY CLUSTERED ([ResponsibilityID] ASC, [AssigningItem] ASC, [AssigningItemID] ASC, [Object] ASC, [ObjectID] ASC, [ContextHash] ASC)
);








GO
CREATE NONCLUSTERED INDEX [IX_CacheResponsibilityItem_ResponsibleObject]
    ON [cache].[ResponsibilityItem]([ResponsibleObject] ASC, [ResponsibleObjectID] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_CacheResponsibilityItem_ResponsibilityTypeID__Object_ObjectID]
    ON [cache].[ResponsibilityItem]([ResponsibilityTypeID] ASC, [Object] ASC, [ObjectID] ASC);

