using System;
using System.Collections.Generic;
using System.Linq;
using Dapper;
using System.Data.SqlClient;
using d360.core.enums;
using Nest;
using System.Web.UI;
using d360.core.queue;
using d360.core;
using System.Runtime.Remoting.Messaging;
using System.Collections.Concurrent;
using MoreLinq;

namespace d360.extensions.search
{
    public class SearchIndexer
    {
        private static int _defaultQueryCommandTimeout = 180;
        private SqlConnection _context;
        private int _companyID;
        private ElasticSearchSource _source;

        public SearchIndexer(SqlConnection context, int companyID, ElasticSearchSource source)
        {
            _context = context;
            _companyID = companyID;
            _source = source;
        }

        private static readonly List<string> allowedClassesAndObjectTypes = new List<string> {
                SystemObjects.Artifact.ToString(),
                SystemObjects.Resource.ToString(),
                SystemObjects.Taxonomy.ToString(),
                AssetTypeClass.BusinessAsset.ToString(),
                AssetTypeClass.TechnicalAsset.ToString(),
                AssetTypeClass.Model.ToString(),
                AssetTypeClass.Policy.ToString(),
                AssetTypeClass.Rule.ToString(),
                AssetTypeClass.ReferenceItemType.ToString(),
                AssetTypeClass.User.ToString(),
                AssetTypeClass.Group.ToString(),
                AssetTypeClass.Fusion.ToString(),
                AssetTypeClass.FusionAttribute.ToString(),
                "Reference",
                SystemObjects.FusionType.ToString(),
                "FusionAttributes",
                "Intersect",
                "Synonym",
            };

        public static bool IsIndexable(string classOrObjectType)
        {
            return allowedClassesAndObjectTypes.Contains(classOrObjectType);
        }

        public static string GetCategoryFromObject(string obj)
        {
            if(obj == SystemObjects.Taxonomy.ToString())
            {
                return AssetTypeClass.Model.ToString();
            } else if (obj == SystemObjects.Resource.ToString())
            {
                return AssetTypeClass.User.ToString();
            } else if(obj == AssetTypeClass.ReferenceItemType.ToString())
            {
                return "Reference";
            }
            return obj;
        }

        public void IndexAsset(Guid AssetUid)
        {
            List<IndexObjectModel> models = LoadModels(_context, _companyID, _source, null, null, AssetUid).ToList();
            if (models.Any())
            {
                _source.RemoveFromIndex(models);
                _source.AddToIndex(models);
            }
        }

        public void IndexAssets(ConcurrentBag<Guid> AssetGuids)
        {
            List<IndexObjectModel> models = AssetGuids.SelectMany(g => LoadModels(_context, _companyID, _source, null, null, g)).ToList();
            if (models.Any())
            {
                _source.RemoveFromIndex(models);
                _source.AddToIndex(models);
            }
        }

        public void IndexAsset(string Object, long ObjectID)
        {
            List<IndexObjectModel> models = LoadModels(_context, _companyID, _source, Object, ObjectID).ToList();
            if (models.Any())
            {
                _source.RemoveFromIndex(models);
                _source.AddToIndex(models);
            }
        }

        public void IndexAssets(ConcurrentBag<Tuple<string, long>> tuples)
        {
            List<IndexObjectModel> models = tuples.SelectMany(t => LoadModels(_context, _companyID, _source, t.Item1, t.Item2)).ToList();
            if (models.Any())
            {
                _source.RemoveFromIndex(models);
                _source.AddToIndex(models);
            }
        }

        public void IndexAssetType(Guid AssetTypeUid)
        {
            AssetClassAndName assettype = _context.QueryFirstOrDefault<AssetClassAndName>("SELECT [Class], [Name] FROM AssetType att WHERE att.uid = @AssetTypeUid", new { AssetTypeUid });
            _source.ClearIndex(_companyID, assettype.Class.ToString(), assettype.Name);
            IEnumerable<IndexObjectModel> models = LoadModels(_context, _companyID, _source, assettype.Class, AssetTypeUid, null);
            _source.AddToIndex(models);
        }

        public void IndexAssetClass(AssetTypeClass assetclass)
        {
            _source.ClearIndex(_companyID, assetclass.ToString());
            IEnumerable<IndexObjectModel> models = LoadModels(_context, _companyID, _source, assetclass, null, null);
            _source.AddToIndex(models);
        }
        public void IndexObjectType(string ObjectType, bool clearIndex = true)
        {
            if(clearIndex)
                _source.ClearIndex(_companyID, ObjectType);

            IEnumerable<IndexObjectModel> models = LoadModels(_context, _companyID, _source, ObjectType, null);
            _source.AddToIndex(models);
        }

