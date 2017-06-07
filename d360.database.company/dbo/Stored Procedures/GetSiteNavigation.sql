CREATE PROCEDURE [dbo].[GetSiteNavigation]
(
	@ResourceID int = 0
)
AS
BEGIN
	SET NOCOUNT ON;

SELECT	n.Name as MenuID,
		n.SortOrder,
		0 as Feature,
		n.Icon as Icon,
		n.Title as Title,
		NULL AS Items	
FROM SiteNav n
WHERE n.Name = '#Monitor' AND dbo.HasSiteNavPermission(n.ID, @ResourceID) = 1
UNION ALL

SELECT	n.Name as MenuID,
		n.SortOrder,
		0 as Feature,
		n.Icon as Icon,
		n.Title as Title,
		NULL AS Items		
FROM SiteNav n
WHERE n.Name = '#Home' AND dbo.HasSiteNavPermission(n.ID, @ResourceID) = 1
UNION ALL

SELECT	n.Name as MenuID,
		n.SortOrder,
		0 as Feature,
		n.Icon as Icon,
		n.Title as Title,
		(
			SELECT	name,
					url,
					0 as feature,
					dbo.ArtifactNgSiteNavigation(id) as items
			FROM	(
					SELECT		TOP 1000
								a.id,
								a.name,
								dbo.GenerateNgObjectUrl('ArtifactType', a.ID, 0) As url
					FROM		ArtifactType a
					left join SiteNav v on v.ObjectID = a.ID and v.Object = 'ArtifactType'
					WHERE		a.ParentID IS NULL and v.ObjectID is null
					ORDER BY	a.name
					) BG
					FOR XML PATH('nav'), TYPE
		) AS Items
FROM SiteNav n
WHERE n.Name = '#Glossary' AND dbo.HasSiteNavPermission(n.ID, @ResourceID) = 1

UNION ALL

SELECT	n.Name as MenuID,
		n.SortOrder,
		0 as Feature,
		n.Icon as Icon,
		n.Title as Title,
		(
		SELECT	ft.name, 
				'model/classification/' + ft.name As url,
				0 as feature,
				(

				SELECT	t.name, 
						dbo.GenerateNgObjectUrl('TaxonomyType', 0, t.ID)  As url,
						0 as feature
				FROM	TaxonomyType t
				LEFT JOIN SiteNav v on v.ObjectID = t.ID and v.Object = 'TaxonomyType'
				WHERE	TaxonomyTypeClassID = FT.ID and v.ObjectID is null
				FOR XML PATH('nav'), TYPE
				) AS items	
		FROM	(
                select top 100 percent ID, name from TaxonomyTypeClass C where exists(select 1 from TaxonomyType where TaxonomyTypeClassID = C.ID) order by name
				) FT
		LEFT JOIN SiteNav v on v.ObjectID = FT.ID and v.Object ='TaxonomyTypeClass'
		WHERE v.ObjectID IS NULL
		FOR XML PATH('nav'), TYPE
		) AS Items
FROM SiteNav n
WHERE n.Name = '#Models' AND dbo.HasSiteNavPermission(n.ID, @ResourceID) = 1

UNION ALL

SELECT	n.Name as MenuID,
		n.SortOrder,
		0 as Feature,
		n.Icon as Icon,
		n.Title as Title,
		(
		SELECT	ft.name, 				
				'policy/classification/' + cast(ft.id as varchar(15)) As url,
				0 as feature,
				(
				SELECT	t.name, 
						dbo.GenerateNgObjectUrl('PolicyType', t.ID, 0)  As url,
						0 as feature
				FROM	PolicyType t
				LEFT JOIN SiteNav v on v.ObjectID = t.ID and v.Object = 'PolicyType'
				WHERE	PolicyTypeClassID = FT.ID and v.ObjectID is null
				FOR XML PATH('nav'), TYPE
				) AS items	
		FROM	(
                select top 100 percent ID, name from PolicyTypeClass C where exists(select 1 from PolicyType where PolicyTypeClassID = C.ID) order by name
				) FT
		LEFT JOIN SiteNav v on v.ObjectID = FT.ID and v.Object ='PolicyTypeClass'
		WHERE v.ObjectID IS NULL
		FOR XML PATH('nav'), TYPE
		) AS Items
		FROM SiteNav n
