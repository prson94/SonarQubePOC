using d360.core;
using d360.core.entities;
using d360.core.enums;
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

namespace igx.jobs
{
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

        static void getDynamicFieldJoinStatements(List<FieldType> fields, string type, out string joins, out string columns, string idColumn = "A.ID")
        {
            columns = "";
            joins = "";

            foreach (var f in fields)
            {
                var name = cleanObjectName(f.Name);
                columns += (f.Type == "Lookup") ? $"[{name}].Value as [{name}ID], [{name}].FormattedValue as [{name}], " : $"[{name}].FormattedValue as [{name}], ";
                joins += $" left join FieldDetail [{name}] on [{name}].Object = '{type}' and [{name}].ObjectID = {idColumn} and [{name}].FieldTypeID = {f.ID}";
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

        const string functionName = "ReportingLayer_Generate";
        const string timerSettings = "0 */10 * * * *";
        //const string timerSettings = "*/5 * * * * *";

        public static void Run([TimerTrigger(timerSettings)]TimerInfo myTimer, TextWriter log) //   
        {
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

                            getDynamicFieldJoinStatements(fieldTypes.Where(f => f.ObjectID == o.ID).ToList(), "Artifact", out joins, out columns, "A.ObjectID");

                            objectName = $"{SCHEMA}.[{prefix}_{pluralize.Pluralize(cleanObjectName(o.Name))}]";
                            viewNames.Add(objectName);

                            var parentIntersectType = companyConnection.Query<IntersectTypeDetail>("select * from IntersectTypeDetail where Object = 'ArtifactType' and ObjectID = @id and PredicateType = @pt", new { id = o.ID, pt = (int)PredicateType.InterTypeHierarchy }).FirstOrDefault();

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
                            left join dbo.GetAssetDisplayValue() ID on ID.ID = IA.ID
				    ) P";
                            }


                            selectSql = $@"
select  A.ID, 
        A.DisplayValue, 
        {parentSqlColumn}
        {columns} 
        dbo.GenerateObjectUrl('{objectType}', A.TypeID, A.ObjectID) as Url, 
        cast(S.Value * 100 as int) as CurrentScore 
from    AssetDetail A 
        left join metrics.Score S on S.Object = 'Artifact' and S.ObjectID = A.ObjectID and getutcdate() between S.EffectiveStartDate and S.EffectiveEndDate 
        {joins} 
        {parentSqlJoin} 
where   A.Type = 'ArtifactType' and A.TypeID = {o.ID}";

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
with h as (
	select	T.ID,
			T.TaxonomyTypeID,
			null as ParentID,
			D.DisplayValue as TextPath,
			1 as [Level]
	from	Taxonomy T
			inner join dbo.GetAssetDisplayValue() D on D.Object = '{objectType}' and D.ObjectID = T.ID
			left join PredicateIntersect I on I.Object = '{objectType}' and I.ObjectID = T.ID and I.PredicateType = 4
	where	T.TaxonomyTypeID = {o.ID} and I.IntersectID is null
	union all
	select	T.ID,
			T.TaxonomyTypeID,
			P.ID as ParentID,
			P.TextPath + '/' + D.DisplayValue as TextPath,
			P.[Level] + 1 as [Level]
	from	Taxonomy T
			inner join dbo.GetAssetDisplayValue() D on D.Object = '{objectType}' and D.ObjectID = T.ID
			inner join PredicateIntersect I on I.Object = '{objectType}' and I.ObjectID = T.ID and I.PredicateType = 4
			inner join h as P on I.Subject = '{objectType}' and I.SubjectID = P.ID
	where	T.TaxonomyTypeID = {o.ID}
)

select  A.ID, 
        A.ParentID, 
        A.TextPath, 
        A.[Level], 
        L.Name as LevelName, 
        L.Description as LevelDescription,
        {columns} 
        dbo.GenerateObjectUrl('{objectType}', A.TaxonomyTypeID, A.ID) as Url, 
        cast(S.Value * 100 as int) as CurrentScore
from    h as A  
        {joins} 
        left join metrics.Score S on S.Object = '{objectType}' and S.ObjectID = A.ID and getutcdate() between S.EffectiveStartDate and S.EffectiveEndDate 
        left join TaxonomyTypeLevel L on L.TaxonomyTypeID = A.TaxonomyTypeID and L.[Level] = A.[Level]";

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
with h as (
	select	T.ID,
			T.PolicyTypeID,
			null as ParentID,
			D.DisplayValue,
			D.DisplayValue as TextPath,
			1 as [Level]
	from	Policy T
			inner join dbo.GetAssetDisplayValue() D on D.Object = '{objectType}' and D.ObjectID = T.ID
			left join PredicateIntersect I on I.Object = '{objectType}' and I.ObjectID = T.ID and I.PredicateType = 4
	where	T.PolicyTypeID = {o.ID} and I.IntersectID is null
	union all
	select	T.ID,
			T.PolicyTypeID,
			P.ID as ParentID,
			D.DisplayValue,
			P.TextPath + '/' + D.DisplayValue as TextPath,
			P.[Level] + 1 as [Level]
	from	Policy T
			inner join dbo.GetAssetDisplayValue() D on D.Object = '{objectType}' and D.ObjectID = T.ID
			inner join PredicateIntersect I on I.Object = '{objectType}' and I.ObjectID = T.ID and I.PredicateType = 4
			inner join h as P on I.Subject = '{objectType}' and I.SubjectID = P.ID
	where	T.PolicyTypeID = {o.ID}
)

select  A.ID, 
        A.ParentID, 
        A.TextPath, 
        A.[Level], 
        L.Name as LevelName, 
        L.Description as LevelDescription,
        {columns} 
        dbo.GenerateObjectUrl('{objectType}', A.PolicyTypeID, A.ID) as Url, 
        cast(S.Value * 100 as int) as CurrentScore
from    h as A  
        {joins} 
        left join metrics.Score S on S.Object = '{objectType}' and S.ObjectID = A.ID and getutcdate() between S.EffectiveStartDate and S.EffectiveEndDate 
        left join PolicyTypeLevel L on L.PolicyTypeID = A.PolicyTypeID and L.[Level] = A.[Level]";

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
select  A.ObjectID as ID,
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
    	left join metrics.Score S on S.Object = 'Artifact' and S.ObjectID = A.ID and getutcdate() between S.EffectiveStartDate and S.EffectiveEndDate 
where	A.AssetTypeClass = 1";

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
select 	O.ObjectID, 
	    O.TypeID as ArtifactTypeID, 
	    O.DisplayValue, 
	    F.FieldTypeID, 
        FT.Name as FieldName, 
        FT.FriendlyName as FieldFriendlyName, 
	    F.FormattedValue as FieldValue 
from	AssetDetail O 
        inner join Field F on F.AssetID = O.ID
	    inner join FieldType FT on FT.ID = F.FieldTypeID
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
with h as (
	select	T.ID,
			T.TaxonomyTypeID,
			null as ParentID,
			D.DisplayValue as TextPath,
			1 as [Level]
	from	Taxonomy T
			inner join dbo.GetAssetDisplayValue() D on D.Object = 'Taxonomy' and D.ObjectID = T.ID
			left join PredicateIntersect I on I.Object = 'Taxonomy' and I.ObjectID = T.ID and I.PredicateType = 4
	where	I.IntersectID is null
	union all
	select	T.ID,
			T.TaxonomyTypeID,
			P.ID as ParentID,
			P.TextPath + '/' + D.DisplayValue as TextPath,
			P.[Level] + 1 as [Level]
	from	Taxonomy T
			inner join dbo.GetAssetDisplayValue() D on D.Object = 'Taxonomy' and D.ObjectID = T.ID
			inner join PredicateIntersect I on I.Object = 'Taxonomy' and I.ObjectID = T.ID and I.PredicateType = 4
			inner join h as P on I.Subject = 'Taxonomy' and I.SubjectID = P.ID
)

SELECT  T.[ID] as [Taxonomy ID],
        T.[ParentID],
        T.[TaxonomyTypeID] as [Taxonomy Type id],
        T.DisplayValue,
        h.[TextPath],
        T.[Level] as [Taxonomy Level],
        T.[UpdatedOn],
        T.[UpdatedBy], 
        CONVERT(VARCHAR(10), t.UpdatedOn, 112) as UpdatedOnKey,
        TY.Name as [Taxonomy Type Name],
        coalesce(TL.Name, 'Level ' + cast(h.[Level] as varchar)) as [Level Name]
FROM    Taxonomy T
        inner join h on h.ID = T.ID
        inner join TaxonomyType Ty on TY.ID = t.TaxonomyTypeID
        left join TaxonomyTypeLevel TL on TL.[TaxonomyTypeID] = TY.ID and TL.[Level] = h.[Level]";

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

                        #region Reference

                        objectName = $"{SCHEMA}.[Reference_All]";
                        viewNames.Add(objectName);

                        selectSql = @"
select		T.Name as ReferenceItemType,
			I.ReferenceItemTypeID,
			I.ID as ReferenceItemID,
			I.Code,
			I.DisplayValue,
			I.CreatedBy,
			I.CreatedOn,
			I.UpdatedBy,
			I.UpdatedOn
from		ReferenceItem I
			inner join ReferenceItemType T on T.ID = I.ReferenceItemTypeID and I.Visible = 1";

                        objectID = companyConnection.Query<string>("select OBJECT_ID(@n, 'V')", new { n = objectName }).First();

                        viewSql = (string.IsNullOrEmpty(objectID)) ? "CREATE " : "ALTER ";
                        viewSql += $@" VIEW {objectName} AS {selectSql}";

                        executeSqlWithTry(companyConnection, viewSql);

                        #endregion

                        #region Reference_Fields

                        objectName = $"{SCHEMA}.[Reference_Fields]";
                        viewNames.Add(objectName);

                        selectSql = @"
select		I.ReferenceItemTypeID,
			I.ID as ReferenceItemID,
			T.Name as FieldTypeName,
			T.FriendlyName as FieldTypeFriendlyName,
			F.FormattedValue
from		Field F
			inner join FieldType T on T.ID = F.FieldTypeID
			inner join ReferenceItem I on F.ObjectType = 'ReferenceItem' and I.ID = F.ObjectID and I.Visible = 1";

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
	select	T.ID,
			T.PolicyTypeID,
			T.UpdatedOn,
			T.UpdatedBy,
			null as ParentID,
			D.DisplayValue,
			D.DisplayValue as TextPath,
			1 as [Level]
	from	Policy T
			inner join dbo.GetAssetDisplayValue() D on D.Object = 'Policy' and D.ObjectID = T.ID
			left join PredicateIntersect I on I.Object = 'Policy' and I.ObjectID = T.ID and I.PredicateType = 4
	where	I.IntersectID is null
	union all
	select	T.ID,
			T.PolicyTypeID,
			T.UpdatedOn,
			T.UpdatedBy,
			p.ID as ParentID,
			D.DisplayValue,
			p.TextPath + '.' + D.DisplayValue as TextPath,
			p.[Level] + 1 as [Level]
	from	Policy T
			inner join dbo.GetAssetDisplayValue() D on D.Object = 'Policy' and D.ObjectID = T.ID
			inner join PredicateIntersect I on I.Object = 'Policy' and I.ObjectID = T.ID and I.PredicateType = 4
			inner join p on I.Subject = 'Policy' and I.SubjectID = p.ID
)

SELECT  p.[ID] as [PolicyID],
        p.[ParentID],
        D.DisplayValue,
		p.TextPath,
        p.[UpdatedOn],
        p.[UpdatedBy],
        p.[PolicyTypeID],
        p.[Level],
        pt.Name as [Policy Type],
        ptl.Name as [Policy Level Name],
        CONVERT(VARCHAR(10), p.UpdatedOn, 112) as UpdatedOnKey
FROM    p
        inner join dbo.GetAssetDisplayValue() D on D.Object = 'Policy' and D.ObjectID = p.ID
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

