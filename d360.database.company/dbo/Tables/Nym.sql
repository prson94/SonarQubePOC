CREATE TABLE [dbo].[Nym] (
	[ID]			 INT			IDENTITY (1, 1) NOT NULL,
    [Object]     VARCHAR (25)   NOT NULL,
    [ObjectID]       INT            NOT NULL,    
    [Name]	         NVARCHAR (250) NULL,
    [PredicateID]	 INT			NOT NULL,	 
	[UpdatedOn]		 DATETIME		NULL default GETUTCDATE(),
	[UpdatedBy]		 INT			NULL,
	[CreatedOn]		 DATETIME		NOT NULL default GETUTCDATE(),
	[CreatedBy]		 INT			NOT NULL,
	CONSTRAINT [PK_Nym] PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [FK_Nym_Predicate] FOREIGN KEY ([PredicateID]) REFERENCES [dbo].[Predicate] ([ID]) ON DELETE CASCADE
);