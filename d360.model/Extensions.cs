using d360.core.entities;
using d360.model.helpers;
using Dapper;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;

namespace d360.model
{
	public static class Extensions
	{
		/// <summary>
		/// This extension converts an enumerable set to a Dapper TVP
		/// </summary>
		/// <typeparam name="T">type of enumerbale</typeparam>
		/// <param name="enumerable">list of values</param>
		/// <param name="typeName">database type name</param>
		/// <param name="orderedColumnNames">if more than one column in a TVP, 
		/// columns order must mtach order of columns in TVP</param>
		/// <returns>a custom query parameter</returns>
		public static SqlMapper.ICustomQueryParameter AsTableValuedParameter<T>
			(this IEnumerable<T> enumerable,
			string typeName, IEnumerable<string> orderedColumnNames = null)
		{
			DataTable dataTable = new DataTable();
			if (typeof(T).IsValueType || typeof(T).FullName.Equals("System.String"))
			{
				dataTable.Columns.Add(orderedColumnNames == null ?
					"NONAME" : orderedColumnNames.First(), typeof(T));
				foreach (T obj in enumerable)
				{
					dataTable.Rows.Add(obj);
				}
			}
			else
			{
				PropertyInfo[] properties = typeof(T).GetProperties
					(BindingFlags.Public | BindingFlags.Instance);
				PropertyInfo[] readableProperties = properties.Where
					(w => w.CanRead).ToArray();
				if (readableProperties.Length > 1 && orderedColumnNames == null)
				{
					throw new ArgumentException("Ordered list of column names must be provided when TVP contains more than one column");
				}

				string[] columnNames = (orderedColumnNames ??
					readableProperties.Select(s => s.Name)).ToArray();
				foreach (string name in columnNames)
				{
					dataTable.Columns.Add(name, readableProperties.Single
						(s => s.Name.Equals(name)).PropertyType);
				}

				foreach (T obj in enumerable)
				{
					dataTable.Rows.Add(
						columnNames.Select(s => readableProperties.Single
							(s2 => s2.Name.Equals(s)).GetValue(obj))
							.ToArray());
				}
			}

			return dataTable.AsTableValuedParameter(typeName);
		}

		public static FieldTypeComplexLookupDefinition ParseComplexLookupDefinition(this FieldTypeLookup lookup)
		{
			FieldTypeComplexLookupDefinition definition = JsonConvert.DeserializeObject<FieldTypeComplexLookupDefinition>(lookup.Definition);

			if (definition.Fields != null)
			{
				foreach (FieldTypeComplexLookupDefinitionField f in definition.Fields.Where(f => f.RelationIndex == null))
				{
					f.RelationIndex = definition.Relations.FindIndex(r => r.AssetTypeUid == f.AssetTypeUid);
				}
			}
			return definition;
		}

		public static FieldTypeOwnershipLookupDefinition ParseOwnershipLookupDefinition(this FieldTypeLookup lookup)
		{
			return JsonConvert.DeserializeObject<FieldTypeOwnershipLookupDefinition>(lookup.Definition);
		}

		public static CustomJSONContractResolver GetFriendlyNameJSONContract(this FieldTypeComplexLookupDefinition definition)
		{
			Dictionary<string, string> customContractProperties = definition.GetFriendlyNamesMapping();
			CustomJSONContractResolver customContract = new CustomJSONContractResolver(customContractProperties);

			return customContract;
		}

		public static Dictionary<string, string> GetFriendlyNamesMapping(this FieldTypeComplexLookupDefinition definition)
		{
			List<Guid> assetTypes = definition.Relations.Select(x => x.AssetTypeUid.HasValue ? x.AssetTypeUid.Value : Guid.Empty).ToList();

			Dictionary<string, string> customContractProperties = new Dictionary<string, string>();
			for (int i = 0; i < definition.Relations.Count; i++)
			{
				customContractProperties.Add($"H{i + 1}_Uid", $"Asset.[{i}].Uid");
				customContractProperties.Add($"H{i + 1}_Url", $"Asset.[{i}].Url");
			}

			int relatedItemIdx = 0;
			definition.Fields.ForEach(ft =>
			{
				int assetIdx = (ft.RelationIndex ?? assetTypes.IndexOf(ft.AssetTypeUid)) + 1;
				string fname = string.IsNullOrEmpty(ft.OverrideDisplayName) ? ft.FieldTypeName : ft.OverrideDisplayName;

				if (ft.FieldTypeID > 0)
				{
					if (ft.FieldTypeName.StartsWith("Related Item."))
					{
						customContractProperties.Add($"H{assetIdx}_{ft.FieldTypeID}_Uid", $"Asset.[{assetIdx - 1}].RelatedItems.[{relatedItemIdx}].Uid");
						customContractProperties.Add($"H{assetIdx}_{ft.FieldTypeID}_DisplayValue", $"Asset.[{assetIdx - 1}].RelatedItems.[{relatedItemIdx}].DisplayValue");
						customContractProperties.Add($"H{assetIdx}_{ft.FieldTypeID}_Url", $"Asset.[{assetIdx - 1}].RelatedItems.[{relatedItemIdx}].Url");
						customContractProperties.Add($"H{assetIdx}_{ft.FieldTypeID}_IntersectTypeUid", $"Asset.[{assetIdx - 1}].RelatedItems.[{relatedItemIdx}].IntersectTypeUid");
						relatedItemIdx++;
					}
					else
					{
						customContractProperties.Add($"H{assetIdx}_{ft.FieldTypeID}", $"Asset.[{assetIdx - 1}].{fname}");
					}
				}
				else
				{
					customContractProperties.Add($"H{assetIdx}_{ft.FieldTypeName}", $"Asset.[{assetIdx - 1}].{fname}");
				}
			});

			return customContractProperties;
		}

