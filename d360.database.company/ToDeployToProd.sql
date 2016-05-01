--Pappas:  Added these on 04/27/16
ALTER TABLE [dbo].[FusionAttributePromotionRuleItem] DROP CONSTRAINT [PK_FusionAttributePromotionRuleItem]
GO
ALTER TABLE [dbo].[FusionAttributePromotionRuleItem] ADD  CONSTRAINT [PK_FusionAttributePromotionRuleItem] PRIMARY KEY NONCLUSTERED  ( [ID] ASC )
GO
CREATE CLUSTERED INDEX CIX_FusionAttributePromotionRuleItem ON dbo.FusionAttributePromotionRuleItem ( FusionAttributePromotionRuleID ASC )
GO

delete FusionAttributePromotionRuleItem where FusionAttributePromotionRuleID not in (select ID from FusionAttributePromotionRule)
go
ALTER TABLE [dbo].[FusionAttributePromotionRuleItem]  WITH CHECK ADD  CONSTRAINT [FK_FusionAttributePromotionRuleItem_FusionAttributePromotionRule] FOREIGN KEY([FusionAttributePromotionRuleID]) REFERENCES [dbo].[FusionAttributePromotionRule] ([ID]) ON DELETE CASCADE
GO
ALTER TABLE [dbo].[FusionAttributePromotionRuleItem] CHECK CONSTRAINT [FK_FusionAttributePromotionRuleItem_FusionAttributePromotionRule]
GO


ALTER TABLE [dbo].[FusionAttributePromotionRuleMapping] DROP CONSTRAINT [PK_FusionAttributePromotionRuleMapping]
GO
ALTER TABLE [dbo].[FusionAttributePromotionRuleMapping] ADD  CONSTRAINT [PK_FusionAttributePromotionRuleMapping] PRIMARY KEY NONCLUSTERED  ( [ID] ASC )
GO
CREATE CLUSTERED INDEX CIX_FusionAttributePromotionRuleMapping ON dbo.FusionAttributePromotionRuleMapping ( FusionAttributePromotionRuleID ASC )
GO

delete FusionAttributePromotionRuleMapping where FusionAttributePromotionRuleID not in (select ID from FusionAttributePromotionRule)
go
ALTER TABLE [dbo].[FusionAttributePromotionRuleMapping]  WITH CHECK ADD  CONSTRAINT [FK_FusionAttributePromotionRuleMapping_FusionAttributePromotionRule] FOREIGN KEY([FusionAttributePromotionRuleID]) REFERENCES [dbo].[FusionAttributePromotionRule] ([ID]) ON DELETE CASCADE
GO
ALTER TABLE [dbo].[FusionAttributePromotionRuleMapping] CHECK CONSTRAINT [FK_FusionAttributePromotionRuleMapping_FusionAttributePromotionRule]
GO

ALTER TABLE [dbo].[FusionAttributeOwnerRuleItem] DROP CONSTRAINT [PK_FusionAttributeOwnerRuleItem]
GO
ALTER TABLE [dbo].[FusionAttributeOwnerRuleItem] ADD  CONSTRAINT [PK_FusionAttributeOwnerRuleItem] PRIMARY KEY NONCLUSTERED  ( [ID] ASC )
GO
CREATE CLUSTERED INDEX CIX_FusionAttributeOwnerRuleItem ON dbo.FusionAttributeOwnerRuleItem ( [FusionAttributeOwnerRuleID] ASC )
GO

delete FusionAttributeOwnerRuleItem where FusionAttributeOwnerRuleID not in (select ID from FusionAttributeOwnerRule)
go
ALTER TABLE [dbo].[FusionAttributeOwnerRuleItem]  WITH CHECK ADD  CONSTRAINT [FK_FusionAttributeOwnerRuleItem_FusionAttributeOwnerRule] FOREIGN KEY([FusionAttributeOwnerRuleID]) REFERENCES [dbo].[FusionAttributeOwnerRule] ([ID]) ON DELETE CASCADE
GO
ALTER TABLE [dbo].[FusionAttributeOwnerRuleItem] CHECK CONSTRAINT [FK_FusionAttributeOwnerRuleItem_FusionAttributeOwnerRule]
GO

ALTER TABLE [dbo].[FusionAttributeOwnerRule] DROP CONSTRAINT [FK_FusionAttributeOwnerRule_Fusion]
GO
ALTER TABLE [dbo].[FusionAttributePromotionRule] DROP CONSTRAINT [FK_FusionAttributePromotionRule_Fusion]
GO
ALTER TABLE [dbo].[FusionAttributePromotion] DROP CONSTRAINT [FK_FusionAttributePromotion_FusionAttribute]
GO

ALTER TABLE [dbo].[FusionAttribute] DROP CONSTRAINT [PK_FusionAttribute]
GO
ALTER TABLE [dbo].[FusionAttribute] ADD  CONSTRAINT [PK_FusionAttribute] PRIMARY KEY NONCLUSTERED ( [ID] ASC )
GO

ALTER TABLE [dbo].[FusionAttributeOwnerRule]  WITH CHECK ADD  CONSTRAINT [FK_FusionAttributeOwnerRule_Fusion] FOREIGN KEY([FusionID]) REFERENCES [dbo].[Fusion] ([ID]) ON DELETE CASCADE
GO
ALTER TABLE [dbo].[FusionAttributeOwnerRule] CHECK CONSTRAINT [FK_FusionAttributeOwnerRule_Fusion]
GO
ALTER TABLE [dbo].[FusionAttributePromotionRule]  WITH CHECK ADD  CONSTRAINT [FK_FusionAttributePromotionRule_Fusion] FOREIGN KEY([FusionID]) REFERENCES [dbo].[Fusion] ([ID])
GO
ALTER TABLE [dbo].[FusionAttributePromotionRule] CHECK CONSTRAINT [FK_FusionAttributePromotionRule_Fusion]
GO
ALTER TABLE [dbo].[FusionAttributePromotion]  WITH CHECK ADD  CONSTRAINT [FK_FusionAttributePromotion_FusionAttribute] FOREIGN KEY([FusionAttributeID]) REFERENCES [dbo].[FusionAttribute] ([ID]) ON DELETE CASCADE
GO
ALTER TABLE [dbo].[FusionAttributePromotion] CHECK CONSTRAINT [FK_FusionAttributePromotion_FusionAttribute]
GO

DROP INDEX [IX_FusionAttribute_FusionID-FusionAttributeTypeID-ParentID] ON [dbo].[FusionAttribute]
GO
CREATE CLUSTERED INDEX [CIX_FusionAttribute] ON [dbo].[FusionAttribute] ( [FusionID] ASC, [FusionAttributeTypeID] ASC, [ParentID] ASC )
GO
--Pappas:  Added above on 04/27/16

