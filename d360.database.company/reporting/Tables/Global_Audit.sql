CREATE TABLE [reporting].[Global_Audit] (
    [ID]                   BIGINT         IDENTITY (1, 1) NOT NULL,
    [Object]               VARCHAR (50)   NOT NULL,
    [ObjectID]             INT            NOT NULL,
    [ObjectName]           NVARCHAR (250) NOT NULL,
    [ResourceID]           INT            NOT NULL,
    [Date]                 DATETIME       NOT NULL,
    [Action]               VARCHAR (15)   NOT NULL,
    [ActionObject]         VARCHAR (50)   NOT NULL,
    [ActionObjectID]       INT            NOT NULL,
    [ActionObjectTypeName] NVARCHAR (250) NOT NULL,
    [ActionObjectName]     NVARCHAR (250) NOT NULL,
    [ActionDescription]    NVARCHAR (MAX) NOT NULL,
    CONSTRAINT [PK_ReportingAudit] PRIMARY KEY NONCLUSTERED ([ID] ASC)
);


GO
CREATE NONCLUSTERED INDEX [IX_ReportingAudit_Object_ActionObject]
    ON [reporting].[Global_Audit]([Object] ASC, [ObjectID] ASC, [ActionObject] ASC, [ActionObjectID] ASC);

