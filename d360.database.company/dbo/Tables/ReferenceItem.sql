CREATE TABLE [dbo].[ReferenceItem] (
    [ID]                  INT      IDENTITY (1, 1) NOT NULL,
    [ReferenceItemTypeID] INT      NOT NULL,
    [CreatedOn]           DATETIME NULL,
    [CreatedBy]           INT      NULL,
    [UpdatedOn]           DATETIME NULL,
    [UpdatedBy]           INT      NULL,
    [DisplayValue]        AS       ([utility].[GetFormattedFieldReferenceItemValue]([ID],[ReferenceItemTypeID])),
    CONSTRAINT [PK_ReferenceItem] PRIMARY KEY NONCLUSTERED ([ID] ASC),
    CONSTRAINT [FK_ReferenceItem_ReferenceItemType] FOREIGN KEY ([ReferenceItemTypeID]) REFERENCES [dbo].[ReferenceItemType] ([ID]) ON DELETE CASCADE
);

