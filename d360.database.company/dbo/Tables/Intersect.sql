CREATE TABLE [dbo].[Intersect] (
    [ID]              INT             IDENTITY (1, 1) NOT NULL,
    [IntersectTypeID] INT             NOT NULL,
    [Name]            AS              ([utility].[DeriveIntersectNameWrapper]([ID])),
    [Classification]  INT             NULL,
    [Description]     NVARCHAR (4000) NULL,
    [Subject]         VARCHAR (50)    NULL,
    [SubjectID]       INT             NULL,
    [Object]          VARCHAR (50)    NULL,
    [ObjectID]        INT             NULL,
    [Deleted]         BIT             NULL,
    [CreatedBy]       INT             NULL,
    [CreatedOn]       DATETIME        NULL,
    [UpdatedBy]       INT             NULL,
    [UpdatedOn]       DATETIME        NULL,
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
	INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
        select 'Add', [queue].WriteIndexXml('', 'Intersect', ID, 0), 'Intersect', ID from inserted
END


GO

CREATE TRIGGER [dbo].[Intersect_AfterDelete]
   ON  [dbo].[Intersect] 
   AFTER DELETE
AS 
	set nocount on;
	INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
		select 'Delete', [queue].WriteIndexXml('Removed', 'Intersect', ID, 0), 'Intersect', ID from deleted


GO
CREATE NONCLUSTERED INDEX [IX_Intersect_Subject]
    ON [dbo].[Intersect]([Subject] ASC, [SubjectID] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_Intersect_Object]
    ON [dbo].[Intersect]([Object] ASC, [ObjectID] ASC);

