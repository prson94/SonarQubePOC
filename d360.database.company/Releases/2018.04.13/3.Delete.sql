DROP TABLE [cache].[ResponsibilityItem]
DROP TABLE [dbo].[EmailTemplate]
DROP TABLE [dbo].[LineageDefault]
DROP TABLE [dbo].[ResponsibilityContextItem]
DROP TABLE [dbo].[ResponsibilityTypeRelationRuleItem]
DROP TABLE [dbo].[Responsibility]
DROP TABLE [dbo].[RuleGraph]
DROP TABLE [dbo].[ScoreMetric]
DROP TABLE [dbo].[ScoreTypeMetric]
DROP TABLE [dbo].[ScoreTypeMetricVersionConditionValue]
DROP TABLE [dbo].[ScoreTypeMetricVersionCondition]
DROP TABLE [dbo].[ScoreTypeMetricGroupVersion]
DROP TABLE [dbo].[ScoreTypeMetricGroup]
DROP TABLE [dbo].[Score]
DROP TABLE [dbo].[ScoreType]
DROP TABLE [dbo].[Statistic]
DROP TABLE [dbo].[StatisticType]
DROP TABLE [dbo].[TestExternalMetric]
GO

DROP VIEW analytics.StatisticDetail
DROP VIEW [cache].[Responsibilities]
DROP VIEW [dbo].[ResponsibilityDetail]
DROP VIEW [dbo].[ResponsibilityDetailForResource]
DROP VIEW [dbo].[ResponsibilitySummaryDetail]
DROP VIEW [utility].[ResponsibilityHierarchy]
GO

DROP PROCEDURE [bulkload].[Synonyms]
DROP procedure [cache].[ReSynchronizeAllObjectDetails]
DROP procedure [cache].[SynchronizeObjectDetails]
DROP procedure [cache].[SynchronizeResponsibilities]
DROP procedure [cache].[SynchronizeResponsibilitiesForObject]
DROP procedure [dbo].[AddRelationship]
DROP procedure [dbo].[AddRelationships]
DROP procedure [dbo].[AddRelationshipTypesBulk]
DROP procedure [dbo].[AddSingleIntersect]
DROP procedure [dbo].[AsyncAddObject]
DROP procedure [dbo].[AsyncDeleteObject]
DROP procedure [dbo].[AsyncUpdateObject]
DROP procedure [dbo].[GetAllowedAndUnallocatedResponsibilityTypesByObject]
DROP PROCEDURE [dbo].[GetStatisticDetails]
DROP PROCEDURE [fusion].[Rules] 
DROP procedure [utility].[CalculateStatistics]
DROP procedure [utility].[GetApproversForWorkflow]
DROP procedure [utility].[GetArtifactsUpForCertification]
GO

DROP function [cache].[SynchronizeObjectResponsibilities]
DROP FUNCTION [utility].[GetDirectlyAssignedResponsibilityList]
DROP FUNCTION [utility].[GetHierarchyAssignedResponsibilityList]
DROP FUNCTION [utility].[GetVerticalResponsibilityList]
DROP FUNCTION dbo.GetWorkflowArtifactID
DROP FUNCTION dbo.GetWorkflowStartDate
DROP FUNCTION [dbo].[SurveyReportGeneratorWrapper]
DROP FUNCTION [dbo].[SurveyReportGenerator]
DROP FUNCTION [utility].[GetBreadcrumbWrapper]
DROP FUNCTION [utility].[GetBreadcrumb]
DROP FUNCTION [utility].[GetFormattedFieldAttributeValue]
DROP FUNCTION [utility].[GetFormattedFieldFusionQueryAttributeValue]
DROP FUNCTION [utility].[GetFormattedFieldReferenceItemValueWrapper]
DROP FUNCTION [utility].[GetFormattedFieldReferenceItemValue]
DROP FUNCTION [utility].[GetFormattedFieldReferenceItemValue2]
DROP FUNCTION [utility].[GetResponsibilityContextHashWrapper]
DROP FUNCTION [utility].[GetResponsibilityContextHash]
GO

DROP TYPE [dbo].[ContextsTable]
DROP TYPE [dbo].[ContextTableCheck]
DROP TYPE [dbo].[IntersectionNodeType]
DROP TYPE [dbo].[PreviouslyCheckedIDTable]
DROP TYPE [dbo].[PreviouslyCheckedIntersectTypeIDTable]
DROP TYPE [dbo].[StaticFieldCandidateTable]
DROP TYPE [utility].[DiagramRelationshipTable]
GO

drop table FieldOld

ALTER TABLE [api].[EntityFieldType] DROP CONSTRAINT [FK_EntityFieldType_FieldType]
GO
ALTER TABLE [dbo].[FieldTypeFilteredLookupDefinition] DROP CONSTRAINT [FK_FieldTypeFilteredLookupDefinition_FieldType]
GO
ALTER TABLE [dbo].[FieldTypeLookup] DROP CONSTRAINT [FK_FieldTypeLookup_FieldType]
GO

drop table FieldTypeOld

ALTER TABLE [api].[EntityFieldType]  WITH CHECK ADD  CONSTRAINT [FK_EntityFieldType_FieldType] FOREIGN KEY([FieldTypeID]) REFERENCES [dbo].[FieldType] ([ID]) ON DELETE CASCADE
ALTER TABLE [api].[EntityFieldType] CHECK CONSTRAINT [FK_EntityFieldType_FieldType]
GO

ALTER TABLE [dbo].[FieldTypeFilteredLookupDefinition]  WITH CHECK ADD  CONSTRAINT [FK_FieldTypeFilteredLookupDefinition_FieldType] FOREIGN KEY([FieldTypeID]) REFERENCES [dbo].[FieldType] ([ID]) ON DELETE CASCADE
ALTER TABLE [dbo].[FieldTypeFilteredLookupDefinition] CHECK CONSTRAINT [FK_FieldTypeFilteredLookupDefinition_FieldType]
GO

ALTER TABLE [dbo].[FieldTypeLookup]  WITH CHECK ADD  CONSTRAINT [FK_FieldTypeLookup_FieldType] FOREIGN KEY([FieldTypeID]) REFERENCES [dbo].[FieldType] ([ID]) ON DELETE CASCADE
ALTER TABLE [dbo].[FieldTypeLookup] CHECK CONSTRAINT [FK_FieldTypeLookup_FieldType]
GO


drop table IntersectOld
drop table IntersectTypeOld

drop TRIGGER [dbo].[IssueType_AfterInsert]
drop TRIGGER [dbo].[IssueType_AfterUpdate]

ALTER TABLE [dbo].[Policy] DROP CONSTRAINT [DF_Policy_Status]
GO
alter table [Policy] drop column [Status]
go
alter table [Rule] drop column Name
alter table [Rule] drop column Description
alter table [Rule] drop column Purpose
alter table [Rule] drop column Measurement
alter table [Rule] drop column Resolution
go

alter table Taxonomy drop column Name
alter table Taxonomy drop column Description
alter table Taxonomy drop column DisplayValue
alter table Taxonomy add [DisplayValue] NVARCHAR (MAX) constraint DF_Taxonomy_DisplayValue  DEFAULT ('<INVALID VALUE>') NOT NULL
go

--DROP SCHEMA [staging]
--GO