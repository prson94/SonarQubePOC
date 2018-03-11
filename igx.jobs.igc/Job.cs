using Microsoft.Azure.WebJobs;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;

namespace igx.jobs.igc
{
    public static class IgcIntegration
    {
        const string functionName = "IGC_Integration";
        //const string timerSettings = "0 */30 * * * *";
        const string timerSettings = "*/10 * * * * *";

        #region State street Settings - NEED to Externalize

        //const string TargetUri = "https://ssb-igx.dev.data3sixty.com/services/assets/";
        const string TargetUri = "http://ssb-igx.dev.data3sixty.local/services/assets/";
        const string TargetAuthString = "w7gt581AOMXhXeW9mh0jWCPMe;3=f+7afAQUq9wUZgyibXq9kGa2iLGS3M0r-Ex-ZxJ6O9TAu+-7";

        //const string SourceUri = "https://192.168.99.100:9443/ibm/iis/igc-rest/v1/";
        //const string SourceAuthString = "Basic aXNhZG1pbjppc2FkbWlu";   //Local
        const string SourceUri = "https://edgm-catalog-uat.statestreet.com/ibm/iis/igc-rest/v1/";
        const string SourceAuthString = "Basic dGVzdDM2MDpkYXRhMzYw";   //State Street UAT
        //const string SourceUri = "https://edgm-catalog.statestreet.com/ibm/iis/igc-rest/v1/";
        //const string SourceAuthString = "Basic c3BsRFRTV0VCMjg2MjM6cChMWlsxfF1bYkl1";   //State Street PROD //UID: splDTSWEB28623    PWD: p(LZ[1|][bIu

        #endregion

        public static void Run([TimerTrigger(timerSettings)]TimerInfo myTimer, TextWriter log) //   
        {
            try
            {
                CoreFunction.AITrackJobStart(functionName);

                //LoadApplicationCatalog();

                //GetRrpFunctionalArea();
                //GetRrpLevel1();
                //GetRrpLevel2();
                //GetRrpLevel3();

                //GetBuLevel1();
                //GetBuLevel2();
                //GetBuLevel3();
                //GetBuLevel4();
                //GetBuLevel5();
                //GetBuLevel6();
                //GetBuLevel7();

                GetHosts();
                //var companies = CoreFunction.GetCompaniesByCurrentSlot();
                //companies.ForEach(c =>
                //{
                //  try
                //  {
                //      var company = CompanyConnectionUtils.GetCompanyConnection(c.CompanyID, c.Server, c.Username, c.Password);
                //      company.OpenWithRetry(RetryPolicy.DefaultFixed);
                //  }
                //  catch (Exception ex)
                //  {
                //      CoreFunction.AITrackException(functionName, ex, c.CompanyID);
                //      //log.Error($"Company [{c.CompanyID}]: [{ex.GetFullExceptionData()}]");
                //  }
                //});

                CoreFunction.AITrackJobCompletedNoErrors(functionName);
            }
            catch (Exception ex)
            {
                CoreFunction.AITrackException(functionName, ex);
                //log.Error($"General Exception: {ex.GetFullExceptionData()}");
            }

            CoreFunction.AIFlush();
        }


        #region Generic

        internal static T GetFromApi<T>(string uri, string authorization)
        {
            var req = HttpWebRequest.CreateHttp(uri);
            req.Accept = "application/json";
            req.Headers.Set(HttpRequestHeader.Authorization, authorization);
            req.ServerCertificateValidationCallback = delegate { return true; };

            var jsonRaw = "";

            var response = req.GetResponse();
            using (var responseStream = response.GetResponseStream())
            {
                using (var rdr = new StreamReader(responseStream))
                {
                    jsonRaw = rdr.ReadToEnd();
                }
            }

            return JsonConvert.DeserializeObject<T>(jsonRaw, new JsonSerializerSettings { MetadataPropertyHandling = MetadataPropertyHandling.Ignore });
        }

        static string PostJsonToApi(string uri, string authorization, string requestBody)
        {
            var jsonToReturn = "";

            using (var client = new WebClient())
            {
                client.Headers.Set(HttpRequestHeader.Accept, "application/json");
                client.Headers.Set(HttpRequestHeader.ContentType, "application/json");
                client.Headers.Set(HttpRequestHeader.Authorization, authorization);
                jsonToReturn = client.UploadString(uri, requestBody);
            }

            return jsonToReturn;
        }

        static string buildSearchUri(string type, List<string> properties)
        {
            var url = $"{SourceUri}search/?pageSize=75&types={type}";
            foreach (var p in properties)
            {
                url += $"&properties={p}";
            }
            return url;
        }

        #endregion

        public static void GetTypes()
        {
            var url = $"{SourceUri}types";

            var models = GetFromApi<dynamic>(url, SourceAuthString);
            //if (models != null)
            //{
            //    url = models.paging.next;
            //}

        }


        #region Application

