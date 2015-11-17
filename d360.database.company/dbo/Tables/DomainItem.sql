CREATE TABLE [dbo].[DomainItem] (
    [ID]          INT             IDENTITY (1, 1) NOT NULL,
    [Parents]     XML             NULL,
    [DomainID]    INT             NOT NULL,
    [Code]        NVARCHAR (50)   NOT NULL,
    [Name]        NVARCHAR (250)  NOT NULL,
    [Description] NVARCHAR (4000) NULL,
    [UpdatedOn]   DATETIME        NULL,
    [UpdatedBy]   INT             NULL,
    CONSTRAINT [PK_DomainItem] PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [FK_DomainItem_Domain] FOREIGN KEY ([DomainID]) REFERENCES [dbo].[Domain] ([ID]) ON DELETE CASCADE
);


GO
CREATE NONCLUSTERED INDEX [IX_DomainItem_DomainID]
    ON [dbo].[DomainItem]([DomainID] ASC);


GO
CREATE TRIGGER [dbo].[DomainItem_AfterDelete]
   ON  [dbo].[DomainItem] 
   AFTER DELETE
AS 
	SET NOCOUNT ON;
	insert into [queue].ObjectVersion ([Object], ObjectID, ResourceID, [Date], [Action], ActionObject, ActionObjectID)
		select 'Domain', DomainID, coalesce(UpdatedBy, 0), coalesce(UpdatedOn, getutcdate()), 'Removed', 'DomainItem', ID from deleted

	BEGIN TRY
		DECLARE @tblIntersectIDs table (ID int)

		INSERT INTO @tblIntersectIDs
			SELECT	N.IntersectID
			FROM	IntersectNode N
					INNER JOIN deleted AS d ON N.ObjectType = 'DomainItem' and N.ObjectID = d.ID

		DELETE	N
		FROM	IntersectNode N
				INNER JOIN @tblIntersectIDs I ON N.IntersectID = I.ID

		DELETE	II
		FROM	[Intersect] II
				INNER JOIN @tblIntersectIDs I ON II.ID = I.ID
	END TRY
	BEGIN CATCH

	END CATCH

GO
CREATE TRIGGER [dbo].[DomainItem_AfterInsert]
   ON  [dbo].[DomainItem] 
   AFTER INSERT
AS 
	SET NOCOUNT ON;
	insert into [queue].ObjectVersion ([Object], ObjectID, ResourceID, [Date], [Action], ActionObject, ActionObjectID)
		select 'Domain', DomainID, coalesce(UpdatedBy, 0), coalesce(UpdatedOn, getutcdate()), 'Created', 'DomainItem', ID from inserted

GO
CREATE TRIGGER [dbo].[DomainItem_AfterUpdate]
   ON  [dbo].[DomainItem] 
   AFTER UPDATE
AS 
	SET NOCOUNT ON;
	insert into [queue].ObjectVersion ([Object], ObjectID, ResourceID, [Date], [Action], ActionObject, ActionObjectID)
		select 'Domain', DomainID, coalesce(UpdatedBy, 0), coalesce(UpdatedOn, getutcdate()), 'Updated', 'DomainItem', ID from inserted
