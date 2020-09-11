using AngleSharp.Io;
using d360.core;
using d360.core.entities;
using d360.core.enums;
using d360.extensions;
using d360.model;
using d360.model.DataAccessLayer;
using d360.web.Filters;
using d360.web.Models;
using Dapper;
using Microsoft.Web.Http;
using Resources;
using Swashbuckle.Swagger.Annotations;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;

namespace d360.web.Controllers.V2
{
    /// <summary>
    /// This service houses all endpoints handling issue management in Govern.
    /// </summary>
    [
        ApiVersion("2.0"),
        RoutePrefix("api/v{version:apiVersion}/issues"),
        Authorize
    ]
    public class IssuesController : BaseV2ApiController
    {

        #region DI

        IQueueSource QueueSource;
        IStorageProvider Storage;
        IAssetRepository AssetRepository;

        public IssuesController(ICommunityContext community, ICompanyContext company, IStorageProvider storage, IQueueSource queueSource, IAssetRepository repository)
            : base(community, company)
        {
            QueueSource = queueSource;
            Storage = storage;
            this.AssetRepository = repository;
        }

        #endregion

        /// <summary>
        /// Create an action
        /// </summary>        
        /// <param name="actionTypeUid">The Uid of the action type</param>
        /// <param name="models">Collection of Issues/Actions</param>
        /// <returns>Response with the uid of the action created.</returns>
        [
            HttpPost,
            Route("{ActionTypeUid:Guid}"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "Response containing the uid of the action created", typeof(List<ApiStatusResponse>)),
            SwaggerResponse(HttpStatusCode.BadRequest, "Invalid request parameters provided.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.NotFound, "Invalid request parameters provided.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.Forbidden, "Insufficient permissions for this request.", typeof(ErrorResponse)),
        ]
        public async Task<IHttpActionResult> CreateAction(Guid actionTypeUid, List<ActionUpsertRequest> models)
        {
            bool isWriteActionDescriptionEnabled = IsWriteActionDescriptionEnabled();

            List<ApiStatusResponse> response = new List<ApiStatusResponse>();

            List <IssueInsertModel> issueModels = new List<IssueInsertModel>();

            if (actionTypeUid == null || actionTypeUid== Guid.Empty)
            {
                return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Invalid Request", "Invalid ActionTypeUid provided."));
            }

            var issueType = Company.Filter<IssueType>(i => i.uid == actionTypeUid).SingleOrDefault();

            if (issueType == null)
            {
                return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Not Found", $"Action Type with Uid {actionTypeUid} could not be found.."));
            }

            WorkHttpStatus validationStatus =  PopulateRequest(models, ref issueModels, issueType);
            if (validationStatus.StatusCode != HttpStatusCode.OK)
            {
                return await Task.FromResult(errorMessageResponse(validationStatus.StatusCode, validationStatus.Error, validationStatus.Message));
            }
                
            foreach (var issueModel in issueModels)
            {               

                if (isWriteActionDescriptionEnabled)
                {
                    var relations = new List<CommentRelation>();
                    var comment = new Comment();

                    relations.Add(new CommentRelation { ObjectID = Company.CurrentResourceID, ObjectType = SystemObjects.Resource.ToString(), Date = DateTime.UtcNow });

                    comment.OwnerObjectType = SystemObjects.Resource.ToString();
                    comment.OwnerObjectID = Company.CurrentResourceID;
                    comment.CommentTypeID = CommentType.Issue;
                    comment.Body = issueModel.Comment ?? $"New {issueType.Name} Raised.";

                    //add relation to current artifact
                    relations.Add(new CommentRelation { ObjectType = issueModel.Issue.Object, ObjectID = issueModel.Issue.ObjectID, Date = DateTime.UtcNow });

                    var dtl = Company.AddComment(comment, relations).FirstOrDefault(i => i.ID == comment.ID);

                    issueModel.Issue.CommentID = dtl.ID;
                }

                var insertSQL = $@"INSERT INTO [dbo].[Issue]
                                               ([IssueTypeID]
                                               ,[Object]
                                               ,[ObjectID]
                                               ,[ObjectType]
                                               ,[ObjectTypeID]
                                               ,[CreatedOn]
                                               ,[CreatedBy]
                                               ,[UpdatedOn]
                                               ,[UpdatedBy]
                                               ,[CommentID])
                                        OUTPUT inserted.Uid, inserted.ID
                                           VALUES
                                               (@issueTypeID
                                               ,@object
                                               ,@objectID
                                               ,@objectType
                                               ,@objectTypeID
                                               ,GETDATE()
                                               ,@userId
                                               ,GETDATE()
                                               ,@userId
                                               ,@commentId)";

                var res = await Company.Database.Connection.QueryAsync<(Guid uid, int id)>(insertSQL, new { issueTypeID = issueType.ID, @object = issueModel.Issue.Object, objectID = issueModel.Issue.ObjectID, objectType = issueModel.Issue.ObjectType, objectTypeID = issueModel.Issue.ObjectTypeID, userId = Company.CurrentResourceID, commentId = issueModel.Issue.CommentID});

                issueModel.Issue.ID = res.FirstOrDefault().id;
                issueModel.Issue.UID = res.FirstOrDefault().uid;

                if (issueModel.fields != null && issueModel.fields.Count > 0)
                {
                    issueModel.fields.ForEach(i =>
                    {
                        i.ObjectID = issueModel.Issue.ID;
                    });
                    Company.AddOrUpdateFields(issueModel.fields);
                }                

                response.Add( new ApiStatusResponse { Uid = issueModel.Issue.UID.Value, Message ="Action Created", Success = true});               
            }

            Company.CreateEventsForAddedActions(issueModels.Select(x => x.Issue).ToList());

            return ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, response));             
        }

        private WorkHttpStatus validateRequest(IssueType issueType, ActionUpsertRequest model, Asset asset)
        {
            if (model.AssetUid == null || model.AssetUid == Guid.Empty)
            {
                return new WorkHttpStatus(HttpStatusCode.BadRequest, "Invalid Request", "Invalid AssetUid provided.");
            }

            if (asset == null)
                return new WorkHttpStatus(HttpStatusCode.NotFound, "Not found", $"Asset with Uid {model.AssetUid} could not be found.");            

            if (!Company.HasAssetPermission(asset.ID, Permission.ReadAsset))
                return new WorkHttpStatus(HttpStatusCode.Forbidden, ApiMessages.EndpointNotAuthorizedHeading, "You are not allowed to add actions on this asset.");

            var allocations = Company.Filter<IssueTypeRelation>(r => r.IssueTypeID == issueType.ID).ToList();

            if (allocations.Count > 0 && !allocations.Any(a => a.AssetTypeID == asset.AssetTypeID))
            {
                return new WorkHttpStatus(HttpStatusCode.NotFound, "Not found", $"Allocation does not exist for Asset Type '{asset.AssetType.Name}' on Action Type '{issueType.Name}'.");
            }            

            var fieldTypes = Company.Filter<FieldType>(ft => ft.ObjectID == issueType.ID);

            var fieldTable = new DataTable();
            fieldTable.Columns.Add("ExecutionID", typeof(Guid));
            fieldTable.Columns.Add("ItemNumber", typeof(int));
            fieldTable.Columns.Add("FieldName", typeof(string));
            fieldTable.Columns.Add("FieldValue", typeof(string));
            fieldTable.Columns.Add("FieldTypeID", typeof(int));

            Company.ValidateFields("IssueType", issueType.ID, true, fieldTypes.ToList(), fieldTypes.Where(f => f.IsRequired && string.IsNullOrEmpty(f.DefaultValue)).Select(f => f.Name).ToList(), model.Fields, Guid.Empty, 1, fieldTable, out bool success, out string errorMessage);            

            if(!success)
            {
                return new WorkHttpStatus(HttpStatusCode.BadRequest, "Invalid Request", errorMessage);
            }            

            return new WorkHttpStatus(HttpStatusCode.OK, "", "");
        }

        private WorkHttpStatus PopulateRequest(List<ActionUpsertRequest> models, ref List<IssueInsertModel> issues, IssueType issueType)
        {
            foreach (var model in models)
            {
                Asset asset = AssetRepository.GetAssetByUID(model.AssetUid);

                var validationStatus =  validateRequest(issueType, model, asset);

                if(validationStatus.StatusCode != HttpStatusCode.OK)
                {
                    return validationStatus;
                }                

                var issue = new Issue
                {
                    CreatedBy = Company.CurrentResourceID,
                    CreatedOn = DateTime.UtcNow,
                    UpdatedBy = Company.CurrentResourceID,
                    UpdatedOn = DateTime.UtcNow,
                    IssueTypeID = issueType.ID,
                    Object = asset.Object,
                    ObjectID = asset.ObjectID,
                    ObjectType = asset.AssetType.Object,
                    ObjectTypeID = asset.AssetType.ObjectID,
                    CommentID = 0
                };                

                var fields = new FieldLoader().GetFormDynamicFieldValues(SystemObjects.Issue, issue.ID, Company.GetFieldTypesByObject(SystemObjects.IssueType, issueType.ID).ToList(), model.Fields, null);

                issues.Add(new IssueInsertModel { Issue = issue, fields = fields, Comment = model.Fields.ContainsKey("ProblemDesc") ? model.Fields["ProblemDesc"] : null});
            }            

            return new WorkHttpStatus(HttpStatusCode.OK, "", "");
        }

        private bool IsWriteActionDescriptionEnabled()
        {
            var setting = Community.Filter<CompanySetting>(i => i.CompanyID == Company.CurrentCompanyID && i.SettingID == 61).SingleOrDefault();
            if (setting == null)
                return true;
            else
                return bool.Parse(setting.Value);
             
        }
        
    }
    public class IssueInsertModel
    {
        public Issue Issue { get; set; }
        public List<Field> fields = new List<Field>();
        public string Comment { get; set; }
    }
}
