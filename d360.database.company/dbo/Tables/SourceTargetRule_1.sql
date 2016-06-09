CREATE TABLE [dbo].[SourceTargetRule] (
    [ID]             INT           IDENTITY (1, 1) NOT NULL,
    [FocalObjectID]  INT           NOT NULL,
    [FocalObject]    VARCHAR (150) NOT NULL,
    [SourceObjectID] INT           NOT NULL,
    [SourceObject]   VARCHAR (150) NOT NULL,
    [TargetObjectID] INT           NOT NULL,
    [TargetObject]   VARCHAR (150) NOT NULL,
    [Transformation] VARCHAR (500) NULL,
    [Sequence]       INT           CONSTRAINT [DF_SourceTargetRule_Sequence] DEFAULT ((1)) NOT NULL,
    CONSTRAINT [PK_SourceTargetRule] PRIMARY KEY NONCLUSTERED ([ID] ASC)
);






GO
CREATE CLUSTERED INDEX [CIX_SourceTargetRule]
    ON [dbo].[SourceTargetRule]([FocalObject] ASC, [FocalObjectID] ASC, [SourceObject] ASC, [SourceObjectID] ASC, [TargetObject] ASC, [TargetObjectID] ASC);

