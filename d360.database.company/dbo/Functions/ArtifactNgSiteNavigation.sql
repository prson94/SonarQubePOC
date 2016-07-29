ALTER FUNCTION [dbo].[ArtifactNgSiteNavigation](@id int)
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
								id,
								name,
								dbo.GenerateNgObjecturl('ArtifactType', ID, 0) As url
					FROM		ArtifactType 
					WHERE		ParentID = @id
					ORDER BY	name
			--		) A
			) BG
			FOR XML PATH('nav'), TYPE
	)
END