WHERE n.Name = '#Policy' AND dbo.HasSiteNavPermission(n.ID, @ResourceID) = 1
		
UNION ALL

SELECT	n.Name as MenuID,
		n.SortOrder,
		0 as Feature,
		n.Icon as Icon,
		n.Title as Title,
		null AS Items
FROM SiteNav n
WHERE n.Name = '#Reference' AND dbo.HasSiteNavPermission(n.ID, @ResourceID) = 1

UNION ALL

SELECT	n.Name as MenuID,
		n.SortOrder,
		2 as Feature,
		n.Icon as Icon,
		n.Title as Title,
		(
		SELECT		name, 
					dbo.GenerateNgObjectUrl('FusionType', FT.ID, 0)  As url,
					2 as feature,
					(
					SELECT		name, 
								dbo.GenerateObjectUrl('Fusion', FT.ID, Fusion.ID)  As url,
								'F' + cast(Fusion.ID as varchar(15)) as menuID,
								2 as feature
					FROM		Fusion
					WHERE		Fusion.FusionTypeID = FT.ID
					ORDER BY	name
					FOR XML PATH('nav'), TYPE
					) AS items	
		FROM		FusionType FT
		ORDER BY	name
		FOR XML PATH('nav'), TYPE
		) AS Items	
	FROM SiteNav n
WHERE n.Name = '#Fusion' AND dbo.HasSiteNavPermission(n.ID, @ResourceID) = 1
		
UNION ALL

SELECT	n.Name as MenuID, 
		n.SortOrder,
		4 as Feature,
		n.Icon as Icon,
		n.Title as Title,
		(
        SELECT	'People' AS name, --'#People' as MenuID,
                'community/groups' AS url, 		        
                0 as feature,
		        NULL AS Items
        FOR XML PATH('nav'), TYPE
        ) AS Items
FROM SiteNav n
WHERE n.Name = '#Community' AND dbo.HasSiteNavPermission(n.ID, @ResourceID) = 1
UNION ALL