        public static void LoadApplicationCatalog()
        {
            var properties = "short_description,long_description,labels,stewards,assigned_to_terms,implements_rules,governed_by_rules,$CMDBAppCode,$ApplicationAlias,$BusinessOwner,$BusinessOwnerId,$ApplicationOwner,$ApplicationOwnerId,$DataSteward,$DataStewardId,$DataOwner,$EDGMStewardId,$Comments,$SSID,$KeyApplicationType,$Status,$DataLocation,$PersonalData,$ComponentType,$ComponentCode,$ComponentSAID,$AuthoritativeSource,$MaturityLevel,$BookOfRecord,impacts_on".Split(',');
            var url = $"{SourceUri}search/?pageSize=75&types=$ApplicationCatalog-ApplicationCatalog";
            foreach (var p in properties)
            {
                url += $"&properties={p}";
            }

            var arr = new List<D3sApplicationCatalogModel>();
            var d3sImpactRelationships = new List<D3sBusinesUnitApplicationCatalogRelationshipModel>();
            var ownershipTopModel = new D3sOwnershipItemsModel { UserIdFieldName = "UserID", Items = new List<D3sOwnershipModel>() };

            Func<IgcApplicationCatalogModels, IgcApplicationCatalogModels> parse = delegate (IgcApplicationCatalogModels root)
            {
                arr.AddRange(root.items.ConvertAll<D3sApplicationCatalogModel>(i => new D3sApplicationCatalogModel
                {
                    SourceID = i.SourceID,
                    Name = i.Name,
                    ShortDescription = i.ShortDescription,
                    ApplicationAlias = i.ApplicationAlias,
                    AuthoritativeSource = i.AuthoritativeSource,
                    Host = i.BookOfRecord,
                    CMDBAppCode = i.CMDBAppCode,
                    Comments = i.Comments,
                    KeyApplicationTypeText = (i.KeyApplicationType != null) ? string.Join(", ", i.KeyApplicationType) : "",
                    ComponentSAID = i.ComponentSAID,
                    ComponentType = i.ComponentType,
                    DataLocation = i.DataLocation,
                    LongDescription = i.LongDescription,
                    MaturityLevel = i.MaturityLevel,
                    PersonalData = i.PersonalData ?? "No",
                    SSID = i.SSID,
                    Status = i.Status
                }));

                ownershipTopModel.Items.AddRange(root.items.ConvertAll<D3sOwnershipModel>(i => new D3sOwnershipModel
                {
                    SourceID = i.SourceID,
                    RoleName = "Business Owner",
                    UserId = i.BusinessOwnerId,
                    UserFullName = i.BusinessOwner
                }));

                ownershipTopModel.Items.AddRange(root.items.ConvertAll<D3sOwnershipModel>(i => new D3sOwnershipModel
                {
                    SourceID = i.SourceID,
                    RoleName = "Application Owner",
                    UserId = i.ApplicationOwnerId,
                    UserFullName = i.ApplicationOwner
                }));

                ownershipTopModel.Items.AddRange(root.items.ConvertAll<D3sOwnershipModel>(i => new D3sOwnershipModel
                {
                    SourceID = i.SourceID,
                    RoleName = "Data Owner",
                    UserId = string.Empty,
                    UserFullName = i.DataOwner
                }));

                ownershipTopModel.Items.AddRange(root.items.ConvertAll<D3sOwnershipModel>(i => new D3sOwnershipModel
                {
                    SourceID = i.SourceID,
                    RoleName = "Data Steward",
                    UserId = i.DataStewardId,
                    UserFullName = i.DataSteward
                }));

                ownershipTopModel.Items.AddRange(root.items.ConvertAll<D3sOwnershipModel>(i => new D3sOwnershipModel
                {
                    SourceID = i.SourceID,
                    RoleName = "EDGM Steward",
                    UserId = i.EDGMStewardId,
                    UserFullName = string.Empty
                }));

                foreach (var app in root.items)
                {
                    app.ImpactsOn.items.ForEach(bu =>
                    {
                        d3sImpactRelationships.Add(
                            new D3sBusinesUnitApplicationCatalogRelationshipModel { SubjectSourceID = bu.SourceID, ObjectSourceID = app.SourceID, PredicateType = 7 }
                        );
                    });
                }

                return root;
            };

            while (!string.IsNullOrEmpty(url))
            {
                var models = GetFromApi<IgcApplicationCatalogModels>(url, SourceAuthString);
                if (models != null)
                {
                    parse(models);
                    url = models.paging.next;
                }
            }

            // If any items to send to server.
            if (arr.Count > 0)
            {
                //var respString = PostJsonToApi(
                //    $"{TargetUri}ArtifactType/2/bulk",
                //    TargetAuthString,
                //    JsonConvert.SerializeObject(arr)
                //);
            }

            // If any owners to send to server.
            if (ownershipTopModel.Items.Count > 0)
            {
                var uniqueUsers = ownershipTopModel.Items
                    .Where(i => !string.IsNullOrEmpty(i.UserFullName) && !string.IsNullOrEmpty(i.UserId))
                    .Select(i => new { i.UserFullName, i.UserId })
                    .Distinct()
                    .ToList();

                // Populate the UserIDs that are missing, based can be looked up by user's full name. TODO: Confirm this logic, as it may not be correct if two or more user's have the same name.
                foreach (var item in ownershipTopModel.Items.Where(i => string.IsNullOrEmpty(i.UserId)))
                {
                    var match = uniqueUsers.FirstOrDefault(i => i.UserFullName == item.UserFullName);
                    if (match != null)
                    {
                        item.UserId = match.UserId;
                    }
                }

                //Now, remove any users whose internal ID cannot be resolved.
                ownershipTopModel.Items.RemoveAll(i => string.IsNullOrEmpty(i.UserId));

                var respString = PostJsonToApi(
                    $"{TargetUri}ownership/bulk",
                    TargetAuthString,
                    JsonConvert.SerializeObject(ownershipTopModel)
                );
            }

            if (d3sImpactRelationships.Count > 0)
            {
                //var respString = PostJsonToApi(
                //    $"{TargetUri}relationships/bulk",
                //    TargetAuthString,
                //    JsonConvert.SerializeObject(d3sImpactRelationships)
                //);
            }
        }

        #endregion

        #region Fusion

