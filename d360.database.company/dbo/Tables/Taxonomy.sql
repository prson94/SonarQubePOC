CREATE TABLE [dbo].[Taxonomy] (
    [ID]             INT             IDENTITY (1, 1) NOT NULL,
    [ParentID]       INT             NULL,
    [TaxonomyTypeID] INT             NOT NULL,
    [Name]           NVARCHAR (250)  NOT NULL,
    [Description]    NVARCHAR (4000) NULL,
    [Path]           XML             NULL,
    [TextPath]       NVARCHAR (1000) NULL,
    [Level]          INT             NULL,
    [UpdatedOn]      DATETIME        NULL,
    [UpdatedBy]      INT             NULL,
    CONSTRAINT [PK_Taxonomy] PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [CK_Taxonomy_IDNotEqualParentID] CHECK ([ID]<>[ParentID]),
    CONSTRAINT [FK_Taxonomy_TaxonomyType] FOREIGN KEY ([TaxonomyTypeID]) REFERENCES [dbo].[TaxonomyType] ([ID]) ON DELETE CASCADE
);


GO
CREATE NONCLUSTERED INDEX [IX_Taxonomy_TaxonomyTypeID-ParentID]
    ON [dbo].[Taxonomy]([TaxonomyTypeID] ASC, [ParentID] ASC);


GO


CREATE TRIGGER [dbo].[Taxonomy_AfterInsert]
   ON  [dbo].[Taxonomy] 
   AFTER INSERT
AS 
	SET NOCOUNT ON;
	insert into [queue].[ObjectVersion] ([Object], ObjectID, ResourceID, [Date], [Action], ActionObject, ActionObjectID)
		select 'Taxonomy', ID, coalesce(UpdatedBy, 0), coalesce(UpdatedOn, getutcdate()), 'Created', 'Taxonomy', ID from inserted

	insert into [queue].[ObjectIndex] ([Object], ObjectID, ResourceID, [Date], [Action], ActionObject, ActionObjectID)
		select 'Taxonomy', ID, coalesce(UpdatedBy, 0), coalesce(UpdatedOn, getutcdate()), 'A', 'Taxonomy', ID from inserted

	declare @tblCache table (RowID int identity, ID int)
	insert into @tblCache 
		select ID from inserted

	declare @current int = 1,
			@max int,
			@thisID int
	select @max = max(RowID) from @tblCache

	while @current <= @max
	begin
		select @thisID = ID from @tblCache where RowID = @current
		exec [cache].[SynchronizeObjectDetails] 'Taxonomy', @thisID
		set @current = @current + 1
	end

	declare @tbl table (ID int);

	with d AS
	(
		SELECT	ParentID, 
				ID
		FROM	inserted	
		UNION ALL
		SELECT	C.ParentID, 
				C.ID
		FROM	Taxonomy	C
				INNER JOIN d AS P ON P.ID = C.ParentID
	)

	insert into @tbl
		select ID from d

	update	T
	set		T.TextPath = utility.GetBreadcrumbStringWrapper('Taxonomy', S.ID, '/'),
			T.[Path] = utility.GetBreadcrumbWrapper('Taxonomy', S.ID),
			T.[Level] = utility.GetObjectLevelWrapper('Taxonomy', S.ID)
	from	Taxonomy T
			inner join @tbl S on S.ID = T.ID
	

	insert into [queue].FollowUpdate (ObjectID, ObjectType) 
	select id as objectid,
	'Taxonomy' as objecttype
	from inserted
	where parentid is not null;


GO

CREATE TRIGGER [dbo].[Taxonomy_AfterUpdate]
   ON  [dbo].[Taxonomy] 
   AFTER UPDATE
