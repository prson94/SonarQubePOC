CREATE TABLE [dbo].[Setting] (
    [ID]           INT             NOT NULL,
    [FieldName]    VARCHAR (50)    NOT NULL,
    [Name]         NVARCHAR (250)  NOT NULL,
    [Description]  NVARCHAR (1000) NULL,
    [DefaultValue] VARCHAR (250)   NOT NULL,
    CONSTRAINT [PK_Setting] PRIMARY KEY CLUSTERED ([ID] ASC)
);


GO
CREATE TRIGGER [dbo].[Setting_AfterUpsert]
   ON  dbo.Setting 
   AFTER INSERT, UPDATE
AS 
	SET NOCOUNT ON;

	insert into CompanySetting
		select	C.ID,
				I.ID,
				I.DefaultValue
		from	inserted I
				full join Company C on 1=1
				left join CompanySetting CS on CS.CompanyID = C.ID and CS.SettingID = I.ID
		where	CS.Value is null

GO
DISABLE TRIGGER [dbo].[Setting_AfterUpsert]
    ON [dbo].[Setting];

