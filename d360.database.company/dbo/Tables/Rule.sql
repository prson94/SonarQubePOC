CREATE TABLE [dbo].[Rule] (
    [ID]          INT             IDENTITY (1, 1) NOT NULL,
    [Name]        NVARCHAR (250)  NOT NULL,
    [Description] NVARCHAR (4000) NULL,
    [RuleType]    INT             NOT NULL,
    [UpdatedOn]   DATETIME        NULL,
    [UpdatedBy]   INT             NULL,
    CONSTRAINT [PK_Rule] PRIMARY KEY CLUSTERED ([ID] ASC)
);


GO
CREATE TRIGGER [dbo].[Rule_AfterUpdate]
   ON  [dbo].[Rule] 
   AFTER UPDATE
AS 
	SET NOCOUNT ON;
	declare @type varchar(50) = 'Rule'

	insert into [queue].[ObjectVersion] ([Object], ObjectID, ResourceID, [Date], [Action], ActionObject, ActionObjectID)
		select @type, ID, coalesce(UpdatedBy, 0), coalesce(UpdatedOn, getutcdate()), 'Updated', @type, ID from inserted

	insert into [queue].[ObjectIndex] ([Object], ObjectID, ResourceID, [Date], [Action], ActionObject, ActionObjectID)
		select @type, ID, coalesce(UpdatedBy, 0), coalesce(UpdatedOn, getutcdate()), 'U', @type, ID from inserted

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
		exec [cache].[SynchronizeObjectDetails] 'Rule', @thisID
		set @current = @current + 1
	end

GO
CREATE TRIGGER [dbo].[Rule_AfterInsert]
   ON  [dbo].[Rule] 
   AFTER INSERT
AS 
	SET NOCOUNT ON;
	declare @type varchar(50) = 'Rule'

	insert into [queue].[ObjectVersion] ([Object], ObjectID, ResourceID, [Date], [Action], ActionObject, ActionObjectID)
		select @type, ID, coalesce(UpdatedBy, 0), coalesce(UpdatedOn, getutcdate()), 'Created', @type, ID from inserted

	insert into [queue].[ObjectIndex] ([Object], ObjectID, ResourceID, [Date], [Action], ActionObject, ActionObjectID)
		select @type, ID, coalesce(UpdatedBy, 0), coalesce(UpdatedOn, getutcdate()), 'A', @type, ID from inserted

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
		exec [cache].[SynchronizeObjectDetails] 'Rule', @thisID
		set @current = @current + 1
	end

GO


CREATE TRIGGER [dbo].[Rule_AfterDelete]
   ON  [dbo].[Rule] 
   AFTER DELETE
AS 
	SET NOCOUNT ON;

	declare @type varchar(50) = 'Rule'

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
