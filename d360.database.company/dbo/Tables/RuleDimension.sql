
CREATE TABLE [dbo].[RuleDimension] (
    [ID]              INT            IDENTITY (1, 1) NOT NULL,
    [Name]            NVARCHAR (250) NOT NULL,
    [Description]     NVARCHAR (MAX) NULL,
    [IsSystemDefined] BIT            DEFAULT ((0)) NOT NULL,
    [UpdatedOn]       DATETIME       DEFAULT (getutcdate()) NOT NULL,
    [UpdatedBy]       INT            NOT NULL,
    CONSTRAINT [PK_RuleDimension] PRIMARY KEY CLUSTERED ([ID] ASC)
);


go

begin
	insert into [dbo].[RuleDimension] (Name,Description,IsSystemDefined,UpdatedBy) values(N'Completeness',N'Is all the requisite information available? Are data values missing, or in an unusable state? In some cases, missing data is irrelevant, but when the information that is missing is critical to a specific business process, completeness becomes an issue. ',1,0)
	insert into [dbo].[RuleDimension] (Name,Description,IsSystemDefined,UpdatedBy) values(N'Conformity',N'Are there expectations that data values conform to specified formats? If so, do all the values conform to those formats? Maintaining conformance to specific formats is important in data representation, presentation, aggregate reporting, search, and establishing key relationships.',1,0)
	insert into [dbo].[RuleDimension] (Name,Description,IsSystemDefined,UpdatedBy) values(N'Consistency',N'Do distinct data instances provide conflicting information about the same underlying data object? Are values consistent across data sets? Do interdependent attributes always appropriately reflect their expected consistency? Inconsistency between data values plagues organizations attempting to reconcile between different systems and applications.',1,0)
	insert into [dbo].[RuleDimension] (Name,Description,IsSystemDefined,UpdatedBy) values(N'Accuracy',N'Do data objects accurately represent the “real-world” values they are expected to model? Incorrect spellings of product or person names, addresses, and even untimely or not current data can impact operational and analytical applications.',1,0)
	insert into [dbo].[RuleDimension] (Name,Description,IsSystemDefined,UpdatedBy) values(N'Duplication',N'Are there multiple, unnecessary representations of the same data objects within your data set? The inability to maintain a single representation for each entity across your systems poses numerous vulnerabilities and risks.',1,0)
	insert into [dbo].[RuleDimension] (Name,Description,IsSystemDefined,UpdatedBy) values(N'Integrity',N'What data is missing important relationship linkages? The inability to link related records together may actually introduce duplication across your systems. Not only that, as more value is derived from analyzing connectivity and relationships, the inability to link related data instance together impedes this valuable analysis.',1,0)	
end
go