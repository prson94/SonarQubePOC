CREATE TABLE [reporting].[Global_Resource] (
    [ResourceID]       INT            NOT NULL,
    [FirstName]        NVARCHAR (250) NOT NULL,
    [LastName]         NVARCHAR (250) NOT NULL,
    [DateLastLoggedIn] DATETIME       NULL,
    [Email]            NVARCHAR (500) NOT NULL,
    [Status]           NVARCHAR (25)  NOT NULL,
    [IsAdministrator]  BIT            NOT NULL,
    CONSTRAINT [PK_ReportingGlobalResource] PRIMARY KEY CLUSTERED ([ResourceID] ASC)
);


GO
CREATE TRIGGER [reporting].[ReportingGlobalResource_AfterUpsert]
	ON [reporting].[Global_Resource]
	FOR INSERT, UPDATE
	AS
	BEGIN
		SET NOCOUNT ON;
		begin try
			declare @tblCache table (RowID int identity, ID int)
			insert into @tblCache 
				select ResourceID from inserted

			declare @current int = 1,
					@max int,
					@thisID int
			select @max = max(RowID) from @tblCache

			while @current <= @max
			begin
				select @thisID = ID from @tblCache where RowID = @current
				exec [cache].[SynchronizeObjectDetails] 'Resource', @thisID
				set @current = @current + 1
			end
		end try
		begin catch
		end catch
	END

GO

CREATE TRIGGER [reporting].[ReportingGlobalResource_AfterDelete]
	ON [reporting].[Global_Resource]
	FOR DELETE
	AS
	BEGIN
		SET NOCOUNT ON;
		
		declare @type varchar(50) = 'Resource'

		DELETE	O
		FROM	cache.ObjectDetails O
				inner join deleted d
		ON		O.[Object] = @type and O.ObjectID = d.ResourceID

		DELETE	O
		FROM	cache.Relationships O
				inner join deleted d
		ON		(O.[SourceObject] = @type and O.SourceObjectID = d.ResourceID) OR (O.[TargetObject] = @type and O.TargetObjectID = d.ResourceID)

		BEGIN TRY
			DECLARE @tblIntersectIDs table (ID int)

			INSERT INTO @tblIntersectIDs
				SELECT	N.IntersectID
				FROM	IntersectNode N
						INNER JOIN deleted AS d ON N.ObjectType = @type and N.ObjectID = d.ResourceID

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
