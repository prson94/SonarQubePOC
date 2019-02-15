using d360.core;
using d360.core.entities;
using d360.core.enums.Workflow;
using d360.core.queue;
using Dapper;
using Microsoft.Practices.EnterpriseLibrary.TransientFaultHandling;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;

namespace d360.model
{
    public class RelationshipsPartiallyProcessedEventArgs : EventArgs
    {
        public List<DatabaseBulkRelationshipResult> Results { get; set; }
    }

    partial class CompanyContext: BaseContext
    {
        #region DbSets

        public DbSet<ApiService> ApiServices { get; set; }

        public DbSet<ApiEndpoint> ApiEndpoints { get; set; }

        public DbSet<ApiEndpointVersion> ApiEndpointVersions { get; set; }

        public DbSet<ApiEntity> ApiEntities { get; set; }

        public DbSet<ApiEntityFieldType> ApiEntityFieldTypes { get; set; }

        public DbSet<ApiEntityFieldTypeMultiSelectField> ApiEntityFieldTypeMultiSelectFields { get; set; }

        public DbSet<ApiEntityUri> ApiEntityUris { get; set; }

        public DbSet<ApiExecution> ApiExecutions { get; set; }

        public DbSet<ApiNamespace> ApiNamespaces { get; set; }

        #endregion

        #region Events specific to API sub-system

        public event EventHandler<RelationshipsPartiallyProcessedEventArgs> RelationshipsPartiallyProcessed;
        protected virtual void OnRelationshipsPartiallyProcessed(RelationshipsPartiallyProcessedEventArgs e)
        {
            RelationshipsPartiallyProcessed?.Invoke(this, e);
        }

        #endregion

