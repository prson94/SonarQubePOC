create TABLE [workflow].[TaskProcedure] (
    [ID]          INT   IDENTITY (1, 1) NOT NULL,
    [Name] NVARCHAR(250)      NOT NULL,
    [Procedure]   VARCHAR(1000)      NOT NULL,  
	[PassObjectInfo] BIT NOT NULL,  
    [UpdatedBy]   INT      NOT NULL,
    [UpdatedOn]   DATETIME NOT NULL,        
);
GO