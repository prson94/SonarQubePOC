CREATE TABLE [dbo].[FusionAttribute] (
    [ID]                    INT             IDENTITY (1, 1) NOT NULL,
    [ParentID]              INT             NULL,
    [Name]                  NVARCHAR (250)  NOT NULL,
    [FusionID]              INT             NOT NULL,
    [FusionAttributeTypeID] INT             NOT NULL,
    [SourceID]              VARCHAR (250)   NULL,
    [Deleted]               BIT             CONSTRAINT [DF_FusionAttribute_Deleted] DEFAULT ((0)) NOT NULL,
    [Path]                  XML             NULL,
    [TextPath]              NVARCHAR (2500) NULL,
    CONSTRAINT [PK_FusionAttribute] PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [FK_FusionAttribute_Fusion] FOREIGN KEY ([FusionID]) REFERENCES [dbo].[Fusion] ([ID]) ON DELETE CASCADE,
    CONSTRAINT [FK_FusionAttribute_FusionAttributeType] FOREIGN KEY ([FusionAttributeTypeID]) REFERENCES [dbo].[FusionAttributeType] ([ID]) ON DELETE CASCADE,
    CONSTRAINT [FK_FusionAttribute_ParentFusionAttribute] FOREIGN KEY ([ParentID]) REFERENCES [dbo].[FusionAttribute] ([ID])
);


GO
CREATE NONCLUSTERED INDEX [IX_FusionAttribute_FusionAttributeTypeID]
    ON [dbo].[FusionAttribute]([FusionAttributeTypeID] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_FusionAttribute_FusionID]
    ON [dbo].[FusionAttribute]([FusionID] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_FusionAttribute_FusionID-FusionAttributeTypeID-ParentID]
    ON [dbo].[FusionAttribute]([FusionID] ASC, [FusionAttributeTypeID] ASC, [ParentID] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_FusionAttribute_FusionID-FusionAttributeTypeID-SourceID]
    ON [dbo].[FusionAttribute]([FusionID] ASC, [FusionAttributeTypeID] ASC, [SourceID] ASC);


GO
CREATE TRIGGER [dbo].[FusionAttribute_AfterUpsert]
       ON [dbo].[FusionAttribute]
       FOR INSERT, UPDATE
       AS
       BEGIN
             SET NOCOUNT ON;

             /*UPDATE     f
             SET          f.[Path] = utility.GetBreadcrumbWrapper('FusionAttribute', f.ID),
                           f.TextPath = utility.GetBreadcrumbStringWrapper('FusionAttribute', f.ID, '.')
             FROM   FusionAttribute AS f
                           INNER JOIN inserted AS i ON f.ID = i.ID*/

             /*begin try
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
                           exec [cache].[SynchronizeObjectDetails] 'FusionAttribute', @thisID
                           set @current = @current + 1
                    end
             end try
             begin catch
             end catch*/

        --insert into [queue].[ObjectCache] ([Object], ObjectID, NumberOfRetries)
        --         select 'FusionAttribute', ID, 0 from inserted

             
       END


GO
DISABLE TRIGGER [dbo].[FusionAttribute_AfterUpsert]
    ON [dbo].[FusionAttribute];


GO

CREATE TRIGGER [dbo].[FusionAttribute_AfterDelete]
	ON [dbo].[FusionAttribute]
	FOR DELETE
	AS
	BEGIN
		SET NOCOUNT ON;

		declare @type varchar(50) = 'FusionAttribute'

		DELETE	Field
		FROM	Field F
				inner join deleted D on F.ObjectType = @type and F.ObjectID = D.ID

		DELETE	O
		FROM	ObjectVersion O
				INNER JOIN deleted d
		ON		O.ObjectType = @type and O.ObjectID = d.ID

		DELETE	O
		FROM	cache.Relationships O
				inner join deleted d
		ON		(O.[SourceObject] = @type and O.SourceObjectID = d.ID) OR (O.[TargetObject] = @type and O.TargetObjectID = d.ID)

		DELETE	[Intersect]
		FROM	[Intersect] T
				inner join	(
							select	distinct 
									TN.IntersectID 
							from	IntersectNode TN 
									inner join deleted D	on TN.ObjectType = @type
															and TN.ObjectID = D.ID
							) S on S.IntersectID = T.ID

		DELETE	O
		FROM	cache.ObjectDetails O
				inner join deleted d
		ON		O.[Object] = @type and O.ObjectID = d.ID
	END
