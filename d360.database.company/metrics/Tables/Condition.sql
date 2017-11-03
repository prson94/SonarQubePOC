CREATE TABLE [metrics].[Condition] (
    [MapID]       BIGINT      IDENTITY (1, 1) NOT NULL,
    [FieldTypeID] INT         NOT NULL,
    [AndOr]       VARCHAR (1) NOT NULL,
    CONSTRAINT [PK_MetricCondition] PRIMARY KEY NONCLUSTERED ([MapID] ASC, [FieldTypeID] ASC),
    CONSTRAINT [FK_MetricCondition_MetricMap] FOREIGN KEY ([MapID]) REFERENCES [metrics].[Map] ([ID])
);