        public static void GetHosts()
        {
            var url = buildSearchUri("host", new List<string> {
                "short_description",
                "long_description",
                "labels",
                "stewards",
                //"assigned_to_terms",
                //"implements_rules",
                //"governed_by_rules",
                //"databases",
                //"data_files",
                //"idoc_types",
                //"transformation_projects"
                //"data_connections",
                //"amazon_s3_buckets",
                //"data_file_folders",
                //"location",
                //"network_node",
                //"imported_from",
                //"in_colleections",
                "notes"
            });

            var arr = new List<dynamic>();
            //var d3sImpactRelationships = new List<D3sBusinesUnitApplicationCatalogRelationshipModel>();
            //var ownershipTopModel = new D3sOwnershipItemsModel { UserIdFieldName = "UserID", Items = new List<D3sOwnershipModel>() };

            Func<IgcDynamicModels, IgcDynamicModels> parse = delegate (IgcDynamicModels root)
            {
                arr.AddRange(root.items.ConvertAll<dynamic>(i => new
                {
                    SourceID = i._id,
                    Name = i._name,
                    ShortDescription = i.short_description,
                    LongDescription = i.long_description,
                    Notes = i.notes
                }));

                //ownershipTopModel.Items.AddRange(root.items.ConvertAll<D3sOwnershipModel>(i => new D3sOwnershipModel
                //{
                //    SourceID = i.SourceID,
                //    RoleName = "Business Owner",
                //    UserId = i.BusinessOwnerId,
                //    UserFullName = i.BusinessOwner
                //}));

                //ownershipTopModel.Items.AddRange(root.items.ConvertAll<D3sOwnershipModel>(i => new D3sOwnershipModel
                //{
                //    SourceID = i.SourceID,
                //    RoleName = "Application Owner",
                //    UserId = i.ApplicationOwnerId,
                //    UserFullName = i.ApplicationOwner
                //}));

                //ownershipTopModel.Items.AddRange(root.items.ConvertAll<D3sOwnershipModel>(i => new D3sOwnershipModel
                //{
                //    SourceID = i.SourceID,
                //    RoleName = "Data Owner",
                //    UserId = string.Empty,
                //    UserFullName = i.DataOwner
                //}));

                //ownershipTopModel.Items.AddRange(root.items.ConvertAll<D3sOwnershipModel>(i => new D3sOwnershipModel
                //{
                //    SourceID = i.SourceID,
                //    RoleName = "Data Steward",
                //    UserId = i.DataStewardId,
                //    UserFullName = i.DataSteward
                //}));

                //ownershipTopModel.Items.AddRange(root.items.ConvertAll<D3sOwnershipModel>(i => new D3sOwnershipModel
                //{
                //    SourceID = i.SourceID,
                //    RoleName = "EDGM Steward",
                //    UserId = i.EDGMStewardId,
                //    UserFullName = string.Empty
                //}));

                //foreach (var app in root.items)
                //{
                //    app.ImpactsOn.items.ForEach(bu =>
                //    {
                //        d3sImpactRelationships.Add(
                //            new D3sBusinesUnitApplicationCatalogRelationshipModel { SubjectSourceID = bu.SourceID, ObjectSourceID = app.SourceID, PredicateType = 7 }
                //        );
                //    });
                //}

                return root;
            };

            while (!string.IsNullOrEmpty(url))
            {
                var cleanUri = new Uri(url);
                if (cleanUri.Port != 80 && cleanUri.Port != 443)
                {
                    url = url.Replace($":{cleanUri.Port}", "");
                }

                var models = GetFromApi<IgcDynamicModels>(url, SourceAuthString);
                if (models != null)
                {
                    parse(models);
                    url = models.paging.next;
                }
            }

            // If any items to send to server.
            if (arr.Count > 0)
            {
                //var respString = PostJsonToApi(
                //    $"{TargetUri}ArtifactType/2/bulk",
                //    TargetAuthString,
                //    JsonConvert.SerializeObject(arr)
                //);
            }

            //// If any owners to send to server.
            //if (ownershipTopModel.Items.Count > 0)
            //{
            //    var uniqueUsers = ownershipTopModel.Items
            //        .Where(i => !string.IsNullOrEmpty(i.UserFullName) && !string.IsNullOrEmpty(i.UserId))
            //        .Select(i => new { i.UserFullName, i.UserId })
            //        .Distinct()
            //        .ToList();

            //    // Populate the UserIDs that are missing, based can be looked up by user's full name. TODO: Confirm this logic, as it may not be correct if two or more user's have the same name.
            //    foreach (var item in ownershipTopModel.Items.Where(i => string.IsNullOrEmpty(i.UserId)))
            //    {
            //        var match = uniqueUsers.FirstOrDefault(i => i.UserFullName == item.UserFullName);
            //        if (match != null)
            //        {
            //            item.UserId = match.UserId;
            //        }
            //    }

            //    //Now, remove any users whose internal ID cannot be resolved.
            //    ownershipTopModel.Items.RemoveAll(i => string.IsNullOrEmpty(i.UserId));

            //    var respString = PostJsonToApi(
            //        $"{TargetUri}ownership/bulk",
            //        TargetAuthString,
            //        JsonConvert.SerializeObject(ownershipTopModel)
            //    );
            //}

            //if (d3sImpactRelationships.Count > 0)
            //{
            //    //var respString = PostJsonToApi(
            //    //    $"{TargetUri}relationships/bulk",
            //    //    TargetAuthString,
            //    //    JsonConvert.SerializeObject(d3sImpactRelationships)
            //    //);
            //}
        }

        //public static void GetDataFiles()
        //{
        //    var url = buildSearchUri("data_file", new List<string> {
        //        "short_description",
        //        "long_description",
        //        "parent_folder",
        //        "host",
        //        "labels",
        //        "stewards",
        //        //"assigned_to_terms",
        //        //"implements_rules",
        //        "governed_by_rules",
        //        "data_file_records",
        //        //"implements_data_file_definition",
        //        //"implements_physical_models",
        //        //"custom_Catalog Status",
        //        //"custom_Classification",
        //        //"custom_Comments",
        //        //"custom_Created By",
        //        //"custom_Data Steward",
        //        //"custom_Data Steward Id",
        //        //"custom_Frequency",
        //        "custom_Information Classification",
        //        //"custom_Modified By",
        //        "custom_Output Format",
        //        //"custom_Owner",
        //        //"custom_Owner Id",
        //        "custom_Status",
        //        "alias_(business_name)",
        //        "path",
        //        //"store_type",
        //        "imported_from",
        //        //"impacted_by",
        //        //"impacts_on",
        //        "include_for_business_lineage",
        //        "suggested_term_assignments",
        //        "notes",
        //        "amazon_s3_data_files",
        //        "implements_data_file_definition",
        //        "implements_physical_models"
        //    });

