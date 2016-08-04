CREATE TABLE [dbo].[FieldTypeLookup] (
    [ID]          INT            IDENTITY (1, 1) NOT NULL,
    [FieldTypeID] INT            NOT NULL,
    [HideHeader]  BIT            CONSTRAINT [DF_FieldTypeLookup_HideHeader] DEFAULT ((1)) NOT NULL,
    [HideFooter]  BIT            CONSTRAINT [DF_FieldTypeLookup_HideFooter] DEFAULT ((1)) NOT NULL,
    [LookupType]  INT            NOT NULL,
    [Definition]  NVARCHAR (MAX) NOT NULL,
    PRIMARY KEY NONCLUSTERED ([ID] ASC),
    CONSTRAINT [CK_FieldTypeLookup_Definition] CHECK (isjson([Definition])>(0)),
    CONSTRAINT [FK_FieldTypeLookup_FieldType] FOREIGN KEY ([FieldTypeID]) REFERENCES [dbo].[FieldType] ([ID]) ON DELETE CASCADE
);

