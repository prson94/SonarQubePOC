using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using d360.core;
using d360.core.entities;
using d360.core.enums;
using d360.core.queue;
using d360.extensions;

using Dapper;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using SpreadsheetLight;

namespace d360.model.DataAccessLayer.repositories
{
	public abstract class BaseRepository
	{
		private readonly ICompanyContext CompanyContext;
		private const string RELATIONSHIP_DELIMITER = "|";
		protected readonly string AZURE_QUEUE_INSERTION_FAILURE_MESSAGE = "An internal error occurred while submitting your batch request.  Please try your request again. [Azure Queue Insertion Failure]";

		protected BaseRepository(ICompanyContext ctx)
		{
			CompanyContext = ctx;
		}

		public int ApiTimeout
		{
			get
			{
				return CompanyContext.ApiTimeout;
			}
		}

		#region Private

		private bool DoesIntersectHasRefList(IntersectType intersectType)
		{
			if (intersectType == null)
			{
				return false;
			}

			return intersectType.Object == "ReferenceItemType" && intersectType.ObjectID == 0 || intersectType.Subject == "ReferenceItemType" && intersectType.SubjectID == 0;
		}

		private SplitFilterCriteriaRelationship GetSplitFilterCriteriaRelationship(int lookupObjectID, string objecttype, int objectid, out bool hasReferenceList, out string targetType)
		{
			hasReferenceList = false;
			var intersecttypeboth = CompanyContext.Filter<IntersectType>(i => i.ID == lookupObjectID && i.Object == objecttype && i.ObjectID == objectid && i.Subject == objecttype && i.SubjectID == objectid).SingleOrDefault();
			hasReferenceList = DoesIntersectHasRefList(intersecttypeboth);
			if (intersecttypeboth == null)
			{
				var intersecttypeobject = CompanyContext.Filter<IntersectType>(i => i.ID == lookupObjectID && i.Object == objecttype && i.ObjectID == objectid).SingleOrDefault();
				hasReferenceList = DoesIntersectHasRefList(intersecttypeobject);

				if (intersecttypeobject == null)
				{
					var intersecttypesubject = CompanyContext.Filter<IntersectType>(i => i.ID == lookupObjectID && i.Subject == objecttype && i.SubjectID == objectid).SingleOrDefault();
					hasReferenceList = DoesIntersectHasRefList(intersecttypesubject);

					if (intersecttypesubject == null)
					{
						targetType = objecttype;
						return SplitFilterCriteriaRelationship.Both;
					}
					else
					{
						targetType = intersecttypesubject.Object;
						return SplitFilterCriteriaRelationship.Subject;
					}
				}
				else
				{
					targetType = intersecttypeobject.Subject;
					return SplitFilterCriteriaRelationship.Object;
				}
			}
			else
			{
				targetType = objecttype;
				return SplitFilterCriteriaRelationship.Both;
			}

		}
		#endregion

		public static string GetPathSelectSql(FieldType fieldType)
		{
			var pathDefinition = JsonConvert.DeserializeObject<FieldTypeDataTypePathApiViewModel_Definition>(fieldType.Definition);
			if (pathDefinition?.AssetTypeUid == null)
			{
				return $"Node.DisplayPath";
			}
			else
			{
				return $@"F{fieldType.ID}.FormattedValue";
			}
		}

		public static string GetPathJoinSql(FieldType fieldType)
		{
			var pathDefinition = JsonConvert.DeserializeObject<FieldTypeDataTypePathApiViewModel_Definition>(fieldType.Definition);
			if (pathDefinition?.AssetTypeUid != null)
			{
				return $@"
					outer apply (SELECT TOP 1 string_agg(Val, ' / ') within group(order by P)
					         FROM (
					         SELECT *
					           FROM (
					           SELECT X.p.value('./@level', 'int') as L,
					                  X.p.value('./@position', 'int') as P,
					                  X.p.value('./@assetTypeId', 'int') as AssetTypeId,
					                  (select X.p.value('.', 'nvarchar(250)') for xml path('')) as Val
					             FROM Node.Segments.nodes('/path/segment') X(p)
					           ) s
					           JOIN AssetType at ON at.ID = s.AssetTypeId and at.uid = '{pathDefinition.AssetTypeUid}'
					         ) segmentPath
					      )F{fieldType.ID}(FormattedValue)
				";
			}
			
			return "";
		}

		protected string AddColumnAlias(string columnSql, string alias, bool strict = true)
		{
			if (strict)
			{
				alias = $"[{alias.Trim('[', ']')}]";
			}

			return $"{columnSql} AS {alias}";
		}

