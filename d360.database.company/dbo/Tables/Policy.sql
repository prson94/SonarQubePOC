CREATE TABLE [dbo].[Policy] (
    [ID]           INT             IDENTITY (1, 1) NOT NULL,
    [ParentID]     INT             NULL,
    [TextPath]     NVARCHAR (2000) NULL,
    [UpdatedOn]    DATETIME        CONSTRAINT [DF_Policy_UpdatedOn] DEFAULT (getutcdate()) NULL,
    [UpdatedBy]    INT             NULL,
    [PolicyTypeID] INT             CONSTRAINT [DF_Policy_PolicyTypeID] DEFAULT ((50000)) NOT NULL,
    [Level]        INT             CONSTRAINT [DF_Policy_Level] DEFAULT ((1)) NOT NULL,
    [Visible]      BIT             CONSTRAINT [DF_Policy_Visible] DEFAULT ((1)) NOT NULL,
    [SourceID]     NVARCHAR (250)  NULL,
    [KeyHash]      VARCHAR (250)   NULL,
    [FieldHash]    VARCHAR (250)   NULL,
    [DisplayValue] AS              ([utility].[GetObjectDisplayValueWrapper]('Policy',[ID],[PolicyTypeID])),
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
	SET NOCOUNT ON
	update	Asset
	set		[State] = 3
	where	Object = 'Policy' and ObjectID in (select ID from deleted)

	delete Asset where Object = 'Policy' and ObjectID in (select ID from deleted)

GO
CREATE TRIGGER [dbo].[Policy_AfterInsert]
   ON  [dbo].[Policy] 
   AFTER INSERT
AS 
	SET NOCOUNT ON
	INSERT INTO [dbo].[Asset] ([AssetTypeID],[State],[Object],[ObjectID],[CreatedOn],[CreatedBy],[UpdatedOn],[UpdatedBy])
		SELECT	T.ID, 1, 'Policy', O.ID, O.[UpdatedOn], O.[UpdatedBy], O.[UpdatedOn], O.[UpdatedBy]
		FROM	inserted O inner join  AssetType T on T.Object = 'PolicyType' and T.ObjectID = O.PolicyTypeID

GO
CREATE TRIGGER [dbo].[Policy_AfterUpdate]
   ON  [dbo].[Policy] 
   AFTER UPDATE
AS 
	SET NOCOUNT ON
	update	T
	set		T.UpdatedBy = S.UpdatedBy,
			T.UpdatedOn = S.UpdatedOn
	from	Asset T
			inner join inserted S on T.Object = 'Policy' and T.ObjectID = S.ID
