CREATE TABLE [dbo].[ArtifactTypeExportTemplate] (
    [ID]             INT             IDENTITY (1, 1) NOT NULL,
    [ArtifactTypeID] INT             NOT NULL,
    [Name]           NVARCHAR (250)  NOT NULL,
    [Description]    NVARCHAR (1000) NULL,
    [IncludeFields]  NVARCHAR (MAX)  NULL,
    [ExportViewType] INT             DEFAULT ((0)) NOT NULL,
    [IncludeUrl]     BIT             DEFAULT ((1)) NOT NULL,
    [IncludeParent]  BIT             DEFAULT ((1)) NOT NULL,
    [UpdatedBy]      INT             NULL,
    [UpdatedOn]      DATETIME        NULL,
    [UsageNotes]     NVARCHAR (MAX)  NULL,
    [TemplateFile]   VARBINARY (MAX) NULL,
    CONSTRAINT [PK_ArtifactTypeExportTemplate] PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [FK_ArtifactTypeExportTemplate_ArtifactType] FOREIGN KEY ([ArtifactTypeID]) REFERENCES [dbo].[ArtifactType] ([ID])
);