		protected void getFieldSql(List<FieldType> fieldTypes, DynamicParameters dbArgs, List<string> fieldJoins, List<string> fieldColumns, string objectSql = "A.[Object]", string objectIdSql = "A.[ObjectId]", bool listColorsAsJSON = false, bool IsCreateTempTable = false, List<string> TempTableScriptList = null)
		{
			List<string> TempTableNameList = new List<string>();

			fieldTypes.OrderBy(x => x.ID).ToList().ForEach(f =>
			 {
				 var defaultVal = f.DefaultFormattedValue;
				 var joinPrefix = "left";
				 var tableAlias = $"F{f.ID}";
				 var columnName = f.Name;
				 var valueColumn = "FormattedValue";
				 var fieldDataType = getFieldDataType(f);
				 var hasColor = CompanyContext.LookupFieldHasColorItem(f);
				 FieldTypeDefinition_JsonElement jsonElementDefinition = null;

				 if (f.Type == "JsonElement")
				 {
					 jsonElementDefinition = JsonConvert.DeserializeObject<FieldTypeDefinition_JsonElement>(f.Definition);
				 }

				 if (f.Type == "Link")
				 {
					 valueColumn = "Value";
					 defaultVal = f.DefaultValue;
				 }


				 if (f.Type == "Score")
				 {
					 joinPrefix = "outer";
				 }

				 FieldType relatedField = null;
				 if (f.Type == "FieldFromRelationship")
				 {
					 if (!f.LookupObjectFieldTypeID.HasValue || !f.LookupObjectID.HasValue)
					 {
						 return;
					 }

					 relatedField = CompanyContext.GetById<FieldType>((int)f.LookupObjectFieldTypeID);
					 if (relatedField == null)
					 {
						 return;
					 }

				 }

				 if (f.IsRequired && string.IsNullOrEmpty(f.DefaultValue))
				 {
					 joinPrefix = "left";
					 if (!string.IsNullOrEmpty(fieldDataType))
					 {
						 if (fieldDataType == "bit")
						 {
							 fieldColumns.Add($"try_cast(case when {tableAlias}.{valueColumn} = 'true' then 1 else 0 end as {fieldDataType}) as [{columnName}]");
						 }
						 else
						 {
							 fieldColumns.Add($"try_cast({tableAlias}.{valueColumn} as {fieldDataType}) as [{columnName}]");
						 }
					 }
					 else if (f.Type == "Lookup" && f.AllowAllValue)
					 {
						 fieldColumns.Add($"case when {tableAlias}.[Value] = '0' then @F{f.ID}_AllValue else {tableAlias}.{valueColumn} end as [{columnName}]");

						 var AllowAllLabelValue = getAllowedAllValue(f.AllowAllLabel, hasColor);

						 dbArgs.Add($"@F{f.ID}_AllValue", AllowAllLabelValue);
					 }
					 else if (f.Type == "Lookup" && listColorsAsJSON)
					 {
						 fieldColumns.Add($"{tableAlias}.{valueColumn} as [{columnName}]");
					 }
					 else if (f.Type == "Path")
					 {
						 fieldColumns.Add(AddColumnAlias(GetPathSelectSql(f), columnName));
					 }
					 else if (f.Type == "Score")
					 {
						 fieldColumns.Add($"{tableAlias}.{valueColumn} as [{columnName}]");
					 }
					 else if (f.Type == "Counter")
					 {
						 fieldColumns.Add($"('{f.CounterPrefix}' + CAST({tableAlias}.{valueColumn} as nvarchar(max))) as [{columnName}]");
					 }
					 else
					 {
						 fieldColumns.Add($"{tableAlias}.{valueColumn} as [{columnName}]");
					 }
				 }
				 else
				 {
					 if (!string.IsNullOrEmpty(f.DefaultValue))
					 {
						 if (!string.IsNullOrEmpty(fieldDataType))
						 {
							 if (fieldDataType == "bit")
							 {
								 fieldColumns.Add($"try_cast(case when coalesce({tableAlias}.{valueColumn}, @defaultValue{tableAlias}) = 'true' then 1 else 0 end as {fieldDataType}) as [{columnName}]");
							 }
							 else
							 {
								 fieldColumns.Add($"coalesce(try_cast({tableAlias}.{valueColumn} as {fieldDataType}), @defaultValue{tableAlias}) as [{columnName}]");
							 }
						 }
						 else if (f.Type == "Lookup" && f.AllowAllValue)
						 {

							 fieldColumns.Add($"case when {tableAlias}.[Value] = '0' then @F{f.ID}_AllValue else coalesce({tableAlias}.{valueColumn}, @defaultValue{tableAlias}) end as [{columnName}]");

							 var AllowAllLabelValue = getAllowedAllValue(f.AllowAllLabel, hasColor);

							 dbArgs.Add($"@F{f.ID}_AllValue", AllowAllLabelValue);
						 }
						 else if (f.Type == "Path")
						 {
							 fieldColumns.Add(AddColumnAlias(GetPathSelectSql(f), columnName));
						 }
						 else if (f.Type == "Score")
						 {
							 fieldColumns.Add($"{tableAlias}.{valueColumn} as [{columnName}]");
						 }
						 else if (f.Type == "Lookup" && listColorsAsJSON && hasColor)
						 {
							 fieldColumns.Add($"coalesce({tableAlias}.{valueColumn}, defaultColorValue{tableAlias}.color, @defaultValue{tableAlias}) as [{columnName}]");
						 }
						 else
						 {
							 fieldColumns.Add($"coalesce({tableAlias}.{valueColumn}, @defaultValue{tableAlias}) as [{columnName}]");
						 }

						 dbArgs.Add($"@defaultValue{tableAlias}", defaultVal);
					 }
					 else
					 {
						 if (!string.IsNullOrEmpty(fieldDataType))
						 {
							 if (fieldDataType == "bit")
							 {
								 fieldColumns.Add($"try_cast(case when {tableAlias}.{valueColumn} is null then null when {tableAlias}.{valueColumn} = 'true' then 1 else 0 end as {fieldDataType}) as [{columnName}]");
							 }
							 else
							 {
								 fieldColumns.Add($"try_cast(case when LEN(ISNULL({tableAlias}.{valueColumn}, '')) < 1 then null else {tableAlias}.{valueColumn} end as {fieldDataType}) as [{columnName}]");
							 }
						 }
						 else if (f.Type == "JsonElement")
						 {
							 if (jsonElementDefinition.DataType == "decimal")
							 {
								 jsonElementDefinition.DataType = "float";
							 }
							 fieldColumns.Add($"try_cast(FJP{f.ID}.[Value] as {jsonElementDefinition.DataType}) as [{columnName}]");
						 }
						 else if (f.Type == "Lookup" && f.AllowAllValue)
						 {
							 fieldColumns.Add($"case when {tableAlias}.[Value] = '0' then @F{f.ID}_AllValue else {tableAlias}.{valueColumn} end as [{columnName}]");

							 var AllowAllLabelValue = getAllowedAllValue(f.AllowAllLabel, hasColor);

							 dbArgs.Add($"@F{f.ID}_AllValue", AllowAllLabelValue);
						 }
						 else if (f.Type == "Path")
						 {
							 fieldColumns.Add(AddColumnAlias(GetPathSelectSql(f), columnName));
						 }
						 else if (f.Type == "Score")
						 {
							 fieldColumns.Add($"{tableAlias}.{valueColumn} as [{columnName}]");
						 }
						 else if (f.Type == "Counter")
						 {
							 fieldColumns.Add($"('{f.CounterPrefix}' + CAST({tableAlias}.{valueColumn} as nvarchar(max))) as [{columnName}]");
						 }
						 else if (f.Type == "Link")
						 {
							 fieldColumns.Add($"NULLIF(NULLIF({tableAlias}.{valueColumn},'|'), '') as [{columnName}]");
						 }
						 else if (f.Type == "ComplexRelationLookup" || f.Type == "OwnershipLookup")
						 {
							 fieldColumns.Add($"{tableAlias}.Definition as [{columnName}]");
						 }
						 else
						 {
							 fieldColumns.Add($"{tableAlias}.{valueColumn} as [{columnName}]");
						 }
					 }
				 }

				 if (f.Type == "FieldFromRelationship")
				 {
					 bool hasReferenceList = false;
					 string targetType;
					 var filtercond = GetSplitFilterCriteriaRelationship(f.LookupObjectID.GetValueOrDefault(), f.Object, f.ObjectID, out hasReferenceList, out targetType);

					 string assetIdBackwardQuery;
					 string assetIdForwardQuery;
					 string assetIdBothQuery;
					 string temptablename;
					 string temptableScript;

					 if (IsCreateTempTable)
					 {
						 switch (filtercond)
						 {
							 case SplitFilterCriteriaRelationship.Object:
								 temptablename = $@"#TempGraphBack{f.LookupObjectID}";

								 temptableScript = $@" 
									drop table if exists {temptablename};
									select I.ObjectAssetId  as SourceAssetID, I.SubjectAssetId as TargetAssetId into {temptablename}
									from [Intersect] I where I.IntersectTypeID = {f.LookupObjectID}";
								 break;
							 case SplitFilterCriteriaRelationship.Subject:
								 temptablename = $@"#TempGraphFwd{f.LookupObjectID}";

								 temptableScript = $@" 
									drop table if exists {temptablename};
									select I.SubjectAssetId  as SourceAssetID, I.ObjectAssetId as TargetAssetId into {temptablename}
									from [Intersect] I where I.IntersectTypeID = {f.LookupObjectID}";
								 break;
							 default:
								 temptablename = $@"#TempGraphBoth{f.LookupObjectID}";

								 temptableScript = $@" 
									drop table if exists {temptablename};
									select * into {temptablename} from (
									select I.SubjectAssetId as SourceAssetID, I.ObjectAssetId as TargetAssetId  
										from [Intersect] I where I.IntersectTypeID = {f.LookupObjectID} 
									union
									select I.ObjectAssetId as SourceAssetID, I.SubjectAssetId as TargetAssetId  
										from [Intersect] I where I.IntersectTypeID = {f.LookupObjectID} 
									) a;";
								 break;
						 }

						 if (!TempTableNameList.Contains(temptablename))
						 {
							 TempTableNameList.Add(temptablename);
							 TempTableScriptList.Add(temptableScript);
						 }

						 assetIdBackwardQuery = $@"select S.TargetAssetId from {temptablename} S where S.SourceAssetID = A.Id";
						 assetIdForwardQuery = $@"select S.TargetAssetId from {temptablename} S where S.SourceAssetID = A.Id";
						 assetIdBothQuery = $@"select S.TargetAssetId from {temptablename} S where S.SourceAssetId = A.Id";
					 }
					 else
					 {
						 assetIdBackwardQuery = $@"select I.ObjectAssetId as TargetAssetId from [Intersect] I where I.IntersectTypeID = {f.LookupObjectID} AND I.SubjectAssetId = A.Id";
						 assetIdForwardQuery = $@"select I.SubjectAssetId as TargetAssetId from [Intersect] I where I.IntersectTypeID = {f.LookupObjectID} AND I.ObjectAssetId = A.Id";
						 assetIdBothQuery = assetIdBackwardQuery + " union " + assetIdForwardQuery;
					 }

					 var assetIdFinalQuery = "";

					 switch (filtercond)
					 {
						 case SplitFilterCriteriaRelationship.Object:
							 assetIdFinalQuery = assetIdBackwardQuery;
							 break;
						 case SplitFilterCriteriaRelationship.Subject:
							 assetIdFinalQuery = assetIdForwardQuery;
							 break;
						 default:
							 assetIdFinalQuery = assetIdBothQuery;
							 break;
					 }

					 if (relatedField.Type == "Path")
					 {
						 fieldJoins.Add($@"outer apply (
							select  STRING_AGG(DisplayPath,'{RELATIONSHIP_DELIMITER}') as FormattedValue 
							from    dbo.AssetPath
							where   ID IN ({assetIdFinalQuery})
							having  string_agg(DisplayPath,'{RELATIONSHIP_DELIMITER}') is not null
						) {tableAlias}");
					 }
					 else
					 {
						 fieldJoins.Add($@"outer apply (
							select  STRING_AGG(FormattedValue,'{RELATIONSHIP_DELIMITER}') as FormattedValue 
							from    Field 
							where   FieldTypeID = {f.LookupObjectFieldTypeID} and AssetID IN ({assetIdFinalQuery})
									and FormattedValue is not null
							having  string_agg(FormattedValue,'{RELATIONSHIP_DELIMITER}') is not null
						) {tableAlias}");
					 }
				 }
				 else if (f.Type == "Relationship")
				 {
					 bool hasReferenceList = false;
					 string targetType;
					 var filtercond = GetSplitFilterCriteriaRelationship(f.LookupObjectID.GetValueOrDefault(), f.Object, f.ObjectID, out hasReferenceList, out targetType);

					 // Models / Policy relationships should return the textpath not just the display name of the bottom level item.
					 bool isModelOrPolicy = (targetType == SystemObjects.PolicyType.ToString() || targetType == SystemObjects.TaxonomyType.ToString());

					 if (hasReferenceList)
					 {
						 if (filtercond == SplitFilterCriteriaRelationship.Object)
						 {
							 fieldJoins.Add($@"outer apply (
								select  STRING_AGG(S.Name,'{RELATIONSHIP_DELIMITER}') as FormattedValue from FieldType FT
									inner join [Intersect] I on I.IntersectTypeId = FT.LookupObjectID 
									left join AssetType S on S.Object = I.Subject and S.ObjectID = I.SubjectID and I.Object = A.Object and I.ObjectID = A.ObjectID
									where FT.Id = {f.ID}
									having STRING_AGG(S.Name,'{RELATIONSHIP_DELIMITER}') is not null
								) {tableAlias}");
						 }
						 else if (filtercond == SplitFilterCriteriaRelationship.Subject)
						 {
							 fieldJoins.Add($@"outer apply (
								select  STRING_AGG(O.Name,'{RELATIONSHIP_DELIMITER}') as FormattedValue from FieldType FT
									inner join [Intersect] I on I.IntersectTypeId = FT.LookupObjectID 
									left join AssetType O on O.Object = I.Object and O.ObjectID = I.ObjectID and I.Subject = A.Object and I.SubjectID = A.ObjectID
									where FT.Id = {f.ID}
									having STRING_AGG(O.Name,'{RELATIONSHIP_DELIMITER}') is not null
								) {tableAlias}");
						 }
						 else
						 {
							 fieldJoins.Add($@"outer apply (
								select  STRING_AGG(ISNULL(S.Name, O.Name),'{RELATIONSHIP_DELIMITER}') as FormattedValue from FieldType FT
									inner join [Intersect] I on I.IntersectTypeId = FT.LookupObjectID 
									left join AssetType S on S.Object = I.Subject and S.ObjectID = I.SubjectID and I.Object = A.Object and I.ObjectID = A.ObjectID
									left join AssetType O on O.Object = I.Object and O.ObjectID = I.ObjectID and I.Subject = A.Object and I.SubjectID = A.ObjectID
									where FT.Id = {f.ID}
									having STRING_AGG(ISNULL(S.Name, O.Name),'{RELATIONSHIP_DELIMITER}') is not null
								) {tableAlias}");
						 }
					 }
					 else
					 {
						 string assetIdQuery;
						 string assetIdFinalQuery;
						 string temptablename;
						 string temptableScript;

						 if (IsCreateTempTable)
						 {
							 switch (filtercond)
							 {
								 case SplitFilterCriteriaRelationship.Object:
									 temptablename = $@"#TempGraphBack{f.LookupObjectID}";

									 temptableScript = $@" 
									drop table if exists {temptablename};
									select I.ObjectAssetId  as SourceAssetID, I.SubjectAssetId as TargetAssetId into {temptablename}
									from [Intersect] I where I.IntersectTypeID = {f.LookupObjectID}";
									 break;
								 case SplitFilterCriteriaRelationship.Subject:
									 temptablename = $@"#TempGraphFwd{f.LookupObjectID}";

									 temptableScript = $@" 
									drop table if exists {temptablename};
									select I.SubjectAssetId  as SourceAssetID, I.ObjectAssetId as TargetAssetId into {temptablename}
									from [Intersect] I where I.IntersectTypeID = {f.LookupObjectID}";
									 break;
								 default:
									 temptablename = $@"#TempGraphBoth{f.LookupObjectID}";

									 temptableScript = $@" 
									drop table if exists {temptablename};
									select * into {temptablename} from (
									select I.SubjectAssetId as SourceAssetID, I.ObjectAssetId as TargetAssetId  
										from [Intersect] I where I.IntersectTypeID = {f.LookupObjectID} 
									union
									select I.ObjectAssetId as SourceAssetID, I.SubjectAssetId as TargetAssetId  
										from [Intersect] I where I.IntersectTypeID = {f.LookupObjectID} 
									) a;";
									 break;
							 }

							 if (!TempTableNameList.Contains(temptablename))
							 {
								 TempTableNameList.Add(temptablename);
								 TempTableScriptList.Add(temptableScript);
							 }
							 assetIdQuery = $@"select S.TargetAssetId from {temptablename} S where S.SourceAssetID = A.Id";
						 }
						 else
						 {
							 if (filtercond == SplitFilterCriteriaRelationship.Object)
							 {
								 assetIdQuery = $@"
									select I.SubjectAssetId as TargetAssetId from [Intersect] I 
									where I.IntersectTypeID = {f.LookupObjectID} AND I.ObjectAssetId = A.Id";
							 }
							 else if (filtercond == SplitFilterCriteriaRelationship.Subject)
							 {

								 assetIdQuery = $@"
									select I.ObjectAssetId as TargetAssetId from [Intersect] I 
									where I.IntersectTypeID = {f.LookupObjectID} AND I.SubjectAssetId = A.Id";
							 }
							 else
							 {
								 assetIdQuery = $@"
												select I.SubjectAssetId as TargetAssetId from [Intersect] I 
												where I.IntersectTypeID = {f.LookupObjectID} AND I.ObjectAssetId = A.Id
												union
												select I.ObjectAssetId as TargetAssetId from [Intersect] I 
												where I.IntersectTypeID = {f.LookupObjectID} AND I.SubjectAssetId = A.Id";
							 }
						 }
						 assetIdFinalQuery = $@" outer apply (
							select STRING_AGG({(isModelOrPolicy ? "ATV.TextPath" : "AD.DisplayValue")},'{RELATIONSHIP_DELIMITER}') as FormattedValue from AssetDetail AD
							{(isModelOrPolicy ? " cross apply [dbo].[GetAssetTextPathById](AD.ID,'/') ATV " : "")}
							where AD.ID in (
							{assetIdQuery})
							having string_agg(AD.DisplayValue,'{RELATIONSHIP_DELIMITER}') is not null
							) {tableAlias} ";

						 fieldJoins.Add(assetIdFinalQuery);
					 }
				 }
				 else if (f.Type == "RefListRelationship")
				 {
					 fieldJoins.Add($@"outer apply (
						select string_agg([Name],'{RELATIONSHIP_DELIMITER}') as FormattedValue
						from (
						select SubjectName as [Name] from IntersectDetail I where I.IntersectTypeID = {f.LookupObjectID} and I.[Object] = A.[Object] and I.ObjectID = A.ObjectID
						union all
						select ObjectName as [Name] from IntersectDetail I where I.IntersectTypeID = {f.LookupObjectID} and I.[Subject] = A.[Object] and I.SubjectID = A.ObjectID
						) Names
					) {tableAlias}");
				 }
				 else if (f.Type == "JsonElement")
				 {
					 fieldJoins.Add($@"
						{joinPrefix} join Field {tableAlias} on {tableAlias}.FieldTypeID = {jsonElementDefinition.FieldTypeID} and {tableAlias}.[ObjectType] = A.[Object] and {tableAlias}.[ObjectID] = A.[ObjectID]
						{joinPrefix} join FieldJsonProperty FJP{f.ID} on FJP{f.ID}.FieldID = {tableAlias}.ID and FJP{f.ID}.[Path] = @jsonPath{f.ID}
					");
					 dbArgs.Add($"@jsonPath{f.ID}", jsonElementDefinition.Path);
				 }
				 else if (f.Type == "Score")
				 {
					 fieldJoins.Add($"{joinPrefix} apply dbo.GetAssetScoreById(A.ID, {f.ScoreType}) {tableAlias}");
				 }
				 else if (f.Type == "Counter")
				 {
					 fieldJoins.Add($@"outer apply (select top 1 [Value] as 'FormattedValue'
							from dbo.FieldCounterValue
							where AssetId = A.Id and FieldTypeId = {f.ID}){tableAlias}");
				 }
				 else if (f.Type == "Tag")
				 {
					 fieldJoins.Add($@"outer apply(
						select FormattedValue = STUFF((
							select '|' + T.Value from AssetTag AT
								inner join Tag T on AT.TagID = T.ID
								where AT.AssetID = A.ID
							for xml path (''), TYPE).value('.','NVARCHAR(MAX)'), 1, 1, '')
						 ){tableAlias}(FormattedValue) ");
				 }
				 else if (f.Type == "Lookup")
				 {
					 string temptablename;
					 string temptableScript;

					 if (listColorsAsJSON && hasColor)
					 {
						 string lookupValueJoinCriteria;
						 string displayName;

						 string sql;

						 string type = f.LookupObjectType == "ReferenceItem" ? f.LookupObjectType + "Type" : f.LookupObjectType;

						 if (f.AllowMultipleValues)
						 {
							 displayName = $@"ADV{tableAlias}.DisplayValue";
							 lookupValueJoinCriteria = $"cross apply GetAssetDisplayValueByID(ACF{tableAlias}.ID) ADV{tableAlias} cross apply STRING_SPLIT(F{tableAlias}.Value, ',') SPF{tableAlias} where ACF{tableAlias}.Object = '{f.LookupObjectType}' and ACF{tableAlias}.ObjectID = try_cast(SPF{tableAlias}.value as int)";
						 }
						 else
						 {
							 displayName = $@"F{tableAlias}.formattedValue";
							 lookupValueJoinCriteria = $" where ACF{tableAlias}.Object = '{f.LookupObjectType}' and ACF{tableAlias}.[ObjectID] = try_cast(F{tableAlias}.[Value] as int)";
						 }

						 if (IsCreateTempTable)
						 {
							 temptablename = $@"#TempLookUp{f.LookupObjectType}{f.LookupObjectID}";
							 temptableScript = $@" 
								drop table if exists {temptablename};
								select A.[Object],A.[ObjectID],A.ID,A.Code,COALESCE(JSON_VALUE(ACJ.ColorJSON,'$.Value'), 'transparent') as color
								into {temptablename}
								from AssetType Att
								inner join Asset A on Att.ID = A.AssetTypeId
								cross apply dbo.GetAssetColorJsonByColor(A.Color) ACJ
								where att.Object = '{type}' and att.objectid = {f.LookupObjectID};

								create index ix_{temptablename} on {temptablename}(object,objectid);

								";

							 sql = $@"
								left join Field F{tableAlias} on F{tableAlias}.FieldTypeID = {f.ID} and F{tableAlias}.ObjectType = {objectSql} and F{tableAlias}.ObjectID = {objectIdSql}
								outer apply(
								select FormattedValue = 
								(SELECT COALESCE({displayName}, ACF{tableAlias}.Code) as name,
								ACF{tableAlias}.color
								from {temptablename} ACF{tableAlias}								                                						
								{lookupValueJoinCriteria} FOR JSON PATH),
								[Value] = F{tableAlias}.[Value]
							){tableAlias}(FormattedValue, [Value]) ";

							 if (!TempTableNameList.Contains(temptablename))
							 {
								 TempTableNameList.Add(temptablename);
								 TempTableScriptList.Add(temptableScript);
							 }
						 }
						 else
						 {
							 sql = $@"
								left join Field F{tableAlias} on F{tableAlias}.FieldTypeID = {f.ID} and F{tableAlias}.ObjectType = {objectSql} and F{tableAlias}.ObjectID = {objectIdSql}
								left join FieldType FT{tableAlias} on FT{tableAlias}.ID = F{tableAlias}.FieldTypeID
								outer apply(
								select FormattedValue = 
								(SELECT COALESCE({displayName}, ACF{tableAlias}.Code) as name,
								COALESCE(JSON_VALUE(ACJ{tableAlias}.ColorJSON,'$.Value'), 'transparent') as color
								from Asset ACF{tableAlias}								                                						
								cross apply dbo.GetAssetColorJsonByColor(ACF{tableAlias}.Color) ACJ{tableAlias}                                
								{lookupValueJoinCriteria} FOR JSON PATH),
								[Value] = F{tableAlias}.[Value]
							){tableAlias}(FormattedValue, [Value]) ";
						 }
						 fieldJoins.Add(sql);

						 if (!string.IsNullOrEmpty(f.DefaultValue))
						 {
							 string selectSource = "";
							 if (IsCreateTempTable)
							 {
								 selectSource = $@"DFColor{tableAlias}.color
									 FROM #TempLookUp{f.LookupObjectType}{f.LookupObjectID} DFColor{tableAlias}
									 WHERE DFColor{tableAlias}.Object = '{f.LookupObjectType}' and DFColor{tableAlias}.ObjectID = {f.DefaultValue}";
							 } else
							 {
								selectSource = $@"COALESCE(JSON_VALUE(DFColor{tableAlias}.ColorJSON,'$.Value'), 'transparent') as color
									FROM AssetType AT
									INNER JOIN Asset A ON A.AssetTypeID = AT.ID
									cross apply dbo.GetAssetColorJsonByColor(A.Color) DFColor{tableAlias}
									WHERE AT.Object = '{type}' and AT.ObjectID = {f.LookupObjectID} and A.ObjectID = {f.DefaultValue}";
							 }


								string defaultSql = $@"
							outer apply(
							select FormattedValue = 
							(SELECT @defaultValue{tableAlias} as name,
								{selectSource} FOR JSON PATH)
							) defaultColorValue{tableAlias}(color)";
							 fieldJoins.Add(defaultSql);
						 }
					 }
					 else
					 {
						 fieldJoins.Add($"{joinPrefix} join Field {tableAlias} on {tableAlias}.FieldTypeID = {f.ID} and {tableAlias}.[ObjectType] = {objectSql} and {tableAlias}.[ObjectID] = {objectIdSql}");
					 }
				 }
				 else if (f.Type == "ComplexRelationLookup" || f.Type == "OwnershipLookup")
				 {
					 fieldJoins.Add($"{joinPrefix} join FieldTypeLookup {tableAlias} on {tableAlias}.FieldTypeID = {f.ID}");
				 }
				 else if (f.Type == "Path")
				 {
					 string pathJoinStatement = GetPathJoinSql(f);
					 if (!string.IsNullOrEmpty(pathJoinStatement))
					 {
						 fieldJoins.Add(pathJoinStatement);
					 }
				 }
				 else
				 {
					 fieldJoins.Add($"{joinPrefix} join Field {tableAlias} on {tableAlias}.FieldTypeID = {f.ID} and {tableAlias}.[ObjectType] = {objectSql} and {tableAlias}.[ObjectID] = {objectIdSql}");
				 }
			 }
			);
		}

