CREATE FUNCTION [dbo].[ArtifactNgSiteNavigation](@id int)
RETURNS XML
WITH RETURNS NULL ON NULL INPUT
BEGIN 
	RETURN 
	(
	SELECT	name,
			url,
			'Menu_AT' + cast(id as varchar(15)) as menuID,
			0 as feature,
			dbo.ArtifactNgSiteNavigation(id) as items
	FROM	(
			--SELECT	A.name,
			--		A.url,
			--		NULL AS items
			--FROM	(
					SELECT		TOP 1000
								a.id,
								a.name,
								dbo.GenerateNgObjecturl('ArtifactType', a.ID, 0) As url
					FROM		ArtifactType a
					LEFT JOIN SiteNav v on v.ObjectID = a.ID and v.Object = 'ArtifactType'
					WHERE		a.ParentID = @id AND v.ObjectID IS NULL
					ORDER BY	a.name
			--		) A
			) BG
			FOR XML PATH('nav'), TYPE
	)
END