        private IEnumerable<IndexObjectModel> LoadModels(SqlConnection context, int companyID, ElasticSearchSource source, AssetTypeClass? assetClass, Guid? AssetTypeUid, Guid? AssetUid)
        {
            bool loadFields = false;
            string sql = "";
            List<string> where = new List<string>();
            Func<dynamic, IndexObjectModel> shaper = null;
            DynamicParameters parameters = new DynamicParameters();

            if(assetClass == null && AssetUid.HasValue)
            {
                assetClass = _context.QueryFirstOrDefault<AssetTypeClass>("SELECT [Class] FROM AssetType att INNER JOIN Asset a on att.ID = a.AssetTypeID WHERE a.uid = @AssetUid", new { AssetUid });
            } else if(assetClass == null && AssetTypeUid.HasValue)
            {
                assetClass = _context.QueryFirstOrDefault<AssetTypeClass>("SELECT [Class] FROM AssetType att WHERE att.uid = @AssetTypeUid", new { AssetTypeUid });
            }
            if (assetClass == null)
                throw new Exception("AssetClass is null");

            int assettypeclass = (int)assetClass;
            switch (assetClass)
            {
                case AssetTypeClass.BusinessAsset:
                case AssetTypeClass.TechnicalAsset:
                case AssetTypeClass.Model:
                case AssetTypeClass.Policy:
                case AssetTypeClass.Rule:
                    where.Add("a.[state] =  " + ((int)State.Active).ToString());
                    where.Add("att.[Class] = @assettypeclass");
                    parameters.Add("@assettypeclass", assettypeclass);
                    if (AssetTypeUid.HasValue && AssetTypeUid != Guid.Empty)
                    {
                        where.Add("att.uid = @assettypeuid");
                        parameters.Add("@assettypeuid", AssetTypeUid);
                    }
                    if (AssetUid.HasValue && AssetUid != Guid.Empty)
                    {
                        where.Add("a.uid = @assetuid");
                        parameters.Add("@assetuid", AssetUid);
                    }
                    string whereCondition = string.Join(" and ", where.ToArray());

                    sql = $@"SELECT
                            A.ID as AssetID,
	                        cast(A.ID as varchar) as ItemUniqueID,
	                        A.ObjectID as ID,
	                        att.ObjectID as TypeID,
	                        adv.DisplayValue,
	                        att.Name as TypeName,
                            att.uid as AssetTypeUid,
	                        a.uid as Uid,
                            dbo.GenerateAssetUrl(a.ID) as 'Url'
                        from
	                        [dbo].Asset a
	                        inner join [dbo].assettype att on a.assettypeid = att.id
	                        inner join [dbo].assetdisplayvalue adv on adv.assetid = a.id
                        where
	                          {whereCondition}
                        ORDER BY A.ID";
                    loadFields = true;
                    shaper = (dynamic o) =>
                    {
                        return new IndexObjectModel
                        {
                            Category = assetClass.ToString(),
                            CompanyID = companyID,
                            ID = o.ID,
                            AssetID = o.AssetID,
                            ItemUniqueID = o.ItemUniqueID,
                            AssetType = o.TypeName,
                            RelativeUrl = o.Url,
                            Uid = o.Uid,
                            AssetTypeUid = o.AssetTypeUid,
                            Fields = new Dictionary<string, string>() {
                                { "Name", o.DisplayValue }
                            }
                        };
                    };
                    break;
                case AssetTypeClass.ReferenceItemType:
                    where.Add("att.[state] =  " + ((int)State.Active).ToString());
                    where.Add("att.[Class] = @assettypeclass");
                    parameters.Add("@assettypeclass", AssetTypeClass.Reference);
                    if (AssetTypeUid.HasValue && AssetTypeUid != Guid.Empty)
                    {
                        where.Add("att.uid = @assettypeuid");
                        parameters.Add("@assettypeuid", AssetTypeUid);
                    }
                    whereCondition = string.Join(" and ", where.ToArray());
                    sql = $@"SELECT
                        ObjectID as ID,
                        Object,
                        Name,
                        Description,
                        uid as AssetTypeUid
                    FROM [dbo].[AssetType] att
                    WHERE {whereCondition}";
                    shaper = (dynamic o) =>
                    {
                        return new IndexObjectModel
                        {
                            Category = GetCategoryFromObject(o.Object),
                            CompanyID = companyID,
                            ID = o.ID,
                            AssetType = "Reference List",
                            RelativeUrl = $"reference/{o.ID}",
                            AssetTypeUid = o.AssetTypeUid,
                            Fields = new Dictionary<string, string>() {
                                { "Name", o.Name },
                                { "Description", o.Description }
                            }
                        };
                    };
                    break;
                case AssetTypeClass.User:
                    where.Add("u.[state] = " + ((int)CompanyResourceState.Active).ToString());
                    where.Add("a.[state] = " + ((int)State.Active).ToString());
                    where.Add("att.[Class] = @assettypeclass");
                    parameters.Add("@assettypeclass", assettypeclass);
                    if (AssetTypeUid.HasValue && AssetTypeUid != Guid.Empty)
                    {
                        where.Add("att.uid = @assettypeuid");
                        parameters.Add("@assettypeuid", AssetTypeUid);
                    }
                    if (AssetUid.HasValue && AssetUid != Guid.Empty)
                    {
                        where.Add("a.uid = @assetuid");
                        parameters.Add("@assetuid", AssetUid);
                    }
                    whereCondition = string.Join(" and ", where.ToArray());
                    sql = $@"SELECT
                            A.ID as AssetID,
	                        cast(A.ID as varchar) as ItemUniqueID,
	                        A.ObjectID as ID,
	                        att.ObjectID as TypeID,
	                        ISNULL(adv.DisplayValue, [utility].GetAssetDisplayValue(A.ID)) DisplayValue,
	                        att.Name as TypeName,
                            att.uid as AssetTypeUid,
	                        a.uid as Uid,
                            dbo.GenerateAssetUrl(a.ID) as 'Url', a.[Object],
	                        u.email as Email,
	                        CASE
                            WHEN u.Email not like '%@data3sixty.com' and Email not like '%@infogix.com'
                                THEN '0'
                                ELSE '1'
                            END as Data3SixtyUser
                        from
	                        [dbo].Asset a
	                        inner join reporting.global_resource u on u.ResourceID = a.ObjectID and a.[Object] = 'Resource'
	                        inner join [dbo].assettype att on a.assettypeid = att.id
	                        left outer join [dbo].assetdisplayvalue adv on adv.assetid = a.id
                        where
	                         {whereCondition}
                        ORDER BY A.ID";
                    shaper = (dynamic o) =>
                    {
                        return new IndexObjectModel
                        {
                            Category = assetClass.ToString(),
                            CompanyID = companyID,
                            ID = o.ID,
                            AssetID = o.AssetID,
                            ItemUniqueID = o.ItemUniqueID,
                            AssetType = o.TypeName,
                            RelativeUrl = o.Url,
                            Uid = o.Uid,
                            AssetTypeUid = o.AssetTypeUid,
                            Fields = new Dictionary<string, string>() {
                                { "Name", o.DisplayValue },
                                { "Email", o.Email },
                                { "Username", o.Email },
                                { "Data3SixtyUser", o.Data3SixtyUser }
                            }
                        };
                    };
                    break;
                case AssetTypeClass.Group:
                    where.Add("a.[state] =  " + ((int)State.Active).ToString());
                    where.Add("att.[Class] = @assettypeclass");
                    parameters.Add("assettypeclass", assettypeclass);
                    if (AssetTypeUid.HasValue && AssetTypeUid != Guid.Empty)
                    {
                        where.Add("att.uid = @assettypeuid");
                        parameters.Add("assettypeuid", AssetTypeUid);
                    }
                    if (AssetUid.HasValue && AssetUid != Guid.Empty)
                    {
                        where.Add("a.uid = @assetuid");
                        parameters.Add("assetuid", AssetUid);
                    }
                    whereCondition = string.Join(" and ", where.ToArray());
                    sql = $@"SELECT
                            A.ID as AssetID,
	                        cast(A.ID as varchar) as ItemUniqueID,
	                        A.ObjectID as ID,
	                        att.ObjectID as TypeID,
	                        adv.DisplayValue,
                            g.[Description],
	                        att.Name as TypeName,
                            att.uid as AssetTypeUid,
	                        a.uid as Uid,
                            dbo.GenerateAssetUrl(a.ID) as 'Url'
                        from
	                        [dbo].Asset a
                            inner join [Group] g on g.ID = a.ObjectID and [Object] = 'Group'
	                        inner join [dbo].assettype att on a.assettypeid = att.id
	                        inner join [dbo].assetdisplayvalue adv on adv.assetid = a.id
                        where
	                        {whereCondition}
                        ORDER BY A.ID";
                    shaper = (dynamic o) =>
                    {
                        return new IndexObjectModel
                        {
                            Category = assetClass.ToString(),
                            CompanyID = companyID,
                            ID = o.ID,
                            AssetID = o.AssetID,
                            ItemUniqueID = o.ItemUniqueID,
                            AssetType = o.TypeName,
                            RelativeUrl = o.Url,
                            Uid = o.Uid,
                            AssetTypeUid = o.AssetTypeUid,
                            Fields = new Dictionary<string, string>() {
                                { "Name", o.DisplayValue },
                                { "Description", o.Description }
                            }
                        };
                    };
                    break;
                case AssetTypeClass.Fusion:
                    sql = @"select
                        f.id as ID,
	                    f.Name as FusionName,
	                    f.Description as FusionDescription,
	                    ft.Name as FusionTypeName,
	                    ft.Description as FusionTypeDescription,
                        ft.ID as FusionTypeID
                    from fusion f
                        inner join fusiontype ft on f.fusiontypeid = ft.id";
                    shaper = (dynamic o) =>
                    {
                        return new IndexObjectModel
                        {
                            Category = SystemObjects.FusionType.ToString(),
                            CompanyID = companyID,
                            ID = o.ID,
                            AssetType = o.FusionTypeName,
                            RelativeUrl = $"fusion/{o.ID}",
                            Fields = new Dictionary<string, string>() {
                                { "Name", o.FusionName },
                                { "Description", o.FusionDescription }
                            }
                        };
                    };
                    break;
                case AssetTypeClass.FusionAttribute:
                    sql = @"select
	                        f.ID,
	                        f.Name,
	                        f.FusionAttributeTypeID,
	                        ft.Name as FusionAttributeTypeName,
	                        fu.Name as FusionName,
							a.id as AssetID,
                            dbo.GenerateAssetUrl(a.id) as 'Url'
                        from fusionattribute f
	                        inner join fusionattributetype ft on (f.fusionattributetypeid = ft.id)
	                        inner join fusion fu on (f.fusionid = fu.id)
                            inner join asset a on a.object = 'FusionAttribute' and f.id = a.objectid
                        where f.Deleted = 0";
                    shaper = (dynamic o) =>
                    {
                        return new IndexObjectModel
                        {
                            Category = "FusionAttributes",
                            CompanyID = companyID,
                            ID = o.ID,
                            AssetID = o.AssetID,
                            AssetType = $"{o.FusionName} {o.FusionAttributeTypeName}",
                            RelativeUrl = o.Url,
                            Fields = new Dictionary<string, string>() {
                                { "Name", o.Name }
                            }
                        };
                    };
                    break;
                default:
                    break;
            }

            return getData(context, sql, companyID, source, parameters, loadFields, shaper);
        }

