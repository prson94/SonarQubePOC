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
using System;

namespace d360.web.Controllers
{
    [Authorize, RoutePrefix("navigation")]
    public class NavigationController : BaseController
    {
        #region DI

        public NavigationController(CommunityContext community, CompanyContext company)
            : base(community, company)
        { }

        #endregion

        [Authorize, Route("top")]
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
        
        [Route("sitemenu")]
        public JsonNetResult SiteMenu()
        {
            return new JsonNetResult
            {
                Data = new
                {
                    MenuItems = GetSiteNavigation(true),
                    IsAdmin = Company.CurrentResourceIsAdmin
                },
                Formatting = Newtonsoft.Json.Formatting.None
            };
        }

        [Route("GetSiteNavigation")]
        public List<TopNavigationItem> GetSiteNavigation(bool bNewSite = false)
        {
            List<TopNavigationItem> nodes = null;

            if (!bNewSite)
            {

                #region Query Old Site

                nodes = Company.Query<TopNavigationItem>(string.Format(@"
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
		(
        SELECT	'People' AS name, --'#People' as MenuID,
                '#/groups' AS url, 		        
                0 as feature,
		        NULL AS Items
        FOR XML PATH('nav'), TYPE
        ) AS Items

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
							                NULL as items
                                            --(
							                --SELECT	'Response Types' AS name, 
									        --        '#/surveyresponsetypes/administration' AS url, 
									        --        7 as feature,
									        --        NULL AS items
							                --FOR XML PATH('nav'), TYPE
							                --) AS items
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

	where {0} = 1", (Company.CurrentResourceIsAdmin ? "1" : "0"))).ToList();

                #endregion
            }
            else
            {
                #region Query Angular

                nodes = Company.Query<TopNavigationItem>(string.Format(@"GetSiteNavigation", (Company.CurrentResourceIsAdmin ? "1" : "0"))).ToList();

                #endregion
            }

            if (nodes == null) return null;

            var features = Community.Filter<CompanyFeature>(i => i.CompanyID == Company.CurrentCompanyID).ToList();

            nodes.ForEach(n => {
                n.ShouldDisplay = features.Any(f => f.Feature == n.Feature);
                n.NavigationItems = (string.IsNullOrEmpty(n.Items)) ? 
                    new List<NavigationItem>() :
                    parseXmlNavigationDocument(XElement.Parse(string.Format("<nav>{0}</nav>", n.Items)), features);
            });

            return nodes;
        }

        [Route("GetAvailableSiteNavigation")]
        public JsonNetResult GetAvailableSiteNavigation()
        {
            var nav = Company.Query<dynamic>(@"
                with s as
                (
	                select cast(
	                case when object = 'ArtifactType' then
		                'Glossary :: ' + name
	                when object = 'TaxonomyTypeClass' then
		                'Models :: ' + name
	                when object = 'PolicyTypeClass' then
		                'Policy :: ' + name
	                else
		                name
	                end	
	                 as varchar(500)) as DisplayName,* from sitenavavailable where parentid is null
	                union all
	                select cast((s.DisplayName + ' :: ' + v.name) as varchar(500)) as displayname, v.* from sitenavavailable v join s on s.objectid = v.parentid and 
	                case when v.object = 'TaxonomyType' or v.object = 'PolicyType' then
		                v.object + 'Class'
	                else
		                v.object
	                end = s.object
                )
                select * from s where object not like '%Class'").ToList();

            return new JsonNetResult
            {
                Data = nav,
                Formatting = Newtonsoft.Json.Formatting.None
            };
        }

        [Authorize, HttpGet, Route("GetSiteNavItems")]
        public JsonNetResult GetSiteNavItems()
        {
            return new JsonNetResult
            {
                Data = Company.SiteNav.Where(s => s.ParentID == null && s.Name != "#Home").OrderBy(s => s.SortOrder).ToList(),
                Formatting = Newtonsoft.Json.Formatting.None
            };
        }

        [Authorize, HttpPost, Route("AddFolderItem")]
        public JsonNetResult AddFolderItem(SiteNav item)
        {
            var success = true;
            var message = "";
            try
            {
                if (string.IsNullOrWhiteSpace(item.Name))
                    throw new Exception("Folder name cannot be empty");

                var deleteExisting = Company.Query<SiteNav>(@"with s as
                (
	                select * from sitenavflat where objectid = @ObjectID and object = @Object
	                union all
	                select v.* from sitenavflat v join s on s.objectid = v.parentid and s.object = v.object
                )
                select n.* from s
                join sitenav n on n.Object = s.Object and n.ObjectID = s.ObjectID", new { ObjectID = item.ObjectID, Object = item.Object }).ToList();

                deleteExisting.ForEach(d =>
                {
                    var record = Company.GetById<SiteNav>(d.ID);
                    Company.Delete(record);
                });

                Company.Add(item);
                Company.SaveChanges();
                message = "Folder item added successfully.";
            } catch(Exception ex)
            {
                success = false;
                message = ex.GetFullExceptionData();
            }
           
            return new JsonNetResult
            {
                Data = new { success, message },
                Formatting = Newtonsoft.Json.Formatting.None
            };

        }

        [Authorize, HttpPost, Route("RemoveFolderItem")]
        public JsonNetResult RemoveFolderItem(int id)
        {
            var success = true;
            var message = "";
            try
            {
                var fi = Company.GetById<SiteNav>(id);
                if (fi == null)
                    throw new Exception($"Folder Item Id ${id} not found");
                Company.Delete(fi);
                Company.SaveChanges();
                message = "Folder item removed successfully.";
            } catch (Exception ex)
            {
                success = false;
                message = ex.GetFullExceptionData();
            }

            return new JsonNetResult
            {
                Data = new { success, message },
                Formatting = Newtonsoft.Json.Formatting.None
            };

        }

        [Authorize, HttpPost, Route("RemoveFolder")]
        public JsonNetResult RemoveFolder(int id)
        {
            var success = true;
            var message = "";

            try
            {
                var folder = Company.GetById<SiteNav>(id);
                if (folder == null)
                    throw new Exception($"Folder id ${id} not found");

                var subNavs = Company.SiteNav.Where(s => s.ParentID == folder.ID);

                Company.SiteNav.RemoveRange(subNavs);
                Company.Delete(folder);
                Company.SaveChanges();
                message = "Folder removed successfully.";
            } catch (Exception ex)
            {
                success = false;
                message = ex.GetFullExceptionData();
            }

            return new JsonNetResult
            {
                Data = new { success, message },
                Formatting = Newtonsoft.Json.Formatting.None
            };
        }

        [Authorize, HttpPost, Route("AddFolder")]
        public JsonNetResult AddFolder(AddSiteNavModel model)
        {
            var success = true;
            var message = "";
            try
            {
                if (string.IsNullOrWhiteSpace(model.Folder.Name))
                    throw new Exception("Folder name cannot be empty.");
                model.Folder.SortOrder = 9999;
                var folder = Company.SiteNav.Add(model.Folder);
                folder.Title = model.Folder.Name;
                Company.SaveChanges();
                model.Items.ForEach(i =>
                {
                    i.ParentID = folder.ID;
                });

                Company.SiteNav.AddRange(model.Items);
                Company.SaveChanges();
                message = "Folder added successfully";
            } catch (Exception ex)
            {
                success = false;
                message = ex.GetFullExceptionData();
            }

            return new JsonNetResult
            {
                Data = new { success, message },
                Formatting = Newtonsoft.Json.Formatting.None
            };

        }

        [Authorize, HttpPut, Route("MoveUp")]
        public JsonNetResult MoveUp(int id)
        {
            var success = true;
            var message = "";

            try
            {
                var siteNav = Company.GetById<SiteNav>(id);
                var siteNavAbove = Company.SiteNav.Where(s => s.ParentID == null && s.SortOrder == siteNav.SortOrder - 1).SingleOrDefault();

                if (siteNav == null)
                    throw new Exception($"Folder id ${id} not found");
                if (siteNavAbove == null)
                    throw new Exception("This folder is already sorted to the top");

                siteNavAbove.SortOrder++;
                siteNav.SortOrder--;
                Company.SaveChanges();
                message = $"Folder ${siteNav.Name} moved up successfully.";
            }
            catch (Exception ex)
            {
                success = false;
                message = ex.GetFullExceptionData();
            }

            return new JsonNetResult
            {
                Data = new { success, message },
                Formatting = Newtonsoft.Json.Formatting.None
            };
        }

        [Authorize, HttpPut, Route("MoveDown")]
        public JsonNetResult MoveDown(int id)
        {
            var success = true;
            var message = "";
            try
            {
                var siteNav = Company.GetById<SiteNav>(id);
                var siteNavBelow = Company.SiteNav.Where(s => s.ParentID == null && s.SortOrder == siteNav.SortOrder + 1).SingleOrDefault();

                if (siteNav == null)
                    throw new Exception($"Folder Id ${id} not found");
                if (siteNavBelow == null)
                    throw new Exception($"This folder is already sorted to the bottom.");

                siteNavBelow.SortOrder--;
                siteNav.SortOrder++;
                Company.SaveChanges();
                message = $"Folder ${siteNav.Name} moved down successfully.";
            }
            catch (Exception ex)
            {
                success = false;
                message = ex.GetFullExceptionData();
            }
            return new JsonNetResult
            {
                Data = new { success, message },
                Formatting = Newtonsoft.Json.Formatting.None
            };
        }

        [Authorize, HttpPut, Route("EditFolder")]
        public JsonNetResult EditFolder(SiteNav folder)
        {
            var success = true;
            var message = "";
            try
            {
                if(folder == null)
                    throw new Exception("Invalid folder.");
                var siteNav = Company.GetById<SiteNav>(folder.ID);
                if (siteNav == null)
                    throw new Exception($"Folder Id ${folder.ID} not found.");
                siteNav.Name = folder.Name;
                siteNav.Icon = folder.Icon;
                siteNav.Title = folder.Name;
                Company.SaveChanges();
                message = "Folder renamed successfully.";
            }
             catch (Exception ex)
            {
                success = false;
                message = ex.GetFullExceptionData();
            }

            return new JsonNetResult
            {
                Data = new { success, message },
                Formatting = Newtonsoft.Json.Formatting.None
            };

        }

        #region Favorites

        /// <summary>
        /// Clears current users favorites list
        /// </summary>
        /// <returns></returns>
        [Authorize, HttpDelete, Route("DeleteMyFavorites")]
        public JsonNetResult DeleteMyFavorites()
        {
            var success = true;
            var message = "";

            try
            {                
                Company.Delete<Favorite>(i => i.ResourceID == Company.CurrentResourceID);

                message = "Favorites List Cleared.";
            }
            catch (Exception ex)
            {
                success = false;
                message = ex.GetFullExceptionData();
            }
            return new JsonNetResult
            {
                Data = new { success, message },
                Formatting = Newtonsoft.Json.Formatting.None
            };
        }

        [Authorize, HttpPut, Route("ToggleFavorite")]
        public JsonNetResult ToggleFavorite(Favorite favorite, bool admin = false)
        {
            var success = true;
            var message = "";

            try
            {
                if (admin && !Company.CurrentResourceIsAdmin)
                    throw new Exception("You do not have permission to perform this action");

                favorite.ResourceID = admin ? 0 : Company.CurrentResourceID;
                favorite.SortOrder = Company.Favorites.Count(f => f.ResourceID == favorite.ResourceID) + 1;

                var existing = Company.Favorites.FirstOrDefault(f => f.ResourceID == favorite.ResourceID && f.Route == favorite.Route);
                var adminExisting = Company.Favorites.FirstOrDefault(f => f.ResourceID == 0 && f.Route == favorite.Route);

                if (!admin)
                    if (adminExisting != null)
                        favorite.IsOverride = true;

                if (existing != null && adminExisting != null && !admin)
                {
                    existing.IsOverride = !existing.IsOverride;
                    Company.Update(existing);
                }
                else if (existing != null && existing.IsOverride && !admin)
                {
                    existing.IsOverride = false;
                    Company.Update(existing);
                }
                else if (existing == null)
                    Company.Add(favorite);
                else
                    Company.Delete(existing);
                message = "Favorite updated.";
            }
            catch (Exception ex)
            {
                success = false;
                message = ex.GetFullExceptionData();
            }
            return new JsonNetResult
            {
                Data = new { success, message},
                Formatting = Newtonsoft.Json.Formatting.None
            };

        }

        [Authorize, HttpPut, Route("MoveFavorite")]
        public JsonNetResult MoveFavorite(string route, bool moveUp = false, bool admin = false)
        {
            var success = true;
            var message = "";
            try
            {
                if (admin && !Company.CurrentResourceIsAdmin)
                    throw new Exception("user does not have admin privileges.");

                var resid = admin ? 0 : Company.CurrentResourceID;

                var favorite = Company.Favorites.Where(f => f.ResourceID == resid && f.Route == route).First();

                if (favorite == null)
                    throw new Exception("no favorite with supplied route");
                if (moveUp)
                {
                    var above = Company.Favorites.Where(f => f.SortOrder == (favorite.SortOrder - 1) && f.ResourceID == favorite.ResourceID).SingleOrDefault();
                    if (above == null)
                        throw new Exception("no favorite above");
                    favorite.SortOrder--;
                    above.SortOrder++;
                }
                else
                {
                    var below = Company.Favorites.Where(f => f.SortOrder == (favorite.SortOrder + 1) && f.ResourceID == favorite.ResourceID).SingleOrDefault();
                    if (below == null)
                        throw new Exception("no favorite below");
                    favorite.SortOrder++;
                    below.SortOrder--;
                }

                Company.SaveChanges();
                message = "Favorite moved successfully.";
            } catch (Exception ex)
            {
                success = false;
                message = ex.GetFullExceptionData();
            }

            return new JsonNetResult
            {
                Data = new { success, message },
                Formatting = Newtonsoft.Json.Formatting.None
            };

        }

        [Authorize, HttpGet, Route("GetFavorites")]
        public JsonNetResult GetFavorites(bool adminOnly = false)
        {
            var favorites = Company.Favorites.Where(f => f.ResourceID == Company.CurrentResourceID && !f.IsOverride).OrderBy(f => f.SortOrder).ToList();
            var overrides = Company.Favorites.Where(f => f.ResourceID == Company.CurrentResourceID && f.IsOverride).Select(o => o.Route).ToList();
            var adminFavorites = Company.Favorites.Where(f => f.ResourceID == 0).OrderBy(f => f.SortOrder).ToList();
            var filteredAdminFavorites = adminFavorites.Where(f => !overrides.Contains(f.Route)).OrderBy(f => f.SortOrder).ToList();
            favorites = favorites.Where(f => !filteredAdminFavorites.Select(a => a.Route).Contains(f.Route)).ToList();


            favorites.InsertRange(0, filteredAdminFavorites);


            return new JsonNetResult
            {
                Data = adminOnly ? adminFavorites : favorites,
                Formatting = Newtonsoft.Json.Formatting.None
            };

        }
        
        #endregion  

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
