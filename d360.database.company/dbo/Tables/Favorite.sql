CREATE TABLE [dbo].[Favorite] (
    [ID]         INT           IDENTITY (1, 1) NOT NULL,
    [ResourceID] INT           NOT NULL,
    [Route]      VARCHAR (250) NULL,
    [Name]       VARCHAR (250) NULL,
    [SortOrder]  INT           NULL,
    [IsOverride] BIT           CONSTRAINT [DF_Favorite_IsOverride] DEFAULT ((0)) NOT NULL,
    [Object]     VARCHAR (50)  NULL,
    [ObjectID]   INT           NULL,
    [IsHomePage] BIT           CONSTRAINT [DF_Favorite_IsHomePage] DEFAULT ((0)) NOT NULL, 
    CONSTRAINT [PK_Favorite_ID] PRIMARY KEY CLUSTERED ([ID] ASC)
);


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

