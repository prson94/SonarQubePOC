using d360.core;
using d360.core.enums;
using d360.core.queue;
using d360.core.search;
using d360.extensions.search.models;
using Dapper;
using MoreLinq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Xml.Linq;

namespace d360.extensions.search
{
	public class SearchIndexer
    {
        private static readonly int _defaultQueryCommandTimeout = 180;
        private static readonly int _indexClassAsTypesLimit = 400000;
        private readonly SqlConnection _context;
        private readonly int _companyID;
        private readonly ISearchSource _source;
        private readonly List<string> _messages;

        public SearchIndexer(SqlConnection context, int companyID, ISearchSource source)
        {
            _context = context;
            _companyID = companyID;
            _source = source;
            _messages = new List<string>();
        }

		public static readonly ReadOnlyCollection<string> ExcludedFieldTypes = new List<string> {
			"DateTime",
			"Color",
			"FilteredLookup",
			"ComplexRelationLookup",
			"OwnershipLookup",
			"Relationship",
			"FieldFromRelationship",
			"RefListRelationship",
			"ReferenceList",
			"JSON"
		}.AsReadOnly();

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
                AssetTypeClass.SemanticType.ToString(),
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
            if (obj == SystemObjects.Taxonomy.ToString())
            {
                return AssetTypeClass.Model.ToString();
            }
            else if (obj == SystemObjects.Resource.ToString())
            {
                return AssetTypeClass.User.ToString();
            }
            else if (obj == AssetTypeClass.ReferenceItemType.ToString())
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
            if (typeClass == null || !Enum.IsDefined(typeof(AssetTypeClass), typeClass))
            {
                return "";
            }
            switch (typeClass)
            {
                case AssetTypeClass.ReferenceItemType:
                    return "Reference";
				case AssetTypeClass.Predicate:
					return "Synonym";
                default:
                    return typeClass.ToString();
            }
        }