		public static Dictionary<string, FieldTypeComplexLookupDefinitionField> GetFieldMapings(this FieldTypeComplexLookupDefinition definition)
		{
			List<Guid> assetTypes = definition.Relations.Select(x => x.AssetTypeUid.HasValue ? x.AssetTypeUid.Value : Guid.Empty).ToList();

			Dictionary<string, FieldTypeComplexLookupDefinitionField> map = new Dictionary<string, FieldTypeComplexLookupDefinitionField>();

			for (int i = 0; i < definition.Relations.Count; i++)
			{
				map.Add($"H{i + 1}_Uid", null);
				map.Add($"H{i + 1}_Url", null);
			}

			definition.Fields.ForEach(ft =>
			{
				int assetIdx = (ft.RelationIndex ?? assetTypes.IndexOf(ft.AssetTypeUid)) + 1;

				if (ft.FieldTypeID > 0)
				{
					if (ft.FieldTypeName.StartsWith("Related Item."))
					{
						map.Add($"H{assetIdx}_{ft.FieldTypeID}_IntersectTypeUid", ft);
						map.Add($"H{assetIdx}_{ft.FieldTypeID}_Uid", null);
						map.Add($"H{assetIdx}_{ft.FieldTypeID}_DisplayValue", null);
						map.Add($"H{assetIdx}_{ft.FieldTypeID}_Url", null);

					}
					else
					{
						map.Add($"H{assetIdx}_{ft.FieldTypeID}", ft);
					}
				}
				else
				{
					map.Add($"H{assetIdx}_{ft.FieldTypeName}", ft);
				}
			});

			return map;
		}

		public static List<dynamic> UnflattenJson(this FieldTypeComplexLookupDefinition definition, List<dynamic> Values)
		{
			List<dynamic> unflattened = new List<dynamic>();
			foreach (dynamic item in Values)
			{
				List<dynamic> Assets = new List<dynamic>();

				//Deflate asset fields
				for (int i = 0; i < definition.Relations.Count; i++)
				{
					int relFields = definition.Fields
						.Where(x => x.FieldTypeName.StartsWith("Related Item."))
						.Count();

					Dictionary<string, object> dict = new Dictionary<string, object>
					{
						{ "RelationshipTypeUid", definition.Relations[i].IntersectTypeUid },
						{ "AssetTypeUid", definition.Relations[i].AssetTypeUid },
						{ "RelationType", definition.Relations[i].RelationType }
					};

					dynamic[] relatedItems = new dynamic[relFields];

					bool hasRelation = false;
					Dictionary<string, object> relationFields = new Dictionary<string, object>();

					foreach (JProperty prop in item)
					{
						string match = $"Asset.[{i}].";

						if (prop.Name.Contains(match))
						{
							string propName = prop.Name.Replace(match, "");
							bool isAdded = false;

							for (int rf = 0; rf < relFields; rf++)
							{
								string relMatch = $"RelatedItems.[{rf}].";

								if (propName.StartsWith(relMatch))
								{
									propName = propName.Replace(relMatch, "");
									IDictionary<string, object> relatedItemFields = (IDictionary<string, object>)relatedItems[rf];

									if (relatedItemFields == null)
									{
										relatedItemFields = new Dictionary<string, object>();

									}

									relatedItemFields.Add(propName, prop.Value);
									relatedItems[rf] = relatedItemFields;
									isAdded = true;
								}
							}

							string relItemMatch = "Relation.";

							if (propName.StartsWith(relItemMatch))
							{
								propName = propName.Replace(relItemMatch, "");
								relationFields.Add(propName, prop.Value);
								isAdded = true;
								hasRelation = true;
							}

							if (!isAdded)
							{
								dict.Add(propName, prop.Value);
							}
						}
					}

					if (relFields > 0)
					{
						dict.Add("RelatedItems", relatedItems.Where(x => x != null));
					}

					if (hasRelation)
					{
						dict.Add("Relation", relationFields);
					}

					Assets.Add(dict);
				}

				unflattened.Add(Assets);
			}

			return unflattened;
		}
	}
}
