CREATE TABLE [dbo].[SourceRule] (
    [ID]                INT             IDENTITY (1, 1) NOT NULL,
    [Name]              NVARCHAR (1000) NOT NULL,
    [Object]            VARCHAR (50)    NOT NULL,
    [ObjectID]          INT             NOT NULL,
    [AppliesToObject]   VARCHAR (50)    NULL,
    [AppliesToObjectID] INT             NULL,
    [IsTemplate]        BIT             DEFAULT ((0)) NOT NULL,
    CONSTRAINT [PK_SourceRule] PRIMARY KEY CLUSTERED ([ID] ASC)
);




GO
CREATE NONCLUSTERED INDEX [IX_SourceRule_AppliesToObject_Object]
    ON [dbo].[SourceRule]([AppliesToObject] ASC, [AppliesToObjectID] ASC, [Object] ASC, [ObjectID] ASC);

