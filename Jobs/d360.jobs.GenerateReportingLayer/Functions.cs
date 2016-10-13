using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Entity.Design.PluralizationServices;
using d360.core.entities;
using System.Data.SqlClient;
using d360.core;
using Dapper;
using System.Text.RegularExpressions;

namespace d360.jobs.GenerateReportingLayer
{
    public class Functions : FunctionsBase
    {
        #region Utility

        static string cleanObjectName(string name)
        {
            name = name.Replace("'", "").Replace(" ", "").Replace("-", "").Replace("&", "And").Replace(":", "").Replace(";", "").Trim();
            Regex rgx = new Regex("[^a-zA-Z0-9-]");
            name = rgx.Replace(name, "");
            return name;
        }

        static void getDynamicFieldJoinStatements(List<FieldTypeWithRelation> fields, string type, out string joins, out string columns)
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

                joins += string.Format(" left join FieldWithRelation [{0}] on [{0}].ObjectType = '{2}' and [{0}].ObjectID = A.ID and [{0}].FieldTypeID = {1}", name, f.ID, type);
            }

            fields = null;
        }

        #endregion

        #region Object Missing Views

        static void GenerateMissingAttributeView(List<string> viewNames, SqlConnection companyConnection, string schema, string prefix, string name, string objectTypeKeyName, string tableName, string objectType, int typeID, bool includeOwningModel = false)
        {
            name = cleanObjectName(name);
            var objectName = string.Format("{0}.[{1}_{2}MissingAttributes]", schema, prefix, name);
            viewNames.Add(objectName);

            var objectID = companyConnection.Query<string>("select OBJECT_ID(@n, 'V')", new { n = objectName }).First();

            var sql = new StringBuilder("");
            sql.Append((string.IsNullOrEmpty(objectID)) ? "CREATE " : "ALTER ");
            sql.AppendFormat("VIEW {0} AS ", objectName);

            sql.AppendFormat("select A.ID as {0}ID, A.Name as {0}Name, ", name);
            if (includeOwningModel) sql.Append("A.Status, V.ID as SubjectAreaID, V.Name as SubjectArea, ");
            sql.AppendFormat("[dbo].GenerateObjectUrl('{0}', A.{1}, A.ID) as Url, Attr.AttributeType, Attr.Category, Attr.[Count] ", objectType, objectTypeKeyName);
            sql.AppendFormat("from {0} A ", tableName);
            if (includeOwningModel) sql.Append("inner join TaxonomyType V on V.ID = A.TaxonomyTypeID ");
            sql.AppendFormat(@"cross apply (
					select		coalesce(C.Name, 'Enterprise-wide') as Category,
								AT.Name as AttributeType,
								count(ATTR.ID) as [Count] 
					from		AttributeTypeRelation ATR
								inner join AttributeType AT on AT.ID = ATR.AttributeTypeID and ATR.ObjectType = '{0}Type' and ATR.ObjectID = A.{2}
								left join AttributeTypeCategory C on C.ID = AT.AttributeTypeCategoryID
								left join Attribute ATTR on ATTR.ObjectType = '{0}' and ATTR.ObjectID = A.ID
					group by	C.Name,
								AT.Name
					) Attr
where	A.{2} = {1}", objectType, typeID, objectTypeKeyName);

            try 
            {
                companyConnection.Execute(sql.ToString());
            }
            catch (Exception ex)
            {
                var msg = ex.GetFullExceptionData() + " Stack: " + ex.StackTrace;
                Console.WriteLine(msg);
                Console.WriteLine("Attempted SQL: " + sql);
            }
        }

        static void GenerateMissingRelationshipView(List<string> viewNames, SqlConnection companyConnection, string schema, string prefix, string name, string objectTypeKeyName, string tableName, string objectType, int typeID, bool includeOwningModel = false)
        {
            name = cleanObjectName(name);
            var objectName = string.Format("{0}.[{1}_{2}MissingRelationships]", schema, prefix, name);
            viewNames.Add(objectName);

            var objectID = companyConnection.Query<string>("select OBJECT_ID(@n, 'V')", new { n = objectName }).First();

            var owningModelColumns = (includeOwningModel) ? "A.Status, V.ID as SubjectAreaID, V.Name as SubjectArea, " : "";
            var owningModelJoins = (includeOwningModel) ? "inner join TaxonomyType V on V.ID = A.TaxonomyTypeID " : "";

            var selectSql = string.Format(@"select	A.ID as [{0}ID], A.Name as [{0}Name], {5}[dbo].GenerateObjectUrl('{1}', A.{3}, A.ID) as Url, O.RelationshipType, O.RelationshipTypeID, O.[Count]
from	{4} A {6}
		cross apply (
					select	IT.ID as RelationshipTypeID,
                            IT.Name as RelationshipType,
							count(I.ID)as [Count]
					from	IntersectType IT
							left join [Intersect] I on I.IntersectTypeID = IT.ID and ( (I.Subject = '{1}' and I.SubjectID = A.ID) OR (I.Object = '{1}' and I.ObjectID = A.ID) )
					where	( (IT.Subject = '{1}Type' and IT.SubjectID = A.{3}) OR (IT.Subject = '{1}Type' and IT.SubjectID = A.{3}) )
							and I.ID is null
					group by IT.ID, IT.Name
					) O
where	A.{3} = {2}", name, objectType, typeID, objectTypeKeyName, tableName, owningModelColumns, owningModelJoins);

            var viewSql = (string.IsNullOrEmpty(objectID)) ? "CREATE " : "ALTER ";
            viewSql += string.Format(@" VIEW {0} AS {1}", objectName, selectSql);

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

        static void GenerateMissingResponsibilityView(List<string> viewNames, SqlConnection companyConnection, string schema, string prefix, string name, string objectTypeKeyName, string tableName, string objectType, int typeID, bool includeOwningModel = false)
        {
            name = cleanObjectName(name);
            var objectName = string.Format("{0}.[{1}_{2}MissingResponsibilities]", schema, prefix, name);
            viewNames.Add(objectName);

            var objectID = companyConnection.Query<string>("select OBJECT_ID(@n, 'V')", new { n = objectName }).First();

            var owningModelColumns = (includeOwningModel) ? "A.Status, V.ID as SubjectAreaID, V.Name as SubjectArea, " : "";
            var owningModelJoins = (includeOwningModel) ? "inner join TaxonomyType V on V.ID = A.TaxonomyTypeID " : "";

            var selectSql = string.Format(@"select	A.ID as {0}ID, A.Name as {0}Name, {5}[dbo].GenerateObjectUrl('{1}', A.{3}, A.ID) as Url, T.ID as ResponsibilityTypeID, T.Name as ResponsibilityType, coalesce(O.[Count], 0) as [Count]
from	{4} A {6}
inner join ResponsibilityTypeRelation R on R.ObjectType = '{1}Type' and R.ObjectID = A.{3} and A.{3} = {2}
inner join ResponsibilityType T on R.ResponsibilityTypeID = T.ID and T.ResponsibilityTypeGroup = 1
outer apply (
			select	ResponsibilityTypeID,
					count(1) as [Count]
			from	[cache].[Responsibilities] --utility.ResponsibilityHierarchy
			where	[Object] = '{1}' and ObjectID = A.ID and ResponsibilityTypeID = T.ID
			group by ResponsibilityTypeID
			) O", name, objectType, typeID, objectTypeKeyName, tableName, owningModelColumns, owningModelJoins);

            var viewSql = (string.IsNullOrEmpty(objectID)) ? "CREATE " : "ALTER ";
            viewSql += string.Format(@" VIEW {0} AS {1}", objectName, selectSql);

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

        static void GenerateMissingOverallView(List<string> viewNames, SqlConnection companyConnection, string schema, string prefix, string name, string objectTypeKeyName, string tableName, string objectType, int typeID, bool includeOwningModel = false)
        {
            name = cleanObjectName(name);
            var objectName = string.Format("{0}.[{1}_{2}Missing]", schema, prefix, name);
            viewNames.Add(objectName);

            var objectID = companyConnection.Query<string>("select OBJECT_ID(@n, 'V')", new { n = objectName }).First();

            var owningModelColumns = (includeOwningModel) ? "A.Status, V.ID as SubjectAreaID, V.Name as SubjectArea, " : "";
            var owningModelJoins = (includeOwningModel) ? "inner join TaxonomyType V on V.ID = A.TaxonomyTypeID " : "";

            var selectSql = string.Format(@"select	A.ID as {0}ID, A.Name as {0}Name, {5}[dbo].GenerateObjectUrl('{1}', A.{3}, A.ID) as Url, Att.MissingAttributes, Rel.MissingRelationships, Res.MissingResponsibilities
from	{4} A {6}
		cross apply (
					select		case 
									when count(ATTR.ID) < count(ATR.AttributeTypeID) then cast(1 as bit)
									else cast(0 as bit)
								end as MissingAttributes
					from		AttributeTypeRelation ATR
								inner join AttributeType AT on AT.ID = ATR.AttributeTypeID and ATR.ObjectType = '{1}Type' and ATR.ObjectID = A.{3}
								left join AttributeTypeCategory C on C.ID = AT.AttributeTypeCategoryID
								left join Attribute ATTR on ATTR.ObjectType = '{1}' and ATTR.ObjectID = A.ID
					) Att
		cross apply (
					select	case 
								when count(1) > 0 then cast(1 as bit)
								else cast(0 as bit)
							end as MissingRelationships
					from	( 
					        select	case 
								        when count(I.ID) = 0 then cast(1 as bit)
								        else cast(0 as bit)
							        end as O
					        from	IntersectType IT
							        left join [Intersect] I on I.IntersectTypeID = IT.ID and ( (I.Subject = '{1}' and I.SubjectID = A.ID) OR (I.Object = '{1}' and I.ObjectID = A.ID) )
					        where	( (IT.Subject = '{1}Type' and IT.SubjectID = A.{3}) OR (IT.Subject = '{1}Type' and IT.SubjectID = A.{3}) )
							        and I.ID is null
					        group by IT.ID having count(I.ID) = 0
                            ) R
					) Rel
		cross apply (
					select	case 
								when count(1) > 0 then cast(1 as bit)
								else cast(0 as bit)
							end as MissingResponsibilities
					from	( 
							select	case 
										when count(1) = 0 then cast(1 as bit)
										else cast(0 as bit) 
									end as MissingResponsibilities
							from	[cache].[Responsibilities] --utility.ResponsibilityHierarchy
							where	[Object] = '{1}' and ObjectID = A.ID
							group by ResponsibilityTypeID having count(1) = 0
							) R
					) Res
where	A.{3} = {2}", name, objectType, typeID, objectTypeKeyName, tableName, owningModelColumns, owningModelJoins);

            var viewSql = (string.IsNullOrEmpty(objectID)) ? "CREATE " : "ALTER ";
            viewSql += string.Format(@" VIEW {0} AS {1}", objectName, selectSql);

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

        static void GenerateAttributeView(List<string> viewNames, SqlConnection companyConnection, string schema, string prefix, string name, string objectTypeKeyName, string tableName, string objectType, int typeID, bool includeOwningModel = false)
        {
            name = cleanObjectName(name);
            var objectName = string.Format("{0}.[{1}_{2}Attributes]", schema, prefix, name);
            viewNames.Add(objectName);

            var objectID = companyConnection.Query<string>("select OBJECT_ID(@n, 'V')", new { n = objectName }).First();

            var sql = new StringBuilder("");
            sql.Append((string.IsNullOrEmpty(objectID)) ? "CREATE " : "ALTER ");
            sql.AppendFormat("VIEW {0} AS ", objectName);

            sql.AppendFormat("select A.ID as {0}ID, A.Name as {0}Name, ", name);
            if (includeOwningModel) sql.Append("V.ID as SubjectAreaID, V.Name as SubjectArea, ");
            sql.AppendFormat("[dbo].GenerateObjectUrl('{0}', A.{1}, A.ID) as {0}Url, AD.ID as AttributeID, AD.ParentID as ParentAttributeID, AD.Name as Attribute, AD.FormattedValue as AttributeValue ", objectType, objectTypeKeyName);
            sql.AppendFormat("from {0} A ", tableName);
            if (includeOwningModel) sql.Append("inner join TaxonomyType V on V.ID = A.TaxonomyTypeID ");
            sql.AppendFormat(@"inner join AttributeDetail AD on AD.ObjectType = '{0}' and AD.ObjectID = A.ID and A.{2} = {1}", objectType, typeID, objectTypeKeyName);

            try
            {
                companyConnection.Execute(sql.ToString());
            }
            catch (Exception ex)
            {
                var msg = ex.GetFullExceptionData() + " Stack: " + ex.StackTrace;
                Console.WriteLine(msg);
                Console.WriteLine("Attempted SQL: " + sql);
            }
        }

        static void GeneratObjectRelationshipView(List<string> viewNames, SqlConnection companyConnection, string schema, string prefix, string name, string objectTypeKeyName, string tableName, string objectType, int typeID, bool includeOwningModel = false)
        {
            name = cleanObjectName(name);
            var objectName = string.Format("{0}.[{1}_{2}Relationships]", schema, prefix, name);
            viewNames.Add(objectName);

            var objectID = companyConnection.Query<string>("select OBJECT_ID(@n, 'V')", new { n = objectName }).First();

            var sql = new StringBuilder("");
            sql.Append((string.IsNullOrEmpty(objectID)) ? "CREATE " : "ALTER ");
            sql.AppendFormat("VIEW {0} AS ", objectName);

            sql.AppendFormat("select R.ID as IntersectID, A.ID as [{0}ID], A.Name as [{0}Name], ", name);
            if (includeOwningModel) sql.Append("A.Status, V.ID as SubjectAreaID, V.Name as SubjectArea, ");
            sql.Append(@"case when (R.Subject = 'Artifact' and A.ID = R.SubjectID) then R.ObjectTypeName else R.SubjectTypeName end as TargetType, 
case when(R.Subject = 'Artifact' and A.ID = R.SubjectID) then R.Object else R.Subject end as Target,
case when(R.Subject = 'Artifact' and A.ID = R.SubjectID) then R.ObjectID else R.SubjectID end as TargetID,
case when(R.Subject = 'Artifact' and A.ID = R.SubjectID) then R.ObjectName else R.SubjectName end as TargetName,
case when(R.Subject = 'Artifact' and A.ID = R.SubjectID) then R.ObjectUrl else R.SubjectUrl end as TargetUrl,
case R.Classification when 1 then 'Critical' else 'Normal' end as Classification,
R.Description,
TR.[Count] as ChildRelationshipCount ");
            sql.Append("from IntersectDetail R ");
            sql.Append($"inner join {tableName} A on A.{objectTypeKeyName} = {typeID} and ((R.Subject = '{objectType}' and A.ID = R.SubjectID) OR (R.Object = '{objectType}' and A.ID = R.ObjectID)) ");
            if (includeOwningModel) sql.Append("inner join TaxonomyType V on V.ID = A.TaxonomyTypeID ");
            sql.Append("outer apply (select	count(1) as [Count] from [Intersect] where Subject = 'Intersect' and SubjectID = R.ID) TR");

            try
            {
                companyConnection.Execute(sql.ToString());
            }
            catch (Exception ex)
            {
                var msg = ex.GetFullExceptionData() + " Stack: " + ex.StackTrace;
                Console.WriteLine(msg);
                Console.WriteLine("Attempted SQL: " + sql);
            }
        }

        static void GenerateObjectResponsibilityView(List<string> viewNames, SqlConnection companyConnection, string schema, string prefix, string name, string objectType, int typeID)
        {
            name = cleanObjectName(name);
            var objectName = string.Format("{0}.[{1}_{2}Responsibilities]", schema, prefix, name);
            viewNames.Add(objectName);

            var objectID = companyConnection.Query<string>("select OBJECT_ID(@n, 'V')", new { n = objectName }).First();

            var selectSql = string.Format(@"select	R.ObjectID as {0}ID,
		R.ObjectName as Name,
		R.ObjectUrl as Url,
		R.ResponsibleObjectName,
		R.ResponsibleObjectType,
		R.ResponsibleObjectUrl,
		R.PrimaryOwnerResourceID,
		R.PrimaryOwnerResourceName,
		R.PrimaryOwnerResourceUrl,
		R.Role,
		--R.RedFlagged,
		R.CurrentScore,
		R.ContextItems,
		R.AssigningItemType,
		R.AssigningItemID--,
		--R.AssigningItemName,
		--R.AssigningItemUrl,
		--R.AssigningTypeName
from	ResponsibilityDetail R
where	R.ObjectType = '{1}' and R.ObjectTypeID = {2}", name, objectType, typeID);

            var viewSql = (string.IsNullOrEmpty(objectID)) ? "CREATE " : "ALTER ";
            viewSql += string.Format(@" VIEW {0} AS {1}", objectName, selectSql);

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

        public static List<Exception> Generate()
        {
            var viewNames = new List<string>();

            var mex = new List<Exception>();
            
            string SCHEMA = "reporting";

            try
            {

                var companies = GetActiveCompanyIDs();

#if DEBUG
                companies = GetActiveCompanyIDs().Where(i => i == 4).ToList();
#endif

                companies.ForEach(companyID =>
                {
                    try
                    {
                        Console.WriteLine("BEGIN COMPANY {0} -----------", companyID);

                        var companyConnection = GetCompanyConnection(companyID);
                        companyConnection.Open();

                        var selectSql = "";
                        var viewSql = "";
                        var objectName = "";
                        var objectType = "Artifact";
                        var objectTypeKey = "ArtifactTypeID";
                        var prefix = "Glossary";
                        var tableName = "Artifact";
                        string objectID;
                        List<FieldTypeWithRelation> fieldTypes = null;

                        var pluralize = PluralizationService.CreateService(System.Globalization.CultureInfo.CurrentCulture);

                        #region Artifact Type

                        var artifactTypes = companyConnection.Query<ArtifactType>("select * from ArtifactType").ToList();

                        try
                        {
                            fieldTypes = companyConnection.Query<FieldTypeWithRelation>("select * from FieldTypeWithRelation where [Object] = 'ArtifactType'").ToList();
                        }
                        catch (Exception)
                        {
                            fieldTypes = companyConnection.Query<FieldTypeWithRelation>("select * from FieldTypeWithRelation where [ObjectType] = 'ArtifactType'").ToList();
                        }

                        artifactTypes.ForEach(o =>
                        {
                            #region Object Views

                            var joins = "";
                            var columns = "";

                            getDynamicFieldJoinStatements(fieldTypes.Where(f => f.ObjectID == o.ID).ToList(), "Artifact", out joins, out columns);

                            objectName = string.Format("{0}.[{1}_{2}]", SCHEMA, prefix, pluralize.Pluralize(cleanObjectName(o.Name)));
                            viewNames.Add(objectName);

                            selectSql = string.Format(@"select A.ID, A.Name, A.TextPath, A.Description, A.Status, V.ID as SubjectAreaID, V.Name as SubjectArea, {0} dbo.GenerateObjectUrl('{3}', A.ArtifactTypeID, A.ID) as Url, dbo.GetObjectStatisticScore('Artifact', A.ID) as CurrentScore, AC.AttributeCount, Rels.[Count] as RelationshipCount from Artifact A inner join TaxonomyType V on V.ID = A.TaxonomyTypeID {2} cross apply (select count(1) as AttributeCount from Attribute where ObjectType = '{3}' and ObjectID = A.ID) AC cross apply (select count(1) as [Count] from cache.Relationships where SourceObject = '{3}' and SourceObjectID = A.ID) Rels where A.ArtifactTypeID = {1}", columns, o.ID, joins, objectType);

                            objectID = companyConnection.Query<string>("select OBJECT_ID(@n, 'V')", new { n = objectName }).First();

                            viewSql = (string.IsNullOrEmpty(objectID)) ? "CREATE " : "ALTER ";
                            viewSql += string.Format(@" VIEW {0} AS {1}", objectName, selectSql);

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

                            #endregion

                            // Object Views
                            GenerateAttributeView(viewNames, companyConnection, SCHEMA, prefix, o.Name, objectTypeKey, tableName, objectType, o.ID, true);
                            GeneratObjectRelationshipView(viewNames, companyConnection, SCHEMA, prefix, o.Name, objectTypeKey, tableName, objectType, o.ID, true);
                            GenerateObjectResponsibilityView(viewNames, companyConnection, SCHEMA, prefix, o.Name, objectType, o.ID);

                            // Object Missing Views
                            GenerateMissingOverallView(viewNames, companyConnection, SCHEMA, prefix, o.Name, objectTypeKey, tableName, objectType, o.ID, true);
                            GenerateMissingAttributeView(viewNames, companyConnection, SCHEMA, prefix, o.Name, objectTypeKey, tableName, objectType, o.ID, true);
                            GenerateMissingRelationshipView(viewNames, companyConnection, SCHEMA, prefix, o.Name, objectTypeKey, tableName, objectType, o.ID, true);
                            GenerateMissingResponsibilityView(viewNames, companyConnection, SCHEMA, prefix, o.Name, objectTypeKey, tableName, objectType, o.ID, true);
                        });

                        artifactTypes = null;

                        #endregion

                        #region Fusion

                        //var fusionAttributeTypes = companyConnection.Query<FusionAttributeType>("select * from FusionAttributeType").ToList();

                        //try
                        //{
                        //    fieldTypes = companyConnection.Query<FieldTypeWithRelation>("select * from FieldTypeWithRelation where [Object] = 'FusionAttributeType'").ToList();
                        //}
                        //catch (Exception)
                        //{
                        //    fieldTypes = companyConnection.Query<FieldTypeWithRelation>("select * from FieldTypeWithRelation where [ObjectType] = 'FusionAttributeType'").ToList();
                        //}

                        //fusionAttributeTypes.ForEach(o =>
                        //{
                        //    objectType = "FusionAttribute";
                        //    objectTypeKey = "FusionAttributeTypeID";
                        //    prefix = "Fusion";
                        //    tableName = "FusionAttribute";

                        //    #region Object Views

                        //    var joins = "";
                        //    var columns = "";

                        //    getDynamicFieldJoinStatements(fieldTypes.Where(f => f.ObjectID == o.ID).ToList(), "FusionAttribute", out joins, out columns);

                        //    objectName = string.Format("{0}.[{1}_{2}]", SCHEMA, prefix, pluralize.Pluralize(cleanObjectName(o.TextPath.Replace(".", "").Replace("_", ""))));
                        //    viewNames.Add(objectName);

                        //    selectSql = string.Format(@"select FT.Name as FusionType, F.Name as Fusion, A.ID, A.Name as [Attribute], A.TextPath, {0} AC.AttributeCount, Rels.[Count] as RelationshipCount from FusionAttribute A inner join Fusion F on F.ID = A.FusionID and A.FusionAttributeTypeID = {1} inner join FusionType FT on FT.ID = F.FusionTypeID {2} cross apply (select count(1) as AttributeCount from Attribute where ObjectType = '{3}' and ObjectID = A.ID) AC cross apply (select count(1) as [Count] from cache.Relationships where SourceObject = '{3}' and SourceObjectID = A.ID) Rels where A.FusionAttributeTypeID = {1}", columns, o.ID, joins, objectType);

                        //    objectID = companyConnection.Query<string>("select OBJECT_ID(@n, 'V')", new { n = objectName }).First();

                        //    viewSql = (string.IsNullOrEmpty(objectID)) ? "CREATE " : "ALTER ";
                        //    viewSql += string.Format(@" VIEW {0} AS {1}", objectName, selectSql);

                        //    try
                        //    {
                        //        companyConnection.Execute(viewSql.ToString());
                        //    }
                        //    catch (Exception ex)
                        //    {
                        //        var msg = ex.GetFullExceptionData() + " Stack: " + ex.StackTrace;
                        //        Console.WriteLine(msg);
                        //        Console.WriteLine("Attempted SQL: " + viewSql);
                        //    }

                        //    #endregion

                        //    // Object Views
                        //    //GenerateAttributeView(viewNames, companyConnection, SCHEMA, prefix, o.Name, objectTypeKey, tableName, objectType, o.ID, true);
                        //    GeneratObjectRelationshipView(viewNames, companyConnection, SCHEMA, prefix, o.TextPath.Replace(".", "").Replace("_", ""), objectTypeKey, tableName, objectType, o.ID, false);
                        //    //GenerateObjectResponsibilityView(viewNames, companyConnection, SCHEMA, prefix, o.Name, objectType, o.ID);

                        //    // Object Missing Views
                        //    //GenerateMissingOverallView(viewNames, companyConnection, SCHEMA, prefix, o.Name, objectTypeKey, tableName, objectType, o.ID, true);
                        //    //GenerateMissingAttributeView(viewNames, companyConnection, SCHEMA, prefix, o.Name, objectTypeKey, tableName, objectType, o.ID, true);
                        //    //GenerateMissingRelationshipView(viewNames, companyConnection, SCHEMA, prefix, o.Name, objectTypeKey, tableName, objectType, o.ID, true);
                        //    //GenerateMissingResponsibilityView(viewNames, companyConnection, SCHEMA, prefix, o.Name, objectTypeKey, tableName, objectType, o.ID, true);
                        //});

                        //fusionAttributeTypes = null;

                        #endregion

                        #region Information Model Type

                        prefix = "Glossary";
                        objectType = "Taxonomy";
                        objectTypeKey = "TaxonomyTypeID";
                        prefix = "Model";
                        tableName = "Taxonomy";

                        var taxonomyTypes = companyConnection.Query<TaxonomyType>("select * from TaxonomyType").ToList();

                        try
                        {
                            fieldTypes = companyConnection.Query<FieldTypeWithRelation>("select * from FieldTypeWithRelation where [Object] = 'TaxonomyType'").ToList();
                        }
                        catch (Exception)
                        {
                            fieldTypes = companyConnection.Query<FieldTypeWithRelation>("select * from FieldTypeWithRelation where [ObjectType] = 'TaxonomyType'").ToList();
                        }

                        taxonomyTypes.ForEach(o =>
                        {
                            #region Object Views

                            var joins = "";
                            var columns = "";

                            getDynamicFieldJoinStatements(fieldTypes.Where(f => f.ObjectID == o.ID).ToList(), "Taxonomy", out joins, out columns);

                            objectName = string.Format("{0}.[{1}_{2}]", SCHEMA, prefix, pluralize.Pluralize(cleanObjectName(o.Name)));
                            viewNames.Add(objectName);

                            selectSql = string.Format(@"select A.ID, A.ParentID, A.Name, A.TextPath, A.Description, A.[Level], L.Name as LevelName, L.Description as LevelDescription, 
    C.Name as Class,
    {0} dbo.GenerateObjectUrl('{3}', A.TaxonomyTypeID, A.ID) as Url, dbo.GetObjectStatisticScore('Taxonomy', A.ID) as CurrentScore, AC.AttributeCount, Rels.[Count] as RelationshipCount 
    from Taxonomy A {2} 
    inner join TaxonomyType T on T.ID = A.TaxonomyTypeID
    inner join TaxonomyTypeClass C on C.ID = T.TaxonomyTypeClassID
    left join TaxonomyTypeLevel L on L.TaxonomyTypeID = A.TaxonomyTypeID and L.[Level] = A.[Level]
    cross apply (select count(1) as AttributeCount from Attribute where ObjectType = '{3}' and ObjectID = A.ID) AC 
    cross apply (select count(1) as [Count] from cache.Relationships where SourceObject = '{3}' and SourceObjectID = A.ID) Rels
    where A.TaxonomyTypeID = {1}", columns, o.ID, joins, objectType);

                            objectID = companyConnection.Query<string>("select OBJECT_ID(@n, 'V')", new { n = objectName }).First();

                            viewSql = (string.IsNullOrEmpty(objectID)) ? "CREATE " : "ALTER ";
                            viewSql += string.Format(@" VIEW {0} AS {1}", objectName, selectSql);

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

                            #endregion

                            // Object Views
                            GenerateAttributeView(viewNames, companyConnection, SCHEMA, prefix, o.Name, objectTypeKey, tableName, objectType, o.ID);
                            GeneratObjectRelationshipView(viewNames, companyConnection, SCHEMA, prefix, o.Name, objectTypeKey, tableName, objectType, o.ID);
                            GenerateObjectResponsibilityView(viewNames, companyConnection, SCHEMA, prefix, o.Name, objectType, o.ID);

                            // Object Missing Views
                            GenerateMissingOverallView(viewNames, companyConnection, SCHEMA, prefix, o.Name, objectTypeKey, tableName, objectType, o.ID);
                            GenerateMissingAttributeView(viewNames, companyConnection, SCHEMA, prefix, o.Name, objectTypeKey, tableName, objectType, o.ID);
                            GenerateMissingRelationshipView(viewNames, companyConnection, SCHEMA, prefix, o.Name, objectTypeKey, tableName, objectType, o.ID);
                            GenerateMissingResponsibilityView(viewNames, companyConnection, SCHEMA, prefix, o.Name, objectTypeKey, tableName, objectType, o.ID);
                        });

                        taxonomyTypes = null;

                        #endregion

                        #region Domain Type

                        objectType = "Domain";
                        objectTypeKey = "DomainTypeID";
                        prefix = "Domain";
                        tableName = "Domain";

                        var domainTypes = companyConnection.Query<DomainType>("select * from DomainType").ToList();

                        domainTypes.ForEach(o =>
                        {
                            #region Object Views

                            objectName = string.Format("{0}.[{1}_{2}]", SCHEMA, prefix, pluralize.Pluralize(cleanObjectName(o.Name)));
                            viewNames.Add(objectName);

                            selectSql = string.Format(@"select A.ID, A.Name, A.Description, G.Name as DomainGroup, MA.Name as GroupMasterList, dbo.GenerateObjectUrl('{1}', A.DomainTypeID, A.ID) as Url, AC.AttributeCount, Rels.[Count] as RelationshipCount 
    from Domain A
    left join DomainGroup G on G.ID = A.DomainGroupID
    left join Domain MA on MA.ID = G.MasterListID
    cross apply (select count(1) as AttributeCount from Attribute where ObjectType = '{1}' and ObjectID = A.ID) AC 
    cross apply (select count(1) as [Count] from cache.Relationships where SourceObject = '{1}' and SourceObjectID = A.ID) Rels
    where A.DomainTypeID = {0}", o.ID, objectType);

                            objectID = companyConnection.Query<string>("select OBJECT_ID(@n, 'V')", new { n = objectName }).First();

                            viewSql = (string.IsNullOrEmpty(objectID)) ? "CREATE " : "ALTER ";
                            viewSql += string.Format(@" VIEW {0} AS {1}", objectName, selectSql);

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

                            #endregion

                            #region Object Item Views

                            objectName = string.Format("{0}.[{1}_{2}Items]", SCHEMA, prefix, cleanObjectName(o.Name));
                            viewNames.Add(objectName);

                            selectSql = string.Format(@"select AI.ID,  A.ID as DomainID, AI.[Code], AI.Name, AI.Description, G.Name as DomainGroup
    from Domain A
    inner join DomainItem AI on AI.DomainID = A.ID
    left join DomainGroup G on G.ID = A.DomainGroupID
    left join Domain MA on MA.ID = G.MasterListID
    where A.DomainTypeID = {0}", o.ID, objectType);

                            objectID = companyConnection.Query<string>("select OBJECT_ID(@n, 'V')", new { n = objectName }).First();

                            viewSql = (string.IsNullOrEmpty(objectID)) ? "CREATE " : "ALTER ";
                            viewSql += string.Format(@" VIEW {0} AS {1}", objectName, selectSql);

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

                            #endregion

                            // Object Views
                            GenerateAttributeView(viewNames, companyConnection, SCHEMA, prefix, o.Name, objectTypeKey, tableName, objectType, o.ID);
                            GeneratObjectRelationshipView(viewNames, companyConnection, SCHEMA, prefix, o.Name, objectTypeKey, tableName, objectType, o.ID);
                            GenerateObjectResponsibilityView(viewNames, companyConnection, SCHEMA, prefix, o.Name, objectType, o.ID);

                            // Object Missing Views
                            GenerateMissingOverallView(viewNames, companyConnection, SCHEMA, prefix, o.Name, objectTypeKey, tableName, objectType, o.ID);
                            GenerateMissingAttributeView(viewNames, companyConnection, SCHEMA, prefix, o.Name, objectTypeKey, tableName, objectType, o.ID);
                            GenerateMissingRelationshipView(viewNames, companyConnection, SCHEMA, prefix, o.Name, objectTypeKey, tableName, objectType, o.ID);
                            GenerateMissingResponsibilityView(viewNames, companyConnection, SCHEMA, prefix, o.Name, objectTypeKey, tableName, objectType, o.ID);
                        });

                        domainTypes = null;

                        #endregion

                        #region Event

//                        objectType = "Event";
//                        objectTypeKey = "Rule";
//                        prefix = "Event";
//                        tableName = "Domain";

//                        #region General Views

//                        #region RuleAnalytics

//                        objectName = string.Format("{0}.[{1}_{2}]", SCHEMA, prefix, "RuleAnalytics");
//                        viewNames.Add(objectName);

//                        selectSql = @"
//select	R.ID,
//		R.Name,
//		R.Description,
//		case R.RuleType
//			when 1 then 'Informational'
//			when 2 then 'Quality Check'
//			when 3 then 'Metric'
//			when 4 then 'Profile'
//			else 'Unknown'
//		end as [Type],
//		EG.*,
//		OE.[Count] as OpenEventCount,
//		AE.[Count] as AssignedEventCount,
//		ACE.[Count] as ActiveEventCount,
//		CE.[Count] as ClosedEventCount
//from	[Rule] R
//		cross apply (
//					select		count(1) as [GroupCount],
//								max(UpdatedOn) as LatestGroupDate
//					from		EventGroup IG
//					where		IG.RuleID = R.ID 
//					) EG
//		cross apply (
//					select		count(1) as [Count]
//					from		EventGroup IG
//								inner join [Event] IE on IG.RuleID = R.ID and IE.EventGroupID = IG.ID and IE.Status = 'Open'
//					) OE
//		cross apply (
//					select		count(1) as [Count]
//					from		EventGroup IG
//								inner join [Event] IE on IG.RuleID = R.ID and IE.EventGroupID = IG.ID and IE.Status = 'Active'
//					) ACE
//		cross apply (
//					select		count(1) as [Count]
//					from		EventGroup IG
//								inner join [Event] IE on IG.RuleID = R.ID and IE.EventGroupID = IG.ID and IE.Status = 'Closed'
//					) CE
//		cross apply (
//					select		count(1) as [Count]
//					from		EventGroup IG
//								inner join [Event] IE on IG.RuleID = R.ID and IE.EventGroupID = IG.ID and IE.Status = 'Assigned'
//					) AE";

//                        objectID = companyConnection.Query<string>("select OBJECT_ID(@n, 'V')", new { n = objectName }).First();

//                        viewSql = (string.IsNullOrEmpty(objectID)) ? "CREATE " : "ALTER ";
//                        viewSql += string.Format(@" VIEW {0} AS {1}", objectName, selectSql);

//                        try
//                        {
//                            companyConnection.Execute(viewSql.ToString());
//                        }
//                        catch (Exception ex)
//                        {
//                            var msg = ex.GetFullExceptionData() + " Stack: " + ex.StackTrace;
//                            Console.WriteLine(msg);
//                            Console.WriteLine("Attempted SQL: " + viewSql);
//                        }

//                        #endregion

//                        #region EventSummaries

//                        objectName = string.Format("{0}.[{1}_{2}]", SCHEMA, prefix, "Summaries");
//                        viewNames.Add(objectName);

//                        selectSql = @"
//select	R.Name as [Rule],
//		R.ID as RuleID,
//		G.[Name] as [Group],
//		G.PublicID as GroupPublicID, 
//		E.ID as EventID,
//		E.SourceID as EventSourceID,
//		E.Status,
//		E.Date
//from	[Rule] R
//		inner join EventGroup G on G.RuleID = R.ID
//		inner join [Event] E on E.EventGroupID = G.ID";

//                        objectID = companyConnection.Query<string>("select OBJECT_ID(@n, 'V')", new { n = objectName }).First();

//                        viewSql = (string.IsNullOrEmpty(objectID)) ? "CREATE " : "ALTER ";
//                        viewSql += string.Format(@" VIEW {0} AS {1}", objectName, selectSql);

//                        try
//                        {
//                            companyConnection.Execute(viewSql.ToString());
//                        }
//                        catch (Exception ex)
//                        {
//                            var msg = ex.GetFullExceptionData() + " Stack: " + ex.StackTrace;
//                            Console.WriteLine(msg);
//                            Console.WriteLine("Attempted SQL: " + viewSql);
//                        }

//                        #endregion

//                        #endregion

//                        var rules = companyConnection.Query<Rule>("select * from [Rule]").ToList();

//                        try
//                        {
//                            fieldTypes = companyConnection.Query<FieldTypeWithRelation>("select * from FieldTypeWithRelation where [Object] = 'Rule'").ToList();
//                        }
//                        catch (Exception)
//                        {
//                            fieldTypes = companyConnection.Query<FieldTypeWithRelation>("select * from FieldTypeWithRelation where [ObjectType] = 'Rule'").ToList();
//                        }

//                        rules.ForEach(o =>
//                        {
//                            #region Object Views

//                            var joins = "";
//                            var columns = "";

//                            getDynamicFieldJoinStatements(fieldTypes.Where(f => f.ObjectID == o.ID).ToList(), "Event", out joins, out columns);

//                            objectName = string.Format("{0}.[{1}_Rule{2}]", SCHEMA, prefix, o.ID);
//                            viewNames.Add(objectName);

//                            selectSql = string.Format(@"select R.ID as RuleID, G.[Name] as [Group], G.PublicID as GroupPublicID, A.ID as EventID, A.SourceID as EventSourceID, A.Date, {0} A.Status 
//from [Rule] R inner join EventGroup G on R.ID = {1} and G.RuleID = R.ID inner join [Event] A on A.EventGroupID = G.ID {2}", columns, o.ID, joins, objectType);

//                            objectID = companyConnection.Query<string>("select OBJECT_ID(@n, 'V')", new { n = objectName }).First();

//                            viewSql = (string.IsNullOrEmpty(objectID)) ? "CREATE " : "ALTER ";
//                            viewSql += string.Format(@" VIEW {0} AS {1}", objectName, selectSql);

//                            try
//                            {
//                                companyConnection.Execute(viewSql.ToString());
//                            }
//                            catch (Exception ex)
//                            {
//                                var msg = ex.GetFullExceptionData() + " Stack: " + ex.StackTrace;
//                                Console.WriteLine(msg);
//                                Console.WriteLine("Attempted SQL: " + viewSql);
//                            }

//                            #endregion
//                        });

//                        rules = null;

                        #endregion

                        #region Policy Type

                        objectType = "Policy";
                        objectTypeKey = "PolicyTypeID";
                        prefix = "Policy";
                        tableName = "Policy";

                        var policyTypes = companyConnection.Query<PolicyType>("select * from PolicyType").ToList();

                        try
                        {
                            fieldTypes = companyConnection.Query<FieldTypeWithRelation>("select * from FieldTypeWithRelation where [Object] = 'PolicyType'").ToList();
                        }
                        catch (Exception)
                        {
                            fieldTypes = companyConnection.Query<FieldTypeWithRelation>("select * from FieldTypeWithRelation where [ObjectType] = 'PolicyType'").ToList();
                        }

                        policyTypes.ForEach(o =>
                        {
                            #region Object Views

                            var joins = "";
                            var columns = "";

                            getDynamicFieldJoinStatements(fieldTypes.Where(f => f.ObjectID == o.ID).ToList(), "Policy", out joins, out columns);

                            objectName = string.Format("{0}.[{1}_{2}]", SCHEMA, prefix, pluralize.Pluralize(cleanObjectName(o.Name)));
                            viewNames.Add(objectName);

                            selectSql = string.Format(@"select A.ID, A.Name, A.TextPath, A.Description, {0} dbo.GenerateObjectUrl('{3}', A.PolicyTypeID, A.ID) as Url, dbo.GetObjectStatisticScore('Policy', A.ID) as CurrentScore, AC.AttributeCount, Rels.[Count] as RelationshipCount from Policy A {2} cross apply (select count(1) as AttributeCount from Attribute where ObjectType = '{3}' and ObjectID = A.ID) AC cross apply (select count(1) as [Count] from cache.Relationships where SourceObject = '{3}' and SourceObjectID = A.ID) Rels where A.PolicyTypeID = {1}", columns, o.ID, joins, objectType);

                            objectID = companyConnection.Query<string>("select OBJECT_ID(@n, 'V')", new { n = objectName }).First();

                            viewSql = (string.IsNullOrEmpty(objectID)) ? "CREATE " : "ALTER ";
                            viewSql += string.Format(@" VIEW {0} AS {1}", objectName, selectSql);

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

                            #endregion

                            // Object Views
                            GenerateAttributeView(viewNames, companyConnection, SCHEMA, prefix, o.Name, objectTypeKey, tableName, objectType, o.ID, false);
                            GeneratObjectRelationshipView(viewNames, companyConnection, SCHEMA, prefix, o.Name, objectTypeKey, tableName, objectType, o.ID, false);
                            GenerateObjectResponsibilityView(viewNames, companyConnection, SCHEMA, prefix, o.Name, objectType, o.ID);

                            // Object Missing Views
                            GenerateMissingOverallView(viewNames, companyConnection, SCHEMA, prefix, o.Name, objectTypeKey, tableName, objectType, o.ID, false);
                            GenerateMissingAttributeView(viewNames, companyConnection, SCHEMA, prefix, o.Name, objectTypeKey, tableName, objectType, o.ID, false);
                            GenerateMissingRelationshipView(viewNames, companyConnection, SCHEMA, prefix, o.Name, objectTypeKey, tableName, objectType, o.ID, false);
                            GenerateMissingResponsibilityView(viewNames, companyConnection, SCHEMA, prefix, o.Name, objectTypeKey, tableName, objectType, o.ID, false);
                        });

                        policyTypes = null;

                        #endregion

                        #region General Views

                        #region All Artifacts

                        objectName = $"{SCHEMA}.[Glossary_All]";
                        viewNames.Add(objectName);

                        selectSql = @"
select    A.ID,
                              A.ParentID,
                              A.ArtifactTypeID,
                              A.Name,
                              A.Description,
                              A.TextPath,
                              T.Name as ArtifactType,
                              A.Status,
                              A.TaxonomyTypeID, 
                              a.CreatedOn,
        a.UpdatedOn,
                              CONVERT(VARCHAR(10), a.CreatedOn, 112) as CreatedOnKey,
                              CONVERT(VARCHAR(10), a.UpdatedOn, 112) as UpdatedOnKey,
                              TX.Name as TaxonomyTypeName,
        dbo.GetObjectStatisticScore('Artifact', A.ID) as CurrentScore,
                              dbo.GetObjectStatisticScore('Artifact', A.ID) * 100 as CurrentScorePct
from      Artifact A
                              inner join ArtifactType T on T.ID = A.ArtifactTypeID
                              inner join TaxonomyType TX on tx.ID = A.TaxonomyTypeID";

                        objectID = companyConnection.Query<string>("select OBJECT_ID(@n, 'V')", new { n = objectName }).First();

                        viewSql = (string.IsNullOrEmpty(objectID)) ? "CREATE " : "ALTER ";
                        viewSql += string.Format(@" VIEW {0} AS {1}", objectName, selectSql);

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

                        #endregion

                        #region All Fusion Attributes

                        objectName = $"{SCHEMA}.[Fusion_All]";
                        viewNames.Add(objectName);

                        selectSql = @"
select	A.ID as FusionAttributeID,
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
                        viewSql += string.Format(@" VIEW {0} AS {1}", objectName, selectSql);

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
        F.Name as FieldName, 
        F.FriendlyName as FieldFriendlyName, 
		F.FormattedValue as FieldValue 
from	FusionAttribute O 
		inner join FieldWithRelation F on F.ObjectType = 'FusionAttribute' and F.ObjectID = O.ID";

                        objectID = companyConnection.Query<string>("select OBJECT_ID(@n, 'V')", new { n = objectName }).First();

                        viewSql = (string.IsNullOrEmpty(objectID)) ? "CREATE " : "ALTER ";
                        viewSql += string.Format(@" VIEW {0} AS {1}", objectName, selectSql);

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
        F.Name as FieldName, 
        F.FriendlyName as FieldFriendlyName, 
		F.FormattedValue as FieldValue 
from	Artifact O 
		inner join FieldWithRelation F on F.ObjectType = 'Artifact' and F.ObjectID = O.ID";

                        objectID = companyConnection.Query<string>("select OBJECT_ID(@n, 'V')", new { n = objectName }).First();

                        viewSql = (string.IsNullOrEmpty(objectID)) ? "CREATE " : "ALTER ";
                        viewSql += string.Format(@" VIEW {0} AS {1}", objectName, selectSql);

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

                        #endregion

                        #region All Models

                        objectName = $"{SCHEMA}.[Model_All]";
                        viewNames.Add(objectName);

                        selectSql = @"
SELECT T.[ID]                                                                as [Taxonomy ID]
      ,T.[ParentID]
      ,T.[TaxonomyTypeID]                             as [Taxonomy Type id]
      ,T.[Name]                                                  as [Taxonomy Name]
      ,T.[Description]                         as [Taxonomy Description]
      ,T.[TextPath]
      ,T.[Level]                                                    as [Taxonomy Level]
      ,T.[UpdatedOn]                                        
      ,T.[UpdatedBy]
                 , CONVERT(VARCHAR(10), t.UpdatedOn, 112) as UpdatedOnKey
                 ,TY.Name                                                       as [Taxonomy Type Name]
                 ,TC.Name                                                       as [Taxonomy Class Name]
                 ,TL.Name                                                       as [Level Name]
  FROM [dbo].[Taxonomy] T
  inner join TaxonomyType Ty on TY.ID = t.TaxonomyTypeID
  inner join TaxonomyTypeClass TC on TC.ID = TY.[TaxonomyTypeClassID]
  inner join TaxonomyTypeLevel TL on TL.[TaxonomyTypeID] = TY.ID and TL.[Level] = T.[Level]";

                        objectID = companyConnection.Query<string>("select OBJECT_ID(@n, 'V')", new { n = objectName }).First();

                        viewSql = (string.IsNullOrEmpty(objectID)) ? "CREATE " : "ALTER ";
                        viewSql += string.Format(@" VIEW {0} AS {1}", objectName, selectSql);

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

                        #endregion

                        #region Model_Fields

                        objectName = $"{SCHEMA}.[Model_Fields]";
                        viewNames.Add(objectName);

                        selectSql = @"
select  O.ID, 
		O.TaxonomyTypeID, 
		O.Name, 
		O.TextPath, 
		F.FieldTypeID, 
        F.Name as FieldName, 
        F.FriendlyName as FieldFriendlyName, 
		F.FormattedValue as FieldValue 
from	Taxonomy O 
		inner join FieldWithRelation F on F.ObjectType = 'Taxonomy' and F.ObjectID = O.ID";

                        objectID = companyConnection.Query<string>("select OBJECT_ID(@n, 'V')", new { n = objectName }).First();

                        viewSql = (string.IsNullOrEmpty(objectID)) ? "CREATE " : "ALTER ";
                        viewSql += string.Format(@" VIEW {0} AS {1}", objectName, selectSql);

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

                        #endregion

                        #region All Responsibility Allocations

                        objectName = $"{SCHEMA}.[ResponsibilityTypeMap]";
                        viewNames.Add(objectName);

                        selectSql = @"
select	R.ResponsibilityTypeID
		, RT.Name as ResponsibilityName
		, D.Name as ObjectName
		, D.Object
		, D.ObjectID 
from	ResponsibilityTypeRelation R
		inner Join [dbo].[ResponsibilityType] RT on RT.ID = R.ResponsibilityTypeID 
		inner join cache.ObjectDetails D on D.Object = R.ObjectType and D.ObjectID = R.ObjectID";

                        objectID = companyConnection.Query<string>("select OBJECT_ID(@n, 'V')", new { n = objectName }).First();

                        viewSql = (string.IsNullOrEmpty(objectID)) ? "CREATE " : "ALTER ";
                        viewSql += string.Format(@" VIEW {0} AS {1}", objectName, selectSql);

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

                        #endregion

                        #region All Policies

                        objectName = $"{SCHEMA}.[Policy_All]";
                        viewNames.Add(objectName);

                        selectSql = @"
SELECT p.[ID] as [PolicyID]
      ,p.[ParentID]
     ,p.[Name] as [Policy Name]
      ,p.[Description]  as [Policy Description]
      ,p.[TextPath] as [Policy TextPath]
      ,p.[UpdatedOn]
      ,p.[UpdatedBy]
      ,p.[PolicyTypeID]
      ,p.[Level]
                 ,pt.Name                                         as [Policy Type]
                 ,ptl.Name                                        as [Policy Level Name]
                 ,pt.[PolicyTypeClassID]
                 ,pc.Name                                        as [Policy Classification]
                 ,CONVERT(VARCHAR(10), p.UpdatedOn, 112) as UpdatedOnKey
  FROM [dbo].[Policy] p
  inner Join PolicyType pt on pt.ID = p.[PolicyTypeID]
  inner Join PolicyTypeLevel ptl on ptl.PolicyTypeID = pt.ID and ptl.[Level] = p.[level]
  inner join PolicyTypeClass pc on pc.ID = pt.[PolicyTypeClassID]
";

                        objectID = companyConnection.Query<string>("select OBJECT_ID(@n, 'V')", new { n = objectName }).First();

                        viewSql = (string.IsNullOrEmpty(objectID)) ? "CREATE " : "ALTER ";
                        viewSql += string.Format(@" VIEW {0} AS {1}", objectName, selectSql);

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
                        viewSql += string.Format(@" VIEW {0} AS {1}", objectName, selectSql);

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

                        #endregion

                        #region All Responsibility

                        objectName = $"{SCHEMA}.[Responsibility_All]";
                        viewNames.Add(objectName);

                        selectSql = @"
SELECT [ResponsibilityID]
      ,[ResponsibilityTypeID]
      ,[ResponsibilityType]
      ,[AssigningItem]
      ,[AssigningItemID]
      ,[Object]
      ,[ObjectID]
      ,[ResponsibleObject]
      ,[ResponsibleObjectID]
      ,[ContextHash]
      ,[ResponsibilityTypeGroup]
      ,[Visible]
      ,[TargetResponsibilityID]
  FROM [cache].[ResponsibilityItem]
";

                        objectID = companyConnection.Query<string>("select OBJECT_ID(@n, 'V')", new { n = objectName }).First();

                        viewSql = (string.IsNullOrEmpty(objectID)) ? "CREATE " : "ALTER ";
                        viewSql += string.Format(@" VIEW {0} AS {1}", objectName, selectSql);

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

                        #endregion

                        #region All Relationship

                        objectName = $"{SCHEMA}.[Relationship_All]";
                        viewNames.Add(objectName);

                        selectSql = @"
SELECT ID as [IntersectID]
      ,Subject as [SourceObject]
      ,SubjectID as [SourceObjectID]
      ,Object as [TargetObject]
      ,ObjectID as [TargetObjectID]
      ,IntersectTypeID 
  FROM [Intersect]";

                        objectID = companyConnection.Query<string>("select OBJECT_ID(@n, 'V')", new { n = objectName }).First();

                        viewSql = (string.IsNullOrEmpty(objectID)) ? "CREATE " : "ALTER ";
                        viewSql += string.Format(@" VIEW {0} AS {1}", objectName, selectSql);

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
        F.Name as FieldName, 
        F.FriendlyName as FieldFriendlyName, 
		F.FormattedValue as FieldValue 
from	IntersectDetail O 
		inner join FieldWithRelation F on F.ObjectType = 'Intersect' and F.ObjectID = O.ID";

                        objectID = companyConnection.Query<string>("select OBJECT_ID(@n, 'V')", new { n = objectName }).First();

                        viewSql = (string.IsNullOrEmpty(objectID)) ? "CREATE " : "ALTER ";
                        viewSql += string.Format(@" VIEW {0} AS {1}", objectName, selectSql);

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

                        #endregion

                        #region All Workflows

                        objectName = $"{SCHEMA}.[Workflows_All]";
                        viewNames.Add(objectName);

                        selectSql = @"
select	w.ID,
		case w.WorkflowType
			when 1 then 'Propose' 
			when 2 then 'Certify' 
			when 3 then 'Issue' 
			when 4 then 'Challange' 
			when 5 then 'Propose' 
		end as [WorkflowTypeName],
		right(convert(varchar(10),W.dateStarted,103),7) as MonYear,
		case  
			when cast(datename(dayofyear,GETDATE()) as int) - cast  (datename(dayofyear,W.[DateStarted]) as int) between -99999999 and 0 then 'Prior Yrs' 
			when  cast(datename(dayofyear,GETDATE()) as int) - cast (datename(dayofyear,W.[DateStarted]) as int) between 0 and 60 then '0 - 60'
			when  cast(datename(dayofyear,GETDATE()) as int) - cast (datename(dayofyear,W.[DateStarted]) as int) between 61 and 90 then '61 - 90'
			else ' > 90 Days'
		END as DaysElapsed, 
		W.DateStarted,
		CONVERT(varchar(8),CAST(W.[DateStarted] AS DATE),112) as DateStartedKey,
		W.DateCompleted,
		CONVERT(varchar(8),CAST(W.[DateCompleted] AS DATE),112) as DateCompletedKey,
		case isNULL(W.DateCompleted,'') 
			when ''  then 'False' 
			else 'True' 
		end as isCompleted,
		coalesce(Ax.Name, A.ArtifactType) as ArtifactType,
		coalesce(T.Name, A.taxonomyTypeName)  as TaxonomyTypeName,
		'/Resource/' + cast(R.ResourceID as varchar(200)) as ResourceURI
from	Workflow W
		inner join [dbo].[WorkflowResource] r on R.WorkflowID = W.ID 
		left join ArtifactType ax on ax.ID = w.data.value('(/fields/ArtifactTypeID)[1]', 'int') and w.data.value('(/fields/ArtifactTypeID)[1]', 'int') is not null
		left join TaxonomyType T on T.ID = w.data.value('(/fields/VocabularyID)[1]', 'int') and w.data.value('(/fields/VocabularyID)[1]', 'int')  is not null
		left join [CommentRelation] cr on CR.CommentID = w.data.value('(/fields/CommentID)[1]', 'int') and CR.ObjectType = 'Artifact' and w.data.value('(/fields/CommentID)[1]', 'int') is not null
		left join [CommentRelation] cr1 on CR1.CommentID = w.data.value('(/fields/CommentID)[1]', 'int') and CR1.ObjectType = 'Resource' and w.data.value('(/fields/CommentID)[1]', 'int') is not null
		left join [reporting].[Glossary_All] A on A.ID = coalesce(w.data.value('(/fields/ArtifactID)[1]', 'int'), CR.ObjectID)";

                        objectID = companyConnection.Query<string>("select OBJECT_ID(@n, 'V')", new { n = objectName }).First();

                        viewSql = (string.IsNullOrEmpty(objectID)) ? "CREATE " : "ALTER ";
                        viewSql += string.Format(@" VIEW {0} AS {1}", objectName, selectSql);

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

                        #endregion

                        prefix = "Global";

                        #region InterRelationships

                        objectName = string.Format("{0}.[{1}_{2}]", SCHEMA, prefix, "ModelInterRelationships");
                        viewNames.Add(objectName);

                        selectSql = @"
    select	R.ID as IntersectID,
		    case R.Classification 
			    when 1 then 'Critical' 
			    else 'Normal' 
		    end as Classification, 
		    R.Description,
		    S.ID as SourceID,
		    S.Name as SourceName,
            S.TextPath as SourceTextPath,
		    ST.Name as SourceType,
		    S.[Level] as SourceLevel,
		    SL.Name as SourceLevelName,
		    dbo.GenerateObjectUrl('Taxonomy', S.TaxonomyTypeID, S.ID) as SourceURL,
		    SC.Name as SourceClass,
		    T.ID as TargetID,
		    T.Name as TargetName,
            T.TextPath as TargetTextPath,
		    TT.Name as TargetType,
		    T.[Level] as TargetLevel,
		    TL.Name as TargetLevelName,
		    dbo.GenerateObjectUrl('Taxonomy', T.TaxonomyTypeID, T.ID) as TargetUrl,
		    TC.Name as TargetClass
    from	[Intersect] R
		    inner join Taxonomy S on R.Subject = 'Taxonomy' and S.ID = R.SubjectID
		    inner join Taxonomy T on R.Object = 'Taxonomy' and T.ID = R.ObjectID
		    inner join TaxonomyType ST on ST.ID = S.TaxonomyTypeID
            inner join TaxonomyTypeClass SC on SC.ID = ST.TaxonomyTypeClassID
            left join TaxonomyTypeLevel SL on SL.TaxonomyTypeID = S.TaxonomyTypeID and S.[Level] = SL.[Level]
		    inner join TaxonomyType TT on TT.ID = T.TaxonomyTypeID
            inner join TaxonomyTypeClass TC on TC.ID = TT.TaxonomyTypeClassID
		    left join TaxonomyTypeLevel TL on TL.TaxonomyTypeID = T.TaxonomyTypeID and T.[Level] = TL.[Level]";

                        objectID = companyConnection.Query<string>("select OBJECT_ID(@n, 'V')", new { n = objectName }).First();

                        viewSql = (string.IsNullOrEmpty(objectID)) ? "CREATE " : "ALTER ";
                        viewSql += string.Format(@" VIEW {0} AS {1}", objectName, selectSql);

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

                        #endregion

                        #region ModelOwners

                        objectName = string.Format("{0}.[{1}_{2}]", SCHEMA, prefix, "ModelResponsibilities");
                        viewNames.Add(objectName);

                        selectSql = @"
    select	R.ResponsibilityID,
		    R.AssigningItemType,
		    R.AssigningItemID,
		    --R.AssigningItemUrl,
		    --R.AssigningItemName,
		    --R.AssigningTypeName,
		    R.ObjectID as InformationModelID,
		    R.ObjectName,
		    R.ObjectTypeID,
		    R.ObjectTypeName,
		    R.ObjectUrl,
		    COALESCE(U.FirstName + ' ' + U.LastName, R.[ResponsibleObjectName]) as ResponsibleObjectName,
		    R.[ResponsibleObjectUrl],
		    R.[PrimaryOwnerResourceID],
		    GU.FirstName + ' ' + GU.LastName as PrimaryOwnerResourceName,
		    R.[Role],
		    R.[CurrentScore],
		    --R.RedFlagged,
		    O.TextPath,
		    O.[Level],
		    TL.Name as LevelName,
		    TL.Description as LevelDescription,
		    C.Name as [Class]
    from	ResponsibilityDetail R
		    inner join Taxonomy O on O.ID = R.ObjectID
		    inner join TaxonomyType T on T.ID = O.TaxonomyTypeID
            inner join TaxonomyTypeClass C on C.ID = T.TaxonomyTypeClassID
            left join TaxonomyTypeLevel TL on TL.TaxonomyTypeID = T.ID and TL.[Level] = O.[Level]
		    left join [reporting].[Global_Resource] U on U.ResourceID = R.[ResponsibleObjectID] and R.[ResponsibleObjectType] = 'Resource'
		    left join [reporting].[Global_Resource] GU on GU.ResourceID = R.[PrimaryOwnerResourceID]
    where	ObjectType = 'Taxonomy'";

                        objectID = companyConnection.Query<string>("select OBJECT_ID(@n, 'V')", new { n = objectName }).First();

                        viewSql = (string.IsNullOrEmpty(objectID)) ? "CREATE " : "ALTER ";
                        viewSql += string.Format(@" VIEW {0} AS {1}", objectName, selectSql);

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
                                    Console.WriteLine(msg);
                                }
                            }
                        });

                        #endregion

                        companyConnection.Close();
                        companyConnection.Dispose();

                        Console.WriteLine("END COMPANY {0} -------------", companyID);
                    }
                    catch (Exception ex)
                    {
                        var msg = "CompanyID: " + companyID + " - " + ex.GetFullExceptionData();
                        Console.WriteLine(msg);
                    }
                });
            }
            catch (Exception ex)
            {
                var msg = ex.GetFullExceptionData();
                Console.WriteLine(msg);
            }

            return mex;
        }
    }
}
