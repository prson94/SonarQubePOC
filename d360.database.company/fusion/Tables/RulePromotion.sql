CREATE TABLE [fusion].[RulePromotion](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[FusionAttributeID] [int] NOT NULL,
	[ObjectType] [varchar](25) NOT NULL,
	[ObjectID] [int] NOT NULL,
	[RuleID] [int] NOT NULL,
	[RuleStepID] [int] NULL,
	[ObjectTypeID] [int] NOT NULL,
	[CreatedOn] [datetime] NOT NULL,
	[UpdatedOn] [datetime] NOT NULL,
 CONSTRAINT [PK_FusionRulePromotion] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)
)

GO

ALTER TABLE [fusion].[RulePromotion] ADD  DEFAULT ((-1)) FOR [ObjectTypeID]
GO

ALTER TABLE [fusion].[RulePromotion] ADD  CONSTRAINT [DF_RulePromotion_CreatedOn]  DEFAULT (getdate()) FOR [CreatedOn]
GO

ALTER TABLE [fusion].[RulePromotion] ADD  CONSTRAINT [DF_RulePromotion_UpdatedOn]  DEFAULT (getdate()) FOR [UpdatedOn]
GO

ALTER TABLE [fusion].[RulePromotion]  WITH CHECK ADD  CONSTRAINT [FK_FusionRulePromotion_FusionAttribute] FOREIGN KEY([FusionAttributeID])
REFERENCES [dbo].[FusionAttribute] ([ID])
ON DELETE CASCADE
GO

ALTER TABLE [fusion].[RulePromotion] CHECK CONSTRAINT [FK_FusionRulePromotion_FusionAttribute]
GO

ALTER TABLE [fusion].[RulePromotion]  WITH CHECK ADD  CONSTRAINT [FK_FusionRulePromotion_FusionRule] FOREIGN KEY([RuleID])
REFERENCES [fusion].[Rule] ([ID])
ON DELETE CASCADE
GO

ALTER TABLE [fusion].[RulePromotion] CHECK CONSTRAINT [FK_FusionRulePromotion_FusionRule]
GO

