using System.Web.Mvc;
using System.Linq;
using d360.core;
using System.Collections.Generic;
using d360.web.Models;
using d360.model;
using d360.core.entities;
using System.Xml.Linq;
using d360.core.enums;
using System;
using d360.web.Models.Attributes;
using d360.extensions;
using d360.web.Filters;
using Resources;
using System.Net;
using System.IO;
using System.Threading.Tasks;
using d360.core.resources;
using d360.model.DataAccessLayer;

namespace d360.web.Controllers
{
    [Authorize, RoutePrefix("navigation")]
    public class NavigationController : BaseController
    {
        IStorageProvider Storage;
        readonly ICoreComponentSet Set;

        public NavigationController(ICoreComponentSet set, IStorageProvider storage)
            : base(set)
        {
            Set = set;
            Storage = storage;
        }

        internal List<NavigationItem> parseXmlNavigationDocument(XElement xml, bool showChildren = true)
        {
            var items = new List<NavigationItem>();

            if (xml != null)
            {
                foreach (var el in xml.Elements("nav"))
                {
                    var item = new NavigationItem { Name = (el.Element("name") ?? el.Element("Name")).Value, Url = el.Element("url").Value, ShowChildren = showChildren };
                    if (el.Element("items") != null)
                    {
                        item.Items = parseXmlNavigationDocument(el.Element("items"), showChildren);
                    }
                    items.Add(item);
                }
            }

            return items;
        }

        internal List<TopNavigationItem> GenerateSiteMenu(List<TopNavigationItem> nodes, bool hasTechAssets, bool showChildren)
        {
            if (!hasTechAssets)
            {
                nodes = nodes.Where(x => x.MenuID != "#Technical").ToList();
            }

            if (nodes != null)
            {
                List<string> toggleVisibilityURLs = new List<string> {
                    "artifact/","policy/","quality/rule","model/"};

                nodes.ForEach(n =>
                {
                    n.NavigationItems = (string.IsNullOrEmpty(n.Items)) ?
                        new List<NavigationItem>() :
                        parseXmlNavigationDocument(XElement.Parse(string.Format("<nav>{0}</nav>", n.Items)), showChildren);


                    var urls = n.NavigationItems.Select(x => x.Url).ToList();
                    var counts = 0;
                    foreach (var urlPart in toggleVisibilityURLs)
                    {
                        var matches = urls.Where(x => !string.IsNullOrEmpty(x) && x.ToLower(System.Globalization.CultureInfo.InvariantCulture).Contains(urlPart.ToLower(System.Globalization.CultureInfo.InvariantCulture)));
                        counts += matches.Count();
                    }

                    if (urls.Count == counts)
                    {
                        n.ShowVisibilityToggle = true;
                    }

                });
            }

            return nodes;
        }


        [ValidateContracts(Ignore = true), Route("sitemenu")]
        public JsonNetResult SiteMenu()
        {
            var techAssets = Company.Query<int>($"select count(*) from AssetType where Class = {(int)AssetTypeClass.TechnicalAsset}").First();
            var showChildren = SettingsRepository.GetSettingValue<bool>(Setting.ShowNavigationChildren);

            return new JsonNetResult
            {
                Data = new
                {
                    MenuItems = GenerateSiteMenu(Company.Query<TopNavigationItem>("GetSiteNavigation @ResourceID", new { ResourceID = Company.CurrentResourceID }).ToList(), techAssets > 0, showChildren),
                    IsAdmin = Company.CurrentResourceIsAdmin
                },
                Formatting = Newtonsoft.Json.Formatting.None
            };
        }



