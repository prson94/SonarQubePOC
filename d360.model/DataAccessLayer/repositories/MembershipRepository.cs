using d360.core.entities.Membership;
using Dapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using d360.core.enums;
using d360.core.resources;
using System.Net;
using System.Data;
using System.Text.RegularExpressions;
using System.Data.SqlClient;
using d360.core.entities;
using d360.core;
using d360.model.DataAccessLayer.repositories;
using d360.model.helpers;
using Newtonsoft.Json.Linq;
using d360.model.helpers.filters;
using d360.core.helpers;
using d360.core.queue;
using d360.extensions;

namespace d360.model.DataAccessLayer
{
    public class MembershipRepository : BaseRepository, IMembershipRepository
    {
        internal ICompanyContext CompanyContext;
        internal ICommunityContext CommunityContext;
        internal IAssetRepository AssetRepository;
        internal IQueueSource QueueSource;
        internal IStorageProvider StorageProvider;

        public MembershipRepository(ICompanyContext companyContext, ICommunityContext communityContext, IAssetRepository assetRepository, IQueueSource queueSource, IStorageProvider storageProvider)
            : base(companyContext)
        {
            CompanyContext = companyContext;
            CommunityContext = communityContext;
            AssetRepository = assetRepository;
            QueueSource = queueSource;
            StorageProvider = storageProvider;
        }
        public async Task<GroupApiModels> GetGroups(IEnumerable<KeyValuePair<string, string>> queryParams)
        {
            var dbArgs = new DynamicParameters();
            List<string> condition = new List<string>();
            string resourceString = "";
            if (queryParams != null)
            {
                if (queryParams.ToList().Any(q => q.Key.ToLower() == "uid"))
                {
                    Guid uid;
                    var uidString = queryParams.ToList().FirstOrDefault(q => q.Key.ToLower() == "uid").Value;
                    if (Guid.TryParse(uidString, out uid))
                    {
                        if (uid != Guid.Empty)
                        {
                            condition.Add("A.Uid = @Uid");
                            dbArgs.Add("uid", uid);
                        }

                    }
                }

                if (queryParams.ToList().Any(q => q.Key.ToLower() == "name"))
                {

                    var name = queryParams.ToList().FirstOrDefault(q => q.Key.ToLower() == "name").Value.Trim();
                    if (!string.IsNullOrEmpty(name))
                    {

                        condition.Add("G.Name like  @name");
                        dbArgs.Add("name", name + '%');
                    }
                }

                if (queryParams.ToList().Any(q => q.Key.ToLower() == "resourceuid"))
                {

                    var user = queryParams.ToList().FirstOrDefault(q => q.Key.ToLower() == "resourceuid").Value.Trim();
                    if (!string.IsNullOrEmpty(user))
                    {
                        resourceString = @"left join Asset U on U.[uid] = @user
                                        left join[dbo].[ResourceGroup] RG on RG.[ResourceID] = U.ObjectID ";
                        condition.Add("RG.[GroupID] = G.ID");
                        dbArgs.Add("user", user);
                    }
                }

            }

            var whereStatements = condition.Count != 0 ? $" where  {string.Join(" and ", condition)}" : "";
            var sql = $@"
                   Select 
                       A.Uid,
                       G.Name,
                       G.Description,
                       gr1.uid as PrimaryOwnerUid,
                       gr2.uid as SecondaryOwnerUid,
                       G.IsActiveDirectoryGroup 
                       from [Group] G
                           inner join Asset A on A.[Object]='Group' and A.ObjectID = G.ID
                           left join [reporting].[Global_Resource] gr1 on gr1.ResourceID = G.PrimaryOwnerResourceID
                           left join [reporting].[Global_Resource] gr2 on gr2.ResourceID = G.SecondaryOwnerResourceID
                           {resourceString} 
                           {whereStatements}  
                           order by G.Name  ";

            var countSql = $@"Select count(*) from [Group] G
            inner join Asset A on A.[Object]='Group' and A.ObjectID = G.ID
            left join [reporting].[Global_Resource] gr1 on gr1.ResourceID = G.PrimaryOwnerResourceID
            left join [reporting].[Global_Resource] gr2 on gr2.ResourceID = G.SecondaryOwnerResourceID
                {resourceString} 
                {whereStatements}  ";

            var countResults = await CompanyContext.QueryAsync<int>(countSql, dbArgs, ApiTimeout);
            var count = countResults.First();

            var results = await this.CompanyContext.QueryAsync<GroupApiModel>(sql, dbArgs, ApiTimeout);

            return new GroupApiModels() { items = results, Total = count };

        }
        public WorkHttpStatus DeleteResources(ApiExecution execution, IEnumerable<UserApiDeleteModel> resources)
        {

            try
            {
                List<UserApiDeleteModel> models = new List<UserApiDeleteModel>();
                foreach (var model in resources)
                {
                    model.Resource = CompanyContext.GlobalReportingResources.SingleOrDefault(r => r.Uid == model.Uid && r.State != CompanyResourceState.Deleted);

                    if (model.Resource == null)
                    {
                        return new WorkHttpStatus(HttpStatusCode.NotFound, AssetTypeErrors.NotFound, string.Format(MemberShipErrors.UserUidNotFound, model.Uid));
                    }

                    if (model.Resource.ResourceID < 1)
                    {
                        return new WorkHttpStatus(HttpStatusCode.BadRequest, AssetTypeErrors.InvalidUser, string.Format(MemberShipErrors.UserUidSystemUser, model.Uid));
                    }

                    model.CompanyResource = CommunityContext.CompanyResources.SingleOrDefault(r => r.CompanyID == CompanyContext.CurrentCompanyID && r.ResourceID == model.Resource.ResourceID && r.State != CompanyResourceState.Deleted);

                    if (model.CompanyResource == null)
                    {
                        return new WorkHttpStatus(HttpStatusCode.NotFound, AssetTypeErrors.NotFound, string.Format(MemberShipErrors.UserUidNotFound, model.Uid));
                    }
                }

                CompanyContext.Add(execution);
                CompanyContext.SetApiExecutionProcessingStartTime(execution.ExecutionID);


                foreach (var model in resources)
                {
                    model.Resource.State = CompanyResourceState.Deleted;
                    model.CompanyResource.State = CompanyResourceState.Deleted;

                    CompanyContext.Update(model.Resource);
                    CommunityContext.Update(model.CompanyResource);

                    CompanyContext.Query<int>($@"insert into reporting.Global_Audit (Object, ObjectID, ObjectName, ResourceID, Date, Action, ActionObject, ActionObjectID, ActionObjectTypeName, ActionObjectName, ActionDescription)
	                    select	distinct
			                    'Resource', 
			                    res.ResourceId,
			                    SUBSTRING(res.FirstName + ' ' +res.LastName,1,250),
			                    @r, 
			                    getutcdate(), 
			                    'Deleted', 
			                    'Resource', 
			                    res.ResourceId,
			                    'Resource', 
			                    SUBSTRING(res.FirstName + ' ' +res.LastName,1,250),
			                    'This user has been removed.'
	                    from reporting.Global_Resource res
	                    where res.resourceid = @resourceId", new
                    {
                        r = CompanyContext.CurrentResourceID,
                        resourceId = model.Resource.ResourceID
                    }).ToList();
                }

                execution.Processed = resources.Count();
                execution.CompletedOn = DateTime.UtcNow;
                CompanyContext.Update(execution);

            }
            catch (Exception ex)
            {
                string message = ex.GetFullExceptionData(false, constants.ERROR_MESSAGE_CHARACTER_LIMIT);
                execution.ErrorMessage = message;
                execution.CompletedOn = DateTime.UtcNow;
                CompanyContext.Update(execution);

                return new WorkHttpStatus(HttpStatusCode.InternalServerError, AssetTypeErrors.InternalServerError, MemberShipErrors.InternalServerErrorMsg);
            }

            return new WorkHttpStatus(HttpStatusCode.OK, AssetTypeErrors.Success, MemberShipErrors.UserDeletedMessage);
        }
        public async Task<IEnumerable<UserApiUpsertResult>> UpsertUsers(ApiExecution execution, IEnumerable<IUserApiUpsertModel> users, bool lookupFieldsPassedByValue = false, bool isInsert = false, bool IsChangePasswordReqeust = false)
        {
            CompanyContext.Add(execution);
            IEnumerable<UserApiUpsertResult> results;
            try
            {
                results = await ProcessUpsertUsers(execution, users, lookupFieldsPassedByValue, isInsert, IsChangePasswordReqeust).ConfigureAwait(false);

            }
            catch (Exception ex)
            {
                string message = ex.GetFullExceptionData(false, constants.ERROR_MESSAGE_CHARACTER_LIMIT);
                execution.ErrorMessage = message;
                execution.CompletedOn = DateTime.UtcNow;
                CompanyContext.Update(execution);
                throw ex;
            }
            execution.CompletedOn = DateTime.UtcNow;
            execution.Error = results.Count(r => r.Success == false);
            execution.Processed = results.Count(r => r.Success == true);
            CompanyContext.Update(execution);
            return results;

        }

