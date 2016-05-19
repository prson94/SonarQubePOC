CREATE TABLE [fusion].[RuleStepSetting](		
	[RuleStepID] [int] NOT NULL,
	[Name] [nvarchar](100) NOT NULL,
	[Value] [nvarchar](250) NULL, 
	CONSTRAINT [FK_FusionRuleStepSetting_FusionRuleStep] FOREIGN KEY([RuleStepID])
		REFERENCES [fusion].[RuleStep] ([ID])
		ON DELETE CASCADE,
	CONSTRAINT [PK_FusionRuleStepSetting] PRIMARY KEY([RuleStepID],[Name])
)