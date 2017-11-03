CREATE TABLE [dbo].[Taxonomy] (
    [ID]             INT             IDENTITY (1, 1) NOT NULL,
    [ParentID]       INT             NULL,
    [TaxonomyTypeID] INT             NOT NULL,
    [TextPath]       NVARCHAR (1000) NULL,
    [Level]          INT             NULL,
    [Visible]        BIT             CONSTRAINT [DF_Taxonomy_Visible] DEFAULT ((1)) NOT NULL,
    [SourceID]       NVARCHAR (250)  NULL,
    [KeyHash]        VARCHAR (250)   NULL,
    [FieldHash]      VARCHAR (250)   NULL,
    [UpdatedBy]      INT             NULL,
    [UpdatedOn]      DATETIME        NULL,
    [DisplayValue]   AS              ([utility].[GetObjectDisplayValueWrapper]('Taxonomy',[ID],[TaxonomyTypeID])),
    CONSTRAINT [PK_Taxonomy] PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [CK_Taxonomy_IDNotEqualParentID] CHECK ([ID]<>[ParentID]),
    CONSTRAINT [FK_Taxonomy_TaxonomyType] FOREIGN KEY ([TaxonomyTypeID]) REFERENCES [dbo].[TaxonomyType] ([ID]) ON DELETE CASCADE
);


GO

CREATE NONCLUSTERED INDEX [IX_Taxonomy_TaxonomyTypeID-ParentID]
    ON [dbo].[Taxonomy]([TaxonomyTypeID] ASC, [ParentID] ASC);
GO

CREATE NONCLUSTERED INDEX [IX_Taxonomy_Visible] 
	ON [dbo].Taxonomy ( Visible ASC );
go
CREATE TRIGGER [dbo].[Taxonomy_AfterDelete]
	ON [dbo].[Taxonomy]
	AFTER DELETE
AS
	SET NOCOUNT ON
	update	Asset
	set		[State] = 3
	where	Object = 'Taxonomy' and ObjectID in (select ID from deleted)

	delete Asset where Object = 'Taxonomy' and ObjectID in (select ID from deleted)
	--INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
 --       select 'Delete', [queue].WriteIndexXml('Removed', 'ArtifactType', ArtifactTypeID, coalesce(UpdatedBy, 0)), 'Artifact', ID from deleted;

	--insert into reporting.Global_Audit (Object, ObjectID, ObjectName, ResourceID, Date, Action, ActionObject, ActionObjectID, ActionObjectTypeName, ActionObjectName, ActionDescription)
	--	select 'Artifact', O.ID, O.TextPath, coalesce(O.UpdatedBy, 0), coalesce(O.UpdatedOn, getutcdate()), 'Deleted', 'Artifact', O.ID, T.Name, O.TextPath, 'This artifact has been removed.' from deleted O inner join ArtifactType T on T.ID = O.ArtifactTypeID;
GO

GO
CREATE TRIGGER [dbo].[Taxonomy_AfterUpdate]
   ON  [dbo].[Taxonomy] 
   AFTER UPDATE
AS 
	SET NOCOUNT ON
	update	T
	set		T.UpdatedBy = S.UpdatedBy,
			T.UpdatedOn = S.UpdatedOn
	from	Asset T
			inner join inserted S on T.Object = 'Taxonomy' and T.ObjectID = S.ID

	--declare @ot varchar(50) = 'Artifact'

	--INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
 --       select	case 
	--				when D.ID is not null then 'Update'
	--				else 'Add'
	--			end, 
	--			[queue].WriteIndexXml('', @ot, I.ID, coalesce(I.UpdatedBy, 0)), 
	--			@ot, 
	--			I.ID 
	--	from	inserted I
	--			left join deleted D on D.ID = I.ID;
	
	--with S as	(
	--			select	ID,
	--					ParentID
	--			from	inserted
	--			union all
	--			select	A.ID,
	--					A.ParentID
	--			from	Artifact A
	--					inner join S on S.ID = A.ParentID
	--			)
	----update	T
	----set		T.TextPath = utility.GetBreadcrumbString(@ot, S.ID, '/')
	----from	Artifact T
	----		inner join S on S.ID = T.ID;

	--merge	[cache].[Object] as T
	--using	(
	--		select	@ot as [Object],
	--				ID as ObjectID,
	--				'ArtifactType' as ObjectType,
	--				ArtifactTypeID as ObjectTypeID
	--		from	inserted
	--		) as S
	--on		T.[Object] = S.[Object] and T.[ObjectID] = S.[ObjectID]
	--when	matched then
	--		update set	T.[ObjectType] = S.[ObjectType],
	--					T.[ObjectTypeID] = S.[ObjectTypeID]
	--when	not matched then
	--		insert	( [Object], [ObjectID], [ObjectType], [ObjectTypeID] )
	--		values	( S.[Object], S.[ObjectID], S.[ObjectType], S.[ObjectTypeID] );
GO
CREATE TRIGGER [dbo].[Taxonomy_AfterInsert]
   ON  [dbo].[Taxonomy] 
   AFTER INSERT
AS 
	SET NOCOUNT ON
	INSERT INTO [dbo].[Asset] ([AssetTypeID],[State],[Object],[ObjectID],[CreatedOn],[CreatedBy],[UpdatedOn],[UpdatedBy])
		SELECT	T.ID, 1, 'Taxonomy', O.ID, O.[UpdatedOn], O.[UpdatedBy], O.[UpdatedOn], O.[UpdatedBy]
		FROM	inserted O inner join  AssetType T on T.Object = 'TaxonomyType' and T.ObjectID = O.TaxonomyTypeID

	--declare @ot varchar(50) = 'Artifact'

	--INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
 --       select	case 
	--				when D.ID is not null then 'Update'
	--				else 'Add'
	--			end, 
	--			[queue].WriteIndexXml('', @ot, I.ID, coalesce(I.UpdatedBy, 0)), 
	--			@ot, 
	--			I.ID 
	--	from	inserted I
	--			left join deleted D on D.ID = I.ID;
	
	--with S as	(
	--			select	ID,
	--					ParentID
	--			from	inserted
	--			union all
	--			select	A.ID,
	--					A.ParentID
	--			from	Artifact A
	--					inner join S on S.ID = A.ParentID
	--			)
	----update	T
	----set		T.TextPath = utility.GetBreadcrumbString(@ot, S.ID, '/')
	----from	Artifact T
	----		inner join S on S.ID = T.ID;

	--merge	[cache].[Object] as T
	--using	(
	--		select	@ot as [Object],
	--				ID as ObjectID,
	--				'ArtifactType' as ObjectType,
	--				ArtifactTypeID as ObjectTypeID
	--		from	inserted
	--		) as S
	--on		T.[Object] = S.[Object] and T.[ObjectID] = S.[ObjectID]
	--when	matched then
	--		update set	T.[ObjectType] = S.[ObjectType],
	--					T.[ObjectTypeID] = S.[ObjectTypeID]
	--when	not matched then
	--		insert	( [Object], [ObjectID], [ObjectType], [ObjectTypeID] )
	--		values	( S.[Object], S.[ObjectID], S.[ObjectType], S.[ObjectTypeID] );