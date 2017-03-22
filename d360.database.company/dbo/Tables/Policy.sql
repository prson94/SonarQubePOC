CREATE TABLE [dbo].[Policy] (
    [ID]           INT             IDENTITY (1, 1) NOT NULL,
    [ParentID]     INT             NULL,
    [Name]         NVARCHAR (250)  NOT NULL,
    [Description]  NVARCHAR (MAX)  NULL,
    [TextPath]     NVARCHAR (2000) NULL,
    [UpdatedOn]    DATETIME        CONSTRAINT [DF_Policy_UpdatedOn] DEFAULT (getutcdate()) NULL,
    [UpdatedBy]    INT             NULL,
    [PolicyTypeID] INT             CONSTRAINT [DF_Policy_PolicyTypeID] DEFAULT ((50000)) NOT NULL,
    [Level]        INT             CONSTRAINT [DF_Policy_Level] DEFAULT ((1)) NOT NULL,
    [Status]       INT             CONSTRAINT [DF_Policy_Status] DEFAULT ((1)) NOT NULL,
	[Visible]	   BIT				NOT NULL DEFAULT ((1)),
    CONSTRAINT [PK_Policy] PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [FK_Policy_ParentPolicy] FOREIGN KEY ([ParentID]) REFERENCES [dbo].[Policy] ([ID]),
    CONSTRAINT [FK_Policy_PolicyType] FOREIGN KEY ([PolicyTypeID]) REFERENCES [dbo].[PolicyType] ([ID])
);

GO


CREATE NONCLUSTERED INDEX [IX_Policy_Visible] 
	ON [dbo].[Policy] ( Visible ASC );
go


CREATE TRIGGER [dbo].[Policy_AfterDelete]
   ON  [dbo].[Policy] 
   AFTER DELETE
AS 
	SET NOCOUNT ON;
	INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
		select 'Delete', [queue].WriteIndexXml('Removed', 'PolicyType', PolicyTypeID, coalesce(UpdatedBy, 0)), 'Policy', ID from deleted

GO

CREATE TRIGGER [dbo].[Policy_AfterInsert]
   ON  [dbo].[Policy] 
   AFTER INSERT
AS 
	SET NOCOUNT ON;
	INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
		select 'Add', [queue].WriteIndexXml('', 'PolicyType', PolicyTypeID, coalesce(UpdatedBy, 0)), 'Policy', ID from inserted	

	declare @IDs table (ID int);

	with d AS
	(
		SELECT	ParentID, 
				ID
		FROM	inserted	
		UNION ALL
		SELECT	C.ParentID, 
				C.ID
		FROM	Policy	C
				INNER JOIN d AS P ON P.ID = C.ParentID
	)

	insert into @IDs
		select ID from d

	update	T
	set		T.TextPath = utility.GetBreadcrumbStringWrapper('Policy', S.ID, '/'),
			T.[Level] = utility.GetObjectLevelWrapper('Policy', S.ID)
	from	Policy T
			inner join @IDs S on S.ID = T.ID

GO

CREATE TRIGGER [dbo].[Policy_AfterUpdate]
   ON  [dbo].[Policy] 
   AFTER UPDATE
AS 
	SET NOCOUNT ON;
	INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
		select 'Update', [queue].WriteIndexXml('', 'PolicyType', PolicyTypeID, coalesce(UpdatedBy, 0)), 'Policy', ID from inserted

	declare @IDs table (ID int);

	with d AS
	(
		SELECT	ParentID, 
				ID
		FROM	inserted	
		UNION ALL
		SELECT	C.ParentID, 
				C.ID
		FROM	Policy	C
				INNER JOIN d AS P ON P.ID = C.ParentID
	)

	insert into @IDs
		select ID from d

	update	T
	set		T.TextPath = utility.GetBreadcrumbStringWrapper('Policy', S.ID, '/'),
			T.[Level] = utility.GetObjectLevelWrapper('Policy', S.ID)
	from	Policy T
			inner join @IDs S on S.ID = T.ID
