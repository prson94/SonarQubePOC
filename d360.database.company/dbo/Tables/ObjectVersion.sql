CREATE TABLE [dbo].[ObjectVersion] (
    [ObjectType] VARCHAR (25)   NOT NULL,
    [ObjectID]   INT            NOT NULL,
    [Version]    INT            CONSTRAINT [DF_ObjectVersion_Version] DEFAULT ((1)) NOT NULL,
    [Action]     VARCHAR (5)    NOT NULL,
    [ResourceID] INT            NOT NULL,
    [Date]       DATETIME       NOT NULL,
    [Value]      NVARCHAR (MAX) NULL,
    CONSTRAINT [PK_ObjectVersion] PRIMARY KEY CLUSTERED ([ObjectType] ASC, [ObjectID] ASC, [Version] ASC)
);




GO
CREATE NONCLUSTERED INDEX [IX_ObjectVersion_Object]
    ON [dbo].[ObjectVersion]([ObjectType] ASC, [ObjectID] ASC);


GO