        private IEnumerable<IndexObjectModel> LoadModels(SqlConnection context, int companyID, ElasticSearchSource source, string Object, long? ObjectID = null)
        {
            string sql = "";
            string where = "";
            Func<dynamic, IndexObjectModel> shaper = null;
            DynamicParameters parameters = new DynamicParameters();

            switch (Object)
            {
                case "Intersect":
                    if(ObjectID != null)
                    {
                        where = "WHERE I.ID = @ObjectID";
                        parameters.Add("ObjectID", ObjectID);
                    }
                    sql = $@"select 
                I.ID,
                'S' as 'Direction',
                SubjectAdv.DisplayValue as 'Synonym',
                I.Subject as 'SynonymObjectType',
                I.SubjectID as  'SynonymObjectID',
                SubjectAsset.ID as 'SynonymAssetID', 
                ObjectAdv.DisplayValue as 'SynonymFor', 
                I.Object as 'SynonymForObject', 
                I.ObjectID as 'SynonymForObjectID',
                dbo.GenerateAssetUrl(ObjectAsset.ID) as 'Url', 
                ArtType.Name as 'SynonymForObjectType',
                P.Name as 'PredicateName' 
            from [intersect] I 
                inner join IntersectType T on T.ID = I.IntersectTypeID
                inner join Predicate P on P.ID = T.PredicateID and P.Type = 6
                inner join Asset SubjectAsset on SubjectAsset.[Object] = I.Subject and SubjectAsset.ObjectID = I.SubjectID 
                inner join [dbo].AssetDisplayValue SubjectAdv on SubjectAdv.AssetID = SubjectAsset.ID 
                inner join Asset ObjectAsset on ObjectAsset.[Object] = I.Object and ObjectAsset.ObjectID = I.ObjectID 
                inner join [dbo].AssetDisplayValue ObjectAdv on ObjectAdv.AssetID = ObjectAsset.ID 
                inner join AssetType ArtType on ObjectAsset.AssetTypeID = ArtType.ID
                {where}
            Union 
            SELECT
                I.ID, 
                'O' as 'Direction', 
                SubjectAdv.DisplayValue as 'Synonym', 
                I.Object as 'SynonymObjectType', 
                I.ObjectID as  'SynonymObjectID', 
                SubjectAsset.ID as 'SynonymAssetID', 
                ObjectAdv.DisplayValue as 'SynonymFor', 
                I.Subject as 'SynonymForObject', 
                I.SubjectID as 'SynonymForObjectID', 
                dbo.GenerateAssetUrl(ObjectAsset.ID) as 'Url', 
                ArtType.Name as 'SynonymForObjectType', 
                P.Name as 'PredicateName'
            from [intersect] I
                inner join IntersectType T on T.ID = I.IntersectTypeID 
                inner join Predicate P on P.ID = T.PredicateID and P.Type = 6 
                inner join Asset SubjectAsset on SubjectAsset.[Object] = I.Object and SubjectAsset.ObjectID = I.ObjectID 
                inner join [dbo].AssetDisplayValue SubjectAdv on SubjectAdv.AssetID = SubjectAsset.ID
                inner join Asset ObjectAsset on ObjectAsset.[Object] = I.Subject and ObjectAsset.ObjectID = I.SubjectID 
                inner join [dbo].AssetDisplayValue ObjectAdv on ObjectAdv.AssetID = ObjectAsset.ID 
                inner join AssetType ArtType on ObjectAsset.AssetTypeID = ArtType.ID
                {where}
            order by SynonymFor";
                    shaper = (dynamic o) =>
                    {
                        return new IndexObjectModel
                        {
                            Category = "Synonym",
                            CompanyID = companyID,
                            AssetType = "Synonym",
                            ItemUniqueID = $"intersect|{o.ID}|{o.Direction}",
                            RelativeUrl = o.Url,
                            Fields = new Dictionary<string, string>() {
                                { "Name", o.Synonym },
                                { "NymType", o.PredicateName },
                                { "SynonymFor", o.SynonymFor },
                                { "SynonymForObject", o.SynonymForObject },
                                { "SynonymForObjectType", o.SynonymForObjectType }
                            }
                        };
                    };
                    break;
                case "Synonym":
                    if (ObjectID != null)
                    {
                        where = "where s.ID = @ObjectID";
                        parameters.Add("ObjectID", ObjectID);
                    }
                    sql = $@"
                        select 
	                        s.Name as 'Synonym'
	                        ,d.DisplayValue as 'SynonymFor'
	                        ,s.[Object] as 'SynonymForObject'
	                        ,s.[ObjectID] as 'SynonymForObjectID'
	                        ,dbo.GenerateAssetUrl(a.ID) as 'Url'
	                        ,t.Name as 'SynonymForObjectType'	
                            ,p.Name as 'PredicateName'    
                            ,s.ID as 'ID'                
                        from
	                        [dbo].[nym] s
                            inner join [dbo].Asset a on a.object = s.object and a.objectid = s.objectid
	                        inner join [dbo].AssetType t on a.assettypeid = t.id
	                        inner join [dbo].AssetDisplayValue d on d.assetid = a.id
                            inner join [dbo].[predicate] p on (s.predicateid = p.id)
                        {where}";
                    shaper = (dynamic o) =>
                    {
                        return new IndexObjectModel
                        {
                            Category = "Synonym",
                            CompanyID = companyID,
                            AssetType = "Synonym",
                            ItemUniqueID = $"custom|{o.ID}",
                            RelativeUrl = o.Url,
                            Fields = new Dictionary<string, string>() {
                                { "Name", o.Synonym },
                                { "NymType", o.PredicateName },
                                { "SynonymFor", o.SynonymFor },
                                { "SynonymForObject", o.SynonymForObject },
                                { "SynonymForObjectType", o.SynonymForObjectType }
                            }
                        };
                    };
                    break;
                case "ReferenceItemType":
                    if (ObjectID != null)
                    {
                        where = " and att.ObjectID = @ObjectID";
                        parameters.Add("ObjectID", ObjectID);
                    }
                    sql = $@"SELECT
                        ObjectID as ID,
                        Object,
                        Name,
                        Description,
                        uid as AssetTypeUid
                    FROM [dbo].[AssetType] att
                    WHERE [Object] = 'ReferenceItemType'
                    {where}";
                    shaper = (dynamic o) =>
                    {
                        return new IndexObjectModel
                        {
                            Category = GetCategoryFromObject(o.Object),
                            CompanyID = companyID,
                            ID = o.ID,
                            AssetType = "Reference List",
                            RelativeUrl = $"reference/{o.ID}",
                            AssetTypeUid = o.AssetTypeUid,
                            Fields = new Dictionary<string, string>() {
                                { "Name", o.Name },
                                { "Description", o.Description }
                            }
                        };
                    };
                    break;
            }
            return getData(context, sql, companyID, source, parameters, false, shaper);
        }

