CREATE TABLE [metrics].[Condition](
	[MapID] [bigint] NOT NULL,
	[FieldTypeID] [int] NOT NULL,
	[AndOr] [varchar](1) NOT NULL,
	[Operator] [varchar](10) NOT NULL,
	[Value] [nvarchar](max) NULL,
 CONSTRAINT [PK_MetricCondition] PRIMARY KEY NONCLUSTERED 
(
	[MapID] ASC,
	[FieldTypeID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)
)
GO

ALTER TABLE [metrics].[Condition]  WITH CHECK ADD  CONSTRAINT [FK_MetricCondition_MetricMap] FOREIGN KEY([MapID])
REFERENCES [metrics].[Map] ([ID])
GO

ALTER TABLE [metrics].[Condition] CHECK CONSTRAINT [FK_MetricCondition_MetricMap]
GO