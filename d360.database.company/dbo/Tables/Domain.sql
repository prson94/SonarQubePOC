CREATE TABLE [dbo].[Domain] (
    [ID]                         INT             IDENTITY (1, 1) NOT NULL,
    [ParentID]                   INT             NULL,
    [DomainTypeID]               INT             NOT NULL,
    [EnforceParentItemSelection] BIT             CONSTRAINT [DF_Domain_EnforceParentItemSelection] DEFAULT ((0)) NOT NULL,
    [Name]                       NVARCHAR (250)  NOT NULL,
    [Description]                NVARCHAR (4000) NULL,
    [DomainGroupID]              INT             NULL,
    [Path]                       XML             NULL,
    [UpdatedOn]                  DATETIME        NULL,
    [UpdatedBy]                  INT             NULL,
    CONSTRAINT [PK_Domain] PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [FK_Domain_DomainType] FOREIGN KEY ([DomainTypeID]) REFERENCES [dbo].[DomainType] ([ID]) ON DELETE CASCADE,
    CONSTRAINT [FK_Domain_ParentDomain] FOREIGN KEY ([ParentID]) REFERENCES [dbo].[Domain] ([ID])
);


GO
CREATE NONCLUSTERED INDEX [IX_Domain_DomainGroupID]
    ON [dbo].[Domain]([DomainGroupID] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_Domain_DomainTypeID]
    ON [dbo].[Domain]([DomainTypeID] ASC);


GO

CREATE TRIGGER [dbo].[Domain_AfterDelete]
   ON  [dbo].[Domain] 
   AFTER DELETE
AS 
	SET NOCOUNT ON;
	
	declare @type varchar(50) = 'Domain'

	insert into [queue].ObjectVersion ([Object], ObjectID, ResourceID, [Date], [Action], ActionObject, ActionObjectID)
		select	@type, ID, coalesce(UpdatedBy, 0), coalesce(UpdatedOn, getutcdate()), 'Removed', 'DomainType', DomainTypeID from deleted

	insert into [queue].[ObjectIndex] ([Object], ObjectID, ResourceID, [Date], [Action], ActionObject, ActionObjectID)
		select @type, ID, coalesce(UpdatedBy, 0), coalesce(UpdatedOn, getutcdate()), 'D', @type, ID from deleted

	DELETE	O
	FROM	cache.ObjectDetails O
			inner join deleted d
	ON		O.[Object] = @type and O.ObjectID = d.ID

	DELETE	C
	FROM	CommentRelation as C
			INNER JOIN deleted AS d
	ON		C.ObjectType = @type and C.ObjectID = d.ID

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

GO

CREATE TRIGGER [dbo].[Domain_AfterInsert]
   ON  [dbo].[Domain] 
   AFTER INSERT
AS 
	SET NOCOUNT ON;
	declare @type varchar(50) = 'Domain'

	insert into [queue].ObjectVersion ([Object], ObjectID, ResourceID, [Date], [Action], ActionObject, ActionObjectID)
		select	@type, ID, coalesce(UpdatedBy, 0), coalesce(UpdatedOn, getutcdate()), 'Added', @type, ID from inserted

	insert into [queue].[ObjectIndex] ([Object], ObjectID, ResourceID, [Date], [Action], ActionObject, ActionObjectID)
		select @type, ID, coalesce(UpdatedBy, 0), coalesce(UpdatedOn, getutcdate()), 'A', @type, ID from inserted

	update	T
	set		T.[Path] = utility.GetBreadcrumbWrapper(@type, S.ID)
	from	Domain T
			inner join inserted S on S.ID = T.ID


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
		exec [cache].[SynchronizeObjectDetails] 'Domain', @thisID
		set @current = @current + 1
	end

GO

CREATE TRIGGER [dbo].[Domain_AfterUpdate]
   ON  [dbo].[Domain] 
   AFTER UPDATE
AS 
	SET NOCOUNT ON;
	declare @type varchar(50) = 'Domain'

	insert into [queue].ObjectVersion ([Object], ObjectID, ResourceID, [Date], [Action], ActionObject, ActionObjectID)
		select	@type, ID, coalesce(UpdatedBy, 0), coalesce(UpdatedOn, getutcdate()), 'Updated', @type, ID from inserted
	insert into [queue].[ObjectIndex] ([Object], ObjectID, ResourceID, [Date], [Action], ActionObject, ActionObjectID)
		select @type, ID, coalesce(UpdatedBy, 0), coalesce(UpdatedOn, getutcdate()), 'U', @type, ID from inserted

	update	T
	set		T.[Path] = utility.GetBreadcrumbWrapper(@type, S.ID)
	from	Domain T
			inner join inserted S on S.ID = T.ID

	UPDATE	R
	SET		R.SourceObjectName = A.Name
	FROM	cache.Relationships R INNER JOIN inserted A ON R.SourceObject = @type and R.SourceObjectID = A.ID

	UPDATE	R
	SET		R.TargetObjectName = A.Name
	FROM	cache.Relationships R INNER JOIN inserted A ON R.TargetObject = @type and R.TargetObjectID = A.ID

	UPDATE	F
	set		F.FormattedValue = utility.GetFormattedFieldLookupValue(FT.Type, FT.LookupDisplayFormat, FT.LookupObjectType, FT.LookupObjectID, F.Value)
	FROM	Field F
			inner join FieldType FT on FT.ID = F.FieldTypeID and FT.LookupObjectType = @type 
			inner join inserted A on A.ID = FT.LookupObjectID

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
		exec [cache].[SynchronizeObjectDetails] 'Domain', @thisID
		set @current = @current + 1
	end
