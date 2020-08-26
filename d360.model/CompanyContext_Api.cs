using d360.core;
using d360.core.entities;
using d360.core.entities.Membership;
using d360.core.entities.Metric;
using d360.core.enums;
using d360.core.enums.Workflow;
using d360.core.helpers;
using d360.core.queue;
using d360.core.resources;
using Dapper;
using Microsoft.ApplicationInsights;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Diagnostics;
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

        public int SqlBulkBatchSize { get; set; } = 5000; // default size to use for sqlbulkcopy operations 0 means one batch
        public int SqlBulkBatchTimeout { get; set; } = 0; // timeout for sqlbulkcopy operations  0 means run until it happens
        public int WorkflowSendBatchSize { get; set; } = 50; // number of items to send at a time for a batch of service bus messages

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

        /// <summary>
        /// Used to check if the given object and object id has workflows setup for the specified change type.  If null all change types are checked
        /// </summary>
        /// <param name="object">Workflow Object</param>
        /// <param name="objectID">Workflow Object ID</param>
        /// <param name="changeType">Workflow change type</param>
        /// <returns>True if workflows for the specified object / change type false otherwise</returns>
        private bool TypeHasWorkflows(string @object, int objectID, ChangeType? changeType)
        {
            if (changeType.HasValue)
                return Database.Connection.QuerySingle<int>("SELECT ISNULL((select count(1) from workflow.EventRegistration where [object] = @obj and [objectid] = @objId and [state] = 1 and [changetype] = @change), 0)", new { obj = @object, objId = objectID, change = changeType.Value }) > 0;

            return Database.Connection.QuerySingle<int>("SELECT ISNULL((select count(1) from workflow.EventRegistration where [object] = @obj and [objectid] = @objId and [state] = 1 ), 0)", new { obj = @object, objId = objectID }) > 0;
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

        private void LogPolicyHierMaxLimitErrors(Guid executionID, bool isInsert, int? intersectTypeID, int maxlevel, int timeout = 3600)
        {
            Connection.Execute(@"

            drop table if exists #tempdata;
            drop table if exists #tempdistparent;
            drop table if exists #tempdistparentresult;
            drop table if exists #tempdistchild;
            drop table if exists #tempdistchildresult;

            select distinct itemnumber,parentuid,uid, 0 TotalLevel
            Into #tempdata
            from api.ExecutionAsset a
            where a.ExecutionID = @executionID;

            create nonclustered index ix_tempdataitemnumber on #tempdata (itemnumber asc);
            create nonclustered index ix_tempdataparentuid on #tempdata (parentuid asc);
            create nonclustered index ix_tempdatauid on #tempdata (uid asc);

            select distinct parentuid
            Into #tempdistparent
            from #tempdata
            where parentuid is not null and ParentUid <> '00000000-0000-0000-0000-000000000000';

            with h as 
            (select 
                    p.parentuid,
                    A.object subject,
	                A.objectid SubjectId,
		            1 [Level]
             from #tempdistparent P
             inner join Asset A
             on A.uid = p.parentuid
             union all
            select  H.parentuid,
                    I.subject,
	                I.SubjectId,
		            H.[Level] + 1 [Level]
            from H
            inner join [Intersect] I
             on I.[object] = h.Subject
             and I.ObjectID = h.SubjectID
             and I.IntersectTypeID = @intersectTypeID
             where H.[Level] <= @maxlevel + 1
              )
            select parentuid,isnull(max([Level]),0) [HLevel]
            into #tempdistparentresult
            from H
            group by parentuid;

            create nonclustered index ix_tempdistparentresultparentuid on #tempdistparentresult (parentuid asc);

            update d
            set d.TotalLevel = d.TotalLevel + t.HLevel
            from #tempdata d
            inner join #tempdistparentresult t
            on d.parentuid = t.parentuid;
            
            if (@isInsert = 0)
                begin
                select distinct uid,0 CLevel
                Into #tempdistchild
                from #tempdata;

                with h as 
                (select c.uid,
                        A.object,
	                    A.objectid,
		                1 [Level]
                 from #tempdistchild C
                 inner join Asset A
                 on a.uid = c.uid
                 union all
                select H.uid,
                        I.Object,
	                    I.ObjectId,
		                H.[Level] + 1 [Level]
                from H
                inner join [Intersect] I
                 on I.[Subject] = h.object
                 and I.SubjectID = h.objectID
                 and I.IntersectTypeID = @intersectTypeID
                 where H.[Level] <= @maxlevel + 1
                  )
                select uid,isnull(max([Level]),0) [CLevel]
                into #tempdistchildresult
                from H
                group by uid;

                create nonclustered index ix_tempdistchildresultuid on #tempdistchildresult (uid asc);

                update d
                set d.TotalLevel = d.TotalLevel + t1.CLevel
                from #tempdata d
                inner join #tempdistchildresult t1
                on d.uid = t1.uid;
            end

            update ea
            set		ea.Success = 0,
            ea.[Message] = coalesce(ea.[Message] + '; ', '') + 'Maximum hierarchy level allowed is less than or equal to ' + cast(@maxlevel as varchar(20)) + '.'
            from api.ExecutionAsset ea
            inner join #tempdata d 
            on ea.ExecutionID =  @executionID and ea.itemnumber = d.itemnumber
            where	(@isInsert = 0 and  d.TotalLevel > @maxlevel)
					or (@isInsert = 1  and  d.TotalLevel >= @maxlevel);
            ", new {executionID, intersectTypeID, maxlevel, isInsert }, commandTimeout: timeout);
        }

        private void LogNullIsRequiredFields(Guid executionID, int timeout = 3600)
        {
            Connection.Execute(@"
            drop table if exists #tempreqfield;
            
            select A.executionid,a.itemnumber,STRING_AGG(FT.NAME,',') WITHIN GROUP (ORDER BY ft.columnorder) stringfield,count(1) cnt
            into #tempreqfield
            from api.ExecutionAsset A
            inner join dbo.FieldType FT on FT.object = A.objecttype and FT.ObjectID = A.objecttypeid and FT.IsRequired = 1
            left join Field EF on EF.FieldTypeID = FT.ID and EF.AssetID = A.AssetID
            left join [api].[ExecutionField] F on F.ExecutionID = A.ExecutionID and F.ItemNumber = A.ItemNumber and F.FieldTypeID = FT.ID
            where A.executionid = @executionID 
            and (trim(EF.Value) is null or EF.Value = char(0))
            and (trim(F.FieldValue) is null or trim(F.FieldValue) = char(0))
            group by A.executionid,a.itemnumber;

            create index idx_tempreqfield on #tempreqfield(itemnumber,executionid);

            update	A
            set		Success = 0,
		            [Message] = coalesce([Message] + '; ', '') + f.stringfield + case when f.cnt = 1 then ' is a ' else ' are ' end + 'required field' + case when f.cnt = 1 then '' else 's' end 
            from api.ExecutionAsset A
            inner join #tempreqfield F on F.ExecutionID = A.ExecutionID and F.ItemNumber = A.ItemNumber
            where A.executionid = @executionID;",
            new { executionID }, commandTimeout: timeout);
        }

        private void LogAssetPermissionErrors(Guid executionID, AssetType at, Permission p, bool isInsert, string apiTableName, int timeout = 3600)
        {
            if (string.IsNullOrEmpty(apiTableName))
            {
                throw new ApplicationException("Endpoint logic is misconfigured, and is missing an API table name.");
            }
            if (!CurrentResourceIsAdmin && isInsert && p == Permission.ModifyAsset)
            {
                PermissionInfo permission = this.GetTypePermissions(at.Object, at.ObjectID).Where(x => x.ID == Permission.ModifyAsset).SingleOrDefault();
                if (permission == null || !permission.Selected)
                {
                    Connection.Execute($@"
    
	                update	T
	                set		T.Success = 0,
			                T.[Message] = coalesce([Message] + '; ', '') + 'User does not have permission to add this asset.'
	                from    api.{apiTableName} T
			                inner join api.Execution E on E.ExecutionID = T.ExecutionID 
											                where  E.ExecutionID = @executionID 
											                and T.AssetID is  null
                            ", new { executionID }, commandTimeout: timeout);
                }

            }
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

        private void LogParentErrors(Guid executionID, int timeout = 3600, bool allowEmptyParentUid = false)
        {
            Connection.Execute($@"
update	api.ExecutionAsset
set		Success = 0,
		[Message] = coalesce([Message] + '; ', '') + 'Asset does not contain a valid ParentUid value'
where	ExecutionID = @executionID
        and ParentAssetID is null
		and ParentUid is not null        
        {(allowEmptyParentUid ? " and ParentUid <> '00000000-0000-0000-0000-000000000000'" : "")}
;",
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

        private void LogInvalidFusionIDFields(Guid executionID, int timeout = 3600)
        {
            Connection.Execute(@"
update	E
set		E.Message = 'Invalid FusionID value for this Asset type',
		E.Success = 0
from	api.ExecutionAsset E
	where E.ExecutionID = @executionID and not exists(select F.ID from api.ExecutionField EF
	inner join api.ExecutionAsset EA on EF.ExecutionID = EA.ExecutionID
	inner join FusionAttributeType FAT on FAT.ID = EA.ObjectTypeID
	inner join FusionType FT on FT.ID = FAT.FusionTypeID
	inner join Fusion F on F.FusionTypeID = FT.ID
	where EA.ObjectType = 'FusionAttributeType' 
	and EF.ExecutionID = @executionID
	and EF.FieldName = 'fusionid'
	and F.ID = EF.FieldValue)",
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
            string targetTable = (obj != "IntersectType") ? "api.ExecutionAsset" : "api.ExecutionRelationship";
            string assetJoin = lookupFieldsPassedByValue ? "AD.ObjectID = try_cast(V.[value] as int)" : "AD.DisplayValue = V.[value]";

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
													left join (
														select	S.ID,
                                                                S.DisplayValue,
                                                                S.[Object], 
																S.ObjectID, 
																S.[Type], 
																S.TypeID 
														from	AssetDetail S 
														union all
														select	T.ID,
                                                                T.[Name] as DisplayValue,
                                                                T.[Object],
																T.ObjectID,
																T.[Object] as [Type],
																0 as TypeID 
														from	AssetType T
														where	T.[Object] = 'ReferenceItemType'
													) AD on {assetJoin} 
														and ((AD.[Type] = IT.[Object] AND AD.TypeID = IT.ObjectID) 
														or (AD.[Type] = IT.[Subject] AND AD.TypeID = IT.SubjectID))
                                        where       A.ExecutionID = @executionID and AD.ID IS NULL
					                    group by	A.ExecutionID, A.ItemNumber
					                    ) S on S.ExecutionID = T.ExecutionID and S.ItemNumber = T.ItemNumber;
                    ", new { executionID, obj, objID }, commandTimeout: timeout);

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

        private List<AssetFieldTypeUpdate> MergeFields(Guid executionID, SqlTransaction trans, string tableName, string objectSqlSyntax, string objectIdSqlSyntax, int beginItemNumber, int endItemNumber, bool sendWorkflowEvents, int timeout = 3600, bool shouldCheckExistingFieldValues = true)
        {
            List<AssetFieldTypeUpdate> res = new List<AssetFieldTypeUpdate>();

            if (sendWorkflowEvents)
            {
                res = Connection.Query<AssetFieldTypeUpdate>($@"
                    select  EA.Object, 
                            EA.ObjectID, 
                            EF.FieldTypeID AS Id 
                    from    {tableName} EA 
	                        inner join api.ExecutionField EF on EF.ExecutionID = EA.ExecutionID 
                                            and EF.ItemNumber = EA.ItemNumber 
                                            and EA.ObjectID is not null 
                                            and EF.FieldTypeID is not null
	                        inner join Field F on F.FieldTypeId = EF.FieldTypeID 
                                            and F.ObjectType = EA.Object 
                                            and F.ObjectId = EA.ObjectID
                    where   EA.ExecutionID = @executionID 
                            and EA.IsNew <> 1 
                            {(shouldCheckExistingFieldValues ? "and F.Value <> EF.FieldValue" : "")} 
                            and @sendWorkflowEvents = 1 
                            and EA.ItemNumber between @beginItemNumber and @endItemNumber

                    union all

                    select  EA.Object, 
                            EA.ObjectID, 
                            EF.FieldTypeID AS Id 
                    from    {tableName} EA 
	                        inner join api.ExecutionField EF on EF.ExecutionID = EA.ExecutionID 
                                            and EF.ItemNumber = EA.ItemNumber 
                                            and EA.ObjectID is not null 
                                            and EF.FieldTypeID is not null
                    where   EA.ExecutionID = @executionID 
                            and EA.IsNew <> 1 
                            and @sendWorkflowEvents = 1 
                            and EA.ItemNumber between @beginItemNumber and @endItemNumber
                            {(shouldCheckExistingFieldValues ? "and coalesce(EF.FieldValue, '') <> ''" : "")} 
                            and not exists (select 1 from Field where FieldTypeID = EF.FieldTypeID 
                                and ObjectType = EA.Object and ObjectID = EA.ObjectID)
"
                    , new { executionID, sendWorkflowEvents, beginItemNumber, endItemNumber }, transaction: trans, commandTimeout: timeout).ToList();
            }

            // if we already have the asset id then insert it
            bool hasAssetID = ((tableName ?? "").ToUpper() == "API.EXECUTIONASSET");

            if (shouldCheckExistingFieldValues)
            {
                Connection.Execute($@"
                    DELETE Field
                    FROM Field F
                    	inner join {tableName} E on E.ExecutionID = @executionID 
                    	inner join api.ExecutionField EF on EF.ExecutionId = E.ExecutionId and EF.ItemNumber = E.ItemNumber
                    	inner join Asset A on A.uid = E.Uid
                    WHERE E.ExecutionID = @executionID
                     and EF.ItemNumber between @beginItemNumber and @endItemNumber
                     and EF.Ignore is null
                     and EF.FieldTypeID is not null
                     and F.ObjectID = A.ObjectID
                     and F.ObjectType = A.Object
                     and F.FieldTypeID = EF.FieldTypeID
                     and EF.FieldValue is null 
                     and EF.LookupValue is null;",
                new { executionID, beginItemNumber, endItemNumber, resourceId = CurrentResourceID }, transaction: trans, commandTimeout: timeout);
            }


            Connection.Execute($@"
merge       Field as T
using       (
            select 
                    {objectSqlSyntax},
                    {objectIdSqlSyntax}, 
                    F.FieldTypeID,
                    coalesce(F.LookupValue, F.FieldValue) as Value,
                    F.FieldValue as FormattedValue
                    {(hasAssetID ? ",A.AssetID as AssetID" : ",null as AssetID")}                    
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
                    and FieldValue is not null
            ) as S 
on          ( T.FieldTypeID = S.FieldTypeID and T.ObjectType = S.Object and T.ObjectID = S.ObjectID )
{(shouldCheckExistingFieldValues ? " when matched and T.Value <> S.Value COLLATE SQL_Latin1_General_CP1_CS_AS OR T.FormattedValue <> S.FormattedValue COLLATE SQL_Latin1_General_CP1_CS_AS then update set T.Value = S.Value,T.FormattedValue = S.FormattedValue, T.UpdatedBy = @resourceId, T.UpdatedOn = getutcdate() " : " ")}
when		not matched by target then
insert		(FieldTypeID, ObjectType, ObjectID, Value, FormattedValue, UpdatedBy, UpdatedOn, AssetID)
values		(S.FieldTypeID, S.Object, S.ObjectID, S.Value, S.FormattedValue, @resourceId, getutcdate(), S.AssetID);",
            new { executionID, sendWorkflowEvents, beginItemNumber, endItemNumber, resourceId = CurrentResourceID }, transaction: trans, commandTimeout: timeout);

            return res;
        }

        private void ImportRelationships(Guid executionID, SqlTransaction trans, string tableName, string objectSqlSyntax, string objectIdSqlSyntax, int beginItemNumber, int endItemNumber, int timeout = 3600, bool resolveRelationshipOnObjectId = false, bool sendGraphEvents = true)
        {

            string assetJoin = resolveRelationshipOnObjectId ? "S.ObjectID = try_cast(V.[value] as int)" : "S.DisplayValue = V.[value]";


            var events = Connection.Query<DatabaseBulkRelationshipResult>($@"

	            drop table if exists #Relationships;
	            create table #Relationships
	            (
		            ID int,
                    [uid] uniqueidentifier,
		            IntersectTypeID int,
		            [Subject] varchar(50),
		            SubjectID int,
                    SubjectAssetTypeID int,
		            [Object] varchar(50),
		            ObjectID int,
                    ObjectAssetTypeID int,
                    SwitchObject bit
	            )

                drop table if exists #DeletedRelationships;
                create table #DeletedRelationships
                (
                    [uid] uniqueidentifier
                )

                ;with R
                    as (
                        select  distinct 
                                A.[Object],
                                A.ObjectID,
                                OT.ID as ObjectAssetTypeID,
                                FT.LookupObjectId as IntersectTypeID,
                                S.[Object] as [Subject],
                                S.ObjectID as SubjectID,
                                S.AssetTypeID as SubjectAssetTypeID,
                                case 
                                when S.[Type] = IT.[Object] AND S.TypeID = IT.ObjectID then 1
                                else 0
                                end as switchObject
                        from    {tableName} A
                                inner join AssetType OT on OT.Object = A.ObjectType and OT.ObjectID = A.ObjectTypeID
                                inner join api.ExecutionField F on F.ExecutionID = A.ExecutionID
                                    and F.ItemNumber = A.ItemNumber 
                                    and A.ObjectID is not null 
                                    and F.FieldTypeID is not null
						            and A.Success is null
                                cross apply string_split(F.FieldValue, ',') V                                    
                                inner join FieldType FT on FT.ID = F.FieldTypeID AND FT.Type = 'Relationship' AND FT.LookupObjectType = 'IntersectType'
                                inner join IntersectType IT on IT.ID = FT.LookupObjectId
                                inner join (
									select	AD.DisplayValue,
                                            AD.[Object], 
											AD.ObjectID, 
											AD.[Type], 
											AD.TypeID,
                                            AD.AssetTypeID
									from	AssetDetail AD 
									union all
									select	T.[Name] as DisplayValue,
                                            T.[Object],
											T.ObjectID,
											T.[Object] as [Type],
											0 as TypeID,
                                            T.ID as AssetTypeID
									from	AssetType T
									where	T.[Object] = 'ReferenceItemType'
                                    and     T.ObjectID <> 0
								) S on {assetJoin}
									and ((S.[Type] = IT.[Object] AND S.TypeID = IT.ObjectID) 
                                    or (S.[Type] = IT.[Subject] AND S.TypeID = IT.SubjectID))
                        where   A.ExecutionID = @executionID
                                and A.ItemNumber between @beginItemNumber and @endItemNumber 
                                and (F.Ignore = 0 or F.Ignore is null)
                                and FT.Type = 'Relationship'
                        )
                        insert into #Relationships (ID, [uid], IntersectTypeID, SubjectAssetTypeID, Subject, SubjectId, ObjectAssetTypeID, Object, ObjectID, SwitchObject)
                        select
                            null as ID,
                            null as [uid],
			                IntersectTypeId, 
			                CASE 
				                when switchObject = 0 then SubjectAssetTypeID
				                else ObjectAssetTypeID
			                END AS SubjectAssetTypeID, 
			                CASE 
				                when switchObject = 0 then Subject
				                else Object
			                END AS Subject, 
			                CASE 
				                when switchObject = 0 then SubjectId
				                else ObjectID
			                END AS SubjectId,
			                CASE 
				                when switchObject = 0 then ObjectAssetTypeID
				                else SubjectAssetTypeID
			                END AS ObjectAssetTypeID, 
			                CASE 
				                when switchObject = 0 then Object
				                else Subject
			                END AS Object, 
			                CASE 
				                when switchObject = 0 then ObjectId
				                else SubjectId
			                END AS ObjectID,
                            SwitchObject
			            from R;

                        update R
                        set R.ID = I.ID,
                            R.[uid] = I.[uid]
                        from #Relationships R
                        inner join [Intersect] I on 
                            I.IntersectTypeID = R.IntersectTypeID 
                            and I.[Subject] = R.[Subject] 
                            and I.SubjectID = R.SubjectID 
                            and I.[Object] = R.[Object] 
                            and I.ObjectID = R.ObjectID;

                        --check reverse if subject/object type are the same
					    update R
                        set R.ID = I.ID,
                            R.[uid] = I.[uid]
                        from #Relationships R
					    inner join IntersectType T on T.ID = R.IntersectTypeID and T.Subject = T.Object and T.SubjectID = T.ObjectID
                        inner join [Intersect] I on 
                            I.IntersectTypeID = R.IntersectTypeID 
                            and I.[Subject] = R.[Object] 
                            and I.SubjectID = R.ObjectID
                            and I.[Object] = R.[Subject] 
                            and I.ObjectID = R.SubjectID
					    where R.ID is null;


                        insert into #DeletedRelationships
                            select I.[uid]  from api.ExecutionAsset A
                                inner join api.ExecutionField F on F.ExecutionID = A.ExecutionID
                                    and F.ItemNumber = A.ItemNumber 
                                    and A.ObjectID is not null 
                                    and F.FieldTypeID is not null
						            and A.Success is null
                                inner join FieldType FT on FT.ID = F.FieldTypeID AND FT.Type = 'Relationship' AND FT.LookupObjectType = 'IntersectType'
								inner join IntersectType IT on IT.ID = FT.LookupObjectId
								inner join [Intersect] I on IT.ID = I.IntersectTypeID
		                                and ((I.Object = A.Object and I.ObjectID = A.ObjectID) OR (I.Subject = A.Object and I.SubjectID = A.ObjectID))
								left join #Relationships R on R.ID = I.Id
								where R.ID is null and
                                        A.ExecutionID = @executionID
                                        and A.ItemNumber between @beginItemNumber and @endItemNumber 
                                        and (F.Ignore = 0 or F.Ignore is null)
                                        and FT.Type = 'Relationship';


                        delete from [Intersect] where [uid] in (select [uid] from #DeletedRelationships);

                        insert into [Intersect] (IntersectTypeID, Subject, SubjectId, Object, ObjectID)
                        select  R.IntersectTypeID,
                                R.Subject,
                                R.SubjectID,
                                R.Object,
                                R.ObjectID
                            from    #Relationships R
							inner join [IntersectType] IT on IT.ID = IntersectTypeID
                            inner join AssetType ST on ST.ID = R.SubjectAssetTypeID
                            inner join AssetType OT on OT.ID = R.ObjectAssetTYpeID
                            where  R.ID is null 
                                    and OT.Object = IT.Object 
                                    and ST.Object = IT.Subject; 

                        update R
                        set R.ID = I.ID,
                            R.[uid] = I.[uid]
                        from #Relationships R
                        inner join [Intersect] I on I.Subject = R.Subject and I.SubjectID = R.SubjectID and I.Object = R.Object
                            and I.ObjectID = R.ObjectID and I.IntersectTypeID = R.IntersectTypeID
                        where R.ID is null;

                        select [uid], 1 as Success, 'Intersect' as [Object] from #Relationships
                        union all
                        select [uid], 1 as Success, 'Intersect' as [Object] from #DeletedRelationships
",
            new { executionID, beginItemNumber, endItemNumber }, transaction: trans, commandTimeout: timeout);


            if (sendGraphEvents)
            {
                SendAssetGraphEvents(events);
            }

        }

        private void MergeJsonFieldProperties(Guid executionID, SqlTransaction trans, List<FieldType> jsonFieldTypes, string tableName, string objectSqlSyntax, string objectIdSqlSyntax, int beginItemNumber, int endItemNumber, int timeout = 3600, bool fieldJsonPropertyLoadLimitToTopLevel = true, Dictionary<string,double> metrics = null, int step = 0)
        {
            var sw = Stopwatch.StartNew();
            var jsonFieldTypeIDs = string.Join(",", jsonFieldTypes.Select(i => i.ID));
            var fields = Connection.Query<dynamic>($@"
                    select  F.ID, 
                            F.Value 
                    from    Field F 
                            inner join api.ExecutionField E on E.ExecutionID = @executionID and E.ItemNumber between @beginItemNumber and @endItemNumber and E.FieldTypeID = F.FieldTypeID and E.FieldTypeID in ({jsonFieldTypeIDs})
                            inner join {tableName} A on A.ExecutionID = E.ExecutionID and A.ItemNumber = E.ItemNumber and A.Object = F.ObjectType and A.ObjectID = F.ObjectID",
                            new { executionID, beginItemNumber, endItemNumber }, transaction: trans, commandTimeout: timeout);

            if(metrics != null) AddMeasurement(metrics, $"MergeJsonFieldProperties >> loadfields", sw.ElapsedMilliseconds, ++step);

            sw.Restart();

            //check for 0 fields to update case which often happens when editing from ui since you cant edit json fields.
            if (!fields.Any()) return;

            var collectionFieldProperties = new List<FieldJsonProperty>();

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
                    collectionFieldProperties.AddRange(assetFieldProperties);
                }

            }

            if (metrics != null) AddMeasurement(metrics, $"MergeJsonFieldProperties >> iterate properties", sw.ElapsedMilliseconds, ++step);

            sw.Restart();

            #region Build data tables for bulk load.

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
);
", new { executionID }, transaction: trans);

            var table = new DataTable();
            table.Columns.Add("FieldID", typeof(long));
            table.Columns.Add("Name", typeof(string));
            table.Columns.Add("Parent", typeof(string));
            table.Columns.Add("Path", typeof(string));
            table.Columns.Add("Position", typeof(int));
            table.Columns.Add("IsArray", typeof(bool));
            table.Columns.Add("Value", typeof(string));

            foreach (var f in collectionFieldProperties)
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
            
            using (var bulkCopy = new SqlBulkCopy((SqlConnection)Database.Connection, SqlBulkCopyOptions.TableLock, trans)
            {
                BatchSize = SqlBulkBatchSize,
                DestinationTableName = "#FieldJsonProperty",
                BulkCopyTimeout = SqlBulkBatchTimeout
            }){

                bulkCopy.ColumnMappings.Add("FieldID", "FieldID");
                bulkCopy.ColumnMappings.Add("Name", "Name");
                bulkCopy.ColumnMappings.Add("Parent", "Parent");
                bulkCopy.ColumnMappings.Add("Path", "Path");
                bulkCopy.ColumnMappings.Add("Position", "Position");
                bulkCopy.ColumnMappings.Add("IsArray", "IsArray");
                bulkCopy.ColumnMappings.Add("Value", "Value");

                bulkCopy.WriteToServer(table);
            }

            if (metrics != null) AddMeasurement(metrics, $"MergeJsonFieldProperties >> bulk load", sw.ElapsedMilliseconds, ++step);

            sw.Restart();

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

            if (metrics != null) AddMeasurement(metrics, $"MergeJsonFieldProperties >> merge results", sw.ElapsedMilliseconds, ++step);

            sw.Restart();
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

        public void ResolveFieldLookupValues(Guid executionID, string fieldTable = "api.ExecutionField", int timeout = 3600, SqlTransaction trans = null)
        {
            Connection.Execute($@"
drop table if exists #RelevantLookupValues;
create table #RelevantLookupValues (FieldTypeID int not null, [Text] nvarchar(max), [Value] nvarchar(max));

;with field_type_ids as( 
select distinct F.Id from {fieldTable} T
				inner join FieldType F on F.ID = T.FieldTypeID and F.[Type] = 'Lookup' and T.ExecutionID = @executionID)
				insert into #RelevantLookupValues
				select FieldTypeId,[Text],[Value] from field_type_ids fti
					inner join FieldLookupValue FLV on FLV.FieldTypeID = fti.ID

declare @maxlen int;
select @maxlen = max(len(text)) from #RelevantLookupValues

if (@maxlen <= 400)
begin
	alter table #RelevantLookupValues alter column text nvarchar(440);
	CREATE CLUSTERED INDEX CIX_RelevantLookupValues ON #RelevantLookupValues ( FieldTypeID ASC,[Text] )
end
else
begin
	CREATE CLUSTERED INDEX CIX_RelevantLookupValues ON #RelevantLookupValues ( FieldTypeID ASC )
end


drop table if exists #LookupValues
create table #LookupValues (FieldValue nvarchar(max) not null, FieldTypeID int not null, [Value] nvarchar(max) null)

;with cte_fieldvalues_multi as (select distinct T.fieldvalue, F.Id, FLV.Value
	from {fieldTable}  T
	cross apply string_split(T.FieldValue, ',') MV
    inner join FieldType F on F.ID = T.FieldTypeID and F.[Type] = 'Lookup' and F.[AllowMultipleValues] = 1 and T.ExecutionID = @executionID
	left join #RelevantLookupValues FLV on FLV.FieldTypeID = T.FieldTypeID and TRIM(MV.value) = FLV.Text
	where executionid = @executionid)
insert into #LookupValues
select FieldValue, Id, STRING_AGG(Value, ',') from cte_fieldvalues_multi
group by fieldvalue, Id

;insert into #LookupValues
select distinct T.fieldvalue, F.Id, FLV.Value
	from {fieldTable}  T
    inner join FieldType F on F.ID = T.FieldTypeID and F.[Type] = 'Lookup' and F.[AllowMultipleValues] = 0 and T.ExecutionID = @executionID
	left join #RelevantLookupValues FLV on FLV.FieldTypeID = T.FieldTypeID and TRIM(T.FieldValue) = FLV.Text
	where T.FieldValue is not null and executionid = @executionid;

update	T
set		T.[Value] = '0'
from	#LookupValues T
		inner join FieldType ST on ST.ID = T.FieldTypeID and ST.AllowAllValue = 1 and ST.AllowAllLabel = T.FieldValue;

update	T
set		T.LookupValue = LV.Value
from	{fieldTable} T
inner join #LookupValues LV on LV.FieldValue = T.FieldValue and T.FieldTypeID = LV.FieldTypeID
where T.ExecutionId = @executionid;
", new { executionID }, commandTimeout: timeout, transaction: trans);
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

        private void ResolveColorValues(Guid executionID, int timeout = 3600)
        {
            Connection.Execute(@"

                        update  F
                        set     F.LookupValue = C.Id
                        from    api.ExecutionField F
                                left join Color C on C.Name = F.FieldValue
                        where   F.ExecutionID = @executionID and F.FieldName = 'Color' and SUBSTRING(F.FieldValue,1,1) <> '#'

                        update  F
                        set     F.LookupValue = F.FieldValue
                        from    api.ExecutionField F
                        where   F.ExecutionID = @executionID and F.FieldName = 'Color' and SUBSTRING(F.FieldValue,1,1) = '#'

                        update  F
                        set     F.LookupValue = null
                        from    api.ExecutionField F
                        where   F.ExecutionID = @executionID and F.FieldName = 'Color' and coalesce(F.FieldValue, '') = ''
                        
                        update  T 
                        set     T.Success = 0,
                                T.Message = coalesce(T.Message, '') + 'Color value is not a valid Govern color; '
                        from    api.ExecutionAsset T
                                inner join api.ExecutionField S on S.ExecutionID = T.ExecutionID and T.ExecutionID = @executionID and S.ItemNumber = T.ItemNumber and S.FieldName = 'Color' 
                        where   S.LookupValue is null and coalesce(S.FieldValue, '') <> ''
                        ", new { executionID }, commandTimeout: timeout);
        }

        private void SendWorkflowEvents(string objectType, int objectTypeID, IEnumerable<IWorkflowEnabledAsset> results, ChangeType? changeTypeOverride = null, List<AssetFieldTypeUpdate> fieldUpdates = null)
        {
            try
            {
                var events = new List<EventInfo>();

                Dictionary<string, int[]> fieldUpdatePairs = new Dictionary<string, int[]>();

                if (fieldUpdates == null) fieldUpdates = new List<AssetFieldTypeUpdate>();

                foreach (var item in fieldUpdates.GroupBy(x => x.Object + x.ObjectId))
                {
                    fieldUpdatePairs.Add(item.Key, item.Select(x => x.Id).ToArray());
                }


                foreach (var result in results)
                {
                    if (result.Success)
                    {
                        List<int> changedFieldsIDS = new List<int>();
                        if (fieldUpdatePairs.ContainsKey(result.Object + result.ObjectID))
                        {
                            changedFieldsIDS = fieldUpdatePairs[result.Object + result.ObjectID].ToList();
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

                        if (events.Count > WorkflowSendBatchSize)
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

        private void SendAssetGraphEvents(IEnumerable<IGraphAsset> results, Dictionary<Guid, List<string>> fields = null, bool delayedDelivery = false)
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

            if (events.Any())
                QueueSource.CreateTopicMessages(Config.GetValue<string>("AssetBusTopicName"), events, delayedDelivery ? new DateTime?(DateTime.UtcNow.AddSeconds(15)) : null);
        }

        public void SendGraphAssetTypeEvent(Guid assetTypeUid)
        {
            var e = new AssetEventInfo()
            {
                Uid = assetTypeUid,
                CompanyID = CurrentCompanyID,
                Type = AssetEventType.AssetType
            };

            QueueSource.CreateTopicMessage(Config.GetValue<string>("AssetBusTopicName"), e);
        }

        public void SendApiGraphEvent(ApiExecutionInfo info)
        {
            var e = new AssetEventInfo()
            {
                execution = info,
                CompanyID = CurrentCompanyID,
                Type = AssetEventType.Execution
            };

            QueueSource.CreateTopicMessage<AssetEventInfo>(Config.GetValue<string>("AssetBusTopicName"), e);
        }

        #region Validation

        public List<DataRow> ValidateFields(
            string ot, int otid, bool isInsert,
            List<FieldType> fieldTypes, List<string> requiredFieldTypeNames,
            Dictionary<string, string> fields, Guid executionID, int itemNumber,
            DataTable fieldTable, out bool success, out string errorMessage,
            bool useFriendlyNames = false,
            bool allowTagFields = false
            )
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

            var restrictedFieldTypes = DataType.Text.GetNotAllowedToUpdateViaAssetApi();
            if (allowTagFields)
            {
                restrictedFieldTypes = restrictedFieldTypes.Where(x => x != "Tag").ToList();
            }

            foreach (var k in fields)
            {
                string fieldName = k.Key.Trim();
                string fieldValue = (k.Value + "").Trim();
                int? fieldTypeId = null;
                string decimalFormatString = $"0.{string.Join("", Enumerable.Repeat("#", 18))}";


                // Validation of field and value;
                fieldType = fieldTypes.SingleOrDefault(f => f.Name == fieldName);
                if (useFriendlyNames)
                {
                    fieldName = fieldType.FriendlyName;
                }
                if (fieldType == null)
                {
                    if (fieldName.ToLower() == "color")
                    {
                        if (fieldValue.StartsWith("#") && fieldValue.Length != 7)
                        {
                            errorMessages.Add($"The Color field must be a seven character RGB code or the name of a Govern color");
                            success = false;
                        }
                    }
                    else if (ot == "FusionAttributeType")
                    {
                        if (fieldName != "FusionID" && fieldName != "Name" && fieldName != "SourceID")
                        {
                            success = false;
                            errorMessages.Add($"{fieldName} is not a valid field");
                        }
                    }
                    else if (ot == "ReferenceItemType")
                    {
                        switch (fieldName.ToLower())
                        {
                            case "code":
                                if ((fieldValue ?? "").Length > 250)
                                {
                                    errorMessages.Add($"The Code field must be 250 characters or less in length");
                                    success = false;
                                }
                                break;
                            case "icon":
                                if ((fieldValue ?? "").Length > 50 || !fieldValue.StartsWith("fa-"))
                                {
                                    errorMessages.Add($"The Icon field must be fifty characters or less in length and start with 'fa-'");
                                    success = false;
                                }
                                break;
                            case "referenceitemtypeid":
                                break;
                            default:
                                success = false;
                                errorMessages.Add($"{fieldName} is not a valid field");
                                break;
                        }
                    }
                    else if (ot == "RuleType")
                    {
                        if (fieldName != "Threshold" && fieldName != "Status" && fieldName != "Dimension")
                        {
                            success = false;
                            errorMessages.Add($"{fieldName} is not a valid field");
                        }
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

                    if (restrictedFieldTypes.Contains(fieldType.Type))
                    {
                        success = false;
                        errorMessages.Add($"{fieldName} is a {fieldType.Type} field and cannot be updated on this request");
                    }
                    else
                    {
                        if (string.IsNullOrEmpty(fieldValue))
                        {
                            if (fieldType.IsRequired)
                            {
                                success = false;
                                errorMessages.Add($"{fieldName} is a required field");
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
                                    if (success)
                                    {
                                        fieldValue = dTest.Date.ToString();
                                    }
                                    break;
                                case "DateTime":
                                    DateTime dtTest;
                                    if (!DateTime.TryParse(fieldValue, out dtTest) && !string.IsNullOrEmpty(fieldValue))
                                    {
                                        success = false;
                                        errorMessages.Add($"{fieldName} must be a valid datetime value");
                                    }
                                    if (success)
                                    {
                                        fieldValue = dtTest.ToString();
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
                                    if (fieldValue.Count(c => c == '|') != 1 && !string.IsNullOrEmpty(fieldValue) && !fieldValue.Equals('|'))
                                    {
                                        success = false;
                                        errorMessages.Add($"{fieldName} must be a valid link, using the format name|url");
                                    }
                                    break;
                                case "Lookup":
                                    break;
                                case "Number":
                                    if (!long.TryParse(fieldValue, out _) && !string.IsNullOrEmpty(fieldValue))
                                    {
                                        success = false;
                                        errorMessages.Add($"{fieldName} must be a valid whole number, greater than -9223372036854775808 and less than 9223372036854775807");
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
                                case "JSON":
                                    if (fieldValue.Length > 2500)
                                    {
                                        success = false;
                                        errorMessages.Add($"{fieldName} exceeds the maximum length of 2500 characters");
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
                }


                if (fieldTable != null)
                {
                    var fieldRow = fieldTable.NewRow();

                    fieldRow["ExecutionID"] = executionID;
                    fieldRow["ItemNumber"] = itemNumber;
                    fieldRow["FieldName"] = fieldName;
                    if (k.Value == null)
                        fieldRow["FieldValue"] = DBNull.Value;
                    else
                        fieldRow["FieldValue"] = fieldValue;
                    if (fieldTypeId.HasValue)
                        fieldRow["FieldTypeID"] = fieldTypeId.Value;

                    fieldRows.Add(fieldRow);    // Added temporarily, but may be invalidated based on success flag.
                }
            }

            if (errorMessages.Any())
            {
                errorMessage = string.Join(errorDelimiter, errorMessages);
                errorMessage += "."; //ending period
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

        public List<DatabaseBulkAssetResult> RemoveAssets(ApiExecution execution, AssetType at, AssetDeletes import, int timeout = 3600, bool sendWorkflowEvents = true, bool sendGraphEvents = true)
        {
            var results = new List<DatabaseBulkAssetResult>();
            var graphResults = new List<DatabaseBulkAssetResult>();
            var dt = DateTime.UtcNow;
            bool generalChecksCompleted = false;
            CurrentExecutionLocationModel currentLocation = null;

            SetApiExecutionProcessingStartTime(execution.ExecutionID);

            //check if trigger workflows is set to true and there are actually no workflows in which case shut off triggering of workflows
            sendWorkflowEvents = sendWorkflowEvents && TypeHasWorkflows(at.Object, at.ObjectID, ChangeType.Delete);

            var executionItemDupes = import.Where(i => i.ExecutionItemUid.HasValue && i.ExecutionItemUid.Value != Guid.Empty).GroupBy(i => i.ExecutionItemUid).Where(i => i.Count() > 1).Select(i => new { ExecutionItemUid = i.Key, Count = i.Count() }).ToList();
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
                            Connection.Open();

                        #region Bulk Copy

                        using (SqlBulkCopy bulkCopy = new SqlBulkCopy(Connection))
                        {

                            bulkCopy.BatchSize = SqlBulkBatchSize;
                            bulkCopy.DestinationTableName = "api.ExecutionDeletedAsset";
                            bulkCopy.BulkCopyTimeout = SqlBulkBatchTimeout;

                            bulkCopy.ColumnMappings.Add("ExecutionID", "ExecutionID");
                            bulkCopy.ColumnMappings.Add("ItemNumber", "ItemNumber");
                            bulkCopy.ColumnMappings.Add("ExecutionItemUid", "ExecutionItemUid");
                            bulkCopy.ColumnMappings.Add("Uid", "Uid");
                            bulkCopy.ColumnMappings.Add("Cascade", "Cascade");

                            bulkCopy.WriteToServer(table);
                        }

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
    where	ExecutionID = @ExecutionID and AssetID is null;


    --Check if asset Results exist 
    update	T
    set		T.Success = 0,
		    T.[Message] = coalesce([Message] + '; ', '') + 'You have not enabled Cascade, yet there are ' + cast(ARE.ResultCount as nvarchar) + ' results(s) present for this rule.'
    from    api.ExecutionDeletedAsset T
            inner join graph.AssetNode AN on AN.ID = T.AssetID
			inner join AssetType AT on AT.ID = AN.AssetTypeID and AT.Class = {(int)AssetTypeClass.Rule}
            cross apply (select count(1) as ResultCount from AssetResultEdge where $from_id = AN.$node_id) ARE
    where	T.ExecutionID = @ExecutionID
            and T.[Cascade] = 0
            and ARE.ResultCount > 0;",
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
    insert into api.ExecutionDeletedAsset ([ExecutionID],[ItemNumber],[Uid],[AssetID],[IntersectID],[FromHierarchy],[Object], [ObjectID])
        select  distinct 
                ExecutionID, 
                ItemNumber, 
                [Uid], 
                AssetID, 
                IntersectID, 
                1,
                Object,
                ObjectID
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
                                                case "RuleType":
                                                    legacyTable = "[Rule]";
                                                    break;
                                            }

                                            if (!string.IsNullOrEmpty(legacyTable))
                                            {
                                                Connection.Execute(
                                                    $"delete {legacyTable} where ID in (select S.ObjectID from api.ExecutionDeletedAsset S where {querySuffix})",
                                                    new { execution.ExecutionID, beginItemNumber, endItemNumber }, transaction: trans, commandTimeout: timeout);
                                            }

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

                            //include hierarchical records for graph tables
                            graphResults.AddRange(
                                Query<DatabaseBulkAssetResult>(
                                    $"select * from api.ExecutionDeletedAsset where ExecutionID = @ExecutionID and ItemNumber between @beginItemNumber and @endItemNumber",
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

                        if (sendGraphEvents)
                        {
                            SendAssetGraphEvents(graphResults);
                        }

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

            SetApiExecutionProcessingStartTime(execution.ExecutionID);


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
                            Connection.Open();

                        #region Bulk Copy

                        using (SqlBulkCopy bulkCopy = new SqlBulkCopy(Connection))
                        {

                            bulkCopy.BatchSize = SqlBulkBatchSize;
                            bulkCopy.DestinationTableName = "api.ExecutionDeletedAssetType";
                            bulkCopy.BulkCopyTimeout = SqlBulkBatchTimeout;

                            bulkCopy.ColumnMappings.Add("ExecutionID", "ExecutionID");
                            bulkCopy.ColumnMappings.Add("ItemNumber", "ItemNumber");
                            bulkCopy.ColumnMappings.Add("ExecutionItemUid", "ExecutionItemUid");
                            bulkCopy.ColumnMappings.Add("Uid", "Uid");
                            bulkCopy.ColumnMappings.Add("Cascade", "Cascade");

                            bulkCopy.WriteToServer(table);
                        }

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
            and A.ChildCount > 0;

    update	T
    set		T.Success = 0,
		    T.[Message] = coalesce([Message] + '; ', '') + 'There are ' + cast(A.ChildCount as nvarchar) + ' Organizations defined for this OrganizationType.'
    from    api.ExecutionDeletedAssetType T
            cross apply (
                select  count(1) as ChildCount
                from	Organization O
		        where O.OrganizationTypeID = T.ObjectID
            ) A 
    where	T.ExecutionID = @ExecutionID
            and T.Object = 'OrganizationType'
            and A.ChildCount > 0;

    --Check if asset Results exist 
    update	T
    set		T.Success = 0,
		    T.[Message] = coalesce([Message] + '; ', '') + 'You have not enabled Cascade, yet there are ' + cast(ARE.ResultCount as nvarchar) + ' results(s) present for this rule type.'
    from    api.ExecutionDeletedAssetType T
            inner join graph.AssetNode AN on AN.AssetTypeID = T.AssetTypeID
            inner join AssetType AT on AT.ID = AN.AssetTypeID and AT.Class = {(int)AssetTypeClass.Rule}
			cross apply (select count(1) as ResultCount from AssetResultEdge where $from_id = AN.$node_id) ARE
    where	T.ExecutionID = @ExecutionID
            and T.[Cascade] = 0
            and ARE.ResultCount > 0;",
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

        public List<RelationshipTypeResult> ImportRelationshipTypes(ApiExecution execution, IEnumerable<RelationshipTypeInsert> import, int timeout = 3600)
        {
            var results = new List<RelationshipTypeResult>();

            SetApiExecutionProcessingStartTime(execution.ExecutionID);

            var dupes = import.Where(i => i.ExecutionItemUid.HasValue && i.ExecutionItemUid.Value != Guid.Empty).GroupBy(i => i.ExecutionItemUid).Where(i => i.Count() > 1).Select(i => new { ExecutionItemUid = i.Key, Count = i.Count() }).ToList();
            if (dupes.Any())
            {
                execution.ErrorMessage = $"Duplicate execution item identifiers: {string.Join(", ", dupes.Select(i => i.ExecutionItemUid.ToString()))}. Identifiers must be unique within a batch.";
                results.AddRange(import.Select(i => new RelationshipTypeResult { ExecutionItemUid = i.ExecutionItemUid, Message = execution.ErrorMessage, Success = false }));
            }
            else
            {
                #region Build data tables for bulk load.

                var table = new DataTable();
                table.Columns.Add("ExecutionID", typeof(Guid));
                table.Columns.Add("ExecutionItemUid", typeof(Guid));
                table.Columns.Add("ItemNumber", typeof(int));
                table.Columns.Add("SubjectUid", typeof(Guid));
                table.Columns.Add("Subject", typeof(string));
                table.Columns.Add("SubjectID", typeof(int));
                table.Columns.Add("SubjectCardinality", typeof(int));
                table.Columns.Add("ObjectUid", typeof(Guid));
                table.Columns.Add("Object", typeof(string));
                table.Columns.Add("ObjectID", typeof(int));
                table.Columns.Add("ObjectCardinality", typeof(int));
                table.Columns.Add("PredicateUid", typeof(Guid));
                table.Columns.Add("PredicateID", typeof(int));
                table.Columns.Add("Message", typeof(string));
                table.Columns.Add("Success", typeof(bool));
                table.Columns.Add("IsNew", typeof(bool));
                table.Columns.Add("uid", typeof(Guid));

                int i = 0;
                foreach (var item in import)
                {
                    var row = table.NewRow();

                    row["ExecutionID"] = execution.ExecutionID;
                    row["ItemNumber"] = i++;
                    if (item.ExecutionItemUid.HasValue)
                        row["ExecutionItemUid"] = item.ExecutionItemUid.Value;
                    row["SubjectUid"] = item.SubjectUid;
                    row["SubjectCardinality"] = (int)item.SubjectCardinality;
                    row["ObjectUid"] = item.ObjectUid;
                    row["ObjectCardinality"] = (int)item.ObjectCardinality;
                    row["PredicateUid"] = item.PredicateUid;
                    row["IsNew"] = true;
                    if (item.Uid.HasValue)
                    {
                        row["uid"] = item.Uid.Value;
                    }


                    table.Rows.Add(row);
                }

                #endregion

                try
                {
                    if (Database.Connection.State != ConnectionState.Open)
                        Connection.Open();

                    #region Bulk Copy

                    using (var bulkCopy = new SqlBulkCopy(Connection)
                    {
                        BatchSize = SqlBulkBatchSize,
                        DestinationTableName = "api.ExecutionRelationshipType",
                        BulkCopyTimeout = SqlBulkBatchTimeout
                    })
                    {

                        bulkCopy.ColumnMappings.Add("ExecutionID", "ExecutionID");
                        bulkCopy.ColumnMappings.Add("ExecutionItemUid", "ExecutionItemUid");
                        bulkCopy.ColumnMappings.Add("ItemNumber", "ItemNumber");

                        bulkCopy.ColumnMappings.Add("SubjectUid", "SubjectUid");
                        bulkCopy.ColumnMappings.Add("SubjectCardinality", "SubjectCardinality");
                        bulkCopy.ColumnMappings.Add("ObjectUid", "ObjectUid");
                        bulkCopy.ColumnMappings.Add("ObjectCardinality", "ObjectCardinality");
                        bulkCopy.ColumnMappings.Add("PredicateUid", "PredicateUid");
                        bulkCopy.ColumnMappings.Add("IsNew", "IsNew");
                        bulkCopy.ColumnMappings.Add("uid", "uid");

                        bulkCopy.WriteToServer(table);
                    }

                    #endregion

                    this.ValidateRelationshipTypes(true, execution, timeout);

                    Connection.Execute(@"
update  api.ExecutionRelationshipType
set     [Uid] = Newid()
where   ExecutionID = @ExecutionID 
        and Success is null
        and ([Uid] is null or [Uid] = @emptyUid);

insert into [IntersectType] 
        (SubjectUid, [Subject], SubjectID, ObjectUid, [Object], ObjectID, PredicateID, SubjectCardinality, ObjectCardinality, CreatedBy, CreatedOn, UpdatedBy, UpdatedOn, [Uid])
select  SubjectUid, [Subject], SubjectID, 
        ObjectUid, [Object], ObjectID, 
        PredicateID, SubjectCardinality, ObjectCardinality,
        @resourceId, @utcNow, @resourceId, @utcNow, [Uid] 
from    api.ExecutionRelationshipType 
where   ExecutionID = @ExecutionID 
        and Success is null;

update  api.ExecutionRelationshipType
set     Success = 1,
        Message = 'Added Successfully'
where   ExecutionID = @ExecutionID 
        and Success is null; ",
                    new { execution.ExecutionID, resourceId = CurrentResourceID, utcNow = DateTime.UtcNow, emptyUid = Guid.Empty }, commandTimeout: timeout);

                    results = Query<RelationshipTypeResult>(
                                        $"select ExecutionItemUid,Uid,Message,Success from api.ExecutionRelationshipType where ExecutionID = @ExecutionID",
                                        new { execution.ExecutionID }
                                        ).ToList();
                }
                finally
                {
                    if (Database.Connection.State == ConnectionState.Open)
                        Connection.Close();
                }

            }
            return results;
        }

        public List<RelationshipTypeResult> ImportRelationshipTypes(ApiExecution execution, IEnumerable<RelationshipTypeUpdate> import, int timeout = 3600)
        {
            var results = new List<RelationshipTypeResult>();

            SetApiExecutionProcessingStartTime(execution.ExecutionID);


            var dupes = import.Where(i => i.ExecutionItemUid.HasValue && i.ExecutionItemUid.Value != Guid.Empty).GroupBy(i => i.ExecutionItemUid).Where(i => i.Count() > 1).Select(i => new { ExecutionItemUid = i.Key, Count = i.Count() }).ToList();
            if (dupes.Any())
            {
                execution.ErrorMessage = $"Duplicate execution item identifiers: {string.Join(", ", dupes.Select(i => i.ExecutionItemUid.ToString()))}. Identifiers must be unique within a batch.";
                results.AddRange(import.Select(i => new RelationshipTypeResult { ExecutionItemUid = i.ExecutionItemUid, Message = execution.ErrorMessage, Success = false }));
            }
            else
            {

                var uidDupes = import.GroupBy(i => i.Uid).Where(i => i.Count() > 1).Select(i => new { Uid = i.Key, Count = i.Count() }).ToList();
                if (uidDupes.Any())
                {
                    var dupesResult = uidDupes.Join(import,
                                        x => x.Uid,
                                        y => y.Uid,
                                        (d, i) => new { i.ExecutionItemUid, i.Uid, d.Count }).ToList();
                    results.AddRange(dupesResult.Select(i => new RelationshipTypeResult { ExecutionItemUid = i.ExecutionItemUid, uid = i.Uid, Message = $"Duplicate Uid", Success = false }));
                }
                else
                {
                    #region Build data tables for bulk load.
                    var table = new DataTable();
                    table.Columns.Add("ExecutionID", typeof(Guid));
                    table.Columns.Add("ExecutionItemUid", typeof(Guid));
                    table.Columns.Add("ItemNumber", typeof(int));
                    table.Columns.Add("SubjectUid", typeof(Guid));
                    table.Columns.Add("Subject", typeof(string));
                    table.Columns.Add("SubjectID", typeof(int));
                    table.Columns.Add("SubjectCardinality", typeof(int));
                    table.Columns.Add("ObjectUid", typeof(Guid));
                    table.Columns.Add("Object", typeof(string));
                    table.Columns.Add("ObjectID", typeof(int));
                    table.Columns.Add("ObjectCardinality", typeof(int));
                    table.Columns.Add("PredicateUid", typeof(Guid));
                    table.Columns.Add("PredicateID", typeof(int));
                    table.Columns.Add("Message", typeof(string));
                    table.Columns.Add("Success", typeof(bool));
                    table.Columns.Add("IsNew", typeof(bool));
                    table.Columns.Add("uid", typeof(Guid));

                    int i = 0;
                    foreach (var item in import)
                    {
                        var row = table.NewRow();

                        row["ExecutionID"] = execution.ExecutionID;
                        row["ItemNumber"] = i++;
                        if (item.ExecutionItemUid.HasValue)
                            row["ExecutionItemUid"] = item.ExecutionItemUid.Value;
                        row["SubjectCardinality"] = (int)item.SubjectCardinality;
                        row["ObjectCardinality"] = (int)item.ObjectCardinality;
                        row["PredicateUid"] = item.PredicateUid;
                        row["uid"] = item.Uid;
                        row["IsNew"] = false;

                        table.Rows.Add(row);
                    }

                    #endregion
                    try
                    {
                        if (Database.Connection.State != ConnectionState.Open)
                            Connection.Open();

                        #region Bulk Copy
                        using (var bulkCopy = new SqlBulkCopy(Connection)
                        {
                            BatchSize = SqlBulkBatchSize,
                            DestinationTableName = "api.ExecutionRelationshipType",
                            BulkCopyTimeout = SqlBulkBatchTimeout
                        })
                        {

                            bulkCopy.ColumnMappings.Add("ExecutionID", "ExecutionID");
                            bulkCopy.ColumnMappings.Add("ExecutionItemUid", "ExecutionItemUid");
                            bulkCopy.ColumnMappings.Add("ItemNumber", "ItemNumber");

                            bulkCopy.ColumnMappings.Add("SubjectCardinality", "SubjectCardinality");
                            bulkCopy.ColumnMappings.Add("ObjectCardinality", "ObjectCardinality");
                            bulkCopy.ColumnMappings.Add("PredicateUid", "PredicateUid");
                            bulkCopy.ColumnMappings.Add("IsNew", "IsNew");
                            bulkCopy.ColumnMappings.Add("uid", "uid");

                            bulkCopy.WriteToServer(table);
                        }

                        #endregion

                        this.ValidateRelationshipTypes(false, execution, timeout);

                        Connection.Execute(@"
                                Update IT
                                Set PredicateID=ER.PredicateID,
                                    SubjectCardinality=ER.SubjectCardinality, 
                                    ObjectCardinality=ER.ObjectCardinality,
                                    UpdatedBy=@resourceId,
                                    UpdatedOn=@utcNow
                                from [intersecttype] IT
                                inner join [api].[ExecutionRelationshipType] ER on IT.UID = ER.UID
                                where  ER.ExecutionID=@executionID and
                                ER.Success is null

                           
                                 Update api.ExecutionRelationshipType
                                Set Success =1,
                                Message ='Updated Successfully'
                                Where ExecutionID=@executionID and Success is null; ",
                                new { executionID = execution.ExecutionID, resourceId = CurrentResourceID, utcNow = DateTime.UtcNow }, commandTimeout: timeout);

                        results = Query<RelationshipTypeResult>(
                                            $"select ExecutionItemUid,Uid,Message,Success from api.ExecutionRelationshipType where ExecutionID = @ExecutionID",
                                            new { execution.ExecutionID }).ToList();
                    }
                    finally
                    {
                        if (Database.Connection.State == ConnectionState.Open)
                            Connection.Close();
                    }

                }
            }
            return results;
        }

        public List<RelationshipTypeResult> DeleteRelationshipTypes(ApiExecution execution, IEnumerable<RelationshipTypeDelete> import, int timeout = 3600)
        {
            var results = new List<RelationshipTypeResult>();

            SetApiExecutionProcessingStartTime(execution.ExecutionID);

            var dupes = import.Where(i => i.ExecutionItemUid.HasValue && i.ExecutionItemUid.Value != Guid.Empty).GroupBy(i => i.ExecutionItemUid).Where(i => i.Count() > 1).Select(i => new { ExecutionItemUid = i.Key, Count = i.Count() }).ToList();
            if (dupes.Any())
            {
                execution.ErrorMessage = $"Duplicate execution item identifiers: {string.Join(", ", dupes.Select(i => i.ExecutionItemUid.ToString()))}. Identifiers must be unique within a batch.";
                results.AddRange(import.Select(i => new RelationshipTypeResult { ExecutionItemUid = i.ExecutionItemUid, Message = execution.ErrorMessage, Success = false }));
            }
            else
            {
                #region Build data tables for bulk load.
                var table = new DataTable();
                table.Columns.Add("ExecutionID", typeof(Guid));
                table.Columns.Add("ExecutionItemUid", typeof(Guid));
                table.Columns.Add("ItemNumber", typeof(int));
                table.Columns.Add("uid", typeof(Guid));
                table.Columns.Add("Cascade", typeof(bool));
                table.Columns.Add("Message", typeof(string));
                table.Columns.Add("Success", typeof(bool));



                int i = 0;
                foreach (var item in import)
                {
                    var row = table.NewRow();

                    row["ExecutionID"] = execution.ExecutionID;
                    row["ItemNumber"] = i++;
                    if (item.ExecutionItemUid.HasValue)
                        row["ExecutionItemUid"] = item.ExecutionItemUid.Value;
                    row["uid"] = item.Uid;
                    row["Cascade"] = item.Cascade;
                    table.Rows.Add(row);
                }

                #endregion
                try
                {
                    if (Database.Connection.State != ConnectionState.Open)
                        Connection.Open();

                    #region Bulk Copy
                    using (var bulkCopy = new SqlBulkCopy(Connection)
                    {
                        BatchSize = SqlBulkBatchSize,
                        DestinationTableName = "api.ExecutionDeletedRelationshipType",
                        BulkCopyTimeout = SqlBulkBatchTimeout
                    })
                    {

                        bulkCopy.ColumnMappings.Add("ExecutionID", "ExecutionID");
                        bulkCopy.ColumnMappings.Add("ExecutionItemUid", "ExecutionItemUid");
                        bulkCopy.ColumnMappings.Add("ItemNumber", "ItemNumber");
                        bulkCopy.ColumnMappings.Add("Uid", "Uid");
                        bulkCopy.ColumnMappings.Add("Cascade", "Cascade");


                        bulkCopy.WriteToServer(table);
                    }

                    #endregion

                    this.ValidateDeleteRelationshipTypes(execution, timeout);

                    //Delete lookup fields
                    //First get the field type id
                    List<long> lookupFieldIdList = Connection.Query<long>($@"
                                            select	
                                               distinct FTL.FieldTypeID
                                            from
                                                FieldTypeLookup FTL
					                            cross apply OPENJSON(FTL.[Definition], N'lax $.Relations') with (
						                            IntersectTypeUid uniqueidentifier, 
						                            AssetTypeUid uniqueidentifier,
						                            RelationType int, 
						                            Direction int
					                            ) R
					                            inner join [IntersectType] IT on IT.uid = R.intersectTypeUid
					                            inner join [api].[ExecutionDeletedRelationshipType] EDR on EDR.Uid=IT.UID and 
					                                                                            EDR.ExecutionID = @ExecutionID 
					                                                                            and 
					                                                                            EDR.Success is null
                                            where ISJSON(FTL.[Definition])>0;",
                        new { execution.ExecutionID }, commandTimeout: timeout).ToList();

                    //delete the lookup
                    Connection.Execute($@"
                                    delete  T
                                    from    [FieldTypeLookup] T
                                    where T.FieldTypeID in @fieldtypeIdList",
                                    new { fieldtypeIdList = lookupFieldIdList.ToArray() }, commandTimeout: timeout);

                    //delete the fieldtype
                    Connection.Execute($@"
                                    delete  T
                                    from    [FieldType] T
                                    where T.ID in @fieldtypeIdList",
                                    new { fieldtypeIdList = lookupFieldIdList.ToArray() }, commandTimeout: timeout);


                    Connection.Execute(@"
                            
                                delete  T
                                from    [Field] T
                                        inner join (Select I.ID from [Intersect] I
                                        inner join [intersecttype] IST on
                                        I.intersecttypeid = IST.ID
                                        inner join api.ExecutionDeletedRelationshipType ER on ER.UID = IST.UID 
                                        where ER.ExecutionID = @ExecutionID 
                                        and ER.Success is null) S on T.ObjectType = 'Intersect' 
                                        and S.ID = T.ObjectID ;

                                delete FT
                                from    [FieldType] FT
                                        inner join (Select I.ID from [intersecttype] I
                                        inner join api.ExecutionDeletedRelationshipType ER on ER.UID = I.UID 
                                        where ER.ExecutionID = @ExecutionID 
                                        and ER.Success is null) S on FT.[Object] = 'IntersectType' 
                                        and S.ID = FT.ObjectID ;

                                delete FT
                                from FieldType FT 
                                        inner join 
                                        [IntersectType] IT on FT.LookupObjectID = IT.ID and FT.Type='Relationship'
                                        inner join [api].[ExecutionDeletedRelationshipType] EDR on EDR.UID=IT.UID and EDR.ExecutionID = @ExecutionID
                                        and 
					                    EDR.Success is null                                

                                delete FT
                                from FieldType FT 
                                        inner join 
                                        [IntersectType] IT on FT.LookupObjectID = IT.ID and FT.Type='RefListRelationship'
                                        inner join [api].[ExecutionDeletedRelationshipType] EDR on EDR.UID=IT.UID and EDR.ExecutionID = @ExecutionID
                                        and 
					                    EDR.Success is null

                                delete FT
                                from FieldType FT 
                                        inner join 
                                        [IntersectType] IT on FT.LookupObjectID = IT.ID and FT.Type='FieldFromRelationship'
                                        inner join [api].[ExecutionDeletedRelationshipType] EDR on EDR.UID=IT.UID and EDR.ExecutionID = @ExecutionID
                                        and 
					                    EDR.Success is null

                            delete  T
                            from    [Intersect] T
                                    inner join [intersecttype] I on
                                    T.intersecttypeid = I.ID
                                    inner join api.ExecutionDeletedRelationshipType ER on ER.UID = I.UID 
                                    where ER.ExecutionID = @ExecutionID 
                                    and ER.Success is null;

                           delete  I
                            from    [intersecttype] I
                                    inner join api.ExecutionDeletedRelationshipType ER on ER.UID = I.UID 
                                    where ER.ExecutionID = @ExecutionID 
                                    and ER.Success is null;

                             Update api.ExecutionDeletedRelationshipType
                            Set Success =1,
                            Message ='Deleted Successfully'
                            Where ExecutionID=@executionID and Success is null; ",
                            new { executionID = execution.ExecutionID, resourceId = CurrentResourceID, utcNow = DateTime.UtcNow }, commandTimeout: timeout);

                    results = Query<RelationshipTypeResult>(
                                        $"select ExecutionItemUid,Uid,Message,Success from api.ExecutionDeletedRelationshipType where ExecutionID = @ExecutionID",
                                        new { ExecutionID = execution.ExecutionID }).ToList();
                }
                finally
                {
                    if (Database.Connection.State == ConnectionState.Open)
                        Connection.Close();
                }
            }
            return results;
        }

        private void AddMeasurement(Dictionary<string,double> metrics, string key, double value, int stepNumber)
        {
            metrics[$"{stepNumber}-{key}"] = value;
        }

        private void AITrackMetric(TelemetryClient client, ApiExecution execution, string methodName, Dictionary<string,double> metrics, bool isLog)
        {            
            if (!isLog) return;

            var propsToSend = new Dictionary<string, string> {
                { "MethodName", methodName },
                { "CompanyID", this.CurrentCompanyID.ToString() },
                { "ExecutionID", execution.ExecutionID.ToString() }
            };

            client.TrackEvent($"API v2 Execution ID[{execution.ExecutionID}]", propsToSend, metrics);
        }

        public List<DatabaseBulkAssetResult> ImportAssets(ApiExecution execution, AssetType at, IEnumerable<IAssetUpsert> import, bool isInsert, int timeout = 3600, bool fieldJsonPropertyLoadLimitToTopLevel = true, bool sendWorkflowEvents = true, bool lookupFieldsPassedByValue = false, int mergeBlockSize = 500, bool sendGraphEvents = true)
        {
            var swBegin = Stopwatch.StartNew();
            TelemetryClient client = new TelemetryClient();
            const string METHOD_NAME = "ImportAssets";
            bool isLog = true; // trace info for all assets is extermely useful
            var results = new List<DatabaseBulkAssetResult>();
            var importFields = new Dictionary<int, List<string>>();
            var metrics = new Dictionary<string, double>();
            var step = 0;
            bool hasDuplicateUids = false;

            SetApiExecutionProcessingStartTime(execution.ExecutionID);


            // duplicate items in load checks is only applicable if there is > 1 item
            if (import.Count() > 1)
            {
                var sw = Stopwatch.StartNew();

                var dupes = import.Where(i => i.ExecutionItemUid.HasValue).GroupBy(i => i.ExecutionItemUid).Where(i => i.Count() > 1).Select(i => new { ExecutionItemUid = i.Key, Count = i.Count() }).ToList();
                if (dupes.Any())
                {
                    execution.ErrorMessage = $"Duplicate execution item identifiers: {string.Join(", ", dupes.Select(i => i.ExecutionItemUid.ToString()))}. Identifiers must be unique within a batch.";
                    results.AddRange(import.Select(i => new DatabaseBulkAssetResult { ExecutionItemUid = i.ExecutionItemUid, uid = i.Uid, Message = execution.ErrorMessage, Success = false }));

                    hasDuplicateUids = true;
                }

                // check for duplicated asset uids if its a put.  
                if (!isInsert)
                {
                    var uidDupes = import.GroupBy(i => i.Uid).Where(i => i.Count() > 1).Select(i => new { Uid = i.Key, Count = i.Count() }).ToList();

                    if (uidDupes.Any())
                    {
                        execution.ErrorMessage = $"Duplicate Asset Uids: {string.Join(", ", uidDupes.Select(i => i.Uid.ToString()))}. Identifiers must be unique within a batch.";
                        results.AddRange(import.Select(i => new DatabaseBulkAssetResult { ExecutionItemUid = i.ExecutionItemUid, uid = i.Uid, Message = execution.ErrorMessage, Success = false }));

                        hasDuplicateUids = true;
                    }
                }

                AddMeasurement(metrics, "Checks for duplicate uids in load", sw.ElapsedMilliseconds, ++step);

                sw.Restart();
            }

            // Only start processing if the duplication checks have passed
            if(!hasDuplicateUids)
            {
                var sw = Stopwatch.StartNew();

                //check if trigger workflows is set to true and there are actually no workflows
                sendWorkflowEvents = sendWorkflowEvents && TypeHasWorkflows(at.Object, at.ObjectID, isInsert ? ChangeType.Add : ChangeType.Update);

                AddMeasurement(metrics, "Check for workflows", sw.ElapsedMilliseconds, ++step);

                sw.Restart();

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
                bool hasRelationshipFieldTypes = false;
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

                    AddMeasurement(metrics, "BuildDatatable and initialization", sw.ElapsedMilliseconds, ++step);

                    sw.Restart();

                    // Get field types.
                    fieldTypes = Query<FieldType>("select * from FieldType where Object = @Object and ObjectID = @ObjectID", new { at.Object, at.ObjectID }).ToList();
                    jsonFieldTypes = fieldTypes.Where(f => f.Type == DataType.JSON.ToString()).ToList();
                    requiredFieldTypeNames = fieldTypes.Where(f => f.IsRequired && string.IsNullOrEmpty(f.DefaultValue)).Select(f => f.Name).ToList();
                    hasLookupFieldTypes = fieldTypes.Any(f => f.Type == DataType.Lookup.ToString());
                    hasRelationshipFieldTypes = fieldTypes.Any(f => f.Type == DataType.Relationship.ToString());
                    AddMeasurement(metrics, "Get field types", sw.ElapsedMilliseconds, ++step);
                    sw.Restart();

                    #region Generate data sets

                    if (predicateType.HasValue)
                    {
                        it = Database.Connection.QueryFirstOrDefault<IntersectType>("select i.[Subject],i.[SubjectID],i.[uid],i.ID from [dbo].[intersecttype] i inner join [predicate] p on (i.predicateid = p.id) where i.[Object] = @obj and i.[ObjectID] = @objID and p.[Type] = @predicate", new { obj = at.Object, objID = at.ObjectID, predicate = predicateType });
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
                    AddMeasurement(metrics, "Get predicateType.HasValue", sw.ElapsedMilliseconds, ++step);
                    sw.Restart();
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

                            if (success && at.Object == "RuleType")
                            {
                                // Check to ensure Threshold is present.
                                success = model.Fields.ContainsKey("Threshold");
                                if (!success)
                                {
                                    errorMessage = "Asset is missing a required Threshold field value";
                                }
                                else if (decimal.TryParse(model.Fields["Threshold"], out decimal threshold)) //Check threshold is a number
                                {
                                    if (!(threshold > 0 && threshold <= 1)) //check threshold is between 0 and 1
                                    {
                                        errorMessage = "Threshold value must be between 0 and 1";
                                        success = false;
                                    }
                                    else if (decimal.Round(threshold, 3) != threshold) //check threshold has a max of 3 decimal places
                                    {
                                        errorMessage = "Threshold value cannot exceed 3 decimal places.";
                                        success = false;
                                    }
                                }
                                else
                                {
                                    errorMessage = "Threshold value is not a valid number";
                                    success = false;
                                }
                            }

                            if (success)
                            {
                                importFields.Add(i, model.Fields.Keys.ToList());
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

                    AddMeasurement(metrics, "ValidateFields", sw.ElapsedMilliseconds, ++step);

                    sw.Restart();

                    #endregion

                    if (results.Count > 0) // There are errors already processed.
                    {
                        OnAssetsPartiallyProcessed(new AssetsPartiallyProcessedEventArgs
                        {
                            Results = results
                        });
                    }

                    if (Database.Connection.State != ConnectionState.Open)
                        Connection.Open();

                    #region Bulk Copy


                        using (var transaction = Connection.BeginTransaction())
                        {
                            try
                            {
                                using (SqlBulkCopy bulkCopy = new SqlBulkCopy((SqlConnection)Database.Connection, SqlBulkCopyOptions.Default, transaction))
                                {
                                    // assets
                                    bulkCopy.BatchSize = SqlBulkBatchSize;
                                    bulkCopy.DestinationTableName = "api.ExecutionAsset";
                                    bulkCopy.BulkCopyTimeout = SqlBulkBatchTimeout;

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
                                }

                                if (errorTable.Rows.Count > 0)
                                {
                                    using (SqlBulkCopy bulkCopy = new SqlBulkCopy((SqlConnection)Database.Connection, SqlBulkCopyOptions.Default, transaction))
                                    {
                                        // asset errors
                                        bulkCopy.BatchSize = SqlBulkBatchSize;
                                        bulkCopy.DestinationTableName = "api.ExecutionAssetError";
                                        bulkCopy.BulkCopyTimeout = SqlBulkBatchTimeout;

                                        bulkCopy.ColumnMappings.Add("ExecutionID", "ExecutionID");
                                        bulkCopy.ColumnMappings.Add("ItemNumber", "ItemNumber");
                                        bulkCopy.ColumnMappings.Add("ExecutionItemUid", "ExecutionItemUid");
                                        bulkCopy.ColumnMappings.Add("Uid", "Uid");
                                        bulkCopy.ColumnMappings.Add("Message", "Message");

                                        bulkCopy.WriteToServer(errorTable);
                                    }
                                }

                                using (SqlBulkCopy bulkCopy = new SqlBulkCopy((SqlConnection)Database.Connection, SqlBulkCopyOptions.Default, transaction))
                                {
                                    // fields
                                    bulkCopy.BatchSize = SqlBulkBatchSize;
                                    bulkCopy.DestinationTableName = "api.ExecutionField";
                                    bulkCopy.BulkCopyTimeout = SqlBulkBatchTimeout;

                                    bulkCopy.ColumnMappings.Add("ExecutionID", "ExecutionID");
                                    bulkCopy.ColumnMappings.Add("ItemNumber", "ItemNumber");
                                    bulkCopy.ColumnMappings.Add("FieldName", "FieldName");
                                    bulkCopy.ColumnMappings.Add("FieldValue", "FieldValue");
                                    bulkCopy.ColumnMappings.Add("FieldTypeID", "FieldTypeID");

                                    bulkCopy.WriteToServer(fieldTable);

                                    AddMeasurement(metrics, "BulkCopy to api.Execution table", sw.ElapsedMilliseconds, ++step);
                                }
                                transaction.Commit();

                            }
                            catch (Exception ex)
                            {
                                if (transaction != null)
                                    transaction.Rollback();

                                throw ex;
                            }
                        }
                        
                        sw.Restart();
                        #endregion


                    ResolveColorValues(execution.ExecutionID, timeout);
                    AddMeasurement(metrics, "ResolveColorValues", sw.ElapsedMilliseconds, ++step);
                    sw.Restart();

                    if (hasLookupFieldTypes)
                    {
                        if (lookupFieldsPassedByValue)
                        {
                            CopyFieldLookupValuesAsIs(execution.ExecutionID, timeout);
                            AddMeasurement(metrics, "CopyFieldLookupValuesAsIs", sw.ElapsedMilliseconds, ++step);
                            sw.Restart();
                        }
                        else
                        {
                            ResolveFieldLookupValues(execution.ExecutionID, "api.ExecutionField", timeout);
                            AddMeasurement(metrics, "ResolveFieldLookupValues", sw.ElapsedMilliseconds, ++step);
                            sw.Restart();
                        }
                    }

                    if (at.Class == AssetTypeClass.Rule)
                    {
                        ResolveRuleTypeLookupValues(execution.ExecutionID, timeout);
                        AddMeasurement(metrics, "ResolveRuleTypeLookupValues", sw.ElapsedMilliseconds, ++step);
                        sw.Restart();
                    }

                    if (hasLookupFieldTypes)
                    {
                        LogFieldLookupErrors(execution.ExecutionID, at.Object, at.ObjectID, "Asset", timeout);
                    }

                    LogRelationshipErrors(execution.ExecutionID, at.Object, at.ObjectID, "Asset", timeout, lookupFieldsPassedByValue);
                    ValidateAssetAndParent(execution.ExecutionID, at.ID, timeout);

                        // If you cannot find parent based on Uids provided.
                        // special case is intratype hierarchy if guid.empty we need to allow this so we later know which items to remove the relationships from
                        LogParentErrors(execution.ExecutionID, timeout, predicateType == PredicateType.IntraTypeHierarchy);

                    if (!isInsert)
                    {
                        LogAssetErrors(execution.ExecutionID, timeout);             // If you cannot find asset based on Uids provided.
                        LoadMissingKeyFields(execution.ExecutionID, at, timeout);   // Get missing key fields if this is an update.
                        LogNullIsRequiredFields(execution.ExecutionID, timeout);    // Get IsRequired Field having Null value if this is an update.
                    }

                    //Policy/Model Check maximum hierarchy maximum level allowed 

                    if (at.Class == AssetTypeClass.Policy || at.Class == AssetTypeClass.Model)
                    {
                        LogPolicyHierMaxLimitErrors(execution.ExecutionID, isInsert, intersectTypeID, at.HierarchyMaximumDepth,  timeout);
                    }


                    AddMeasurement(metrics, "Log Errors", sw.ElapsedMilliseconds, ++step);
                    sw.Restart();

                    #region Generate proposed key hash and compare against existing data.


                    if (at.Object == "FusionAttributeType")
                    {
                        LogErrorsWhereChildFusionConfigDifferentFromParent(execution.ExecutionID);
                        LogInvalidFusionIDFields(execution.ExecutionID);
                    }

                    CalculateProposedKeyHashes(at, execution.ExecutionID, timeout, intersectTypeID);

                    #endregion

                    #region Invalidate repetitious items in load

                        // dont be a tool and look for duplicates in a load of 1 item
                        if (execution.Total > 1)
                        {

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

                            AddMeasurement(metrics, "Invalidate repetitious items in load", sw.ElapsedMilliseconds, ++step);
                        }
                        
                        sw.Restart();
                        #endregion

                    // Validate permissions
                    LogAssetPermissionErrors(execution.ExecutionID, at, Permission.ModifyAsset, "ExecutionAsset");
                    LogAssetPermissionErrors(execution.ExecutionID, at, Permission.ModifyAsset, isInsert, "ExecutionAsset");
                    AddMeasurement(metrics, "LogAssetPermissionErrors -  Permission.ModifyAsset- ExecutionAsset", sw.ElapsedMilliseconds, ++step);
                    sw.Restart();

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
                sw.Restart();
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
                                                AddMeasurement(metrics, $"AssetTypeClass.FusionAttribute >> api.ExecutionAsset >> {currentLoop}", sw.ElapsedMilliseconds, ++step);
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
                                                AddMeasurement(metrics, $"AssetTypeClass.FusionAttribute >> api.ExecutionAsset >> Names {currentLoop}", sw.ElapsedMilliseconds, ++step);
                                            }

                                            #region Recalculate the text paths
                                            sw.Restart();
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
                                            AddMeasurement(metrics, $"AssetTypeClass.FusionAttribute >> api.ExecutionAsset >> Textpaths {currentLoop}", sw.ElapsedMilliseconds, ++step);
                                            #endregion

                                                break;
                                            #endregion
                                            case AssetTypeClass.Policy:
                                            case AssetTypeClass.BusinessAsset:
                                            case AssetTypeClass.TechnicalAsset:
                                            case AssetTypeClass.Diagram:
                                            case AssetTypeClass.Model:
                                                #region
                                                string @object = "Artifact";
                                                if (at.Class == AssetTypeClass.Policy)
                                                    @object = "Policy";
                                                if (at.Class == AssetTypeClass.Diagram)
                                                    @object = "Task";
                                                if (at.Class == AssetTypeClass.Model)
                                                    @object = "Taxonomy";

                                            sw.Restart();
                                            if (isInsert)
                                            {
                                                Connection.Execute($@"
    create table #ObjectMergeTableResult (ID int, ItemNumber int, [Operation] varchar(10));
    CREATE NONCLUSTERED INDEX IX_TempObjectMergeTableResult ON #ObjectMergeTableResult ( ItemNumber ASC );

    merge   [Asset] as T
    using   (
            select  A.ItemNumber,
                    CR.LookupValue as Color
            from    api.ExecutionAsset A
                    left join api.ExecutionField CR on CR.ExecutionID = A.ExecutionID and CR.ItemNumber = A.ItemNumber and CR.FieldName = 'Color' 
            where   A.ExecutionID = @ExecutionID
                    and A.Success is null
                    and A.ItemNumber between @beginItemNumber and @endItemNumber
            ) S
    on      (T.AssetTypeID = @AssetTypeID and T.SourceID = @NonExistentUid)
    when    not matched then
    insert  (AssetTypeID,State,[Object], CreatedBy, CreatedOn, UpdatedBy, UpdatedOn, Color)
    values  (@AssetTypeID,1,@Object, @R, @D, @R, @D, S.Color)
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
                                                    AddMeasurement(metrics, $"AssetTypeClass.{@object} >> api.ExecutionAsset >> {currentLoop}", sw.ElapsedMilliseconds, ++step);                                                    
                                                }
                                                else
                                                {
                                                    Connection.Execute($@"
    update	T
    set		T.UpdatedBy = @R,
		    T.UpdatedOn = @D,
            T.Color = case when CR.ExecutionID is not null then CR.LookupValue else T.Color end
    from	[Asset] T
		    inner join api.ExecutionAsset S on S.ObjectID = T.ObjectID and T.[Object] = @Object and {executionAssetWhereSql}
            left join api.ExecutionField CR on CR.ExecutionID = S.ExecutionID and CR.ItemNumber = S.ItemNumber and CR.FieldName = 'Color' 


    update	api.ExecutionAsset
    set		IsNew = 0
    where	{executionAssetWhereSql};",
                                                new { execution.ExecutionID, R = CurrentResourceID, D = DateTime.UtcNow, @object, beginItemNumber, endItemNumber }, transaction: trans, commandTimeout: timeout);
                                                AddMeasurement(metrics, $"AssetTypeClass.Policy - BusinessAsset >> TechnicalAsset >> api.ExecutionAsset >> {currentLoop}", sw.ElapsedMilliseconds, ++step);
                                            }
                                            break;
                                        #endregion
                                        case AssetTypeClass.Rule:
                                            #region
                                            sw.Restart();
                                            if (isInsert)
                                            {
                                                Connection.Execute($@"
    create table #ObjectMergeTableResult (ID int, ItemNumber int,[Operation] varchar(10));
    CREATE NONCLUSTERED INDEX IX_TempObjectMergeTableResult ON #ObjectMergeTableResult ( ItemNumber ASC );

    merge   [Rule] as T
    using   (
            select  A.ItemNumber,
                    T.FieldValue as Threshold,
                    CR.LookupValue as Color
            from    api.ExecutionAsset A
                    inner join api.ExecutionField T on T.ExecutionID = A.ExecutionID and T.ItemNumber = A.ItemNumber and T.FieldName = 'Threshold'
                    left join api.ExecutionField CR on CR.ExecutionID = A.ExecutionID and CR.ItemNumber = A.ItemNumber and CR.FieldName = 'Color' 
            where   A.ExecutionID = @ExecutionID
                    and A.Success is null
                    and A.ItemNumber between @beginItemNumber and @endItemNumber
            ) S
    on      (T.RuleTypeID = @ObjectID and T.SourceID = @NonExistentUid)
    when    not matched then
    insert  (RuleTypeID, Threshold, CreatedBy, CreatedOn, UpdatedBy, UpdatedOn, Color)
    values  (@ObjectID, S.Threshold, @R, @D, @R, @D, S.Color)
    output  inserted.ID, S.ItemNumber, $action into #ObjectMergeTableResult;

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
                                                AddMeasurement(metrics, $"AssetTypeClass.Rule >> api.ExecutionAsset >> {currentLoop}", sw.ElapsedMilliseconds, ++step);
                                            }
                                            else
                                            {
                                                Connection.Execute($@"
    update	T
    set		
            T.Threshold = case when FD.FieldValue is not null then FD.FieldValue else T.Threshold end,
            T.Color = case when CR.ExecutionID is not null then CR.LookupValue else T.Color end,
            T.UpdatedBy = @R,
		    T.UpdatedOn = @D
    from	[Rule] T
		    inner join api.ExecutionAsset S on S.ObjectID = T.ID and {executionAssetWhereSql}
            left join api.ExecutionField FD on FD.ExecutionID = S.ExecutionID and FD.ItemNumber = S.ItemNumber and FD.FieldName = 'Threshold'
            left join api.ExecutionField CR on CR.ExecutionID = S.ExecutionID and CR.ItemNumber = S.ItemNumber and CR.FieldName = 'Color' 

    update	api.ExecutionAsset
    set		IsNew = 0
    where	{executionAssetWhereSql};",
                                                new { execution.ExecutionID, R = CurrentResourceID, D = DateTime.UtcNow, beginItemNumber, endItemNumber }, transaction: trans, commandTimeout: timeout);
                                                AddMeasurement(metrics, $"AssetTypeClass.Rule >> api.ExecutionAsset >> {currentLoop}", sw.ElapsedMilliseconds, ++step);
                                            }
                                            break;
                                        #endregion
                                        case AssetTypeClass.Reference:
                                            #region
                                            sw.Restart();
                                            if (isInsert)
                                            {
                                                Connection.Execute($@"
                                                        create table #ObjectMergeTableResult (ID int, ItemNumber int, [Operation] varchar(10));
                                                        CREATE NONCLUSTERED INDEX IX_TempObjectMergeTableResult ON #ObjectMergeTableResult ( ItemNumber ASC );

                                                        merge   [Asset] as T
                                                        using   (
                                                                select  A.ItemNumber,
                                                                        C.FieldValue as [Code],
                                                                        CR.LookupValue as [Color],
                                                                        I.FieldValue as [Icon]
                                                                from    api.ExecutionAsset A
                                                                        inner join api.ExecutionField C on C.ExecutionID = A.ExecutionID and C.ItemNumber = A.ItemNumber and C.FieldName = 'Code' 
                                                                        left join api.ExecutionField CR on CR.ExecutionID = A.ExecutionID and CR.ItemNumber = A.ItemNumber and CR.FieldName = 'Color' 
                                                                        left join api.ExecutionField I on I.ExecutionID = A.ExecutionID and I.ItemNumber = A.ItemNumber and I.FieldName = 'Icon' 
                                                                where   A.ExecutionID = @ExecutionID
                                                                        and A.Success is null
                                                                        and A.ItemNumber between @beginItemNumber and @endItemNumber
                                                                ) S
                                                        on      (T.AssetTypeID = @AssetTypeID and T.[Code] = @NonExistentUid)
                                                        when    not matched then
                                                        insert  (AssetTypeID,State,[Object], [Code], [Color], [Icon], CreatedBy, CreatedOn, UpdatedBy, UpdatedOn)
                                                        values  (@AssetTypeID,1,'ReferenceItem', S.[Code], S.[Color], S.[Icon], @R, @D, @R, @D)
                                                        output  inserted.ObjectID, S.ItemNumber, $action into #ObjectMergeTableResult;

                                                        update  T
                                                        set     T.Object = 'ReferenceItem',
                                                                T.ObjectID = S.ID,
                                                                T.IsNew = 1
                                                        from    api.ExecutionAsset T
                                                                inner join #ObjectMergeTableResult S on T.Executionid = @ExecutionID and S.ItemNumber = T.ItemNumber;

                                                        {updateAssetInfoOnExecutionRecordsSql}",
                                                new { beginItemNumber, endItemNumber, execution.ExecutionID, R = CurrentResourceID, D = DateTime.UtcNow, at.ObjectID, AssetTypeID = at.ID, NonExistentUid = Guid.NewGuid().ToString() }, transaction: trans, commandTimeout: timeout);
                                                AddMeasurement(metrics, $"AssetTypeClass.Reference >> api.ExecutionAsset >> {currentLoop}", sw.ElapsedMilliseconds, ++step);
                                            }
                                            else
                                            {
                                                Connection.Execute($@"
                                                        update	T
                                                        set		T.[Code] = C.FieldValue,
                                                                T.[Color] = case when CR.ExecutionID is not null then CR.LookupValue else T.Color end,
                                                                T.[Icon] = I.FieldValue,
                                                                T.UpdatedBy = @R,
                                                                T.UpdatedOn = @D
                                                        from	Asset T
		                                                        inner join api.ExecutionAsset S on S.ObjectID = T.ObjectID and S.[Object]=T.[Object] and T.[Object]='ReferenceItem'  and S.ExecutionID = @ExecutionID and S.Success is null and S.ItemNumber between @beginItemNumber and @endItemNumber
                                                                inner join api.ExecutionField C on C.ExecutionID = S.ExecutionID and C.ItemNumber = S.ItemNumber and C.FieldName = 'Code'
                                                                left join api.ExecutionField CR on CR.ExecutionID = S.ExecutionID and CR.ItemNumber = S.ItemNumber and CR.FieldName = 'Color' 
                                                                left join api.ExecutionField I on I.ExecutionID = S.ExecutionID and I.ItemNumber = S.ItemNumber and I.FieldName = 'Icon';

                                                        update	api.ExecutionAsset
                                                        set		IsNew = 0
                                                        where	{executionAssetWhereSql};",
                                                new { execution.ExecutionID, R = CurrentResourceID, D = DateTime.UtcNow, beginItemNumber, endItemNumber }, transaction: trans, commandTimeout: timeout);
                                                AddMeasurement(metrics, $"AssetTypeClass.Reference >> api.ExecutionAsset >> {currentLoop}", sw.ElapsedMilliseconds, ++step);
                                            }
                                            break;
                                            #endregion

                                    }

                                    #region Parent/Child Relationship
                                    sw.Restart();
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
                                            AddMeasurement(metrics, $"Parent/Child Relationship >> graph.AssetEdge >> {currentLoop}", sw.ElapsedMilliseconds, ++step);


                                            // if its an intra type hierarchy models or policies and NOT an insert its possible that parent child relations are being removed IE an item moved to root
                                            if (predicateType == PredicateType.IntraTypeHierarchy && !isInsert)
                                            {
                                                sw.Restart();

                                                Connection.Execute($@"
drop table if exists #DeletedRelationships;
create table #DeletedRelationships([ID] int);

delete i output deleted.ID into #DeletedRelationships from [intersect] i inner join  api.ExecutionAsset  ea on (ea.IntersectTypeID = i.intersecttypeid and ea.object = i.object and ea.objectid = i.objectid and ea.ParentUid = '00000000-0000-0000-0000-000000000000')
    where ea.executionid = @executionid and ea.success is null and ea.ItemNumber between @beginItemNumber and @endItemNumber and ea.IntersectTypeID is not null

delete from graph.AssetEdge where ID in (select ID from #DeletedRelationships);
	",
new { beginItemNumber, endItemNumber, execution.ExecutionID, R = CurrentResourceID, D = DateTime.UtcNow }, transaction: trans, commandTimeout: timeout);


                                                AddMeasurement(metrics, $"Parent/Child Delete Relationship >> graph.AssetEdge >> {currentLoop}", sw.ElapsedMilliseconds, ++step);
                                            }
                                        }

                                    #endregion
                                    sw.Restart();
                                    var transationFieldUpdates = MergeFields(execution.ExecutionID, trans, "api.ExecutionAsset", "A.Object", "A.ObjectID", beginItemNumber, endItemNumber, sendWorkflowEvents, timeout, !isInsert);
                                    AddMeasurement(metrics, $"MergeFields >> {currentLoop}", sw.ElapsedMilliseconds, ++step);
                                    sw.Restart();

                                    if (hasRelationshipFieldTypes)
                                    {
                                        ImportRelationships(execution.ExecutionID, trans, "api.ExecutionAsset", "A.Object", "A.ObjectID", beginItemNumber, endItemNumber, timeout, lookupFieldsPassedByValue);
                                        AddMeasurement(metrics, $"ImportRelationships >> {currentLoop}", sw.ElapsedMilliseconds, ++step);
                                    }

                                    if (jsonFieldTypes.Count > 0)
                                    {
                                        sw.Restart();
                                        MergeJsonFieldProperties(execution.ExecutionID, trans, jsonFieldTypes, "api.ExecutionAsset", "A.Object", "A.ObjectID", beginItemNumber, endItemNumber, timeout, fieldJsonPropertyLoadLimitToTopLevel, metrics, step);
                                        AddMeasurement(metrics, $"MergeJsonFieldProperties >> {currentLoop}", sw.ElapsedMilliseconds, ++step);
                                    }

                                    // Must execute BEFORE the Success flag is updated below.
                                    sw.Restart();
                                    MergeAssetDisplayValues(execution.ExecutionID, trans, beginItemNumber, endItemNumber, timeout);
                                    AddMeasurement(metrics, $"MergeAssetDisplayValues >> {currentLoop}", sw.ElapsedMilliseconds, ++step);

                                    //Delete all field without value ONLY do this if there are lookup fields AND this is an update.
                                    if (hasLookupFieldTypes && !isInsert)
                                    {
                                        sw.Restart();
                                        DeleteEmptyAssetListFieldByApiExecutionUid(execution.ExecutionID, trans, beginItemNumber, endItemNumber, timeout);
                                        AddMeasurement(metrics, $"DeleteEmptyAssetListFieldByApiExecutionUid >> {currentLoop}", sw.ElapsedMilliseconds, ++step);
                                    }

                                    sw.Restart();
                                    // Update success flag.
                                    Connection.Execute(
                                        $@"update api.ExecutionAsset set Success = 1 where {executionAssetWhereSql} and Object is not null and ObjectID is not null;",
                                        new { execution.ExecutionID, beginItemNumber, endItemNumber }, transaction: trans, commandTimeout: timeout);
                                    metrics.Add($"{++step} Update success flag", sw.ElapsedMilliseconds);
                                    trans.Commit();

                                    //Add items after commit, so we dont have dirty data if trans is rolled back
                                    if (transationFieldUpdates != null && transationFieldUpdates.Count > 0)
                                    {
                                        fieldTypeUpdates.AddRange(transationFieldUpdates);
                                    }
                                    runCompleted = true;
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

                                    retryCount++;

                                    if (retryCount > API_V2_RETRY_LIMIT)
                                    {
                                        LogLoopExecutionError(execution.ExecutionID, beginItemNumber, endItemNumber, "api.ExecutionAsset", ex.GetFullExceptionData(false), timeout);
                                    }
                                    else
                                    {
                                        Thread.Sleep(API_V2_RETRY_INTERVAL);
                                    }
                                }
                            }
                        }

                        sw.Restart();
                        results.AddRange(
                            Query<DatabaseBulkAssetResult>(
                                $"select * from api.ExecutionAsset where ExecutionID = @ExecutionID and ItemNumber between @beginItemNumber and @endItemNumber",
                                new { execution.ExecutionID, beginItemNumber, endItemNumber }
                            )
                        );
                        AddMeasurement(metrics, $"results.AddRange >> DatabaseBulkAssetResult", sw.ElapsedMilliseconds, ++step);
                        OnAssetsPartiallyProcessed(new AssetsPartiallyProcessedEventArgs
                        {
                            Results = results
                        });

                        beginItemNumber += loopSize;
                        endItemNumber += loopSize;
                    }

                    Connection.Close();

                    if (sendGraphEvents)
                    {
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

                            var changedFields = new Dictionary<Guid, List<string>>();
                            foreach (var key in importFields.Keys)
                            {
                                var r = results.SingleOrDefault(i => i.ItemNumber == key);
                                if (r != null && !changedFields.ContainsKey(r.uid))
                                {
                                    changedFields.Add(r.uid, importFields[key]);
                                }
                            }

                            sw.Restart();
                            SendAssetGraphEvents(graphResults, changedFields, true);
                            AddMeasurement(metrics, $"SendAssetGraphEvents", sw.ElapsedMilliseconds, ++step);
                        }
                        catch
                        {

                        }
                    }

                    if (sendWorkflowEvents)
                    {
                        sw.Restart();
                        SendWorkflowEvents(at.Object, at.ObjectID, results, null, fieldTypeUpdates);
                        AddMeasurement(metrics, $"SendWorkflowEvents", sw.ElapsedMilliseconds, ++step);
                    }
                }
            }

            AddMeasurement(metrics, $"End of Method", swBegin.ElapsedMilliseconds, ++step);
            
            this.AITrackMetric(client, execution, METHOD_NAME, metrics, isLog);
            
            return results;
        }

        public List<DatabaseBulkRelationshipResult> ImportRelationships(ApiExecution execution, IntersectType rt, RelationshipInserts import, int timeout = 3600, bool sendWorkflowEvents = false, bool lookupFieldsPassedByValue = false, bool sendGraphEvents = true)
        {
            var swBegin = Stopwatch.StartNew();
            TelemetryClient client = new TelemetryClient();
            const string METHOD_NAME = "ImportRelationships";
            bool isLog = import.Count() > 1;
            var results = new List<DatabaseBulkRelationshipResult>();
            bool generalChecksCompleted = false;
            CurrentExecutionLocationModel currentLocation = null;
            bool checkCircularRelationships = false;
            bool checkSemanticRelation = false;
            bool relationshipTypeHasFieldTypes = false;
            bool relationshipTypeHasLookupFieldTypes = false;
            Dictionary<string, double> metrics = new Dictionary<string, double>();
            var step = 0;

            if ((rt.Predicate != null) && rt.Predicate.Type == PredicateType.Transformation)
                checkCircularRelationships = true;

            if ((rt.Predicate != null) && rt.Predicate.Type.AsInfoModel().SingleRelationshipByFunctionalType)
                checkSemanticRelation = true;

            SetApiExecutionProcessingStartTime(execution.ExecutionID);

            //check if trigger workflows is set to true and there are actually no workflows
            sendWorkflowEvents = sendWorkflowEvents && TypeHasWorkflows(SystemObjects.IntersectType.ToString(), rt.ID, null);

            var executionItemDupes = import.Where(i => i.ExecutionItemUid.HasValue).GroupBy(i => i.ExecutionItemUid).Where(i => i.Count() > 1).Select(i => new { ExecutionItemUid = i.Key, Count = i.Count() }).ToList();
            if (executionItemDupes.Any())
            {
                execution.ErrorMessage = $"Duplicate execution item identifiers: {string.Join(", ", executionItemDupes.Select(i => i.ExecutionItemUid.ToString()))}. Identifiers must be unique within a batch.";
                results.AddRange(import.Select(i => new DatabaseBulkRelationshipResult { ExecutionItemUid = i.ExecutionItemUid, Message = execution.ErrorMessage, Success = false }));
            }
            else
            {
                var sw = Stopwatch.StartNew();
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

                    var errorTable = new DataTable();
                    errorTable.Columns.Add("ExecutionID", typeof(Guid));
                    errorTable.Columns.Add("ItemNumber", typeof(int));
                    errorTable.Columns.Add("Message", typeof(string));
                    errorTable.Columns.Add("ExecutionItemUid", typeof(Guid));

                    var fieldTable = new DataTable();
                    fieldTable.Columns.Add("ExecutionID", typeof(Guid));
                    fieldTable.Columns.Add("ItemNumber", typeof(int));
                    fieldTable.Columns.Add("FieldName", typeof(string));
                    fieldTable.Columns.Add("FieldValue", typeof(string));
                    fieldTable.Columns.Add("FieldTypeID", typeof(int));

                    #endregion

                    // Get field types.
                    sw.Restart();
                    var fieldTypes = Query<FieldType>("select * from FieldType where Object = 'IntersectType' and ObjectID = @ID", new { rt.ID }).ToList();
                    AddMeasurement(metrics, "Get field types", sw.ElapsedMilliseconds, ++step);                    
                    var requiredFieldTypeNames = fieldTypes.Where(f => f.IsRequired && string.IsNullOrEmpty(f.DefaultValue)).Select(f => f.Name).ToList();
                    relationshipTypeHasFieldTypes = fieldTypes.Any();
                    relationshipTypeHasLookupFieldTypes = fieldTypes.Any(f => f.Type == DataType.Lookup.ToString());

                    #region Generate data sets
                    sw.Restart();
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
                                var row = errorTable.NewRow();
                                row["ExecutionID"] = execution.ExecutionID;
                                if (model.ExecutionItemUid.HasValue) row["ExecutionItemUid"] = model.ExecutionItemUid.Value;
                                row["ItemNumber"] = i;
                                row["Message"] = errorMessage;

                                errorTable.Rows.Add(row);

                                results.Add(new DatabaseBulkRelationshipResult { IntersectID = 0, ExecutionItemUid = model.ExecutionItemUid, IsNew = false, ItemNumber = i, Message = errorMessage, Success = false });

                            }
                        }
                    }
                    AddMeasurement(metrics, "Generate data sets", sw.ElapsedMilliseconds, ++step);                    
                    #endregion

                    if (results.Count > 0) // There are errors already processed.
                    {
                        OnRelationshipsPartiallyProcessed(new RelationshipsPartiallyProcessedEventArgs
                        {
                            Results = results
                        });
                    }

                    if (Database.Connection.State != ConnectionState.Open)
                        Connection.Open();

                    #region Bulk Copy
                    sw.Restart();
                    using (SqlBulkCopy bulkCopy = new SqlBulkCopy(Connection))
                    {

                        bulkCopy.BatchSize = SqlBulkBatchSize;
                        bulkCopy.DestinationTableName = "api.ExecutionRelationship";
                        bulkCopy.BulkCopyTimeout = SqlBulkBatchTimeout;

                        bulkCopy.ColumnMappings.Add("ExecutionID", "ExecutionID");
                        bulkCopy.ColumnMappings.Add("ItemNumber", "ItemNumber");
                        bulkCopy.ColumnMappings.Add("SubjectUid", "SubjectUid");
                        bulkCopy.ColumnMappings.Add("ObjectUid", "ObjectUid");
                        bulkCopy.ColumnMappings.Add("ExecutionItemUid", "ExecutionItemUid");

                        bulkCopy.WriteToServer(table);
                    }


                    using (SqlBulkCopy bulkCopy = new SqlBulkCopy(Connection))
                    {

                        bulkCopy.BatchSize = SqlBulkBatchSize;
                        bulkCopy.DestinationTableName = "api.ExecutionRelationshipError";
                        bulkCopy.BulkCopyTimeout = SqlBulkBatchTimeout;

                        bulkCopy.ColumnMappings.Add("ExecutionID", "ExecutionID");
                        bulkCopy.ColumnMappings.Add("ItemNumber", "ItemNumber");
                        bulkCopy.ColumnMappings.Add("ExecutionItemUid", "ExecutionItemUid");
                        bulkCopy.ColumnMappings.Add("Message", "Message");


                        bulkCopy.WriteToServer(errorTable);
                    }

                    // if there are no field types on this relationship type dont waste time bulk writting to the executionfield table 0 rows.
                    if (relationshipTypeHasFieldTypes)
                    {
                        using (SqlBulkCopy bulkCopy = new SqlBulkCopy(Connection))
                        {

                            bulkCopy.BatchSize = SqlBulkBatchSize;
                            bulkCopy.DestinationTableName = "api.ExecutionField";
                            bulkCopy.BulkCopyTimeout = SqlBulkBatchTimeout;

                            bulkCopy.ColumnMappings.Add("ExecutionID", "ExecutionID");
                            bulkCopy.ColumnMappings.Add("ItemNumber", "ItemNumber");
                            bulkCopy.ColumnMappings.Add("FieldName", "FieldName");
                            bulkCopy.ColumnMappings.Add("FieldValue", "FieldValue");
                            bulkCopy.ColumnMappings.Add("FieldTypeID", "FieldTypeID");

                            bulkCopy.WriteToServer(fieldTable);
                        }
                    }
                                        
                    AddMeasurement(metrics, "Bulk Copy", sw.ElapsedMilliseconds, ++step);
                    #endregion
                    sw.Restart();
                    if (relationshipTypeHasLookupFieldTypes)
                    {
                        if (lookupFieldsPassedByValue)
                        {
                            CopyFieldLookupValuesAsIs(execution.ExecutionID, timeout);
                        }
                        else
                        {
                            ResolveFieldLookupValues(execution.ExecutionID, "api.ExecutionField", timeout);
                        }
                        AddMeasurement(metrics, "ResolveFieldLookupValues", sw.ElapsedMilliseconds, ++step);
                        sw.Restart();
                        LogFieldLookupErrors(execution.ExecutionID, "IntersectType", rt.ID, "Relationship", timeout);
                        AddMeasurement(metrics, "LogFieldLookupErrors", sw.ElapsedMilliseconds, ++step);
                    }

                    #region Invalidate duplicates
                    sw.Restart();

                    if (execution.Total > 1)
                    {
                        Connection.Execute(@"
                            update	T
                            set		T.Message = coalesce(T.Message + '; ', '') + 'This relationship is specified more than once. Each relationship must be unique within a given request.',
		                            T.Success = 0
                            from	api.ExecutionRelationship T
                            cross apply (
                                select      SubjectUid, ObjectUid
                                from        api.ExecutionRelationship
                                where       ExecutionID = @ExecutionID
                                group by    SubjectUid, ObjectUid
                                having      count(*) > 1
                            ) D
		                    where   T.ExecutionId = @ExecutionID
                                    and T.SubjectUid = D.SubjectUid and T.ObjectUid = D.ObjectUid
                    ",
                        new { execution.ExecutionID }, commandTimeout: timeout);
                        AddMeasurement(metrics, "Invalidate duplicates", sw.ElapsedMilliseconds, ++step);
                    }
                    #endregion

                    #region Validate subjects/objects
                    sw.Restart();
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
                    AddMeasurement(metrics, "Validate subjects/objects", sw.ElapsedMilliseconds, ++step);                    
                    #endregion

                    #region Log subject/object resolution errors
                    sw.Restart();
                    Connection.Execute(@"
update	api.ExecutionRelationship
set		Success = 0,
		[Message] = coalesce([Message] + '; ', '') + 'Not able to resolve subject of this relationship to a valid asset.'
where	ExecutionID = @ExecutionID and (Subject is null or SubjectID is null);
	
update	api.ExecutionRelationship
set		Success = 0,
		[Message] = coalesce([Message] + '; ', '') + 'Not able to resolve object of this relationship to a valid asset.'
where	ExecutionID = @ExecutionID and (Object is null or ObjectID is null);

update	api.ExecutionRelationship
set		Success = 0,
		[Message] = coalesce([Message] + '; ', '') + 'Subject and Object cannot be same Asset.'
where	ExecutionID = @ExecutionID and SubjectUid = ObjectUid;
",
                    new { execution.ExecutionID }, commandTimeout: timeout);
                    AddMeasurement(metrics, "Log subject/object resolution errors", sw.ElapsedMilliseconds, ++step);                    
                    #endregion

                    #region Cardinality Validation

                    if (rt.SubjectCardinality == Cardinality.One)
                    {
                        sw.Restart();
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
                        AddMeasurement(metrics, "SubjectCardinality == Cardinality.One", sw.ElapsedMilliseconds, ++step);                        
                    }

                    if (rt.ObjectCardinality == Cardinality.One)
                    {
                        sw.Restart();
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
                        AddMeasurement(metrics, "ObjectCardinality == Cardinality.One", sw.ElapsedMilliseconds, ++step);                        
                    }

                    #endregion

                    #region Permissions Validation
                    sw.Restart();
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
                    AddMeasurement(metrics, "Permissions Validation", sw.ElapsedMilliseconds, ++step);                    
                    #endregion

                    if (checkCircularRelationships)
                    {
                        sw.Restart();
                        Connection.Execute(@"
                            update	T
                            set		T.Message = coalesce(T.Message + '; ', '') + 'Not able to create this relationship as it would cause circular relationship',
		                            T.Success = 0
                            from	api.ExecutionRelationship T
		                            where T.ExecutionId = @ExecutionID
                                    and T.IsNew = 1 
		                            and graph.CheckCircularRelationshipCollision(T.SubjectUid, T.ObjectUid, @predicateType) = 1
                            ", new { execution.ExecutionID, predicateType = rt.Predicate.Type }, commandTimeout: timeout);
                        AddMeasurement(metrics, "Circular Relationships Validation", sw.ElapsedMilliseconds, ++step);                        
                    }

                    if (checkSemanticRelation)
                    {
                        sw.Restart();
                        Connection.Execute(@"
                            update	T
                            set		T.Message = coalesce(T.Message + '; ', '') + 'Not able to create this relationship because a relationship for this functional type already exists.',
		                            T.Success = 0
                            from	api.ExecutionRelationship T
                                    inner join [Intersect] I on ((I.[Subject] = T.[Subject] and I.SubjectID = T.SubjectID and I.[Object] = T.[Object] and I.ObjectID = T.ObjectID) 
                                        or (I.[Object] = T.[Subject] and I.ObjectID = T.SubjectID and I.[Subject] = T.[Object] and I.SubjectID = T.ObjectID))
                                    inner join [IntersectType] IT on IT.ID = I.IntersectTypeID
                                    inner join [Predicate] P on P.ID = IT.PredicateID and P.[Type] = @predicateType  
		                            where IT.ID <> @intersectTypeID and T.ExecutionId = @ExecutionID 
                                    and T.IsNew = 1 
                            ", new { execution.ExecutionID, predicateType = (int)PredicateType.SemanticRelation, intersectTypeID = rt.ID }, commandTimeout: timeout);
                        AddMeasurement(metrics, "Semantic Relationships Validation", sw.ElapsedMilliseconds, ++step);                        
                    }

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
                                    sw.Restart();
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
                                    AddMeasurement(metrics, "Intersect table merge", sw.ElapsedMilliseconds, ++step);                                    
                                    #endregion
                                    fieldTypeUpdates.Clear();
                                    
                                    if (relationshipTypeHasFieldTypes)
                                    {
                                        sw.Restart();
                                        fieldTypeUpdates = MergeFields(execution.ExecutionID, trans, "api.ExecutionRelationship", "'Intersect' as [Object]", "A.IntersectID as ObjectID", beginItemNumber, endItemNumber, sendWorkflowEvents, timeout);
                                        AddMeasurement(metrics, "MergeFields", sw.ElapsedMilliseconds, ++step);
                                    }
                                    
                                    // Update success flag
                                    sw.Restart();
                                    Connection.Execute(
                                        $"update api.ExecutionRelationship set Success = 1 where Success is null and ExecutionID = @ExecutionID and ItemNumber between @beginItemNumber and @endItemNumber and IntersectID is not null;",
                                        new { execution.ExecutionID, beginItemNumber, endItemNumber }, transaction: trans, commandTimeout: timeout);
                                    AddMeasurement(metrics, "Update success flag", sw.ElapsedMilliseconds, ++step);
                                                                        
                                    trans.Commit();

                                    runCompleted = true;
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
                                        AddMeasurement(metrics, "LogLoop Execution Error In Rollback", sw.ElapsedMilliseconds, ++step);                                        
                                    }

                                    retryCount++;

                                    if (retryCount > API_V2_RETRY_LIMIT)
                                    {
                                        sw.Restart();
                                        LogLoopExecutionError(execution.ExecutionID, beginItemNumber, endItemNumber, "api.ExecutionRelationship", ex.GetFullExceptionData(false), timeout);
                                        AddMeasurement(metrics, "LogLoopExecutionError", sw.ElapsedMilliseconds, ++step);                                        
                                    }
                                    else
                                    {
                                        Thread.Sleep(API_V2_RETRY_INTERVAL);
                                    }
                                }
                            }
                        }
                        sw.Restart();
                        results.AddRange(
                            Query<DatabaseBulkRelationshipResult>(
                                $"select * from api.ExecutionRelationship where ExecutionID = @ExecutionID and ItemNumber between @beginItemNumber and @endItemNumber",
                                new { execution.ExecutionID, beginItemNumber, endItemNumber }
                            )
                        );
                        AddMeasurement(metrics, "results.AddRange >> DatabaseBulkRelationshipResult", sw.ElapsedMilliseconds, ++step);
                        
                        OnRelationshipsPartiallyProcessed(new RelationshipsPartiallyProcessedEventArgs
                        {
                            Results = results
                        });

                        beginItemNumber += loopSize;
                        endItemNumber += loopSize;
                    }

                    Connection.Close();
                    sw.Restart();

                    if (sendGraphEvents)
                    {
                        SendAssetGraphEvents(results);
                        AddMeasurement(metrics, "SendAssetGraphEvents", sw.ElapsedMilliseconds, ++step);                        
                        sw.Restart();
                    }

                    if (sendWorkflowEvents)
                        SendWorkflowEvents("IntersectType", rt.ID, results, null, fieldTypeUpdates);

                    AddMeasurement(metrics, "SendWorkflowEvents", sw.ElapsedMilliseconds, ++step);                    
                }
            }            
            AddMeasurement(metrics, "End Method", swBegin.ElapsedMilliseconds, ++step);
            this.AITrackMetric(client, execution, METHOD_NAME, metrics, isLog);
            return results;
        }

        public List<DatabaseBulkRelationshipResult> DeleteRelationships(ApiExecution execution, IntersectType it, RelationshipDeletes import, int timeout = 3600, bool sendWorkflowEvents = false, bool sendGraphEvents = true)
        {
            var results = new List<DatabaseBulkRelationshipResult>();
            bool generalChecksCompleted = false;
            CurrentExecutionLocationModel currentLocation = null;

            SetApiExecutionProcessingStartTime(execution.ExecutionID);

            //check if trigger workflows is set to true and there are actually no workflows in which case shut off triggering of workflows
            sendWorkflowEvents = sendWorkflowEvents && TypeHasWorkflows(SystemObjects.IntersectType.ToString(), it.ID, ChangeType.Delete);

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
                    Connection.Open();

                #region Bulk Copy

                using (SqlBulkCopy bulkCopy = new SqlBulkCopy(Connection))
                {

                    bulkCopy.BatchSize = SqlBulkBatchSize;
                    bulkCopy.DestinationTableName = "api.ExecutionDeletedRelationship";
                    bulkCopy.BulkCopyTimeout = SqlBulkBatchTimeout;

                    bulkCopy.ColumnMappings.Add("ExecutionID", "ExecutionID");
                    bulkCopy.ColumnMappings.Add("ExecutionItemUid", "ExecutionItemUid");
                    bulkCopy.ColumnMappings.Add("ItemNumber", "ItemNumber");
                    bulkCopy.ColumnMappings.Add("Uid", "Uid");
                    bulkCopy.ColumnMappings.Add("Cascade", "Cascade");

                    bulkCopy.WriteToServer(table);
                }

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

                                #region Audit

                                var auditSql = @"
                                insert into reporting.Global_Audit (Object, ObjectID, ObjectName, ResourceID, Date, Action, ActionObject, ActionObjectID, ActionObjectTypeName, ActionObjectName, ActionDescription)
	                                select	distinct
			                                A.Object, 
			                                A.ObjectID,
			                                SUBSTRING(A.DisplayValue,1,250), 
			                                @r, 
			                                @dt, 
			                                'Deleted', 
			                                'Intersect',
			                                I.ID, 
			                                TName.[Name], 
			                                SUBSTRING(IName.[Name],1,250), 
			                                'This relationship has been removed.' 
	                                from	[Intersect] I
                                            inner join AssetDetail A on {0}
                                            cross apply dbo.getIntersectNames(I.ID) IName
                                            cross apply dbo.getIntersectTypeNames(I.IntersectTypeID) TName
			                                inner join api.ExecutionDeletedRelationship S on S.IntersectID = I.ID 
                                                and S.ExecutionID = @executionID 
                                                and S.ItemNumber between @beginItemNumber and @endItemNumber 
                                                and S.Success is null;";

                                Connection.Execute(string.Format(auditSql, "A.[Object] = I.[Subject] and A.ObjectID = I.SubjectID"), new { execution.ExecutionID, r = CurrentResourceID, dt = DateTime.UtcNow, beginItemNumber, endItemNumber }, transaction: trans, commandTimeout: timeout);
                                Connection.Execute(string.Format(auditSql, "A.[Object] = I.[Object] and A.ObjectID = I.ObjectID"), new { execution.ExecutionID, r = CurrentResourceID, dt = DateTime.UtcNow, beginItemNumber, endItemNumber }, transaction: trans, commandTimeout: timeout);


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
                if (sendGraphEvents)
                {
                    SendAssetGraphEvents(results);
                }

                if (sendWorkflowEvents)
                    SendWorkflowEvents("IntersectType", it.ID, results, ChangeType.Delete);
            }

            return results;
        }

        private void ValidateDeleteRelationshipTypes(ApiExecution execution, int timeout = 3600)
        {
            var predicateTypeInfo = new PredicateType().GetAsList();
            var disallowEditIds = predicateTypeInfo.Where(p => p.AllowEditFromRelationshipEditor == false).Select(p => (int)p.ID).ToList();

            Connection.Execute(@"
                                    Update ER
                                    Set Success=0,
                                    Message='Relationship type (Uid) not found.' 
                                    from [api].[ExecutionDeletedRelationshipType] ER
                                    where  ER.ExecutionID=@executionID and
                                    ER.Success is null
                                    and not exists (select 1 from IntersectType where Uid = ER.[UID])
                         ", new { executionID = execution.ExecutionID }, commandTimeout: timeout);



            Connection.Execute(@"
                                    Update ER
                                    Set Success=0,
                                    Message='Relationship type not allowed to delete' 
                                    from [api].[ExecutionDeletedRelationshipType] ER
                                    where  ER.ExecutionID=@executionID and
                                    ER.Success is null
                                    and  exists (select 1 from IntersectType I
                                                        inner join [Predicate] P on P.ID = I.PredicateID
                                                    where I.Uid = ER.[UID] and P.[TYPE]  in @disallowEditIds)
                          ", new { executionID = execution.ExecutionID, disallowEditIds = disallowEditIds }, commandTimeout: timeout);


            Connection.Execute(@"
                                    Update ER
                                    Set Success=0,
                                    Message='Relationship type has existing relationships' 
                                    from [api].[ExecutionDeletedRelationshipType] ER
                                    where  ER.ExecutionID=@executionID and ER.[Cascade] =0 and
                                    ER.Success is null
                                    and  exists (select 1 from IntersectType I
                                                        inner join [Intersect] T  on I.ID = T.IntersectTypeID
                                                    where I.Uid = ER.[UID] )
                            ", new { executionID = execution.ExecutionID }, commandTimeout: timeout);

            //check for lookups
            Connection.Execute(@"
                                update	T
                                set		T.Message = coalesce(T.Message + '; ', '') + 'You have not enabled Cascade and there are ' + cast(S.[Count] as nvarchar) + ' relationship lookups associated with this relationship.',
	                                    T.Success = 0
                                from	api.ExecutionDeletedRelationshipType T
                                        inner join
		                                (
			                                select	EDR.ExecutionID,
					                                EDR.ItemNumber,
					                                Count(1) as [Count]
			                                from	FieldTypeLookup O
					                                cross apply OPENJSON(O.[Definition], N'lax $.Relations') with (
						                                IntersectTypeUid uniqueidentifier, 
						                                AssetTypeUid uniqueidentifier,
						                                RelationType int, 
						                                Direction int
					                                ) R
					                                inner join [IntersectType] IT on IT.uid = R.intersectTypeUid
					                                inner join [api].[ExecutionDeletedRelationshipType] EDR on EDR.UID=IT.UID and 
					                                EDR.ExecutionID = @ExecutionID
					                                and 
					                                EDR.Success is null
			                                where EDR.[Cascade]=0 and ISJSON(o.Definition)>0
					                                group by ExecutionID, ItemNumber
                                        ) S on S.ExecutionID = T.ExecutionID and S.ItemNumber = T.ItemNumber;",
                                            new { execution.ExecutionID }, commandTimeout: timeout);

            //check for relationship, RefListRelationship fields
            Connection.Execute(@"
                                update	T
                                set		T.Message = coalesce(T.Message + '; ', '') + 'You have not enabled Cascade and there are ' + cast(S.[Count] as nvarchar) + ' fields associated with this relationship.',
	                                    T.Success = 0
                                from	api.ExecutionDeletedRelationshipType T
                                        inner join
		                                (
                                            select	EDR.ExecutionID,
					                                EDR.ItemNumber,
					                                Count(1) as [Count]                                             
                                            from 
                                                    FieldType FT 
                                                    inner join 
                                                    [IntersectType] IT on FT.LookupObjectID = IT.ID and FT.Type in ('Relationship', 'RefListRelationship', 'FieldFromRelationship')
                                                    inner join [api].[ExecutionDeletedRelationshipType] EDR on EDR.UID=IT.UID and EDR.ExecutionID = @ExecutionID
                                                    and 
					                                EDR.Success is null
                                                    AND
                                                    EDR.[Cascade]=0
                                            group by ExecutionID, ItemNumber			                                
                                        ) S on S.ExecutionID = T.ExecutionID and S.ItemNumber = T.ItemNumber;",
                                            new { execution.ExecutionID }, commandTimeout: timeout);            

        }

        private void ValidateRelationshipTypes(bool isInsert, ApiExecution execution, int timeout = 3600)
        {
            var predicateTypeInfo = new PredicateType().GetAsList();
            Guid emptyUid = Guid.Empty;

            if (!isInsert)
            {
                Connection.Execute(@"
update  api.ExecutionRelationshipType 
set     Success = 0, Message = 'Uid is missing / incorrect format.' 
where   ExecutionID = @ExecutionID and Success is null and (Uid is null or Uid = @emptyUid);

update  ER 
set     Success = 0,
        Message = 'Relationship type (Uid) not found.' 
from    [api].[ExecutionRelationshipType] ER 
where   ER.ExecutionID = @ExecutionID 
        and ER.Success is null 
        and not exists (select 1 from IntersectType where Uid = ER.[Uid]);

Update  T
set     SubjectUid = SA.Uid, [Subject] = SA.Object, SubjectID = SA.ObjectID,
        ObjectUid = OA.Uid, [Object] = OA.Object, ObjectID = OA.ObjectID
from    [api].[ExecutionRelationshipType] T
        inner join IntersectType S on S.Uid = T.Uid
        inner join AssetType SA on SA.Object = S.Subject and SA.ObjectID = S.SubjectID
        inner join AssetType OA on OA.Object = S.Object and OA.ObjectID = S.ObjectID
where   T.ExecutionID = @ExecutionID and T.Success is null;",
                new { execution.ExecutionID, emptyUid }, commandTimeout: timeout);
            }

            #region Insert/Update

            var predicateCheckSql = "";
            predicateTypeInfo.ForEach(p =>
            {
                string message = "";

                if (p.Obsolete)
                {
                    message = $"You may not use the {p.Name} functional type as it is obsolete and no longer supported.";
                    predicateCheckSql += $@"update T set T.Success = 0, T.Message = coalesce(T.Message+' ', '') + '{message}' from api.ExecutionRelationshipType T inner join [Predicate] P on P.Uid = T.PredicateUid and P.[Type] = {(int)p.ID} and T.ExecutionID = @ExecutionID and T.Success is null; ";
                }
                else if (!p.AllowEditFromRelationshipEditor)
                {
                    message = $"Creating or updating of relationship types with a {p.Name} functional type is not allowed.";
                    predicateCheckSql += $@"update T set T.Success = 0, T.Message = coalesce(T.Message+' ', '') + '{message}' from api.ExecutionRelationshipType T inner join [Predicate] P on P.Uid = T.PredicateUid and P.[Type] = {(int)p.ID} and T.ExecutionID = @ExecutionID and T.Success is null; ";
                }
                else
                {
                    if (!p.AllowDifferentSubjectObject)
                    {
                        message = $"ObjectUid and SubjectUid must be the same for the {p.Name} functional type.";
                        predicateCheckSql += $@"update T set T.Success = 0, T.Message = coalesce(T.Message+' ', '') + '{message}' from api.ExecutionRelationshipType T inner join [Predicate] P on P.Uid = T.PredicateUid and P.[Type] = {(int)p.ID} and T.ExecutionID = @ExecutionID and T.Success is null and (T.ObjectUid <> T.SubjectUid); ";
                    }

                    if (p.ForceDifferentSubjectObject)
                    {
                        message = $"ObjectUid and SubjectUid must be different for the {p.Name} functional type.";
                        predicateCheckSql += $@"update T set T.Success = 0, T.Message = coalesce(T.Message+' ', '') + '{message}' from api.ExecutionRelationshipType T inner join [Predicate] P on P.Uid = T.PredicateUid and P.[Type] = {(int)p.ID} and T.ExecutionID = @ExecutionID and T.Success is null and (T.ObjectUid = T.SubjectUid); ";
                    }

                    if (p.ID == PredicateType.Transformation)
                    {
                        message = $"When using the {p.Name} functional type, either your Subject or Object must support being used as a transformation, but not both.";
                        predicateCheckSql += $@"
update  T 
set     T.Success = 0, T.Message = coalesce(T.Message+' ', '') + '{message}' 
from    api.ExecutionRelationshipType T 
        inner join [Predicate] P on P.Uid = T.PredicateUid and P.[Type] = {(int)p.ID} and T.ExecutionID = @ExecutionID and T.Success is null 
        inner join AssetType S on S.Uid = T.SubjectUid
        inner join AssetType O on O.Uid = T.ObjectUid 
where   (S.UseAsTransformation = 1 and O.UseAsTransformation = 1) OR (S.UseAsTransformation = 0 and O.UseAsTransformation = 0); ";
                    }

                    // Always do this.
                    message = $"When using the {p.Name} functional type, your Subject must be an asset type of class {string.Join(" or ", p.SubjectAssetClassesSupported.Select(c => c.AsInfoModel().Name))}, and Object of class {string.Join(" or ", p.ObjectAssetClassesSupported.Select(c => c.AsInfoModel().Name))}.";
                    predicateCheckSql += $@"
update  T 
set     T.Success = 0, T.Message = coalesce(T.Message+' ', '') + '{message}' 
from    api.ExecutionRelationshipType T 
        inner join [Predicate] P on P.Uid = T.PredicateUid and P.[Type] = {(int)p.ID} and T.ExecutionID = @ExecutionID and T.Success is null 
        inner join AssetType S on S.Uid = T.SubjectUid 
        inner join AssetType O on O.Uid = T.ObjectUid 
where   (S.[Class] not in ({string.Join(",", p.SubjectAssetClassesSupported.Select(c => (int)c.AsInfoModel().ID))}) 
        OR O.[Class] not in ({string.Join(",", p.ObjectAssetClassesSupported.Select(c => (int)c.AsInfoModel().ID))})); ";
                }
            });
            Connection.Execute(predicateCheckSql, new { execution.ExecutionID, emptyUid }, commandTimeout: timeout);

            Connection.Execute(@"
update  api.ExecutionRelationshipType
set     Message = coalesce(Message+' ', '') + 'PredicateUid is missing / incorrect format.'
where   ExecutionID = @ExecutionID 
        and Success = 0
        and (PredicateUid is null or PredicateUid = @emptyUid);

update  api.ExecutionRelationshipType 
set     Success = 0, 
        Message='SubjectCardinality is missing / incorrect' 
where   ExecutionID = @ExecutionID 
        and Success is null 
        and (SubjectCardinality is null  or SubjectCardinality =0 );

update api.ExecutionRelationshipType 
set     Success = 0, 
        Message='ObjectCardinality is missing / incorrect' 
where   ExecutionID = @ExecutionID 
        and Success is null 
        and (ObjectCardinality is null or ObjectCardinality = 0);

with cte_relations as (
                      select    ItemNumber, 
                                Row_Number() Over (PARTITION BY SubjectUID,ObjectUID,PredicateUID,SubjectCardinality,ObjectCardinality order by ItemNumber)  row_num
                      from      [api].[ExecutionRelationshipType] 
                      where     ExecutionID=@executionID 
                                and Success is null
                      )
update  ER
SET     Success = 0,
        Message = 'Duplicate relationship types' 
from    api.[ExecutionRelationshipType] ER
where   ER.ExecutionID = @ExecutionID 
        and Success is null 
        and  exists ( select 1 from cte_relations where row_num > 1 and ER.ItemNumber = ItemNumber );

Update  ER 
set     [Subject] = AST.[Object],
        SubjectID = AST.[ObjectID]
from    [api].[ExecutionRelationshipType] ER 
        inner join AssetType AST on AST.UID = ER.SubjectUID 
where   ER.ExecutionID = @ExecutionID and ER.Success is null;

Update  ER 
set     [Object] = AST.[Object], 
        ObjectID = AST.[ObjectID] 
from    [api].[ExecutionRelationshipType] ER 
        inner join AssetType AST on AST.UID = ER.ObjectUID 
where   ER.ExecutionID = @ExecutionID and ER.Success is null;

update  ER 
set     PredicateID = P.ID 
from    [api].[ExecutionRelationshipType] ER 
        inner join [Predicate] P on P.UID = ER.PredicateUID 
where   ER.ExecutionID = @ExecutionID and ER.Success is null;

update  api.ExecutionRelationshipType 
set     Success = 0, 
        Message = 'Predicate not found.' 
where   ExecutionID = @ExecutionID 
        and Success is null 
        and PredicateID is null;", new { execution.ExecutionID, emptyUid }, commandTimeout: timeout);

            #endregion

            if (isInsert)
            {
                Connection.Execute(@"
update  api.ExecutionRelationshipType 
set     Success = 0, 
        Message = 'SubjectUid is missing / incorrect format.' 
where   ExecutionID = @ExecutionID 
        and Success is null 
        and (SubjectUid is null or SubjectUid = @emptyUid);

update  api.ExecutionRelationshipType 
set     Success = 0, 
        Message ='Subject asset type not found.' 
where   ExecutionID = @ExecutionID 
        and Success is null 
        and (SubjectId is null or [Subject] is null);

update  api.ExecutionRelationshipType
set     Success = 0,
        Message = 'ObjectUid is missing / incorrect format.' 
where   ExecutionID = @ExecutionID 
        and Success is null 
        and (ObjectUid is null or ObjectUid = @emptyUid);

update  api.ExecutionRelationshipType 
set     Success = 0, 
        Message = 'Object asset type not found.' 
where   ExecutionID = @ExecutionID 
        and Success is null 
        and (ObjectId is null or [Object] is null);

update  T
set     T.Success = 0, 
        T.Message = 'Relationship with specified Uid already exists.' 
from    api.ExecutionRelationshipType T
        inner join IntersectType S on S.Uid = T.Uid and T.ExecutionID = @ExecutionID 
        and T.Success is null 
        and (T.Uid is not null and T.Uid <> @emptyUid);

update  ER 
set     Success = 0, 
        Message = 'Another relationship already exists with this configuration.' 
from    [api].[ExecutionRelationshipType] ER 
where   ER.ExecutionID = @ExecutionID 
        and ER.Success is null 
        and exists (
                    select  1 
                    from    IntersectType 
                    where   [Subject] = ER.[Subject] 
                            and SubjectID = ER.SubjectID 
                            and [Object] = ER.[Object] 
                            and ObjectID = ER.ObjectID 
                            and PredicateID = ER.PredicateID);",
                new { execution.ExecutionID, emptyUid }, commandTimeout: timeout);
            }
            else
            {
                Connection.Execute(@"
update  ER
set     Success = 0, 
        Message='Relationships already present for this type' 
from    [api].[ExecutionRelationshipType] ER 
        inner join [intersecttype] IT on IT.UID = ER.UID 
where   ER.ExecutionID = @ExecutionID 
        and ER.Success is null
        and exists (select 1 from [Intersect] where IntersectTypeID =IT.ID);

update  ER 
set     Success = 0, 
        Message = 'Relationship type with the specified predicate already exists.' 
from    [api].[ExecutionRelationshipType] ER 
        inner join [intersecttype] IT on IT.UID = ER.UID 
where   ER.ExecutionID = @ExecutionID 
        and ER.Success is null 
        and exists (
            select  1 
            from    [intersecttype] I 
                    inner join Predicate P on P.ID = I.PredicateID 
            where   P.Uid = ER.PredicateUid 
                    and I.Subject = IT.Subject 
                    and I.SubjectID=IT.SubjectID 
                    and I.Uid != IT.Uid 
                    and I.[Object]=IT.[Object] 
                    and I.ObjectID=IT.ObjectID);",
                new { execution.ExecutionID }, commandTimeout: timeout);
            }
        }

        private void ValidateAssetCrossReference(ApiExecution execution, int timeout = 3600)
        {
            Connection.Execute(@"Update api.ExecutionAssetCrossReference
                                    Set Success=0,
                                    Message='Does not contain valid Uid.' 
                                    Where ExecutionID = @executionID and Success is null and
                                    (Uid is null or  UID ='00000000-0000-0000-0000-000000000000' ) ", new { executionID = execution.ExecutionID }, commandTimeout: timeout);



            Connection.Execute(@"Update api.ExecutionAssetCrossReference
                                    Set Success=0,
                                    Message='DataSource is required.' 
                                    Where ExecutionID = @executionID and Success is null and
                                    ( DataSource is null or Trim(DataSource) ='') ", new { executionID = execution.ExecutionID }, commandTimeout: timeout);



            Connection.Execute(@"Update api.ExecutionAssetCrossReference
                                    Set Success=0,
                                    Message='Type is required.' 
                                    Where ExecutionID = @executionID and Success is null and
                                    ([Type] is null  or TRIM([Type]) = '' ) ", new { executionID = execution.ExecutionID }, commandTimeout: timeout);



            Connection.Execute(@"Update api.ExecutionAssetCrossReference
                                    Set Success=0,
                                    Message='ExternalID is required.' 
                                    Where ExecutionID = @executionID and Success is null and
                                    ( ExternalID is null or TRIM(ExternalID) ='') ", new { executionID = execution.ExecutionID }, commandTimeout: timeout);



            Connection.Execute(@"Update api.ExecutionAssetCrossReference
                                    Set Success=0,
                                    Message='Does not contain required fields.' 
                                    Where ExecutionID = @executionID and Success is null and
                                    (Uid is null or DataSource is null or [Type] is null or ExternalID is null
                                   or UID ='00000000-0000-0000-0000-000000000000' or Trim(DataSource) ='' or TRIM([Type]) = '' or TRIM(ExternalID) ='') ", new { executionID = execution.ExecutionID }, commandTimeout: timeout);


            Connection.Execute(@"
                        Update  ECR
                        SET Success=0,
                        Message='Asset cross reference already exists'
                        from api.ExecutionAssetCrossReference ECR
                        Where ECR.ExecutionID = @executionID and Success is null and exists (Select 1 from AssetCrossReference where UID=ECR.UID and DataSource= ECR.DataSource and
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

            SetApiExecutionProcessingStartTime(execution.ExecutionID);

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
                row["DataSource"] = item.DataSource != null ? item.DataSource.Trim() : item.DataSource;
                row["Type"] = item.Type != null ? item.Type.Trim() : item.Type;
                row["ExternalID"] = item.ExternalID != null ? item.ExternalID.Trim() : item.ExternalID;
                row["FieldHash"] = item.FieldHash;

                table.Rows.Add(row);
            }

            #endregion
            try
            {


                if (Database.Connection.State != ConnectionState.Open)
                    Connection.Open();

                #region Bulk Copy
                using (var bulkCopy = new SqlBulkCopy(Connection)
                {
                    BatchSize = SqlBulkBatchSize,
                    DestinationTableName = "api.ExecutionAssetCrossReference",
                    BulkCopyTimeout = SqlBulkBatchTimeout
                })
                {

                    bulkCopy.ColumnMappings.Add("ExecutionID", "ExecutionID");
                    bulkCopy.ColumnMappings.Add("ItemNumber", "ItemNumber");
                    bulkCopy.ColumnMappings.Add("uid", "uid");
                    bulkCopy.ColumnMappings.Add("DataSource", "DataSource");
                    bulkCopy.ColumnMappings.Add("Type", "Type");
                    bulkCopy.ColumnMappings.Add("ExternalID", "ExternalID");
                    bulkCopy.ColumnMappings.Add("FieldHash", "FieldHash");


                    bulkCopy.WriteToServer(table);
                }

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

            SetApiExecutionProcessingStartTime(execution.ExecutionID);

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
                            Connection.Open();

                        #region Bulk Copy

                        using (SqlBulkCopy bulkCopy = new SqlBulkCopy(Connection))
                        {

                            bulkCopy.BatchSize = SqlBulkBatchSize;
                            bulkCopy.DestinationTableName = "api.ExecutionDeletedPredicate";
                            bulkCopy.BulkCopyTimeout = SqlBulkBatchTimeout;

                            bulkCopy.ColumnMappings.Add("ExecutionID", "ExecutionID");
                            bulkCopy.ColumnMappings.Add("ItemNumber", "ItemNumber");
                            bulkCopy.ColumnMappings.Add("ExecutionItemUid", "ExecutionItemUid");
                            bulkCopy.ColumnMappings.Add("Uid", "Uid");

                            bulkCopy.WriteToServer(table);
                        }

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
    where	T.ExecutionID = @ExecutionID

    update T
    set T.Success = 0, [Message] = coalesce([Message] + '; ', '') + 'This predicate is system predicate and may not be removed.'
    from	api.ExecutionDeletedPredicate T
    cross apply (select * from Predicate P  where P.ID = T.PredicateID AND P.IsSystem = 1) Usage
    where	T.ExecutionID = @ExecutionID
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

            SetApiExecutionProcessingStartTime(execution.ExecutionID);


            var executionItemDupes = import.Where(i => i.ExecutionItemUid.HasValue).GroupBy(i => i.ExecutionItemUid).Where(i => i.Count() > 1).Select(i => new { ExecutionItemUid = i.Key, Count = i.Count() }).ToList();
            var predDupes = import.GroupBy(x => x.Name + x.Type).Where(x => x.Count() > 1).Select(x => new { x.Key, Items = x.ToList() }).ToList();

            var predInverseDupes = import.GroupBy(x => x.Inverse + x.Type).Where(x => x.Count() > 1).Select(x => new { x.Key, Items = x.ToList() }).ToList();

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
            else if (predInverseDupes.Any())
            {
                execution.ErrorMessage = $"Duplicate predicate items: {string.Join(", ", predInverseDupes.Select(i => i.Items.First().Inverse + "|" + i.Items.First().Type.ToString()))}. Inverse and type must be unique within a batch.";
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
                        Connection.Open();

                    #region Bulk Copy

                    using (SqlBulkCopy bulkCopy = new SqlBulkCopy(Connection))
                    {

                        bulkCopy.BatchSize = SqlBulkBatchSize;
                        bulkCopy.DestinationTableName = "api.ExecutionPredicate";
                        bulkCopy.BulkCopyTimeout = SqlBulkBatchTimeout;

                        bulkCopy.ColumnMappings.Add("ExecutionID", "ExecutionID");
                        bulkCopy.ColumnMappings.Add("ItemNumber", "ItemNumber");
                        bulkCopy.ColumnMappings.Add("ExecutionItemUid", "ExecutionItemUid");
                        bulkCopy.ColumnMappings.Add("Type", "Type");
                        bulkCopy.ColumnMappings.Add("Name", "Name");
                        bulkCopy.ColumnMappings.Add("Inverse", "Inverse");
                        bulkCopy.ColumnMappings.Add("uid", "uid");

                        bulkCopy.WriteToServer(table);
                    }

                    #endregion

                    #region Log data errors

                    int lineageVersion = Community.GetCompanySettingByKey<int>("LineageVersion");
                    var allowedFunctionalTypes = PredicateType.DataLineage.GetAsList()
                        .Where(p =>
                            p.AllowEditFromPredicateEditor &&
                            p.LineageVersionsSupported.Contains(lineageVersion)
                            ).ToList();
                    var allowedTypeIdList = string.Join(", ", allowedFunctionalTypes.Select(p => (int)p.ID));
                    var allowedTypeNameList = string.Join(", ", allowedFunctionalTypes.Select(p => p.ID.ToString().Replace("'", "''")));

                    var differentLineageVersionFunctionalTypes = PredicateType.DataLineage
                        .GetAsList()
                        .Where(p => !p.LineageVersionsSupported.Contains(lineageVersion))
                        .Select(p => (int)p.ID)
                        .ToList();
                    if (differentLineageVersionFunctionalTypes.Count == 0) differentLineageVersionFunctionalTypes.Add(-1);
                    var differentLineageVersionIdList = string.Join(", ", differentLineageVersionFunctionalTypes);

                    var checkSQL = $@"
    update	api.ExecutionPredicate 
    set		Success = 0,
		    [Message] = coalesce([Message] + '; ', '') + 'Predicate with same Name and Type already exists'
    from api.ExecutionPredicate EP
    inner join [Predicate] P on P.Name = EP.Name and P.Type = EP.Type
    where	ExecutionID = @ExecutionID and EP.uid is null

    update	api.ExecutionPredicate 
    set		Success = 0,
		    [Message] = coalesce([Message] + '; ', '') + 'Predicate with same Inverse and Type already exists'
    from api.ExecutionPredicate EP
    inner join [Predicate] P on P.Inverse = EP.Inverse and P.Type = EP.Type
    where	ExecutionID = @ExecutionID and EP.uid is null

    update	api.ExecutionPredicate 
    set		Success = 0,
		    [Message] = coalesce([Message] + '; ', '') + 'Predicate with same Inverse and Type already exists'
    from api.ExecutionPredicate EP
    inner join [Predicate] P on P.Inverse = EP.Inverse and P.Type = EP.Type and P.uid != EP.uid
    where	ExecutionID = @ExecutionID and EP.uid is not null

    update	api.ExecutionPredicate
    set		Success = 0,
		    [Message] = coalesce([Message] + '; ', '') + 'Predicate with same Name and Type already exists'
    from api.ExecutionPredicate EP
    inner join [Predicate] P on P.Name = EP.Name and P.Type = EP.Type and P.uid != EP.uid
    where	ExecutionID = @ExecutionID and EP.uid is not null

    update api.ExecutionPredicate 
    set     Success = 0, 
            [Message] = coalesce([Message] + '; ', '') + 'You may not change the type for this predicate as it is already in use.' 
    from api.ExecutionPredicate EP 
    inner join [Predicate] P on P.[Uid] = Ep.[Uid] 
    where ExecutionID = @ExecutionID and P.Type <> EP.Type and exists (select 1 from IntersectType T where T.PredicateID = P.ID)

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
		    [Message] = coalesce([Message] + '; ', '') + 'Predicate Type invalid. Allowed values are {allowedTypeNameList}'
    where	ExecutionID = @ExecutionID and [Type] not in ({allowedTypeIdList}) and [Type] not in ({differentLineageVersionIdList})

    update	api.ExecutionPredicate
    set		Success = 0,
		    [Message] = coalesce([Message] + '; ', '') + 'Your current version of lineage does not support using this predicates of this type.'
    where	ExecutionID = @ExecutionID and [Type] in ({differentLineageVersionIdList});";

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
                                            
                                            update  api.ExecutionPredicate 
                                            set     [Uid] = newid() 
                                            where   [Uid] is null or [Uid] = @emptyUid 
                                                    and ItemNumber between @beginItemNumber and @endItemNumber; 

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
	                                            insert (Uid, Name, Inverse, Type, IsSystem)
	                                            values (S.Uid, S.Name,S.Inverse, S.Type, 0)
	                                        output inserted.ID, inserted.Uid, S.ExecutionItemUid into #mergeResultTable;

                                            update EP
                                            set EP.PredicateID = Res.PredicateId,
	                                            EP.uid = Res.PredicateUid
                                            from api.ExecutionPredicate EP
                                                 inner join #mergeResultTable Res on Res.ExecutionItemUid = EP.ExecutionItemUid
                                            where EP.ExecutionID = @ExecutionID";

                                    Connection.Execute(insertSQL,
                                            new { execution.ExecutionID, beginItemNumber, endItemNumber, emptyUid = Guid.Empty }, transaction: trans, commandTimeout: timeout);

                                    Connection.Execute(
                                        $"update P set P.Success = 1 from api.ExecutionPredicate P where	{querySuffix} and P.PredicateID is not null;",
                                        new { execution.ExecutionID, beginItemNumber, endItemNumber }, transaction: trans, commandTimeout: timeout);

                                    trans.Commit();
                                    runCompleted = true;

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

            SetApiExecutionProcessingStartTime(execution.ExecutionID);

            var uidDupes = import.GroupBy(i => i.Uid).Where(i => i.Count() > 1).Select(i => new { Uid = i.Key, Count = i.Count() }).ToList();
            var nameDupes = import.GroupBy(i => i.Name).Where(i => i.Count() > 1).Select(i => new { Name = i.Key, Count = i.Count() }).ToList();
            if (uidDupes.Any() && execution.Method == "PUT")
            {
                execution.ErrorMessage = $"Duplicate Asset Uids: {string.Join(", ", uidDupes.Select(i => i.Uid.ToString()))}. Identifiers must be unique within a batch.";
                results.AddRange(import.Select(i => new ResponsibilityTypeUpsertResult { Uid = i.Uid.Value, Message = execution.ErrorMessage, Success = false }));
            }
            else if (nameDupes.Any())
            {
                for (int idx = 0; idx < import.Count; idx++)
                {
                    var dupe = nameDupes.FirstOrDefault(x => x.Name == import[idx].Name);
                    results.Add(new ResponsibilityTypeUpsertResult()
                    {
                        ItemNumber = idx,
                        Success = false,
                        Message = dupe == null ? "Names must be unique within a batch." : $"Duplicate Name '{dupe.Name}'. Names must be unique within a batch."
                    });
                }
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
                            row["Name"] = model.Name.Trim();
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
                        Connection.Open();

                    #region Bulk Copy

                    using (SqlBulkCopy bulkCopy = new SqlBulkCopy(Connection))
                    {

                        bulkCopy.BatchSize = SqlBulkBatchSize;
                        bulkCopy.DestinationTableName = "api.ExecutionResponsibilityType";
                        bulkCopy.BulkCopyTimeout = SqlBulkBatchTimeout;

                        bulkCopy.ColumnMappings.Add("ExecutionID", "ExecutionID");
                        bulkCopy.ColumnMappings.Add("ItemNumber", "ItemNumber");
                        bulkCopy.ColumnMappings.Add("ExecutionItemUid", "ExecutionItemUid");
                        bulkCopy.ColumnMappings.Add("Name", "Name");
                        bulkCopy.ColumnMappings.Add("Description", "Description");
                        bulkCopy.ColumnMappings.Add("Uid", "Uid");
                        bulkCopy.ColumnMappings.Add("IsNew", "IsNew");

                        bulkCopy.WriteToServer(table);
                    }

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

    update	api.ExecutionResponsibilityType 
    set		Success = 0,
		    [Message] = coalesce([Message] + '; ', '') + 'Name field cannot be empty'
    where	ExecutionID = @ExecutionID and (Name is null or Name = '');
   ";

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

        public void SetApiExecutionProcessingStartTime(Guid ExecutionId)
        {
            Query<int>("update api.Execution set ProcessingStartedOn = @startedOn where ExecutionId = @ExecutionId and ProcessingStartedOn is null",
                new { startedOn = DateTime.UtcNow, ExecutionId }).FirstOrDefault();
        }

        public void CalculateProposedKeyHashes(AssetType at, Guid executionID, int timeout = 3600, int? parentIntersectTypeId = null, SqlTransaction trans = null, string assetTable = "api.ExecutionAsset", string fieldTable = "api.ExecutionField")
        {
            string keyErrorMessage = "'Key values match another asset under a different set of key fields. '";
            string keyTableTempCreation = @"CREATE TABLE #Keys (AssetID bigint, ActiveKey varchar(32)); CREATE CLUSTERED INDEX CIX_TempApiExecutionKeys ON #Keys ( ActiveKey ASC ); ";
            string keyComparisonUpdateStatement = $@"
                            update  T 
                            set     T.Success = 0, 
                                    T.Message = {keyErrorMessage}
                            from    {assetTable} T 
                                    inner join #Keys S on T.ExecutionID = @ExecutionID and S.ActiveKey = T.ProposedKey and S.AssetID <> T.AssetID and T.AssetID is not null; 

                            update  T 
                            set     T.Success = 0, 
                                    T.Message = {keyErrorMessage}
                            from    {assetTable} T 
                                    inner join #Keys S on T.ExecutionID = @ExecutionID and S.ActiveKey = T.ProposedKey and T.AssetID is null; ";


            if (at.Object == "FusionAttributeType")
            {
                Connection.Execute($@"
{keyTableTempCreation}

update  A
set     A.ProposedKey = utility.GetHash(
                            cast(@ID as nvarchar) + '|' + 
                            FC.FieldValue + '|' + 
                            COALESCE(
                                FS.FieldValue, 
                                COALESCE(cast(A.ParentUid as nvarchar(50))+'|', '') + FN.FieldValue + coalesce('|'+DF.DynamicProposedKey,'')
                            )
                        )
from	{assetTable} A
        inner join {fieldTable} FC on FC.ExecutionID = A.ExecutionID and FC.ItemNumber = A.ItemNumber and FC.FieldName = 'FusionID'
        inner join {fieldTable} FN on FN.ExecutionID = A.ExecutionID and FN.ItemNumber = A.ItemNumber and FN.FieldName = 'Name'
        left join {fieldTable} FS on FS.ExecutionID = A.ExecutionID and FS.ItemNumber = A.ItemNumber and FS.FieldName = 'SourceID'
        outer apply (
            select		DF.ItemNumber,
                        STRING_AGG(coalesce(DF.LookupValue, DF.FieldValue, DFT.DefaultValue), '|') within group (order by DFT.ColumnOrder asc, DFT.Name asc) as DynamicProposedKey
            from		{fieldTable} DF
                        inner join FieldType DFT on DFT.ID = DF.FieldTypeID and DFT.IsPartOfKey = 1 and DF.ExecutionID = A.ExecutionID and DF.ItemNumber = A.ItemNumber
            group by    DF.ItemNumber
        ) DF
where	A.ExecutionID = @ExecutionID;

insert into #Keys
    select	A.ID,
            utility.GetHash(
                cast(@ID as nvarchar) + '|' + 
                cast(O.FusionID as nvarchar) + '|' + 
                COALESCE(
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
                new { executionID, at.ID }, commandTimeout: timeout, transaction: trans);

            }
            else if (at.Object == "ReferenceItemType")
            {
                Connection.Execute($@"
update  T
set     T.ProposedKey = utility.GetHash(cast(@ID as nvarchar) + '|' + S.ProposedKey) 
from    {assetTable} T
		inner join	(
					select		A.ItemNumber,
								F.FieldValue as ProposedKey
					from		{assetTable} A
								inner join {fieldTable} F on F.ExecutionID = A.ExecutionID and F.ItemNumber = A.ItemNumber and F.FieldName = 'Code'
					where		A.ExecutionID = @ExecutionID	
					) S on T.ExecutionID = @ExecutionID and S.ItemNumber = T.ItemNumber;

{keyTableTempCreation}

insert into #Keys
    select		A.ID,
                utility.GetHash(cast(@ID as nvarchar) + '|' + A.Code) as ActiveKey
    from		Asset A 
    where	    A.AssetTypeID = @ID;

{keyComparisonUpdateStatement}",
                new { executionID, at.ID }, commandTimeout: timeout, transaction: trans);
            }
            else
            {
                var activeKeySql = $@"
select		A.ID,
			utility.GetHash(cast(@ID as nvarchar) + '|' + STRING_AGG(coalesce(F.Value, FT.DefaultValue), '|') within group (order by FT.ColumnOrder asc, FT.Name asc)) as ActiveKey 
from		Asset A 
			inner join FieldType FT on FT.AssetTypeID = A.AssetTypeID and FT.IsPartOfKey = 1
			left join Field F on FT.ID = F.FieldTypeID and F.AssetID = A.ID
where	    A.AssetTypeID = @ID
group by    A.ID;";

                if (parentIntersectTypeId.HasValue)
                {
                    activeKeySql = $@"
select		A.ID,
			utility.GetHash(cast(@ID as nvarchar) + '|' + COALESCE(cast(P.Uid as nvarchar(50))+'|', '') + STRING_AGG(coalesce(F.Value, FT.DefaultValue), '|') within group (order by FT.ColumnOrder asc, FT.Name asc)) as ActiveKey
from		Asset A 
			left join [Intersect] I on I.IntersectTypeID = @intersectTypeID and I.Object = A.Object and I.ObjectID = A.ObjectID
			left join Asset P on P.Object = I.Subject and P.ObjectID = I.SubjectID
			inner join FieldType FT on FT.AssetTypeID = A.AssetTypeID and FT.IsPartOfKey = 1
			left join Field F on FT.ID = F.FieldTypeID and F.AssetID = A.ID
where		A.AssetTypeID = @ID
group by	A.ID, P.Uid";
                }

                Connection.Execute($@"
update  T
set     T.ProposedKey = utility.GetHash(cast(@ID as nvarchar) + '|' + S.ProposedKey) 
from    {assetTable} T
		inner join	(
					select		A.ItemNumber,
								COALESCE(cast(A.ParentUid as nvarchar(50))+'|', '') + STRING_AGG(coalesce(F.LookupValue, F.FieldValue), '|') within group (order by FT.ColumnOrder asc, FT.Name asc) as ProposedKey
					from		{assetTable} A
								inner join {fieldTable} F on F.ExecutionID = A.ExecutionID and F.ItemNumber = A.ItemNumber
								inner join FieldType FT on FT.AssetTypeID = @ID and FT.ID = F.FieldTypeID and FT.IsPartOfKey = 1
					where		A.ExecutionID = @ExecutionID
					group by	A.ItemNumber, A.ParentUid
					) S on T.ExecutionID = @ExecutionID and S.ItemNumber = T.ItemNumber;

{keyTableTempCreation}

insert into #Keys
    {activeKeySql} 

{keyComparisonUpdateStatement}",
                new { executionID, at.ID, intersectTypeID = parentIntersectTypeId ?? 0 }, commandTimeout: timeout, transaction: trans);
            }

        }

        public List<AssetMeasureModel> GetAssetMeasuresFromRuleResults(List<Guid> ruleResultUids)
        {
            var ruleResults = new DataTable();
            ruleResults.Columns.Add("RuleResultUid", typeof(Guid));
            ruleResultUids.ForEach(r => {
                var dr = ruleResults.NewRow();
                dr["RuleResultUid"] = r;
                ruleResults.Rows.Add(dr);
            });

            if (Database.Connection.State != ConnectionState.Open)
                Connection.Open();

            List<RuleResultChangedRawModel> rawMeasures;
            using (var trans = Connection.BeginTransaction())
            {
                Connection.Execute(@"create table #RuleResults (
                        RuleResultUid uniqueidentifier not null
                    )", transaction: trans);

                using (var bulkCopy = new SqlBulkCopy(Connection, SqlBulkCopyOptions.Default, trans))
                {
                    bulkCopy.BatchSize = 500;
                    bulkCopy.DestinationTableName = "#RuleResults";
                    bulkCopy.BulkCopyTimeout = 3600;

                    bulkCopy.ColumnMappings.Add("RuleResultUid", "RuleResultUid");

                    bulkCopy.WriteToServer(ruleResults);
                }

                rawMeasures = Connection.Query<RuleResultChangedRawModel>(@"
select	A.Uid as AssetUid,
		Re.EffectiveDate,
		Ma.Uid as MetricAssetUid,
		Mver.Uid as MetricAssetVersionUid
from	AssetResult Re,
		AssetResultEdge E,
		graph.AssetNode Ea,
		[metrics].[RollupPathSegment] Seg,
		[metrics].[RollupPath] Rol,
		[metrics].[AssetVersionRollupPath] VerRol,
		metrics.AssetVersion Mver,
		metrics.Asset Ma,
		metrics.Allocation Mal,
		AssetType T,
		Asset A
where	match(Ea-(E)->Re)
		and E.Class = 2
		and Seg.AssetTypeID = Ea.AssetTypeID
		and Rol.Uid = Seg.RollupPathUid
		and VerRol.RollupPathUid = Rol.Uid
		and Mver.Uid = VerRol.AssetVersionUid
		and Ma.Uid = Mver.AssetUid
		and Mal.Uid = Ma.AllocationUid
		and Mal.ScoreType = 2
		and Mal.IsExternallyCalculated = 0
		and T.Uid = Mal.AssetTypeUid
		and A.AssetTypeID = T.ID
        and Re.Uid in (select RuleResultUid from #RuleResults)", transaction: trans).ToList();
            }

            var structuredMeasures = rawMeasures
                .GroupBy(m => new { m.AssetUid, m.EffectiveDate })
                .Select(m => new AssetMeasureModel
                {
                    AssetUid = m.Key.AssetUid,
                    EffectiveDate = m.Key.EffectiveDate,
                    Measures = m.Select(o => new AssetMeasureChildModel
                    {
                        MetricAssetUid = o.MetricAssetUid,
                        MetricAssetVersionUid = o.MetricAssetVersionUid
                    }).ToList()
                }).ToList();

            return structuredMeasures;
        }

        public List<DataQualityResponseModel> UpsertAssetResults(List<IDataQualityUpsert> import, ApiExecution execution, int timeout = 3600, bool sendWorkflowEvents = true)
        {
            var results = new List<DataQualityResponseModel>();
            bool generalChecksCompleted = false;
            CurrentExecutionLocationModel currentLocation = null;

            SetApiExecutionProcessingStartTime(execution.ExecutionID);

            var dupes = import.Where(i => i.ExecutionItemUid.HasValue).GroupBy(i => i.ExecutionItemUid).Where(i => i.Count() > 1).Select(i => new { ExecutionItemUid = i.Key, Count = i.Count() }).ToList();

            if (dupes.Any())
            {
                execution.ErrorMessage = $"Duplicate execution item identifiers: {string.Join(", ", dupes.Select(i => i.ExecutionItemUid.ToString()))}. Identifiers must be unique within a batch.";
                results.AddRange(import.Select(i => new DataQualityResponseModel { ExecutionItemUid = i.ExecutionItemUid.Value, Message = execution.ErrorMessage, Success = false }));
            }
            else
            {
                try
                {
                    currentLocation = GetCurrentExecutionLocation(execution.ExecutionID, "api.ExecutionAssetResult");

                    if (currentLocation.HighestItemNumberProcessed > 0)
                    {
                        results.AddRange(
                            Query<DataQualityResponseModel>(
                                $"select ItemNumber, Uid, ExecutionItemUid, Success, Message from api.ExecutionAssetResult where ExecutionID = @ExecutionID and ItemNumber <= {currentLocation.HighestItemNumberProcessed}",
                                new { execution.ExecutionID }
                            )
                        );
                    }

                    #region Build data tables.

                    var table = new DataTable();
                    table.Columns.Add("ExecutionID", typeof(Guid));
                    table.Columns.Add("ItemNumber", typeof(int));
                    table.Columns.Add("ExecutionItemUid", typeof(Guid));
                    table.Columns.Add("EvaluatedAssetUid", typeof(Guid));
                    table.Columns.Add("OwningAssetUid", typeof(Guid));
                    table.Columns.Add("Uid", typeof(Guid));
                    table.Columns.Add("EffectiveDate", typeof(string));
                    table.Columns.Add("RunDate", typeof(string));
                    table.Columns.Add("PassCount", typeof(long));
                    table.Columns.Add("FailCount", typeof(long));
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
                            row["ExecutionItemUid"] = model.ExecutionItemUid ?? Guid.NewGuid();
                            row["ItemNumber"] = i;

                            if (model.RunDate != null)
                            {
                                row["RunDate"] = model.RunDate;

                                DateTime rundate;
                                if (!DateTime.TryParseExact(model.RunDate,
                                                       "yyyy-MM-dd HH:mm:ss",
                                                       System.Globalization.CultureInfo.InvariantCulture,
                                                       System.Globalization.DateTimeStyles.None,
                                                       out rundate))
                                {
                                    row["Message"] = String.Format(DataQualityErrors.InvalidFormatError, "RunDate", "yyyy-MM-dd HH:mm:ss");
                                    row["Success"] = 0;
                                }
                                else
                                {
                                    if (rundate > DateTime.Now)
                                    {
                                        row["Message"] = String.Format(DataQualityErrors.GreaterThanTodayError, "RunDate");
                                        row["Success"] = 0;
                                    }
                                    else if (rundate == DateTime.MinValue)
                                    {
                                        row["Message"] = String.Format(DataQualityErrors.GenericInvalidFieldValueError, model.RunDate, "RunDate");
                                        row["Success"] = 0;
                                    }
                                }
                            }

                            if (model is DataQualityInsertModel dataQualityInsertModel)
                            {
                                row["OwningAssetUid"] = dataQualityInsertModel.OwningAssetUid;

                                if (dataQualityInsertModel.EffectiveDate != null)
                                {
                                    row["EffectiveDate"] = dataQualityInsertModel.EffectiveDate;

                                    DateTime effectiveDate;
                                    if (!DateTime.TryParseExact(dataQualityInsertModel.EffectiveDate,
                                                           "yyyy-MM-dd",
                                                           System.Globalization.CultureInfo.InvariantCulture,
                                                           System.Globalization.DateTimeStyles.None,
                                                           out effectiveDate))
                                    {
                                        row["Message"] = String.Format(DataQualityErrors.InvalidFormatError, "EffectiveDate", "yyyy-MM-dd");
                                        row["Success"] = 0;
                                    }
                                    else if (effectiveDate == DateTime.MinValue)
                                    {
                                        row["Message"] = String.Format(DataQualityErrors.GenericInvalidFieldValueError, dataQualityInsertModel.EffectiveDate, "EffectiveDate");
                                        row["Success"] = 0;
                                    }
                                    else if (effectiveDate > DateTime.Now)
                                    {
                                        row["Message"] = String.Format(DataQualityErrors.GreaterThanTodayError, "EffectiveDate");
                                        row["Success"] = 0;
                                    }
                                }
                                else
                                {
                                    row["Message"] = String.Format(DataQualityErrors.RequiredFieldError, "EffectiveDate");
                                    row["Success"] = 0;
                                }



                                if (model.RunDate == null)
                                {
                                    row["Message"] = String.Format(DataQualityErrors.RequiredFieldError, "RunDate");
                                    row["Success"] = 0;
                                }

                                if (!model.PassCount.HasValue)
                                {
                                    row["Message"] = String.Format(DataQualityErrors.RequiredFieldError, "PassCount");
                                    row["Success"] = 0;
                                }

                                if (!model.FailCount.HasValue)
                                {
                                    row["Message"] = String.Format(DataQualityErrors.RequiredFieldError, "FailCount");
                                    row["Success"] = 0;
                                }
                            }

                            if (model is DataQualityUpdateModel dataQualityUpdateModel)
                            {
                                row["Uid"] = dataQualityUpdateModel.Uid;

                                if (!model.EvaluatedAssetUid.HasValue && model.RunDate == null && !model.PassCount.HasValue && !model.FailCount.HasValue)
                                {
                                    row["Message"] = DataQualityErrors.InvalidUpdateError;
                                    row["Success"] = 0;
                                }
                            }

                            if (model.EvaluatedAssetUid.HasValue)
                            {
                                row["EvaluatedAssetUid"] = model.EvaluatedAssetUid.Value;
                            }
                            else
                            {
                                row["EvaluatedAssetUid"] = DBNull.Value;
                            }
                            if (model.PassCount.HasValue)
                            {
                                row["PassCount"] = model.PassCount.Value;
                            }
                            else
                            {
                                row["PassCount"] = DBNull.Value;
                            }

                            if (model.FailCount.HasValue)
                            {
                                row["FailCount"] = model.FailCount.Value;
                            }
                            else
                            {
                                row["FailCount"] = DBNull.Value;
                            }

                            if (model.PassCount.HasValue && (model.PassCount < 0 || model.PassCount > 9223372036854775807))
                            {
                                row["Message"] = String.Format(DataQualityErrors.ValueBetweenError, "PassCount", 0, 9223372036854775807);
                                row["Success"] = 0;
                            }

                            if (model.FailCount.HasValue && (model.FailCount < 0 || model.FailCount > 9223372036854775807))
                            {
                                row["Message"] = String.Format(DataQualityErrors.ValueBetweenError, "FailCount", 0, 9223372036854775807);
                                row["Success"] = 0;
                            }

                            if (model.PassCount.HasValue && model.FailCount.HasValue)
                            {
                                ulong total = (ulong)model.PassCount.Value + (ulong)model.FailCount.Value;

                                if (total > 9223372036854775807)
                                {
                                    row["Message"] = String.Format(DataQualityErrors.GreaterThanError, "PassCount + FailCount", "9223372036854775807", 0);
                                    row["Success"] = 0;
                                }

                            }


                            table.Rows.Add(row);
                        }
                    }

                    #endregion

                    if (Database.Connection.State != ConnectionState.Open)
                        Connection.Open();

                    #region Bulk Copy

                    using (SqlBulkCopy bulkCopy = new SqlBulkCopy(Connection))
                    {

                        bulkCopy.BatchSize = SqlBulkBatchSize;
                        bulkCopy.DestinationTableName = "api.ExecutionAssetResult";
                        bulkCopy.BulkCopyTimeout = SqlBulkBatchTimeout;

                        bulkCopy.ColumnMappings.Add("ExecutionID", "ExecutionID");
                        bulkCopy.ColumnMappings.Add("ItemNumber", "ItemNumber");
                        bulkCopy.ColumnMappings.Add("ExecutionItemUid", "ExecutionItemUid");
                        bulkCopy.ColumnMappings.Add("OwningAssetUid", "OwningAssetUid");
                        bulkCopy.ColumnMappings.Add("EvaluatedAssetUid", "EvaluatedAssetUid");
                        bulkCopy.ColumnMappings.Add("EffectiveDate", "EffectiveDate");
                        bulkCopy.ColumnMappings.Add("RunDate", "RunDate");
                        bulkCopy.ColumnMappings.Add("PassCount", "PassCount");
                        bulkCopy.ColumnMappings.Add("FailCount", "FailCount");
                        bulkCopy.ColumnMappings.Add("Message", "Message");
                        bulkCopy.ColumnMappings.Add("Success", "Success");
                        bulkCopy.ColumnMappings.Add("Uid", "Uid");

                        bulkCopy.WriteToServer(table);
                    }

                    #endregion

                    #region Log data errors                    


                    var checkSQL = $@"
    	                            --check user permissions
                                    declare @IsAdministrator bit = 0
                                    select	@IsAdministrator = IsAdministrator
                                    from	reporting.Global_Resource
                                    where	ResourceID = @ResourceID

                                    if @IsAdministrator = 0
                                    begin
                                        -- check on insert
	                                    update	EAR
	                                    set		EAR.Success = 0,
			                                    EAR.[Message] = coalesce([Message] + '; ', '') + 'User does not have permission to create this result.'
	                                    from    api.ExecutionAssetResult EAR
			                                    inner join api.Execution E on E.ExecutionID = EAR.ExecutionID 
											                                    and E.ExecutionID = @executionID and EAR.Success is null and UPPER(E.Method)='POST'
			                                    inner join 
			                                    Asset A on (EAR.OwningAssetUid = A.uid) 
			                                    and EAR.OwningAssetUid is not null												
                                                outer apply dbo.UserAssetPermissions(E.ResourceID, A.AssetTypeID) P 
                                                Where 
                                                P.PermissionsBitMask is null
                                                or 
                                                (
	                                                P.AssetTypeID = A.AssetTypeID 
	                                                and 
	                                                (
		                                                P.AssetID <> A.ID 
		                                                and
		                                                P.AssetID <> 0
	                                                ) 
	                                                and 
	                                                P.PermissionsBitMask & @p <> @p
                                                )			                                    
                                        
                                        -- Check on update
                                        update	EAR
	                                    set		EAR.Success = 0,
			                                    EAR.[Message] = coalesce([Message] + '; ', '') + 'User does not have permission to update this result.'
	                                    from    api.ExecutionAssetResult EAR                                                
                                        inner join api.Execution E on E.ExecutionID = EAR.ExecutionID and E.ExecutionID=@ExecutionID and EAR.Success is null and UPPER(E.Method)='PUT'
										inner join AssetResult AR on AR.uid =EAR.Uid
                                        inner join AssetResultEdge ARE on AR.$node_id = ARE.$to_id and ARE.class = {(int)ResultRelationClass.Owns}
										inner join graph.AssetNode AN on AN.$node_id = ARE.$from_id
                                        outer apply dbo.UserAssetPermissions(E.ResourceID, AN.AssetTypeID) P 
                                        Where 
                                        P.PermissionsBitMask is null
                                        or 
                                        (
	                                        P.AssetTypeID = AN.AssetTypeID 
	                                        and 
	                                        (
		                                        P.AssetID <> AN.ID 
		                                        and
		                                        P.AssetID <> 0
	                                        ) 
	                                        and 
	                                        P.PermissionsBitMask & @p <> @p
                                        )
                                    end

	                                -- check Uid on Put
	                                update EAR
                                    set		Success = 0,
		                                    [Message] = coalesce([Message] + '; ', '') + 'Invalid Rule Result UID value'
                                    from api.[ExecutionAssetResult] EAR
                                        inner join api.Execution AE on AE.ExecutionID = EAR.ExecutionID
                                        left join AssetResult AR on AR.Uid = EAR.Uid
                                    where 
		                                AE.Method = 'PUT'
		                                and EAR.ExecutionID = @ExecutionID 		
		                                and 
		                                (EAR.Uid is null or EAR.Uid = '00000000-0000-0000-0000-000000000000' or AR.Uid is null)                                        

	                                -- check Owning Asset Uid
	                                update EAR
                                    set		Success = 0,
		                                    [Message] = coalesce([Message] + '; ', '') + 'Invalid OwningAssetUid value'
                                    from api.[ExecutionAssetResult] EAR
                                        inner join api.Execution AE on AE.ExecutionID = EAR.ExecutionID
		                                left join asset a on a.uid = EAR.OwningAssetUid
		                                left Join assettype at on at.id = a.AssetTypeID
                                    where 
                                        AE.Method = 'POST'
                                        AND
		                                EAR.ExecutionID = @ExecutionID 		
		                                and 
		                                (
			                                (EAR.OwningAssetUid is null or EAR.OwningAssetUid = '00000000-0000-0000-0000-000000000000')
			                                or
			                                (EAR.OwningAssetUid is not null and a.ID is null)
			                                or
			                                (EAR.OwningAssetUid is not null and a.ID is not null and at.Class <> {(int)AssetTypeClass.Rule})
			                                or
			                                A.State = {(int)State.InActive}
		                                )

	                                -- check Evaluated Asset Uid
	                                update EAR
                                    set		Success = 0,
		                                    [Message] = coalesce([Message] + '; ', '') + 'Invalid EvaluatedAssetUid value'
                                    from api.[ExecutionAssetResult] EAR
                                        inner join api.Execution AE on AE.ExecutionID = EAR.ExecutionID
		                                left join asset a on a.uid = EAR.EvaluatedAssetUid
		                                left Join assettype at on at.id = a.AssetTypeID
                                    where 		                               
		                                EAR.ExecutionID = @ExecutionID 		
		                                And
		                                EAR.EvaluatedAssetUid is not null
		                                and 
		                                (
			                                a.ID is null -- no match
			                                or
			                                (a.ID is not null and at.Class not in ({(int)AssetTypeClass.TechnicalAsset}, {(int)AssetTypeClass.BusinessAsset}))-- match but wrong asset type
			                                or
			                                A.State = {(int)State.InActive} -- inactive state
		                                )	                                    

                                    -- check PassCount/FailCount on Put
	                                update EAR
                                    set		Success = 0,
		                                    [Message] = coalesce([Message] + '; ', '') + '{String.Format(DataQualityErrors.GreaterThanError, "PassCount + FailCount", "9223372036854775807", 0)}'
                                    from api.[ExecutionAssetResult] EAR
                                        inner join api.Execution AE on AE.ExecutionID = EAR.ExecutionID 
                                        left join AssetResult AR on AR.Uid = EAR.Uid
                                    where 
		                                AE.Method = 'PUT'
		                                and EAR.ExecutionID = @ExecutionID
                                        and success is null
		                                and (
                                        CASE
                                            WHEN EAR.FailCount is not null and EAR.PassCount is null and (9223372036854775807 - AR.PassCount - EAR.FailCount)<0 THEN 1
                                            WHEN EAR.PassCount is not null and EAR.FailCount is null and (9223372036854775807 - AR.FailCount - EAR.PassCount)<0 THEN 1
                                            WHEN EAR.PassCount is not null and EAR.FailCount is not null and (9223372036854775807 - EAR.Passcount - EAR.FailCount)<0 THEN 1
                                            ELSE 0
                                        END)=1
                                   ";

                    Connection.Execute(checkSQL, new { ResourceID = CurrentResourceID, execution.ExecutionID, p = Permission.ModifyAsset }, commandTimeout: timeout);

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

                    results = new List<DataQualityResponseModel>();
                    results.AddRange(import.Select(i => new DataQualityResponseModel { Message = msg, Success = false }));
                }

                if (generalChecksCompleted)
                {
                    int loopSize = 250;
                    int numberOfLoops = (int)Math.Ceiling((decimal)(execution.Total - currentLocation.HighestItemNumberProcessed) / loopSize);
                    int beginItemNumber = currentLocation.HighestItemNumberProcessed + 1;
                    int endItemNumber = currentLocation.HighestItemNumberProcessed + loopSize;

                    string assetResultSQL = $@"create table #ObjectMergeTableAssetResult (Uid uniqueidentifier, ItemNumber int, [Operation] varchar(10));
                                                CREATE NONCLUSTERED INDEX IX_TempObjectMergeTableAssetResult ON #ObjectMergeTableAssetResult ( ItemNumber ASC );

                                                Merge into AssetResult AR
                                                using (
                                                        select  ItemNumber, 
                                                                UID,
                                                                EffectiveDate,
			                                                    RunDate,
			                                                    PassCount,
			                                                    FailCount			                                                    
                                                        from    api.[ExecutionAssetResult]
                                                        where   ExecutionID = @ExecutionID
                                                                and Success is null
                                                                and ItemNumber between @beginItemNumber and @endItemNumber
                                                        ) S
                                                ON S.UID = AR.UID
                                                WHEN NOT MATCHED THEN
                                                INSERT ([Uid]
			                                                ,[EffectiveDate]
			                                                ,[RunDate]
			                                                ,[PassCount]
			                                                ,[FailCount]
			                                                ,[CreatedOn]
			                                                ,[CreatedBy]
			                                                ,[UpdatedOn]
			                                                ,[UpdatedBy])
		                                                VALUES
			                                                (NEWID()
			                                                ,S.EffectiveDate
			                                                ,S.RunDate
			                                                ,S.PassCount
			                                                ,S.FailCount
			                                                ,@requestDate
			                                                ,@userId
			                                                ,@requestDate
			                                                ,@userId)	                                                
                                                WHEN MATCHED THEN
                                                 UPDATE 
                                                    SET RunDate = (case when S.RunDate is null then AR.RunDate else S.RunDate end),
                                                    PassCount = (case when S.PassCount is null then AR.PassCount else S.PassCount end),
                                                    FailCount = (case when S.FailCount is null then AR.FailCount else S.FailCount end),
                                                    UpdatedOn = @requestDate,
                                                    UpdatedBy = @userId
                                                output inserted.Uid, S.ItemNumber, $action into #ObjectMergeTableAssetResult;

                                                    --Update Exection record with new Uid
                                                    Update EAR
                                                    set Uid = MTR.Uid
                                                    from 
                                                        api.ExecutionAssetResult EAR 
                                                        inner join 
                                                        #ObjectMergeTableAssetResult MTR on EAR.ItemNumber=MTR.ItemNumber and EAR.ExecutionID=@ExecutionID                                                                                                         
                                                    
                                                    --Add new owning asset record in Edge table (insert only)
	                                                INSERT INTO [dbo].[AssetResultEdge]	($from_id,$to_id,[Class])
	                                                select 
		                                                AN.$node_Id, AR.$node_Id, {(int)ResultRelationClass.Owns}
	                                                from 
		                                                AssetResult AR 
		                                                inner join
		                                                #ObjectMergeTableAssetResult MTR on MTR.Uid = AR.Uid
		                                                inner join 
		                                                api.ExecutionAssetResult EAR on MTR.Uid = EAR.Uid 
		                                                inner join 
		                                                graph.AssetNode AN on AN.Uid = EAR.[OwningAssetUid]
                                                        inner join 
                                                        api.Execution E on EAR.ExecutionID = E.ExecutionID and E.ExecutionID=@ExecutionID and E.Method='POST'
                                                    
                                                    --Delete existing evaluated edge record if there is one.
                                                    DELETE ARE FROM                                                     
                                                        AssetResultEdge ARE 
                                                        inner join 
                                                        AssetResult AR on AR.$node_id = ARE.$to_id and ARE.Class = {(int)ResultRelationClass.EvaluatedBy}
                                                        inner join 
                                                        #ObjectMergeTableAssetResult MTR on MTR.Uid = AR.Uid
                                                        inner join 
                                                        api.ExecutionAssetResult EAR on MTR.Uid = EAR.Uid and EAR.ExecutionID = @ExecutionID and EAR.Success is null and EAR.EvaluatedAssetUid is not null 
                                                        inner join 
                                                        api.Execution E on EAR.ExecutionID = E.ExecutionID and E.ExecutionID=@ExecutionID and E.Method='PUT'                                                  

                                                    -- and new edge records
	                                                INSERT INTO [dbo].[AssetResultEdge]	($from_id,$to_id,[Class])
	                                                select 
		                                                AN.$node_Id, AR.$node_Id, {(int)ResultRelationClass.EvaluatedBy}
	                                                from 
		                                                AssetResult AR 
		                                                inner join 
		                                                #ObjectMergeTableAssetResult MTR on MTR.Uid = AR.Uid
		                                                inner join 
		                                                api.ExecutionAssetResult EAR on MTR.Uid = EAR.Uid and EAR.ExecutionID = @ExecutionID
		                                                inner join 
		                                                graph.AssetNode AN on AN.Uid = EAR.EvaluatedAssetUid
                                                        left Join AssetResultEdge ARE on ARE.$to_id = AR.$node_Id and ARE.Class = {(int)ResultRelationClass.EvaluatedBy}-- find any results already in Edge table.
                                                    where
                                                        ARE.$to_id is null --only insert if a matching record does not already exist                                                   

	                                                Update EAR
	                                                set EAR.success = 1 
	                                                FROM 
	                                                api.ExecutionAssetResult EAR
	                                                inner join 
	                                                #ObjectMergeTableAssetResult MTR on MTR.Uid = EAR.Uid and EAR.ExecutionID = @ExecutionID";

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
                                    Connection.Execute(assetResultSQL, new { ExecutionID = execution.ExecutionID, beginItemNumber = beginItemNumber, endItemNumber = endItemNumber, userId = CurrentResourceID, requestDate = DateTime.UtcNow }, transaction: trans, commandTimeout: timeout);
                                    trans.Commit();
                                    runCompleted = true;

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

                                    retryCount++;

                                    if (retryCount > API_V2_RETRY_LIMIT)
                                    {
                                        LogLoopExecutionError(execution.ExecutionID, beginItemNumber, endItemNumber, "api.ExecutionAssetResult", ex.GetFullExceptionData(false), timeout);
                                    }
                                }
                            }
                        }

                        results.AddRange(
                                Query<DataQualityResponseModel>(
                                    $"select ItemNumber, Uid, ExecutionItemUid, Success, Message from api.ExecutionAssetResult where ExecutionID = @ExecutionID and ItemNumber between @beginItemNumber and @endItemNumber",
                                    new { execution.ExecutionID, beginItemNumber, endItemNumber }
                                )
                            );

                        beginItemNumber += loopSize;
                        endItemNumber += loopSize;
                    }

                }
            }

            var ruleResultUids = results.Where(i => i.Success).Select(i => i.Uid.Value).ToList();
            if (ruleResultUids.Count > 0) {
                var assetMeasures = GetAssetMeasuresFromRuleResults(ruleResultUids);
                if (assetMeasures.Count > 0)
                {
                    SendScoreEventWithPayload(execution.ExecutionID, ScoreQueueChangeType.AssetMeasures, assetMeasures);
                }
            }

            return results;
        }

        public List<DataQualityDeleteResponseModel> DeleteAssetResults(List<DataQualityDeleteModel> import, ApiExecution execution, int timeout = 3600)
        {
            var results = new List<DataQualityDeleteResponseModel>();
            bool generalChecksCompleted = false;
            CurrentExecutionLocationModel currentLocation = null;

            SetApiExecutionProcessingStartTime(execution.ExecutionID);

            var dupes = import.Where(i => i.ExecutionItemUid.HasValue).GroupBy(i => i.ExecutionItemUid).Where(i => i.Count() > 1).Select(i => new { ExecutionItemUid = i.Key, Count = i.Count() }).ToList();

            if (dupes.Any())
            {
                execution.ErrorMessage = $"Duplicate execution item identifiers: {string.Join(", ", dupes.Select(i => i.ExecutionItemUid.ToString()))}. Identifiers must be unique within a batch.";
                results.AddRange(import.Select(i => new DataQualityDeleteResponseModel { ExecutionItemUid = i.ExecutionItemUid.Value, Message = execution.ErrorMessage, Success = false }));
            }
            else
            {
                try
                {
                    currentLocation = GetCurrentExecutionLocation(execution.ExecutionID, "api.ExecutionDeleteAssetResult");

                    if (currentLocation.HighestItemNumberProcessed > 0)
                    {
                        results.AddRange(
                            Query<DataQualityDeleteResponseModel>(
                                $"select ExecutionItemUid, Success, Message from api.ExecutionDeleteAssetResult where ExecutionID = @ExecutionID and ItemNumber <= {currentLocation.HighestItemNumberProcessed}",
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
                    table.Columns.Add("EvaluatedAssetUid", typeof(Guid));
                    table.Columns.Add("OwningAssetUid", typeof(Guid));
                    table.Columns.Add("EffectiveDateStart", typeof(string));
                    table.Columns.Add("EffectiveDateEnd", typeof(string));
                    table.Columns.Add("RunDateStart", typeof(string));
                    table.Columns.Add("RunDateEnd", typeof(string));
                    table.Columns.Add("Message", typeof(string));
                    table.Columns.Add("Success", typeof(bool));

                    #endregion

                    #region Generate data sets

                    for (int i = 1; i <= import.Count; i++)
                    {
                        if (i > currentLocation.HighestItemNumber)
                        {
                            var model = import[i - 1];
                            List<string> messages = new List<string>();
                            var row = table.NewRow();
                            DateTime effectiveDateStart = new DateTime();
                            DateTime runDateStart = new DateTime();

                            row["ExecutionID"] = execution.ExecutionID;
                            row["ExecutionItemUid"] = model.ExecutionItemUid ?? Guid.NewGuid();
                            row["ItemNumber"] = i;

                            if (model.Uid.HasValue)
                            {
                                row["Uid"] = model.Uid.Value;
                            }
                            else
                            {
                                row["Uid"] = DBNull.Value;
                            }

                            if (model.OwningAssetUid.HasValue)
                            {
                                row["OwningAssetUid"] = model.OwningAssetUid.Value;
                            }
                            else
                            {
                                row["OwningAssetUid"] = DBNull.Value;
                            }

                            if (model.EvaluatedAssetUid.HasValue)
                            {
                                row["EvaluatedAssetUid"] = model.EvaluatedAssetUid.Value;
                            }
                            else
                            {
                                row["EvaluatedAssetUid"] = DBNull.Value;
                            }

                            if (model.EffectiveDateStart != null)
                            {
                                row["EffectiveDateStart"] = model.EffectiveDateStart;

                                if (!DateTime.TryParseExact(model.EffectiveDateStart,
                                                       "yyyy-MM-dd",
                                                       System.Globalization.CultureInfo.InvariantCulture,
                                                       System.Globalization.DateTimeStyles.None,
                                                       out effectiveDateStart))
                                {
                                    row["Message"] = String.Format(DataQualityErrors.InvalidFormatError, "EffectiveDateStart", "yyyy-MM-dd");
                                    row["Success"] = 0;
                                }
                            }

                            if (model.EffectiveDateEnd != null)
                            {
                                row["EffectiveDateEnd"] = model.EffectiveDateEnd;

                                DateTime effectiveDateEnd;
                                if (!DateTime.TryParseExact(model.EffectiveDateEnd,
                                                       "yyyy-MM-dd",
                                                       System.Globalization.CultureInfo.InvariantCulture,
                                                       System.Globalization.DateTimeStyles.None,
                                                       out effectiveDateEnd))
                                {
                                    row["Message"] = String.Format(DataQualityErrors.InvalidFormatError, "EffectiveDateEnd", "yyyy-MM-dd");
                                    row["Success"] = 0;
                                }
                                else if (model.EffectiveDateStart != null && effectiveDateStart > effectiveDateEnd)
                                {
                                    messages.Add(String.Format(DataQualityErrors.GreaterThanError, "EffectiveDateStart", "EffectiveDateEnd"));
                                    row["Success"] = 0;
                                }
                            }

                            if (model.RunDateStart != null)
                            {
                                row["RunDateStart"] = model.RunDateStart;

                                if (!DateTime.TryParseExact(model.RunDateStart,
                                                       "yyyy-MM-dd HH:mm:ss",
                                                       System.Globalization.CultureInfo.InvariantCulture,
                                                       System.Globalization.DateTimeStyles.None,
                                                       out runDateStart))
                                {
                                    row["Message"] = String.Format(DataQualityErrors.InvalidFormatError, "RunDateStart", "yyyy-MM-dd HH:mm:ss");
                                    row["Success"] = 0;
                                }
                            }

                            if (model.RunDateEnd != null)
                            {
                                row["RunDateEnd"] = model.RunDateEnd;

                                DateTime runDateEnd;
                                if (!DateTime.TryParseExact(model.RunDateEnd,
                                                       "yyyy-MM-dd HH:mm:ss",
                                                       System.Globalization.CultureInfo.InvariantCulture,
                                                       System.Globalization.DateTimeStyles.None,
                                                       out runDateEnd))
                                {
                                    row["Message"] = String.Format(DataQualityErrors.InvalidFormatError, "RunDateEnd", "yyyy-MM-dd HH:mm:ss");
                                    row["Success"] = 0;
                                }
                                else if (model.RunDateStart != null && runDateStart > runDateEnd)
                                {
                                    messages.Add(String.Format(DataQualityErrors.GreaterThanError, "RunDateStart", "RunDateEnd"));
                                    row["Success"] = 0;
                                }
                            }
                            if ((!model.Uid.HasValue || model.Uid.Value == Guid.Empty) && (!model.OwningAssetUid.HasValue || model.OwningAssetUid.Value == Guid.Empty) && (!model.EvaluatedAssetUid.HasValue || model.EvaluatedAssetUid.Value == Guid.Empty))
                            {
                                messages.Add("At least one of the following MUST be provided: Uid, OwningAssetUid, EvaluatedAssetUid.");
                                row["Success"] = 0;
                            }

                            row["Message"] = string.Join(";", messages.ToArray());


                            table.Rows.Add(row);
                        }
                    }

                    #endregion

                    if (Database.Connection.State != ConnectionState.Open)
                        Connection.Open();

                    #region Bulk Copy

                    using (SqlBulkCopy bulkCopy = new SqlBulkCopy(Connection))
                    {

                        bulkCopy.BatchSize = SqlBulkBatchSize;
                        bulkCopy.DestinationTableName = "api.ExecutionDeleteAssetResult";
                        bulkCopy.BulkCopyTimeout = SqlBulkBatchTimeout;

                        bulkCopy.ColumnMappings.Add("ExecutionID", "ExecutionID");
                        bulkCopy.ColumnMappings.Add("ItemNumber", "ItemNumber");
                        bulkCopy.ColumnMappings.Add("ExecutionItemUid", "ExecutionItemUid");
                        bulkCopy.ColumnMappings.Add("Uid", "Uid");
                        bulkCopy.ColumnMappings.Add("OwningAssetUid", "OwningAssetUid");
                        bulkCopy.ColumnMappings.Add("EvaluatedAssetUid", "EvaluatedAssetUid");
                        bulkCopy.ColumnMappings.Add("EffectiveDateStart", "EffectiveDateStart");
                        bulkCopy.ColumnMappings.Add("EffectiveDateEnd", "EffectiveDateEnd");
                        bulkCopy.ColumnMappings.Add("RunDateStart", "RunDateStart");
                        bulkCopy.ColumnMappings.Add("RunDateEnd", "RunDateEnd");
                        bulkCopy.ColumnMappings.Add("Message", "Message");
                        bulkCopy.ColumnMappings.Add("Success", "Success");

                        bulkCopy.WriteToServer(table);
                    }

                    #endregion

                    #region Log data errors 

                    var checkSQL = $@"
    	                            --check user permissions
                                    declare @IsAdministrator bit = 0
                                    select	@IsAdministrator = IsAdministrator
                                    from	reporting.Global_Resource
                                    where	ResourceID = @ResourceID                                    

                                    if @IsAdministrator = 0
                                    begin
	                                    update	DAR
	                                    set		DAR.Success = 0,
			                                    DAR.[Message] = coalesce([Message] + '; ', '') + 'User does not have permission to delete this result.'
	                                    from    api.ExecutionDeleteAssetResult DAR                                                
                                        inner join api.Execution E on E.ExecutionID = DAR.ExecutionID and E.ExecutionID=@ExecutionID
                                        left join AssetResult AR_result on DAR.Uid = AR_result.uid
                                        left join graph.AssetNode AN_eval on DAR.EvaluatedAssetUid = AN_eval.uid 
                                        left join AssetResultEdge ARE_eval on ARE_eval.$From_id = AN_eval.$node_id and ARE_eval.class = {(int)ResultRelationClass.EvaluatedBy} and AN_eval.Uid = DAR.EvaluatedAssetUid -- find all the matching recored in the edge table for the evaludated asset
                                        left join AssetResultEdge ARE_own on (ARE_eval.$to_id = ARE_own.$to_id or ARE_own.$to_id = AR_result.$node_id) and ARE_own.class = {(int)ResultRelationClass.Owns} -- join the edge table to itself but only get the owning records.
                                        left join graph.AssetNode AN_own on DAR.OwningAssetUid = AN_own.uid or ARE_own.$From_id = AN_own.$node_id
                                        outer apply dbo.UserAssetPermissions(E.ResourceID, AN_own.AssetTypeID) P 
                                        Where 
                                        P.PermissionsBitMask is null
                                        or 
                                        (
	                                        P.AssetTypeID = AN_own.AssetTypeID 
	                                        and 
	                                        (
		                                        P.AssetID <> AN_own.ID 
		                                        and
		                                        P.AssetID <> 0
	                                        ) 
	                                        and 
	                                        P.PermissionsBitMask & @p <> @p
                                        )
                                                
                                    end
                                                                        
	                                -- check Owning Asset Uid
	                                update DAR
                                    set		Success = 0,
		                                    [Message] = coalesce([Message] + '; ', '') + 'Invalid OwningAssetUid value'
                                    from api.[ExecutionDeleteAssetResult] DAR
                                        inner join api.Execution AE on AE.ExecutionID = DAR.ExecutionID
		                                left join asset a on a.uid = DAR.OwningAssetUid
		                                left Join assettype at on at.id = a.AssetTypeID
                                    where 
		                                DAR.ExecutionID = @ExecutionID 		
		                                and 
                                        DAR.OwningAssetUid is not null
                                        AND
                                        DAR.OwningAssetUid <> '00000000-0000-0000-0000-000000000000'
                                        AND
		                                (			                                			                             
			                                a.ID is null
			                                or
			                                (a.ID is not null and at.Class <> {(int)AssetTypeClass.Rule})
			                                or
			                                A.State = {(int)State.InActive}
		                                )

	                                -- check Evaluated Asset Uid
	                                update DAR
                                    set		Success = 0,
		                                    [Message] = coalesce([Message] + '; ', '') + 'Invalid EvaluatedAssetUid value'
                                    from api.[ExecutionDeleteAssetResult] DAR
                                        inner join api.Execution AE on AE.ExecutionID = DAR.ExecutionID
		                                left join asset a on a.uid = DAR.EvaluatedAssetUid
		                                left Join assettype at on at.id = a.AssetTypeID
                                    where 		                               
		                                DAR.ExecutionID = @ExecutionID 		
		                                And
		                                DAR.EvaluatedAssetUid is not null
                                        AND
                                        DAR.EvaluatedAssetUid <> '00000000-0000-0000-0000-000000000000'
		                                and 
		                                (
			                                a.ID is null -- no match
			                                or
			                                (a.ID is not null and at.Class not in ({(int)AssetTypeClass.TechnicalAsset}, {(int)AssetTypeClass.BusinessAsset}))-- match but wrong asset type
			                                or
			                                A.State = {(int)State.InActive} -- inactive state
		                                )                                      

                                   ";

                    Connection.Execute(checkSQL, new { ResourceID = CurrentResourceID, execution.ExecutionID, p = Permission.DeleteAsset }, commandTimeout: timeout);

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

                    results = new List<DataQualityDeleteResponseModel>();
                    results.AddRange(import.Select(i => new DataQualityDeleteResponseModel { Message = msg, Success = false }));
                }

                if (generalChecksCompleted)
                {
                    int loopSize = 250;
                    int numberOfLoops = (int)Math.Ceiling((decimal)(execution.Total - currentLocation.HighestItemNumberProcessed) / loopSize);
                    int beginItemNumber = currentLocation.HighestItemNumberProcessed + 1;
                    int endItemNumber = currentLocation.HighestItemNumberProcessed + loopSize;

                    var querySuffix = $"DAR.Success is null and DAR.ExecutionID = @ExecutionID and DAR.ItemNumber between @beginItemNumber and @endItemNumber";

                    var updateOnSuccess = $@"update DAR set DAR.Success = 1 from api.ExecutionDeleteAssetResult DAR inner join
	                                                #ObjectDeleteAssetEdge DAE on DAE.ExecutionItemUid = DAR.ExecutionItemUid where {querySuffix}";

                    string deleteAssetResultSQL = $@"
create table #ObjectDeleteAssetEdge ([uid] uniqueidentifier, class int, ItemNumber int, ExecutionItemUid uniqueidentifier, [Operation] varchar(10));
CREATE NONCLUSTERED INDEX IX_TempObjectMergeAssetEdge ON #ObjectDeleteAssetEdge ( ItemNumber ASC );

                                                merge into AssetResultEdge DARE
                                                using 
                                                (select 
	                                                ARE.$from_id as from_id,ARE.$to_id as to_id, AR.Uid, ARE.Class, DAR.itemnumber, DAR.ExecutionItemUid
	                                                --AR.Uid, ARE.[Class], AR.RunDate, AR.EffectiveDate
                                                from 
	                                                AssetResult AR, assetResultedge ARE, graph.AssetNode AN, API.[ExecutionDeleteAssetResult] DAR
                                                where 
	                                                DAR.ExecutionID = @executionID
	                                                and
                                                    DAR.Success is null
                                                    and DAR.ItemNumber between @beginItemNumber and @endItemNumber
                                                    AND
	                                                Match (AN -(ARE)-> AR)
	                                                and 
	                                                ((DAR.Uid is null or DAR.Uid ='00000000-0000-0000-0000-000000000000') or AR.Uid = DAR.Uid)
	                                                and
	                                                (
		                                                (DAR.OwningAssetUid is null or DAR.OwningAssetUid ='00000000-0000-0000-0000-000000000000') or AR.Uid in 
		                                                (	
			                                                select 
				                                                AR1.Uid
			                                                from 
				                                                AssetResult AR1, assetResultedge ARE1, graph.AssetNode AN1					
			                                                where 
				                                                Match (AN1 -(ARE1)-> AR1)
				                                                and 
				                                                AN1.Uid = DAR.owningAssetUid
				                                                and
				                                                ARE1.Class = {(int)ResultRelationClass.Owns}	
		                                                )
	                                                )
	                                                and 
	                                                (	
		                                                (DAR.EvaluatedAssetUid is null or DAR.EvaluatedAssetUid ='00000000-0000-0000-0000-000000000000')  or AR.Uid in 
		                                                (
			                                                select 
				                                                AR2.Uid
			                                                from 
				                                                AssetResult AR2, assetResultedge ARE2, graph.AssetNode AN2					
			                                                where 
				                                                Match (AN2 -(ARE2)-> AR2)
				                                                and 
				                                                AN2.Uid = DAR.evaluatedAssetUid
				                                                and
				                                                ARE2.Class = {(int)ResultRelationClass.EvaluatedBy}
		                                                )
	                                                )
	                                                and 
	                                                (
		                                                (
                                                            (DAR.EvaluatedAssetUid is null or DAR.EvaluatedAssetUid ='00000000-0000-0000-0000-000000000000')
			                                                
		                                                )
		                                                or
		                                                (
			                                               DAR.EvaluatedAssetUid is not null and ARE.class =  {(int)ResultRelationClass.EvaluatedBy}
		                                                )
	                                                )
	                                                and
	                                                (
		                                                DAR.EffectiveDateStart is null or DAR.EffectiveDateStart <= AR.EffectiveDate
	                                                )
	                                                and
	                                                (
		                                                DAR.EffectiveDateEnd is null or DAR.EffectiveDateEnd >= AR.EffectiveDate
	                                                )
	                                                and
	                                                (
		                                                DAR.RunDateStart is null or AR.RunDate >= DAR.RunDateStart
	                                                )
	                                                and
	                                                (
		                                                DAR.RunDateEnd is null or AR.RunDate <= DAR.RunDateEnd 
												  
	                                                )
                                                ) R on R.from_id = DARE.$from_id and R.to_id = DARE.$to_id
                                                WHEN MATCHED THEN DELETE
                                                output R.uid, R.class, R.itemnumber, R.ExecutionItemUid, $action into #ObjectDeleteAssetEdge;

                                                merge into AssetResult AR
                                                using (
	                                                select AR1.uid
	                                                FROM AssetResult AR1
	                                                INNER JOIN #ObjectDeleteAssetEdge MAE
	                                                  ON AR1.UID=MAE.Uid
	                                                left join 
	                                                assetResultEdge ARE on ARE.$to_id = AR1.$node_id
	                                                Where ARE.$to_id is null
	                                                ) R on R.Uid = AR.Uid
                                                WHEN MATCHED THEN DELETE;

                                                {updateOnSuccess}
                                                    ";
                    
                    // TODO: Gotta figure out how to get asset measure records BEFORe we delete the results above.
                    
                    //var ruleResultUids = import.Where(i => i.Uid).Select(i => i.Uid.Value).ToList();
                    //var assetMeasures = GetAssetMeasuresFromRuleResults(ruleResultUids);
                    //SendScoreEventWithPayload(execution.ExecutionID, ScoreQueueChangeType.AssetMeasures, assetMeasures);

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
                                    Connection.Execute(deleteAssetResultSQL, new { execution.ExecutionID, beginItemNumber, endItemNumber }, transaction: trans, commandTimeout: timeout);
                                    trans.Commit();
                                    runCompleted = true;

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

                                    retryCount++;

                                    if (retryCount > API_V2_RETRY_LIMIT)
                                    {
                                        LogLoopExecutionError(execution.ExecutionID, beginItemNumber, endItemNumber, "api.ExecutionDeleteAssetResult", ex.GetFullExceptionData(false), timeout);
                                    }
                                }
                            }
                        }

                        results.AddRange(
                                Query<DataQualityDeleteResponseModel>(
                                    $"select ExecutionItemUid, Success, Message from api.ExecutionDeleteAssetResult where ExecutionID = @ExecutionID and ItemNumber between @beginItemNumber and @endItemNumber",
                                    new { execution.ExecutionID, beginItemNumber, endItemNumber }
                                )
                            );

                        beginItemNumber += loopSize;
                        endItemNumber += loopSize;
                    }
                }
            }

            //SendScoreEventWithPayload(execution.ExecutionID, ScoreQueueChangeType.AssetMeasures, import);

            return results;
        }

        public List<ResponsibilityRuleUpsertResponseModel> UpsertResponsibilityRules(ApiExecution execution, Guid responsibilityTypeUid, List<ResponsibilityRuleUpsertModel> import, int timeout = 3600)
        {
            var results = new List<ResponsibilityRuleUpsertResponseModel>();
            bool generalChecksCompleted = false;
            CurrentExecutionLocationModel currentLocation = null;

            SetApiExecutionProcessingStartTime(execution.ExecutionID);


            var executionItemDupes = import.Where(i => i.ExecutionItemUid.HasValue).GroupBy(i => i.ExecutionItemUid).Where(i => i.Count() > 1).Select(i => new { ExecutionItemUid = i.Key, Count = i.Count() }).ToList();
            var uidDupes = import.Where(x => x.Uid.HasValue).GroupBy(x => x.Uid).Where(x => x.Count() > 1).Select(i => new { Uid = i.Key, Count = i.Count() }).ToList();

            if (executionItemDupes.Any())
            {
                execution.ErrorMessage = $"Duplicate execution item identifiers: {string.Join(", ", executionItemDupes.Select(i => i.ExecutionItemUid.ToString()))}. Identifiers must be unique within a batch.";
                results.AddRange(import.Select(i => new ResponsibilityRuleUpsertResponseModel { ExecutionItemUid = i.ExecutionItemUid.Value, Message = execution.ErrorMessage, Success = false }));
            }
            else if (uidDupes.Any())
            {
                execution.ErrorMessage = $"Duplicate uid item identifiers: {string.Join(", ", uidDupes.Select(i => i.Uid.ToString()))}. Identifiers must be unique within a batch.";
                results.AddRange(import.Select(i => new ResponsibilityRuleUpsertResponseModel { Uid = i.Uid.Value, Message = execution.ErrorMessage, Success = false }));
            }
            else
            {
                try
                {
                    currentLocation = GetCurrentExecutionLocation(execution.ExecutionID, "api.ExecutionResponsibilityRule");

                    if (currentLocation.HighestItemNumberProcessed > 0)
                    {
                        results.AddRange(
                            Query<ResponsibilityRuleUpsertResponseModel>(
                                $"select * from api.ExecutionResponsibilityRule where ExecutionID = @ExecutionID and ItemNumber <= {currentLocation.HighestItemNumberProcessed}",
                                new { execution.ExecutionID }
                            )
                        );
                    }

                    #region Build data tables.

                    var table = new DataTable();
                    table.Columns.Add("ExecutionID", typeof(Guid));
                    table.Columns.Add("ItemNumber", typeof(int));
                    table.Columns.Add("ResponsibilityTypeUid", typeof(Guid));
                    table.Columns.Add("AssetTypeUid", typeof(Guid));
                    table.Columns.Add("uid", typeof(Guid));
                    table.Columns.Add("Name", typeof(string));
                    table.Columns.Add("IsVisible", typeof(bool));
                    table.Columns.Add("ApplyToType", typeof(bool));
                    table.Columns.Add("Context", typeof(string));
                    table.Columns.Add("Definition", typeof(string));
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
                            var rowError = string.Empty;

                            var row = table.NewRow();

                            row["ExecutionID"] = execution.ExecutionID;
                            row["ItemNumber"] = i;
                            if (model.ExecutionItemUid.HasValue) row["ExecutionItemUid"] = model.ExecutionItemUid.Value;
                            else row["ExecutionItemUid"] = Guid.NewGuid();

                            row["ResponsibilityTypeUid"] = responsibilityTypeUid;

                            if (model.AssetTypeUid.HasValue)
                                row["AssetTypeUid"] = model.AssetTypeUid;
                            row["Name"] = model.Name;
                            row["IsVisible"] = model.IsVisible;
                            row["ApplyToType"] = model.ApplyToType;
                            row["Context"] = model.Context;
                            if (model.Definition != null)
                            {
                                row["Definition"] = JsonConvert.SerializeObject(model.Definition);
                            }

                            if (execution.Method.ToLower() == "post" && model.Uid.HasValue)
                            {
                                rowError += ";Cannot use Uid in POST request. Please use PUT Api for updating records!";
                            }

                            if (execution.Method.ToLower() == "put" && !model.Uid.HasValue)
                            {
                                rowError += ";UID cannot be empty!";
                            }

                            if (model.Uid.HasValue)
                            {
                                row["uid"] = model.Uid.Value;
                            }

                            //initial validation
                            if (!model.AssetTypeUid.HasValue || model.AssetTypeUid.Value == Guid.Empty)
                            {
                                rowError += ";AssetTypeUid is not valid!";
                            }

                            if (string.IsNullOrEmpty(model.Name))
                            {
                                rowError += ";Name cannot be empty.";
                            }

                            if (model.Definition == null)
                            {
                                rowError += ";Definition cannot be empty/null.";
                            }

                            if (model.ApplyToType == true && (model.Definition.When != null && model.Definition.When.Count > 0))
                            {
                                rowError += "Cannot use When conditions when ApplyToType value is set to true.";
                            }

                            model.Definition.Then.ForEach(th =>
                            {
                                if (th.AssigneeTypeUid == null || th.AssigneeTypeUid == Guid.Empty)
                                {
                                    rowError += ";AssigneeTypeUid cannot be null or empty.";
                                }
                                th.Conditions.ForEach(cond =>
                                {
                                    if (cond.Assignee == null && cond.Field == null)
                                    {
                                        rowError += ";Then condition should have either Field or Assignee values set.";
                                    }

                                    if (cond.Assignee != null && cond.Field != null)
                                    {
                                        rowError += ";Condition cannot have Field and Asignee within same condition.";
                                    }

                                    if (cond.Assignee != null)
                                    {
                                        if (!cond.Assignee.Uid.HasValue)
                                        {
                                            rowError += ";Assignee Uid is required field.";
                                        }
                                    }

                                    if (cond.Field != null)
                                    {
                                        if (string.IsNullOrEmpty(cond.Field.ApiName))
                                        {
                                            rowError += ";ApiName is required field.";
                                        }
                                        if (string.IsNullOrEmpty(cond.Field.Value))
                                        {
                                            rowError += ";Value is required field.";
                                        }
                                    }


                                });

                            });

                            if (model.Definition.When != null && model.Definition.When.Count > 0)
                            {
                                model.Definition.When.ForEach(cond =>
                                {
                                    if (cond.Relation == null && cond.Field == null)
                                    {
                                        rowError += ";Then condition should have either Field or Relation value set.";
                                    }
                                    if (cond.Relation != null && cond.Field != null)
                                    {
                                        rowError += ";Condition cannot have Field and Relation within same condition.";
                                    }

                                    if (cond.Relation != null)
                                    {
                                        if (!cond.Relation.IntersectTypeUid.HasValue)
                                        {
                                            rowError += ";IntersectTypeUid is required field.";
                                        }
                                        if (!cond.Relation.AssetUid.HasValue)
                                        {
                                            rowError += ";AssetUid is required field.";
                                        }
                                    }

                                    if (cond.Field != null)
                                    {
                                        if (string.IsNullOrEmpty(cond.Field.ApiName))
                                        {
                                            rowError += ";ApiName is required field.";
                                        }
                                        if (string.IsNullOrEmpty(cond.Field.Value))
                                        {
                                            rowError += ";Value is required field.";
                                        }
                                    }
                                });
                            }

                            if (!string.IsNullOrEmpty(rowError))
                            {
                                row["Message"] = rowError.Trim(';');
                                row["Success"] = false;
                            }

                            table.Rows.Add(row);
                        }
                    }

                    #endregion

                    if (Database.Connection.State != ConnectionState.Open)
                        Connection.Open();

                    #region Bulk Copy

                    using (SqlBulkCopy bulkCopy = new SqlBulkCopy(Connection))
                    {

                        bulkCopy.BatchSize = SqlBulkBatchSize;
                        bulkCopy.DestinationTableName = "api.ExecutionResponsibilityRule";
                        bulkCopy.BulkCopyTimeout = SqlBulkBatchTimeout;


                        bulkCopy.ColumnMappings.Add("ExecutionID", "ExecutionID");
                        bulkCopy.ColumnMappings.Add("ItemNumber", "ItemNumber");
                        bulkCopy.ColumnMappings.Add("ExecutionItemUid", "ExecutionItemUid");
                        bulkCopy.ColumnMappings.Add("Uid", "uid");
                        bulkCopy.ColumnMappings.Add("ResponsibilityTypeUid", "ResponsibilityTypeUid");
                        bulkCopy.ColumnMappings.Add("AssetTypeUid", "AssetTypeUid");
                        bulkCopy.ColumnMappings.Add("Name", "Name");
                        bulkCopy.ColumnMappings.Add("IsVisible", "IsVisible");
                        bulkCopy.ColumnMappings.Add("ApplyToType", "ApplyToType");
                        bulkCopy.ColumnMappings.Add("Context", "Context");
                        bulkCopy.ColumnMappings.Add("Definition", "Definition");
                        bulkCopy.ColumnMappings.Add("Success", "Success");
                        bulkCopy.ColumnMappings.Add("Message", "Message");


                        bulkCopy.WriteToServer(table);
                    }

                    #endregion

                    #region Log data errors


                    var checkSQL = $@"
    update	api.ExecutionResponsibilityRule 
    set		Success = 0,
		    [Message] = coalesce([Message] + '; ', '') + 'Responsibility Rule with specified Uid not found!'
    from api.ExecutionResponsibilityRule EP
    left join ResponsibilityTypeRelationRule rtrr on rtrr.uid = ep.uid
    where	ExecutionID = @ExecutionID and EP.Uid is not null and rtrr.uid is null;

    update	api.ExecutionResponsibilityRule 
    set		Success = 0,
		    [Message] = coalesce([Message] + '; ', '') + 'Invalid Asset Type Uid'
    from api.ExecutionResponsibilityRule EP
    left join AssetType AT on AT.uid = EP.AssetTypeUid
    where	ExecutionID = @ExecutionID and AT.Id is null;

    drop table if exists #allowedTypes
    select distinct at.uid 
    into #allowedTypes
    from api.ExecutionResponsibilityRule EP
        inner join ResponsibilityType RT on rt.uid = EP.ResponsibilityTypeUid
        inner join [ResponsibilityTypeRelation] RR on RR.ResponsibilityTypeID = RT.Id
	    inner join assettype at on rr.ObjectType=at.Object and rr.ObjectID = at.ObjectID
    where ExecutionID = @executionId;

    update	api.ExecutionResponsibilityRule 
    set		Success = 0,
		    [Message] = coalesce([Message] + '; ', '') + 'Invalid Asset Type Uid for Responsibility Type.'
    from api.ExecutionResponsibilityRule EP
    left join AssetType AT on AT.uid = EP.AssetTypeUid
    where	ExecutionID = @ExecutionID and (AT.Id is null or AT.uid not in (select * from #allowedTypes));

";

                    Connection.Execute(checkSQL, new { execution.ExecutionID }, commandTimeout: timeout);

                    #endregion

                    #region Parse new json to old format

                    var jsonParseSql = $@"
drop table if exists #tempData
create table #tempData
(
    ItemNumber int, 
    ExecutionId uniqueidentifier, 
	AssetTypeUid uniqueidentifier,
    AssigneeTypeUid uniqueidentifier, 
    RelIntersectTypeUid uniqueidentifier, 
    RelAssetUid uniqueidentifier,
	FieldApiName nvarchar(250),
    FieldValue nvarchar(250),
    AssigneeUid uniqueidentifier,
	ValueAsUid uniqueidentifier,
)

insert into #tempData
select
ItemNumber,
ExecutionId,
AssetTypeUid,
ThenData.AssigneeTypeUid,
ThenCond.*,
case 
when ThenCond.IntersectTypeUid is not null then cast(thencond.intersecttypeuid as uniqueidentifier)
else null
end as ValueAsUid
from api.executionresponsibilityrule
cross apply OPENJSON (Definition, N'$.Then')
  WITH (
    AssigneeTypeUid uniqueidentifier N'$.AssigneeTypeUid',
    Conditions nvarchar(max) N'$.Conditions' as Json
  ) AS ThenData
outer apply OPENJSON(ThenData.Conditions)
   with(
		IntersectTypeUid uniqueidentifier N'$.Relation.IntersectTypeUid',
		AssetUid uniqueidentifier N'$.Relation.AssetUid',
		FieldApiName nvarchar(250) N'$.Field.ApiName',
		Value nvarchar(250) N'$.Field.Value',
		AssetUid uniqueidentifier  N'$.Assignee.Uid'
   ) as ThenCond
where executionid = @executionId and success is null

insert into #tempData
select
ItemNumber,
ExecutionId,
AssetTypeUid,
null as AssigneeTypeUid,
WhenData.*,
case 
when WhenData.IntersectTypeUid is not null then cast(WhenData.value as uniqueidentifier)
else null
end as ValueAsUid
from api.executionresponsibilityrule
cross apply OPENJSON (Definition, N'$.When')
  WITH (
		IntersectTypeUid uniqueidentifier N'$.Relation.IntersectTypeUid',
		AssetUid uniqueidentifier N'$.Relation.AssetUid',
		FieldApiName nvarchar(250) N'$.Field.ApiName',
		Value nvarchar(250) N'$.Field.Value',
		AssetUid uniqueidentifier  N'$.Assignee.Uid'
  ) AS WhenData
where executionid = @executionId and success is null

drop table if exists #parsedData
select  d.itemnumber, 
		d.executionid ,
		at.object,
		at.objectid,
		d.valueasuid,
		d.AssigneeTypeUid,
		d.AssigneeUid,
		d.RelAssetUid,
		case 
			when d.RelIntersectTypeUid is null then 'F'
			else 'R'
		end as CheckType,
		case 
			when at.uid is not null then ft.id
			else ft2.id
		end as FieldTypeId,
		case
			when at.uid is not null then isnull(ft.friendlyname,d.fieldapiname)
			else isnull(ft2.friendlyname, d.fieldapiname)
		end as FieldTypeName,
		case 
			when it.id is null then d.FieldValue
			else a.Object+'|'+ cast(a.objectid as nvarchar(20)) 
		end as Value,
		it.id as IntersectTypeId,
		a.object as TargetObject,
		isnull(a.objectid,0) as TargetObjectId,
		cast('' as nvarchar(max)) as ErrorMessage,
		ROW_NUMBER() OVER(ORDER BY(SELECT NULL)) as rowNumber
	into #parsedData
	from #tempData d
		left join assettype at on d.assigneetypeuid = at.uid
		left join FieldType ft on at.Object = ft.Object and at.ObjectID = ft.ObjectID and ft.Name = d.FieldApiName
		left join IntersectType it on it.uid = d.RelIntersectTypeUid
		left join assettype at2 on d.AssetTypeUid = at2.uid
		left join FieldType ft2 on ft2.object = at2.object and ft2.objectid = at2.objectid and ft2.name = d.fieldapiname
		left join asset a on a.uid = d.RelAssetUid

update #parsedData
set FieldTypeId = 0,
FieldTypeName = 'Name',
Value = a.ObjectID
from #parsedData
	inner join asset a on a.uid = AssigneeUid
	inner join assettype at on a.assettypeid = at.id and at.objectid = #parsedData.objectid and at.object = #parsedData.object
where AssigneeUid is not null

update #parsedData
set Value = LOWER(pd.value)
from #parsedData pd
	inner join fieldtype ft on pd.fieldtypeid = ft.id
where pd.fieldtypeid is not null and ft.type = 'Boolean'

update #parsedData
set Value = flv.Value
from #parsedData pd
	inner join fieldtype ft on pd.fieldtypeid = ft.id
	left join FieldLookupValue FLV on FLV.FieldTypeID = ft.ID  and TRIM(pd.value) = FLV.Text
where pd.fieldtypeid is not null and ft.type = 'Lookup'

update #parsedData
set ErrorMessage = 'Invalid Field name.'
where isnull(fieldtypeid,0) = 0 and fieldtypename <> '' and AssigneeUid is null

update #parsedData
set ErrorMessage = 'Invalid Lookup value.'
from #parsedData pd
	inner join fieldtype ft on pd.fieldtypeid = ft.id
where pd.fieldtypeid is not null and ft.type = 'Lookup' and Value is null

update #parsedData
set ErrorMessage = 'Invalid AssetUid for condition.'
where isnull(value,0) = 0 and fieldtypename <> '' and AssigneeUid is not null

update #parsedData
set ErrorMessage = 'Invalid Intersect Type Uid for condition.'
where CheckType = 'R' and IntersectTypeId is null

update #parsedData
set ErrorMessage = 'Invalid Asset UID for condition value.'
where CheckType = 'R' and isnull(targetobjectid,0) = 0

update #parsedData
set ErrorMessage =  'Invalid Assignee Type. Allowed Types are ''Resource'', ''Group'' and ''Organization'''
where object is not null and object not in('ResourceType','OrganizationType','GroupType')

update #parsedData
set ErrorMessage = 'Invalid Asset UID for Intersect Type.'
from #parsedData
  left join IntersectType it on it.ID= IntersectTypeId
  left join Asset A on a.object = TargetObject and a.objectid = targetobjectid
  left join assettype at on a.AssetTypeID = at.ID 
where CheckType = 'R' and (at.uid <> it.subjectuid and at.uid <> it.objectuid)

update #parsedData
set ErrorMessage = 'AssigneeType not found.'
from #parsedData pd
left join AssetType at on at.uid = pd.assigneetypeuid
where pd.AssigneeTypeUid is not null and at.id is null

update #parsedData
set ErrorMessage = 'Invalid AssigneeType. Allowed types are ResourceType, GroupType and OrganizationType.'
from #parsedData pd
inner join AssetType at on at.uid = pd.assigneetypeuid
where pd.AssigneeTypeUid is not null and at.Object not in ('ResourceType', 'GroupType','OrganizationType')

update #parsedData
set ErrorMessage = 'Invalid Assignee for Assignee Type.'
from #parsedData pd
left join AssetType at on at.uid = pd.assigneetypeuid
left join asset a on a.uid = assigneeuid
where pd.AssigneeTypeUid is not null and at.id is not null and at.id <> a.assettypeid

update #parsedData
set ErrorMessage = 'Invalid JSON Data.'
where fieldtypeid is null and fieldtypename is null and value is null and intersecttypeid is null and TargetObject is null and errormessage is null

MERGE api.ExecutionResponsibilityRule err
USING (select itemnumber,executionid,trim(string_agg(errormessage,',')) as msg from #parsedData
where isnull(errormessage,'') <> ''
group by itemnumber,executionid
) cd
ON cd.itemnumber = err.itemnumber and cd.executionid = err.executionid and cd.msg <> '' 
WHEN MATCHED
    THEN UPDATE
	SET [Message] = coalesce([Message] + '; ', '') + cd.msg,
	Success = 0;

drop table if exists #convertedData
create table #convertedData
(
    ItemNumber int, 
    ExecutionId uniqueidentifier, 
	[When] nvarchar(max),
	[Then] nvarchar(max),
	[Definition] nvarchar(max)
)

insert into #convertedData
select ItemNumber,ExecutionId, null,null,null
from #parsedData
group by ItemNumber,ExecutionId

;with conditions as (select 
ItemNumber,
ExecutionId,
ConditionsThen.json as [Then],
ConditionsWhen.json as [When]
 from #parsedData pd
cross apply (
	select top 1 Object,ObjectID, Conditions.json as Conditions
	from #parsedData
		outer apply(select
		 CheckType,
		 isnull(FieldTypeID,0) as FieldTypeID,
		 FieldTypeName,
		 Value,
		 isnull(IntersectTypeID,0) as IntersectTypeID,
		 TargetObject,
		 TargetObjectId
		  from #parsedData
		 where ItemNumber =pd.ItemNumber and ExecutionId = pd.ExecutionId and Object= pd.Object  and ((FieldTypeName is not null and FieldTypeID <> 0 or AssigneeUid is not null))
		 for json path, include_null_values
		)Conditions(json)
	where ItemNumber =pd.ItemNumber and ExecutionId = pd.ExecutionId and Object= pd.Object
	for json path, include_null_values, without_array_wrapper
	)ConditionsThen(json)

cross apply (
select
		 CheckType,
		 isnull(FieldTypeID,0) as FieldTypeID,
		 FieldTypeName,
		 Value,
		 isnull(IntersectTypeID,0) as IntersectTypeID,
		 TargetObject,
		 TargetObjectId
		  from #parsedData
		 where ItemNumber =pd.ItemNumber and ExecutionId = pd.ExecutionId and Object is null
		 for json path, include_null_values
)ConditionsWhen(json)
where 
object is not null
group by ItemNumber,ExecutionId,ConditionsThen.json, ConditionsWhen.json)
update #convertedData 
set [When] = c.[When],
[Then] = c.[Then],
[Definition] = '{{'+Concat_ws(',','""When"":' + c.[When],'""Then"":' + c.[Then]) + '}}'
from conditions c
where #convertedData.itemnumber = c.itemnumber and #convertedData.executionid = c.executionid


MERGE api.ExecutionResponsibilityRule err
USING #convertedData cd
ON cd.itemnumber = err.itemnumber and cd.executionid = err.executionid
WHEN MATCHED
    THEN UPDATE
    SET DefinitionConverted = cd.[Definition];


                    ";
                    Connection.Execute(jsonParseSql, new { execution.ExecutionID }, commandTimeout: timeout);

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

                    results = new List<ResponsibilityRuleUpsertResponseModel>();
                    results.AddRange(import.Select(i => new ResponsibilityRuleUpsertResponseModel { ExecutionItemUid = i.ExecutionItemUid, Message = msg, Success = false }));
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
                                       DECLARE @mergeResults table(  
                                            uid uniqueidentifier,  
                                            executionid uniqueidentifier,  
                                            itemnumber int); 

                                        MERGE dbo.ResponsibilityTypeRelationRule RTRR
                                        USING (
                                        select 
										xrr.executionid,
										xrr.itemnumber,
                                        xrr.uid,
                                        rt.id as ResponsibilityTypeId,
                                        at.object as Object,
                                        at.objectid as ObjectId,
                                        xrr.Name,
                                        xrr.Context,
                                        xrr.IsVisible,
                                        xrr.ApplyToType, 
                                        xrr.DefinitionConverted
                                         from api.executionresponsibilityrule xrr
                                        inner join assettype at on at.uid = xrr.AssetTypeUid
                                        inner join ResponsibilityType rt on rt.uid = xrr.ResponsibilityTypeUid
                                        where xrr.executionid = @ExecutionID and xrr.ItemNumber between @beginItemNumber and @endItemNumber and xrr.success is null
                                        )Data
                                        ON RTRR.uid = Data.uid
                                        WHEN MATCHED
                                            THEN update set 
                                                name = data.name,
                                                ResponsibilityTypeId = data.ResponsibilityTypeId,
                                                object = data.Object,
                                                objectId = data.ObjectId,
                                                context = data.context,
                                                isvisible = data.isvisible,
                                                applytotype = data.applytotype,
                                                definition = data.DefinitionConverted,
                                                updatedon = getdate(),
                                                updatedby = @resourceId
                                        WHEN NOT MATCHED
                                            THEN insert (ResponsibilityTypeId,Object,ObjectId,Name,Context,IsVisible, ApplyToType,CreatedOn,CreatedBy,Definition)
	                                        values (data.ResponsibilityTypeId,data.Object, data.ObjectId, data.Name, data.Context, data.IsVisible, data.ApplyToType, getdate(), @resourceId,data.DefinitionConverted)
                                            output inserted.uid, data.executionid, data.itemnumber into @mergeResults;

                                        update api.executionresponsibilityrule
                                           set uid = mr.uid
                                        from @mergeResults mr 
                                            where executionresponsibilityrule.executionid = mr.executionid and executionresponsibilityrule.itemnumber = mr.itemnumber";

                                    Connection.Execute(insertSQL,
                                            new { execution.ExecutionID, beginItemNumber, endItemNumber, resourceId = CurrentResourceID }, transaction: trans, commandTimeout: timeout);

                                    Connection.Execute(
                                        $"update P set P.Success = 1 from api.ExecutionResponsibilityRule P where	{querySuffix};",
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
                                        LogLoopExecutionError(execution.ExecutionID, beginItemNumber, endItemNumber, "api.ExecutionResponsibilityRule", ex.GetFullExceptionData(false), timeout);
                                    }
                                }
                            }
                        }

                        results.AddRange(
                            Query<ResponsibilityRuleUpsertResponseModel>(
                                $"select * from api.ExecutionResponsibilityRule where ExecutionID = @ExecutionID and ItemNumber between @beginItemNumber and @endItemNumber",
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

        public List<GroupResponseResult> UpdateGroups(ApiExecution execution, List<UpdateGroupModel> groups)
        {
            DynamicParameters dbArgs = new DynamicParameters();
            bool generalChecksCompleted = false;
            int itemNumber = 1;
            List<GroupResponseResult> results = new List<GroupResponseResult>();
            CurrentExecutionLocationModel currentLocation = null;
            var currentUser = CurrentCompanyID;

            var dups = groups.GroupBy(x => x.Name.Trim()).Where(x => x.Count() > 1).Select(x => new { x.Key, Items = x.ToList() }).ToList();

            Add(execution);
            SetApiExecutionProcessingStartTime(execution.ExecutionID);

            if (dups.Any())
            {
                execution.ErrorMessage = $"Duplicate Names: {string.Join(", ", dups.Select(i => i.Items.First().Name.Trim()))}. Name must be unique within a batch.";
                results.AddRange(groups.Select(i => new GroupResponseResult { ExecutionItemUid = execution.ExecutionID, Message = execution.ErrorMessage, Success = false }));
            }
            else
            {

                try
                {
                    currentLocation = GetCurrentExecutionLocation(execution.ExecutionID, "api.ExecutionGroup");

                    var table = new DataTable();

                    table.Columns.Add("ExecutionID", typeof(Guid));
                    table.Columns.Add("ItemNumber", typeof(int));
                    table.Columns.Add("GroupUid", typeof(Guid));
                    table.Columns.Add("Name", typeof(string));
                    table.Columns.Add("Description", typeof(string));
                    table.Columns.Add("PrimaryOwnerUid", typeof(Guid));
                    table.Columns.Add("SecondaryOwnerUid", typeof(Guid));
                    table.Columns.Add("IsActiveDirectoryGroup", typeof(bool));
                    table.Columns.Add("ExecutionItemUid", typeof(Guid));

                    #region Generate data sets

                    foreach (var item in groups)
                    {
                        var row = table.NewRow();
                        row["ExecutionID"] = execution.ExecutionID;
                        row["ItemNumber"] = itemNumber;
                        if (item.Uid != null)
                            row["GroupUid"] = item.Uid;

                        if (item.Name == null)
                            row["Name"] = "";
                        else
                            row["Name"] = item.Name.Trim();

                        row["Description"] = item.Description;
                        if (item.PrimaryOwnerUid != null)
                            row["PrimaryOwnerUid"] = item.PrimaryOwnerUid;
                        if (item.SecondaryOwnerUid != null)
                            row["SecondaryOwnerUid"] = item.SecondaryOwnerUid;

                        row["IsActiveDirectoryGroup"] = item.IsActiveDirectoryGroup;
                        row["ExecutionItemUid"] = Guid.NewGuid();

                        table.Rows.Add(row);

                        itemNumber++;
                    }

                    #endregion

                    if (Database.Connection.State != ConnectionState.Open)
                        Connection.Open();

                    #region Bulk Copy

                    using (var bulkCopy = new SqlBulkCopy(Connection)
                    {
                        BatchSize = table.Rows.Count,
                        DestinationTableName = "[api].[ExecutionGroup]",
                        BulkCopyTimeout = 3600
                    })
                    {

                        bulkCopy.ColumnMappings.Add("ExecutionID", "ExecutionID");
                        bulkCopy.ColumnMappings.Add("ItemNumber", "ItemNumber");
                        bulkCopy.ColumnMappings.Add("GroupUid", "GroupUid");
                        bulkCopy.ColumnMappings.Add("Name", "Name");
                        bulkCopy.ColumnMappings.Add("Description", "Description");
                        bulkCopy.ColumnMappings.Add("PrimaryOwnerUid", "PrimaryOwnerUid");
                        bulkCopy.ColumnMappings.Add("SecondaryOwnerUid", "SecondaryOwnerUid");
                        bulkCopy.ColumnMappings.Add("IsActiveDirectoryGroup", "IsActiveDirectoryGroup");
                        bulkCopy.ColumnMappings.Add("ExecutionItemUid", "ExecutionItemUid");


                        bulkCopy.WriteToServer(table);
                    }
                    
                    #endregion

                    var checkSQL = $@"update	[api].[ExecutionGroup]
                    set		Success = 0,
		                    [Message] = coalesce([Message], '') + 'Name field cannot be empty;'
                    where	ExecutionID = @ExecutionID and (Name is null or TRIM(Name) = '');

                    update	[api].[ExecutionGroup]
                    set		Success = 0,
		                    [Message] = coalesce([Message], '') + 'Already a group called this name;'
	                from [api].[ExecutionGroup] EG 
	                inner join [Group] G on G.[Name] = EG.[Name]
                    left join [Asset] A on A.ObjectID = G.[ID] and A.Object = 'Group' and A.uid = EG.[GroupUid]
                    where	ExecutionID = @ExecutionID and A.uid is null and G.Name is not null;

                    update	[api].[ExecutionGroup]
                    set		Success = 0,
		                    [Message] = coalesce([Message], '') + 'Uid provided is not a group uid;'
	                from [api].[ExecutionGroup] EG 
	                left join [Asset] A on A.[uid] = EG.[GroupUid] and A.Object = 'Group'
                    where	ExecutionID = @ExecutionID and A.uid is null and EG.[GroupUid] is not null;

                    update	[api].[ExecutionGroup]
                    set		Success = 0,
		                    [Message] = coalesce([Message], '') + 'Primary Owner Uid provided is not a resource uid;'
                    from [api].[ExecutionGroup] EG 
                    left join [Asset] A on A.[uid] = EG.[PrimaryOwnerUid] and A.Object = 'Resource'
                    where	ExecutionID = @ExecutionID and coalesce(EG.[PrimaryOwnerUid], @emptyUid) <> @emptyUid and A.uid is null;

                    update	[api].[ExecutionGroup]
                    set		Success = 0,
		                    [Message] = coalesce([Message], '') + 'Secondary Owner Uid provided is not a resource uid;'
	                from [api].[ExecutionGroup] EG 
	                left join [Asset] A on A.[uid] = EG.[SecondaryOwnerUid] and A.Object = 'Resource'
                    where	ExecutionID = @ExecutionID and A.uid is null and EG.SecondaryOwnerUid is not null;";

                    Connection.Execute(checkSQL, new { execution.ExecutionID, emptyUid = Guid.Empty }, commandTimeout: timeout);

                    generalChecksCompleted = true;
                }
                catch (Exception generalEx)
                {
                    generalChecksCompleted = false;
                    var msg = generalEx.GetFullExceptionData(false);
                    execution.ErrorMessage = msg;
                    execution.Processed = 0;
                    execution.Error = groups.Count();

                    results = new List<GroupResponseResult>();
                    results.AddRange(groups.Select(i => new GroupResponseResult { ExecutionItemUid = execution.ExecutionID, Message = msg, Success = false }));
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
                                    var insertSQL = $@"
                                            					drop table if exists #mergeResultTable
                create table #mergeResultTable (GroupName varchar(250), ExecutionItemUid uniqueidentifier) 
                                            
                merge into [Group] G
                using ( 
select A.ObjectID as GroupID ,
EG.Name,EG.Description,
EG.ExecutionItemUid,
EG.IsActiveDirectoryGroup,
PO.ObjectID as PrimaryID,
SO.ObjectID as SecondaryID
	                    from api.ExecutionGroup EG
						left join Asset A on A.uid = EG.GroupUid and A.Object = 'Group'
						left join Asset PO on PO.uid = EG.PrimaryOwnerUid and PO.Object = 'Resource'
						left join Asset SO on SO.uid = EG.SecondaryOwnerUid and SO.Object = 'Resource'
		                where EG.ExecutionID = @ExecutionID
                                and EG.ItemNumber between @beginItemNumber and @endItemNumber
                                and EG.Success is null
	                    ) S
                on (G.ID = GroupID)
				when matched then
					update  
						set G.Name = TRIM(S.Name),
						G.Description = S.Description,
						G.PrimaryOwnerResourceID = PrimaryID,
						G.SecondaryOwnerResourceID = SecondaryID,
                        G.IsActiveDirectoryGroup = S.IsActiveDirectoryGroup
                    when not matched then
	                    insert (Name, Description, PrimaryOwnerResourceID, SecondaryOwnerResourceID,IsActiveDirectoryGroup,UpdatedOn,UpdatedBy)
	                    values (TRIM(S.Name),S.Description, S.PrimaryID, S.SecondaryID,S.IsActiveDirectoryGroup,GETDATE(),@currentUser)
	                output TRIM(S.Name), S.ExecutionItemUid into #mergeResultTable;


                    INSERT INTO [ResourceGroup](GroupID,[ResourceID])
                    SELECT G.ID, G.PrimaryOwnerResourceID
                    FROM [Group] G
                    inner join api.ExecutionGroup EG on EG.Name = G.Name
                    where EG.ExecutionID = @ExecutionID 
                    and EG.ItemNumber between @beginItemNumber and @endItemNumber
                    and EG.Success is null
                    and EG.GroupUid is null
                    and coalesce(EG.PrimaryOwnerUid, 0x0) <> 0x0
					and G.PrimaryOwnerResourceID is not null;

	                INSERT INTO [ResourceGroup](GroupID,[ResourceID])
                    SELECT G.ID, G.SecondaryOwnerResourceID
                    FROM [Group] G
                    inner join api.ExecutionGroup EG on EG.Name = G.Name
                    where EG.ExecutionID = @ExecutionID 
                    and EG.ItemNumber between @beginItemNumber and @endItemNumber
                    and EG.Success is null
                    and EG.GroupUid is null
					and G.SecondaryOwnerResourceID is not null
                    and G.PrimaryOwnerResourceID != G.SecondaryOwnerResourceID;

                    IF NOT EXISTS    
                    (
                    SELECT 1    
                    FROM [ResourceGroup] RG
	                inner join api.ExecutionGroup EG on EG.ExecutionID = @ExecutionID and EG.ItemNumber between @beginItemNumber and @endItemNumber and EG.Success is null
	                inner join [Group] G on G.Name = EG.Name     
                    WHERE ResourceID = G.PrimaryOwnerResourceID and [GroupID] = G.ID 
                    )    
                    BEGIN
                        INSERT INTO [ResourceGroup](GroupID,[ResourceID])
                                    SELECT G.ID, G.PrimaryOwnerResourceID
                                    FROM [Group] G
                                    inner join api.ExecutionGroup EG on EG.Name = G.Name
                                    where EG.ExecutionID = @ExecutionID 
                                    and EG.ItemNumber between @beginItemNumber and @endItemNumber
                                    and EG.Success is null
                                    and coalesce(EG.PrimaryOwnerUid, 0x0) <> 0x0
                    END

                    IF NOT EXISTS    
                    (
                    SELECT 1    
                    FROM [ResourceGroup] RG
	                inner join api.ExecutionGroup EG on EG.ExecutionID = @ExecutionID and EG.ItemNumber between @beginItemNumber and @endItemNumber and EG.Success is null
	                inner join [Group] G on G.Name = EG.Name     
                    WHERE ResourceID = G.SecondaryOwnerResourceID and [GroupID] = G.ID and G.SecondaryOwnerResourceID is not null
                    )    
                    BEGIN
                        INSERT INTO [ResourceGroup](GroupID,[ResourceID])
                                    SELECT G.ID, G.SecondaryOwnerResourceID
                                    FROM [Group] G
                                    inner join api.ExecutionGroup EG on EG.Name = G.Name
                                    where EG.ExecutionID = @ExecutionID 
                                    and EG.ItemNumber between @beginItemNumber and @endItemNumber
                                    and EG.Success is null
                                    and G.SecondaryOwnerResourceID is not null
                    END

                    update EG
                    set EG.GroupUid = A.uid
                    from api.ExecutionGroup EG
                    inner join #mergeResultTable Res on Res.ExecutionItemUid = EG.ExecutionItemUid
					inner join [Group] G on G.Name = Res.GroupName
		            inner join Asset A on A.ObjectID = G.ID and A.Object ='Group'
                    where EG.ExecutionID = @ExecutionID and EG.Success is null";

                                    Connection.Execute(insertSQL,
                                            new { execution.ExecutionID, beginItemNumber, endItemNumber, currentUser }, transaction: trans, commandTimeout: timeout);

                                    Connection.Execute(
                                                        $"update [api].[ExecutionGroup] set Success = 1, Message = 'Success' where	Success is null and ExecutionID = @ExecutionID;",
                                                        new { execution.ExecutionID }, transaction: trans, commandTimeout: timeout);

                                    trans.Commit();
                                    runCompleted = true;
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

                                    retryCount++;

                                    if (retryCount > API_V2_RETRY_LIMIT)
                                    {
                                        LogLoopExecutionError(execution.ExecutionID, beginItemNumber, endItemNumber, "api.ExecutionGroup", ex.GetFullExceptionData(false), timeout);
                                    }
                                }
                            }
                        }
                        results.AddRange(
                                Query<GroupResponseResult>(
                                    $"select [ItemNumber],[GroupUid] as uid,[ExecutionItemUid],[Message],[Success] from api.ExecutionGroup where ExecutionID = @ExecutionID and ItemNumber between @beginItemNumber and @endItemNumber",
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

        public List<GroupResponseResult> DeleteGroups(ApiExecution execution, List<DeleteGroupModel> groups)
        {
            DynamicParameters dbArgs = new DynamicParameters();
            bool generalChecksCompleted = false;
            int itemNumber = 1;
            List<GroupResponseResult> results = new List<GroupResponseResult>();
            CurrentExecutionLocationModel currentLocation = null;

            SetApiExecutionProcessingStartTime(execution.ExecutionID);

            try
            {

                #region Build data tables.

                currentLocation = GetCurrentExecutionLocation(execution.ExecutionID, "[api].[ExecutionDeletedGroup]");

                var table = new DataTable();

                table.Columns.Add("ExecutionID", typeof(Guid));
                table.Columns.Add("ItemNumber", typeof(int));
                table.Columns.Add("GroupUid", typeof(Guid));

                #region Generate data sets

                foreach (var item in groups)
                {
                    var row = table.NewRow();
                    row["ExecutionID"] = execution.ExecutionID;
                    row["ItemNumber"] = itemNumber;
                    row["GroupUid"] = item.Uid;

                    table.Rows.Add(row);

                    itemNumber++;
                }

                #endregion

                if (Database.Connection.State != ConnectionState.Open)
                    Connection.Open();

                #region Bulk Copy

                using (var bulkCopy = new SqlBulkCopy(Connection)
                {
                    BatchSize = table.Rows.Count,
                    DestinationTableName = "[api].[ExecutionDeletedGroup]",
                    BulkCopyTimeout = 3600
                })
                {

                    bulkCopy.ColumnMappings.Add("ExecutionID", "ExecutionID");
                    bulkCopy.ColumnMappings.Add("ItemNumber", "ItemNumber");
                    bulkCopy.ColumnMappings.Add("GroupUid", "GroupUid");

                    bulkCopy.WriteToServer(table);

                }
                #endregion

                var checkSQL = $@"update	[api].[ExecutionDeletedGroup]
                        set		Success = 0,
	                            [Message] = coalesce([Message] + '; ', '') + 'Not a valid group'
                        from [api].[ExecutionDeletedGroup] EP
                        left join Asset A on A.UID = EP.GroupUid and A.Object = 'Group'
                        where	ExecutionID = @ExecutionID and A.uid is null";

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
                execution.Error = groups.Count();

                results = new List<GroupResponseResult>();
                results.AddRange(groups.Select(i => new GroupResponseResult { ExecutionItemUid = execution.ExecutionID, Message = msg, Success = false }));
            }



            itemNumber = 1;
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
                        using (var trans = Connection.BeginTransaction())
                        {
                            try
                            {
                                var deleteSQL = $@"DELETE G
	                                        FROM [Group] G
		                                    inner join api.ExecutionDeletedGroup EG on EG.Success is null and EG.ExecutionID = @ExecutionID and EG.ItemNumber between @beginItemNumber and @endItemNumber
		                                    inner join Asset A on A .uid = EG.GroupUid
		                                    where A.ObjectID = G.ID";

                                Connection.Execute(deleteSQL,
                                        new { execution.ExecutionID, beginItemNumber, endItemNumber }, transaction: trans, commandTimeout: timeout);

                                Connection.Execute(
                                                    $"update EG set EG.Success = 1, EG.Message = 'Deleted Successfully' from api.ExecutionDeletedGroup EG where EG.Success is null and EG.ExecutionID = @ExecutionID;",
                                                    new { execution.ExecutionID }, transaction: trans, commandTimeout: timeout);

                                trans.Commit();
                                runCompleted = true;
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

                                retryCount++;

                                if (retryCount > API_V2_RETRY_LIMIT)
                                {
                                    LogLoopExecutionError(execution.ExecutionID, beginItemNumber, endItemNumber, "api.ExecutionDeletedGroup", ex.GetFullExceptionData(false), timeout);
                                }
                            }
                        }
                    }
                }
            }

            results.AddRange(
                            Query<GroupResponseResult>(
                                $"select [ItemNumber],[GroupUid] as uid,[ExecutionID] as ExecutionItemUid,[Message],[Success] from api.ExecutionDeletedGroup where ExecutionID = @ExecutionID",
                                new { execution.ExecutionID }
                            )
                        );

            return results;
        }


    }

}
