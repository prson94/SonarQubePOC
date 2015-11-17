CREATE TABLE [dbo].[ArtifactType] (
    [ID]                    INT             IDENTITY (1, 1) NOT NULL,
    [ParentID]              INT             NULL,
    [Name]                  NVARCHAR (250)  NOT NULL,
    [Description]           NVARCHAR (4000) NULL,
    [CanOwnFusion]          BIT             CONSTRAINT [DF_Artifact_CanOwnFusion] DEFAULT ((0)) NOT NULL,
    [AllowRelatedArtifacts] BIT             NOT NULL,
    [UpdatedOn]             DATETIME        NULL,
    [UpdatedBy]             INT             NULL,
    [AllowHierarchy]        BIT             CONSTRAINT [DF_Artifact_AllowHierarchy] DEFAULT ((0)) NOT NULL,
    CONSTRAINT [PK_ArtifactType] PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [FK_ArtifactType_ParentArtifactType] FOREIGN KEY ([ParentID]) REFERENCES [dbo].[ArtifactType] ([ID])
);


GO


CREATE TRIGGER [dbo].[ArtifactType_AfterDelete]
   ON  [dbo].[ArtifactType] 
   AFTER DELETE
AS 
	SET NOCOUNT ON;
	declare @type varchar(50) = 'ArtifactType'

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


GO
CREATE TRIGGER [dbo].[ArtifactType_AfterInsert]
   ON  [dbo].[ArtifactType] 
   AFTER INSERT
AS 
	SET NOCOUNT ON;
	insert into [queue].[ObjectVersion] ([Object], ObjectID, ResourceID, [Date], [Action], ActionObject, ActionObjectID)
		select 'ArtifactType', ID, coalesce(UpdatedBy, 0), coalesce(UpdatedOn, getutcdate()), 'Created', 'ArtifactType', ID from inserted

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
		exec [cache].[SynchronizeObjectDetails] 'ArtifactType', @thisID
		--exec utility.CalculateStatistics 'ArtifactType', @thisID
		set @current = @current + 1
	end

GO
CREATE TRIGGER [dbo].[ArtifactType_AfterUpdate]
   ON  [dbo].[ArtifactType] 
   AFTER UPDATE
AS 
	SET NOCOUNT ON;
	insert into [queue].[ObjectVersion] ([Object], ObjectID, ResourceID, [Date], [Action], ActionObject, ActionObjectID)
		select 'ArtifactType', ID, coalesce(UpdatedBy, 0), coalesce(UpdatedOn, getutcdate()), 'Updated', 'ArtifactType', ID from inserted

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
		exec [cache].[SynchronizeObjectDetails] 'ArtifactType', @thisID
		--exec utility.CalculateStatistics 'ArtifactType', @thisID
		set @current = @current + 1
	end
