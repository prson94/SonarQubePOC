CREATE TABLE [dbo].[SiteNav](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[ParentID] [int] NULL,
	[Name] [varchar](250) NULL,
	[Route] [varchar](250) NULL,
	[SortOrder] [int] NULL,
	[ObjectID] [int] NULL,
	[Object] [varchar](50) NULL,
	[Icon] [varchar](100) NULL,
	[Title] [nvarchar](250) NULL
)

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