CREATE TABLE [dbo].[PolicyType] (
    [ID]                INT             IDENTITY (50000, 1) NOT NULL,
    [Name]              NVARCHAR (250)  NOT NULL,
    [Description]       NVARCHAR (4000) NULL,
    [PolicyTypeClassID] INT             NULL,
    [UpdatedOn]         DATETIME        NULL,
    [UpdatedBy]         INT             NULL,
    [MaximumDepth]      INT             NULL,
    CONSTRAINT [PK_PolicyType] PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [FK_PolicyType_PolicyTypeClass] FOREIGN KEY ([PolicyTypeClassID]) REFERENCES [dbo].[PolicyTypeClass] ([ID])
);


GO

CREATE TRIGGER [dbo].[PolicyType_AfterInsert]
   ON  [dbo].[PolicyType] 
   AFTER INSERT
AS 
	SET NOCOUNT ON;
	insert into [queue].[ObjectVersion] ([Object], ObjectID, ResourceID, [Date], [Action], ActionObject, ActionObjectID)
		select 'PolicyType', ID, coalesce(UpdatedBy, 0), coalesce(UpdatedOn, getutcdate()), 'Created', 'PolicyType', ID from inserted

	insert into [queue].[ObjectIndex] ([Object], ObjectID, ResourceID, [Date], [Action], ActionObject, ActionObjectID)
		select 'PolicyType', ID, coalesce(UpdatedBy, 0), coalesce(UpdatedOn, getutcdate()), 'A', 'PolicyType', ID from inserted

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
		exec [cache].[SynchronizeObjectDetails] 'PolicyType', @thisID
		set @current = @current + 1
	end

GO
CREATE TRIGGER [dbo].[PolicyType_AfterUpdate]
   ON  [dbo].[PolicyType] 
   AFTER UPDATE
AS 
	SET NOCOUNT ON;
	insert into [queue].[ObjectVersion] ([Object], ObjectID, ResourceID, [Date], [Action], ActionObject, ActionObjectID)
		select 'PolicyType', ID, coalesce(UpdatedBy, 0), coalesce(UpdatedOn, getutcdate()), 'Updated', 'PolicyType', ID from inserted

	insert into [queue].[ObjectIndex] ([Object], ObjectID, ResourceID, [Date], [Action], ActionObject, ActionObjectID)
		select 'PolicyType', ID, coalesce(UpdatedBy, 0), coalesce(UpdatedOn, getutcdate()), 'U', 'PolicyType', ID from inserted

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
		exec [cache].[SynchronizeObjectDetails] 'PolicyType', @thisID
		set @current = @current + 1
	end

	update	T
	set		T.TextPath = utility.GetBreadcrumbStringWrapper('Policy', T.ID, '/')
	from	Policy T
			inner join inserted S on S.ID = T.PolicyTypeID

GO
CREATE TRIGGER [dbo].[PolicyType_AfterDelete]
   ON  [dbo].[PolicyType] 
   AFTER DELETE
AS 
BEGIN
	SET NOCOUNT ON;

	declare @type varchar(50) = 'PolicyType'

	insert into [queue].[ObjectVersion] ([Object], ObjectID, ResourceID, [Date], [Action], ActionObject, ActionObjectID)
		select @type, ID, coalesce(UpdatedBy, 0), coalesce(UpdatedOn, getutcdate()), 'Removed', @type, ID from deleted

	insert into [queue].[ObjectIndex] ([Object], ObjectID, ResourceID, [Date], [Action], ActionObject, ActionObjectID)
		select @type, ID, coalesce(UpdatedBy, 0), coalesce(UpdatedOn, getutcdate()), 'D', @type, ID from deleted

	DELETE	R
	FROM	AttributeTypeRelation R
			INNER JOIN deleted D on R.ObjectType = @type AND R.ObjectID = D.ID

	DELETE	R
	FROM	[FieldType] R
			INNER JOIN deleted D on R.[Object] = @type AND R.ObjectID = D.ID

	delete Responsibility where ObjectType = @type and ObjectID in (select ID from deleted)

	delete ResponsibilityTypeRelation where ObjectType = @type and ObjectID in (select ID from deleted)
	delete ResponsibilityTypeObjectClaim where ObjectType = @type and ObjectID in (select ID from deleted)
	delete ResponsibilityTypeRelation where ObjectType = @type and ObjectID in (select ID from deleted)
	delete ResponsibilityTypeSourceType where ObjectType = @type and ObjectID in (select ID from deleted)

	delete StatisticTypeRelation where ObjectType = @type and ObjectID in (select ID from deleted)
	delete WorkflowTypeRelation where [Object] = @type and ObjectID in (select ID from deleted)

	DELETE	O
	FROM	cache.ObjectDetails O
			inner join deleted d
	ON		O.[Object] = @type and O.ObjectID = d.ID
END
