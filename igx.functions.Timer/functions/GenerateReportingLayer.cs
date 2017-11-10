using d360.core;
using d360.core.entities;
using d360.utils.company;
using Dapper;
using igx.functions.Core;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Practices.EnterpriseLibrary.TransientFaultHandling;
using System;
using System.Collections.Generic;
using System.Data.Entity.Design.PluralizationServices;
using System.Data.SqlClient;
using System.Linq;
using System.Text.RegularExpressions;

namespace igx.functions.Timer
{
    public static class GenerateReportingLayer
    {
        #region Utility

        static string cleanObjectName(string name)
        {
            name = name.Replace("'", "").Replace(" ", "").Replace("-", "").Replace("&", "And").Replace(":", "").Replace(";", "").Trim();
            Regex rgx = new Regex("[^a-zA-Z0-9-]");
            name = rgx.Replace(name, "");
            return name;
        }

        static void getDynamicFieldJoinStatements(List<FieldType> fields, string type, out string joins, out string columns)
        {
            columns = "";
            joins = "";

            foreach (var f in fields)
            {
                var name = cleanObjectName(f.Name);
                if (f.Type == "Lookup")
                    columns += string.Format("[{0}].Value as [{0}ID], [{0}].FormattedValue as [{0}], ", name);
                else
                    columns += string.Format("[{0}].FormattedValue as [{0}], ", name);

                joins += string.Format(" left join Field [{0}] on [{0}].ObjectType = '{2}' and [{0}].ObjectID = A.ID and [{0}].FieldTypeID = {1}", name, f.ID, type);
            }

            fields = null;
        }

        static void executeSqlWithTry(SqlConnection companyConnection, string viewSql)
        {
            try
            {
                companyConnection.Execute(viewSql.ToString());
            }
            catch (Exception ex)
            {
                var msg = ex.GetFullExceptionData() + " Stack: " + ex.StackTrace;
                Console.WriteLine(msg);
                Console.WriteLine("Attempted SQL: " + viewSql);
            }
        }

        #endregion

        #region Object Views

        //static void GenerateAttributeView(List<string> viewNames, SqlConnection companyConnection, string schema, string prefix, string name, string objectTypeKeyName, string tableName, string objectType, int typeID, bool includeOwningModel = false)
        //{
        //    name = cleanObjectName(name);
        //    var objectName = string.Format("{0}.[{1}_{2}Attributes]", schema, prefix, name);
        //    viewNames.Add(objectName);

        //    var objectID = companyConnection.Query<string>("select OBJECT_ID(@n, 'V')", new { n = objectName }).First();

        //    var sql = new StringBuilder("");
        //    sql.Append((string.IsNullOrEmpty(objectID)) ? "CREATE " : "ALTER ");
        //    sql.AppendFormat("VIEW {0} AS ", objectName);

        //    sql.AppendFormat("select A.ID as {0}ID, A.Name as {0}Name, ", name);
        //    if (includeOwningModel) sql.Append("V.ID as SubjectAreaID, V.Name as SubjectArea, ");
        //    sql.AppendFormat("[dbo].GenerateObjectUrl('{0}', A.{1}, A.ID) as {0}Url, AD.ID as AttributeID, AD.ParentID as ParentAttributeID, AD.Name as Attribute, AD.FormattedValue as AttributeValue ", objectType, objectTypeKeyName);
        //    sql.AppendFormat("from {0} A ", tableName);
        //    if (includeOwningModel) sql.Append("inner join TaxonomyType V on V.ID = A.TaxonomyTypeID ");
        //    sql.AppendFormat(@"inner join AttributeDetail AD on AD.ObjectType = '{0}' and AD.ObjectID = A.ID and A.{2} = {1}", objectType, typeID, objectTypeKeyName);

