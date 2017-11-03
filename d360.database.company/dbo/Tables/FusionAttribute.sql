CREATE TABLE [dbo].[FusionAttribute] (
    [ParentID]              INT             NULL,
    [Name]                  NVARCHAR (250)  NOT NULL,
    [FusionID]              INT             NOT NULL,
    [FusionAttributeTypeID] INT             NOT NULL,
    [SourceID]              VARCHAR (250)   NULL,
    [Deleted]               BIT             CONSTRAINT [DF_FusionAttribute_Deleted] DEFAULT ((0)) NOT NULL,
    [TextPath]              NVARCHAR (2500) NULL,
    [ID]                    INT             CONSTRAINT [Const_FusionAttributeSeq] DEFAULT (NEXT VALUE FOR [dbo].[FusionAttribute_Seq]) NOT NULL,
    CONSTRAINT [PK_FusionAttribute] PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [FK_FusionAttribute_Fusion] FOREIGN KEY ([FusionID]) REFERENCES [dbo].[Fusion] ([ID]) ON DELETE CASCADE,
    CONSTRAINT [FK_FusionAttribute_FusionAttributeType] FOREIGN KEY ([FusionAttributeTypeID]) REFERENCES [dbo].[FusionAttributeType] ([ID]) ON DELETE CASCADE
);












GO
CREATE NONCLUSTERED INDEX [IX_FusionAttribute_FusionAttributeTypeID]
    ON [dbo].[FusionAttribute]([FusionAttributeTypeID] ASC)
    INCLUDE([ID], [Name]);

GO


CREATE NONCLUSTERED INDEX [IX_FusionAttribute_FusionID]
    ON [dbo].[FusionAttribute]([FusionID] ASC);
GO

CREATE NONCLUSTERED INDEX [IX_FusionAttribute_FusionID-FusionAttributeTypeID-SourceID]
    ON [dbo].[FusionAttribute]([FusionID] ASC, [FusionAttributeTypeID] ASC, [SourceID] ASC);

GO

CREATE NONCLUSTERED INDEX [IX_FusionAttribute_FusionID-SourceID]
    ON [dbo].[FusionAttribute]([FusionID] ASC, [SourceID] ASC);


GO



GO
CREATE NONCLUSTERED INDEX [IX_FusionAttribute_FusionID_TextPath]
    ON [dbo].[FusionAttribute]([FusionID] ASC, [TextPath] ASC);
GO

CREATE INDEX IX_FusionAttribute_FusionID_Deleted_ParentID 
	ON FusionAttribute (FusionID, Deleted, ParentID)
GO
CREATE TRIGGER [dbo].[FusionAttribute_AfterUpdate]
   ON  [dbo].[FusionAttribute] 
   AFTER UPDATE
AS 
	SET NOCOUNT ON
	update	T
	set		T.UpdatedBy = 0,
			T.UpdatedOn = getutcdate()
	from	Asset T
			inner join inserted S on T.Object = 'FusionAttribute' and T.ObjectID = S.ID
GO
CREATE TRIGGER [dbo].[FusionAttribute_AfterInsert]
   ON  [dbo].[FusionAttribute] 
   AFTER INSERT
AS 
BEGIN
	SET NOCOUNT ON
	INSERT INTO [dbo].[Asset] ([AssetTypeID],[State],[Object],[ObjectID],[CreatedOn],[CreatedBy],[UpdatedOn],[UpdatedBy])
		SELECT	T.ID, 1, 'FusionAttribute', O.ID, getutcdate(), 0, getutcdate(), 0
		FROM	inserted O inner join  AssetType T on T.Object = 'FusionAttributeType' and T.ObjectID = O.FusionAttributeTypeID
END
GO
CREATE TRIGGER [dbo].[FusionAttribute_AfterDelete]
   ON  [dbo].[FusionAttribute] 
   AFTER DELETE
AS 
	SET NOCOUNT ON
	update	Asset
	set		[State] = 3
	where	Object = 'FusionAttribute' and ObjectID in (select ID from deleted)

	delete Asset where Object = 'FusionAttribute' and ObjectID in (select ID from deleted)