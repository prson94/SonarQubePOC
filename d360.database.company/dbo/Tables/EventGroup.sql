CREATE TABLE [dbo].[EventGroup] (
    [ID]         INT            IDENTITY (1, 1) NOT NULL,
    [Name]       NVARCHAR (500) NOT NULL,
    [PublicID]   NVARCHAR (250) NOT NULL,
    [RuleID]     INT            NULL,
    [UpdatedOn]  DATETIME       NULL,
    [UpdatedBy]  INT            NULL,
    [EventCount] INT            CONSTRAINT [DF_EventGroup_EventCount] DEFAULT ((0)) NOT NULL,
    CONSTRAINT [PK_EventGroup] PRIMARY KEY CLUSTERED ([ID] ASC)
);


GO
CREATE TRIGGER [dbo].[EventGroup_AfterDelete]
   ON  [dbo].[EventGroup] 
   AFTER DELETE
AS 
	SET NOCOUNT ON;
	declare @type varchar(50) = 'EventGroup'

	insert into [queue].ObjectVersion ([Object], ObjectID, ResourceID, [Date], [Action], ActionObject, ActionObjectID)
		select 'Rule', RuleID, coalesce(UpdatedBy, 0), coalesce(UpdatedOn, getutcdate()), 'Removed', @type, ID from deleted

	DELETE	O
	FROM	cache.ObjectDetails O
			inner join deleted d
	ON		O.[Object] = @type and O.ObjectID = d.ID

	DELETE	O
	FROM	[Event] O
			INNER JOIN deleted d
	ON		O.EventGroupID = d.ID

GO

CREATE TRIGGER [dbo].[EventGroup_AfterInsert]
   ON  [dbo].[EventGroup] 
   AFTER INSERT
AS 
	SET NOCOUNT ON;
	insert into [queue].ObjectVersion ([Object], ObjectID, ResourceID, [Date], [Action], ActionObject, ActionObjectID)
		select 'Rule', RuleID, coalesce(UpdatedBy, 0), coalesce(UpdatedOn, getutcdate()), 'Created', 'EventGroup', ID from inserted

	declare @tbl table (RowID int identity, ID int, RuleID int, Name nvarchar(500))
	insert into @tbl 
		select ID, RuleID, Name from inserted

	declare @current int = 1,
			@max int,
			@thisID int,
			@ruleID int,
			@name nvarchar(500)
	select @max = max(RowID) from @tbl

	while @current <= @max
	begin
		select @thisID = ID, @ruleID = RuleID, @name = Name from @tbl where RowID = @current

		declare @commentID int
		insert into Comment (CommentTypeID, Body, DateCreated, CreatingResourceID, OwnerObjectType, OwnerObjectID) 
		values (
			8, 
			'Rule produced <a data-type="EventGroup" data-id="' + cast(@thisID as varchar(15)) + '" data-context="Preview" href="#/rules/' + cast(@ruleID as varchar(15)) + '">events</a> for run ' + REPLACE(@name,'''','''''') + '.',
			getutcdate(),
			0,
			'Rule',
			@ruleID
			)
		set @commentID = SCOPE_IDENTITY()
		insert into CommentRelation (CommentID, ObjectType, ObjectID, [Date]) values (@commentID, 'Rule', @ruleID, getutcdate())

		exec [cache].[SynchronizeObjectDetails] 'EventGroup', @thisID
		set @current = @current + 1
	end


GO
CREATE TRIGGER [dbo].[EventGroup_AfterUpdate]
   ON  [dbo].[EventGroup] 
   AFTER UPDATE
AS 
	SET NOCOUNT ON;
	insert into [queue].ObjectVersion ([Object], ObjectID, ResourceID, [Date], [Action], ActionObject, ActionObjectID)
		select 'Rule', RuleID, coalesce(UpdatedBy, 0), coalesce(UpdatedOn, getutcdate()), 'Updated', 'EventGroup', ID from inserted

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
		exec [cache].[SynchronizeObjectDetails] 'EventGroup', @thisID
		set @current = @current + 1
	end