                        selectSql = @"SELECT  ID as IntersectID, * FROM IntersectDetail";

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
	    D.DisplayValue as Name,
	    R.RuleTypeID,
        RT.Name as RuleType,
        R.RuleDimensionID,
	    RD.Name as RuleDimensionName,
	    R.Threshold,
	    R.Status,
	    R.CreatedOn,
	    R.CreatedBy,
	    R.UpdatedOn,
	    R.UpdatedBy
from	[Rule] R
        inner join dbo.GetAssetDisplayValue() D on D.Object = 'Rule' and D.ObjectID = R.ID 
        inner join RuleType RT on RT.ID = R.RuleTypeID 
        left join RuleDimension RD on RD.ID = R.RuleDimensionID";

                        objectID = companyConnection.Query<string>("select OBJECT_ID(@n, 'V')", new { n = objectName }).First();

                        viewSql = (string.IsNullOrEmpty(objectID)) ? "CREATE " : "ALTER ";
                        viewSql += $@" VIEW {objectName} AS {selectSql}";

                        executeSqlWithTry(companyConnection, viewSql);

                        #endregion

                        #region Rule_Fields

                        objectName = $"{SCHEMA}.[Rules_Fields]";
                        viewNames.Add(objectName);

                        selectSql = @"
select 	O.ID as RuleID, 
	    D.DisplayValue as RuleName, 
	    F.FieldTypeID, 
        FT.Name as FieldName, 
        FT.FriendlyName as FieldFriendlyName, 
	    F.FormattedValue as FieldValue 
from	[Rule] O 
	    inner join dbo.GetAssetDisplayValue() D on D.Object = 'Rule' and D.ObjectID = O.ID 
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
		D.DisplayValue as [Rule],
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
        inner join dbo.GetAssetDisplayValue() D on D.Object = 'Rule' and D.ObjectID = R.ID 
		inner join RuleType RT on RT.ID = R.RuleTypeID 
group by R.RuleTypeID,
		RT.Name,
		I.RuleID,
		D.DisplayValue,
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
		SD.DisplayValue as SourceName,
		ST.Name as SourceType,
		dbo.GenerateObjectUrl('Taxonomy', S.TaxonomyTypeID, S.ID) as SourceUrl,
		T.ID as TargetID,
		TD.DisplayValue as TargetName,
		TT.Name as TargetType,
		dbo.GenerateObjectUrl('Taxonomy', T.TaxonomyTypeID, T.ID) as TargetUrl 
from	[Intersect] R
		inner join Taxonomy S on R.Subject = 'Taxonomy' and S.ID = R.SubjectID
        inner join dbo.GetAssetDisplayValue() SD on SD.Object = R.Subject and SD.ObjectID = R.SubjectID 
        inner join TaxonomyType ST on ST.ID = S.TaxonomyTypeID 
        inner join Taxonomy T on R.Object = 'Taxonomy' and T.ID = R.ObjectID 
        inner join dbo.GetAssetDisplayValue() TD on TD.Object = R.Object and TD.ObjectID = R.ObjectID 
        inner join TaxonomyType TT on TT.ID = T.TaxonomyTypeID";

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
                                    log.WriteLine(msg);
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
                    }
                });
            }
            catch (Exception ex)
            {
                CoreFunction.AITrackException(functionName, ex);
            }
            finally
            {
                CoreFunction.AITrackJobCompletedNoErrors(functionName);
            }
        }
    }
}
