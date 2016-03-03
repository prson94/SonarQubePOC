using System.Web.Mvc;
using System.Linq;
using d360.core;
using System.Collections.Generic;
using d360.web.Models;
using d360.model;
using d360.core.entities;
using System.Xml.Linq;
using d360.core.enums;
using System.Text;
using System.Security.Cryptography;

namespace d360.web.Controllers
{
    [Authorize]
    public class NavigationController : BaseController
    {
        #region DI

        public NavigationController(CommunityContext community, CompanyContext company)
            : base(community, company)
        { }

        #endregion

        [Authorize]
        public ActionResult Top()
        {
            var resource = Community.GetById<Resource>(Company.CurrentResourceID);

            var md5Hasher = MD5.Create();

            // Convert the input string to a byte array and compute the hash.  
            byte[] data = md5Hasher.ComputeHash(Encoding.Default.GetBytes(resource.Email));

            // Create a new Stringbuilder to collect the bytes and create a string.  
            var sBuilder = new StringBuilder();

            // Loop through each byte of the hashed data and format each one as a hexadecimal string.  
            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }

            var navigation = new TopNavigation 
            {
                ResourceID = Company.CurrentResourceID,
                ResourceName = resource.FormatDisplayName(),
                ResourceImageUrl = string.Format("https://secure.gravatar.com/avatar/{0}?s={1}", sBuilder.ToString(), 150),
                ResourceUrl = string.Format("#/resources/{0}", Company.CurrentResourceID),
                LastLoggedInDate = resource.DateLastLoggedIn.HasValue ? resource.DateLastLoggedIn.Value.ToShortDateString() : "",
                NavigationItems = GetSiteNavigation()
            };
            return PartialView(navigation);
        }

        public List<TopNavigationItem> GetSiteNavigation()
        {
            #region Query

            var nodes = Company.Query<TopNavigationItem>(string.Format(@"
SELECT	'#Home' as MenuID,
		0 as Feature,
		NULL AS Items
		
UNION ALL

SELECT	'#Glossary' as MenuID,
		0 as Feature,
		(
			SELECT	name,
					url,
					0 as feature,
					dbo.ArtifactSiteNavigation(id) as items
			FROM	(
					SELECT		TOP 1000
								id,
								name,
								dbo.GenerateObjectUrl('ArtifactType', ID, 0) As url
					FROM		ArtifactType 
					WHERE		ParentID IS NULL
					ORDER BY	name
					) BG
					FOR XML PATH('nav'), TYPE
		) AS Items

UNION ALL

SELECT	'#Models' as MenuID,
		0 as Feature,
		(
		SELECT	name, 
				'#/catalogs?classification=' + name As url,
				0 as feature,
				(
				SELECT	name, 
						dbo.GenerateObjectUrl('TaxonomyType', ID, 0)  As url,
						0 as feature
				FROM	TaxonomyType
				WHERE	TaxonomyTypeClassID = FT.ID
				FOR XML PATH('nav'), TYPE
				) AS items	
		FROM	(
                select top 100 percent ID, name from TaxonomyTypeClass C where exists(select 1 from TaxonomyType where TaxonomyTypeClassID = C.ID) order by name
				) FT
		FOR XML PATH('nav'), TYPE
		) AS Items

UNION ALL

SELECT	'#People' as MenuID,
		0 as Feature,
		NULL AS Items

UNION ALL

		
SELECT	'#Monitor' as MenuID,
		1 as Feature, 
		(
        select  *
        from    (
		        SELECT	name, 
				        '#/' As url,
				        0 as feature,
				        (
				        SELECT	name, 
						        dbo.GenerateObjectUrl('PolicyType', ID, 0)  As url,
						        0 as feature
				        FROM	PolicyType
				        WHERE	PolicyTypeClassID = FT.ID
				        FOR XML PATH('nav'), TYPE
				        ) AS items	
		        FROM	(
                        select top 100 percent ID, name from PolicyTypeClass C where exists(select 1 from PolicyType where PolicyTypeClassID = C.ID) order by name
				        ) FT
				union all
				SELECT	'Rules' AS name, 
						'#/rules' AS url, 
						0 as feature,
						NULL AS items
                ) as mo
		FOR XML PATH('nav'), TYPE
		) AS Items

UNION ALL

SELECT	'#Domains' as MenuID,
		0 as Feature,
		(
		SELECT	name, 
				dbo.GenerateObjectUrl('DomainType', ID, 0)  As url,
				0 as feature
		FROM	DomainType
		FOR XML PATH('nav'), TYPE				
		) AS Items

UNION ALL

SELECT	'#Fusion' as MenuID,
		2 as Feature,
		(
		SELECT		name, 
					dbo.GenerateObjectUrl('FusionType', FT.ID, 0)  As url,
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
			
UNION ALL

SELECT	'#Community' as MenuID, 
		4 as Feature,
		NULL AS Items

UNION ALL

SELECT	'#Admin' as MenuID,
		0 as Feature,
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
									SELECT	'Analytics' AS name, 
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
											'/help' AS url, 
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

	where {0} = 1", (Company.CurrentResourceIsAdmin ? "1" : "0"))).ToList();

            #endregion

            var features = Community.Filter<CompanyFeature>(i => i.CompanyID == Company.CurrentCompanyID).ToList();

            nodes.ForEach(n => {
                n.ShouldDisplay = features.Any(f => f.Feature == n.Feature);
                n.NavigationItems = (string.IsNullOrEmpty(n.Items)) ? 
                    new List<NavigationItem>() :
                    parseXmlNavigationDocument(XElement.Parse(string.Format("<nav>{0}</nav>", n.Items)), features);
            });

            return nodes;
        }

        List<NavigationItem> parseXmlNavigationDocument(XElement xml, List<CompanyFeature> features)
        {
            var items = new List<NavigationItem>();

            foreach (var el in xml.Elements("nav"))
            {
                bool shouldParse = (el.Element("feature").Value == "0");
                if (!shouldParse)   //further check is required.
                {
                    var feature = (Feature)System.Enum.Parse(typeof(Feature), el.Element("feature").Value);
                    shouldParse = features.Any(i => i.Feature == feature);
                }
                if (shouldParse)
                {
                    var item = new NavigationItem { Name = el.Element("name").Value, Url = el.Element("url").Value };
                    if (el.Element("items") != null)
                    {
                        item.Items = parseXmlNavigationDocument(el.Element("items"), features);
                    }
                    items.Add(item);
                }
            }

            return items;
        }
    }
}
