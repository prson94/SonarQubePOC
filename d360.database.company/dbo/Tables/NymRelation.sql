  CREATE TABLE [dbo].[NymRelation] (
	[ID]				   INT			IDENTITY (1, 1) NOT NULL,
    [PredicateID]          INT          NOT NULL,
    [Object]               VARCHAR (25) NOT NULL,
    [ObjectID]             INT          NOT NULL,    
	[UpdatedOn]			   DATETIME		NOT NULL,
	[UpdatedBy]			   INT			NOT NULL,
	CONSTRAINT [PK_NymRelation] PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [CONST_NymRelation_Name] UNIQUE NONCLUSTERED ([PredicateID] ASC, [Object] ASC, [ObjectID] ASC),    
    CONSTRAINT [FK_NymRelation_PredicateType] FOREIGN KEY ([PredicateID]) REFERENCES [dbo].[Predicate] ([ID]) ON DELETE CASCADE
);