CREATE TABLE [quality].[Dimension] (
    [ID]              INT            IDENTITY (1, 1) NOT NULL,
    [Name]            NVARCHAR (250) NOT NULL,
    [Description]     NVARCHAR (MAX) NULL,
    [IsSystemDefined] BIT            CONSTRAINT [DF__Dimension__IsSys__34EA6C10] DEFAULT ((0)) NOT NULL,
    [Weight]          DECIMAL (2, 2) NULL,
    [UpdatedOn]       DATETIME       CONSTRAINT [DF__Dimension__Updat__35DE9049] DEFAULT (getutcdate()) NOT NULL,
    [UpdatedBy]       INT            NOT NULL,
    CONSTRAINT [PK_RuleDimension] PRIMARY KEY CLUSTERED ([ID] ASC)
);

