CREATE TABLE [dbo].[Taxonomy] (
    [ID]             INT             IDENTITY (1, 1) NOT NULL,
    [ParentID]       INT             NULL,
    [TaxonomyTypeID] INT             NOT NULL,
    [Name]           NVARCHAR (250)  NOT NULL,
    [Description]    NVARCHAR (4000) NULL,
    [Path]           XML             NULL,
    [TextPath]       NVARCHAR (1000) NULL,
    [Level]          INT             NULL,
    [UpdatedOn]      DATETIME        NULL,
    [UpdatedBy]      INT             NULL,
    CONSTRAINT [PK_Taxonomy] PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [CK_Taxonomy_IDNotEqualParentID] CHECK ([ID]<>[ParentID]),
    CONSTRAINT [FK_Taxonomy_TaxonomyType] FOREIGN KEY ([TaxonomyTypeID]) REFERENCES [dbo].[TaxonomyType] ([ID]) ON DELETE CASCADE
);






GO
CREATE NONCLUSTERED INDEX [IX_Taxonomy_TaxonomyTypeID-ParentID]
    ON [dbo].[Taxonomy]([TaxonomyTypeID] ASC, [ParentID] ASC);


GO



GO


GO

CREATE TRIGGER [dbo].[Taxonomy_AfterDelete]
   ON  [dbo].[Taxonomy] 
   AFTER DELETE
AS 
	SET NOCOUNT ON;
	INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
		select 'Delete', [queue].WriteIndexXml('Removed', 'TaxonomyType', TaxonomyTypeID, coalesce(UpdatedBy, 0)), 'Taxonomy', ID from deleted

GO

CREATE TRIGGER [dbo].[Taxonomy_AfterUpsert]
   ON  [dbo].[Taxonomy] 
   AFTER INSERT, UPDATE
AS 
	SET NOCOUNT ON;
	declare @ot varchar(50) = 'Taxonomy'

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

		declare @tbl table (ID int);

		with d AS
		(
			SELECT	ParentID, 
					ID
			FROM	inserted
			UNION ALL
			SELECT	C.ParentID, 
					C.ID
			FROM	Taxonomy	C
					INNER JOIN d AS P ON P.ID = C.ParentID
		)

		insert into @tbl
			select ID from d

		update	T
		set		T.TextPath = utility.GetBreadcrumbStringWrapper(@ot, S.ID, '/'),
				T.[Level] = utility.GetObjectLevelWrapper(@ot, S.ID)
		from	Taxonomy T
				inner join @tbl S on S.ID = T.ID;

		merge	[cache].[Object] as T
		using	(
				select	@ot as [Object],
						ID as ObjectID,
						'TaxonomyType' as ObjectType,
						TaxonomyTypeID as ObjectTypeID
				from	inserted
				) as S
		on		T.[Object] = S.[Object] and T.[ObjectID] = S.[ObjectID]
		when	matched then
				update set	T.[ObjectType] = S.[ObjectType],
							T.[ObjectTypeID] = S.[ObjectTypeID]
		when	not matched then
				insert	( [Object],[ObjectID], [ObjectType], [ObjectTypeID] )
				values	( S.[Object], S.[ObjectID], S.[ObjectType], S.[ObjectTypeID] );