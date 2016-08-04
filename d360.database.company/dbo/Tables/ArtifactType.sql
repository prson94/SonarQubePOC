CREATE TABLE [dbo].[ArtifactType] (
    [ID]                    INT            IDENTITY (1, 1) NOT NULL,
    [ParentID]              INT            NULL,
    [Name]                  NVARCHAR (250) NOT NULL,
    [Description]           NVARCHAR (MAX) NULL,
    [CanOwnFusion]          BIT            CONSTRAINT [DF_Artifact_CanOwnFusion] DEFAULT ((0)) NOT NULL,
    [AllowRelatedArtifacts] BIT            NOT NULL,
    [UpdatedOn]             DATETIME       NULL,
    [UpdatedBy]             INT            NULL,
    [AllowHierarchy]        BIT            CONSTRAINT [DF_Artifact_AllowHierarchy] DEFAULT ((0)) NOT NULL,
    CONSTRAINT [PK_ArtifactType] PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [FK_ArtifactType_ParentArtifactType] FOREIGN KEY ([ParentID]) REFERENCES [dbo].[ArtifactType] ([ID])
);






GO

CREATE TRIGGER [dbo].[ArtifactType_AfterDelete]
   ON  [dbo].[ArtifactType] 
   AFTER DELETE
AS 
	SET NOCOUNT ON;
	declare @ot varchar(50) = 'ArtifactType'
	INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
        select 'Delete', [queue].WriteIndexXml('Removed', @ot, ID, coalesce(UpdatedBy, 0)), @ot, ID from deleted


GO
CREATE TRIGGER [dbo].[ArtifactType_AfterInsert]
   ON  [dbo].[ArtifactType] 
   AFTER INSERT
AS 
	SET NOCOUNT ON;
	declare @ot varchar(50) = 'ArtifactType'
	INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
        select 'Add', [queue].WriteIndexXml('', @ot, ID, coalesce(UpdatedBy, 0)), @ot, ID from inserted

	merge	[cache].[Object] as T
	using	(
			select	'ArtifactType' as [Object],
					ID as ObjectID,
					'ArtifactType' as ObjectType,
					ID as ObjectTypeID
			from	inserted
			) as S
	on		T.[Object] = S.[Object] and T.[ObjectID] = S.[ObjectID]
	when	not matched then
			insert	( [Object], [ObjectID], [ObjectType], [ObjectTypeID] )
			values	( S.[Object], S.[ObjectID], S.[ObjectType], S.[ObjectTypeID] );

GO
CREATE TRIGGER [dbo].[ArtifactType_AfterUpdate]
   ON  [dbo].[ArtifactType] 
   AFTER UPDATE
AS 
	SET NOCOUNT ON;
	declare @ot varchar(50) = 'ArtifactType'
	INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
        select 'Update', [queue].WriteIndexXml('', @ot, ID, coalesce(UpdatedBy, 0)), @ot, ID from inserted

	merge	[cache].[Object] as T
	using	(
			select	'ArtifactType' as [Object],
					ID as ObjectID,
					'ArtifactType' as ObjectType,
					ID as ObjectTypeID
			from	inserted
			) as S
	on		T.[Object] = S.[Object] and T.[ObjectID] = S.[ObjectID]
	when	matched then
			update set	T.[ObjectType] = S.[ObjectType],
						T.[ObjectTypeID] = S.[ObjectTypeID]
	when	not matched then
			insert	( [Object], [ObjectID], [ObjectType], [ObjectTypeID] )
			values	( S.[Object], S.[ObjectID], S.[ObjectType], S.[ObjectTypeID] );
