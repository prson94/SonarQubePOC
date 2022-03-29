using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

using d360.core.entities;
using d360.core.enums;
using d360.web.Models;
using d360.web.Models.Attributes;

using Dapper;

namespace d360.web.Controllers
{
	public partial class FormController : BaseController
	{
		#region Supporting Json Feeds

		[Route("FieldType_Lookup_FilteredByPredicate"), NonNullableParameters]
		public JsonNetResult FieldType_Lookup_FilteredByPredicate(int fieldTypeId, string objectType, int ObjectID, string value = "", string query = "")
		{
			IntersectType it;
			DynamicParameters queryParameters = new DynamicParameters();

			var selectList = new List<SelectListInfoItem>();
			string exceptionMessage = "";
			bool useTypeahead = false;

			int typeaheadThreshold = 1 + SettingsRepository.GetSettingValue<int>(Setting.MaxDropdownItems);

			try
			{
				var filterObject = Company.Filter<Asset>(i => i.ObjectID == ObjectID && i.Object == objectType).SingleOrDefault();
				
				if (filterObject == null)
				{
					exceptionMessage = "Action subject is not an asset. Filter disabled.";
				}

				var ft = Company.GetById<FieldType>(fieldTypeId);

				queryParameters.Add("fieldTypeId", ft.ID);
				queryParameters.Add("lookupObjectType", ft.LookupObjectType);
				queryParameters.Add("lookupObjectId", ft.LookupObjectID);

				string selectedValue = string.IsNullOrWhiteSpace(value) ? (string.IsNullOrWhiteSpace(ft.DefaultValue) ? "" : ft.DefaultValue) : value;
				queryParameters.Add("selectedValue", selectedValue);

				if (!string.IsNullOrWhiteSpace(query))
				{
					//If the query is not empty, this is a typeahead query, so our threshold on results is 20
					typeaheadThreshold = 20;
				}
				else
				{
					//Don't include "choose.." and allow all in typeahead results
					if (!ft.IsRequired && !ft.AllowMultipleValues)
					{
						selectList.Add(new SelectListInfoItem { Text = "Choose...", Value = "" });
					}

					if (ft.AllowAllValue)
					{
						selectList.Add(new SelectListInfoItem { Text = ft.AllowAllLabel, Value = "0" });
					}
				}

				if (exceptionMessage == "")
				{
					if (ft.FilterPredicateDirection == true)
					{
						it = Company.Filter<IntersectType>(i =>
							i.Object == ft.LookupObjectType + "Type" &&
							i.ObjectID == ft.LookupObjectID &&
							i.Subject == filterObject.AssetType.Object &&
							i.SubjectID == filterObject.AssetType.ObjectID &&
							i.PredicateID == ft.FilterPredicateID
						).SingleOrDefault();
					}
					else
					{
						it = Company.Filter<IntersectType>(i =>
							i.Subject == ft.LookupObjectType + "Type" &&
							i.SubjectID == ft.LookupObjectID &&
							i.Object == filterObject.AssetType.Object &&
							i.ObjectID == filterObject.AssetType.ObjectID &&
							i.PredicateID == ft.FilterPredicateID
						).SingleOrDefault();
					}

					if (it == null)
					{
						var lookupObjectType = Company.Filter<AssetType>(i => i.ObjectID == ft.LookupObjectID && i.Object == ft.LookupObjectType + "Type").SingleOrDefault();
						string listObjectType = lookupObjectType.Class + ":" + lookupObjectType.Name;
						Predicate pred = Company.GetById<Predicate>(ft.FilterPredicateID.GetValueOrDefault());
						string predicate = (ft.FilterPredicateDirection == true) ? pred.Inverse : pred.Name;
						var filterObjectDetail = Company.Filter<AssetDetail>(i => i.ObjectID == ObjectID && i.Object == objectType).SingleOrDefault();
						string actionSubject = filterObjectDetail.DisplayValue;
						string actionSubjectType = filterObject.AssetType.Class + ":" + filterObject.AssetType.Name;
						exceptionMessage = $@"Filtering for this list has been disabled as we cannot filter a list of types {listObjectType} by the action subject {actionSubject}.";
						exceptionMessage += $@" The relationship {listObjectType} - {predicate} - {actionSubjectType} does not exist.";
					}
					else
					{
						queryParameters.Add("IntersectTypeID", it.ID);
					}
				}

				var selectedSql = $@"select TOP ({typeaheadThreshold}) V.Value, V.Text, '' as Info
					from FieldLookupValue V 
					where V.FieldTypeID = @fieldTypeId and V.LookupObjectType = @lookupObjectType and V.lookupObjectID = @lookupObjectId and V.Value = @selectedValue 
					union";

				var columns = $@"
					V.Value,
					V.Text";
				
				var joinSql = "";
				var whereSql = "where V.FieldTypeID = @fieldTypeId and V.LookupObjectType = @lookupObjectType and V.lookupObjectID = @lookupObjectId ";

				if (exceptionMessage == "")
				{
					if (ft.FilterPredicateDirection == true)
					{
						columns += @", concat(I.PredicateInverse,' ', I.SubjectShortName) as Info";
					}
					else
					{
						columns += @", concat(I.PredicateName,' ', I.ObjectShortName) as Info";
					}

					joinSql = $@" inner join [IntersectDetail] I on I.{ (ft.FilterPredicateDirection == true ? "ObjectID" : "SubjectID") } = V.Value ";
					whereSql += $@" and I.IntersectTypeID = @IntersectTypeID and I.{(ft.FilterPredicateDirection == true ? "SubjectID" : "ObjectID")} = @ObjecctID ";

					queryParameters.Add("ObjecctID", ObjectID);
				}
				else
				{
					columns += @", '' as Info";

				}

				if (!string.IsNullOrWhiteSpace(query))
				{
					whereSql += " and V.Text like '%' + @query + '%' ";
					queryParameters.Add("query", query);
				}

				var itemsSql = $@"
					{(string.IsNullOrWhiteSpace(selectedValue) ? "" : selectedSql)}
					select {columns}
					from FieldLookupValue V
					{joinSql}
					{whereSql}";

				var items = Company.Query<SelectListInfoItem>(itemsSql, queryParameters).ToList();

				if (items.Count() >= typeaheadThreshold)
				{
					useTypeahead = true;
				}

				selectList.AddRange(items.Select(i => new SelectListInfoItem
				{
					Text = i.Text,
					Value = i.Value.ToString(),
					Selected = string.IsNullOrEmpty(selectedValue) ? false : i.Value.ToString() == selectedValue,
					Info = i.Info
				}));

			}
			catch
			{
				if (exceptionMessage == "")
				{
					exceptionMessage = "Filter disabled. An error occured when attempting to apply it.";
				}
			}
			selectList = selectList.OrderBy(i => i.Selected ? 0 : 1).ToList();

			return new JsonNetResult
			{
				Data = new { items = selectList, exceptionMessage, useTypeahead },
				Formatting = Newtonsoft.Json.Formatting.None
			};
		}