		protected void getQueryParamsSql(AssetsApiViewModel model, AssetType assetType, List<FieldType> fieldTypes, DynamicParameters dbArgs, List<string> whereStatements, List<string> pagingSql, IEnumerable<KeyValuePair<string, string>> queryParams, List<string> fieldsUsedInMainQuery)
		{
			if (queryParams != null)
			{
				var orderBySql = "";
				var orderDirection = "";
				var offsetSql = "";
				var pageNum = -1;
				var pageSize = 200;

				if (queryParams.Any(x => x.Key == "_direction"))
				{
					var allowedDirections = new[] { "asc", "desc" };
					var order = queryParams.FirstOrDefault(x => x.Key.Trim().ToLower() == "_direction").Value;

					orderDirection = allowedDirections.Contains(order.Trim().ToLower()) ? order : "";
				}

				//add base sort if none is specified
				if (!queryParams.Any(p => p.Key == "_order"))
				{
					orderBySql = $"order by A.ID {orderDirection}";
				}

				queryParams
					.ToList()
					.ForEach(q =>
					{
						var key = q.Key.ToLower();

						if (key.StartsWith("_"))
						{
							if (key == "_order")
							{
								if (assetType.Object == "ReferenceItemType" && q.Value.ToLower() == "code")
								{
									orderBySql += (string.IsNullOrEmpty(orderBySql) ? "order by " : ", ") + $"A.Code {orderDirection} ";
								}
								else if (assetType.Object == "ReferenceItemType" && q.Value.ToLower() == "color")
								{
									orderBySql += (string.IsNullOrEmpty(orderBySql) ? "order by " : ", ") + $"ACJ.ColorJson {orderDirection} ";
									fieldsUsedInMainQuery.Add("ACJ.ColorJson");
								}
								else
								{
									var field = fieldTypes.FirstOrDefault(f => f.Name.ToLower() == q.Value.ToLower());
									var valueColumn = "FormattedValue";
									var fieldDataType = getFieldDataType(field);

									if (field == null)
									{
										string orderBy;
										switch (q.Value.Trim().ToLower())
										{
											case "createdon":
												orderBy = $"A.CreatedOn {(string.IsNullOrEmpty(orderDirection) ? "DESC" : orderDirection)}";
												break;
											case "updatedon":
												orderBy = $"A.UpdatedOn {(string.IsNullOrEmpty(orderDirection) ? "DESC" : orderDirection)}";
												break;
											case "parentdisplayname":
												orderBy = $"Parent.DisplayValue {orderDirection}";
												fieldsUsedInMainQuery.Add("Parent.DisplayValue");
												break;
											case string order when order.ToUpperInvariant().Contains("PATH_SEGMENT_IDX_"):
												var segment_idx = int.Parse(order.ToUpperInvariant().Replace("PATH_SEGMENT_IDX_", "")) + 1;
												orderBy = $"Node.Segments.value('(/path/segment)[{segment_idx}]', 'nvarchar(800)') {(string.IsNullOrEmpty(orderDirection) ? "DESC" : orderDirection)}";
												break;
											default:
												orderBy = $"A.ID {orderDirection}";
												break;
										}

										orderBySql += (string.IsNullOrEmpty(orderBySql) ? "order by " : ", ") + orderBy;
										return;
									}

									if (field.Type == "Link")
									{
										valueColumn = "Value";
									}

									if (!string.IsNullOrEmpty(fieldDataType))
									{
										fieldsUsedInMainQuery.Add($"F{field.ID}.");
										if (fieldDataType == "bit")
										{
											orderBySql += (string.IsNullOrEmpty(orderBySql) ? "order by " : ", ") + $"try_cast(case when F{field.ID}.{valueColumn} is null then null when F{field.ID}.{valueColumn} = 'true' then 1 else 0 end as {fieldDataType}) {orderDirection}";
										}
										else
										{
											orderBySql += (string.IsNullOrEmpty(orderBySql) ? "order by " : ", ") + $"try_cast(case when LEN(ISNULL(F{field.ID}.{valueColumn}, '')) < 1 then null else F{field.ID}.{valueColumn} end as {fieldDataType}) {orderDirection}";
										}
									}
									else
									{
										fieldsUsedInMainQuery.Add($"F{field.ID}");
										if (field.Type == "JsonElement")
										{
											fieldsUsedInMainQuery.Add($"FJP{field.ID}");
											FieldTypeDefinition_JsonElement jsonElementDefinition = JsonConvert.DeserializeObject<FieldTypeDefinition_JsonElement>(field.Definition);

											if (jsonElementDefinition.DataType == "decimal")
											{
												jsonElementDefinition.DataType = "float";
											}

											fieldDataType = jsonElementDefinition.DataType;

											orderBySql += (string.IsNullOrEmpty(orderBySql) ? "order by " : ", ") + $"try_cast(FJP{field.ID}.Value as {fieldDataType}) {orderDirection}";
										}
										else if (field.Type == "Path")
										{
											var orderStatement = GetPathSelectSql(field);
											fieldsUsedInMainQuery.Add(orderStatement);

											orderBySql += (string.IsNullOrEmpty(orderBySql) ? "order by " : ", ") + $"{orderStatement} {orderDirection}";
										}
										else if (field.Type == "Score")
										{
											orderBySql += (string.IsNullOrEmpty(orderBySql) ? "order by " : ", ") + $"F{field.ID}.[Value] {orderDirection}";
										}
										else if (field.Type == "Counter")
										{
											orderBySql += (string.IsNullOrEmpty(orderBySql) ? "order by " : ", ") + $"F{field.ID}.{valueColumn} {orderDirection}";
										}
										else
										{
											var tableAlias = (field.Type == "Lookup" && CompanyContext.LookupFieldHasColorItem(field)) ? $"FF{field.ID}" : $"F{field.ID}";
											if(!string.IsNullOrEmpty(field.DefaultValue))
											{
												orderBySql += (string.IsNullOrEmpty(orderBySql) ? "order by " : ", ") + $"COALESCE({tableAlias}.{valueColumn}, @defaultValueF{field.ID}) {orderDirection}";
											}
											else
											{
												orderBySql += (string.IsNullOrEmpty(orderBySql) ? "order by " : ", ") + $"{tableAlias}.{valueColumn} {orderDirection}";
											}
										}
									}
								}
							}
							else if (key == "_pagenum")
							{
								if (int.TryParse(q.Value, out pageNum))
								{
									if (pageNum < 1)
									{
										pageNum = 1;
									}
								}
							}
							else if (key == "_pagesize")
							{
								if (int.TryParse(q.Value, out pageSize))
								{
									if (pageSize < 1)
									{
										pageSize = 1;
									}
								}
							}
						}
						else
						{
							if (assetType.Object == "ReferenceItemType" && key == "code")
							{
								fieldsUsedInMainQuery.Add("RI.");
								whereStatements.Add($"RI.[Code] = @code");
								dbArgs.Add($"@code", q.Value);
							}
							else
							{
								var field = fieldTypes.Find(f => f.Name.ToLower() == key);

								if (field != null)
								{
									switch (field.Type)
									{
										case "JsonElement":
											fieldsUsedInMainQuery.Add($"FJP{field.ID}");
											whereStatements.Add($"FJP{field.ID}.Value = @field{field.ID}");
											dbArgs.Add($"@field{field.ID}", q.Value);
											break;
										case "Path":
											whereStatements.Add($"Node.DisplayPath like '%' + ltrim(rtrim(replace(replace(@field{field.ID}, '>', ''), '%', ''))) + '%'");
											dbArgs.Add($"@field{field.ID}", q.Value);
											break;
										default:
											fieldsUsedInMainQuery.Add($"F{field.ID}");
											whereStatements.Add($"F{field.ID}.FormattedValue = @field{field.ID}");
											dbArgs.Add($"@field{field.ID}", q.Value);
											break;
									}
								}
							}
						}
					});


				bool useTypeLevelDefaultSorts = false;
				var defSorts = queryParams.FirstOrDefault(x => x.Key.ToLower() == "usetypeleveldefaultsorts");
				if (!string.IsNullOrEmpty(defSorts.Key))
				{
					bool.TryParse(defSorts.Value, out useTypeLevelDefaultSorts);
				}

				if (useTypeLevelDefaultSorts)
				{
					var orderFields = fieldTypes.Where(x => x.SortOrder > 0 && x.IsListable == true)
						.OrderBy(x => x.SortOrder)
						.GroupBy(x => x.SortOrder)
						.ToList();


					if (orderFields.Count == 0)
					{
						orderBySql = "order by A.ID ";
					}
					else
					{
						List<string> sortStatements = new List<string>();
						orderFields.ForEach(ft =>
						{
							fieldsUsedInMainQuery.AddRange(ft.Select(x => "F" + x.ID.ToString()));
							if (ft.Count() == 1)
							{
								var sortDirection = ft.FirstOrDefault().SortByAscending ? "asc" : "desc";
								sortStatements.Add(getFieldDataTypeWrapper(ft.FirstOrDefault()) + " " + sortDirection);
							}
							else
							{
								//If same sort number order by field type Name
								var fts = ft.ToList().OrderBy(x => x.Name).ToList();
								fts.ForEach(_ft =>
								{
									var sortDirection = _ft.SortByAscending ? "asc" : "desc";
									sortStatements.Add(getFieldDataTypeWrapper(_ft) + " " + sortDirection);
								});
							}
						});

						orderBySql = "order by " + string.Join(", ", sortStatements);
					}
				}

				pagingSql.Add(orderBySql);

				if (pageSize > 0 || pageNum > 0)
				{
					if (pageSize < 1)
					{
						pageSize = 1;
					}
					if (pageNum < 1)
					{
						pageNum = 1;
					}

					model.pageSize = pageSize;
					model.pageNum = pageNum;

					offsetSql = $"offset {pageSize * (pageNum - 1)} rows fetch next {pageSize} rows only";
					pagingSql.Add(offsetSql);
				}

			}
		}

