using d360.core;
using d360.core.entities;
using d360.extensions.caching;
using d360.extensions.info;
using d360.extensions.queue;
using d360.extensions.storage;
using d360.model;
using Dapper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;

namespace igx.tests
{
    public class TestJson
    {
        [JsonProperty("$SomeProperty")]
        public int SomeProperty { get; set; }
    }


    [TestClass]
    public class RunJobsTest: BaseTest
    {
        [TestMethod]
        public void TestJsonMetaPropertyParsing()
        {
            var json = "{ $SomeProperty: 123 }";
            var obj = JsonConvert.DeserializeObject<TestJson>(json, new JsonSerializerSettings { MetadataPropertyHandling = MetadataPropertyHandling.Ignore });
            Assert.IsTrue(obj.SomeProperty == 123);
        }

        [TestMethod]
        public void DeployFusionConnector()
        {
            var companyID = 54; //10
            var fusionTypeID = 1;//13;
            var community = new CommunityContext(new DummyCachingProvider(), new AzureQueueSource(), new UriSecurityContextProvider());

            var fusionType = community.GetById<d360.core.entities.Plugins.FusionType>(fusionTypeID, i => i.FusionTypeFields);
            var fusionAttributeTypes = community.Filter<d360.core.entities.Plugins.FusionAttributeType>(i => i.FusionTypeID == fusionTypeID, i => i.FusionAttributeTypeFields).ToList();
            var fusionIntersectTypes = community.Filter<d360.core.entities.Plugins.FusionIntersectType>(i => i.FusionTypeID == fusionTypeID).ToList();

            var company = getCompanyConnection(companyID);

            company.Execute(@"SET IDENTITY_INSERT FusionType ON
if not exists(select 1 from FusionType where ID = @i) BEGIN INSERT INTO FusionType (ID, Name, Description, UpdatedOn, UpdatedBy) VALUES (@i, @n, @d, @dt, @u) END
SET IDENTITY_INSERT FusionType OFF", new { i = fusionType.ID, n = fusionType.Name, d = fusionType.Description, dt = DateTime.UtcNow, u = 0 });

            foreach (var o in fusionType.FusionTypeFields)
            {
                var oID = company.Execute(@"
                    declare @ft int
                    if not exists(select 1 from FieldType where Name = @n and [Object] = 'FusionType' and ObjectID = @oid) 
                    BEGIN 
                    INSERT INTO FieldType (Name, FriendlyName, [Type], [Object], ObjectID, SortOrder, IsRequired, IsListable, IsDisplayable, IsEditable, Category) VALUES (@n, @f, @t, 'FusionType', @oid, @s, 0, @l, @d, @e, @cat)
                    END",
                new { n = o.Name, f = o.FriendlyName, t = o.Type, oid = o.FusionTypeID, s = o.SortOrder, l = o.IsListable, d = true, e = true, cat = o.Category });
            }

            loadFusionAttributeTypes(company, fusionType.ID, "FusionAttributeType", null, fusionAttributeTypes);

            fusionIntersectTypes.ForEach(t => {
                company.Execute(@"
if not exists(select 1 from IntersectType where Subject = @type and SubjectID = @si and Object = @type and ObjectID = @ti) 
BEGIN 
        declare @predicateID int
        select  top 1 
                @predicateID = ID 
        from    [Predicate] where [Type] = @pt 
		INSERT INTO IntersectType (Subject, SubjectID, Object, ObjectID, UpdatedOn, UpdatedBy, IsSystem, CreatedBy, CreatedOn, PredicateID) 
        values (@type, @si, @type, @ti, @dt, @u, @system, @u, @dt, @predicateID)
END", 
                new { type = "FusionAttributeType", si = t.StartFusionAttributeTypeID, ti = t.EndFusionAttributeTypeID, system = true, ro = t.ReadOnly, dt = DateTime.UtcNow, u = 0, pt = t.PredicateType });
            });
        }

        void loadFusionAttributeTypes(SqlConnection company, int fusionTypeID, string type, int? parentID, List<d360.core.entities.Plugins.FusionAttributeType> types)
        {
            types.Where(i => i.ParentID == parentID).ToList().ForEach(t =>
            {
                company.Execute(@"
if not exists(select 1 from FusionAttributeType where ID = @i) 
BEGIN 
SET IDENTITY_INSERT FusionAttributeType ON
INSERT INTO FusionAttributeType (ID, ParentID, FusionTypeID, Name, Assignable) VALUES (@i, @p, @t, @n, 0) 
SET IDENTITY_INSERT FusionAttributeType OFF
END",
                new { i = t.ID, p = parentID, t = fusionTypeID, n = t.Name });

                foreach (var o in t.FusionAttributeTypeFields)
                {
                    var oID = company.Execute(@"
                    declare @ft int
                    if not exists(select 1 from FieldType where Name = @n and [Object] = 'FusionAttributeType' and ObjectID = @oid) 
                    BEGIN 
                    INSERT INTO FieldType (Name, FriendlyName, [Type], [Object], ObjectID, SortOrder, IsRequired, IsListable, IsEditable) VALUES (@n, @f, @t, 'FusionAttributeType', @oid, @s, 0, @l, @e) 
                    END 
                    ",
                    new { n = o.Name, f = o.FriendlyName, t = o.Type, oid = o.FusionAttributeTypeID, s = o.SortOrder, l = o.IsListable, e = false });
                }

                loadFusionAttributeTypes(company, fusionTypeID, type, t.ID, types);
            });
        }

        [TestMethod]
        public void DeployDatabaseChanges()
        {
            #region SQL
            var sql = @"";

            #endregion
            var list = getCompanies().ToList();
            list.ForEach(id =>
            {
                var cnn = getCompanyConnection(id);
                cnn.Open();
                cnn.Execute(sql);
                cnn.Close();
                cnn.Dispose();
            });
        }

        [TestMethod]
        public void GetFusionTypesByCompany()
        {
            var clientFusionTypes = new List<d360.core.entities.Plugins.ClientFusionType>();
            var cnn = new SqlConnection(constants.COMMUNITY_DATABASE_CONNECTION);
            cnn.Open();

            var list = cnn.Query<Company>("select * from Company").ToList();

            list.ForEach(c =>
            {
                var company = getCompanyConnection(c.ID);
                company.Open();
                var fusionTypeIDs = company.Query<int>("select ID from FusionType where ID < 50000").ToList();
                company.Close();
                company.Dispose();

                clientFusionTypes.AddRange(
                    fusionTypeIDs.Select(i => 
                    new d360.core.entities.Plugins.ClientFusionType {
                        ClientID = c.ClientID,
                        FusionTypeID = i
                    })
                    .Except(clientFusionTypes)
                );
            });

            clientFusionTypes.Distinct().ToList().ForEach(ft =>
            {
                cnn.Execute(@"
MERGE
	INTO    plugin.ClientFusionType T
	USING   (
			SELECT	@c as ClientID, 
                    @f as FusionTypeID
			) S
	ON      (S.ClientID = T.ClientID and S.FusionTypeID = T.FusionTypeID) 
WHEN NOT MATCHED THEN
	INSERT  (ClientID, FusionTypeID)
	VALUES  (S.ClientID, S.FusionTypeID);", new { c = ft.ClientID, f = ft.FusionTypeID });
            });

            cnn.Close();
            cnn.Dispose();
        }

        [TestMethod]
        public void ParseResponsibilityRule()
        {
            //var clientFusionTypes = new List<d360.core.entities.Plugins.ClientFusionType>();
            //var cnn = new SqlConnection(constants.COMMUNITY_DATABASE_CONNECTION);
            //cnn.Open();

            //var list = cnn.Query<Company>("select * from Company").ToList();

            //list.ForEach(c =>
            //{
                var company = getCompanyConnection(4);
                company.Open();
            company.ProcessResponsibilityRelationRules(45);
                //var fusionTypeIDs = company.Query<int>("select ID from FusionType where ID < 50000").ToList();
                company.Close();
                company.Dispose();

            //});

            //cnn.Close();
            //cnn.Dispose();
        }


        [TestMethod]
        public void SaveCertificate_Success()
        {
            var companyID = 6;
            var sec = new UriSecurityContextProvider() { CompanyID = companyID, ResourceID = 1 };
            var community = new CommunityContext(new DummyCachingProvider(), new AzureQueueSource(), sec);

            var bytes = File.ReadAllBytes("SST Test Certificate - AAD.cer");//("SecAuth3Pubcert.cer");
            var dc = new DomainCertificate { Name = "SST Test Certificate - AAD - 2018-19", File = bytes };
            community.Add<DomainCertificate>(dc);
        }

        [TestMethod]
        public void SaveExportTemplate()
        {
            //var companyID = 74; //10
            var community = new CommunityContext(new DummyCachingProvider(), new AzureQueueSource(), new UriSecurityContextProvider()) { CurrentCompanyID = 74, CurrentResourceID = 0, CurrentResourceIsAdmin = true };
            var company = new CompanyContext(community, new DummyCachingProvider(), new AzureQueueSource(), new UriSecurityContextProvider() { CompanyID = 74, ResourceID = 0, CompanyPrefix = "lmtom", IsAdministrator = true }, true);//getCompanyConnection(companyID);

            var bytes = File.ReadAllBytes("Export.xlsx");
            var export = new AssetTypeExportTemplate { Name = "Data Guidance", TemplateFile = bytes, AssetTypeID = 10, ExportViewType = d360.core.enums.ExportView.Grouped, IncludeUrl = false, IncludeParent = false, UpdatedBy = 0, UpdatedOn = DateTime.UtcNow, IncludeFields = "0" };
            company.Add(export);
        }

        [TestMethod]
        public void ReleaseBlobFileLock_Success()
        {
            var folder = "d3s-searchindex-10";
            var file = "write.lock";

            var storage = new AzureStorageProvider();
            Assert.IsTrue(storage.ReleaseLockOnBlobFile(folder, file));
        }

        [TestMethod]
        public void DeployFusionLookupEagleStarTagData()
        {
            var companyID = 22;

            var company = getCompanyConnection(companyID);

            //var starTagFieldTypeID = 51622;
            var starTagFieldTypeID = company.Query<int>("select id from fieldtype where name = 'startag' and[object] = 'FusionAttributeType' and objectid = 205").FirstOrDefault();

            if (starTagFieldTypeID <= 0) throw new Exception("Unable to find StarTag field associated with fusionattribute 205.");

            //delete fields with null star tags or star tags that are not numbers
            //delete from field where (value = '' or IsNumeric(value) = 0) and objecttype = 'FusionAttribute' and fieldtypeid = (select id from fieldtype where name = 'startag' and[object] = 'FusionAttributeType' and objectid = 205)
            company.Execute("delete from field where (value = '' or IsNumeric(value) = 0) and objecttype = 'FusionAttribute' and fieldtypeid = (select id from fieldtype where name = 'startag' and[object] = 'FusionAttributeType' and objectid = 205)");

            //load id's of fields that dont have star tags         
            var fusionAttributes = company.Query<dynamic>(@"select
                                                fa.id,fa.name from fusionattribute fa where fa.fusionattributetypeid = 205 and fa.sourceid like 'security%'
                                                            and
                                        not exists(select 1 from field f where fa.id = f.objectid and f.objecttype = 'fusionattribute' and f.fieldtypeid = (select id from fieldtype where name = 'startag' and[object] = 'FusionAttributeType' and objectid = 205))").ToList();

            //if there are none we are done
            if (fusionAttributes.Count == 0) return;

            //load all the star tags from the community
            var eagleStarTagMap = LoadEagleStarTagsFromCommunity();

            //need to find tag for given column
            foreach (var item in fusionAttributes)
            {
                // try to get star tag for that name from the map
                var fieldName = (string)item.name;
                var fusionAttributeId = (int)item.id;
                var tag = string.Empty;

                //if found add to the fields 
                if (!eagleStarTagMap.TryGetValue(fieldName, out tag)) continue;

                //add an entry to field table for this fusion attribute for the star tag field type
                company.Execute(@"                            
			                      INSERT INTO [Field] ([ObjectType], [ObjectID], [FieldTypeID], [Value]) values ('FusionAttribute', @id, @fieldId, @val)
                            ",
                    new { type = "FusionAttributeType", id = fusionAttributeId, val = tag, fieldId = starTagFieldTypeID });
            }       
            
        }

        private Dictionary<string,string> LoadEagleStarTagsFromCommunity()
        {
            Dictionary<string, string> hash = new Dictionary<string, string>();

            var community = new CommunityContext(new DummyCachingProvider(), new AzureQueueSource(), new UriSecurityContextProvider());

            var mapping = community.Query<dynamic>(@"
                    select 
	                    flv.[key],
	                    flv.[value]
                    FROM 
	                    [plugin].[FusionLookupType] flt
	                    inner join [plugin].[fusionlookupvalue] flv on(flt.id = flv.fusionlookuptypeid)
                    where
	                    flt.name = 'EagleStarTags'
                 ").ToList();

            foreach (var item in mapping)
            {
                hash.Add(item.key, item.value);
            }
            return hash;
        }

        public class WebActivityEntity : TableEntity
        {
            public string Activity { get; set; }

            public int ObjectId { get; set; }

            public string ObjectName { get; set; }

            public int ResourceID { get; set; }

            public string ResourceName { get; set; }

            public string IP { get; set; }

            public string UserAgent { get; set; }

            public string Path { get; set; }

            public string Host { get; set; }

            public string BrowserLanguages { get; set; }
        }

        //[TestMethod]
        //public void MoveWebAnalyticsFromTableStorageToDb()
        //{
        //    var storageAccount = CloudStorageAccount.Parse(d360.core.constants.WEBJOBS_STORAGE_CONNECTION);
        //    var tableClient = storageAccount.CreateCloudTableClient();

        //    var companyIDs = getCompanies(true);

        //    companyIDs.AsParallel().ForAll(companyID =>
        //    {
        //        try
        //        {
        //            //var companyID = 15;
        //            var table = tableClient.GetTableReference($"WebLogs{companyID}");

        //            var qry = new TableQuery<WebActivityEntity>();
        //            qry.Take(10000);
        //            var list = table.ExecuteQuery<WebActivityEntity>(qry);

        //            var company = getCompanyConnection(companyID);
        //            foreach (var item in list)
        //            {
        //                var okToDelete = false;
        //                try
        //                {
        //                    SystemObjects ot;

        //                    if (Enum.TryParse(item.ObjectName, true, out ot))
        //                    {
        //                        company.Execute(
        //                            @"analytics.AddStatistic @Object, @ObjectID, @Ip, @UserAgent, @Host, @BrowserLanguage, @Action, @ResourceID, @Timestamp",
        //                            new
        //                            {
        //                                Object = new Dapper.DbString { Value = ot.ToString(), IsAnsi = false, IsFixedLength = false, Length = 50 },
        //                                ObjectID = item.ObjectId,
        //                                UserAgent = item.UserAgent,
        //                                Ip = new Dapper.DbString { Value = item.IP, IsAnsi = false, IsFixedLength = false, Length = 100 },
        //                                Host = new Dapper.DbString { Value = item.Host, IsAnsi = false, IsFixedLength = false, Length = 50 },
        //                                BrowserLanguage = new Dapper.DbString { Value = item.BrowserLanguages, IsAnsi = false, IsFixedLength = false, Length = 500 },
        //                                Action = new Dapper.DbString { Value = item.Activity, IsAnsi = false, IsFixedLength = false, Length = 50 },
        //                                ResourceID = item.ResourceID,
        //                                Timestamp = item.Timestamp.Date
        //                            });
        //                        okToDelete = true;
        //                    }
        //                    else
        //                    {
        //                        okToDelete = true;
        //                    }

        //                    if (okToDelete)
        //                    {
        //                        var deleteOperation = TableOperation.Delete(item);
        //                        table.Execute(deleteOperation);
        //                    }
        //                }
        //                catch (Exception ex)
        //                {
        //                }
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //        }
        //    });
        //}

        [TestMethod]
        public void CreateKey()
        {
            var crypto = new RNGCryptoServiceProvider();

            var buff = new byte[128];
            crypto.GetBytes(buff);
            var valKey = BytesToHexString(buff);

            buff = new byte[64];
            crypto.GetBytes(buff);
            var decKey = BytesToHexString(buff);
        }

        string BytesToHexString(byte[] bytes)
        {
            var hexString = new StringBuilder(64);

            for (int counter = 0; counter < bytes.Length; counter++)
            {
                hexString.Append(String.Format("{0:X2}", bytes[counter]));
            }
            return hexString.ToString();
        }
    }
}
