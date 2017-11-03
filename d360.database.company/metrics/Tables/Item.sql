CREATE TABLE [metrics].[Item] (
    [ID]                 BIGINT          IDENTITY (1, 1) NOT NULL,
    [Name]               NVARCHAR (250)  NOT NULL,
    [Description]        NVARCHAR (4000) NULL,
    [EffectiveStartDate] DATE            CONSTRAINT [DF_MetricItem_EffectiveStartDate] DEFAULT (getutcdate()) NOT NULL,
    [EffectiveEndDate]   DATE            NULL,
    [CreatedOn]          DATETIME        NULL,
    [CreatedBy]          INT             NULL,
    [UpdatedOn]          DATETIME        NULL,
    [UpdatedBy]          INT             NULL,
    [SourceID]           NVARCHAR (500)  NULL,
    CONSTRAINT [PK_MetricItem] PRIMARY KEY NONCLUSTERED ([ID] ASC)
);

