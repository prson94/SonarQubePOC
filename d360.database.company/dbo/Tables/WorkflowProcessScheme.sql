CREATE TABLE [dbo].[WorkflowProcessScheme] (
    [Id]                     UNIQUEIDENTIFIER NOT NULL,
    [Scheme]                 NTEXT            NOT NULL,
    [DefiningParameters]     NTEXT            NOT NULL,
    [DefiningParametersHash] NVARCHAR (1024)  NOT NULL,
    [SchemeCode]             NVARCHAR (MAX)   NOT NULL,
    [IsObsolete]             BIT              DEFAULT ((0)) NOT NULL,
    [RootSchemeCode]         NVARCHAR (MAX)   NULL,
    [RootSchemeId]           UNIQUEIDENTIFIER NULL,
    [AllowedActivities]      NVARCHAR (MAX)   NULL,
    [StartingTransition]     NVARCHAR (MAX)   NULL,
    CONSTRAINT [PK_WorkflowProcessScheme] PRIMARY KEY CLUSTERED ([Id] ASC)
);