		protected string getFieldDataTypeWrapper(FieldType ft)
		{
			if (ft.Type == DataType.Path.ToString())
			{
				return GetPathSelectSql(ft);
			}

			if (ft.Type == "Score")
			{
				return $"F{ft.ID}.Value";
			}
			else
			{
				var fieldType = getFieldDataType(ft);

				string val = $"F{ft.ID}.FormattedValue";

				if (!string.IsNullOrEmpty(ft.DefaultFormattedValue))
				{
					if(ft.Type == "Lookup" && CompanyContext.LookupFieldHasColorItem(ft))
					{
						val = $"F{val}";
					}
					val = $"coalesce({val}, '{ft.DefaultFormattedValue}')";
				}

				if (!string.IsNullOrEmpty(fieldType))
				{
					if (fieldType == "bit")
					{
						return $"try_cast(case when {val} = 'true' then 1 else 0 end as {fieldType})";
					}
					else
					{
						return $"try_cast({val} as {fieldType})";
					}
				}

				return val;
			}
		}

		protected string getFieldDataType(FieldType field)
		{
			switch (field?.Type)
			{
				case "Date":
				case "DateTime":
					return "datetime";
				case "Number":
					return "bigint";
				case "Decimal":
					return "float";
				case "Boolean":
					return "bit";
				default:
					return "";
			}
		}