AS 
	SET NOCOUNT ON;
	insert into [queue].[ObjectVersion] ([Object], ObjectID, ResourceID, [Date], [Action], ActionObject, ActionObjectID)
		select 'Taxonomy', ID, coalesce(UpdatedBy, 0), coalesce(UpdatedOn, getutcdate()), 'Updated', 'Taxonomy', ID from inserted

	insert into [queue].[ObjectIndex] ([Object], ObjectID, ResourceID, [Date], [Action], ActionObject, ActionObjectID)
		select 'Taxonomy', ID, coalesce(UpdatedBy, 0), coalesce(UpdatedOn, getutcdate()), 'U', 'Taxonomy', ID from inserted

	declare @tblCache table (RowID int identity, ID int)
	insert into @tblCache 
		select ID from inserted

	declare @current int = 1,
			@max int,
			@thisID int
	select @max = max(RowID) from @tblCache

	while @current <= @max
	begin
		select @thisID = ID from @tblCache where RowID = @current
		exec [cache].[SynchronizeObjectDetails] 'Taxonomy', @thisID
		set @current = @current + 1
	end

	declare @tbl table (ID int);

	with d AS
	(
		SELECT	ParentID, 
				ID
		FROM	inserted	
		UNION ALL
		SELECT	C.ParentID, 
				C.ID
		FROM	Taxonomy	C
				INNER JOIN d AS P ON P.ID = C.ParentID
	)

	insert into @tbl
		select ID from d

	update	T
	set		T.TextPath = utility.GetBreadcrumbStringWrapper('Taxonomy', S.ID, '/'),
			T.[Path] = utility.GetBreadcrumbWrapper('Taxonomy', S.ID),
			T.[Level] = utility.GetObjectLevelWrapper('Taxonomy', S.ID)
	from	Taxonomy T
			inner join @tbl S on S.ID = T.ID

	update	T
	set		T.TextPath = utility.GetBreadcrumbStringWrapper('Taxonomy', S.ID, '/')
	from	cache.ObjectDetails T
			inner join @tbl S on T.[Object] = 'Taxonomy' and S.ID = T.ObjectID

	UPDATE	R
	SET		R.SourceObjectName = A.Name
	FROM	cache.Relationships R INNER JOIN inserted A ON R.SourceObject = 'Taxonomy' and R.SourceObjectID = A.ID

	UPDATE	R
	SET		R.TargetObjectName = A.Name
	FROM	cache.Relationships R INNER JOIN inserted A ON R.TargetObject = 'Taxonomy' and R.TargetObjectID = A.ID

	UPDATE	F
	set		F.FormattedValue = utility.GetFormattedFieldLookupValue(FT.Type, FT.LookupDisplayFormat, FT.LookupObjectType, FT.LookupObjectID, F.Value)
	FROM	Field F
			inner join FieldType FT on FT.ID = F.FieldTypeID and FT.LookupObjectType = 'Taxonomy' 
			inner join inserted A on A.ID = FT.LookupObjectID

	exec [cache].[SynchronizeResponsibilities]

GO

CREATE TRIGGER [dbo].[Taxonomy_AfterDelete]
   ON  [dbo].[Taxonomy] 
   AFTER DELETE
AS 
	SET NOCOUNT ON;

	declare @type varchar(50) = 'Taxonomy'

	insert into [queue].[ObjectVersion] ([Object], ObjectID, ResourceID, [Date], [Action], ActionObject, ActionObjectID)
		select @type, ID, coalesce(UpdatedBy, 0), coalesce(UpdatedOn, getutcdate()), 'Removed', @type, ID from deleted

	insert into [queue].[ObjectIndex] ([Object], ObjectID, ResourceID, [Date], [Action], ActionObject, ActionObjectID)
		select @type, ID, coalesce(UpdatedBy, 0), coalesce(UpdatedOn, getutcdate()), 'D', @type, ID from deleted

	DELETE	O
	FROM	cache.ObjectDetails O
			inner join deleted d
	ON		O.[Object] = @type and O.ObjectID = d.ID

	DELETE	F
	FROM	Field as F
			INNER JOIN deleted AS d
	ON		F.ObjectType = @type and F.ObjectID = d.ID

	DELETE	F
	FROM	Follow as F
			INNER JOIN deleted AS d
	ON		F.ObjectType = @type and F.ObjectID = d.ID

	DELETE	O
	FROM	cache.Relationships O
			inner join deleted d
	ON		(O.[SourceObject] = @type and O.SourceObjectID = d.ID) OR (O.[TargetObject] = @type and O.TargetObjectID = d.ID)

	BEGIN TRY
		DECLARE @tblIntersectIDs table (ID int)

		INSERT INTO @tblIntersectIDs
			SELECT	N.IntersectID
			FROM	IntersectNode N
					INNER JOIN deleted AS d ON N.ObjectType = @type and N.ObjectID = d.ID

		DELETE	N
		FROM	IntersectNode N
				INNER JOIN @tblIntersectIDs I ON N.IntersectID = I.ID

		DELETE	II
		FROM	[Intersect] II
				INNER JOIN @tblIntersectIDs I ON II.ID = I.ID
	END TRY
	BEGIN CATCH

	END CATCH
