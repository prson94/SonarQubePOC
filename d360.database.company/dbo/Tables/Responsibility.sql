CREATE TABLE [dbo].[Responsibility] (
    [ID]                     INT          IDENTITY (1, 1) NOT NULL,
    [ResponsibilityTypeID]   INT          NOT NULL,
    [ObjectType]             VARCHAR (50) NULL,
    [ObjectID]               INT          NULL,
    [ResponsibleObjectType]  VARCHAR (50) NULL,
    [ResponsibleObjectID]    INT          NULL,
    [UpdatedOn]              DATETIME     NULL,
    [UpdatedBy]              INT          NULL,
    [Visible]                BIT          CONSTRAINT [DF_Responsibility_Visible] DEFAULT ((1)) NOT NULL,
    [TargetResponsibilityID] INT          NULL,
    CONSTRAINT [PK_Responsibility] PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [FK_Responsibility_ResponsibilityType] FOREIGN KEY ([ResponsibilityTypeID]) REFERENCES [dbo].[ResponsibilityType] ([ID]) ON DELETE CASCADE
);












GO
CREATE NONCLUSTERED INDEX [IX_Responsibility_ObjectType-ObjectID]
    ON [dbo].[Responsibility]([ObjectType] ASC, [ObjectID] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_Responsibility_ResponsibleObjectType-ResponsibleObjectID]
    ON [dbo].[Responsibility]([ResponsibleObjectType] ASC, [ResponsibleObjectID] ASC);


GO
CREATE TRIGGER [dbo].[Responsibility_AfterDelete]
   ON  [dbo].[Responsibility] 
   AFTER DELETE
AS 
begin
	SET NOCOUNT ON;
	INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
		select 'Delete', [queue].WriteIndexXml('Removed', ObjectType, ObjectID, coalesce(UpdatedBy, 0)), 'Responsibility', ID from deleted

	delete	T
	from	cache.ResponsibilityItem T
			inner join deleted S on S.ID = T.ResponsibilityID

	MERGE
	INTO    [cache].[ResponsibilityItem] T
	USING   (
			select	distinct
					J.ResponsibilityID, J.ResponsibilityTypeID, 
					J.AssigningItem, J.AssigningItemID, 
					J.[Object], J.ObjectID, 
					R.ResponsibleObjectType, R.ResponsibleObjectID,  
					J.ContextHash,
					J.Visible
			from	deleted I
					cross apply cache.SynchronizeObjectResponsibilities(i.ObjectType, i.ObjectID) J
					inner join [Responsibility] R on R.ID = J.ResponsibilityID
			) S
	ON      (T.ResponsibilityID = S.ResponsibilityID and T.AssigningItem = S.AssigningItem and T.AssigningItemID = S.AssigningItemID and T.Object = S.Object and T.ObjectID = S.ObjectID and T.ContextHash = S.ContextHash)
	WHEN MATCHED THEN
		UPDATE
		SET	T.[ResponsibleObject] = S.ResponsibleObjectType,
			T.[ResponsibleObjectID] = S.[ResponsibleObjectID]
	WHEN NOT MATCHED THEN
		INSERT  (
				[ResponsibilityID], [ResponsibilityTypeID], 
				[AssigningItem], [AssigningItemID], 
				[Object], [ObjectID], 
				[ResponsibleObject], [ResponsibleObjectID], 
				[ContextHash], [ResponsibilityTypeGroup], [Visible]
				)
		VALUES  (
				S.ResponsibilityID, S.ResponsibilityTypeID, 
				S.AssigningItem, S.AssigningItemID, 
				S.[Object], S.ObjectID, 
				S.ResponsibleObjectType, S.ResponsibleObjectID,  
				S.ContextHash,
				1, S.Visible
				);
end

GO
CREATE TRIGGER [dbo].[Responsibility_AfterInsert]
   ON  [dbo].[Responsibility] 
   AFTER INSERT
