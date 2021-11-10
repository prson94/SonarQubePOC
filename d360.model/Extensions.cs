using System;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using System.Linq.Expressions;
using System.Reflection;
using System.Xml.Linq;
using System.Xml;
using Dapper;
using d360.core.entities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using d360.model.helpers;

namespace d360.model
{
    #region Dynamic SQL Querying

    public static class DynamicQueryable
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
            var dataTable = new DataTable();
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

                var columnNames = (orderedColumnNames ??
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

        public static IQueryable Take(this IQueryable source, int count)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }
            return source.Provider.CreateQuery(
                Expression.Call(
                    typeof(Queryable), "Take",
                    new[] { source.ElementType },
                    source.Expression, Expression.Constant(count)));
        }

        public static IQueryable Skip(this IQueryable source, int count)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }
            return source.Provider.CreateQuery(
                Expression.Call(
                    typeof(Queryable), "Skip",
                    new[] { source.ElementType },
                    source.Expression, Expression.Constant(count)));
        }

        public static bool Any(this IQueryable source)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }
            return (bool)source.Provider.Execute(
                Expression.Call(
                    typeof(Queryable), "Any",
                    new[] { source.ElementType }, source.Expression));
        }

        public static int Count(this IQueryable source)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }
            return (int)source.Provider.Execute(
                Expression.Call(
                    typeof(Queryable), "Count",
                    new[] { source.ElementType }, source.Expression));
        }
    }

    #endregion

    #region Xml Generation for Dynamics

    /// <summary>
    /// Extension methods for the dynamic object.
    /// </summary>
    public static class DynamicHelper
    {
        /// <summary>
        /// Defines the simple types that is directly writeable to XML.
        /// </summary>
        private static readonly Type[] _writeTypes = new[] { typeof(string), typeof(DateTime), typeof(Enum), typeof(decimal), typeof(Guid) };

        /// <summary>
        /// Determines whether [is simple type] [the specified type].
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <returns>
        /// 	<c>true</c> if [is simple type] [the specified type]; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsSimpleType(this Type type)
        {
            return type.IsPrimitive || _writeTypes.Contains(type);
        }

        /// <summary>
        /// Converts the specified dynamic object to XML.
        /// </summary>
        /// <param name="dynamicObject">The dynamic object.</param>
        /// <returns>Returns an Xml representation of the dynamic object.</returns>
        public static XElement ConvertToXml(dynamic dynamicObject)
        {
            return ConvertToXml(dynamicObject, null);
        }

        /// <summary>
        /// Converts the specified dynamic object to XML.
        /// </summary>
        /// <param name="dynamicObject">The dynamic object.</param>
        /// <param name="element">The element name.</param>
        /// <param name="namespaces">An optional dictionary of available namespaces</param>
        /// <param name="parent">An optional parent XElement to inherit a namespace from if none is directly applied</param>
        /// <returns>Returns an Xml representation of the dynamic object.</returns>
        public static XElement ConvertToXml(dynamic dynamicObject, string element, Dictionary<string, string> namespaces = null, XElement parent = null)
        {
            if (namespaces == null)
                namespaces = new Dictionary<string, string>();

            if (String.IsNullOrWhiteSpace(element))
            {
                element = "object";
            }

            element = XmlConvert.EncodeName(element);
            var ret = GetXElement(element, namespaces, parent);

            var members = new Dictionary<string, object>(dynamicObject);

            foreach (var prop in members)
            {
                var name = XmlConvert.EncodeName(prop.Key);
                //id should only be a attribute of the root ignore the id field
                if ((name ?? "").ToUpper() == "ID") continue;

                if (prop.Value != null)
                {
                    if (prop.Value.GetType().IsArray)
                    {
                        var key = XmlConvert.EncodeName(prop.Key);
                        var el = GetArrayElement(prop.Key, (Array)prop.Value, namespaces, ret);

                        ret.Add(el);
                    }
                    else
                    {
                        if (prop.Value.GetType().IsSimpleType())
                        {
                            if (!string.IsNullOrEmpty(Convert.ToString(prop.Value)))
                            {
                                var el = GetXElement(name, namespaces, ret, prop.Value);
                                ret.Add(el);

                            }
                        }
                        else
                        {
                            var el = prop.Value.ToXml(name, namespaces, ret);
                            ret.Add(el);
                        }
                    }

                }
            }

            return ret;
        }

        /// <summary>
        /// Generates an XML string from the dynamic object.
        /// </summary>
        /// <param name="dynamicObject">The dynamic object.</param>
        /// <returns>Returns an XML string.</returns>
        public static string ToXmlString(dynamic dynamicObject)
        {
            XElement xml = DynamicHelper.ConvertToXml(dynamicObject);

            return xml.ToString();
        }

        /// <summary>
        /// Converts an anonymous type to an XElement.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <returns>Returns the object as it's XML representation in an XElement.</returns>
        public static XElement ToXml(this object input)
        {
            return input.ToXml(null);
        }

        /// <summary>
        /// Converts an anonymous type to an XElement.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <param name="element">The element name.</param>
        /// <param name="namespaces">An optional dictionary of available namespaces</param>
        /// <param name="parent">An optional parent XElement to inherit a namespace from if none is directly applied</param>
        /// <returns>Returns the object as it's XML representation in an XElement.</returns>
        public static XElement ToXml(this object input, string element, Dictionary<string, string> namespaces = null, XElement parent = null)
        {
            if (input == null)
            {
                return null;
            }
            if (namespaces == null)
                namespaces = new Dictionary<string, string>();

            if (String.IsNullOrWhiteSpace(element))
            {
                element = "object";
            }

            element = XmlConvert.EncodeName(element);
            var ret = GetXElement(element, namespaces, parent);

            if (input != null)
            {
                var type = input.GetType();
                var props = type.GetProperties();

                var elements = from prop in props
                               let name = XmlConvert.EncodeName(prop.Name)
                               let val = prop.PropertyType.IsArray ? "array" : prop.GetValue(input, null)
                               let value = prop.PropertyType.IsArray ? GetArrayElement(prop, (Array)prop.GetValue(input, null), namespaces) : (prop.PropertyType.IsSimpleType() ? GetXElement(name, namespaces, ret, val) : val.ToXml(name, namespaces, ret))
                               where value != null
                               select value;

                ret.Add(elements);
            }

            return ret;
        }

        /// <summary>
        /// Gets the array element.
        /// </summary>
        /// <param name="info">The property info.</param>
        /// <param name="input">The input object.</param>
        /// <param name="namespaces">An optional dictionary of available namespaces</param>
        /// <param name="parent">An optional parent XElement to inherit a namespace from if none is directly applied</param>
        /// <returns>Returns an XElement with the array collection as child elements.</returns>
        private static XElement GetArrayElement(PropertyInfo info, Array input, Dictionary<string, string> namespaces = null, XElement parent = null)
        {
            return GetArrayElement(info.Name, input, namespaces, parent);
        }

        /// <summary>
        /// Gets the array element.
        /// </summary>
        /// <param name="propertyName">The property name.</param>
        /// <param name="input">The input object.</param>
        /// <param name="namespaces">An optional dictionary of available namespaces</param>
        /// <param name="parent">An optional parent XElement to inherit a namespace from if none is directly applied</param>
        /// <returns>Returns an XElement with the array collection as child elements.</returns>
        private static XElement GetArrayElement(string propertyName, Array input, Dictionary<string, string> namespaces = null, XElement parent = null)
        {
            if (namespaces == null)
                namespaces = new Dictionary<string, string>();

            var name = XmlConvert.EncodeName(propertyName);

            XElement rootElement = GetXElement(name, namespaces, parent);

            var arrayCount = input.GetLength(0);

            for (int i = 0; i < arrayCount; i++)
            {
                var val = input.GetValue(i);
                XElement childElement = val.GetType().IsSimpleType() ? GetXElement(name + "Child", namespaces, rootElement, val) : val.ToXml();

                rootElement.Add(childElement);
            }

            return rootElement;
        }

        /// <summary>
        /// Creates an XElement with the appropriate namespace
        /// </summary>
        /// <param name="name">The node name</param>
        /// <param name="namespaces">An optional dictionary of available namespaces</param>
        /// <param name="parent">An optional parent XElement to inherit a namespace from if none is directly applied</param>
        /// <param name="content">Content for the XElement</param>
        /// <returns></returns>
        public static XElement GetXElement(string name, Dictionary<string, string> namespaces = null, XElement parent = null, params object[] content)
        {
            if (namespaces == null)
                namespaces = new Dictionary<string, string>();

            if (namespaces.ContainsKey(name))
            {
                XNamespace ns = namespaces[name];
                var x = new XElement(ns + name, content);
                return x;
            }
            else
            {
                if (parent != null)
                {
                    XNamespace ns = parent.Name.NamespaceName;
                    return new XElement(ns + name, content);
                }
                else
                    return new XElement(name, content);
            }
        }
    }

    #endregion

    public static class Extensions
    {
        public static FieldTypeComplexLookupDefinition ParseComplexLookupDefinition(this FieldTypeLookup lookup)
        {
            FieldTypeComplexLookupDefinition definition = JsonConvert.DeserializeObject<FieldTypeComplexLookupDefinition>(lookup.Definition);

            if (definition.Fields != null)
            {
                foreach (var f in definition.Fields.Where(f => f.RelationIndex == null))
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
            var customContract = new CustomJSONContractResolver(customContractProperties);
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
                var assetIdx = (ft.RelationIndex ?? assetTypes.IndexOf(ft.AssetTypeUid)) + 1;
                var fname = string.IsNullOrEmpty(ft.OverrideDisplayName) ? ft.FieldTypeName : ft.OverrideDisplayName;

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
                var assetIdx = (ft.RelationIndex ?? assetTypes.IndexOf(ft.AssetTypeUid)) + 1;
                var fname = string.IsNullOrEmpty(ft.OverrideDisplayName) ? ft.FieldTypeName : ft.OverrideDisplayName;

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
            foreach (var item in Values)
            {
                List<dynamic> Assets = new List<dynamic>();

                //Deflate asset fields
                for (int i = 0; i < definition.Relations.Count; i++)
                {
                    var relFields = definition.Fields
                        .Where(x => x.FieldTypeName.StartsWith("Related Item."))
                        .Count();

                    var dict = new Dictionary<string, object>();
                    dict.Add("RelationshipTypeUid", definition.Relations[i].IntersectTypeUid);
                    dict.Add("AssetTypeUid", definition.Relations[i].AssetTypeUid);
                    dict.Add("RelationType", definition.Relations[i].RelationType);

                    dynamic[] relatedItems = new dynamic[relFields];

                    bool hasRelation = false;
                    var relationFields = new Dictionary<string, object>();


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

                            var relItemMatch = "Relation.";
                            if (propName.StartsWith(relItemMatch))
                            {
                                propName = propName.Replace(relItemMatch, "");
                                relationFields.Add(propName, prop.Value);
                                isAdded = true;
                                hasRelation = true;
                            }

                            if (!isAdded)
                                dict.Add(propName, prop.Value);
                        }
                    }

                    if (relFields > 0)
                        dict.Add("RelatedItems", relatedItems.Where(x => x != null));

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
