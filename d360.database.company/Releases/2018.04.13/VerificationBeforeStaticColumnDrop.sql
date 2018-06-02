DROP INDEX [IX_Artifact_TaxonomyTypeID_WithIncludes] ON [dbo].[Artifact]
GO


ALTER TABLE Artifact DROP COLUMN Name
ALTER TABLE Artifact DROP COLUMN Description
ALTER TABLE Artifact DROP COLUMN Status
ALTER TABLE Artifact DROP COLUMN TextPath
ALTER TABLE Artifact DROP COLUMN DateLastCertified
ALTER TABLE Artifact DROP COLUMN TaxonomyTypeID
ALTER TABLE Artifact DROP COLUMN [KeyHash]
ALTER TABLE Artifact DROP COLUMN [FieldHash]
ALTER TABLE Artifact DROP COLUMN [DisplayValue]
GO

alter table [Policy] drop column [Name]
alter table [Policy] drop column [Description]
alter table [Policy] drop column [Status]
GO

alter table [Rule] drop column Name
alter table [Rule] drop column Description
alter table [Rule] drop column Purpose
alter table [Rule] drop column Measurement
alter table [Rule] drop column Resolution
GO

alter table Taxonomy drop column Name
alter table Taxonomy drop column Description
alter table Taxonomy drop column DisplayValue
alter table Taxonomy add [DisplayValue] NVARCHAR (MAX) constraint DF_Taxonomy_DisplayValue  DEFAULT ('<INVALID VALUE>') NOT NULL
GO