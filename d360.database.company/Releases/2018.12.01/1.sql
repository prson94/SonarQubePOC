CREATE NONCLUSTERED INDEX [IX_FieldType_AssetTypeID-Name] ON [dbo].[FieldType] ( [AssetTypeID] ASC, Name ASC )
GO;


------------------------------------------------------------------
-- GOV-5886
-- issue deleting a user then adding them back
------------------------------------------------------------------

-- fix busted trigger

ALTER TRIGGER [reporting].[ReportingGlobalResource_AfterDelete]
	ON [reporting].[Global_Resource]
	FOR DELETE
AS
	SET NOCOUNT ON;
	INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
		select 'Delete', [queue].WriteIndexXml('Removed', 'Resource', ResourceID, 0), 'Resource', ResourceID from deleted;


	delete Asset
	where Object = 'Resource' and ObjectID in (select ResourceID from deleted);

go

-- delete partially deleted users
delete from asset where [object] = 'Resource' and objectid not in (select resourceid from reporting.global_resource)

go


------------------------------------------------------------------
	