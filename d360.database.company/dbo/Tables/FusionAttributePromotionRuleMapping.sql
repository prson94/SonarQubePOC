CREATE TABLE [dbo].[FusionAttributePromotionRuleMapping] (
    [ID]                             INT            IDENTITY (1, 1) NOT NULL,
    [FusionAttributePromotionRuleID] INT            NOT NULL,
    [SourceFieldName]                NVARCHAR (250) NULL,
    [SourceFieldTypeID]              INT            NOT NULL,
    [TargetFieldName]                NVARCHAR (250) NULL,
    [TargetFieldTypeID]              INT            NOT NULL,
    CONSTRAINT [PK_FusionAttributePromotionRuleMapping] PRIMARY KEY CLUSTERED ([ID] ASC)
);