		[Route("FieldType_TypeAheadLookup"), NonNullableParameters]
		public JsonNetResult FieldType_TypeAheadLookup(int fieldTypeId, string value = "", string query = "", bool useColor = false)
		{
			var selectList = new List<SelectListItem>();
			var ft = Company.GetById<FieldType>(fieldTypeId);
			string selectedValue = string.IsNullOrWhiteSpace(value) 
									? (string.IsNullOrWhiteSpace(ft.DefaultValue) ? "" : ft.DefaultValue) 
									: value;

			if (ft.AllowAllValue)
			{
				selectList.Add(new SelectListItem { Text = ft.AllowAllLabel, Value = "0" });
			}

			int maxItems = 20;
			var columns = $@"
				V.FieldTypeID,
				V.LookupObjectType,
				V.LookupObjectID,
				V.Value,
				{(useColor ? "colorJson.FV AS Text" : "V.Text")}";

			var colorjoin = $@"
							outer apply(SELECT FV = (SELECT V.Text as name, COALESCE(JSON_VALUE(ACJ.ColorJSON,'$.Value'), 'transparent') as color 
										from Asset A 
										outer apply dbo.GetAssetColorJsonByColor(A.Color) ACJ
										where A.Object = v.LookupObjectType and A.ObjectID = V.Value FOR JSON PATH, WITHOUT_ARRAY_WRAPPER) 
							)colorJSON ";

			var selectedSql = $@"select {columns} 
				from FieldLookupValue V 
				{(useColor ? colorjoin : "")}
				where V.FieldTypeID = @fieldTypeId and V.LookupObjectType = @lookupObjectType and V.lookupObjectID = @lookupObjectId and V.Value = @selectedValue 
				union";

			var resourceJoin = $@"
				inner join reporting.Global_resource R on R.ResourceID = V.Value and R.Email not like '%@data3sixty.com' and R.Email not like '%@infogix.com' and R.Email not like '%@precisely.com'";

			var itemsSql = $@"
				{(string.IsNullOrWhiteSpace(selectedValue) ? "" : selectedSql)}
				select top {maxItems} {columns}
				from FieldLookupValue V
				{(HideData3SixtyUsers() && ft.LookupObjectType == "Resource" ? resourceJoin : "")}
				{(useColor ? colorjoin : "")}
				where V.FieldTypeID = @fieldTypeId and V.LookupObjectType = @lookupObjectType and V.lookupObjectID = @lookupObjectId {(string.IsNullOrWhiteSpace(query) ? "" : " and V.Text like '%' + @query + '%' ")}";

			var items = Company.Query<FieldLookupValue>(itemsSql, new { fieldTypeId = ft.ID, lookupObjectType = ft.LookupObjectType, lookupObjectId = ft.LookupObjectID, selectedValue, query }).ToList();

			selectList.AddRange(items.Select(i => new SelectListItem { Text = i.Text, Value = i.Value.ToString(), Selected = string.IsNullOrEmpty(selectedValue) ? false : i.Value.ToString() == selectedValue }));

			selectList = selectList.OrderBy(i => i.Selected ? 0 : 1).ToList();

			return new JsonNetResult
			{
				Data = selectList,
				Formatting = Newtonsoft.Json.Formatting.None
			};
		}

