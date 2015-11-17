CREATE TABLE [dbo].[Policy] (
    [ID]           INT             IDENTITY (1, 1) NOT NULL,
    [ParentID]     INT             NULL,
    [Name]         NVARCHAR (250)  NOT NULL,
    [Description]  NVARCHAR (4000) NULL,
    [TextPath]     NVARCHAR (2000) NULL,
    [UpdatedOn]    DATETIME        NULL,
    [UpdatedBy]    INT             NULL,
    [PolicyTypeID] INT             CONSTRAINT [DF_Policy_PolicyTypeID] DEFAULT ((50000)) NOT NULL,
    [Level]        INT             CONSTRAINT [DF_Policy_Level] DEFAULT ((1)) NOT NULL,
    CONSTRAINT [PK_Policy] PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [FK_Policy_ParentPolicy] FOREIGN KEY ([ParentID]) REFERENCES [dbo].[Policy] ([ID]),
    CONSTRAINT [FK_Policy_PolicyType] FOREIGN KEY ([PolicyTypeID]) REFERENCES [dbo].[PolicyType] ([ID])
);


GO

CREATE TRIGGER [dbo].[Policy_AfterDelete]
   ON  [dbo].[Policy] 
   AFTER DELETE
AS 
	SET NOCOUNT ON;
	
	declare @type varchar(50) = 'Policy'

	insert into [queue].[ObjectVersion] ([Object], ObjectID, ResourceID, [Date], [Action], ActionObject, ActionObjectID)
		select @type, ID, coalesce(UpdatedBy, 0), coalesce(UpdatedOn, getutcdate()), 'Removed', @type, ID from deleted

	insert into [queue].[ObjectIndex] ([Object], ObjectID, ResourceID, [Date], [Action], ActionObject, ActionObjectID)
		select @type, ID, coalesce(UpdatedBy, 0), coalesce(UpdatedOn, getutcdate()), 'D', @type, ID from deleted

	DELETE	O
	FROM	cache.ObjectDetails O
			inner join deleted d
	ON		O.[Object] = @type and O.ObjectID = d.ID

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

GO

CREATE TRIGGER [dbo].[Policy_AfterInsert]
   ON  [dbo].[Policy] 
   AFTER INSERT
AS 
	SET NOCOUNT ON;
	insert into [queue].[ObjectVersion] ([Object], ObjectID, ResourceID, [Date], [Action], ActionObject, ActionObjectID)
		select 'Policy', ID, coalesce(UpdatedBy, 0), coalesce(UpdatedOn, getutcdate()), 'Created', 'Policy', ID from inserted

	insert into [queue].[ObjectIndex] ([Object], ObjectID, ResourceID, [Date], [Action], ActionObject, ActionObjectID)
		select 'Policy', ID, coalesce(UpdatedBy, 0), coalesce(UpdatedOn, getutcdate()), 'A', 'Policy', ID from inserted

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
		exec [cache].[SynchronizeObjectDetails] 'Policy', @thisID
		set @current = @current + 1
	end
	
	declare @IDs table (ID int);

	with d AS
	(
		SELECT	ParentID, 
				ID
		FROM	inserted	
		UNION ALL
		SELECT	C.ParentID, 
				C.ID
		FROM	Policy	C
				INNER JOIN d AS P ON P.ID = C.ParentID
	)

	insert into @IDs
		select ID from d

	update	T
	set		T.TextPath = utility.GetBreadcrumbStringWrapper('Policy', S.ID, '/'),
			T.[Level] = utility.GetObjectLevelWrapper('Policy', S.ID)
	from	Policy T
			inner join @tbl S on S.ID = T.ID

	update	T
	set		T.TextPath = utility.GetBreadcrumbStringWrapper('Policy', S.ID, '/')
	from	cache.ObjectDetails T
			inner join @tbl S on T.[Object] = 'Policy' and S.ID = T.ObjectID

GO
CREATE TRIGGER [dbo].[Policy_AfterUpdate]
   ON  [dbo].[Policy] 
   AFTER UPDATE
AS 
	SET NOCOUNT ON;
	insert into [queue].[ObjectVersion] ([Object], ObjectID, ResourceID, [Date], [Action], ActionObject, ActionObjectID)
		select 'Policy', ID, coalesce(UpdatedBy, 0), coalesce(UpdatedOn, getutcdate()), 'Updated', 'Policy', ID from inserted

	insert into [queue].[ObjectIndex] ([Object], ObjectID, ResourceID, [Date], [Action], ActionObject, ActionObjectID)
		select 'Policy', ID, coalesce(UpdatedBy, 0), coalesce(UpdatedOn, getutcdate()), 'U', 'Policy', ID from inserted

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
		exec [cache].[SynchronizeObjectDetails] 'Policy', @thisID
		set @current = @current + 1
	end

	declare @IDs table (ID int);

	with d AS
	(
		SELECT	ParentID, 
				ID
		FROM	inserted	
		UNION ALL
		SELECT	C.ParentID, 
				C.ID
		FROM	Policy	C
				INNER JOIN d AS P ON P.ID = C.ParentID
	)

	insert into @IDs
		select ID from d

	update	T
	set		T.TextPath = utility.GetBreadcrumbStringWrapper('Policy', S.ID, '/'),
			T.[Level] = utility.GetObjectLevelWrapper('Policy', S.ID)
	from	Policy T
			inner join @tbl S on S.ID = T.ID

	update	T
	set		T.TextPath = utility.GetBreadcrumbStringWrapper('Policy', S.ID, '/')
	from	cache.ObjectDetails T
			inner join @tbl S on T.[Object] = 'Policy' and S.ID = T.ObjectID
