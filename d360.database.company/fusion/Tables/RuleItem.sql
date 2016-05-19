CREATE TABLE [fusion].[RuleItem](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[RuleID] [int] NOT NULL,
	[FusionAttributeID] [int] NULL,
	CONSTRAINT [FK_FusionRuleItem_FusionRule] FOREIGN KEY([RuleID])
		REFERENCES [fusion].[Rule] ([ID])
		ON DELETE CASCADE,
	CONSTRAINT [PK_RuleItem] PRIMARY KEY NONCLUSTERED 
	(
		[ID] ASC
	)
)