CREATE TABLE [fusion].[RuleLog] (
    [ID]                   INT      IDENTITY (1, 1) NOT NULL,
    [DateStarted]          DATETIME NOT NULL,
    [DateCompleted]        DATETIME NULL,
    [PromotedTaxonomies]   INT      NULL,
    [PromotedDomainItems]  INT      NULL,
    [PromotedDomains]      INT      NULL,
    [PromotedArtifacts]    INT      NULL,
    [TotalNewPromotions]   INT      NULL,
    [AttributesConsidered] INT      NULL,
    [NumberOfRules]        INT      NULL,
    [RelationshipsAdded]   INT      NULL,
    CONSTRAINT [PK_FusionRuleLog] PRIMARY KEY NONCLUSTERED ([ID] ASC)
);


GO
CREATE CLUSTERED INDEX [CIX_FusionRuleLog]
    ON [fusion].[RuleLog]([DateStarted] DESC);