        private List<DataRow> ValidateFields(
            List<FieldType> fieldTypes, List<string> requiredFieldTypeNames,
            Dictionary<string, string> fields, Guid executionID, int itemNumber,
            DataTable fieldTable, out bool success, out string errorMessage)
        {
            List<DataRow> fieldRows = new List<DataRow>();

            success = true;
            errorMessage = string.Empty;

            FieldType fieldType = null;

            // Contains all required fields?
            var missingFields = requiredFieldTypeNames.Except(fields.Select(f => f.Key));

            if (missingFields.Any())
            {
                success = false;
                bool isSinglar = (missingFields.Count() == 1);
                errorMessage += $"{string.Join(",", missingFields)} {(isSinglar ? "is a" : "are")} required field{(isSinglar ? "" : "s")};";
            }

            foreach (var k in fields)
            {
                string fieldName = k.Key.Trim();
                string fieldValue = (k.Value + "").Trim();
                int? fieldTypeId = null;

                // Validation of field and value;
                fieldType = fieldTypes.SingleOrDefault(f => f.Name == fieldName);
                if (fieldType == null)
                {
                    success = false;
                    errorMessage += $"{fieldName} is not a valid field; ";
                }
                else
                {
                    fieldTypeId = fieldType.ID;

                    if (fieldType.IsRequired)
                    {
                        if (string.IsNullOrEmpty(fieldValue))
                        {
                            success = false;
                            errorMessage += $"{fieldName} is a required field; ";
                        }
                    }
                    else
                    {
                        switch (fieldType.Type)
                        {
                            case "Boolean":
                                if ((fieldValue.ToLower() != "true" && fieldValue.ToLower() != "false") && !string.IsNullOrEmpty(fieldValue))
                                {
                                    success = false;
                                    errorMessage += $"{fieldName} is a boolean field and may only be 'false' or 'true'; ";
                                }
                                break;
                            case "Date":
                                DateTime dTest;
                                if (!DateTime.TryParse(fieldValue, out dTest) && !string.IsNullOrEmpty(fieldValue))
                                {
                                    success = false;
                                    errorMessage += $"{fieldName} must be a valid date; ";
                                }
                                break;
                            case "DateTime":
                                DateTime dtTest;
                                if (!DateTime.TryParse(fieldValue, out dtTest) && !string.IsNullOrEmpty(fieldValue))
                                {
                                    success = false;
                                    errorMessage += $"{fieldName} must be a valid datetime value; ";
                                }
                                break;
                            case "Decimal":
                                decimal decTest;
                                if (!decimal.TryParse(fieldValue, out decTest) && !string.IsNullOrEmpty(fieldValue))
                                {
                                    success = false;
                                    errorMessage += $"{fieldName} must be a valid decimal; ";
                                }
                                break;
                            case "Link":
                                if (fieldValue.Count(c => c == '|') != 1 && !string.IsNullOrEmpty(fieldValue))
                                {
                                    success = false;
                                    errorMessage += $"{fieldName} must be a valid link, using the format name|url; ";
                                }
                                break;
                            case "Lookup":
                                break;
                            case "Number":
                                int intTest;
                                if (!int.TryParse(fieldValue, out intTest) && !string.IsNullOrEmpty(fieldValue))
                                {
                                    success = false;
                                    errorMessage += $"{fieldName} must be a valid whole number; ";
                                }
                                break;
                            case "Percentage":
                                decimal pctTest;
                                if (!decimal.TryParse(fieldValue, out pctTest) && !string.IsNullOrEmpty(fieldValue))
                                {
                                    success = false;
                                    errorMessage += $"{fieldName} must be a valid percentage; ";
                                }
                                break;
                            default: // Html, Text
                                if (!string.IsNullOrEmpty(fieldType.Pattern) && !string.IsNullOrEmpty(fieldValue))
                                {
                                    if (!System.Text.RegularExpressions.Regex.IsMatch(fieldValue, fieldType.Pattern))
                                    {
                                        success = false;
                                        errorMessage += $"{fieldName} must match regular expression pattern defined for this field; ";
                                    }
                                }
                                break;
                        }

                        if (fieldType.Length.HasValue)
                        {
                            if (fieldValue.Length < fieldType.Length.Value)
                            {
                                success = false;
                                errorMessage += $"{fieldName} must have an exact length of {fieldType.Length.Value}; ";
                            }
                        }
                        if (fieldType.MinimumLength.HasValue)
                        {
                            if (fieldValue.Length < fieldType.MinimumLength.Value)
                            {
                                success = false;
                                errorMessage += $"{fieldName} must have a minimum length of {fieldType.MinimumLength.Value}; ";
                            }
                        }
                        if (fieldType.MaximumLength.HasValue)
                        {
                            if (fieldValue.Length > fieldType.MaximumLength.Value)
                            {
                                success = false;
                                errorMessage += $"{fieldName} may only have a maximum length of {fieldType.MaximumLength.Value}; ";
                            }
                        }
                    }
                }

                var fieldRow = fieldTable.NewRow();

                fieldRow["ExecutionID"] = executionID;
                fieldRow["ItemNumber"] = itemNumber;
                fieldRow["FieldName"] = fieldName;
                fieldRow["FieldValue"] = fieldValue;
                if (fieldTypeId.HasValue)
                    fieldRow["FieldTypeID"] = fieldTypeId.Value;

                fieldRows.Add(fieldRow);    // Added temporarily, but may be invalidated based on success flag.
            }

            return fieldRows;
        }

