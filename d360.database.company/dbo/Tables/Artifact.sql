CREATE TABLE [dbo].[Artifact] (
    [ID]                INT             IDENTITY (1, 1) NOT NULL,
    [ParentID]          INT             NULL,
    [ArtifactTypeID]    INT             NOT NULL,
    [Name]              NVARCHAR (250)  NOT NULL,
    [Description]       NVARCHAR (4000) NULL,
    [Status]            NVARCHAR (25)   NOT NULL,
    [TextPath]          NVARCHAR (1000) NULL,
    [Path]              XML             NULL,
    [DateLastCertified] DATETIME        NULL,
    [UpdatedOn]         DATETIME        NULL,
    [UpdatedBy]         INT             NULL,
    [TaxonomyTypeID]    INT             CONSTRAINT [DF_Artifact_TaxonomyTypeID] DEFAULT ((0)) NOT NULL,
    CONSTRAINT [PK_Artifact] PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [FK_Artifact_ArtifactType] FOREIGN KEY ([ArtifactTypeID]) REFERENCES [dbo].[ArtifactType] ([ID]),
    CONSTRAINT [FK_Artifact_ParentArtifact] FOREIGN KEY ([ParentID]) REFERENCES [dbo].[Artifact] ([ID]),
    CONSTRAINT [FK_Artifact_TaxonomyType] FOREIGN KEY ([TaxonomyTypeID]) REFERENCES [dbo].[TaxonomyType] ([ID])
);


GO
CREATE NONCLUSTERED INDEX [IX_Artifact_ArtifactTypeID]
    ON [dbo].[Artifact]([ArtifactTypeID] ASC)
    INCLUDE([ID], [ParentID], [Name], [Description], [Status]);


GO
CREATE NONCLUSTERED INDEX [IX_Artifact_ArtifactTypeID-Status]
    ON [dbo].[Artifact]([ArtifactTypeID] ASC, [Status] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_Artifact_ParentID]
    ON [dbo].[Artifact]([ParentID] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_Artifact_TaxonomyTypeID]
    ON [dbo].[Artifact]([TaxonomyTypeID] ASC);


GO
CREATE TRIGGER [dbo].[Artifact_AfterInsert]
   ON  [dbo].[Artifact] 
   AFTER INSERT
AS 
	SET NOCOUNT ON;
	
	insert into [queue].[ObjectVersion] ([Object], ObjectID, ResourceID, [Date], [Action], ActionObject, ActionObjectID)
		select 'Artifact', ID, coalesce(UpdatedBy, 0), coalesce(UpdatedOn, getutcdate()), 'Created', 'Artifact', ID from inserted
	insert into [queue].[ObjectIndex] ([Object], ObjectID, ResourceID, [Date], [Action], ActionObject, ActionObjectID)
		select 'Artifact', ID, coalesce(UpdatedBy, 0), coalesce(UpdatedOn, getutcdate()), 'A', 'Artifact', ID from deleted
	
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
		exec [cache].[SynchronizeObjectDetails] 'Artifact', @thisID
		exec utility.CalculateStatistics 'Artifact', @thisID
		set @current = @current + 1
	end

	update	T
	set		T.TextPath = utility.GetBreadcrumbStringWrapper('Artifact', S.ID, '/'),
			T.[Path] = utility.GetBreadcrumbWrapper('Artifact', S.ID)
	from	Artifact T
			inner join inserted S on S.ID = T.ID

GO

CREATE TRIGGER [dbo].[Artifact_AfterUpdate]
   ON  [dbo].[Artifact] 
   AFTER UPDATE
