CREATE TABLE [dbo].[TaxonomyType] (
    [ID]                  INT             IDENTITY (50000, 1) NOT NULL,
    [Name]                NVARCHAR (250)  NOT NULL,
    [Description]         NVARCHAR (4000) NULL,
    [MaximumDepth]        INT             NULL,
    [TaxonomyTypeClassID] INT             NULL,
    [UpdatedOn]           DATETIME        NULL,
    [UpdatedBy]           INT             NULL,
    CONSTRAINT [PK_TaxonomyType] PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [FK_TaxonomyType_TaxonomyTypeClass] FOREIGN KEY ([TaxonomyTypeClassID]) REFERENCES [dbo].[TaxonomyTypeClass] ([ID])
);


GO
CREATE TRIGGER [dbo].[TaxonomyType_AfterInsert]
   ON  [dbo].[TaxonomyType] 
   AFTER INSERT
AS 
	SET NOCOUNT ON;
	insert into [queue].[ObjectVersion] ([Object], ObjectID, ResourceID, [Date], [Action], ActionObject, ActionObjectID)
		select 'TaxonomyType', ID, coalesce(UpdatedBy, 0), coalesce(UpdatedOn, getutcdate()), 'Created', 'TaxonomyType', ID from inserted

	insert into [queue].[ObjectIndex] ([Object], ObjectID, ResourceID, [Date], [Action], ActionObject, ActionObjectID)
		select 'TaxonomyType', ID, coalesce(UpdatedBy, 0), coalesce(UpdatedOn, getutcdate()), 'A', 'TaxonomyType', ID from inserted

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
		exec [cache].[SynchronizeObjectDetails] 'TaxonomyType', @thisID
		set @current = @current + 1
	end

GO

CREATE TRIGGER [dbo].[TaxonomyType_AfterUpdate]
   ON  [dbo].[TaxonomyType] 
   AFTER UPDATE
AS 
	SET NOCOUNT ON;
	insert into [queue].[ObjectVersion] ([Object], ObjectID, ResourceID, [Date], [Action], ActionObject, ActionObjectID)
		select 'TaxonomyType', ID, coalesce(UpdatedBy, 0), coalesce(UpdatedOn, getutcdate()), 'Updated', 'TaxonomyType', ID from inserted

	insert into [queue].[ObjectIndex] ([Object], ObjectID, ResourceID, [Date], [Action], ActionObject, ActionObjectID)
		select 'TaxonomyType', ID, coalesce(UpdatedBy, 0), coalesce(UpdatedOn, getutcdate()), 'U', 'TaxonomyType', ID from inserted

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
		exec [cache].[SynchronizeObjectDetails] 'TaxonomyType', @thisID
		set @current = @current + 1
	end

	update	T
	set		T.TextPath = utility.GetBreadcrumbStringWrapper('Taxonomy', S.ID, '/'),
			T.[Path] = utility.GetBreadcrumbWrapper('Taxonomy', S.ID)
	from	Taxonomy T
			inner join inserted S on S.ID = T.TaxonomyTypeID

GO
CREATE TRIGGER [dbo].[TaxonomyType_AfterDelete]
   ON  [dbo].[TaxonomyType] 
   AFTER DELETE
AS 
BEGIN
	SET NOCOUNT ON;

	declare @type varchar(50) = 'TaxonomyType'

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

