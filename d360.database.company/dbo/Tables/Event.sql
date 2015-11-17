CREATE TABLE [dbo].[Event] (
    [ID]           INT            IDENTITY (1, 1) NOT NULL,
    [EventGroupID] INT            NULL,
    [SourceID]     NVARCHAR (250) NULL,
    [Status]       VARCHAR (50)   CONSTRAINT [DF_Event_Status] DEFAULT ('Open') NOT NULL,
    [Date]         DATETIME       CONSTRAINT [DF_Event_Date] DEFAULT (getutcdate()) NOT NULL,
    [Criticality]  INT            NULL,
    CONSTRAINT [PK_Event] PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [FK_Event_EventGroup] FOREIGN KEY ([EventGroupID]) REFERENCES [dbo].[EventGroup] ([ID])
);


GO
CREATE NONCLUSTERED INDEX [IX_Event_EventGroupID]
    ON [dbo].[Event]([EventGroupID] ASC);


GO
CREATE TRIGGER [dbo].[Event_AfterDelete]
	ON [dbo].[Event]
	AFTER DELETE
	AS
	BEGIN
		SET NOCOUNT ON;
		
		declare @type varchar(50) = 'Event'

		DELETE	O
		FROM	cache.ObjectDetails O
				inner join deleted d
		ON		O.[Object] = @type and O.ObjectID = d.ID

		DELETE	F
		FROM	Field as F
				INNER JOIN deleted AS d
		ON		F.ObjectType = @type and F.ObjectID = d.ID
	END



GO
CREATE TRIGGER [dbo].[Event_AfterInsert]
	ON [dbo].[Event]
	FOR INSERT
AS
	BEGIN
		SET NOCOUNT ON;

		--insert into [queue].[ObjectVersion] ([Object], ObjectID, ResourceID, [Date], [Action], ActionObject, ActionObjectID)
		--	select 'Event', ID, coalesce(UpdatedBy, 0), coalesce(UpdatedOn, getutcdate()), 'Added', 'Event', ID from deleted

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
			exec [cache].[SynchronizeObjectDetails] 'Event', @thisID
			set @current = @current + 1
		end
	END

GO
CREATE TRIGGER [dbo].[Event_AfterUpdate]
	ON [dbo].[Event]
	FOR UPDATE
	AS
	BEGIN
		SET NOCOUNT ON;

		--insert into [queue].[ObjectVersion] ([Object], ObjectID, ResourceID, [Date], [Action], ActionObject, ActionObjectID)
		--	select 'Event', ID, coalesce(UpdatedBy, 0), coalesce(UpdatedOn, getutcdate()), 'Updated', 'Event', ID from deleted

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
			exec [cache].[SynchronizeObjectDetails] 'Event', @thisID
			set @current = @current + 1
		end
	END
