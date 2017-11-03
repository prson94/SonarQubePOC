CREATE TABLE [dbo].[Rule] (
    [ID]              INT            IDENTITY (1, 1) NOT NULL,
    [RuleTypeID]      INT            NOT NULL,
    [RuleDimensionID] INT            NULL,
    [Status]          INT            CONSTRAINT [DF_Rule_Status] DEFAULT ((1)) NOT NULL,
    [Threshold]       DECIMAL (4, 3) CONSTRAINT [DF_Rule_Threshold] DEFAULT ((0)) NULL,
    [Visible]         BIT            CONSTRAINT [DF_Rule_Visible] DEFAULT ((1)) NOT NULL,
    [SourceID]        NVARCHAR (250) NULL,
    [KeyHash]         VARCHAR (250)  NULL,
    [FieldHash]       VARCHAR (250)  NULL,
    [CreatedBy]       INT            NULL,
    [CreatedOn]       DATETIME       NULL,
    [UpdatedBy]       INT            NULL,
    [UpdatedOn]       DATETIME       NULL,
    [DisplayValue]    AS             ([utility].[GetObjectDisplayValueWrapper]('Rule',[ID],[RuleTypeID])),
    CONSTRAINT [PK_Rule] PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [FK_Rule_RuleDimension] FOREIGN KEY ([RuleDimensionID]) REFERENCES [dbo].[RuleDimension] ([ID]),
    CONSTRAINT [FK_Rule_RuleType] FOREIGN KEY ([RuleTypeID]) REFERENCES [dbo].[RuleType] ([ID])
);




GO


CREATE NONCLUSTERED INDEX [IX_Rule_Visible] ON [dbo].[Rule] ( Visible ASC );
go













GO
CREATE TRIGGER [dbo].[Rule_AfterUpdate]
   ON  [dbo].[Rule] 
   AFTER UPDATE
AS 
	SET NOCOUNT ON
	update	T
	set		T.UpdatedBy = S.UpdatedBy,
			T.UpdatedOn = S.UpdatedOn
	from	Asset T
			inner join inserted S on T.Object = 'Rule' and T.ObjectID = S.ID

GO
CREATE TRIGGER [dbo].[Rule_AfterInsert]
   ON  [dbo].[Rule] 
   AFTER INSERT
AS 
	SET NOCOUNT ON
	INSERT INTO [dbo].[Asset] ([AssetTypeID],[State],[Object],[ObjectID],[CreatedOn],[CreatedBy],[UpdatedOn],[UpdatedBy])
		SELECT	T.ID, 1, 'Rule', O.ID, O.[CreatedOn], O.[CreatedBy], O.[UpdatedOn], O.[UpdatedBy]
		FROM	inserted O inner join  AssetType T on T.Object = 'RuleType' and T.ObjectID = O.RuleTypeID

GO
CREATE TRIGGER [dbo].[Rule_AfterDelete]
   ON  [dbo].[Rule] 
   AFTER DELETE
AS 
	SET NOCOUNT ON
	update	Asset
	set		[State] = 3
	where	Object = 'Rule' and ObjectID in (select ID from deleted)

	delete Asset where Object = 'Rule' and ObjectID in (select ID from deleted)
