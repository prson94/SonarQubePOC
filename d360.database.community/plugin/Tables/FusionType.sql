CREATE TABLE [plugin].[FusionType] (
    [ID]          INT             NOT NULL,
    [Name]        NVARCHAR (250)  NOT NULL,
    [Description] NVARCHAR (4000) NOT NULL,
    CONSTRAINT [PK_Plugin_FusionType] PRIMARY KEY CLUSTERED ([ID] ASC)
);

