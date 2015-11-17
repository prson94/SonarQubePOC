CREATE TABLE [dbo].[Resolution] (
    [ID]     INT             IDENTITY (1, 1) NOT NULL,
    [Name]   NVARCHAR (250)  NOT NULL,
    [Body]   NVARCHAR (4000) NULL,
    [RuleID] INT             CONSTRAINT [CK_Resolution_RuleID] DEFAULT ((0)) NOT NULL,
    CONSTRAINT [PK_Resolution] PRIMARY KEY CLUSTERED ([ID] ASC)
);

