using d360.core;
using d360.core.entities;
using d360.core.enums;
using d360.core.helpers;
using d360.utils.company;
using Dapper;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Data.Entity.Design.PluralizationServices;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace igx.jobs.reportlayer
{
    class Program
    {
        static async Task Main()
        {
            var builder = CoreFunction.JobHostConfigBuilder();
            builder.ConfigureWebJobs(c =>
            {
                c.AddAzureStorageCoreServices()
                .AddAzureStorage()
                .AddTimers();
            });

            using (var host = builder.Build())
            {
                await host.RunAsync();
            }
        }
    }

    public static class ReportLayerGenerator
    {
        #region Utility

        static string cleanObjectName(string name)
        {
            name = name.Replace("'", "").Replace(" ", "").Replace("-", "").Replace("&", "And").Replace(":", "").Replace(";", "").Trim();
            Regex rgx = new Regex("[^a-zA-Z0-9-]");
            name = rgx.Replace(name, "");
            return name;
        }

        static void getDynamicFieldJoinStatements(List<FieldType> fields, string type, out string joins, out string columns, List<string> reservedNames = null, string idColumn = "A.ID")
        {
            columns = "";
            joins = "";

            var typesToIgnore = new List<string> {
                DataType.Path.ToString(), DataType.Color.ToString(), DataType.ComplexRelationLookup.ToString(), DataType.DataTableSelect.ToString(),
                DataType.File.ToString(), DataType.Hidden.ToString(), DataType.OwnershipLookup.ToString(),
                DataType.Password.ToString(), DataType.RefListRelationship.ToString(), DataType.UncLink.ToString(), DataType.JsonElement.ToString()
            };

            fields.RemoveAll(i => typesToIgnore.Contains(i.Type));

            var usedNames = new List<string>();
            foreach (var f in fields)
            {
                var name = cleanObjectName(f.Name);
                var alias = getAliasedName(name, reservedNames);

                if (!usedNames.Contains(name.ToLowerInvariant()))
                {
                    columns += (f.Type == "Lookup") ? $"[{name}].Value as [{alias}ID], [{name}].FormattedValue as [{alias}], " : $"[{name}].FormattedValue as [{alias}], ";
                    joins += $" left join FieldDetail [{name}] on [{name}].AssetID = {idColumn} and [{name}].FieldTypeID = {f.ID}";
                    usedNames.Add(name.ToLowerInvariant());
                }
            }

            fields = null;
        }

        static string getAliasedName(string name, List<string> reservedNames)
        {
            if (reservedNames == null)
                return name;
            else if (!reservedNames.Contains(name.ToLower()))
                return name;
            else
            {
                if (int.TryParse(name.Last().ToString(), out int i))
                    return name.Substring(0, name.Length - 1) + (i + 1).ToString();
                else
                    return name + "2";
            }
        }

        static void executeSqlWithTry(SqlConnection companyConnection, string viewSql)
        {
            try
            {
                companyConnection.Execute(viewSql.ToString());
            }
            catch (Exception ex)
            {
                CoreFunction.AITrackException(functionName, ex, null, new Dictionary<string, string>() { { "Attempted SQL: ", viewSql } });
            }
        }

        #endregion

        const string functionName = "ReportingLayer_Generate";

#if DEBUG
        const string timerSettings = "*/1 * * * * *";
#else
        const string timerSettings = "0 */5 * * * *";
#endif

        public static void Run([TimerTrigger(timerSettings, RunOnStartup = true)]TimerInfo myTimer, TextWriter log)
        {
            try
            {
                var companies = CoreFunction.GetCompaniesByCurrentSlot();

#if DEBUG
                companies = companies.Where(i => i.CompanyID == 3).ToList();
#endif

                companies.ForEach(c =>
                {
                    var reservedNames = new List<string>() { "id", "uid", "assetid", "displayvalue", "parentid", "textpath", "level", "levelname", "leveldescription", "parentdisplayvalue", "currentscore", "url" };

                    var synonymNames = new List<string>();
                    var viewNames = new List<string>();
                    string SCHEMA = "reporting";

                    try
                    {
                        using (var companyConnection = CompanyConnectionUtils.GetCompanyConnection(c.CompanyID, c.Server, c.Username, c.Password))
                        {
                            companyConnection.Open();

                            var selectSql = "";
                            var objectName = "";
                            var assetTypePluralizedName = "";
                            var legacyPrefix = "Glossary";
                            var allAssetTypes = companyConnection.Query<AssetType>($"select * from dbo.AssetType  where[Class] in ({ (int)AssetTypeClass.BusinessAsset}, { (int)AssetTypeClass.TechnicalAsset}, { (int)AssetTypeClass.Model}, { (int)AssetTypeClass.Policy})").ToList();

                            //Generate required view names
                            foreach(var at in allAssetTypes)
                            {
                                if (PluralCultureHelper.IsNeutralCultureEnglish())
                                {
                                    var pluralize = PluralizationService.CreateService(System.Globalization.CultureInfo.CurrentCulture);
                                    assetTypePluralizedName = pluralize.Pluralize(cleanObjectName(at.Name));
                                }


                                if (assetTypePluralizedName.Length > 100)
                                    assetTypePluralizedName = assetTypePluralizedName.Substring(0, 100);

                                objectName = $"{SCHEMA}.[{at.Class.ToString()}_{assetTypePluralizedName}]";

                                viewNames.Add(objectName);
                            }


                            //Get only changed assettypes
                            var changedAssetTypes = GetChangedAssetTypeUids(companyConnection);
                            var dbArgs = new DynamicParameters();
                            dbArgs.Add("@assetTypeUids", changedAssetTypes);

                            var assetTypes = allAssetTypes.Where(x => changedAssetTypes.Contains(x.uid)).ToList();
                            var fieldTypes = companyConnection.Query<FieldType>($@"select FT.* from FieldType FT
                                                    inner join AssetType AT on AT.Id = FT.AssetTypeID
                                                        where AT.uid in @assetTypeUids", dbArgs).ToList();

                            assetTypes.ForEach(o =>
                            {
                                var joins = "";
                                var columns = "";

                                if (PluralCultureHelper.IsNeutralCultureEnglish())
                                {
                                    var pluralize = PluralizationService.CreateService(System.Globalization.CultureInfo.CurrentCulture);
                                    assetTypePluralizedName = pluralize.Pluralize(cleanObjectName(o.Name));
                                }


                                if (assetTypePluralizedName.Length > 100)
                                    assetTypePluralizedName = assetTypePluralizedName.Substring(0, 100);

                                objectName = $"{SCHEMA}.[{o.Class.ToString()}_{assetTypePluralizedName}]";

                                var skippedFieldTypes = DataType.Text.GetNotAllowedInReportingViews();

                                // Get fields for asset
                                getDynamicFieldJoinStatements(
                                    fieldTypes.Where(f => f.AssetTypeID == o.ID && !skippedFieldTypes.Contains(f.Type)).ToList(),
                                    o.Object.Replace("Type", ""),
                                    out joins,
                                    out columns,
                                    reservedNames,
                                    "A.ID");


                                if (o.Class == AssetTypeClass.BusinessAsset || o.Class == AssetTypeClass.TechnicalAsset)
                                {
                                    #region Business/Technical

                                    var parentIntersectType = companyConnection.Query<IntersectTypeDetail>($"select * from IntersectTypeDetail where Object = '{o.Object}' and ObjectID = {o.ObjectID} and PredicateType = @pt", new { id = o.ObjectID, pt = (int)PredicateType.InterTypeHierarchy }).FirstOrDefault();

                                    var parentSqlColumn = @"";
                                    var parentSqlJoin = @"";

                                    if (parentIntersectType != null)
                                    {
                                        parentSqlColumn = @"P.ParentID, P.DisplayValue as ParentDisplayValue, ";
                                        parentSqlJoin = @" outer apply (
				    select	I.SubjectID as ParentID,
                            ID.DisplayValue
				    from	[PredicateIntersect] I
                            inner join Asset IA on I.Object = A.Object and I.ObjectID = A.ObjectID and IA.Object = 'Artifact' and IA.ObjectID = I.SubjectID and I.PredicateType = 3
                            inner join AssetType IAT on IAT.ID = IA.AssetTypeID
                            left join dbo.AssetDisplayValue ID on ID.AssetID = IA.ID
				    ) P";
                                    }


                                    selectSql = $@"
select  A.ObjectID as ID,
        A.ID as AssetID,
        A.DisplayValue, 
        {parentSqlColumn}
        {columns} 
        dbo.GenerateAssetUrl(A.ID) as Url, 
        cast(S.Value * 100 as int) as CurrentScore 
from    AssetDetail A 
		left join metrics.Score S on S.AssetUid = A.[Uid] and S.EndDate is null
        left join metrics.Allocation Al on Al.Uid = S.AllocationUid and Al.ScoreType = 1
        {joins} 
        {parentSqlJoin} 
where   A.Type = '{o.Object}' and A.TypeID = {o.ObjectID}";

                                    #endregion Business
                                }
                                else
                                {
                                    #region Model/Policy

                                    selectSql = $@"
with h as (
	select	A.ID as AssetID,
			A.[Uid],
			A.ObjectID as ID,
			A.AssetTypeID,        
			null as ParentID,
			A.DisplayValue as TextPath,
			1 as [Level]
	from	AssetDetail A
			left join PredicateIntersect I on I.Object = A.Object and I.ObjectID = A.ObjectID and I.PredicateType = 4
	where	A.[Type] = '{o.Object}' and A.TypeID = {o.ObjectID}
			and I.IntersectID is null
	union all
	select	C.ID as AssetID,
			C.[Uid],
			C.ObjectID as ID,
			C.AssetTypeID,
			P.ID as ParentID,
			P.TextPath + '/' + C.DisplayValue as TextPath,
			P.[Level] + 1 as [Level]
	from	AssetDetail C
			inner join PredicateIntersect I on I.Object = C.Object and I.ObjectID = C.ObjectID and I.PredicateType = 4
			inner join h as P on I.Subject = C.Object and I.SubjectID = P.ID
	where	C.[Type] = '{o.Object}' and C.TypeID = {o.ObjectID}
)

select  A.[Uid],
		A.ID, 
        A.ParentID, 
        A.TextPath, 
        A.[Level], 
        L.Name as LevelName, 
        L.Description as LevelDescription,
        {columns} 
        dbo.GenerateAssetUrl(A.AssetID) as Url, 
        cast(S.Value * 100 as int) as CurrentScore
from    h as A  
        {joins} 
		left join metrics.Score S on S.AssetUid = A.[Uid] and S.EndDate is null
        left join metrics.Allocation Al on Al.Uid = S.AllocationUid and Al.ScoreType = 1
        left join AssetTypeLevel L on L.AssetTypeID = A.AssetTypeID and L.[Level] = A.[Level]";

                                    #endregion Model/Policy
                                }

                                executeSqlWithTry(companyConnection, $@"CREATE OR ALTER VIEW {objectName} AS {selectSql}");

                                if (o.Class == AssetTypeClass.BusinessAsset)
                                {
                                    var synonymName = $"{SCHEMA}.{legacyPrefix}_{assetTypePluralizedName}";
                                    synonymNames.Add(synonymName);
                                    executeSqlWithTry(companyConnection, $@"
if (not exists(select * from sys.synonyms where name = '{legacyPrefix}_{assetTypePluralizedName}')
and not exists(select * from sys.views where name = '{legacyPrefix}_{assetTypePluralizedName}')
)
BEGIN
	CREATE SYNONYM {synonymName} FOR {objectName}
END");
                                }
                            });

                            #region General Views

                            #region REPORING USERS (dynamic)

                            fieldTypes = companyConnection.Query<FieldType>("select * from FieldType where [Object] = 'ResourceType'").ToList();
                            var fjoins = string.Empty;
                            var ffields = string.Empty;
                            fieldTypes.ForEach(f =>
                            {
                                fjoins += $@" left join field as [Type_{f.ID}] on 
                                        [Type_{f.ID}].fieldtypeId={f.ID} and [Type_{f.ID}].ObjectType='Resource' and [Type_{f.ID}].ObjectId=r.resourceid ";
                                ffields += $@",[Type_{f.ID}].FormattedValue as [{f.FriendlyName}]";
                            });

                            objectName = $"{SCHEMA}.[Users]";
                            viewNames.Add(objectName);

                            selectSql = $@"select 
                                    r.FirstName ,
                                    r.LastName ,
                                    r.Email, 
                                    r.ResourceID,
                                    '/Resource/' + cast(r.ResourceID as varchar(250)) as ResourceURI,
                                    r.LastLoggedInOn as DateLastLoggedIn, 
                                    case when r.[State] = 1 then 'Active' else 'Inactive' end as [Status], 
                                    r.LastLoggedInOn, 
                                    r.[State], 
                                    r.IsAdministrator
                                    {ffields}
                                    from reporting.Global_Resource as r
                                    {fjoins}";

                            executeSqlWithTry(companyConnection, $@"CREATE OR ALTER VIEW {objectName} AS {selectSql}");

                            #endregion

                            #endregion

                            RemoveOldDynamicViews(companyConnection, AssetTypeClass.BusinessAsset, viewNames, log);
                            RemoveOldDynamicViews(companyConnection, AssetTypeClass.TechnicalAsset, viewNames, log);
                            RemoveOldDynamicViews(companyConnection, AssetTypeClass.Model, viewNames, log);
                            RemoveOldDynamicViews(companyConnection, AssetTypeClass.Policy, viewNames, log);

                            RemoveSynonyms(companyConnection, synonymNames, log, SCHEMA);
                        }
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

        private static List<Guid> GetChangedAssetTypeUids(SqlConnection companyConnection)
        {
            var sql = $@"drop table if exists #hashTable
                                    create  table #hashTable (
	                                    Uid uniqueidentifier,
	                                    OldHash binary(20),
	                                    NewHash binary(20)
                                    )

                                    INSERT INTO #hashTable
                                    SELECT
	                                    at.uid,	 
	                                    at.HashValue as OldHashValue,
	                                    HASHBYTES('SHA1',(
                                            SELECT   [ID]
			                                    ,[Name]
			                                    ,[Description]
			                                    ,[Class]
			                                    ,[DisplayFormat]
			                                    ,[State]
			                                    ,[Hierarchical]
			                                    ,[HierarchyPredicateID]
			                                    ,[HierarchyIntersectTypeID]
			                                    ,[HierarchyMaximumDepth]
			                                    ,[Object]
			                                    ,[ObjectID]
			                                    ,[CreatedOn]
			                                    ,[CreatedBy]
			                                    ,[UpdatedOn]
			                                    ,[UpdatedBy]
			                                    ,[Notes]
			                                    ,[uid]
			                                    ,[AutoDisplayDescription]
			                                    ,[UseAsTransformation]
			                                    ,F.FieldXML as FieldXml
                                            FROM    [dbo].AssetType as HashingTable 
		                                    CROSS APPLY (select * from FieldType where AssetTypeID = HashingTable.ID for xml path)F(FieldXML)
                                            where   HashingTable.ID = AT.id 
                                            FOR XML RAW
                                        ))  as  NewHashValue
                                    from dbo.AssetType as AT
                                    where [Class] in ({(int)AssetTypeClass.BusinessAsset}, {(int)AssetTypeClass.TechnicalAsset}, {(int)AssetTypeClass.Model}, {(int)AssetTypeClass.Policy})
                                    

                                    select uid from #hashTable 
                                       where NewHash <> OldHash or OldHash is null


                                    MERGE dbo.AssetType as AT
                                    USING #hashTable as HT
                                    ON AT.uid = HT.Uid and (HT.NewHash <> HT.OldHash or HT.OldHash is null)
                                    WHEN MATCHED
	                                    THEN UPDATE SET AT.HashValue = HT.NewHash;";

            return companyConnection.Query<Guid>(sql).ToList();
        }

        private static void RemoveSynonyms(SqlConnection companyConnection, List<string> synonymNames, TextWriter log, string schemaName)
        {
            var currentSynonyms = companyConnection.Query<string>(@"select name from sys.synonyms where base_object_name like '%reporting%' and base_object_name not in (select '[' + TABLE_SCHEMA + '].[' + TABLE_NAME + ']' from [INFORMATION_SCHEMA].[VIEWS] where TABLE_SCHEMA = 'reporting')").ToList();

            currentSynonyms.ForEach(cv =>
            {
                cv = $"{schemaName}.{cv}";

                if (!synonymNames.Contains(cv))
                {
                    try
                    {
                        companyConnection.Execute(string.Format(@"drop synonym {0}", cv));
                    }
                    catch (Exception ex)
                    {
                        var msg = ex.GetFullExceptionData() + " Stack: " + ex.StackTrace;
                        log.WriteLine(msg);
                    }
                }
            });

        }

        private static void RemoveOldDynamicViews(SqlConnection connection, AssetTypeClass className, List<string> viewNames, TextWriter log)
        {
            var currentViewNames = connection.Query<string>($@"select TABLE_SCHEMA + '.[' + TABLE_NAME + ']' from [INFORMATION_SCHEMA].[VIEWS] where TABLE_SCHEMA = 'reporting' and TABLE_NAME like '{className}_%' and TABLE_NAME not in('model_all','model_fields', 'ModelInterRelationships','policy_all')").ToList();
                        
            currentViewNames.ForEach(cv =>
            {
                if (!viewNames.Contains(cv))
                {
                    try
                    {
                        connection.Execute(string.Format(@"drop view {0}", cv));
                    }
                    catch (Exception ex)
                    {
                        var msg = ex.GetFullExceptionData() + " Stack: " + ex.StackTrace;
                        log.WriteLine(msg);
                    }
                }
            });
        }
    }
}
