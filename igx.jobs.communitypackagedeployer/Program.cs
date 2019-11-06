using d360.core;
using d360.core.entities.Community.Templates;
using d360.core.enums;
using d360.extensions.caching;
using d360.extensions.info;
using d360.extensions.queue;
using d360.model;
using d360.utils.company;
using Dapper;
using Microsoft.Azure.WebJobs;
using Microsoft.Practices.EnterpriseLibrary.TransientFaultHandling;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace igx.jobs.communitypackagedeployer
{
    class Program
    {
        static void Main()
        {
            var config = CoreFunction.GetJobHostConfiguration();
            config.UseTimers();
#if DEBUG
            config.UseDevelopmentSettings();
#endif

            var host = new JobHost(config);
            host.RunAndBlock();
        }
    }

    public static class CommunityPackageCheckTimerJob
    {
        const string functionName = "CommunityPackageCheck_TimerJob";
        const string timerSettings = "0 */15 * * * *";

        const int ALTER_TRIGGER_TIMEOUT = 90;

        public static void Run([TimerTrigger(timerSettings, RunOnStartup = true)]TimerInfo myTimer, TextWriter log)
        {
            try
            {
                var community = new SqlConnection(constants.COMMUNITY_DATABASE_CONNECTION);
                community.OpenWithRetry(RetryPolicy.DefaultProgressive);

                var environment = CoreFunction.GetConfigValueByKey("Environment");
                
                DateTime Now = DateTime.UtcNow;

                #region Get Package Lists

                var lvl = CoreFunction.GetEnvironmentLevelCurrentSlot();
                var reader = community.QueryMultiple(@"exec community.GetChanges @lvl", new { lvl = (int)lvl }, commandTimeout: 60);

                var packageVersions = reader.Read<PackageVersion>().ToList();
                var packageClients = reader.Read<PackageClient>().ToList();
                var allocations = reader.Read<Allocation>().ToList();
                var assetTypes = reader.Read<AssetType>().ToList();
                var assetTypeVersions = reader.Read<AssetTypeVersion>().ToList();
                var assets = reader.Read<Asset>().ToList();
                var predicates = reader.Read<Predicate>().ToList();
                var intersectTypes = reader.Read<IntersectType>().ToList();
                var intersects = reader.Read<Intersect>().ToList();
                
                #endregion

                community.Close();
                community.Dispose();

#if DEBUG
                var companies = CompanyConnectionUtils.GetCompaniesWithDatabaseServerSettings().Where(i => i.CompanyID == 1).ToList();
#else
                var companies = CoreFunction.GetCompaniesByCurrentSlot();
#endif

                companies.ForEach(c =>
                {
                    try
                    {
                        var caching = new DummyCachingProvider();
                        var queue = new DummyQueueSource();
                        var securityContext = new UriSecurityContextProvider {
                            CompanyID = c.CompanyID, CompanyPrefix = c.UrlPrefix, IsAdministrator = true, ResourceID = 0
                        };
                        var communityContext = new CommunityContext(caching, queue, securityContext);
                        var companyContext = new CompanyContext(communityContext, caching, queue, securityContext, true);

                        var thisClientPackages = packageClients.Where(p => p.ClientID == c.ClientID).ToList();

                        thisClientPackages.ForEach(p =>
                        {
                            var thisPackageAllocations = allocations.Where(a => a.PackageVersionUid == p.PackageVersionUid).ToList();
                            thisPackageAllocations.ForEach(a => {

                                #region Asset Types

                                var theseAssetTypes = (
                                    from vUid in a.AssetTypeVersions
                                    join v in assetTypeVersions on vUid equals v.Uid
                                    join t in assetTypes on v.AssetTypeUid equals t.Uid
                                    select new
                                    {
                                        v.AssetTypeUid,
                                        v.AutoDisplayDescription,
                                        v.BackColor,
                                        v.CanOwnFusion,
                                        v.CreatedOn,
                                        v.Description,
                                        v.DisplayFormat,
                                        v.Fields,
                                        v.ForeColor,
                                        v.HierarchyMaximumDepth,
                                        v.Icon,
                                        v.Levels,
                                        t.Class,
                                        t.Hierarchical,
                                        t.Name,
                                        t.Object,
                                        t.ObjectID,
                                        v.State,
                                        v.UpdatedOn,
                                        v.UseAsTransformation
                                    }).ToList();

                                var existingAssetTypes = companyContext.Table<d360.core.entities.AssetType>().ToList();
                                var existingAssetTypeLevels = companyContext.Table<d360.core.entities.AssetTypeLevel>().ToList();
                                var existingAssetTypeStyles = companyContext.Table<d360.core.entities.AssetTypeStyle>().ToList();

                                theseAssetTypes.ForEach(e =>
                                {
                                    var companyAssetType = existingAssetTypes.FirstOrDefault(o => o.uid == e.AssetTypeUid);
                                    if (companyAssetType == null)
                                    {
                                        companyAssetType = new d360.core.entities.AssetType {
                                            AutoDisplayDescription = e.AutoDisplayDescription,
                                            CanOwnFusion = e.CanOwnFusion,
                                            Class = e.Class,
                                            CreatedBy = 0,
                                            CreatedOn = e.CreatedOn,
                                            Description = e.Description,
                                            DisplayFormat = e.DisplayFormat,
                                            Hierarchical = e.Hierarchical,
                                            HierarchyMaximumDepth = e.HierarchyMaximumDepth,
                                            Name = e.Name,
                                            Object = e.Object.ToString(),
                                            State = e.State,
                                            uid = e.AssetTypeUid
                                        };
                                        companyContext.Add(companyAssetType);
                                        existingAssetTypes.Add(companyAssetType);
                                    }

                                    bool anyUpdate = false;

                                    #region Simple Property Checks

                                    if (companyAssetType.AutoDisplayDescription != e.AutoDisplayDescription)
                                    {
                                        companyAssetType.AutoDisplayDescription = e.AutoDisplayDescription;
                                        anyUpdate = true;
                                    }

                                    if (companyAssetType.CanOwnFusion != e.CanOwnFusion)
                                    {
                                        companyAssetType.CanOwnFusion = e.CanOwnFusion;
                                        anyUpdate = true;
                                    }

                                    if (companyAssetType.Class != e.Class)
                                    {
                                        companyAssetType.Class = e.Class;
                                        anyUpdate = true;
                                    }

                                    if (companyAssetType.Description != e.Description)
                                    {
                                        companyAssetType.Description = e.Description;
                                        anyUpdate = true;
                                    }

                                    if (companyAssetType.DisplayFormat != e.DisplayFormat)
                                    {
                                        companyAssetType.DisplayFormat = e.DisplayFormat;
                                        anyUpdate = true;
                                    }

                                    if (companyAssetType.Hierarchical != e.Hierarchical)
                                    {
                                        companyAssetType.Hierarchical = e.Hierarchical;
                                        anyUpdate = true;
                                    }

                                    if (companyAssetType.HierarchyMaximumDepth != e.HierarchyMaximumDepth)
                                    {
                                        companyAssetType.HierarchyMaximumDepth = e.HierarchyMaximumDepth;
                                        anyUpdate = true;
                                    }

                                    if (companyAssetType.Name != e.Name)
                                    {
                                        companyAssetType.Name = e.Name;
                                        anyUpdate = true;
                                    }

                                    if (companyAssetType.State != e.State)
                                    {
                                        companyAssetType.State = e.State;
                                        anyUpdate = true;
                                    }

                                    if (companyAssetType.UseAsTransformation != e.UseAsTransformation)
                                    {
                                        companyAssetType.UseAsTransformation = e.UseAsTransformation;
                                        anyUpdate = true;
                                    }

                                    #endregion

                                    #region Levels

                                    if (e.Hierarchical)
                                    {
                                        var companyAssetTypeLevels = existingAssetTypeLevels.Where(i => i.AssetTypeID == companyAssetType.ID).ToList();
                                        e.Levels.ForEach(cl => {
                                            var el = companyAssetTypeLevels.FirstOrDefault(i => i.Level == cl.Level);
                                            if (el != null)
                                            {
                                                if (el.Name != cl.Name)
                                                {
                                                    el.Name = cl.Name;
                                                }
                                                if (el.Description != cl.Description)
                                                {
                                                    el.Description = cl.Description;
                                                }
                                            }
                                            else 
                                            {
                                                el = new d360.core.entities.AssetTypeLevel { AssetTypeID = companyAssetType.ID, Level = cl.Level, Description = cl.Description, Name = cl.Name };
                                                companyContext.AssetTypeLevels.Add(el);
                                                existingAssetTypeLevels.Add(el);
                                            }
                                        });
                                        var levelsToRemove = existingAssetTypeLevels.Where(i => i.AssetTypeID == companyAssetType.ID && i.Level > companyAssetType.HierarchyMaximumDepth);
                                        companyContext.AssetTypeLevels.RemoveRange(levelsToRemove);
                                        existingAssetTypeLevels.RemoveAll(i => i.AssetTypeID == companyAssetType.ID && i.Level > companyAssetType.HierarchyMaximumDepth);
                                        anyUpdate = true;
                                    }
                                    else 
                                    {
                                        var levelsToRemove = existingAssetTypeLevels.Where(i => i.AssetTypeID == companyAssetType.ID);
                                        companyContext.AssetTypeLevels.RemoveRange(levelsToRemove);
                                        existingAssetTypeLevels.RemoveAll(i => i.AssetTypeID == companyAssetType.ID);
                                    }

                                    #endregion

                                    #region Style Logic

                                    var style = existingAssetTypeStyles.FirstOrDefault(i => i.ID == companyAssetType.ID);
                                    bool styleIsNew = (style == null);

                                    if (style.IconBackColor != e.BackColor)
                                    {
                                        style.IconBackColor = e.BackColor;
                                        anyUpdate = true;
                                    }
                                    if (style.IconForeColor != e.ForeColor)
                                    {
                                        style.IconForeColor = e.ForeColor;
                                        anyUpdate = true;
                                    }

                                    #endregion

                                    if (anyUpdate || styleIsNew)
                                    {
                                        if (styleIsNew)
                                        {
                                            companyContext.AssetTypeStyles.Add(style);
                                            existingAssetTypeStyles.Add(style);
                                        }

                                        companyAssetType.UpdatedBy = 0;
                                        companyAssetType.UpdatedOn = e.UpdatedOn;
                                    }
                                });

                                #endregion

                                //a.AssetTypeVersions.ForEach(v =>
                                //{

                                //});

                                //a.IntersectTypes.ForEach(v =>
                                //{

                                //});

                                try
                                {

                                }
                                catch (Exception ex)
                                {
                                    CoreFunction.AITrackException(functionName, ex, c.CompanyID);
                                }

                            });
                        });
                    }
                    catch (Exception ex)
                    {
                        CoreFunction.AITrackException(functionName, ex, c.CompanyID);
                    }
                });
            }
            catch (Exception ex)
            {
                CoreFunction.AITrackException(functionName, ex);
            }            
        }
    }
}