        //    var arr = new List<dynamic>();
        //    //var d3sImpactRelationships = new List<D3sBusinesUnitApplicationCatalogRelationshipModel>();
        //    //var ownershipTopModel = new D3sOwnershipItemsModel { UserIdFieldName = "UserID", Items = new List<D3sOwnershipModel>() };

        //    Func<IgcDataFileModels, IgcDataFileModels> parse = delegate (IgcDataFileModels root)
        //    {
        //        arr.AddRange(root.items.ConvertAll<dynamic>(i => new //D3sApplicationCatalogModel
        //        {
        //            SourceID = i.SourceID,
        //            Name = i.Name,
        //            ShortDescription = i.ShortDescription,
        //            ApplicationAlias = i.ApplicationAlias,
        //            AuthoritativeSource = i.AuthoritativeSource,
        //            Host = i.BookOfRecord,
        //            CMDBAppCode = i.CMDBAppCode,
        //            Comments = i.Comments,
        //            KeyApplicationTypeText = (i.KeyApplicationType != null) ? string.Join(", ", i.KeyApplicationType) : "",
        //            ComponentSAID = i.ComponentSAID,
        //            ComponentType = i.ComponentType,
        //            DataLocation = i.DataLocation,
        //            LongDescription = i.LongDescription,
        //            MaturityLevel = i.MaturityLevel,
        //            PersonalData = i.PersonalData ?? "No",
        //            SSID = i.SSID,
        //            Status = i.Status
        //        }));

        //        //ownershipTopModel.Items.AddRange(root.items.ConvertAll<D3sOwnershipModel>(i => new D3sOwnershipModel
        //        //{
        //        //    SourceID = i.SourceID,
        //        //    RoleName = "Business Owner",
        //        //    UserId = i.BusinessOwnerId,
        //        //    UserFullName = i.BusinessOwner
        //        //}));

        //        //ownershipTopModel.Items.AddRange(root.items.ConvertAll<D3sOwnershipModel>(i => new D3sOwnershipModel
        //        //{
        //        //    SourceID = i.SourceID,
        //        //    RoleName = "Application Owner",
        //        //    UserId = i.ApplicationOwnerId,
        //        //    UserFullName = i.ApplicationOwner
        //        //}));

        //        //ownershipTopModel.Items.AddRange(root.items.ConvertAll<D3sOwnershipModel>(i => new D3sOwnershipModel
        //        //{
        //        //    SourceID = i.SourceID,
        //        //    RoleName = "Data Owner",
        //        //    UserId = string.Empty,
        //        //    UserFullName = i.DataOwner
        //        //}));

        //        //ownershipTopModel.Items.AddRange(root.items.ConvertAll<D3sOwnershipModel>(i => new D3sOwnershipModel
        //        //{
        //        //    SourceID = i.SourceID,
        //        //    RoleName = "Data Steward",
        //        //    UserId = i.DataStewardId,
        //        //    UserFullName = i.DataSteward
        //        //}));

        //        //ownershipTopModel.Items.AddRange(root.items.ConvertAll<D3sOwnershipModel>(i => new D3sOwnershipModel
        //        //{
        //        //    SourceID = i.SourceID,
        //        //    RoleName = "EDGM Steward",
        //        //    UserId = i.EDGMStewardId,
        //        //    UserFullName = string.Empty
        //        //}));

        //        //foreach (var app in root.items)
        //        //{
        //        //    app.ImpactsOn.items.ForEach(bu =>
        //        //    {
        //        //        d3sImpactRelationships.Add(
        //        //            new D3sBusinesUnitApplicationCatalogRelationshipModel { SubjectSourceID = bu.SourceID, ObjectSourceID = app.SourceID, PredicateType = 7 }
        //        //        );
        //        //    });
        //        //}

        //        return root;
        //    };

        //    while (!string.IsNullOrEmpty(url))
        //    {
        //        var models = GetFromApi<IgcDataFileModels>(url, SourceAuthString);
        //        if (models != null)
        //        {
        //            parse(models);
        //            url = models.paging.next;
        //        }
        //    }

        //    // If any items to send to server.
        //    if (arr.Count > 0)
        //    {
        //        //var respString = PostJsonToApi(
        //        //    $"{TargetUri}ArtifactType/2/bulk",
        //        //    TargetAuthString,
        //        //    JsonConvert.SerializeObject(arr)
        //        //);
        //    }

        //    //// If any owners to send to server.
        //    //if (ownershipTopModel.Items.Count > 0)
        //    //{
        //    //    var uniqueUsers = ownershipTopModel.Items
        //    //        .Where(i => !string.IsNullOrEmpty(i.UserFullName) && !string.IsNullOrEmpty(i.UserId))
        //    //        .Select(i => new { i.UserFullName, i.UserId })
        //    //        .Distinct()
        //    //        .ToList();

        //    //    // Populate the UserIDs that are missing, based can be looked up by user's full name. TODO: Confirm this logic, as it may not be correct if two or more user's have the same name.
        //    //    foreach (var item in ownershipTopModel.Items.Where(i => string.IsNullOrEmpty(i.UserId)))
        //    //    {
        //    //        var match = uniqueUsers.FirstOrDefault(i => i.UserFullName == item.UserFullName);
        //    //        if (match != null)
        //    //        {
        //    //            item.UserId = match.UserId;
        //    //        }
        //    //    }

        //    //    //Now, remove any users whose internal ID cannot be resolved.
        //    //    ownershipTopModel.Items.RemoveAll(i => string.IsNullOrEmpty(i.UserId));

        //    //    var respString = PostJsonToApi(
        //    //        $"{TargetUri}ownership/bulk",
        //    //        TargetAuthString,
        //    //        JsonConvert.SerializeObject(ownershipTopModel)
        //    //    );
        //    //}