        public async Task<IEnumerable<UserApiUpsertResult>> ProcessUpsertUsers(ApiExecution execution, IEnumerable<IUserApiUpsertModel> users, bool lookupFieldsPassedByValue = false, bool isInsert = false, bool IsChangePasswordReqeust = false)
        {
            const int ResourceTypeID = 1;

            var executionID = execution.ExecutionID;
            var results = new List<UserApiUpsertResult>();
            var validationResults = new List<UserApiUpsertResult>();

            var fieldTypes = CompanyContext.FieldTypes.Where(f => f.Object == "ResourceType").ToList();

            var hasRelationshipFieldTypes = fieldTypes.Any(f => f.Type == DataType.Relationship.ToString());


            #region Data Tables

            var resourceTable = new DataTable();
            var userTable = new DataTable();
            var fieldTable = new DataTable();

            resourceTable.Columns.Add("ExecutionID", typeof(Guid));
            resourceTable.Columns.Add("ItemNumber", typeof(int));
            resourceTable.Columns.Add("ResourceID", typeof(int));
            resourceTable.Columns.Add("Username", typeof(string));
            resourceTable.Columns.Add("uid", typeof(Guid));


            userTable.Columns.Add("ExecutionID", typeof(Guid));
            userTable.Columns.Add("Uid", typeof(Guid));
            userTable.Columns.Add("ResourceID", typeof(int));

            userTable.Columns.Add("ExecutionItemUid", typeof(Guid));
            userTable.Columns.Add("ItemNumber", typeof(int));
            userTable.Columns.Add("Username", typeof(string));
            userTable.Columns.Add("FirstName", typeof(string));
            userTable.Columns.Add("LastName", typeof(string));
            userTable.Columns.Add("Password", typeof(string));
            userTable.Columns.Add("State", typeof(int));
            userTable.Columns.Add("IsAdministrator", typeof(bool));
            userTable.Columns.Add("IsNew", typeof(bool));
            userTable.Columns.Add("Success", typeof(bool));
            userTable.Columns.Add("Message", typeof(string));
            userTable.Columns.Add("Object", typeof(string));
            userTable.Columns.Add("ObjectID", typeof(int));
            userTable.Columns.Add("ObjectType", typeof(string));
            userTable.Columns.Add("ObjectTypeID", typeof(int));


            fieldTable.Columns.Add("ExecutionID", typeof(Guid));
            fieldTable.Columns.Add("ItemNumber", typeof(int));
            fieldTable.Columns.Add("FieldName", typeof(string));
            fieldTable.Columns.Add("FieldValue", typeof(string));
            fieldTable.Columns.Add("FieldTypeID", typeof(int));
            fieldTable.Columns.Add("LookupValue", typeof(string));

            #endregion

            #region Process Community

            int itemNumber = 0;
            foreach (var user in users)
            {
                itemNumber++;
                user.ItemNumber = itemNumber;

                var row = resourceTable.NewRow();

                row["ExecutionID"] = executionID;
                row["ItemNumber"] = itemNumber;
                row["Username"] = user.Username;
                if (user.uid.HasValue)
                {
                    row["uid"] = user.uid;
                }

                resourceTable.Rows.Add(row);

            }

            if (CommunityContext.Connection.State == ConnectionState.Closed)
            {
                await CommunityContext.Connection.OpenAsync();
            }

            CompanyContext.SetApiExecutionProcessingStartTime(execution.ExecutionID);


            using (SqlTransaction trans = CommunityContext.Connection.BeginTransaction())
            {
                try
                {
                    await CommunityContext.Connection.ExecuteAsync(@"
                        drop table if exists #UserResources;
                        create table #UserResources
                        (
                            ExecutionID uniqueidentifier,
                            ItemNumber int,
                            Username nvarchar(500),
                            ResourceID int,
                            [uid] uniqueidentifier,
                            CompanyResourceState int
                        )
                        ", transaction: trans);


                    SqlBulkCopy bulkCopy = new SqlBulkCopy(CommunityContext.Connection, SqlBulkCopyOptions.Default, trans);
                    bulkCopy.DestinationTableName = "#UserResources";

                    bulkCopy.ColumnMappings.Add("ExecutionID", "ExecutionID");
                    bulkCopy.ColumnMappings.Add("ItemNumber", "ItemNumber");
                    bulkCopy.ColumnMappings.Add("Username", "Username");
                    bulkCopy.ColumnMappings.Add("uid", "uid");


                    await bulkCopy.WriteToServerAsync(resourceTable);


                    await CommunityContext.Connection.ExecuteAsync(@"
                        update  U
                        set     U.ResourceID = coalesce(R2.ID, R.ID)
                        from    #UserResources U
                                left join [Resource] R on R.Email = U.Username
                                left join [Resource] R2 on R2.[uid] = U.[uid];

                        update  U
                        set U.CompanyResourceState = R.[State],
                            U.uid = CR.uid
                        from #UserResources U
                        left join [CompanyResource] R on R.ResourceID = U.ResourceID and R.CompanyID = @companyId
                        left join [Resource] CR on CR.ID = R.ResourceID;
                        ", new { companyId = CompanyContext.CurrentCompanyID }, transaction: trans);

                    var communityResults = await CommunityContext.Connection.QueryAsync<dynamic>(@"select * from #UserResources", transaction: trans);

                    foreach (var result in communityResults)
                    {
                        var user = users.SingleOrDefault(u => u.ItemNumber == result.ItemNumber);
                        if (user != null)
                        {
                            user.ResourceID = result.ResourceID;
                            user.uid = user.IsNew ? result.uid : user.uid;
                            user.CompanyResourceState = (CompanyResourceState?)result.CompanyResourceState;
                        }
                    }

                    await CommunityContext.Connection.ExecuteAsync(@"drop table if exists #UserResources", transaction: trans);

                    trans.Commit();

                }
                catch (Exception ex)
                {
                    try
                    {
                        if (trans != null)
                        {
                            trans.Rollback();
                        }
                    }
                    catch
                    {
                    }
                    throw ex;
                }
            }


            #endregion

            foreach (var user in users)
            {
                var row = userTable.NewRow();
                var CurrPassword = "";
                var NewPassword = "";

                var success = true;
                var messages = new List<string>();

                user.FirstName = SanitizeValue(user.FirstName);
                user.LastName = SanitizeValue(user.LastName);

                if (user.IsNew)
                {
                    if (user.ResourceID.HasValue)
                    {
                        if (user.CompanyResourceState.HasValue && user.CompanyResourceState != CompanyResourceState.Deleted)
                        {
                            success = false;
                            messages.Add(MemberShipErrors.ResourceUserNameExists);
                        }
                    }

                    if (user.State.HasValue)
                    {
                        success = false;
                        messages.Add(MemberShipErrors.CanNotProvideStateOfNewUser);
                    }

                    if (!string.IsNullOrEmpty(user.Password))
                    {
                        if (!validatePassword(user.Password))
                        {
                            success = false;
                            messages.Add(MemberShipErrors.PasswordRule);
                        }
                    }
                    if (string.IsNullOrEmpty(user.FirstName))
                    {
                        success = false;
                        messages.Add(MemberShipErrors.FirstNameMissing);
                    }
                    if (string.IsNullOrEmpty(user.LastName))
                    {
                        success = false;
                        messages.Add(MemberShipErrors.LastNameMissing);
                    }
                }
                else
                {
                    if (!user.uid.HasValue)
                    {
                        success = false;
                        messages.Add(MemberShipErrors.ProvideUserUid);
                    }

                    if (!user.ResourceID.HasValue && user.uid.HasValue)
                    {
                        success = false;
                        messages.Add(MemberShipErrors.ResourceUidNotFound);
                    }

                    //Password Change
                    if (IsChangePasswordReqeust)
                    {
                        NewPassword = user.Fields.Where(z => z.Key == "NewPassword").Select(z => z.Value).FirstOrDefault();
                        CurrPassword = user.Fields.Where(z => z.Key == "CurrentPassword").Select(z => z.Value).FirstOrDefault();
                        if (NewPassword == null)
                        {
                            success = false;
                            messages.Add(MemberShipErrors.ResourceUidNotFound);
                        }
                        else
                        {
                            user.Password = NewPassword;
                        }

                        if (CurrPassword == null)
                        {
                            success = false;
                            messages.Add(MemberShipErrors.MissingCurrentPasswordParameter);
                        }

                        var CurrPasswordHash = PasswordHelper.HashPassword(CurrPassword);
                        var existing = CommunityContext.Filter<Resource>(i => i.Password == CurrPasswordHash && i.Uid == user.uid).FirstOrDefault();
                        if (existing == null)
                        {
                            success = false;
                            messages.Add(MemberShipErrors.CurrentPasswordWrong);
                        }

                        if (NewPassword == CurrPassword)
                        {
                            success = false;
                            messages.Add(MemberShipErrors.NewAndCurrentNotSame);
                        }
                    }

                    if (!string.IsNullOrEmpty(user.Password))
                    {
                        if (!validatePassword(user.Password))
                        {
                            success = false;
                            messages.Add(MemberShipErrors.PasswordRule);
                        }
                    }

                    if (user.uid != null)
                    {
                        Guid currentUser = (Guid)user.uid;
                        var isUser = this.AssetRepository.GetAssetByUID(currentUser);

                        if (isUser == null || isUser.Object != "Resource")
                        {
                            success = false;
                            messages.Add(string.Format(MemberShipErrors.UserUidNotFound, user.uid));
                        }
                    }

                    if (string.IsNullOrEmpty(user.FirstName))
                    {
                        success = false;
                        messages.Add(MemberShipErrors.FirstNameMissing);
                    }
                    if (string.IsNullOrEmpty(user.LastName))
                    {
                        success = false;
                        messages.Add(MemberShipErrors.LastNameMissing);
                    }
                }

                if (string.IsNullOrEmpty(user.Username) || !Regex.IsMatch(user.Username + "", @"^$|\b([A-Za-z0-9'_\.-]+)@([\dA-Za-z\.-]+)\.([A-Za-z\.]{2,6})\b"))
                {
                    success = false;
                    messages.Add(MemberShipErrors.InvalidEmail);
                }
                else if (users.Count(u => u.Username.Trim().Equals(user.Username.Trim(), StringComparison.InvariantCultureIgnoreCase)) > 1)
                {
                    success = false;
                    messages.Add(MemberShipErrors.UsernameDuplicate);
                }


                if (user.CompanyResourceState.HasValue)
                {
                    if (user.IsNew)
                    {
                        user.State = CompanyResourceState.Active;
                        user.IsNew = false;
                    }
                }

                row["ExecutionID"] = executionID;
                if (user.uid.HasValue)
                {
                    row["Uid"] = user.uid;
                }
                if (user.ResourceID.HasValue)
                {
                    row["ResourceID"] = user.ResourceID;
                }
                if (user.ExecutionItemUid.HasValue)
                {
                    row["ExecutionItemUId"] = user.ExecutionItemUid;
                }
                row["ItemNumber"] = user.ItemNumber;
                row["Username"] = user.Username;

                row["FirstName"] = user.FirstName;
                row["LastName"] = user.LastName;

                row["Password"] = user.Password;
                if (user.State.HasValue && !IsChangePasswordReqeust)
                {
                    row["State"] = (int)user.State;
                }
                row["IsAdministrator"] = user.IsAdministrator;
                row["IsNew"] = user.IsNew;
                row["Object"] = "Resource";
                row["ObjectID"] = user.ResourceID ?? 0;
                row["ObjectType"] = "ResourceType";
                row["ObjectTypeID"] = ResourceTypeID;

                userTable.Rows.Add(row);

                if (user.Fields != null && !IsChangePasswordReqeust)
                {
                    foreach (var field in user.Fields.Keys)
                    {
                        var fieldType = fieldTypes.FirstOrDefault(f => f.Name == field);

                        if (fieldType == null)
                        {
                            success = false;
                            messages.Add(string.Format(MemberShipErrors.FieldTypeKeyNotFound, field));
                        }

                        var fieldRow = fieldTable.NewRow();
                        fieldRow["ExecutionID"] = executionID;
                        fieldRow["ItemNumber"] = user.ItemNumber;
                        fieldRow["FieldName"] = field;
                        fieldRow["FieldValue"] = user.Fields[field];

                        fieldTable.Rows.Add(fieldRow);
                    }
                }

                if (!success)
                {
                    row["Success"] = false;
                }
                row["Message"] = messages.Any() ? string.Join(". ", messages) + ". " : "";

            }

            #region Bulk Copy Company

            if (CompanyContext.Connection.State == ConnectionState.Closed)
            {
                await CompanyContext.Connection.OpenAsync();
            }

            using (SqlTransaction trans = CompanyContext.Connection.BeginTransaction())
            {
                try
                {
                    await CompanyContext.Connection.ExecuteAsync(@"
                        drop table if exists #UserFields;
                        create table #UserFields
                        (
                            ExecutionID uniqueidentifier not null,
                            ItemNumber int not null,
                            FieldName nvarchar(250),
                            FieldValue nvarchar(max),
                            FieldTypeID int,
                            LookupValue nvarchar(max)
                        );

                        ", transaction: trans);

                    SqlBulkCopy bulkCopy = new SqlBulkCopy(CompanyContext.Connection, SqlBulkCopyOptions.Default, trans);
                    bulkCopy.DestinationTableName = "api.ExecutionUser";

                    bulkCopy.ColumnMappings.Add("ExecutionID", "ExecutionID");
                    bulkCopy.ColumnMappings.Add("ExecutionItemUid", "ExecutionItemUid");
                    bulkCopy.ColumnMappings.Add("ItemNumber", "ItemNumber");
                    bulkCopy.ColumnMappings.Add("Username", "Username");
                    bulkCopy.ColumnMappings.Add("Uid", "Uid");
                    bulkCopy.ColumnMappings.Add("ResourceID", "ResourceID");

                    bulkCopy.ColumnMappings.Add("FirstName", "FirstName");
                    bulkCopy.ColumnMappings.Add("LastName", "LastName");
                    bulkCopy.ColumnMappings.Add("State", "State");
                    bulkCopy.ColumnMappings.Add("IsAdministrator", "IsAdministrator");
                    bulkCopy.ColumnMappings.Add("IsNew", "IsNew");
                    bulkCopy.ColumnMappings.Add("Success", "Success");
                    bulkCopy.ColumnMappings.Add("Message", "Message");
                    bulkCopy.ColumnMappings.Add("Object", "Object");
                    bulkCopy.ColumnMappings.Add("ObjectID", "ObjectID");
                    bulkCopy.ColumnMappings.Add("ObjectType", "ObjectType");
                    bulkCopy.ColumnMappings.Add("ObjectTypeID", "ObjectTypeID");

                    await bulkCopy.WriteToServerAsync(userTable);

                    bulkCopy = new SqlBulkCopy(CompanyContext.Connection, SqlBulkCopyOptions.Default, trans);
                    bulkCopy.DestinationTableName = "#UserFields";

                    bulkCopy.ColumnMappings.Add("ExecutionID", "ExecutionID");
                    bulkCopy.ColumnMappings.Add("ItemNumber", "ItemNumber");
                    bulkCopy.ColumnMappings.Add("FieldName", "FieldName");
                    bulkCopy.ColumnMappings.Add("FieldValue", "FieldValue");
                    bulkCopy.ColumnMappings.Add("FieldTypeID", "FieldTypeID");
                    bulkCopy.ColumnMappings.Add("LookupValue", "LookupValue");


                    await bulkCopy.WriteToServerAsync(fieldTable);


                    #region Populate table values

                    await CompanyContext.Connection.ExecuteAsync(@"
                        update  U
                        set     U.ResourceID = G.ResourceID
                        from    api.ExecutionUser U
                                inner join reporting.Global_Resource G on G.[uid] = U.[Uid] and G.[State] <> @deleted
                        where   U.ExecutionID = @executionID and U.Success is null and U.IsNew = 0;

                        update  U
                        set     U.FieldTypeID = F.ID
                        from    #UserFields U
                                inner join FieldType F on F.Name = U.FieldName and F.Object = 'ResourceType' and F.ObjectID = @ResourceTypeID
                        where   U.ExecutionID = @executionID;
                        ", new { executionID, deleted = (int)CompanyResourceState.Deleted, ResourceTypeID }, transaction: trans);

                    #endregion

                    #region Validation
                    if (!IsChangePasswordReqeust)
                    {
                        await CompanyContext.Connection.ExecuteAsync(@"
                        update  U
                        set     U.Success = 0,
                                U.Message = U.Message + 'Resource for this uid not found. '
                        from    api.ExecutionUser U
                        where   U.Success is null and U.IsNew = 0 and U.ResourceID is null and U.ExecutionID = @executionID;

                        update  U
                        set     U.Success = 0,
                                U.Message = U.Message + 'One or more field values supplied is missing a field type. '
                        from    api.ExecutionUser U
                                cross apply (
                                    select  count(*) as MissingCount 
                                    from    #UserFields F 
                                    where   F.ItemNumber = U.ItemNumber 
                                            and F.ExecutionID = U.ExecutionID
                                            and F.FieldTypeID is null
                                ) C
                        where   U.Success is null and U.ExecutionID = @executionID and C.MissingCount > 0;

                        update  U
                        set     U.Success = 0,
                                U.Message = U.Message + 'Missing required fields. '
                        from    api.ExecutionUser U
                                cross apply (
                                    select  count(*) as MissingCount
                                    from    FieldType F
                                    where   F.Object = 'ResourceType' 
                                            and F.ObjectID = @ResourceTypeID and F.IsRequired = 1
                                            and not exists (
                                                select  1 
                                                from    #UserFields R 
                                                where   R.ItemNumber = U.ItemNumber 
                                                        and R.ExecutionID = U.ExecutionID 
                                                        and R.FieldTypeID = F.ID
                                            )
                                ) C
                        where   U.Success is null and U.ExecutionID = @executionID and C.MissingCount > 0;

                        ", new { executionID, deleted = (int)CompanyResourceState.Deleted, ResourceTypeID }, transaction: trans);

                        if (lookupFieldsPassedByValue)
                        {
                            CompanyContext.CopyFieldLookupValuesAsIs(execution.ExecutionID, 3600, "#UserFields", trans);
                        }
                        else
                        {
                            CompanyContext.ResolveFieldLookupValues(executionID, "#UserFields", 3600, trans);
                        }

                        //validate lookup fields
                        await CompanyContext.Connection.ExecuteAsync(@"
                        update  U
                        set     U.Success = 0,
                                U.Message = U.Message + 'Invalid lookup value for field ' + F.FieldName + '. '
                        from    api.ExecutionUser U
                        inner join #UserFields F on F.ItemNumber = U.ItemNumber and F.ExecutionID = @executionID
                        inner join FieldType FT on FT.ID = F.FieldTypeID and FT.Type = 'Lookup'
                        where U.ExecutionID = @executionID and F.LookupValue is null and F.FieldValue is not null
                        ", new { executionID }, transaction: trans);

                        await CompanyContext.Connection.ExecuteAsync(@"
                        insert into api.ExecutionField (ExecutionID, ItemNumber, FieldName, FieldValue, FieldTypeID, LookupValue, Ignore)
                        select  ExecutionID,
                        ItemNumber,
                        FieldName,
                        FieldValue,
                        FieldTypeID,
                        LookupValue,
                        null as Ignore
                        from #UserFields
                        ", transaction: trans);
                    }

                    validationResults = (await CompanyContext.Connection.QueryAsync<UserApiUpsertResult>(@"
                        select ItemNumber, 
                        uid, 
                        ExecutionItemUid, 
                        Message, 
                        coalesce(Success, cast(1 as bit)) as Success 
                        from api.ExecutionUser 
                        where ExecutionID = @executionID", new { executionID }, transaction: trans))
                        .ToList();

                    #endregion

                    trans.Commit();
                }
                catch (Exception ex)
                {
                    try
                    {
                        if (trans != null)
                        {
                            trans.Rollback();
                        }
                    }
                    catch
                    {
                    }

                    throw ex;
                }
            }

            #endregion

            #region Upsert records

            foreach (var result in validationResults)
            {

                if (result.Success == true)
                {
                    var user = users.SingleOrDefault(u => u.ItemNumber == result.ItemNumber);

                    if (user != null)
                    {
                        if (!IsChangePasswordReqeust)
                        {
                            bool success;
                            string message;
                            var requiredFieldNames = fieldTypes.Where(f => f.IsRequired && f.Type != DataType.Counter.ToString()).Select(f => f.Name).ToList();

                            CompanyContext.ValidateFields("ResourceType",
                               ResourceTypeID,
                               isInsert,
                               fieldTypes,
                               requiredFieldNames,
                               user.Fields,
                               executionID,
                               user.ItemNumber,
                               null,
                               out success,
                               out message);

                            if (success == false)
                            {
                                result.Success = false;
                                result.Message += message;

                                results.Add(result);
                                continue;
                            }

                        }
                        //add resource
                        if (!user.ResourceID.HasValue)
                        {
                            if (string.IsNullOrEmpty(user.Password))
                            {
                                user.Password = PasswordHelper.CreateRandomPassword();
                            }

                            var resource = new Resource()
                            {
                                FirstName = user.FirstName,
                                LastName = user.LastName,
                                Email = user.Username,
                                Username = user.Username,
                                Password = PasswordHelper.HashPassword(user.Password)
                            };

                            CommunityContext.Add(resource);

                            user.ResourceID = resource.ID;
                            user.uid = resource.Uid;
                            result.uid = resource.Uid;
                        }
                        else
                        {
                            var resource = CommunityContext.Resources.FirstOrDefault(r => r.ID == (int)user.ResourceID);
                            if (resource != null)
                            {
                                resource.FirstName = user.FirstName;
                                resource.LastName = user.LastName;

                                if (string.Compare(user.Username, resource.Username, true) != 0)
                                {
                                    //check if the resource already exists in community
                                    var existing = CommunityContext.Filter<Resource>(i => i.Email == user.Username && i.Uid != user.uid).FirstOrDefault();

                                    if (existing != null)
                                    {
                                        result.Success = false;
                                        result.uid = user.uid;
                                        result.Message += "Cannot update the user because the specified email address / username is already in use. ";
                                        results.Add(result);
                                        continue;
                                    }

                                    resource.Email = user.Username;
                                    resource.Username = user.Username;

                                }

                                if (!string.IsNullOrEmpty(user.Password))
                                {
                                    resource.Password = PasswordHelper.HashPassword(user.Password);
                                }

                                user.uid = resource.Uid;
                                resource.UpdatedOn = DateTime.UtcNow;
                                CommunityContext.Update(resource);
                            }
                        }

                        if (!IsChangePasswordReqeust)
                        {
                            CompanyResource companyResource;

                            if (user.CompanyResourceState.HasValue)
                            {
                                companyResource = CommunityContext.CompanyResources.FirstOrDefault(c => c.CompanyID == CompanyContext.CurrentCompanyID && c.ResourceID == user.ResourceID);

                                if (companyResource != null)
                                {
                                    //disallow changing the admin flag if the current user is not an admin
                                    if (CompanyContext.CurrentResourceIsAdmin == false && user.IsAdministrator != companyResource.IsAdministrator)
                                    {
                                        result.Success = false;
                                        result.uid = user.uid;
                                        result.Message += "Non-administrator users cannot update the administrator flag. ";
                                        results.Add(result);
                                        continue;
                                    }

                                    companyResource.IsAdministrator = user.IsAdministrator;
                                    companyResource.State = user.State ?? companyResource.State;

                                    CommunityContext.Update(companyResource);
                                }
                            }
                            else
                            {
                                //disallow creating admin users if the current user is not an admin
                                if (CompanyContext.CurrentResourceIsAdmin == false && user.IsAdministrator == true)
                                {
                                    result.Success = false;
                                    result.uid = user.uid;
                                    result.Message += "Non-administrator users cannot update the administrator flag. ";
                                    results.Add(result);
                                    continue;
                                }

                                companyResource = new CompanyResource()
                                {
                                    ResourceID = (int)user.ResourceID,
                                    CompanyID = CompanyContext.CurrentCompanyID,
                                    State = CompanyResourceState.Active,
                                    IsAdministrator = user.IsAdministrator
                                };

                                CommunityContext.Add(companyResource);

                            }

                            var globalResource = CompanyContext.GlobalReportingResources.FirstOrDefault(r => r.ResourceID == user.ResourceID);

                            if (globalResource != null)
                            {
                                globalResource.FirstName = user.FirstName;
                                globalResource.LastName = user.LastName;
                                globalResource.Email = user.Username;
                                globalResource.IsAdministrator = user.IsAdministrator;
                                globalResource.State = user.State ?? companyResource.State;
                                globalResource.UpdatedOn = DateTime.UtcNow;

                                CompanyContext.Update(globalResource);



                            }
                            else
                            {
                                globalResource = new GlobalReportingResource
                                {
                                    IsAdministrator = user.IsAdministrator,
                                    ResourceID = (int)user.ResourceID,
                                    Email = user.Username,
                                    FirstName = user.FirstName,
                                    LastName = user.LastName,
                                    State = user.State ?? companyResource.State,
                                    UpdatedOn = DateTime.UtcNow,
                                    Uid = (Guid)user.uid,
                                    CreatedOn = DateTime.UtcNow
                                };

                                CompanyContext.Add(globalResource);
                            }
                        }
                    }
                }

                results.Add(result);
            }

            #endregion

            #region Merge Fields

            if (CompanyContext.Connection.State == ConnectionState.Closed)
                await CompanyContext.Connection.OpenAsync();

            using (SqlTransaction trans = CompanyContext.Connection.BeginTransaction())
            {
                try
                {
                    await CompanyContext.Connection.ExecuteAsync(@"
                        drop table if exists #UserResults;
                        create table #UserResults
                        (
                            ExecutionID uniqueidentifier not null,
                            ItemNumber int not null,
                            [uid] uniqueidentifier null,
                            Success bit null,
                            Message nvarchar(max)
                        );
                        ", transaction: trans);

                    var resultsTable = new DataTable();

                    resultsTable.Columns.Add("ExecutionID", typeof(Guid));
                    resultsTable.Columns.Add("ItemNumber", typeof(int));
                    resultsTable.Columns.Add("uid", typeof(Guid));
                    resultsTable.Columns.Add("Success", typeof(bool));
                    resultsTable.Columns.Add("Message", typeof(string));

                    results.ForEach(r =>
                    {
                        var row = resultsTable.NewRow();
                        row["ExecutionID"] = executionID;
                        row["ItemNumber"] = r.ItemNumber;
                        if (r.uid.HasValue)
                        {
                            row["uid"] = r.uid;
                        }
                        if (r.Success == false)
                        {
                            row["Success"] = false;
                        }
                        row["Message"] = r.Message ?? "";

                        resultsTable.Rows.Add(row);
                    });

                    var bulkCopy = new SqlBulkCopy(CompanyContext.Connection, SqlBulkCopyOptions.Default, trans);
                    bulkCopy.DestinationTableName = "#UserResults";

                    bulkCopy.ColumnMappings.Add("ExecutionID", "ExecutionID");
                    bulkCopy.ColumnMappings.Add("ItemNumber", "ItemNumber");
                    bulkCopy.ColumnMappings.Add("uid", "uid");
                    bulkCopy.ColumnMappings.Add("Success", "Success");
                    bulkCopy.ColumnMappings.Add("Message", "Message");

                    await bulkCopy.WriteToServerAsync(resultsTable);

                    await CompanyContext.Connection.ExecuteAsync(@"
                        update U
                        set U.ObjectID = GR.ResourceID,
                            U.ResourceID = GR.ResourceID
                        from api.ExecutionUser U
                        inner join #UserResults R on R.ExecutionID = U.ExecutionID and R.ItemNumber = U.ItemNumber and U.ObjectID = 0
                        inner join reporting.Global_resource GR on GR.uid = R.uid

                        update U
                        set U.Success = 0,
                            U.Message = R.Message
                        from api.ExecutionUser U
                        inner join #UserResults R on R.ExecutionID = U.ExecutionID and R.ItemNumber = U.ItemNumber and R.Success = 0
                        ", transaction: trans);

                    if (!IsChangePasswordReqeust)
                    {
                        bool isInsertForMergeField = isInsert;

                        if (isInsert == true)
                        {
                            var UserUpdateCountResult = (await CompanyContext.Connection.QueryAsync<int>(@"
                                select count(1) 
                                from api.ExecutionUser U
                                inner join #UserResults R 
                                on R.ExecutionID = U.ExecutionID and R.ItemNumber = U.ItemNumber and U.IsNew = 0
                                ", new { executionID }, transaction: trans));
                            var UserUpdateCount = UserUpdateCountResult.First();

                            if (UserUpdateCount > 0)
                            {
                                isInsertForMergeField = false;
                            }
                        }

                        CompanyContext.MergeFields(executionID, trans, "api.ExecutionUser", "A.[Object]", "A.ObjectID", 0, itemNumber, sendWorkflowEvents: true, isInsert: isInsertForMergeField);

                        if (hasRelationshipFieldTypes)
                        {
                            CompanyContext.ImportRelationships(executionID, trans, "api.ExecutionUser", "A.Object", "A.ObjectID", 0, itemNumber, resolveRelationshipOnObjectId: lookupFieldsPassedByValue);
                        }
                    }

                    trans.Commit();

                    //Convert UserApiUpsertResult to DatabaseBulkAssetResult to use in SendAssetGraphEvents
                    IEnumerable<IGraphAsset> graphResults = results.Where(r => r.uid.HasValue).Select(r =>
                    {
                        return new DatabaseBulkAssetResult
                        {
                            ExecutionItemUid = r.ExecutionItemUid,
                            ItemNumber = r.ItemNumber,
                            uid = r.uid ?? Guid.Empty,
                            Message = r.Message,
                            Success = r.Success,
                            Object = SystemObjects.Resource.ToString()
                        };
                    }).AsEnumerable();
                    if (graphResults.Any())
                    {
                        CompanyContext.SendAssetGraphEvents(graphResults);
                    }
                }
                catch (Exception ex)
                {
                    try
                    {
                        if (trans != null)
                        {
                            trans.Rollback();
                        }
                    }
                    catch
                    {
                    }
                    throw ex;
                }
            }


            using (SqlTransaction trans = CompanyContext.Connection.BeginTransaction())
            {
                try
                {
                    string oldValuesSQL = "";
                    string logMessage = "Created";
                    if (!isInsert)
                    {
                        logMessage = "Updated";
                        oldValuesSQL = @"update  ar
							set ar.OldValue = fa.Value
							from #auditRecords ar
							inner join reporting.Global_Resource gr on gr.uid = ar.uid
							outer apply (select top 1 ID from reporting.Global_Audit where Object = 'Resource' and ObjectId = gr.resourceid and Action in ('Created','Updated') order by id desc)Audit(ID)
							left join reporting.Global_FieldAudit fa on fa.auditid = audit.id and fa.fieldname = ar.fieldname";
                    }

                    await CompanyContext.Connection.ExecuteAsync($@"
                            drop table if exists #auditRecords
                            create table #auditRecords (uid uniqueidentifier, FieldName nvarchar(200), OldValue nvarchar(max), NewValue nvarchar(max))

                            ;with cte as (select ex.*, gr.uid as resourceUid from api.executionuser ex
                            inner join reporting.Global_Resource gr on gr.resourceid = ex.ResourceID
                            where ex.executionid = @executionid and (ex.success <> 0 or ex.success is null))
                            insert into #auditRecords
                            select cte.resourceUid, 'Email','', cte.Username from cte
                            union 
                            select cte.resourceUid, 'First Name','', cte.FirstName from cte
                            union
                            select cte.resourceUid, 'Last Name', '',cte.lastName from cte
                            union
                            select cte.resourceUid, 'Is Administrator', '',try_cast( cte.IsAdministrator as nvarchar(255)) from cte

                            insert into #auditRecords
                            select gr.uid, ef.FieldName,'', ef.fieldvalue from api.executionuser ex
                            inner join reporting.Global_Resource gr on gr.resourceid = ex.ResourceID
                            left join api.executionfield ef on ef.executionid = ex.executionid and ef.itemnumber = ex.ItemNumber
                            where ex.executionid = @executionid and (ex.success <> 0 or ex.success is null)
                            
                            {oldValuesSQL}

                            declare @audit table (auditId int)
                            insert into reporting.Global_Audit
                            OUTPUT INSERTED.ID
                            INTO @audit
                            select distinct 'Resource', gr.ResourceId, SUBSTRING(gr.FirstName + ' ' + gr.LastName,0,250), @currentresourceid, GETUTCDATE(), '{logMessage}', 'Resource', gr.ResourceId, 'Resource', SUBSTRING(gr.FirstName + ' ' + gr.LastName,0,250),'Resource {logMessage}' from #auditRecords ar
                            inner join reporting.Global_Resource gr on gr.uid = ar.uid

                            insert into reporting.global_fieldaudit
                            select a.auditid,0, ar.fieldname, 1,ar.newvalue, ar.oldvalue from @audit a
                            inner join reporting.Global_Audit ga on ga.id = a.auditid
                            inner join reporting.Global_Resource gr on gr.ResourceId = ga.ObjectID
                            inner join #auditRecords ar on gr.uid = ar.uid
                            where isnull(ar.newvalue,'') <> isnull(ar.oldvalue,'')
                            order by ar.uid",
                            new { executionID, CompanyContext.CurrentResourceID },
                            transaction: trans);

                    trans.Commit();
                }
                catch (Exception ex)
                {
                    try
                    {
                        if (trans != null)
                        {
                            trans.Rollback();
                        }
                    }
                    catch
                    {
                    }

                    execution.ErrorMessage += ";Audit Log creation failed";
                    execution.CompletedOn = DateTime.UtcNow;
                    CompanyContext.Update(execution);

                    throw ex;
                }
            }

            #endregion

            return results;
        }

        public async Task<ApiExecutionInfo> UpsertBulkUsers(ApiExecution execution, UserUpsertModel model)
        {
            var executionInfo = new ApiExecutionInfo
            {
                CompanyID = CompanyContext.CurrentCompanyID,
                CompanyDomainPrefix = CompanyContext.CurrentCompanyDomain,
                ExecutionID = Guid.NewGuid(),
                ResourceID = execution.ResourceID,
                Action = ApiExecutionAction.UpsertUsers,

            };

            return await CreateApiBatchJob(executionInfo, execution, model, StorageProvider, QueueSource).ConfigureAwait(false);
        }

        private string SanitizeValue(string ParameterValue)
        {
            var allowedTags = new[] { "data" };
            var allowedSchemas = new[] { "data" };

            var sanitizer = new Ganss.XSS.HtmlSanitizer(allowedTags: allowedTags, allowedSchemes: allowedSchemas);
            var retstring = sanitizer.Sanitize(ParameterValue);
            return retstring;
        }

        private bool validatePassword(string password)
        {
            if (string.IsNullOrEmpty(password))
            {
                return false;
            }

            if (password.Length < 7 || password.Length > 25)
            {
                return false;
            }

            if (!password.Any(char.IsUpper) || !password.Any(char.IsLower))
            {
                return false;
            }

            if (!password.Any(char.IsDigit))
            {
                return false;
            }

            return true;
        }

        public List<GroupResponseResult> UpdateGroups(ApiExecution execution, List<UpdateGroupModel> groups)
        {
            List<GroupResponseResult> results = null;
            try
            {
                results = CompanyContext.UpdateGroups(execution, groups);

                // Close execution record.
                execution.Processed = results.Count;
                execution.Error = results.Count(i => !i.Success);
                execution.CompletedOn = DateTime.UtcNow;
                CompanyContext.Update(execution);
            }
            catch (Exception ex)
            {
                string message = ex.GetFullExceptionData(false, constants.ERROR_MESSAGE_CHARACTER_LIMIT);
                execution.ErrorMessage = message;
                execution.CompletedOn = DateTime.UtcNow;
                CompanyContext.Update(execution);
            }


            return results;
        }

        public List<GroupResponseResult> AddGroups(ApiExecution execution, List<UpdateGroupModel> groups)
        {
            List<GroupResponseResult> results = null;

            try
            {
                results = CompanyContext.UpdateGroups(execution, groups);

                // Close execution record.
                execution.Processed = results.Count;
                execution.Error = results.Count(i => !i.Success);
                execution.CompletedOn = DateTime.UtcNow;
                CompanyContext.Update(execution);
            }
            catch (Exception ex)
            {
                string message = ex.GetFullExceptionData(false, constants.ERROR_MESSAGE_CHARACTER_LIMIT);
                execution.ErrorMessage = message;
                execution.CompletedOn = DateTime.UtcNow;
                CompanyContext.Update(execution);
            }


            return results;
        }

        public List<GroupResponseResult> DeleteGroups(ApiExecution execution, List<DeleteGroupModel> groups)
        {
            CompanyContext.Add(execution);

            List<GroupResponseResult> results = null;
            try
            {
                results = CompanyContext.DeleteGroups(execution, groups);

                // Close execution record.
                execution.Processed = results.Count;
                execution.Error = results.Count(i => !i.Success);
                execution.CompletedOn = DateTime.UtcNow;
                CompanyContext.Update(execution);
            }
            catch (Exception ex)
            {
                string message = ex.GetFullExceptionData(false, constants.ERROR_MESSAGE_CHARACTER_LIMIT);
                execution.ErrorMessage = message;
                execution.CompletedOn = DateTime.UtcNow;
                CompanyContext.Update(execution);
            }

            return results;
        }

        private string RoutePrefixToObjectType(string prefix)
        {
            switch (prefix)
            {
                case "artifact":
                case "domain":
                case "policy":
                case "reference":
                    return char.ToUpper(prefix[0]) + prefix.ToLower().Substring(1);
                case "admin/lookups":
                    return "Lookup";
                case "quality/rule":
                    return "Rule";
                case "model":
                    return "Taxonomy";
                case "resource":
                case "resource/list":
                    return "Resource";
                case "cart":
                    return "ShoppingCart";
                case "group":
                case "groups":
                    return "Group";
                default:
                    return "";
            }
        }

        [Obsolete]
        public async Task ClearFavorites(int resourceID)
        {
            await CompanyContext.DeleteAsync<Favorite>(i => i.ResourceID == resourceID && !i.IsHomePage);
        }

        public async Task DeleteFavorites(int resourceID, List<int> favoriteIds)
        {
            await CompanyContext.DeleteAsync<Favorite>(i => i.ResourceID == resourceID && favoriteIds.Contains(i.ID));
        }

        public async Task<List<OrganizationModel>> GetOrganizationsByType(Guid organizationTypeUid, IEnumerable<KeyValuePair<string, string>> queryParams)
        {
            var dbArgs = new DynamicParameters();

            int pageSize = 200;
            int pageNum = 0;
            string direction = "asc";
            string order = "Name";

            string whereSQL = "";


            dbArgs.Add("@organizationTypeUid", organizationTypeUid);

            if (queryParams.ToList().Any(x => x.Key.ToLower() == "_pagesize"))
            {
                if (int.TryParse(queryParams.FirstOrDefault(x => x.Key.ToLower() == "_pagesize").Value, out int res))
                {
                    pageSize = res;
                }
            }
            if (queryParams.ToList().Any(x => x.Key.ToLower() == "_pagenum"))
            {
                if (int.TryParse(queryParams.FirstOrDefault(x => x.Key.ToLower() == "_pagenum").Value, out int res))
                {
                    pageNum = res - 1;
                }
            }

            dbArgs.Add("@pageNum", pageNum);
            dbArgs.Add("@pageSize", pageSize);
            dbArgs.Add("@offset", (pageSize * pageNum));

            if (queryParams.Any(q => q.Key == "_order"))
            {
                order = queryParams.ToList().FirstOrDefault(q => q.Key == "_order").Value;
            }

            if (queryParams.Any(q => q.Key == "_direction"))
            {
                direction = queryParams.ToList().FirstOrDefault(q => q.Key == "_direction").Value;
            }

            var orderBySQL = $"Order by {order} {direction}";

            if (queryParams.Any(q => q.Key == "_filter"))
            {
                var filterValue = queryParams.FirstOrDefault(x => x.Key.ToLower() == "_filter").Value;
                List<DefaultFilter> fieldList = new List<DefaultFilter>
                {
                new DefaultFilter("Name", "O.name", SqlFieldType.Text),
                new DefaultFilter("Email", "R.Email", SqlFieldType.Text)
                };

                if (!string.IsNullOrEmpty(filterValue))
                {
                    var filterDataProvider = new FilterDataProvider(CompanyContext);

                    var filterExpressionParser = new FilterExpressionParser(filterDataProvider, FilterExpressionParseType.CustomFields, false, true);
                    filterExpressionParser.OverrideAllowedDefaultFields(fieldList);
                    Dictionary<string, object> sqlParams = new Dictionary<string, object>();
                    List<int> filteredFieldIds = new List<int>();
                    whereSQL = "Where " + filterExpressionParser.Parse(filterValue, out sqlParams, out filteredFieldIds);

                    foreach (var item in sqlParams)
                    {
                        dbArgs.Add(item.Key, item.Value);
                    }
                }
            }

            string sql = $@"select 
	                        A.uid,
	                        O.Name,
	                        R.uid as AcceptedBy,
	                        R.FirstName + ' ' + R.LastName as AcceptedByUserName,
	                        O.DateAccepted as AcceptedOn,
	                        O.AdministratorEmail
                        from 
	                        Organization O 
	                        inner join 
	                        OrganizationType OT on O.OrganizationTypeID=OT.ID and O.state = 1
	                        inner join AssetType AST on AST.Object = 'OrganizationType' and OT.ID=AST.ObjectID and AST.uid =  @organizationTypeUid
	                        inner join Asset A on A.Object ='Organization' and A.ObjectID = O.ID
	                        left join 
	                        reporting.Global_REsource R on R.ResourceID = O.acceptedBy
                        {whereSQL}
                        {orderBySQL}
                        OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY";

            return (await this.CompanyContext.QueryAsync<OrganizationModel>(sql, dbArgs, ApiTimeout)).ToList();
        }

        public async Task<OrganizationDetailModel> GetOrganizationsDetails(Guid organizationUid)
        {
            var dbArgs = new DynamicParameters();

            dbArgs.Add("@organizationUid", organizationUid);

            string @sql = $@"Select 
                                A.uid, 
                                O.Name, 
                                R.uid as AcceptedBy, 
                                OD.AcceptedByName as AcceptedByUserName, 
                                O.DateAccepted as AcceptedOn,
                                O.AdministratorEmail,
                                JSON_QUERY((
                                    select 
		                                CONCAT('[""',STRING_AGG(STRING_ESCAPE(cast([uid] as nvarchar(36)),'JSON'), '"",""'),'""]')  
                                    from 
		                                reporting.Global_resource U 
		                                inner join OrganizationResource ORes on U.ResourceID=ORes.ResourceID and ORes.OrganizationID = O.ID
                                )) as Users,
                                JSON_QUERY((
                                    select 
                                        CONCAT('[""',STRING_AGG(STRING_ESCAPE([Domain],'JSON'), '"",""'),'""]')  		                                 
	                                from 
		                                OrganizationDomain D 
	                                where 
		                                D.OrganizationID = O.ID						
                                )) as [Domains],
                                JSON_QUERY((
                                    select 
                                        CONCAT('[""',STRING_AGG(STRING_ESCAPE([Email],'JSON'), '"",""'),'""]')  			                                 
	                                from 
		                                OrganizationInvitation I 
	                                where 
		                                I.OrganizationID = O.ID						
                                )) as Invitations
                                from 
                                Asset A 
                                inner join Organization O on A.UID = @organizationUid and A.Object ='{SystemObjects.Organization.ToString()}' and A.ObjectID = O.ID and O.state = 1
                                inner join OrganizationDetail OD on O.ID = OD.ID 
                                left join 
	                            reporting.Global_Resource R on R.ResourceID = O.acceptedBy
                        for Json Path, WITHOUT_ARRAY_WRAPPER";

            var jsonString = await this.CompanyContext.QueryFirstOrDefaultAsync<string>(sql, dbArgs, ApiTimeout);

            if (string.IsNullOrEmpty(jsonString))
            {
                return null;
            }

            var models = JObject.Parse(jsonString).ToObject<OrganizationDetailModel>();

            models.Domains = models.Domains.Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
            models.Users = models.Users.Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
            models.Invitations = models.Invitations.Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
            return models;
        }
    }
}