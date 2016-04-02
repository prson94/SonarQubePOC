CREATE TABLE [dbo].[SourceTargetRule] (
    [ID]             INT           IDENTITY (1, 1) NOT NULL,
    [FocalObjectID]  INT           NOT NULL,
    [FocalObject]    VARCHAR (150) NOT NULL,
    [SourceObjectID] INT           NOT NULL,
    [SourceObject]   VARCHAR (150) NOT NULL,
    [TargetObjectID] INT           NOT NULL,
    [TargetObject]   VARCHAR (150) NOT NULL,
    [Transformation] VARCHAR (500) NULL,
    PRIMARY KEY CLUSTERED ([ID] ASC)
);