AS 
	SET NOCOUNT ON;
	insert into [queue].[ObjectVersion] ([Object], ObjectID, ResourceID, [Date], [Action], ActionObject, ActionObjectID)
		select 'Artifact', ID, coalesce(UpdatedBy, 0), coalesce(UpdatedOn, getutcdate()), 'Updated', 'Artifact', ID from inserted
	insert into [queue].[ObjectIndex] ([Object], ObjectID, ResourceID, [Date], [Action], ActionObject, ActionObjectID)
		select 'Artifact', ID, coalesce(UpdatedBy, 0), coalesce(UpdatedOn, getutcdate()), 'U', 'Artifact', ID from deleted
	
	begin try
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
			exec [cache].[SynchronizeObjectDetails] 'Artifact', @thisID
			exec utility.CalculateStatistics 'Artifact', @thisID
			set @current = @current + 1
		end
	end try
	begin catch

	end catch

	--UPDATE	R
	--SET		R.SourceObjectName = A.Name
	--FROM	cache.Relationships R INNER JOIN inserted A ON R.SourceObject = 'Artifact' and R.SourceObjectID = A.ID

	--UPDATE	R
	--SET		R.TargetObjectName = A.Name
	--FROM	cache.Relationships R INNER JOIN inserted A ON R.TargetObject = 'Artifact' and R.TargetObjectID = A.ID

	--UPDATE	F
	--set		F.FormattedValue = utility.GetFormattedFieldLookupValue(FT.Type, FT.LookupDisplayFormat, FT.LookupObjectType, FT.LookupObjectID, F.Value)
	--FROM	Field F
	--		inner join FieldType FT on FT.ID = F.FieldTypeID and FT.LookupObjectType = 'Artifact' 
	--		inner join inserted A on A.ID = FT.LookupObjectID

	declare @tbl1 table (ID int);

	with d AS
	(
		SELECT	ParentID, 
				ID
		FROM	inserted	
		UNION ALL
		SELECT	C.ParentID, 
				C.ID
		FROM	Artifact	C
				INNER JOIN d AS P ON P.ID = C.ParentID
	)

	insert into @tbl1
		select ID from d

	update	T
	set		T.TextPath = utility.GetBreadcrumbStringWrapper('Artifact', S.ID, '/'),
			T.[Path] = utility.GetBreadcrumbWrapper('Artifact', S.ID)
	from	Artifact T
			inner join @tbl1 S on S.ID = T.ID

	update	T
	set		T.TextPath = utility.GetBreadcrumbStringWrapper('Artifact', S.ID, '/')
	from	cache.ObjectDetails T
			inner join @tbl1 S on T.[Object] = 'Artifact' and S.ID = T.ObjectID

	UPDATE	R
	SET		R.SourceObjectName = A.Name
	FROM	cache.Relationships R INNER JOIN inserted A ON R.SourceObject = 'Artifact' and R.SourceObjectID = A.ID

	UPDATE	R
	SET		R.TargetObjectName = A.Name
	FROM	cache.Relationships R INNER JOIN inserted A ON R.TargetObject = 'Artifact' and R.TargetObjectID = A.ID

	UPDATE	F
	set		F.FormattedValue = utility.GetFormattedFieldLookupValue(FT.Type, FT.LookupDisplayFormat, FT.LookupObjectType, FT.LookupObjectID, F.Value)
	FROM	Field F
			inner join FieldType FT on FT.ID = F.FieldTypeID and FT.LookupObjectType = 'Artifact' 
			inner join inserted A on A.ID = FT.LookupObjectID

GO
CREATE TRIGGER [dbo].[Artifact_AfterDelete]
	ON [dbo].[Artifact]
	AFTER DELETE
	AS
	BEGIN
		SET NOCOUNT ON;

		declare @type varchar(50) = 'Artifact'

		DELETE	F
		FROM	Field as F
				INNER JOIN deleted AS d
		ON		F.ObjectType = @type and F.ObjectID = d.ID

		DELETE	A
		FROM	Attribute as A
				INNER JOIN deleted AS d
		ON		A.ObjectType = @type and A.ObjectID = d.ID

		DELETE	C
		FROM	CommentRelation as C
				INNER JOIN deleted AS d
		ON		C.ObjectType = @type and C.ObjectID = d.ID

		DELETE	F
		FROM	Follow as F
				INNER JOIN deleted AS d
		ON		F.ObjectType = @type and F.ObjectID = d.ID

		DELETE	RA
		FROM	RelatedArtifact as RA
				INNER JOIN deleted AS d
		ON		RA.ArtifactID = d.ID

		DELETE	O
		FROM	cache.ObjectDetails O
				inner join deleted d
		ON		O.[Object] = @type and O.ObjectID = d.ID

		insert into [queue].[ObjectVersion] ([Object], ObjectID, ResourceID, [Date], [Action], ActionObject, ActionObjectID)
			select 'Artifact', ID, coalesce(UpdatedBy, 0), coalesce(UpdatedOn, getutcdate()), 'Removed', 'Artifact', ID from deleted

		insert into [queue].[ObjectIndex] ([Object], ObjectID, ResourceID, [Date], [Action], ActionObject, ActionObjectID)
			select 'Artifact', ID, coalesce(UpdatedBy, 0), coalesce(UpdatedOn, getutcdate()), 'D', 'Artifact', ID from deleted

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
	END


