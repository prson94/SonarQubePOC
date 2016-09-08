CREATE TABLE [dbo].[Intersect] (
    [ID]              INT             IDENTITY (1, 1) NOT NULL,
    [IntersectTypeID] INT             NOT NULL,
    [Name]            AS              ([utility].[DeriveIntersectNameWrapper]([ID])),
    [Classification]  INT             NULL,
    [Description]     NVARCHAR (4000) NULL,
    [Subject]         VARCHAR (50)    NULL,
    [SubjectID]       INT             NULL,
    [Object]          VARCHAR (50)    NULL,
    [ObjectID]        INT             NULL,
    [Deleted]         BIT             NULL,
    [CreatedBy]       INT             NULL,
    [CreatedOn]       DATETIME        NULL,
    [UpdatedBy]       INT             NULL,
    [UpdatedOn]       DATETIME        NULL,
    CONSTRAINT [PK_Intersect] PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [FK_Intersect_IntersectType] FOREIGN KEY ([IntersectTypeID]) REFERENCES [dbo].[IntersectType] ([ID]) ON DELETE CASCADE
);












GO
CREATE NONCLUSTERED INDEX [IX_Intersect_IntersectTypeID]
    ON [dbo].[Intersect]([IntersectTypeID] ASC);


GO

CREATE TRIGGER [dbo].[Intersect_AfterUpsert]
	ON [dbo].[Intersect]
	FOR INSERT, UPDATE
AS
BEGIN
	SET NOCOUNT ON;
	INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
        select 'Add', [queue].WriteIndexXml('', 'Intersect', ID, 0), 'Intersect', ID from inserted
END
GO





GO

CREATE TRIGGER [dbo].[Intersect_AfterDelete]
   ON  [dbo].[Intersect] 
   AFTER DELETE
AS 
	set nocount on;
	INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
		select 'Delete', [queue].WriteIndexXml('Removed', 'Intersect', ID, 0), 'Intersect', ID from deleted


GO
CREATE NONCLUSTERED INDEX [IX_Intersect_Subject]
    ON [dbo].[Intersect]([Subject] ASC, [SubjectID] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_Intersect_Object]
    ON [dbo].[Intersect]([Object] ASC, [ObjectID] ASC);


GO

CREATE TRIGGER [dbo].[Intersect_AfterUpdate]
	ON [dbo].[Intersect]
	FOR UPDATE
AS
BEGIN
	SET NOCOUNT ON;

	declare @tbl table(ID int identity, IntersectID int, ResourceID int, Subject varchar(50), SubjectID int, Object varchar(50), ObjectID int)
	insert into @tbl
		select ID, UpdatedBy, Subject, SubjectID, Object, ObjectID from inserted;

	declare @current int = 1,
			@max int,
			@id int,
			@r int,
			@s varchar(50),
			@sid int,
			@o varchar(50),
			@oid int,
			@date datetime = getutcdate()

	select @max =max(ID) from @tbl

	while @current <= @max
	begin
		select	@id = IntersectID,
				@r = ResourceID,
				@s = coalesce(Subject, 'Intersect'),
				@sid = coalesce(SubjectID, IntersectID),
				@o = coalesce(Object, 'Intersect'),
				@oid = coalesce(ObjectID, IntersectID)
		from	@tbl
		where	ID = @current

		exec [cache].[SynchronizeObjectDetails] 'Intersect', @id
		exec [utility].[AddAuditEntry] @s, @sid, @r, @date, 'Updated', 'Intersect', @id
		exec [utility].[AddAuditEntry] @o, @oid, @r, @date, 'Updated', 'Intersect', @id

		merge cache.Relationship as T
		using (
				select	distinct
						S.IntersectID,
						S.IntersectTypeNodeID as SourceIntersectTypeNodeID, 
						S.ID as SourceIntersectNodeID,
						S.ObjectType as SourceObject,
						S.ObjectID as SourceObjectID,
						T.IntersectTypeNodeID as TargetIntersectTypeNodeID,
						T.ID as TargetIntersectNodeID,
						T.ObjectType as TargetObject,
						T.ObjectID as TargetObjectID
				from	dbo.IntersectNode S
						inner join dbo.IntersectNode T on T.IntersectID = S.IntersectID and T.ID <> S.ID
				where	S.IntersectID = @id
				) as S (
					IntersectID, 
					SourceIntersectTypeNodeID, SourceIntersectNodeID, SourceObject, SourceObjectID, 
					TargetIntersectTypeNodeID, TargetIntersectNodeID, TargetObject, TargetObjectID
					)
		on    (T.IntersectID = S.IntersectID and T.SourceObject = S.SourceObject and T.SourceObjectID = S.SourceObjectID)
		when not matched then
			insert (
					IntersectID, 
					SourceIntersectTypeNodeID, SourceIntersectNodeID, SourceObject, SourceObjectID, 
					TargetIntersectTypeNodeID, TargetIntersectNodeID, TargetObject, TargetObjectID
					)
			values (
					S.IntersectID, 
					S.SourceIntersectTypeNodeID, S.SourceIntersectNodeID, S.SourceObject, S.SourceObjectID, 
					S.TargetIntersectTypeNodeID, S.TargetIntersectNodeID, S.TargetObject, S.TargetObjectID
					);

		set @current = @current +1
	end;
