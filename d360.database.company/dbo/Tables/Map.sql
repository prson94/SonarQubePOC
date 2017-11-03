CREATE TABLE [dbo].[Map] (
    [ID]                INT      IDENTITY (1, 1) NOT NULL,
    [CreatedBy]         INT      CONSTRAINT [DF_Map_CreatedBy] DEFAULT ((0)) NOT NULL,
    [CreatedOn]         DATETIME CONSTRAINT [DF_Map_CreatedOn] DEFAULT (getutcdate()) NOT NULL,
    [UpdatedBy]         INT      CONSTRAINT [DF_Map_UpdatedBy] DEFAULT ((0)) NOT NULL,
    [UpdatedOn]         DATETIME CONSTRAINT [DF_Map_UpdatedOn] DEFAULT (getutcdate()) NOT NULL,
    [MapTypeID]         INT      CONSTRAINT [DF_Map_MapTypeID] DEFAULT ((1)) NOT NULL,
    [State]             SMALLINT CONSTRAINT [DF_Map_State] DEFAULT ((1)) NOT NULL,
    [MapTypeTemplateID] INT      NULL,
    CONSTRAINT [PK_Map] PRIMARY KEY NONCLUSTERED ([ID] ASC),
    CONSTRAINT [FK_Map_MapType] FOREIGN KEY ([MapTypeID]) REFERENCES [dbo].[MapType] ([ID]),
    CONSTRAINT [FK_Map_MapTypeTemplateID] FOREIGN KEY ([MapTypeTemplateID]) REFERENCES [dbo].[MapTypeTemplate] ([ID])
);






GO
CREATE TRIGGER [dbo].[Map_AfterUpdate]
   ON  [dbo].[Map] 
   AFTER UPDATE
AS 
	SET NOCOUNT ON
	update	T
	set		T.UpdatedBy = S.UpdatedBy,
			T.UpdatedOn = S.UpdatedOn
	from	Asset T
			inner join inserted S on T.Object = 'Map' and T.ObjectID = S.ID
GO
CREATE TRIGGER [dbo].[Map_AfterInsert]
   ON  [dbo].[Map] 
   AFTER INSERT
AS 
	SET NOCOUNT ON
	INSERT INTO [dbo].[Asset] ([AssetTypeID],[State],[Object],[ObjectID],[CreatedOn],[CreatedBy],[UpdatedOn],[UpdatedBy])
		SELECT	T.ID, 1, 'Map', O.ID, O.[CreatedOn], O.[CreatedBy], O.[UpdatedOn], O.[UpdatedBy]
		FROM	inserted O inner join  AssetType T on T.Object = 'MapType' and T.ObjectID = O.MapTypeID
GO
CREATE TRIGGER [dbo].[Map_AfterDelete]
   ON  [dbo].[Map] 
   AFTER DELETE
AS 
	SET NOCOUNT ON
	update	Asset
	set		[State] = 3
	where	Object = 'Map' and ObjectID in (select ID from deleted)

	delete Asset where Object = 'Map' and ObjectID in (select ID from deleted)