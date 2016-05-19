CREATE TABLE [fusion].[RuleStep](
	[ID] INT IDENTITY (1, 1) NOT NULL PRIMARY KEY,
	[RuleID] [int] NOT NULL,
	[Step] [int] NOT NULL,
	[Action] [varchar](25) NOT NULL,
	[Description] [nvarchar](4000) NULL, 
	CONSTRAINT [FK_FusionRuleStep_FusionRule] FOREIGN KEY([RuleID])
		REFERENCES [fusion].[Rule] ([ID])
		ON DELETE CASCADE
)

GO