CREATE TABLE [dbo].[RuleResultQualifierType] (
    [ID]                      INT            IDENTITY (1, 1) NOT NULL,
    [RuleID]                  INT            NOT NULL,
    [Name]                    NVARCHAR (250) NOT NULL,
    [Order]                   INT            NOT NULL,
    [ResolutionObject]        VARCHAR (50)   NULL,
    [ResolutionObjectID]      INT            NULL,
    [ResolutionFieldTypeID]   INT            NULL,
    [ResolutionFieldTypeName] NVARCHAR (250) NULL,
    CONSTRAINT [PK_RuleResultQualifierType] PRIMARY KEY NONCLUSTERED ([ID] ASC),
    CONSTRAINT [FK_RuleResultQualifierType_Rule] FOREIGN KEY ([RuleID]) REFERENCES [dbo].[Rule] ([ID])
);