        //    try
        //    {
        //        companyConnection.Execute(sql.ToString());
        //    }
        //    catch (Exception ex)
        //    {
        //        var msg = ex.GetFullExceptionData() + " Stack: " + ex.StackTrace;
        //        Console.WriteLine(msg);
        //        Console.WriteLine("Attempted SQL: " + sql);
        //    }
        //}

        //        static void GeneratObjectRelationshipView(List<string> viewNames, SqlConnection companyConnection, string schema, string prefix, string name, string objectTypeKeyName, string tableName, string objectType, int typeID, bool includeOwningModel = false)
        //        {
        //            name = cleanObjectName(name);
        //            var objectName = string.Format("{0}.[{1}_{2}Relationships]", schema, prefix, name);
        //            viewNames.Add(objectName);

        //            var objectID = companyConnection.Query<string>("select OBJECT_ID(@n, 'V')", new { n = objectName }).First();

        //            var sql = new StringBuilder("");
        //            sql.Append((string.IsNullOrEmpty(objectID)) ? "CREATE " : "ALTER ");
        //            sql.AppendFormat("VIEW {0} AS ", objectName);

        //            sql.AppendFormat("select R.ID as IntersectID, A.ID as [{0}ID], A.Name as [{0}Name], ", name);
        //            if (includeOwningModel) sql.AppendFormat("A.Status, V.ID as SubjectAreaID, V.Name as SubjectArea, P.ID as [{0}ParentID], P.TextPath as [{0}ParentName], ", name);
        //            sql.Append(@"case when (R.Subject = 'Artifact' and A.ID = R.SubjectID) then R.ObjectTypeName else R.SubjectTypeName end as TargetType, 
        //case when(R.Subject = 'Artifact' and A.ID = R.SubjectID) then R.Object else R.Subject end as Target,
        //case when(R.Subject = 'Artifact' and A.ID = R.SubjectID) then R.ObjectID else R.SubjectID end as TargetID,
        //case when(R.Subject = 'Artifact' and A.ID = R.SubjectID) then R.ObjectName else R.SubjectName end as TargetName,
        //case when(R.Subject = 'Artifact' and A.ID = R.SubjectID) then R.ObjectUrl else R.SubjectUrl end as TargetUrl,
        //TR.[Count] as ChildRelationshipCount ");
        //            sql.Append("from IntersectDetail R ");
        //            sql.Append($"inner join {tableName} A on A.{objectTypeKeyName} = {typeID} and ((R.Subject = '{objectType}' and A.ID = R.SubjectID) OR (R.Object = '{objectType}' and A.ID = R.ObjectID)) ");
        //            if (includeOwningModel) sql.Append("inner join TaxonomyType V on V.ID = A.TaxonomyTypeID ");
        //            if (includeOwningModel) sql.Append("left join Artifact P on P.ID = A.ParentID ");
        //            sql.Append("outer apply (select	count(1) as [Count] from [Intersect] where Subject = 'Intersect' and SubjectID = R.ID) TR");

        //            try
        //            {
        //                companyConnection.Execute(sql.ToString());
        //            }
        //            catch (Exception ex)
        //            {
        //                var msg = ex.GetFullExceptionData() + " Stack: " + ex.StackTrace;
        //                Console.WriteLine(msg);
        //                Console.WriteLine("Attempted SQL: " + sql);
        //            }
        //        }

        //        static void GenerateObjectResponsibilityView(List<string> viewNames, SqlConnection companyConnection, string schema, string prefix, string name, string objectType, int typeID)
        //        {
        //            name = cleanObjectName(name);
        //            var objectName = string.Format("{0}.[{1}_{2}Responsibilities]", schema, prefix, name);
        //            viewNames.Add(objectName);

        //            var objectID = companyConnection.Query<string>("select OBJECT_ID(@n, 'V')", new { n = objectName }).First();

