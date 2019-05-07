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
using d360.web.Filters;
using Resources;
using System.Net;
using System.IO;
using System.Threading.Tasks;

namespace d360.web.Controllers
{
    [Authorize, RoutePrefix("navigation")]
    public class NavigationController : BaseController
    {
        #region DI
        IStorageProvider Storage;
        public NavigationController(ICommunityContext community, ICompanyContext company, IStorageProvider storage)
            : base(community, company)
        {
            Storage = storage;
        }

        #endregion

        [ValidateContracts(Ignore = true), Route("sitemenu")]
        public JsonNetResult SiteMenu()
        {
            List<TopNavigationItem> nodes = null;

            nodes = Company.Query<TopNavigationItem>("GetSiteNavigation @ResourceID", new { ResourceID = Company.CurrentResourceID }).ToList();

            var features = Community.Filter<CompanyFeature>(i => i.CompanyID == Company.CurrentCompanyID).ToList();

            if (nodes != null)
                nodes.ForEach(n => {
                    n.ShouldDisplay = features.Any(f => f.Feature == n.Feature);
                    n.NavigationItems = (string.IsNullOrEmpty(n.Items)) ?
                        new List<NavigationItem>() :
                        parseXmlNavigationDocument(XElement.Parse(string.Format("<nav>{0}</nav>", n.Items)), features);
                });

            return new JsonNetResult
            {
                Data = new
                {
                    MenuItems = nodes,
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
	                else
		                name
	                end	
	                 as varchar(500)) as Title,* from sitenavavailable where parentid is null
	                union all
	                select cast((s.Title + ' :: ' + v.name) as varchar(500)) as Title, v.* from sitenavavailable v join s on s.objectid = v.parentid and 
	                v.object = s.object
                )
                select * from s where object not like '%Class' order by 1 asc").ToList();

            return new JsonNetResult
            {
                Data = nav,
                Formatting = Newtonsoft.Json.Formatting.None
            };
        }

        [HttpGet, Route("GetSiteNavItems")]
        public JsonNetResult GetSiteNavItems()
        {
            return new JsonNetResult
            {
                Data = Company.SiteNav.Where(s => s.ParentID == null && s.Name != "#Home").OrderBy(s => s.SortOrder).ToList(),
                Formatting = Newtonsoft.Json.Formatting.None
            };
        }

