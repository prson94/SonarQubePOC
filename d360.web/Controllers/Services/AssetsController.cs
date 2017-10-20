using d360.core.entities;
using d360.model;
using d360.core;
using Dapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using d360.core.exceptions;
using d360.core.enums;
using System.Collections;
using System.Text;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using System.Diagnostics;
using Newtonsoft.Json;
using System.Data.SqlClient;

namespace d360.web.Controllers.Services
{
    /// <summary>
    /// This service houses all endpoints handling glossary-related data such as artifacts and models.
    /// </summary>
    [RoutePrefix("services/assets"), Authorize]
    public class AssetsController : BaseApiController
    {
        #region DI

        public AssetsController(CommunityContext community, CompanyContext company)
            : base(community, company)
        {
        }

        #endregion

        #region Bulk Endpoints

        /// <summary>
        /// Takes a given set of assets and bulk inserts/updates them.
        /// </summary>
        /// <param name="ot">The Object Type of the asset type.</param>
        /// <param name="otid">The Object Type ID of the asset type.</param>
        /// <returns>An HTTP status code and message.</returns>
        [HttpPost, Route("{ot}/{otid:int}/bulk")]
        public async Task<HttpResponseMessage> PostBulkAssetsAsync(SystemObjects ot, int otid)
        {
            if (!Company.HasPermission(ot, otid, Claim.Update, ClaimObject.Root))
                return Request.CreateErrorResponse(HttpStatusCode.Unauthorized, "You are not allowed to add/update assets of this type.");

            var prefix = "Assets.PostBulkAssetsAsync => ";
            var errorMessage = "";

            string json = "";

            if (Request.Content.IsMimeMultipartContent())
            {
                var streamProvider = new MultipartMemoryStreamProvider();
                await Request.Content.ReadAsMultipartAsync(streamProvider);

                json = await streamProvider.Contents.Single().ReadAsStringAsync();
            }
            else
            {
                json = await Request.Content.ReadAsStringAsync();
            }

            try
            {
                var artifactType = Company.GetById<ArtifactType>(otid, i => i.Parent);

                if (artifactType == null)
                    return Request.CreateErrorResponse(HttpStatusCode.NotFound, $"Asset Type with ID {otid} could not be found.");

                var import = JsonConvert.DeserializeObject<BulkAssetImport>(json);
                var results = new List<AssetImportResult>();

                var assetTable = new System.Data.DataTable();
                var assetFieldTable = new System.Data.DataTable();

                assetTable.Columns.Add("ItemNumber", typeof(int));
                assetTable.Columns.Add("SourceID", typeof(string));
                assetTable.Columns.Add("Message", typeof(string));
                assetTable.Columns.Add("Success", typeof(bool));
                assetTable.Columns.Add("TaxonomyTypeID", typeof(int));
                assetTable.Columns.Add("ParentID", typeof(int));
                assetTable.Columns.Add("Name", typeof(string));
                assetTable.Columns.Add("Description", typeof(string));
                assetTable.Columns.Add("ArtifactID", typeof(int));
                assetTable.Columns.Add("IsNew", typeof(bool));

                assetFieldTable.Columns.Add("ItemNumber", typeof(int));
                assetFieldTable.Columns.Add("FieldName", typeof(string));
                assetFieldTable.Columns.Add("FieldValue", typeof(string));
                assetFieldTable.Columns.Add("FieldTypeID", typeof(int));

                #region Generate data sets

                #region Parent validation. Do they exist?

                var unvalidatedParents = new List<int>();
                var validatedParents = new List<int>();
                var parentArtifactTypeName = "";
                if (artifactType.ParentID.HasValue)
                {
                    for (int i = 1; i <= import.Count; i++)
                    {
                        var model = import[i - 1];
                        if (model.ContainsKey("ParentID"))
                        {
                            int pID;
                            if (int.TryParse(model["ParentID"], out pID))
                            {
                                if (!unvalidatedParents.Contains(pID))
                                {
                                    unvalidatedParents.Add(pID);
                                }
                            }
                        }
                    }

                    if (unvalidatedParents.Count > 0)
                    {
                        validatedParents = Company.Filter<Artifact>(i => i.ArtifactTypeID == artifactType.ParentID.Value && unvalidatedParents.Contains(i.ID)).Select(i => i.ID).ToList();
                    }

                    parentArtifactTypeName = artifactType.Parent.Name.ToLower();
                }

                #endregion

                for (int i = 1; i <= import.Count; i++)
                {
                    var model = import[i - 1];
                    var result = new AssetImportResult { ItemNumber = i, Message = "", Success = true };

                    if (!model.ContainsKey("Name"))
                    {
                        result.Message += "Name property not present for asset; ";
                        result.Success = false;
                    }

                    if (!model.ContainsKey("TaxonomyTypeID"))
                    {
                        result.Message += "TaxonomyTypeID property not present for asset; ";
                        result.Success = false;
                    }

                    if (artifactType.ParentID.HasValue)
                    {
                        if (model.ContainsKey("ParentID"))
                        {
                            if (!validatedParents.Contains(int.Parse(model["ParentID"])))
                            {
                                result.Message += $"Value ({model["ParentID"]}) in ParentID property does not correspond to a known {parentArtifactTypeName}.; ";
                                result.Success = false;
                            }
                        }
                        else
                        {
                            result.Message += "ParentID property not present for asset; ";
                            result.Success = false;
                        }
                    }

                    if (model.ContainsKey("SourceID"))
                    {
                        result.SourceID = model["SourceID"];
                    }

                    if (result.Success)
                    {
                        var row = assetTable.NewRow();

                        row["ItemNumber"] = result.ItemNumber;
                        row["SourceID"] = result.SourceID;
                        row["TaxonomyTypeID"] = int.Parse(model["TaxonomyTypeID"]);
                        if (model.ContainsKey("ParentID"))
                        {
                            row["ParentID"] = int.Parse(model["ParentID"]);
                        }
                        row["Name"] = model["Name"];

                        assetTable.Rows.Add(row);

                        foreach (var k in model.Keys)
                        {
                            if (k != "Name" && k != "TaxonomyTypeID" && k != "ParentID" && k != "SourceID" && k != "Description")
                            {
                                if (!string.IsNullOrEmpty(model[k]))
                                {
                                    var fieldRow = assetFieldTable.NewRow();

                                    fieldRow["ItemNumber"] = result.ItemNumber;
                                    fieldRow["FieldName"] = k.Trim();
                                    fieldRow["FieldValue"] = model[k].Trim();

                                    assetFieldTable.Rows.Add(fieldRow);
                                }
                            }
                        }
                    }

                    results.Add(result);
                }

                #endregion

                #region

                List<dynamic> retResults = null;

                if ((Company.Database.Connection as SqlConnection).State != System.Data.ConnectionState.Open)
                    (Company.Database.Connection as SqlConnection).Open();

                using (var trans = (Company.Database.Connection as SqlConnection).BeginTransaction())
                {
                    #region Asset Bulk Copy

                    Company.Database.Connection.Execute(@"
create table #AssetTable (
    ItemNumber int not null,
    SourceID nvarchar(1000) null,
    Message nvarchar(2500) null,
    Success bit null,
    TaxonomyTypeID int not null,
    ParentID int null,
    Name nvarchar(250) not null,
    Description nvarchar(max) not null,
    ArtifactID int null,
    IsNew bit null
)", transaction: trans);

                    var assetBulkCopy = new SqlBulkCopy(
                        (SqlConnection)Company.Database.Connection,
                        SqlBulkCopyOptions.Default,
                        trans);

                    assetBulkCopy.BatchSize = assetTable.Rows.Count;
                    assetBulkCopy.DestinationTableName = "#AssetTable";
                    assetBulkCopy.BulkCopyTimeout = 3600;

                    assetBulkCopy.ColumnMappings.Add("ItemNumber", "ItemNumber");
                    assetBulkCopy.ColumnMappings.Add("SourceID", "SourceID");
                    assetBulkCopy.ColumnMappings.Add("Message", "Message");
                    assetBulkCopy.ColumnMappings.Add("Success", "Success");
                    assetBulkCopy.ColumnMappings.Add("TaxonomyTypeID", "TaxonomyTypeID");
                    assetBulkCopy.ColumnMappings.Add("ParentID", "ParentID");
                    assetBulkCopy.ColumnMappings.Add("Name", "Name");
                    assetBulkCopy.ColumnMappings.Add("Description", "Description");
                    assetBulkCopy.ColumnMappings.Add("ArtifactID", "ArtifactID");
                    assetBulkCopy.ColumnMappings.Add("IsNew", "IsNew");

                    assetBulkCopy.WriteToServer(assetTable);

                    #endregion

                    #region Asset Field Bulk Copy

                    Company.Database.Connection.Execute(@"
create table #AssetFieldTable (
    ItemNumber int not null,
    FieldName nvarchar(250) not null,
    FieldValue nvarchar(max) null,
    FieldTypeID int null
)", transaction: trans);

                    var assetFieldBulkCopy = new SqlBulkCopy(
                        (SqlConnection)Company.Database.Connection,
                        SqlBulkCopyOptions.Default,
                        trans);

                    assetFieldBulkCopy.BatchSize = assetTable.Rows.Count;
                    assetFieldBulkCopy.DestinationTableName = "#AssetFieldTable";
                    assetFieldBulkCopy.BulkCopyTimeout = 3600;

                    assetBulkCopy.ColumnMappings.Add("ItemNumber", "ItemNumber");
                    assetBulkCopy.ColumnMappings.Add("FieldName", "FieldName");
                    assetBulkCopy.ColumnMappings.Add("FieldValue", "FieldValue");
                    assetBulkCopy.ColumnMappings.Add("FieldTypeID", "FieldTypeID");

                    assetFieldBulkCopy.WriteToServer(assetFieldTable);

                    #endregion

                    #region Merge into Artifact table

                    var parentOnMergeQuery = "";
                    if (artifactType.ParentID.HasValue)
                    {
                        parentOnMergeQuery = "S.ParentID = T.ParentID and ";
                    }

                    Company.Database.Connection.Execute($@"
create table #ArtifactMergeTableResult (ID int, ItemNumber int, [Action] nvarchar(10));

merge into  Artifact T
using       (
            select  *
            from    #AssetTable
            ) S
on          (
                T.ArtifactTypeID = @id and 
                ( 
                    (
                        S.TaxonomyTypeID = T.TaxonomyTypeID and 
                        {parentOnMergeQuery}
                        S.Name = T.Name and 
                        (S.SourceID is null or S.SourceID = '')
                    ) or 
                    (
                        S.SourceID is not null and 
                        S.SourceID <> '' and 
                        S.SourceID = T.SourceID
                    ) 
                )
            )
when matched then
    update set
            T.TaxonomyTypeID = S.TaxonomyTypeID,
            T.ParentID = S.ParentID,
            T.Name = S.Name,
            T.Description = S.Description,
            T.UpdatedBy = @r,
            T.UpdatedOn = getutcdate()
when not matched by target then
    insert  (ArtifactTypeID, TaxonomyTypeID, ParentID, SourceID, Name, Description, Status, CreatedOn, UpdatedBy, UpdatedOn, Visible)
    values  (@id, S.TaxonomyTypeID, S.ParentID, S.SourceID, S.Name, S.Description, 'Draft', getutcdate(), @r, getutcdate(), 1)
output inserted.ID, S.ItemNumber, $action into #ArtifactMergeTableResult;

update  T
set     T.ArtifactID = S.ID,
        T.IsNew = case when S.[Action] = 'INSERT' then 1 else 0 end
from    #AssetTable T
        inner join #ArtifactMergeTableResult S on S.ItemNumber = T.ItemNumber;
", new { id = otid, @r = Company.CurrentResourceID }, transaction: trans);

                    #endregion

                    #region Update the asset field temp table with the proper FieldTypeID

                    Company.Database.Connection.Execute(@"
update  T
set     T.FieldTypeID = S.ID
from    #AssetFieldTable T
        inner join FieldType S on S.Object = 'ArtifactType' and S.ObjectID = @id and S.Name = T.FieldName
", new { id = otid }, transaction: trans);

                    #endregion

                    #region Merge into the Field table

                    Company.Database.Connection.Execute(@"
merge into  Field T
using       (
            select  A.ArtifactID,
                    F.*
            from    #AssetFieldTable F
                    inner join #AssetTable A on A.ItemNumber = F.ItemNumber 
                        and A.ArtifactID is not null 
                        and F.FieldTypeID is not null
                    inner join FieldType FT on FT.ID = F.FieldTypeID and FT.[Type] not in ('Attribute', 'FilteredLookup', 'ComplexRelationLookup', 'DataTableSelect', 'OwnershipLookup', 'Relationship', 'FieldFromRelationship', 'RefListRelationship')
            ) S
on          (
                T.FieldTypeID = S.FieldTypeID and 
                T.ObjectType = 'Artifact' and
                T.ObjectID = S.ArtifactID
            )
when matched then
    update set
            T.Value = S.FieldValue
when not matched by target then
    insert  (FieldTypeID, ObjectType, ObjectID, Value)
    values  (S.FieldTypeID, 'Artifact', S.ArtifactID, S.FieldValue);
", new { id = otid }, transaction: trans);

                    #endregion

                    retResults = Company.Database.Connection.Query<dynamic>("select * from #AssetTable", transaction: trans).ToList();

                    trans.Commit();
                }

                return Request.CreateResponse(HttpStatusCode.OK, retResults);

                #endregion

            }
            catch (Exception ex)
            {
                errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                Trace.TraceError("{0}{1}", prefix, errorMessage);

                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, errorMessage);
            }
            finally
            {
                json = null;
            }
        }

        #endregion
    }
}
