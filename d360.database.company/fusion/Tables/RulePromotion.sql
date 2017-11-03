CREATE TABLE [fusion].[RulePromotion] (
    [ID]            INT          IDENTITY (1, 1) NOT NULL,
    [AttributeID]   INT          NOT NULL,
    [ObjectType]    VARCHAR (25) NOT NULL,
    [ObjectID]      INT          NOT NULL,
    [RuleID]        INT          NOT NULL,
    [RuleStepID]    INT          NULL,
    [ObjectTypeID]  INT          DEFAULT ((-1)) NOT NULL,
    [CreatedOn]     DATETIME     CONSTRAINT [DF_RulePromotion_CreatedOn] DEFAULT (getdate()) NOT NULL,
    [UpdatedOn]     DATETIME     CONSTRAINT [DF_RulePromotion_UpdatedOn] DEFAULT (getdate()) NOT NULL,
    [AttributeType] VARCHAR (25) NOT NULL,
    CONSTRAINT [PK_FusionRulePromotion] PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [FK_FusionRulePromotion_FusionRule] FOREIGN KEY ([RuleID]) REFERENCES [fusion].[Rule] ([ID]) ON DELETE CASCADE
);





GO


GO


GO


GO


GO


GO


GO


GO
CREATE NONCLUSTERED INDEX [IX_FusionRulePromotion_FusionAttribute_Rule_RuleStep_Object]
    ON [fusion].[RulePromotion]([AttributeID] ASC, [RuleID] ASC, [RuleStepID] ASC, [ObjectID] ASC, [ObjectType] ASC);



