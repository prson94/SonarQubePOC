CREATE TABLE [plugin].[Package] (
    [ID]          INT            IDENTITY (1, 1) NOT NULL,
    [Name]        NVARCHAR (250) NOT NULL,
    [Version]     VARCHAR (25)   NOT NULL,
    [Hash]        VARCHAR (500)  NOT NULL,
    [DateUpdated] DATETIME       DEFAULT (getutcdate()) NOT NULL,
    [Component]   VARCHAR (1)    NOT NULL,
    CONSTRAINT [PK_PluginPackage] PRIMARY KEY CLUSTERED ([ID] ASC)
);

