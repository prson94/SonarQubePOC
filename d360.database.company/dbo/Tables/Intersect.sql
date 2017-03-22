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
    [Deleted]         BIT             CONSTRAINT [DF_Intersect_Deleted] DEFAULT ((0)) NULL,
    [CreatedBy]       INT             CONSTRAINT [DF_Intersect_CreatedBy] DEFAULT ((0)) NULL,
    [CreatedOn]       DATETIME        CONSTRAINT [DF_Intersect_CreatedOn] DEFAULT (getutcdate()) NULL,
    [UpdatedBy]       INT             CONSTRAINT [DF_Intersect_UpdatedBy] DEFAULT ((0)) NULL,
    [UpdatedOn]       DATETIME        CONSTRAINT [DF_Intersect_UpdatedOn] DEFAULT (getutcdate()) NULL,
	[Owner]			  VARCHAR (50)	  NULL,
	[Visible]		  BIT			  NOT NULL DEFAULT (1),
    CONSTRAINT [PK_Intersect] PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [FK_Intersect_IntersectType] FOREIGN KEY ([IntersectTypeID]) REFERENCES [dbo].[IntersectType] ([ID]) ON DELETE CASCADE,
    CONSTRAINT [UQ_Intersect] UNIQUE NONCLUSTERED ([IntersectTypeID] ASC, [Subject] ASC, [SubjectID] ASC, [Object] ASC, [ObjectID] ASC)
);


















GO
CREATE NONCLUSTERED INDEX [IX_Intersect_IntersectTypeID]
    ON [dbo].[Intersect]([IntersectTypeID] ASC);


GO

GO





GO



GO
CREATE NONCLUSTERED INDEX [IX_Intersect_Subject]
    ON [dbo].[Intersect]([Subject] ASC, [SubjectID] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_Intersect_Object]
    ON [dbo].[Intersect]([Object] ASC, [ObjectID] ASC);


GO

CREATE NONCLUSTERED INDEX [IX_Intersect_Visible] 
	ON [dbo].[Intersect] ( Visible ASC );
go


CREATE TRIGGER [dbo].[Intersect_AfterUpdate]
	ON [dbo].[Intersect]
	FOR UPDATE
AS
BEGIN
	SET NOCOUNT ON;

	merge cache.Object as T
	using (
			select	'Intersect' as Object,
					ID as ObjectID,
					'IntersectType' as ObjectType,
					IntersectTypeID as ObjectTypeID
			from	inserted
			) as S
	on    (T.Object = S.Object and T.ObjectID = S.ObjectID)
	when not matched then
		insert (Object, ObjectID, ObjectType, ObjectTypeID)
		values (S.Object, S.ObjectID, S.ObjectType, S.ObjectTypeID);

	INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
        select 'Update', [queue].WriteIndexXml('', Subject, SubjectID, UpdatedBy), 'Intersect', ID from inserted;

	INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
        select 'Update', [queue].WriteIndexXml('', Object, ObjectID, UpdatedBy), 'Intersect', ID from inserted;
END
GO
CREATE TRIGGER [dbo].[Intersect_AfterInsert]
	ON [dbo].[Intersect]
	FOR INSERT
AS
BEGIN
	SET NOCOUNT ON;

	merge cache.Object as T
	using (
			select	'Intersect' as Object,
					ID as ObjectID,
					'IntersectType' as ObjectType,
					IntersectTypeID as ObjectTypeID
			from	inserted
			) as S
	on    (T.Object = S.Object and T.ObjectID = S.ObjectID)
	when not matched then
		insert (Object, ObjectID, ObjectType, ObjectTypeID)
		values (S.Object, S.ObjectID, S.ObjectType, S.ObjectTypeID);

	INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
        select 'Add', [queue].WriteIndexXml('', Subject, SubjectID, UpdatedBy), 'Intersect', ID from inserted;

	INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
        select 'Add', [queue].WriteIndexXml('', Object, ObjectID, UpdatedBy), 'Intersect', ID from inserted;
END
GO