        //            var selectSql = string.Format(@"select	R.ObjectID as {0}ID,
        //		R.ObjectName as Name,
        //		R.ObjectUrl as Url,
        //		R.ResponsibleObjectName,
        //		R.ResponsibleObjectType,
        //		R.ResponsibleObjectUrl,
        //		R.PrimaryOwnerResourceID,
        //		R.PrimaryOwnerResourceName,
        //		R.PrimaryOwnerResourceUrl,
        //		R.Role,
        //		--R.RedFlagged,
        //		R.CurrentScore,
        //		R.ContextItems,
        //		R.AssigningItemType,
        //		R.AssigningItemID--,
        //		--R.AssigningItemName,
        //		--R.AssigningItemUrl,
        //		--R.AssigningTypeName
        //from	ResponsibilityDetail R
        //where	R.ObjectType = '{1}' and R.ObjectTypeID = {2}", name, objectType, typeID);

        //            var viewSql = (string.IsNullOrEmpty(objectID)) ? "CREATE " : "ALTER ";
        //            viewSql += string.Format(@" VIEW {0} AS {1}", objectName, selectSql);

        //            try
        //            {
        //                companyConnection.Execute(viewSql.ToString());
        //            }
        //            catch (Exception ex)
        //            {
        //                var msg = ex.GetFullExceptionData() + " Stack: " + ex.StackTrace;
        //                Console.WriteLine(msg);
        //                Console.WriteLine("Attempted SQL: " + viewSql);
        //            }
        //        }

        #endregion

        const string functionName = "GenerateReportingLayer";
        const string timerSettings = "0 */10 * * * *";
        //const string timerSettings = "*/5 * * * * *";