		protected void setCellValueFromField(SLDocument document, int rowIndex, int colIndex, FieldType field, object value)
		{
			var valueString = value?.ToString() ?? "";
			switch ((field.Type ?? "").ToUpper())
			{
				case "DECIMAL":
					double dVal = 0;
					if (double.TryParse(valueString, out dVal))
					{
						document.SetCellValue(rowIndex, colIndex, dVal);
					}
					else
					{
						document.SetCellValue(rowIndex, colIndex, valueString);
					}
					break;
				case "NUMBER":
					int intVal = 0;
					if (int.TryParse(valueString, out intVal))
					{
						document.SetCellValue(rowIndex, colIndex, intVal);
					}
					else
					{
						document.SetCellValue(rowIndex, colIndex, valueString);
					}
					break;
				case "DATE":
					if (DateTime.TryParse(valueString, out DateTime dateVal))
					{
						document.SetCellValue(rowIndex, colIndex, dateVal);

						SLStyle style = document.CreateStyle();
						style.FormatCode = "m/d/yyyy";
						document.SetCellStyle(rowIndex, colIndex, style);
					}
					break;
				case "HTML":
					var doc = new HtmlAgilityPack.HtmlDocument();
					doc.LoadHtml(value + "");
					var txt = HtmlAgilityPack.HtmlEntity.DeEntitize(doc.DocumentNode.InnerText);
					if (txt.StartsWith("="))
					{
						txt = "'" + txt;
					}
					document.SetCellValue(rowIndex, colIndex, txt);
					break;
				case "OWNERSHIPLOOKUP":
					if (value != null)
					{
						string owners = "";
						if (value.GetType() == typeof(JArray))
						{
							var ownerships = ((JArray)value).ToObject<List<dynamic>>();
							owners = string.Join(" | ", ownerships.OrderBy(o => o.ResourceName).Select(o => $"{o.ResourceName} ({o.ResponsibilityTypes})"));
						}
						document.SetCellValue(rowIndex, colIndex, owners);
					}
					break;
				default:
					if (valueString.StartsWith("="))
					{
						valueString = "'" + valueString;
					}
					document.SetCellValue(rowIndex, colIndex, valueString);
					break;
			}
		}

