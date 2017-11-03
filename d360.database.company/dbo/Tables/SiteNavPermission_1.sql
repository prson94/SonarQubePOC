CREATE TABLE [dbo].[SiteNavPermission] (
    [SiteNavID] INT           NOT NULL,
    [Object]    VARCHAR (250) NOT NULL,
    [ObjectID]  INT           NOT NULL,
    CONSTRAINT [PK_SiteNavPermission] PRIMARY KEY CLUSTERED ([SiteNavID] ASC, [Object] ASC, [ObjectID] ASC),
    CONSTRAINT [FK_SiteNavPermission_SiteNavID] FOREIGN KEY ([SiteNavID]) REFERENCES [dbo].[SiteNav] ([ID])
);

