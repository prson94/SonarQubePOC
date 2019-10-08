using d360.core;
using d360.core.entities;
using d360.core.enums;
using d360.core.helpers;
using d360.utils.company;
using Dapper;
using Microsoft.Azure.WebJobs;
using Microsoft.Practices.EnterpriseLibrary.TransientFaultHandling;
using System;
using System.Collections.Generic;
using System.Data.Entity.Design.PluralizationServices;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace igx.jobs.reportlayer
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
                DataType.Attribute.ToString(), DataType.Color.ToString(), DataType.ComplexRelationLookup.ToString(), DataType.DataTableSelect.ToString(),
                DataType.File.ToString(), DataType.FilteredLookup.ToString(), DataType.Hidden.ToString(), DataType.OwnershipLookup.ToString(),
                DataType.Password.ToString(), DataType.RefListRelationship.ToString(), DataType.UncLink.ToString(), DataType.JsonElement.ToString()
            };

            fields.RemoveAll(i => typesToIgnore.Contains(i.Type));

            var usedNames = new List<string>();
            foreach (var f in fields)
            {
                var name = cleanObjectName(f.Name);
                var alias = getAliasedName(name, reservedNames);

                if (!usedNames.Contains(name))
                {
                    columns += (f.Type == "Lookup") ? $"[{name}].Value as [{alias}ID], [{name}].FormattedValue as [{alias}], " : $"[{name}].FormattedValue as [{alias}], ";
                    joins += $" left join FieldDetail [{name}] on [{name}].AssetID = {idColumn} and [{name}].FieldTypeID = {f.ID}";
                    usedNames.Add(name);
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
        const string timerSettings = "*/5 * * * * *";
#else
        const string timerSettings = "0 */5 * * * *";
#endif

        public static void Run([TimerTrigger(timerSettings, RunOnStartup = true)]TimerInfo myTimer, TextWriter log)
        {
            try
            {
                var companies = CoreFunction.GetCompaniesByCurrentSlot();


                companies.ForEach(c =>
                {
                    var reservedNames = new List<string>() { "id", "uid", "assetid", "displayvalue", "parentid", "textpath", "level", "levelname", "leveldescription", "parentdisplayvalue", "currentscore", "url" };

                    var viewNames = new List<string>();
                    string SCHEMA = "reporting";

                    try
                    {
                        using (var companyConnection = CompanyConnectionUtils.GetCompanyConnection(c.CompanyID, c.Server, c.Username, c.Password))
                        {
                            companyConnection.OpenWithRetry(RetryPolicy.DefaultProgressive);

                            var selectSql = "";
                            var viewSql = "";
                            var objectName = "";
                            var objectType = "Artifact";
                            var prefix = "Glossary";
                            string objectID;

                            var assetTypes = companyConnection.Query<AssetType>($"select * from AssetType where [Class] in ({(int)AssetTypeClass.BusinessAsset}, {(int)AssetTypeClass.TechnicalAsset}, {(int)AssetTypeClass.Model}, {(int)AssetTypeClass.Policy})").ToList();
                            var fieldTypes = companyConnection.Query<FieldType>($"select * from FieldType where AssetTypeID in (select ID from AssetType where [Class] in ({(int)AssetTypeClass.BusinessAsset}, {(int)AssetTypeClass.TechnicalAsset}, {(int)AssetTypeClass.Model}, {(int)AssetTypeClass.Policy}))").ToList();

                            assetTypes.ForEach(o =>
                            {
                                var joins = "";
                                var columns = "";

                                if (PluralCultureHelper.IsNeutralCultureEnglish())
                                {
                                    var pluralize = PluralizationService.CreateService(System.Globalization.CultureInfo.CurrentCulture);
                                    objectName = pluralize.Pluralize(cleanObjectName(o.Name));
                                }


                                if (objectName.Length > 100)
                                    objectName = objectName.Substring(0, 100);

                                objectName = $"{SCHEMA}.[{o.Class.ToString()}_{objectName}]";

                                viewNames.Add(objectName);

                                // Get fields for asset
                                getDynamicFieldJoinStatements(
                                    fieldTypes.Where(f => f.AssetTypeID == o.ID).ToList(),
                                    o.Object.Replace("Type", ""),
                                    out joins,
                                    out columns,
                                    reservedNames,
                                    "A.ID");


                                if (o.Class == AssetTypeClass.BusinessAsset || o.Class == AssetTypeClass.TechnicalAsset)
                                {
                                    #region Business

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
        outer apply (
					select	max(EffectiveDate) as EffectiveDate
					from	[metrics].[Score] 
					where	[AssetUid] = A.[Uid] 
							and EffectiveDate <= getutcdate()
					) MS
		left join metrics.Score S on S.AssetUid = A.[Uid] and S.EffectiveDate = MS.EffectiveDate
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
        outer apply (
					select	max(EffectiveDate) as EffectiveDate
					from	[metrics].[Score] 
					where	[AssetUid] = A.[Uid] 
							and EffectiveDate <= getutcdate()
					) MS
		left join metrics.Score S on S.AssetUid = A.[Uid] and S.EffectiveDate = MS.EffectiveDate
        left join AssetTypeLevel L on L.AssetTypeID = A.AssetTypeID and L.[Level] = A.[Level]";

                                    #endregion Model/Policy
                                }

                                objectID = companyConnection.Query<string>("select OBJECT_ID(@n, 'V')", new { n = objectName }).First();

                                viewSql = (string.IsNullOrEmpty(objectID)) ? "CREATE " : "ALTER ";
                                viewSql += $@" VIEW {objectName} AS {selectSql}";

                                executeSqlWithTry(companyConnection, viewSql);
                            });

                            assetTypes = null;

                            #region General Views

                            #region All Assets

                            objectName = $"{SCHEMA}.[Asset_All]";
                            viewNames.Add(objectName);

                            selectSql = @"
select	A.ID,
		A.[uid],
        A.AssetTypeID,
        T.[uid] as AssetTypeUid,
		A.State,
		A.Object,
		A.ObjectID,
		A.SourceID,
		A.CreatedOn,
		A.CreatedBy,
		A.UpdatedOn,
		A.UpdatedBy,
		T.Class as AssetTypeClass,
		T.Description as AssetTypeDescription,
		T.Name as TypeName,
		T.Object as Type,
		T.ObjectID as TypeID
from	Asset A
		inner join AssetType T on T.ID = A.AssetTypeID";

                            objectID = companyConnection.Query<string>("select OBJECT_ID(@n, 'V')", new { n = objectName }).First();

                            viewSql = (string.IsNullOrEmpty(objectID)) ? "CREATE " : "ALTER ";
                            viewSql += $@" VIEW {objectName} AS {selectSql}";

                            executeSqlWithTry(companyConnection, viewSql);

                            #endregion

                            #region All Metrics

                            objectName = $"{SCHEMA}.[Metric_All]";
                            viewNames.Add(objectName);

                            selectSql = @"
 select	A.AssetTypeUid,
		A.[Uid],
		A.Name,
		A.ParentUid,
		A.IsGroup,
		V.Weight,
		V.EffectiveDate as MetricMaxEffectiveDate,
		(
			select	F.Name as FieldName,
					C.Operator,
					C.ValueJson
			from	[metrics].[AssetVersionCondition] C
					inner join FieldType F on F.ID = C.FieldTypeID
			where	[Uid] = A.[Uid]
					and EffectiveDate = V.EffectiveDate
			for json path
		) as ConditionsJson
from	metrics.AssetVersion V
		inner join metrics.Asset A on A.[Uid] = V.[Uid] 
		inner join (
				select		IA.[Uid],
							max(IV.EffectiveDate) as EffectiveDate
				from		metrics.AssetVersion IV
							inner join metrics.Asset IA on IA.[Uid] = IV.[Uid] 
														and IV.EffectiveDate <= getutcdate()
														and IA.State = 1
				group by	IA.[Uid]
		) MV on MV.[Uid] = V.[Uid] AND MV.EffectiveDate = V.EffectiveDate";

                            objectID = companyConnection.Query<string>("select OBJECT_ID(@n, 'V')", new { n = objectName }).First();

                            viewSql = (string.IsNullOrEmpty(objectID)) ? "CREATE " : "ALTER ";
                            viewSql += $@" VIEW {objectName} AS {selectSql}";

                            executeSqlWithTry(companyConnection, viewSql);

                            #endregion

                            #region All Artifacts

                            objectName = $"{SCHEMA}.[Glossary_All]";
                            viewNames.Add(objectName);

                            selectSql = @"
select  A.ObjectID as ID,
        A.ID as AssetID,
        I.SubjectID as ParentID,
        A.TypeID as ArtifactTypeID,
        A.DisplayValue,
        A.TypeName as ArtifactType,
        A.CreatedOn,
        coalesce(A.UpdatedOn, A.CreatedOn) as UpdatedOn,
		A.KeyHash,
        S.Value as CurrentScore,
        cast(S.Value * 100 as int) as CurrentScorePct
from    AssetDetail A  
		left join PredicateIntersect I on I.Object = 'Artifact' and I.ObjectID = A.ID and I.PredicateType = 3
        outer apply (
					select	max(EffectiveDate) as EffectiveDate
					from	[metrics].[Score] 
					where	[AssetUid] = A.[Uid] 
							and EffectiveDate <= getutcdate()
					) MS
		left join metrics.Score S on S.AssetUid = A.[Uid] and S.EffectiveDate = MS.EffectiveDate
where	A.AssetTypeClass = 1";

                            objectID = companyConnection.Query<string>("select OBJECT_ID(@n, 'V')", new { n = objectName }).First();

                            viewSql = (string.IsNullOrEmpty(objectID)) ? "CREATE " : "ALTER ";
                            viewSql += $@" VIEW {objectName} AS {selectSql}";

                            executeSqlWithTry(companyConnection, viewSql);

                            #endregion

                            #region All Issues

                            objectName = $"{SCHEMA}.[Issue_All]";
                            viewNames.Add(objectName);

                            selectSql = @"
                            select  t.ID as IssueTypeID,
		                            t.Name as IssueTypeName
                                    ,i.[ID] as IssueID
                                    ,i.[CreatedOn]
                                    ,i.[CreatedBy]
                                    ,i.[UpdatedOn]
                                    ,i.[UpdatedBy]
                                    ,ATT.ObjectID as AssetTypeID
		                            ,ATT.Name as AssetTypeName
                                    ,i.ObjectType as [Type]
                                    ,a.ID as AssetID
                                    ,a.[Uid]
                                    ,ATT.[Uid] as AssetTypeUid
                            from    Issue i
                                    inner Join IssueType t on t.ID = i.IssueTypeID
		                            left join AssetType ATT on ATT.[Object] = i.ObjectType and ATT.ObjectID = i.ObjectTypeID
		                            left join Asset A on A.[Object] = i.[Object] and A.[ObjectID] = i.ObjectID";

                            objectID = companyConnection.Query<string>("select OBJECT_ID(@n, 'V')", new { n = objectName }).First();

                            viewSql = (string.IsNullOrEmpty(objectID)) ? "CREATE " : "ALTER ";
                            viewSql += $@" VIEW {objectName} AS {selectSql}";

                            executeSqlWithTry(companyConnection, viewSql);

                            #endregion

                            #region All Fusion Attributes

                            objectName = $"{SCHEMA}.[Fusion_All]";
                            viewNames.Add(objectName);

                            selectSql = @"
select	A.ID as FusionAttributeID,
		A.SourceID,
		O.ID as AssetID,
        A.ParentID as ParentFusionAttributeID,
        A.TextPath as FusionAttributePath,
        A.Name as FusionAttribute,
        F.ID as ConfigurationID,
        F.Name as [Configuration],
        FAT.TextPath as FusionAttributeTypePath,
        A.FusionAttributeTypeID,
        FAT.Name as FusionAttributeType,
        FAT.FusionTypeID,
        FT.Name as FusionType
from	FusionAttribute A
		inner join Asset O on O.Object = 'FusionAttribute' and O.ObjectID = A.ID
        inner join Fusion F on F.ID = A.FusionID
        inner join FusionAttributeType FAT on FAT.ID = A.FusionAttributeTypeID
        inner join FusionType FT on FT.ID = FAT.FusionTypeID";

                            objectID = companyConnection.Query<string>("select OBJECT_ID(@n, 'V')", new { n = objectName }).First();

                            viewSql = (string.IsNullOrEmpty(objectID)) ? "CREATE " : "ALTER ";
                            viewSql += $@" VIEW {objectName} AS {selectSql}";

                            executeSqlWithTry(companyConnection, viewSql);

                            #endregion

                            #region FusionAttribute_Fields

                            objectName = $"{SCHEMA}.[Fusion_Fields]";
                            viewNames.Add(objectName);

                            selectSql = @"
select  O.ID, 
	    F.AssetID,
        O.FusionAttributeTypeID, 
	    O.Name, 
	    O.TextPath, 
	    F.FieldTypeID, 
        FT.Name as FieldName, 
        FT.FriendlyName as FieldFriendlyName, 
	    F.FormattedValue as FieldValue 
from	FusionAttribute O 
	    inner join Field F on F.ObjectType = 'FusionAttribute' and F.ObjectID = O.ID
	    inner join FieldType FT on FT.ID = F.FieldTypeID";

                            objectID = companyConnection.Query<string>("select OBJECT_ID(@n, 'V')", new { n = objectName }).First();

                            viewSql = (string.IsNullOrEmpty(objectID)) ? "CREATE " : "ALTER ";
                            viewSql += $@" VIEW {objectName} AS {selectSql}";

                            executeSqlWithTry(companyConnection, viewSql);

                            #endregion

                            #region All Fusion Query Attributes

                            objectName = $"{SCHEMA}.[FusionQuery_All]";
                            viewNames.Add(objectName);

                            selectSql = @"
select	A.ID as FusionQueryAttributeID,
		--A.DisplayValue as FusionQueryAttribute,
		F.ID as ConfigurationID,
		F.Name as [Configuration],
		A.FusionQueryAttributeTypeID,
		FAT.Name as FusionQueryAttributeType,
		F.FusionTypeID,
		FT.Name as FusionType
from	FusionQueryAttribute A
		inner join FusionQueryAttributeType FAT on FAT.ID = A.FusionQueryAttributeTypeID
		inner join Fusion F on F.ID = FAT.FusionID
		inner join FusionType FT on FT.ID = F.FusionTypeID";

                            objectID = companyConnection.Query<string>("select OBJECT_ID(@n, 'V')", new { n = objectName }).First();

                            viewSql = (string.IsNullOrEmpty(objectID)) ? "CREATE " : "ALTER ";
                            viewSql += $@" VIEW {objectName} AS {selectSql}";

                            executeSqlWithTry(companyConnection, viewSql);

                            #endregion

                            #region FusionQuery_Fields

                            objectName = $"{SCHEMA}.[FusionQuery_Fields]";
                            viewNames.Add(objectName);

                            selectSql = @"
select  O.ID as FusionQueryAttributeID, 
		O.FusionQueryAttributeTypeID, 
		F.FieldTypeID, 
		FT.Name as FieldName, 
		FT.FriendlyName as FieldFriendlyName, 
		F.FormattedValue as FieldValue 
from	FusionQueryAttribute O 
		inner join Field F on F.ObjectType = 'FusionQueryAttribute' and F.ObjectID = O.ID
		inner join FieldType FT on FT.ID = F.FieldTypeID";

                            objectID = companyConnection.Query<string>("select OBJECT_ID(@n, 'V')", new { n = objectName }).First();

                            viewSql = (string.IsNullOrEmpty(objectID)) ? "CREATE " : "ALTER ";
                            viewSql += $@" VIEW {objectName} AS {selectSql}";

                            executeSqlWithTry(companyConnection, viewSql);

                            #endregion

                            #region Glossary_Fields

                            objectName = $"{SCHEMA}.[Glossary_Fields]";
                            viewNames.Add(objectName);

                            selectSql = @"
select 	O.ID as AssetID,
        O.ObjectID, 
	    O.TypeID as ArtifactTypeID, 
	    O.DisplayValue, 
	    F.FieldTypeID, 
        F.Name as FieldName, 
        F.FriendlyName as FieldFriendlyName, 
	    F.FormattedValue as FieldValue 
from	AssetDetail O 
        inner join FieldDetail F on F.AssetID = O.ID
where	O.AssetTypeClass = 1";

                            objectID = companyConnection.Query<string>("select OBJECT_ID(@n, 'V')", new { n = objectName }).First();

                            viewSql = (string.IsNullOrEmpty(objectID)) ? "CREATE " : "ALTER ";
                            viewSql += $@" VIEW {objectName} AS {selectSql}";

                            executeSqlWithTry(companyConnection, viewSql);

                            #endregion

                            #region All Models

                            objectName = $"{SCHEMA}.[Model_All]";
                            viewNames.Add(objectName);

                            selectSql = @"
select	A.ID as AssetID,
        A.ObjectID as [Taxonomy ID],
		I.SubjectID as ParentID,
        A.TypeID as [Taxonomy Type id],
		A.DisplayValue,
		TP.TextPath,
		LV.[Level] as [Taxonomy Level],
		A.UpdatedOn,
		A.UpdatedBy,
		CONVERT(VARCHAR(10), A.UpdatedOn, 112) as UpdatedOnKey,
		A.TypeName as [Taxonomy Type Name],
		coalesce(TL.Name, 'Level ' + cast(LV.[Level] as varchar)) as [Level Name]
from	AssetDetail A
		cross apply dbo.GetAssetTextPathById(A.ID, '/') TP
		cross apply dbo.GetAssetLevelById(A.ID) LV
		left join PredicateIntersect I on I.Object = A.Object and I.ObjectID = A.ObjectID and I.PredicateType = 4
		left join AssetTypeLevel TL on TL.[AssetTypeID] = A.AssetTypeID and TL.[Level] = LV.[Level]
where	A.AssetTypeClass = 2";

                            objectID = companyConnection.Query<string>("select OBJECT_ID(@n, 'V')", new { n = objectName }).First();

                            viewSql = (string.IsNullOrEmpty(objectID)) ? "CREATE " : "ALTER ";
                            viewSql += $@" VIEW {objectName} AS {selectSql}";

                            executeSqlWithTry(companyConnection, viewSql);

                            #endregion

                            #region Model_Fields

                            objectName = $"{SCHEMA}.[Model_Fields]";
                            viewNames.Add(objectName);

                            selectSql = @"
select  O.ID as AssetID,
        O.ObjectID as ID, 
	    T.ObjectID as TaxonomyTypeID, 
	    D.DisplayValue, 
	    null as TextPath, --TP.TextPath, 
	    F.FieldTypeID, 
        FT.Name as FieldName, 
        FT.FriendlyName as FieldFriendlyName, 
	    F.FormattedValue as FieldValue 
from	Asset O 
        inner join AssetType T on T.ID = O.AssetTypeID and T.Class = 2 and O.State = 1
        inner join dbo.AssetDisplayValue D on D.AssetID = O.ID	
        inner join Field F on F.ObjectType = O.Object and F.ObjectID = O.ObjectID
	    inner join FieldType FT on FT.ID = F.FieldTypeID";

                            objectID = companyConnection.Query<string>("select OBJECT_ID(@n, 'V')", new { n = objectName }).First();

                            viewSql = (string.IsNullOrEmpty(objectID)) ? "CREATE " : "ALTER ";
                            viewSql += $@" VIEW {objectName} AS {selectSql}";

                            executeSqlWithTry(companyConnection, viewSql);

                            #endregion

                            #region Reference

                            objectName = $"{SCHEMA}.[Reference_All]";
                            viewNames.Add(objectName);

                            selectSql = @"
select		A.ID as AssetID,
            ATT.Name as ReferenceItemType,
			ATT.ObjectID as ReferenceItemTypeID,
			A.ObjectID as ReferenceItemID,
			A.Code,
			AD.DisplayValue,
			A.CreatedBy,
			A.CreatedOn,
			A.UpdatedBy,
			A.UpdatedOn
from		Asset A
			inner join AssetType ATT on (A.AssetTypeID = ATT.ID)
            inner join AssetDisplayValue AD on (A.ID = AD.AssetID)
where       ATT.Class = 9 and A.State = 1";

                            objectID = companyConnection.Query<string>("select OBJECT_ID(@n, 'V')", new { n = objectName }).First();

                            viewSql = (string.IsNullOrEmpty(objectID)) ? "CREATE " : "ALTER ";
                            viewSql += $@" VIEW {objectName} AS {selectSql}";

                            executeSqlWithTry(companyConnection, viewSql);

                            #endregion

                            #region Reference_Fields

                            objectName = $"{SCHEMA}.[Reference_Fields]";
                            viewNames.Add(objectName);

                            selectSql = @"
select		F.AssetID,
            ATT.ObjectID as ReferenceItemTypeID,
			A.ObjectID as ReferenceItemID,
			T.Name as FieldTypeName,
			T.FriendlyName as FieldTypeFriendlyName,
			F.FormattedValue
from		Field F
			inner join FieldType T on T.ID = F.FieldTypeID
            inner join Asset A on (A.Object = F.ObjectType and A.ObjectID = F.ObjectID)
			inner join AssetType ATT on (A.AssetTypeID = ATT.ID)
where       A.[Object] = 'ReferenceItem' and A.State = 1";

                            objectID = companyConnection.Query<string>("select OBJECT_ID(@n, 'V')", new { n = objectName }).First();

                            viewSql = (string.IsNullOrEmpty(objectID)) ? "CREATE " : "ALTER ";
                            viewSql += $@" VIEW {objectName} AS {selectSql}";

                            executeSqlWithTry(companyConnection, viewSql);

                            #endregion

                            #region All Responsibility Allocations

                            objectName = $"{SCHEMA}.[ResponsibilityTypeMap]";
                            viewNames.Add(objectName);

                            selectSql = @"
select	R.ResponsibilityTypeID, 
        RT.Name as ResponsibilityName, 
        D.Name as ObjectName, 
        D.Object, 
        D.ObjectID 
from	ResponsibilityTypeRelation R
	    inner Join [dbo].[ResponsibilityType] RT on RT.ID = R.ResponsibilityTypeID 
	    inner join AssetType D on D.Object = R.ObjectType and D.ObjectID = R.ObjectID";

                            objectID = companyConnection.Query<string>("select OBJECT_ID(@n, 'V')", new { n = objectName }).First();

                            viewSql = (string.IsNullOrEmpty(objectID)) ? "CREATE " : "ALTER ";
                            viewSql += $@" VIEW {objectName} AS {selectSql}";

                            executeSqlWithTry(companyConnection, viewSql);

                            #endregion

                            #region All Policies

                            objectName = $"{SCHEMA}.[Policy_All]";
                            viewNames.Add(objectName);

                            selectSql = @"
with p as (
	select	T.ObjectID as ID,
			T.TypeID as PolicyTypeID,
			T.UpdatedOn,
			T.UpdatedBy,
			null as ParentID,
			T.DisplayValue,
			T.DisplayValue as TextPath,
			1 as [Level]
	from	AssetDetail T
			left join PredicateIntersect I on I.Object = 'Policy' and I.ObjectID = T.ObjectID and I.PredicateType = 4
	where	I.IntersectID is null and T.Object = 'Policy'
	union all
	select	T.ObjectID as ID,
			T.TypeID as PolicyTypeID,
			T.UpdatedOn,
			T.UpdatedBy,
			p.ID as ParentID,
			T.DisplayValue,
			p.TextPath + '.' + T.DisplayValue as TextPath,
			p.[Level] + 1 as [Level]
	from	AssetDetail T
			inner join PredicateIntersect I on I.Object = 'Policy' and I.ObjectID = T.ObjectID and I.PredicateType = 4
			inner join p on I.Subject = 'Policy' and I.SubjectID = p.ID
	where	T.Object = 'Policy'
)

SELECT  A.ID as AssetID,
        p.[ID] as [PolicyID],
        p.[ParentID],
        p.DisplayValue,
		p.TextPath,
        p.[UpdatedOn],
        p.[UpdatedBy],
        p.[PolicyTypeID],
        p.[Level],
        t.Name as [Policy Type],
        coalesce(ptl.Name, 'Level ' + cast(p.[Level] as nvarchar)) as [Policy Level Name],
        CONVERT(VARCHAR(10), p.UpdatedOn, 112) as UpdatedOnKey
FROM    p
		inner join Asset A on A.Object = 'Policy' and A.ObjectID = p.ID
		inner join AssetType T on T.ID = A.AssetTypeID
        left Join AssetTypeLevel ptl on ptl.AssetTypeID = T.ID and ptl.[Level] = p.[level]";

                            objectID = companyConnection.Query<string>("select OBJECT_ID(@n, 'V')", new { n = objectName }).First();

                            viewSql = (string.IsNullOrEmpty(objectID)) ? "CREATE " : "ALTER ";
                            viewSql += $@" VIEW {objectName} AS {selectSql}";

                            executeSqlWithTry(companyConnection, viewSql);

                            #endregion

                            #region All Resources

                            objectName = $"{SCHEMA}.[Resource_All]";
                            viewNames.Add(objectName);

                            selectSql = @"
select 
    '/Resource/' + cast(ResourceID as varchar(250)) as ResourceURI,
    'Individual' as ResourceType,
    ResourceID,
    Email, 
    LastName + ', ' + FirstName as Resourcename
from reporting.Global_Resource where [State] = 1
Union
    select 
    '/Group/' + cast(ID as varchar(250)) as ResourceURI,
    'Group' as ResourceType,
    ID as ResourceID,
    null as Email,
    Name as Resourcename
FROM [dbo].[Group]";

                            objectID = companyConnection.Query<string>("select OBJECT_ID(@n, 'V')", new { n = objectName }).First();

                            viewSql = (string.IsNullOrEmpty(objectID)) ? "CREATE " : "ALTER ";
                            viewSql += $@" VIEW {objectName} AS {selectSql}";

                            executeSqlWithTry(companyConnection, viewSql);

                            #endregion

                            #region REPORING USERS

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

                            objectID = companyConnection.Query<string>("select OBJECT_ID(@n, 'V')", new { n = objectName }).First();

                            viewSql = (string.IsNullOrEmpty(objectID)) ? "CREATE " : "ALTER ";
                            viewSql += $@" VIEW {objectName} AS {selectSql}";

                            executeSqlWithTry(companyConnection, viewSql);

                            #endregion


                            #region All Responsibility

                            objectName = $"{SCHEMA}.[Responsibility_All]";
                            viewNames.Add(objectName);

                            selectSql = @"
SELECT S.AssetID
      ,S.Object
      ,S.ObjectID
      ,S.Type
      ,S.TypeID
      ,S.Context
      ,S.ResponsibilityTypeID
      ,S.ResponsibilityTypeName
	  ,R.FirstName
	  ,R.LastName
      ,S.ResourceID
      ,S.SecurityAsset
      ,S.SecurityAssetID
      ,S.SecurityAssetName
      ,S.IsVisible
      ,S.ApplyToType
      ,S.OverrideID as OverrideItemID
      ,[PermissionsBitMask]
  FROM ResponsibilityDetail S inner join reporting.Global_Resource R on R.ResourceID = S.ResourceID and R.[State] = 1";

                            objectID = companyConnection.Query<string>("select OBJECT_ID(@n, 'V')", new { n = objectName }).First();

                            viewSql = (string.IsNullOrEmpty(objectID)) ? "CREATE " : "ALTER ";
                            viewSql += $@" VIEW {objectName} AS {selectSql}";

                            executeSqlWithTry(companyConnection, viewSql);

                            #endregion

                            #region All Relationship

                            objectName = $"{SCHEMA}.[Relationship_All]";
                            viewNames.Add(objectName);

                            selectSql = @"SELECT  ID as IntersectID, * FROM IntersectDetail";

                            objectID = companyConnection.Query<string>("select OBJECT_ID(@n, 'V')", new { n = objectName }).First();

                            viewSql = (string.IsNullOrEmpty(objectID)) ? "CREATE " : "ALTER ";
                            viewSql += $@" VIEW {objectName} AS {selectSql}";

                            executeSqlWithTry(companyConnection, viewSql);

                            #endregion

                            #region All Relationship Slimmed Down

                            objectName = $"{SCHEMA}.[Relationship_Asset]";
                            viewNames.Add(objectName);

                            selectSql = @"
select  i.ID as IntersectID,
        st.ID as SubjectAssetTypeID,
        st.Name as SubjectAssetTypeName, 
        s.ID as SubjectAssetID, 
        ot.ID as ObjectAssetTypeID,
        ot.Name as ObjectAssetTypeName,         
        o.ID as ObjectAssetID, 
        i.IntersectTypeID 
from    [intersect] i 
        inner join asset s on (i.[subject] = s.[object] and i.[subjectid] = s.objectid) 
        inner join assettype st on s.assettypeid = st.id
        inner join asset o on (i.[object] = o.[object] and i.[objectid] = o.objectid)
        inner join assettype ot on o.assettypeid = ot.id";

                            objectID = companyConnection.Query<string>("select OBJECT_ID(@n, 'V')", new { n = objectName }).First();

                            viewSql = (string.IsNullOrEmpty(objectID)) ? "CREATE " : "ALTER ";
                            viewSql += $@" VIEW {objectName} AS {selectSql}";

                            executeSqlWithTry(companyConnection, viewSql);

                            #endregion

                            #region Relationship_Fields

                            objectName = $"{SCHEMA}.[Relationship_Fields]";
                            viewNames.Add(objectName);

                            selectSql = @"
select 	O.ID, 
	    O.IntersectTypeID, 
	    O.Subject, 
	    O.SubjectID, 
	    O.SubjectTypeName, 
	    O.SubjectName, 
	    O.Object, 
	    O.ObjectID, 
	    O.ObjectTypeName, 
	    O.ObjectName, 
	    F.FieldTypeID, 
        FT.Name as FieldName, 
        FT.FriendlyName as FieldFriendlyName, 
	    F.FormattedValue as FieldValue 
from	IntersectDetail O 
	    inner join Field F on F.ObjectType = 'Intersect' and F.ObjectID = O.ID
	    inner join FieldType FT on FT.ID = F.FieldTypeID";

                            objectID = companyConnection.Query<string>("select OBJECT_ID(@n, 'V')", new { n = objectName }).First();

                            viewSql = (string.IsNullOrEmpty(objectID)) ? "CREATE " : "ALTER ";
                            viewSql += $@" VIEW {objectName} AS {selectSql}";

                            executeSqlWithTry(companyConnection, viewSql);

                            #endregion

                            #region Rules

                            objectName = $"{SCHEMA}.[Rules_All]";
                            viewNames.Add(objectName);

                            selectSql = @"
                                SELECT        
                                	A.ID AS AssetID, 
                                	A.ObjectID AS ID, 
                                	A.DisplayValue AS Name, 
                                	A.TypeID AS RuleTypeID, 
                                	A.TypeName AS RuleType, 
                                	'-1' as RuleDimensionID, 
                                	'' as RuleDimensionName, 
                                	R.Threshold, 
                                	'' as Status, 
                                	A.CreatedOn, 
                                	A.CreatedBy, 
                                	A.UpdatedOn, 
                                	A.UpdatedBy
                                FROM  dbo.AssetDetail AS A 
                                	INNER JOIN dbo.[Rule] AS R ON R.ID = A.ObjectID AND A.AssetTypeClass = 7 AND A.State = 1";

                            objectID = companyConnection.Query<string>("select OBJECT_ID(@n, 'V')", new { n = objectName }).First();

                            viewSql = (string.IsNullOrEmpty(objectID)) ? "CREATE " : "ALTER ";
                            viewSql += $@" VIEW {objectName} AS {selectSql}";

                            executeSqlWithTry(companyConnection, viewSql);

                            #endregion

                            #region Rule_Fields

                            objectName = $"{SCHEMA}.[Rules_Fields]";
                            viewNames.Add(objectName);

                            selectSql = @"
select 	O.ID as AssetID,
        O.ObjectID as RuleID, 
	    O.DisplayValue as RuleName, 
	    F.FieldTypeID, 
        FT.Name as FieldName, 
        FT.FriendlyName as FieldFriendlyName, 
	    F.FormattedValue as FieldValue 
from	AssetDetail O 
        inner join Field F on F.ObjectType = O.Object and F.ObjectID = O.ObjectID and O.AssetTypeClass = 7 and O.State = 1
	    inner join FieldType FT on FT.ID = F.FieldTypeID";

                            objectID = companyConnection.Query<string>("select OBJECT_ID(@n, 'V')", new { n = objectName }).First();

                            viewSql = (string.IsNullOrEmpty(objectID)) ? "CREATE " : "ALTER ";
                            viewSql += $@" VIEW {objectName} AS {selectSql}";

                            executeSqlWithTry(companyConnection, viewSql);

                            #endregion

                            #region RuleImplementations

                            objectName = $"{SCHEMA}.[RuleImplementations_All]";
                            viewNames.Add(objectName);

                            selectSql = @"
select	R.RuleTypeID,
		RT.Name as RuleType,
		D.ID as AssetID,
        I.RuleID,
		AD.DisplayValue as [Rule],
		I.ID as RuleImplementationID,
		coalesce(I.Name, 'Implementation ' + cast(I.ID as nvarchar)) as RuleImplementation,
		I.SourceID,
		I.SourceUri,
		count(Q.ID) as QualifierCount,
		I.CreatedOn,
		I.CreatedBy,
		I.UpdatedOn,
		I.UpdatedBy
from	RuleImplementation I
		left join RuleResultQualifierType Q on Q.RuleImplementationID = I.ID
		inner join [Rule] R on R.ID = I.RuleID
        inner join dbo.Asset D on D.Object = 'Rule' and D.ObjectID = R.ID
        inner join dbo.AssetDisplayValue AD on AD.AssetId = D.ID
		inner join RuleType RT on RT.ID = R.RuleTypeID 
group by R.RuleTypeID,
		RT.Name,
        D.ID,
		I.RuleID,
		AD.DisplayValue,
		I.ID,
		coalesce(I.Name, 'Implementation ' + cast(I.ID as nvarchar)),
		I.SourceID,
		I.SourceUri,
		I.CreatedOn,
		I.CreatedBy,
		I.UpdatedOn,
		I.UpdatedBy";

                            objectID = companyConnection.Query<string>("select OBJECT_ID(@n, 'V')", new { n = objectName }).First();

                            viewSql = (string.IsNullOrEmpty(objectID)) ? "CREATE " : "ALTER ";
                            viewSql += $@" VIEW {objectName} AS {selectSql}";

                            executeSqlWithTry(companyConnection, viewSql);

                            objectName = $"{SCHEMA}.[RuleImplementations_Results]";
                            viewNames.Add(objectName);

                            selectSql = @"
select	RI.RuleID,
		R.Threshold,
		RI.ID as RuleImplementationID,
		coalesce(RI.Name, 'Implementation ' + cast(RI.ID as nvarchar)) as RuleImplementation,
		RR.ID as RuleResultID,
		RR.EffectiveDate,
		RR.CreatedOn,
		RR.RunDate,
		RR.RowsPassed,
		RR.RowsFailed,
		RR.PassFraction,
		RR.FailFraction,
		RR.Passed
from	RuleResult RR
		inner join RuleImplementation RI on RI.ID = RR.RuleImplementationID
		inner join [Rule] R on R.ID = RI.RuleID";

                            objectID = companyConnection.Query<string>("select OBJECT_ID(@n, 'V')", new { n = objectName }).First();

                            viewSql = (string.IsNullOrEmpty(objectID)) ? "CREATE " : "ALTER ";
                            viewSql += $@" VIEW {objectName} AS {selectSql}";

                            executeSqlWithTry(companyConnection, viewSql);


                            objectName = $"{SCHEMA}.[RuleImplementations_ResultQualifiers]";
                            viewNames.Add(objectName);

                            selectSql = @"
select	q.RuleResultID, 
		qt.Name as QualifierName, 
		q.Value as QualifierValue--,
		--q.[ResolvedObject],
		--q.[ResolvedObjectID]
from	RuleResultQualifier q
		inner join RuleResultQualifierType qt on qt.ID = q.RuleResultQualifierTypeID";
                            objectID = companyConnection.Query<string>("select OBJECT_ID(@n, 'V')", new { n = objectName }).First();

                            viewSql = (string.IsNullOrEmpty(objectID)) ? "CREATE " : "ALTER ";
                            viewSql += $@" VIEW {objectName} AS {selectSql}";

                            executeSqlWithTry(companyConnection, viewSql);

                            #endregion

                            #region Workflows

                            objectName = $"{SCHEMA}.[Workflows]";
                            viewNames.Add(objectName);

                            selectSql = @"
select	T.Name as WorkflowTypeName,
		T.Description as WorkflowTypeDescription,
		I.ID as ItemID,
		I.Active,
		I.StartedBy,
		I.StartedOn,
		I.CompletedBy,
		I.CompletedOn,
		I.Object,
		I.ObjectID,
		V.Version
from	workflow.Item I
		inner join workflow.[Version] V on V.ID = I.VersionID
		inner join workflow.Type T on T.ID = V.TypeID";

                            objectID = companyConnection.Query<string>("select OBJECT_ID(@n, 'V')", new { n = objectName }).First();

                            viewSql = (string.IsNullOrEmpty(objectID)) ? "CREATE " : "ALTER ";
                            viewSql += $@" VIEW {objectName} AS {selectSql}";

                            executeSqlWithTry(companyConnection, viewSql);

                            #endregion

                            #region Workflow Steps

                            objectName = $"{SCHEMA}.[WorkflowSteps]";
                            viewNames.Add(objectName);

                            selectSql = @"
select	S.ItemID,
        S.StepID,
		S.StartedBy,
		S.StartedOn,
		S.CompletedBy,
		S.CompletedOn,
		V.Name as StepName,
		case V.ActivityType
			when 1 then 'Email Notification'
			when 2 then 'Status Change'
			when 3 then 'Form'
			when 4 then 'Procedure'
			when 5 then 'Field Change'
			else 'None'
		end as ActivityTypeName
from	workflow.ItemStep S
		inner join workflow.VersionStep V on V.ID = S.StepID";

                            objectID = companyConnection.Query<string>("select OBJECT_ID(@n, 'V')", new { n = objectName }).First();

                            viewSql = (string.IsNullOrEmpty(objectID)) ? "CREATE " : "ALTER ";
                            viewSql += $@" VIEW {objectName} AS {selectSql}";

                            executeSqlWithTry(companyConnection, viewSql);

                            #endregion

                            #region Workflow Forms

                            objectName = $"{SCHEMA}.[WorkflowForms]";
                            viewNames.Add(objectName);

                            selectSql = @"
                                select 
	                                t.ID as WorkflowID,
	                                i.ID as ItemID,
	                                coalesce(a.DisplayValue, ai.DisplayValue, arn.[Name], '[unknown]') as WorkflowItem,
	                                coalesce(a.ID, ai.ID) as AssetID,
	                                si.StepID,
	                                form.fieldId as FieldID,
	                                form.fieldLabel as Field,
	                                coalesce(form.fieldValue, form.fieldValueRaw) as FieldValue,
	                                form.resId as ResourceID,
	                                si.CompletedOn as [Date]
                                from workflow.itemstep si
	                                inner join workflow.item i on i.ID = si.ItemID
	                                left join AssetDetail a on a.[Object] = i.[Object] and a.ObjectiD = i.ObjectID
	                                left join Issue on issue.ID = i.ObjectID and i.[Object] = 'Issue'
	                                left join AssetDetail ai on ai.[Object] = issue.[Object] and ai.ObjectID = issue.ObjectID
	                                left join IntersectDetail ar on ar.ID = i.ID and i.[Object] = 'Intersect'
	                                outer apply dbo.GetIntersectNames(ar.ID) arn
	                                inner join workflow.versionstep vs on vs.ID = si.StepID
	                                inner join workflow.[version] v on v.ID = vs.VersionID
	                                inner join workflow.[type] t on t.ID = v.TypeID
	                                cross apply (
		                                select 
			                                sif.ID,
			                                f.n.value('(../@ResourceID)[1]','int') as resId,
			                                f.n.value('(./@label)[1]','varchar(max)') as fieldLabel,
			                                f.n.value('(./@value)[1]','varchar(max)') as fieldValueRaw,
			                                f.n.value('(./@displayvalue)[1]','varchar(max)') as fieldValue,
			                                f.n.value('(./@fieldtype)[1]','varchar(max)') as fieldType,
			                                f.n.value('(./@id)[1]','varchar(max)') as fieldId
		                                from 
			                                workflow.itemstep sif
			                                cross apply sif.Fields.nodes('fields/form/field') f(n)
		                                where 
			                                sif.ID = si.ID
	                                ) form
                                where 
	                                vs.StepType = 2 and vs.ActivityType = 3";

                            objectID = companyConnection.Query<string>("select OBJECT_ID(@n, 'V')", new { n = objectName }).First();

                            viewSql = (string.IsNullOrEmpty(objectID)) ? "CREATE " : "ALTER ";
                            viewSql += $@" VIEW {objectName} AS {selectSql}";

                            executeSqlWithTry(companyConnection, viewSql);
                            #endregion


                            prefix = "Global";

                            #region InterRelationships

                            objectName = string.Format("{0}.[{1}_{2}]", SCHEMA, prefix, "ModelInterRelationships");
                            viewNames.Add(objectName);

                            selectSql = @"
select	I.ID as IntersectID,

		S.ObjectID as SourceID,
		S.DisplayValue as SourceName,
		S.TypeName as SourceType,
		dbo.GenerateAssetUrl(S.ID) as SourceUrl,

		O.ObjectID as TargetID,
		O.DisplayValue as TargetName,
		O.TypeName as TargetType,
		dbo.GenerateAssetUrl(O.ID) as TargetUrl 

from	[Intersect] I
		inner join IntersectType T on T.ID = I.IntersectTypeID
		inner join [Predicate] P on P.ID = T.PredicateID and P.Type <> 4
		inner join AssetDetail S on S.Object = I.Subject and S.ObjectID = I.SubjectID and S.AssetTypeClass = 2 and S.State = 1
		inner join AssetDetail O on O.Object = I.Object and O.ObjectID = I.ObjectID and O.AssetTypeClass = 2 and O.State = 1";

                            objectID = companyConnection.Query<string>("select OBJECT_ID(@n, 'V')", new { n = objectName }).First();

                            viewSql = (string.IsNullOrEmpty(objectID)) ? "CREATE " : "ALTER ";
                            viewSql += $@" VIEW {objectName} AS {selectSql}";

                            executeSqlWithTry(companyConnection, viewSql);

                            #endregion

                            #region Integration 


                            # region Mappings

                            objectName = $"{SCHEMA}.[Integration_Mappings]";
                            viewNames.Add(objectName);

                            selectSql = @"
select	case SE.IntegrationSystem
			when 1 then 'IGC'
			else cast(SE.IntegrationSystem as varchar)
		end IntegrationSystem,
		O.Active,
		O.MappingType,
		O.SourceAssetTypeName,
		O.SourceField,
		O.TargetType,
		O.TargetAssetTypeName,
		O.TargetField,
		O.DefaultValue
from	(
		select	ST.IntegrationSettingID,
				F.Active,
				'Field' as MappingType,
				ST.SourceAssetTypeName,
				F.SourceField,
				ST.Object as TargetType,
				T.Name as TargetAssetTypeName,
				F.TargetField,
				F.DefaultValue
		from	[integration].[SynchedAssetType] ST
				inner join AssetType T on T.ID = ST.AssetTypeID
				inner join [integration].[SynchedAssetTypeFieldItem] F on F.SynchedAssetTypeID = ST.ID
		union
		select	ST.IntegrationSettingID,
				F.Active,
				'Relation' as MappingType,
				ST.SourceAssetTypeName,
				F.SourceField,
				ST.Object as TargetType,
				T.Name as TargetAssetTypeName,
				I.SubjectName + ' ' + coalesce(I.PredicateName,'') + ' ' + I.ObjectName as TargetField,
				null as DefaultValue
		from	[integration].[SynchedAssetType] ST
				inner join AssetType T on T.ID = ST.AssetTypeID
				inner join [integration].[SynchedAssetTypeRelationItem] F on F.SynchedAssetTypeID = ST.ID
				inner join [integration].[SynchedAssetTypeRelationItemTarget] FT on FT.[SynchedAssetTypeRelationItemID] = F.ID
				inner join IntersectTypeDetail I on I.ID = FT.IntersectTypeID
		union
		select	ST.IntegrationSettingID,
				F.Active,
				'Role' as MappingType,
				ST.SourceAssetTypeName,
				F.SourceIdField as SourceField,
				ST.Object as TargetType,
				T.Name as TargetAssetTypeName,
				RT.Name as TargetField,
				null as DefaultValue
		from	[integration].[SynchedAssetType] ST
				inner join AssetType T on T.ID = ST.AssetTypeID
				inner join [integration].[SynchedAssetTypeRoleItem] F on F.SynchedAssetTypeID = ST.ID
				inner join ResponsibilityType RT on RT.ID = F.ResponsibilityTypeID
		) O 
		inner join [integration].Setting SE on SE.ID = O.IntegrationSettingID";

                            objectID = companyConnection.Query<string>("select OBJECT_ID(@n, 'V')", new { n = objectName }).First();

                            viewSql = (string.IsNullOrEmpty(objectID)) ? "CREATE " : "ALTER ";
                            viewSql += $@" VIEW {objectName} AS {selectSql}";

                            executeSqlWithTry(companyConnection, viewSql);

                            #endregion


                            #region Errors

                            objectName = $"{SCHEMA}.[Integration_ExecutionErrors]";
                            viewNames.Add(objectName);

                            selectSql = @"
select	E.Uid,
		E.ExecutionID,
		E.SynchedAssetTypeID,
		S.SourceAssetTypeName,
		coalesce(D.DisplayValue, F.NAme) as AssetName,
		E.SourceID,
		E.ErrorMessages
from	integration.ExecutionAssetTypeError E
		inner join integration.ExecutionAssetType Ex on Ex.ExecutionID = E.ExecutionID and Ex.SynchedAssetTypeID = E.SynchedAssetTypeID and Ex.StartedOn > DATEADD(d, -14, getutcdate())
		inner join integration.SynchedAssetType S on S.ID = E.SynchedAssetTypeID 
		inner join Asset A on A.AssetTypeID = S.AssetTypeID and A.SourceID = E.SourceID
		left join AssetDisplayValue D on D.AssetID = A.ID
		left join FusionAttribute F on F.FusionAttributeTypeID = S.ObjectID and A.Object = 'FusionAttribute' and F.ID = A.ObjectID";

                            objectID = companyConnection.Query<string>("select OBJECT_ID(@n, 'V')", new { n = objectName }).First();

                            viewSql = (string.IsNullOrEmpty(objectID)) ? "CREATE " : "ALTER ";
                            viewSql += $@" VIEW {objectName} AS {selectSql}";

                            executeSqlWithTry(companyConnection, viewSql);

                            #endregion


                            #endregion

                            #endregion

                            #region Remove Old Views

                            var currentViewNames = companyConnection.Query<string>(@"select TABLE_SCHEMA + '.[' + TABLE_NAME + ']' from [INFORMATION_SCHEMA].[VIEWS] where TABLE_SCHEMA = 'reporting'").ToList();

                            currentViewNames.ForEach(cv =>
                            {
                                if (!viewNames.Contains(cv))
                                {
                                    try
                                    {
                                        companyConnection.Execute(string.Format(@"drop view {0}", cv));
                                    }
                                    catch (Exception ex)
                                    {
                                        var msg = ex.GetFullExceptionData() + " Stack: " + ex.StackTrace;
                                        log.WriteLine(msg);
                                    }
                                }
                            });

                            #endregion

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
    }
}
