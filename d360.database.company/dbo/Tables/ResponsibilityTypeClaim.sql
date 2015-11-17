CREATE TABLE [dbo].[ResponsibilityTypeClaim] (
    [ID]                   INT IDENTITY (1, 1) NOT NULL,
    [ResponsibilityTypeID] INT NOT NULL,
    [Claim]                INT NOT NULL,
    [ClaimObject]          INT NULL,
    CONSTRAINT [PK_ResponsibilityTypeClaim] PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [FK_ResponsibilityTypeClaim_ResponsibilityType] FOREIGN KEY ([ResponsibilityTypeID]) REFERENCES [dbo].[ResponsibilityType] ([ID]) ON DELETE CASCADE
);

