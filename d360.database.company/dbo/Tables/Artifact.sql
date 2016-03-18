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
    [CreatedOn]			DATETIME		CONSTRAINT [DF_Artifact_CreatedOn] DEFAULT(CURRENT_TIMESTAMP) NOT NULL, 
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
	declare @ot varchar(50) = 'Artifact'
	INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
        select 'Add', [queue].WriteIndexXml('', @ot, ID, coalesce(UpdatedBy, 0)), @ot, ID from inserted
	
	update	T
	set		T.TextPath = utility.GetBreadcrumbStringWrapper(@ot, S.ID, '/'),
			T.[Path] = utility.GetBreadcrumbWrapper(@ot, S.ID)
	from	Artifact T
			inner join inserted S on S.ID = T.ID

	merge	[cache].[Object] as T
	using	(
			select	'Artifact' as [Object],
					ID as ObjectID,
					--Name as Name,
					--TextPath as TextPath,
					'ArtifactType' as ObjectType,
					ArtifactTypeID as ObjectTypeID--,
					--[dbo].[GenerateObjectUrl]('Artifact', ArtifactTypeID, ID) as Url
			from	inserted
			) as S
	on		T.[Object] = S.[Object] and T.[ObjectID] = S.[ObjectID]
	when	matched then
			update set	T.[ObjectType] = S.[ObjectType],
						T.[ObjectTypeID] = S.[ObjectTypeID]--,
						--T.[Name] = S.[Name],
						--T.[TextPath] = S.[TextPath]
	when	not matched then
			insert	(
					[Object], [ObjectID], [ObjectType], [ObjectTypeID]--, [Name], [TextPath], [Url]
					)
			values	(
					S.[Object], S.[ObjectID], S.[ObjectType], S.[ObjectTypeID]--, S.[Name], S.[TextPath], S.[Url]
					);

GO
CREATE TRIGGER [dbo].[Artifact_AfterUpdate]
   ON  [dbo].[Artifact] 
   AFTER UPDATE
AS 
	SET NOCOUNT ON;
	declare @ot varchar(50) = 'Artifact'
	INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
        select 'Update', [queue].WriteIndexXml('', @ot, ID, coalesce(UpdatedBy, 0)), @ot, ID from inserted;

	with S as	(
				select	ID,
						ParentID
				from	inserted
				union all
				select	A.ID,
						A.ParentID
				from	Artifact A
						inner join S on S.ID = A.ParentID
				)
	update	T
	set		T.TextPath = utility.GetBreadcrumbString('Artifact', S.ID, '/')
	from	Artifact T
			inner join S on S.ID = T.ID


	merge	[cache].[Object] as T
	using	(
			select	'Artifact' as [Object],
					ID as ObjectID,
					--Name as Name,
					--TextPath as TextPath,
					'ArtifactType' as ObjectType,
					ArtifactTypeID as ObjectTypeID--,
					--[dbo].[GenerateObjectUrl]('Artifact', ArtifactTypeID, ID) as Url
			from	inserted
			) as S
	on		T.[Object] = S.[Object] and T.[ObjectID] = S.[ObjectID]
	when	matched then
			update set	T.[ObjectType] = S.[ObjectType],
						T.[ObjectTypeID] = S.[ObjectTypeID]--,
						--T.[Name] = S.[Name],
						--T.[TextPath] = S.[TextPath]
	when	not matched then
			insert	(
					[Object],[ObjectID], --[Name], [TextPath], 
					[ObjectType], [ObjectTypeID]--, [Url]
					)
			values	(
					S.[Object], S.[ObjectID], --S.[Name], S.[TextPath], 
					S.[ObjectType], S.[ObjectTypeID]--, S.[Url]
					);

GO

CREATE TRIGGER [dbo].[Artifact_AfterDelete]
	ON [dbo].[Artifact]
	AFTER DELETE
AS
	SET NOCOUNT ON;
	INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
        select 'Delete', [queue].WriteIndexXml('Removed', 'ArtifactType', ArtifactTypeID, coalesce(UpdatedBy, 0)), 'Artifact', ID from deleted