END
GO
CREATE TRIGGER [dbo].[Intersect_AfterInsert]
	ON [dbo].[Intersect]
	FOR INSERT
AS
BEGIN
	SET NOCOUNT ON;

	declare @tbl table(ID int identity, IntersectID int, ResourceID int, Subject varchar(50), SubjectID int, Object varchar(50), ObjectID int)
	insert into @tbl
		select ID, UpdatedBy, Subject, SubjectID, Object, ObjectID from inserted;

	declare @current int = 1,
			@max int,
			@id int,
			@r int,
			@s varchar(50),
			@sid int,
			@o varchar(50),
			@oid int,
			@date datetime = getutcdate()

	select @max =max(ID) from @tbl

	while @current <= @max
	begin
		select	@id = IntersectID,
				@r = ResourceID,
				@s = coalesce(Subject, 'Intersect'),
				@sid = coalesce(SubjectID, IntersectID),
				@o = coalesce(Object, 'Intersect'),
				@oid = coalesce(ObjectID, IntersectID)
		from	@tbl
		where	ID = @current

		exec [cache].[SynchronizeObjectDetails] 'Intersect', @id
		exec [utility].[AddAuditEntry] @s, @sid, @r, @date, 'Created', 'Intersect', @id
		exec [utility].[AddAuditEntry] @o, @oid, @r, @date, 'Created', 'Intersect', @id

		exec cache.SynchronizeResponsibilitiesForObject @s, @sid
		--exec cache.SynchronizeResponsibilitiesForObject @o, @oid

		merge cache.Relationship as T
		using (
				select	distinct
						S.IntersectID,
						S.IntersectTypeNodeID as SourceIntersectTypeNodeID, 
						S.ID as SourceIntersectNodeID,
						S.ObjectType as SourceObject,
						S.ObjectID as SourceObjectID,
						T.IntersectTypeNodeID as TargetIntersectTypeNodeID,
						T.ID as TargetIntersectNodeID,
						T.ObjectType as TargetObject,
						T.ObjectID as TargetObjectID
				from	dbo.IntersectNode S
						inner join dbo.IntersectNode T on T.IntersectID = S.IntersectID and T.ID <> S.ID
				where	S.IntersectID = @id
				) as S (
					IntersectID, 
					SourceIntersectTypeNodeID, SourceIntersectNodeID, SourceObject, SourceObjectID, 
					TargetIntersectTypeNodeID, TargetIntersectNodeID, TargetObject, TargetObjectID
					)
		on    (T.IntersectID = S.IntersectID and T.SourceObject = S.SourceObject and T.SourceObjectID = S.SourceObjectID)
		when not matched then
			insert (
					IntersectID, 
					SourceIntersectTypeNodeID, SourceIntersectNodeID, SourceObject, SourceObjectID, 
					TargetIntersectTypeNodeID, TargetIntersectNodeID, TargetObject, TargetObjectID
					)
			values (
					S.IntersectID, 
					S.SourceIntersectTypeNodeID, S.SourceIntersectNodeID, S.SourceObject, S.SourceObjectID, 
					S.TargetIntersectTypeNodeID, S.TargetIntersectNodeID, S.TargetObject, S.TargetObjectID
					);

		set @current = @current +1
	end;
END