        private static IEnumerable<IndexObjectModel> getData(SqlConnection context, string sql, int companyID, ElasticSearchSource source, DynamicParameters parameters, bool loadFields, Func<dynamic, IndexObjectModel> convertToDictionary)
        {
            if (loadFields)
            {
                return getDataWithFields(context, sql, companyID, source, parameters, convertToDictionary);
            }

            return getDataWithoutFields(context, sql, companyID, source, parameters, convertToDictionary);
        }

        private static IEnumerable<IndexObjectModel> getDataWithoutFields(SqlConnection context, string sql, int companyID, ElasticSearchSource source, DynamicParameters parameters, Func<dynamic, IndexObjectModel> convertToDictionary)
        {
            return context.Query(sql, parameters, commandTimeout: _defaultQueryCommandTimeout, buffered: false).ToList().Select(a => (IndexObjectModel)convertToDictionary(a));
        }

        private static IEnumerable<IndexObjectModel> getDataWithFields(SqlConnection context, string sql, int companyID, ElasticSearchSource source, DynamicParameters parameters, Func<dynamic, IndexObjectModel> convertToDictionary)
        {
            var FieldQuery = new PagedQuery<FieldSqlModel>(context, GetFieldQuery(parameters), parameters);
            var TagsQuery = new PagedQuery<TagSqlModel>(context, GetTagQuery(parameters), parameters);
            var ResponsibilityQuery = new PagedQuery<ResponsibilitySqlModel>(context, GetResponsibilityQuery(parameters), parameters);
            var list = getDataWithoutFields(context, sql, companyID, source, parameters, convertToDictionary);

            foreach (var item in list)
            {
                var subset = FieldQuery.GetByAssetID(item.AssetID);
                foreach (var f in subset)
                {
                    item.Fields[f.Name] = f.FormattedValue;
                }
                if (item.Uid.HasValue && item.Uid != Guid.Empty)
                {
                    item.Tags = TagsQuery.GetByAssetID(item.AssetID).ToDictionary(x => x.TagUID.ToString(), x => x.Value);
                }
                var secset = ResponsibilityQuery.GetByAssetID(item.AssetID);
                item.NoRead = new Dictionary<string, List<int>> {
                    { "R" , secset.Where(r => r.SecurityAsset == "R").Select(r => r.SecurityAssetID).ToList() },
                    { "G" , secset.Where(r => r.SecurityAsset == "G").Select(r => r.SecurityAssetID).ToList() },
                    { "O" , secset.Where(r => r.SecurityAsset == "O").Select(r => r.SecurityAssetID).ToList() }
                };
                yield return item;
            }
        }

