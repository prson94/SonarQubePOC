CREATE TABLE [dbo].[Lookup] (
    [ID]           INT      IDENTITY (1, 1) NOT NULL,
    [LookupTypeID] INT      NOT NULL,
    [UpdatedOn]    DATETIME NULL,
    [UpdatedBy]    INT      NULL,
    CONSTRAINT [PK_Lookup] PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [FK_Lookup_LookupType] FOREIGN KEY ([LookupTypeID]) REFERENCES [dbo].[LookupType] ([ID]) ON DELETE CASCADE
);


GO
CREATE NONCLUSTERED INDEX [IX_Lookup_LookupTypeID]
    ON [dbo].[Lookup]([LookupTypeID] ASC);


GO
CREATE TRIGGER [dbo].[Lookup_AfterDelete]
   ON  [dbo].[Lookup] 
   AFTER DELETE
AS 
	SET NOCOUNT ON;
	insert into [queue].[ObjectVersion] ([Object], ObjectID, ResourceID, [Date], [Action], ActionObject, ActionObjectID)
		select 'LookupType', LookupTypeID, coalesce(UpdatedBy, 0), coalesce(UpdatedOn, getutcdate()), 'Removed', 'Lookup', ID from deleted

	DELETE	O
	FROM	cache.ObjectDetails O
			inner join deleted d
	ON		O.[Object] = 'LookupType' and O.ObjectID = d.ID

GO
CREATE TRIGGER [dbo].[Lookup_AfterInsert]
   ON  [dbo].[Lookup] 
   AFTER INSERT
AS 
	SET NOCOUNT ON;
	insert into [queue].[ObjectVersion] ([Object], ObjectID, ResourceID, [Date], [Action], ActionObject, ActionObjectID)
		select 'LookupType', LookupTypeID, coalesce(UpdatedBy, 0), coalesce(UpdatedOn, getutcdate()), 'Created', 'Lookup', ID from inserted

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
		exec [cache].[SynchronizeObjectDetails] 'Lookup', @thisID
		set @current = @current + 1
	end

	UPDATE	F
	set		F.FormattedValue = utility.GetFormattedFieldLookupValue(FT.Type, FT.LookupDisplayFormat, FT.LookupObjectType, FT.LookupObjectID, F.Value)
	FROM	Field F
			inner join FieldType FT on FT.ID = F.FieldTypeID and FT.LookupObjectType = 'Lookup' 
			inner join inserted A on A.LookupTypeID = FT.LookupObjectID and cast(A.ID as nvarchar(15)) = F.Value

GO
CREATE TRIGGER [dbo].[Lookup_AfterUpdate]
   ON  [dbo].[Lookup] 
   AFTER UPDATE
AS 
	SET NOCOUNT ON;
	insert into [queue].[ObjectVersion] ([Object], ObjectID, ResourceID, [Date], [Action], ActionObject, ActionObjectID)
		select 'LookupType', LookupTypeID, coalesce(UpdatedBy, 0), coalesce(UpdatedOn, getutcdate()), 'Updated', 'Lookup', ID from inserted

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
		exec [cache].[SynchronizeObjectDetails] 'Lookup', @thisID
		set @current = @current + 1
	end

	UPDATE	F
	set		F.FormattedValue = utility.GetFormattedFieldLookupValue(FT.Type, FT.LookupDisplayFormat, FT.LookupObjectType, FT.LookupObjectID, F.Value)
	FROM	Field F
			inner join FieldType FT on FT.ID = F.FieldTypeID and FT.LookupObjectType = 'Lookup' 
			inner join inserted A on A.LookupTypeID = FT.LookupObjectID and cast(A.ID as nvarchar(15)) = F.Value
