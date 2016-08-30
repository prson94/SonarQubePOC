CREATE TABLE [dbo].[Favorite](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[ResourceID] [int] NOT NULL,
	[Route] [varchar](250) NULL,
	[Name] [varchar](250) NOT NULL,
	[SortOrder] [int] NULL,
 CONSTRAINT [PK_Favorite_ID] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)
)

GO

CREATE TRIGGER [dbo].[Favorite_AfterDelete]
	ON [dbo].[Favorite]
	FOR DELETE
AS

	--update sortorder
	update f
	set f.sortorder = s.sortorder 
	from
		favorite f
		join (
		select 
			id, 
			resourceid, 
			row_number() over (partition by resourceid order by sortorder) as sortorder
		from favorite) s on s.id = f.id
		join deleted i on i.ResourceID = s.ResourceID;

GO

ALTER TABLE [dbo].[Favorite] ENABLE TRIGGER [Favorite_AfterDelete]
GO

CREATE TRIGGER [dbo].[Favorite_AfterInsert]
	ON [dbo].[Favorite]
	FOR INSERT
AS

	--update sortorder
	update f
	set f.sortorder = s.sortorder 
	from
		favorite f
		join (
		select 
			id, 
			resourceid, 
			row_number() over (partition by resourceid order by sortorder) as sortorder
		from favorite) s on s.id = f.id
		join inserted i on i.ResourceID = s.ResourceID;

GO

ALTER TABLE [dbo].[Favorite] ENABLE TRIGGER [Favorite_AfterInsert]
GO