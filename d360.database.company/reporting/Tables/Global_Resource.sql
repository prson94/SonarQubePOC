CREATE TABLE [reporting].[Global_Resource] (
    [ResourceID]       INT            NOT NULL,
    [FirstName]        NVARCHAR (250) NOT NULL,
    [LastName]         NVARCHAR (250) NOT NULL,
    [DateLastLoggedIn] DATETIME       NULL,
    [Email]            NVARCHAR (500) NOT NULL,
    [Status]           NVARCHAR (25)  NOT NULL,
    [IsAdministrator]  BIT            NOT NULL,
    CONSTRAINT [PK_ReportingGlobalResource] PRIMARY KEY CLUSTERED ([ResourceID] ASC)
);




GO


GO

CREATE TRIGGER [reporting].[ReportingGlobalResource_AfterDelete]
	ON [reporting].[Global_Resource]
	FOR DELETE
AS
	SET NOCOUNT ON;
	INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
		select 'Delete', [queue].WriteIndexXml('Removed', 'Resource', ResourceID, 0), 'Resource', ResourceID from deleted

GO
create TRIGGER [reporting].[ReportingGlobalResource_AfterInsert]
	ON [reporting].[Global_Resource]
	FOR INSERT
AS
BEGIN
	SET NOCOUNT ON;
	INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
		select 'Add', [queue].WriteIndexXml('', 'Resource', ResourceID, 0), 'Resource', ResourceID from inserted
END