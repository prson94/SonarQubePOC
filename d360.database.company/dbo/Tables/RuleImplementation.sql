CREATE TABLE [dbo].[RuleImplementation] (
    [ID]        INT            IDENTITY (1, 1) NOT NULL,
    [RuleID]    INT            NOT NULL,
    [SourceID]  VARCHAR (250)  NULL,
    [SourceUri] VARCHAR (2500) NULL,
    [Name]      NVARCHAR (250) NULL,
    [CreatedOn] DATETIME       NULL,
    [CreatedBy] INT            NULL,
    [UpdatedOn] DATETIME       NULL,
    [UpdatedBy] INT            NULL,
    CONSTRAINT [PK_RuleImplementation] PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [FK_RuleImplementation_Rule] FOREIGN KEY ([RuleID]) REFERENCES [dbo].[Rule] ([ID])
);

