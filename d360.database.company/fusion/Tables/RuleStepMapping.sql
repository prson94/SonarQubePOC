CREATE TABLE [fusion].[RuleStepMapping](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[RuleStepID] [int] NOT NULL,	
	[SourceFieldName] [nvarchar](250) NULL,
	[SourceFieldTypeID] [int] NOT NULL,
	[TargetFieldName] [nvarchar](250) NULL,
	[TargetFieldTypeID] [int] NOT NULL,
	[IsConstantValue] [bit] NOT NULL DEFAULT ((0)),
	[ConstantValue] [nvarchar](250) NULL,	
	 CONSTRAINT [PK_FusionRuleStepMapping] PRIMARY KEY NONCLUSTERED 
	(
		[ID] ASC
	),
	CONSTRAINT [FK_FusionRuleStepMapping_FusionRuleStep] FOREIGN KEY([RuleStepID])
		REFERENCES [fusion].[RuleStep] ([ID])
		ON DELETE CASCADE
)

GO