        private static string GetFieldQuery(DynamicParameters parameters)
        {
            List<string> fieldWhere = new List<string>();
            List<string> fieldJoin = new List<string>();

            fieldJoin.Add("inner join FieldType FT on FT.ID = F.FieldTypeID");
            fieldJoin.Add("inner join AssetType ATT on ATT.ID = FT.AssetTypeID");

            if (parameters.ParameterNames.Contains("assettypeclass"))
            {
                fieldWhere.Add("ATT.class = @assettypeclass");
            }
            if (parameters.ParameterNames.Contains("assettypeuid"))
            {
                fieldWhere.Add("att.uid = @assettypeuid");
            }
            if (parameters.ParameterNames.Contains("assetuid"))
            {
                fieldJoin.Add("inner join Asset a on a.ID = F.AssetID");
                fieldWhere.Add("a.uid = @assetuid");
            }

            string fieldsSql = @"select F.AssetID, FT.Name, F.FormattedValue from Field F " +
                string.Join(" " + Environment.NewLine, fieldJoin.ToArray()) +
            " where F.FormattedValue is not null and F.FormattedValue <> '' and " +
            " FT.[Type] not in('DateTime','Color','FilteredLookup','ComplexRelationLookup','OwnershipLookup','Relationship','FieldFromRelationship','RefListRelationship','JSON')";
            if (fieldWhere.Any())
            {
                fieldsSql += " and " + string.Join(Environment.NewLine + " and ", fieldWhere.ToArray());
            }
            return fieldsSql;
        }