        [FunctionName(functionName)]
        public static void Run([TimerTrigger(timerSettings)]TimerInfo myTimer, TraceWriter log) //   
        {
            //trigger every two hours: https://docs.microsoft.com/en-us/azure/azure-functions/functions-bindings-timer#schedule-examples

            try
            {
                CoreFunction.AITrackJobStart(functionName);
                var companies = CoreFunction.GetCompaniesByCurrentSlot();

                companies.ForEach(c =>
                {
                    var viewNames = new List<string>();
                    string SCHEMA = "reporting";

                    try
                    {
                        var companyConnection = CompanyConnectionUtils.GetCompanyConnection(c.CompanyID, c.Server, c.Username, c.Password);
                        companyConnection.OpenWithRetry(RetryPolicy.DefaultProgressive);

                        var selectSql = "";
                        var viewSql = "";
                        var objectName = "";
                        var objectType = "Artifact";
                        var prefix = "Glossary";
                        string objectID;
                        List<FieldType> fieldTypes = null;

                        var pluralize = PluralizationService.CreateService(System.Globalization.CultureInfo.CurrentCulture);

                        #region Artifact Type

                        var artifactTypes = companyConnection.Query<ArtifactType>("select * from ArtifactType").ToList();

                        try
                        {
                            fieldTypes = companyConnection.Query<FieldType>("select * from FieldType where [Object] = 'ArtifactType'").ToList();
                        }
                        catch (Exception)
                        {
                            fieldTypes = companyConnection.Query<FieldType>("select * from FieldType where [Object] = 'ArtifactType'").ToList();
                        }

                        artifactTypes.ForEach(o =>
                        {
                            #region Object Views

                            var joins = "";
                            var columns = "";

                            getDynamicFieldJoinStatements(fieldTypes.Where(f => f.ObjectID == o.ID).ToList(), "Artifact", out joins, out columns);

                            objectName = $"{SCHEMA}.[{prefix}_{pluralize.Pluralize(cleanObjectName(o.Name))}]";
                            viewNames.Add(objectName);

                            selectSql = $@"
select  A.ID, 
        A.DisplayValue, 
        A.ParentID, 
        P.DisplayValue as ParentDisplayValue, 
        {columns} 
        dbo.GenerateObjectUrl('{objectType}', A.ArtifactTypeID, A.ID) as Url, 
        cast(S.Value * 100 as int) as CurrentScore 
from    Artifact A 
        left join metrics.Score S on S.Object = 'Artifact' and S.ObjectID = A.ID and getutcdate() between S.EffectiveStartDate and S.EffectiveEndDate 
        left join Artifact P on P.ID = A.ParentID 
        {joins} 
where   A.ArtifactTypeID = {o.ID}";

                            objectID = companyConnection.Query<string>("select OBJECT_ID(@n, 'V')", new { n = objectName }).First();

                            viewSql = (string.IsNullOrEmpty(objectID)) ? "CREATE " : "ALTER ";
                            viewSql += $@" VIEW {objectName} AS {selectSql}";

                            executeSqlWithTry(companyConnection, viewSql);

                            #endregion
                        });

                        artifactTypes = null;

                        #endregion

                        #region Information Model Type

                        prefix = "Glossary";
                        objectType = "Taxonomy";
                        prefix = "Model";

                        var taxonomyTypes = companyConnection.Query<TaxonomyType>("select * from TaxonomyType").ToList();

                        try
                        {
                            fieldTypes = companyConnection.Query<FieldType>("select * from FieldType where [Object] = 'TaxonomyType'").ToList();
                        }
                        catch (Exception)
                        {
                            fieldTypes = companyConnection.Query<FieldType>("select * from FieldType where [Object] = 'TaxonomyType'").ToList();
                        }

                        taxonomyTypes.ForEach(o =>
                        {
                            #region Object Views

                            var joins = "";
                            var columns = "";

                            getDynamicFieldJoinStatements(fieldTypes.Where(f => f.ObjectID == o.ID).ToList(), "Taxonomy", out joins, out columns);

                            objectName = $"{SCHEMA}.[{prefix}_{pluralize.Pluralize(cleanObjectName(o.Name))}]";
                            viewNames.Add(objectName);

                            selectSql = $@"
select  A.ID, 
        A.ParentID, 
        A.DisplayValue, 
        A.TextPath, 
        A.[Level], 
        L.Name as LevelName, 
        L.Description as LevelDescription,
        {columns} 
        dbo.GenerateObjectUrl('{objectType}', A.TaxonomyTypeID, A.ID) as Url, 
        cast(S.Value * 100 as int) as CurrentScore
from    Taxonomy A 
        {joins} 
        inner join TaxonomyType T on T.ID = A.TaxonomyTypeID
        left join metrics.Score S on S.Object = 'Taxonomy' and S.ObjectID = A.ID and getutcdate() between S.EffectiveStartDate and S.EffectiveEndDate 
    left join TaxonomyTypeLevel L on L.TaxonomyTypeID = A.TaxonomyTypeID and L.[Level] = A.[Level]
where   A.TaxonomyTypeID = {o.ID}";

                            objectID = companyConnection.Query<string>("select OBJECT_ID(@n, 'V')", new { n = objectName }).First();

                            viewSql = (string.IsNullOrEmpty(objectID)) ? "CREATE " : "ALTER ";
                            viewSql += $@" VIEW {objectName} AS {selectSql}";

                            executeSqlWithTry(companyConnection, viewSql);

                            #endregion
                        });

                        taxonomyTypes = null;

                        #endregion

                        #region Policy Type

                        objectType = "Policy";
                        prefix = "Policy";

                        var policyTypes = companyConnection.Query<PolicyType>("select * from PolicyType").ToList();

                        try
                        {
                            fieldTypes = companyConnection.Query<FieldType>("select * from FieldType where [Object] = 'PolicyType'").ToList();
                        }
                        catch (Exception)
                        {
                            fieldTypes = companyConnection.Query<FieldType>("select * from FieldType where [Object] = 'PolicyType'").ToList();
                        }

                        policyTypes.ForEach(o =>
                        {
                            #region Object Views

                            var joins = "";
                            var columns = "";

                            getDynamicFieldJoinStatements(fieldTypes.Where(f => f.ObjectID == o.ID).ToList(), "Policy", out joins, out columns);

                            objectName = $"{SCHEMA}.[{prefix}_{pluralize.Pluralize(cleanObjectName(o.Name))}]";
                            viewNames.Add(objectName);

                            selectSql = $@"
select  A.ID, 
        A.DisplayValue, 
        A.TextPath, 
        A.Description, 
        {columns} 
        dbo.GenerateObjectUrl('{objectType}', A.PolicyTypeID, A.ID) as Url, 
        cast(S.Value * 100 as int) as CurrentScore 
from    Policy A 
	    left join metrics.Score S on S.Object = 'Policy' and S.ObjectID = A.ID and getutcdate() between S.EffectiveStartDate and S.EffectiveEndDate 
        {joins} 
where   A.PolicyTypeID = {o.ID}";

                            objectID = companyConnection.Query<string>("select OBJECT_ID(@n, 'V')", new { n = objectName }).First();

                            viewSql = (string.IsNullOrEmpty(objectID)) ? "CREATE " : "ALTER ";
                            viewSql += $@" VIEW {objectName} AS {selectSql}";

                            executeSqlWithTry(companyConnection, viewSql);

                            #endregion
                        });

                        policyTypes = null;

                        #endregion

                        #region General Views

                        #region All Artifacts

                        objectName = $"{SCHEMA}.[Glossary_All]";
                        viewNames.Add(objectName);

                        selectSql = @"
select  A.ID,
        A.ParentID,
        A.ArtifactTypeID,
        A.DisplayValue,
        T.Name as ArtifactType,
        a.CreatedOn,
        a.UpdatedOn,
        CONVERT(VARCHAR(10), a.CreatedOn, 112) as CreatedOnKey,
        CONVERT(VARCHAR(10), a.UpdatedOn, 112) as UpdatedOnKey,
        TX.Name as TaxonomyTypeName,
        S.Value as CurrentScore,
        cast(S.Value * 100 as int) as CurrentScorePct
from    Artifact A  
    	left join metrics.Score S on S.Object = 'Artifact' and S.ObjectID = A.ID and getutcdate() between S.EffectiveStartDate and S.EffectiveEndDate 
        inner join ArtifactType T on T.ID = A.ArtifactTypeID";

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
select 	O.ID, 
	    O.ArtifactTypeID, 
	    O.Name, 
	    O.TextPath, 
	    F.FieldTypeID, 
        FT.Name as FieldName, 
        FT.FriendlyName as FieldFriendlyName, 
	    F.FormattedValue as FieldValue 
from	Artifact O 
	    inner join Field F on F.ObjectType = 'Artifact' and F.ObjectID = O.ID
	    inner join FieldType FT on FT.ID = F.FieldTypeID";

                        objectID = companyConnection.Query<string>("select OBJECT_ID(@n, 'V')", new { n = objectName }).First();

                        viewSql = (string.IsNullOrEmpty(objectID)) ? "CREATE " : "ALTER ";
                        viewSql += $@" VIEW {objectName} AS {selectSql}";

                        executeSqlWithTry(companyConnection, viewSql);

                        #endregion

                        #region All Models

                        objectName = $"{SCHEMA}.[Model_All]";
                        viewNames.Add(objectName);

                        selectSql = @"
SELECT  T.[ID] as [Taxonomy ID],
        T.[ParentID],
        T.[TaxonomyTypeID] as [Taxonomy Type id],
        T.DisplayValue,
        T.[TextPath],
        T.[Level] as [Taxonomy Level],
        T.[UpdatedOn],
        T.[UpdatedBy], 
        CONVERT(VARCHAR(10), t.UpdatedOn, 112) as UpdatedOnKey,
        TY.Name as [Taxonomy Type Name],
        TL.Name as [Level Name]
FROM [dbo].[Taxonomy] T
inner join TaxonomyType Ty on TY.ID = t.TaxonomyTypeID
inner join TaxonomyTypeLevel TL on TL.[TaxonomyTypeID] = TY.ID and TL.[Level] = T.[Level]";

                        objectID = companyConnection.Query<string>("select OBJECT_ID(@n, 'V')", new { n = objectName }).First();

                        viewSql = (string.IsNullOrEmpty(objectID)) ? "CREATE " : "ALTER ";
                        viewSql += $@" VIEW {objectName} AS {selectSql}";

                        executeSqlWithTry(companyConnection, viewSql);

                        #endregion

                        #region Model_Fields

                        objectName = $"{SCHEMA}.[Model_Fields]";
                        viewNames.Add(objectName);

                        selectSql = @"
select  O.ID, 
	    O.TaxonomyTypeID, 
	    O.DisplayValue, 
	    O.TextPath, 
	    F.FieldTypeID, 
        FT.Name as FieldName, 
        FT.FriendlyName as FieldFriendlyName, 
	    F.FormattedValue as FieldValue 
from	Taxonomy O 
	    inner join Field F on F.ObjectType = 'Taxonomy' and F.ObjectID = O.ID
	    inner join FieldType FT on FT.ID = F.FieldTypeID";

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
SELECT  p.[ID] as [PolicyID],
        p.[ParentID],
        p.[DisplayValue,
        p.[TextPath] as [Policy TextPath],
        p.[UpdatedOn],
        p.[UpdatedBy],
        p.[PolicyTypeID],
        p.[Level],
        pt.Name as [Policy Type],
        ptl.Name as [Policy Level Name],
        CONVERT(VARCHAR(10), p.UpdatedOn, 112) as UpdatedOnKey
FROM    [dbo].[Policy] p
        inner Join PolicyType pt on pt.ID = p.[PolicyTypeID]
        inner Join PolicyTypeLevel ptl on ptl.PolicyTypeID = pt.ID and ptl.[Level] = p.[level]";

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
    LastName + ', ' + FirstName as Resourcename
from reporting.Global_Resource
Union
    select 
    '/Group/' + cast(ID as varchar(250)) as ResourceURI,
    'Group' as ResourceType,
    ID as ResourceID,
    Name as Resourcename
FROM [dbo].[Group]";

                        objectID = companyConnection.Query<string>("select OBJECT_ID(@n, 'V')", new { n = objectName }).First();

                        viewSql = (string.IsNullOrEmpty(objectID)) ? "CREATE " : "ALTER ";
                        viewSql += $@" VIEW {objectName} AS {selectSql}";

                        executeSqlWithTry(companyConnection, viewSql);

                        #endregion

                        #region All Responsibility

                        objectName = $"{SCHEMA}.[Responsibility_All]";
                        viewNames.Add(objectName);

                        selectSql = @"
SELECT  * 
FROM    ResponsibilityDetails";

                        objectID = companyConnection.Query<string>("select OBJECT_ID(@n, 'V')", new { n = objectName }).First();

                        viewSql = (string.IsNullOrEmpty(objectID)) ? "CREATE " : "ALTER ";
                        viewSql += $@" VIEW {objectName} AS {selectSql}";

                        executeSqlWithTry(companyConnection, viewSql);

                        #endregion

                        #region All Relationship

                        objectName = $"{SCHEMA}.[Relationship_All]";
                        viewNames.Add(objectName);

                        selectSql = @"
SELECT 
I.[ID] as [IntersectID]
    ,I.[IntersectTypeID]
	,I.[Subject] as [SourceObject]
	,I.[SubjectID] as [SourceObjectID]
	,I.[Object] as [TargetObject]
	,I.[ObjectID] as [TargetObjectID]
    ,I.[Subject]
    ,I.[SubjectID]
    ,I.[SubjectName]
    ,I.[SubjectUrl]
    ,I.[SubjectType]
    ,I.[SubjectTypeID]
    ,I.[SubjectTypeName]
    ,I.[Object]
    ,I.[ObjectID]
    ,I.[ObjectName]
    ,I.[ObjectUrl]
    ,I.[ObjectType]
    ,I.[ObjectTypeID]
    ,I.[ObjectTypeName]
    ,I.[PredicateID]
    ,I.[PredicateName]
	,P.Inverse as PredicateInverse
FROM [dbo].[IntersectDetail] I
	inner join [Predicate] P on P.ID = I.[PredicateID]";

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
select	R.ID,
	    R.Name,
	    R.RuleTypeID,
        RT.Name as RuleType,
        R.RuleDimensionID,
	    D.Name as RuleDimensionName,
	    R.Description,
	    R.Purpose,
	    R.Measurement,
	    R.Resolution,
	    R.Threshold,
	    R.Status,
	    R.CreatedOn,
	    R.CreatedBy,
	    R.UpdatedOn,
	    R.UpdatedBy
from	[Rule] R
	    inner join RuleType RT on RT.ID = R.RuleTypeID 
        left join RuleDimension D on D.ID = R.RuleDimensionID";

                        objectID = companyConnection.Query<string>("select OBJECT_ID(@n, 'V')", new { n = objectName }).First();

                        viewSql = (string.IsNullOrEmpty(objectID)) ? "CREATE " : "ALTER ";
                        viewSql += $@" VIEW {objectName} AS {selectSql}";

                        executeSqlWithTry(companyConnection, viewSql);

                        //                        objectName = $"{SCHEMA}.[Rules_Results]";
                        //                        viewNames.Add(objectName);

                        //                        selectSql = @"
                        //select	RI.RuleID,
                        //	R.Name as RuleName,
                        //	R.RuleDimensionID,
                        //	D.Name as RuleDimensionName,
                        //    R.Status as RuleStatus,
                        //	R.Threshold,
                        //    RI.ID as RuleImplementationID,
                        //    RI.Name as RuleImplementationName,
                        //    RR.EffectiveDate,
                        //	RR.CreatedOn,
                        //    RR.RunDate,
                        //	RR.RowsPassed,
                        //	RR.RowsFailed,
                        //	RR.PassFraction,
                        //	RR.FailFraction,
                        //	RR.Passed,
                        //	Q.C as QualifierCount
                        //from	RuleResult RR
                        //	inner join RuleImplementation RI on RI.ID = RR.RuleImplementationID
                        //    inner join [Rule] R on R.ID = RI.RuleID
                        //    left join RuleDimension D on D.ID = R.RuleDimensionID
                        //	--left join FusionAttribute FA on Fa.ID = RR.FusionAttributeID
                        //	cross apply (
                        //				select	count(1) as C
                        //				from	RuleResultQualifier 
                        //				where	RuleResultID = RR.ID
                        //				) Q";
                        //                        objectID = companyConnection.Query<string>("select OBJECT_ID(@n, 'V')", new { n = objectName }).First();

                        //                        viewSql = (string.IsNullOrEmpty(objectID)) ? "CREATE " : "ALTER ";
                        //                        viewSql += $@" VIEW {objectName} AS {selectSql}";

                        //                        executeSqlWithTry(companyConnection, viewSql);

                        //                        objectName = $"{SCHEMA}.[Rules_ResultQualifiers]";
                        //                        viewNames.Add(objectName);

                        //                        selectSql = @"
                        //select	rr.ID as RuleResultID
                        //		, rr.RunDate
                        //		, rr.EffectiveDate
                        //		, rr.RowsPassed
                        //		, rr.RowsFailed
                        //		, rr.PassFraction
                        //		, rr.FailFraction
                        //		, rr.Passed
                        //		, qt.Name as QualifierName
                        //		, q.Value as QualifierValue
                        //from	RuleResult rr
                        //		inner Join RuleResultQualifier q on q.RuleResultID = rr.ID
                        //		inner join RuleResultQualifierType qt on qt.ID = q.RuleResultQualifierTypeID";
                        //                        objectID = companyConnection.Query<string>("select OBJECT_ID(@n, 'V')", new { n = objectName }).First();

                        //                        viewSql = (string.IsNullOrEmpty(objectID)) ? "CREATE " : "ALTER ";
                        //                        viewSql += $@" VIEW {objectName} AS {selectSql}";

                        //                        executeSqlWithTry(companyConnection, viewSql);

                        #endregion

                        #region Rule_Fields

                        objectName = $"{SCHEMA}.[Rules_Fields]";
                        viewNames.Add(objectName);

                        selectSql = @"
select 	O.ID as RuleID, 
	    O.Name as RuleName, 
	    F.FieldTypeID, 
        FT.Name as FieldName, 
        FT.FriendlyName as FieldFriendlyName, 
	    F.FormattedValue as FieldValue 
from	[Rule] O 
	    inner join Field F on F.ObjectType = 'Rule' and F.ObjectID = O.ID
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
		I.RuleID,
		R.Name as [Rule],
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
		inner join RuleType RT on RT.ID = R.RuleTypeID 
group by R.RuleTypeID,
		RT.Name,
		I.RuleID,
		R.Name,
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

                        prefix = "Global";

                        #region InterRelationships

                        objectName = string.Format("{0}.[{1}_{2}]", SCHEMA, prefix, "ModelInterRelationships");
                        viewNames.Add(objectName);

                        selectSql = @"
select	R.ID as IntersectID,
		S.ID as SourceID,
		S.Name as SourceName,
        S.TextPath as SourceTextPath,
		ST.Name as SourceType,
		S.[Level] as SourceLevel,
		SL.Name as SourceLevelName,
		dbo.GenerateObjectUrl('Taxonomy', S.TaxonomyTypeID, S.ID) as SourceURL,
		T.ID as TargetID,
		T.Name as TargetName,
        T.TextPath as TargetTextPath,
		TT.Name as TargetType,
		T.[Level] as TargetLevel,
		TL.Name as TargetLevelName,
		dbo.GenerateObjectUrl('Taxonomy', T.TaxonomyTypeID, T.ID) as TargetUrl,
from	[Intersect] R
		inner join Taxonomy S on R.Subject = 'Taxonomy' and S.ID = R.SubjectID
		inner join Taxonomy T on R.Object = 'Taxonomy' and T.ID = R.ObjectID
		inner join TaxonomyType ST on ST.ID = S.TaxonomyTypeID
        left join TaxonomyTypeLevel SL on SL.TaxonomyTypeID = S.TaxonomyTypeID and S.[Level] = SL.[Level]
		inner join TaxonomyType TT on TT.ID = T.TaxonomyTypeID
		left join TaxonomyTypeLevel TL on TL.TaxonomyTypeID = T.TaxonomyTypeID and T.[Level] = TL.[Level]";

                        objectID = companyConnection.Query<string>("select OBJECT_ID(@n, 'V')", new { n = objectName }).First();

                        viewSql = (string.IsNullOrEmpty(objectID)) ? "CREATE " : "ALTER ";
                        viewSql += $@" VIEW {objectName} AS {selectSql}";

                        executeSqlWithTry(companyConnection, viewSql);

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
                                    log.Warning(msg);
                                }
                            }
                        });

                        #endregion

                        companyConnection.Close();
                        companyConnection.Dispose();
                    }
                    catch (Exception ex)
                    {
                        CoreFunction.AITrackException(functionName, ex, c.CompanyID);
                        log.Error($"Company [{c.CompanyID}]: [{ex.GetFullExceptionData()}]");
                    }
                });
            }
            catch (Exception ex)
            {
                CoreFunction.AITrackException(functionName, ex);
                log.Error($"General Exception: {ex.GetFullExceptionData()}");
            }
            finally
            {
                CoreFunction.AITrackJobCompletedNoErrors(functionName);
            }
        }
    }
}
