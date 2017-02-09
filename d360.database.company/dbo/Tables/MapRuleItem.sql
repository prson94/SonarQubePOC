CREATE TABLE [dbo].[MapRuleItem] (
    [ID]                      INT          IDENTITY (1, 1) NOT NULL,
    [SourceOwner]             VARCHAR (50) NULL,
    [SourceOwnerID]           INT          NULL,
    [SourceFusionAttributeID] INT          NOT NULL,
    [TargetOwner]             VARCHAR (50) NULL,
    [TargetOwnerID]           INT          NULL,
    [TargetFusionAttributeID] INT          NULL,
    [CreatedBy]               INT          NOT NULL,
    [CreatedOn]               DATETIME     NOT NULL,
    [UpdatedBy]               INT          NOT NULL,
    [UpdatedOn]               DATETIME     NOT NULL,
	[Owner]					  VARCHAR (50) NULL,
    CONSTRAINT [PK_MapRuleItem] PRIMARY KEY NONCLUSTERED ([ID] ASC)
);



