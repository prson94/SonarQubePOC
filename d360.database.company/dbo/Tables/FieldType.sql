CREATE TABLE [dbo].[FieldType] (
    [ID]                    INT             IDENTITY (50000, 1) NOT NULL,
    [Name]                  NVARCHAR (250)  NOT NULL,
    [FriendlyName]          NVARCHAR (250)  NOT NULL,
    [Description]           NVARCHAR (4000) NULL,
    [DisplayDescription]    NVARCHAR (4000) NULL,
    [FormDescription]       NVARCHAR (4000) NULL,
    [Type]                  VARCHAR (25)    NOT NULL,
    [LookupObjectType]      VARCHAR (25)    NULL,
    [LookupObjectID]        INT             NULL,
    [LookupDisplayFormat]   NVARCHAR (250)  NULL,
    [MinimumLength]         INT             NULL,
    [MaximumLength]         INT             NULL,
    [Length]                INT             NULL,
    [Pattern]               VARCHAR (1000)  NULL,
    [Object]                VARCHAR (50)    CONSTRAINT [CK_FieldType_Object] DEFAULT ('') NOT NULL,
    [ObjectID]              INT             CONSTRAINT [CK_FieldType_ObjectID] DEFAULT ((0)) NOT NULL,
    [SortOrder]             INT             CONSTRAINT [CK_FieldType_SortOrder] DEFAULT ((0)) NOT NULL,
    [IsRequired]            BIT             CONSTRAINT [CK_FieldType_IsRequired] DEFAULT ((0)) NOT NULL,
    [IsListable]            BIT             CONSTRAINT [CK_FieldType_IsListable] DEFAULT ((0)) NOT NULL,
    [ValidationDescription] NVARCHAR (500)  NULL,
    [Category]              NVARCHAR (250)  NULL,
    CONSTRAINT [PK_FieldType] PRIMARY KEY CLUSTERED ([ID] ASC)
);






GO
CREATE NONCLUSTERED INDEX [IX_FieldType_Object]
    ON [dbo].[FieldType]([Object] ASC, [ObjectID] ASC);


GO
CREATE TRIGGER [dbo].[FieldType_AfterUpsert]
   ON  dbo.FieldType 
   AFTER INSERT, UPDATE
AS 
	UPDATE	F
	set		F.FormattedValue = utility.GetFormattedFieldLookupValue(FT.Type, FT.LookupDisplayFormat, FT.LookupObjectType, FT.LookupObjectID, F.Value)
	FROM	Field F
			inner join inserted FT on FT.ID = F.FieldTypeID

GO

CREATE TRIGGER [dbo].[FieldType_AfterDelete]
   ON  [dbo].[FieldType] 
   AFTER DELETE
AS 
	INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
		select 'Delete', [queue].WriteIndexXml('Removed', 'FieldType', ID, 0), 'FieldType', ID from deleted
