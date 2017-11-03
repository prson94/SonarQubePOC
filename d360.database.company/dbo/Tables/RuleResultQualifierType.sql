CREATE TABLE [dbo].[RuleResultQualifierType] (
    [ID]                      INT            IDENTITY (1, 1) NOT NULL,
    [Name]                    NVARCHAR (250) NOT NULL,
    [Order]                   INT            NOT NULL,
    [ResolutionObject]        VARCHAR (50)   NULL,
    [ResolutionObjectID]      INT            NULL,
    [ResolutionFieldTypeID]   INT            NULL,
    [ResolutionFieldTypeName] NVARCHAR (250) NULL,
    [RuleImplementationID]    INT            CONSTRAINT [DF_RuleResultQualifierType_RuleImplementationID] DEFAULT ((0)) NOT NULL,
    CONSTRAINT [PK_RuleResultQualifierType] PRIMARY KEY NONCLUSTERED ([ID] ASC),
    CONSTRAINT [FK_RuleResultQualifierType_RuleImplementation] FOREIGN KEY ([RuleImplementationID]) REFERENCES [dbo].[RuleImplementation] ([ID])
);



