CREATE TABLE [dbo].[BusinessTransformationRule] (
    [ID]             INT           IDENTITY (1, 1) NOT NULL,
    [FocalObjectID]  INT           NOT NULL,
    [FocalObject]    VARCHAR (50)  NOT NULL,
    [SourceObjectID] INT           NOT NULL,
    [SourceObject]   VARCHAR (50)  NOT NULL,
    [TargetObjectID] INT           NOT NULL,
    [TargetObject]   VARCHAR (50)  NOT NULL,
    [Transformation] VARCHAR (MAX) NOT NULL,
    CONSTRAINT [PK_BusinessTransformationRule] PRIMARY KEY NONCLUSTERED ([ID] ASC)
);


GO
CREATE CLUSTERED INDEX [CIX_BusinessTransformationRule]
    ON [dbo].[BusinessTransformationRule]([FocalObject] ASC, [FocalObjectID] ASC, [SourceObject] ASC, [SourceObjectID] ASC, [TargetObject] ASC, [TargetObjectID] ASC);

