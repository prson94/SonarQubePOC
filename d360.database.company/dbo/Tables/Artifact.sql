CREATE TABLE [dbo].[Artifact] (
    [ID]                INT             IDENTITY (1, 1) NOT NULL,
    [ParentID]          INT             NULL,
    [ArtifactTypeID]    INT             NOT NULL,
    [Name]              NVARCHAR (250)  NOT NULL,
    [Description]       NVARCHAR (MAX)  NULL,
    [Status]            NVARCHAR (25)   NOT NULL,
    [TextPath]          NVARCHAR (1000) NULL,
    [DateLastCertified] DATETIME        NULL,
    [UpdatedOn]         DATETIME        NULL,
    [UpdatedBy]         INT             NULL,
    [TaxonomyTypeID]    INT             CONSTRAINT [DF_Artifact_TaxonomyTypeID] DEFAULT ((0)) NOT NULL,
    [CreatedOn]         DATETIME        CONSTRAINT [DF_Artifact_CreatedOn] DEFAULT (getdate()) NOT NULL,
    [Visible] BIT NOT NULL DEFAULT ((1)), 
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

CREATE NONCLUSTERED INDEX [IX_Artifact_Visible] 
	ON [dbo].Artifact ( [Visible] ASC );
go

CREATE TRIGGER [dbo].[Artifact_AfterDelete]
	ON [dbo].[Artifact]
	AFTER DELETE
AS
	SET NOCOUNT ON;
	INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
        select 'Delete', [queue].WriteIndexXml('Removed', 'ArtifactType', ArtifactTypeID, coalesce(UpdatedBy, 0)), 'Artifact', ID from deleted;

	insert into reporting.Global_Audit (Object, ObjectID, ObjectName, ResourceID, Date, Action, ActionObject, ActionObjectID, ActionObjectTypeName, ActionObjectName, ActionDescription)
		select 'Artifact', O.ID, O.TextPath, coalesce(O.UpdatedBy, 0), coalesce(O.UpdatedOn, getutcdate()), 'Deleted', 'Artifact', O.ID, T.Name, O.TextPath, 'This artifact has been removed.' from deleted O inner join ArtifactType T on T.ID = O.ArtifactTypeID;
GO

CREATE TRIGGER [dbo].[Artifact_AfterUpsert]
   ON  [dbo].[Artifact] 
   AFTER INSERT, UPDATE
AS 
	SET NOCOUNT ON;
	declare @ot varchar(50) = 'Artifact'

	INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
        select	case 
					when D.ID is not null then 'Update'
					else 'Add'
				end, 
				[queue].WriteIndexXml('', @ot, I.ID, coalesce(I.UpdatedBy, 0)), 
				@ot, 
				I.ID 
		from	inserted I
				left join deleted D on D.ID = I.ID;
	
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
	set		T.TextPath = utility.GetBreadcrumbString(@ot, S.ID, '/')
	from	Artifact T
			inner join S on S.ID = T.ID;

	merge	[cache].[Object] as T
	using	(
			select	@ot as [Object],
					ID as ObjectID,
					'ArtifactType' as ObjectType,
					ArtifactTypeID as ObjectTypeID
			from	inserted
			) as S
	on		T.[Object] = S.[Object] and T.[ObjectID] = S.[ObjectID]
	when	matched then
			update set	T.[ObjectType] = S.[ObjectType],
						T.[ObjectTypeID] = S.[ObjectTypeID]
	when	not matched then
			insert	( [Object], [ObjectID], [ObjectType], [ObjectTypeID] )
			values	( S.[Object], S.[ObjectID], S.[ObjectType], S.[ObjectTypeID] );
GO