AS 
BEGIN
	SET NOCOUNT ON;

	INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
		select 'Add', [queue].WriteIndexXml('', ObjectType, ObjectID, coalesce(UpdatedBy, 0)), 'Responsibility', ID from inserted

	MERGE
	INTO    [cache].[ResponsibilityItem] T
	USING   (
			select	distinct
					J.ResponsibilityID, J.ResponsibilityTypeID, 
					J.AssigningItem, J.AssigningItemID, 
					J.[Object], J.ObjectID, 
					R.ResponsibleObjectType, R.ResponsibleObjectID,  
					J.ContextHash,
					J.Visible
			from	inserted I
					cross apply cache.SynchronizeObjectResponsibilities(i.ObjectType, i.ObjectID) J
					inner join [Responsibility] R on R.ID = J.ResponsibilityID
			) S
	ON      (T.ResponsibilityID = S.ResponsibilityID and T.AssigningItem = S.AssigningItem and T.AssigningItemID = S.AssigningItemID and T.Object = S.Object and T.ObjectID = S.ObjectID and T.ContextHash = S.ContextHash)
	WHEN MATCHED THEN
		UPDATE
		SET	T.[ResponsibleObject] = S.ResponsibleObjectType,
			T.[ResponsibleObjectID] = S.[ResponsibleObjectID]
	WHEN NOT MATCHED THEN
		INSERT  (
				[ResponsibilityID], [ResponsibilityTypeID], 
				[AssigningItem], [AssigningItemID], 
				[Object], [ObjectID], 
				[ResponsibleObject], [ResponsibleObjectID], 
				[ContextHash], [ResponsibilityTypeGroup], [Visible]
				)
		VALUES  (
				S.ResponsibilityID, S.ResponsibilityTypeID, 
				S.AssigningItem, S.AssigningItemID, 
				S.[Object], S.ObjectID, 
				S.ResponsibleObjectType, S.ResponsibleObjectID,  
				S.ContextHash,
				1, S.Visible
				);
END

GO
CREATE TRIGGER [dbo].[Responsibility_AfterUpdate]
   ON  [dbo].[Responsibility] 
   AFTER UPDATE
AS 
begin
	SET NOCOUNT ON;

	INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
		select 'Update', [queue].WriteIndexXml('', ObjectType, ObjectID, coalesce(UpdatedBy, 0)), 'Responsibility', ID from inserted

	delete	T
	from	cache.[ResponsibilityItem] T
			inner join inserted S on S.ID = T.ResponsibilityID 

	MERGE
	INTO    [cache].[ResponsibilityItem] T
	USING   (
			select	distinct
					J.ResponsibilityID, J.ResponsibilityTypeID, 
					J.AssigningItem, J.AssigningItemID, 
					J.[Object], J.ObjectID, 
					R.ResponsibleObjectType, R.ResponsibleObjectID,  
					J.ContextHash,
					J.Visible
			from	inserted I
					cross apply cache.SynchronizeObjectResponsibilities(i.ObjectType, i.ObjectID) J
					inner join [Responsibility] R on R.ID = J.ResponsibilityID
			) S
	ON      (T.ResponsibilityID = S.ResponsibilityID and T.AssigningItem = S.AssigningItem and T.AssigningItemID = S.AssigningItemID and T.Object = S.Object and T.ObjectID = S.ObjectID and T.ContextHash = S.ContextHash)
	WHEN MATCHED THEN
		UPDATE
		SET	T.[ResponsibleObject] = S.ResponsibleObjectType,
			T.[ResponsibleObjectID] = S.[ResponsibleObjectID]
	WHEN NOT MATCHED THEN
		INSERT  (
				[ResponsibilityID], [ResponsibilityTypeID], 
				[AssigningItem], [AssigningItemID], 
				[Object], [ObjectID], 
				[ResponsibleObject], [ResponsibleObjectID], 
				[ContextHash], [ResponsibilityTypeGroup], [Visible]
				)
		VALUES  (
				S.ResponsibilityID, S.ResponsibilityTypeID, 
				S.AssigningItem, S.AssigningItemID, 
				S.[Object], S.ObjectID, 
				S.ResponsibleObjectType, S.ResponsibleObjectID,  
				S.ContextHash,
				1, S.Visible
				);
end
