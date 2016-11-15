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
        SELECT  COALESCE(a.Name,pc.Name,tc.Name,v.Name) as name --v.name
                , v.[Route] AS url
				, 0 as feature,
				case when v.Object = 'ArtifactType' then
					dbo.ArtifactNgSiteNavigation(a.id)
				when v.Object = 'PolicyTypeClass' then
				        (SELECT	name, 
						        dbo.GenerateNgObjectUrl('PolicyType', ID, 0)  As url,
						        0 as feature
				        FROM	PolicyType
				        WHERE	PolicyTypeClassID = pc.ID
				        FOR XML PATH('nav'), TYPE)
				when v.Object = 'TaxonomyTypeClass' then
					(SELECT name, 
							dbo.GenerateNgObjectUrl('TaxonomyType', id, 0)  As url,
							0 as feature
					FROM	TaxonomyType
					WHERE	TaxonomyTypeClassID = tc.ID
					FOR XML PATH('nav'), TYPE)
				when v.Object = 'TaxonomyType' or v.Object = 'PolicyType' then
					null
				else
					[dbo].CustomSiteNavigation(v.id)
				end as items
        FROM    dbo.SiteNav v
		left join artifacttype a on a.id = v.objectID and v.Object = 'ArtifactType'
		left join policytypeclass pc on pc.id = v.objectID and v.Object = 'PolicyTypeClass'
		left join taxonomytypeclass tc on tc.id = v.objectid and v.object = 'TaxonomyTypeClass'
        WHERE   v.ParentID = @id
        FOR XML PATH('nav'),TYPE
    )
END