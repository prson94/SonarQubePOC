CREATE TABLE [dbo].[FieldTypeFilteredLookupDefinition] (
    [ID]          INT          IDENTITY (1, 1) NOT NULL,
    [FieldTypeID] INT          NOT NULL,
    [Object]      VARCHAR (50) NOT NULL,
    [ObjectID]    INT          NOT NULL,
    [HideHeader]  BIT          CONSTRAINT [DF_FieldTypeFilteredLookupDefinition_HideHeader] DEFAULT ((1)) NOT NULL,
    [HideFooter]  BIT          CONSTRAINT [DF_FieldTypeFilteredLookupDefinition_HideFooter] DEFAULT ((1)) NOT NULL,
    PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [FK_FieldTypeFilteredLookupDefinition_FieldType] FOREIGN KEY ([FieldTypeID]) REFERENCES [dbo].[FieldType] ([ID]) ON DELETE CASCADE
);

