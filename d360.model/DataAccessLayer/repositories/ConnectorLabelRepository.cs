using d360.core;
using d360.core.entities;
using d360.core.enums;
using d360.core.resources;
using d360.model.DataAccessLayer.repositories;
using Dapper;
using SpreadsheetLight;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace d360.model.DataAccessLayer
{
    public class ConnectorLabelRepository : BaseRepository, IConnectorLabelRepository
    {
        ICompanyContext companyContext;
        ICommunityContext communityContext;
        public ConnectorLabelRepository(ICompanyContext company, ICommunityContext community) : base(company)
        {
            this.companyContext = company;
            this.communityContext = community;
        }

        public bool DeleteConnectorLabels(List<ConnectorLabelApiDeleteModel> model)
        {
            IEnumerable<Guid> labelUids = model.Select(m => m.uid);

            List<ConnectorLabel> labelsToDelete = companyContext.ConnectorLabels.Where(x => labelUids.Contains(x.uid)).ToList();

            foreach (var item in model)
            {
                DeleteConnectorLabel(item.uid, item.cascade, ref labelsToDelete);
            }

            var result = companyContext.SaveChanges() > 0;
            return result;
        }

        private void DeleteConnectorLabel(Guid uid, bool cascade, ref List<ConnectorLabel> labelsToDelete)
        {
            var model = labelsToDelete.FirstOrDefault(i => i.uid == uid);
            if (model == null && model.State != State.Deleted)
                throw new Exception($"Connector Label with uid '{uid}' does not exists!");

            model.State = State.Deleted;
        }

        public async Task<ConnectorLabelApiModelWrapper> GetLabels(IEnumerable<KeyValuePair<string, string>> queryParams)
        {
            ConnectorLabelApiModelWrapper results = new ConnectorLabelApiModelWrapper();
            int pageSize = 250;
            int pageNum = 0;

            bool disablePaging = false;

            var dbArgs = new DynamicParameters();

            List<string> queryFilters = new List<string>();

            dbArgs.Add("@state", State.Active);
            queryFilters.Add($"t.[state] = @state");


            if (queryParams.ToList().Any(q => q.Key.ToLower() == "uid"))
            {
                Guid uid = new Guid();

                var tagUidString = queryParams.ToList().FirstOrDefault(q => q.Key.ToLower() == "uid").Value;
                if (Guid.TryParse(tagUidString, out uid))
                {
                    dbArgs.Add("@uid", uid);
                    queryFilters.Add($"t.[UID] = @uid");
                }
            }

            if (queryParams.ToList().Any(q => q.Key.ToLower() == "_pagesize"))
            {

                if (int.TryParse(queryParams.ToList().FirstOrDefault(q => q.Key.ToLower() == "_pagesize").Value, out pageSize))
                {
                    if (pageSize < 1) pageSize = 1;
                }
                if (pageSize > 250)
                {
                    pageSize = 250; // max page size is 250 people.
                }
            }

            if (queryParams.ToList().Any(q => q.Key.ToLower() == "_pagenum"))
            {
                if (int.TryParse(queryParams.ToList().FirstOrDefault(q => q.Key.ToLower() == "_pagenum").Value, out pageNum))
                {
                    if (pageNum < 1) pageNum = 1;
                }
            }

            string whereClause = $"WHERE t.State = 1";
            if (queryFilters.Count > 0)
            {
                whereClause += $" and ({string.Join(" AND ", queryFilters)})";
            }

            var sql = $@"drop table if exists #labelUidMap
                        create table #labelUidMap(
	                        uid uniqueidentifier
                        )

                        insert into #labelUidMap
                        select LabelUid from ProcessExpandedData
                        where LabelUid is not null

                        select 
                        Labels.count as UseCount,
                        t.uid,
                        t.Value,
                        t.CreatedOn,
                        grc.uid as CreatedByUid,
                        t.UpdatedOn,
                        gru.uid as UpdatedByUid
                        from ConnectorLabel t
                          left join reporting.Global_Resource grc on t.CreatedBy = grc.ResourceID
                          left join reporting.Global_Resource gru on t.UpdatedBy = gru.ResourceID
                          cross apply (select count(*) from #labelUidMap where uid = t.uid)Labels (count)
                        {whereClause}";

            var countSql = @"select count(*)
                            from ConnectorLabel";

            sql += " order by [ID] ASC"; // admin screen will most likely order results however it sees fit

            if (pageSize < 1) pageSize = 1;
            if (pageNum < 1) pageNum = 1;

            if (!disablePaging)
                sql += $" offset {pageSize * (pageNum - 1)} rows fetch next {pageSize} rows only";

            results.pageNum = pageNum;
            results.pageSize = pageSize;
            results.total = (await companyContext.QueryAsync<int>(countSql, dbArgs, ApiTimeout)).FirstOrDefault();

            if (results.total > 0)
            {
                results.items = (await companyContext.QueryAsync<ConnectorLabelApiModel>(sql, dbArgs, ApiTimeout));
            }

            return results;
        }

        public ConnectorLabelApiModel CreateConnectorLabel(ConnectorLabelPostModel model)
        {
            var result = new ConnectorLabelApiModel();
            result.Value = model.Value;

            var label = companyContext.ConnectorLabels.FirstOrDefault(x => x.Value.ToLower() == model.Value.ToLower() && x.State == State.Deleted);

            if (label == null)
            {
                label = new ConnectorLabel { Value = model.Value };
                companyContext.Add(label);
            }
            else
            {
                label.State = State.Active;
                label.CreatedBy = label.UpdatedBy = companyContext.CurrentResourceID;
                label.CreatedOn = label.UpdatedOn = DateTime.UtcNow;
                companyContext.SaveChanges();
            }


            var user = companyContext.GlobalReportingResources.FirstOrDefault(x => x.ResourceID == companyContext.CurrentResourceID);

            result.uid = label.uid;
            result.UpdatedOn = label.UpdatedOn.GetValueOrDefault();
            result.UpdatedByUid = user.Uid;
            result.CreatedOn = label.CreatedOn.GetValueOrDefault();
            result.CreatedByUid = user.Uid;

            return result;
        }

        public ConnectorLabelApiModel UpdateConnectorLabel(Guid uid, ConnectorLabelPostModel model, ConnectorLabel existingLabel)
        {
            var result = new ConnectorLabelApiModel();
            existingLabel.Value = model.Value;
            companyContext.Update(existingLabel);

            result.Value = model.Value;
            result.uid = existingLabel.uid;
            result.UpdatedOn = existingLabel.UpdatedOn.GetValueOrDefault();
            result.CreatedOn = existingLabel.CreatedOn.GetValueOrDefault();
            result.UseCount = companyContext.Query<int>
                ("select count(*) from ProcessExpandedData where LabelUid = @uid",
                new DynamicParameters(new { existingLabel.uid })).FirstOrDefault();

            var createUser = companyContext.GlobalReportingResources.FirstOrDefault(x => x.ResourceID == existingLabel.CreatedBy);
            if (createUser != null)
            {
                result.CreatedByUid = createUser.Uid;
            }
            var updateUser = companyContext.GlobalReportingResources.First(x => x.ResourceID == companyContext.CurrentResourceID);
            if (updateUser != null)
            {
                result.UpdatedByUid = updateUser.Uid;
            }


            return result;
        }

        public bool DoesLabelExists(Guid uid)
        {
            return companyContext.ConnectorLabels.Any(x => x.uid == uid);
        }

        public bool DoesLabelExists(string value)
        {
            return companyContext.ConnectorLabels.Any(x => x.Value == value && x.State == State.Active);
        }

        public bool DoesLabelExists(Guid existingUid, ConnectorLabelPostModel model)
        {
            return companyContext.ConnectorLabels.Any(x => x.Value == model.Value && x.uid != existingUid && x.State == State.Active);
        }

        public async Task<dynamic> GetConnectorLabelsForExcel(IEnumerable<KeyValuePair<string, string>> queryParams)
        {

            var dbArgs = new DynamicParameters();
            List<string> whereClauses = new List<string>();
            string sortField = "";
            string sortOrder = "";
            string whereOperater = " and ";
            int useCount = 0;

            foreach (var qitem in queryParams.Where(x => !string.IsNullOrEmpty(x.Value)))
            {
                switch (qitem.Key.ToLower())
                {
                    case "globalsearch":
                        dbArgs.Add("value", $"%{qitem.Value.ToLower()}%");
                        whereClauses.Add("LOWER(t.Value) like @value");
                        whereClauses.Add("STR(Labels.count) like @value");

                        whereOperater = " or ";

                        break;
                    case "value":
                        dbArgs.Add("value", $"%{qitem.Value.ToLower()}%");
                        whereClauses.Add("LOWER(t.Value) like @value");

                        break;
                    case "usecount":
                        if (int.TryParse(qitem.Value, out useCount))
                        {
                            dbArgs.Add("useCount", $"%{qitem.Value.ToLower()}%");
                            whereClauses.Add("STR(Labels.count) like @useCount");
                        }

                        break;
                    case "sortby":
                        if (qitem.Value.ToLower() == "usecount") sortField = "usecount";
                        if (qitem.Value.ToLower() == "value") sortField = "t.value";
                        break;
                    case "sortorder":
                        int val = int.Parse(qitem.Value);
                        if (val >= 0) sortOrder = "ASC";
                        else
                        {
                            sortOrder = "DESC";
                        }
                        break;
                }
            }

            string sortClause = $"ORDER BY {sortField} {sortOrder}";

            string whereClause = $"WHERE t.State = 1";
            if (whereClauses.Count > 0)
            {
                whereClause += $" and ({string.Join(whereOperater, whereClauses)})";
            }
            var sql = $@"drop table if exists #labelUidMap
                        create table #labelUidMap(
	                        uid uniqueidentifier
                        )

                        insert into #labelUidMap
                        select LabelUid from ProcessExpandedData
                        where LabelUid is not null

                        select 
                        Labels.count as UseCount,
                        t.uid,
                        t.Value,
                        t.CreatedOn,
                        grc.FirstName + ' ' +grc.LastName as CreatedBy,
                        t.UpdatedOn,
                        gru.FirstName + ' ' +gru.LastName as UpdatedBy
                        from ConnectorLabel t
                          left join reporting.Global_Resource grc on t.CreatedBy = grc.ResourceID
                          left join reporting.Global_Resource gru on t.UpdatedBy = gru.ResourceID
                          cross apply (select count(*) from #labelUidMap where uid = t.uid)Labels (count)
                        {whereClause}
                        {sortClause}";

            return await companyContext.QueryAsync<dynamic>(sql, dbArgs, ApiTimeout);

        }

        public IEnumerable<dynamic> GetConnectorLabelUsage(Guid labelUid, IEnumerable<KeyValuePair<string, string>> queryParams)
        {
            var dbArgs = new DynamicParameters();
            List<string> whereClauses = new List<string>();
            string sortField = "";
            string sortOrder = "";
            string whereOperater = " and ";
            int useCount = 0;
            dbArgs.Add("labelUid", labelUid);
            foreach (var qitem in queryParams.Where(x => !string.IsNullOrEmpty(x.Value)))
            {
                switch (qitem.Key.ToLower())
                {
                    case "globalsearch":
                        dbArgs.Add("global", $"%{qitem.Value.ToLower()}%");
                        whereClauses.Add("Path.Diagram like @global");
                        whereClauses.Add("Type.AssetTypeName like @global");
                        whereClauses.Add("STR(Count) like @global");

                        whereOperater = " or ";

                        break;
                    case "diagram":
                        if (!string.IsNullOrEmpty(qitem.Value))
                        {
                            dbArgs.Add("diagram", $"%{qitem.Value.ToLower()}%");
                            whereClauses.Add("Path.Diagram like @diagram");
                        }

                        break;
                    case "occurrences":
                        if (int.TryParse(qitem.Value, out useCount))
                        {
                            dbArgs.Add("occurrences", $"%{qitem.Value.ToLower()}%");
                            whereClauses.Add("STR(Count) like @occurrences");
                        }

                        break;
                    case "assettypename":
                        if (!string.IsNullOrEmpty(qitem.Value))
                        {
                            dbArgs.Add("assettypename", $"%{qitem.Value.ToLower()}%");
                            whereClauses.Add("Type.AssetTypeName like @assettypename");
                        }
                        break;
                    case "sortby":
                        if (qitem.Value.ToLower() == "diagram") sortField = "Path.Diagram";
                        if (qitem.Value.ToLower() == "assettypename") sortField = "Type.AssetTypeName";
                        if (qitem.Value.ToLower() == "occurrences") sortField = "count";
                        break;
                    case "sortorder":
                        int val = int.Parse(qitem.Value);
                        if (val >= 0) sortOrder = "ASC";
                        else sortOrder = "DESC";
                        break;
                }
            }

            string sortClause = !string.IsNullOrEmpty(sortField) ? $"ORDER BY {sortField} {sortOrder}" : "";

            string whereClause = $"";
            if (whereClauses.Count > 0)
            {
                whereClause += $"where ({string.Join(whereOperater, whereClauses)})";
            }

            var labelsSql = $@";with usage as(
                    select DiagramAssetUid, count(*) as count from dbo.processexpandeddata ped
                    where ped.labeluid = @labelUid
                    group by ped.DiagramAssetUid)
                    select
                    Path.Diagram,
                    Count as Occurrences,
                    u.diagramassetuid as AssetUid,
                    an.ID as AssetId,
                    'asset/' + lower(cast(an.uid as nvarchar(36))) as url,
					Type.AssetTypeName,
                    a.Object,
                    a.ObjectID
                    from usage u
                    inner join graph.assetnode an on an.uid = u.diagramassetuid
                    inner join asset a on a.uid = an.uid
                    inner join assettype ast on a.assettypeid = ast.id
                    cross apply (select graph.GetPath(an.Segments, ' > ', ' / ') as Diagram)Path
                    cross apply (
					    select 
					        CASE 
							    WHEN AST.Object = 'TaxonomyType' THEN '{CommonNames.AssetTypeClass_Model.CleanForSql()}' + ' > ' +  AST.Name
							    WHEN AST.Object = 'ArtifactType' and AST.[Class] = 1 THEN '{CommonNames.AssetTypeClass_Business.CleanForSql()}'+  ' > ' + AST.Name
                                WHEN AST.Object = 'ArtifactType' and AST.[Class] = 8 THEN '{CommonNames.AssetTypeClass_Technical.CleanForSql()}'+  ' > ' + AST.Name
							    WHEN AST.Object = 'PolicyType' THEN '{CommonNames.AssetTypeClass_Policy.CleanForSql()}'+  ' > ' + AST.Name
							    WHEN AST.Object = 'RuleType' THEN '{CommonNames.AssetTypeClass_Rule.CleanForSql()}'+  ' > ' + AST.Name
							    ELSE ''+ AST.Name
						   END AS AssetTypeName)Type
                    {whereClause}
                    {sortClause}";

            var response = companyContext.Query<dynamic>(labelsSql, dbArgs, ApiTimeout);
            return response;
        }

        public (byte[], string) GetExcelFromConnectorLabelUsage(ConnectorLabel label, IEnumerable<dynamic> response)
        {
            var fileName = $"Where Used report for Connector Label '{label.Value}'";
            var fields = new List<FieldType>();
            fields.Add(new FieldType { Type = "string", Name = "Diagram", FriendlyName = "Diagram" });
            fields.Add(new FieldType { Type = "string", Name = "AssetTypeName", FriendlyName = "Asset Type" });
            fields.Add(new FieldType { Type = "string", Name = "Occurrences", FriendlyName = "Occurrences" });
            fields.Add(new FieldType { Type = "string", Name = "AssetUid", FriendlyName = "UID" });
            fields.Add(new FieldType { Type = "string", Name = "AssetId", FriendlyName = "Asset ID" });
            fields.Add(new FieldType { Type = "string", Name = "url", FriendlyName = "URL" });

            var document = new SLDocument();
            const string sheetName = "Where Used";


            #region Populate Excel Document

            document.RenameWorksheet(SLDocument.DefaultFirstSheetName, sheetName);
            int index = 1;

            foreach (var field in fields)
            {
                document.SetCellValue(1, index++, field.FriendlyName);
            }

            int rowNumber = 1;

            foreach (var row in response)
            {
                index = 1;
                rowNumber++;
                var rowValues = (row as IDictionary<string, object>);
                foreach (var field in fields)
                {
                    if (rowValues.ContainsKey(field.Name))
                    {
                        var val = rowValues[field.Name];
                        setCellValueFromField(document, rowNumber, index, field, val);
                    }
                    index++;
                }
            }

            #endregion
            var stream = new MemoryStream();
            document.SaveAs(stream);
            return (stream.ToArray(), fileName);
        }

        public bool IsAuthorizedToEditConnectorLabel(Guid connectorLabelUid)
        {
            var connectorLabel = companyContext.ConnectorLabels.FirstOrDefault(x => x.uid == connectorLabelUid);
            if (connectorLabel == null) return false;
            if (companyContext.CurrentResourceIsAdmin || companyContext.CurrentResourceID == connectorLabel.CreatedBy) return true;
            return false;
        }
    }
}