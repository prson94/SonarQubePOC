using System;
using System.Collections.Generic;
using System.Linq;
using Dapper;
using System.Data.SqlClient;
using d360.core.enums;
using d360.core.queue;
using d360.core;
using System.Collections.Concurrent;
using MoreLinq;
using System.Data;
using d360.extensions.queue;

namespace d360.extensions.search
{
    public class SearchIndexer
    {
        private static int _defaultQueryCommandTimeout = 180;
        private static int _indexClassAsTypesLimit = 400000;
        private SqlConnection _context;
        private int _companyID;
        private readonly ISearchSource _source;
        private readonly List<string> _messages;

        public SearchIndexer(SqlConnection context, int companyID, ISearchSource source)
        {
            _context = context;
            _companyID = companyID;
            _source = source;
            _messages = new List<string>();
        }

        private static readonly List<string> allowedClassesAndObjectTypes = new List<string> {
                SystemObjects.Artifact.ToString(),
                SystemObjects.Resource.ToString(),
                SystemObjects.Taxonomy.ToString(),
                SystemObjects.Task.ToString(),
                AssetTypeClass.BusinessAsset.ToString(),
                AssetTypeClass.TechnicalAsset.ToString(),
                AssetTypeClass.Diagram.ToString(),
                AssetTypeClass.Model.ToString(),
                AssetTypeClass.Policy.ToString(),
                AssetTypeClass.Rule.ToString(),
                AssetTypeClass.ReferenceItemType.ToString(),
                AssetTypeClass.User.ToString(),
                AssetTypeClass.Group.ToString(),
                "Reference",
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

        public static string GetCategoryFromClass(int typeClass)
        {
            return GetCategoryFromClass((AssetTypeClass)typeClass);
        }

        public static string GetCategoryFromClass(AssetTypeClass? typeClass)
        {
            if(typeClass == null || !Enum.IsDefined(typeof(AssetTypeClass), typeClass))
            {
                return "";
            }
            switch(typeClass)
            {
                case AssetTypeClass.ReferenceItemType:
                    return "Reference";
                default:
                    return typeClass.ToString();
            }
        }

        public static int GetClassFromCategory(string category)
        {
            switch(category)
            {
                case "Reference":
                    return (int)AssetTypeClass.ReferenceItemType;
                default:
                    if(Enum.TryParse(category, out AssetTypeClass assetTypeClass))
                    {
                        return (int)assetTypeClass;
                    } else {
                        return (int)AssetTypeClass.Generic;
                    }
            }
        }

        public void IndexAsset(Guid AssetUid)
        {
            List<IndexObjectModel> models = LoadModels(_context, _companyID, null, null, AssetUid).ToList();
            if (models.Any())
            {
                _source.RemoveFromIndex(models);
                _source.AddToIndex(models);
            }
        }

        public void IndexAssets(ConcurrentBag<Guid> AssetGuids)
        {
            if (AssetGuids.Count == 1)
            {
                IndexAsset(AssetGuids.First());
                return;
            }

            string batchUid = Guid.NewGuid().ToString().Replace('-', '_');
            string batchTableName = $"##searcindexbatch_{batchUid}";

            var batchTable = new DataTable();
            batchTable.Columns.Add("AssetUid", typeof(Guid));
            AssetGuids.Distinct().ForEach(g =>
            {
                var batchRow = batchTable.NewRow();
                batchRow["AssetUid"] = g;
                batchTable.Rows.Add(batchRow);
            });

            _context.Execute($@"DROP TABLE IF EXISTS {batchTableName};
            CREATE TABLE {batchTableName} (AssetUid uniqueidentifier, AssetTypeUid uniqueidentifier, class int, AssetID bigint);");

            using (SqlBulkCopy bulkCopy = new SqlBulkCopy(_context))
            {
                bulkCopy.DestinationTableName = batchTableName;
                bulkCopy.ColumnMappings.Add("AssetUid", "AssetUid");
                bulkCopy.WriteToServer(batchTable);
            }

            _context.Execute($@"UPDATE t
            SET t.AssetTypeUid = at.uid,
	            t.Class = at.Class,
	            t.AssetID = a.id
            FROM {batchTableName} t
            INNER JOIN Asset a ON t.AssetUid = a.uid
            INNER JOIN AssetType at on a.AssetTypeID = at.ID;

            CREATE NONCLUSTERED INDEX IX_searcindexbatch_{batchUid} ON {batchTableName} (AssetID);
            ");

            IEnumerable<AssetTypeClass> types = _context.Query<AssetTypeClass>($"SELECT DISTINCT Class FROM {batchTableName} WHERE Class IS NOT NULL");
            types.ForEach(t =>
            {
                IEnumerable<IndexObjectModel> models = LoadModels(_context, _companyID, t, null, null, false, batchTableName);
                if (models.Any())
                {
                    _source.RemoveFromIndex(models);
                    _source.AddToIndex(models);
                }
            });
            _context.Execute($@"DROP TABLE IF EXISTS {batchTableName};");
        }

        public void IndexAsset(string Object, long ObjectID)
        {
            List<IndexObjectModel> models = LoadModels(_context, _companyID, Object, ObjectID).ToList();
            if (models.Any())
            {
                _source.RemoveFromIndex(models);
                _source.AddToIndex(models);
            }
        }

        public void IndexAssets(ConcurrentBag<Tuple<string, long>> tuples)
        {
            List<IndexObjectModel> models = tuples.Distinct().SelectMany(t => LoadModels(_context, _companyID, t.Item1, t.Item2)).ToList();
            if (models.Any())
            {
                _source.RemoveFromIndex(models);
                _source.AddToIndex(models);
            }
        }

        public void IndexAssetType(Guid AssetTypeUid, bool clearIndex = true)
        {
            if (_context.State != ConnectionState.Open)
            {
                _context.Open();
            }

            if (clearIndex)
            {
                _source.ClearIndex(_companyID, AssetTypeUid);
            }

            AssetClassAndName assettype = _context.QueryFirstOrDefault<AssetClassAndName>("SELECT [Class], [Name] FROM AssetType att WHERE att.uid = @AssetTypeUid", new { AssetTypeUid });
            if (assettype != null)
            {
                UpdateDBLog(assettype.Class, AssetTypeUid, SearchJobStatus.Processing);
                try
                {
                    //If clearIndex is not set, the method is being called from IndexAssetClass, so field query should use temp tables.
                    IEnumerable<IndexObjectModel> models = LoadModels(_context, _companyID, assettype.Class, AssetTypeUid, null, !clearIndex);
                    _source.AddToIndex(models);
                } catch (Exception e)
                {
                    UpdateDBLog(assettype.Class, AssetTypeUid, SearchJobStatus.Error, e.Message);
                    throw;
                }
                UpdateDBLog(assettype.Class, AssetTypeUid, SearchJobStatus.Completed);
            }

            if (_context.State != ConnectionState.Closed)
            {
                _context.Close();
            }

        }

        public void IndexAssetClass(AssetTypeClass assetClass)
        {
            if (_context.State != ConnectionState.Open)
            {
                _context.Open();
            }
            _source.ClearIndex(_companyID, assetClass.ToString());
            bool processByAssetType = false;
            int assettypeclass = (int)assetClass;

            long assetCount = CreatePendingDBLog(assetClass, null);

            if(processByAssetType)
            {
                UpdateDBLog(assetClass, null, SearchJobStatus.ProcessingAsType);
                List<Guid> assetTypes = _context.Query<Guid>("SELECT at.uid FROM [dbo].[AssetType] at WHERE EXISTS (SELECT 1 FROM [dbo].[Asset] a WHERE a.assettypeid = at.id) AND at.class =  @assettypeclass", new { assettypeclass }).ToList();
                assetTypes.ForEach(t => CreatePendingDBLog(assetClass, t));

                assetTypes.ForEach(t =>
                {
                    try
                    {
                        IndexAssetType(t, false);
                    }
                    catch (PagedQueryException e)
                    {
                        _messages.Add($"Failed to index AssetType {t}: {e.Message}");
                    }
                    catch (Exception e)
                    {
                        _messages.Add($"Exception caught indexing AssetType {t}: {e.Message}");
                    }
                });
            }
            else
            {
                UpdateDBLog(assetClass, null, SearchJobStatus.Processing);
                IEnumerable<IndexObjectModel> models = LoadModels(_context, _companyID, assetClass, null, null);
                _source.AddToIndex(models);
            }

            if(_messages.Any())
            {
                string exceptionMessage = string.Join(Environment.NewLine, _messages);
                UpdateDBLog(assetClass, null, SearchJobStatus.Error, exceptionMessage);
                _messages.Clear();
                throw new SearchIndexException(exceptionMessage);
            }
            else
            {
                UpdateDBLog(assetClass, null, SearchJobStatus.Completed);
            }
            if (_context.State != ConnectionState.Closed)
            {
                _context.Close();
            }

        }

        public void IndexObjectType(string ObjectType, bool clearIndex = true)
        {
            if (clearIndex)
            {
                _source.ClearIndex(_companyID, ObjectType);
            }

            IEnumerable<IndexObjectModel> models = LoadModels(_context, _companyID, ObjectType);
            _source.AddToIndex(models);
        }

        public enum SearchJobStatus
        {
            None,
            Pending,
            Processing,
            ProcessingAsType,
            Error,
            Completed
        }

        public void QueueRebuildRequest(AssetTypeClass assetClass, Guid? assetTypeUid)
        {
            if(assetTypeUid == Guid.Empty)
            {
                assetTypeUid = null;
            }

            var queue = new AzureQueueSource();
            ReindexModel model = new ReindexModel { CompanyID = _companyID, Category = assetClass.ToString() };
            if(assetTypeUid != null)
            {
                model.AssetTypeUid = assetTypeUid;
            }
            CreatePendingDBLog(assetClass, assetTypeUid);

            queue.CreateMessage(Config.GetValue<string>("SearchIndexQueue"), model);
        }

        private int CreatePendingDBLog(AssetTypeClass assetClass, Guid? assetTypeUid)
        {
            object param = new { assetClass, assetTypeUid = assetTypeUid ?? Guid.Empty, status = SearchJobStatus.Pending };

            //clean table, remove x days old records
            _context.Execute("UPDATE [queue].[Search] SET Active=0 WHERE Active=1 and Class = @assetClass and AssetTypeUid = @AssetTypeUid", param);
            _context.Execute("DELETE FROM [queue].[Search] WHERE LastUpdate <= DATEADD(DAY, -30, GETUTCDATE())");

            //Insert record with count
            _context.Execute(@"INSERT INTO [queue].[Search] (Class, AssetTypeUid, Status, TargetCount)
                    SELECT @assetClass, @assetTypeUid, @status, count(1)
                    FROM [dbo].[asset] a INNER JOIN  [dbo].[assettype] at ON a.assettypeid = at.id
                    where at.class = @assetClass" + (assetTypeUid == null ? "" : " and at.uid = @assetTypeUid"), param);

            if(assetTypeUid == null)
            {
                //When pending a Class/Categroy, archive all asset types under that category
                _context.Execute("UPDATE [queue].[Search] SET Active=0 WHERE Active=1 and Class = @assetClass and AssetTypeUid <> @AssetTypeUid", param);
            }

            int count = _context.Query<int>("SELECT TargetCount FROM [queue].[Search] where Class=@assetClass and AssetTypeUid=@assetTypeUid and status=@status", param).FirstOrDefault();
            return count;
        }

        private void UpdateDBLog(AssetTypeClass assetClass, Guid? assetTypeUid, SearchJobStatus status, string message = "")
        {
            //Update record
            var param = new { assetClass, assetTypeUid = assetTypeUid ?? Guid.Empty, status, message };
            _context.Execute(@"MERGE INTO [queue].[Search] AS tgt
                USING
                  (SELECT @assetClass, @assetTypeUid, @status, @message) AS src (Class, AssetTypeUid, Status, Message)
                  ON tgt.Class = src.Class and tgt.AssetTypeUid = src.AssetTypeUid and tgt.Active=1
                WHEN MATCHED THEN
                  UPDATE       
                    SET
                        Status=src.Status,
                        Message=src.Message,
                        LastUpdate=getutcdate()
                WHEN NOT MATCHED THEN
                  INSERT (Class, AssetTypeUid, Status, Message, LastUpdate)
                  VALUES (src.Class, src.AssetTypeUid, src.Status, src.Message, getutcdate());", param);

            if (new List<SearchJobStatus> { SearchJobStatus.Processing, SearchJobStatus.ProcessingAsType }.Contains(status))
            {
                _context.Execute("UPDATE [queue].[Search] SET Start = getutcdate() WHERE Active=1 AND Class=@assetClass AND AssetTypeUid=@assetTypeUid", param);
            }
        }

        private string GenerateUrl(string type, int typeId, int objectId, string fallback = "")
        {
            switch(type)
            {
                case "Artifact":
                    return $"artifact/{typeId}/{objectId}";
                case "Policy":
                    return $"policy/{typeId}/id/{objectId}";
                case "Rule":
                    return $"quality/rule/{typeId}/{objectId}";
                case "Taxonomy":
                    return $"model/{typeId}/id/{objectId}";
                default:
                    return fallback;
            }
        }

        private IEnumerable<IndexObjectModel> LoadModels(SqlConnection context, int companyID, AssetTypeClass? assetClass, Guid? AssetTypeUid, Guid? AssetUid, bool useTempTable = false, string batchTable = null)
        {
            IndexMode mode = IndexMode.BaseQuery;
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
            {
                throw new Exception("AssetClass is null");
            }

            string joinBatchTable = "";
            if (!string.IsNullOrEmpty(batchTable))
            {
                joinBatchTable = $"inner join {batchTable} bt on bt.AssetID = a.id";
                parameters.Add("batchtable", batchTable);
            }

            int assettypeclass = (int)assetClass;
            switch (assetClass)
            {
                case AssetTypeClass.BusinessAsset:
                case AssetTypeClass.TechnicalAsset:
                case AssetTypeClass.Diagram:
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
                    string UrlMethod = assetClass == AssetTypeClass.Diagram ? "dbo.GenerateAssetUrl(a.ID)" : "''";

                    sql = $@"SELECT AssetID, ItemUniqueID, Type, ID, TypeID, DisplayValue, TypeName, AssetTypeUid, Uid, Url FROM (
                        SELECT
                            A.ID as AssetID,
	                        cast(A.ID as varchar) as ItemUniqueID,
                            A.Object as Type,
	                        A.ObjectID as ID,
	                        att.ObjectID as TypeID,
	                        adv.DisplayValue,
	                        att.Name as TypeName,
                            att.uid as AssetTypeUid,
	                        a.uid as Uid,
                            {UrlMethod} as 'Url'
                        from
	                        [dbo].Asset a
	                        inner join [dbo].assettype att on a.assettypeid = att.id
	                        inner join [dbo].assetdisplayvalue adv on adv.assetid = a.id
                            {joinBatchTable}
                        where
	                          {whereCondition}
                        ) q ORDER BY q.ID";
                    mode = IndexMode.WithFields | IndexMode.WithTags | IndexMode.WithResponsibility;
                    shaper = (dynamic o) =>
                    {
                        return new IndexObjectModel
                        {
                            Category = GetCategoryFromClass(assetClass),
                            CompanyID = companyID,
                            ID = o.ID,
                            AssetID = o.AssetID,
                            ItemUniqueID = o.ItemUniqueID,
                            AssetType = o.TypeName,
                            RelativeUrl = GenerateUrl(o.Type, o.TypeID, o.ID, o.Url),
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
                            Category = GetCategoryFromClass(assetClass),
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
                            {joinBatchTable}
	                        left outer join [dbo].assetdisplayvalue adv on adv.assetid = a.id
                        where
                            {whereCondition}
                        ORDER BY A.ID";
                    shaper = (dynamic o) =>
                    {
                        return new IndexObjectModel
                        {
                            Category = GetCategoryFromClass(assetClass),
                            CompanyID = companyID,
                            ID = o.ID,
                            AssetID = o.AssetID,
                            ItemUniqueID = o.ItemUniqueID,
                            AssetType = o.TypeName,
                            RelativeUrl = $"resource/{o.ID}",
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
	                        a.uid as Uid
                        from
	                        [dbo].Asset a
                            inner join [Group] g on g.ID = a.ObjectID and [Object] = 'Group'
	                        inner join [dbo].assettype att on a.assettypeid = att.id
	                        inner join [dbo].assetdisplayvalue adv on adv.assetid = a.id
                            {joinBatchTable}
                        where
	                        {whereCondition}
                        ORDER BY A.ID";
                    shaper = (dynamic o) =>
                    {
                        return new IndexObjectModel
                        {
                            Category = GetCategoryFromClass(assetClass),
                            CompanyID = companyID,
                            ID = o.ID,
                            AssetID = o.AssetID,
                            ItemUniqueID = o.ItemUniqueID,
                            AssetType = o.TypeName,
                            RelativeUrl = $"group/{o.ID}",
                            Uid = o.Uid,
                            AssetTypeUid = o.AssetTypeUid,
                            Fields = new Dictionary<string, string>() {
                                { "Name", o.DisplayValue },
                                { "Description", o.Description }
                            }
                        };
                    };
                    break;
                default:
                    break;
            }

            return getData(context, sql, parameters, mode, shaper, useTempTable);
        }

        private IEnumerable<IndexObjectModel> LoadModels(SqlConnection context, int companyID, string Object, long? ObjectID = null)
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
                            Category = GetCategoryFromClass(AssetTypeClass.ReferenceItemType),
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
            return getData(context, sql, parameters, IndexMode.BaseQuery, shaper);
        }

        private static IEnumerable<IndexObjectModel> getData(SqlConnection context, string sql, DynamicParameters parameters, IndexMode mode, Func<dynamic, IndexObjectModel> convertToDictionary, bool useTempTable = false)
        {
            IPagedQuery<FieldSqlModel> FieldQuery = null;
            PagedQuery<TagSqlModel> TagsQuery = null;
            PagedQuery<ResponsibilitySqlModel> ResponsibilityQuery = null;

            if (mode.HasFlag(IndexMode.WithFields))
            {
                if (!useTempTable)
                {
                    long assetCount = context.QueryFirstOrDefault<int>(@"SELECT COUNT(1) FROM [dbo].[Asset]");
                    useTempTable = assetCount > (4 * _indexClassAsTypesLimit);
                }

                if (useTempTable)
                {
                    FieldQuery = new TempTablePagedQuery<FieldSqlModel>(context, GetFieldQuery(parameters), parameters);
                }
                else
                {
                    FieldQuery = new PagedQuery<FieldSqlModel>(context, GetFieldQuery(parameters), parameters);
                }
            }
            if (mode.HasFlag(IndexMode.WithTags))
            {
                TagsQuery = new PagedQuery<TagSqlModel>(context, GetTagQuery(parameters), parameters);
            }
            if (mode.HasFlag(IndexMode.WithResponsibility))
            {
                ResponsibilityQuery = new PagedQuery<ResponsibilitySqlModel>(context, GetResponsibilityQuery(parameters), parameters);
            }
            IEnumerable<IndexObjectModel> list = context.Query(sql, parameters, commandTimeout: _defaultQueryCommandTimeout, buffered: false).Select<dynamic, IndexObjectModel>(a => convertToDictionary(a));

            foreach (var item in list)
            {
                if (FieldQuery != null) {
                    var subset = FieldQuery.GetByAssetID(item.AssetID);
                    foreach (var f in subset)
                    {
                        item.Fields[f.Name] = f.FormattedValue;
                    }
                }
                if (TagsQuery != null && item.Uid.HasValue && item.Uid != Guid.Empty)
                {
                    item.Tags = TagsQuery.GetByAssetID(item.AssetID).ToDictionary(x => x.TagUID.ToString(), x => x.Value);
                }
                if (ResponsibilityQuery != null)
                {
                    var secset = ResponsibilityQuery.GetByAssetID(item.AssetID);
                    item.NoRead = new Dictionary<string, List<int>> {
                        { "R" , secset.Where(r => r.SecurityAsset == "R").Select(r => r.SecurityAssetID).ToList() },
                        { "G" , secset.Where(r => r.SecurityAsset == "G").Select(r => r.SecurityAssetID).ToList() },
                        { "O" , secset.Where(r => r.SecurityAsset == "O").Select(r => r.SecurityAssetID).ToList() }
                    };
                }
                yield return item;
            }
        }

        private static string GetFieldQuery(DynamicParameters parameters)
        {
            List<string> fieldWhere = new List<string>();
            List<string> existsWhere = new List<string>();
            List<string> fieldJoin = new List<string>();

            fieldJoin.Add("inner join FieldType FT on FT.ID = F.FieldTypeID");

            if (parameters.ParameterNames.Contains("assettypeclass"))
            {
                existsWhere.Add("ATT.class = @assettypeclass");
            }
            if (parameters.ParameterNames.Contains("assettypeuid"))
            {
                existsWhere.Add("att.uid = @assettypeuid");
            }
            if (parameters.ParameterNames.Contains("assetuid"))
            {
                fieldJoin.Add("inner join Asset a on a.ID = F.AssetID");
                fieldWhere.Add("a.uid = @assetuid");
            }
            if (parameters.ParameterNames.Contains("batchtable"))
            {
                fieldJoin.Add($"inner join {parameters.Get<string>("batchtable")} bt on F.AssetID = bt.AssetID");
            }

            if(existsWhere.Any())
            {
                existsWhere.Add("ATT.ID = FT.AssetTypeID");
                fieldWhere.Add($"exists (select 1 from AssetType ATT where {string.Join(Environment.NewLine + " and ", existsWhere.ToArray())})");
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
            if (parameters.ParameterNames.Contains("batchtable"))
            {
                tagJoin.Add($"inner join {parameters.Get<string>("batchtable")} bt on at.AssetID = bt.AssetID");
            }

            string tagsSql = @"SELECT a.ID as AssetID, a.uid AS AssetUID, t.uid AS TagUID, t.Value FROM [dbo].[AssetTag] at " +
            "INNER JOIN [dbo].[Tag] t ON at.TagID = t.ID INNER JOIN [dbo].[Asset] a ON at.AssetID = a.ID";
            if (tagJoin.Any())
            {
                tagsSql += " " + string.Join(" " + Environment.NewLine, tagJoin.ToArray());
            }
            if (tagWhere.Any())
            {
                tagsSql += " WHERE " + string.Join(" AND " + Environment.NewLine, tagWhere.ToArray());
            }

            return tagsSql;
        }

        private static string GetResponsibilityQuery(DynamicParameters parameters)
        {
            List<string> joins = new List<string>();
            List<string> conditions = new List<string>();

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
                joins.Add("INNER JOIN [dbo].Asset a ON q.AssetID = a.ID");
                conditions.Add("a.uid = @assetuid");
            }
            else if (parameters.ParameterNames.Contains("assettypeuid"))
            {
                joins.Add("INNER JOIN [dbo].Asset a ON q.AssetID = a.ID");
                joins.Add("INNER JOIN [dbo].AssetType att on a.AssetTypeID = att.id");
                conditions.Add("att.uid = @assettypeuid");
            }
            if (parameters.ParameterNames.Contains("batchtable"))
            {
                joins.Add($"INNER JOIN {parameters.Get<string>("batchtable")} bt on q.AssetID = bt.AssetID");
                if(parameters.ParameterNames.Contains("assettypeclass"))
                {
                    conditions.Add("bt.Class = @assettypeclass");
                }
            }
            if(joins.Any())
            {
                sql = $@"SELECT q.* FROM ({sql}) q {string.Join(" ", joins)}";
                if (conditions.Any())
                {
                    sql += $" WHERE {string.Join(" AND ", conditions)}";
                }
            }
            return sql;
        }
    }

    [Serializable]
    public class SearchIndexException : Exception
    {
        public SearchIndexException()
        { }
        public SearchIndexException(string message)
            : base(message)
        { }
        public SearchIndexException(string message, Exception innerException)
            : base(message, innerException)
        { }
    }

    [Flags]
    internal enum IndexMode
    {
        BaseQuery = 0,
        WithFields = 1,
        WithTags = 2,
        WithResponsibility = 4
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

    [Serializable]
    public class PagedQueryException : Exception
    {
        public PagedQueryException()
        { }
        public PagedQueryException(string message)
            : base(message)
        { }
        public PagedQueryException(string message, Exception innerException)
            : base(message, innerException)
        { }
    }

    internal abstract class BasePagedQuery<T> : IPagedQuery<T> where T : IPagedQuerySqlModel
    {
        protected int PageSize = 50000;
        protected long CurrentHighID;
        protected List<T> _data;
        protected SqlConnection _connection;
        protected string _query;
        public DynamicParameters _param;
        protected bool LastPage;
        protected readonly int _defaultQueryCommandTimeout = 180;

        protected BasePagedQuery(SqlConnection connection, DynamicParameters param = null)
        {
            _connection = connection;
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

        //Hook, used to clean up temp tables
        protected virtual void OnLastPage()
        {
        }

        /// <summary>
        /// Fetches the next "page" of data. Starting with the requested AssetID
        /// No need to get any records with a lower AssetID's
        /// </summary>
        /// <param name="AssetID"></param>
        private void FetchDataPage(long AssetID)
        {
            if (LastPage)
            {
                return;
            }

            _param.Add("PagerAssetID", AssetID);
            _param.Add("PageSize", PageSize);
            try
            {
                _data = _connection.Query<T>(_query, _param, commandTimeout: _defaultQueryCommandTimeout).ToList();
                if (_data.Count() < PageSize)
                {
                    //If we fetched less than PageSize, this is the last page of data
                    LastPage = true;
                    OnLastPage();
                }
                else
                {
                    long MinAssetID = _data.Min(i => i.AssetID);
                    long MaxAssetID = _data.Max(i => i.AssetID);
                    if (MinAssetID == MaxAssetID)
                    {
                        //If min and max AssetID is the same, the whole "page" is the same asset and it can't be guaranteed that all records for one asset has been fetched
                        throw new PagedQueryException("Search of " + typeof(T) + " got more than " + PageSize + " results for one AssetID");
                    }
                    else
                    {
                        //The page may have an incomplete set of records for the highest Asset ID, so remove those from the data stored.
                        _data.RemoveAll(i => i.AssetID == MaxAssetID);
                        CurrentHighID = _data.Max(i => i.AssetID);
                    }
                }
            } catch (Exception e)
            {
                throw new PagedQueryException($"Failed paged query for {AssetID}, {_query}. Error: {e.Message}");
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
            {
                FetchDataPage(AssetID);
            }

            return _data.Where(i => i.AssetID == AssetID).ToList();
        }
    }

    internal class PagedQuery<T> : BasePagedQuery<T> where T : IPagedQuerySqlModel
    {
        /// <summary>
        /// Performs a paged/chunked query
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="query">Query string</param>
        /// <param name="param"></param>
        public PagedQuery(SqlConnection connection, string query, DynamicParameters param = null) : base(connection, param)
        {
            //Use <T> to specify columns to select, as SqlMapper can slow down a lot over *
            string alias = "pagedquery";
            string queryColumns = string.Join(", ", typeof(T).GetProperties().Select(p => $"{alias}.{p.Name}").ToArray());
            _query = $"SELECT TOP (@PageSize) {queryColumns} FROM ({query}) {alias} WHERE {alias}.AssetID >= @PagerAssetID ORDER BY {alias}.AssetID option(recompile)";
        }
    }

    internal class TempTablePagedQuery<T> : BasePagedQuery<T> where T : IPagedQuerySqlModel
    {
        private readonly string _tableIdentifier;

        /// <summary>
        /// Performs a paged/chunked query using a global temporary table to hold the result of the query
        /// and then paging from that
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="query">Query string</param>
        /// <param name="param"></param>
        public TempTablePagedQuery(SqlConnection connection, string query, DynamicParameters param = null) : base(connection, param)
        {
            PageSize = 150000;
            //generate random name for global temp table
            _tableIdentifier = "pagedQuery_" + Guid.NewGuid().ToString().Replace("-", "_");

            //Use <T> to specify columns to select, as SqlMapper can slow down a lot over *
            string alias = "pagedquery";
            string queryColumns = string.Join(", ", typeof(T).GetProperties().Select(p => $"{alias}.{p.Name}").ToArray());
            _query = $"SELECT TOP (@PageSize) {queryColumns} FROM ##{_tableIdentifier} {alias} WHERE {alias}.AssetID >= @PagerAssetID ORDER BY {alias}.sortid";

            try
            {
                _connection.Execute($@"
                    DROP TABLE IF EXISTS ##{_tableIdentifier};

                    SELECT ROW_NUMBER() OVER (ORDER BY AssetID) AS sortid, {queryColumns}
                    INTO ##{_tableIdentifier}
                    FROM ({query}) {alias};

                    CREATE UNIQUE INDEX UIX_{_tableIdentifier} ON ##{_tableIdentifier} (sortid); 

                    CREATE NONCLUSTERED INDEX IX_{_tableIdentifier}_AssetID ON ##{_tableIdentifier} (AssetID); 
                ", _param, null, _defaultQueryCommandTimeout * 20); //Multiply timeout for statement creating the temp table
            }
            catch (Exception e)
            {
                throw new PagedQueryException($"TempTablePagedQuery failed to create temp table. Error: {e.Message}");
            }
        }
        ~TempTablePagedQuery()
        {
            OnLastPage();
        }

        protected override void OnLastPage()
        {
            base.OnLastPage();
            try
            {
                _connection.Execute($"DROP TABLE IF EXISTS ##{_tableIdentifier}");
            } catch (Exception)
            {
                //If connection is closed, the temp table is automatically dropped
            }
        }
    }
}
