/*
 Pre-Deployment Script Template							
--------------------------------------------------------------------------------------
 This file contains a list of SQL files that need to be executed when releasing 
 to production in the next cycle.
--------------------------------------------------------------------------------------
\dbo\Stored Procedures\GetNonIntersections.sql
*/


ALTER TABLE [dbo].[FusionAttributePromotionRuleMapping]
ADD IsConstantValue bit NOT NULL DEFAULT(0)

go

ALTER TABLE [dbo].[FusionAttributePromotionRuleMapping]
ADD ConstantValue nvarchar(250)

ALTER TABLE [dbo].[FusionAttributePromotion]
ADD [ObjectTypeID] INT NOT NULL DEFAULT(-1)

go

alter table [fusion].[execution] add [Version] VARCHAR(250) not NULL DEFAULT ('unknown')

go