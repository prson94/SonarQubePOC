CREATE TABLE [metrics].[Group] (
    [ID]                 INT             IDENTITY (1, 1) NOT NULL,
    [ParentID]           INT             NULL,
    [Name]               NVARCHAR (250)  NOT NULL,
    [Description]        NVARCHAR (4000) NULL,
    [Weight]             DECIMAL (5, 3)  NOT NULL,
    [EffectiveStartDate] DATE            CONSTRAINT [DF_MetricGroup_EffectiveStartDate] DEFAULT (getutcdate()) NOT NULL,
    [EffectiveEndDate]   DATE            NULL,
    [CreatedOn]          DATETIME        NULL,
    [CreatedBy]          INT             NULL,
    [UpdatedOn]          DATETIME        NULL,
    [UpdatedBy]          INT             NULL,
    [SourceID]           NVARCHAR (500)  NULL,
    CONSTRAINT [PK_MetricGroup] PRIMARY KEY NONCLUSTERED ([ID] ASC)
);

