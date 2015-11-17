CREATE TABLE [plugin].[FusionAttributeType] (
    [ID]           INT            NOT NULL,
    [ParentID]     INT            NULL,
    [FusionTypeID] INT            NOT NULL,
    [Name]         NVARCHAR (250) NOT NULL,
    CONSTRAINT [PK_Plugin_FusionAttributeType] PRIMARY KEY CLUSTERED ([ID] ASC)
);

