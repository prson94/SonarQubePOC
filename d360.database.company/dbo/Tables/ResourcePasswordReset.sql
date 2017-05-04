CREATE TABLE [dbo].[ResourcePasswordReset] (
	[ID]      uniqueidentifier  NOT NULL PRIMARY KEY DEFAULT (NEWID()),
    [ResourceID]     INT   NOT NULL,    
    [CreateDate]     DATETIME  NOT NULL,    
);