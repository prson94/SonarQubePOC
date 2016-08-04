CREATE TABLE [dbo].[Domain] (
    [ID]                         INT            IDENTITY (1, 1) NOT NULL,
    [ParentID]                   INT            NULL,
    [DomainTypeID]               INT            NOT NULL,
    [EnforceParentItemSelection] BIT            CONSTRAINT [DF_Domain_EnforceParentItemSelection] DEFAULT ((0)) NOT NULL,
    [Name]                       NVARCHAR (250) NOT NULL,
    [Description]                NVARCHAR (MAX) NULL,
    [DomainGroupID]              INT            NULL,
    [Path]                       XML            NULL,
    [UpdatedOn]                  DATETIME       NULL,
    [UpdatedBy]                  INT            NULL,
    [SourceArtifactID]           INT            NULL,
    [DomainClassificationID]     INT            CONSTRAINT [DF_Domain_DomainClassification] DEFAULT ((1)) NOT NULL,
    CONSTRAINT [PK_Domain] PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [FK_Domain_DomainType] FOREIGN KEY ([DomainTypeID]) REFERENCES [dbo].[DomainType] ([ID]) ON DELETE CASCADE,
    CONSTRAINT [FK_Domain_ParentDomain] FOREIGN KEY ([ParentID]) REFERENCES [dbo].[Domain] ([ID])
);






GO
CREATE NONCLUSTERED INDEX [IX_Domain_DomainGroupID]
    ON [dbo].[Domain]([DomainGroupID] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_Domain_DomainTypeID]
    ON [dbo].[Domain]([DomainTypeID] ASC);


GO

CREATE TRIGGER [dbo].[Domain_AfterDelete]
   ON  [dbo].[Domain] 
   AFTER DELETE
AS 
	SET NOCOUNT ON;
	INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
		select 'Delete', [queue].WriteIndexXml('Removed', 'Domain', ID, coalesce(UpdatedBy, 0)), 'Domain', ID from deleted

GO

CREATE TRIGGER [dbo].[Domain_AfterInsert]
   ON  [dbo].[Domain] 
   AFTER INSERT
AS
	SET NOCOUNT ON;
	INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
        select 'Add', [queue].WriteIndexXml('', 'Domain', ID, coalesce(UpdatedBy, 0)), 'Domain', ID from inserted
	update	T
	set		T.[Path] = utility.GetBreadcrumbWrapper('Domain', S.ID)
	from	Domain T
			inner join inserted S on S.ID = T.ID

GO

CREATE TRIGGER [dbo].[Domain_AfterUpdate]
   ON  [dbo].[Domain] 
   AFTER UPDATE
AS 
	SET NOCOUNT ON;
	INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
        select 'Update', [queue].WriteIndexXml('', 'Domain', ID, coalesce(UpdatedBy, 0)), 'Domain', ID from inserted

	update	T
	set		T.[Path] = utility.GetBreadcrumbWrapper('Domain', S.ID)
	from	Domain T
			inner join inserted S on S.ID = T.ID
