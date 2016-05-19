CREATE TABLE [fusion].[RulePromotion](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[FusionAttributeID] [int] NOT NULL,
	[ObjectType] [varchar](25) NOT NULL,
	[ObjectID] [int] NOT NULL,
	[RuleID] [int] NOT NULL,
	[RuleStepID] [int] NULL,
	[ObjectTypeID] [int] NOT NULL DEFAULT ((-1)),
	CONSTRAINT [PK_FusionRulePromotion] PRIMARY KEY CLUSTERED 
	(
		[ID] ASC
	),
	CONSTRAINT [FK_FusionRulePromotion_FusionAttribute] FOREIGN KEY([FusionAttributeID])
		REFERENCES [dbo].[FusionAttribute] ([ID])
		ON DELETE CASCADE,
	CONSTRAINT [FK_FusionRulePromotion_FusionRule] FOREIGN KEY([RuleID])
		REFERENCES [fusion].[Rule] ([ID])
		ON DELETE CASCADE,
	CONSTRAINT [FK_FusionRulePromotion_FusionRuleStep] FOREIGN KEY([RuleStepID])
		REFERENCES [fusion].[RuleStep] ([ID])		
)