SELECT	'#Admin' as MenuID,
		999 as SortOrder,
		0 as Feature,
		'fa-cogs' as Icon,
		'Administration' as Title,
		(
			select	*
			from	(
					SELECT	'Security' AS name, 
							'#/' AS url, 
							0 as feature,
							(
							select	*
							from	(
									SELECT	'Groups' AS name, 
											'#/groups/administration' AS url, 
											--'Menu_A_S_G' as menuID,
											0 as feature,
											NULL AS items
									union all
									SELECT	'Users' AS name, 
											'#/resources/administration' AS url, 
											--'Menu_A_S_R' as menuID,
											0 as feature,
											NULL AS items
									union all
									SELECT	'Responsibilities' AS name, 
											'#/governance/administration' AS url, 
											0 as feature,
											NULL AS items
                            ) bg
							FOR XML PATH('nav'), TYPE
							) AS items
						
					union all

					SELECT	'MetaModel' AS name, 
							'#/' AS url,
							0 as feature, 
							(
							select	*
							from	(
									SELECT	'Artifacts' AS name, 
											'#/artifacts/administration' AS url, 
											0 as feature,
											NULL AS items
									union all
									SELECT	'Attributes' AS name, 
											'#/attributes/administration' AS url, 
											0 as feature,
											NULL AS items
									union all
									SELECT	'Lookups' AS name, 
											'#/lookups/administration' AS url, 
											0 as feature,
											NULL AS items
									union all
									SELECT	'Models' AS name, 
											'#/catalogs/administration' AS url, 
											0 as feature,
											NULL AS items
                                    union all
									SELECT	'Policies' AS name, 
											'#/policies/administration' AS url, 
											1 as feature,
											NULL AS items
                                    union all
									SELECT	'Relationships' AS name, 
											'#/relations/administration' AS url, 
											0 as feature,
											NULL AS items
                                    union all
                                    SELECT	'Rules' AS name, 
											'#/rules/administration' AS url, 
											0 as feature,
											NULL AS items
									) bg
							FOR XML PATH('nav'), TYPE
							) AS items
						
					union all

					SELECT	'Metrics' AS name, 
							'#/' AS url,
							0 as feature, 
							(
							select	*
							from	(
									SELECT	'Scoring' AS name, 
											'#/analytics/administration' AS url, 
											5 as feature,
											NULL AS items
									union all
					                SELECT	'Dashboards' AS name, 
							                '#/reporting/administration' AS url, 
							                0 as feature,
							                NULL AS items
                                    union all
					                SELECT	'Surveys' AS name, 
							                '#/surveys/administration' AS url, 
							                7 as feature,
							                (
							                SELECT	'Response Types' AS name, 
									                '#/surveyresponsetypes/administration' AS url, 
									                7 as feature,
									                NULL AS items
							                FOR XML PATH('nav'), TYPE
							                ) AS items
									) bg
							FOR XML PATH('nav'), TYPE
							) AS items
						
					union all

					SELECT	'Reference' AS name, 
							'#/domains/administration' AS url, 
							0 as feature,
							NULL AS items

					union all

					SELECT	'Workflow' AS name, 
							'#/workflow/administration' AS url, 
							0 as feature,
							NULL AS items

                    union all

                    SELECT	'Templates' AS name, 
							'#/templates/administration' AS url, 
							0 as feature,
							NULL AS items

					union all

					SELECT	'Integration' AS name, 
							'#/' AS url, 
							0 as feature,
							(
							select	*
							from	(
									SELECT	'Bulk Loader' AS name, 
											'#/load' AS url, 
											0 as feature,
											NULL AS items
									union all
									SELECT	'Fusion' AS name, 
											'#/fusion/administration' AS url, 
											2 as feature,
											NULL AS items
									union all
									SELECT	'API' AS name, 
											'/swagger' AS url, 
											0 as feature,
											NULL AS items
									) bg
							FOR XML PATH('nav'), TYPE
							) AS items

                    union all

                    SELECT	'Settings' AS name, 
							'#/settings' AS url, 
							0 as feature,
							NULL AS items
            ) bg
			for xml path('nav'), type
		) as Items

	where 1 = 1

	UNION ALL

	SELECT	n.Name as MenuID,
		n.SortOrder,
		0 as Feature,
		n.Icon as Icon,
		n.Title as Title,
		(
		SELECT	RT.name, 				
				dbo.GenerateNgObjectUrl('RuleType', RT.ID, RT.ID) As url,
				0 as feature,
				null AS items	
		FROM	RuleType RT
				LEFT JOIN SiteNav v on v.ObjectID = RT.ID and v.Object ='RuleType'
		WHERE	v.ObjectID IS NULL
		FOR XML PATH('nav'), TYPE
		) AS Items
	FROM SiteNav n
	WHERE n.Name = '#Data Quality' AND dbo.HasSiteNavPermission(n.ID, @ResourceID) = 1

	UNION ALL

	SELECT 
		'~' + Name AS MenuID,
		s.SortOrder,
		0 AS Feature,
		s.Icon as Icon,
		s.Title as Title,
		dbo.CustomSiteNavigation(ID) AS Items
	from SiteNav s
	where ParentID IS NULL and Name not like '#%' AND dbo.HasSiteNavPermission(s.ID, @ResourceID) = 1

	order by sortorder
END
