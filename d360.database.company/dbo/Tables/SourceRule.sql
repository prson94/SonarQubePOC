CREATE TABLE [dbo].[SourceRule] (
    [ID]                  INT             IDENTITY (1, 1) NOT NULL,
    [Name]                NVARCHAR (1000) NOT NULL,
    [Object]              VARCHAR (50)    NOT NULL,
    [ObjectID]            INT             NOT NULL,
    [AppliesToObject]     VARCHAR (50)    NULL,
    [AppliesToObjectID]   INT             NULL,
    [AppliesToObjectList] XML             NULL,
    CONSTRAINT [PK_SourceRule] PRIMARY KEY CLUSTERED ([ID] ASC)
);