        [HttpPost, Route("AddFolderItem"), AjaxValidateAntiForgeryToken]
        public JsonNetResult AddFolderItem(SiteNav item)
        {
            if (!Company.CurrentResourceIsAdmin)
                return jsonNetException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

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
                item.SortOrder = Company.SiteNav.Max(i=> i.SortOrder) + 1;
                Company.Add(item);
                Company.SaveChanges();
                message = "Folder item added successfully.";
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

        [HttpPost, Route("RemoveFolderItem"), NonNullableParameters, AjaxValidateAntiForgeryToken]
        public JsonNetResult RemoveFolderItem(int id)
        {
            if (!Company.CurrentResourceIsAdmin)
                return jsonNetException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

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

        [HttpPost, Route("RemoveFolder"), NonNullableParameters, AjaxValidateAntiForgeryToken]
        public JsonNetResult RemoveFolder(int id)
        {
            if (!Company.CurrentResourceIsAdmin)
                return jsonNetException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

            var success = true;
            var message = "";

            try
            {
                var folder = Company.GetById<SiteNav>(id);
                if (folder == null)
                    throw new Exception($"Folder id ${id} not found");
                string originalImage = folder.ImageIconUrl;
                if (!string.IsNullOrEmpty(originalImage))
                {
                    Storage.DeleteFile(constants.COMPANY_RESOURCES_FOLDER, originalImage);
                }
                //clear out permissions
                folder.Permissions = new List<SiteNavPermission>();
                SetSiteNavPermissions(folder);

                var subNavs = Company.SiteNav.Where(s => s.ParentID == folder.ID);

                Company.SiteNav.RemoveRange(subNavs);
                Company.Delete(folder);
                Company.SaveChanges();
                message = "Folder removed successfully.";
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

        [HttpPost, Route("AddFolder"), AjaxValidateAntiForgeryToken]
        public JsonNetResult AddFolder(AddSiteNavModel model)
        {
            if (!Company.CurrentResourceIsAdmin)
                return jsonNetException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

            var success = true;
            var message = "";
            try
            {
                if (string.IsNullOrWhiteSpace(model.Folder.Name))
                    throw new Exception("Folder name cannot be empty.");

                if (!string.IsNullOrEmpty(model.Folder.IconPayload))
                {
                    var imageMatch = MimeTypeExtensionsMap.RegEx.Match(model.Folder.IconPayload);

                    var imageMime = imageMatch.Groups["mime"].Value;
                    var imageData = imageMatch.Groups["data"].Value;
                    var imageExtension = MimeTypeExtensionsMap.GetExtension(imageMime);
                    var imageByteArray = Convert.FromBase64String(imageData);
                    var imageGuid = Guid.NewGuid();

                    using (var imageStream = new MemoryStream(imageByteArray))
                    {
                        var imageFileName = string.Format("{0}.menuicon.{1}{2}", Company.CurrentCompanyID, imageGuid, imageExtension);
                        Storage.CreateFile(constants.COMPANY_RESOURCES_FOLDER, imageFileName, imageStream);

                        model.Folder.ImageIconUrl = $"{imageFileName}";

                    }
                }

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
                message = "Folder added successfully";
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

        [HttpPut, Route("MoveUp"), NonNullableParameters]
        public JsonNetResult MoveUp(int id)
        {
            if (!Company.CurrentResourceIsAdmin)
                return jsonNetException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

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

        [HttpPut, Route("MoveDown"), NonNullableParameters]
        public JsonNetResult MoveDown(int id)
        {
            if (!Company.CurrentResourceIsAdmin)
                return jsonNetException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

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

        [HttpPut, Route("EditFolder")]
        public JsonNetResult EditFolder(SiteNav folder)
        {
            if (!Company.CurrentResourceIsAdmin)
                return jsonNetException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

            var success = true;
            var message = "";
            try
            {
                if (folder == null)
                    throw new Exception("Invalid folder.");
                var siteNav = Company.GetById<SiteNav>(folder.ID);
                if (siteNav == null)
                    throw new Exception($"Folder Id ${folder.ID} not found.");
                string originalImage = siteNav.ImageIconUrl;

                if (!string.IsNullOrEmpty(originalImage))
                {
                    try
                    {
                        Storage.DeleteFile(constants.COMPANY_RESOURCES_FOLDER, originalImage);
                    }catch(Exception e)
                    {

                    }
                }

                if (!string.IsNullOrEmpty(folder.IconPayload))
                {
                    var imageMatch = MimeTypeExtensionsMap.RegEx.Match(folder.IconPayload);

                    var imageMime = imageMatch.Groups["mime"].Value;
                    var imageData = imageMatch.Groups["data"].Value;
                    var imageExtension = MimeTypeExtensionsMap.GetExtension(imageMime);
                    var imageByteArray = Convert.FromBase64String(imageData);
                    var imageGuid = Guid.NewGuid();

                    using (var imageStream = new MemoryStream(imageByteArray))
                    {
                        var imageFileName = string.Format("{0}.menuicon.{1}{2}", Company.CurrentCompanyID, imageGuid, imageExtension);
                        Storage.CreateFile(constants.COMPANY_RESOURCES_FOLDER, imageFileName, imageStream);

                        folder.ImageIconUrl = $"{imageFileName}";

                    }
                }



                siteNav.Name = folder.Name;
                siteNav.Icon = folder.Icon;
                siteNav.Title = folder.Title ?? folder.Name;
                siteNav.ImageIconUrl = folder.ImageIconUrl;
                Company.SaveChanges();
                SetSiteNavPermissions(folder);
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

        [HttpPut, Route("SiteNavFolderMove"), NonNullableParameters]
        public JsonNetResult SiteNavFolderMove(int targetFolderId,int adjacentFolderId)
        {
            if (!Company.CurrentResourceIsAdmin)
                return jsonNetException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

            var success = true;
            var message = "";
            try
            {
                var siteNav = Company.GetById<SiteNav>(targetFolderId);
                var siteNavBelow = Company.GetById<SiteNav>(adjacentFolderId);

                if (siteNav == null)
                    throw new Exception($"Folder Id ${targetFolderId} not found.");
                if (siteNavBelow == null)
                    throw new Exception($"Folder Id ${adjacentFolderId} not found.");

                int? tmpSortOrder = siteNav.SortOrder;
                siteNav.SortOrder = siteNavBelow.SortOrder;
                siteNavBelow.SortOrder = tmpSortOrder;
                Company.SaveChanges();
                message = $"Folder ${siteNav.Name} moved successfully.";
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

        [HttpGet, Route("permissions/get/list/{id:int}")]
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


        [HttpGet, Route("permissions/get/list")]
        public JsonNetResult GetSiteNavPermissionList(int id , int pagenum, int pagesize, string sortDataField, string sortOrder, string gbfilter)
        {
            var dbArgs = new Dapper.DynamicParameters();
            var hideUsersSql = "";

            if (HideData3SixtyUsers())
            {
                hideUsersSql = " and (r.Email not like '%@data3sixty.com' and r.Email not like '%@infogix.com')";
            }

            var querySql = @"
                    select Text,  [Value] + '|' + [Type] + ' :: ' + Text as [Value],[Type] from
						(
							select  g.Name as Text, 'Group|' + cast(g.ID as varchar) as [Value],'Group' as [Type] from [Group] g
							where not exists (select 1 from SiteNavPermission where object='Group' and siteNavId =@id and objectId=g.id) 
							union all
							select  r.LastName + ' ' + r.FirstName as label, 'Resource|' + cast(r.ResourceID as varchar) as [Value],'User' as 'Type' from reporting.Global_Resource r
							where r.[State] = 1 and  not exists (select 1 from SiteNavPermission where object='Resource' and objectId=r.ResourceID and siteNavId =@id) "
                            + hideUsersSql +
                        ") as Sub";
                   

            if (!string.IsNullOrEmpty(gbfilter))
            {
                querySql = string.Format(@"select * from ({0}) gb where  [Text] like '%' +   @gbfilter + '%'  or [Type] like   @gbfilter + '%'", querySql);
                dbArgs.Add("gbfilter", gbfilter);
            }

            var countSql = string.Format(@"select count(1) from ({0}) A", querySql);
            var sql = string.Format(@"select * from ({0}) A", querySql);

          
           dbArgs.Add("id", id);

            countSql = applyFilteringSuffixBind(countSql, Request, dbArgs);
            int totalCount = Company.Query<int>(countSql, dbArgs).First();


            sql = applySortSuffix(sql, sortDataField, sortOrder, "Text", "asc");
            sql = applyPagingSuffix(sql, pagenum, pagesize);

            var query = Company.Query<dynamic>(sql, dbArgs);

           
            return new JsonNetResult
            {
                Data = new { total= totalCount, results = query },
                Formatting = Newtonsoft.Json.Formatting.None
            };
        }

        [HttpGet, Route("permissions/get/{id:int}")]
        public JsonNetResult GetSiteNavPermissions(int id)
        {
            var perms = Company.Query<SiteNavPermission>(QueryConstants.SiteNavPermissions, new { id }).ToList();

            return new JsonNetResult
            {
                Data = perms,
                Formatting = Newtonsoft.Json.Formatting.None
            };
        }

        [HttpPost, Route("permissions/set"), AjaxValidateAntiForgeryToken]
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
            catch (Exception ex)
            {
                return jsonNetException(ex);
            }

            return new JsonNetResult
            {
                Data = nav.ID,
                Formatting = Newtonsoft.Json.Formatting.None
            };
        }

        [HttpPost, Route("permissions/add"), AjaxValidateAntiForgeryToken]
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
            }
            catch (Exception ex)
            {
                return jsonNetException(ex);
            }

            return new JsonNetResult
            {
                Data = perm,
                Formatting = Newtonsoft.Json.Formatting.None
            };

        }

        [HttpDelete, Route("permissions/remove")]
        public JsonNetResult RemoveSiteNavPermission(SiteNavPermission perm)
        {
            if (!Company.CurrentResourceIsAdmin)
                return jsonNetException(new Exception("You do not have permission to do this."));

            perm = Company.SiteNavPermissions.Where(p => p.SiteNavID == perm.SiteNavID && p.Object == perm.Object && p.ObjectID == perm.ObjectID).FirstOrDefault();

            if (perm != null)
            {
                try
                {
                    Company.SiteNavPermissions.Remove(perm);
                    Company.SaveChanges();
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
        [HttpDelete, Route("DeleteMyFavorites")]
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

        [HttpPut, Route("ToggleFavorite")]
        public JsonNetResult ToggleFavorite(Favorite favorite)
        {
            var success = true;
            var message = "";

            try
            {
                favorite.ResourceID = Company.CurrentResourceID;
                favorite.SortOrder = Company.Favorites.Count(f => f.ResourceID == favorite.ResourceID) + 1;
                favorite.IsOverride = false;

                //only 1 home page allowed at once, remove old one(s)
                if (favorite.IsHomePage)
                {
                    var favorites = Company.Filter<Favorite>(f => f.ResourceID == Company.CurrentResourceID && f.IsHomePage).ToList();
                    Company.Favorites.RemoveRange(favorites);
                    Company.SaveChanges();
                }

                Favorite existing = null;

                if (!string.IsNullOrEmpty(favorite.Object) && favorite.ObjectID > 0)
                {
                    existing = Company.Favorites.FirstOrDefault(f => f.ResourceID == favorite.ResourceID && f.Object == favorite.Object && f.ObjectID == favorite.ObjectID);
                }
                else
                {
                    existing = Company.Favorites.FirstOrDefault(f => f.ResourceID == favorite.ResourceID && f.Name == favorite.Name && f.Route == favorite.Route);
                }

                if (existing == null)
                {
                    Company.Add(favorite);

                    message = favorite.IsHomePage ? "Home page added" : "Favorite Added.";
                }
                else
                {
                    if (existing.IsHomePage != favorite.IsHomePage)
                    {
                        existing.IsHomePage = favorite.IsHomePage;
                        Company.Update(existing);
                        message = favorite.IsHomePage ? "Home page added" : "Favorite Added.";
                    }
                    else
                    {
                        Company.Delete(existing);
                        message = favorite.IsHomePage ? "Home page removed" : "Favorite Removed.";
                    }

                }
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

        [HttpPut, Route("MoveFavorite"), NonNullableParameters]
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

        [HttpGet, ValidateContracts(Ignore = true), Route("GetFavorites")]
        public JsonNetResult GetFavorites(bool adminOnly = false)
        {
            var sql = @"
select	coalesce(AName.DisplayValue, TA.[Name]) as [Name],
		f.Route as [Route],
		f.[Object],
		f.[ObjectId],
		f.SortOrder,
		f.Id,
		f.ResourceId,
		f.IsHomePage
from	Favorite f
		left join Asset a on a.[Object] = f.[Object] and a.[ObjectID] = f.[ObjectID]
		left join AssetType ta on ta.[Object] = f.[Object] and ta.[ObjectID] = f.[ObjectID]
        outer apply [dbo].[GetAssetDisplayValueById](A.ID) AName
where	f.ObjectID > 0 and f.ResourceID = @resId
union
select		f.Name as Name,	
			f.Route as [Route],
			f.[Object],
			f.[ObjectId],
			f.SortOrder,
			f.Id,
			f.ResourceId,
			f.IsHomePage
from		Favorite f	
where		f.ObjectID is null 
			and f.ResourceID = @resId
order by	f.SortOrder";

            var favorites = Company.Query<Favorite>(sql, new { resId = Company.CurrentResourceID }).ToList();

            return new JsonNetResult
            {
                Data = favorites,
                Formatting = Newtonsoft.Json.Formatting.None
            };

        }

        [HttpGet, Route("GetItemCount/{url}")]
        public async Task<JsonNetResult> GetItemCount(string url)
        {
            int count = 0;
            string type = ""; 
            int id = 0;
            var urlElements = url.Split('-');
            type = urlElements[0];
            if (type.Equals("quality", StringComparison.OrdinalIgnoreCase))
            {
                id = int.TryParse(urlElements[2], out id) ? id : 0;
                type = urlElements[1];
            }
            else if (type.Equals("model", StringComparison.OrdinalIgnoreCase))
            {
                type = "Taxonomy";
                id = int.TryParse(urlElements[1], out id) ? id : 0;
            }
            else
                id = int.TryParse(urlElements[1], out id) ? id : 0;
            type = FormatType(type);

            count = (await Company.QueryAsync<int>(@"
                        select	count(1)
                        from    [Asset] A
						inner join AssetType ATT on (a.AssetTypeID = Att.id)
			            where  ATT.[Object] = @type and  ATT.ObjectID = @id and A.[State] = 1
                        ", new { type, id })).FirstOrDefault();

            return new JsonNetResult
            {
                Data = count,
                Formatting = Newtonsoft.Json.Formatting.None
            };
        }
        public static string FormatType(string s)
        {
            // Check for empty string.  
            if (string.IsNullOrEmpty(s))
            {
                return string.Empty;
            }
            // Return char and concat substring.  
            return string.Format("{0}{1}{2}",char.ToUpper(s[0]), s.Substring(1), "Type");
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