        //    //if (d3sImpactRelationships.Count > 0)
        //    //{
        //    //    //var respString = PostJsonToApi(
        //    //    //    $"{TargetUri}relationships/bulk",
        //    //    //    TargetAuthString,
        //    //    //    JsonConvert.SerializeObject(d3sImpactRelationships)
        //    //    //);
        //    //}
        //}

        #endregion

        #region RRP

        public static void GetRrpFunctionalArea()
        {
            var properties = "short_description,long_description".Split(',');
            var url = $"{SourceUri}search/?pageSize=75&types=$RRP-RRPFunctionalArea";
            foreach (var p in properties)
            {
                url += $"&properties={p}";
            }

            var arr = new List<D3sRrpFunctionalAreaModel>();

            Func<IgcRrpFunctionalAreaModels, IgcRrpFunctionalAreaModels> parse = delegate (IgcRrpFunctionalAreaModels root)
            {
                arr.AddRange(root.items.ConvertAll<D3sRrpFunctionalAreaModel>(i => new D3sRrpFunctionalAreaModel
                {
                    SourceID = i.SourceID,
                    Name = i.Name,
                    ShortDescription = i.ShortDescription,
                    LongDescription = i.LongDescription
                }));

                //ownershipTopModel.Items.AddRange(root.items.ConvertAll<D3sOwnershipModel>(i => new D3sOwnershipModel
                //{
                //    SourceID = i.SourceID,
                //    RoleName = "Business Owner",
                //    UserId = i.BusinessOwnerId,
                //    UserFullName = i.BusinessOwner
                //}));

                //foreach (var app in root.items)
                //{
                //    app.ImpactsOn.items.ForEach(bu =>
                //    {
                //        d3sImpactRelationships.Add(
                //            new D3sBusinesUnitApplicationCatalogRelationshipModel { SubjectSourceID = bu.SourceID, ObjectSourceID = app.SourceID, PredicateType = 7 }
                //        );
                //    });
                //}

                return root;
            };

            while (!string.IsNullOrEmpty(url))
            {
                var models = GetFromApi<IgcRrpFunctionalAreaModels>(url, SourceAuthString);
                if (models != null)
                {
                    parse(models);
                    url = models.paging.next;
                }
            }

            var respString = PostJsonToApi(
                $"{TargetUri}TaxonomyType/3/bulk",
                TargetAuthString,
                JsonConvert.SerializeObject(arr)
            );
        }

        public static void GetRrpLevel1()
        {
            var properties = "short_description,long_description".Split(',');
            var url = $"{SourceUri}search/?pageSize=75&types=$RRP-RRPLevel1Service";
            foreach (var p in properties)
            {
                url += $"&properties={p}";
            }

            var arr = new List<D3sRrpLevelOneModel>();

            Func<IgcRrpLevelOneModels, IgcRrpLevelOneModels> parse = delegate (IgcRrpLevelOneModels root)
            {
                arr.AddRange(root.items.ConvertAll<D3sRrpLevelOneModel>(i => new D3sRrpLevelOneModel
                {
                    SourceID = i.SourceID,
                    ParentSourceID = i._context[0]._id,
                    Name = i.Name,
                    ShortDescription = i.ShortDescription,
                    LongDescription = i.LongDescription
                }));

                //ownershipTopModel.Items.AddRange(root.items.ConvertAll<D3sOwnershipModel>(i => new D3sOwnershipModel
                //{
                //    SourceID = i.SourceID,
                //    RoleName = "Business Owner",
                //    UserId = i.BusinessOwnerId,
                //    UserFullName = i.BusinessOwner
                //}));

                //foreach (var app in root.items)
                //{
                //    app.ImpactsOn.items.ForEach(bu =>
                //    {
                //        d3sImpactRelationships.Add(
                //            new D3sBusinesUnitApplicationCatalogRelationshipModel { SubjectSourceID = bu.SourceID, ObjectSourceID = app.SourceID, PredicateType = 7 }
                //        );
                //    });
                //}

                return root;
            };

            while (!string.IsNullOrEmpty(url))
            {
                var models = GetFromApi<IgcRrpLevelOneModels>(url, SourceAuthString);
                if (models != null)
                {
                    parse(models);
                    url = models.paging.next;
                }
            }

            var respString = PostJsonToApi(
                $"{TargetUri}TaxonomyType/3/bulk",
                TargetAuthString,
                JsonConvert.SerializeObject(arr)
            );
        }

        public static void GetRrpLevel2()
        {
            var properties = "short_description,long_description".Split(',');
            var url = $"{SourceUri}search/?pageSize=75&types=$RRP-RRPLevel2Service";
            foreach (var p in properties)
            {
                url += $"&properties={p}";
            }

            var arr = new List<D3sRrpLevelTwoModel>();

            Func<IgcRrpLevelTwoModels, IgcRrpLevelTwoModels> parse = delegate (IgcRrpLevelTwoModels root)
            {
                arr.AddRange(root.items.ConvertAll<D3sRrpLevelTwoModel>(i => new D3sRrpLevelTwoModel
                {
                    SourceID = i.SourceID,
                    ParentSourceID = i._context[1]._id,
                    Name = i.Name,
                    ShortDescription = i.ShortDescription,
                    LongDescription = i.LongDescription
                }));

                //ownershipTopModel.Items.AddRange(root.items.ConvertAll<D3sOwnershipModel>(i => new D3sOwnershipModel
                //{
                //    SourceID = i.SourceID,
                //    RoleName = "Business Owner",
                //    UserId = i.BusinessOwnerId,
                //    UserFullName = i.BusinessOwner
                //}));

                //foreach (var app in root.items)
                //{
                //    app.ImpactsOn.items.ForEach(bu =>
                //    {
                //        d3sImpactRelationships.Add(
                //            new D3sBusinesUnitApplicationCatalogRelationshipModel { SubjectSourceID = bu.SourceID, ObjectSourceID = app.SourceID, PredicateType = 7 }
                //        );
                //    });
                //}

                return root;
            };

            while (!string.IsNullOrEmpty(url))
            {
                var models = GetFromApi<IgcRrpLevelTwoModels>(url, SourceAuthString);
                if (models != null)
                {
                    parse(models);
                    url = models.paging.next;
                }
            }

            var respString = PostJsonToApi(
                $"{TargetUri}TaxonomyType/3/bulk",
                TargetAuthString,
                JsonConvert.SerializeObject(arr)
            );
        }

