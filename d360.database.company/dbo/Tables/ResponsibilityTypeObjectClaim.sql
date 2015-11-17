CREATE TABLE [dbo].[ResponsibilityTypeObjectClaim] (
    [ID]                   INT          IDENTITY (1, 1) NOT NULL,
    [ResponsibilityTypeID] INT          NOT NULL,
    [ObjectType]           VARCHAR (50) NOT NULL,
    [ObjectID]             INT          NOT NULL,
    [Claim]                INT          NOT NULL,
    [ClaimObject]          INT          NOT NULL,
    CONSTRAINT [PK_ResponsibilityTypeObjectClaim] PRIMARY KEY CLUSTERED ([ID] ASC)
);

