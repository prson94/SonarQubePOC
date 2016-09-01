CREATE FUNCTION [dbo].[CustomSiteNavigation]
(
	@id int
)
RETURNS XML
WITH RETURNS NULL ON NULL INPUT
AS
BEGIN
	 RETURN 
    (
        SELECT  name
                , [Route] AS url
				, 0 as feature
                , [dbo].CustomSiteNavigation(id)
        FROM    dbo.SiteNav
        WHERE   ParentID = @id
        FOR XML PATH('nav'),TYPE
    )
END