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
using d360.web.Models.Attributes;
using d360.extensions;
using d360.web.Repositories;

namespace d360.web.Controllers
{
    [Authorize, RoutePrefix("navigation")]
    public class NavigationController : BaseController
    {
        #region DI

        SiteMenuRepository menuRepository;

        public NavigationController(CommunityContext community, CompanyContext company)
            : base(community, company)
        {
            menuRepository = new SiteMenuRepository(community, company);
        }

        #endregion
      
        [Route("sitemenu")]
        public JsonNetResult SiteMenu()
        {
            return new JsonNetResult
            {
                Data = new
                {
                    MenuItems = menuRepository.SiteMenu,
                    IsAdmin = Company.CurrentResourceIsAdmin
                },
                Formatting = Newtonsoft.Json.Formatting.None
            };
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
	                 as varchar(500)) as Title,* from sitenavavailable where parentid is null
	                union all
	                select cast((s.Title + ' :: ' + v.name) as varchar(500)) as Title, v.* from sitenavavailable v join s on s.objectid = v.parentid and 
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
                menuRepository.ClearCachedMenu();
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

        [Authorize, HttpPost, Route("RemoveFolderItem"), NonNullableParameters]
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
                menuRepository.ClearCachedMenu();
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

        [Authorize, HttpPost, Route("RemoveFolder"), NonNullableParameters]
        public JsonNetResult RemoveFolder(int id)
        {
            var success = true;
            var message = "";

            try
            {
                var folder = Company.GetById<SiteNav>(id);
                if (folder == null)
                    throw new Exception($"Folder id ${id} not found");

                //clear out permissions
                folder.Permissions = new List<SiteNavPermission>();
                SetSiteNavPermissions(folder);

                var subNavs = Company.SiteNav.Where(s => s.ParentID == folder.ID);

                Company.SiteNav.RemoveRange(subNavs);
                Company.Delete(folder);
                Company.SaveChanges();
                menuRepository.ClearCachedMenu();
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
                SetSiteNavPermissions(model.Folder);
                menuRepository.ClearCachedMenu();
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

        [Authorize, HttpPut, Route("MoveUp"), NonNullableParameters]
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
                menuRepository.ClearCachedMenu();
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

        [Authorize, HttpPut, Route("MoveDown"), NonNullableParameters]
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
                menuRepository.ClearCachedMenu();
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
                siteNav.Title = folder.Title ?? folder.Name;
                Company.SaveChanges();
                SetSiteNavPermissions(folder);
                menuRepository.ClearCachedMenu();
                message = "Folder updated successfully.";

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

        #region Permissions
        
        [Authorize, HttpGet, Route("permissions/get/list/{id:int}")]
        public JsonNetResult GetSiteNavPermissionList(int id = 0)
        {
                var sql = @"
                    select * from
                    (
	                    select 'Group :: ' + g.Name as label, 'Group|' + cast(g.ID as varchar) as [value] from [Group] g
	                    union all
	                    select 'Resource :: ' + r.FirstName + ' ' + r.LastName as label, 'Resource|' + cast(r.ResourceID as varchar) as [value] from reporting.Global_Resource r
                    ) a
                    where a.[value] not in (select p.[Object] + '|' + cast(p.ObjectID as varchar) as [value] from SiteNavPermission p where p.SiteNavID = 191)
                    order by a.label
                    ";

            var results = Company.Query<dynamic>(sql, new { id }).ToList();

            return new JsonNetResult
            {
                Data = results,
                Formatting = Newtonsoft.Json.Formatting.None
            };
        }

        [Authorize, HttpGet, Route("permissions/get/{id:int}")]
        public JsonNetResult GetSiteNavPermissions(int id)
        {
            var perms = Company.Query<SiteNavPermission>(QueryConstants.SiteNavPermissions, new { id }).ToList();

            return new JsonNetResult
            {
                Data = perms,
                Formatting = Newtonsoft.Json.Formatting.None
            };
        }

        [Authorize, HttpPost, Route("permissions/set")]
        public JsonNetResult SetSiteNavPermissions(SiteNav nav)
        {
            if (!Company.CurrentResourceIsAdmin)
                return jsonNetException(new Exception("You do not have permission to do this"));

            if (nav == null || nav.ID < 1)
                return jsonNetException(new Exception("The model passed to the method was invalid"));

            var existing = Company.SiteNavPermissions.Where(p => p.SiteNavID == nav.ID).ToList();

            if (nav.Permissions == null)
                nav.Permissions = new List<SiteNavPermission>();

            nav.Permissions.ForEach(p => { p.SiteNavID = nav.ID; });

            try
            {
                Company.SiteNavPermissions.RemoveRange(existing);
                Company.SaveChanges();
                if (nav.Permissions != null && nav.Permissions.Count > 0)
                {
                    Company.SiteNavPermissions.AddRange(nav.Permissions);
                    Company.SaveChanges();
                }
            }
            catch(Exception ex)
            {
                return jsonNetException(ex);
            }

            return new JsonNetResult
            {
                Data = nav.ID,
                Formatting = Newtonsoft.Json.Formatting.None
            };
        }

        [Authorize, HttpPost, Route("permissions/add")]
        public JsonNetResult AddSiteNavPermission(SiteNavPermission perm)
        {
            var nav = Company.GetById<SiteNav>(perm.SiteNavID);

            if (!Company.CurrentResourceIsAdmin)
                return jsonNetException(new Exception("You do not have permission to do this"));

            if (nav == null)
                return jsonNetException(new Exception("The site nav specified was not found"));

            if (string.IsNullOrEmpty(perm.Object) || perm.ObjectID == 0)
                return jsonNetException(new Exception("Invalid object passed"));

            try
            {
                Company.SiteNavPermissions.Add(perm);
                Company.SaveChanges();
                menuRepository.ClearCachedMenu();
            }
            catch(Exception ex)
            {
                return jsonNetException(ex);
            }

            return new JsonNetResult
            {
                Data = perm,
                Formatting = Newtonsoft.Json.Formatting.None
            };

        }

        [Authorize, HttpDelete, Route("permissions/remove")]
        public JsonNetResult RemoveSiteNavPermission(SiteNavPermission perm)
        {
            if (!Company.CurrentResourceIsAdmin)
                return jsonNetException(new Exception("You do not have permission to do this"));

            perm = Company.SiteNavPermissions.Where(p => p.SiteNavID == perm.SiteNavID && p.Object == perm.Object && p.ObjectID == perm.ObjectID).FirstOrDefault();

            if (perm != null)
            {
                try
                {
                    Company.SiteNavPermissions.Remove(perm);
                    Company.SaveChanges();
                    menuRepository.ClearCachedMenu();
                }
                catch (Exception ex)
                {
                    return jsonNetException(ex);
                }
            }
            else
                return jsonNetException(new Exception("Could not find existing permission"));

            return new JsonNetResult
            {
                Data = perm.SiteNavID,
                Formatting = Newtonsoft.Json.Formatting.None
            };
        }

        #endregion

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
        public JsonNetResult ToggleFavorite(Favorite favorite)
        {
            var success = true;
            var message = "";

            try
            {
                favorite.ResourceID = Company.CurrentResourceID;
                favorite.SortOrder = Company.Favorites.Count(f => f.ResourceID == favorite.ResourceID) + 1;
                favorite.IsOverride = false;

                Favorite existing = null;

                if (!string.IsNullOrEmpty(favorite.Object) && favorite.ObjectID > 0) {
                    existing = Company.Favorites.FirstOrDefault(f => f.ResourceID == favorite.ResourceID && f.Object == favorite.Object && f.ObjectID == favorite.ObjectID);
                }
                else
                {
                    existing = Company.Favorites.FirstOrDefault(f => f.ResourceID == favorite.ResourceID && f.Name == favorite.Name && f.Route == favorite.Route);
                }
                              
                if (existing == null)
                {
                    Company.Add(favorite);

                    message = "Favorite Added.";
                }
                else
                {
                    Company.Delete(existing);

                    message = "Favorite Removed.";
                }                
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

        [Authorize, HttpPut, Route("MoveFavorite"), NonNullableParameters]
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
            var sql = @"select 
	                    od.Name as Name,	
	                    fav.Route as [Route],
	                    fav.[Object],
	                    fav.[ObjectId],
	                    fav.SortOrder,
	                    fav.Id,
                        fav.ResourceId
                    from
	                    [dbo].[favorite] fav
	                    inner join [cache].[objectdetails] od on ( fav.[Object] = od.[Object] and fav.[ObjectId] = od.[ObjectId])
                    where 
	                    fav.objectid > 0 and fav.resourceid = @resId  
                    union
                    select 
	                    fav.Name as Name,	
	                    fav.Route as [Route],
	                    fav.[Object],
	                    fav.[ObjectId],
	                    fav.SortOrder,
	                    fav.Id,
                        fav.ResourceId
                    from
	                    [dbo].[favorite] fav	
                    where 
	                    fav.objectid is null and fav.resourceid = @resId  order by fav.sortorder";

            var favorites = Company.Query<Favorite>(sql, new { resId = Company.CurrentResourceID }).ToList();

            return new JsonNetResult
            {
                Data = favorites,
                Formatting = Newtonsoft.Json.Formatting.None
            };

        }
        
        #endregion  

        
    }
}
