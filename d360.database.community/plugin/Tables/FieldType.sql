CREATE TABLE [plugin].[FieldType] (
    [ID]           INT            IDENTITY (1, 1) NOT NULL,
    [Name]         NVARCHAR (250) NOT NULL,
    [FriendlyName] NVARCHAR (250) NOT NULL,
    [Type]         VARCHAR (25)   NOT NULL,
    CONSTRAINT [PK_Plugin_FieldType] PRIMARY KEY CLUSTERED ([ID] ASC)
);