        public static int GetClassFromCategory(string category)
        {
            switch (category)
            {
                case "Reference":
                    return (int)AssetTypeClass.ReferenceItemType;
                case "Synonym":
                    return (int)AssetTypeClass.Predicate;
                default:
                    if (Enum.TryParse(category, out AssetTypeClass assetTypeClass))
                    {
                        return (int)assetTypeClass;
                    }
                    else
                    {
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

        public void IndexAssets(List<Guid> AssetGuids)
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

			if (_context.State != ConnectionState.Open)
			{
				_context.Open();
			}
			
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

            UPDATE t
            SET t.Class = {(int)AssetTypeClass.SemanticType}
            FROM {batchTableName} t
            INNER JOIN Semantic s ON t.AssetUid = s.uid;

            CREATE CLUSTERED INDEX CX_searcindexbatch_{batchUid} ON {batchTableName} (AssetID);
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

        public void IndexAssets(IEnumerable<Tuple<string, long>> tuples)
        {
            List<IndexObjectModel> models = tuples.Distinct().SelectMany(t => LoadModels(_context, _companyID, t.Item1, t.Item2)).ToList();
            if (models.Any())
            {
                _source.RemoveFromIndex(models);
                _source.AddToIndex(models);
            }
        }

        public void RemoveAssets(IEnumerable<Guid> AssetGuids)
        {
            if (!AssetGuids.Any())
            {
                return;
            }
            _source.RemoveByUids(_companyID, AssetGuids);
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
                }
                catch (Exception e)
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

            //Class "Predicate" is overloaded to be used for synonyms and intersects
            if (assetClass == AssetTypeClass.Predicate)
            {
                long assetCount = CreatePendingDBLog(assetClass, null);
                UpdateDBLog(assetClass, null, SearchJobStatus.Processing);
                IndexObjectType("Synonym");
				IndexObjectType("Intersect", false);
			}
			else
            {
				var skip = false;
				try
				{
					_source.ClearIndex(_companyID, assetClass.ToString());
				}
				catch (Exception e)
				{
					skip = true;
					_messages.Add($"Exception caught clearing index for Asset Category {assetClass}: {e.Message}");
				}

				if (!skip)
				{
					bool processByAssetType = false;
					int assettypeclass = (int)assetClass;

					long assetCount = CreatePendingDBLog(assetClass, null);

					//Use count of assets in class to determine if the class contains a large number of assets
					//and indexing by asset type is more performant. Only larger asset classes.
					if (new List<AssetTypeClass> {
						AssetTypeClass.BusinessAsset,
						AssetTypeClass.TechnicalAsset,
						AssetTypeClass.Diagram,
						AssetTypeClass.Model,
						AssetTypeClass.Policy,
						AssetTypeClass.Rule
					}.Contains(assetClass))
					{
						processByAssetType = assetCount > _indexClassAsTypesLimit;
					}

					if (processByAssetType)
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
						try
						{
							IEnumerable<IndexObjectModel> models = LoadModels(_context, _companyID, assetClass, null, null);
							_source.AddToIndex(models);
						}
						catch (Exception e)
						{
							_messages.Add($"Exception caught indexing Asset Category {assetClass}: {e.Message}");
						}
					}
				}
			}

            if (_messages.Any())
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

        private void IndexObjectType(string ObjectType, bool clearIndex = true)
        {
            if (clearIndex)
            {
                _source.ClearIndex(_companyID, ObjectType);
            }

            IEnumerable<IndexObjectModel> models = LoadModels(_context, _companyID, ObjectType);
            _source.AddToIndex(models);
        }

		public void IndexIntersects(IEnumerable<int> intersectIds)
		{
			string batchUid = Guid.NewGuid().ToString().Replace('-', '_');
			string batchTableName = $"##searcindexintersectbatch_{batchUid}";

			var batchTable = new DataTable();
			batchTable.Columns.Add("IntersectID", typeof(int));
			intersectIds.Distinct().ForEach(g =>
			{
				var batchRow = batchTable.NewRow();
				batchRow["IntersectID"] = g;
				batchTable.Rows.Add(batchRow);
			});

			if (_context.State != ConnectionState.Open)
			{
				_context.Open();
			}

			_context.Execute($@"DROP TABLE IF EXISTS {batchTableName};
            CREATE TABLE {batchTableName} (IntersectID int);");

			using (SqlBulkCopy bulkCopy = new SqlBulkCopy(_context))
			{
				bulkCopy.DestinationTableName = batchTableName;
				bulkCopy.ColumnMappings.Add("IntersectID", "IntersectID");
				bulkCopy.WriteToServer(batchTable);
			}

			_context.Execute($"CREATE NONCLUSTERED INDEX IX_searcindexintersectbatch_{batchUid} ON {batchTableName} (IntersectID);");

			IEnumerable<IndexObjectModel> models = LoadModels(_context, _companyID, "Intersect", null, batchTableName);
			if (models.Any())
			{
				_source.RemoveFromIndex(models);
				_source.AddToIndex(models);
			}

			_context.Execute($@"DROP TABLE IF EXISTS {batchTableName};");
		}

		public void RemoveIntersects(IEnumerable<int> intersectIds)
		{
			var deletes = new List<IndexObjectModel>();
			intersectIds.ForEach((i) =>
			{
				//Intersects have two search documents, se we need to delete both
				deletes.Add(new IndexObjectModel
				{
					CompanyID = _companyID,
					Category = "Synonym",
					ItemUniqueID = $"intersect|{i}|O"
				});
				deletes.Add(new IndexObjectModel
				{
					CompanyID = _companyID,
					Category = "Synonym",
					ItemUniqueID = $"intersect|{i}|S"
				});
			});

			_source.RemoveFromIndex(deletes);
		}

		public void IndexUpdateAssetPaths(IEnumerable<long> AssetIds)
        {
            string sql = @"select
	            att.Class as assetclass,
	            A.ObjectID as ID,
	            A.ID as AssetID,
	            cast(A.ID as varchar) as ItemUniqueID,
	            att.Name as TypeName,
	            a.uid as Uid,
                att.uid as AssetTypeUid,
	            ap.Segments,
				att.DefaultPermissions
            from [dbo].[Asset] a
            inner join [dbo].[AssetType] att on a.AssetTypeID = att.id
            inner join [dbo].[AssetPath] ap on a.ID = ap.id
            inner join @ids i on a.ID = i.Id 
            where ap.segments is not null";

            DynamicParameters parameters = new DynamicParameters();
            DataTable dt = new DataTable();
            dt.Columns.Add("Id", typeof(long));
            AssetIds.Distinct().ForEach(a => dt.Rows.Add(a));
            parameters.Add("ids", dt.AsTableValuedParameter("[dbo].[Ids]"));

            Func<dynamic, IndexObjectModel> shaper = (o) =>
            {
                return new IndexObjectModel
                {
                    Category = GetCategoryFromClass(o.assetclass),
                    CompanyID = _companyID,
                    ID = o.ID,
                    AssetID = o.AssetID,
                    ItemUniqueID = o.ItemUniqueID,
                    AssetType = o.TypeName,
                    Uid = o.Uid,
                    AssetTypeUid = o.AssetTypeUid,
					DefaultPermissions = o.DefaultPermissions == 1,
					AssetPath = GetPathArrayFromSegments(o.Segments),
                };
            };

            IEnumerable<IndexObjectModel> models = getData(_context, sql, parameters, IndexMode.Basic, shaper);
            _source.UpdateInIndex(models, true);
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

		public bool CanCreatePendingDBLog(AssetTypeClass assetClass, Guid? assetTypeUid, bool force = false)
		{
			object param = new { assetClass, assetTypeUid = assetTypeUid ?? Guid.Empty, status = SearchJobStatus.Pending };
			var pending = _context.QuerySingle<int>(@$"SELECT COUNT(1) FROM [queue].[Search]
				WHERE Class = @assetClass AND AssetTypeUid = @AssetTypeUid
				AND ((Active = 1 AND status <> {(int)SearchJobStatus.Completed}) OR status = @status)
				AND LastUpdate > DATEADD(MINUTE, -5, GETUTCDATE())
			", param) > 0;

			if (!pending || force)
			{
				CreatePendingDBLog(assetClass, assetTypeUid);
				return true;
			}
			return false;
		}

		private int CreatePendingDBLog(AssetTypeClass assetClass, Guid? assetTypeUid)
        {
            object param = new { assetClass, assetTypeUid = assetTypeUid ?? Guid.Empty, status = SearchJobStatus.Pending };
			var assetTypeEmpty = assetTypeUid == null || assetTypeUid == Guid.Empty;

            //clean table, remove x days old records
            _context.Execute("UPDATE [queue].[Search] SET Active=0 WHERE Active=1 and Class = @assetClass and AssetTypeUid = @AssetTypeUid", param);
            _context.Execute("DELETE FROM [queue].[Search] WHERE LastUpdate <= DATEADD(DAY, -30, GETUTCDATE())");

            //Insert record with count
            if (assetClass == AssetTypeClass.Predicate)
            {
                //Class "Predicate" is overloaded to be used for synonyms and intersects
                _context.Execute(@"INSERT INTO [queue].[Search] (Class, AssetTypeUid, Status, TargetCount)
                    SELECT @assetClass, @assetTypeUid, @status, sum(cnt) from (
                        select count(*) * 2 as cnt
                        from [dbo].[intersect] I
                        inner join IntersectType T on T.ID = I.IntersectTypeID
                        inner join Predicate P on P.ID = T.PredicateID and P.Type = 6
                        union all
                        select count(*) as cnt
                        from [dbo].[nym]
                    ) A", param);
            }
			else if (assetClass == AssetTypeClass.SemanticType)
			{
				_context.Execute(@"INSERT INTO [queue].[Search] (Class, AssetTypeUid, Status, TargetCount)
                    SELECT @assetClass, @assetTypeUid, @status, sum(cnt) from (
                        select count(distinct Qualifier) as cnt
                        from [dbo].[semantic]
                    ) A", param);
			}
			else if (assetClass == AssetTypeClass.User)
			{
				_context.Execute($@"INSERT INTO [queue].[Search] (Class, AssetTypeUid, Status, TargetCount)
                    SELECT @assetClass, @assetTypeUid, @status, count(1)
                    FROM [dbo].[asset] a
					INNER JOIN [dbo].[assettype] at ON a.assettypeid = at.id
					INNER JOIN [reporting].[global_resource] u on u.ResourceID = a.ObjectID and a.[Object] = 'Resource'
				where u.[state] = {(int)CompanyResourceState.Active} and a.[state] = {(int)State.Active}
				and at.class = @assetClass", param);
			}
			else if (assetClass == AssetTypeClass.ReferenceItemType)
			{
				_context.Execute($@"INSERT INTO [queue].[Search] (Class, AssetTypeUid, Status, TargetCount)
                    SELECT @assetClass, @assetTypeUid, @status, count(1)
                    FROM [dbo].[assettype] at
                    where at.class = {(int)AssetTypeClass.Reference}" + (assetTypeEmpty ? "" : " and at.uid = @assetTypeUid"), param);
			}
			else
			{
				_context.Execute(@"INSERT INTO [queue].[Search] (Class, AssetTypeUid, Status, TargetCount)
                    SELECT @assetClass, @assetTypeUid, @status, count(1)
                    FROM [dbo].[asset] a INNER JOIN  [dbo].[assettype] at ON a.assettypeid = at.id
                    where at.class = @assetClass" + (assetTypeEmpty ? "" : " and at.uid = @assetTypeUid"), param);
			}

			if (assetTypeEmpty)
            {
                //When pending a Class/Categroy, archive all asset types under that category
                _context.Execute("UPDATE [queue].[Search] SET Active=0 WHERE Active=1 and Class = @assetClass and AssetTypeUid <> @AssetTypeUid", param);
            }

            int count = _context.Query<int>("SELECT TargetCount FROM [queue].[Search] where Class=@assetClass and AssetTypeUid=@assetTypeUid and status=@status and Active=1", param).FirstOrDefault();
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

		private string[] GetPathArrayFromSegments(string segments)
        {
            if (string.IsNullOrWhiteSpace(segments) || segments.IndexOf('<') < 0)
            {
                return null;
            }

            XElement segmentXML = XElement.Parse(segments);
            return segmentXML
                .Descendants("segment")
                .OrderBy(s => { int.TryParse(s.Attribute("level")?.Value, out int l); return l; })
                .ThenBy(s => { int.TryParse(s.Attribute("position")?.Value, out int p); return p; })
                .Select(e => e.Value)
                .ToArray();
        }

        private IEnumerable<IndexObjectModel> LoadModels(SqlConnection context, int companyID, AssetTypeClass? assetClass, Guid? AssetTypeUid, Guid? AssetUid, bool useTempTable = false, string batchTable = null)
        {
            IndexMode mode = IndexMode.Basic;
            string sql = "";
            List<string> where = new List<string>();
            Func<dynamic, IndexObjectModel> shaper = null;
            DynamicParameters parameters = new DynamicParameters();

            if (assetClass == null && AssetUid.HasValue)
            {
                assetClass = _context.QueryFirstOrDefault<AssetTypeClass>("SELECT [Class] FROM AssetType att INNER JOIN Asset a on att.ID = a.AssetTypeID WHERE a.uid = @AssetUid", new { AssetUid });

                if (assetClass == AssetTypeClass.Generic)
                {
                    assetClass = _context.QueryFirstOrDefault<AssetTypeClass>($"SELECT {(int)AssetTypeClass.SemanticType} FROM Semantic WHERE uid = @AssetUid", new { AssetUid });
                }
            }
            else if (assetClass == null && AssetTypeUid.HasValue)
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

                    sql = $@"SELECT AssetID, ItemUniqueID, Type, ID, TypeID, DisplayValue, TypeName, AssetTypeUid, Uid, Url, Segments, DefaultPermissions FROM (
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
                            {UrlMethod} as 'Url',
                            ap.Segments as Segments,
							att.DefaultPermissions
                        from
	                        [dbo].Asset a
	                        inner join [dbo].assettype att on a.assettypeid = att.id
	                        inner join [dbo].assetdisplayvalue adv on adv.assetid = a.id
                            left outer join [dbo].[AssetPath] ap on a.ID = ap.ID
                            {joinBatchTable}
                        where
	                          {whereCondition}
                        ) q ORDER BY q.ID";
                    mode = IndexMode.WithFields | IndexMode.WithTags | IndexMode.WithResponsibility;

					if(assetClass == AssetTypeClass.BusinessAsset || assetClass == AssetTypeClass.TechnicalAsset)
					{
						mode |= IndexMode.WithSemantic;
					}
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
							RelativeUrl = (assetClass == AssetTypeClass.Diagram) ? o.Url : $"asset/{o.Uid.ToString().ToLower()}",
							Uid = o.Uid,
							AssetTypeUid = o.AssetTypeUid,
							AssetPath = GetPathArrayFromSegments(o.Segments),
							DefaultPermissions = o.DefaultPermissions == 1,
							Fields = new Dictionary<string, string>() {
                                { "Name", o.DisplayValue }
                            }
                        };
                    };
                    break;
				case AssetTypeClass.Reference:
				case AssetTypeClass.ReferenceItemType:
                    string pathSeperator = "||";
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
                        att.ObjectID as ID,
                        att.Object,
                        att.Name,
                        att.Description,
                        att.uid as AssetTypeUid,
                        p.Path as Path,
						att.DefaultPermissions
                    FROM [dbo].[AssetType] att
                    cross apply dbo.GetAssetTypeTextPathById(att.id, '{pathSeperator}') p
                    WHERE {whereCondition}";
                    shaper = (dynamic o) =>
                    {
                        return new IndexObjectModel
                        {
                            Category = GetCategoryFromClass(assetClass),
                            CompanyID = companyID,
                            ID = o.ID,
                            AssetType = "Reference List",
                            RelativeUrl = $"assets/{o.AssetTypeUid.ToString().ToLower()}",
                            AssetTypeUid = o.AssetTypeUid,
                            AssetPath = o.Path.Split(new[] { pathSeperator }, StringSplitOptions.RemoveEmptyEntries),
							DefaultPermissions = o.DefaultPermissions == 1,
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
	                        adv.DisplayValue,
	                        att.Name as TypeName,
                            att.uid as AssetTypeUid,
	                        a.uid as Uid,
	                        u.email as Email,
	                        CASE
                            WHEN u.Email not like '%@data3sixty.com' and Email not like '%@infogix.com' and Email not like '%@precisely.com'
                                THEN '0'
                                ELSE '1'
                            END as Data3SixtyUser
                        from	Asset a
								inner join reporting.global_resource u on u.ResourceID = a.ObjectID and a.[Object] = 'Resource'
								inner join AssetType att on a.assettypeid = att.id
								{joinBatchTable}
								inner join AssetDisplayValue adv on adv.AssetID = a.ID
                        where	{whereCondition}
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
                            RelativeUrl = $"users/{o.Uid.ToString().ToLower()}",
                            Uid = o.Uid,
                            AssetTypeUid = o.AssetTypeUid,
							DefaultPermissions = true,
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
							DefaultPermissions = true,
                            Fields = new Dictionary<string, string>() {
                                { "Name", o.DisplayValue },
                                { "Description", o.Description }
                            }
                        };
                    };
                    break;
                case AssetTypeClass.SemanticType:
                    if (AssetUid.HasValue && AssetUid != Guid.Empty)
                    {
                        where.Add("s.uid = @assetuid");
                        parameters.Add("assetuid", AssetUid);
                    }
                    else if (!string.IsNullOrEmpty(batchTable))
                    {
                        joinBatchTable = $"inner join {batchTable} bt on bt.AssetUid = s.uid and bt.class = {(int)AssetTypeClass.SemanticType}";
                    }

                    whereCondition = string.Join(" ", where.Select(w => " and " + w).ToArray());

                    sql = $@"select
                        Qualifier,
                        Name,
                        Description,
                        Uid
                    from (
                        select ROW_NUMBER() OVER (PARTITION BY Qualifier ORDER BY EffectiveDate desc ) AS RowNum,
                            Qualifier,
                            Name,
                            Description,
                            uid
                        from Semantic
                    ) S
                    {joinBatchTable}
                    where S.RowNum = 1 {whereCondition}";
                    shaper = (dynamic o) =>
                    {
                        return new IndexObjectModel
                        {
                            Category = GetCategoryFromClass(assetClass),
                            CompanyID = companyID,
                            AssetType = "Semantic Type",
                            ItemUniqueID = o.Qualifier,
                            RelativeUrl = $"semantics/{o.Uid.ToString().ToLower()}",
                            Uid = o.Uid,
							DefaultPermissions = true,
                            Fields = new Dictionary<string, string>() {
                                { "Name", o.Name },
                                { "Description", o.Description },
                                { "Qualifier", o.Qualifier},
                            }
                        };
                    };
                    break;
                case AssetTypeClass.Generic:
                    throw new Exception("AssetClass is generic and cannot be indexed");
                default:
                    break;
            }

            return getData(context, sql, parameters, mode, shaper, useTempTable);
        }

        private IEnumerable<IndexObjectModel> LoadModels(SqlConnection context, int companyID, string Object, long? ObjectID = null, string? batchTable = null)
        {
            string sql = "";
            string where = "";
            Func<dynamic, IndexObjectModel>? shaper = null;
            DynamicParameters parameters = new DynamicParameters();

            switch (Object)
            {
                case "Intersect":
					if (ObjectID != null)
					{
						where = "WHERE I.ID = @ObjectID";
						parameters.Add("ObjectID", ObjectID);
					}
					else if (batchTable != null) {
						where = $" inner join {batchTable} bt on bt.IntersectID = i.ID ";
					}
                    sql = $@"select 
                I.ID,
                'S' as 'Direction',
                SubjectAdv.DisplayValue as 'Synonym',
                ObjectAdv.DisplayValue as 'SynonymFor', 
                ObjectAsset.Object as 'SynonymForObject', 
                dbo.GenerateAssetUrl(I.ObjectAssetID) as 'Url',
                ObjectAsset.uid as Uid,
                ArtType.Name as 'SynonymForObjectType',
                P.Name as 'PredicateName' 
            from [intersect] I 
                inner join IntersectType T on T.ID = I.IntersectTypeID
                inner join Predicate P on P.ID = T.PredicateID and P.Type = {(int)PredicateType.Grammar}
                inner join [dbo].AssetDisplayValue SubjectAdv on SubjectAdv.AssetID = I.SubjectAssetID 
                inner join [dbo].AssetDisplayValue ObjectAdv on ObjectAdv.AssetID = I.ObjectAssetID 
                inner join Asset ObjectAsset on ObjectAsset.ID = I.ObjectAssetID
                inner join AssetType ArtType on I.ObjectAssetTypeID = ArtType.ID
                {where}
            Union 
            SELECT
                I.ID, 
                'O' as 'Direction', 
                SubjectAdv.DisplayValue as 'Synonym', 
                ObjectAdv.DisplayValue as 'SynonymFor', 
                ObjectAsset.Object as 'SynonymForObject', 
                dbo.GenerateAssetUrl(I.ObjectAssetID) as 'Url', 
                ObjectAsset.uid as Uid,
                ArtType.Name as 'SynonymForObjectType', 
                P.Name as 'PredicateName'
            from [intersect] I
                inner join IntersectType T on T.ID = I.IntersectTypeID 
                inner join Predicate P on P.ID = T.PredicateID and P.Type = {(int)PredicateType.Grammar} 
                inner join [dbo].AssetDisplayValue SubjectAdv on SubjectAdv.AssetID = I.ObjectAssetID
                inner join [dbo].AssetDisplayValue ObjectAdv on ObjectAdv.AssetID = I.SubjectAssetID
                inner join Asset ObjectAsset on ObjectAsset.ID = I.SubjectAssetID
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
                            Uid = o.Uid,
							DefaultPermissions = true,
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
	                        ,dbo.GenerateAssetUrl(a.ID) as 'Url'
                            ,a.uid as Uid
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
                            Uid = o.Uid,
							DefaultPermissions = true,
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
                        uid as AssetTypeUid,
						DefaultPermissions
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
                            RelativeUrl = $"assets/{o.AssetTypeUid.ToString().ToLower()}",
							AssetTypeUid = o.AssetTypeUid,
							DefaultPermissions = o.DefaultPermissions == 1,
							Fields = new Dictionary<string, string>() {
                                { "Name", o.Name },
                                { "Description", o.Description }
                            }
                        };
                    };
                    break;
            }
            return getData(context, sql, parameters, IndexMode.Basic, shaper);
        }

        private static IEnumerable<IndexObjectModel> getData(SqlConnection context, string sql, DynamicParameters parameters, IndexMode mode, Func<dynamic, IndexObjectModel> convertToDictionary, bool useTempTable = false)
        {
            IPagedQuery<FieldSqlModel> FieldQuery = null;
            PagedQuery<TagSqlModel> TagsQuery = null;
            PagedQuery<ResponsibilitySqlModel> ResponsibilityQuery = null;
			PagedQuery<ResponsibilitySqlModel> DefaultPermissionsResponsibilityQuery = null;
			PagedQuery<SemanticTypeSqlModel> SemanticQuery = null;

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
				DefaultPermissionsResponsibilityQuery = new PagedQuery<ResponsibilitySqlModel>(context, GetDefaultPermissionsResponsibilityQuery(parameters), parameters);
			}

			if(mode.HasFlag(IndexMode.WithSemantic))
			{
				SemanticQuery = new PagedQuery<SemanticTypeSqlModel>(context, GetSemanticQuery(parameters), parameters);
			}
            
            IEnumerable<IndexObjectModel> list = context.Query(sql, parameters, commandTimeout: _defaultQueryCommandTimeout, buffered: false).Select<dynamic, IndexObjectModel>(a => convertToDictionary(a));

            foreach (var item in list)
            {
                if (FieldQuery != null)
                {
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
                        { "G" , secset.Where(r => r.SecurityAsset == "G").Select(r => r.SecurityAssetID).ToList() }
                    };
                }

				if (DefaultPermissionsResponsibilityQuery != null && item.DefaultPermissions == false)
				{
					var secset = DefaultPermissionsResponsibilityQuery.GetByAssetID(item.AssetID);
					item.CanRead = new Dictionary<string, List<int>> {
						{ "R" , secset.Where(r => r.SecurityAsset == "R").Select(r => r.SecurityAssetID).ToList() },
						{ "G" , secset.Where(r => r.SecurityAsset == "G").Select(r => r.SecurityAssetID).ToList() }
					};
				}

				if (SemanticQuery != null)
				{
					var semantic = SemanticQuery.GetByAssetID(item.AssetID).FirstOrDefault();
					if(semantic != null)
					{
						item.Semantic = new IndexObjectSemanticModel { 
							Name = semantic.Name,
							Qualifier = semantic.Qualifier,
							Uid = semantic.SemanticUID
						};
					}
				}

                item.IndexFlags = mode;
                yield return item;
            }
        }

        private static string GetFieldQuery(DynamicParameters parameters)
        {
            List<string> fieldWhere = new List<string>();
            List<string> existsWhere = new List<string>();
            List<string> fieldJoin = new List<string>
            {
                "inner join FieldType FT on FT.ID = F.FieldTypeID"
            };

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

            if (existsWhere.Any())
            {
                existsWhere.Add("ATT.ID = FT.AssetTypeID");
                fieldWhere.Add($"exists (select 1 from AssetType ATT where {string.Join(Environment.NewLine + " and ", existsWhere.ToArray())})");
            }

            string fieldsSql = @"select F.AssetID, FT.Name, F.FormattedValue from Field F " +
                string.Join(" " + Environment.NewLine, fieldJoin.ToArray()) +
            " where F.FormattedValue is not null and F.FormattedValue <> '' and " +
            " FT.[Type] not in('" + string.Join("','", ExcludedFieldTypes.ToArray()) + "')";
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

		private static string GetSemanticQuery(DynamicParameters parameters)
		{
			List<string> semanticWhere = new List<string>();
			List<string> semanticJoin = new List<string>();

			if (parameters.ParameterNames.Contains("assettypeuid"))
			{
				semanticWhere.Add("att.uid = @assettypeuid");
				semanticJoin.Add("INNER JOIN [dbo].[Asset] a ON a.ID = adp.AssetID");
				semanticJoin.Add("INNER JOIN [dbo].[AssetType] att ON att.ID = a.AssetTypeID");
			}

			if (parameters.ParameterNames.Contains("assetuid"))
			{
				semanticWhere.Add("a.uid = @assetuid");
				semanticJoin.Add("INNER JOIN [dbo].[Asset] a ON a.ID = adp.AssetID");
			}

			if (parameters.ParameterNames.Contains("batchtable"))
			{
				semanticJoin.Add($"inner join {parameters.Get<string>("batchtable")} bt on adp.AssetID = bt.AssetID");
			}

			string semanticSql = @"SELECT adp.AssetID, s.Qualifier, s.Name, s.uid AS SemanticUid
				FROM [dbo].[AssetDataProfile] adp
				OUTER APPLY (
					SELECT MAX(ProfileSetDate) profileSetDate 
					FROM AssetDataProfile 
					WHERE AssetID = adp.AssetID
				) maxProfileDate
				INNER JOIN (
					SELECT ROW_NUMBER() OVER (PARTITION BY Qualifier ORDER BY EffectiveDate desc ) AS RowNum,
						Qualifier, Name, uid
					FROM Semantic
				) S on S.RowNum = 1 and adp.TypeQualifier = s.Qualifier";

			semanticWhere.Add("ADP.ProfileSetDate = maxProfileDate.profileSetDate");

			if (semanticJoin.Any())
			{
				semanticSql += " " + string.Join(" " + Environment.NewLine, semanticJoin.ToArray());
			}

			if (semanticWhere.Any())
			{
				semanticSql += " WHERE " + string.Join(" AND " + Environment.NewLine, semanticWhere.ToArray());
			}

			return semanticSql;
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
                         AND rasset.AssetTypeID = 0
						UNION ALL
						SELECT  oride.AssetID,
								oride.SecurityAsset,
								oride.SecurityAssetID
						FROM [ResponsibilityTypeRelationOverrideItem] oride
						INNER JOIN [dbo].asset a on (a.id = oride.assetID)
						INNER JOIN [dbo].assettype att on (att.id = a.assettypeid)
						INNER JOIN [dbo].[ResponsibilityTypeRelation] RR on (att.[object] = RR.[objectType] and att.objectid = RR.[Objectid] and RR.ResponsibilityTypeID = oride.ResponsibilityTypeID)
						WHERE RR.PermissionsBitMask & {(int)Permission.ReadAsset} = 0";
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
                
                if (parameters.ParameterNames.Contains("assettypeclass"))
                {
                    conditions.Add("bt.Class = @assettypeclass");
                }
            }

            if (joins.Any())
            {
                sql = $@"SELECT q.* FROM ({sql}) q {string.Join(" ", joins)}";
                if (conditions.Any())
                {
                    sql += $" WHERE {string.Join(" AND ", conditions)}";
                }
            }

            return sql;
        }

		private static string GetDefaultPermissionsResponsibilityQuery(DynamicParameters parameters)
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
						INNER JOIN AssetType att on att.id = rasset.AssetTypeID
                        INNER JOIN Asset aa ON AA.AssetTypeID = att.ID
                        WHERE rrel.PermissionsBitMask & {(int)Permission.ReadAsset} = 1
                         AND rasset.AssetID = 0
						 AND att.DefaultPermissions = 0
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
						INNER JOIN Asset aa on aa.id = rasset.AssetID
						INNER JOIN AssetType att on att.id = aa.AssetTypeID
                        WHERE rrel.PermissionsBitMask & {(int)Permission.ReadAsset} = 1
                         AND rasset.AssetTypeID = 0
						 AND att.DefaultPermissions = 0
						UNION ALL
						SELECT oride.AssetID,
							oride.SecurityAsset,
							oride.SecurityAssetID
						FROM ResponsibilityTypeRelationOverrideItem oride
							inner join [dbo].asset a on (a.id = oride.assetID)
							inner join [dbo].assettype att on (att.id = a.assettypeid)
							inner join [dbo].[ResponsibilityTypeRelation] RR on (att.[object] = RR.[objectType] and att.objectid = RR.[Objectid] and RR.ResponsibilityTypeID = oride.ResponsibilityTypeID)
						WHERE RR.PermissionsBitMask & {(int)Permission.ReadAsset} = 1
						and att.DefaultPermissions = 0
						";
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

				if (parameters.ParameterNames.Contains("assettypeclass"))
				{
					conditions.Add("bt.Class = @assettypeclass");
				}
			}

			if (joins.Any())
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
}