        private static string GetTagQuery(DynamicParameters parameters)
        {
            List<string> tagWhere = new List<string>();
            List<string> tagJoin = new List<string>();

            if (parameters.ParameterNames.Contains("assettypeuid"))
            {
                tagWhere.Add("att.uid = @assettypeuid");
                tagJoin.Add("INNER JOIN [dbo].[AssetType] att ON att.ID = a.AssetTypeID");
            }
            if (parameters.ParameterNames.Contains("assetuid"))
            {
                tagWhere.Add("a.uid = @assetuid");
            }

            string tagsSql = @"SELECT a.ID as AssetID, a.uid AS AssetUID, t.uid AS TagUID, t.Value FROM [dbo].[AssetTag] at " +
            "INNER JOIN [dbo].[Tag] t ON at.TagID = t.ID INNER JOIN [dbo].[Asset] a ON at.AssetID = a.ID";
            if (tagJoin.Any())
                tagsSql += " " + string.Join(" " + Environment.NewLine, tagJoin.ToArray());
            if (tagWhere.Any())
                tagsSql += " WHERE " + string.Join(" AND " + Environment.NewLine, tagWhere.ToArray());

            return tagsSql;
        }

        private static string GetResponsibilityQuery(DynamicParameters parameters)
        {
            string sql = $@"SELECT aa.id as AssetID, 
                              rresource.SecurityAsset,
                              rresource.SecurityAssetID
                        FROM ResponsibilityTypeRelationRule r
                        INNER JOIN ResponsibilityTypeRelation rrel ON (r.ResponsibilityTypeID = rrel.ResponsibilityTypeID
                                                                      AND r.[Object] = rrel.[ObjectType]
                                                                      AND r.ObjectID = rrel.ObjectID)
                        INNER JOIN [ResponsibilityRuleResultAsset] rasset ON (r.ID = rasset.RuleID)
                        INNER JOIN [ResponsibilityRuleResultSecurityAsset] rresource ON (r.ID = rresource.RuleID)
                        INNER JOIN Asset aa ON AA.AssetTypeID = rasset.AssetTypeID
                        WHERE rrel.PermissionsBitMask & {(int)Permission.ReadAsset} = 0
                         AND rasset.AssetID = 0
                        UNION ALL
                        SELECT rasset.AssetID,
                              rresource.SecurityAsset,
                              rresource.SecurityAssetID
                        FROM ResponsibilityTypeRelationRule rtrr
                        INNER JOIN ResponsibilityTypeRelation rrel ON (rtrr.ResponsibilityTypeID = rrel.ResponsibilityTypeID
                                                                      AND rtrr.[Object] = rrel.[ObjectType]
                                                                      AND rtrr.ObjectID = rrel.ObjectID)
                        INNER JOIN [ResponsibilityRuleResultAsset] rasset ON (rtrr.ID = rasset.RuleID)
                        INNER JOIN [ResponsibilityRuleResultSecurityAsset] rresource ON (rtrr.ID = rresource.RuleID)
                        WHERE rrel.PermissionsBitMask & {(int)Permission.ReadAsset} = 0
                         AND rasset.AssetTypeID = 0";
            if (parameters.ParameterNames.Contains("assetuid"))
            {
                return "SELECT q.* FROM (" + sql + ") q INNER JOIN [dbo].Asset a ON q.AssetID = a.ID WHERE a.uid = @assetuid";
            }
            else if (parameters.ParameterNames.Contains("assettypeuid"))
            {
                return "SELECT q.* FROM (" + sql + ") q INNER JOIN [dbo].Asset a ON q.AssetID = a.ID INNER JOIN [dbo].AssetType att on a.AssetTypeID = att.id WHERE att.uid = @assettypeuid";
            }
            return sql;
        }
    }


