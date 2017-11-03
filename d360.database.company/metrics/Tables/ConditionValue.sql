CREATE TABLE [metrics].[ConditionValue] (
    [MapID]       BIGINT         IDENTITY (1, 1) NOT NULL,
    [FieldTypeID] INT            NOT NULL,
    [Value]       NVARCHAR (250) NOT NULL,
    CONSTRAINT [PK_MetricConditionValue] PRIMARY KEY NONCLUSTERED ([MapID] ASC, [FieldTypeID] ASC, [Value] ASC),
    CONSTRAINT [FK_MetricConditionValue_MetricCondition] FOREIGN KEY ([MapID], [FieldTypeID]) REFERENCES [metrics].[Condition] ([MapID], [FieldTypeID])
);

