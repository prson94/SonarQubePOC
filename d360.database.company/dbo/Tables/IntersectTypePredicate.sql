CREATE TABLE [dbo].[IntersectTypePredicate](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[PredicateID] [int] NOT NULL,
	[IntersectTypeID] [int] NOT NULL
)

GO

ALTER TABLE [dbo].[IntersectTypePredicate]  WITH CHECK ADD  CONSTRAINT [FK_IntersectTypePredicate_IntersectType] FOREIGN KEY([IntersectTypeID])
REFERENCES [dbo].[IntersectType] ([ID])
GO

ALTER TABLE [dbo].[IntersectTypePredicate] CHECK CONSTRAINT [FK_IntersectTypePredicate_IntersectType]
GO

ALTER TABLE [dbo].[IntersectTypePredicate]  WITH CHECK ADD  CONSTRAINT [FK_IntersectTypePredicate_Predicate] FOREIGN KEY([PredicateID])
REFERENCES [dbo].[Predicate] ([ID])
GO

ALTER TABLE [dbo].[IntersectTypePredicate] CHECK CONSTRAINT [FK_IntersectTypePredicate_Predicate]
GO