		[Route("FieldType_TypeaheadJsonPropertyOptionsForJsonField"), NonNullableParameters]
		public JsonNetResult FieldType_TypeaheadJsonPropertyOptionsForJsonField(string fieldName, string phrase, Guid? assetTypeUid, Guid? actionTypeUid, Guid? relationshipTypeUid)
		{
			var selectList = new List<SelectListItem>();
			FieldType ft = null;

			if (assetTypeUid != null)
			{
				int atID = Company.Filter<AssetType>(x => x.uid == assetTypeUid).SingleOrDefault().ID;
				ft = Company.Filter<FieldType>(x => x.AssetTypeID == atID && x.Name == fieldName).SingleOrDefault();
			}
			else if (actionTypeUid != null)
			{
				int atID = Company.Filter<IssueType>(x => x.uid == actionTypeUid).SingleOrDefault().ID;
				ft = Company.Filter<FieldType>(x => x.AssetTypeID == atID && x.Name == fieldName).SingleOrDefault();
			}
			else if (relationshipTypeUid != null)
			{
				var itID = Company.Filter<IntersectType>(i => i.uid == relationshipTypeUid).SingleOrDefault().ID;
				ft = Company.Filter<FieldType>(x => x.AssetTypeID == itID && x.Name == fieldName).SingleOrDefault();
			}
			else
			{
				throw new Exception("No assetTypeUid or actionTypeUid or relationshipTypeUid provided");
			}
			phrase = phrase.Replace("[", @"\[");
			var sql = $@"
						select		P.[Path]
						from		FieldJsonProperty P
									inner join Field F on F.ID = P.FieldID and F.FieldTypeID = @fieldTypeId and P.[Path] like @phrase+'%' escape '\'
						group by	P.[Path]
						order by	P.[Path]
						offset 0 rows fetch next 25 rows only";

			var items = Company.Query<string>(sql, new { fieldTypeId = ft.ID, phrase }).ToList();

			return new JsonNetResult
			{
				Data = items,
				Formatting = Newtonsoft.Json.Formatting.None
			};
		}

		#endregion                        
	}
}
