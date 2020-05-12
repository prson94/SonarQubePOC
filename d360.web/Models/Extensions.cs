using d360.core.entities;
using d360.web.Filters;
using DocumentFormat.OpenXml.Office2013.PowerPoint.Roaming;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Web;

namespace d360.web.Models
{
    public static class Extensions
    {
        public static FieldTypeComplexLookupDefinition ParseComplexLookupDefinition(this FieldTypeLookup lookup)
        {
            return JsonConvert.DeserializeObject<FieldTypeComplexLookupDefinition>(lookup.Definition);
        }

        public static FieldTypeOwnershipLookupDefinition ParseOwnershipLookupDefinition(this FieldTypeLookup lookup)
        {
            return JsonConvert.DeserializeObject<FieldTypeOwnershipLookupDefinition>(lookup.Definition);
        }

        public static CustomJSONContractResolver GetFriendlyNameJSONContract(this FieldTypeComplexLookupDefinition definition)
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
                var assetIdx = assetTypes.IndexOf(ft.AssetTypeUid) + 1;
                var fname = string.IsNullOrEmpty(ft.OverrideDisplayName) ? ft.FieldTypeName : ft.OverrideDisplayName;

                if (ft.FieldTypeID > 0)
                {
                    if (ft.FieldTypeName.StartsWith("Related Item."))
                    {
                        customContractProperties.Add($"H{assetIdx}_{ft.FieldTypeID}_Uid", $"Asset.[{assetIdx - 1}].RelatedItems.[{relatedItemIdx}].Uid");
                        customContractProperties.Add($"H{assetIdx}_{ft.FieldTypeID}_DisplayValue", $"Asset.[{assetIdx - 1}].RelatedItems.[{relatedItemIdx}].DisplayValue");
                        customContractProperties.Add($"H{assetIdx}_{ft.FieldTypeID}_Url", $"Asset.[{assetIdx - 1}].RelatedItems.[{relatedItemIdx}].Url");
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
            var customContract = new CustomJSONContractResolver(customContractProperties);
            return customContract;
        }


        public static List<dynamic> UnflattenJson(this FieldTypeComplexLookupDefinition definition, List<dynamic> Values)
        {
            List<dynamic> unflattened = new List<dynamic>();
            foreach (var item in Values)
            {
                List<dynamic> Assets = new List<dynamic>();

                //Deflate asset fields
                for (int i = 0; i < definition.Relations.Count; i++)
                {
                    var relFields = definition.Fields
                        .Where(x => x.AssetTypeUid == definition.Relations[i].AssetTypeUid
                                && x.FieldTypeName.StartsWith("Related Item."))
                        .Count();

                    var dict = new Dictionary<string, object>();
                    dict.Add("RelationshipTypeUid", definition.Relations[i].IntersectTypeUid);
                    dict.Add("AssetTypeUid", definition.Relations[i].AssetTypeUid);
                    dict.Add("RelationType", definition.Relations[i].RelationType);

                    dynamic[] relatedItems = new dynamic[relFields];

                    foreach (JProperty prop in item)
                    {
                        var match = $"Asset.[{i}].";
                        if (prop.Name.Contains(match))
                        {
                            var propName = prop.Name.Replace(match, "");
                            bool isAdded = false;

                            for (int rf = 0; rf < relFields; rf++)
                            {
                                var relMatch = $"RelatedItems.[{rf}].";
                                if (propName.StartsWith(relMatch))
                                {
                                    propName = propName.Replace(relMatch, "");
                                    var relatedItemFields = (IDictionary<string, object>)relatedItems[rf];
                                    if (relatedItemFields == null)
                                    {
                                        relatedItemFields = new Dictionary<string, object>();
                                    }
                                    relatedItemFields.Add(propName, prop.Value);
                                    relatedItems[rf] = relatedItemFields;
                                    isAdded = true;
                                }
                            }

                            if (!isAdded)
                                dict.Add(propName, prop.Value);
                        }
                    }
                    dict.Add("RelatedItems", relatedItems);
                    Assets.Add(dict);
                }



                unflattened.Add(Assets);
            }

            return unflattened;
        }

    }
}