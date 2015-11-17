CREATE TABLE [dbo].[Intersect] (
    [ID]                  INT             IDENTITY (1, 1) NOT NULL,
    [IntersectTypeID]     INT             NOT NULL,
    [Name]                AS              ([utility].[DeriveIntersectNameWrapper]([ID])),
    [Classification]      INT             NULL,
    [Description]         NVARCHAR (4000) NULL,
    [IntersectTypeRoleID] INT             NULL,
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
		insert into [queue].[ObjectCache] ([Object], ObjectID) 
			select 'Intersect', ID from inserted
	END


GO

CREATE TRIGGER [dbo].[Intersect_AfterDelete]
   ON  [dbo].[Intersect] 
   AFTER DELETE
AS 
	set nocount on;

	declare @type varchar(50) = 'Intersect'

	DELETE	O
	FROM	cache.Relationships O
			inner join deleted d
	ON		O.IntersectID = d.ID

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

	DELETE	O
	FROM	cache.ObjectDetails O
			inner join deleted d
	ON		O.[Object] = @type and O.ObjectID = d.ID

