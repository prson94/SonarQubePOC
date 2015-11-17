CREATE TABLE [dbo].[FusionAttributeOwnerRuleItem] (
    [ID]                         INT IDENTITY (1, 1) NOT NULL,
    [FusionAttributeOwnerRuleID] INT NOT NULL,
    [FusionAttributeID]          INT NULL,
    CONSTRAINT [PK_FusionAttributeOwnerRuleItem] PRIMARY KEY CLUSTERED ([ID] ASC)
);