        [Route("GetAvailableSiteNavigation")]
        public JsonNetResult GetAvailableSiteNavigation()
        {
            var nav = Company.Query<dynamic>($@"
                with s as
                (
	                select  cast(
	                            case 
                                    when [Object] = 'ArtifactType' and [Class] = 1 then '{CommonNames.AssetTypeClass_Business.CleanForSql()} :: ' + Name
	                                when [Object] = 'ArtifactType' and [Class] = 8 then '{CommonNames.AssetTypeClass_Technical.CleanForSql()} :: ' + Name
	                                else Name
	                            end	
	                            as varchar(500)
                                ) as Title, * 
                    from    SiteNavAvailable 
                    where   ParentID is null
	                union all
	                select  cast((s.Title + ' :: ' + v.name) as varchar(500)) as Title, 
                            v.* 
                    from    SiteNavAvailable v 
                            join s on s.objectid = v.parentid and v.object = s.object
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
            {
                return jsonNetException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);
            }

            var success = true;
            var message = "";
            try
            {
                if (string.IsNullOrWhiteSpace(item.Name))
                {
                    throw new ArgumentNullException(FormControllerApiMessage.FolderNameNotEmpty);
                }

                var deleteExisting = Company.Query<SiteNav>(@"with s as
                (
	                select * from sitenavflat where objectid = @ObjectID and object = @Object
	                union all
	                select v.* from sitenavflat v join s on s.objectid = v.parentid and s.object = v.object
                )
                select n.* from s
                join sitenav n on n.Object = s.Object and n.ObjectID = s.ObjectID", new { item.ObjectID, item.Object }).ToList();

                deleteExisting.ForEach(d =>
                {
                    var record = Company.GetById<SiteNav>(d.ID);
                    Company.Delete(record);
                });
                item.SortOrder = Company.SiteNav.Max(i => i.SortOrder) + 1;
                Company.Add(item);
                Company.SaveChanges();
                message = FormControllerApiMessage.FolderAdded;
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
            {
                return jsonNetException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);
            }

            var success = true;
            var message = "";
            try
            {
                var fi = Company.GetById<SiteNav>(id);
                if (fi == null)
                {
                    throw new ArgumentNullException(string.Format(FormControllerApiMessage.FolderIdNotFound, id.ToString()));
                }
                Company.Delete(fi);
                Company.SaveChanges();
                message = FormControllerApiMessage.FolderItemRemoved;
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
        public async Task<JsonNetResult> RemoveFolder(int id)
        {
            if (!Company.CurrentResourceIsAdmin)
                return jsonNetException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

            var success = true;
            var message = "";

            try
            {
                var folder = Company.GetById<SiteNav>(id);
                if (folder == null)
                {
                    throw new ArgumentNullException(string.Format(FormControllerApiMessage.FolderIdNotFound, id.ToString()));
                }
                string originalImage = folder.ImageIconUrl;
                if (!string.IsNullOrEmpty(originalImage))
                {
                    await Storage.DeleteFile(constants.COMPANY_RESOURCES_FOLDER, originalImage);
                }
                //clear out permissions
                folder.Permissions = new List<SiteNavPermission>();
                SetSiteNavPermissions(folder);

                var subNavs = Company.SiteNav.Where(s => s.ParentID == folder.ID);

                Company.SiteNav.RemoveRange(subNavs);
                Company.Delete(folder);
                Company.SaveChanges();
                message = FormControllerApiMessage.FolderRemoved;
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
        public async Task<JsonNetResult> AddFolder(AddSiteNavModel model)
        {
            if (!Company.CurrentResourceIsAdmin)
                return jsonNetException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

            var success = true;
            var message = "";
            try
            {
                if (string.IsNullOrWhiteSpace(model.Folder.Name))
                {
                    throw new ArgumentNullException(FormControllerApiMessage.FolderNameNotEmpty);
                }

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
                        await Storage.CreateFile(constants.COMPANY_RESOURCES_FOLDER, imageFileName, imageStream);

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
                message = FormControllerApiMessage.FolderAdded;
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
                {
                    throw new ArgumentNullException(string.Format(FormControllerApiMessage.FolderIdNotFound, id.ToString()));
                }
                if (siteNavAbove == null)
                {
                    throw new ArgumentNullException(FormControllerApiMessage.FolderAlreadySortedToTop);
                }

                siteNavAbove.SortOrder++;
                siteNav.SortOrder--;
                Company.SaveChanges();
                message = string.Format(FormControllerApiMessage.FolderMovedUp, siteNav.Name);
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
                {
                    throw new ArgumentNullException(string.Format(FormControllerApiMessage.FolderIdNotFound, id.ToString()));
                }

                if (siteNavBelow == null)
                {
                    throw new ArgumentNullException(FormControllerApiMessage.FolderAlreadySortedToBottom);
                }

                siteNavBelow.SortOrder--;
                siteNav.SortOrder++;
                Company.SaveChanges();
                message = string.Format(FormControllerApiMessage.FolderMovedDown, siteNav.Name);
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
        public async Task<JsonNetResult> EditFolder(SiteNav folder)
        {
            if (!Company.CurrentResourceIsAdmin)
                return jsonNetException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

            var success = true;
            var message = "";
            try
            {
                if (folder == null)
                {
                    throw new ArgumentNullException(FormControllerApiMessage.InvalidFolder);
                }
                var siteNav = Company.GetById<SiteNav>(folder.ID);
                if (siteNav == null)
                {
                    throw new ArgumentNullException(string.Format(FormControllerApiMessage.FolderIdNotFound, folder.ID.ToString()));
                }
                string originalImage = siteNav.ImageIconUrl;

                if (!string.IsNullOrEmpty(originalImage) && string.IsNullOrEmpty(folder.ImageIconUrl))
                {
                    try
                    {
                        await Storage.DeleteFile(constants.COMPANY_RESOURCES_FOLDER, originalImage);
                    }
                    catch { }
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
                        await Storage.CreateFile(constants.COMPANY_RESOURCES_FOLDER, imageFileName, imageStream);

                        folder.ImageIconUrl = $"{imageFileName}";

                    }
                }

                siteNav.Name = folder.Name;
                siteNav.Icon = folder.Icon;
                siteNav.Title = folder.Title ?? folder.Name;
                siteNav.ImageIconUrl = folder.ImageIconUrl;
                Company.SaveChanges();
                SetSiteNavPermissions(folder);
                message = FormControllerApiMessage.FolderUpdated;

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
        public JsonNetResult SiteNavFolderMove(int targetFolderId, int adjacentFolderId)
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
                {
                    throw new ArgumentNullException(string.Format(FormControllerApiMessage.FolderIdNotFound, targetFolderId.ToString()));
                }
                if (siteNavBelow == null)
                {
                    throw new ArgumentNullException(string.Format(FormControllerApiMessage.FolderIdNotFound, adjacentFolderId.ToString()));
                }

                int? tmpSortOrder = siteNav.SortOrder;
                siteNav.SortOrder = siteNavBelow.SortOrder;
                siteNavBelow.SortOrder = tmpSortOrder;
                Company.SaveChanges();
                message = string.Format(FormControllerApiMessage.FolderMoved, siteNav.Name);
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
        public JsonNetResult GetSiteNavPermissionList(int id, int pagenum, int pagesize, string sortDataField, string sortOrder, string gbfilter)
        {
            var dbArgs = new Dapper.DynamicParameters();
            var hideUsersSql = "";

            if (HideData3SixtyUsers())
            {
                hideUsersSql = " and (r.Email not like '%@data3sixty.com' and r.Email not like '%@infogix.com' and r.Email not like '%@precisely.com')";
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
                Data = new { total = totalCount, results = query },
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
            {
                return jsonNetException(new Exception(FormControllerApiMessage.NoPermissionToDoThis));
            }

            if (nav == null || nav.ID < 1)
            {
                return jsonNetException(new Exception(FormControllerApiMessage.ModelPassedToMethodInvalid));
            }

            var existing = Company.SiteNavPermissions.Where(p => p.SiteNavID == nav.ID).ToList();

            if (nav.Permissions == null)
            {
                nav.Permissions = new List<SiteNavPermission>();
            }

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
            {
                return jsonNetException(new Exception(FormControllerApiMessage.NoPermissionToDoThis));
            }

            if (nav == null)
            {
                return jsonNetException(new Exception(FormControllerApiMessage.SiteNavNotFound));
            }

            if (string.IsNullOrEmpty(perm.Object) || perm.ObjectID == 0)
            {
                return jsonNetException(new Exception(FormControllerApiMessage.invalidObjectPassed));
            }

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
                return jsonNetException(new Exception(FormControllerApiMessage.NoPermissionToDoThis));

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
            {
                return jsonNetException(new Exception(FormControllerApiMessage.CoultNotFindPermission));
            }

            return new JsonNetResult
            {
                Data = perm.SiteNavID,
                Formatting = Newtonsoft.Json.Formatting.None
            };
        }

        #endregion

        #region Favorites

        [HttpPut, Route("MoveFavorite"), NonNullableParameters]
        public JsonNetResult MoveFavorite(string route, bool moveUp = false, bool admin = false)
        {
            var success = true;
            var message = "";
            try
            {
                if (admin && !Company.CurrentResourceIsAdmin)
                {
                    throw new ArgumentNullException(FormControllerApiMessage.UserDoesnotAdmin);
                }

                var resid = admin ? 0 : Company.CurrentResourceID;

                var favorite = Company.Favorites.Where(f => f.ResourceID == resid && f.Route == route).First();

                if (favorite == null)
                {
                    throw new ArgumentNullException(FormControllerApiMessage.NoFavoriteRoute);
                }
                if (moveUp)
                {
                    var above = Company.Favorites.Where(f => f.SortOrder == (favorite.SortOrder - 1) && f.ResourceID == favorite.ResourceID).SingleOrDefault();
                    if (above == null)
                    {
                        throw new ArgumentNullException(FormControllerApiMessage.NoFavoriteAbove);
                    }
                    favorite.SortOrder--;
                    above.SortOrder++;
                }
                else
                {
                    var below = Company.Favorites.Where(f => f.SortOrder == (favorite.SortOrder + 1) && f.ResourceID == favorite.ResourceID).SingleOrDefault();
                    if (below == null)
                    {
                        throw new ArgumentNullException(FormControllerApiMessage.NoFavoriteBelow);
                    }
                    favorite.SortOrder++;
                    below.SortOrder--;
                }

                Company.SaveChanges();
                message = FormControllerApiMessage.FavoriteMoved;
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


        [HttpGet, Route("GetCounts")]
        public async Task<JsonNetResult> GetCounts()
        {
            var ItemCounts = await Company.QueryAsync<dynamic>("GetSiteNavigationCounts @ResourceID",
                new { ResourceID = Company.CurrentResourceID });

            return new JsonNetResult
            {
                Data = ItemCounts,
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
            return string.Format("{0}{1}{2}", char.ToUpper(s[0]), s.Substring(1), "Type");
        }
        #endregion

        [HttpPost, Route("secondaryNavigationSettings")]
        public JsonNetResult GetSecondaryNavigationSettings(SecondaryNavigationPostModel model)
        {
            bool execProcedure = true;
            SecondaryNavigationResponseModel responseModel = new SecondaryNavigationResponseModel() { Items = new SecondaryNavItems() };
            //Static nav
            if (model.AssetUid == null)
            {
                if (model.ObjectType != null && model.ObjectId != null)
                {
                    var assetType = Company.AssetTypes.FirstOrDefault(x => x.Object == model.ObjectType && x.ObjectID == model.ObjectId);
                    if (assetType != null)
                    {
                        model.Class = assetType.Class;
                        responseModel.Uid = assetType.uid;
                    }
                }

                if (model.ObjectType == SystemObjects.ArtifactType.ToString())
                {
                    execProcedure = false;
                    responseModel.Object = responseModel.ObjectType = SystemObjects.ArtifactType.ToString();
                    responseModel.ObjectID = model.ObjectId ?? 0;

                    responseModel.Items.HasAudit = true;

                    if (model.Class == AssetTypeClass.TechnicalAsset)
                    {
                        responseModel.DisplayValue = "Technical Assets";
                        responseModel.MainTabTitle = "Technical Asset Types";
                    }
                    if (model.Class == AssetTypeClass.BusinessAsset)
                    {
                        responseModel.DisplayValue = "Business Assets";
                        responseModel.MainTabTitle = "Business Asset Types";
                    }
                }

                if (model.ObjectType == SystemObjects.TaskType.ToString())
                {
                    execProcedure = false;
                    responseModel.Object = responseModel.ObjectType = SystemObjects.TaskType.ToString();
                    responseModel.ObjectID = model.ObjectId ?? 0;

                    if ((responseModel.Uid == null || responseModel.Uid == Guid.Empty) && responseModel.ObjectID == 0)
                    {
                        var assetType = Company.AssetTypes.Where(x => x.Object == model.ObjectType).OrderBy(x => x.Name).FirstOrDefault();
                        if (assetType != null)
                        {
                            responseModel.Uid = assetType.uid;
                        }
                    }

                    responseModel.Items.HasAudit = true;
                    responseModel.DisplayValue = "Diagram Assets";
                    responseModel.MainTabTitle = "Diagram Asset Types";
                    responseModel.Object = SystemObjects.TaskType.ToString();

                    var govRoleUid = SettingsRepository.GetSettingValue<Guid>(Setting.GovernanceRoleReferenceListUid);

                    responseModel.Items.HasGovernanceRoleUidSet = govRoleUid != null && govRoleUid != Guid.Empty;
                }

                if (model.ObjectType == SystemObjects.IntersectType.ToString())
                {
                    execProcedure = false;
                    responseModel.Object = responseModel.ObjectType = SystemObjects.IntersectType.ToString();
                    responseModel.ObjectID = model.ObjectId ?? 0;
                    responseModel.DisplayValue = "Relationships";
                    responseModel.MainTabTitle = "Relationship Types";
                    responseModel.Items.HasAudit = true;
                    responseModel.Items.HasField = true;

                }

                if (model.ObjectType == SystemObjects.IssueType.ToString())
                {
                    execProcedure = false;
                    responseModel.Object = responseModel.ObjectType = SystemObjects.IssueType.ToString();
                    responseModel.ObjectID = model.ObjectId ?? 0;
                    responseModel.DisplayValue = "Workflow Actions";
                    responseModel.MainTabTitle = "Action Types";
                    responseModel.Items.HasAudit = true;
                }

                if (model.ObjectType == SystemObjects.ResponsibilityType.ToString())
                {
                    execProcedure = false;
                    responseModel.Object = responseModel.ObjectType = SystemObjects.ResponsibilityType.ToString();
                    responseModel.ObjectID = model.ObjectId ?? 0;
                    responseModel.DisplayValue = "Responsibilities";
                    responseModel.MainTabTitle = "Responsibility Types";
                    responseModel.Items.HasAudit = true;
                }

                if (model.ObjectType == SystemObjects.Report.ToString())
                {
                    execProcedure = false;
                    responseModel.Object = responseModel.ObjectType = SystemObjects.Report.ToString();
                    responseModel.ObjectID = model.ObjectId ?? 0;
                    responseModel.DisplayValue = "Dashboards";
                    responseModel.MainTabTitle = "Dashboards";
                    responseModel.Items.HasAudit = true;
                }

                if (model.ObjectType == SystemObjects.TaxonomyType.ToString())
                {
                    execProcedure = false;
                    responseModel.Object = responseModel.ObjectType = SystemObjects.TaxonomyType.ToString();
                    responseModel.ObjectID = model.ObjectId ?? 0;
                    responseModel.DisplayValue = "Models";
                    responseModel.MainTabTitle = "Model Types";
                    responseModel.Items.HasAudit = true;
                }

                if (model.ObjectType == SystemObjects.PolicyType.ToString())
                {
                    execProcedure = false;
                    responseModel.Object = responseModel.ObjectType = SystemObjects.PolicyType.ToString();
                    responseModel.ObjectID = model.ObjectId ?? 0;
                    responseModel.DisplayValue = "Policies";
                    responseModel.MainTabTitle = "Policy Types";
                    responseModel.Items.HasAudit = true;
                }

                if (model.ObjectType == SystemObjects.Tag.ToString())
                {
                    execProcedure = false;
                    responseModel.Object = responseModel.ObjectType = SystemObjects.Tag.ToString();
                    responseModel.ObjectID = model.ObjectId ?? 0;
                    responseModel.DisplayValue = "Tags";
                    responseModel.MainTabTitle = "Tags";
                    responseModel.Items.HasAudit = true;
                }
                if (model.ObjectType == SystemObjects.RuleType.ToString())
                {
                    execProcedure = false;
                    responseModel.Object = responseModel.ObjectType = SystemObjects.RuleType.ToString();
                    responseModel.ObjectID = model.ObjectId ?? 0;
                    responseModel.DisplayValue = "Rules";
                    responseModel.MainTabTitle = "Rules";
                    responseModel.Items.HasAudit = true;
                }
                if (model.ObjectType == SystemObjects.ConnectorLabel.ToString())
                {
                    execProcedure = false;
                    responseModel.Object = responseModel.ObjectType = SystemObjects.TaskType.ToString();
                    responseModel.ObjectID = model.ObjectId ?? 0;
                    responseModel.DisplayValue = "Diagram Assets";
                    responseModel.MainTabTitle = "Diagram Asset Types";
                    responseModel.Items.HasAudit = true;
                    var govRoleUid = SettingsRepository.GetSettingValue<Guid>(Setting.GovernanceRoleReferenceListUid);
                    if ((responseModel.Uid == null || responseModel.Uid == Guid.Empty) && responseModel.ObjectID == 0)
                    {
                        var assetType = Company.AssetTypes.Where(x => x.Object == SystemObjects.TaskType.ToString()).OrderBy(x => x.Name).FirstOrDefault();
                        if (assetType != null)
                        {
                            responseModel.Uid = assetType.uid;
                        }
                    }
                    responseModel.Items.HasGovernanceRoleUidSet = govRoleUid != null && govRoleUid != Guid.Empty;
                }

                if (model.ObjectType == SystemObjects.ResourceType.ToString())
                {
                    execProcedure = false;
                    responseModel.Object = responseModel.ObjectType = SystemObjects.ResourceType.ToString();
                    responseModel.ObjectID = model.ObjectId ?? 0;
                    responseModel.DisplayValue = "Users";
                    responseModel.MainTabTitle = "Users";
                    responseModel.Items.HasAudit = true;
                }

                if (model.ObjectType == SystemObjects.GroupType.ToString())
                {
                    execProcedure = false;
                    responseModel.Object = responseModel.ObjectType = SystemObjects.GroupType.ToString();
                    responseModel.ObjectID = model.ObjectId ?? 0;
                    responseModel.DisplayValue = "Groups";
                    responseModel.MainTabTitle = "Groups";
                    responseModel.Items.HasAudit = true;
                    responseModel.Items.HasField = true;
                }

                if (model.ObjectType == SystemObjects.MetricAllocation.ToString())
                {
                    execProcedure = false;
                    responseModel.Object = responseModel.ObjectType = SystemObjects.MetricAllocation.ToString();
                    responseModel.ObjectID = model.ObjectId ?? 0;
                    responseModel.DisplayValue = "Scoring Definitions";
                    responseModel.MainTabTitle = "Scoring Definitions";
                    responseModel.Items.HasAudit = true;
                }

                if (model.ObjectType == SystemObjects.Predicate.ToString())
                {
                    execProcedure = false;
                    responseModel.Object = responseModel.ObjectType = SystemObjects.Predicate.ToString();
                    responseModel.ObjectID = model.ObjectId ?? 0;
                    responseModel.Uid = Guid.Parse("00000001-0000-0000-0000-b00000000012");
                    responseModel.DisplayValue = "Predicates";
                    responseModel.MainTabTitle = "Predicates";
                    responseModel.Items.HasAudit = true;
                }

                if (model.ObjectType == SystemObjects.Resource.ToString())
                {
                    var assetDetail = Company.AssetDetails.FirstOrDefault(x => x.Object == model.ObjectType && x.ObjectID == model.ObjectId);
                    FillResponseModelForResource(assetDetail);
                }
            }

            if (model.AssetUid != null && model.ObjectType == SystemObjects.Tag.ToString())
            {
                execProcedure = false;
                var tag = Company.Tags.FirstOrDefault(x => x.uid == model.AssetUid);
                responseModel.Object = responseModel.ObjectType = SystemObjects.Tag.ToString();
                responseModel.ObjectID = model.ObjectId ?? 0;
                responseModel.DisplayValue = tag.Value;
                responseModel.MainTabTitle = "Tagged Assets";
                responseModel.Items.HasAudit = true;
                responseModel.Uid = tag.uid;
            }

            if (model.AssetUid != null)
            {
                var asset = Company.Assets.FirstOrDefault(x => x.uid == model.AssetUid);
                if (asset != null && (asset.Object == "Group"))
                {
                    execProcedure = false;
                    responseModel.Object = asset.Object;
                    responseModel.ObjectID = asset.ObjectID;
                    responseModel.DisplayValue = "Groups";
                    responseModel.MainTabTitle = "Groups";
                    responseModel.Items.HasAudit = true;
                    responseModel.Uid = asset.uid;
                }

                if (asset != null && (asset.Object == "Resource"))
                {
                    var assetDetail = Company.AssetDetails.FirstOrDefault(x => x.uid == model.AssetUid);
                    FillResponseModelForResource(assetDetail);
                }                
            }

            if (model.ObjectType == SystemObjects.SemanticType.ToString())
            {
                execProcedure = false;
                responseModel.Object = responseModel.ObjectType = SystemObjects.SemanticType.ToString();
                responseModel.ObjectID = model.ObjectId ?? 0;
                responseModel.DisplayValue = "Semantic Types";
                responseModel.MainTabTitle = "Semantic Types";
                responseModel.Items.HasAudit = true;
                if (model.AssetUid.HasValue)
                {
                    var semantic = Company.Semantics.FirstOrDefault(x => x.Uid == model.AssetUid);
                    responseModel.Uid = semantic.Uid;
                }
            }

            if (execProcedure)
            {
                if (model.ObjectId != null && model.ObjectType != null)
                {
                    model.AssetUid = Company.Assets.FirstOrDefault(x => x.Object == model.ObjectType && x.ObjectID == model.ObjectId)?.uid;

                    if (model.AssetUid == null)
                    {
                        model.AssetTypeUid = Company.AssetTypes.FirstOrDefault(x => x.Object == model.ObjectType && x.ObjectID == model.ObjectId)?.uid;
                    }
                }

                if (model.AssetId != null)
                {
                    model.AssetUid = Company.Assets.FirstOrDefault(x => x.ID == model.AssetId)?.uid;
                }

                var response = Company.Query<string>("exec [dbo].[SecondaryNavSettings] @uid, @assetTypeUid , @resourceId, @isAdmin", new { assetTypeUid = model.AssetTypeUid, uid = model.AssetUid, resourceId = Company.CurrentResourceID, isAdmin = Company.CurrentResourceIsAdmin }).ToList();
                responseModel = Newtonsoft.Json.JsonConvert.DeserializeObject<SecondaryNavigationResponseModel>(string.Join("", response));

                if (responseModel != null)
                {
                    if (responseModel.Object == "Artifact")
                    {
                        responseModel.Artifact = Company.GetPageInformation(SystemObjects.Artifact, responseModel.ObjectID);
                    }

                    if (responseModel.Object == SystemObjects.Policy.ToString() && model.PreloadData)
                    {
                        var apiCtrlr = new D3SApiController(Set, null, null, null, null, null);
                        apiCtrlr.Request = new System.Net.Http.HttpRequestMessage();
                        responseModel.PreloadData = apiCtrlr.GetPoliciesByType(responseModel.ObjectTypeId, true);
                    }


                    if (responseModel.Object == SystemObjects.Taxonomy.ToString() && model.PreloadData)
                    {
                        var apiCtrlr = new TaxonomyController(Set);
                        responseModel.PreloadData = apiCtrlr.ModelHierarchy(responseModel.ObjectTypeId);
                    }


                    var anyDiagramRelationTypes = Company.Query<bool>("select case when count(*) > 0 then 1 else 0 end from IntersectTypeDetail D where D.PredicateType = @predicateType and Subject = @ObjectType and SubjectID = @ObjectTypeId", new { responseModel.ObjectType, responseModel.ObjectTypeId, predicateType = (int)PredicateType.Diagram }).SingleOrDefault();
                    if (anyDiagramRelationTypes)
                    {
                        responseModel.Items.HasProcessDiagram = true;
                    }

                    if (responseModel.Object == SystemObjects.ReferenceItemType.ToString())
                    {
                        responseModel.Items.HasImpact = responseModel.Items.HasLineage = responseModel.Items.HasProcessDiagram = false;
                    }
                }
            }
            if (responseModel != null && !Company.CurrentResourceIsAdmin && model.ObjectType != SystemObjects.SemanticType.ToString())
            {
                if (model.AssetUid != null)
                {
                    var permissions = Company.GetPermissions(responseModel.AssetId, responseModel.AssetTypeId);
                    if (permissions.Any(x => x.ID == Permission.ReadResponsibilities) || permissions.Count == 0)
                    {
                        if (responseModel.ObjectType == SystemObjects.ReferenceItemType.ToString() && !Company.CurrentResourceIsAdmin)
                        {
                            responseModel.Items.HasOwnership = false;
                        }
                        else
                        {
                            responseModel.Items.HasOwnership = true;
                        }
                    }

                    if (permissions.Any(x => x.ID == Permission.ReadRelationships) || permissions.Count == 0)
                    {
                        responseModel.Items.HasRelationship = true;
                    }
                }

                if (model.AssetId == null && model.AssetTypeUid != null)
                {
                    var permissions = Company.GetTypePermissions(responseModel.ObjectType, responseModel.ObjectTypeId);
                    if (permissions.Any(x => x.ID == Permission.ReadResponsibilities) || permissions.Count == 0)
                    {
                        if (responseModel.ObjectType == SystemObjects.ReferenceItemType.ToString() && !Company.CurrentResourceIsAdmin)
                        {
                            responseModel.Items.HasOwnership = false;
                        }
                        else
                        {
                            responseModel.Items.HasOwnership = true;
                        }
                    }
                    else
                    {
                        responseModel.Items.HasOwnership = false;
                    }

                    if (permissions.Any(x => x.ID == Permission.ReadRelationships) || permissions.Count == 0)
                    {
                        responseModel.Items.HasRelationship = true;
                    }
                    else
                    {
                        responseModel.Items.HasRelationship = false;
                    }
                }
            }
            return new JsonNetResult
            {
                Data = responseModel,
                Formatting = Newtonsoft.Json.Formatting.None
            };

            void FillResponseModelForResource(AssetDetail assetDetail)
            {
                execProcedure = false;
                responseModel.Object = assetDetail.Object;
                responseModel.ObjectID = assetDetail.ObjectID;
                responseModel.DisplayValue = assetDetail.DisplayValue;
                responseModel.MainTabTitle = "Profile";
                responseModel.Items.HasItemOwn = true;
                responseModel.Items.HasRelationship = true;
                responseModel.Items.HasGroups = true;
                responseModel.Items.HasFollowing = true;
                responseModel.Uid = assetDetail.uid;
            }
        }
    }
}