        public static void GetRrpLevel3()
        {
            var properties = "short_description,long_description".Split(',');
            var url = $"{SourceUri}search/?pageSize=75&types=$RRP-RRPLevel3Service";
            foreach (var p in properties)
            {
                url += $"&properties={p}";
            }

            var arr = new List<D3sRrpLevelThreeModel>();

            Func<IgcRrpLevelThreeModels, IgcRrpLevelThreeModels> parse = delegate (IgcRrpLevelThreeModels root)
            {
                arr.AddRange(root.items.ConvertAll<D3sRrpLevelThreeModel>(i => new D3sRrpLevelThreeModel
                {
                    SourceID = i.SourceID,
                    ParentSourceID = i._context[2]._id,
                    Name = i.Name,
                    ShortDescription = i.ShortDescription,
                    LongDescription = i.LongDescription
                }));

                //ownershipTopModel.Items.AddRange(root.items.ConvertAll<D3sOwnershipModel>(i => new D3sOwnershipModel
                //{
                //    SourceID = i.SourceID,
                //    RoleName = "Business Owner",
                //    UserId = i.BusinessOwnerId,
                //    UserFullName = i.BusinessOwner
                //}));

                //foreach (var app in root.items)
                //{
                //    app.ImpactsOn.items.ForEach(bu =>
                //    {
                //        d3sImpactRelationships.Add(
                //            new D3sBusinesUnitApplicationCatalogRelationshipModel { SubjectSourceID = bu.SourceID, ObjectSourceID = app.SourceID, PredicateType = 7 }
                //        );
                //    });
                //}

                return root;
            };

            while (!string.IsNullOrEmpty(url))
            {
                var models = GetFromApi<IgcRrpLevelThreeModels>(url, SourceAuthString);
                if (models != null)
                {
                    parse(models);
                    url = models.paging.next;
                }
            }

            var respString = PostJsonToApi(
                $"{TargetUri}TaxonomyType/3/bulk",
                TargetAuthString,
                JsonConvert.SerializeObject(arr)
            );
        }

        #endregion

        #region Business Unit

        public static void GetBuLevel1()
        {
            var properties = "short_description,long_description,$BusinessUnitId,$BusinessOwnerId,$BusinessOwner".Split(',');
            var url = $"{SourceUri}search/?pageSize=75&types=$BUOrg-BusinessUnitLevel1";
            foreach (var p in properties)
            {
                url += $"&properties={p}";
            }

            var arr = new List<D3sBuTopModel>();

            Func<IgcBuTopModels, IgcBuTopModels> parse = delegate (IgcBuTopModels root)
            {
                arr.AddRange(root.items.ConvertAll<D3sBuTopModel>(i => new D3sBuTopModel
                {
                    SourceID = i.SourceID,
                    Name = i.Name,
                    ShortDescription = i.ShortDescription,
                    LongDescription = i.LongDescription,
                    BusinessOwner = i.BusinessOwner,
                    BusinessOwnerId = i.BusinessOwnerId,
                    BusinessUnitID = i.BusinessUnitId
                }));

                //ownershipTopModel.Items.AddRange(root.items.ConvertAll<D3sOwnershipModel>(i => new D3sOwnershipModel
                //{
                //    SourceID = i.SourceID,
                //    RoleName = "Business Owner",
                //    UserId = i.BusinessOwnerId,
                //    UserFullName = i.BusinessOwner
                //}));

                //foreach (var app in root.items)
                //{
                //    app.ImpactsOn.items.ForEach(bu =>
                //    {
                //        d3sImpactRelationships.Add(
                //            new D3sBusinesUnitApplicationCatalogRelationshipModel { SubjectSourceID = bu.SourceID, ObjectSourceID = app.SourceID, PredicateType = 7 }
                //        );
                //    });
                //}

                return root;
            };

            while (!string.IsNullOrEmpty(url))
            {
                var models = GetFromApi<IgcBuTopModels>(url, SourceAuthString);
                if (models != null)
                {
                    parse(models);
                    url = models.paging.next;
                }
            }

            var respString = PostJsonToApi(
                $"{TargetUri}TaxonomyType/7/bulk",
                TargetAuthString,
                JsonConvert.SerializeObject(arr)
            );
        }

        public static void GetBuLevel2()
        {
            var properties = "short_description,long_description,$BusinessUnitId,$BusinessOwnerId,$BusinessOwner".Split(',');
            var url = $"{SourceUri}search/?pageSize=75&types=$BUOrg-BusinessUnitLevel2";
            foreach (var p in properties)
            {
                url += $"&properties={p}";
            }

            var arr = new List<D3sBuChildModel>();

            Func<IgcBuChildModels, IgcBuChildModels> parse = delegate (IgcBuChildModels root)
            {
                arr.AddRange(root.items.ConvertAll<D3sBuChildModel>(i => new D3sBuChildModel
                {
                    SourceID = i.SourceID,
                    ParentSourceID = i._context[0]._id,
                    Name = i.Name,
                    ShortDescription = i.ShortDescription,
                    LongDescription = i.LongDescription,
                    BusinessOwner = i.BusinessOwner,
                    BusinessOwnerId = i.BusinessOwnerId,
                    BusinessUnitID = i.BusinessUnitId
                }));

                //ownershipTopModel.Items.AddRange(root.items.ConvertAll<D3sOwnershipModel>(i => new D3sOwnershipModel
                //{
                //    SourceID = i.SourceID,
                //    RoleName = "Business Owner",
                //    UserId = i.BusinessOwnerId,
                //    UserFullName = i.BusinessOwner
                //}));

                //foreach (var app in root.items)
                //{
                //    app.ImpactsOn.items.ForEach(bu =>
                //    {
                //        d3sImpactRelationships.Add(
                //            new D3sBusinesUnitApplicationCatalogRelationshipModel { SubjectSourceID = bu.SourceID, ObjectSourceID = app.SourceID, PredicateType = 7 }
                //        );
                //    });
                //}

                return root;
            };

            while (!string.IsNullOrEmpty(url))
            {
                var models = GetFromApi<IgcBuChildModels>(url, SourceAuthString);
                if (models != null)
                {
                    parse(models);
                    url = models.paging.next;
                }
            }

            var respString = PostJsonToApi(
                $"{TargetUri}TaxonomyType/7/bulk",
                TargetAuthString,
                JsonConvert.SerializeObject(arr)
            );
        }

