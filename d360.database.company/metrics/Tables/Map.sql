CREATE TABLE [metrics].[Map] (
    [ID]                 BIGINT         IDENTITY (1, 1) NOT NULL,
    [GroupID]            INT            NOT NULL,
    [ItemID]             BIGINT         NOT NULL,
    [Object]             VARCHAR (50)   NULL,
    [ObjectID]           INT            NULL,
    [Weight]             DECIMAL (5, 3) NOT NULL,
    [EffectiveStartDate] DATE           CONSTRAINT [DF_MetricMap_EffectiveStartDate] DEFAULT (getutcdate()) NOT NULL,
    [EffectiveEndDate]   DATE           NULL,
    [CreatedOn]          DATETIME       NULL,
    [CreatedBy]          INT            NULL,
    [UpdatedOn]          DATETIME       NULL,
    [UpdatedBy]          INT            NULL,
    CONSTRAINT [PK_MetricMap] PRIMARY KEY NONCLUSTERED ([ID] ASC),
    CONSTRAINT [FK_MetricMap_MetricGroup] FOREIGN KEY ([GroupID]) REFERENCES [metrics].[Group] ([ID]),
    CONSTRAINT [FK_MetricMap_MetricItem] FOREIGN KEY ([ItemID]) REFERENCES [metrics].[Item] ([ID])
);

