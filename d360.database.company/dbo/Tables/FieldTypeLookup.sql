CREATE TABLE [dbo].[FieldTypeLookup] (
    [FieldTypeID] INT            NOT NULL,
    [HideHeader]  BIT            CONSTRAINT [DF_FieldTypeLookup_HideHeader] DEFAULT ((1)) NOT NULL,
    [HideFooter]  BIT            CONSTRAINT [DF_FieldTypeLookup_HideFooter] DEFAULT ((1)) NOT NULL,
	[HideFilter]  BIT			 CONSTRAINT [DF_FieldTypeLookup_HideFilter] DEFAULT ((1)) NOT NULL,
    [LookupType]  INT            NOT NULL,
    [Definition]  NVARCHAR (MAX) NOT NULL,
    CONSTRAINT [PK_FieldTypeLookup] PRIMARY KEY CLUSTERED ([FieldTypeID] ASC),
    CONSTRAINT [FK_FieldTypeLookup_FieldType] FOREIGN KEY ([FieldTypeID]) REFERENCES [dbo].[FieldType] ([ID]) ON DELETE CASCADE
);
GO

