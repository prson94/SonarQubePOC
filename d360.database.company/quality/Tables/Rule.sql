CREATE TABLE [quality].[Rule] (
    [ID]                 INT            IDENTITY (1, 1) NOT NULL,
    [Name]               NVARCHAR (250) NOT NULL,
    [Definition]         NVARCHAR (MAX) NULL,
    [Status]             INT            NULL,
    [QualityDimensionID] INT            NULL,
    [Threshold]          DECIMAL (3, 3) NOT NULL,
    [WhatIsWrong]        NVARCHAR (MAX) NULL,
    [WhatIsRight]        NVARCHAR (MAX) NULL,
    [HowToMeasure]       NVARCHAR (MAX) NULL,
    [HowToResolve]       NVARCHAR (MAX) NULL,
    [CreatedOn]          DATETIME       NULL,
    [CreatedBy]          INT            NULL,
    [UpdatedOn]          DATETIME       NULL,
    [UpdatedBy]          INT            NULL,
    CONSTRAINT [PK_QualityRule] PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [FK_QualityRule_QualityDimension] FOREIGN KEY ([QualityDimensionID]) REFERENCES [quality].[Dimension] ([ID])
);