    internal interface IPagedQuerySqlModel
    {
        long AssetID { get; set; }
    }

    internal class AssetClassAndName
    {
        public AssetTypeClass Class { get; set; }
        public string Name { get; set; }
    }

    internal class FieldSqlModel : IPagedQuerySqlModel
    {
        public long AssetID { get; set; }
        public string Name { get; set; }
        public string FormattedValue { get; set; }
    }

    internal class TagSqlModel : IPagedQuerySqlModel
    {
        public long AssetID { get; set; }
        public Guid AssetUID { get; set; }
        public Guid TagUID { get; set; }
        public string Value { get; set; }
    }

    internal class ResponsibilitySqlModel : IPagedQuerySqlModel
    {
        public long AssetID { get; set; }
        public string SecurityAsset { get; set; }
        public int SecurityAssetID { get; set; }
    }

    internal interface IPagedQuery<T>
    {
        List<T> GetByAssetID(long AssetID);
    }
    internal class PagedQuery<T> : IPagedQuery<T> where T : IPagedQuerySqlModel
    {
        private static readonly int PageSize = 50000;
        private long CurrentHighID = 0;
        private List<T> _data;
        private SqlConnection _connection;
        private readonly string _query;
        public DynamicParameters _param;
        private bool LastPage = false;
        private static readonly int _defaultQueryCommandTimeout = 180;

