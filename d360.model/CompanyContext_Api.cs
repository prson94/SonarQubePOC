using d360.core;
using d360.core.entities;
using d360.core.enums;
using d360.core.enums.Workflow;
using d360.core.queue;
using Dapper;
using Microsoft.Practices.EnterpriseLibrary.TransientFaultHandling;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace d360.model
{
    public class RelationshipsPartiallyProcessedEventArgs : EventArgs
    {
        public List<DatabaseBulkRelationshipResult> Results { get; set; }
    }

    public class AssetsPartiallyProcessedEventArgs : EventArgs
    {
        public List<DatabaseBulkAssetResult> Results { get; set; }
    }

    internal class CurrentExecutionLocationModel
    {
        public Guid ExecutionID { get; set; }
        public int HighestItemNumber { get; set; }
        public int HighestItemNumberProcessed { get; set; }
    }

    partial class CompanyContext : BaseContext
    {
        internal const int API_V2_RETRY_LIMIT = 10;
        internal const int API_V2_RETRY_INTERVAL = 100; // interval set in ms

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

        public event EventHandler<AssetsPartiallyProcessedEventArgs> AssetsPartiallyProcessed;
        protected virtual void OnAssetsPartiallyProcessed(AssetsPartiallyProcessedEventArgs e)
        {
            AssetsPartiallyProcessed?.Invoke(this, e);
        }

        public event EventHandler<RelationshipsPartiallyProcessedEventArgs> RelationshipsPartiallyProcessed;
        protected virtual void OnRelationshipsPartiallyProcessed(RelationshipsPartiallyProcessedEventArgs e)
        {
            RelationshipsPartiallyProcessed?.Invoke(this, e);
        }

        #endregion

        #region Utility Methods

        private PredicateType? DeterminePredicateType(string obj)
        {
            PredicateType? predicateType = null;
            switch (obj)
            {
                case "ArtifactType":
                case "FusionAttributeType":
                case "ReferenceItemType":
                    predicateType = PredicateType.InterTypeHierarchy;
                    break;
                case "PolicyType":
                case "TaxonomyType":
                    predicateType = PredicateType.IntraTypeHierarchy;
                    break;
            }

            return predicateType;
        }

        private CurrentExecutionLocationModel GetCurrentExecutionLocation(Guid executionID, string targetTable)
        {
            return Connection.Query<CurrentExecutionLocationModel>($@"
select	E.ExecutionID,
		coalesce(T.ItemNumber, 0) as HighestItemNumber,
		coalesce(C.ItemNumber, 0) as HighestItemNumberProcessed
from	api.Execution E
		outer apply (
			select	max(ItemNumber) as ItemNumber
			from	{targetTable} A
			where	ExecutionID = E.ExecutionID
		) T
		outer apply (
			select	max(ItemNumber) as ItemNumber
			from	{targetTable} A
			where	ExecutionID = E.ExecutionID
					and Success is not null
		) C
where	E.ExecutionID = @executionID;",
         new { executionID }).SingleOrDefault();
        }

        private void LoadMissingKeyFields(Guid executionID, AssetType at, int timeout = 3600)
        {
            Connection.Execute(@"
insert into [api].[ExecutionField] (ExecutionID, ItemNumber, FieldName, FieldValue, FieldTypeID, LookupValue, Ignore)
	select	A.ExecutionID,
            A.ItemNumber,
			FT.Name,
			EF.FormattedValue,
			FT.ID,
			EF.Value,
			1
	from	[api].[ExecutionAsset] A
			inner join FieldType FT on FT.AssetTypeID = @assetTypeID 
										and FT.IsPartOfKey = 1
			inner join Field EF on EF.FieldTypeID = FT.ID and EF.AssetID = A.AssetID
			left join [api].[ExecutionField] F on F.ExecutionID = A.ExecutionID and F.ItemNumber = A.ItemNumber and F.FieldTypeID = FT.ID
	where	A.ExecutionID = @executionID 
            and F.ItemNumber is null;

update  T
set     T.ParentUid = S.Uid,
        T.ParentObject = S.Object,
        T.ParentObjectID = S.ObjectID
from    api.ExecutionAsset T
        inner join [Intersect] I on T.ExecutionID = @executionID and I.IntersectTypeID = T.IntersectTypeID and I.Object = T.Object and I.ObjectID = T.ObjectID and T.ParentUid is null
        inner join Asset S on S.Object = I.Subject and S.ObjectID = I.SubjectID;",
            new { executionID, assetTypeID = at.ID }, commandTimeout: timeout);

            if (at.Class == AssetTypeClass.Reference)
            {
                Connection.Execute(@"
insert into [api].[ExecutionField] (ExecutionID, ItemNumber, FieldName, FieldValue, FieldTypeID, LookupValue, Ignore)
	select	A.ExecutionID,
            A.ItemNumber,
			'Code',
			R.Code,
			0,
			R.Code,
			1
	from	[api].[ExecutionAsset] A
            inner join Asset R on A.Object =  R.Object and R.ObjectID = A.ObjectID
			left join [api].[ExecutionField] F on F.ExecutionID = A.ExecutionID and F.ItemNumber = A.ItemNumber and F.FieldName = 'Code'
	where	A.ExecutionID = @executionID 
	and A.Object = 'ReferenceItem' 
    and F.ItemNumber is null;",
                new { executionID }, commandTimeout: timeout);
            }

            if (at.Class == AssetTypeClass.FusionAttribute)
            {
                Connection.Execute(@"
insert into [api].[ExecutionField] (ExecutionID, ItemNumber, FieldName, FieldValue, FieldTypeID, LookupValue, Ignore)
	select	A.ExecutionID,
            A.ItemNumber,
			'Name',
			R.Name,
			0,
			R.Name,
			1
	from	[api].[ExecutionAsset] A
            inner join FusionAttribute R on A.Object = 'FusionAttribute' and R.ID = A.ObjectID
			left join [api].[ExecutionField] F on F.ExecutionID = A.ExecutionID and F.ItemNumber = A.ItemNumber and F.FieldName = 'Name'
	where	A.ExecutionID = @executionID 
            and F.ItemNumber is null;",
                new { executionID }, commandTimeout: timeout);

                Connection.Execute(@"
insert into [api].[ExecutionField] (ExecutionID, ItemNumber, FieldName, FieldValue, FieldTypeID, LookupValue, Ignore)
	select	A.ExecutionID,
            A.ItemNumber,
			'FusionID',
			R.FusionID,
			0,
			R.FusionID,
			1
	from	[api].[ExecutionAsset] A
            inner join FusionAttribute R on A.Object = 'FusionAttribute' and R.ID = A.ObjectID
			left join [api].[ExecutionField] F on F.ExecutionID = A.ExecutionID and F.ItemNumber = A.ItemNumber and F.FieldName = 'FusionID'
	where	A.ExecutionID = @executionID 
            and F.ItemNumber is null;",
                new { executionID }, commandTimeout: timeout);
            }
        }

        private void LogAssetErrors(Guid executionID, int timeout = 3600)
        {
            Connection.Execute(@"
update	api.ExecutionAsset
set		Success = 0,
		[Message] = coalesce([Message] + '; ', '') + 'Asset cannot be found based on Uid value'
where	ExecutionID = @executionID
        and AssetID is null
		and Uid is not null;",
            new { executionID }, commandTimeout: timeout);
        }

        private void LogAssetPermissionErrors(Guid executionID, AssetType at, Permission p, string apiTableName, int timeout = 3600)
        {
            if (string.IsNullOrEmpty(apiTableName))
            {
                throw new ApplicationException("Endpoint logic is misconfigured, and is missing an API table name.");
            }
            if (!CurrentResourceIsAdmin)
            {
                Connection.Execute($@"
    declare @hasAssetTypePermission bit = 0

    select @hasAssetTypePermission = case when exists (select AssetTypeID from UserAssetPermissions(@resourceID, @assetTypeID) where PermissionsBitMask & @p = @p and AssetID = 0) then 1 else 0 end

    if @hasAssetTypePermission = 0
    begin
	    update	T
	    set		T.Success = 0,
			    T.[Message] = coalesce([Message] + '; ', '') + 'User does not have permission to update this asset.'
	    from    api.{apiTableName} T
			    inner join api.Execution E on E.ExecutionID = T.ExecutionID 
											    and E.ExecutionID = @executionID 
											    and T.AssetID is not null
											    and T.AssetID not in (select AssetID from UserAssetPermissions(E.ResourceID, @assetTypeID) where PermissionsBitMask & @p = @p)
    end", new { executionID, assetTypeID = at.ID, p = (int)p, resourceID = CurrentResourceID }, commandTimeout: timeout);
            }
        }

        private void LogParentErrors(Guid executionID, int timeout = 3600)
        {
            Connection.Execute(@"
update	api.ExecutionAsset
set		Success = 0,
		[Message] = coalesce([Message] + '; ', '') + 'Asset does not contain a valid ParentUid value'
where	ExecutionID = @executionID
        and ParentAssetID is null
		and ParentUid is not null;",
            new { executionID }, commandTimeout: timeout);
        }

        private void LogErrorsWhereChildFusionConfigDifferentFromParent(Guid executionID, int timeout = 3600)
        {
            Connection.Execute(@"
update	E
set		E.Message = 'Unable to add or update child asset as the fusion configuration does not match it''s parent''s configuration.',
		E.Success = 0
from	api.ExecutionAsset E
		inner join FusionAttribute P on P.ID = E.ParentObjectID and E.ParentObject = 'FusionAttribute'
		inner join api.ExecutionField C on C.ExecutionID = E.ExecutionID and C.FieldName = 'FusionID' and C.FieldValue <> P.FusionID
where	E.ExecutionID = @executionID;",
            new { executionID }, commandTimeout: timeout);
        }

        private void LogFieldLookupErrors(Guid executionID, string obj, int objID, string errorPrefix, int timeout = 3600)
        {
            string targetTable = "api.ExecutionRelationship";
            if (obj != "IntersectType") targetTable = "api.ExecutionAsset";
            Connection.Execute($@"
update	T
set		T.Success = 0,
		T.[Message] = coalesce(T.[Message] + '; ', '') + '{errorPrefix} contains one or more fields with invalid lookup values: [' + S.Names + ']'
from	{targetTable} T
		inner join	(
					select		A.ExecutionID,
                                A.ItemNumber,
								STRING_AGG(FT.Name+'='+F.FieldValue, ', ') as Names
					from		{targetTable} A
								inner join FieldType FT on FT.Object = @obj
															and FT.ObjectID = @objID
															and FT.[Type] = 'Lookup'
								inner join api.ExecutionField F on F.ExecutionID = A.ExecutionID and F.ItemNumber = A.ItemNumber and F.FieldTypeID = FT.ID and F.LookupValue is null and (F.FieldValue != '' or FT.IsRequired = 1)
                    where       A.ExecutionID = @executionID
					group by	A.ExecutionID, A.ItemNumber
					) S on S.ExecutionID = T.ExecutionID and S.ItemNumber = T.ItemNumber;
", new { executionID, obj, objID }, commandTimeout: timeout);
        }

        private void LogRelationshipErrors(Guid executionID, string obj, int objID, string errorPrefix, int timeout = 3600, bool lookupFieldsPassedByValue = false)
        {
            string targetTable = "api.ExecutionRelationship";
            if (obj != "IntersectType") targetTable = "api.ExecutionAsset";

            if (!lookupFieldsPassedByValue)
            {
                Connection.Execute($@"
                    update	T
                    set		T.Success = 0,
		                    T.[Message] = coalesce(T.[Message] + '; ', '') + '{errorPrefix} contains one or more fields with invalid relationship values: [' + S.Names + ']'
                    from	{targetTable} T
		                    inner join	(
					                    select		A.ExecutionID,
                                                    A.ItemNumber,
								                    STRING_AGG(FT.Name+'='+F.FieldValue, ', ') as Names
					                    from		{targetTable} A
                                                    inner join FieldType FT on FT.Object = @obj
								                        and FT.ObjectID = @objID
									                    and FT.[Type] = 'Relationship' and FT.LookupObjectType ='IntersectType'
								                    inner join api.ExecutionField F on F.ExecutionID = A.ExecutionID and F.ItemNumber = A.ItemNumber and F.FieldTypeID = FT.ID and F.LookupValue is null and (F.FieldValue != '' or FT.IsRequired = 1)
								                    cross apply string_split(F.FieldValue,',') V
                                                    inner join IntersectType IT on IT.ID = FT.LookupObjectID
								                    left join AssetDetail AD on AD.DisplayValue = V.[value]
									                    and ((AD.Type = IT.Object AND AD.TypeID = IT.ObjectID) 
									                    or (AD.Type = IT.Subject AND AD.TypeID = IT.SubjectId))
                                        where       A.ExecutionID = @executionID and AD.ID IS NULL
					                    group by	A.ExecutionID, A.ItemNumber
					                    ) S on S.ExecutionID = T.ExecutionID and S.ItemNumber = T.ItemNumber;
                    ", new { executionID, obj, objID }, commandTimeout: timeout);
            }
            else
            {
                Connection.Execute($@"
                    update	T
                    set		T.Success = 0,
		                    T.[Message] = coalesce(T.[Message] + '; ', '') + '{errorPrefix} contains one or more fields with invalid relationship values: [' + S.Names + ']'
                    from	{targetTable} T
		                    inner join	(
					                    select		A.ExecutionID,
                                                    A.ItemNumber,
								                    STRING_AGG(FT.Name+'='+F.FieldValue, ', ') as Names
					                    from		{targetTable} A
                                                    inner join FieldType FT on FT.Object = @obj
								                        and FT.ObjectID = @objID
									                    and FT.[Type] = 'Relationship' and FT.LookupObjectType ='IntersectType'
								                    inner join api.ExecutionField F on F.ExecutionID = A.ExecutionID and F.ItemNumber = A.ItemNumber and F.FieldTypeID = FT.ID and F.LookupValue is null and (F.FieldValue != '' or FT.IsRequired = 1)
                                                    cross apply string_split(F.FieldValue, ',') V								                    
                                                    inner join IntersectType IT on IT.ID = FT.LookupObjectID
								                    left join AssetDetail AD on AD.ObjectID = cast(V.[value] as int)
									                    and ((AD.Type = IT.Object AND AD.TypeID = IT.ObjectID) 
									                    or (AD.Type = IT.Subject AND AD.TypeID = IT.SubjectId))
                                        where       A.ExecutionID = @executionID and AD.ID IS NULL
					                    group by	A.ExecutionID, A.ItemNumber
					                    ) S on S.ExecutionID = T.ExecutionID and S.ItemNumber = T.ItemNumber;
                    ", new { executionID, obj, objID }, commandTimeout: timeout);
            }
        }

        private void LogLoopExecutionError(Guid executionID, int beginItemNumber, int endItemNumber, string targetTable, string msg, int timeout = 3600)
        {
            Connection.Execute($@"
update	api.Execution
set		[ErrorMessage] = coalesce([ErrorMessage],'') + @msg
where	ExecutionID = @executionID; 

update	{targetTable} 
set		Success = 0,
		[Message] = @msg
where	ExecutionID = @executionID 
         and ItemNumber between @beginItemNumber and @endItemNumber;",
         new { executionID, msg, beginItemNumber, endItemNumber }, commandTimeout: timeout);
        }

        private void DeleteEmptyAssetListFieldByApiExecutionUid(Guid executionUid, SqlTransaction trans, int beginItemNumber, int endItemNumber, int timeout = 3600)
        {
            Connection.Execute(@"delete F from Field F
	                                inner join api.ExecutionAsset EA on EA.ExecutionID = @executionUid
                                    inner join FieldType FT on F.FieldTypeID = FT.ID
	                                where 
                                        FT.[Type] = 'Lookup'
                                      and F.ObjectType = EA.Object 
                                      and F.ObjectId = EA.ObjectID 
                                      and EA.ItemNumber between @beginItemNumber and @endItemNumber
                                      and F.Value = ''", new { executionUid, beginItemNumber, endItemNumber }, transaction: trans, commandTimeout: timeout);
        }

        private void MergeAssetDisplayValues(Guid executionID, SqlTransaction trans, int beginItemNumber, int endItemNumber, int timeout = 3600)
        {
            Connection.Execute($@"
merge       AssetDisplayValue as T
using       (
                select  A.AssetID as ID,
                        ADV.DisplayValue,
                        CONVERT(NVARCHAR(32), HashBytes('SHA1', ADV.DisplayValue), 2) as DisplayValueHash,
                        SUBSTRING(ADV.DisplayValue, 1, 250) as DisplayValuePrefix
                from    api.ExecutionAsset A
                        cross apply GetAssetDisplayValueByID(A.AssetID) ADV
                where   A.ExecutionID = @executionID
                        and A.ItemNumber between @beginItemNumber and @endItemNumber 
                        and A.Success is null 
                        and A.[Object] not in( 'FusionAttribute', 'FusionQueryAttribute')
                        and ADV.DisplayValue is not null
            ) as S 
on          ( T.AssetID = S.ID )
when		matched then
update		set
				T.DisplayValue = S.DisplayValue,
                T.DisplayValueHash = S.DisplayValueHash,
                T.[DisplayValuePrefix] = S.DisplayValuePrefix,
                T.UpdatedOn = @dt
when		not matched by target then
insert		(AssetID, DisplayValue, DisplayValueHash, DisplayValuePrefix, UpdatedOn)
values		(S.ID, S.DisplayValue, S.DisplayValueHash, S.DisplayValuePrefix, @dt);",
            new { executionID, r = CurrentResourceID, dt = DateTime.UtcNow, beginItemNumber, endItemNumber }, transaction: trans, commandTimeout: timeout);
        }

        private List<AssetFieldTypeUpdate> MergeFields(Guid executionID, SqlTransaction trans, string tableName, string objectSqlSyntax, string objectIdSqlSyntax, int beginItemNumber, int endItemNumber, bool sendWorkflowEvents, int timeout = 3600)
        {
            return Connection.Query<AssetFieldTypeUpdate>($@"
select EA.Object, EA.ObjectID, EF.FieldTypeID AS Id from api.ExecutionAsset EA 
	inner join api.ExecutionField EF on EF.ExecutionID = EA.ExecutionID 
                        and EF.ItemNumber = EA.ItemNumber 
                        and EA.ObjectID is not null 
                        and EF.FieldTypeID is not null
	inner join Field F on F.FieldTypeId = EF.FieldTypeID and F.ObjectType = EA.Object and F.ObjectId = EA.ObjectID
where EA.ExecutionID = @executionID and EA.IsNew <> 1 and F.Value <> EF.FieldValue and @sendWorkflowEvents = 1

merge       Field as T
using       (
            select  distinct 
                    {objectSqlSyntax}, 
                    {objectIdSqlSyntax}, 
                    F.FieldTypeID,
                    coalesce(F.LookupValue, F.FieldValue) as Value,
                    F.FieldValue as FormattedValue
            from    {tableName} A
                    inner join api.ExecutionField F on F.ExecutionID = A.ExecutionID
                        and F.ItemNumber = A.ItemNumber 
                        and A.ObjectID is not null 
                        and F.FieldTypeID is not null
						and A.Success is null
                    inner join FieldType FT on FT.Id = F.FieldTypeID
            where   A.ExecutionID = @executionID
                    and A.ItemNumber between @beginItemNumber and @endItemNumber 
                    and (F.Ignore = 0 or F.Ignore is null)
                    and FT.Type != 'Relationship'
            ) as S 
on          ( T.FieldTypeID = S.FieldTypeID and T.ObjectType = S.Object and T.ObjectID = S.ObjectID )
when		matched and T.Value <> S.Value COLLATE SQL_Latin1_General_CP1_CS_AS OR T.FormattedValue <> S.FormattedValue COLLATE SQL_Latin1_General_CP1_CS_AS then
update		set
				T.Value = S.Value,
                T.FormattedValue = S.FormattedValue
when		not matched by target then
insert		(FieldTypeID, ObjectType, ObjectID, Value, FormattedValue)
values		(S.FieldTypeID, S.Object, S.ObjectID, S.Value, S.FormattedValue);",
            new { executionID, sendWorkflowEvents, beginItemNumber, endItemNumber }, transaction: trans, commandTimeout: timeout).ToList();
        }

        private void ImportRelationships(Guid executionID, SqlTransaction trans, string tableName, string objectSqlSyntax, string objectIdSqlSyntax, int beginItemNumber, int endItemNumber, int timeout = 3600, bool resolveRelationshipOnObjectId = false)
        {
            if (!resolveRelationshipOnObjectId)
            {
                Connection.Execute($@"
                begin
	                drop table if exists #Relationships;
	                create table #Relationships
	                (
		                ID int,
		                IntersectTypeID int,
		                [Subject] varchar(50),
		                SubjectID int,
		                [Object] varchar(50),
		                ObjectID int
	                )
                    ;with R
                        as (
                            select  distinct 
                                    A.Object,
                                    A.ObjectID,
                                    FT.LookupObjectId as IntersectTypeId,
                                    AD.Object as Subject,
                                    AD.ObjectId as SubjectId,
                                    case 
                                    when AD.Type = IT.Object AND AD.TypeID = IT.ObjectID then 0
                                    else 1
                                    end as switchObject
                            from    {tableName} A
                                    inner join api.ExecutionField F on F.ExecutionID = A.ExecutionID
                                        and F.ItemNumber = A.ItemNumber 
                                        and A.ObjectID is not null 
                                        and F.FieldTypeID is not null
						                and A.Success is null
                                    cross apply string_split(F.FieldValue, ',') V                                    
                                    inner join FieldType FT on FT.ID = F.FieldTypeID AND FT.Type = 'Relationship' AND FT.LookupObjectType = 'IntersectType'
                                    inner join IntersectType IT on IT.ID = FT.LookupObjectId
                                    inner join AssetDetail AD on AD.DisplayValue = V.[value]
                                            and ((AD.Type = IT.Object AND AD.TypeID = IT.ObjectID) 
                                                or (AD.Type = IT.Subject AND AD.TypeID = IT.SubjectId))
                            where   A.ExecutionID = @executionID
                                    and A.ItemNumber between @beginItemNumber and @endItemNumber 
                                    and (F.Ignore = 0 or F.Ignore is null)
                                    and FT.Type = 'Relationship'
                            )
                            insert into #Relationships (ID, IntersectTypeID, Subject, SubjectId, Object, ObjectID)
                            select
                                null as ID,
			                    IntersectTypeId, 
			                    CASE 
				                    when switchObject = 0 then Subject
				                    else Object
			                    END AS Subject, 
			                    CASE 
				                    when switchObject = 0 then SubjectId
				                    else ObjectID
			                    END AS SubjectId,
			                    CASE 
				                    when switchObject = 0 then Object
				                    else Subject
			                    END AS Object, 
			                    CASE 
				                    when switchObject = 0 then ObjectId
				                    else SubjectId
			                    END AS ObjectID
			                from R;

                            update R
                            set R.ID = I.ID
                            from #Relationships R
                            inner join [Intersect] I on 
                                I.IntersectTypeID = R.IntersectTypeID 
                                and I.[Subject] = R.[Subject] 
                                and I.SubjectID = R.SubjectID 
                                and I.[Object] = R.[Object] 
                                and I.ObjectID = R.ObjectID;

                            delete I
			                from [Intersect] I
			                inner join #Relationships R on R.IntersectTypeID = I.IntersectTypeID and R.[Object] = I.[Object] and R.ObjectID = I.ObjectID
			                where not exists (select 1 from #Relationships where [Subject] = I.[Subject] and SubjectID = I.SubjectID);

                            delete I
			                from [Intersect] I
			                inner join #Relationships R on R.IntersectTypeID = I.IntersectTypeID and R.[Subject] = I.[Subject] and R.SubjectID = I.SubjectID
			                where not exists (select 1 from #Relationships where [Object] = I.[Object] and ObjectID = I.ObjectID);

                            insert into [Intersect] (IntersectTypeID, Subject, SubjectId, Object, ObjectID)
                            select  IntersectTypeID,
                                    Subject,
                                    SubjectID,
                                    Object,
                                    ObjectID    
                            from    #Relationships
                            where  ID is null;
                end
",
                new { executionID, beginItemNumber, endItemNumber }, transaction: trans, commandTimeout: timeout);
            }
            else
            {
                Connection.Execute($@"
begin
	                drop table if exists #Relationships;
	                create table #Relationships
	                (
		                ID int,
		                IntersectTypeID int,
		                [Subject] varchar(50),
		                SubjectID int,
		                [Object] varchar(50),
		                ObjectID int
	                )
;with R
    as (
            select  distinct 
                    A.Object,
                    A.ObjectID,
                    FT.LookupObjectId as IntersectTypeId,
                    AD.Object as Subject,
                    AD.ObjectId as SubjectId,
                    case 
                    when AD.Type = IT.Object AND AD.TypeID = IT.ObjectID then 0
                    else 1
                    end as switchObject
            from    {tableName} A
                    inner join api.ExecutionField F on F.ExecutionID = A.ExecutionID
                        and F.ItemNumber = A.ItemNumber 
                        and A.ObjectID is not null 
                        and F.FieldTypeID is not null
						and A.Success is null
                    cross apply string_split(F.FieldValue, ',') V
                    inner join FieldType FT on FT.ID = F.FieldTypeID AND FT.Type = 'Relationship' AND FT.LookupObjectType = 'IntersectType'
                    inner join IntersectType IT on IT.ID = FT.LookupObjectId
                    inner join AssetDetail AD on AD.ObjectID = cast(V.[value] as int)
                            and ((AD.Type = IT.Object AND AD.TypeID = IT.ObjectID) 
                                or (AD.Type = IT.Subject AND AD.TypeID = IT.SubjectId))
            where   A.ExecutionID = @executionID
                    and A.ItemNumber between @beginItemNumber and @endItemNumber 
                    and (F.Ignore = 0 or F.Ignore is null)
                    and FT.Type = 'Relationship'
            )
            insert into #Relationships (ID, IntersectTypeID, Subject, SubjectId, Object, ObjectID)
            select
                null as ID,
			    IntersectTypeId, 
			    CASE 
				    when switchObject = 0 then Subject
				    else Object
			    END AS Subject, 
			    CASE 
				    when switchObject = 0 then SubjectId
				    else ObjectID
			    END AS SubjectId,
			    CASE 
				    when switchObject = 0 then Object
				    else Subject
			    END AS Object, 
			    CASE 
				    when switchObject = 0 then ObjectId
				    else SubjectId
			    END AS ObjectID
			from R;

                    update R
                    set R.ID = I.ID
                    from #Relationships R
                    inner join [Intersect] I on 
                        I.IntersectTypeID = R.IntersectTypeID 
                        and I.[Subject] = R.[Subject] 
                        and I.SubjectID = R.SubjectID 
                        and I.[Object] = R.[Object] 
                        and I.ObjectID = R.ObjectID;

                    delete I
			        from [Intersect] I
			        inner join #Relationships R on R.IntersectTypeID = I.IntersectTypeID and R.[Object] = I.[Object] and R.ObjectID = I.ObjectID
			        where not exists (select 1 from #Relationships where [Subject] = I.[Subject] and SubjectID = I.SubjectID);

                    delete I
			        from [Intersect] I
			        inner join #Relationships R on R.IntersectTypeID = I.IntersectTypeID and R.[Subject] = I.[Subject] and R.SubjectID = I.SubjectID
			        where not exists (select 1 from #Relationships where [Object] = I.[Object] and ObjectID = I.ObjectID);


                    insert into [Intersect] (IntersectTypeID, Subject, SubjectId, Object, ObjectID)
                    select  IntersectTypeID,
                            Subject,
                            SubjectID,
                            Object,
                            ObjectID    
                    from    #Relationships
                    where  ID is null;
                end",
                new { executionID, beginItemNumber, endItemNumber }, transaction: trans, commandTimeout: timeout);
            }
        }

        private void MergeJsonFieldProperties(Guid executionID, SqlTransaction trans, List<FieldType> jsonFieldTypes, string tableName, string objectSqlSyntax, string objectIdSqlSyntax, int beginItemNumber, int endItemNumber, int timeout = 3600, bool fieldJsonPropertyLoadLimitToTopLevel = true)
        {
            var jsonFieldTypeIDs = string.Join(",", jsonFieldTypes.Select(i => i.ID));
            var fields = Connection.Query<dynamic>($@"
select  F.ID, 
        F.Value 
from    Field F 
        inner join api.ExecutionField E on E.ExecutionID = @executionID and E.ItemNumber between @beginItemNumber and @endItemNumber and E.FieldTypeID = F.FieldTypeID and E.FieldTypeID in ({jsonFieldTypeIDs})
        inner join {tableName} A on A.ExecutionID = E.ExecutionID and A.ItemNumber = E.ItemNumber and A.Object = F.ObjectType and A.ObjectID = F.ObjectID",
        new { executionID, beginItemNumber, endItemNumber }, transaction: trans, commandTimeout: timeout);

            var collectionFieldroperties = new List<FieldJsonProperty>();

            foreach (var f in fields)
            {
                string value = f.Value;
                if (!string.IsNullOrEmpty(value))
                {
                    List<FieldJsonProperty> assetFieldProperties = value.ParseJsonIntoJsonPropertiesCollection(fieldJsonPropertyLoadLimitToTopLevel);
                    assetFieldProperties.ForEach(i =>
                    {
                        i.FieldID = f.ID;
                    });
                    collectionFieldroperties.AddRange(assetFieldProperties);
                }

            }

            #region Build data tables for bulk load.

            var table = new DataTable();
            table.Columns.Add("FieldID", typeof(long));
            table.Columns.Add("Name", typeof(string));
            table.Columns.Add("Parent", typeof(string));
            table.Columns.Add("Path", typeof(string));
            table.Columns.Add("Position", typeof(int));
            table.Columns.Add("IsArray", typeof(bool));
            table.Columns.Add("Value", typeof(string));

            foreach (var f in collectionFieldroperties)
            {
                var row = table.NewRow();

                row["FieldID"] = f.FieldID;
                row["Name"] = f.Name;
                row["Parent"] = f.Parent + "";
                row["Path"] = f.Path;
                row["Position"] = f.Position;
                row["IsArray"] = f.IsArray;
                row["Value"] = f.Value;

                table.Rows.Add(row);
            }

            Connection.Execute($@"
drop table if exists #FieldJsonProperty;
CREATE TABLE #FieldJsonProperty (
	[FieldID] bigint NOT NULL,
	[Name] nvarchar(250) NOT NULL,
	[Parent] nvarchar(250) NOT NULL,
	[Path] nvarchar(500) NOT NULL,
	[Position] int NOT NULL,
	[IsArray] bit NOT NULL,
	[Value] nvarchar(2500) NULL,
)", new { executionID }, transaction: trans);

            var bulkCopy = new SqlBulkCopy((SqlConnection)Database.Connection, SqlBulkCopyOptions.Default, trans)
            {
                BatchSize = table.Rows.Count,
                DestinationTableName = "#FieldJsonProperty",
                BulkCopyTimeout = timeout
            };

            bulkCopy.ColumnMappings.Add("FieldID", "FieldID");
            bulkCopy.ColumnMappings.Add("Name", "Name");
            bulkCopy.ColumnMappings.Add("Parent", "Parent");
            bulkCopy.ColumnMappings.Add("Path", "Path");
            bulkCopy.ColumnMappings.Add("Position", "Position");
            bulkCopy.ColumnMappings.Add("IsArray", "IsArray");
            bulkCopy.ColumnMappings.Add("Value", "Value");

            bulkCopy.WriteToServer(table);

            bulkCopy = null;

            #endregion

            Connection.Execute($@"
merge       FieldJsonProperty as T
using       #FieldJsonProperty as S 
on          ( T.FieldID = S.FieldID and T.Position = S.Position and T.Parent = S.Parent and T.Name = S.Name )
when		matched then
update		set
				T.Value = S.Value,
                T.IsArray = S.IsArray,
                T.[Path] = S.[Path],
                T.UpdatedBy = @r,
                T.UpdatedOn = @dt
when		not matched by source and T.FieldID in (select FieldID from #FieldJsonProperty) then
delete
when		not matched by target then
insert		(FieldID, Name, Parent, [Path], Position, IsArray, Value, CreatedBy, CreatedOn, UpdatedBy, UpdatedOn)
values		(S.FieldID, S.Name, S.Parent, S.[Path], S.Position, S.IsArray, S.Value, @r, @dt, @r, @dt);",
            new { executionID, r = CurrentResourceID, dt = DateTime.UtcNow }, transaction: trans, commandTimeout: timeout);
        }

        private void CopyFieldLookupValuesAsIs(Guid executionID, int timeout = 3600)
        {
            Connection.Execute(@"
        update	T
        set		T.LookupValue = T.[FieldValue]
        from	api.ExecutionField T
		inner join FieldType ST on ST.ID = T.FieldTypeID and ST.[Type] = 'Lookup' and T.ExecutionID = @executionID
            ", new { executionID }, commandTimeout: timeout);
        }

        private void ResolveFieldLookupValues(Guid executionID, int timeout = 3600)
        {
            Connection.Execute(@"
drop table if exists #LookupValues
create table #LookupValues (ItemNumber int, FieldTypeID int not null, FieldValue nvarchar(max) not null, [Value] nvarchar(max) null)
CREATE CLUSTERED INDEX CIX_TempLookupValues ON #LookupValues ( FieldTypeID ASC );
		
insert into #LookupValues
	select		T.ItemNumber,
				T.FieldTypeID,
				T.FieldValue,
                null
	from		api.ExecutionField T
				inner join FieldType F on F.ID = T.FieldTypeID and F.[Type] = 'Lookup' and T.ExecutionID = @executionID
	group by	T.ItemNumber,
				T.FieldTypeID,
				T.FieldValue;

update	T
set		T.[Value] = S.[Value]
from	#LookupValues T
		inner join FieldType ST on ST.ID = T.FieldTypeID and ST.AllowMultipleValues = 0
		inner join FieldLookupValue S on S.FieldTypeID = T.FieldTypeID and S.[Text] = T.FieldValue;

update	T
set		T.[Value] = '0'
from	#LookupValues T
		inner join FieldType ST on ST.ID = T.FieldTypeID and ST.AllowAllValue = 1 and ST.AllowAllLabel = T.FieldValue;

drop table if exists #MvLookupValues
create table #MvLookupValues (ItemNumber int, FieldTypeID int not null, [RawValue] nvarchar(250) null, [Value] nvarchar(max) null)
CREATE CLUSTERED INDEX CIX_TempMvLookupValues ON #MvLookupValues ( ItemNumber ASC, FieldTypeID ASC );
CREATE NONCLUSTERED INDEX IX_TempMvLookupValues_FieldTypeID_RawValue ON #MvLookupValues ( FieldTypeID ASC, RawValue ASC );

insert into #MvLookupValues (ItemNumber, FieldTypeID, [RawValue])
	select		T.ItemNumber,
				T.FieldTypeID,
				rtrim(ltrim(MV.Value))
	from		#LookupValues T
				inner join FieldType ST on ST.ID = T.FieldTypeID and ST.AllowMultipleValues = 1
				cross apply string_split(T.FieldValue, ',') MV;

update	T
set		T.Value = S.Value
from	#MvLookupValues T
		inner join (
					select		top 100 percent
								T.ItemNumber,
								T.FieldTypeID,
								S.Value,
								S.[Text]
					from		#LookupValues T
								inner join FieldType ST on ST.ID = T.FieldTypeID and ST.AllowMultipleValues = 1
								cross apply string_split(T.FieldValue, ',') MV 
								inner join FieldLookupValue S on S.FieldTypeID = T.FieldTypeID and S.[Text] = MV.value
					group by	T.ItemNumber, T.FieldTypeID, S.Value, S.[Text]
					order by	T.ItemNumber, T.FieldTypeID, S.[Text]	
		) S on S.ItemNumber = T.ItemNumber and S.FieldTypeID = T.FieldTypeID and S.[Text] = T.[RawValue];

delete	T
from	#MvLookupValues T
		inner join	(
					select	* 
					from	#MvLookupValues
					where	Value is null
					) S on S.ItemNumber = T.ItemNumber and S.FieldTypeID = T.FieldTypeID

update	T
set		T.[Value] = S.[Value]
from	#LookupValues T
		inner join (
					select		ItemNumber,
								FieldTypeID,
								STRING_AGG(T.Value, ',') as Value
					from		#MvLookupValues T
					group by	ItemNumber,
								FieldTypeID
					) S on S.ItemNumber = T.ItemNumber and S.FieldTypeID = T.FieldTypeID;

update	T
set		T.LookupValue = S.[Value]
from	api.ExecutionField T
		inner join #LookupValues S on S.FieldTypeID = T.FieldTypeID and T.FieldValue = S.FieldValue and T.ExecutionID = @executionID;
", new { executionID }, commandTimeout: timeout);
        }

        private void ResolveRuleTypeLookupValues(Guid executionID, int timeout = 3600)
        {
            Connection.Execute(@"
                        update  T 
                        set     T.Success = 0,
                                T.Message = coalesce(T.Message, '') + 'Rule asset contains an invalid threshold; '
                        from    api.ExecutionAsset T
                                inner join api.ExecutionField S on S.ExecutionID = T.ExecutionID and T.ExecutionID = @executionID and S.ItemNumber = T.ItemNumber and S.FieldName = 'Threshold' and ISNUMERIC(S.FieldValue) = 0;
                        ", new { executionID }, commandTimeout: timeout);
        }

        private void SendWorkflowEvents(string objectType, int objectTypeID, IEnumerable<IWorkflowEnabledAsset> results, ChangeType? changeTypeOverride = null, List<AssetFieldTypeUpdate> fieldUpdates = null)
        {
            try
            {
                var events = new List<EventInfo>();
                foreach (var result in results)
                {
                    if (result.Success)
                    {
                        List<int> changedFieldsIDS = new List<int>();
                        if (fieldUpdates != null)
                        {
                            foreach (var ftUpdate in fieldUpdates.Where(x => x.Object == result.Object && x.ObjectId == result.ObjectID))
                            {
                                changedFieldsIDS.Add(ftUpdate.Id);
                            }
                        }

                        events.Add(new EventInfo
                        {
                            CompanyID = CurrentCompanyID,
                            DomainPrefix = CurrentCompanyDomain,
                            ResourceID = CurrentResourceID,
                            Action = changeTypeOverride ?? result.ChangeType,
                            Object = new EventObjectInfo
                            {
                                Object = (SystemObjects)Enum.Parse(typeof(SystemObjects), result.Object),
                                ObjectType = (SystemObjects)Enum.Parse(typeof(SystemObjects), objectType),
                                ObjectID = result.ObjectID,
                                ObjectTypeID = objectTypeID,
                                ChangedFieldIds = changedFieldsIDS
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
        }

        private void SendAssetGraphEvents(IEnumerable<IGraphAsset> results, Dictionary<Guid, List<string>> fields = null)
        {
            List<AssetEventInfo> events = new List<AssetEventInfo>();

            foreach (var result in results)
            {
                if (result.Success)
                {
                    events.Add(new AssetEventInfo
                    {
                        CompanyID = CurrentCompanyID,
                        Uid = result.uid,
                        ChangedFieldNames = (fields != null && fields.ContainsKey(result.uid)) ? fields[result.uid] : null,
                        Type = result.Object == "Intersect" ? AssetEventType.Edge : AssetEventType.Node
                    });
                }
            }

            _ = QueueSource.CreateTopicMessagesAsync<AssetEventInfo>(Config.GetValue<string>("AssetBusTopicName"), events);

        }

        #region Validation

        private List<DataRow> ValidateFields(
            string ot, int otid, bool isInsert,
            List<FieldType> fieldTypes, List<string> requiredFieldTypeNames,
            Dictionary<string, string> fields, Guid executionID, int itemNumber,
            DataTable fieldTable, out bool success, out string errorMessage)
        {
            List<DataRow> fieldRows = new List<DataRow>();
            List<string> errorMessages = new List<string>();
            string errorDelimiter = ". ";

            success = true;
            errorMessage = string.Empty;

            FieldType fieldType = null;

            // Contains all required fields?
            var missingFields = requiredFieldTypeNames.Except(fields.Select(f => f.Key));

            if (missingFields.Any() && isInsert) // Only check for required fields on insert.
            {
                success = false;
                bool isSinglar = (missingFields.Count() == 1);
                errorMessages.Add($"{string.Join(",", missingFields)} {(isSinglar ? "is a" : "are")} required field{(isSinglar ? "" : "s")}");
            }

            foreach (var k in fields)
            {
                string fieldName = k.Key.Trim();
                string fieldValue = (k.Value + "").Trim();
                int? fieldTypeId = null;
                string decimalFormatString = $"0.{string.Join("", Enumerable.Repeat("#", 18))}";


                // Validation of field and value;
                fieldType = fieldTypes.SingleOrDefault(f => f.Name == fieldName);
                if (fieldType == null)
                {
                    if (ot == "FusionAttributeType" && (fieldName == "FusionID" || fieldName == "Name" || fieldName == "SourceID"))
                    {
                        success = true;
                    }
                    else if (ot == "ReferenceItemType" && fieldName == "Code")
                    {
                        success = true;
                    }
                    else if (ot == "RuleType" && (fieldName == "Threshold" || fieldName == "Status" || fieldName == "Dimension"))
                    {
                        success = true;
                    }
                    else
                    {
                        success = false;
                        errorMessages.Add($"{fieldName} is not a valid field");
                    }
                }
                else
                {
                    fieldTypeId = fieldType.ID;

                    if (fieldType.IsRequired)
                    {
                        if (string.IsNullOrEmpty(fieldValue))
                        {
                            success = false;
                            errorMessages.Add($"{fieldName} is a required field");
                        }
                    }

                    if (!string.IsNullOrEmpty(fieldValue))
                    {
                        switch (fieldType.Type)
                        {
                            case "Boolean":
                                if ((fieldValue.ToLower() != "true" && fieldValue.ToLower() != "false") && !string.IsNullOrEmpty(fieldValue))
                                {
                                    success = false;
                                    errorMessages.Add($"{fieldName} is a boolean field and may only be 'false' or 'true'");
                                }
                                break;
                            case "Date":
                                DateTime dTest;
                                if (!DateTime.TryParse(fieldValue, out dTest) && !string.IsNullOrEmpty(fieldValue))
                                {
                                    success = false;
                                    errorMessages.Add($"{fieldName} must be a valid date");
                                }
                                break;
                            case "DateTime":
                                DateTime dtTest;
                                if (!DateTime.TryParse(fieldValue, out dtTest) && !string.IsNullOrEmpty(fieldValue))
                                {
                                    success = false;
                                    errorMessages.Add($"{fieldName} must be a valid datetime value");
                                }
                                break;
                            case "Decimal":
                                decimal decTest;
                                if (!decimal.TryParse(fieldValue, out decTest) && !string.IsNullOrEmpty(fieldValue))
                                {
                                    success = false;
                                    errorMessages.Add($"{fieldName} must be a valid decimal");
                                }
                                break;
                            case "Link":
                                if (fieldValue.Count(c => c == '|') != 1 && !string.IsNullOrEmpty(fieldValue))
                                {
                                    success = false;
                                    errorMessages.Add($"{fieldName} must be a valid link, using the format name|url");
                                }
                                break;
                            case "Lookup":
                                break;
                            case "Number":
                                int intTest;
                                if (!int.TryParse(fieldValue, out intTest) && !string.IsNullOrEmpty(fieldValue))
                                {
                                    success = false;
                                    errorMessages.Add($"{fieldName} must be a valid whole number");
                                }
                                break;
                            case "Percentage":
                                decimal pctTest;
                                if (!decimal.TryParse(fieldValue, out pctTest) && !string.IsNullOrEmpty(fieldValue))
                                {
                                    success = false;
                                    errorMessages.Add($"{fieldName} must be a valid percentage");
                                }
                                break;
                            default: // Html, Text
                                if (!string.IsNullOrEmpty(fieldType.Pattern) && !string.IsNullOrEmpty(fieldValue))
                                {
                                    if (!System.Text.RegularExpressions.Regex.IsMatch(fieldValue, fieldType.Pattern))
                                    {
                                        success = false;
                                        errorMessages.Add($"{fieldName} must match regular expression pattern defined for this field");
                                    }
                                }
                                break;
                        }

                        if (fieldType.Length.HasValue)
                        {
                            if (fieldValue.Length < fieldType.Length.Value)
                            {
                                success = false;
                                errorMessages.Add($"{fieldName} must have an exact length of {fieldType.Length.Value}");
                            }
                        }
                        if (fieldType.MinimumLength.HasValue)
                        {
                            if (fieldType.Type == "Decimal" || fieldType.Type == "Number")
                            {
                                if (decimal.TryParse(fieldValue, out var fieldDecimalValue) && fieldDecimalValue < fieldType.MinimumLength.Value)
                                {
                                    success = false;
                                    errorMessages.Add($"{fieldName} must have a minimum value of {fieldType.MinimumLength.Value.ToString(decimalFormatString)}");
                                }
                            }
                            else
                            {
                                if (fieldValue.Length < fieldType.MinimumLength.Value)
                                {
                                    success = false;
                                    errorMessages.Add($"{fieldName} must have a minimum length of {fieldType.MinimumLength.Value.ToString(decimalFormatString)}");
                                }
                            }

                        }
                        if (fieldType.MaximumLength.HasValue)
                        {
                            if (fieldType.Type == "Decimal" || fieldType.Type == "Number")
                            {
                                if (decimal.TryParse(fieldValue, out var fieldDecimalValue) && fieldDecimalValue > fieldType.MaximumLength.Value)
                                {
                                    success = false;
                                    errorMessages.Add($"{fieldName} must have a maximum value of {fieldType.MaximumLength.Value.ToString(decimalFormatString)}");
                                }
                            }
                            else
                            {
                                if (fieldValue.Length > fieldType.MaximumLength.Value)
                                {
                                    success = false;
                                    errorMessages.Add($"{fieldName} may only have a maximum length of {fieldType.MaximumLength.Value.ToString(decimalFormatString)}");
                                }
                            }

                        }
                    }
                }

                if (errorMessages.Any())
                {
                    errorMessage = string.Join(errorDelimiter, errorMessages);
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

        private void ValidateAssetAndParent(Guid executionID, int assetTypeID, int timeout = 3600)
        {
            Connection.Execute(@"
    update  T
    set     T.AssetID = S.ID,
            T.Object = S.Object,
            T.ObjectID = S.ObjectID
    from    api.ExecutionAsset T
            inner join Asset S on T.ExecutionID = @executionID and S.AssetTypeID = @assetTypeID and S.Uid = T.Uid and T.Uid is not null;

    update  T
    set     T.ParentAssetID = S.ID,
            T.ParentObject = S.Object,
            T.ParentObjectID = S.ObjectID
    from    api.ExecutionAsset T
            inner join Asset S on T.ExecutionID = @executionID and S.Uid = T.ParentUid and T.ParentUid is not null
            inner join AssetType ST on ST.ID = S.AssetTypeID and ST.Object = T.ParentObjectType and ST.ObjectID = T.ParentObjectTypeID;",
            new { executionID, assetTypeID }, commandTimeout: timeout);
        }

        #endregion

        #endregion

        public async Task<List<IntersectTypeApiViewModel>> GetRelationshipTypes(IEnumerable<KeyValuePair<string, string>> queryParams, string whereClause = "")
        {
            var dbArgs = new DynamicParameters();

            if (queryParams != null)
            {
                if (queryParams.ToList().Any(q => q.Key.ToLower() == "predicateuid"))
                {
                    Guid predicateUid;
                    var predicateUidString = queryParams.ToList().FirstOrDefault(q => q.Key.ToLower() == "predicateuid").Value;
                    if (Guid.TryParse(predicateUidString, out predicateUid))
                    {
                        dbArgs.Add("@predicateUid", predicateUid);
                        whereClause += (string.IsNullOrEmpty(whereClause) ? " where" : " and") + $" P.[UID] = @predicateUid";
                    }
                }
                if (queryParams.ToList().Any(q => q.Key.ToLower() == "assettypeuid"))
                {
                    Guid assetTypeUid;
                    var assetTypeUidString = queryParams.ToList().FirstOrDefault(q => q.Key.ToLower() == "assettypeuid").Value;
                    if (Guid.TryParse(assetTypeUidString, out assetTypeUid))
                    {
                        dbArgs.Add("@assettypeuid", assetTypeUid);
                        whereClause += (string.IsNullOrEmpty(whereClause) ? " where" : " and") + $" (S.Uid = @assettypeuid OR O.Uid = @assettypeuid)";
                    }
                }
                if (queryParams.ToList().Any(q => q.Key.ToLower() == "state"))
                {
                    State state;
                    var stateString = queryParams.ToList().FirstOrDefault(q => q.Key.ToLower() == "state").Value;
                    if (Enum.TryParse(stateString, out state))
                    {
                        dbArgs.Add("@state", state);
                        whereClause += (string.IsNullOrEmpty(whereClause) ? " where" : " and") + $" I.State = @state";
                    }
                }
            }

            if (!Community.IsFusionEnabled())
            {
                List<SystemObjects> filteredObjects = new List<SystemObjects>()
                {
                    SystemObjects.FusionAttributeType,
                    SystemObjects.FusionQueryAttributeType,
                    SystemObjects.FusionType
                };

                whereClause += (string.IsNullOrEmpty(whereClause) ? " where" : " and")
                    + $" I.Object not in ({string.Join(",", filteredObjects.Select(x => "'" + x + "'"))})";

                whereClause += $" AND I.Subject not in ({string.Join(",", filteredObjects.Select(x => "'" + x + "'"))})";
            }

            var sql = $@"
select	I.Id,
        I.Uid,
		I.State as State,
        coalesce(I.IsSystem, 0) as IsSystem,
		P.UID as 'Predicate.Uid',
		coalesce(P.[Type],0) as 'Predicate.Type',
		coalesce(P.Name,'') as 'Predicate.Name',
		coalesce(P.Inverse,'') as 'Predicate.Inverse',
		coalesce(SI.Uid, S.Uid) as 'Subject.Uid',
		case 
			when I.Subject = 'IntersectType' then SI.SubjectName + ' ' + SI.PredicateName + ' ' + SI.ObjectName + ' relationship'
			else coalesce(SFT.Name + ' / ','') + coalesce(SP.[Path], S.Name)
		end as 'Subject.Name',
		coalesce(S.Class, 0) as 'Subject.Class',
		I.SubjectCardinality as 'Subject.Cardinality',
		O.Uid as 'Object.Uid',
		coalesce(OFT.Name + ' / ','') + coalesce(OP.[Path], O.Name)  as 'Object.Name',
		coalesce(O.Class, 0) as 'Object.Class',
		I.ObjectCardinality as 'Object.Cardinality'
from	IntersectType I
		left join [Predicate] P on P.ID = I.PredicateID

		left join AssetType S on (S.uid = I.SubjectUid OR (S.Object = I.Subject and S.ObjectID = I.SubjectID))
        left join FusionAttributeType SFAT on I.Subject = 'FusionAttributeType' and SFAT.ID = I.SubjectID 
        left join FusionType SFT on SFT.ID = SFAT.FusionTypeID 
        outer apply dbo.GetAssetTypeTextPathById(S.ID, '/') SP

		left join IntersectTypeDetail SI on I.Subject = 'IntersectType' and SI.ID = I.SubjectID
		left join AssetType O on (O.uid = I.ObjectUid OR (O.Object = I.Object and O.ObjectID = I.ObjectID))
        left join FusionAttributeType OFAT on I.Object = 'FusionAttributeType' and OFAT.ID = I.ObjectID 
        left join FusionType OFT on OFT.ID = OFAT.FusionTypeID 
        outer apply dbo.GetAssetTypeTextPathById(O.ID, '/') OP
{whereClause} for json path";

            var models = await GetDatabaseJsonAsObjectAsync<List<IntersectTypeApiViewModel>>(sql, dbArgs);

            return models;
        }

        public List<DatabaseBulkAssetResult> RemoveAssets(ApiExecution execution, AssetType at, AssetDeletes import, int timeout = 3600, bool sendWorkflowEvents = true)
        {
            var results = new List<DatabaseBulkAssetResult>();
            var dt = DateTime.UtcNow;
            bool generalChecksCompleted = false;
            CurrentExecutionLocationModel currentLocation = null;

            var executionItemDupes = import.Where(i => i.ExecutionItemUid.HasValue).GroupBy(i => i.ExecutionItemUid).Where(i => i.Count() > 1).Select(i => new { ExecutionItemUid = i.Key, Count = i.Count() }).ToList();
            if (executionItemDupes.Any())
            {
                execution.ErrorMessage = $"Duplicate execution item identifiers: {string.Join(", ", executionItemDupes.Select(i => i.ExecutionItemUid.ToString()))}. Identifiers must be unique within a batch.";
                results.AddRange(import.Select(i => new DatabaseBulkAssetResult { ExecutionItemUid = i.ExecutionItemUid, uid = i.Uid, Message = execution.ErrorMessage, Success = false }));
            }
            else
            {
                var uidDupes = import.GroupBy(i => i.Uid).Where(i => i.Count() > 1).Select(i => new { Uid = i.Key, Count = i.Count() }).ToList();
                if (uidDupes.Any())
                {
                    execution.ErrorMessage = $"Duplicate Asset Uids: {string.Join(", ", uidDupes.Select(i => i.Uid.ToString()))}. Identifiers must be unique within a batch.";
                    results.AddRange(import.Select(i => new DatabaseBulkAssetResult { ExecutionItemUid = i.ExecutionItemUid, uid = i.Uid, Message = execution.ErrorMessage, Success = false }));
                }
                else
                {
                    try
                    {
                        currentLocation = GetCurrentExecutionLocation(execution.ExecutionID, "api.ExecutionDeletedAsset");

                        if (currentLocation.HighestItemNumberProcessed > 0)
                        {
                            results.AddRange(
                                Query<DatabaseBulkAssetResult>(
                                    $"select * from api.ExecutionDeletedAsset where ExecutionID = @ExecutionID and ItemNumber <= {currentLocation.HighestItemNumberProcessed}",
                                    new { execution.ExecutionID }
                                )
                            );
                        }

                        #region Build data tables.

                        var table = new DataTable();
                        table.Columns.Add("ExecutionID", typeof(Guid));
                        table.Columns.Add("ItemNumber", typeof(int));
                        table.Columns.Add("ExecutionItemUid", typeof(Guid));
                        table.Columns.Add("Uid", typeof(Guid));
                        table.Columns.Add("AssetID", typeof(long));
                        table.Columns.Add("Message", typeof(string));
                        table.Columns.Add("Success", typeof(bool));
                        table.Columns.Add("Cascade", typeof(bool));

                        #endregion

                        #region Generate data sets

                        for (int i = 1; i <= import.Count; i++)
                        {
                            if (i > currentLocation.HighestItemNumber)
                            {
                                var model = import[i - 1];

                                var row = table.NewRow();

                                row["ExecutionID"] = execution.ExecutionID;
                                row["ItemNumber"] = i;
                                if (model.ExecutionItemUid.HasValue) row["ExecutionItemUid"] = model.ExecutionItemUid.Value;
                                row["Uid"] = model.Uid;
                                row["Cascade"] = model.Cascade ?? false;

                                table.Rows.Add(row);
                            }
                        }

                        #endregion

                        if (Database.Connection.State != ConnectionState.Open)
                            Connection.OpenWithRetry(RetryPolicy.DefaultProgressive);

                        #region Bulk Copy

                        SqlBulkCopy bulkCopy = new SqlBulkCopy(Connection);

                        bulkCopy.BatchSize = table.Rows.Count;
                        bulkCopy.DestinationTableName = "api.ExecutionDeletedAsset";
                        bulkCopy.BulkCopyTimeout = timeout;

                        bulkCopy.ColumnMappings.Add("ExecutionID", "ExecutionID");
                        bulkCopy.ColumnMappings.Add("ItemNumber", "ItemNumber");
                        bulkCopy.ColumnMappings.Add("ExecutionItemUid", "ExecutionItemUid");
                        bulkCopy.ColumnMappings.Add("Uid", "Uid");
                        bulkCopy.ColumnMappings.Add("Cascade", "Cascade");

                        bulkCopy.WriteToServer(table);

                        bulkCopy = null;

                        #endregion

                        #region Resolve assets based on UIDs

                        Connection.Execute(@"
    update	T
    set		T.Object = S.Object, 
            T.ObjectID = S.ObjectID, 
            T.AssetID = S.ID
    from	api.ExecutionDeletedAsset T
		    inner join Asset S on S.Uid = T.Uid and T.ExecutionID = @ExecutionID
		    inner join AssetType ST on ST.Uid = @uid and ST.ID = S.AssetTypeID;",
                    new { execution.ExecutionID, at.uid }, commandTimeout: timeout);

                        #endregion

                        #region Log lookup errors

                        Connection.Execute($@"
    update	api.ExecutionDeletedAsset
    set		Success = 0,
		    [Message] = coalesce([Message] + '; ', '') + 'You must provide a valid Uid for this asset when you are attempting to delete it'
    where	ExecutionID = @ExecutionID and ([Uid] is null or [Uid] = CAST(CAST(0 AS BINARY) AS UNIQUEIDENTIFIER)); 

    update	api.ExecutionDeletedAsset
    set		Success = 0,
		    [Message] = coalesce([Message] + '; ', '') + 'Not found based on Uid provided'
    where	ExecutionID = @ExecutionID and AssetID is null;",
                        new { execution.ExecutionID }, commandTimeout: timeout);

                        #endregion

                        // Validate permissions
                        LogAssetPermissionErrors(execution.ExecutionID, at, Permission.DeleteAsset, "ExecutionDeletedAsset");

                        generalChecksCompleted = true;
                    }
                    catch (Exception generalEx)
                    {
                        generalChecksCompleted = false;
                        var msg = generalEx.GetFullExceptionData(false);
                        execution.ErrorMessage = msg;
                        execution.Processed = 0;
                        execution.Error = import.Count();

                        results = new List<DatabaseBulkAssetResult>();
                        results.AddRange(import.Select(i => new DatabaseBulkAssetResult { ExecutionItemUid = i.ExecutionItemUid, Message = msg, Success = false }));
                    }

                    if (generalChecksCompleted)
                    {
                        var predicateType = DeterminePredicateType(at.Object);
                        int loopSize = 250;
                        int numberOfLoops = (int)Math.Ceiling((decimal)(execution.Total - currentLocation.HighestItemNumberProcessed) / loopSize);
                        int beginItemNumber = currentLocation.HighestItemNumberProcessed + 1;
                        int endItemNumber = currentLocation.HighestItemNumberProcessed + loopSize;

                        for (int currentLoop = 1; currentLoop <= numberOfLoops; currentLoop++)
                        {
                            bool runCompleted = false;
                            int retryCount = 0;

                            while (!runCompleted && retryCount <= API_V2_RETRY_LIMIT)
                            {
                                var querySuffix = $"S.Success is null and S.ExecutionID = @ExecutionID and S.ItemNumber between @beginItemNumber and @endItemNumber";
                                using (var trans = Connection.BeginTransaction())
                                {
                                    try
                                    {
                                        if (at.Object == "FusionType")
                                        {
                                            var fusion = import.First();
                                            var data = Connection.Query<dynamic>($@"
                                                    create table #forDelete (ID int, Type varchar(50))
                                                    declare @result table (Status bit, Message varchar(255))
                                                                                                        
                                                    declare @fusionId int = (select ObjectID from api.ExecutionDeletedAsset
                                                    	where ExecutionID = @ExecutionID AND Object = 'Fusion')
                                                    
                                                    insert into #forDelete values(@fusionId, 'Fusion')
                                                    
                                                    insert into #forDelete select ID,'Asset' as Type from Asset where Object = 'Fusion' and ObjectID = @fusionId
                                                    
                                                    
                                                    declare @fusionTypeId int = (select FusionTypeID from fusion where ID = @fusionId)
                                                    
                                                    insert into #forDelete
                                                    select ID as ID,'FusionAttribute' as Type from FusionAttribute where FusionID = @fusionId
                                                    
                                                    insert into #forDelete
                                                    	select ID, 'Intersect' as Type
                                                    	from [Intersect] where [Object] = 'FusionAttribute' and [ObjectID] in (select id from #forDelete where Type = 'FusionAttribute')
                                                    
                                                    insert into #forDelete
                                                    	select ID, 'Intersect' as Type
                                                        from [Intersect] where [Subject] = 'FusionAttribute' and [SubjectID] in (select id from #forDelete where Type = 'FusionAttribute')
                                                    
                                                    insert into #forDelete
                                                    	select ID, 'Field' as Type
                                                        from Field where ObjectType = 'FusionAttribute' and ObjectID in (select id from #forDelete where Type = 'FusionAttribute')
                                                    
                                                    declare @itemCount int = (select count(*) from #forDelete)
                                                    
                                                    declare @goodForDeletion bit = 0
                                                    if @itemCount > 2 and @isCascade = 0
                                                    	insert into @result values(0,'Fusion configuration cannot be deleted. Use Cascade=`true` to delete Fusion configuration and its children!')
                                                    else if (select count(*) from #forDelete where Type = 'Asset') != 1
                                                    	insert into @result values(0,'Asset not found!')
                                                    else if (select count(*) from #forDelete where Type = 'Fusion') != 1
                                                    	insert into @result values(0,'Asset is not found!')
                                                    else 
                                                    	set @goodForDeletion = 1
                                                    
                                                    if @goodForDeletion = 1
                                                    begin	
                                                    	delete F
                                                    		from Field F
                                                    		inner join #forDelete FD on FD.Type = 'Field' and F.ID = FD.ID
                                                    	
                                                    	delete I
                                                    		from [Intersect] I
                                                    		inner join #forDelete FD on FD.Type = 'Intersect' and I.ID = FD.ID
                                                    	
                                                    	delete FA 
                                                    		from [FusionAttribute] FA
                                                    		inner join #forDelete FD on FD.Type = 'FusionAttribute' and FA.ID = FD.ID
                                                    	
                                                    	delete A 
                                                    		from Asset A
                                                    		inner join #forDelete FD on FD.Type = 'Asset' and A.ID = FD.ID
                                                    	
                                                    	delete F 
                                                    		from Fusion F
                                                    		inner join #forDelete FD on FD.Type = 'Fusion' and F.ID = FD.ID
                                                    
                                                    	insert into @result values(1,'Success')
                                                    end
                                                    select * from @result",
                                                    new { execution.ExecutionID, isCascade = fusion.Cascade.HasValue ? fusion.Cascade.Value : false, beginItemNumber, endItemNumber }, transaction: trans, commandTimeout: timeout).FirstOrDefault();

                                            bool IsDeleted = false;
                                            if (bool.TryParse(data.Status.ToString(), out IsDeleted))
                                            {
                                                if (!IsDeleted)
                                                {
                                                    Connection.Execute(
                                                        $"update S set S.Success = 0, S.Message = '{data.Message.ToString()}' from api.ExecutionDeletedAsset S where	{querySuffix} and S.AssetID is not null;",
                                                        new { execution.ExecutionID, beginItemNumber, endItemNumber }, transaction: trans, commandTimeout: timeout);
                                                    runCompleted = true;
                                                    trans.Commit();
                                                    continue;
                                                }
                                                else
                                                {
                                                    Connection.Execute(
                                                        $"update S set S.Success = 1 from api.ExecutionDeletedAsset S where	{querySuffix} and S.AssetID is not null;",
                                                        new { execution.ExecutionID, beginItemNumber, endItemNumber }, transaction: trans, commandTimeout: timeout);
                                                    runCompleted = true;
                                                    trans.Commit();
                                                    continue;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            #region Cascade Behaviour

                                            // Parent/Child Relationships
                                            if (predicateType.HasValue)
                                            {
                                                Connection.Execute($@" 
            if OBJECT_ID('tempdb..#ExecutionDeletedAsset') IS NOT NULL
                truncate TABLE #ExecutionDeletedAsset
            else
                create table #ExecutionDeletedAsset (
                    ExecutionID	uniqueidentifier,
                    [Root] uniqueidentifier,
                    ItemNumber	int,
                    Uid	uniqueidentifier,
                    AssetID	bigint,
                    FromHierarchy	bit
                );

            with h as (
	            select	D.ExecutionID,
			            D.ItemNumber,
			            D.AssetID,
			            D.[Uid],
			            A.Object,
			            A.ObjectID, 
			            D.IntersectID,
                        0 as [Level],
                        D.Uid as Root
	            from	api.ExecutionDeletedAsset D
			            inner join Asset A on D.ExecutionID = @ExecutionID and A.ID = D.AssetID
	            where	D.AssetID is not null
                        and D.ItemNumber between @beginItemNumber and @endItemNumber
	            union all
	            select	P.ExecutionID,
			            P.ItemNumber,
			            C.ID as AssetID,
			            C.[Uid],
			            C.Object,
			            C.ObjectID, 
			            I.IntersectID,
                        P.[Level] + 1 as [Level],
                        P.[Root] as Root
	            from	PredicateIntersect I 
			            inner join h as P on P.ExecutionID = @ExecutionID and I.PredicateType = @predicateTypeValue and P.Object = I.Subject and P.ObjectID = I.SubjectID
			            inner join Asset C on C.Object = I.Object and C.ObjectID = I.ObjectID
                where   P.ItemNumber between @beginItemNumber and @endItemNumber and P.[Level] <= 15
            )

            insert into #ExecutionDeletedAsset ([ExecutionID],[ItemNumber],[Uid],[AssetID],[FromHierarchy],[Root])
                select   
                        ExecutionID, 
                        ItemNumber, 
                        [Uid], 
                        AssetID, 
                        1 as Hiearchy,
                        h.[Root]
                from    h 
                where   IntersectID is not null 
                        and [Level] > 0 
                        and Uid not in (select Uid from api.ExecutionDeletedAsset where ExecutionID = h.ExecutionID and ItemNumber = h.ItemNumber )
			            and  ExecutionID = @ExecutionID;
            
			update  S 
            set     S.Success = 0 ,
			        [Message] ='You have not enabled Cascade, yet there are child relationships for this asset.'
			from    api.ExecutionDeletedAsset S 
			        inner join  (
                                select      [Root] as UID,
                                            ExecutionID,
                                            ItemNumber  
                                from        #ExecutionDeletedAsset
			                    group by    [Root], ExecutionID, ItemNumber 
                                            having (count (*) > 0)
                                ) E on S.Uid= E.UID and s.ItemNumber=E.ItemNumber and s.ExecutionID = e.ExecutionID
			where	{querySuffix}  and AssetId is not null
			        and S.[Cascade] = 0", new { execution.ExecutionID, predicateTypeValue = predicateType.HasValue ? (int)predicateType : -1, beginItemNumber, endItemNumber }, transaction: trans, commandTimeout: timeout);
                                            }

                                            // Workflows
                                            Connection.Execute($@" 
            if OBJECT_ID('tempdb..#ExecutionDeletedAsset') IS NOT NULL
                truncate TABLE #ExecutionDeletedAsset
            else
                create table #ExecutionDeletedAsset (
                    ExecutionID	uniqueidentifier,
                    [Root] uniqueidentifier,
                    ItemNumber	int,
                    Uid	uniqueidentifier,
                    AssetID	bigint,
                    FromHierarchy	bit
                );

            insert into #ExecutionDeletedAsset ([ExecutionID],[ItemNumber],[Root])
                select distinct 
                        ExecutionID, 
                        ItemNumber, 
                        S.[Uid]
	            from	workflow.[Type] wt
			            inner join workflow.EventRegistration we on we.typeid = wt.id and we.changetype <> 3
			            inner join workflow.[Version] wv on wt.id = wv.typeId
			            inner join workflow.Item wi on 	wv.id = wi.VersionID
			            inner join api.ExecutionDeletedAsset S on S.Object = wi.Object and S.ObjectID = wi.ObjectID 
                where   {querySuffix} ;

            insert into #ExecutionDeletedAsset ([ExecutionID],[ItemNumber],[Root])
                select distinct 
                        ExecutionID, 
                        ItemNumber, 
                        S.[Uid]
	            from	workflow.Item wi
			            inner join Issue i on wi.object = 'Issue' and i.id = wi.objectid
			            inner join api.ExecutionDeletedAsset S on S.Object = i.Object and S.ObjectID = i.ObjectID 
                where   {querySuffix} ;
            
			update  S 
            set     S.Success = 0 ,
			        [Message] ='You have not enabled Cascade, yet there are workflows for this asset.'
			from    api.ExecutionDeletedAsset S 
			        inner join  (
                                select      [Root] as UID,
                                            ExecutionID,
                                            ItemNumber  
                                from        #ExecutionDeletedAsset
			                    group by    [Root], ExecutionID, ItemNumber 
                                            having (count (*) > 0)
                                ) E on S.Uid= E.UID and s.ItemNumber=E.ItemNumber and s.ExecutionID = e.ExecutionID
			where	{querySuffix}  and AssetId is not null
			        and S.[Cascade] = 0", new { execution.ExecutionID, beginItemNumber, endItemNumber }, transaction: trans, commandTimeout: timeout);

                                            // Rule Implementations
                                            if (at.Object == "RuleType")
                                            {
                                                Connection.Execute($@" 
            if OBJECT_ID('tempdb..#ExecutionDeletedAsset') IS NOT NULL
                truncate TABLE #ExecutionDeletedAsset
            else
                create table #ExecutionDeletedAsset (
                    ExecutionID	uniqueidentifier,
                    [Root] uniqueidentifier,
                    ItemNumber	int,
                    Uid	uniqueidentifier,
                    AssetID	bigint,
                    FromHierarchy	bit
                );

            insert into #ExecutionDeletedAsset ([ExecutionID],[ItemNumber],[Root])
                select distinct 
                        S.ExecutionID, 
                        S.ItemNumber, 
                        S.[Uid]
	            from	RuleImplementation T
			            inner join api.ExecutionDeletedAsset S on S.Object = 'Rule' and S.ObjectID = T.RuleID 
                where   {querySuffix} ;
            
			update  S 
            set     S.Success = 0 ,
			        [Message] ='You have not enabled Cascade, yet there are implementations for this rule asset.'
			from    api.ExecutionDeletedAsset S 
			        inner join  (
                                select      [Root] as UID,
                                            ExecutionID,
                                            ItemNumber  
                                from        #ExecutionDeletedAsset
			                    group by    [Root], ExecutionID, ItemNumber 
                                            having (count (*) > 0)
                                ) E on S.Uid= E.UID and s.ItemNumber=E.ItemNumber and s.ExecutionID = e.ExecutionID
			where	{querySuffix}  and AssetId is not null
			        and S.[Cascade] = 0", new { execution.ExecutionID, beginItemNumber, endItemNumber }, transaction: trans, commandTimeout: timeout);

                                            }

                                            #endregion

                                            // Get the hierarchy items we also need to remove
                                            if (predicateType.HasValue)
                                            {
                                                Connection.Execute($@"
    with h as (
	    select	S.ExecutionID,
			    S.ItemNumber,
			    S.AssetID,
			    S.[Uid],
			    A.Object,
			    A.ObjectID, 
			    S.IntersectID,
                0 as [Level]
	    from	api.ExecutionDeletedAsset S
			    inner join Asset A on  A.ID = S.AssetID
	    where	S.AssetID is not null
                and {querySuffix}
	    union all
	    select	P.ExecutionID,
			    P.ItemNumber,
			    C.ID as AssetID,
			    C.[Uid],
			    C.Object,
			    C.ObjectID, 
			    I.IntersectID,
                P.[Level] + 1 as [Level]
	    from	PredicateIntersect I 
			    inner join h as P on P.ExecutionID = @ExecutionID and I.PredicateType = {(int)predicateType} and P.Object = I.Subject and P.ObjectID = I.SubjectID
			    inner join Asset C on C.Object = I.Object and C.ObjectID = I.ObjectID
        where   P.ItemNumber between @beginItemNumber and @endItemNumber and P.[Level] <= 15
    )
    insert into api.ExecutionDeletedAsset ([ExecutionID],[ItemNumber],[Uid],[AssetID],[IntersectID],[FromHierarchy])
        select  distinct 
                ExecutionID, 
                ItemNumber, 
                [Uid], 
                AssetID, 
                IntersectID, 
                1 
        from    h 
        where   IntersectID is not null 
                and [Level] > 0 
                and Uid not in (select Uid from api.ExecutionDeletedAsset where ExecutionID = @ExecutionID)",
                                                new { execution.ExecutionID, beginItemNumber, endItemNumber }, transaction: trans, commandTimeout: timeout);
                                            }

                                            #region Delete workflow items

                                            Connection.Execute($@"
    create table #w (ItemID int);

    insert into #w
	    select	distinct 
			    wi.ID 
	    from	workflow.[Type] wt
			    inner join workflow.EventRegistration we on we.typeid = wt.id and we.changetype <> 3
			    inner join workflow.[Version] wv on wt.id = wv.typeId
			    inner join workflow.Item wi on 	wv.id = wi.VersionID
			    inner join api.ExecutionDeletedAsset S on S.Object = wi.Object and S.ObjectID = wi.ObjectID and {querySuffix};

    insert into #w
	    select	wi.id 
	    from	workflow.Item wi
			    inner join Issue i on wi.object = 'Issue' and i.id = wi.objectid
			    inner join api.ExecutionDeletedAsset S on S.Object = i.Object and S.ObjectID = i.ObjectID and {querySuffix};

    delete	T
    from	[workflow].[ItemAssignment] T
		    inner join #w S on S.ItemID = T.ItemID;

    delete  T
    from	[workflow].[ItemStepTransition] T
		    inner join workflow.itemstep wis on (wis.ID = T.ToItemStepID or wis.ID = T.FromItemStepID)
		    inner join #w S on S.ItemID = wis.ItemID;

    delete  workflow.itemstep 
    where	ItemID in (Select ItemID from #w);
 
    delete  [workflow].[Item] 
    where	ID in (Select ItemID from #w);",
                                            new { execution.ExecutionID, beginItemNumber, endItemNumber }, transaction: trans, commandTimeout: timeout);

                                            #endregion

                                            #region De-index queue / Audit

                                            Connection.Execute($@"
    INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID],[AssetID])
	    select	distinct 
                'ObjectIndex', 'D',	S.Object, S.ObjectID, S.AssetID 
        from    api.ExecutionDeletedAsset S
        where   {querySuffix} and S.Object is not null and S.ObjectID is not null;

    insert into reporting.Global_Audit (Object, ObjectID, ObjectName, ResourceID, Date, Action, ActionObject, ActionObjectID, ActionObjectTypeName, ActionObjectName, ActionDescription)
	    select	distinct
			    O.Object, 
			    O.ObjectID,
			    SUBSTRING(O.DisplayValue,1,250), 
			    @r, 
			    @dt, 
			    'Deleted', 
			    O.Object, 
			    O.ObjectID, 
			    O.TypeName, 
			    SUBSTRING(O.DisplayValue,1,250), 
			    'This asset has been removed.' 
	    from	AssetDetail O
			    inner join api.ExecutionDeletedAsset S on S.AssetID = O.ID and {querySuffix} and S.Object is not null and S.ObjectID is not null;",
                                            new { execution.ExecutionID, r = CurrentResourceID, dt, beginItemNumber, endItemNumber }, transaction: trans, commandTimeout: timeout);

                                            #endregion

                                            #region Cross-references

                                            Connection.Execute($@"
    delete	T
    from	AssetCrossReference T
		    inner join api.ExecutionDeletedAsset S on S.[Uid] = T.[Uid] and {querySuffix};",
                                            new { execution.ExecutionID, beginItemNumber, endItemNumber }, transaction: trans, commandTimeout: timeout);

                                            #endregion

                                            #region Asset table

                                            Connection.Execute(
                                                $"delete Asset where Uid in (select S.Uid from api.ExecutionDeletedAsset S where {querySuffix})",
                                                new { execution.ExecutionID, beginItemNumber, endItemNumber }, transaction: trans, commandTimeout: timeout);

                                            #endregion

                                            #region Legacy table

                                            var legacyTable = "";
                                            switch (at.Object)
                                            {
                                                case "FusionAttributeType":
                                                    legacyTable = "FusionAttribute";
                                                    break;
                                                case "ReferenceItemType":
                                                    legacyTable = "ReferenceItem";
                                                    break;
                                                case "RuleType":
                                                    legacyTable = "[Rule]";
                                                    break;
                                            }

                                            if (!string.IsNullOrEmpty(legacyTable))
                                            {
                                                if (legacyTable == "[Rule]") //You need to also remove rule implementations, results, and other legacy dependent tables.
                                                {
                                                    Connection.Execute($@"
delete T from RuleResultFusionAttribute T inner join RuleResult R on R.ID = T.RuleResultID inner join RuleImplementation S on S.ID = R.RuleImplementationID and S.RuleID in (select S.ObjectID from api.ExecutionDeletedAsset S where {querySuffix}); 
delete T from RuleResultQualifier T inner join RuleResult R on R.ID = T.RuleResultID inner join RuleImplementation S on S.ID = R.RuleImplementationID and S.RuleID in (select S.ObjectID from api.ExecutionDeletedAsset S where {querySuffix});
delete T from RuleResult T inner join RuleImplementation S on S.ID = T.RuleImplementationID and S.RuleID in (select S.ObjectID from api.ExecutionDeletedAsset S where {querySuffix});
delete T from RuleResultQualifierType T inner join RuleImplementation S on S.ID = T.RuleImplementationID and S.RuleID in (select S.ObjectID from api.ExecutionDeletedAsset S where {querySuffix});
delete RuleImplementation where RuleID in (select S.ObjectID from api.ExecutionDeletedAsset S where {querySuffix});",
                                                        new { execution.ExecutionID, beginItemNumber, endItemNumber }, transaction: trans, commandTimeout: timeout
                                                    );
                                                }

                                                Connection.Execute(
                                                    $"delete {legacyTable} where ID in (select S.ObjectID from api.ExecutionDeletedAsset S where {querySuffix})",
                                                    new { execution.ExecutionID, beginItemNumber, endItemNumber }, transaction: trans, commandTimeout: timeout);
                                            }

                                            #endregion

                                            #region Attributes

                                            Connection.Execute($@"
    delete	T
    from	Field T 
		    inner join [Attribute] A on T.ObjectType = 'Attribute' and A.ID = T.ObjectID
		    inner join api.ExecutionDeletedAsset S on S.Object = A.ObjectType and S.ObjectID = A.ObjectID and {querySuffix};

    delete	T
    from	[Attribute] T
		    inner join api.ExecutionDeletedAsset S on S.Object = T.ObjectType and S.ObjectID = T.ObjectID and {querySuffix};",
                                            new { execution.ExecutionID, beginItemNumber, endItemNumber }, transaction: trans, commandTimeout: timeout);

                                            #endregion

                                            #region Delete Intersects

                                            if (predicateType.HasValue)
                                            {
                                                Connection.Execute($@"
    delete	T
    from	[Intersect] T 
		    inner join api.ExecutionDeletedAsset S on S.IntersectID = T.ID and {querySuffix};",
                                                new { execution.ExecutionID, beginItemNumber, endItemNumber }, transaction: trans, commandTimeout: timeout);
                                            }

                                            Connection.Execute($@"
    delete	T
    from	[Intersect] T
            inner join api.ExecutionDeletedAsset S on S.Object = T.Subject and S.ObjectID = T.SubjectID and {querySuffix};

    delete	T
    from	[Intersect] T
            inner join api.ExecutionDeletedAsset S on S.Object = T.Object and S.ObjectID = T.ObjectID and {querySuffix};",
                                            new { execution.ExecutionID, beginItemNumber, endItemNumber }, transaction: trans, commandTimeout: timeout);

                                            #endregion

                                            #region Delete Social tables

                                            Connection.Execute($@"
    delete	T
    from	CommentRelation T
		    inner join api.ExecutionDeletedAsset S on S.Object = T.ObjectType and S.ObjectID = T.ObjectID and {querySuffix};

    delete	T
    from	CommentVote T
		    inner join Comment C on C.ID = T.CommentID
		    inner join api.ExecutionDeletedAsset S on S.Object = C.OwnerObjectType and S.ObjectID = C.OwnerObjectID and {querySuffix};

    delete	T
    from	Comment T
		    inner join api.ExecutionDeletedAsset S on S.Object = T.OwnerObjectType and S.ObjectID = T.OwnerObjectID and {querySuffix};

    delete	T
    from	Favorite T
		    inner join api.ExecutionDeletedAsset S on S.Object = T.Object and S.ObjectID = T.ObjectID and {querySuffix};

    delete	T
    from	Follow T
		    inner join api.ExecutionDeletedAsset S on S.Object = T.ObjectType and S.ObjectID = T.ObjectID and {querySuffix};",
                                            new { execution.ExecutionID, beginItemNumber, endItemNumber }, transaction: trans, commandTimeout: timeout);

                                            #endregion

                                            #region Delete subsidiary tables

                                            Connection.Execute($@"
    delete	T
    from	Field T
		    inner join api.ExecutionDeletedAsset S on S.Object = T.ObjectType and S.ObjectID = T.ObjectID and {querySuffix};

    delete	T
    from	Issue T
		    inner join api.ExecutionDeletedAsset S on S.Object = T.Object and S.ObjectID = T.ObjectID and {querySuffix};

    delete	T
    from	Nym T
		    inner join api.ExecutionDeletedAsset S on S.Object = T.Object and S.ObjectID = T.ObjectID and {querySuffix};",
                                            new { execution.ExecutionID, beginItemNumber, endItemNumber }, transaction: trans, commandTimeout: timeout);

                                            #endregion

                                            #region Delete owner tables

                                            Connection.Execute($@"
    delete	T
    from	ResponsibilityTypeRelationOverrideItem T
		    inner join api.ExecutionDeletedAsset S on S.AssetID = T.AssetID and {querySuffix};

    delete	T
    from	ResponsibilityRuleResultAsset T
		    inner join api.ExecutionDeletedAsset S on S.AssetID = T.AssetID and {querySuffix};",
                                            new { execution.ExecutionID, beginItemNumber, endItemNumber }, transaction: trans, commandTimeout: timeout);

                                            #endregion

                                            // Update success flag
                                            Connection.Execute(
                                                $"update S set S.Success = 1 from api.ExecutionDeletedAsset S where	{querySuffix} and S.AssetID is not null;",
                                                new { execution.ExecutionID, beginItemNumber, endItemNumber }, transaction: trans, commandTimeout: timeout);

                                            trans.Commit();
                                            runCompleted = true;
                                        }


                                    }
                                    catch (Exception ex)
                                    {
                                        trans.Rollback();

                                        retryCount++;

                                        if (retryCount > API_V2_RETRY_LIMIT)
                                        {
                                            LogLoopExecutionError(execution.ExecutionID, beginItemNumber, endItemNumber, "api.ExecutionDeletedAsset", ex.GetFullExceptionData(false), timeout);
                                        }
                                    }
                                }
                            }

                            results.AddRange(
                                Query<DatabaseBulkAssetResult>(
                                    $"select * from api.ExecutionDeletedAsset where ExecutionID = @ExecutionID and ItemNumber between @beginItemNumber and @endItemNumber and FromHierarchy = 0",
                                    new { execution.ExecutionID, beginItemNumber, endItemNumber }
                                )
                            );

                            OnAssetsPartiallyProcessed(new AssetsPartiallyProcessedEventArgs
                            {
                                Results = results
                            });

                            beginItemNumber += loopSize;
                            endItemNumber += loopSize;
                        }

                        Connection.Close();

                        SendAssetGraphEvents(results);

                        if (sendWorkflowEvents)
                        {
                            SendWorkflowEvents(at.Object, at.ObjectID, results, ChangeType.Delete);
                        }
                    }
                }
            }

            return results;
        }

        public List<DatabaseBulkAssetTypeResult> RemoveAssetTypes(ApiExecution execution, AssetTypeDeletes import, int timeout = 7200)
        {
            var results = new List<DatabaseBulkAssetTypeResult>();
            var dt = DateTime.UtcNow;
            bool generalChecksCompleted = false;
            CurrentExecutionLocationModel currentLocation = null;

            var executionItemDupes = import.Where(i => i.ExecutionItemUid.HasValue).GroupBy(i => i.ExecutionItemUid).Where(i => i.Count() > 1).Select(i => new { ExecutionItemUid = i.Key, Count = i.Count() }).ToList();
            if (executionItemDupes.Any())
            {
                execution.ErrorMessage = $"Duplicate execution item identifiers: {string.Join(", ", executionItemDupes.Select(i => i.ExecutionItemUid.ToString()))}. Identifiers must be unique within a batch.";
                results.AddRange(import.Select(i => new DatabaseBulkAssetTypeResult { ExecutionItemUid = i.ExecutionItemUid, uid = i.Uid, Message = execution.ErrorMessage, Success = false }));
            }
            else
            {
                var uidDupes = import.GroupBy(i => i.Uid).Where(i => i.Count() > 1).Select(i => new { Uid = i.Key, Count = i.Count() }).ToList();
                if (uidDupes.Any())
                {
                    execution.ErrorMessage = $"Duplicate Asset Type Uids: {string.Join(", ", uidDupes.Select(i => i.Uid.ToString()))}. Identifiers must be unique within a batch.";
                    results.AddRange(import.Select(i => new DatabaseBulkAssetTypeResult { ExecutionItemUid = i.ExecutionItemUid, uid = i.Uid, Message = execution.ErrorMessage, Success = false }));
                }
                else
                {
                    try
                    {
                        currentLocation = GetCurrentExecutionLocation(execution.ExecutionID, "api.ExecutionDeletedAssetType");

                        if (currentLocation.HighestItemNumberProcessed > 0)
                        {
                            results.AddRange(
                                Query<DatabaseBulkAssetTypeResult>(
                                    $"select * from api.ExecutionDeletedAssetType where ExecutionID = @ExecutionID and ItemNumber <= {currentLocation.HighestItemNumberProcessed}",
                                    new { execution.ExecutionID }
                                )
                            );
                        }

                        #region Build data tables.

                        var table = new DataTable();
                        table.Columns.Add("ExecutionID", typeof(Guid));
                        table.Columns.Add("ItemNumber", typeof(int));
                        table.Columns.Add("ExecutionItemUid", typeof(Guid));
                        table.Columns.Add("Uid", typeof(Guid));
                        table.Columns.Add("Cascade", typeof(bool));
                        table.Columns.Add("AssetTypeID", typeof(int));
                        table.Columns.Add("Message", typeof(string));
                        table.Columns.Add("Success", typeof(bool));

                        #endregion

                        #region Generate data sets

                        for (int i = 1; i <= import.Count; i++)
                        {
                            if (i > currentLocation.HighestItemNumber)
                            {
                                var model = import[i - 1];

                                var row = table.NewRow();

                                row["ExecutionID"] = execution.ExecutionID;
                                row["ItemNumber"] = i;
                                if (model.ExecutionItemUid.HasValue) row["ExecutionItemUid"] = model.ExecutionItemUid.Value;
                                row["Uid"] = model.Uid;
                                row["Cascade"] = model.Cascade;

                                table.Rows.Add(row);
                            }
                        }

                        #endregion

                        if (Database.Connection.State != ConnectionState.Open)
                            Connection.OpenWithRetry(RetryPolicy.DefaultProgressive);

                        #region Bulk Copy

                        SqlBulkCopy bulkCopy = new SqlBulkCopy(Connection);

                        bulkCopy.BatchSize = table.Rows.Count;
                        bulkCopy.DestinationTableName = "api.ExecutionDeletedAssetType";
                        bulkCopy.BulkCopyTimeout = timeout;

                        bulkCopy.ColumnMappings.Add("ExecutionID", "ExecutionID");
                        bulkCopy.ColumnMappings.Add("ItemNumber", "ItemNumber");
                        bulkCopy.ColumnMappings.Add("ExecutionItemUid", "ExecutionItemUid");
                        bulkCopy.ColumnMappings.Add("Uid", "Uid");
                        bulkCopy.ColumnMappings.Add("Cascade", "Cascade");

                        bulkCopy.WriteToServer(table);

                        bulkCopy = null;

                        #endregion

                        #region Resolve asset types based on UIDs

                        Connection.Execute(@"
    update	T
    set		T.Object = S.Object, 
            T.ObjectID = S.ObjectID, 
            T.AssetTypeID = S.ID
    from	api.ExecutionDeletedAssetType T
		    inner join AssetType S on S.Uid = T.Uid and T.ExecutionID = @ExecutionID;",
                    new { execution.ExecutionID }, commandTimeout: timeout);

                        #endregion

                        #region Log lookup errors

                        Connection.Execute($@"
    update	api.ExecutionDeletedAssetType
    set		Success = 0,
		    [Message] = coalesce([Message] + '; ', '') + 'You must provide a valid Uid for this asset type when you are attempting to delete it'
    where	ExecutionID = @ExecutionID and ([Uid] is null or [Uid] = CAST(CAST(0 AS BINARY) AS UNIQUEIDENTIFIER)); 

    update	api.ExecutionDeletedAssetType
    set		Success = 0,
		    [Message] = coalesce([Message] + '; ', '') + 'Not found based on Uid provided'
    where	ExecutionID = @ExecutionID and AssetTypeID is null;",
                        new { execution.ExecutionID }, commandTimeout: timeout);

                        #endregion

                        #region Log cascade errors

                        Connection.Execute($@"
    update	T
    set		T.Success = 0,
		    T.[Message] = coalesce([Message] + '; ', '') + 'You have not enabled Cascade, yet there are ' + cast(A.AssetCount as nvarchar) + ' asset(s) present for this type.'
    from    api.ExecutionDeletedAssetType T
            cross apply (
                select  count(1) as AssetCount
                from    Asset
                where   AssetTypeID = T.AssetTypeID
                        and [State] not in (3,4)
            ) A 
    where	T.ExecutionID = @ExecutionID
            and T.[Cascade] = 0
            and A.AssetCount > 0; 

    update	T
    set		T.Success = 0,
		    T.[Message] = coalesce([Message] + '; ', '') + 'You have not enabled Cascade, yet there are ' + cast(A.ChildCount as nvarchar) + ' child asset type(s) present for this type.'
    from    api.ExecutionDeletedAssetType T
            cross apply (
                select  count(1) as ChildCount
                from	IntersectType I
		                inner join AssetType A on I.Object = A.Object and I.ObjectID = A.ObjectID and I.Subject = T.Object and I.SubjectID = T.ObjectID and A.[State] not in (3,4)
		                inner join [Predicate] P on P.ID = I.PredicateID and P.[Type] in (3,4)
            ) A 
    where	T.ExecutionID = @ExecutionID
            and T.Object not in ('PolicyType', 'TaxonomyType')
            and T.[Cascade] = 0
            and A.ChildCount > 0;",
                        new { execution.ExecutionID }, commandTimeout: timeout);

                        #endregion

                        generalChecksCompleted = true;
                    }
                    catch (Exception generalEx)
                    {
                        generalChecksCompleted = false;
                        var msg = generalEx.GetFullExceptionData(false);
                        execution.ErrorMessage = msg;
                        execution.Processed = 0;
                        execution.Error = import.Count();

                        results = new List<DatabaseBulkAssetTypeResult>();
                        results.AddRange(import.Select(i => new DatabaseBulkAssetTypeResult { ExecutionItemUid = i.ExecutionItemUid, Message = msg, Success = false }));
                    }

                    if (generalChecksCompleted)
                    {
                        int itemNumber = 1;

                        foreach (var t in import)
                        {
                            bool runCompleted = false;
                            int retryCount = 0;

                            while (!runCompleted && retryCount <= API_V2_RETRY_LIMIT)
                            {
                                try
                                {
                                    var thisResult = Connection.Query<DatabaseBulkAssetTypeResult>(
                                        "exec api.DeleteAssetType @executionUid, @itemNumber, @resourceID",
                                        new { executionUid = execution.ExecutionID, itemNumber, resourceID = CurrentResourceID },
                                        commandTimeout: timeout
                                    ).Single();

                                    results.Add(thisResult);

                                    runCompleted = true;
                                }
                                catch (Exception ex)
                                {
                                    retryCount++;

                                    if (retryCount > API_V2_RETRY_LIMIT)
                                    {
                                        LogLoopExecutionError(execution.ExecutionID, itemNumber, itemNumber, "api.ExecutionDeletedAssetType", ex.GetFullExceptionData(false), timeout);
                                    }
                                }
                            }

                            itemNumber++;
                        }

                        Connection.Close();
                    }
                }
            }

            return results;
        }

        public List<DatabaseBulkAssetResult> ImportAssets(ApiExecution execution, AssetType at, IEnumerable<IAssetUpsert> import, bool isInsert, int timeout = 3600, bool fieldJsonPropertyLoadLimitToTopLevel = true, bool sendWorkflowEvents = true, bool lookupFieldsPassedByValue = false, int mergeBlockSize = 500)
        {
            var results = new List<DatabaseBulkAssetResult>();
            var dupes = import.Where(i => i.ExecutionItemUid.HasValue).GroupBy(i => i.ExecutionItemUid).Where(i => i.Count() > 1).Select(i => new { ExecutionItemUid = i.Key, Count = i.Count() }).ToList();
            if (dupes.Any())
            {
                execution.ErrorMessage = $"Duplicate execution item identifiers: {string.Join(", ", dupes.Select(i => i.ExecutionItemUid.ToString()))}. Identifiers must be unique within a batch.";
                results.AddRange(import.Select(i => new DatabaseBulkAssetResult { ExecutionItemUid = i.ExecutionItemUid, uid = i.Uid, Message = execution.ErrorMessage, Success = false }));
            }
            else
            {
                var uidDupes = import.GroupBy(i => i.Uid).Where(i => i.Count() > 1).Select(i => new { Uid = i.Key, Count = i.Count() }).ToList();
                if (isInsert)
                {
                    uidDupes.RemoveAll(i => i.Uid == Guid.Empty); // No need to count empty Uids if this is an insert.
                }
                if (uidDupes.Any())
                {
                    execution.ErrorMessage = $"Duplicate Asset Uids: {string.Join(", ", uidDupes.Select(i => i.Uid.ToString()))}. Identifiers must be unique within a batch.";
                    results.AddRange(import.Select(i => new DatabaseBulkAssetResult { ExecutionItemUid = i.ExecutionItemUid, uid = i.Uid, Message = execution.ErrorMessage, Success = false }));
                }
                else
                {
                    #region Build data tables for bulk load.

                    var table = new DataTable();
                    table.Columns.Add("ExecutionID", typeof(Guid));
                    table.Columns.Add("ItemNumber", typeof(int));
                    table.Columns.Add("ExecutionItemUid", typeof(Guid));

                    table.Columns.Add("Message", typeof(string));
                    table.Columns.Add("Success", typeof(bool));
                    table.Columns.Add("Uid", typeof(Guid));
                    table.Columns.Add("ParentUid", typeof(Guid));
                    table.Columns.Add("ObjectType", typeof(string));
                    table.Columns.Add("ObjectTypeID", typeof(int));

                    table.Columns.Add("ParentObjectType", typeof(string));
                    table.Columns.Add("ParentObjectTypeID", typeof(int));
                    table.Columns.Add("IntersectTypeUid", typeof(Guid));
                    table.Columns.Add("IntersectTypeID", typeof(int));

                    var errorTable = new DataTable();
                    errorTable.Columns.Add("ExecutionID", typeof(Guid));
                    errorTable.Columns.Add("ItemNumber", typeof(int));
                    errorTable.Columns.Add("ExecutionItemUid", typeof(Guid));
                    errorTable.Columns.Add("Uid", typeof(Guid));
                    errorTable.Columns.Add("Message", typeof(string));

                    var fieldTable = new DataTable();
                    fieldTable.Columns.Add("ExecutionID", typeof(Guid));
                    fieldTable.Columns.Add("ItemNumber", typeof(int));
                    fieldTable.Columns.Add("FieldName", typeof(string));
                    fieldTable.Columns.Add("FieldValue", typeof(string));
                    fieldTable.Columns.Add("FieldTypeID", typeof(int));

                    #endregion

                    bool generalChecksCompleted = false;
                    List<FieldType> fieldTypes = null;
                    List<FieldType> jsonFieldTypes = null;
                    List<string> requiredFieldTypeNames = null;
                    var predicateType = DeterminePredicateType(at.Object);
                    IntersectType it = null;
                    string parentObject = null;
                    int? parentObjectID = null;
                    List<Guid> parentIntersectGuids = new List<Guid>();
                    Guid? intersectTypeUid = null;
                    int? intersectTypeID = null;
                    CurrentExecutionLocationModel currentLocation = null;
                    bool hasLookupFieldTypes = false;
                    List<AssetFieldTypeUpdate> fieldTypeUpdates = new List<AssetFieldTypeUpdate>();

                    try
                    {
                        currentLocation = GetCurrentExecutionLocation(execution.ExecutionID, "api.ExecutionAsset");

                        if (currentLocation.HighestItemNumberProcessed > 0)
                        {
                            results.AddRange(
                                Query<DatabaseBulkAssetResult>(
                                    $"select * from api.ExecutionAsset where ExecutionID = @ExecutionID and ItemNumber <= {currentLocation.HighestItemNumberProcessed}",
                                    new { execution.ExecutionID }
                                )
                            );
                        }

                        // Get field types.
                        fieldTypes = Query<FieldType>("select * from FieldType where Object = @Object and ObjectID = @ObjectID", new { at.Object, at.ObjectID }).ToList();
                        jsonFieldTypes = fieldTypes.Where(f => f.Type == DataType.JSON.ToString()).ToList();
                        requiredFieldTypeNames = fieldTypes.Where(f => f.IsRequired && string.IsNullOrEmpty(f.DefaultValue)).Select(f => f.Name).ToList();
                        hasLookupFieldTypes = fieldTypes.Any(f => f.Type == DataType.Lookup.ToString());

                        #region Generate data sets

                        if (predicateType.HasValue)
                        {
                            it = Filter<IntersectType>(o => o.Object == at.Object && o.ObjectID == at.ObjectID && o.Predicate.Type == predicateType).FirstOrDefault();
                            if (it != null)
                            {
                                parentObject = it.Subject;
                                parentObjectID = it.SubjectID;
                                intersectTypeUid = it.uid;
                                intersectTypeID = it.ID;
                            }
                            else
                            {
                                if (at.Object == "FusionAttributeType")
                                {
                                    var fusionAttributeType = GetById<FusionAttributeType>(at.ObjectID);
                                    if (fusionAttributeType != null)
                                    {
                                        if (fusionAttributeType.ParentID.HasValue)
                                        {
                                            parentObject = "FusionAttributeType";
                                            parentObjectID = fusionAttributeType.ParentID;
                                        }
                                    }
                                }
                            }
                        }

                        int i = 1;
                        foreach (var model in import)
                        {
                            if (i > currentLocation.HighestItemNumber)
                            {
                                bool success;
                                string errorMessage;
                                var fieldRows = ValidateFields(at.Object, at.ObjectID, isInsert, fieldTypes, requiredFieldTypeNames, model.Fields, execution.ExecutionID, i, fieldTable, out success, out errorMessage);

                                if (success && isInsert && parentObjectID.HasValue && predicateType == PredicateType.InterTypeHierarchy)
                                {
                                    // Check to ensure ParentUid is present.
                                    success = model.ParentUid.HasValue;
                                    if (!success)
                                    {
                                        errorMessage = "Asset is missing a required ParentUid value";
                                    }
                                }

                                if (success && isInsert)
                                {
                                    if (at.Object == "FusionAttributeType")
                                    {
                                        // Check to ensure Name is present.
                                        success = model.Fields.ContainsKey("Name");
                                        if (!success)
                                        {
                                            errorMessage = "Asset is missing a required Name field value";
                                        }

                                        // Check to ensure FusionID is present.
                                        if (success)
                                        {
                                            success = model.Fields.ContainsKey("FusionID");
                                            if (!success)
                                            {
                                                errorMessage = "Asset is missing a required FusionID field value";
                                            }
                                        }
                                    }
                                    if (at.Object == "RuleType")
                                    {
                                        // Check to ensure Threshold is present.
                                        if (success)
                                        {
                                            success = model.Fields.ContainsKey("Threshold");
                                            if (!success)
                                            {
                                                errorMessage = "Asset is missing a required Threshold field value";
                                            }
                                        }
                                    }
                                    if (at.Object == "ReferenceItemType")
                                    {
                                        // Check to ensure Code is present.
                                        success = model.Fields.ContainsKey("Code");
                                        if (!success)
                                        {
                                            errorMessage = "Asset is missing a required Code field value";
                                        }
                                    }
                                }

                                if (success)
                                {
                                    fieldRows.ForEach(fr => { fieldTable.Rows.Add(fr); });

                                    var row = table.NewRow();

                                    row["ExecutionID"] = execution.ExecutionID;
                                    row["ItemNumber"] = i;
                                    if (model.ExecutionItemUid.HasValue) row["ExecutionItemUid"] = model.ExecutionItemUid.Value;
                                    row["Uid"] = model.Uid;
                                    if (model.ParentUid.HasValue) row["ParentUid"] = model.ParentUid;
                                    row["ObjectType"] = at.Object;
                                    row["ObjectTypeID"] = at.ObjectID;

                                    if (!string.IsNullOrEmpty(parentObject)) row["ParentObjectType"] = parentObject;
                                    if (parentObjectID.HasValue) row["ParentObjectTypeID"] = parentObjectID.Value;
                                    if (intersectTypeUid.HasValue) row["IntersectTypeUid"] = intersectTypeUid.Value;
                                    if (intersectTypeID.HasValue) row["IntersectTypeID"] = intersectTypeID.Value;

                                    table.Rows.Add(row);
                                }
                                else
                                {
                                    var errorRow = errorTable.NewRow();
                                    errorRow["ExecutionID"] = execution.ExecutionID;
                                    errorRow["ItemNumber"] = i;
                                    if (model.ExecutionItemUid.HasValue) errorRow["ExecutionItemUid"] = model.ExecutionItemUid.Value;
                                    errorRow["Uid"] = model.Uid;
                                    errorRow["Message"] = errorMessage;

                                    errorTable.Rows.Add(errorRow);

                                    results.Add(new DatabaseBulkAssetResult { IsNew = false, ItemNumber = i, Message = errorMessage, Success = false });
                                }
                            }

                            i++;
                        }

                        #endregion

                        if (results.Count > 0) // There are errors already processed.
                        {
                            OnAssetsPartiallyProcessed(new AssetsPartiallyProcessedEventArgs
                            {
                                Results = results
                            });
                        }

                        if (Database.Connection.State != ConnectionState.Open)
                            Connection.OpenWithRetry(RetryPolicy.DefaultProgressive);

                        #region Bulk Copy

                        SqlBulkCopy bulkCopy = new SqlBulkCopy((SqlConnection)Database.Connection);

                        bulkCopy.BatchSize = table.Rows.Count;
                        bulkCopy.DestinationTableName = "api.ExecutionAsset";
                        bulkCopy.BulkCopyTimeout = timeout;

                        bulkCopy.ColumnMappings.Add("ExecutionID", "ExecutionID");
                        bulkCopy.ColumnMappings.Add("ItemNumber", "ItemNumber");
                        bulkCopy.ColumnMappings.Add("ExecutionItemUid", "ExecutionItemUid");
                        bulkCopy.ColumnMappings.Add("Uid", "Uid");
                        bulkCopy.ColumnMappings.Add("ObjectType", "ObjectType");
                        bulkCopy.ColumnMappings.Add("ObjectTypeID", "ObjectTypeID");

                        bulkCopy.ColumnMappings.Add("ParentUid", "ParentUid");
                        bulkCopy.ColumnMappings.Add("ParentObjectType", "ParentObjectType");
                        bulkCopy.ColumnMappings.Add("ParentObjectTypeID", "ParentObjectTypeID");

                        bulkCopy.ColumnMappings.Add("IntersectTypeUid", "IntersectTypeUid");
                        bulkCopy.ColumnMappings.Add("IntersectTypeID", "IntersectTypeID");

                        bulkCopy.WriteToServer(table);



                        bulkCopy = new SqlBulkCopy((SqlConnection)Database.Connection);

                        bulkCopy.BatchSize = errorTable.Rows.Count;
                        bulkCopy.DestinationTableName = "api.ExecutionAssetError";
                        bulkCopy.BulkCopyTimeout = timeout;

                        bulkCopy.ColumnMappings.Add("ExecutionID", "ExecutionID");
                        bulkCopy.ColumnMappings.Add("ItemNumber", "ItemNumber");
                        bulkCopy.ColumnMappings.Add("ExecutionItemUid", "ExecutionItemUid");
                        bulkCopy.ColumnMappings.Add("Uid", "Uid");
                        bulkCopy.ColumnMappings.Add("Message", "Message");

                        bulkCopy.WriteToServer(errorTable);



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

                        bulkCopy = null;

                        #endregion

                        if (hasLookupFieldTypes)
                        {
                            if (lookupFieldsPassedByValue)
                            {
                                CopyFieldLookupValuesAsIs(execution.ExecutionID, timeout);
                            }
                            else
                            {
                                ResolveFieldLookupValues(execution.ExecutionID, timeout);
                            }
                        }

                        if (at.Object == "RuleType")
                        {
                            ResolveRuleTypeLookupValues(execution.ExecutionID, timeout);
                        }

                        if (hasLookupFieldTypes)
                        {
                            LogFieldLookupErrors(execution.ExecutionID, at.Object, at.ObjectID, "Asset", timeout);
                        }

                        LogRelationshipErrors(execution.ExecutionID, at.Object, at.ObjectID, "Asset", timeout, lookupFieldsPassedByValue);
                        ValidateAssetAndParent(execution.ExecutionID, at.ID, timeout);

                        LogParentErrors(execution.ExecutionID, timeout);                // If you cannot find parent based on Uids provided.

                        if (!isInsert)
                        {
                            LogAssetErrors(execution.ExecutionID, timeout);             // If you cannot find asset based on Uids provided.
                            LoadMissingKeyFields(execution.ExecutionID, at, timeout);   // Get missing key fields if this is an update.
                        }

                        #region Generate proposed key hash and compare against existing data.

                        string keyErrorMessage = "'Key values match another asset under a different set of key fields. '";
                        string keyTableTempCreation = @"CREATE TABLE #Keys (AssetID bigint, ActiveKey varchar(32)); CREATE CLUSTERED INDEX CIX_TempApiExecutionKeys ON #Keys ( ActiveKey ASC ); ";
                        string keyComparisonUpdateStatement = $@"
update  T 
set     T.Success = 0, 
        T.Message = {keyErrorMessage}
from    api.ExecutionAsset T 
        inner join #Keys S on T.ExecutionID = @ExecutionID and S.ActiveKey = T.ProposedKey and S.AssetID <> T.AssetID and T.AssetID is not null; 

update  T 
set     T.Success = 0, 
        T.Message = {keyErrorMessage}
from    api.ExecutionAsset T 
        inner join #Keys S on T.ExecutionID = @ExecutionID and S.ActiveKey = T.ProposedKey and T.AssetID is null; ";

                        if (at.Object == "FusionAttributeType")
                        {
                            LogErrorsWhereChildFusionConfigDifferentFromParent(execution.ExecutionID);

                            Connection.Execute($@"
{keyTableTempCreation}

update  A
set     A.ProposedKey = utility.GetHash(
                            FC.FieldValue + '|' + COALESCE(
                                FS.FieldValue, 
                                COALESCE(cast(A.ParentUid as nvarchar(50))+'|', '') + FN.FieldValue + coalesce('|'+DF.DynamicProposedKey,'')
                            )
                        )
from	api.ExecutionAsset A
        inner join api.ExecutionField FC on FC.ExecutionID = A.ExecutionID and FC.ItemNumber = A.ItemNumber and FC.FieldName = 'FusionID'
        inner join api.ExecutionField FN on FN.ExecutionID = A.ExecutionID and FN.ItemNumber = A.ItemNumber and FN.FieldName = 'Name'
        left join api.ExecutionField FS on FS.ExecutionID = A.ExecutionID and FS.ItemNumber = A.ItemNumber and FS.FieldName = 'SourceID'
        outer apply (
            select		DF.ItemNumber,
                        STRING_AGG(coalesce(DF.LookupValue, DF.FieldValue, DFT.DefaultValue), '|') within group (order by DFT.ColumnOrder asc, DFT.Name asc) as DynamicProposedKey
            from		api.ExecutionField DF
                        inner join FieldType DFT on DFT.ID = DF.FieldTypeID and DFT.IsPartOfKey = 1 and DF.ExecutionID = A.ExecutionID and DF.ItemNumber = A.ItemNumber
            group by    DF.ItemNumber
        ) DF
where	A.ExecutionID = @ExecutionID;

insert into #Keys
    select	A.ID,
            utility.GetHash(
                cast(O.FusionID as nvarchar) + '|' + COALESCE(
                    O.SourceID, 
                    COALESCE(cast(P.Uid as nvarchar(50))+'|', '') + O.Name + COALESCE('|'+DF.ProposedKey,'')
                )
            ) as ActiveKey
    from	Asset A 
            inner join FusionAttribute O on A.Object = 'FusionAttribute' and O.ID = A.ObjectID
            left join Asset P on P.Object = 'FusionAttribute' and P.ObjectID = O.ParentID and O.ParentID is not null
            left join (
                select		A.ID,
                            STRING_AGG(coalesce(F.Value, FT.DefaultValue), '|') within group (order by FT.ColumnOrder asc, FT.Name asc) as ProposedKey
                from		Asset A 
                            inner join FieldType FT on FT.AssetTypeID = A.AssetTypeID and FT.IsPartOfKey = 1
                            left join Field F on FT.ID = F.FieldTypeID and F.AssetID = A.ID
                where	    A.AssetTypeID = @ID
                group by    A.ID
            ) DF on DF.ID = A.ID
    where	A.AssetTypeID = @ID;

{keyComparisonUpdateStatement}",
                            new { execution.ExecutionID, at.ID }, commandTimeout: timeout);
                        }
                        else if (at.Object == "ReferenceItemType")
                        {
                            Connection.Execute($@"
update  T
set     T.ProposedKey = utility.GetHash(S.ProposedKey) 
from    api.ExecutionAsset T
		inner join	(
					select		A.ItemNumber,
								F.FieldValue as ProposedKey
					from		api.ExecutionAsset A
								inner join api.ExecutionField F on F.ExecutionID = A.ExecutionID and F.ItemNumber = A.ItemNumber and F.FieldName = 'Code'
					where		A.ExecutionID = @ExecutionID	
					) S on T.ExecutionID = @ExecutionID and S.ItemNumber = T.ItemNumber;

{keyTableTempCreation}

insert into #Keys
    select		A.ID,
                utility.GetHash(A.Code) as ActiveKey
    from		Asset A 
    where	    A.AssetTypeID = @ID;

{keyComparisonUpdateStatement}",
                            new { execution.ExecutionID, at.ID }, commandTimeout: timeout);
                        }
                        else
                        {
                            var activeKeySql = $@"
select		A.ID,
			utility.GetHash(STRING_AGG(coalesce(F.Value, FT.DefaultValue), '|') within group (order by FT.ColumnOrder asc, FT.Name asc)) as ActiveKey 
from		Asset A 
			inner join FieldType FT on FT.AssetTypeID = A.AssetTypeID and FT.IsPartOfKey = 1
			left join Field F on FT.ID = F.FieldTypeID and F.AssetID = A.ID
where	    A.AssetTypeID = @ID
group by    A.ID;";

                            if (parentObjectID.HasValue)
                            {
                                activeKeySql = $@"
select		A.ID,
			utility.GetHash(COALESCE(cast(P.Uid as nvarchar(50))+'|', '') + STRING_AGG(coalesce(F.Value, FT.DefaultValue), '|') within group (order by FT.ColumnOrder asc, FT.Name asc)) as ActiveKey
from		Asset A 
			inner join [Intersect] I on I.IntersectTypeID = @intersectTypeID and I.Object = A.Object and I.ObjectID = A.ObjectID
			inner join Asset P on P.Object = I.Subject and P.ObjectID = I.SubjectID
			inner join FieldType FT on FT.AssetTypeID = A.AssetTypeID and FT.IsPartOfKey = 1
			left join Field F on FT.ID = F.FieldTypeID and F.AssetID = A.ID
where		A.AssetTypeID = @ID
group by	A.ID, P.Uid";
                            }

                            Connection.Execute($@"
update  T
set     T.ProposedKey = utility.GetHash(S.ProposedKey) 
from    api.ExecutionAsset T
		inner join	(
					select		A.ItemNumber,
								COALESCE(cast(A.ParentUid as nvarchar(50))+'|', '') + STRING_AGG(coalesce(F.LookupValue, F.FieldValue), '|') within group (order by FT.ColumnOrder asc, FT.Name asc) as ProposedKey
					from		api.ExecutionAsset A
								inner join api.ExecutionField F on F.ExecutionID = A.ExecutionID and F.ItemNumber = A.ItemNumber
								inner join FieldType FT on FT.AssetTypeID = @ID and FT.ID = F.FieldTypeID and FT.IsPartOfKey = 1
					where		A.ExecutionID = @ExecutionID
					group by	A.ItemNumber, A.ParentUid
					) S on T.ExecutionID = @ExecutionID and S.ItemNumber = T.ItemNumber;

{keyTableTempCreation}

insert into #Keys
    {activeKeySql} 

{keyComparisonUpdateStatement}",
                            new { execution.ExecutionID, at.ID, intersectTypeID }, commandTimeout: timeout);
                        }

                        #endregion

                        #region Invalidate repetitious items in load

                        Connection.Execute($@"
update	T
set		T.Success = 0,
		T.[Message] = coalesce(T.[Message] + '; ', '') + 'Asset with matching key is already referenced previously. Nodes must be unique within a load.'
from	api.ExecutionAsset T
		inner join	(
					select	min(ItemNumber) as ItemNumber,
							ProposedKey
					from	api.ExecutionAsset
                    where   ExecutionID = @ExecutionID
					group by ProposedKey
					) S on T.ExecutionID = @ExecutionID and S.ProposedKey = T.ProposedKey and S.ItemNumber < T.ItemNumber;",
                        new { execution.ExecutionID }, commandTimeout: timeout);

                        #endregion

                        // Validate permissions
                        LogAssetPermissionErrors(execution.ExecutionID, at, Permission.ModifyAsset, "ExecutionAsset");

                        generalChecksCompleted = true;
                    }
                    catch (Exception generalEx)
                    {
                        generalChecksCompleted = false;
                        var msg = generalEx.GetFullExceptionData(false);
                        execution.ErrorMessage = msg;
                        execution.Processed = 0;
                        execution.Error = import.Count();

                        results = new List<DatabaseBulkAssetResult>();
                        results.AddRange(import.Select(i => new DatabaseBulkAssetResult { ExecutionItemUid = i.ExecutionItemUid, Message = msg, Success = false }));
                    }

                    if (generalChecksCompleted)
                    {
                        int loopSize = mergeBlockSize;
                        int numberOfLoops = (int)Math.Ceiling((decimal)(execution.Total - currentLocation.HighestItemNumberProcessed) / loopSize);
                        int beginItemNumber = currentLocation.HighestItemNumberProcessed + 1;
                        int endItemNumber = currentLocation.HighestItemNumberProcessed + loopSize;

                        for (int currentLoop = 1; currentLoop <= numberOfLoops; currentLoop++)
                        {
                            bool runCompleted = false;
                            int retryCount = 0;

                            while (!runCompleted && retryCount <= API_V2_RETRY_LIMIT)
                            {
                                #region common sql

                                var executionAssetWhereSql = $"ExecutionID = @ExecutionID and Success is null and ItemNumber between @beginItemNumber and @endItemNumber";
                                var updateAssetInfoOnExecutionRecordsSql = $@"update  T
    set     T.AssetID = S.ID, T.Uid = S.Uid
    from    api.ExecutionAsset T
            inner join Asset S on T.Executionid = @ExecutionID and S.AssetTypeID = @AssetTypeID and S.Object = T.Object and S.ObjectID = T.ObjectID and T.ItemNumber between @beginItemNumber and @endItemNumber;";
                                var insertGraphAssetNode = $@"		
insert into graph.AssetNode (ID, [Uid], AssetTypeID, AssetTypeUid, [State], UpdatedOn)
        select  EA.AssetID,
				EA.Uid,
				@AssetTypeID,
				T.[Uid] as AssetTypeUid,
				1,
				@D
        from    api.ExecutionAsset EA
                inner join #ObjectMergeTableResult R on R.ItemNumber = EA.ItemNumber and R.[Operation] = 'INSERT'
                inner join AssetType T on T.ID = @AssetTypeID
        where EA.ExecutionID = @ExecutionID and not exists (select 1 from graph.AssetNode where [uid] = EA.Uid)";

                                #endregion

                                using (var trans = Connection.BeginTransaction())
                                {
                                    try
                                    {
                                        switch (at.Class)
                                        {

                                            case AssetTypeClass.Model:
                                                #region
                                                if (isInsert)
                                                {
                                                    Connection.Execute($@"
                                                        create table #ObjectMergeTableResult (ID int, ItemNumber int, [Operation] varchar(10));
                                                        CREATE NONCLUSTERED INDEX IX_TempObjectMergeTableResult ON #ObjectMergeTableResult ( ItemNumber ASC );

                                                        merge   [Asset] as T
                                                        using   (
                                                                select  ItemNumber
                                                                from    api.ExecutionAsset
                                                                where   ExecutionID = @ExecutionID
                                                                        and Success is null
                                                                        and ItemNumber between @beginItemNumber and @endItemNumber
                                                                ) S
                                                        on      (T.AssetTypeID = @AssetTypeID and T.SourceID = @NonExistentUid)
                                                        when    not matched then
                                                        insert  (AssetTypeID,State,[Object], CreatedBy, CreatedOn, UpdatedBy, UpdatedOn)
                                                        values  (@AssetTypeID,1,'Taxonomy', @R, @D, @R, @D)
                                                        output  inserted.ObjectID, S.ItemNumber, $action into #ObjectMergeTableResult;

                                                        update  T
                                                        set     T.Object = 'Taxonomy',
                                                                T.ObjectID = S.ID,
                                                                T.IsNew = 1
                                                        from    api.ExecutionAsset T
                                                                inner join #ObjectMergeTableResult S on T.Executionid = @ExecutionID and S.ItemNumber = T.ItemNumber;
                                                            
                                                        {updateAssetInfoOnExecutionRecordsSql}

                                                        {insertGraphAssetNode}",
                                                        new { beginItemNumber, endItemNumber, execution.ExecutionID, at.ObjectID, AssetTypeID = at.ID, NonExistentUid = Guid.NewGuid().ToString(), R = CurrentResourceID, D = DateTime.UtcNow }, transaction: trans, commandTimeout: timeout);
                                                }
                                                else
                                                {
                                                    Connection.Execute($@"
                                                        update	T
                                                        set		T.UpdatedBy = @R,
                                                        T.UpdatedOn = @D
                                                        from	[Asset] T
                                                        inner join api.ExecutionAsset S on S.ObjectID = T.ObjectID and T.[Object] = 'Taxonomy' and {executionAssetWhereSql};

                                                        update	api.ExecutionAsset
                                                        set		IsNew = 0
                                                        where	{executionAssetWhereSql};",
                                                    new { execution.ExecutionID, R = CurrentResourceID, D = DateTime.UtcNow, beginItemNumber, endItemNumber }, transaction: trans, commandTimeout: timeout);
                                                }
                                                break;
                                            #endregion
                                            case AssetTypeClass.FusionAttribute:
                                                #region
                                                if (isInsert)
                                                {
                                                    Connection.Execute($@"
    create table #ObjectMergeTableResult (ID int, ItemNumber int, [Operation] varchar(10));
    CREATE NONCLUSTERED INDEX IX_TempObjectMergeTableResult ON #ObjectMergeTableResult ( ItemNumber ASC );

    merge   FusionAttribute as T
    using   (
            select  A.ParentObjectID,
                    A.ItemNumber,
                    F.FieldValue as FusionID,
                    N.FieldValue as Name,
                    FS.FieldValue as SourceID
            from    api.ExecutionAsset A
                    inner join api.ExecutionField F on F.ExecutionID = A.ExecutionID and F.ItemNumber = A.ItemNumber and F.FieldName = 'FusionID'
                    inner join api.ExecutionField N on N.ExecutionID = A.ExecutionID and N.ItemNumber = A.ItemNumber and N.FieldName = 'Name'
                    left join api.ExecutionField FS on FS.ExecutionID = A.ExecutionID and FS.ItemNumber = A.ItemNumber and FS.FieldName = 'SourceID'
            where   A.ExecutionID = @ExecutionID
                    and A.Success is null
                    and A.ItemNumber between @beginItemNumber and @endItemNumber
            ) S
    on      (T.FusionAttributeTypeID = @ObjectID and T.SourceID = @NonExistentUid)
    when    not matched then
    insert  (FusionAttributeTypeID, ParentID, Name, FusionID, SourceID)
    values  (@ObjectID, S.ParentObjectID, S.Name, S.FusionID, S.SourceID)
    output  inserted.ID, S.ItemNumber, $action into #ObjectMergeTableResult;

    update  T
    set     T.Object = 'FusionAttribute',
            T.ObjectID = S.ID,
            T.IsNew = 1
    from    api.ExecutionAsset T
            inner join #ObjectMergeTableResult S on T.Executionid = @ExecutionID and S.ItemNumber = T.ItemNumber;

    {updateAssetInfoOnExecutionRecordsSql}

    {insertGraphAssetNode}",
                                                    new { beginItemNumber, endItemNumber, execution.ExecutionID, at.ObjectID, AssetTypeID = at.ID, NonExistentUid = Guid.NewGuid().ToString(), D = DateTime.UtcNow }, transaction: trans, commandTimeout: timeout);
                                                }
                                                else
                                                {
                                                    Connection.Execute($@"
    update	T
    set		T.Name = N.FieldValue
    from	FusionAttribute T
		    inner join api.ExecutionAsset S on S.ObjectID = T.ID and S.ExecutionID = @ExecutionID and S.Success is null and S.ItemNumber between @beginItemNumber and @endItemNumber
            inner join api.ExecutionField N on N.ExecutionID = S.ExecutionID and N.ItemNumber = S.ItemNumber and N.FieldName = 'Name';

    update	api.ExecutionAsset
    set		IsNew = 0
    where	{executionAssetWhereSql};",
                                                    new { execution.ExecutionID, R = CurrentResourceID, D = DateTime.UtcNow, beginItemNumber, endItemNumber }, transaction: trans, commandTimeout: timeout);
                                                }

                                                #region Recalculate the text paths

                                                Connection.Execute($@"
    WITH hierarchy (RootID, ID, ParentID, ItemPath) AS
    (
	    SELECT	ID,
			    ID, 
			    ParentID,
			    cast(name as nvarchar(2500))
	    FROM	FusionAttribute F
			    inner join api.ExecutionAsset S on S.ObjectID = F.ID and {executionAssetWhereSql}
	    UNION ALL
	    SELECT	c.RootID,
			    p.ID, 
			    p.ParentID,
			    cast(p.name + '.' +  c.ItemPath as nvarchar(2500))
	    FROM	hierarchy c
			    inner join FusionAttribute p ON p.ID = c.parentid
    )
    update	T
    set		T.TextPath = cte.ItemPath
    from	FusionAttribute T
		    inner join hierarchy cte on cte.RootID = T.ID and cte.ParentID is null option (MAXRECURSION 10);",
                                                new { execution.ExecutionID, R = CurrentResourceID, D = DateTime.UtcNow, beginItemNumber, endItemNumber }, transaction: trans, commandTimeout: timeout);

                                                #endregion

                                                break;
                                            #endregion
                                            case AssetTypeClass.Policy:
                                            case AssetTypeClass.BusinessAsset:
                                            case AssetTypeClass.TechnicalAsset:
                                                #region
                                                string @object = "Artifact";
                                                if (at.Class == AssetTypeClass.Policy)
                                                    @object = "Policy";

                                                if (isInsert)
                                                {
                                                    Connection.Execute($@"
    create table #ObjectMergeTableResult (ID int, ItemNumber int, [Operation] varchar(10));
    CREATE NONCLUSTERED INDEX IX_TempObjectMergeTableResult ON #ObjectMergeTableResult ( ItemNumber ASC );

    merge   [Asset] as T
    using   (
            select  ItemNumber
            from    api.ExecutionAsset
            where   ExecutionID = @ExecutionID
                    and Success is null
                    and ItemNumber between @beginItemNumber and @endItemNumber
            ) S
    on      (T.AssetTypeID = @AssetTypeID and T.SourceID = @NonExistentUid)
    when    not matched then
    insert  (AssetTypeID,State,[Object], CreatedBy, CreatedOn, UpdatedBy, UpdatedOn)
    values  (@AssetTypeID,1,@Object, @R, @D, @R, @D)
    output  inserted.ObjectID, S.ItemNumber, $action into #ObjectMergeTableResult;

    update  T
    set     T.Object = @Object,
            T.ObjectID = S.ID,
            T.IsNew = 1
    from    api.ExecutionAsset T
            inner join #ObjectMergeTableResult S on T.Executionid = @ExecutionID and S.ItemNumber = T.ItemNumber;

    {updateAssetInfoOnExecutionRecordsSql}

    {insertGraphAssetNode}",
                                                    new { beginItemNumber, endItemNumber, execution.ExecutionID, at.ObjectID, AssetTypeID = at.ID, NonExistentUid = Guid.NewGuid().ToString(), R = CurrentResourceID, D = DateTime.UtcNow, @object }, transaction: trans, commandTimeout: timeout);
                                                }
                                                else
                                                {
                                                    Connection.Execute($@"
    update	T
    set		T.UpdatedBy = @R,
		    T.UpdatedOn = @D
    from	[Asset] T
		    inner join api.ExecutionAsset S on S.ObjectID = T.ObjectID and T.[Object] = @Object and {executionAssetWhereSql};

    update	api.ExecutionAsset
    set		IsNew = 0
    where	{executionAssetWhereSql};",
                                                    new { execution.ExecutionID, R = CurrentResourceID, D = DateTime.UtcNow, @object, beginItemNumber, endItemNumber }, transaction: trans, commandTimeout: timeout);
                                                }
                                                break;
                                            #endregion
                                            case AssetTypeClass.Rule:
                                                #region
                                                if (isInsert)
                                                {
                                                    Connection.Execute($@"
    create table #ObjectMergeTableResult (ID int, ItemNumber int);
    CREATE NONCLUSTERED INDEX IX_TempObjectMergeTableResult ON #ObjectMergeTableResult ( ItemNumber ASC );

    merge   [Rule] as T
    using   (
            select  A.ItemNumber,
                    T.FieldValue as Threshold
            from    api.ExecutionAsset A
                    inner join api.ExecutionField T on T.ExecutionID = A.ExecutionID and T.ItemNumber = A.ItemNumber and T.FieldName = 'Threshold'
            where   A.ExecutionID = @ExecutionID
                    and A.Success is null
                    and A.ItemNumber between @beginItemNumber and @endItemNumber
            ) S
    on      (T.RuleTypeID = @ObjectID and T.SourceID = @NonExistentUid)
    when    not matched then
    insert  (RuleTypeID, Threshold, CreatedBy, CreatedOn, UpdatedBy, UpdatedOn)
    values  (@ObjectID, S.Threshold, @R, @D, @R, @D)
    output  inserted.ID, S.ItemNumber into #ObjectMergeTableResult;

    update  T
    set     T.Object = 'Rule',
            T.ObjectID = S.ID,
            T.IsNew = 1
    from    api.ExecutionAsset T
            inner join #ObjectMergeTableResult S on T.Executionid = @ExecutionID and S.ItemNumber = T.ItemNumber;

    {updateAssetInfoOnExecutionRecordsSql}

    {insertGraphAssetNode}",
                                                    new { beginItemNumber, endItemNumber, execution.ExecutionID, at.ObjectID, AssetTypeID = at.ID, NonExistentUid = Guid.NewGuid().ToString(), R = CurrentResourceID, D = DateTime.UtcNow },
                                                    transaction: trans,
                                                    commandTimeout: timeout);
                                                }
                                                else
                                                {
                                                    Connection.Execute($@"
    update	T
    set		
            T.Threshold = case when FD.FieldValue is not null then FD.FieldValue else T.Threshold end,
            T.UpdatedBy = @R,
		    T.UpdatedOn = @D
    from	[Rule] T
		    inner join api.ExecutionAsset S on S.ObjectID = T.ID and {executionAssetWhereSql}
            left join api.ExecutionField FD on FD.ExecutionID = S.ExecutionID and FD.ItemNumber = S.ItemNumber and FD.FieldName = 'Threshold';

    update	api.ExecutionAsset
    set		IsNew = 0
    where	{executionAssetWhereSql};",
                                                    new { execution.ExecutionID, R = CurrentResourceID, D = DateTime.UtcNow, beginItemNumber, endItemNumber }, transaction: trans, commandTimeout: timeout);
                                                }
                                                break;
                                            #endregion
                                            case AssetTypeClass.Reference:
                                                #region
                                                if (isInsert)
                                                {
                                                    Connection.Execute($@"
                                                        create table #ObjectMergeTableResult (ID int, ItemNumber int, [Operation] varchar(10));
                                                        CREATE NONCLUSTERED INDEX IX_TempObjectMergeTableResult ON #ObjectMergeTableResult ( ItemNumber ASC );

                                                        merge   [Asset] as T
                                                        using   (
                                                                select  A.ItemNumber,
                                                                        C.FieldValue as [Code]
                                                                from    api.ExecutionAsset A
                                                                        inner join api.ExecutionField C on C.ExecutionID = A.ExecutionID and C.ItemNumber = A.ItemNumber and C.FieldName = 'Code'
                                                                where   A.ExecutionID = @ExecutionID
                                                                        and A.Success is null
                                                                        and A.ItemNumber between @beginItemNumber and @endItemNumber
                                                                ) S
                                                        on      (T.AssetTypeID = @AssetTypeID and T.[Code] = @NonExistentUid)
                                                        when    not matched then
                                                        insert  (AssetTypeID,State,[Object], [Code], CreatedBy, CreatedOn, UpdatedBy, UpdatedOn)
                                                        values  (@AssetTypeID,1,'ReferenceItem', S.[Code], @R, @D, @R, @D)
                                                        output  inserted.ObjectID, S.ItemNumber, $action into #ObjectMergeTableResult;

                                                        update  T
                                                        set     T.Object = 'ReferenceItem',
                                                                T.ObjectID = S.ID,
                                                                T.IsNew = 1
                                                        from    api.ExecutionAsset T
                                                                inner join #ObjectMergeTableResult S on T.Executionid = @ExecutionID and S.ItemNumber = T.ItemNumber;

                                                        {updateAssetInfoOnExecutionRecordsSql}",
                                                    new { beginItemNumber, endItemNumber, execution.ExecutionID, R = CurrentResourceID, D = DateTime.UtcNow, at.ObjectID, AssetTypeID = at.ID, NonExistentUid = Guid.NewGuid().ToString() }, transaction: trans, commandTimeout: timeout);
                                                }
                                                else
                                                {
                                                    Connection.Execute($@"
                                                        update	T
                                                        set		T.[Code] = C.FieldValue,
                                                                T.UpdatedBy = @R,
                                                                T.UpdatedOn = @D
                                                        from	Asset T
		                                                        inner join api.ExecutionAsset S on S.ObjectID = T.ObjectID and S.[Object]=T.[Object] and T.[Object]='ReferenceItem'  and S.ExecutionID = @ExecutionID and S.Success is null and S.ItemNumber between @beginItemNumber and @endItemNumber
                                                                inner join api.ExecutionField C on C.ExecutionID = S.ExecutionID and C.ItemNumber = S.ItemNumber and C.FieldName = 'Code';

                                                        update	api.ExecutionAsset
                                                        set		IsNew = 0
                                                        where	{executionAssetWhereSql};",
                                                    new { execution.ExecutionID, R = CurrentResourceID, D = DateTime.UtcNow, beginItemNumber, endItemNumber }, transaction: trans, commandTimeout: timeout);
                                                }
                                                break;
                                                #endregion

                                        }

                                        #region Parent/Child Relationship

                                        if (intersectTypeID.HasValue)
                                        {
                                            parentIntersectGuids = Connection.Query<Guid>($@"
drop table if exists #ParentChildRelationships;
create table #ParentChildRelationships([operation] varchar(10),[uid] uniqueidentifier);

    merge       [Intersect] as T
    using		(
			    select  * 
                from    api.ExecutionAsset 
                where   ExecutionID = @ExecutionID 
                        and Success is null 
                        and ItemNumber between @beginItemNumber and @endItemNumber
                        and IntersectTypeID is not null
                        and ParentObject is not null 
                        and ParentObjectID is not null 
                        and Object is not null 
                        and ObjectID is not null 
                ) as S
    on          ( T.IntersectTypeID = S.IntersectTypeID and S.Object = T.Object and S.ObjectID = T.ObjectID )
    when matched then
        update 
        set     T.Subject = S.ParentObject,
                T.SubjectID = S.ParentObjectID,
                T.UpdatedBy = @R
    when not matched by target then
	    insert  (IntersectTypeID, Subject, SubjectID, Object, ObjectID, CreatedBy, UpdatedBy)
	    values  (S.IntersectTypeID, S.ParentObject, S.ParentObjectID, S.Object, S.ObjectID, @R, @R)
    output $action, inserted.[uid] into #ParentChildRelationships;

	insert into graph.AssetEdge ($from_id, $to_id, ID, Uid, IntersectTypeID, IntersectTypeUid, PredicateID, PredicateUid, PredicateType, Properties, [State], UpdatedOn)
    select  SG.$node_id,
            OG.$node_id,
            I.ID,
            I.[Uid],
            T.ID as IntersectTypeID,
            T.[Uid] as IntersectTypeUid,
            P.ID as PredicateID,
            P.Uid as PredicateUid,
            P.Type as PredicateType,
		    '<props/>' as Properties,
		    I.[State],
		    coalesce(I.UpdatedOn, I.CreatedOn, getutcdate()) as UpdatedOn
    from    [Intersect] I
            inner join #ParentChildRelationships R on R.[Uid] = I.[Uid] and R.[Operation] = 'INSERT'
            inner join Asset SA on SA.[Object] = I.[Subject] and SA.ObjectID = I.SubjectID
		    inner join graph.AssetNode SG on SG.ID = SA.ID
		    inner join Asset OA on OA.[Object] = I.[Object] and OA.ObjectID = I.ObjectID
		    inner join graph.AssetNode OG on OG.ID = OA.ID
		    inner join IntersectType T on T.ID = I.IntersectTypeID
		    inner join [Predicate] P on P.ID = T.PredicateID
    where   not exists (select 1 from graph.AssetEdge where [uid] = I.[Uid]);

select [uid] from #ParentChildRelationships",
                                            new { beginItemNumber, endItemNumber, execution.ExecutionID, R = CurrentResourceID, D = DateTime.UtcNow }, transaction: trans, commandTimeout: timeout)
                                            .ToList();
                                        }

                                        #endregion
                                        fieldTypeUpdates.Clear();
                                        fieldTypeUpdates = MergeFields(execution.ExecutionID, trans, "api.ExecutionAsset", "A.Object", "A.ObjectID", beginItemNumber, endItemNumber, sendWorkflowEvents, timeout);
                                        ImportRelationships(execution.ExecutionID, trans, "api.ExecutionAsset", "A.Object", "A.ObjectID", beginItemNumber, endItemNumber, timeout, lookupFieldsPassedByValue);

                                        if (jsonFieldTypes.Count > 0)
                                        {
                                            MergeJsonFieldProperties(execution.ExecutionID, trans, jsonFieldTypes, "api.ExecutionAsset", "A.Object", "A.ObjectID", beginItemNumber, endItemNumber, timeout, fieldJsonPropertyLoadLimitToTopLevel);
                                        }

                                        // Must execute BEFORE the Success flag is updated below.
                                        MergeAssetDisplayValues(execution.ExecutionID, trans, beginItemNumber, endItemNumber, timeout);

                                        //Delete all field without value ONLY do this if there are lookup fields AND this is an update.
                                        if (hasLookupFieldTypes && !isInsert)
                                        {
                                            DeleteEmptyAssetListFieldByApiExecutionUid(execution.ExecutionID, trans, beginItemNumber, endItemNumber, timeout);
                                        }

                                        // Update success flag.
                                        Connection.Execute(
                                            $@"update api.ExecutionAsset set Success = 1 where {executionAssetWhereSql} and Object is not null and ObjectID is not null;",
                                            new { execution.ExecutionID, beginItemNumber, endItemNumber }, transaction: trans, commandTimeout: timeout);

                                        trans.Commit();

                                        runCompleted = true;
                                    }
                                    catch (Exception ex)
                                    {
                                        trans.Rollback();

                                        retryCount++;

                                        if (retryCount > API_V2_RETRY_LIMIT)
                                        {
                                            LogLoopExecutionError(execution.ExecutionID, beginItemNumber, endItemNumber, "api.ExecutionAsset", ex.GetFullExceptionData(false), timeout);
                                        }
                                    }
                                }
                            }

                            results.AddRange(
                                Query<DatabaseBulkAssetResult>(
                                    $"select * from api.ExecutionAsset where ExecutionID = @ExecutionID and ItemNumber between @beginItemNumber and @endItemNumber",
                                    new { execution.ExecutionID, beginItemNumber, endItemNumber }
                                )
                            );

                            OnAssetsPartiallyProcessed(new AssetsPartiallyProcessedEventArgs
                            {
                                Results = results
                            });

                            beginItemNumber += loopSize;
                            endItemNumber += loopSize;
                        }

                        Connection.Close();

                        IEnumerable<IGraphAsset> graphResults = results.AsEnumerable();

                        if (parentIntersectGuids.Any())
                        {
                            graphResults = graphResults.Concat(parentIntersectGuids.Select(i => new DatabaseBulkRelationshipResult()
                            {
                                uid = i,
                                Success = true
                            }));
                        }

                        try
                        {
                            var changedFields = import.ToDictionary(k => k.Uid, v => v.Fields.Keys.ToList());
                            SendAssetGraphEvents(graphResults, changedFields);
                        }
                        catch { }


                        if (sendWorkflowEvents)
                        {
                            SendWorkflowEvents(at.Object, at.ObjectID, results, null, fieldTypeUpdates);
                        }
                    }
                }
            }

            return results;
        }

        public List<DatabaseBulkRelationshipResult> ImportRelationships(ApiExecution execution, IntersectType rt, RelationshipInserts import, int timeout = 3600, bool sendWorkflowEvents = false)
        {
            var results = new List<DatabaseBulkRelationshipResult>();
            bool generalChecksCompleted = false;
            CurrentExecutionLocationModel currentLocation = null;
            var executionItemDupes = import.Where(i => i.ExecutionItemUid.HasValue).GroupBy(i => i.ExecutionItemUid).Where(i => i.Count() > 1).Select(i => new { ExecutionItemUid = i.Key, Count = i.Count() }).ToList();
            if (executionItemDupes.Any())
            {
                execution.ErrorMessage = $"Duplicate execution item identifiers: {string.Join(", ", executionItemDupes.Select(i => i.ExecutionItemUid.ToString()))}. Identifiers must be unique within a batch.";
                results.AddRange(import.Select(i => new DatabaseBulkRelationshipResult { ExecutionItemUid = i.ExecutionItemUid, Message = execution.ErrorMessage, Success = false }));
            }
            else
            {
                try
                {
                    currentLocation = GetCurrentExecutionLocation(execution.ExecutionID, "api.ExecutionRelationship");

                    if (currentLocation.HighestItemNumberProcessed > 0)
                    {
                        results.AddRange(
                            Query<DatabaseBulkRelationshipResult>(
                                $"select * from api.ExecutionRelationship where ExecutionID = @ExecutionID and ItemNumber <= {currentLocation.HighestItemNumberProcessed}",
                                new { execution.ExecutionID }
                            )
                        );
                    }

                    #region Build data tables for bulk load.

                    var table = new DataTable();
                    table.Columns.Add("ExecutionID", typeof(Guid));
                    table.Columns.Add("ItemNumber", typeof(int));
                    table.Columns.Add("Message", typeof(string));
                    table.Columns.Add("Success", typeof(bool));
                    table.Columns.Add("SubjectUid", typeof(Guid));
                    table.Columns.Add("ObjectUid", typeof(Guid));
                    table.Columns.Add("ExecutionItemUid", typeof(Guid));

                    var fieldTable = new DataTable();
                    fieldTable.Columns.Add("ExecutionID", typeof(Guid));
                    fieldTable.Columns.Add("ItemNumber", typeof(int));
                    fieldTable.Columns.Add("FieldName", typeof(string));
                    fieldTable.Columns.Add("FieldValue", typeof(string));
                    fieldTable.Columns.Add("FieldTypeID", typeof(int));

                    #endregion

                    // Get field types.
                    var fieldTypes = Query<FieldType>("select * from FieldType where Object = 'IntersectType' and ObjectID = @ID", new { rt.ID }).ToList();
                    var requiredFieldTypeNames = fieldTypes.Where(f => f.IsRequired && string.IsNullOrEmpty(f.DefaultValue)).Select(f => f.Name).ToList();

                    #region Generate data sets

                    for (int i = 1; i <= import.Count; i++)
                    {
                        if (i > currentLocation.HighestItemNumber)
                        {

                            var model = import[i - 1];

                            bool success;
                            string errorMessage;
                            var fieldRows = ValidateFields("IntersectType", rt.ID, true, fieldTypes, requiredFieldTypeNames, model.Fields, execution.ExecutionID, i, fieldTable, out success, out errorMessage);

                            if (success)
                            {
                                fieldRows.ForEach(fr => { fieldTable.Rows.Add(fr); });

                                var row = table.NewRow();

                                row["ExecutionID"] = execution.ExecutionID;
                                row["ItemNumber"] = i;
                                row["SubjectUid"] = model.SubjectAssetUid;
                                row["ObjectUid"] = model.ObjectAssetUid;
                                if (model.ExecutionItemUid.HasValue) row["ExecutionItemUid"] = model.ExecutionItemUid.Value;
                                table.Rows.Add(row);
                            }
                            else
                            {
                                results.Add(new DatabaseBulkRelationshipResult { IntersectID = 0, ExecutionItemUid = model.ExecutionItemUid, IsNew = false, ItemNumber = i, Message = errorMessage, Success = false });
                            }
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
                        Connection.OpenWithRetry(RetryPolicy.DefaultProgressive);

                    #region Bulk Copy

                    SqlBulkCopy bulkCopy = new SqlBulkCopy(Connection);

                    bulkCopy.BatchSize = table.Rows.Count;
                    bulkCopy.DestinationTableName = "api.ExecutionRelationship";
                    bulkCopy.BulkCopyTimeout = timeout;

                    bulkCopy.ColumnMappings.Add("ExecutionID", "ExecutionID");
                    bulkCopy.ColumnMappings.Add("ItemNumber", "ItemNumber");
                    bulkCopy.ColumnMappings.Add("SubjectUid", "SubjectUid");
                    bulkCopy.ColumnMappings.Add("ObjectUid", "ObjectUid");
                    bulkCopy.ColumnMappings.Add("ExecutionItemUid", "ExecutionItemUid");

                    bulkCopy.WriteToServer(table);

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

                    bulkCopy = null;

                    #endregion

                    ResolveFieldLookupValues(execution.ExecutionID, timeout);
                    LogFieldLookupErrors(execution.ExecutionID, "IntersectType", rt.ID, "Relationship", timeout);

                    #region Validate subjects/objects

                    Connection.Execute(@"
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
		T.ObjectID = O.ObjectID,
        T.IsNew = CASE
                    WHEN I.Id is null THEN 1
                    ELSE 0
                  END
from	api.ExecutionRelationship T
		    left join AssetWithType S on S.[Type] = @st and S.TypeID = @stid and S.[uid] = T.SubjectUid
		    left join AssetWithType O on O.[Type] = @ot and O.TypeID = @otid and O.[uid] = T.ObjectUid
            left join IntersectType IT on IT.uid = @uid
            left join [Intersect] I on IT.Id = I.IntersectTypeId and I.SubjectId= S.ObjectId and I.ObjectId = O.ObjectId and I.Subject = S.Object and I.Object = O.Object
        where T.ExecutionID = @ExecutionID;

if @st = 'ReferenceItemType' and @stid = 0
begin
	update	T
	set		T.Subject = S.Object,
			T.SubjectID = S.ObjectID
	from	api.ExecutionRelationship T
			    inner join AssetType S on S.[uid] = T.SubjectUid and T.Subject is null
            where T.ExecutionID = @ExecutionID;
end

if @ot = 'ReferenceItemType' and @otid = 0 
begin
	update	T
	set		T.Object = O.Object,
			T.ObjectID = O.ObjectID
	from	api.ExecutionRelationship T
			inner join AssetType O on O.[uid] = T.ObjectUid and T.Object is null
            where T.ExecutionID = @ExecutionID;
end",
                    new { execution.ExecutionID, rt.uid }, commandTimeout: timeout);

                    #endregion

                    #region Log subject/object resolution errors

                    Connection.Execute(@"
update	api.ExecutionRelationship
set		Success = 0,
		[Message] = coalesce([Message] + '; ', '') + 'Not able to resolve subject of this relationship to a valid asset.'
where	ExecutionID = @ExecutionID and (Subject is null or SubjectID is null);
	
update	api.ExecutionRelationship
set		Success = 0,
		[Message] = coalesce([Message] + '; ', '') + 'Not able to resolve object of this relationship to a valid asset.'
where	ExecutionID = @ExecutionID and (Object is null or ObjectID is null);",
                    new { execution.ExecutionID }, commandTimeout: timeout);

                    #endregion

                    #region Cardinality Validation

                    if (rt.SubjectCardinality == Cardinality.One)
                    {
                        Connection.Execute(@"
update	T
set		T.Message = coalesce(T.Message + '; ', '') + 'Object already related to one item and cardinality is set to one.',
		T.Success = 0
from	api.ExecutionRelationship T
		inner join	(
					select	ER.ExecutionID,
							ER.ItemNumber,
							count(1) as RelationshipCount
					from	api.ExecutionRelationship ER
							inner join Asset O on O.Uid = ER.ObjectUid and ER.ExecutionID = @ExecutionID
							inner join [Intersect] I on I.IntersectTypeID = @IntersectTypeID and I.Object = O.Object and I.ObjectID = O.ObjectID
                            inner join Asset S on S.Uid <> ER.SubjectUid and S.Object = I.Subject and S.ObjectID = I.SubjectID 
					group by ER.ExecutionID, ER.ItemNumber
					) S on S.ExecutionID = T.ExecutionID and S.ItemNumber = T.ItemNumber;

update	T
set		T.Message = coalesce(T.Message + '; ', '') + 'Object already referenced in this batch and cannot be used again due to cardinality restrictions.',
		T.Success = 0
from	api.ExecutionRelationship T
		inner join	(
					select	ER.ExecutionID,
							ER.ObjectUid,
							min(ER.ItemNumber) as ItemNumber
					from	api.ExecutionRelationship ER
							inner join Asset O on O.Uid = ER.ObjectUid and ER.ExecutionID = @ExecutionID
					group by ER.ExecutionID, ER.ObjectUid
					) S on S.ExecutionID = T.ExecutionID and S.ObjectUid = T.ObjectUid and S.ItemNumber < T.ItemNumber;",
                        new { execution.ExecutionID, IntersectTypeID = rt.ID }, commandTimeout: timeout);
                    }

                    if (rt.ObjectCardinality == Cardinality.One)
                    {
                        Connection.Execute(@"
update	T
set		T.Message = coalesce(T.Message + '; ', '') + 'Subject already related to one item and cardinality is set to one.',
		T.Success = 0
from	api.ExecutionRelationship T
		inner join	(
					select	ER.ExecutionID,
							ER.ItemNumber,
							count(1) as RelationshipCount
					from	api.ExecutionRelationship ER
							inner join Asset S on S.Uid = ER.SubjectUid and ER.ExecutionID = @ExecutionID
							inner join [Intersect] I on I.IntersectTypeID = @IntersectTypeID and I.Subject = S.Object and I.SubjectID = S.ObjectID
                            inner join Asset O on O.Uid <> ER.ObjectUid and O.Object = I.Object and O.ObjectID = I.ObjectID 
					group by ER.ExecutionID, ER.ItemNumber
					) S on S.ExecutionID = T.ExecutionID and S.ItemNumber = T.ItemNumber;

update	T
set		T.Message = coalesce(T.Message + '; ', '') + 'Subject already referenced in this batch and cannot be used again due to cardinality restrictions.',
		T.Success = 0
from	api.ExecutionRelationship T
		inner join	(
					select	ER.ExecutionID,
							ER.SubjectUid,
							min(ER.ItemNumber) as ItemNumber
					from	api.ExecutionRelationship ER
							inner join Asset O on O.Uid = ER.SubjectUid and ER.ExecutionID = @ExecutionID
					group by ER.ExecutionID, ER.SubjectUid
					) S on S.ExecutionID = T.ExecutionID and S.SubjectUid = T.SubjectUid and S.ItemNumber < T.ItemNumber;",
                        new { execution.ExecutionID, IntersectTypeID = rt.ID }, commandTimeout: timeout);
                    }

                    #endregion

                    #region Permissions Validation

                    Connection.Execute(@"
declare @IsAdministrator bit = 0
select	@IsAdministrator = IsAdministrator
from	reporting.Global_Resource
where	ResourceID = @ResourceID

if @IsAdministrator = 0
begin
    update	T
    set		T.Message = coalesce(T.Message + '; ', '') + 'You do not have permission to modify relationships on the subject asset.',
	        T.Success = 0
    from	api.ExecutionRelationship T
            inner join	(
                        select	R.ExecutionID, R.ItemNumber
	                    from	api.ExecutionRelationship R
			                    inner join Asset A on A.Uid = R.SubjectUid and R.ExecutionID = @ExecutionID
			                    outer apply dbo.UserAssetPermissions(@ResourceID, A.AssetTypeID) P
	                    where	(
			                    (P.AssetID = A.ID) 
			                    or P.AssetID is null
			                    )
			                    and (
				                    (P.PermissionsBitMask is not null and P.PermissionsBitMask & 1024 <> 1024) 
				                    or 
				                    P.PermissionsBitMask is null
				                    )
                        group by R.ExecutionID, R.ItemNumber
                        ) S on S.ExecutionID = T.ExecutionID and S.ItemNumber = T.ItemNumber;

    update	T
    set		T.Message = coalesce(T.Message + '; ', '') + 'You do not have permission to modify relationships on the object asset.',
	        T.Success = 0
    from	api.ExecutionRelationship T
            inner join	(
                        select	R.ExecutionID, R.ItemNumber
	                    from	api.ExecutionRelationship R
			                    inner join Asset A on A.Uid = R.ObjectUid and R.ExecutionID = @ExecutionID
			                    outer apply dbo.UserAssetPermissions(@ResourceID, A.AssetTypeID) P
	                    where	(
			                    (P.AssetID = A.ID) 
			                    or P.AssetID is null
			                    )
			                    and (
				                    (P.PermissionsBitMask is not null and P.PermissionsBitMask & 1024 <> 1024) 
				                    or 
				                    P.PermissionsBitMask is null
				                    )
                        group by R.ExecutionID, R.ItemNumber
                        ) S on S.ExecutionID = T.ExecutionID and S.ItemNumber = T.ItemNumber;
end",
                    new { execution.ExecutionID, execution.ResourceID }, commandTimeout: timeout);

                    #endregion

                    generalChecksCompleted = true;
                }
                catch (Exception generalEx)
                {
                    generalChecksCompleted = false;
                    var msg = generalEx.GetFullExceptionData(false);
                    execution.ErrorMessage = msg;
                    execution.Processed = 0;
                    execution.Error = import.Count();

                    results = new List<DatabaseBulkRelationshipResult>();
                    results.AddRange(import.Select(i => new DatabaseBulkRelationshipResult { ExecutionItemUid = i.ExecutionItemUid, Message = msg, Success = false }));
                }

                if (generalChecksCompleted)
                {
                    int loopSize = 100;
                    int numberOfLoops = (int)Math.Ceiling((decimal)(execution.Total - currentLocation.HighestItemNumberProcessed) / loopSize);
                    int beginItemNumber = currentLocation.HighestItemNumberProcessed + 1;
                    int endItemNumber = currentLocation.HighestItemNumberProcessed + loopSize;
                    List<AssetFieldTypeUpdate> fieldTypeUpdates = new List<AssetFieldTypeUpdate>();

                    for (int currentLoop = 1; currentLoop <= numberOfLoops; currentLoop++)
                    {
                        bool runCompleted = false;
                        int retryCount = 0;

                        while (!runCompleted && retryCount <= API_V2_RETRY_LIMIT)
                        {
                            using (var trans = Connection.BeginTransaction())
                            {
                                try
                                {
                                    #region Intersect table merge
                                    Connection.Execute($@"
        drop table if exists #ObjectMergeTableResult;
        create table #ObjectMergeTableResult (ID int, ItemNumber int, [Action] nvarchar(10));
        CREATE NONCLUSTERED INDEX IX_TempObjectMergeTableResult ON #ObjectMergeTableResult ( ItemNumber ASC );

        merge into  [Intersect] T
        using		(
			        select      *
			        from        api.ExecutionRelationship
			        where		ExecutionID = @ExecutionID
                                and ItemNumber between @beginItemNumber and @endItemNumber
                                and Success is null	
                ) S
        on      ( T.IntersectTypeID = @rtID and T.Subject = S.Subject and T.SubjectID = S.SubjectID and T.Object = S.Object and T.ObjectID = S.ObjectID )
        when matched then
	        update set
			        T.UpdatedBy = @CurrentResourceID,
			        T.UpdatedOn = getutcdate()
        when not matched by target then
	        insert  (IntersectTypeID, Subject, SubjectID, Object, ObjectID, [State], CreatedBy, CreatedOn, UpdatedBy, UpdatedOn, [Owner])
	        values  (@rtID, S.Subject, S.SubjectID, S.Object, S.ObjectID, 1, @CurrentResourceID, getutcdate(), @CurrentResourceID, getutcdate(), 'BULK_API')
        output inserted.ID, S.ItemNumber, $action into #ObjectMergeTableResult;

        update	T
        set		T.IntersectID = S.ID,
                T.uid = IT.uid
        from	api.ExecutionRelationship T
		        inner join #ObjectMergeTableResult S on T.ExecutionID = @ExecutionID and S.ItemNumber = T.ItemNumber
                inner join [Intersect] IT on IT.ID = S.ID
        where   T.ItemNumber between @beginItemNumber and @endItemNumber;", new { execution.ExecutionID, beginItemNumber, endItemNumber, CurrentResourceID, rtID = rt.ID }, transaction: trans, commandTimeout: timeout);

                                    #endregion
                                    fieldTypeUpdates.Clear();
                                    fieldTypeUpdates = MergeFields(execution.ExecutionID, trans, "api.ExecutionRelationship", "'Intersect' as [Object]", "A.IntersectID as ObjectID", beginItemNumber, endItemNumber, sendWorkflowEvents, timeout);

                                    // Update success flag
                                    Connection.Execute(
                                        $"update api.ExecutionRelationship set Success = 1 where Success is null and ExecutionID = @ExecutionID and ItemNumber between @beginItemNumber and @endItemNumber and IntersectID is not null;",
                                        new { execution.ExecutionID, beginItemNumber, endItemNumber }, transaction: trans, commandTimeout: timeout);

                                    trans.Commit();

                                    runCompleted = true;
                                }
                                catch (Exception ex)
                                {
                                    trans.Rollback();

                                    retryCount++;

                                    if (retryCount > API_V2_RETRY_LIMIT)
                                    {
                                        LogLoopExecutionError(execution.ExecutionID, beginItemNumber, endItemNumber, "api.ExecutionRelationship", ex.GetFullExceptionData(false), timeout);
                                    }
                                    else
                                    {
                                        Thread.Sleep(API_V2_RETRY_INTERVAL);
                                    }
                                }
                            }
                        }

                        results.AddRange(
                            Query<DatabaseBulkRelationshipResult>(
                                $"select * from api.ExecutionRelationship where ExecutionID = @ExecutionID and ItemNumber between @beginItemNumber and @endItemNumber",
                                new { execution.ExecutionID, beginItemNumber, endItemNumber }
                            )
                        );

                        OnRelationshipsPartiallyProcessed(new RelationshipsPartiallyProcessedEventArgs
                        {
                            Results = results
                        });

                        beginItemNumber += loopSize;
                        endItemNumber += loopSize;
                    }

                    Connection.Close();

                    SendAssetGraphEvents(results);

                    if (sendWorkflowEvents)
                        SendWorkflowEvents("IntersectType", rt.ID, results, null, fieldTypeUpdates);
                }
            }
            return results;
        }

        public List<DatabaseBulkRelationshipResult> DeleteRelationships(ApiExecution execution, IntersectType it, RelationshipDeletes import, int timeout = 3600, bool sendWorkflowEvents = false)
        {
            var results = new List<DatabaseBulkRelationshipResult>();
            bool generalChecksCompleted = false;
            CurrentExecutionLocationModel currentLocation = null;

            try
            {
                currentLocation = GetCurrentExecutionLocation(execution.ExecutionID, "api.ExecutionDeletedRelationship");

                if (currentLocation.HighestItemNumberProcessed > 0)
                {
                    results.AddRange(
                        Query<DatabaseBulkRelationshipResult>(
                            $"select * from api.ExecutionDeletedRelationship where ExecutionID = @ExecutionID and ItemNumber <= {currentLocation.HighestItemNumberProcessed}",
                            new { execution.ExecutionID }
                        )
                    );
                }

                #region Build data tables for bulk load.

                var table = new DataTable();
                table.Columns.Add("ExecutionID", typeof(Guid));
                table.Columns.Add("ItemNumber", typeof(int));
                table.Columns.Add("ExecutionItemUid", typeof(Guid));
                table.Columns.Add("Uid", typeof(Guid));
                table.Columns.Add("Cascade", typeof(bool));

                #endregion

                #region Generate data sets

                for (int i = currentLocation.HighestItemNumber + 1; i <= import.Count; i++)
                {
                    var model = import[i - 1];

                    var row = table.NewRow();

                    row["ExecutionID"] = execution.ExecutionID;
                    row["ItemNumber"] = i;
                    row["Uid"] = model.Uid;
                    row["Cascade"] = model.Cascade;
                    if (model.ExecutionItemUid.HasValue) row["ExecutionItemUid"] = model.ExecutionItemUid.Value;
                    table.Rows.Add(row);
                }

                #endregion

                if (Database.Connection.State != ConnectionState.Open)
                    Connection.OpenWithRetry(RetryPolicy.DefaultProgressive);

                #region Bulk Copy

                SqlBulkCopy bulkCopy = new SqlBulkCopy(Connection);

                bulkCopy.BatchSize = table.Rows.Count;
                bulkCopy.DestinationTableName = "api.ExecutionDeletedRelationship";
                bulkCopy.BulkCopyTimeout = timeout;

                bulkCopy.ColumnMappings.Add("ExecutionID", "ExecutionID");
                bulkCopy.ColumnMappings.Add("ExecutionItemUid", "ExecutionItemUid");
                bulkCopy.ColumnMappings.Add("ItemNumber", "ItemNumber");
                bulkCopy.ColumnMappings.Add("Uid", "Uid");
                bulkCopy.ColumnMappings.Add("Cascade", "Cascade");

                bulkCopy.WriteToServer(table);

                bulkCopy = null;

                #endregion

                #region Validate Intersect Uid / Intersect Type Uid

                Connection.Execute(@"
update	T
set		T.IntersectID = I.ID,
        T.Success = case 
                        when I.ID is null then 0
                        else null
                    end,
        T.Message = case 
                        when IT.ID is null then coalesce(T.[Message] + '; ', '') + 'No relationship type with the specified Uid found.'
                        when I.ID is null then coalesce(T.[Message] + '; ', '') + 'No relationship with the specified Uid found.'
                        else T.Message
                    end
from	api.ExecutionDeletedRelationship T
        left join IntersectType IT on IT.uid = @uid
        left join [Intersect] I on I.IntersectTypeId = IT.Id and I.Uid = T.Uid
where   T.ExecutionID = @ExecutionID;",
                new { execution.ExecutionID, it.uid }, commandTimeout: timeout);

                #endregion

                #region Permissions Validation

                Connection.Execute(@"
declare @IsAdministrator bit = 0
select	@IsAdministrator = IsAdministrator
from	reporting.Global_Resource
where	ResourceID = @ResourceID

if @IsAdministrator = 0
begin
update	T
set		T.Message = coalesce(T.Message + '; ', '') + 'You do not have permission to remove relationships on the subject asset.',
	    T.Success = 0
from	api.ExecutionDeletedRelationship T
        left join	(
                    select	R.ExecutionID, R.ItemNumber
	                from	api.ExecutionDeletedRelationship R 
                            inner join [Intersect] I on I.ID = R.IntersectID and R.ExecutionID = @ExecutionID 
			                inner join Asset A on A.Object = I.Subject and A.ObjectID = I.SubjectID
			                outer apply dbo.UserAssetPermissions(@ResourceID, A.AssetTypeID) P
	                where	R.FromHierarchy = 0
                            and P.AssetTypeID = A.AssetTypeID
                            and ( P.AssetID = A.ID or P.AssetID = 0 )
			                and (
				                (P.PermissionsBitMask is not null and P.PermissionsBitMask & 2048 = 2048) 
				                or 
				                P.PermissionsBitMask is null
				                )
                    group by R.ExecutionID, R.ItemNumber
                    ) S on S.ExecutionID = T.ExecutionID and S.ItemNumber = T.ItemNumber 
where	T.ExecutionID = @ExecutionID 
		and S.ItemNumber is null;

update	T
set		T.Message = coalesce(T.Message + '; ', '') + 'You do not have permission to remove relationships on the object asset.',
	    T.Success = 0
from	api.ExecutionDeletedRelationship T
        left join	(
                    select	R.ExecutionID, R.ItemNumber
	                from	api.ExecutionDeletedRelationship R
			                inner join [Intersect] I on I.ID = R.IntersectID and R.ExecutionID = @ExecutionID
                            inner join Asset A on A.Object = I.Object and A.ObjectID = I.ObjectID
			                outer apply dbo.UserAssetPermissions(@ResourceID, A.AssetTypeID) P
	                where	R.FromHierarchy = 0
                            and P.AssetTypeID = A.AssetTypeID
                            and ( P.AssetID = A.ID or P.AssetID = 0 )
			                and (
				                (P.PermissionsBitMask is not null and P.PermissionsBitMask & 2048 = 2048) 
				                or 
				                P.PermissionsBitMask is null
				                )
                    group by R.ExecutionID, R.ItemNumber
                    ) S on S.ExecutionID = T.ExecutionID and S.ItemNumber = T.ItemNumber 
where	T.ExecutionID = @ExecutionID 
		and S.ItemNumber is null;
end",
                new { execution.ExecutionID, execution.ResourceID }, commandTimeout: timeout);

                #endregion

                #region Cascade Validation

                Connection.Execute(@"
update	T
set		T.Message = coalesce(T.Message + '; ', '') + 'You have not enabled Cascade on this relationship, and there are ' + cast(S.[Count] as nvarchar) + ' child relationship(s) associated with it.',
	    T.Success = 0
from	api.ExecutionDeletedRelationship T
        inner join	(
                    select  S.ExecutionID,
                            S.ItemNumber,
                            count(1) as [Count]
                    from    api.ExecutionDeletedRelationship S
                            inner join [Intersect] T on T.Subject = 'Intersect' and T.SubjectID = S.IntersectID and S.ExecutionID = @ExecutionID and S.Success is null
                    where   S.[Cascade] = 0
                    group by S.ExecutionID, S.ItemNumber
                    ) S on S.ExecutionID = T.ExecutionID and S.ItemNumber = T.ItemNumber;",
                new { execution.ExecutionID }, commandTimeout: timeout);

                #endregion

                #region Finally, load any child intersects

                Connection.Execute(@"
insert into api.ExecutionDeletedRelationship (ExecutionID, ItemNumber, [Uid], IntersectID, FromHierarchy, HierarchyIntersectID)
    select  S.ExecutionID,
            S.ItemNumber,
            T.[Uid],
            T.ID,
            1 as FromHierarchy,
            S.IntersectID as HierarchyIntersectID
    from    api.ExecutionDeletedRelationship S
            inner join [Intersect] T on T.Subject = 'Intersect' and T.SubjectID = S.IntersectID and S.ExecutionID = @ExecutionID and S.Success is null;",
                new { execution.ExecutionID }, commandTimeout: timeout);

                #endregion

                generalChecksCompleted = true;
            }
            catch (Exception generalEx)
            {
                generalChecksCompleted = false;
                var msg = generalEx.GetFullExceptionData(false);
                execution.ErrorMessage = msg;
                execution.Processed = 0;
                execution.Error = import.Count();

                results = new List<DatabaseBulkRelationshipResult>();
                results.AddRange(import.Select(i => new DatabaseBulkRelationshipResult { ExecutionItemUid = i.ExecutionItemUid, Message = msg, Success = false }));
            }

            if (generalChecksCompleted)
            {
                int loopSize = 100;
                int numberOfLoops = (int)Math.Ceiling((decimal)(execution.Total - currentLocation.HighestItemNumberProcessed) / loopSize);
                int beginItemNumber = currentLocation.HighestItemNumberProcessed + 1;
                int endItemNumber = currentLocation.HighestItemNumberProcessed + loopSize;

                for (int currentLoop = 1; currentLoop <= numberOfLoops; currentLoop++)
                {
                    bool runCompleted = false;
                    int retryCount = 0;

                    while (!runCompleted && retryCount <= API_V2_RETRY_LIMIT)
                    {
                        using (var trans = Connection.BeginTransaction())
                        {
                            try
                            {
                                #region Field table delete

                                Connection.Execute($@"
delete  T
from    [Field] T
        inner join api.ExecutionDeletedRelationship S on T.ObjectType = 'Intersect' 
            and S.IntersectID = T.ObjectID 
            and S.ExecutionID = @ExecutionID 
            and S.ItemNumber between @beginItemNumber and @endItemNumber
            and S.Success is null;",
            new { execution.ExecutionID, beginItemNumber, endItemNumber }, transaction: trans, commandTimeout: timeout);

                                #endregion

                                #region Intersect table delete

                                Connection.Execute($@"
delete  T
from    [Intersect] T
        inner join api.ExecutionDeletedRelationship S on S.IntersectID = T.ID 
            and S.ExecutionID = @ExecutionID 
            and S.ItemNumber between @beginItemNumber and @endItemNumber
            and S.Success is null;",
            new { execution.ExecutionID, beginItemNumber, endItemNumber }, transaction: trans, commandTimeout: timeout);

                                #endregion

                                // Update success flag
                                Connection.Execute(
                                    $"update api.ExecutionDeletedRelationship set Success = 1 where Success is null and ExecutionID = @ExecutionID and ItemNumber between @beginItemNumber and @endItemNumber and IntersectID is not null;",
                                    new { execution.ExecutionID, beginItemNumber, endItemNumber }, transaction: trans, commandTimeout: timeout);

                                trans.Commit();

                                runCompleted = true;
                            }
                            catch (Exception ex)
                            {
                                trans.Rollback();

                                retryCount++;

                                if (retryCount > API_V2_RETRY_LIMIT)
                                {
                                    LogLoopExecutionError(execution.ExecutionID, beginItemNumber, endItemNumber, "api.ExecutionRelationship", ex.GetFullExceptionData(false), timeout);
                                }
                            }
                        }
                    }

                    results.AddRange(
                        Query<DatabaseBulkRelationshipResult>(
                            $"select * from api.ExecutionDeletedRelationship where ExecutionID = @ExecutionID and ItemNumber between @beginItemNumber and @endItemNumber",
                            new { execution.ExecutionID, beginItemNumber, endItemNumber }
                        )
                    );

                    OnRelationshipsPartiallyProcessed(new RelationshipsPartiallyProcessedEventArgs
                    {
                        Results = results
                    });

                    beginItemNumber += loopSize;
                    endItemNumber += loopSize;
                }

                Connection.Close();

                SendAssetGraphEvents(results);

                if (sendWorkflowEvents)
                    SendWorkflowEvents("IntersectType", it.ID, results);
            }

            return results;
        }

        private void ValidateAssetCrossReference(ApiExecution execution, int timeout = 3600)
        {
            Connection.Execute(@"Update api.ExecutionAssetCrossReference
                                    Set Success=0,
                                    Message='Does not contain required fields.' 
                                    Where ExecutionID = @executionID and 
                                    (Uid is null or DataSource is null or [Type] is null or ExternalID is null
                                   or UID ='00000000-0000-0000-0000-000000000000' or Trim(DataSource) ='' or TRIM([Type]) = '' or TRIM(ExternalID) ='') ", new { executionID = execution.ExecutionID }, commandTimeout: timeout);

            Connection.Execute(@"
                        Update  ECR
                        SET Success=0,
                        Message='Asset cross reference already exists'
                        from api.ExecutionAssetCrossReference ECR
                        Where ECR.ExecutionID = @executionID and exists (Select 1 from AssetCrossReference where UID=ECR.UID and DataSource= ECR.DataSource and
                        [Type]=ECR.[Type] and ExternalID =ECR.ExternalID)",
                        new { executionID = execution.ExecutionID }, commandTimeout: timeout);

            Connection.Execute(@"
                        Update ECR
                            Set Success=0,
                            Message ='Duplicate asset cross reference;'
                            From api.ExecutionAssetCrossReference ECR
                            inner join 
                            (Select Uid,DataSource,Type,ExternalID from api.ExecutionAssetCrossReference
                            where Success is null and ExecutionID=@executionID
                            group by Uid,DataSource,Type,ExternalID
                            having(count(*)>1)) T on
                            ECR.[Uid] = T.[UID] and
                            ECR.DataSource = T.DataSource and
                            ECR.[Type] = T.[Type] and
                            ECR.ExternalID = T.ExternalID
                            Where ECR.Success is null  and ExecutionID=@executionID ",
                        new { executionID = execution.ExecutionID }, commandTimeout: timeout);
        }

        public List<AssetCrossReferenceResult> ImportCrossReferences(ApiExecution execution, IEnumerable<AssetCrossReference> import, int timeout = 3600)
        {

            List<AssetCrossReferenceResult> bulkResult = new List<AssetCrossReferenceResult>();
            #region Build data tables for bulk load

            var table = new DataTable();
            table.Columns.Add("ExecutionID", typeof(Guid));
            table.Columns.Add("ItemNumber", typeof(int));
            table.Columns.Add("Uid", typeof(Guid));
            table.Columns.Add("DataSource", typeof(string));
            table.Columns.Add("Type", typeof(string));
            table.Columns.Add("ExternalID", typeof(string));
            table.Columns.Add("FieldHash", typeof(string));
            table.Columns.Add("Message", typeof(string));
            table.Columns.Add("Success", typeof(bool));



            int i = 0;
            foreach (var item in import)
            {
                var row = table.NewRow();

                row["ExecutionID"] = execution.ExecutionID;
                row["ItemNumber"] = i++;
                row["uid"] = item.uid;
                row["DataSource"] = item.DataSource;
                row["Type"] = item.Type;
                row["ExternalID"] = item.ExternalID;
                row["FieldHash"] = item.FieldHash;

                table.Rows.Add(row);
            }

            #endregion
            try
            {


                if (Database.Connection.State != ConnectionState.Open)
                    Connection.OpenWithRetry(RetryPolicy.DefaultProgressive);

                #region Bulk Copy
                var bulkCopy = new SqlBulkCopy(Connection)
                {
                    BatchSize = table.Rows.Count,
                    DestinationTableName = "api.ExecutionAssetCrossReference",
                    BulkCopyTimeout = timeout
                };

                bulkCopy.ColumnMappings.Add("ExecutionID", "ExecutionID");
                bulkCopy.ColumnMappings.Add("ItemNumber", "ItemNumber");
                bulkCopy.ColumnMappings.Add("uid", "uid");
                bulkCopy.ColumnMappings.Add("DataSource", "DataSource");
                bulkCopy.ColumnMappings.Add("Type", "Type");
                bulkCopy.ColumnMappings.Add("ExternalID", "ExternalID");
                bulkCopy.ColumnMappings.Add("FieldHash", "FieldHash");


                bulkCopy.WriteToServer(table);

                bulkCopy = null;

                #endregion

                this.ValidateAssetCrossReference(execution, timeout);

                Connection.Execute(@"
                            insert into AssetCrossReference
                            (Uid,DataSource,Type,ExternalID,FieldHash)
                            Select Uid,DataSource,Type,ExternalID,FieldHash from api.ExecutionAssetCrossReference
                            Where ExecutionID=@executionID and Success is null;

                            Update api.ExecutionAssetCrossReference
                            Set Success =1,
                            Message ='Added Successfully'
                            Where ExecutionID=@executionID and Success is null; ",
                    new { executionID = execution.ExecutionID }, commandTimeout: timeout);

                bulkResult = Query<AssetCrossReferenceResult>(
                                        $"select ItemNumber,Uid,Message,Success from api.ExecutionAssetCrossReference where ExecutionID = @ExecutionID",
                                        new { ExecutionID = execution.ExecutionID }).ToList();


            }
            finally
            {
                if (Database.Connection.State == ConnectionState.Open)
                    Connection.Close();
            }
            return bulkResult;
        }

        public List<PredicateDeleteResult> RemovePredicates(ApiExecution execution, PredicateDeletes import, int timeout = 3600)
        {
            var results = new List<PredicateDeleteResult>();
            bool generalChecksCompleted = false;
            CurrentExecutionLocationModel currentLocation = null;

            var executionItemDupes = import.Where(i => i.ExecutionItemUid.HasValue).GroupBy(i => i.ExecutionItemUid).Where(i => i.Count() > 1).Select(i => new { ExecutionItemUid = i.Key, Count = i.Count() }).ToList();
            if (executionItemDupes.Any())
            {
                execution.ErrorMessage = $"Duplicate execution item identifiers: {string.Join(", ", executionItemDupes.Select(i => i.ExecutionItemUid.ToString()))}. Identifiers must be unique within a batch.";
                results.AddRange(import.Select(i => new PredicateDeleteResult { ExecutionItemUid = i.ExecutionItemUid, Uid = i.Uid, Message = execution.ErrorMessage, Success = false }));
            }
            else
            {
                var uidDupes = import.GroupBy(i => i.Uid).Where(i => i.Count() > 1).Select(i => new { Uid = i.Key, Count = i.Count() }).ToList();
                if (uidDupes.Any())
                {
                    execution.ErrorMessage = $"Duplicate predicate Uids: {string.Join(", ", uidDupes.Select(i => i.Uid.ToString()))}. Identifiers must be unique within a batch.";
                    results.AddRange(import.Select(i => new PredicateDeleteResult { ExecutionItemUid = i.ExecutionItemUid, Uid = i.Uid, Message = execution.ErrorMessage, Success = false }));
                }
                else
                {
                    try
                    {
                        currentLocation = GetCurrentExecutionLocation(execution.ExecutionID, "api.ExecutionDeletedPredicate");

                        if (currentLocation.HighestItemNumberProcessed > 0)
                        {
                            results.AddRange(
                                Query<PredicateDeleteResult>(
                                    $"select * from api.ExecutionDeletedPredicate where ExecutionID = @ExecutionID and ItemNumber <= {currentLocation.HighestItemNumberProcessed}",
                                    new { execution.ExecutionID }
                                )
                            );
                        }

                        #region Build data tables.

                        var table = new DataTable();
                        table.Columns.Add("ExecutionID", typeof(Guid));
                        table.Columns.Add("ItemNumber", typeof(int));
                        table.Columns.Add("ExecutionItemUid", typeof(Guid));
                        table.Columns.Add("Uid", typeof(Guid));
                        table.Columns.Add("PredicateID", typeof(long));
                        table.Columns.Add("Message", typeof(string));
                        table.Columns.Add("Success", typeof(bool));

                        #endregion

                        #region Generate data sets

                        for (int i = 1; i <= import.Count; i++)
                        {
                            if (i > currentLocation.HighestItemNumber)
                            {
                                var model = import[i - 1];

                                var row = table.NewRow();

                                row["ExecutionID"] = execution.ExecutionID;
                                row["ItemNumber"] = i;
                                if (model.ExecutionItemUid.HasValue) row["ExecutionItemUid"] = model.ExecutionItemUid.Value;
                                else row["ExecutionItemUid"] = Guid.NewGuid();
                                row["Uid"] = model.Uid;

                                table.Rows.Add(row);
                            }
                        }

                        #endregion

                        if (Database.Connection.State != ConnectionState.Open)
                            Connection.OpenWithRetry(RetryPolicy.DefaultProgressive);

                        #region Bulk Copy

                        SqlBulkCopy bulkCopy = new SqlBulkCopy(Connection);

                        bulkCopy.BatchSize = table.Rows.Count;
                        bulkCopy.DestinationTableName = "api.ExecutionDeletedPredicate";
                        bulkCopy.BulkCopyTimeout = timeout;

                        bulkCopy.ColumnMappings.Add("ExecutionID", "ExecutionID");
                        bulkCopy.ColumnMappings.Add("ItemNumber", "ItemNumber");
                        bulkCopy.ColumnMappings.Add("ExecutionItemUid", "ExecutionItemUid");
                        bulkCopy.ColumnMappings.Add("Uid", "Uid");

                        bulkCopy.WriteToServer(table);

                        bulkCopy = null;

                        #endregion

                        #region Resolve predicates based on UIDs

                        Connection.Execute(@"
    update	T
    set		T.PredicateID = P.ID
    from	api.ExecutionDeletedPredicate T
		    inner join Predicate P on P.Uid = T.Uid and T.ExecutionID = @ExecutionID;",
                    new { execution.ExecutionID }, commandTimeout: timeout);

                        #endregion

                        #region Log lookup errors

                        Connection.Execute($@"
    update	api.ExecutionDeletedPredicate
    set		Success = 0,
		    [Message] = coalesce([Message] + '; ', '') + 'You must provide a valid Uid for this predicate'
    where	ExecutionID = @ExecutionID and ([Uid] is null or [Uid] = CAST(CAST(0 AS BINARY) AS UNIQUEIDENTIFIER)); 

    update	api.ExecutionDeletedPredicate
    set		Success = 0,
		    [Message] = coalesce([Message] + '; ', '') + 'Not found based on Uid provided'
    where	ExecutionID = @ExecutionID and PredicateID is null;

    update T
    set T.Success = 0, [Message] = coalesce([Message] + '; ', '') + 'This predicate is currently in use and may not be removed.'
    from	api.ExecutionDeletedPredicate T
    cross apply (select * from IntersectType where PredicateId = T.PredicateId)Usage
",
                        new { execution.ExecutionID }, commandTimeout: timeout);

                        #endregion

                        generalChecksCompleted = true;
                    }
                    catch (Exception generalEx)
                    {
                        generalChecksCompleted = false;
                        var msg = generalEx.GetFullExceptionData(false);
                        execution.ErrorMessage = msg;
                        execution.Processed = 0;
                        execution.Error = import.Count();

                        results = new List<PredicateDeleteResult>();
                        results.AddRange(import.Select(i => new PredicateDeleteResult { ExecutionItemUid = i.ExecutionItemUid, Message = msg, Success = false }));
                    }

                    if (generalChecksCompleted)
                    {
                        int loopSize = 250;
                        int numberOfLoops = (int)Math.Ceiling((decimal)(execution.Total - currentLocation.HighestItemNumberProcessed) / loopSize);
                        int beginItemNumber = currentLocation.HighestItemNumberProcessed + 1;
                        int endItemNumber = currentLocation.HighestItemNumberProcessed + loopSize;

                        for (int currentLoop = 1; currentLoop <= numberOfLoops; currentLoop++)
                        {
                            bool runCompleted = false;
                            int retryCount = 0;

                            while (!runCompleted && retryCount <= API_V2_RETRY_LIMIT)
                            {
                                var querySuffix = $"P.Success is null and P.ExecutionID = @ExecutionID and P.ItemNumber between @beginItemNumber and @endItemNumber";
                                using (var trans = Connection.BeginTransaction())
                                {
                                    try
                                    {
                                        Connection.Execute(
                                            $"delete Predicate where Uid in (select P.Uid from api.ExecutionDeletedPredicate P where {querySuffix})",
                                            new { execution.ExecutionID, beginItemNumber, endItemNumber }, transaction: trans, commandTimeout: timeout);

                                        Connection.Execute(
                                            $"update P set P.Success = 1 from api.ExecutionDeletedPredicate P where	{querySuffix} and P.PredicateID is not null;",
                                            new { execution.ExecutionID, beginItemNumber, endItemNumber }, transaction: trans, commandTimeout: timeout);

                                        trans.Commit();
                                        runCompleted = true;

                                    }
                                    catch (Exception ex)
                                    {
                                        trans.Rollback();

                                        retryCount++;

                                        if (retryCount > API_V2_RETRY_LIMIT)
                                        {
                                            LogLoopExecutionError(execution.ExecutionID, beginItemNumber, endItemNumber, "api.ExecutionDeletedPredicate", ex.GetFullExceptionData(false), timeout);
                                        }
                                    }
                                }
                            }

                            results.AddRange(
                                Query<PredicateDeleteResult>(
                                    $"select * from api.ExecutionDeletedPredicate where ExecutionID = @ExecutionID and ItemNumber between @beginItemNumber and @endItemNumber",
                                    new { execution.ExecutionID, beginItemNumber, endItemNumber }
                                )
                            );


                            beginItemNumber += loopSize;
                            endItemNumber += loopSize;
                        }

                        Connection.Close();

                    }
                }
            }

            return results;
        }
        public List<PredicateUpsertResult> UpdatePredicates(ApiExecution execution, PredicateUpserts import, int timeout = 3600)
        {
            var results = new List<PredicateUpsertResult>();
            bool generalChecksCompleted = false;
            CurrentExecutionLocationModel currentLocation = null;

            var executionItemDupes = import.Where(i => i.ExecutionItemUid.HasValue).GroupBy(i => i.ExecutionItemUid).Where(i => i.Count() > 1).Select(i => new { ExecutionItemUid = i.Key, Count = i.Count() }).ToList();
            var predDupes = import.GroupBy(x => x.Name + x.Type).Where(x => x.Count() > 1).Select(x => new { x.Key, Items = x.ToList() }).ToList();

            if (executionItemDupes.Any())
            {
                execution.ErrorMessage = $"Duplicate execution item identifiers: {string.Join(", ", executionItemDupes.Select(i => i.ExecutionItemUid.ToString()))}. Identifiers must be unique within a batch.";
                results.AddRange(import.Select(i => new PredicateUpsertResult { ExecutionItemUid = i.ExecutionItemUid, Message = execution.ErrorMessage, Success = false }));
            }
            else if (predDupes.Any())
            {
                execution.ErrorMessage = $"Duplicate predicate items: {string.Join(", ", predDupes.Select(i => i.Items.First().Name + "|" + i.Items.First().Type.ToString()))}. Name and type must be unique within a batch.";
                results.AddRange(import.Select(i => new PredicateUpsertResult { ExecutionItemUid = i.ExecutionItemUid, Message = execution.ErrorMessage, Success = false }));
            }
            else
            {
                try
                {
                    currentLocation = GetCurrentExecutionLocation(execution.ExecutionID, "api.ExecutionPredicate");

                    if (currentLocation.HighestItemNumberProcessed > 0)
                    {
                        results.AddRange(
                            Query<PredicateUpsertResult>(
                                $"select * from api.ExecutionPredicate where ExecutionID = @ExecutionID and ItemNumber <= {currentLocation.HighestItemNumberProcessed}",
                                new { execution.ExecutionID }
                            )
                        );
                    }

                    #region Build data tables.

                    var table = new DataTable();
                    table.Columns.Add("ExecutionID", typeof(Guid));
                    table.Columns.Add("ItemNumber", typeof(int));
                    table.Columns.Add("PredicateID", typeof(long));
                    table.Columns.Add("uid", typeof(Guid));
                    table.Columns.Add("Type", typeof(string));
                    table.Columns.Add("Name", typeof(string));
                    table.Columns.Add("Inverse", typeof(string));
                    table.Columns.Add("Message", typeof(string));
                    table.Columns.Add("Success", typeof(bool));
                    table.Columns.Add("ExecutionItemUid", typeof(Guid));

                    #endregion

                    #region Generate data sets

                    for (int i = 1; i <= import.Count; i++)
                    {
                        if (i > currentLocation.HighestItemNumber)
                        {
                            var model = import[i - 1];

                            var row = table.NewRow();

                            row["ExecutionID"] = execution.ExecutionID;
                            row["ItemNumber"] = i;
                            if (model.ExecutionItemUid.HasValue) row["ExecutionItemUid"] = model.ExecutionItemUid.Value;
                            else row["ExecutionItemUid"] = Guid.NewGuid();
                            row["Type"] = (int)model.Type;
                            row["Name"] = model.Name;
                            row["Inverse"] = model.Inverse;
                            if (model.Uid.HasValue)
                                row["uid"] = model.Uid;

                            table.Rows.Add(row);
                        }
                    }

                    #endregion

                    if (Database.Connection.State != ConnectionState.Open)
                        Connection.OpenWithRetry(RetryPolicy.DefaultProgressive);

                    #region Bulk Copy

                    SqlBulkCopy bulkCopy = new SqlBulkCopy(Connection);

                    bulkCopy.BatchSize = table.Rows.Count;
                    bulkCopy.DestinationTableName = "api.ExecutionPredicate";
                    bulkCopy.BulkCopyTimeout = timeout;

                    bulkCopy.ColumnMappings.Add("ExecutionID", "ExecutionID");
                    bulkCopy.ColumnMappings.Add("ItemNumber", "ItemNumber");
                    bulkCopy.ColumnMappings.Add("ExecutionItemUid", "ExecutionItemUid");
                    bulkCopy.ColumnMappings.Add("Type", "Type");
                    bulkCopy.ColumnMappings.Add("Name", "Name");
                    bulkCopy.ColumnMappings.Add("Inverse", "Inverse");
                    bulkCopy.ColumnMappings.Add("uid", "uid");

                    bulkCopy.WriteToServer(table);

                    bulkCopy = null;

                    #endregion


                    #region Log data errors
                    var allowedPredicates = new List<PredicateType>() { PredicateType.DataLineage, PredicateType.Grammar, PredicateType.SeeAlso, PredicateType.Simple, PredicateType.Usage };
                    List<PredicateType> systemReserved = new List<PredicateType>() { PredicateType.InterTypeHierarchy, PredicateType.IntraTypeHierarchy, PredicateType.ObjectOwnerhip };

                    foreach (PredicateType pred in (PredicateType[])Enum.GetValues(typeof(PredicateType)))
                    {
                        if(pred.IsSystemReserved() && !systemReserved.Contains(pred))
                        {
                            systemReserved.Add(pred);
                        }
                    }

                    var allowedTypesInt = allowedPredicates.Select(x => (int)x).ToList();
                    string checkTypeSQL = $"Type not in ({string.Join(",", allowedTypesInt)})";

                    var systemReservedInt = systemReserved.Select(x=> (int)x).ToList();
                    string systemReservedSQL = $"Type in ({string.Join(",", systemReservedInt)})";

                    var lineageVersion = Community.GetCompanySettingByKey<int>("LineageVersion");
                    List<int> notAllowedTypesForLineage = new List<int>() { -1 };

                    foreach (var item in import)
                    {
                        if ((int)item.Type != 0 && !item.Type.AsInfoModel().LineageVersionsSupported.Contains(lineageVersion))
                        {
                            notAllowedTypesForLineage.Add((int)item.Type);
                        }
                    }

                    string checkLineageTypes = $"Type in ({string.Join(",", notAllowedTypesForLineage)})";

                    var checkSQL = $@"
    update	api.ExecutionPredicate 
    set		Success = 0,
		    [Message] = coalesce([Message] + '; ', '') + 'Predicate with same Name and Type already exists'
    from api.ExecutionPredicate EP
    inner join [Predicate] P on P.Name = EP.Name and P.Type = EP.Type
    where	ExecutionID = @ExecutionID and EP.uid is null

    update	api.ExecutionPredicate 
    set		Success = 0,
		    [Message] = coalesce([Message] + '; ', '') + 'Predicate with same Name and Type already exists'
    from api.ExecutionPredicate EP
    inner join [Predicate] P on P.Name = EP.Name and P.Type = EP.Type and P.uid != EP.uid
    where	ExecutionID = @ExecutionID and EP.uid is not null

    update	api.ExecutionPredicate
    set		Success = 0,
		    [Message] = coalesce([Message] + '; ', '') + 'Predicate with this uid does not exists'
    from api.ExecutionPredicate EP
    left join [Predicate] P on P.Uid = Ep.uid
    where	ExecutionID = @ExecutionID and EP.uid is not null and P.uid is null;

    update api.ExecutionPredicate 
    set     Success = 0, 
            [Message] = coalesce([Message] + '; ', '') + 'You may not change the type for this predicate as it is already in use.' 
    from api.ExecutionPredicate EP 
    inner join [Predicate] P on P.[Uid] = Ep.[Uid] 
    where ExecutionID = @ExecutionID and P.Type <> EP.Type and exists (select 1 from IntersectType T inner join [Intersect] I on I.IntersectTypeID = T.ID and T.PredicateID = P.ID)

    update	api.ExecutionPredicate
    set		Success = 0,
		    [Message] = coalesce([Message] + '; ', '') + 'Name field cannot be empty'
    where	ExecutionID = @ExecutionID and (Name is null or TRIM(Name) = '');

    update	api.ExecutionPredicate
    set		Success = 0,
		    [Message] = coalesce([Message] + '; ', '') + 'Inverse field cannot be empty'
    where	ExecutionID = @ExecutionID and (Inverse is null or TRIM(Inverse) = '');

    update	api.ExecutionPredicate
    set		Success = 0,
		    [Message] = coalesce([Message] + '; ', '') + 'Predicate Type invalid. Allowed values are {string.Join(", ", allowedPredicates)}'
    where	ExecutionID = @ExecutionID and {checkTypeSQL.Replace("''", "'")}

    update	api.ExecutionPredicate
    set		Success = 0,
		    [Message] = coalesce([Message] + '; ', '') + 'Your current version of lineage does not support using this predicates of this type.'
    where	ExecutionID = @ExecutionID and {checkLineageTypes}

        update	api.ExecutionPredicate
    set		Success = 0,
		    [Message] = 'Predicate is system reserved and may not be created.'
    where	ExecutionID = @ExecutionID and {systemReservedSQL}
;";

                    Connection.Execute(checkSQL, new { execution.ExecutionID }, commandTimeout: timeout);

                    #endregion

                    generalChecksCompleted = true;
                }
                catch (Exception generalEx)
                {
                    generalChecksCompleted = false;
                    var msg = generalEx.GetFullExceptionData(false);
                    execution.ErrorMessage = msg;
                    execution.Processed = 0;
                    execution.Error = import.Count();

                    results = new List<PredicateUpsertResult>();
                    results.AddRange(import.Select(i => new PredicateUpsertResult { ExecutionItemUid = i.ExecutionItemUid, Message = msg, Success = false }));
                }

                if (generalChecksCompleted)
                {
                    int loopSize = 250;
                    int numberOfLoops = (int)Math.Ceiling((decimal)(execution.Total - currentLocation.HighestItemNumberProcessed) / loopSize);
                    int beginItemNumber = currentLocation.HighestItemNumberProcessed + 1;
                    int endItemNumber = currentLocation.HighestItemNumberProcessed + loopSize;
                    var predicateTypes = Enum.GetValues(typeof(PredicateType)).Cast<PredicateType>().ToList();

                    for (int currentLoop = 1; currentLoop <= numberOfLoops; currentLoop++)
                    {
                        bool runCompleted = false;
                        int retryCount = 0;

                        while (!runCompleted && retryCount <= API_V2_RETRY_LIMIT)
                        {
                            var querySuffix = $"P.Success is null and P.ExecutionID = @ExecutionID and P.ItemNumber between @beginItemNumber and @endItemNumber";
                            using (var trans = Connection.BeginTransaction())
                            {
                                try
                                {

                                    var insertSQL = $@"
                                            drop table if exists #mergeResultTable
                                            create table #mergeResultTable (PredicateId int, PredicateUid uniqueidentifier, ExecutionItemUid uniqueidentifier) 

                                            merge into [Predicate] P
                                            using ( select * 
	                                                from api.ExecutionPredicate
		                                            where ExecutionID = @ExecutionID
                                                          and ItemNumber between @beginItemNumber and @endItemNumber
                                                          and PredicateID is null
                                                          and Success is null
	                                              ) S
                                            on (P.uid = S.uid)
											when matched then
											update  
												set P.Name = S.Name,
												P.Inverse = S.Inverse,
												P.Type = S.Type
                                            when not matched then
	                                            insert (Name, Inverse, Type, IsSystem)
	                                            values (S.Name,S.Inverse, S.Type, 0)
	                                        output inserted.ID, inserted.Uid, S.ExecutionItemUid into #mergeResultTable;

                                            update EP
                                            set EP.PredicateID = Res.PredicateId,
	                                            EP.uid = Res.PredicateUid
                                            from api.ExecutionPredicate EP
                                                 inner join #mergeResultTable Res on Res.ExecutionItemUid = EP.ExecutionItemUid
                                            where EP.ExecutionID = @ExecutionID";

                                    Connection.Execute(insertSQL,
                                            new { execution.ExecutionID, beginItemNumber, endItemNumber }, transaction: trans, commandTimeout: timeout);

                                    Connection.Execute(
                                        $"update P set P.Success = 1 from api.ExecutionPredicate P where	{querySuffix} and P.PredicateID is not null;",
                                        new { execution.ExecutionID, beginItemNumber, endItemNumber }, transaction: trans, commandTimeout: timeout);

                                    trans.Commit();
                                    runCompleted = true;

                                }
                                catch (Exception ex)
                                {
                                    trans.Rollback();

                                    retryCount++;

                                    if (retryCount > API_V2_RETRY_LIMIT)
                                    {
                                        LogLoopExecutionError(execution.ExecutionID, beginItemNumber, endItemNumber, "api.ExecutionPredicate", ex.GetFullExceptionData(false), timeout);
                                    }
                                }
                            }
                        }

                        results.AddRange(
                            Query<PredicateUpsertResult>(
                                $"select * from api.ExecutionPredicate where ExecutionID = @ExecutionID and ItemNumber between @beginItemNumber and @endItemNumber",
                                new { execution.ExecutionID, beginItemNumber, endItemNumber }
                            )
                        );


                        beginItemNumber += loopSize;
                        endItemNumber += loopSize;
                    }

                    Connection.Close();

                }
            }

            return results;
        }

        public List<ResponsibilityTypeUpsertResult> UpsertResponsibilityTypes(ApiExecution execution, List<ResponsibilityTypeUpsertModel> import, int timeout = 3600)
        {
            var results = new List<ResponsibilityTypeUpsertResult>();
            bool generalChecksCompleted = false;
            CurrentExecutionLocationModel currentLocation = null;

            var uidDupes = import.GroupBy(i => i.Uid).Where(i => i.Count() > 1).Select(i => new { Uid = i.Key, Count = i.Count() }).ToList();
            if (uidDupes.Any() && execution.Method == "PUT")
            {
                execution.ErrorMessage = $"Duplicate Asset Uids: {string.Join(", ", uidDupes.Select(i => i.Uid.ToString()))}. Identifiers must be unique within a batch.";
                results.AddRange(import.Select(i => new ResponsibilityTypeUpsertResult { Uid = i.Uid.Value, Message = execution.ErrorMessage, Success = false }));
            }
            else
            {

                try
                {
                    currentLocation = GetCurrentExecutionLocation(execution.ExecutionID, "api.ExecutionResponsibilityType");

                    if (currentLocation.HighestItemNumberProcessed > 0)
                    {
                        results.AddRange(
                            Query<ResponsibilityTypeUpsertResult>(
                                $"select * from api.ExecutionResponsibilityType where ExecutionID = @ExecutionID and ItemNumber <= {currentLocation.HighestItemNumberProcessed}",
                                new { execution.ExecutionID }
                            )
                        );
                    }

                    #region Build data tables.

                    var table = new DataTable();
                    table.Columns.Add("ExecutionID", typeof(Guid));
                    table.Columns.Add("ItemNumber", typeof(int));
                    table.Columns.Add("ResponsibilityTypeId", typeof(long));
                    table.Columns.Add("Uid", typeof(Guid));
                    table.Columns.Add("Name", typeof(string));
                    table.Columns.Add("Description", typeof(string));
                    table.Columns.Add("IsNew", typeof(bool));
                    table.Columns.Add("Message", typeof(string));
                    table.Columns.Add("Success", typeof(bool));
                    table.Columns.Add("ExecutionItemUid", typeof(Guid));

                    #endregion

                    #region Generate data sets

                    for (int i = 1; i <= import.Count; i++)
                    {
                        if (i > currentLocation.HighestItemNumber)
                        {
                            var model = import[i - 1];

                            var row = table.NewRow();

                            row["ExecutionID"] = execution.ExecutionID;
                            row["ExecutionItemUid"] = Guid.NewGuid();
                            row["ItemNumber"] = i;
                            row["Name"] = model.Name;
                            row["Description"] = model.Description;
                            if (model.Uid.HasValue)
                                row["Uid"] = model.Uid;
                            else
                            {
                                row["IsNew"] = true;
                            }

                            table.Rows.Add(row);
                        }
                    }

                    #endregion

                    if (Database.Connection.State != ConnectionState.Open)
                        Connection.OpenWithRetry(RetryPolicy.DefaultProgressive);

                    #region Bulk Copy

                    SqlBulkCopy bulkCopy = new SqlBulkCopy(Connection);

                    bulkCopy.BatchSize = table.Rows.Count;
                    bulkCopy.DestinationTableName = "api.ExecutionResponsibilityType";
                    bulkCopy.BulkCopyTimeout = timeout;

                    bulkCopy.ColumnMappings.Add("ExecutionID", "ExecutionID");
                    bulkCopy.ColumnMappings.Add("ItemNumber", "ItemNumber");
                    bulkCopy.ColumnMappings.Add("ExecutionItemUid", "ExecutionItemUid");
                    bulkCopy.ColumnMappings.Add("Name", "Name");
                    bulkCopy.ColumnMappings.Add("Description", "Description");
                    bulkCopy.ColumnMappings.Add("Uid", "Uid");
                    bulkCopy.ColumnMappings.Add("IsNew", "IsNew");

                    bulkCopy.WriteToServer(table);

                    bulkCopy = null;

                    #endregion


                    #region Log data errors
                    var checkSQL = $@"
    update api.ExecutionResponsibilityType
    set		Success = 0,
		    [Message] = coalesce([Message] + '; ', '') + 'Invalid UID value'
    from api.ExecutionResponsibilityType ERT
        inner join api.Execution AE on AE.ExecutionID = ERT.ExecutionID
    where   AE.Method = 'PUT' and ERT.ExecutionID = @ExecutionID and (ERT.Uid is null or ERT.Uid = '00000000-0000-0000-0000-000000000000')

    update	api.ExecutionResponsibilityType 
    set		Success = 0,
		    [Message] = coalesce([Message] + '; ', '') + 'Responsibility type with same Name already exists'
    from api.ExecutionResponsibilityType ERT
    inner join [ResponsibilityType] RT on RT.Name = ERT.Name
    where	ExecutionID = @ExecutionID  and (RT.Uid <> ERT.Uid or ERT.Uid is null);

    update	api.ExecutionResponsibilityType 
    set		Success = 0,
		    [Message] = coalesce([Message] + '; ', '') + 'Responsibility type with this Uid does not exists'
    from api.ExecutionResponsibilityType ERT
    left join [ResponsibilityType] RT on RT.Uid = ERT.Uid
    where	ExecutionID = @ExecutionID and ERT.Uid is not null and RT.Uid is null;
;";

                    Connection.Execute(checkSQL, new { execution.ExecutionID }, commandTimeout: timeout);

                    #endregion

                    generalChecksCompleted = true;
                }
                catch (Exception generalEx)
                {
                    generalChecksCompleted = false;
                    var msg = generalEx.GetFullExceptionData(false);
                    execution.ErrorMessage = msg;
                    execution.Processed = 0;
                    execution.Error = import.Count();

                    results = new List<ResponsibilityTypeUpsertResult>();
                    results.AddRange(import.Select(i => new ResponsibilityTypeUpsertResult { Message = msg, Success = false }));
                }

                if (generalChecksCompleted)
                {
                    int loopSize = 250;
                    int numberOfLoops = (int)Math.Ceiling((decimal)(execution.Total - currentLocation.HighestItemNumberProcessed) / loopSize);
                    int beginItemNumber = currentLocation.HighestItemNumberProcessed + 1;
                    int endItemNumber = currentLocation.HighestItemNumberProcessed + loopSize;
                    var predicateTypes = Enum.GetValues(typeof(PredicateType)).Cast<PredicateType>().ToList();

                    for (int currentLoop = 1; currentLoop <= numberOfLoops; currentLoop++)
                    {
                        bool runCompleted = false;
                        int retryCount = 0;

                        while (!runCompleted && retryCount <= API_V2_RETRY_LIMIT)
                        {
                            var querySuffix = $"ERT.Success is null and ERT.ExecutionID = @ExecutionID and ERT.ItemNumber between @beginItemNumber and @endItemNumber";
                            using (var trans = Connection.BeginTransaction())
                            {
                                try
                                {

                                    var insertSQL = $@"
                                            drop table if exists #mergeResultTable
                                            create table #mergeResultTable (ResponsibilityTypeId int, ResponsibilityTypeUid uniqueidentifier, ExecutionItemUid uniqueidentifier) 

                                            merge into [ResponsibilityType] RT
                                            using ( select * 
	                                                from api.ExecutionResponsibilityType
		                                            where ExecutionID = @ExecutionID
                                                          and ItemNumber between @beginItemNumber and @endItemNumber
                                                          and ResponsibilityTypeId is null
                                                          and Success is null
	                                              ) S
                                            on (RT.uid = S.uid)
											when matched then
											update  
												set RT.Name = S.Name,
												RT.Description = S.Description
                                            when not matched then
	                                            insert (Name, Description)
	                                            values (S.Name,S.Description)
	                                        output inserted.ID, inserted.Uid, S.ExecutionItemUid into #mergeResultTable;

                                            update RT
                                            set RT.ResponsibilityTypeId = Res.ResponsibilityTypeId,
	                                            RT.Uid = Res.ResponsibilityTypeUid,
                                                RT.Success = 1
                                            from api.ExecutionResponsibilityType RT
                                                 inner join #mergeResultTable Res on Res.ExecutionItemUid = RT.ExecutionItemUid
                                            where RT.ExecutionID = @ExecutionID and RT.Success is null";

                                    Connection.Execute(insertSQL,
                                            new { execution.ExecutionID, beginItemNumber, endItemNumber }, transaction: trans, commandTimeout: timeout);

                                    Connection.Execute(
                                        $"update ERT set ERT.Success = 1 from api.ExecutionResponsibilityType ERT where	{querySuffix} and ERT.ResponsibilityTypeId is not null;",
                                        new { execution.ExecutionID, beginItemNumber, endItemNumber }, transaction: trans, commandTimeout: timeout);

                                    trans.Commit();
                                    runCompleted = true;

                                }
                                catch (Exception ex)
                                {
                                    trans.Rollback();

                                    retryCount++;

                                    if (retryCount > API_V2_RETRY_LIMIT)
                                    {
                                        LogLoopExecutionError(execution.ExecutionID, beginItemNumber, endItemNumber, "api.ExecutionResponsibilityType", ex.GetFullExceptionData(false), timeout);
                                    }
                                }
                            }
                        }

                        results.AddRange(
                            Query<ResponsibilityTypeUpsertResult>(
                                $"select * from api.ExecutionResponsibilityType where ExecutionID = @ExecutionID and ItemNumber between @beginItemNumber and @endItemNumber",
                                new { execution.ExecutionID, beginItemNumber, endItemNumber }
                            )
                        );


                        beginItemNumber += loopSize;
                        endItemNumber += loopSize;
                    }

                    Connection.Close();

                }

            }


            return results;
        }

    }
}
