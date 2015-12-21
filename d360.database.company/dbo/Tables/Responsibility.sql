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
	SET NOCOUNT ON;
	INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
		select 'Delete', [queue].WriteIndexXml('Removed', ObjectType, ObjectID, coalesce(UpdatedBy, 0)), 'Responsibility', ID from deleted

	delete	T
	from	cache.ResponsibilityItem T
			inner join deleted R on R.ID = T.ResponsibilityID

GO
CREATE TRIGGER [dbo].[Responsibility_AfterInsert]
   ON  [dbo].[Responsibility] 
   AFTER INSERT
AS 
	SET NOCOUNT ON;
	INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
		select 'Add', [queue].WriteIndexXml('', ObjectType, ObjectID, coalesce(UpdatedBy, 0)), 'Responsibility', ID from inserted

	INSERT INTO [cache].[ResponsibilityItem]
				(
				[ResponsibilityID],[ResponsibilityTypeID], [ResponsibilityType], [AssigningItem], [AssigningItemID], [Object], [ObjectID],
				[ResponsibleObject], [ResponsibleObjectID],
				[ContextHash], [ResponsibilityTypeGroup], [Visible], [TargetResponsibilityID]
				)
		select	R.ID, 
				R.ResponsibilityTypeID, 
				T.Name, 
				R.[ObjectType], 
				R.[ObjectID], 
				R.[ObjectType], 
				R.[ObjectID], 
				R.[ResponsibleObjectType],
				R.[ResponsibleObjectID], 
				utility.GetResponsibilityContextHashWrapper(R.ID), 
				T.[ResponsibilityTypeGroup], 
				R.[Visible], 
				R.[TargetResponsibilityID]
		from	inserted R
				inner join ResponsibilityType T on T.ID = R.ResponsibilityTypeID

GO
CREATE TRIGGER [dbo].[Responsibility_AfterUpdate]
   ON  [dbo].[Responsibility] 
   AFTER UPDATE
AS 
	SET NOCOUNT ON;
		INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
			select 'Update', [queue].WriteIndexXml('', ObjectType, ObjectID, coalesce(UpdatedBy, 0)), 'Responsibility', ID from inserted

		merge	[cache].[ResponsibilityItem] as T
		using	(
				select	R.ID as ResponsibilityID, 
						R.ResponsibilityTypeID, 
						T.Name as [ResponsibilityType], 
						R.[ObjectType] as [AssigningItem], R.[ObjectID] as [AssigningItemID], 
						R.[ObjectType] as [Object], R.[ObjectID], 
						R.[ResponsibleObjectType] as [ResponsibleObject], R.[ResponsibleObjectID], 
						utility.GetResponsibilityContextHashWrapper(R.ID) as [ContextHash], 
						T.[ResponsibilityTypeGroup], 
						R.[Visible], 
						R.[TargetResponsibilityID]
				from	inserted R
						inner join ResponsibilityType T on T.ID = R.ResponsibilityTypeID
				) as S
		on		T.ResponsibilityID = S.ResponsibilityID and T.[AssigningItem] = S.[AssigningItem] and T.[AssigningItemID] = S.[AssigningItemID] and T.[Object] = S.[Object] and T.[ObjectID] = S.[ObjectID]
		when	matched then
				update set	T.[ResponsibleObject] = S.[ResponsibleObject],
							T.[ResponsibleObjectID] = S.[ResponsibleObjectID],
							T.[ContextHash] = S.[ContextHash],
							T.[Visible] = S.[Visible],
							T.[TargetResponsibilityID] = S.[TargetResponsibilityID]
		when	not matched then
				insert	(
						[ResponsibilityID],[ResponsibilityTypeID], [ResponsibilityType], [AssigningItem], [AssigningItemID], [Object], [ObjectID],
						[ResponsibleObject], [ResponsibleObjectID],
						[ContextHash], [ResponsibilityTypeGroup], [Visible], [TargetResponsibilityID]
						)
				values	(
						S.[ResponsibilityID], S.[ResponsibilityTypeID], S.[ResponsibilityType], S.[AssigningItem], S.[AssigningItemID], S.[Object], S.[ObjectID],
						S.[ResponsibleObject], S.[ResponsibleObjectID],
						S.[ContextHash], S.[ResponsibilityTypeGroup], S.[Visible], S.[TargetResponsibilityID]				
						);
