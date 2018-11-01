CREATE NONCLUSTERED INDEX [IX_FieldType_AssetTypeID-Name] ON [dbo].[FieldType] ( [AssetTypeID] ASC, Name ASC )
GO;


------------------------------------------------------------------
-- GOV-5886
-- issue deleting a user then adding them back
------------------------------------------------------------------

-- fix busted trigger

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
delete from field where [objecttype] = 'Resource' and objectid not in (select resourceid from reporting.global_resource)
go

delete from asset where [object] = 'Resource' and objectid not in (select resourceid from reporting.global_resource)
go

------------------------------------------------------------------


------------------------------------------------------------------
-- GOV-5891
-- Workflow Assignment duplication issue when workflow has multiple forms assigned to multiple users
------------------------------------------------------------------

-- clear out any duplicated workflow assignments
;WITH cte AS (SELECT *,ROW_NUMBER() OVER(PARTITION BY itemid, resourceobject,resourceobjectid ORDER BY id DESC) AS RN 
              FROM workflow.itemassignment where stepid is null
              )
delete cte
WHERE RN > 1
	
GO

------------------------------------------------------------------
