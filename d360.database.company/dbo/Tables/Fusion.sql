CREATE TABLE [dbo].[Fusion] (
    [ID]                INT             IDENTITY (1, 1) NOT NULL,
    [FusionTypeID]      INT             NOT NULL,
    [Name]              NVARCHAR (250)  NOT NULL,
    [Description]       NVARCHAR (4000) NULL,
    [Enabled]           BIT             CONSTRAINT [DF_Fusion_Enabled] DEFAULT ((1)) NOT NULL,
    [Manual]            BIT             NOT NULL,
    [LockPromotedItems] BIT             CONSTRAINT [DF_Fusion_LockPromotedItems] DEFAULT ((1)) NOT NULL,
    [IntervalType]      INT             NULL,
    [Interval]          INT             NULL,
    [ForceRefresh]      BIT             NULL,
    [UpdatedOn]         DATETIME        NULL,
    [UpdatedBy]         INT             NULL,
    CONSTRAINT [PK_Fusion] PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [FK_Fusion_FusionType] FOREIGN KEY ([FusionTypeID]) REFERENCES [dbo].[FusionType] ([ID]) ON DELETE CASCADE
);


GO
CREATE NONCLUSTERED INDEX [IX_Fusion_FusionTypeID]
    ON [dbo].[Fusion]([FusionTypeID] ASC);


GO
CREATE TRIGGER [dbo].[Fusion_AfterDelete]
   ON  [dbo].[Fusion] 
   AFTER DELETE
AS 
	SET NOCOUNT ON;
	declare @type varchar(50) = 'Fusion'

	insert into [queue].ObjectVersion ([Object], ObjectID, ResourceID, [Date], [Action], ActionObject, ActionObjectID)
		select @type, ID, coalesce(UpdatedBy, 0), coalesce(UpdatedOn, getutcdate()), 'Removed', @type, ID from deleted

	DELETE	O
	FROM	cache.ObjectDetails O
			inner join deleted d
	ON		O.[Object] = @type and O.ObjectID = d.ID

	DELETE	F
	FROM	Field as F
			INNER JOIN deleted AS d
	ON		F.ObjectType = @type and F.ObjectID = d.ID

GO
CREATE TRIGGER [dbo].[Fusion_AfterInsert]
   ON  [dbo].[Fusion] 
   AFTER INSERT
AS 
	SET NOCOUNT ON;
	insert into [queue].ObjectVersion ([Object], ObjectID, ResourceID, [Date], [Action], ActionObject, ActionObjectID)
		select 'Fusion', ID, coalesce(UpdatedBy, 0), coalesce(UpdatedOn, getutcdate()), 'Created', 'Fusion', ID from inserted

	declare @tbl table (RowID int identity, ID int)
	insert into @tbl 
		select ID from inserted

	declare @current int = 1,
			@max int,
			@thisID int
	select @max = max(RowID) from @tbl

	while @current <= @max
	begin
		select @thisID = ID from @tbl where RowID = @current
		exec [cache].[SynchronizeObjectDetails] 'Fusion', @thisID
		set @current = @current + 1
	end

GO
CREATE TRIGGER [dbo].[Fusion_AfterUpdate]
   ON  [dbo].[Fusion] 
   AFTER UPDATE
AS 
	SET NOCOUNT ON;
	insert into [queue].ObjectVersion ([Object], ObjectID, ResourceID, [Date], [Action], ActionObject, ActionObjectID)
		select 'Fusion', ID, coalesce(UpdatedBy, 0), coalesce(UpdatedOn, getutcdate()), 'Updated', 'Fusion', ID from inserted

	declare @tbl table (RowID int identity, ID int)
	insert into @tbl 
		select ID from inserted

	declare @current int = 1,
			@max int,
			@thisID int
	select @max = max(RowID) from @tbl

	while @current <= @max
	begin
		select @thisID = ID from @tbl where RowID = @current
		exec [cache].[SynchronizeObjectDetails] 'Fusion', @thisID
		set @current = @current + 1
	end
