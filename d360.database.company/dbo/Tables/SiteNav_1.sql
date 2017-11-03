CREATE TABLE [dbo].[SiteNav] (
    [ID]        INT            IDENTITY (1, 1) NOT NULL,
    [ParentID]  INT            NULL,
    [Name]      VARCHAR (250)  NULL,
    [Route]     VARCHAR (250)  NULL,
    [SortOrder] INT            NULL,
    [ObjectID]  INT            NULL,
    [Object]    VARCHAR (50)   NULL,
    [Icon]      VARCHAR (100)  NULL,
    [Title]     NVARCHAR (250) NULL,
    CONSTRAINT [PK_SiteNav] PRIMARY KEY CLUSTERED ([ID] ASC)
);




GO


CREATE TRIGGER [dbo].[SiteNav_AfterDelete]
	ON [dbo].[SiteNav]
	FOR DELETE
AS

	--update sortorder
	update n
	set n.sortorder = s.sortorder 
	from
		sitenav n
		join (
		select 
			id,
			row_number() over (partition by parentid order by sortorder) as sortorder
		from sitenav where parentid is null) s on s.id = n.id;
GO

CREATE TRIGGER [dbo].[SiteNav_AfterInsert]
	ON [dbo].[SiteNav]
	FOR INSERT
AS

	--update sortorder
	update n
	set n.sortorder = s.sortorder 
	from
		sitenav n
		join (
		select 
			id,
			row_number() over (partition by parentid order by sortorder) as sortorder
		from sitenav where parentid is null) s on s.id = n.id;