		protected void SetExcelColumnWidths(SLDocument document, List<FieldType> fields, int totalRows = -1)
		{
			int index = 1;
			foreach (var field in fields)
			{
				try
				{
					if (field.ColumnWidth.HasValue)
					{
						int width = field.ColumnWidth.Value > 0 ? field.ColumnWidth.Value / 10 : 0;
						document.SetColumnWidth(index, width);
					}
					else
					{
						//dont autofit colums if there are > 2000 rows or json fields as this process is slow for these
						if (field.Type != "JSON" && totalRows < 2000)
						{
							document.AutoFitColumn(index);
						}
					}
					index++;
				}
				catch
				{
					document.SetColumnWidth(index, 10);
					index++;
				}
			}
		}

		/// <summary>
		/// Common code for creating batch calls.
		/// </summary>
		/// <param name="executionInfo"></param>
		/// <param name="execution"></param>
		/// <param name="data"></param>
		/// <returns></returns>
		protected async Task<ApiExecutionInfo> CreateApiBatchJob(ApiExecutionInfo executionInfo, ApiExecution execution, object data, IStorageProvider storageProvider, IQueueSource queueSource)
		{
			// Save to storage container.
			await storageProvider.CreateFile(executionInfo.StorageFolder, executionInfo.RequestFileName, JsonConvert.SerializeObject(data));

			// Save to the database.
			execution.ExecutionID = executionInfo.ExecutionID;
			CompanyContext.Add(execution);

			// Save to queue.
			if (!await queueSource.CreateMessageAsync(Config.GetValue<string>("ApiExecutionQueue"), executionInfo))
			{
				throw new Exception(AZURE_QUEUE_INSERTION_FAILURE_MESSAGE);
			}

			return executionInfo;
		}

		protected string getAllowedAllValue(string AllowAllLabel, bool hasColor)
		{
			if (!hasColor)
			{
				return AllowAllLabel;
			}
			else
			{
				return "[{ \"name\":\"" + AllowAllLabel + "\",\"color\":\"transparent\"}]";
			}
		}
	}
}