        public List<DatabaseBulkRelationshipResult> ImportRelationships(ApiExecution execution, IntersectType rt, RelationshipInserts import, int timeout = 3600)
        {
            var results = new List<DatabaseBulkRelationshipResult>();

            #region Build data tables for bulk load.

            var table = new DataTable();
            table.Columns.Add("ExecutionID", typeof(Guid));
            table.Columns.Add("ItemNumber", typeof(int));
            table.Columns.Add("Message", typeof(string));
            table.Columns.Add("Success", typeof(bool));
            table.Columns.Add("SubjectUid", typeof(Guid));
            table.Columns.Add("ObjectUid", typeof(Guid));

            var fieldTable = new DataTable();
            fieldTable.Columns.Add("ExecutionID", typeof(Guid));
            fieldTable.Columns.Add("ItemNumber", typeof(int));
            fieldTable.Columns.Add("FieldName", typeof(string));
            fieldTable.Columns.Add("FieldValue", typeof(string));
            fieldTable.Columns.Add("FieldTypeID", typeof(int));

            #endregion

            // Get field types.
            var fieldTypes = Query<FieldType>("select * from FieldType where Object = 'IntersectType' and ObjectID = @ID", new { rt.ID }).ToList();
            var requiredFieldTypeNames = fieldTypes.Where(f => f.IsRequired).Select(f => f.Name).ToList();

            #region Generate data sets

            for (int i = 1; i <= import.Count; i++)
            {
                var model = import[i - 1];

                bool success;
                string errorMessage;
                var fieldRows = ValidateFields(fieldTypes, requiredFieldTypeNames, model.Fields, execution.ExecutionID, i, fieldTable, out success, out errorMessage);

                if (success)
                {
                    fieldRows.ForEach(fr => { fieldTable.Rows.Add(fr); });

                    var row = table.NewRow();

                    row["ExecutionID"] = execution.ExecutionID;
                    row["ItemNumber"] = i;
                    row["SubjectUid"] = model.SubjectAssetUid;
                    row["ObjectUid"] = model.ObjectAssetUid;

                    table.Rows.Add(row);
                }
                else
                {
                    results.Add(new DatabaseBulkRelationshipResult { IntersectID = 0, IsNew = false, ItemNumber = i, Message = errorMessage, Success = false });
                }
            }

            #endregion

            if (results.Count > 0) // There are errors already processed.
            {
                OnRelationshipsPartiallyProcessed(new RelationshipsPartiallyProcessedEventArgs
                {
                    Results = results
                });
            }

            if (Database.Connection.State != ConnectionState.Open)
                (Database.Connection as SqlConnection).OpenWithRetry(RetryPolicy.DefaultProgressive);

            SqlBulkCopy bulkCopy = null;

            #region Asset Bulk Copy

            bulkCopy = new SqlBulkCopy((SqlConnection)Database.Connection);

            bulkCopy.BatchSize = table.Rows.Count;
            bulkCopy.DestinationTableName = "api.ExecutionRelationship";
            bulkCopy.BulkCopyTimeout = timeout;

            bulkCopy.ColumnMappings.Add("ExecutionID", "ExecutionID");
            bulkCopy.ColumnMappings.Add("ItemNumber", "ItemNumber");
            bulkCopy.ColumnMappings.Add("SubjectUid", "SubjectUid");
            bulkCopy.ColumnMappings.Add("ObjectUid", "ObjectUid");

            bulkCopy.WriteToServer(table);

            #endregion

            #region Asset Field Bulk Copy

            bulkCopy = new SqlBulkCopy((SqlConnection)Database.Connection);

            bulkCopy.BatchSize = fieldTable.Rows.Count;
            bulkCopy.DestinationTableName = "api.ExecutionField";
            bulkCopy.BulkCopyTimeout = timeout;

            bulkCopy.ColumnMappings.Add("ExecutionID", "ExecutionID");
            bulkCopy.ColumnMappings.Add("ItemNumber", "ItemNumber");
            bulkCopy.ColumnMappings.Add("FieldName", "FieldName");
            bulkCopy.ColumnMappings.Add("FieldValue", "FieldValue");
            bulkCopy.ColumnMappings.Add("FieldTypeID", "FieldTypeID");

            bulkCopy.WriteToServer(fieldTable);

            #endregion

            bulkCopy = null;

            #region Resolve lookup values

            (Database.Connection as SqlConnection).Execute(@"
create table #LookupValues (FieldTypeID int not null, FieldValue nvarchar(max) not null, [Value] int null)
CREATE CLUSTERED INDEX CIX_TempLookupValues ON #LookupValues ( FieldTypeID ASC );
		
insert into #LookupValues
	select		T.FieldTypeID,
				T.FieldValue,
                null
	from		api.ExecutionField T
				inner join FieldType F on F.ID = T.FieldTypeID and F.[Type] = 'Lookup' and T.ExecutionID = @ExecutionID
	group by	T.FieldTypeID,
				T.FieldValue;

update	T
set		T.[Value] = S.[Value]
from	#LookupValues T
		inner join FieldLookupValue S on S.FieldTypeID = T.FieldTypeID and S.[Text] = T.FieldValue;

update	T
set		T.LookupValue = S.[Value]
from	api.ExecutionField T
		inner join #LookupValues S on S.FieldTypeID = T.FieldTypeID and T.FieldValue = S.FieldValue and T.ExecutionID = @ExecutionID;
", new { execution.ExecutionID }, commandTimeout: timeout);

            #endregion

            #region Log lookup errors

            (Database.Connection as SqlConnection).Execute($@"
update	T
set		T.Success = 0,
		T.[Message] = coalesce(T.[Message] + '; ', '') + 'Relationship contains one or more fields with invalid lookup values: [' + S.Names + ']'
from	api.ExecutionRelationship T
		inner join	(
					select		A.ExecutionID,
                                A.ItemNumber,
								STRING_AGG(FT.Name+'='+F.FieldValue, ', ') as Names
					from		api.ExecutionRelationship A
								inner join FieldType FT on FT.Object = 'IntersectType' 
															and FT.ObjectID = {rt.ID}
															and FT.[Type] = 'Lookup'
								inner join api.ExecutionField F on F.ExecutionID = A.ExecutionID and F.ItemNumber = A.ItemNumber and F.FieldTypeID = FT.ID and F.LookupValue is null
                    where       A.ExecutionID = @ExecutionID
					group by	A.ExecutionID, A.ItemNumber
					) S on S.ExecutionID = T.ExecutionID and S.ItemNumber = T.ItemNumber;
", new { execution.ExecutionID }, commandTimeout: timeout);

            #endregion

            #region Validate subjects/objects

            (Database.Connection as SqlConnection).Execute(@"
declare @st varchar(50),
		@stid int,
		@ot varchar(50),
		@otid int,
		@it int

select	@st = Subject,
		@stid = SubjectID,
		@ot = Object,
		@otid = ObjectID,
		@it = ID
from	IntersectType
where	[uid] = @uid

update	T
set		T.Subject = S.Object,
		T.SubjectID = S.ObjectID,
		T.Object = O.Object,
		T.ObjectID = O.ObjectID
from	api.ExecutionRelationship T
		left join AssetWithType S on T.ExecutionID = @ExecutionID and S.[Type] = @st and S.TypeID = @stid and S.[uid] = T.SubjectUid
		left join AssetWithType O on T.ExecutionID = @ExecutionID and O.[Type] = @ot and O.TypeID = @otid and O.[uid] = T.ObjectUid;

if @st = 'ReferenceItemType' and @stid = 0
begin
	update	T
	set		T.Subject = S.Object,
			T.SubjectID = S.ObjectID
	from	api.ExecutionRelationship T
			inner join AssetType S on T.ExecutionID = @ExecutionID and S.[uid] = T.SubjectUid and T.Subject is null;
end

if @ot = 'ReferenceItemType' and @otid = 0 
begin
	update	T
	set		T.Object = O.Object,
			T.ObjectID = O.ObjectID
	from	api.ExecutionRelationship T
			inner join AssetType O on T.ExecutionID = @ExecutionID and O.[uid] = T.ObjectUid and T.Object is null;
end", new { execution.ExecutionID, rt.uid }, commandTimeout: timeout);

            #endregion

            #region Log subject/object resolution errors

            (Database.Connection as SqlConnection).Execute(@"
update	api.ExecutionRelationship
set		Success = 0,
		[Message] = coalesce([Message] + '; ', '') + 'Not able to resolve subject of this relationship to a valid asset.'
where	ExecutionID = @ExecutionID and Subject is null or SubjectID is null;
	
update	api.ExecutionRelationship
set		Success = 0,
		[Message] = coalesce([Message] + '; ', '') + 'Not able to resolve object of this relationship to a valid asset.'
where	ExecutionID = @ExecutionID and Object is null or ObjectID is null;", new { execution.ExecutionID }, commandTimeout: timeout);

            #endregion

            int loopSize = 100;
            int numberOfLoops = (int)Math.Ceiling((decimal)execution.Total / loopSize);
            int beginItemNumber = 1;
            int endItemNumber = loopSize;

            for (int currentLoop = 1; currentLoop <= numberOfLoops; currentLoop++)
            {
                using (var trans = (Database.Connection as SqlConnection).BeginTransaction())
                {
                    try
                    {
                        #region Intersect table merge

                        (Database.Connection as SqlConnection).Execute($@"
    drop table if exists #ObjectMergeTableResult;
    create table #ObjectMergeTableResult (ID int, ItemNumber int, [Action] nvarchar(10));
    CREATE NONCLUSTERED INDEX IX_TempObjectMergeTableResult ON #ObjectMergeTableResult ( ItemNumber ASC );

    merge into  [Intersect] T
    using		(
			    select      *
			    from        api.ExecutionRelationship
			    where		ExecutionID = @ExecutionID
                            and ItemNumber between {beginItemNumber} and {endItemNumber}
                            and Success is null	
            ) S
    on      ( T.IntersectTypeID = {rt.ID} and T.Subject = S.Subject and T.SubjectID = S.SubjectID and T.Object = S.Object and T.ObjectID = S.ObjectID )
    when matched then
	    update set
			    T.UpdatedBy = {CurrentResourceID},
			    T.UpdatedOn = getutcdate()
    when not matched by target then
	    insert  (IntersectTypeID, Subject, SubjectID, Object, ObjectID, [State], CreatedBy, CreatedOn, UpdatedBy, UpdatedOn, [Owner])
	    values  ({rt.ID}, S.Subject, S.SubjectID, S.Object, S.ObjectID, 1, {CurrentResourceID}, getutcdate(), {CurrentResourceID}, getutcdate(), 'BULK_API')
    output inserted.ID, S.ItemNumber, $action into #ObjectMergeTableResult;

    update	T
    set		T.IntersectID = S.ID,
		    T.IsNew = IIF(S.[Action] = 'I', 1, 0)
    from	api.ExecutionRelationship T
		    inner join #ObjectMergeTableResult S on T.ExecutionID = @ExecutionID and S.ItemNumber = T.ItemNumber
    where   T.ItemNumber between {beginItemNumber} and {endItemNumber};", new { execution.ExecutionID }, transaction: trans, commandTimeout: timeout);

                        #endregion

                        #region Field table merge

                        (Database.Connection as SqlConnection).Execute($@"
merge into  Field T
using       (
            select  distinct 
                    A.IntersectID as ObjectID, 
                    F.FieldTypeID,
                    coalesce(F.LookupValue, F.FieldValue) as Value
            from    api.ExecutionRelationship A
                    inner join api.ExecutionField F on F.ExecutionID = A.ExecutionID
                        and F.ItemNumber = A.ItemNumber 
                        and A.ObjectID is not null 
                        and F.FieldTypeID is not null
						and A.Success is null	
            where   A.ExecutionID = @ExecutionID
                    and A.ItemNumber between {beginItemNumber} and {endItemNumber}
            ) S
on          (
                T.FieldTypeID = S.FieldTypeID and 
                T.ObjectType = 'Intersect' and 
				T.ObjectID = S.ObjectID
            )
when		matched then
update		set
				T.Value = S.Value
when		not matched by target then
insert		(FieldTypeID, ObjectType, ObjectID, Value)
values		(S.FieldTypeID, 'Intersect', S.ObjectID, S.Value);", new { execution.ExecutionID }, transaction: trans, commandTimeout: timeout);

                        #endregion

                        #region Update success flag

                        (Database.Connection as SqlConnection).Execute($@"
update	api.ExecutionRelationship
set		Success = 1
where	Success is null
		and ExecutionID = @ExecutionID 
        and ItemNumber between {beginItemNumber} and {endItemNumber} 
        and IntersectID is not null;", new { execution.ExecutionID }, transaction: trans, commandTimeout: timeout);

                        #endregion

                        trans.Commit();
                    }
                    catch (Exception ex)
                    {
                        trans.Rollback();
                        throw ex;
                    }
                }

                results.AddRange(
                    Query<DatabaseBulkRelationshipResult>(
                        $"select * from api.ExecutionRelationship where ExecutionID = @ExecutionID and ItemNumber between {beginItemNumber} and {endItemNumber}",
                        new { execution.ExecutionID }
                    )
                );

                OnRelationshipsPartiallyProcessed(new RelationshipsPartiallyProcessedEventArgs {
                    Results = results
                });

                beginItemNumber += loopSize;
                endItemNumber += loopSize;
            }

            (Database.Connection as SqlConnection).Close();

            #region Send Relationship Events

            try
            {
                var events = new List<EventInfo>();
                foreach (var result in results)
                {
                    if (result.Success)
                    {
                        var changeType = result.IsNew ? ChangeType.Add : ChangeType.Update;

                        events.Add(new EventInfo
                        {
                            CompanyID = CurrentCompanyID,
                            DomainPrefix = CurrentCompanyDomain,
                            ResourceID = CurrentResourceID,
                            Action = changeType,
                            Object = new EventObjectInfo
                            {
                                Object = SystemObjects.Intersect,
                                ObjectType = SystemObjects.IntersectType,
                                ObjectID = result.IntersectID,
                                ObjectTypeID = rt.ID
                            }
                        });

                        if (events.Count > 50)
                        {
                            QueueSource.CreateTopicMessages(events);
                            events.Clear();
                        }
                    }
                }

                if (events.Count > 0)
                {
                    QueueSource.CreateTopicMessages(events);
                    events.Clear();
                }
            }
            catch (Exception)
            {

            }

            #endregion

            return results;
        }
    }
}