        public static void GetBuLevel3()
        {
            var properties = "short_description,long_description,$BusinessUnitId,$BusinessOwnerId,$BusinessOwner".Split(',');
            var url = $"{SourceUri}search/?pageSize=75&types=$BUOrg-BusinessUnitLevel3";
            foreach (var p in properties)
            {
                url += $"&properties={p}";
            }

            var arr = new List<D3sBuChildModel>();

            Func<IgcBuChildModels, IgcBuChildModels> parse = delegate (IgcBuChildModels root)
            {
                arr.AddRange(root.items.ConvertAll<D3sBuChildModel>(i => new D3sBuChildModel
                {
                    SourceID = i.SourceID,
                    ParentSourceID = i._context[1]._id,
                    Name = i.Name,
                    ShortDescription = i.ShortDescription,
                    LongDescription = i.LongDescription,
                    BusinessOwner = i.BusinessOwner,
                    BusinessOwnerId = i.BusinessOwnerId,
                    BusinessUnitID = i.BusinessUnitId
                }));

                //ownershipTopModel.Items.AddRange(root.items.ConvertAll<D3sOwnershipModel>(i => new D3sOwnershipModel
                //{
                //    SourceID = i.SourceID,
                //    RoleName = "Business Owner",
                //    UserId = i.BusinessOwnerId,
                //    UserFullName = i.BusinessOwner
                //}));

                //foreach (var app in root.items)
                //{
                //    app.ImpactsOn.items.ForEach(bu =>
                //    {
                //        d3sImpactRelationships.Add(
                //            new D3sBusinesUnitApplicationCatalogRelationshipModel { SubjectSourceID = bu.SourceID, ObjectSourceID = app.SourceID, PredicateType = 7 }
                //        );
                //    });
                //}

                return root;
            };

            while (!string.IsNullOrEmpty(url))
            {
                var models = GetFromApi<IgcBuChildModels>(url, SourceAuthString);
                if (models != null)
                {
                    parse(models);
                    url = models.paging.next;
                }
            }

            var respString = PostJsonToApi(
                $"{TargetUri}TaxonomyType/7/bulk",
                TargetAuthString,
                JsonConvert.SerializeObject(arr)
            );
        }

        public static void GetBuLevel4()
        {
            var properties = "short_description,long_description,$BusinessUnitId,$BusinessOwnerId,$BusinessOwner".Split(',');
            var url = $"{SourceUri}search/?pageSize=75&types=$BUOrg-BusinessUnitLevel4";
            foreach (var p in properties)
            {
                url += $"&properties={p}";
            }

            var arr = new List<D3sBuChildModel>();

            Func<IgcBuChildModels, IgcBuChildModels> parse = delegate (IgcBuChildModels root)
            {
                arr.AddRange(root.items.ConvertAll<D3sBuChildModel>(i => new D3sBuChildModel
                {
                    SourceID = i.SourceID,
                    ParentSourceID = i._context[2]._id,
                    Name = i.Name,
                    ShortDescription = i.ShortDescription,
                    LongDescription = i.LongDescription,
                    BusinessOwner = i.BusinessOwner,
                    BusinessOwnerId = i.BusinessOwnerId,
                    BusinessUnitID = i.BusinessUnitId
                }));

                //ownershipTopModel.Items.AddRange(root.items.ConvertAll<D3sOwnershipModel>(i => new D3sOwnershipModel
                //{
                //    SourceID = i.SourceID,
                //    RoleName = "Business Owner",
                //    UserId = i.BusinessOwnerId,
                //    UserFullName = i.BusinessOwner
                //}));

                //foreach (var app in root.items)
                //{
                //    app.ImpactsOn.items.ForEach(bu =>
                //    {
                //        d3sImpactRelationships.Add(
                //            new D3sBusinesUnitApplicationCatalogRelationshipModel { SubjectSourceID = bu.SourceID, ObjectSourceID = app.SourceID, PredicateType = 7 }
                //        );
                //    });
                //}

                return root;
            };

            while (!string.IsNullOrEmpty(url))
            {
                var models = GetFromApi<IgcBuChildModels>(url, SourceAuthString);
                if (models != null)
                {
                    parse(models);
                    url = models.paging.next;
                }
            }

            var respString = PostJsonToApi(
                $"{TargetUri}TaxonomyType/7/bulk",
                TargetAuthString,
                JsonConvert.SerializeObject(arr)
            );
        }