        /// <summary>
        /// Performs a paged/chunked query
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="query">Query string</param>
        /// <param name="param"></param>
        public PagedQuery(SqlConnection connection, string query, DynamicParameters param = null)
        {
            _connection = connection;

            //Use <T> to specify columns to select, as SqlMapper can slow down a lot over *
            string alias = "pagedquery";
            string queryColumns = string.Join(", ", typeof(T).GetProperties().Select(p => $"{alias}.{p.Name}").ToArray());
            _query = $"SELECT TOP (@PageSize) {queryColumns} FROM ({query}) {alias} WHERE {alias}.AssetID >= @PagerAssetID ORDER BY {alias}.AssetID"; ;
            _param = new DynamicParameters();
            if (param != null)
            {
                foreach (var paramName in param.ParameterNames)
                {
                    _param.Add(paramName, ((SqlMapper.IParameterLookup)param)[paramName]);
                }
            }
            _data = new List<T>();
        }

        /// <summary>
        /// Fetches the next "page" of data. Starting with the requested AssetID
        /// No need to get any records with a lower AssetID's
        /// </summary>
        /// <param name="AssetID"></param>
        private void FetchDataPage(long AssetID)
        {
            if (LastPage)
                return;

            _param.Add("PagerAssetID", AssetID);
            _param.Add("PageSize", PageSize);
            _data = _connection.Query<T>(_query, _param, commandTimeout: _defaultQueryCommandTimeout).ToList();
            if (_data.Count() < PageSize)
            {
                //If we fetched less than PageSize, this is the last page of data
                LastPage = true;
            }
            else
            {
                long MinAssetID = _data.Min(i => i.AssetID);
                long MaxAssetID = _data.Max(i => i.AssetID);
                if (MinAssetID == MaxAssetID)
                {
                    //If min and max AssetID is the same, the whole "page" is the same asset and it can't be guaranteed that all records for one asset has been fetched
                    throw new Exception("Search of " + typeof(T) + " got more than " + PageSize + " results for one AssetID");
                }
                else
                {
                    //The page may have an incomplete set of records for the highest Asset ID, so remove those from the data stored.
                    _data.RemoveAll(i => i.AssetID == MaxAssetID);
                    CurrentHighID = _data.Max(i => i.AssetID);
                }
            }
        }

        /// <summary>
        /// Fetches records from the query for the provided Asset ID
        /// </summary>
        /// <param name="AssetID"></param>
        /// <returns></returns>
        public List<T> GetByAssetID(long AssetID)
        {
            //If requested ID is higher than what is current, and last page has not been reached, fetch the next data page
            if (!LastPage && AssetID > CurrentHighID)
                FetchDataPage(AssetID);

            return _data.Where(i => i.AssetID == AssetID).ToList();
        }
    }
}
