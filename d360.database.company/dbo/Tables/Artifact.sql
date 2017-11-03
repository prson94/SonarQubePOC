CREATE TABLE [dbo].[Artifact] (
    [ID]             INT            IDENTITY (1, 1) NOT NULL,
    [ParentID]       INT            NULL,
    [ArtifactTypeID] INT            NOT NULL,
    [UpdatedOn]      DATETIME       NULL,
    [UpdatedBy]      INT            NULL,
    [CreatedOn]      DATETIME       CONSTRAINT [DF_Artifact_CreatedOn] DEFAULT (getdate()) NOT NULL,
    [Visible]        BIT            CONSTRAINT [DF_Artifact_Visible] DEFAULT ((1)) NOT NULL,
    [SourceID]       NVARCHAR (250) NULL,
    [CreatedBy]      INT            NULL,
    [DisplayValue]   AS             ([utility].[GetObjectDisplayValueWrapper]('Artifact',[ID],[ArtifactTypeID])),
    [KeyHash]        AS             ([utility].[GetObjectHashWrapper]('Artifact',[ID],[ArtifactTypeID],(1))),
    [FieldHash]      AS             ([utility].[GetObjectHashWrapper]('Artifact',[ID],[ArtifactTypeID],(0))),
    CONSTRAINT [PK_Artifact] PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [FK_Artifact_ArtifactType] FOREIGN KEY ([ArtifactTypeID]) REFERENCES [dbo].[ArtifactType] ([ID]),
    CONSTRAINT [FK_Artifact_ParentArtifact] FOREIGN KEY ([ParentID]) REFERENCES [dbo].[Artifact] ([ID])
);


GO

CREATE NONCLUSTERED INDEX [IX_Artifact_ArtifactTypeID]
    ON [dbo].[Artifact]([ArtifactTypeID] ASC);


GO


GO

CREATE NONCLUSTERED INDEX [IX_Artifact_ParentID]
    ON [dbo].[Artifact]([ParentID] ASC);
GO


GO

CREATE NONCLUSTERED INDEX [IX_Artifact_Visible] 
	ON [dbo].Artifact ( [Visible] ASC );
go
CREATE TRIGGER [dbo].[Artifact_AfterDelete]
	ON [dbo].[Artifact]
	AFTER DELETE
AS
	--SET TRANSACTION ISOLATION LEVEL SNAPSHOT
	SET NOCOUNT ON
	update	Asset 
	set		[State] = 3
	where	Object = 'Artifact' and ObjectID in (select ID from deleted);

	delete Asset
	where Object = 'Artifact' and ObjectID in (select ID from deleted);
GO

GO
CREATE TRIGGER [dbo].[Artifact_AfterUpdate]
   ON  [dbo].[Artifact] 
   AFTER UPDATE
AS 
	SET NOCOUNT ON
	update	T
	set		T.UpdatedBy = S.UpdatedBy,
			T.UpdatedOn = S.UpdatedOn
	from	Asset T
			inner join inserted S on T.Object = 'Artifact' and T.ObjectID = S.ID

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
CREATE TRIGGER [dbo].[Artifact_AfterInsert]
   ON  [dbo].[Artifact] 
   AFTER INSERT
AS 
	SET NOCOUNT ON
	INSERT INTO [dbo].[Asset] ([AssetTypeID],[State],[Object],[ObjectID],[CreatedOn],[CreatedBy],[UpdatedOn],[UpdatedBy])
		SELECT	T.ID, 1, 'Artifact', O.ID, O.[CreatedOn], O.[CreatedBy], O.[UpdatedOn], O.[UpdatedBy]
		FROM	inserted O inner join  AssetType T on T.Object = 'ArtifactType' and T.ObjectID = O.ArtifactTypeID;
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