        public static void GetBuLevel5()
        {
            var properties = "short_description,long_description,$BusinessUnitId,$BusinessOwnerId,$BusinessOwner".Split(',');
            var url = $"{SourceUri}search/?pageSize=75&types=$BUOrg-BusinessUnitLevel5";
            foreach (var p in properties)
            {
                url += $"&properties={p}";
            }

            var arr = new List<D3sBuChildModel>();

            Func<IgcBuChildModels, IgcBuChildModels> parse = delegate (IgcBuChildModels root)
            {
                arr.AddRange(root.items.ConvertAll<D3sBuChildModel>(i => new D3sBuChildModel
                {
                    SourceID = i.SourceID,
                    ParentSourceID = i._context[3]._id,
                    Name = i.Name,
                    ShortDescription = i.ShortDescription,
                    LongDescription = i.LongDescription,
                    BusinessOwner = i.BusinessOwner,
                    BusinessOwnerId = i.BusinessOwnerId,
                    BusinessUnitID = i.BusinessUnitId
                }));

                //ownershipTopModel.Items.AddRange(root.items.ConvertAll<D3sOwnershipModel>(i => new D3sOwnershipModel
                //{
                //    SourceID = i.SourceID,
                //    RoleName = "Business Owner",
                //    UserId = i.BusinessOwnerId,
                //    UserFullName = i.BusinessOwner
                //}));

                //foreach (var app in root.items)
                //{
                //    app.ImpactsOn.items.ForEach(bu =>
                //    {
                //        d3sImpactRelationships.Add(
                //            new D3sBusinesUnitApplicationCatalogRelationshipModel { SubjectSourceID = bu.SourceID, ObjectSourceID = app.SourceID, PredicateType = 7 }
                //        );
                //    });
                //}

                return root;
            };

            while (!string.IsNullOrEmpty(url))
            {
                var models = GetFromApi<IgcBuChildModels>(url, SourceAuthString);
                if (models != null)
                {
                    parse(models);
                    url = models.paging.next;
                }
            }

            var respString = PostJsonToApi(
                $"{TargetUri}TaxonomyType/7/bulk",
                TargetAuthString,
                JsonConvert.SerializeObject(arr)
            );
        }

        public static void GetBuLevel6()
        {
            var properties = "short_description,long_description,$BusinessUnitId,$BusinessOwnerId,$BusinessOwner".Split(',');
            var url = $"{SourceUri}search/?pageSize=75&types=$BUOrg-BusinessUnitLevel6";
            foreach (var p in properties)
            {
                url += $"&properties={p}";
            }

            var arr = new List<D3sBuChildModel>();

            Func<IgcBuChildModels, IgcBuChildModels> parse = delegate (IgcBuChildModels root)
            {
                arr.AddRange(root.items.ConvertAll<D3sBuChildModel>(i => new D3sBuChildModel
                {
                    SourceID = i.SourceID,
                    ParentSourceID = i._context[4]._id,
                    Name = i.Name,
                    ShortDescription = i.ShortDescription,
                    LongDescription = i.LongDescription,
                    BusinessOwner = i.BusinessOwner,
                    BusinessOwnerId = i.BusinessOwnerId,
                    BusinessUnitID = i.BusinessUnitId
                }));

                //ownershipTopModel.Items.AddRange(root.items.ConvertAll<D3sOwnershipModel>(i => new D3sOwnershipModel
                //{
                //    SourceID = i.SourceID,
                //    RoleName = "Business Owner",
                //    UserId = i.BusinessOwnerId,
                //    UserFullName = i.BusinessOwner
                //}));

                //foreach (var app in root.items)
                //{
                //    app.ImpactsOn.items.ForEach(bu =>
                //    {
                //        d3sImpactRelationships.Add(
                //            new D3sBusinesUnitApplicationCatalogRelationshipModel { SubjectSourceID = bu.SourceID, ObjectSourceID = app.SourceID, PredicateType = 7 }
                //        );
                //    });
                //}

                return root;
            };

            while (!string.IsNullOrEmpty(url))
            {
                var models = GetFromApi<IgcBuChildModels>(url, SourceAuthString);
                if (models != null)
                {
                    parse(models);
                    url = models.paging.next;
                }
            }

            var respString = PostJsonToApi(
                $"{TargetUri}TaxonomyType/7/bulk",
                TargetAuthString,
                JsonConvert.SerializeObject(arr)
            );
        }

        public static void GetBuLevel7()
        {
            var properties = "short_description,long_description,$BusinessUnitId,$BusinessOwnerId,$BusinessOwner".Split(',');
            var url = $"{SourceUri}search/?pageSize=75&types=$BUOrg-BusinessUnitLevel7";
            foreach (var p in properties)
            {
                url += $"&properties={p}";
            }

            var arr = new List<D3sBuChildModel>();

            Func<IgcBuChildModels, IgcBuChildModels> parse = delegate (IgcBuChildModels root)
            {
                arr.AddRange(root.items.ConvertAll<D3sBuChildModel>(i => new D3sBuChildModel
                {
                    SourceID = i.SourceID,
                    ParentSourceID = i._context[5]._id,
                    Name = i.Name,
                    ShortDescription = i.ShortDescription,
                    LongDescription = i.LongDescription,
                    BusinessOwner = i.BusinessOwner,
                    BusinessOwnerId = i.BusinessOwnerId,
                    BusinessUnitID = i.BusinessUnitId
                }));

                //ownershipTopModel.Items.AddRange(root.items.ConvertAll<D3sOwnershipModel>(i => new D3sOwnershipModel
                //{
                //    SourceID = i.SourceID,
                //    RoleName = "Business Owner",
                //    UserId = i.BusinessOwnerId,
                //    UserFullName = i.BusinessOwner
                //}));

                //foreach (var app in root.items)
                //{
                //    app.ImpactsOn.items.ForEach(bu =>
                //    {
                //        d3sImpactRelationships.Add(
                //            new D3sBusinesUnitApplicationCatalogRelationshipModel { SubjectSourceID = bu.SourceID, ObjectSourceID = app.SourceID, PredicateType = 7 }
                //        );
                //    });
                //}

                return root;
            };

            while (!string.IsNullOrEmpty(url))
            {
                var models = GetFromApi<IgcBuChildModels>(url, SourceAuthString);
                if (models != null)
                {
                    parse(models);
                    url = models.paging.next;
                }
            }

            var respString = PostJsonToApi(
                $"{TargetUri}TaxonomyType/7/bulk",
                TargetAuthString,
                JsonConvert.SerializeObject(arr)
            );
        }

        #endregion
    }
}
