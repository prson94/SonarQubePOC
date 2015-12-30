CREATE TABLE [dbo].[FusionAttributePromotionLogSummary] (
    [ID]                  INT              IDENTITY (1, 1) NOT NULL PRIMARY KEY,    
    [DateStarted]         DATETIME         NOT NULL,
    [DateCompleted]       DATETIME         NULL,
	[PromotedTaxonomies]  INT			   NULL,
	[PromotedDomainItems] INT			   NULL,
	[PromotedDomains]	  INT			   NULL,
	[PromotedArtifacts]	  INT			   NULL,
    [TotalNewPromotions]  INT              NULL,  
	[AttributesConsidered] INT			   NULL,  
	[NumberOfRules]		  INT			   NULL	
);
