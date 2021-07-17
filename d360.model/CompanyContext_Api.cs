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
using System.Data;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

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

        public string ApiExecutionFieldTable { get; set; } = "api.executionfield"; // table to use to load field values from

        public int SqlBulkBatchSize { get; set; } = 5000; // default size to use for sqlbulkcopy operations 0 means one batch
        public int SqlBulkBatchTimeout { get; set; } = 0; // timeout for sqlbulkcopy operations  0 means run until it happens
        public int SqlBulkAssetDeleteSize { get; set; } = 10000; // number of assets removed per transaction on type deletion
        public int SqlBulkIntersectFieldDeleteSize { get; set; } = 50000; //Number of fields, intersects in bulk delete sql
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

        public DbSet<ApiExecutionsExternal> ApiExecutionsExternals { get; set; }

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

        private bool TypeHasProcessRelationshipTypes(AssetType at)
        {
            return Database.Connection.QuerySingle<bool>(@"select CASE WHEN count(*) = 0 THEN 0 ELSE 1 END from assettype at
	                            inner join IntersectType it on it.SubjectUid = at.uid or it.objectuid = at.uid
	                            inner join [Predicate] P on p.id = it.predicateid
                            where at.uid = @uid and p.[type] = 15", new { at.uid });
        }

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
                return Database.Connection.QuerySingle<int>("SELECT ISNULL((select count(1) from workflow.EventRegistration where [object] = @obj and [objectid] = @objId and [state] = 1 and [changetype] = @change), 0)", new { obj = new DbString { Value = @object, IsFixedLength = true, Length = 50, IsAnsi = true }, objId = objectID, change = changeType.Value }) > 0;

            return Database.Connection.QuerySingle<int>("SELECT ISNULL((select count(1) from workflow.EventRegistration where [object] = @obj and [objectid] = @objId and [state] = 1 ), 0)", new { obj = new DbString { Value = @object, IsFixedLength = true, Length = 50, IsAnsi = true }, objId = objectID }) > 0;
        }

        private CurrentExecutionLocationModel GetCurrentExecutionLocation(Guid executionID, string targetTable)
        {
            return Connection.Query<CurrentExecutionLocationModel>($@"
select	E.ExecutionID,
		coalesce(T.HighestItemNumber, 0) as HighestItemNumber,
		coalesce(T.HighestItemNumberProcessed, 0) as HighestItemNumberProcessed
from	api.Execution E
		outer apply (
			select	max(ItemNumber) as HighestItemNumber,
                max(case when Success is not null then ItemNumber else 0 end) as HighestItemNumberProcessed
			from	{targetTable} A
			where	ExecutionID = E.ExecutionID
		) T
where	E.ExecutionID = @executionID;",
         new { executionID }).SingleOrDefault();
        }

        private void LoadMissingKeyFields(Guid executionID, AssetType at, int timeout = 3600)
        {
            Connection.Execute($@"
insert into {ApiExecutionFieldTable} (ExecutionID, ItemNumber, FieldName, FieldValue, FieldTypeID, LookupValue, Ignore)
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
			left join {ApiExecutionFieldTable} F on F.ExecutionID = A.ExecutionID and F.ItemNumber = A.ItemNumber and F.FieldTypeID = FT.ID
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
                Connection.Execute($@"
insert into {ApiExecutionFieldTable} (ExecutionID, ItemNumber, FieldName, FieldValue, FieldTypeID, LookupValue, Ignore)
	select	A.ExecutionID,
            A.ItemNumber,
			'Code',
			R.Code,
			0,
			R.Code,
			1
	from	[api].[ExecutionAsset] A
            inner join Asset R on A.Object =  R.Object and R.ObjectID = A.ObjectID
			left join {ApiExecutionFieldTable} F on F.ExecutionID = A.ExecutionID and F.ItemNumber = A.ItemNumber and F.FieldName = 'Code'
	where	A.ExecutionID = @executionID 
	and A.Object = 'ReferenceItem' 
    and F.ItemNumber is null;",
                new { executionID }, commandTimeout: timeout);
            }

            if (at.Class == AssetTypeClass.FusionAttribute)
            {
                Connection.Execute($@"
insert into {ApiExecutionFieldTable} (ExecutionID, ItemNumber, FieldName, FieldValue, FieldTypeID, LookupValue, Ignore)
	select	A.ExecutionID,
            A.ItemNumber,
			'Name',
			R.Name,
			0,
			R.Name,
			1
	from	[api].[ExecutionAsset] A
            inner join FusionAttribute R on A.Object = 'FusionAttribute' and R.ID = A.ObjectID
			left join {ApiExecutionFieldTable} F on F.ExecutionID = A.ExecutionID and F.ItemNumber = A.ItemNumber and F.FieldName = 'Name'
	where	A.ExecutionID = @executionID 
            and F.ItemNumber is null;",
                new { executionID }, commandTimeout: timeout);

                Connection.Execute($@"
insert into {ApiExecutionFieldTable} (ExecutionID, ItemNumber, FieldName, FieldValue, FieldTypeID, LookupValue, Ignore)
	select	A.ExecutionID,
            A.ItemNumber,
			'FusionID',
			R.FusionID,
			0,
			R.FusionID,
			1
	from	[api].[ExecutionAsset] A
            inner join FusionAttribute R on A.Object = 'FusionAttribute' and R.ID = A.ObjectID
			left join {ApiExecutionFieldTable} F on F.ExecutionID = A.ExecutionID and F.ItemNumber = A.ItemNumber and F.FieldName = 'FusionID'
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
            ", new { executionID, intersectTypeID, maxlevel, isInsert }, commandTimeout: timeout);
        }

        private void LogNullIsRequiredFields(Guid executionID, int timeout = 3600)
        {
            Connection.Execute($@"
            drop table if exists #tempreqfield;
            
            select A.executionid,a.itemnumber,STRING_AGG(FT.NAME,',') WITHIN GROUP (ORDER BY ft.columnorder) stringfield,count(1) cnt
            into #tempreqfield
            from api.ExecutionAsset A
            inner join dbo.FieldType FT on FT.object = A.objecttype and FT.ObjectID = A.objecttypeid and FT.IsRequired = 1
            left join Field EF on EF.FieldTypeID = FT.ID and EF.AssetID = A.AssetID
            left join {ApiExecutionFieldTable} F on F.ExecutionID = A.ExecutionID and F.ItemNumber = A.ItemNumber and F.FieldTypeID = FT.ID
            where A.executionid = @executionID 
            and ft.type <> 'Counter'
            and (trim(EF.FormattedValue) is null or EF.FormattedValue = char(0))
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
            if (!CurrentResourceIsAdmin && isInsert && (p & Permission.AddAsset) != 0)
            {
                PermissionInfo permission = this.GetTypePermissions(at.Object, at.ObjectID).Where(x => (x.ID & Permission.AddAsset) != 0).SingleOrDefault();
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

        drop table if exists #tempcheckpermission;

        select usrper.AssetID
        into #tempcheckpermission
        from api.Execution E
        cross apply UserAssetPermissions(E.ResourceID, @assetTypeID) usrper
        where E.ExecutionID = @executionID
        and usrper.PermissionsBitMask & @p = @p;

        create nonclustered index cix_tempcheckpermission on #tempcheckpermission(AssetID);

	    update	T
	    set		T.Success = 0,
			    T.[Message] = coalesce([Message] + '; ', '') + 'User does not have permission to update this asset.'
	    from    api.{apiTableName} T
        where   T.ExecutionID = @executionID
                and T.AssetID is not null
                and not exists (select 1 from #tempcheckpermission ua where ua.AssetID = T.AssetID);
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
            Connection.Execute($@"
update	E
set		E.Message = 'Unable to add or update child asset as the fusion configuration does not match it''s parent''s configuration.',
		E.Success = 0
from	api.ExecutionAsset E
		inner join FusionAttribute P on P.ID = E.ParentObjectID and E.ParentObject = 'FusionAttribute'
		inner join {ApiExecutionFieldTable} C on C.ExecutionID = E.ExecutionID and C.FieldName = 'FusionID' and C.FieldValue <> P.FusionID
where	E.ExecutionID = @executionID;",
            new { executionID }, commandTimeout: timeout);
        }

        private void LogInvalidFusionIDFields(Guid executionID, int timeout = 3600)
        {
            Connection.Execute($@"
update	E
set		E.Message = 'Invalid FusionID value for this Asset type',
		E.Success = 0
from	api.ExecutionAsset E
	where E.ExecutionID = @executionID and not exists(select F.ID from {ApiExecutionFieldTable} EF
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

        private void LogCounterFieldErrors(Guid executionId, int timeout = 3600)
        {
            Connection.Execute($@"
;with DuplicateCounters as (
select EF.FieldTypeID, FieldValue from api.ExecutionAsset EA
inner join {ApiExecutionFieldTable} EF on EA.ItemNumber = EF.ItemNumber AND EF.ExecutionID = EA.ExecutionId
inner join FieldType FT on FT.Id = EF.FieldTypeId and FT.[Type] = 'Counter'
where EA.ExecutionID = @executionId and EA.Success is null
group by fieldtypeid, fieldvalue
having count(*) > 1
)
update EA
set EA.Success = 0,
    EA.[Message] = 'Counter field must have unique value within batch'
from api.ExecutionAsset EA
inner join {ApiExecutionFieldTable} EF on EA.ItemNumber = EF.ItemNumber AND EF.ExecutionID = EA.ExecutionId
inner join DuplicateCounters DC on DC.FieldTypeID = EF.FieldTypeID AND DC.FieldValue = EF.FieldValue
where EA.ExecutionID = @executionId and EA.Success is null;

update EA
set EA.Success = 0,
    EA.[Message] = 'Asset with same counter value already exists. (' + FT.Name + ' = ' + cast(fcv.value as nvarchar(50)) + ')'
from api.ExecutionAsset EA
left join asset a on a.uid = ea.[uid]
inner join api.Execution EX on ex.executionid = @executionId
inner join api.ExecutionField EF on EA.ItemNumber = EF.ItemNumber AND EF.ExecutionID = EA.ExecutionId
inner join FieldType FT on FT.Id = EF.FieldTypeId and FT.[Type] = 'Counter'
inner join FieldCounterValue FCV on FCV.FieldTypeId = FT.ID and FCV.Value = TRY_CAST(EF.FieldValue AS INT) and a.id <> fcv.assetid
where EA.ExecutionID = @executionId and EA.Success is null;

update EA
set EA.Success = 0,
    EA.[Message] = 'Asset with same counter value already exists. (' + FT.Name + ' = ' + cast(fcv.value as nvarchar(50)) + ')'
from api.ExecutionAsset EA
left join asset a on a.uid = ea.[uid]
inner join api.Execution EX on ex.executionid = @executionId
inner join api.ExecutionField EF on EA.ItemNumber = EF.ItemNumber AND EF.ExecutionID = EA.ExecutionId
inner join FieldType FT on FT.Id = EF.FieldTypeId and FT.[Type] = 'Counter'
inner join FieldCounterValue FCV on FCV.FieldTypeId = FT.ID and FCV.Value = TRY_CAST(EF.FieldValue AS INT)
where EA.ExecutionID = @executionId and EA.Success is null and a.uid is null;
",
                new { executionId }, commandTimeout: timeout);

        }

        private void LogFieldLookupErrors(Guid executionID, string obj, int objID, string errorPrefix, bool lookupFieldsPassedByValue, int timeout = 3600)
        {
            string targetTable = "api.ExecutionRelationship";
            if (obj != "IntersectType") targetTable = "api.ExecutionAsset";

            if (lookupFieldsPassedByValue)
            {
                Connection.Execute($@"
update	T
set		T.Success = 0,
		T.[Message] = coalesce(T.[Message] + '; ', '') + '{errorPrefix} contains one or more fields [' + S.FieldName + '] with invalid lookup values: [' + S.FieldValue + ']'
from	{targetTable} T
		inner join	(
					select F.* from FieldType FT
					inner join {ApiExecutionFieldTable} F on F.FieldTypeID = ft.Id and executionid = @executionid
					cross apply STRING_SPLIT(ISNULL(f.fieldvalue,''),',')Val
					left join AssetType AT on AT.object = ft.lookupobjecttype + 'Type' and at.ObjectID = ft.LookupObjectID
					left join Asset A on A.AssetTypeID = AT.ID and A.ObjectID = try_cast(CONVERT(NVARCHAR(20), val.Value) as int)
					left join AssetType RefType on RefType.Object = ft.LookupObjectType and RefType.Object = 'ReferenceItemType' and reftype.objectid =  try_cast(CONVERT(NVARCHAR(20), val.Value) as int)
					left join AssetType ModelType on ModelType.Object = ft.LookupObjectType and ModelType.Object = 'TaxonomyType' and ModelType.objectid =  try_cast(CONVERT(NVARCHAR(20), val.Value) as int)
					where FT.Object = @obj and FT.ObjectID = @objid and [Type] = 'Lookup' and F.FieldValue is not null and (A.Id is null and reftype.id is null and ModelType.id is null) and (try_cast(CONVERT(NVARCHAR(20), val.Value) as int) <> 0 or try_cast(CONVERT(NVARCHAR(20), val.Value) as int) IS NULL)
					) 
					S on S.ExecutionID = T.ExecutionID and S.ItemNumber = T.ItemNumber;
", new { executionID, obj = new DbString { Value = obj, Length = 50, IsAnsi = true }, objID }, commandTimeout: timeout);
            }
            else
            {
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
								inner join {ApiExecutionFieldTable} F on F.ExecutionID = A.ExecutionID and F.ItemNumber = A.ItemNumber and F.FieldTypeID = FT.ID and F.LookupValue is null and (F.FieldValue != '' or FT.IsRequired = 1)
                    where       A.ExecutionID = @executionID
					group by	A.ExecutionID, A.ItemNumber
					) S on S.ExecutionID = T.ExecutionID and S.ItemNumber = T.ItemNumber;
", new { executionID, obj = new DbString { Value = obj, Length = 50, IsAnsi = true }, objID }, commandTimeout: timeout);
            }

        }

        private void LogRelationshipErrors(Guid executionID, string obj, int objID, string errorPrefix, int timeout = 3600, bool lookupFieldsPassedByValue = false)
        {
            string targetTable = (obj != "IntersectType") ? "api.ExecutionAsset" : "api.ExecutionRelationship";
            string assetJoin = lookupFieldsPassedByValue ? "AD.ObjectID = try_cast(V.[value] as int)" : "AD.DisplayValue = V.[value]";

            var sql = $@"
                    update	T
                    set		T.Success = 0,
		                    T.[Message] = coalesce(T.[Message] + '; ', '') + '{errorPrefix} contains one or more fields with invalid relationship values: [' + S.Names + ']'
                    from	{targetTable} T
		                    inner join	(
					                    select		A.ExecutionID,
                                                    A.ItemNumber,
								                    STRING_AGG(FT.Name+'='+left(F.FieldValue,250), ', ') as Names
					                    from		{targetTable} A
                                                    inner join FieldType FT on FT.Object = @obj
								                        and FT.ObjectID = @objID
									                    and FT.[Type] = 'Relationship' and FT.LookupObjectType ='IntersectType'
								                    inner join {ApiExecutionFieldTable} F on F.ExecutionID = A.ExecutionID and F.ItemNumber = A.ItemNumber and F.FieldTypeID = FT.ID and F.LookupValue is null and (F.FieldValue != '' or FT.IsRequired = 1)
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
                    ";

            Connection.Execute(sql, new { executionID, obj = new DbString { Value = obj, Length = 50, IsAnsi = true }, objID }, commandTimeout: timeout);

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

        private void MergeAssetDisplayValues(Guid executionID, SqlTransaction trans, int beginItemNumber, int endItemNumber, int timeout = 3600, bool isInsert = false)
        {
            var fieldsSelectSql = $@"
                select  A.AssetID as ID,
                            ADV.DisplayValue,
                            CONVERT(NVARCHAR(32), HashBytes('SHA1', ADV.DisplayValue), 2) as DisplayValueHash,
                            SUBSTRING(ADV.DisplayValue, 1, 250) as DisplayValuePrefix
                    from    api.ExecutionAsset A
                            cross apply GetAssetDisplayValueByID(A.AssetID) ADV
                    where   A.ExecutionID = @executionID
                            and A.ItemNumber between @beginItemNumber and @endItemNumber 
                            and A.Success is null 
                            and A.[Object] not in( 'FusionAttribute' )
                            and ADV.DisplayValue is not null
            ";

            if (isInsert)
            {
                Connection.Execute($@"
                    insert into AssetDisplayValue (AssetID, DisplayValue, DisplayValueHash,DisplayValuePrefix) 
                        {fieldsSelectSql}
                ",
                new { executionID, r = CurrentResourceID, dt = DateTime.UtcNow, beginItemNumber, endItemNumber }, transaction: trans, commandTimeout: timeout);
            }
            else
            {
                Connection.Execute($@"
    merge       AssetDisplayValue as T
    using       (
                    {fieldsSelectSql}
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
        }

        public List<AssetFieldTypeUpdate> MergeFields(Guid executionID, SqlTransaction trans, string tableName, string objectSqlSyntax, string objectIdSqlSyntax, int beginItemNumber, int endItemNumber, bool sendWorkflowEvents, int timeout = 3600, bool isInsert = false, bool hasLookupFieldTypes = true)
        {
            List<AssetFieldTypeUpdate> res = new List<AssetFieldTypeUpdate>();

            if (sendWorkflowEvents)
            {
                var changedFieldsSql = $@"select  EA.Object, 
                            EA.ObjectID, 
                            EF.FieldTypeID AS Id 
                    from    {tableName} EA 
	                        inner join {ApiExecutionFieldTable} EF on EF.ExecutionID = EA.ExecutionID 
                                            and EF.ItemNumber = EA.ItemNumber 
                                            and EA.ObjectID is not null 
                                            and EF.FieldTypeID is not null
	                        inner join Field F on F.FieldTypeId = EF.FieldTypeID 
                                            and F.ObjectType = EA.Object 
                                            and F.ObjectId = EA.ObjectID
                    where   EA.ExecutionID = @executionID 
                            and EA.IsNew <> 1 
                            {(!isInsert ? "and F.FormattedValue <> EF.FieldValue" : "")} 
                            and EA.ItemNumber between @beginItemNumber and @endItemNumber

                    union all

                    select  EA.Object, 
                            EA.ObjectID, 
                            EF.FieldTypeID AS Id 
                    from    {tableName} EA 
	                        inner join {ApiExecutionFieldTable} EF on EF.ExecutionID = EA.ExecutionID 
                                            and EF.ItemNumber = EA.ItemNumber 
                                            and EA.ObjectID is not null 
                                            and EF.FieldTypeID is not null
                    where   EA.ExecutionID = @executionID 
                            and EA.IsNew <> 1 
                            and EA.ItemNumber between @beginItemNumber and @endItemNumber
                            {(!isInsert ? "and coalesce(EF.FieldValue, '') <> ''" : "")} 
                            and not exists (select 1 from Field where FieldTypeID = EF.FieldTypeID 
                                and ObjectType = EA.Object and ObjectID = EA.ObjectID)
                ";

                if (!isInsert)
                {
                    changedFieldsSql += $@"
                    union all

                    select  F.ObjectType as [Object], 
                            F.ObjectID, 
                            F.FieldTypeID as Id
                    from    Field F
                    	    inner join {tableName} E on E.ExecutionID = @executionID 
                    	    inner join {ApiExecutionFieldTable} EF on EF.ExecutionId = E.ExecutionId and EF.ItemNumber = E.ItemNumber
                    	    inner join Asset A on A.uid = E.Uid                  
                    where   E.ExecutionID = @executionID
                            and EF.ItemNumber between @beginItemNumber and @endItemNumber
                            and EF.Ignore is null
                            and EF.FieldTypeID is not null
                            and F.ObjectID = A.ObjectID
                            and F.ObjectType = A.Object
                            and F.FieldTypeID = EF.FieldTypeID
                            and EF.FieldValue is null 
                            and EF.LookupValue is null";
                }

                res = Connection.Query<AssetFieldTypeUpdate>(changedFieldsSql, new { executionID, beginItemNumber, endItemNumber }, transaction: trans, commandTimeout: timeout).ToList();
            }

            // if we already have the asset id then insert it
            bool hasAssetID = ((tableName ?? "").ToUpper() == "API.EXECUTIONASSET");

            var fieldValuesSql = $@"
                                select 
                                        {objectSqlSyntax} as [Object]
                                        ,{objectIdSqlSyntax} as [ObjectID] 
                                        ,F.FieldTypeID as [FieldTypeID]                                        
                                        ,case 
                                            when FT.Type = 'Link' then F.FieldValue
                                            else F.LookupValue
                                        end as [Value]
                                        ,F.FieldValue as [FormattedValue]
                                        ,getutcdate() as [UpdatedOn]
                                        ,@resourceId as [UpdatedBy]
                                        {(hasAssetID ? ",A.AssetID as AssetID" : ",null as AssetID")}                                          
                                from    {tableName} A
                                        inner join {ApiExecutionFieldTable} F on F.ExecutionID = A.ExecutionID
                                            and F.ItemNumber = A.ItemNumber 
                                            and A.ObjectID is not null 
                                            and F.FieldTypeID is not null
						                    and A.Success is null
                                        inner join FieldType FT on FT.Id = F.FieldTypeID
                                where   A.ExecutionID = @executionID
                                        and A.ItemNumber between @beginItemNumber and @endItemNumber 
                                        and (F.Ignore = 0 or F.Ignore is null)
                                        and FT.Type != 'Relationship'
                                        and FT.Type != 'Counter'
                                        and FieldValue is not null";

            var lookupFieldValuesSql = $@"
                                select 
                                        {objectSqlSyntax} as [Object]
                                        ,{objectIdSqlSyntax} as [ObjectID] 
                                        ,F.FieldTypeID as [FieldTypeID]                                        
                                        ,F.LookupValue as [Value]
                                        ,F.FieldValue as [FormattedValue]
                                        ,getutcdate() as [UpdatedOn]
                                        ,@resourceId as [UpdatedBy]
                                        {(hasAssetID ? ",A.AssetID as AssetID" : ",null as AssetID")}                                          
                                from    {tableName} A
                                        inner join {ApiExecutionFieldTable} F on F.ExecutionID = A.ExecutionID
                                            and F.ItemNumber = A.ItemNumber 
                                            and A.ObjectID is not null 
                                            and F.FieldTypeID is not null
						                    and A.Success is null
                                        inner join FieldType FT on FT.Id = F.FieldTypeID
                                where   A.ExecutionID = @executionID
                                        and A.ItemNumber between @beginItemNumber and @endItemNumber 
                                        and (F.Ignore = 0 or F.Ignore is null)
                                        and FT.Type = 'Lookup'
                                        and FieldValue is not null";

            // Insert can blast in field values since all the assets are new.  Update needs to update the existing values and clear any existing
            if (isInsert)
            {
                Connection.Execute(
                    $@"
                        INSERT INTO 
                        dbo.[Field] ([ObjectType],[ObjectID],[FieldTypeID],[Value],[FormattedValue],[UpdatedOn],[UpdatedBy],[AssetID])                         
                        {fieldValuesSql}
                    "
                    , new { executionID, beginItemNumber, endItemNumber, resourceId = CurrentResourceID }, transaction: trans, commandTimeout: timeout);
            }
            else
            {
                Connection.Execute($@"
                    DELETE Field
                    FROM Field F
                    	inner join {tableName} A on A.ExecutionID = @executionID 
                    	inner join {ApiExecutionFieldTable} EF on EF.ExecutionId = A.ExecutionId and EF.ItemNumber = A.ItemNumber
                    WHERE EF.ItemNumber between @beginItemNumber and @endItemNumber
                     and EF.Ignore is null
                     and EF.FieldTypeID is not null
                     and F.ObjectID = {objectIdSqlSyntax}
                     and F.ObjectType = {objectSqlSyntax}
                     and F.FieldTypeID = EF.FieldTypeID
                     and EF.FieldValue is null 
                     and EF.LookupValue is null;",
                new { executionID, beginItemNumber, endItemNumber, resourceId = CurrentResourceID }, transaction: trans, commandTimeout: timeout);


                // update non-lookup fields
                Connection.Execute($@"
                    merge       Field as T
                    using       (
                                    {fieldValuesSql} and FT.Type != 'Lookup'
                                ) as S 
                    on          ( T.FieldTypeID = S.FieldTypeID and T.ObjectType = S.Object and T.ObjectID = S.ObjectID )
                    when matched and T.Value <> S.Value COLLATE SQL_Latin1_General_CP1_CS_AS OR T.FormattedValue <> S.FormattedValue COLLATE SQL_Latin1_General_CP1_CS_AS then
                    update set T.Value = S.Value,T.FormattedValue = S.FormattedValue, T.UpdatedBy = @resourceId, T.UpdatedOn = getutcdate()                     
                    when		not matched by target then
                    insert		(FieldTypeID, ObjectType, ObjectID, Value, FormattedValue, UpdatedBy, UpdatedOn, AssetID)
                    values		(S.FieldTypeID, S.Object, S.ObjectID, S.Value, S.FormattedValue, @resourceId, getutcdate(), S.AssetID);",
                                new { executionID, sendWorkflowEvents, beginItemNumber, endItemNumber, resourceId = CurrentResourceID }, transaction: trans, commandTimeout: timeout);

                if (hasLookupFieldTypes)
                {
                    // update lookup fields, DO NOT SET THE FORMATTED VALUE to the ID only compare on the id since you dont have the formatted value...
                    Connection.Execute($@"
                    merge       Field as T
                    using       (
                                    {lookupFieldValuesSql}
                                ) as S 
                    on          ( T.FieldTypeID = S.FieldTypeID and T.ObjectType = S.Object and T.ObjectID = S.ObjectID )
                    when matched and T.Value <> S.Value COLLATE SQL_Latin1_General_CP1_CS_AS then
                    update set T.Value = S.Value, T.UpdatedBy = @resourceId, T.UpdatedOn = getutcdate()                     
                    when		not matched by target then
                    insert		(FieldTypeID, ObjectType, ObjectID, Value, FormattedValue, UpdatedBy, UpdatedOn, AssetID)
                    values		(S.FieldTypeID, S.Object, S.ObjectID, S.Value, S.FormattedValue, @resourceId, getutcdate(), S.AssetID);",
                                    new { executionID, sendWorkflowEvents, beginItemNumber, endItemNumber, resourceId = CurrentResourceID }, transaction: trans, commandTimeout: timeout);
                }
            }

            return res;
        }

        public List<AssetFieldTypeUpdate> UpdateCounterFields(int assetTypeId, Guid executionID, SqlTransaction trans, int beginItemNumber, int endItemNumber, bool sendWorkflowEvents, int timeout = 3600)
        {
            Connection.Execute(
                      $@"insert into FieldCounterValue (AssetId, AssetTypeId, FieldTypeId, [Value])
                        select distinct ea.assetid, ft.assettypeid, ft.id, ef.FieldValue 
                            from api.ExecutionAsset ea
                        inner join FieldType ft on ft.AssetTypeID = @assetTypeId and ft.Type = @dataType
                        inner join api.execution ex on ex.executionid = @executionid
                        left join {ApiExecutionFieldTable} ef on ef.executionid = @executionid and ef.itemnumber = ea.itemnumber and ft.id = ef.fieldtypeid
                        left join dbo.FieldCounterValue FCV on FCV.AssetId = ea.assetid and FCV.FieldTypeId = ft.id
                        where ea.ExecutionID = @executionID 
                                and ea.Success is null and ea.assetid is not null
                                and ea.ItemNumber between @beginItemNumber and @endItemNumber
                                and ((ex.Method = 'PUT' and ef.FieldValue is not null and cast(ef.FieldValue as int) <> isnull(FCV.Value,0)) or ex.Method = 'POST' or ex.Method = 'BULK');"
                      , new { executionID, beginItemNumber, endItemNumber, resourceId = CurrentResourceID, assetTypeId, dataType = DataType.Counter.ToString() }, transaction: trans, commandTimeout: timeout);
            if (sendWorkflowEvents)
            {
                return Connection.Query<AssetFieldTypeUpdate>($@"
                        select ea.[object], ea.[objectid], ft.id from api.ExecutionAsset ea
                        inner join FieldType ft on ft.AssetTypeID = @assetTypeId and ft.Type = @dataType
                        where ea.ExecutionID = @executionID and ea.Success is null and ea.ItemNumber between @beginItemNumber and @endItemNumber",
                    new { executionID, beginItemNumber, endItemNumber, resourceId = CurrentResourceID, assetTypeId, dataType = DataType.Counter.ToString() }, transaction: trans, commandTimeout: timeout).ToList();
            }
            else
            {
                return new List<AssetFieldTypeUpdate>();
            }
        }


        public void ImportRelationships(Guid executionID, SqlTransaction trans, string tableName, string objectSqlSyntax, string objectIdSqlSyntax, int beginItemNumber, int endItemNumber, int timeout = 3600, bool resolveRelationshipOnObjectId = false, bool sendGraphEvents = true)
        {

            string assetJoin = resolveRelationshipOnObjectId ? "S.ObjectID = try_cast(V.[value] as int)" : "S.DisplayValue = V.[value]";

            var sql = $@"

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
                                inner join {ApiExecutionFieldTable} F on F.ExecutionID = A.ExecutionID
                                    and F.ItemNumber = A.ItemNumber 
                                    and A.ObjectID is not null 
                                    and F.FieldTypeID is not null
						            and A.Success is null
                                cross apply string_split(left(F.FieldValue,4000), ',') V                                    
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
                        insert into #Relationships WITH(TABLOCK) (ID, [uid], IntersectTypeID, SubjectAssetTypeID, Subject, SubjectId, ObjectAssetTypeID, Object, ObjectID, SwitchObject)
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


                        insert into #DeletedRelationships WITH(TABLOCK)
                            select I.[uid]  from {tableName} A
                                inner join {ApiExecutionFieldTable} F on F.ExecutionID = A.ExecutionID
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
";

            var events = Connection.Query<DatabaseBulkRelationshipResult>(sql,
            new { executionID, beginItemNumber, endItemNumber }, transaction: trans, commandTimeout: timeout);


            if (sendGraphEvents)
            {
                SendAssetGraphEvents(events);
            }

        }

        private void MergeJsonFieldProperties(Guid executionID, SqlTransaction trans, List<FieldType> jsonFieldTypes, string tableName, string objectSqlSyntax, string objectIdSqlSyntax, int beginItemNumber, int endItemNumber, int timeout = 3600, Dictionary<string, double> metrics = null, int step = 0, bool isInsert = false)
        {
            var sw = Stopwatch.StartNew();
            var jsonFieldTypeIDs = string.Join(",", jsonFieldTypes.Select(i => i.ID));
            var fields = Connection.Query<dynamic>($@"
                    select  F.ID, 
                            F.FormattedValue 
                    from    Field F 
                            inner join {ApiExecutionFieldTable} E on E.ExecutionID = @executionID and E.ItemNumber between @beginItemNumber and @endItemNumber and E.FieldTypeID = F.FieldTypeID and E.FieldTypeID in ({jsonFieldTypeIDs})
                            inner join {tableName} A on A.ExecutionID = E.ExecutionID and A.ItemNumber = E.ItemNumber and A.Object = F.ObjectType and A.ObjectID = F.ObjectID",
                            new { executionID, beginItemNumber, endItemNumber }, transaction: trans, commandTimeout: timeout);

            if (metrics != null) AddMeasurement(metrics, $"MergeJsonFieldProperties >> loadfields", sw.ElapsedMilliseconds, ++step);

            sw.Restart();

            //check for 0 fields to update case which often happens when editing from ui since you cant edit json fields.
            if (!fields.Any()) return;

            var collectionFieldProperties = new List<FieldJsonProperty>();

            foreach (var f in fields)
            {
                string value = f.FormattedValue;
                if (!string.IsNullOrEmpty(value))
                {
                    List<FieldJsonProperty> assetFieldProperties = value.ParseJsonIntoJsonPropertiesCollection();
                    assetFieldProperties.ForEach(i =>
                    {
                        i.FieldID = f.ID;
                    });
                    collectionFieldProperties.AddRange(assetFieldProperties);
                }

            }

            if (metrics != null) AddMeasurement(metrics, $"MergeJsonFieldProperties >> iterate properties", sw.ElapsedMilliseconds, ++step);

            sw.Restart();


            //delete old json field values if this is not a POST
            if (!isInsert)
            {
                Connection.Execute($@"
                    delete from FieldJsonProperty where fieldid in(
                    select  F.ID
                    from    Field F 
                            inner join {ApiExecutionFieldTable} E on E.ExecutionID = @executionID and E.ItemNumber between @beginItemNumber and @endItemNumber and E.FieldTypeID = F.FieldTypeID and E.FieldTypeID in ({jsonFieldTypeIDs})
                            inner join {tableName} A on A.ExecutionID = E.ExecutionID and A.ItemNumber = E.ItemNumber and A.Object = F.ObjectType and A.ObjectID = F.ObjectID)",
                            new { executionID, beginItemNumber, endItemNumber }, transaction: trans, commandTimeout: timeout);
            }

            if (metrics != null) AddMeasurement(metrics, $"MergeJsonFieldProperties >> delete old values", sw.ElapsedMilliseconds, ++step);

            sw.Restart();


            #region Build data tables for bulk load.


            var table = new DataTable();
            table.Columns.Add("FieldID", typeof(long));
            table.Columns.Add("Name", typeof(string));
            table.Columns.Add("Parent", typeof(string));
            table.Columns.Add("Path", typeof(string));
            table.Columns.Add("Position", typeof(int));
            table.Columns.Add("IsArray", typeof(bool));
            table.Columns.Add("Value", typeof(string));
            table.Columns.Add("CreatedBy", typeof(int));
            table.Columns.Add("UpdatedBy", typeof(int));


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
                row["CreatedBy"] = CurrentResourceID;
                row["UpdatedBy"] = CurrentResourceID;

                table.Rows.Add(row);
            }

            using (var bulkCopy = new SqlBulkCopy((SqlConnection)Database.Connection, SqlBulkCopyOptions.TableLock, trans)
            {
                BatchSize = SqlBulkBatchSize,
                DestinationTableName = "FieldJsonProperty",
                BulkCopyTimeout = SqlBulkBatchTimeout
            })
            {
                bulkCopy.ColumnMappings.Add("FieldID", "FieldID");
                bulkCopy.ColumnMappings.Add("Name", "Name");
                bulkCopy.ColumnMappings.Add("Parent", "Parent");
                bulkCopy.ColumnMappings.Add("Path", "Path");
                bulkCopy.ColumnMappings.Add("Position", "Position");
                bulkCopy.ColumnMappings.Add("IsArray", "IsArray");
                bulkCopy.ColumnMappings.Add("Value", "Value");
                bulkCopy.ColumnMappings.Add("CreatedBy", "CreatedBy");
                bulkCopy.ColumnMappings.Add("UpdatedBy", "UpdatedBy");

                bulkCopy.WriteToServer(table);
            }

            if (metrics != null) AddMeasurement(metrics, $"MergeJsonFieldProperties >> bulk load values", sw.ElapsedMilliseconds, ++step);

            sw.Restart();

            #endregion            
        }

        public void CopyFieldLookupValuesAsIs(Guid executionID, int timeout = 3600, string fieldTable = "api.ExecutionField", SqlTransaction trans = null)
        {
            Connection.Execute($@"
        update	T
        set		T.LookupValue = T.[FieldValue]
        from	{fieldTable} T
		inner join FieldType ST on ST.ID = T.FieldTypeID and ST.[Type] = 'Lookup' and T.ExecutionID = @executionID
            ", new { executionID }, commandTimeout: timeout, transaction: trans);
        }

        public void ResolveFieldLookupValues(Guid executionID, string fieldTable = "api.ExecutionField", int timeout = 3600, SqlTransaction trans = null)
        {
            Connection.Execute($@"
drop table if exists #RelevantLookupValues;
create table #RelevantLookupValues (FieldTypeID int not null, [Text] nvarchar(max), [Value] nvarchar(max));

;with field_type_ids as( 
select distinct F.Id from {fieldTable} T
				inner join FieldType F on F.ID = T.FieldTypeID and F.[Type] = 'Lookup' and T.ExecutionID = @executionID)
				insert into #RelevantLookupValues WITH(TABLOCK)
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
insert into #LookupValues WITH(TABLOCK)
select FieldValue, Id, STRING_AGG(Value, ',') from cte_fieldvalues_multi
group by fieldvalue, Id

;insert into #LookupValues WITH(TABLOCK)
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
            Connection.Execute($@"
                        update  T 
                        set     T.Success = 0,
                                T.Message = coalesce(T.Message, '') + 'Rule asset contains an invalid threshold; '
                        from    api.ExecutionAsset T
                                inner join {ApiExecutionFieldTable} S on S.ExecutionID = T.ExecutionID and T.ExecutionID = @executionID and S.ItemNumber = T.ItemNumber and S.FieldName = 'Threshold' and ISNUMERIC(S.FieldValue) = 0;
                        ", new { executionID }, commandTimeout: timeout);
        }

        private void ResolveColorValues(Guid executionID, int timeout = 3600)
        {
            Connection.Execute($@"

                        update  F
                        set     F.LookupValue = C.Id
                        from    {ApiExecutionFieldTable} F
                                left join Color C on C.Name = F.FieldValue
                        where   F.ExecutionID = @executionID and F.FieldName = 'Color' and SUBSTRING(F.FieldValue,1,1) <> '#'

                        update  F
                        set     F.LookupValue = F.FieldValue
                        from    {ApiExecutionFieldTable} F
                        where   F.ExecutionID = @executionID and F.FieldName = 'Color' and SUBSTRING(F.FieldValue,1,1) = '#'

                        update  F
                        set     F.LookupValue = null
                        from    {ApiExecutionFieldTable} F
                        where   F.ExecutionID = @executionID and F.FieldName = 'Color' and coalesce(F.FieldValue, '') = ''
                        
                        update  T 
                        set     T.Success = 0,
                                T.Message = coalesce(T.Message, '') + 'Color value is not a valid Govern color; '
                        from    api.ExecutionAsset T
                                inner join {ApiExecutionFieldTable} S on S.ExecutionID = T.ExecutionID and T.ExecutionID = @executionID and S.ItemNumber = T.ItemNumber and S.FieldName = 'Color' 
                        where   S.LookupValue is null and coalesce(S.FieldValue, '') <> ''
                        ", new { executionID }, commandTimeout: timeout);
        }

        public void SendWorkflowEvents(string objectType, int objectTypeID, IEnumerable<IWorkflowEnabledAsset> results, ChangeType? changeTypeOverride = null, List<AssetFieldTypeUpdate> fieldUpdates = null, ScoreType? scoreType = null)
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
                                ChangedFieldIds = changedFieldsIDS,
                                ScoreType = scoreType
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

        public void SendAssetGraphEvents(IEnumerable<IGraphAsset> results, Dictionary<Guid, List<string>> fields = null, bool delayedDelivery = false)
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
            bool allowTagFields = false,
            FieldValidationFieldProperties validationFieldProperties = null,
            bool jsonElementsEnabled = true
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
                bool isValueEmptyString = k.Value == string.Empty;

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
                        if (validationFieldProperties != null) validationFieldProperties.ContainsColorField = true;
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
                            if (isValueEmptyString)
                            {
                                switch (fieldType.Type)
                                {
                                    case "Boolean":
                                        errorMessages.Add($"{fieldName} is a boolean field and may only be 'false' or 'true'");
                                        success = false;
                                        break;
                                    case "Date":
                                        errorMessages.Add($"{fieldName} must be a valid date");
                                        success = false;
                                        break;
                                    case "DateTime":
                                        errorMessages.Add($"{fieldName} must be a valid datetime value");
                                        success = false;
                                        break;
                                    case "Decimal":
                                        errorMessages.Add($"{fieldName} must be a valid decimal");
                                        success = false;
                                        break;
                                    case "Number":
                                        errorMessages.Add($"{fieldName} must be a valid number");
                                        success = false;
                                        break;
                                }
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
                                        fieldValue = dtTest.ToUniversalTime().ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'"); ;
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
                                    if (success)
                                    {
                                        //Remove 'inner' trailing/leading spaces in link value
                                        fieldValue = Regex.Replace(fieldValue, "(\\s*\\|\\s*)", "|");
                                    }
                                    break;
                                case "Lookup":
                                    if (fieldType.AllowMultipleValues == false && fieldValue.Split(',').Length > 1)
                                    {
                                        success = false;
                                        errorMessages.Add($"{fieldName} does not allow selection of multiple values");
                                    }
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
                                    if (jsonElementsEnabled && (fieldValue.Length > 2500))
                                    {
                                        success = false;
                                        errorMessages.Add($"{fieldName} exceeds the maximum length of 2500 characters");
                                    }
                                    validationFieldProperties.JsonFieldCount++;
                                    break;
                                case "Counter":
                                    int counterValue = 0;
                                    if (!int.TryParse(fieldValue, out counterValue) || counterValue <= 0)
                                    {
                                        success = false;
                                        errorMessages.Add($"{fieldName} must be a valid whole number, greater than 0 and less than 2147483647.");
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
            var swBegin = Stopwatch.StartNew();
            TelemetryClient client = new TelemetryClient();
            const string METHOD_NAME = "RemoveAssets";
            bool isLog = true; // trace info for all assets is extermely useful
            var metrics = new Dictionary<string, double>();
            var step = 0;
            var results = new List<DatabaseBulkAssetResult>();
            var graphResults = new List<DatabaseBulkAssetResult>();
            var dt = DateTime.UtcNow;
            bool generalChecksCompleted = false;

            bool canHaveProcess = TypeHasProcessRelationshipTypes(at);

            CurrentExecutionLocationModel currentLocation = null;

            SetApiExecutionProcessingStartTime(execution.ExecutionID);

            //check if trigger workflows is set to true and there are actually no workflows in which case shut off triggering of workflows
            var sw = Stopwatch.StartNew();
            sendWorkflowEvents = sendWorkflowEvents && TypeHasWorkflows(at.Object, at.ObjectID, ChangeType.Delete);

            AddMeasurement(metrics, "Check for workflows", sw.ElapsedMilliseconds, ++step);
            sw.Restart();

            var executionItemDupes = import.Where(i => i.ExecutionItemUid.HasValue && i.ExecutionItemUid.Value != Guid.Empty).GroupBy(i => i.ExecutionItemUid).Where(i => i.Count() > 1).Select(i => new { ExecutionItemUid = i.Key, Count = i.Count() }).ToList();

            AddMeasurement(metrics, "Checking for duplicate execution uids", sw.ElapsedMilliseconds, ++step);
            sw.Restart();

            if (executionItemDupes.Any())
            {
                execution.ErrorMessage = $"Duplicate execution item identifiers: {string.Join(", ", executionItemDupes.Select(i => i.ExecutionItemUid.ToString()))}. Identifiers must be unique within a batch.";
                results.AddRange(import.Select(i => new DatabaseBulkAssetResult { ExecutionItemUid = i.ExecutionItemUid, uid = i.Uid, Message = execution.ErrorMessage, Success = false }));
            }
            else
            {

                var uidDupes = import.GroupBy(i => i.Uid).Where(i => i.Count() > 1).Select(i => new { Uid = i.Key, Count = i.Count() }).ToList();

                AddMeasurement(metrics, "Checking for duplicate asset uids", sw.ElapsedMilliseconds, ++step);
                sw.Restart();

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

                        AddMeasurement(metrics, "Getting execution current location", sw.ElapsedMilliseconds, ++step);
                        sw.Restart();

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

                        AddMeasurement(metrics, "BuildDatatable and initialization", sw.ElapsedMilliseconds, ++step);
                        sw.Restart();

                        #endregion

                        #region Resolve assets based on UIDs

                        Connection.Execute(@"
update	T
set		T.Object = S.Object, 
        T.ObjectID = S.ObjectID, 
        T.AssetID = S.ID
from	api.ExecutionDeletedAsset T
		inner join Asset S on S.Uid = T.Uid and T.ExecutionID = @ExecutionID
where 
        exists (select 1 from AssetType ST where ST.Uid = @uid and ST.ID = S.AssetTypeID);",
                    new { execution.ExecutionID, at.uid }, commandTimeout: timeout);

                        AddMeasurement(metrics, "Resolve assets based on UIDs", sw.ElapsedMilliseconds, ++step);
                        sw.Restart();

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

                        AddMeasurement(metrics, "Log lookup errors invalid asset uids or asset ids", sw.ElapsedMilliseconds, ++step);
                        sw.Restart();

                        //Check if asset Results exist 
                        Connection.Execute($@"
update	T
set		T.Success = 0,
		T.[Message] = coalesce([Message] + '; ', '') + 'You have not enabled Cascade, yet there are ' + cast(ARE.ResultCount as nvarchar) + ' results(s) present for this rule.'
from    api.ExecutionDeletedAsset T
        inner join graph.AssetNode AN on AN.ID = T.AssetID
        cross apply (select count(1) as ResultCount from AssetResultEdge where $from_id = AN.$node_id having count(1) > 0) ARE
where	T.ExecutionID = @ExecutionID
        and T.[Cascade] = 0
        and exists (select 1 from AssetType AT where AT.ID = AN.AssetTypeID and AT.Class = {(int)AssetTypeClass.Rule});",
            new { execution.ExecutionID }, commandTimeout: timeout);

                        AddMeasurement(metrics, "Log error asset result exists with not enabled cascade", sw.ElapsedMilliseconds, ++step);
                        sw.Restart();

                        #endregion

                        // Validate permissions
                        LogAssetPermissionErrors(execution.ExecutionID, at, Permission.DeleteAsset, "ExecutionDeletedAsset");
                        AddMeasurement(metrics, "LogAssetPermissionErrors", sw.ElapsedMilliseconds, ++step);
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
                                            sw.Restart();
                                            var data = Connection.Query<dynamic>($@"
                                                    drop table if exists #forDelete

                                                create table #forDelete (ID int, Type varchar(50))
                                                create nonclustered index cix_forDelete on #forDelete (Type, ID)
                                                create nonclustered index cix_forDeleteID on #forDelete (ID)

                                                declare @result table (Status bit, Message varchar(255))
                                                                                                        
                                                declare @fusionId int = (select ObjectID from api.ExecutionDeletedAsset
                                                    where ExecutionID = @ExecutionID AND Object = 'Fusion')
                                                    
                                                insert into #forDelete values(@fusionId, 'Fusion')
                                                    
                                                insert into #forDelete select ID,'Asset' as Type from Asset where Object = 'Fusion' and ObjectID = @fusionId
                                                    
                                                insert into #forDelete
                                                select ID as ID,'FusionAttribute' as Type from FusionAttribute where FusionID = @fusionId
                                                    
                                                    insert into #forDelete
                                                    	select I.ID, 'Intersect' as Type
                                                    	from [Intersect] I where I.[Object] = 'FusionAttribute'
                                                        and exists (select 1 from #forDelete FD where FD.Type = 'FusionAttribute' and FD.ID = I.[ObjectID])
                                                    
                                                insert into #forDelete
                                                    select I.ID, 'Intersect' as Type
                                                    from [Intersect] I where I.[Subject] = 'FusionAttribute'
                                                    and exists (select 1 from #forDelete FD where FD.Type = 'FusionAttribute' and FD.ID = I.[SubjectID])
                                                    
                                                insert into #forDelete
                                                    select F.ID, 'Field' as Type
                                                    from Field F where F.ObjectType = 'FusionAttribute'
                                                    and exists (select 1 from #forDelete FD where FD.Type = 'FusionAttribute' and FD.ID = F.ObjectID)
                                                    
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

                                            AddMeasurement(metrics, $"Fusion type delete assets >> {currentLoop} >> {retryCount}", sw.ElapsedMilliseconds, ++step);
                                            sw.Restart();

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
                                            AddMeasurement(metrics, $"update delete status for Fusion type delete assets>> {currentLoop} >> {retryCount}", sw.ElapsedMilliseconds, ++step);
                                            sw.Restart();

                                        }
                                        else
                                        {
                                            #region Cascade Behaviour

                                            // Parent/Child Relationships
                                            if (predicateType.HasValue)
                                            {
                                                sw.Restart();

                                                Connection.Execute($@" 
        if OBJECT_ID('tempdb..#ExecutionDeletedAsset') IS NOT NULL
            truncate TABLE #ExecutionDeletedAsset
        else
            begin
                create table #ExecutionDeletedAsset (
                    ExecutionID	uniqueidentifier,
                    [Root] uniqueidentifier,
                    ItemNumber	int,
                    Uid	uniqueidentifier,
                    AssetID	bigint,
                    FromHierarchy	bit
                );

                create nonclustered index cix_tempExecutionDeletedAsset on #ExecutionDeletedAsset([Root], ExecutionID, ItemNumber)
            end;

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
            where   P.ItemNumber between @beginItemNumber and @endItemNumber and P.[Level] <= 1
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
                    and not exists (select 1 from api.ExecutionDeletedAsset ed where ed.ExecutionID = h.ExecutionID and ed.ItemNumber = h.ItemNumber and ed.Uid = h.uid)
			        and  ExecutionID = @ExecutionID;

        drop table if exists #tempChildTable;

        select [Root] as UID,
            ExecutionID,
            ItemNumber
        into #tempChildTable
        from #ExecutionDeletedAsset
        group by [Root], ExecutionID, ItemNumber
        having count(1) > 0;

        create nonclustered index cix_tempchildtable on #tempChildTable (UID, ExecutionID, ItemNumber);
            
		update  S 
        set     S.Success = 0 ,
			    [Message] ='You have not enabled Cascade, yet there are child relationships for this asset.'
		from    api.ExecutionDeletedAsset S 
			    inner join #tempChildTable E on S.Uid= E.UID and s.ItemNumber=E.ItemNumber and s.ExecutionID = e.ExecutionID
		where	{querySuffix}  and AssetId is not null
			    and S.[Cascade] = 0;

        drop table if exists #tempChildTable;", new { execution.ExecutionID, predicateTypeValue = predicateType.HasValue ? (int)predicateType : -1, beginItemNumber, endItemNumber }, transaction: trans, commandTimeout: timeout);
                                            }

                                            AddMeasurement(metrics, $"Log parent and child relationships assets without cascade enabled>> {currentLoop} >> {retryCount}", sw.ElapsedMilliseconds, ++step);
                                            sw.Restart();


                                            // Workflows
                                            Connection.Execute($@" 
        if OBJECT_ID('tempdb..#ExecutionDeletedAsset') IS NOT NULL
            truncate TABLE #ExecutionDeletedAsset
        else
            begin
                create table #ExecutionDeletedAsset (
                    ExecutionID	uniqueidentifier,
                    [Root] uniqueidentifier,
                    ItemNumber	int,
                    Uid	uniqueidentifier,
                    AssetID	bigint,
                    FromHierarchy	bit
                );

                create nonclustered index cix_tempExecutionDeletedAsset on #ExecutionDeletedAsset([Root], ExecutionID, ItemNumber)
            end;

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

        drop table if exists #tempworkflow;

        select [Root] as UID,
            ExecutionID,
            ItemNumber
        into #tempworkflow
        from #ExecutionDeletedAsset
        group by [Root], ExecutionID, ItemNumber
        having count(1) > 0;

        create nonclustered index cix_tempworkflow on #tempworkflow (UID, ExecutionID, ItemNumber);
            
		update  S 
        set     S.Success = 0 ,
			    [Message] ='You have not enabled Cascade, yet there are workflows for this asset.'
		from    api.ExecutionDeletedAsset S 
			    inner join #tempworkflow E on S.Uid= E.UID and s.ItemNumber=E.ItemNumber and s.ExecutionID = e.ExecutionID
		where	{querySuffix}  and AssetId is not null
			    and S.[Cascade] = 0;

        drop table if exists #tempworkflow;", new { execution.ExecutionID, beginItemNumber, endItemNumber }, transaction: trans, commandTimeout: timeout);

                                            AddMeasurement(metrics, $"Log workflow for assets exists without cascade enabled>> {currentLoop} >> {retryCount}", sw.ElapsedMilliseconds, ++step);
                                            sw.Restart();

                                            #endregion

                                            // Get the hierarchy items we also need to remove
                                            if (predicateType.HasValue)
                                            {
                                                sw.Restart();
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
            and not exists (select 1 from api.ExecutionDeletedAsset ed where ed.ExecutionID = @ExecutionID and ed.Uid = h.Uid)",
                                                new { execution.ExecutionID, beginItemNumber, endItemNumber }, transaction: trans, commandTimeout: timeout);

                                                AddMeasurement(metrics, $"Get the hierarchy items we also need to remove>> {currentLoop} >> {retryCount}", sw.ElapsedMilliseconds, ++step);
                                                sw.Restart();

                                            }

                                            #region Delete workflow items

                                            Connection.Execute($@"
declare @count bigint = 0;

create table #w (ItemID int);
create nonclustered index cix_tempw on #w(ItemID);

drop table if exists #tempExecutionDeletedAsset;
    
select S.[Object], S.[ObjectID]
into #tempExecutionDeletedAsset
from api.ExecutionDeletedAsset S
where {querySuffix};

create nonclustered index cix_tempExecutionDeletedAsset on #tempExecutionDeletedAsset([Object], [ObjectID]);

insert into #w
	select	distinct 
			wi.ID 
	from	workflow.[Type] wt
			inner join workflow.EventRegistration we on we.typeid = wt.id and we.changetype <> 3
			inner join workflow.[Version] wv on wt.id = wv.typeId
			inner join workflow.Item wi on 	wv.id = wi.VersionID
			inner join #tempExecutionDeletedAsset S on S.Object = wi.Object and S.ObjectID = wi.ObjectID;

insert into #w
	select	wi.id 
	from	workflow.Item wi
			inner join Issue i on wi.object = 'Issue' and i.id = wi.objectid
			inner join #tempExecutionDeletedAsset S on S.Object = i.Object and S.ObjectID = i.ObjectID;

drop table if exists #tempExecutionDeletedAsset;

select @count = count(1) from #w;

if(@count > 0)
begin
    delete	T
    from	[workflow].[ItemAssignment] T
		    where exists(select 1 from #w S where S.ItemID = T.ItemID);

    delete  T
    from	[workflow].[ItemStepTransition] T
		    inner join workflow.itemstep wis on (wis.ID = T.ToItemStepID or wis.ID = T.FromItemStepID)
		    where exists (select 1 from #w S where S.ItemID = wis.ItemID);

    delete  wis
    from workflow.itemstep wis
    where	exists (Select 1 from #w S where S.ItemID = wis.ItemID);
 
    delete  wi
    from [workflow].[Item] wi
    where	exists (Select 1 from #w S where S.ItemID = wi.ID);
end;", new { execution.ExecutionID, beginItemNumber, endItemNumber }, transaction: trans, commandTimeout: timeout);
                                            AddMeasurement(metrics, $"Delete workflow items>> {currentLoop} >> {retryCount}", sw.ElapsedMilliseconds, ++step);
                                            sw.Restart();

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
                                            AddMeasurement(metrics, $"De-index queue / Audit>> {currentLoop} >> {retryCount}", sw.ElapsedMilliseconds, ++step);
                                            sw.Restart();

                                            #endregion

                                            #region Cross-references

                                            Connection.Execute($@"
delete	T
from	AssetCrossReference T
		inner join api.ExecutionDeletedAsset S on S.[Uid] = T.[Uid] and {querySuffix};",
                                            new { execution.ExecutionID, beginItemNumber, endItemNumber }, transaction: trans, commandTimeout: timeout);
                                            AddMeasurement(metrics, $"remove from Asset Cross-references>> {currentLoop} >> {retryCount}", sw.ElapsedMilliseconds, ++step);
                                            sw.Restart();

                                            #endregion

                                            #region Process diagram
                                            if (canHaveProcess)
                                            {
                                                Connection.Execute(
                                                $@"
                        drop table if exists #delAssets
                        create table #delAssets(
	                        uid uniqueidentifier,
	                        ObjectID int
                        )

                        drop table if exists #delRel
                        create table #delRel(
	                        uid uniqueidentifier,
	                        ID int
                        )

                        insert into #delAssets
                        select fromuid, a.ObjectID from ProcessExpandedData pxd
	                        inner join asset a on a.uid = pxd.diagramassetuid
                        where pxd.diagramassetuid in (select S.Uid from api.ExecutionDeletedAsset S where {querySuffix})
                        union 
                        select touid, a.ObjectID from ProcessExpandedData pxd
	                        inner join asset a on a.uid = pxd.diagramassetuid
                        where pxd.diagramassetuid in (select S.Uid from api.ExecutionDeletedAsset S where {querySuffix})


                        insert into #delRel
                        select i.uid,I.Id from #delAssets
	                        inner join Asset A on A.uid = #delAssets.uid
	                        inner join [Intersect] I on I.Object = A.Object and I.ObjectId = A.ObjectId
                        union 
                        select i.uid,I.Id from #delAssets
	                        inner join Asset A on A.uid = #delAssets.uid
	                        inner join [Intersect] I on I.Subject = A.Object and I.SubjectID = A.ObjectId

                        delete from Field where ObjectType = 'Intersect' and ObjectID in (select ID from #delRel)

                        delete from Field where ObjectType = 'Task' and ObjectID in (select ObjectId from #delAssets)
                        delete from asset where uid in (select uid from #delAssets)

                        delete from graph.AssetNode where uid in (select uid from #delAssets) and Class = 15
",
                                                new { execution.ExecutionID, beginItemNumber, endItemNumber }, transaction: trans, commandTimeout: timeout);
                                                AddMeasurement(metrics, $"remove process assets>> {currentLoop} >> {retryCount}", sw.ElapsedMilliseconds, ++step);
                                                sw.Restart();
                                            }
                                            #endregion

                                            #region Remove default value settings from FieldTypes

                                            Connection.Execute(
                                                $@"
drop table if exists #tempassetobject;

create table #tempassetobject (id [bigint] IDENTITY(1,1) NOT NULL, Object varchar(50), ObjectID int);

insert into #tempassetobject (Object, ObjectID)
    select  a.Object,
            a.ObjectID
    from    Asset a
    where   exists (
                select  1
                from    api.ExecutionDeletedAsset S 
                where   s.Uid = A.Uid 
                        and {querySuffix}
            );

create nonclustered index cix_tempassetid on #tempassetobject (Object, ObjectID, id);

update	T
set     T.DefaultValue = null
from	dbo.FieldType T
        inner join #tempassetobject S on S.Object = T.LookupObjectType and S.ObjectID = T.DefaultValue and T.LookupObjectType is not null and T.DefaultValue is not null and T.[Type] = 'Lookup';

drop table if exists #tempassetobject;",
                                                new { execution.ExecutionID, beginItemNumber, endItemNumber }, transaction: trans, commandTimeout: timeout);

                                            #endregion

                                            #region Asset table

                                            Connection.Execute(
                                                $@"
declare @totalcount bigint = 0,
        @runcount bigint = 0,
        @struncount bigint = 0,
        @enruncount bigint = 0,
        @batchsize int = {SqlBulkIntersectFieldDeleteSize};

drop table if exists #tempassetid;
drop table if exists #tempruleresults;

create table #tempassetid (id [bigint] IDENTITY(1,1) NOT NULL, assetid [bigint]);
create table #tempruleresults ([Uid] uniqueidentifier);

insert into #tempassetid (assetid)
    select  a.ID
    from    Asset a
    where   exists (
                select  1
                from    api.ExecutionDeletedAsset S 
                where   s.Uid = A.Uid 
                        and {querySuffix}
            );

create nonclustered index cix_tempassetid on #tempassetid (assetid, id);

insert into #tempruleresults
    select	R.Uid
    from	graph.AssetNode A,
            dbo.AssetResultEdge E,
            dbo.AssetResult R
    where	MATCH(A-(E)->R)
            and E.Class = 1
            and A.Id in (select id from #tempassetid);

create clustered index cix_tempruleresults on #tempruleresults (Uid);

select @totalcount = count(id) from #tempassetid;
while (@runcount <= @totalcount)
begin
    set @struncount = @runcount + 1;
    set @enruncount = @runcount + @batchsize;

    delete  a
    from    Asset a
    where   exists (
                select  1
                from    #tempassetid S
                where   S.assetid = a.ID
                        and S.id between @struncount and @enruncount
            );

    delete	E
    from    dbo.AssetResultEdge E
            inner join graph.AssetNode N on E.$from_id = N.$node_id
    where   exists (
                select  1
                from    #tempassetid S
                where   S.assetid = N.ID
                        and S.id between @struncount and @enruncount
            );

    delete	A
    from	graph.AssetNode A
    where   exists (
                select  1
                from    #tempassetid S
                where   S.assetid = A.ID
                        and S.id between @struncount and @enruncount
            );

    set @runcount = @enruncount;
end;

delete	T
from	dbo.AssetResult T
        inner join #tempruleresults S on S.Uid = T.Uid;

drop table if exists #tempassetid; 
drop table if exists #tempruleresults;",
                                                new { execution.ExecutionID, beginItemNumber, endItemNumber }, transaction: trans, commandTimeout: timeout);
                                            AddMeasurement(metrics, $"remove from asset table>> {currentLoop} >> {retryCount}", sw.ElapsedMilliseconds, ++step);
                                            sw.Restart();

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
                                                    $@"delete t
                                                    from {legacyTable} t
                                                    where exists (select 1 from api.ExecutionDeletedAsset S where t.ID = s.ObjectID and {querySuffix})",
                                                    new { execution.ExecutionID, beginItemNumber, endItemNumber }, transaction: trans, commandTimeout: timeout);

                                                AddMeasurement(metrics, $"remove from {legacyTable} table >> {currentLoop} >> {retryCount}", sw.ElapsedMilliseconds, ++step);
                                                sw.Restart();
                                            }

                                            #endregion

                                            #region Delete Intersects

                                            Connection.Execute($@"
declare @totalcount bigint = 0,
    @runcount bigint = 0,
    @struncount bigint = 0,
    @enruncount bigint = 0,
    @batchsize int = {SqlBulkIntersectFieldDeleteSize};

    drop table if exists #tempexecdelass;

    select IntersectID, [Object], ObjectID
    into #tempexecdelass
    from api.ExecutionDeletedAsset S
    where {querySuffix};

    create nonclustered index [cix_tempexecdelass] on #tempexecdelass ([Object], ObjectID);
    create nonclustered index [cix_tempexecdelass2] on #tempexecdelass (IntersectID);

    drop table if exists #tempintersect;
    create table #tempintersect(id [bigint] IDENTITY(1,1) NOT NULL, IntersectID int); 

    if(@predicateType = 1)
    begin
        insert into #tempintersect (IntersectID)
        select T.ID
        from [Intersect] T 
		where exists (select 1 from #tempexecdelass S where S.IntersectID = T.ID and S.IntersectID is not null);
    end;

    insert into #tempintersect (IntersectID)
    select T.ID
    from [Intersect] T 
	where exists (select 1 from #tempexecdelass S where S.Object = T.Subject and S.ObjectID = T.SubjectID);

    insert into #tempintersect (IntersectID)
    select T.ID
    from [Intersect] T 
	where exists (select 1 from #tempexecdelass S where S.Object = T.Object and S.ObjectID = T.ObjectID);

    create nonclustered index [cix_tempintersect] on #tempintersect(IntersectID, id);

    delete T
    from #tempintersect T
    where T.ID > (select min(t1.ID)
        from #tempintersect t1
        where t.IntersectID = t1.IntersectID
        );

    select @totalcount = count(id) from #tempintersect;
    while (@runcount <= @totalcount)
    begin
        set @struncount = @runcount + 1;
        set @enruncount = @runcount + @batchsize;

        delete  T
        from    [Intersect] T
        where   exists (
                    select  1
                    from    #tempintersect S
                    where   S.IntersectID = T.ID
                            and S.id between @struncount and @enruncount
                );

        delete  T
        from    [graph].AssetEdge T
        where   exists (
                    select  1
                    from    #tempintersect S
                    where   S.IntersectID = T.ID
                            and S.id between @struncount and @enruncount
                );

        set @runcount = @enruncount;
    end;

    drop table if exists #tempexecdelass;
    drop table if exists #tempintersect;",
                                            new { execution.ExecutionID, beginItemNumber, endItemNumber, predicateType = predicateType.HasValue ? 1 : 0 }, transaction: trans, commandTimeout: timeout);

                                            AddMeasurement(metrics, $"remove from Intersect-(IntersectID, subject/subjectid and object/objectid) >> {currentLoop} >> {retryCount}", sw.ElapsedMilliseconds, ++step);
                                            sw.Restart();
                                            #endregion

                                            #region Delete Social tables

                                            Connection.Execute($@"
delete	T
from	CommentRelation T
		inner join api.ExecutionDeletedAsset S on S.AssetID = T.AssetID and {querySuffix};

delete	T
from	CommentVote T
		inner join Comment C on C.ID = T.CommentID
		inner join api.ExecutionDeletedAsset S on S.AssetID = C.AssetID and {querySuffix};

delete	T
from	Comment T
		inner join api.ExecutionDeletedAsset S on S.AssetID = T.AssetID and {querySuffix};

delete	T
from	Favorite T
		inner join api.ExecutionDeletedAsset S on S.Object = T.Object and S.ObjectID = T.ObjectID and {querySuffix};

delete	T
from	Follow T
		inner join api.ExecutionDeletedAsset S on S.Object = T.ObjectType and S.ObjectID = T.ObjectID and {querySuffix};",
                                            new { execution.ExecutionID, beginItemNumber, endItemNumber }, transaction: trans, commandTimeout: timeout);

                                            AddMeasurement(metrics, $"remove from social tables>> {currentLoop} >> {retryCount}", sw.ElapsedMilliseconds, ++step);
                                            sw.Restart();
                                            #endregion

                                            #region Delete subsidiary tables

                                            Connection.Execute($@"
declare @totalcount bigint = 0,
    @runcount bigint = 0,
    @struncount bigint = 0,
    @enruncount bigint = 0,
    @batchsize int = {SqlBulkIntersectFieldDeleteSize};

    drop table if exists #tempfieldid;

    create table #tempfieldid (id [bigint] IDENTITY(1,1) NOT NULL, fieldid [bigint]);

    insert into #tempfieldid (fieldid)
    select T.ID
    from Field T
    where exists (
        select 1
        from api.ExecutionDeletedAsset S where s.[Object] = T.ObjectType and S.ObjectID = T.ObjectID and {querySuffix}
    );

    create nonclustered index [cix_tempfieldid] on #tempfieldid (fieldid, id);

    select @totalcount = count(id) from #tempfieldid;
    while (@runcount <= @totalcount)
    begin
        set @struncount = @runcount + 1;
        set @enruncount = @runcount + @batchsize;

        delete T
        from Field T
        where exists (
            select 1
            from #tempfieldid S
            where S.FieldID = T.ID
            and S.id between @struncount and @enruncount
        );

        set @runcount = @enruncount;
    end;

    drop table if exists #tempfieldid;",
                                            new { execution.ExecutionID, beginItemNumber, endItemNumber }, transaction: trans, commandTimeout: timeout);

                                            AddMeasurement(metrics, $"remove from subsidiary tables field>> {currentLoop} >> {retryCount}", sw.ElapsedMilliseconds, ++step);
                                            sw.Restart();

                                            Connection.Execute($@"
                                        delete	T
from	Issue T
		inner join api.ExecutionDeletedAsset S on S.Object = T.Object and S.ObjectID = T.ObjectID and {querySuffix};

delete	T
from	Nym T
		inner join api.ExecutionDeletedAsset S on S.Object = T.Object and S.ObjectID = T.ObjectID and {querySuffix};",
                                            new { execution.ExecutionID, beginItemNumber, endItemNumber }, transaction: trans, commandTimeout: timeout);

                                            AddMeasurement(metrics, $"remove from subsidiary tables issue/nym>> {currentLoop} >> {retryCount}", sw.ElapsedMilliseconds, ++step);
                                            sw.Restart();
                                            #endregion

                                            #region Delete owner tables

                                            Connection.Execute($@"
declare @count bigint = 0;

drop table if exists #temprestable;
create table #temprestable (id bigint);
create nonclustered index cix_temprestable on #temprestable ([ID] asc);

insert into #temprestable
select T.ID
from ResponsibilityTypeRelationOverrideItem T
where exists (select 1 from api.ExecutionDeletedAsset S where S.AssetID = T.AssetID and {querySuffix});

select @count = count(1) from #temprestable;

if(@count > 0)
begin
    delete	T
    from	ResponsibilityTypeRelationOverrideItem T
		    where exists (select 1 from #temprestable S where S.ID = T.ID);
end;
drop table if exists #temprestable;

drop table if exists #temprestable2;
create table #temprestable2 (RuleID bigint, AssetID bigint);
create nonclustered index [ix_temprestable2] on #temprestable2 ([RuleID] asc, [AssetID] asc);

insert into #temprestable2
select T.RuleID, T.AssetID
from	ResponsibilityRuleResultAsset T
where exists (select 1 from api.ExecutionDeletedAsset S where S.AssetID = T.AssetID and {querySuffix});

select @count = count(1) from #temprestable2;

if(@count > 0)
begin
    delete	T
    from	ResponsibilityRuleResultAsset T
		    where exists (select 1 from #temprestable2 S where S.RuleID = T.RuleID and S.AssetID = T.AssetID);
end;
drop table if exists #temprestable2;",
                                            new { execution.ExecutionID, beginItemNumber, endItemNumber }, transaction: trans, commandTimeout: timeout);

                                            AddMeasurement(metrics, $"remove from owner tables>> {currentLoop} >> {retryCount}", sw.ElapsedMilliseconds, ++step);
                                            sw.Restart();
                                            #endregion

                                            // Update success flag
                                            Connection.Execute(
                                                $"update S set S.Success = 1 from api.ExecutionDeletedAsset S where	{querySuffix} and S.AssetID is not null;",
                                                new { execution.ExecutionID, beginItemNumber, endItemNumber }, transaction: trans, commandTimeout: timeout);

                                            AddMeasurement(metrics, $"Update status flag >> {currentLoop} >> {retryCount}", sw.ElapsedMilliseconds, ++step);
                                            sw.Restart();

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
                                            sw.Restart();
                                            LogLoopExecutionError(execution.ExecutionID, beginItemNumber, endItemNumber, "api.ExecutionDeletedAsset", ex.GetFullExceptionData(false), timeout);
                                            AddMeasurement(metrics, $"LogLoopExecutionError >> {currentLoop} >> {retryCount}", sw.ElapsedMilliseconds, ++step);
                                            sw.Restart();
                                        }
                                    }
                                }
                            }
                            sw.Restart();
                            results.AddRange(
                                Query<DatabaseBulkAssetResult>(
                                    $"select * from api.ExecutionDeletedAsset where ExecutionID = @ExecutionID and ItemNumber between @beginItemNumber and @endItemNumber and FromHierarchy = 0",
                                    new { execution.ExecutionID, beginItemNumber, endItemNumber }
                                )
                            );
                            AddMeasurement(metrics, $"results.AddRange >> DatabaseBulkAssetResult>> {currentLoop}", sw.ElapsedMilliseconds, ++step);
                            sw.Restart();

                            OnAssetsPartiallyProcessed(new AssetsPartiallyProcessedEventArgs
                            {
                                Results = results
                            });

                            AddMeasurement(metrics, $"OnAssetsPartiallyProcessed >> {currentLoop}", sw.ElapsedMilliseconds, ++step);
                            sw.Restart();

                            beginItemNumber += loopSize;
                            endItemNumber += loopSize;
                        }

                        Connection.Close();

                        if (sendWorkflowEvents)
                        {
                            SendWorkflowEvents(at.Object, at.ObjectID, results, ChangeType.Delete);
                            AddMeasurement(metrics, "SendWorkflowEvents", sw.ElapsedMilliseconds, ++step);
                            sw.Restart();
                        }

                        // Data Quality Scoring - send to engine to determine what scores need to be recalculated.
                        if (at.Class == AssetTypeClass.Rule)
                        {
                            CreateRulesRemovedExecution(execution.ExecutionID, at.ID);
                        }
                    }
                }
            }

            AddMeasurement(metrics, $"End of Method", swBegin.ElapsedMilliseconds, ++step);

            this.AITrackMetric(client, execution, METHOD_NAME, metrics, isLog);

            return results;
        }

        public List<DatabaseBulkAssetTypeResult> RemoveAssetTypes(ApiExecution execution, AssetTypeDeletes deletes, int timeout = 7200, int maxRetryCount = 10)
        {
            bool isLog = true;
            var sw = Stopwatch.StartNew();
            TelemetryClient client = new TelemetryClient();
            const string METHOD_NAME = "RemoveAssetTypes";
            var metrics = new Dictionary<string, double>();

            var results = new List<DatabaseBulkAssetTypeResult>();
            var dt = DateTime.UtcNow;
            bool generalChecksCompleted = false;
            CurrentExecutionLocationModel currentLocation = null;

            SetApiExecutionProcessingStartTime(execution.ExecutionID);


            var executionItemDupes = deletes.Where(i => i.ExecutionItemUid.HasValue).GroupBy(i => i.ExecutionItemUid).Where(i => i.Count() > 1).Select(i => new { ExecutionItemUid = i.Key, Count = i.Count() }).ToList();
            if (executionItemDupes.Any())
            {
                execution.ErrorMessage = $"Duplicate execution item identifiers: {string.Join(", ", executionItemDupes.Select(i => i.ExecutionItemUid.ToString()))}. Identifiers must be unique within a batch.";
                results.AddRange(deletes.Select(i => new DatabaseBulkAssetTypeResult { ExecutionItemUid = i.ExecutionItemUid, uid = i.Uid, Message = execution.ErrorMessage, Success = false }));
            }
            else
            {
                var uidDupes = deletes.GroupBy(i => i.Uid).Where(i => i.Count() > 1).Select(i => new { Uid = i.Key, Count = i.Count() }).ToList();
                if (uidDupes.Any())
                {
                    execution.ErrorMessage = $"Duplicate Asset Type Uids: {string.Join(", ", uidDupes.Select(i => i.Uid.ToString()))}. Identifiers must be unique within a batch.";
                    results.AddRange(deletes.Select(i => new DatabaseBulkAssetTypeResult { ExecutionItemUid = i.ExecutionItemUid, uid = i.Uid, Message = execution.ErrorMessage, Success = false }));
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

                        for (int i = 1; i <= deletes.Count; i++)
                        {
                            if (i > currentLocation.HighestItemNumber)
                            {
                                var model = deletes[i - 1];

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

                        AddMeasurement(metrics, "Building data tables and initialization completed", sw.ElapsedMilliseconds, 1);
                        sw.Restart();
                    }
                    catch (Exception generalEx)
                    {
                        generalChecksCompleted = false;
                        var msg = generalEx.GetFullExceptionData(false);
                        execution.ErrorMessage = msg;
                        execution.Processed = 0;
                        execution.Error = deletes.Count();

                        results = new List<DatabaseBulkAssetTypeResult>();
                        results.AddRange(deletes.Select(i => new DatabaseBulkAssetTypeResult { ExecutionItemUid = i.ExecutionItemUid, Message = msg, Success = false }));

                        AddMeasurement(metrics, "Error occurred > Building data tables and initialization completed", sw.ElapsedMilliseconds, 1);
                        sw.Restart();
                    }

                    if (generalChecksCompleted)
                    {
                        bool runCompleted = false;
                        int retryCount = 0;

                        AddMeasurement(metrics, "Checks passed (Deletion started)", sw.ElapsedMilliseconds, 1);
                        sw.Restart();

                        while (!runCompleted && retryCount <= maxRetryCount)
                        {
                            int itemNumber = 1;
                            try
                            {

                                //Create list of asset types for deletion + their children
                                using (var trans = Connection.BeginTransaction())
                                {
                                    try
                                    {
                                        BuildDeletionTree(execution, timeout, itemNumber, trans);

                                        trans.Commit();

                                        AddMeasurement(metrics, "Building Deletion Tree", sw.ElapsedMilliseconds, 1);
                                        sw.Restart();
                                    }
                                    catch (Exception)
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

                                        AddMeasurement(metrics, "Error occurred > Building Deletion Tree", sw.ElapsedMilliseconds, 1);
                                        sw.Restart();

                                        throw;
                                    }
                                }

                                var assetTypes = Connection.Query<AssetTypeDeleteObject>(
                                    @"select D.uid, 
                                    T.Class,
                                    D.ObjectId, 
                                    D.Object,
                                    D.AssetTypeId,
                                    isnull(D.HierarchyLevel,0) as Level,
                                    D.ItemNumber 
                                    from api.ExecutionDeletedAssetType D inner join AssetType T on T.ID = D.AssetTypeId
                                    where D.executionid = @executionUid and D.success is null", new { executionUid = execution.ExecutionID }).ToList();

                                //Delete hierarchy by hierarchy and start from highest level (children)
                                var hierarchies = assetTypes.GroupBy(x => x.ItemNumber).ToList();
                                int success = 0;
                                int failed = 0;

                                foreach (var hierarchy in hierarchies)
                                {
                                    var typesToDelete = hierarchy.OrderByDescending(x => x.Level).ToList();
                                    bool hasError = false;

                                    foreach (var at in typesToDelete)
                                    {
                                        //If error occured stop deleting this hierarchy do not continue deleting parent asset types
                                        if (hasError)
                                            continue;

                                        int totalAssetCount = Connection.Query<int>("select count(*) from asset where assettypeid = @id", new { id = at.AssetTypeId }).FirstOrDefault();

                                        if (totalAssetCount > 0)
                                        {
                                            var transactionsCount = (totalAssetCount / SqlBulkAssetDeleteSize) + 1;

                                            AddMeasurement(metrics, $"Asset Type '{at.uid}' deletion started ({totalAssetCount} assets) by chunks of size {SqlBulkAssetDeleteSize}", sw.ElapsedMilliseconds, 1);
                                            sw.Restart();

                                            List<Guid> assetUids = null;
                                            if (at.Class == AssetTypeClass.Rule)
                                            {
                                                assetUids = Connection.Query<Guid>("select uid from asset where assettypeid = @id", new { id = at.AssetTypeId }).ToList();
                                            }

                                            for (int i = 0; i < transactionsCount; i++)
                                            {
                                                AddMeasurement(metrics, $"Asset Type '{at.uid}' deletion : {(i + 1)}/{transactionsCount}", sw.ElapsedMilliseconds, 1);
                                                sw.Restart();

                                                #region RemoveAssetsGraphDataByChunks

                                                using (var trans = Connection.BeginTransaction())
                                                {
                                                    try
                                                    {
                                                        RemoveAssetsGraphDataByChunk(execution, timeout, itemNumber, at, trans);
                                                        trans.Commit();

                                                        AddMeasurement(metrics, $"Asset Type '{at.uid}' deletion > Graph data {(i + 1)}/{transactionsCount} > Finished", sw.ElapsedMilliseconds, 1);
                                                        sw.Restart();
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

                                                        Connection.Query(@"update api.executiondeletedassettype
                                                    set Message = isnull(Message,'') + @msg
                                                    where executionid = @executionuid and uid = @assetTypeUid",
                                                            new
                                                            {
                                                                executionuid = execution.ExecutionID,
                                                                assetTypeUid = at.uid,
                                                                msg = $@"Error occurred while deleting assets graph data : ({ex.Message})"
                                                            });

                                                        hasError = true;
                                                        i = transactionsCount + 1;

                                                        AddMeasurement(metrics, $"Asset Type '{at.uid}' deletion > Graph data {(i + 1)}/{transactionsCount} > Error occurred", sw.ElapsedMilliseconds, 1);
                                                        sw.Restart();

                                                        throw;
                                                    }

                                                }
                                                #endregion

                                                #region RemoveAssetsDataByChunks
                                                using (var trans = Connection.BeginTransaction())
                                                {

                                                    try
                                                    {
                                                        RemoveAssetsDataByChunk(execution, timeout, itemNumber, at, trans);

                                                        trans.Commit();

                                                        AddMeasurement(metrics, $"Asset Type '{at.uid}' deletion > Asset data {(i + 1)}/{transactionsCount} > Finished", sw.ElapsedMilliseconds, 1);
                                                        sw.Restart();
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

                                                        Connection.Query(@"update api.executiondeletedassettype
                                                    set Message = isnull(Message,'') + @msg
                                                    where executionid = @executionuid and uid = @assetTypeUid",
                                                            new
                                                            {
                                                                executionuid = execution.ExecutionID,
                                                                assetTypeUid = at.uid,
                                                                msg = $@"Error occurred while deleting assets : ({ex.Message})"
                                                            });

                                                        hasError = true;
                                                        i = transactionsCount + 1;

                                                        AddMeasurement(metrics, $"Asset Type '{at.uid}' deletion > Graph data {(i + 1)}/{transactionsCount} > Error occurred", sw.ElapsedMilliseconds, 1);
                                                        sw.Restart();

                                                        throw;
                                                    }

                                                }
                                                #endregion
                                            }

                                            // Data Quality Scoring - send to engine to determine what scores need to be recalculated.
                                            if (at.Class == AssetTypeClass.Rule && assetUids != null)
                                            {
                                                CreateRulesRemovedExecution(execution.ExecutionID, assetUids);
                                            }
                                        }
                                        else
                                        {
                                            AddMeasurement(metrics, $"Asset Type '{at.uid}' deletion skipped asset data deletion", sw.ElapsedMilliseconds, 1);
                                            sw.Restart();
                                        }

                                        if (!hasError)
                                        {
                                            #region RemoveAssetTypeData


                                            AddMeasurement(metrics, $"Asset Type '{at.uid}' > Deleting Asset Type Data Started", sw.ElapsedMilliseconds, 1);
                                            sw.Restart();

                                            using (var trans = Connection.BeginTransaction())
                                            {
                                                try
                                                {
                                                    RemoveAssetTypeData(execution, timeout, itemNumber, at, trans);

                                                    trans.Commit();
                                                    AddMeasurement(metrics, $"Asset Type '{at.uid}' > Deleting Asset Type Data Finished", sw.ElapsedMilliseconds, 1);
                                                    sw.Restart();
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
                                                    hasError = true;
                                                    Connection.Query(@"update api.executiondeletedassettype
                                                    set Message = isnull(Message,'') + @msg
                                                    where executionid = @executionuid and uid = @assetTypeUid",
                                                        new
                                                        {
                                                            executionuid = execution.ExecutionID,
                                                            assetTypeUid = at.uid,
                                                            msg = $@"Error occurred while deleting asset type : ({ex.Message})"
                                                        }
                                                    );

                                                    AddMeasurement(metrics, $"Asset Type '{at.uid}' > Deleting Asset Type Data > Error Occurred", sw.ElapsedMilliseconds, 1);
                                                    sw.Restart();

                                                    throw;
                                                }
                                            }

                                            #endregion
                                        }

                                        AddMeasurement(metrics, $"Asset Type '{at.uid}' deletion finished", sw.ElapsedMilliseconds, 1);
                                        sw.Restart();
                                    }

                                    if (hasError)
                                        failed++;
                                    else
                                        success++;


                                }

                                Connection.Query(@"update api.execution
                                                    set Processed = @success,
                                                    Error = @failed
                                                    where executionid = @executionuid",
                                                        new
                                                        {
                                                            success,
                                                            failed,
                                                            executionUid = execution.ExecutionID
                                                        });

                                results = Connection.Query<DatabaseBulkAssetTypeResult>(@"select	*
                                    	                        from	api.ExecutionDeletedAssetType
                                    	                        where	ExecutionID = @executionUid 
                                    			                        and FromHierarchy = 0;", new { executionUid = execution.ExecutionID, itemNumber, resource = CurrentResourceID }, commandTimeout: timeout).ToList();


                                runCompleted = true;
                            }
                            catch (Exception ex)
                            {
                                retryCount++;

                                if (retryCount > maxRetryCount)
                                {
                                    LogLoopExecutionError(execution.ExecutionID, itemNumber, itemNumber, "api.ExecutionDeletedAssetType", ex.GetFullExceptionData(false), timeout);
                                }
                            }

                        }

                        Connection.Close();

                        // Queue successfully deleted asset types for reindexing
                        results.Where(r => r.Success).ToList().ForEach(r =>
                            {
                                Enqueue(Config.GetValue<string>("SearchIndexQueue"), new ReindexModel
                                {
                                    CompanyID = CurrentCompanyID,
                                    AssetTypeUid = r.uid
                                });
                            });
                    }
                }
            }

            this.AITrackMetric(client, execution, METHOD_NAME, metrics, isLog);

            return results;
        }

        private void RemoveAssetsGraphDataByChunk(ApiExecution execution, int timeout, int itemNumber, AssetTypeDeleteObject at, SqlTransaction trans)
        {
            Connection.Execute(@"
                                            drop table if exists #deleteAssets
                                            create table #deleteAssets (id bigint)
                                            create clustered index CIX_TempDeleteAssetsIds on #deleteAssets (id)

                                            insert into #deleteAssets
                                            select top (@deleteCount) id from asset where assettypeid = @assettypeid

                                                
                                    		-- Delete from graph tables.
                                    		delete E
                                    		from    graph.AssetEdge E
                                    				inner join graph.AssetNode N on E.$from_id = N.$node_id or E.$to_id = N.$node_id
                                    				where N.Id in (select id from #deleteAssets);

                                    		-- Delete rule results where the asset we are deleting is the owner of the results (i.e. a Rule).
                                    		drop table if exists #Uids
                                    		create table #Uids (Uid uniqueidentifier)
                                    		create clustered index CIX_TempUids on #Uids (Uid)

                                    		insert into #Uids
                                    			select	R.Uid
                                    			from	graph.AssetNode A,
                                    					dbo.AssetResultEdge E,
                                    					dbo.AssetResult R
                                    			where	MATCH(A-(E)->R)
                                    					and E.Class = 1
                                    					and A.AssetTypeID = @AssetTypeId
                                    					and A.Id in (select id from #deleteAssets);


                                    		delete	E
                                    		from    dbo.AssetResultEdge E
                                    				inner join graph.AssetNode N on E.$from_id = N.$node_id
                                    				where N.Id in (select id from #deleteAssets);

                                    		delete	T
                                    		from	dbo.AssetResult T
                                    				inner join #Uids S on S.Uid = T.Uid;

                                    		delete	A
                                    		from	graph.AssetNode A
                                    				where A.Id in (select id from #deleteAssets);

                                ", new { deleteCount = SqlBulkAssetDeleteSize, assetTypeUid = at.uid, at.AssetTypeId, at.Object, at.ObjectId, at.IntersectTypeId, executionUid = execution.ExecutionID, itemNumber, resource = CurrentResourceID }, transaction: trans, commandTimeout: timeout);
        }

        private void RemoveAssetsDataByChunk(ApiExecution execution, int timeout, int itemNumber, AssetTypeDeleteObject at, SqlTransaction trans)
        {
            Connection.Execute(@"
                drop table if exists #deleteAssets
                create table #deleteAssets (id bigint)
                create clustered index CIX_TempDeleteAssetsIds on #deleteAssets (id)

                insert into #deleteAssets
                select top (@deleteCount) id from asset where assettypeid = @assettypeid

                delete	T
                from	ResponsibilityTypeRelationOverrideItem T
                        inner join Asset A on A.ID = T.AssetID
                        inner join api.ExecutionDeletedAssetType S on S.AssetTypeID = A.AssetTypeID and A.ID in (select id from #deleteAssets);

                delete	T
                from	AssetCrossReference T
                        inner join Asset A on A.Uid = T.Uid
                        inner join api.ExecutionDeletedAssetType S on S.AssetTypeID = A.AssetTypeID and S.ExecutionID = @executionUid
                        where A.ID in (select id from #deleteAssets);

                delete	T
                from	CommentRelation T
                        inner join Asset O on O.ID = T.AssetID 
                        inner join api.ExecutionDeletedAssetType S on S.AssetTypeID = O.AssetTypeID and S.ExecutionID = @executionUid
                        where O.ID in (select id from #deleteAssets);
                delete	T
                from	CommentVote T
                        inner join Comment C on C.ID = T.CommentID
                        inner join Asset O on O.ID = C.AssetID 
                        inner join api.ExecutionDeletedAssetType S on S.AssetTypeID = O.AssetTypeID and S.ExecutionID = @executionUid
                        where O.ID in (select id from #deleteAssets);
                delete	T
                from	Comment T
                        inner join Asset O on O.ID = T.AssetID
                        inner join api.ExecutionDeletedAssetType S on S.AssetTypeID = O.AssetTypeID and S.ExecutionID = @executionUid
                        where O.ID in (select id from #deleteAssets);
                delete	T
                from	Favorite T
                        inner join Asset O on O.Object = T.Object and O.ObjectID = T.ObjectID 
                        inner join api.ExecutionDeletedAssetType S on S.AssetTypeID = O.AssetTypeID and S.ExecutionID = @executionUid
                        where O.ID in (select id from #deleteAssets);
                delete	T
                from	Follow T
                        inner join Asset O on O.Object = T.ObjectType and O.ObjectID = T.ObjectID 
                        inner join api.ExecutionDeletedAssetType S on S.AssetTypeID = O.AssetTypeID and S.ExecutionID = @executionUid
                        where O.ID in (select id from #deleteAssets);
                                                

                delete	T
                from	Nym T
                        inner join Asset O on O.Object = T.Object and O.ObjectID = T.ObjectID 
                        inner join api.ExecutionDeletedAssetType S on S.AssetTypeID = O.AssetTypeID and S.ExecutionID = @executionUid
                        where O.ID in (select id from #deleteAssets);

                delete	T
                from	reporting.Global_Audit T
                        inner join Asset A on A.Object = T.Object and A.ObjectID = T.ObjectID
                        where A.Id in (select id from #deleteAssets);

                delete	T
                from	AssetDisplayValue T
                        where T.AssetId in (select id from #deleteAssets);

                -- Delete where assets are on the subject side of relationship.
                delete	T
                from	[Intersect] T
                        inner join Asset A on A.Object = T.Subject and A.ObjectID = T.SubjectID
                        where A.Id in (select id from #deleteAssets);

                -- Delete where assets are on the object side of relationship.
                delete	T
                from	[Intersect] T
                        inner join Asset A on A.Object = T.Object and A.ObjectID = T.ObjectID
                        where A.Id in (select id from #deleteAssets);

                delete	T
                from	Field T
					    inner join Asset O on O.Object = T.ObjectType and O.ObjectID = T.ObjectID 
                        where O.Id in (select id from #deleteAssets);
                                    			
                delete	T
                from	Field T
                        inner join Issue I on T.ObjectType = 'Issue' and I.ID = T.ObjectID
					    inner join Asset O on O.Object = I.Object and O.ObjectID = I.ObjectID 
                        where O.Id in (select id from #deleteAssets);
                                    			
                delete	T
                from	Issue T
                        inner join Asset O on O.Object = T.Object and O.ObjectID = T.ObjectID 
                        where O.Id in (select id from #deleteAssets);

                delete	T
                from	[metrics].[ScoreItem] T
                        inner join metrics.ScoreItemLink L on L.ScoreItemUid = T.Uid
                        inner join metrics.Score S on S.Uid = L.ScoreUid
                        inner join Asset O on O.Uid = S.AssetUid
                        where O.Id in (select id from #deleteAssets);

                delete	T
                from	[metrics].[Score] T
                        inner join Asset O on O.Uid = T.AssetUid
                        where O.Id in (select id from #deleteAssets);

                delete	T
                from	Asset T
                        where T.Id in (select id from #deleteAssets);",
                        new { deleteCount = SqlBulkAssetDeleteSize, assetTypeUid = at.uid, at.AssetTypeId, at.Object, at.ObjectId, at.IntersectTypeId, executionUid = execution.ExecutionID, itemNumber, resource = CurrentResourceID }, transaction: trans, commandTimeout: timeout);
        }

        private void RemoveAssetTypeData(ApiExecution execution, int timeout, int itemNumber, AssetTypeDeleteObject at, SqlTransaction trans)
        {
            Connection.Execute(@"
                                            drop table if exists #w;
                                    		create table #w (ID int);
                                    		insert into #w
                                    			select	distinct 
                                    					wi.ID 
                                    			from	workflow.[Type] wt
                                    					inner join workflow.EventRegistration we on we.typeid = wt.id and we.changetype <> 3
                                    					inner join workflow.[Version] wv on wt.id = wv.typeId
                                    					inner join workflow.Item wi on 	wv.id = wi.VersionID
                                    					inner join api.ExecutionDeletedAssetType S on S.Object = we.Object and S.ObjectID = we.ObjectID and S.ExecutionID = @executionUid and S.AssetTypeId = @AssetTypeId;
                                    		insert into #w
                                    			select	wi.id 
                                    			from	workflow.Item wi
                                    					inner join Issue i on wi.object = 'Issue' and i.id = wi.objectid
                                    					inner join Asset A on A.Object = i.ObjectType and A.ObjectID = i.ObjectID
                                    					inner join api.ExecutionDeletedAssetType S on S.AssetTypeID = A.AssetTypeID and S.ExecutionID = @executionUid and S.AssetTypeId = @AssetTypeId;
                                    		delete  T
                                    		from	[workflow].[ItemStepTransition] T
                                    				inner join workflow.itemstep wis on (wis.ID = T.ToItemStepID or wis.ID = T.FromItemStepID)
                                    				inner join #w S on S.ID = wis.ItemID;
                                    		delete  workflow.itemstep 
                                    		where	ItemID in (Select ID from #w);
                                    		delete	T
                                    		from	[workflow].[ItemAssignment] T
                                    				inner join #w S on S.ID = T.ItemID;

                                    		delete  [workflow].[Item] 
                                    		where	ID in (Select ID from #w);
                                    		truncate table #w;
                                    		insert into #w
                                    			select	distinct 
                                    					wt.ID 
                                    			from	workflow.[Type] wt
                                    					inner join workflow.EventRegistration we on we.typeid = wt.id and we.changetype <> 3
                                    					inner join api.ExecutionDeletedAssetType S on S.Object = we.Object and S.ObjectID = we.ObjectID and S.ExecutionID = @executionUid and S.AssetTypeId = @AssetTypeId;
                                    		delete  T
                                    		from	[workflow].[VersionStepTransition] T
                                    				inner join workflow.Versionstep wis on (wis.ID = T.ToVersionStepID or wis.ID = T.FromVersionStepID)
                                    				inner join [workflow].[Version] v on v.ID = wis.VersionID
                                    				inner join [workflow].[Type] wt on wt.ID = v.TypeID
                                    				inner join #w S on S.ID = wt.ID;
                                    		delete  wis
                                    		from	workflow.Versionstep wis
                                    				inner join [workflow].[Version] v on v.ID = wis.VersionID
                                    				inner join [workflow].[Type] wt on wt.ID = v.TypeID
                                    				inner join #w S on S.ID = wt.ID;

                                     		update	wt
                                    		set		PublishedVersionID = null
                                    		from	workflow.type wt
                                    				inner join #w S on S.ID = wt.ID;
                                    		delete  v
                                    		from	[workflow].[Version] v
                                    				inner join [workflow].[Type] wt on wt.ID = v.TypeID
                                    				inner join #w S on S.ID = wt.ID;
                                    		delete  wt
                                    		from	[workflow].[Type] wt
                                    				inner join #w S on S.ID = wt.ID;

                                    		delete	T
                                    		from	ResponsibilityRuleResultAsset T
                                    				inner join ResponsibilityTypeRelationRule R on R.ID = T.RuleID
                                    				inner join api.ExecutionDeletedAssetType S on S.Object = R.Object and S.ObjectID = R.ObjectID and S.ExecutionID = @executionUid and S.AssetTypeId = @AssetTypeId;
                                    		delete	T
                                    		from	ResponsibilityRuleResultSecurityAsset T
                                    				inner join ResponsibilityTypeRelationRule R on R.ID = T.RuleID
                                    				inner join api.ExecutionDeletedAssetType S on S.Object = R.Object and S.ObjectID = R.ObjectID and S.ExecutionID = @executionUid and S.AssetTypeId = @AssetTypeId;
                                    		delete	T
                                    		from	ResponsibilityTypeRelationRule T
                                    				inner join api.ExecutionDeletedAssetType S on S.Object = T.Object and S.ObjectID = T.ObjectID and S.ExecutionID = @executionUid and S.AssetTypeId = @AssetTypeId;
                                    		delete	T
                                    		from	ResponsibilityTypeRelation T
                                    				inner join api.ExecutionDeletedAssetType S on S.Object = T.ObjectType and S.ObjectID = T.ObjectID and S.ExecutionID = @executionUid and S.AssetTypeId = @AssetTypeId;

                                            delete	T
                                    		from	api.EntityFieldTypeMultiSelectField T
                                    				inner join api.EntityFieldType F on F.ID = T.EntityFieldTypeID
                                    				inner join api.Entity E on E.ID = F.EntityID
                                    				inner join api.ExecutionDeletedAssetType S on S.AssetTypeID = E.AssetTypeID and S.AssetTypeID = @AssetTypeID and S.ExecutionID = @executionUid;
                                    		delete	T
                                    		from	api.EntityFieldType T
                                    				inner join api.Entity E on E.ID = T.EntityID
                                    				inner join api.ExecutionDeletedAssetType S on S.AssetTypeID = E.AssetTypeID and S.AssetTypeID = @AssetTypeID and S.ExecutionID = @executionUid;
                                    		delete	T
                                    		from	api.EntityUri T
                                    				inner join api.Entity E on E.ID = T.EntityID
                                    				inner join api.ExecutionDeletedAssetType S on S.AssetTypeID = E.AssetTypeID and S.AssetTypeID = @AssetTypeID and S.ExecutionID = @executionUid;
                                    		delete	T
                                    		from	api.Entity T
                                    				inner join api.ExecutionDeletedAssetType S on S.AssetTypeID = T.AssetTypeID and S.AssetTypeID = @AssetTypeID and S.ExecutionID = @executionUid;
                                  			
                                    		delete	T
                                    		from	[Load] T
                                    				inner join api.ExecutionDeletedAssetType S on S.Object = T.Object and S.ObjectID = T.ObjectID and S.ExecutionID = @executionUid and S.AssetTypeId = @AssetTypeId;

                                       		delete	T
                                    		from	SiteNavPermission T
                                    				inner join SiteNav O on O.ID = T.SiteNavID
                                    				inner join api.ExecutionDeletedAssetType S on S.Object = O.Object and S.ObjectID = O.ObjectID and S.ExecutionID = @executionUid and S.AssetTypeId = @AssetTypeId;
                                    		delete	O
                                    		from	SiteNav O
                                    				inner join api.ExecutionDeletedAssetType S on S.Object = O.Object and S.ObjectID = O.ObjectID and S.ExecutionID = @executionUid and S.AssetTypeId = @AssetTypeId;

                                            delete	T
                                    		from	NymRelation T
                                    				inner join api.ExecutionDeletedAssetType S on S.Object = T.Object and S.ObjectID = T.ObjectID and S.ExecutionID = @executionUid and S.AssetTypeId = @AssetTypeId;

                                    		delete  T
                                    		from    AssetTypeExportTemplateStyle T
                                    				inner join AssetTypeExportTemplate E on E.ID = T.AssetTypeExportTemplateID
                                    				inner join api.ExecutionDeletedAssetType S on S.AssetTypeID = E.AssetTypeID and S.ExecutionID = @executionUid and S.AssetTypeId = @AssetTypeId;

                                    		delete  E
                                    		from    AssetTypeExportTemplate E 
                                    				inner join api.ExecutionDeletedAssetType S on S.AssetTypeID = E.AssetTypeID and S.ExecutionID = @executionUid and S.AssetTypeId = @AssetTypeId;

                                    		delete  E
                                    		from    AssetTypeLevel E 
                                    				inner join api.ExecutionDeletedAssetType S on S.AssetTypeID = E.AssetTypeID and S.ExecutionID = @executionUid and S.AssetTypeId = @AssetTypeId;

                                    		delete  E
                                    		from    AssetTypeStyle E 
                                    				inner join api.ExecutionDeletedAssetType S on S.AssetTypeID = E.ID and S.ExecutionID = @executionUid and S.AssetTypeId = @AssetTypeId;
                                    			
                                            delete	T
                                    		from	[metrics].[ScoreItem] T
                                    				inner join [metrics].[AssetVersion] MV on MV.Uid = T.AssetVersionUid
                                    				inner join [metrics].[Asset] MA on MA.Uid = MV.AssetUid
                                    				inner join [metrics].Allocation A on A.Uid = MA.AllocationUid
                                    				where A.AssetTypeUid = @assetTypeUid;

                                            delete	T
                                    		from	[metrics].[Score] T
                                    				inner join Asset MA on MA.Uid = T.AssetUid and MA.AssetTypeID = @assetTypeId;

                                            delete  [metrics].[RollupPathSegment] 
                                            where   AssetTypeID = @assetTypeId;

                                            delete  [metrics].[AssetVersionRollupPathFilter] 
                                            where   AssetTypeID = @assetTypeId;

                                    		delete	[metrics].Allocation where AssetTypeUid = @assetTypeUid;

                                    		delete	T
                                    		from	[IntersectType] T
                                                    where T.Subject = @Object and T.SubjectID = @ObjectId;

                                    		delete	T
                                    		from	[IntersectType] T
                                                    where T.Object = @Object and T.ObjectID = @ObjectId;

                                    		-- Delete parent/child relationships.
                                    		delete	T
                                    		from	[Intersect] T 
                                    				where T.IntersectTypeID = @IntersectTypeId; 

                                    		-- Delete counter field values.
                                            delete T 
                                            from dbo.FieldCounterValue T
                                            inner join FieldType FT ON FT.Object = @Object and FT.ObjectID = @ObjectId and [Type] = 'Counter'
                                            where FT.Id = T.FieldTypeId

                                            delete	T
                                    		from	FieldType T
                                    				where T.Object = @Object and T.ObjectID = @ObjectId;
                                               
                                    		delete	T
                                    		from	IssueTypeRelation T
                                    				where T.AssetTypeID = @AssetTypeId;

                                            delete	T
                                    		from	Fusion T
                                    				inner join FusionType A on A.ID = T.FusionTypeID
                                    				where @Object = 'FusionType' and @ObjectId = A.ID;

                                    		delete	T
                                    		from	FusionAttribute T
                                    				inner join FusionType A on A.ID = T.FusionAttributeTypeID
                                    				where @Object = 'FusionAttributeType' and @ObjectId = A.ID;
          
                                    		delete	T
                                    		from	[Rule] T
                                    				inner join RuleType A on A.ID = T.RuleTypeID
                                    				where @Object = 'RuleType' and @ObjectId = A.ID;


                                            delete T
                                    		from	AssetType T
                                    				where @Object = 'ArtifactType' and T.ID = @AssetTypeId;
                                    			                                    			
                                            delete	A
                                    		from	FusionType A
                                    				where @Object = 'FusionType' and @ObjectId = A.ID;

                                            delete	A
                                    		from	FusionAttributeType A
                                    				where @Object = 'FusionAttributeType' and @ObjectId = A.ID;
                                    			
                                            delete T
                                    		from	AssetType T
                                    				where @Object = 'PolicyType' and @AssetTypeId = T.ID;

                                            delete T
                                    		from	AssetType T
                                    				where @Object = 'ReferenceItemType' and @AssetTypeId = T.ID;

                                    		delete	A
                                    		from	RuleType A
                                    				where @Object = 'RuleType' and @ObjectId = A.ID;

                                            delete T
                                    		from	AssetType T
                                    				where @Object = 'TaxonomyType' and @AssetTypeId = T.ID;

                                            update	OrganizationType
                                    		set		State = 3,
                                    				UpdatedBy = @resource,
                                    				UpdatedOn = getutcdate()
                                    		from	OrganizationType T
                                            where @Object = 'OrganizationType' and @ObjectId = T.ID;

                                            delete	T
                                    		from	AssetType T
                                    				where @AssetTypeId = T.ID;

                                    		update	api.ExecutionDeletedAssetType
                                    		set		[Message] = 'Removed Asset type from environment, along with all related assets, scores, and relationships.',
                                    				Success = 1
                                    		where	ExecutionID = @executionUid 
                                    				and Uid = @assetTypeUid
                                ", new { assetTypeUid = at.uid, at.AssetTypeId, at.Object, at.ObjectId, at.IntersectTypeId, executionUid = execution.ExecutionID, itemNumber, resource = CurrentResourceID }, transaction: trans, commandTimeout: timeout);
        }

        private void BuildDeletionTree(ApiExecution execution, int timeout, int itemNumber, SqlTransaction trans)
        {
            Connection.Execute(@";with h as (
			select	D.ExecutionID,
					D.ItemNumber,
					D.AssetTypeID,
					D.[Uid],
					A.Object,
					A.ObjectID, 
					D.IntersectTypeID,
					0 as [Level]
			from	api.ExecutionDeletedAssetType D
					inner join AssetType A on D.ExecutionID = @executionUid and A.ID = D.AssetTypeID
			where	D.AssetTypeID is not null and D.Success is null
			union all
			select	P.ExecutionID,
					P.ItemNumber,
					C.ID as AssetTypeID,
					C.[Uid],
					C.Object,
					C.ObjectID, 
					I.ID as IntersectTypeID,
					P.[Level] + 1 as [Level]
			from	IntersectType I 
					inner join h as P on P.ExecutionID = @executionUid and P.Object = I.Subject and P.ObjectID = I.SubjectID
					inner join AssetType C on C.Object = I.Object and C.ObjectID = I.ObjectID and C.ID <> P.AssetTypeID
					inner join [Predicate] PR on PR.ID = I.PredicateID and PR.[Type] in (3,4)
			where   P.[Level] <= 15
		)
		insert into api.ExecutionDeletedAssetType ([ExecutionID],[ItemNumber],[Uid],[AssetTypeID],Object,ObjectID,[IntersectTypeID],[FromHierarchy],[HierarchyLevel])
			select  distinct 
					ExecutionID, 
					ItemNumber, 
					[Uid], 
					AssetTypeID, 
					Object,
					ObjectID,
					IntersectTypeID, 
					1,
                    [Level]
			from    h 
			where   IntersectTypeID is not null 
					and [Level] > 0 
					and Uid not in (select Uid from api.ExecutionDeletedAsset where ExecutionID = @executionUid and Success is null);
			
		INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID],[AssetID])
			select	'ObjectIndex', 
					'D',
					A.Object, 
					A.ObjectID, 
					A.ID
			from	Asset A
					inner join api.ExecutionDeletedAssetType S on S.AssetTypeID = A.AssetTypeID and S.ExecutionID = @executionUid;
	", new
            {
                executionUid = execution.ExecutionID,
                itemNumber,
                resource = CurrentResourceID
            }, transaction: trans, commandTimeout: timeout);
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

                    CreateRollupPathChangedExecution(null, null, execution.ExecutionID);
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

                    var impactedMeasureVersions = new List<Guid>();
                    var intersectTypeIds = Query<int>("select ID from IntersectType where Uid in @Uids", new { Uids = import.Select(imp => imp.Uid) }).ToList();
                    intersectTypeIds.ForEach(it =>
                    {
                        var impacted = GetImpactedMeasureVersionsBy(MetricGovernanceCheckType.Relation, it);
                        impactedMeasureVersions.AddRange(impacted);
                    });

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
                                        new { execution.ExecutionID }).ToList();

                    if (impactedMeasureVersions.Count > 0)
                    {
                        CreateCheckDependencyRemovedNotificationExecution(impactedMeasureVersions);
                    }
                }
                finally
                {
                    if (Database.Connection.State == ConnectionState.Open)
                        Connection.Close();
                }
            }
            return results;
        }

        private void AddMeasurement(Dictionary<string, double> metrics, string key, double value, int stepNumber)
        {
            metrics[$"{stepNumber}-{key}"] = value;
        }

        private void AITrackMetric(TelemetryClient client, ApiExecution execution, string methodName, Dictionary<string, double> metrics, bool isLog)
        {
            if (!isLog) return;

            var propsToSend = new Dictionary<string, string> {
            { "MethodName", methodName },
            { "CompanyID", this.CurrentCompanyID.ToString() },
            { "ExecutionID", execution.ExecutionID.ToString() }
        };

            client.TrackEvent($"API v2 Execution ID[{execution.ExecutionID}]", propsToSend, metrics);
        }

        public List<DatabaseBulkAssetResult> ImportAssets(ApiExecution execution, AssetType at, IEnumerable<IAssetUpsert> import, bool isInsert, int timeout = 3600, bool sendWorkflowEvents = true, bool lookupFieldsPassedByValue = false, int mergeBlockSize = 500, bool sendGraphEvents = true, bool useTempTableForFields = false)
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
            bool enableJsonAttributes = false;
            bool hasCounterField = false;

            try
            {
                enableJsonAttributes = Community.GetCompanySettingByKey<bool>("EnableJsonAttribute");
            }
            catch { }

            FieldValidationFieldProperties fieldLoadProperties = new FieldValidationFieldProperties(); // properties of fields in the data load.  Returned from validate fields so we are efficient and dont keep going through the fields.

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
            if (!hasDuplicateUids)
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
                    fieldTypes = Query<FieldType>("select * from FieldType where Object = @Object and ObjectID = @ObjectID", new { @Object = new DbString { Value = at.Object, IsFixedLength = true, Length = 50, IsAnsi = true }, at.ObjectID }).ToList();
                    jsonFieldTypes = fieldTypes.Where(f => f.Type == DataType.JSON.ToString()).ToList();
                    requiredFieldTypeNames = fieldTypes.Where(f => f.IsRequired && string.IsNullOrEmpty(f.DefaultValue) && f.Type != DataType.Counter.ToString()).Select(f => f.Name).ToList();
                    hasLookupFieldTypes = fieldTypes.Any(f => f.Type == DataType.Lookup.ToString());
                    hasRelationshipFieldTypes = fieldTypes.Any(f => f.Type == DataType.Relationship.ToString());
                    hasCounterField = fieldTypes.Any(x => x.Type == DataType.Counter.ToString());
                    AddMeasurement(metrics, "Get field types", sw.ElapsedMilliseconds, ++step);
                    sw.Restart();

                    #region Generate data sets

                    if (predicateType.HasValue)
                    {
                        it = Database.Connection.QueryFirstOrDefault<IntersectType>("select i.[Subject],i.[SubjectID],i.[uid],i.ID from [dbo].[intersecttype] i inner join [predicate] p on (i.predicateid = p.id) where i.[Object] = @obj and i.[ObjectID] = @objID and p.[Type] = @predicate", new { obj = new DbString { Value = at.Object, IsFixedLength = true, Length = 50, IsAnsi = true }, objID = at.ObjectID, predicate = predicateType });
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
                            var fieldRows = ValidateFields(at.Object, at.ObjectID, isInsert, fieldTypes, requiredFieldTypeNames, model.Fields, execution.ExecutionID, i, fieldTable, out success, out errorMessage, validationFieldProperties: fieldLoadProperties, jsonElementsEnabled: enableJsonAttributes);

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
                                if (model.Uid != Guid.Empty)
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
                            // if needed create temp tables for data
                            useTempTableForFields = false;
                            CreateWorkareaTempTables(useTempTableForFields, transaction);

                            AddMeasurement(metrics, "Create work area temp tables", sw.ElapsedMilliseconds, ++step);

                            sw.Restart();

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

                            using (SqlBulkCopy bulkCopy = new SqlBulkCopy((SqlConnection)Database.Connection, (useTempTableForFields ? SqlBulkCopyOptions.TableLock : SqlBulkCopyOptions.Default), transaction))
                            {
                                // fields
                                bulkCopy.BatchSize = SqlBulkBatchSize;
                                bulkCopy.DestinationTableName = ApiExecutionFieldTable;
                                bulkCopy.BulkCopyTimeout = SqlBulkBatchTimeout;

                                bulkCopy.ColumnMappings.Add("ExecutionID", "ExecutionID");
                                bulkCopy.ColumnMappings.Add("ItemNumber", "ItemNumber");
                                bulkCopy.ColumnMappings.Add("FieldName", "FieldName");
                                bulkCopy.ColumnMappings.Add("FieldValue", "FieldValue");
                                bulkCopy.ColumnMappings.Add("FieldTypeID", "FieldTypeID");

                                bulkCopy.WriteToServer(fieldTable);
                            }


                            transaction.Commit();

                            AddMeasurement(metrics, "BulkCopy to api.Execution table", sw.ElapsedMilliseconds, ++step);
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


                    if (fieldLoadProperties.ContainsColorField)
                    {
                        ResolveColorValues(execution.ExecutionID, timeout);
                        AddMeasurement(metrics, "ResolveColorValues", sw.ElapsedMilliseconds, ++step);
                        sw.Restart();
                    }

                    if (hasLookupFieldTypes)
                    {
                        if (lookupFieldsPassedByValue)
                        {
                            CopyFieldLookupValuesAsIs(execution.ExecutionID, timeout, ApiExecutionFieldTable);
                            AddMeasurement(metrics, "CopyFieldLookupValuesAsIs", sw.ElapsedMilliseconds, ++step);
                            sw.Restart();
                        }
                        else
                        {
                            ResolveFieldLookupValues(execution.ExecutionID, ApiExecutionFieldTable, timeout);
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
                        LogFieldLookupErrors(execution.ExecutionID, at.Object, at.ObjectID, "Asset", lookupFieldsPassedByValue, timeout);
                        AddMeasurement(metrics, "LogFieldLookupErrors", sw.ElapsedMilliseconds, ++step);
                        sw.Restart();
                    }

                    if (hasRelationshipFieldTypes)
                    {
                        LogRelationshipErrors(execution.ExecutionID, at.Object, at.ObjectID, "Asset", timeout, lookupFieldsPassedByValue);
                        AddMeasurement(metrics, "LogRelationshipErrors", sw.ElapsedMilliseconds, ++step);
                        sw.Restart();
                    }

                    if (hasCounterField)
                    {
                        LogCounterFieldErrors(execution.ExecutionID, timeout);
                        AddMeasurement(metrics, "LogCounterFieldErrors", sw.ElapsedMilliseconds, ++step);
                        sw.Restart();
                    }

                    ValidateAssetAndParent(execution.ExecutionID, at.ID, timeout);
                    AddMeasurement(metrics, "ValidateAssetAndParent", sw.ElapsedMilliseconds, ++step);
                    sw.Restart();

                    // If you cannot find parent based on Uids provided.
                    // special case is intratype hierarchy if guid.empty we need to allow this so we later know which items to remove the relationships from
                    LogParentErrors(execution.ExecutionID, timeout, predicateType == PredicateType.IntraTypeHierarchy);
                    AddMeasurement(metrics, "LogParentErrors", sw.ElapsedMilliseconds, ++step);
                    sw.Restart();

                    if (!isInsert)
                    {
                        LogAssetErrors(execution.ExecutionID, timeout);             // If you cannot find asset based on Uids provided.
                        LoadMissingKeyFields(execution.ExecutionID, at, timeout);   // Get missing key fields if this is an update.
                        LogNullIsRequiredFields(execution.ExecutionID, timeout);    // Get IsRequired Field having Null value if this is an update.

                        AddMeasurement(metrics, "LogAssetErrors / LoadMissingKeyFields/ LogNullIsRequiredFields", sw.ElapsedMilliseconds, ++step);
                        sw.Restart();
                    }

                    //Policy/Model Check maximum hierarchy maximum level allowed 

                    if (at.Class == AssetTypeClass.Policy || at.Class == AssetTypeClass.Model)
                    {
                        LogPolicyHierMaxLimitErrors(execution.ExecutionID, isInsert, intersectTypeID, at.HierarchyMaximumDepth, timeout);
                    }


                    AddMeasurement(metrics, "Log Errors", sw.ElapsedMilliseconds, ++step);
                    sw.Restart();

                    #region Generate proposed key hash and compare against existing data.


                    if (at.Object == "FusionAttributeType")
                    {
                        LogErrorsWhereChildFusionConfigDifferentFromParent(execution.ExecutionID);
                        LogInvalidFusionIDFields(execution.ExecutionID);
                    }

                    CalculateProposedKeyHashes(at, execution.ExecutionID, timeout, intersectTypeID, fieldTable: ApiExecutionFieldTable);
                    AddMeasurement(metrics, "CalculateProposedKeyHashes", sw.ElapsedMilliseconds, ++step);
                    sw.Restart();

                    #endregion

                    #region Invalidate repetitious items in load

                    // dont be a tool and look for duplicates in a load of 1 item
                    if (execution.Total > 1)
                    {

                        Connection.Execute($@"
update	T
set		T.Success = 0,
	T.[Message] = coalesce(T.[Message] + '; ', '') + 'This asset is specified more than once based on the key fields defined on the asset type. Each asset must be unique within a given request.'
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
                    LogAssetPermissionErrors(execution.ExecutionID, at, isInsert ? Permission.AddAsset : Permission.EditAsset, "ExecutionAsset");
                    LogAssetPermissionErrors(execution.ExecutionID, at, isInsert ? Permission.AddAsset : Permission.EditAsset, isInsert, "ExecutionAsset");
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
                inner join {ApiExecutionFieldTable} F on F.ExecutionID = A.ExecutionID and F.ItemNumber = A.ItemNumber and F.FieldName = 'FusionID'
                inner join {ApiExecutionFieldTable} N on N.ExecutionID = A.ExecutionID and N.ItemNumber = A.ItemNumber and N.FieldName = 'Name'
                left join {ApiExecutionFieldTable} FS on FS.ExecutionID = A.ExecutionID and FS.ItemNumber = A.ItemNumber and FS.FieldName = 'SourceID'
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
        inner join {ApiExecutionFieldTable} N on N.ExecutionID = S.ExecutionID and N.ItemNumber = S.ItemNumber and N.FieldName = 'Name';

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
                CR.LookupValue as Color,
                A.Uid
        from    api.ExecutionAsset A
                left join {ApiExecutionFieldTable} CR on CR.ExecutionID = A.ExecutionID and CR.ItemNumber = A.ItemNumber and CR.FieldName = 'Color' 
        where   A.ExecutionID = @ExecutionID
                and A.Success is null
                and A.ItemNumber between @beginItemNumber and @endItemNumber
        ) S
on      1 = 0
when    not matched then
insert  (Uid,AssetTypeID,State,[Object], CreatedBy, CreatedOn, UpdatedBy, UpdatedOn, Color)
values  (isnull(S.Uid,newid()),@AssetTypeID,1,@Object, @R, @D, @R, @D, S.Color)
output  inserted.ObjectID, S.ItemNumber, $action into #ObjectMergeTableResult;



update  T
set     T.Object = @Object,
        T.ObjectID = S.ID,
        T.IsNew = 1
from    api.ExecutionAsset T
        inner join #ObjectMergeTableResult S on T.Executionid = @ExecutionID and S.ItemNumber = T.ItemNumber;

{updateAssetInfoOnExecutionRecordsSql}

{insertGraphAssetNode}",
                                                    new { beginItemNumber, endItemNumber, execution.ExecutionID, at.ObjectID, AssetTypeID = at.ID, R = CurrentResourceID, D = DateTime.UtcNow, @object = new DbString { Value = @object, Length = 50, IsAnsi = true } }, transaction: trans, commandTimeout: timeout);
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
        left join {ApiExecutionFieldTable} CR on CR.ExecutionID = S.ExecutionID and CR.ItemNumber = S.ItemNumber and CR.FieldName = 'Color' 


update	api.ExecutionAsset
set		IsNew = 0
where	{executionAssetWhereSql};",
                                            new { execution.ExecutionID, R = CurrentResourceID, D = DateTime.UtcNow, @object = new DbString { Value = @object, Length = 50, IsAnsi = true }, beginItemNumber, endItemNumber }, transaction: trans, commandTimeout: timeout);
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
                inner join {ApiExecutionFieldTable} T on T.ExecutionID = A.ExecutionID and T.ItemNumber = A.ItemNumber and T.FieldName = 'Threshold'
                left join {ApiExecutionFieldTable} CR on CR.ExecutionID = A.ExecutionID and CR.ItemNumber = A.ItemNumber and CR.FieldName = 'Color' 
        where   A.ExecutionID = @ExecutionID
                and A.Success is null
                and A.ItemNumber between @beginItemNumber and @endItemNumber
        ) S
on      (1 = 0)
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
                                                new { beginItemNumber, endItemNumber, execution.ExecutionID, at.ObjectID, AssetTypeID = at.ID, R = CurrentResourceID, D = DateTime.UtcNow },
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
        left join {ApiExecutionFieldTable} FD on FD.ExecutionID = S.ExecutionID and FD.ItemNumber = S.ItemNumber and FD.FieldName = 'Threshold'
        left join {ApiExecutionFieldTable} CR on CR.ExecutionID = S.ExecutionID and CR.ItemNumber = S.ItemNumber and CR.FieldName = 'Color' 

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
                                                                    A.Uid,
                                                                    C.FieldValue as [Code],
                                                                    CR.LookupValue as [Color],
                                                                    I.FieldValue as [Icon]
                                                            from    api.ExecutionAsset A
                                                                    inner join {ApiExecutionFieldTable} C on C.ExecutionID = A.ExecutionID and C.ItemNumber = A.ItemNumber and C.FieldName = 'Code' 
                                                                    left join {ApiExecutionFieldTable} CR on CR.ExecutionID = A.ExecutionID and CR.ItemNumber = A.ItemNumber and CR.FieldName = 'Color' 
                                                                    left join {ApiExecutionFieldTable} I on I.ExecutionID = A.ExecutionID and I.ItemNumber = A.ItemNumber and I.FieldName = 'Icon' 
                                                            where   A.ExecutionID = @ExecutionID
                                                                    and A.Success is null
                                                                    and A.ItemNumber between @beginItemNumber and @endItemNumber
                                                            ) S
                                                    on      (1 = 0)
                                                    when    not matched then
                                                    insert  (Uid, AssetTypeID,State,[Object], [Code], [Color], [Icon], CreatedBy, CreatedOn, UpdatedBy, UpdatedOn)
                                                    values  (isnull(S.Uid,newid()), @AssetTypeID,1,'ReferenceItem', S.[Code], S.[Color], S.[Icon], @R, @D, @R, @D)
                                                    output  inserted.ObjectID, S.ItemNumber, $action into #ObjectMergeTableResult;

                                                    update  T
                                                    set     T.Object = 'ReferenceItem',
                                                            T.ObjectID = S.ID,
                                                            T.IsNew = 1
                                                    from    api.ExecutionAsset T
                                                            inner join #ObjectMergeTableResult S on T.Executionid = @ExecutionID and S.ItemNumber = T.ItemNumber;

                                                    {updateAssetInfoOnExecutionRecordsSql}",
                                                new { beginItemNumber, endItemNumber, execution.ExecutionID, R = CurrentResourceID, D = DateTime.UtcNow, at.ObjectID, AssetTypeID = at.ID }, transaction: trans, commandTimeout: timeout);
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
                                                            inner join {ApiExecutionFieldTable} C on C.ExecutionID = S.ExecutionID and C.ItemNumber = S.ItemNumber and C.FieldName = 'Code'
                                                            left join {ApiExecutionFieldTable} CR on CR.ExecutionID = S.ExecutionID and CR.ItemNumber = S.ItemNumber and CR.FieldName = 'Color' 
                                                            left join {ApiExecutionFieldTable} I on I.ExecutionID = S.ExecutionID and I.ItemNumber = S.ItemNumber and I.FieldName = 'Icon';

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
                                    var transationFieldUpdates = MergeFields(execution.ExecutionID, trans, "api.ExecutionAsset", "A.Object", "A.ObjectID", beginItemNumber, endItemNumber, sendWorkflowEvents, timeout, isInsert,hasLookupFieldTypes);
                                    AddMeasurement(metrics, $"MergeFields >> {currentLoop}", sw.ElapsedMilliseconds, ++step);
                                    sw.Restart();

                                    if (hasCounterField)
                                    {
                                        UpdateCounterFields(at.ID, execution.ExecutionID, trans, beginItemNumber, endItemNumber, sendWorkflowEvents, timeout);
                                        AddMeasurement(metrics, $"UpdateCounteFields >> {currentLoop}", sw.ElapsedMilliseconds, ++step);
                                        sw.Restart();
                                    }

                                    if (hasRelationshipFieldTypes)
                                    {
                                        ImportRelationships(execution.ExecutionID, trans, "api.ExecutionAsset", "A.Object", "A.ObjectID", beginItemNumber, endItemNumber, timeout, lookupFieldsPassedByValue);
                                        AddMeasurement(metrics, $"ImportRelationships >> {currentLoop}", sw.ElapsedMilliseconds, ++step);
                                    }

                                    // only populate json properties IF there are 1 json fields on the asset type, AND values have been specified for JSON fields IE if they didnt provide any optional json fields disregard.
                                    // Only save all properties to the database if we json attributes enabled
                                    if (enableJsonAttributes && jsonFieldTypes.Count > 0 && fieldLoadProperties.JsonFieldCount > 0)
                                    {
                                        sw.Restart();
                                        MergeJsonFieldProperties(execution.ExecutionID, trans, jsonFieldTypes, "api.ExecutionAsset", "A.Object", "A.ObjectID", beginItemNumber, endItemNumber, timeout, metrics, step, isInsert);
                                        AddMeasurement(metrics, $"MergeJsonFieldProperties >> {currentLoop}", sw.ElapsedMilliseconds, ++step);
                                    }

                                    // Must execute BEFORE the Success flag is updated below.
                                    sw.Restart();
                                    MergeAssetDisplayValues(execution.ExecutionID, trans, beginItemNumber, endItemNumber, timeout, isInsert);
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
                                    AddMeasurement(metrics, "Commit Loop of data", sw.ElapsedMilliseconds, ++step);
                                    sw.Restart();

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
                                        AddMeasurement(metrics, "LogLoopExecutionError", sw.ElapsedMilliseconds, ++step);
                                        sw.Restart();
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

                        AddMeasurement(metrics, "End of batch loop", sw.ElapsedMilliseconds, ++step);
                        sw.Restart();
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

                    // Send score recalculation notifications.
                    if (Any<MetricAllocation>(i => i.AssetTypeUid == at.uid && i.ScoreType == ScoreType.Governance && !i.IsExternallyCalculated))
                    {
                        sw.Restart();
                        CreateImportAssetsExecution(execution.ExecutionID, at.uid);
                        AddMeasurement(metrics, $"SendScoreEventWithPayload", sw.ElapsedMilliseconds, ++step);
                    }
                }
            }

            AddMeasurement(metrics, $"End of Method", swBegin.ElapsedMilliseconds, ++step);

            this.AITrackMetric(client, execution, METHOD_NAME, metrics, isLog);

            return results;
        }

        private void CreateWorkareaTempTables(bool useTempTableForFields, SqlTransaction trans)
        {
            if (useTempTableForFields)
            {
                ApiExecutionFieldTable = "#ExecutionField";
                //create a ExecutionFields temp table version
                Connection.Execute($@"
                drop table if exists #ExecutionField;
        
                create table #ExecutionField (
                        [ExecutionID] [uniqueidentifier] NOT NULL,
                        [ItemNumber] [int] NOT NULL,
	                    [FieldName] [nvarchar](250) NOT NULL,
	                    [FieldValue] [nvarchar](max) NULL,
	                    [FieldTypeID] [int] NULL,
	                    [LookupValue] [nvarchar](max) NULL,
	                    [Ignore] [bit] NULL,
                );

                CREATE NONCLUSTERED INDEX IX_TempExecutionField ON #ExecutionField ( ExecutionID ASC, ItemNumber ASC, FieldName ASC );
            ", transaction: trans);
            }
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

            import.ForEach(rel =>
            {
                if (!string.IsNullOrEmpty(rel.Owner))
                {
                    rel.Owner = rel.Owner.Trim();
                }
            });

            SetApiExecutionProcessingStartTime(execution.ExecutionID);

            //check if trigger workflows is set to true and there are actually no workflows
            sendWorkflowEvents = sendWorkflowEvents && TypeHasWorkflows(SystemObjects.IntersectType.ToString(), rt.ID, null);

            var executionItemDupes = import.Where(i => i.ExecutionItemUid.HasValue).GroupBy(i => i.ExecutionItemUid).Where(i => i.Count() > 1).Select(i => new { ExecutionItemUid = i.Key, Count = i.Count() }).ToList();
            var tooLongOwners = import.Where(x => !string.IsNullOrEmpty(x.Owner) && x.Owner.Length > 100).ToList();

            if (executionItemDupes.Any())
            {
                execution.ErrorMessage = $"Duplicate execution item identifiers: {string.Join(", ", executionItemDupes.Select(i => i.ExecutionItemUid.ToString()))}. Identifiers must be unique within a batch.";
                results.AddRange(import.Select(i => new DatabaseBulkRelationshipResult { ExecutionItemUid = i.ExecutionItemUid, Message = execution.ErrorMessage, Success = false }));
            }
            else if (tooLongOwners.Any())
            {
                execution.ErrorMessage = $"Owner value max length exceeded : {string.Join(", ", tooLongOwners.Select(i => i.Owner))}. Max length of Owner field is 100 characters.";
                results.AddRange(import.Select(i => new DatabaseBulkRelationshipResult { ExecutionItemUid = i.ExecutionItemUid, Message = execution.ErrorMessage, Success = false }));
            }
            else if (!executionItemDupes.Any() && !tooLongOwners.Any())
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
                    table.Columns.Add("Owner", typeof(string));

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
                    var requiredFieldTypeNames = fieldTypes.Where(f => f.IsRequired && string.IsNullOrEmpty(f.DefaultValue) && f.Type != DataType.Counter.ToString()).Select(f => f.Name).ToList();
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
                            var fieldRows = ValidateFields("IntersectType", rt.ID, true, fieldTypes, requiredFieldTypeNames, model.Fields, execution.ExecutionID, i, fieldTable, out success, out errorMessage, jsonElementsEnabled: false);

                            if (success)
                            {
                                fieldRows.ForEach(fr => { fieldTable.Rows.Add(fr); });

                                var row = table.NewRow();

                                row["ExecutionID"] = execution.ExecutionID;
                                row["ItemNumber"] = i;
                                row["SubjectUid"] = model.SubjectAssetUid;
                                row["ObjectUid"] = model.ObjectAssetUid;
                                row["Owner"] = model.Owner;
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
                        bulkCopy.ColumnMappings.Add("Owner", "Owner");

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
                            bulkCopy.DestinationTableName = ApiExecutionFieldTable;
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
                            ResolveFieldLookupValues(execution.ExecutionID, ApiExecutionFieldTable, timeout);
                        }
                        AddMeasurement(metrics, "ResolveFieldLookupValues", sw.ElapsedMilliseconds, ++step);
                        sw.Restart();
                        LogFieldLookupErrors(execution.ExecutionID, "IntersectType", rt.ID, "Relationship", lookupFieldsPassedByValue, timeout);
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
				                (P.PermissionsBitMask is not null and P.PermissionsBitMask & @p <> @p) 
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
				                (P.PermissionsBitMask is not null and P.PermissionsBitMask & @p <> @p) 
				                or 
				                P.PermissionsBitMask is null
				                )
                    group by R.ExecutionID, R.ItemNumber
                    ) S on S.ExecutionID = T.ExecutionID and S.ItemNumber = T.ItemNumber;
end",
                    new { execution.ExecutionID, execution.ResourceID, p = (int)Permission.ModifyRelationships }, commandTimeout: timeout);
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
			    T.UpdatedOn = getutcdate(),
                T.Owner = coalesce(S.Owner,T.Owner)
    when not matched by target then
	    insert  (IntersectTypeID, Subject, SubjectID, Object, ObjectID, [State], CreatedBy, CreatedOn, UpdatedBy, UpdatedOn, [Owner])
	    values  (@rtID, S.Subject, S.SubjectID, S.Object, S.ObjectID, 1, @CurrentResourceID, getutcdate(), @CurrentResourceID, getutcdate(), coalesce(S.Owner,'BULK_API'))
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
                                        fieldTypeUpdates = MergeFields(execution.ExecutionID, trans, "api.ExecutionRelationship", "'Intersect'", "A.IntersectID", beginItemNumber, endItemNumber, sendWorkflowEvents, timeout);
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


                    // Send score recalculation notifications.
                    sw.Restart();
                    CreateImportRelationshipsExecution(execution.ExecutionID, rt.ID);
                    AddMeasurement(metrics, $"SendScoreEventWithPayload", sw.ElapsedMilliseconds, ++step);
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

                #region Place Subject / Object Asset ID on Execution table for record keeping and scoring.

                Connection.Execute(@"
update	T
set		T.SubjectID = S.ID,
        T.ObjectID = O.ID
from	api.ExecutionDeletedRelationship T
        inner join [Intersect] I on I.ID = T.IntersectID
        left join Asset S on S.Object = I.Subject and S.ObjectID = I.SubjectID
        left join Asset O on O.Object = I.Object and O.ObjectID = I.ObjectID
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
				            (P.PermissionsBitMask is not null and P.PermissionsBitMask & @p = @p) 
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
				            (P.PermissionsBitMask is not null and P.PermissionsBitMask & @p = @p) 
				            or 
				            P.PermissionsBitMask is null
				            )
                group by R.ExecutionID, R.ItemNumber
                ) S on S.ExecutionID = T.ExecutionID and S.ItemNumber = T.ItemNumber 
where	T.ExecutionID = @ExecutionID 
	and S.ItemNumber is null;
end",
                new { execution.ExecutionID, execution.ResourceID, p = (int)Permission.DeleteRelationships }, commandTimeout: timeout);

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
                {
                    SendWorkflowEvents("IntersectType", it.ID, results, ChangeType.Delete);
                }
                CreateDeleteRelationshipsExecution(execution.ExecutionID, it.ID);
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


            //Check for diagram relationships
            Connection.Execute($@"
                                Update ER
                                Set Success=0,
                                Message='Relationship type has existing relationships' 
                                from [api].[ExecutionDeletedRelationshipType] ER
								inner join IntersectType it on er.Uid = it.uid
								inner join [Predicate] p on it.PredicateID = p.ID
                                where  ER.ExecutionID=@executionID and p.Type = {((int)PredicateType.Diagram)}  and
                                ER.Success is null
                                and  exists (select it.id from processexpandeddata ped
                            inner join IntersectType it on it.uid = ER.Uid
                            where ped.DiagramAssetTypeUid = it.SubjectUid 
                            and (ped.FromAssetTypeUid = it.ObjectUid or ped.ToAssetTypeUid = it.objectuid) )
                        ", new { executionID = execution.ExecutionID }, commandTimeout: timeout);


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

update  ER 
set     Success = 0,
        Message = 'Relationship type referenced in FieldFromRelationship field type. Cardinality may not be changed.' 
from    [api].[ExecutionRelationshipType] ER 
        inner join IntersectType I on I.Uid = ER.[Uid] 
            and (
                (I.SubjectCardinality = 1 and ER.SubjectCardinality <> 1 and I.ID in (select LookupObjectID from FieldType where LookupObjectType = 'IntersectType' and Object = I.Object and ObjectID = I.ObjectID and [Type] = 'FieldFromRelationship')) 
                or (I.ObjectCardinality = 1 and ER.ObjectCardinality <> 1  and I.ID in (select LookupObjectID from FieldType where LookupObjectType = 'IntersectType' and Object = I.Subject and ObjectID = I.SubjectID and [Type] = 'FieldFromRelationship')) 
                )
            and  ER.ExecutionID = @ExecutionID 
            and ER.Success is null;

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
                            if (model.Name == null)
                                row["Name"] = "";
                            else
                            {
                                row["Name"] = model.Name.Trim();
                            }
                            row["Description"] = model.Description;
                            if (model.Uid.HasValue && model.Uid.Value != Guid.Empty)
                            {
                                row["Uid"] = model.Uid;
                            }

                            if (model.IsNew == true)
                            {
                                row["IsNew"] = true;
                            }
                            else
                            {
                                row["IsNew"] = false;
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
inner join api.Execution AE on AE.ExecutionID = ERT.ExecutionID
left join [ResponsibilityType] RT on RT.Uid = ERT.Uid
where	 AE.Method = 'PUT' and ERT.ExecutionID = @ExecutionID and ERT.Uid is not null and RT.Uid is null;

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
                                        on (RT.Uid = S.Uid and S.IsNew = 0)
										when matched then
										update  
											set RT.Name = S.Name,
											RT.Description = S.Description,
                                            UpdatedOn = getutcdate(),
                                            UpdatedBy = @CurrentResourceID
                                        when not matched then
	                                        insert (Name, Description, Uid, CreatedOn, CreatedBy)
	                                        values (S.Name,S.Description, ISNULL(S.Uid,newid()), getutcdate(), @CurrentResourceID)
	                                    output inserted.ID, inserted.Uid, S.ExecutionItemUid into #mergeResultTable;

                                        update RT
                                        set RT.ResponsibilityTypeId = Res.ResponsibilityTypeId,
	                                        RT.Uid = Res.ResponsibilityTypeUid,
                                            RT.Success = 1
                                        from api.ExecutionResponsibilityType RT
                                                inner join #mergeResultTable Res on Res.ExecutionItemUid = RT.ExecutionItemUid
                                        where RT.ExecutionID = @ExecutionID and RT.Success is null";

                                    Connection.Execute(insertSQL,
                                            new { execution.ExecutionID, beginItemNumber, endItemNumber, CurrentResourceID }, transaction: trans, commandTimeout: timeout);

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
            string keyTableTempCreation = @"CREATE TABLE #Keys (AssetID bigint, ActiveKey varchar(32)); CREATE NONCLUSTERED INDEX CIX_TempApiExecutionKeys ON #Keys ( ActiveKey ASC ); ";
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

insert into #Keys WITH(TABLOCK)
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
                        STRING_AGG(coalesce(F.Value, F.FormattedValue, FT.DefaultValue), '|') within group (order by FT.ColumnOrder asc, FT.Name asc) as ProposedKey
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

insert into #Keys WITH(TABLOCK)
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
		utility.GetHash(cast(@ID as nvarchar) + '|' + STRING_AGG(coalesce((case when ft.type <> 'Counter' then F.Value else isnull(cast(FCV.Value as nvarchar(50)),newid()) end), F.FormattedValue, FT.DefaultValue), '|') within group (order by FT.ColumnOrder asc, FT.Name asc)) as ActiveKey 
from		Asset A 
		inner join FieldType FT on FT.AssetTypeID = A.AssetTypeID and FT.IsPartOfKey = 1
		left join Field F on FT.ID = F.FieldTypeID and F.AssetID = A.ID
        left join FieldCounterValue FCV on FT.Type = 'Counter' and FCV.FieldTypeId = FT.ID and FCV.AssetId = F.AssetId
where	    A.AssetTypeID = @ID
group by    A.ID;";

                if (parentIntersectTypeId.HasValue)
                {
                    activeKeySql = $@"
select		A.ID,
		utility.GetHash(cast(@ID as nvarchar) + '|' + COALESCE(cast(P.Uid as nvarchar(50))+'|', '') + STRING_AGG(coalesce(F.Value, F.FormattedValue, FT.DefaultValue), '|') within group (order by FT.ColumnOrder asc, FT.Name asc)) as ActiveKey
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

insert into #Keys WITH(TABLOCK)
{activeKeySql} 

{keyComparisonUpdateStatement}",
                new { executionID, at.ID, intersectTypeID = parentIntersectTypeId ?? 0 }, commandTimeout: timeout, transaction: trans);
            }

        }

        public List<AssetMeasureModel> GetAssetMeasuresFromRuleResults(List<Guid> ruleResultUids)
        {
            var ruleResults = new DataTable();
            ruleResults.Columns.Add("RuleResultUid", typeof(Guid));
            
            foreach(var r in ruleResultUids.Distinct()) 
            { 
                var dr = ruleResults.NewRow();
                dr["RuleResultUid"] = r;
                ruleResults.Rows.Add(dr);
            }

            if (Database.Connection.State != ConnectionState.Open)
                Connection.Open();

            List<RuleResultChangedRawModel> rawMeasures;
            using (var trans = Connection.BeginTransaction())
            {
                Connection.Execute(@"create table #RuleResults (
                    RuleResultUid uniqueidentifier not null,
                    PRIMARY KEY NONCLUSTERED (RuleResultUid)
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
create table #Items (
	AllocationUid uniqueidentifier, 
	MetricAssetUid uniqueidentifier, 
	MetricAssetVersionUid uniqueidentifier, 
	AssetUid uniqueidentifier, 
	EffectiveDate date
);

select	distinct
		cast(Re.EffectiveDate as date) as EffectiveDate,
		Oa.AssetTypeUid as RuleAssetTypeUid,
		Oa.AssetTypeId as RuleAssetTypeId,
		Oa.Uid as RuleAssetUid,
		Ea.AssetTypeUid as EvaluatedAssetTypeUid,
		Ea.AssetTypeId as EvaluatedAssetTypeId,
		Ea.Uid as EvaluatedAssetUid,
		Ev.IntersectTypeID,
		Ev.ID as IntersectID
into	#Results
from	AssetResult Re,
		AssetResultEdge Ee,
		graph.AssetNode Ea,
		AssetResultEdge Eo,
		graph.AssetNode Oa,
		#RuleResults Rr,
		graph.AssetEdge Ev
where	match(Ea-(Ee)->Re<-(Eo)-Oa-(Ev)->Ea)
		and Ee.Class = 2
		and Eo.Class = 1
		and Ev.PredicateType = 2
		and Re.Uid = Rr.RuleResultUid;

select	R.IntersectID,
		R.EvaluatedAssetUid,
		R.EvaluatedAssetTypeUid,
		R.RuleAssetUid,
		R.RuleAssetTypeUid,
		L.RollupPathUid,
		L.StartPosition as Position,
		Ma.AllocationUid,
		Mal.AssetTypeUid as AllocationAssetTypeUid,
		Mv.Uid as MetricAssetVersionUid,
		Mv.AssetUid as MetricAssetUid,
		R.EffectiveDate
into	#Raw
from	#Results R
		inner join metrics.RollupPathLink L on L.IntersectTypeID = R.IntersectTypeID
		inner join metrics.AssetVersionRollupPath Mr on Mr.RollupPathUid = L.RollupPathUid
		inner join metrics.AssetVersion Mv on Mv.Uid = Mr.AssetVersionUid
			and (
				(Mv.EffectiveDate <= R.EffectiveDate and Mv.EffectiveEndDate >= R.EffectiveDate)
				or (Mv.EffectiveDate <= R.EffectiveDate and Mv.EffectiveEndDate is null)
			)
		inner join metrics.Asset Ma on Ma.Uid = Mv.AssetUid
		inner join metrics.Allocation Mal on Mal.Uid = Ma.AllocationUid;

with cte as (
	select	EvaluatedAssetUid as AssetUid,
			EffectiveDate,
			RollupPathUid,
			Position,
			AllocationUid,
			MetricAssetUid,
			MetricAssetVersionUid,
			AllocationAssetTypeUid,
			EvaluatedAssetTypeUid as AssetTypeUid
	from	#Raw
	where	EvaluatedAssetTypeUid <> AllocationAssetTypeUid
			and RuleAssetTypeUid <> AllocationAssetTypeUid
	union all
	select	S.Uid as AssetUid,
			cte.EffectiveDate,
			L.RollupPathUid,
			L.StartPosition as Position,
			cte.AllocationUid,
			cte.MetricAssetUid,
			cte.MetricAssetVersionUid,
			cte.AllocationAssetTypeUid,
			ST.Uid as AssetTypeUid
	from	cte
			inner join [metrics].[RollupPathLink] L on L.RollupPathUid = cte.RollupPathUid and L.EndPosition = cte.Position and L.StartPosition < cte.Position
			inner join [Intersect] I on I.IntersectTypeID = L.IntersectTypeID
			inner join Asset S on S.Object = I.Subject and S.ObjectID = I.SubjectID
			inner join Asset O on O.Object = I.Object and O.ObjectID = I.ObjectID and O.Uid = cte.AssetUid
			inner join AssetType ST on ST.ID = S.AssetTypeID
)

-- Start Path Asset Scoring
insert into #Items
	select	AllocationUid, 
			MetricAssetUid,
			MetricAssetVersionUid,
			AssetUid,
			EffectiveDate
	from	cte 
	where	Position = 1;

-- Rule Asset Scoring
insert into #Items
	select	distinct
			AllocationUid,
			MetricAssetUid,
			MetricAssetVersionUid,
			RuleAssetUid as AssetUid,
			EffectiveDate
	from	#Raw
	where	RuleAssetTypeUid = AllocationAssetTypeUid;

-- Evaluated Asset Scoring
insert into #Items
	select	distinct
			AllocationUid,
			MetricAssetUid,
			MetricAssetVersionUid,
			EvaluatedAssetUid as AssetUid,
			EffectiveDate
	from	#Raw
	where	EvaluatedAssetTypeUid = AllocationAssetTypeUid;

select * from #Items", transaction: trans).ToList();
            }

            var structuredMeasures = rawMeasures
                .GroupBy(m => new { m.AssetUid, m.EffectiveDate })
                .Select(m => new AssetMeasureModel
                {
                    AssetUid = m.Key.AssetUid,
                    EffectiveDate = m.Key.EffectiveDate,
                    Measures = m.Select(o => new AssetMeasureChildModel
                    {
                        AllocationUid = o.AllocationUid,
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

            #region Scoring
            var ruleResultUids = results.Where(i => i.Success).Select(i => i.Uid.Value).ToList();
            if (ruleResultUids.Count > 0)
            {
                var assetMeasures = GetAssetMeasuresFromRuleResults(ruleResultUids);
                CreateMeasureChangedResultExecution(assetMeasures);
            }
            #endregion Scoring

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

                    var updateOnSuccess = $@"update DAR set DAR.Success = 1 from api.ExecutionDeleteAssetResult DAR inner join #ObjectDeleteAssetEdge DAE on DAE.ExecutionItemUid = DAR.ExecutionItemUid where {querySuffix}";

                    string ruleResultWhereClause = $@"from AssetResult AR, assetResultedge ARE, graph.AssetNode AN, API.[ExecutionDeleteAssetResult] DAR 
where 
DAR.ExecutionID = @executionID 
and DAR.Success is null 

and Match (AN -(ARE)-> AR) 
and (
    (DAR.Uid is null or DAR.Uid ='00000000-0000-0000-0000-000000000000') 
    or AR.Uid = DAR.Uid
    )
and (
        (DAR.OwningAssetUid is null or DAR.OwningAssetUid ='00000000-0000-0000-0000-000000000000') 
        or AR.Uid in    (
                        select  AR1.Uid
                        from    AssetResult AR1, assetResultedge ARE1, graph.AssetNode AN1					
			            where   Match (AN1 -(ARE1)-> AR1)
				                and AN1.Uid = DAR.owningAssetUid
				                and ARE1.Class = {(int)ResultRelationClass.Owns}
		                )
	) 
and (
        (DAR.EvaluatedAssetUid is null or DAR.EvaluatedAssetUid ='00000000-0000-0000-0000-000000000000')  
        or AR.Uid in    (
			            select  AR2.Uid
			            from    AssetResult AR2, assetResultedge ARE2, graph.AssetNode AN2					
			            where   Match (AN2 -(ARE2)-> AR2)
				                and AN2.Uid = DAR.evaluatedAssetUid
				                and ARE2.Class = {(int)ResultRelationClass.EvaluatedBy}
		                )
    ) 
and (
        (
            (DAR.EvaluatedAssetUid is null or DAR.EvaluatedAssetUid ='00000000-0000-0000-0000-000000000000')
		)
		or  (DAR.EvaluatedAssetUid is not null and ARE.class =  {(int)ResultRelationClass.EvaluatedBy})
	) 
and (DAR.EffectiveDateStart is null or DAR.EffectiveDateStart <= AR.EffectiveDate) 
and (DAR.EffectiveDateEnd is null or DAR.EffectiveDateEnd >= AR.EffectiveDate) 
and (DAR.RunDateStart is null or AR.RunDate >= DAR.RunDateStart) 
and (DAR.RunDateEnd is null or AR.RunDate <= DAR.RunDateEnd) ";

                    string deleteAssetResultSQL = $@"
    create table #ObjectDeleteAssetEdge ([uid] uniqueidentifier, class int, ItemNumber int, ExecutionItemUid uniqueidentifier, [Operation] varchar(10));
    CREATE NONCLUSTERED INDEX IX_TempObjectMergeAssetEdge ON #ObjectDeleteAssetEdge ( ItemNumber ASC );

    merge into AssetResultEdge DARE 
    using   (
        select  ARE.$from_id as from_id,
                ARE.$to_id as to_id, 
                AR.Uid, 
                ARE.Class, 
                DAR.itemnumber, 
                DAR.ExecutionItemUid
        {ruleResultWhereClause} 
                and DAR.ItemNumber between @beginItemNumber and @endItemNumber  
        ) R on R.from_id = DARE.$from_id and R.to_id = DARE.$to_id
    WHEN MATCHED THEN DELETE 
    output R.uid, R.class, R.itemnumber, R.ExecutionItemUid, $action into #ObjectDeleteAssetEdge;

    merge into AssetResult AR
    using   (
	    select  AR1.uid
	    from    AssetResult AR1
	            INNER JOIN #ObjectDeleteAssetEdge MAE ON AR1.UID=MAE.Uid
	            left join assetResultEdge ARE on ARE.$to_id = AR1.$node_id
	    where   ARE.$to_id is null
	    ) R on R.Uid = AR.Uid
    WHEN MATCHED THEN DELETE;

    {updateOnSuccess}";

                    // Find out which items we need to update scores for.
                    var ruleResultUids = Query<Guid>($@"select distinct AR.Uid {ruleResultWhereClause}", new { execution.ExecutionID }).ToList();
                    List<AssetMeasureModel> assetMeasures = null;
                    if (ruleResultUids.Count > 0)
                    {
                        assetMeasures = GetAssetMeasuresFromRuleResults(ruleResultUids);
                    }

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

                    // Now that results are deleted, send the score events to re-process scores for impacted assets.
                    if (assetMeasures != null)
                    {
                        CreateMeasureChangedResultExecution(assetMeasures, execution.ExecutionID);
                    }
                }
            }

            return results;
        }

        public List<ResponsibilityRuleUpsertResponseModel> UpsertResponsibilityRules(ApiExecution execution, Guid responsibilityTypeUid, List<ResponsibilityRuleUpsertModel> import, int timeout = 3600)
        {
            var results = new List<ResponsibilityRuleUpsertResponseModel>();
            bool generalChecksCompleted = false;
            CurrentExecutionLocationModel currentLocation = null;

            SetApiExecutionProcessingStartTime(execution.ExecutionID);

            List<string> invalidFieldTypes = new List<string> {
            DataType.Path.ToString(),
            DataType.ComplexRelationLookup.ToString(),
            DataType.FieldFromRelationship.ToString(),
            DataType.DataTableSelect.ToString(),
            DataType.OwnershipLookup.ToString(),
            DataType.RefListRelationship.ToString(),
            DataType.JsonElement.ToString(),
            DataType.Tag.ToString(),
            DataType.JSON.ToString(),
            DataType.Score.ToString(),
            DataType.Relationship.ToString()
        };

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

                            if (execution.Method.ToLower() == "put" && !model.Uid.HasValue)
                            {
                                rowError += ";UID cannot be empty!";
                            }

                            if (model.Uid.HasValue && model.Uid.Value != Guid.Empty)
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
inner join api.execution ae on ae.executionid = ep.executionid
left join ResponsibilityTypeRelationRule rtrr on rtrr.uid = ep.uid
where	ep.ExecutionID = @ExecutionID and EP.Uid is not null and rtrr.uid is null and ae.Method = 'Put';

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
MatchType nvarchar(20),
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
ThenData.MatchType,
ThenCond.*,
case 
when ThenCond.IntersectTypeUid is not null then cast(thencond.intersecttypeuid as uniqueidentifier)
else null
end as ValueAsUid
from api.executionresponsibilityrule
cross apply OPENJSON (Definition, N'$.Then')
WITH (
AssigneeTypeUid uniqueidentifier N'$.AssigneeTypeUid',
MatchType nvarchar(20) N'$.MatchType',
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
null as MatchType,
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
    d.MatchType,
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
	ROW_NUMBER() OVER(ORDER BY(SELECT NULL)) as rowNumber,
    ft2.type as FieldType
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
set ErrorMessage = 'Invalid Field Type.'
where 
isnull(fieldtypeid,0) != 0 
AND 
fieldtypename <> '' 
AND 
AssigneeUid is null
AND
FieldType in @invalidFieldTypes

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
select top 1 Object,ObjectID, MatchType, Conditions.json as Conditions
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
                    Connection.Execute(jsonParseSql, new { execution.ExecutionID, invalidFieldTypes }, commandTimeout: timeout);

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
                                    xrr.DefinitionConverted,
                                    ae.method
                                        from api.executionresponsibilityrule xrr
                                    inner join api.execution ae on ae.executionid = xrr.executionid
                                    inner join assettype at on at.uid = xrr.AssetTypeUid
                                    inner join ResponsibilityType rt on rt.uid = xrr.ResponsibilityTypeUid
                                    where xrr.executionid = @ExecutionID and xrr.ItemNumber between @beginItemNumber and @endItemNumber and xrr.success is null
                                    )Data
                                    ON (RTRR.uid = Data.uid and method = 'PUT')
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
                                        THEN insert (uid,ResponsibilityTypeId,Object,ObjectId,Name,Context,IsVisible, ApplyToType,CreatedOn,CreatedBy,Definition)
	                                    values (isnull(data.uid, newid()), data.ResponsibilityTypeId,data.Object, data.ObjectId, data.Name, data.Context, data.IsVisible, data.ApplyToType, getdate(), @resourceId,data.DefinitionConverted)
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

                        if (item.Uid.HasValue && item.Uid.Value != Guid.Empty)
                        {
                            row["GroupUid"] = item.Uid;
                        }

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
		                [Message] = coalesce([Message], '') + 'Already a group called ' + EG.[Name] + ';'
	            from [api].[ExecutionGroup] EG 
	            inner join [Group] G on G.[Name] = EG.[Name]
                left join [Asset] A on A.ObjectID = G.[ID] and A.Object = 'Group' and A.uid = EG.[GroupUid]
                where	ExecutionID = @ExecutionID and A.uid is null and G.Name is not null;

                update	[api].[ExecutionGroup]
                set		Success = 0,
		                [Message] = coalesce([Message], '') + 'Uid provided is not a group uid;'
	            from [api].[ExecutionGroup] EG 
                Inner Join [api].[Execution] E on E.ExecutionID = EG.ExecutionID
	            left join [Asset] A on A.[uid] = EG.[GroupUid] and A.Object = 'Group'
                where	E.Method = 'PUT' and EG.ExecutionID = @ExecutionID and A.uid is null and EG.[GroupUid] is not null;

                update	[api].[ExecutionGroup]
                set		Success = 0,
		                [Message] = coalesce([Message], '') + 'Uid already exists;'
	            from [api].[ExecutionGroup] EG 
                Inner Join [api].[Execution] E on E.ExecutionID = EG.ExecutionID
	            left join [Asset] A on A.[uid] = EG.[GroupUid]
                where	E.Method = 'POST' and EG.ExecutionID = @ExecutionID and A.uid is not null and EG.[GroupUid] is not null;

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

            drop table if exists #auditRecords
			create table #auditRecords (uid uniqueidentifier, FieldName nvarchar(200), OldValue nvarchar(max), NewValue nvarchar(max))
				;with cte as (
				select G.uid, 
				G.Name as OldName, 
				EG.Name as NewName, 
				G.Description as OldDesc,
				eg.Description as NewDesc,
				G.IsActiveDirectoryGroup as OldIsActiveDirectoryGroup,
				eg.IsActiveDirectoryGroup as NewIsActiveDirectoryGroup
				 from api.ExecutionGroup eg
				inner join [Group] G on G.uid = eg.groupuid
				where EG.ExecutionID = @ExecutionID
				and EG.ItemNumber between @beginItemNumber and @endItemNumber
                and EG.Success is null)
			insert into #auditRecords
			select uid, 'Name' as FieldName, OldName as OldValue, NewName as NewValue from cte
            union 
			select uid, 'Description' as FieldName, OldDesc as OldValue, NewDesc as NewValue from cte
			union
			select uid, 'IsActiveDirectoryGroup' as FieldName, try_cast(OldIsActiveDirectoryGroup as nvarchar(10)) as OldValue, try_cast(NewIsActiveDirectoryGroup as nvarchar(10)) as NewValue from cte


                                            
            merge into [Group] G
            using ( 
select A.ObjectID as GroupID ,
EG.Name,EG.Description,
EG.ExecutionItemUid,
EG.IsActiveDirectoryGroup,
PO.ObjectID as PrimaryID,
SO.ObjectID as SecondaryID,
EG.GroupUid
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
                    G.UpdatedBy = @CurrentResourceID,
                    G.UpdatedOn = GETUTCDATE(),
                    G.IsActiveDirectoryGroup = S.IsActiveDirectoryGroup
                when not matched then
	                insert ([Uid], Name, Description, PrimaryOwnerResourceID, SecondaryOwnerResourceID,IsActiveDirectoryGroup,UpdatedOn,UpdatedBy)
	                values (ISNULL(S.GroupUid, NEWID()), TRIM(S.Name),S.Description, S.PrimaryID, S.SecondaryID,S.IsActiveDirectoryGroup,GETDATE(),@CurrentResourceID)
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
                where EG.ExecutionID = @ExecutionID and EG.Success is null

			    declare @audit table (auditId int)
			    insert into reporting.Global_Audit
			    OUTPUT INSERTED.ID
			    INTO @audit
			    select distinct 'Group', g.id, G.Name, @currentresourceid, GETUTCDATE(), 'Updated', 'Group', g.ID, 'Group', G.Name,'Group updated' from #auditRecords ar
			    inner join [Group] G on G.uid = ar.uid

			    insert into reporting.global_fieldaudit
			    select a.auditid,0, ar.fieldname, 1, ar.newvalue, ar.oldvalue from @audit a
			    inner join reporting.Global_Audit ga on ga.id = a.auditid
			    inner join [Group] G on G.Id = ga.ObjectId
			    inner join #auditRecords ar on g.uid = ar.uid
			    where isnull(ar.newvalue,'') <> isnull(ar.oldvalue,'')";

                                    Connection.Execute(insertSQL,
                                            new { execution.ExecutionID, beginItemNumber, endItemNumber, CurrentResourceID }, transaction: trans, commandTimeout: timeout);

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

            //Convert GroupResponseResult to DatabaseBulkAssetResult to use in SendAssetGraphEvents
            IEnumerable<IGraphAsset> graphResults = results.Where(r => r.uid.HasValue).Select(r =>
            {
                return new DatabaseBulkAssetResult
                {
                    ExecutionItemUid = r.ExecutionItemUid,
                    ItemNumber = r.ItemNumber,
                    uid = r.uid ?? Guid.Empty,
                    Message = r.Message,
                    Success = r.Success,
                    Object = SystemObjects.Group.ToString()
                };
            }).AsEnumerable();
            if (graphResults.Any())
            {
                SendAssetGraphEvents(graphResults);
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
                                var deleteSQL = $@"
                                        insert into reporting.Global_Audit (Object, ObjectID, ObjectName, ResourceID, Date, Action, ActionObject, ActionObjectID, ActionObjectTypeName, ActionObjectName, ActionDescription)
	                                        select	distinct
			                                        'Group', 
			                                        G.ID,
			                                        SUBSTRING(G.Name,1,250),
			                                        @CurrentResourceID, 
			                                        getutcdate(), 
			                                        'Deleted', 
			                                        'Group', 
			                                        G.ID,
			                                        'Group', 
			                                        SUBSTRING(G.Name,1,250), 
			                                        'This group has been removed.'
	                                        from [api].[ExecutionDeletedGroup] EDG
                                            inner join [Group] G on G.Uid = EDG.GroupUid
                                            where	ExecutionID = @ExecutionID

                                        DELETE G
	                                    FROM [Group] G
		                                inner join api.ExecutionDeletedGroup EG on EG.Success is null and EG.ExecutionID = @ExecutionID and EG.ItemNumber between @beginItemNumber and @endItemNumber
		                                inner join Asset A on A .uid = EG.GroupUid
		                                where A.ObjectID = G.ID";

                                Connection.Execute(deleteSQL,
                                        new { execution.ExecutionID, CurrentResourceID, beginItemNumber, endItemNumber }, transaction: trans, commandTimeout: timeout);

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

        public List<DataProfileUpsertResponse> UpsertDataProfiles(List<DataProfileUpsertModel> request, ApiExecution execution, bool isInsert, int timeout = 3600)
        {
            var swBegin = Stopwatch.StartNew();
            TelemetryClient client = new TelemetryClient();
            const string METHOD_NAME = "UpsertDataProfiles";
            bool isLog = true; // trace info for all assets is extermely useful

            DynamicParameters dbArgs = new DynamicParameters();
            bool generalChecksCompleted = false;
            int itemNumber = 1;
            List<DataProfileUpsertResponse> results = new List<DataProfileUpsertResponse>();
            CurrentExecutionLocationModel currentLocation = null;
            var metrics = new Dictionary<string, double>();
            var sw = Stopwatch.StartNew();
            var step = 0;

            var dups = request.Where(i => i.ExecutionItemUid.HasValue && i.ExecutionItemUid.Value != Guid.Empty).GroupBy(i => i.ExecutionItemUid).Where(i => i.Count() > 1).Select(i => new { ExecutionItemUid = i.Key, Count = i.Count() }).ToList();

            var dupRecords = request.GroupBy(i => new { i.assetUid, i.profileSetDate }).Where(i => i.Count() > 1).Select(i => new { keyFields = i.Key, Count = i.Count() }).ToList();

            SetApiExecutionProcessingStartTime(execution.ExecutionID);

            AddMeasurement(metrics, "Checks for duplicates in load", sw.ElapsedMilliseconds, ++step);

            sw.Restart();

            if (dups.Any() || dupRecords.Any())
            {
                if (dups.Any())
                {
                    execution.ErrorMessage = $"Duplicate Execution Item Identifiers: {string.Join(", ", dups.Select(i => i.ExecutionItemUid.ToString()))}. Identifiers must be unique within a batch.";
                }
                else
                {
                    execution.ErrorMessage = $"Duplicate Records: {string.Join(", ", dupRecords.Select(i => $"AssetUid: {i.keyFields.assetUid}, ProfileSetDate: {i.keyFields.profileSetDate}"))}. AssetUid and ProfileSetDate pairs are used as record identifiers and must be unique within a batch.";
                }

                results.AddRange(request.Select(i => new DataProfileUpsertResponse { ExecutionItemUid = execution.ExecutionID, Message = execution.ErrorMessage, Success = false }));
            }
            else
            {
                try
                {
                    currentLocation = GetCurrentExecutionLocation(execution.ExecutionID, "api.ExecutionAssetDataProfile");

                    AddMeasurement(metrics, "Getting execution current location", sw.ElapsedMilliseconds, ++step);
                    sw.Restart();

                    if (currentLocation.HighestItemNumberProcessed > 0)
                    {
                        results.AddRange(
                            Query<DataProfileUpsertResponse>(
                                $"select * from api.ExecutionAssetDataProfile where ExecutionID = @ExecutionID and ItemNumber <= {currentLocation.HighestItemNumberProcessed}",
                                new { execution.ExecutionID }
                            )
                        );
                    }

                    #region Build data tables.
                    var DataProfileTable = new DataTable();
                    var DataProfileSampleTable = new DataTable();

                    DataProfileTable.Columns.Add("ExecutionID", typeof(Guid));
                    DataProfileTable.Columns.Add("ItemNumber", typeof(int));
                    DataProfileTable.Columns.Add("ExecutionItemUid", typeof(Guid));
                    DataProfileTable.Columns.Add("AssetUid", typeof(Guid));
                    DataProfileTable.Columns.Add("ProfileSetDate", typeof(DateTime));

                    DataProfileTable.Columns.Add("SampleCount", typeof(long));
                    DataProfileTable.Columns.Add("NullCount", typeof(long));
                    DataProfileTable.Columns.Add("BlankCount", typeof(long));
                    DataProfileTable.Columns.Add("MeanValue", typeof(double));
                    DataProfileTable.Columns.Add("MinimumValue", typeof(string));

                    DataProfileTable.Columns.Add("MaximumValue", typeof(string));
                    DataProfileTable.Columns.Add("MinimumLength", typeof(int));
                    DataProfileTable.Columns.Add("MaximumLength", typeof(int));
                    DataProfileTable.Columns.Add("StandardDeviation", typeof(double));
                    DataProfileTable.Columns.Add("Type", typeof(string));

                    DataProfileTable.Columns.Add("Multiline", typeof(bool));
                    DataProfileTable.Columns.Add("RegExp", typeof(string));
                    DataProfileTable.Columns.Add("Confidence", typeof(decimal));
                    DataProfileTable.Columns.Add("TypeQualifier", typeof(string));
                    DataProfileTable.Columns.Add("LogicalType", typeof(bool));

                    DataProfileTable.Columns.Add("LeadingWhiteSpace", typeof(bool));
                    DataProfileTable.Columns.Add("LeadingZeroCount", typeof(int));
                    DataProfileTable.Columns.Add("TrailingWhiteSpace", typeof(bool));

                    DataProfileTable.Columns.Add("MatchCount", typeof(long));
                    DataProfileTable.Columns.Add("OutlierCardinality", typeof(int));
                    DataProfileTable.Columns.Add("DataSignature", typeof(string));

                    DataProfileTable.Columns.Add("StructureSignature", typeof(string));
                    DataProfileTable.Columns.Add("Cardinality", typeof(int));
                    DataProfileTable.Columns.Add("ShapeCardinality", typeof(int));

                    DataProfileTable.Columns.Add("TotalCount", typeof(long));
                    DataProfileTable.Columns.Add("OutlierCount", typeof(long));
                    DataProfileTable.Columns.Add("KeyConfidence", typeof(decimal));
                    DataProfileTable.Columns.Add("DetectionLocale", typeof(string));
                    DataProfileTable.Columns.Add("FtaVersion", typeof(string));
                    DataProfileTable.Columns.Add("DecimalSeparator", typeof(string));

                    DataProfileSampleTable.Columns.Add("ExecutionID", typeof(Guid));
                    DataProfileSampleTable.Columns.Add("ItemNumber", typeof(int));
                    DataProfileSampleTable.Columns.Add("ExecutionItemUid", typeof(Guid));
                    DataProfileSampleTable.Columns.Add("SampleType", typeof(string));
                    DataProfileSampleTable.Columns.Add("Key", typeof(string));
                    DataProfileSampleTable.Columns.Add("Value", typeof(string));

                    #region Populate Data Tables
                    foreach (var item in request)
                    {
                        var row = DataProfileTable.NewRow();

                        row["ExecutionID"] = execution.ExecutionID;
                        row["ItemNumber"] = itemNumber;
                        if (item.ExecutionItemUid.HasValue)
                        {
                            row["ExecutionItemUid"] = item.ExecutionItemUid;
                        }
                        else
                        {
                            row["ExecutionItemUid"] = DBNull.Value;
                        }
                        row["AssetUid"] = item.assetUid;
                        row["ProfileSetDate"] = item.profileSetDate.Date;

                        row["SampleCount"] = item.sampleCount ?? (object)DBNull.Value;
                        row["NullCount"] = item.nullCount ?? (object)DBNull.Value;
                        row["BlankCount"] = item.blankCount ?? (object)DBNull.Value;
                        row["MeanValue"] = item.meanValue ?? (object)DBNull.Value;
                        row["MinimumValue"] = item.minValue ?? (object)DBNull.Value;

                        row["MaximumValue"] = item.maxValue ?? (object)DBNull.Value;
                        row["MinimumLength"] = item.minLength ?? (object)DBNull.Value;
                        row["MaximumLength"] = item.maxLength ?? (object)DBNull.Value;
                        row["StandardDeviation"] = item.standardDeviation ?? (object)DBNull.Value;
                        row["Type"] = item.type ?? (object)DBNull.Value;

                        row["Multiline"] = item.multiline ?? (object)DBNull.Value;
                        row["RegExp"] = item.regExp ?? (object)DBNull.Value;
                        row["Confidence"] = item.confidence ?? (object)DBNull.Value;
                        row["TypeQualifier"] = item.typeQualifier ?? (object)DBNull.Value;
                        row["LogicalType"] = item.logicalType ?? (object)DBNull.Value;

                        row["LeadingWhiteSpace"] = item.leadingWhiteSpace ?? (object)DBNull.Value;
                        row["LeadingZeroCount"] = item.leadingZeroCount ?? (object)DBNull.Value;
                        row["TrailingWhiteSpace"] = item.trailingWhiteSpace ?? (object)DBNull.Value;
                        row["MatchCount"] = item.matchCount ?? (object)DBNull.Value;
                        row["OutlierCardinality"] = item.outlierCardinality ?? (object)DBNull.Value;

                        row["DataSignature"] = item.dataSignature ?? (object)DBNull.Value;
                        row["StructureSignature"] = item.structureSignature ?? (object)DBNull.Value;
                        row["Cardinality"] = item.cardinality ?? (object)DBNull.Value;
                        row["ShapeCardinality"] = item.shapesCardinality ?? (object)DBNull.Value;

                        row["TotalCount"] = item.TotalCount ?? (object)DBNull.Value;
                        row["OutlierCount"] = item.OutlierCount ?? (object)DBNull.Value;
                        row["KeyConfidence"] = item.KeyConfidence ?? (object)DBNull.Value;
                        row["DetectionLocale"] = item.DetectionLocale ?? (object)DBNull.Value;
                        row["FtaVersion"] = item.FtaVersion ?? (object)DBNull.Value;
                        row["DecimalSeparator"] = item.DecimalSeparator ?? (object)DBNull.Value;

                        DataProfileTable.Rows.Add(row);
                        if (item.outlierDetail != null)
                        {
                            foreach (var outlier in item.outlierDetail)
                            {
                                var sampleRow = DataProfileSampleTable.NewRow();
                                sampleRow["ExecutionID"] = execution.ExecutionID;
                                sampleRow["ItemNumber"] = itemNumber;
                                if (item.ExecutionItemUid.HasValue)
                                {
                                    row["ExecutionItemUid"] = item.ExecutionItemUid;
                                }
                                else
                                {
                                    row["ExecutionItemUid"] = DBNull.Value;
                                }
                                sampleRow["SampleType"] = "outlierDetail";
                                sampleRow["Key"] = outlier.key ?? (object)DBNull.Value;
                                sampleRow["Value"] = outlier.count.ToString();
                                DataProfileSampleTable.Rows.Add(sampleRow);
                            }
                        }

                        if (item.shapesDetail != null)
                        {
                            foreach (var shape in item?.shapesDetail)
                            {
                                var sampleRow = DataProfileSampleTable.NewRow();
                                sampleRow["ExecutionID"] = execution.ExecutionID;
                                sampleRow["ItemNumber"] = itemNumber;
                                if (item.ExecutionItemUid.HasValue)
                                {
                                    sampleRow["ExecutionItemUid"] = item.ExecutionItemUid;
                                }
                                sampleRow["SampleType"] = "shapesDetail";
                                sampleRow["Key"] = shape.key;
                                sampleRow["Value"] = shape.count.ToString();
                                DataProfileSampleTable.Rows.Add(sampleRow);
                            }
                        }

                        if (item.cardinalityDetail != null)
                        {
                            foreach (var cardinality in item?.cardinalityDetail)
                            {
                                var sampleRow = DataProfileSampleTable.NewRow();
                                sampleRow["ExecutionID"] = execution.ExecutionID;
                                sampleRow["ItemNumber"] = itemNumber;
                                if (item.ExecutionItemUid.HasValue)
                                {
                                    sampleRow["ExecutionItemUid"] = item.ExecutionItemUid;
                                }
                                sampleRow["SampleType"] = "cardinalityDetail";
                                sampleRow["Key"] = cardinality.key;
                                sampleRow["Value"] = cardinality.count.ToString();
                                DataProfileSampleTable.Rows.Add(sampleRow);
                            }
                        }


                        if (item.topK != null)
                        {
                            foreach (var topK in item?.topK)
                            {
                                var sampleRow = DataProfileSampleTable.NewRow();
                                sampleRow["ExecutionID"] = execution.ExecutionID;
                                sampleRow["ItemNumber"] = itemNumber;
                                if (item.ExecutionItemUid.HasValue)
                                {
                                    sampleRow["ExecutionItemUid"] = item.ExecutionItemUid;
                                }
                                sampleRow["SampleType"] = "topK";
                                sampleRow["Key"] = DBNull.Value;
                                sampleRow["Value"] = topK;
                                DataProfileSampleTable.Rows.Add(sampleRow);
                            }
                        }


                        if (item.bottomK != null)
                        {
                            foreach (var bottomK in item?.bottomK)
                            {
                                var sampleRow = DataProfileSampleTable.NewRow();
                                sampleRow["ExecutionID"] = execution.ExecutionID;
                                sampleRow["ItemNumber"] = itemNumber;
                                if (item.ExecutionItemUid.HasValue)
                                {
                                    sampleRow["ExecutionItemUid"] = item.ExecutionItemUid;
                                }
                                sampleRow["SampleType"] = "bottomK";
                                sampleRow["Key"] = DBNull.Value;
                                sampleRow["Value"] = bottomK;
                                DataProfileSampleTable.Rows.Add(sampleRow);
                            }
                        }

                        itemNumber++;
                    }
                    #endregion
                    #region Bulk Copy

                    if (Database.Connection.State != ConnectionState.Open)
                        Connection.Open();

                    using (var transaction = Connection.BeginTransaction())
                    {
                        try
                        {
                            #region Bulk Copy Data Profile
                            using (var bulkCopy = new SqlBulkCopy((SqlConnection)Database.Connection, SqlBulkCopyOptions.Default, transaction)
                            {
                                BatchSize = DataProfileTable.Rows.Count,
                                DestinationTableName = "[api].[ExecutionAssetDataProfile]",
                                BulkCopyTimeout = SqlBulkBatchTimeout
                            })
                            {
                                bulkCopy.ColumnMappings.Add("ExecutionID", "ExecutionID");
                                bulkCopy.ColumnMappings.Add("ItemNumber", "ItemNumber");
                                bulkCopy.ColumnMappings.Add("ExecutionItemUid", "ExecutionItemUid");
                                bulkCopy.ColumnMappings.Add("AssetUid", "AssetUid");
                                bulkCopy.ColumnMappings.Add("ProfileSetDate", "ProfileSetDate");
                                bulkCopy.ColumnMappings.Add("SampleCount", "SampleCount");
                                bulkCopy.ColumnMappings.Add("NullCount", "NullCount");
                                bulkCopy.ColumnMappings.Add("BlankCount", "BlankCount");
                                bulkCopy.ColumnMappings.Add("MeanValue", "MeanValue");

                                bulkCopy.ColumnMappings.Add("MinimumValue", "MinimumValue");
                                bulkCopy.ColumnMappings.Add("MaximumValue", "MaximumValue");
                                bulkCopy.ColumnMappings.Add("MinimumLength", "MinimumLength");
                                bulkCopy.ColumnMappings.Add("MaximumLength", "MaximumLength");
                                bulkCopy.ColumnMappings.Add("StandardDeviation", "StandardDeviation");

                                bulkCopy.ColumnMappings.Add("Type", "Type");
                                bulkCopy.ColumnMappings.Add("Multiline", "Multiline");
                                bulkCopy.ColumnMappings.Add("RegExp", "RegExp");
                                bulkCopy.ColumnMappings.Add("Confidence", "Confidence");
                                bulkCopy.ColumnMappings.Add("TypeQualifier", "TypeQualifier");

                                bulkCopy.ColumnMappings.Add("LogicalType", "LogicalType");
                                bulkCopy.ColumnMappings.Add("LeadingWhiteSpace", "LeadingWhiteSpace");
                                bulkCopy.ColumnMappings.Add("LeadingZeroCount", "LeadingZeroCount");

                                bulkCopy.ColumnMappings.Add("TrailingWhiteSpace", "TrailingWhiteSpace");
                                bulkCopy.ColumnMappings.Add("MatchCount", "MatchCount");
                                bulkCopy.ColumnMappings.Add("OutlierCardinality", "OutlierCardinality");

                                bulkCopy.ColumnMappings.Add("DataSignature", "DataSignature");
                                bulkCopy.ColumnMappings.Add("StructureSignature", "StructureSignature");
                                bulkCopy.ColumnMappings.Add("Cardinality", "Cardinality");
                                bulkCopy.ColumnMappings.Add("ShapeCardinality", "ShapeCardinality");

                                bulkCopy.ColumnMappings.Add("TotalCount", "TotalCount");
                                bulkCopy.ColumnMappings.Add("OutlierCount", "OutlierCount");
                                bulkCopy.ColumnMappings.Add("KeyConfidence", "KeyConfidence");
                                bulkCopy.ColumnMappings.Add("DetectionLocale", "DetectionLocale");
                                bulkCopy.ColumnMappings.Add("FtaVersion", "FtaVersion");
                                bulkCopy.ColumnMappings.Add("DecimalSeparator", "DecimalSeparator");

                                bulkCopy.WriteToServer(DataProfileTable);
                            }
                            #endregion

                            #region Bulk Copy Data Profile Sample
                            using (var bulkCopy = new SqlBulkCopy((SqlConnection)Database.Connection, SqlBulkCopyOptions.Default, transaction)
                            {
                                BatchSize = DataProfileSampleTable.Rows.Count,
                                DestinationTableName = "[api].[ExecutionAssetDataProfileSample]",
                                BulkCopyTimeout = SqlBulkBatchTimeout
                            })
                            {
                                bulkCopy.ColumnMappings.Add("ExecutionID", "ExecutionID");
                                bulkCopy.ColumnMappings.Add("ItemNumber", "ItemNumber");
                                bulkCopy.ColumnMappings.Add("ExecutionItemUid", "ExecutionItemUid");
                                bulkCopy.ColumnMappings.Add("SampleType", "SampleType");
                                bulkCopy.ColumnMappings.Add("Key", "Key");
                                bulkCopy.ColumnMappings.Add("Value", "Value");

                                bulkCopy.WriteToServer(DataProfileSampleTable);
                            }
                            #endregion

                            transaction.Commit();

                            AddMeasurement(metrics, "BulkCopy to api.Execution table", sw.ElapsedMilliseconds, ++step);
                        }
                        catch (Exception ex)
                        {
                            if (transaction != null)
                            {
                                transaction.Rollback();
                            }
                            throw ex;
                        }

                    }
                    #endregion

                    #endregion

                    Connection.Execute($@"
                        update	api.ExecutionAssetDataProfile
                        set		Success = 0,
		                        [Message] = coalesce([Message] + '; ', '') + 'You must provide a valid Uid.'
                        where	ExecutionID = @ExecutionID and ([AssetUid] is null or [AssetUid] = CAST(CAST(0 AS BINARY) AS UNIQUEIDENTIFIER));

                        update	api.ExecutionAssetDataProfile
                        set		Success = 0,
		                        [Message] = coalesce([Message] + '; ', '') + 'You must provide a ProfileSetDate.'
                        where	ExecutionID = @ExecutionID and [ProfileSetDate] is null;

                        update	EDP
                        set		Success = 0,
		                        [Message] = coalesce([Message] + '; ', '') + 'Asset not found based on Uid provided'
                        from
                            api.ExecutionAssetDataProfile EDP
                            left Join
                            Asset A on EDP.AssetUid = A.Uid
                        where	ExecutionID = @ExecutionID and A.Uid is null;

                        update	EDP
                        set		Success = 0,
		                        [Message] = coalesce([Message] + '; ', '') + 'Record does not exist with AssetUid '+ convert(nvarchar(36), EDP.AssetUid) +' and profileSetDate '+ convert(varchar, EDP.ProfileSetDate, 23)
                        from
                            api.ExecutionAssetDataProfile EDP
                            inner join 
                            Asset A on EDP.AssetUid = A.Uid
                            left join 
                            AssetDataProfile ADP on A.ID = ADP.AssetId and EDP.ProfileSetDate = ADP.ProfileSetDate
                        where	ExecutionID = @ExecutionID and ADP.AssetId is null and @isInsert = 0;
                        
                        update	EDP
                        set		Success = 0,
		                        [Message] = coalesce([Message] + '; ', '') + 'Record already exists with AssetUid '+ convert(nvarchar(36), EDP.AssetUid) +' and profileSetDate '+ convert(varchar, EDP.ProfileSetDate, 23)
                        from
                            api.ExecutionAssetDataProfile EDP
                            inner join 
                            Asset A on EDP.AssetUid = A.Uid
                            inner join 
                            AssetDataProfile ADP on A.ID = ADP.AssetId and EDP.ProfileSetDate = ADP.ProfileSetDate
                        where	ExecutionID = @ExecutionID and @isInsert = 1;

                        Update EDP
                        set		Success = 0,
		                        [Message] = coalesce([Message] + '; ', '') + 'Elements in '+ EDPS.SampleType +' cannot be Empty strings'
                        from  
                            api.ExecutionAssetDataProfile EDP 
                            inner join 
                            (
                                select 
                                    distinct ExecutionID, itemnumber, SampleType 
                                from 
                                    api.ExecutionAssetDataProfileSample 
                                where ExecutionID = @ExecutionID and TRIM(Value)='' and LOWER(SampleType) in ('topk', 'bottomk') 
                            ) EDPS on EDP.ExecutionID=EDPS.ExecutionID and EDP.ItemNumber=EDPS.ItemNumber 
                        where 
                            EDP.ExecutionID = @ExecutionID                             ",
                                    new { execution.ExecutionID, isInsert }, commandTimeout: timeout);

                    AddMeasurement(metrics, "LogAssetDataProfileErrors", sw.ElapsedMilliseconds, ++step);
                    sw.Restart();

                    generalChecksCompleted = true;
                }
                catch (Exception generalEx)
                {
                    generalChecksCompleted = false;
                    var msg = generalEx.GetFullExceptionData(false);
                    execution.ErrorMessage = msg;
                    execution.Processed = 0;
                    execution.Error = request.Count();

                    results = new List<DataProfileUpsertResponse>();
                    results.AddRange(request.Select(i => new DataProfileUpsertResponse { ExecutionItemUid = i.ExecutionItemUid, Message = msg, Success = false }));
                }

                if (generalChecksCompleted)
                {
                    int loopSize = 250;
                    int numberOfLoops = (int)Math.Ceiling((decimal)(execution.Total - currentLocation.HighestItemNumberProcessed) / loopSize);
                    int beginItemNumber = currentLocation.HighestItemNumberProcessed + 1;
                    int endItemNumber = currentLocation.HighestItemNumberProcessed + loopSize;
                    var querySuffix = $"E.Success is null and E.ExecutionID = @ExecutionID and E.ItemNumber between @beginItemNumber and @endItemNumber";
                    var insertSQL = $@"
                                        DROP TABLE IF EXISTS #mergeResultTable
                                        CREATE TABLE #mergeResultTable (DataProfileId INT, ItemNumber INT) 

                                        MERGE INTO AssetDataProfile ADP
                                        USING (
                                                SELECT
                                                    A.ID as AssetId, E.*
                                                FROM  
                                                    api.ExecutionAssetDataProfile E
                                                INNER JOIN
                                                    Asset A ON A.Uid = E.AssetUid
		                                        WHERE {querySuffix}
                                                ) EDP
                                        ON 1 = 0                                       
                                        WHEN NOT MATCHED THEN
                                        INSERT ([AssetID]
                                                    ,[ProfileSetDate]
                                                    ,[SampleCount]
                                                    ,[NullCount]
                                                    ,[BlankCount]
                                                    ,[MeanValue]
                                                    ,[MinimumValue]
                                                    ,[MaximumValue]
                                                    ,[MinimumLength]
                                                    ,[MaximumLength]
                                                    ,[StandardDeviation]
                                                    ,[Type]
                                                    ,[Multiline]
                                                    ,[RegExp]
                                                    ,[Confidence]
                                                    ,[TypeQualifier]
                                                    ,[LogicalType]
                                                    ,[LeadingWhiteSpace]
                                                    ,[LeadingZeroCount]
                                                    ,[TrailingWhiteSpace]
                                                    ,[MatchCount]
                                                    ,[OutlierCardinality]
                                                    ,[DataSignature]
                                                    ,[StructureSignature]
                                                    ,[Cardinality]
                                                    ,[ShapeCardinality]
                                                    ,[TotalCount]
			                                        ,[OutlierCount]
			                                        ,[KeyConfidence]
			                                        ,[DetectionLocale]
			                                        ,[FtaVersion]
			                                        ,[DecimalSeparator]
                                                    ,[CreatedBy]
                                                    ,[CreatedOn]
                                                    ,[UpdatedBy]
                                                    ,[UpdatedOn])
                                                VALUES
                                                    (EDP.AssetID
                                                    ,EDP.ProfileSetDate
                                                    ,EDP.SampleCount
                                                    ,EDP.NullCount
                                                    ,EDP.BlankCount
                                                    ,EDP.MeanValue
                                                    ,EDP.MinimumValue
                                                    ,EDP.MaximumValue
                                                    ,EDP.MinimumLength
                                                    ,EDP.MaximumLength
                                                    ,EDP.StandardDeviation
                                                    ,EDP.Type
                                                    ,EDP.Multiline
                                                    ,EDP.RegExp
                                                    ,EDP.Confidence
                                                    ,EDP.TypeQualifier
                                                    ,EDP.LogicalType
                                                    ,EDP.LeadingWhiteSpace
                                                    ,EDP.LeadingZeroCount
                                                    ,EDP.TrailingWhiteSpace
                                                    ,EDP.MatchCount
                                                    ,EDP.OutlierCardinality
                                                    ,EDP.DataSignature
                                                    ,EDP.StructureSignature
                                                    ,EDP.Cardinality
                                                    ,EDP.ShapeCardinality
                                                    ,EDP.TotalCount
                                                    ,EDP.OutlierCount
                                                    ,EDP.KeyConfidence
                                                    ,EDP.DetectionLocale
                                                    ,EDP.FtaVersion
                                                    ,EDP.DecimalSeparator
                                                    ,@CurrentResourceID
                                                    ,GETDATE()
                                                    ,@CurrentResourceID
                                                    ,GETDATE())
                                            OUTPUT  inserted.ID INT, EDP.ItemNumber INTO #mergeResultTable;";
                    var updateSQL = $@"
                                        DROP TABLE IF EXISTS #mergeResultTable
                                        CREATE TABLE #mergeResultTable (DataProfileId INT, ItemNumber INT) 

                                        MERGE INTO AssetDataProfile ADP
                                        USING (
                                                SELECT
                                                    A.ID as AssetId, E.*
                                                FROM  
                                                    api.ExecutionAssetDataProfile E
                                                INNER JOIN
                                                    Asset A ON A.Uid = E.AssetUid
		                                        WHERE {querySuffix}
                                                ) EDP
                                        ON (EDP.AssetId = ADP.AssetID AND EDP.profileSetDate = ADP.profileSetDate)
                                        WHEN MATCHED THEN
                                        UPDATE SET
                                            ADP.[SampleCount] = EDP.[SampleCount]
                                            ,ADP.[NullCount] = EDP.[NullCount]
                                            ,ADP.[BlankCount] = EDP.[BlankCount]
                                            ,ADP.[MeanValue] = EDP.[MeanValue]
                                            ,ADP.[MinimumValue] = EDP.[MinimumValue]
                                            ,ADP.[MaximumValue] = EDP.[MaximumValue]
                                            ,ADP.[MinimumLength] = EDP.[MinimumLength]
                                            ,ADP.[MaximumLength] = EDP.[MaximumLength]
                                            ,ADP.[StandardDeviation] = EDP.[StandardDeviation]
                                            ,ADP.[Type] = EDP.[Type]
                                            ,ADP.[Multiline] = EDP.[Multiline]
                                            ,ADP.[RegExp] = EDP.[RegExp]
                                            ,ADP.[Confidence] = EDP.[Confidence]
                                            ,ADP.[TypeQualifier] = EDP.[TypeQualifier]
                                            ,ADP.[LogicalType] = EDP.[LogicalType]
                                            ,ADP.[LeadingWhiteSpace] = EDP.[LeadingWhiteSpace]
                                            ,ADP.[LeadingZeroCount] = EDP.[LeadingZeroCount]
                                            ,ADP.[TrailingWhiteSpace] = EDP.[TrailingWhiteSpace]
                                            ,ADP.[MatchCount] = EDP.[MatchCount]
                                            ,ADP.[OutlierCardinality] = EDP.[OutlierCardinality]
                                            ,ADP.[DataSignature] = EDP.[DataSignature]
                                            ,ADP.[StructureSignature] = EDP.[StructureSignature]
                                            ,ADP.[Cardinality] = EDP.[Cardinality]
                                            ,ADP.[ShapeCardinality] = EDP.[ShapeCardinality]
                                            ,ADP.[TotalCount] = EDP.[TotalCount]
                                            ,ADP.[OutlierCount] = EDP.[OutlierCount]
                                            ,ADP.[KeyConfidence] = EDP.[KeyConfidence]
                                            ,ADP.[DetectionLocale] = EDP.[DetectionLocale]
                                            ,ADP.[FtaVersion] = EDP.[FtaVersion]
                                            ,ADP.[DecimalSeparator] = EDP.[DecimalSeparator]
                                            ,ADP.[UpdatedBy] = @CurrentResourceID
                                            ,ADP.[UpdatedOn] = GETDATE()                                       
                                        OUTPUT  inserted.ID INT, EDP.ItemNumber INTO #mergeResultTable;

                                            Delete ADPS from AssetDataProfileSample ADPS inner join #mergeResultTable rt on ADPS.AssetDataProfileID = rt.DataProfileID";
                
                    var insertSampleSQL = $@"
                                        insert into AssetDataProfileSample 
                                                    ([AssetDataProfileID]
                                                    ,[SampleType]
                                                    ,[Key]
                                                    ,[Value])                                            
                                        SELECT  
                                            rt.DataProfileID
                                            ,EDPS.SampleType
                                            ,EDPS.[Key]
                                            ,EDPS.Value
                                        FROM  
                                            api.ExecutionAssetDataProfileSample EDPS
			                            INNER JOIN
				                            api.ExecutionAssetDataProfile E ON EDPS.ExecutionID=E.ExecutionID AND EDPS.itemnumber = E.itemnumber
                                        INNER JOIN 
                                            #mergeResultTable rt ON rt.itemNumber = EDPS.itemNumber
			                            WHERE 
                                            {querySuffix}
                                            ";

                    var sql = $@"{insertSQL}
                                {insertSampleSQL}";

                    if (!isInsert)
                    {
                        sql = $@"{updateSQL}
                                {insertSampleSQL}";
                    }

                    for (int currentLoop = 1; currentLoop <= numberOfLoops; currentLoop++)
                    {
                        bool runCompleted = false;
                        int retryCount = 0;

                        while (!runCompleted && retryCount <= API_V2_RETRY_LIMIT)
                        {
                            
                            using (var trans = Connection.BeginTransaction())
                            {
                                #region Load valid items into table
                                try
                                {                                   
                                    Connection.Execute(sql, new { execution.ExecutionID, beginItemNumber, endItemNumber, CurrentResourceID }, transaction: trans, commandTimeout: timeout);

                                    #endregion

                                    // Update success flag.
                                    Connection.Execute(
                                        $@"update E 
                                            set Success = 1 
                                       From api.ExecutionAssetDataProfile E
                                       where {querySuffix};",
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
                                        sw.Restart();
                                        LogLoopExecutionError(execution.ExecutionID, beginItemNumber, endItemNumber, "api.ExecutionAssetDataProfile", ex.GetFullExceptionData(false), timeout);
                                        AddMeasurement(metrics, $"LogLoopExecutionError >> {currentLoop} >> {retryCount}", sw.ElapsedMilliseconds, ++step);
                                        sw.Restart();
                                    }
                                }
                            }
                        }

                        sw.Restart();
                        results.AddRange(
                            Query<DataProfileUpsertResponse>(
                                $"select [ItemNumber],[AssetUid] as uid,[ExecutionItemUid],[Message],[Success] from api.ExecutionAssetDataProfile where ExecutionID = @ExecutionID and ItemNumber between @beginItemNumber and @endItemNumber ",
                                new { execution.ExecutionID, beginItemNumber, endItemNumber }
                            )
                        );
                        AddMeasurement(metrics, $"results.AddRange >> DataProfileUpsertResponse>> {currentLoop}", sw.ElapsedMilliseconds, ++step);
                        sw.Restart();

                        beginItemNumber += loopSize;
                        endItemNumber += loopSize;
                    }
                }
            }

            AddMeasurement(metrics, $"End of Method", swBegin.ElapsedMilliseconds, ++step);

            this.AITrackMetric(client, execution, METHOD_NAME, metrics, isLog);

            if (Database.Connection.State == ConnectionState.Open)
            {
                Connection.Close();
            }
                
            return results;
        }

        public List<BulkResponsibilityOverrideResponseModel> BulkInsertResponsibilityOverride(List<BulkResponsibilityOverridePostModel> request, ApiExecution execution, int timeout = 3600)
        {
            var swBegin = Stopwatch.StartNew();
            TelemetryClient client = new TelemetryClient();
            const string METHOD_NAME = "BulkInsertResponsibilityOverride";
            bool isLog = true; // trace info for all assets is extermely useful

            DynamicParameters dbArgs = new DynamicParameters();
            bool generalChecksCompleted = false;
            int itemNumber = 1;
            List<BulkResponsibilityOverrideResponseModel> results = new List<BulkResponsibilityOverrideResponseModel>();
            CurrentExecutionLocationModel currentLocation = null;
            var metrics = new Dictionary<string, double>();
            var sw = Stopwatch.StartNew();
            var step = 0;

            var dups = request.Where(i => i.ExecutionItemUid.HasValue && i.ExecutionItemUid.Value != Guid.Empty).GroupBy(i => i.ExecutionItemUid).Where(i => i.Count() > 1).Select(i => new { ExecutionItemUid = i.Key, Count = i.Count() }).ToList();

            var dupRecords = request.GroupBy(i => new { i.AssetUid, i.ResponsibilityTypeUid, i.AssignedUid }).Where(i => i.Count() > 1).Select(i => new { keyFields = i.Key, Count = i.Count() }).ToList();

            SetApiExecutionProcessingStartTime(execution.ExecutionID);

            AddMeasurement(metrics, "Checks for duplicates in load", sw.ElapsedMilliseconds, ++step);

            sw.Restart();

            if (dups.Any() || dupRecords.Any())
            {

                if (dups.Any())
                {
                    execution.ErrorMessage = $"Duplicate Execution Item Identifiers: {string.Join(", ", dups.Select(i => i.ExecutionItemUid.ToString()))}. Identifiers must be unique within a batch.";
                }
                else
                {
                    execution.ErrorMessage = $"Duplicate Records: {string.Join(", ", dupRecords.Select(i => $"AssetUid: {i.keyFields.AssetUid}, ResponsibilityTypeUid: {i.keyFields.ResponsibilityTypeUid}, AssignedUid: {i.keyFields.AssignedUid}"))}. AssetUid, ResponsibilityTypeUid, AssignedUid are key fields and the combination must be unique within a batch.";
                }

                results.AddRange(request.Select(i => new BulkResponsibilityOverrideResponseModel { ExecutionItemUid = execution.ExecutionID, Message = execution.ErrorMessage, Success = false }));
            }
            else
            {
                try
                {
                    currentLocation = GetCurrentExecutionLocation(execution.ExecutionID, "api.ExecutionResponsibilityTypeRelationOverrideItem");

                    AddMeasurement(metrics, "Getting execution current location", sw.ElapsedMilliseconds, ++step);
                    sw.Restart();

                    if (currentLocation.HighestItemNumberProcessed > 0)
                    {
                        results.AddRange(
                            Query<BulkResponsibilityOverrideResponseModel>(
                                $"select * from api.ExecutionResponsibilityTypeRelationOverrideItem where ExecutionID = @ExecutionID and ItemNumber <= {currentLocation.HighestItemNumberProcessed}",
                                new { execution.ExecutionID }
                            )
                        );
                    }

                    #region Build data tables.
                    var ResponsibilityTypeRelationOverrideTable = new DataTable();

                    ResponsibilityTypeRelationOverrideTable.Columns.Add("ExecutionID", typeof(Guid));
                    ResponsibilityTypeRelationOverrideTable.Columns.Add("ItemNumber", typeof(int));
                    ResponsibilityTypeRelationOverrideTable.Columns.Add("ExecutionItemUid", typeof(Guid));
                    ResponsibilityTypeRelationOverrideTable.Columns.Add("ResponsibilityTypeUid", typeof(Guid));
                    ResponsibilityTypeRelationOverrideTable.Columns.Add("AssetUid", typeof(Guid));
                    ResponsibilityTypeRelationOverrideTable.Columns.Add("SecurityAssetUid", typeof(Guid));
                    ResponsibilityTypeRelationOverrideTable.Columns.Add("Context", typeof(string));
                    ResponsibilityTypeRelationOverrideTable.Columns.Add("Message", typeof(string));
                    ResponsibilityTypeRelationOverrideTable.Columns.Add("Success", typeof(bool));

                    #region Populate Data Tables
                    foreach (var item in request)
                    {
                        var row = ResponsibilityTypeRelationOverrideTable.NewRow();

                        row["ExecutionID"] = execution.ExecutionID;
                        row["ItemNumber"] = itemNumber;
                        if (item.ExecutionItemUid.HasValue)
                        {
                            row["ExecutionItemUid"] = item.ExecutionItemUid;
                        }
                        else
                        {
                            row["ExecutionItemUid"] = DBNull.Value;
                        }
                        row["ResponsibilityTypeUid"] = item.ResponsibilityTypeUid;
                        row["AssetUid"] = item.AssetUid;

                        row["SecurityAssetUid"] = item.AssignedUid;
                        row["Context"] = item.Description;

                        ResponsibilityTypeRelationOverrideTable.Rows.Add(row);                        

                        itemNumber++;
                    }
                    #endregion
                    #region Bulk Copy

                    if (Database.Connection.State != ConnectionState.Open)
                        Connection.Open();

                    using (var transaction = Connection.BeginTransaction())
                    {
                        try
                        {
                            #region Bulk Copy Data Profile
                            using (var bulkCopy = new SqlBulkCopy((SqlConnection)Database.Connection, SqlBulkCopyOptions.Default, transaction)
                            {
                                BatchSize = ResponsibilityTypeRelationOverrideTable.Rows.Count,
                                DestinationTableName = "[api].[ExecutionResponsibilityTypeRelationOverrideItem]",
                                BulkCopyTimeout = SqlBulkBatchTimeout
                            })
                            {
                                bulkCopy.ColumnMappings.Add("ExecutionID", "ExecutionID");
                                bulkCopy.ColumnMappings.Add("ItemNumber", "ItemNumber");
                                bulkCopy.ColumnMappings.Add("ExecutionItemUid", "ExecutionItemUid");
                                bulkCopy.ColumnMappings.Add("ResponsibilityTypeUid", "ResponsibilityTypeUid");
                                bulkCopy.ColumnMappings.Add("AssetUid", "AssetUid");
                                bulkCopy.ColumnMappings.Add("SecurityAssetUid", "SecurityAssetUid");
                                bulkCopy.ColumnMappings.Add("Context", "Context");

                                bulkCopy.WriteToServer(ResponsibilityTypeRelationOverrideTable);
                            }
                            #endregion                            

                            transaction.Commit();

                            AddMeasurement(metrics, "BulkCopy to api.Execution table", sw.ElapsedMilliseconds, ++step);
                        }
                        catch (Exception ex)
                        {
                            try
                            {
                                if (transaction != null)
                                {
                                    transaction.Rollback();
                                }
                            }
                            catch { }
                            
                            throw ex;
                        }

                    }
                    #endregion

                    #endregion

                    Connection.Execute($@"
                        update	api.ExecutionResponsibilityTypeRelationOverrideItem
                        set		Success = 0,
		                        [Message] = coalesce([Message] + '; ', '') + 'You must provide a valid ResponsibilityTypeUid.'
                        where	ExecutionID = @ExecutionID and ([ResponsibilityTypeUid] is null or [ResponsibilityTypeUid] = CAST(CAST(0 AS BINARY) AS UNIQUEIDENTIFIER));

                        update	api.ExecutionResponsibilityTypeRelationOverrideItem
                        set		Success = 0,
		                        [Message] = coalesce([Message] + '; ', '') + 'You must provide a valid AssetUid.'
                        where	ExecutionID = @ExecutionID and ([AssetUid] is null or [AssetUid] = CAST(CAST(0 AS BINARY) AS UNIQUEIDENTIFIER));

                        update	api.ExecutionResponsibilityTypeRelationOverrideItem
                        set		Success = 0,
		                        [Message] = coalesce([Message] + '; ', '') + 'You must provide a valid SecurityAssetUid.'
                        where	ExecutionID = @ExecutionID and ([SecurityAssetUid] is null or [SecurityAssetUid] = CAST(CAST(0 AS BINARY) AS UNIQUEIDENTIFIER));

                        update	ERTROI
                        set		Success = 0,
		                        [Message] = coalesce([Message] + '; ', '') + 'Asset not found based on AssetUid provided'
                        from
                            api.ExecutionResponsibilityTypeRelationOverrideItem ERTROI
                            left Join
                            Asset A on ERTROI.AssetUid = A.Uid
                        where	ExecutionID = @ExecutionID and A.Uid is null;         

                        update	ERTROI
                        set		Success = 0,
		                        [Message] = coalesce([Message] + '; ', '') + 'ResponsibilityType not found based on ResponsibilityTypeUid provided'
                        from
                            api.ExecutionResponsibilityTypeRelationOverrideItem ERTROI
                            left Join
                            ResponsibilityType RT on ERTROI.ResponsibilityTypeUid = rt.Uid
                        where	ExecutionID = @ExecutionID and rt.Uid is null;  

                        update	ERTROI
                        set		Success = 0,
		                        [Message] = coalesce([Message] + '; ', '') + 'SecurityAsset not found based on SecurityAssetUid provided'
                        from
                            api.ExecutionResponsibilityTypeRelationOverrideItem ERTROI
                            left Join
                            Asset SA on SA.Uid = ERTROI.SecurityAssetUid and SA.Object in ('Resource', 'Group', 'Organization')
                        where	ExecutionID = @ExecutionID and SA.Uid is null;
                        
                        update	ERTROI
                        set		Success = 0,
		                        [Message] = coalesce([Message] + '; ', '') + 'Responsibility Type not valid for Asset.'
                        from
							  api.ExecutionResponsibilityTypeRelationOverrideItem ERTROI
							  inner join ResponsibilityType RT on RT.[uid] = ERTROI.ResponsibilityTypeUid
							  inner join Asset A on A.uid = ERTROI.AssetUid
							  inner join assettype att on att.id = A.AssetTypeID							  
							  left join responsibilitytyperelation rtr on rtr.responsibilitytypeid = rt.id and att.object = rtr.ObjectType and att.ObjectID = rtr.ObjectID            
                        where	ExecutionID = @ExecutionID and rtr.ResponsibilityTypeID is null;					

                        update	ERTROI
                        set		Success = 0,
		                        [Message] = coalesce([Message] + '; ', '') + 'Responsibility override already exists with AssetUid '+ convert(nvarchar(36), ERTROI.AssetUid) +' and ResponsibilityTypeUid '+ convert(nvarchar(36), ERTROI.ResponsibilityTypeUid) +' and SecurityAssetUid '+ convert(nvarchar(36), ERTROI.SecurityAssetUid)
                        from
                            api.ExecutionResponsibilityTypeRelationOverrideItem ERTROI
                            inner join 
                            Asset A on ERTROI.AssetUid = A.Uid
                            inner join 
                            ResponsibilityType RT on RT.Uid = ERTROI.ResponsibilityTypeUid
                            inner join
                            Asset SA on SA.Uid = ERTROI.SecurityAssetUid and SA.Object in ('Resource', 'Group', 'Organization')
                            inner join
                            ResponsibilityTypeRelationOverrideItem RTROI on RTROI.ResponsibilityTypeId = RT.ID and RTROI.AssetId = A.Id and RTROI.SecurityAssetId = SA.ObjectId
                        where ExecutionID = @ExecutionID;",
                                    new { execution.ExecutionID }, commandTimeout: timeout);

                    AddMeasurement(metrics, "LogResponsibilityTypeRelationOverrideItemErrors", sw.ElapsedMilliseconds, ++step);
                    sw.Restart();

                    generalChecksCompleted = true;
                }
                catch (Exception generalEx)
                {
                    generalChecksCompleted = false;
                    var msg = generalEx.GetFullExceptionData(false);
                    execution.ErrorMessage = msg;
                    execution.Processed = 0;
                    execution.Error = request.Count();

                    results = new List<BulkResponsibilityOverrideResponseModel>();
                    results.AddRange(request.Select(i => new BulkResponsibilityOverrideResponseModel { ExecutionItemUid = i.ExecutionItemUid, Message = msg, Success = false }));
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
                            var querySuffix = $"E.Success is null and E.ExecutionID = @ExecutionID and E.ItemNumber between @beginItemNumber and @endItemNumber";
                            using (var trans = Connection.BeginTransaction())
                            {
                                #region Load valid items into table
                                try
                                {
                                    Connection.Execute($@"
                                        DROP TABLE IF EXISTS #mergeResultTable
                                        CREATE TABLE #mergeResultTable (DataProfileId INT, ItemNumber INT) 

                                        MERGE INTO ResponsibilityTypeRelationOverrideItem RTROI
                                        USING (
                                                SELECT
                                                    A.ID as AssetId, 
                                                    RT.ID as ResponsibilityTypeId, 
                                                    CASE SA.Object
                                                        WHEN 'Resource' THEN 'R'
						                                WHEN 'Group' THEN 'G'
                                                        WHEN 'Organization' THEN 'O'
						                                END as SecurityAsset,                                            
                                                    SA.ObjectID as SecurityAssetId, E.*
                                                FROM  
                                                    api.ExecutionResponsibilityTypeRelationOverrideItem E
                                                INNER JOIN
                                                    Asset A ON A.Uid = E.AssetUid
                                                inner join 
                                                    ResponsibilityType RT on RT.Uid = E.ResponsibilityTypeUid
                                                inner join
                                                    Asset SA on SA.Uid = E.SecurityAssetUid and SA.Object in ('Resource', 'Group', 'Organization')
		                                        WHERE {querySuffix}
                                                ) ERTROI
                                        ON (ERTROI.AssetId = RTROI.AssetID AND ERTROI.ResponsibilityTypeId = RTROI.ResponsibilityTypeId and ERTROI.AssetId = RTROI.AssetID)                                        
                                        WHEN NOT MATCHED THEN
                                        INSERT
                                            ([ResponsibilityTypeID]
                                            ,[AssetID]
                                            ,[SecurityAsset]
                                            ,[SecurityAssetID]
                                            ,[Context]
                                            ,[UpdatedBy]
                                            ,[UpdatedOn])
                                        VALUES
                                            (ERTROI.ResponsibilityTypeID
                                            ,ERTROI.AssetID
                                            ,ERTROI.SecurityAsset
                                            ,ERTROI.SecurityAssetID
                                            ,ERTROI.Context
                                            ,@CurrentResourceID
                                            ,GETDATE())                               
                                        OUTPUT  inserted.ID INT, ERTROI.ItemNumber INTO #mergeResultTable;                                                                                   
                                            ", new { execution.ExecutionID, beginItemNumber, endItemNumber, CurrentResourceID }, transaction: trans, commandTimeout: timeout);

                                    #endregion

                                    // Update success flag.
                                    Connection.Execute(
                                        $@"update E 
                                            set Success = 1 
                                       From api.ExecutionResponsibilityTypeRelationOverrideItem E
                                       where {querySuffix};",
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
                                        sw.Restart();
                                        LogLoopExecutionError(execution.ExecutionID, beginItemNumber, endItemNumber, "api.ExecutionResponsibilityTypeRelationOverrideItem", ex.GetFullExceptionData(false), timeout);
                                        AddMeasurement(metrics, $"LogLoopExecutionError >> {currentLoop} >> {retryCount}", sw.ElapsedMilliseconds, ++step);
                                        sw.Restart();
                                    }
                                }
                            }
                        }

                        sw.Restart();
                        results.AddRange(
                            Query<BulkResponsibilityOverrideResponseModel>(
                                $"select [ItemNumber],[AssetUid] as uid,[ExecutionItemUid],[Message],[Success] from api.ExecutionResponsibilityTypeRelationOverrideItem where ExecutionID = @ExecutionID and ItemNumber between @beginItemNumber and @endItemNumber ",
                                new { execution.ExecutionID, beginItemNumber, endItemNumber }
                            )
                        );
                        AddMeasurement(metrics, $"results.AddRange >> BulkResponsibilityOverrideResponse>> {currentLoop}", sw.ElapsedMilliseconds, ++step);
                        sw.Restart();

                        beginItemNumber += loopSize;
                        endItemNumber += loopSize;
                    }
                }
            }

            AddMeasurement(metrics, $"End of Method", swBegin.ElapsedMilliseconds, ++step);

            this.AITrackMetric(client, execution, METHOD_NAME, metrics, isLog);

            if (Database.Connection.State == ConnectionState.Open)
            {
                Connection.Close();
            }

            return results;
        }

        public List<DataProfileDeleteResponse> DeleteDataProfiles(List<AssetDataProfileDeleteModel> models, ApiExecution execution, int timeout = 3600)
        {
            var swBegin = Stopwatch.StartNew();
            TelemetryClient client = new TelemetryClient();
            const string METHOD_NAME = "DeleteDataProfiles";
            bool isLog = true; // trace info for all assets is extermely useful            

            DynamicParameters dbArgs = new DynamicParameters();
            bool generalChecksCompleted = false;
            int itemNumber = 1;
            List<DataProfileDeleteResponse> results = new List<DataProfileDeleteResponse>();
            CurrentExecutionLocationModel currentLocation = null;
            var metrics = new Dictionary<string, double>();
            var sw = Stopwatch.StartNew();
            var step = 0;

            var dups = models.Where(i => i.ExecutionItemUid.HasValue && i.ExecutionItemUid.Value != Guid.Empty).GroupBy(i => i.ExecutionItemUid).Where(i => i.Count() > 1).Select(i => new { ExecutionItemUid = i.Key, Count = i.Count() }).ToList();

            SetApiExecutionProcessingStartTime(execution.ExecutionID);

            AddMeasurement(metrics, "Checks for duplicates in load", sw.ElapsedMilliseconds, ++step);

            sw.Restart();

            if (dups.Any())
            {
                execution.ErrorMessage = $"Duplicate Execution Item Identifiers: {string.Join(", ", dups.Select(i => i.ExecutionItemUid.ToString()))}. Identifiers must be unique within a batch.";

                results.AddRange(models.Select(i => new DataProfileDeleteResponse { ExecutionItemUid = execution.ExecutionID, Message = execution.ErrorMessage, Success = false }));
            }
            else
            {
                try
                {
                    currentLocation = GetCurrentExecutionLocation(execution.ExecutionID, "api.ExecutionDeleteAssetDataProfile");

                    AddMeasurement(metrics, "Getting execution current location", sw.ElapsedMilliseconds, ++step);
                    sw.Restart();

                    if (currentLocation.HighestItemNumberProcessed > 0)
                    {
                        results.AddRange(
                            Query<DataProfileDeleteResponse>(
                                $"select * from api.ExecutionDeleteAssetDataProfile where ExecutionID = @ExecutionID and ItemNumber <= {currentLocation.HighestItemNumberProcessed}",
                                new { execution.ExecutionID }
                            )
                        );
                    }

                    var table = new DataTable();
                    table.Columns.Add("ExecutionID", typeof(Guid));
                    table.Columns.Add("ItemNumber", typeof(int));
                    table.Columns.Add("ExecutionItemUid", typeof(Guid));
                    table.Columns.Add("AssetUid", typeof(Guid));
                    table.Columns.Add("StartDate", typeof(DateTime));
                    table.Columns.Add("EndDate", typeof(DateTime));
                    table.Columns.Add("Cascade", typeof(bool));
                    table.Columns.Add("Message", typeof(string));
                    table.Columns.Add("Success", typeof(bool));

                    foreach (var item in models)
                    {
                        var row = table.NewRow();
                        List<string> errorMessages = new List<string>();

                        row["ExecutionID"] = execution.ExecutionID;
                        row["ItemNumber"] = itemNumber;
                        if (item.ExecutionItemUid.HasValue)
                        {
                            row["ExecutionItemUid"] = item.ExecutionItemUid;
                        }
                        else
                        {
                            row["ExecutionItemUid"] = DBNull.Value;
                        }
                        row["AssetUid"] = item.AssetUid;
                        row["StartDate"] = item.StartDate.Date;

                        if (item.StartDate == DateTime.MinValue)
                        {
                            errorMessages.Add("Startdate is a required field");
                        }

                        row["EndDate"] = item.EndDate.Date;

                        if (item.EndDate == DateTime.MinValue)
                        {
                            errorMessages.Add("EndDate is a required field");
                        }

                        if (errorMessages.Any())
                        {
                            row["Message"] = string.Join(";", errorMessages);
                            row["Success"] = 0;
                        }

                        row["Cascade"] = item.Cascade;

                        table.Rows.Add(row);

                        itemNumber++;
                    }

                    #region Bulk Copy

                    if (Database.Connection.State != ConnectionState.Open)
                        Connection.Open();

                    using (var transaction = Connection.BeginTransaction())
                    {
                        try
                        {
                            #region Bulk Copy Data Profile
                            using (var bulkCopy = new SqlBulkCopy((SqlConnection)Database.Connection, SqlBulkCopyOptions.Default, transaction)
                            {
                                BatchSize = table.Rows.Count,
                                DestinationTableName = "[api].[ExecutionDeleteAssetDataProfile]",
                                BulkCopyTimeout = SqlBulkBatchTimeout
                            })
                            {
                                bulkCopy.ColumnMappings.Add("ExecutionID", "ExecutionID");
                                bulkCopy.ColumnMappings.Add("ItemNumber", "ItemNumber");
                                bulkCopy.ColumnMappings.Add("ExecutionItemUid", "ExecutionItemUid");
                                bulkCopy.ColumnMappings.Add("AssetUid", "AssetUid");
                                bulkCopy.ColumnMappings.Add("StartDate", "StartDate");
                                bulkCopy.ColumnMappings.Add("EndDate", "EndDate");
                                bulkCopy.ColumnMappings.Add("Cascade", "Cascade");
                                bulkCopy.ColumnMappings.Add("Message", "Message");
                                bulkCopy.ColumnMappings.Add("Success", "Success");

                                bulkCopy.WriteToServer(table);
                            }
                            #endregion

                            transaction.Commit();

                            AddMeasurement(metrics, "BulkCopy to api.Execution table", sw.ElapsedMilliseconds, ++step);
                            sw.Restart();
                        }
                        catch (Exception ex)
                        {
                            if (transaction != null)
                            {
                                transaction.Rollback();
                            }
                            throw ex;
                        }



                    }
                    #endregion
                    Connection.Execute($@"
                        update	api.ExecutionDeleteAssetDataProfile
                        set		Success = 0,
		                        [Message] = coalesce([Message] + '; ', '') + 'You must provide a valid Uid.'
                        where	ExecutionID = @ExecutionID and ([AssetUid] is null or [AssetUid] = CAST(CAST(0 AS BINARY) AS UNIQUEIDENTIFIER));

                        update	DEDP
                        set		Success = 0,
		                        [Message] = coalesce([Message] + '; ', '') + 'Asset not found based on Uid provided'
                        from
                            api.ExecutionDeleteAssetDataProfile DEDP
                            left Join
                            Asset A on DEDP.AssetUid = A.Uid
                        where	ExecutionID = @ExecutionID and A.Uid is null;

                        update	api.ExecutionDeleteAssetDataProfile
                        set		Success = 0,
		                        [Message] = coalesce([Message] + '; ', '') + 'StartDate must be before EndDate.'
                        where	ExecutionID = @ExecutionID and startdate > enddate;",
                                    new { execution.ExecutionID }, commandTimeout: timeout);

                    AddMeasurement(metrics, "LogDeleteAssetDataProfileErrors", sw.ElapsedMilliseconds, ++step);
                    sw.Restart();

                    generalChecksCompleted = true;
                }
                catch (Exception generalEx)
                {
                    generalChecksCompleted = false;
                    var msg = generalEx.GetFullExceptionData(false);
                    execution.ErrorMessage = msg;
                    execution.Processed = 0;
                    execution.Error = models.Count();

                    results = new List<DataProfileDeleteResponse>();
                    results.AddRange(models.Select(i => new DataProfileDeleteResponse { ExecutionItemUid = i.ExecutionItemUid, Message = msg, Success = false }));
                }

                if (generalChecksCompleted)
                {
                    int loopSize = 250;
                    int numberOfLoops = (int)Math.Ceiling((decimal)(execution.Total - currentLocation.HighestItemNumberProcessed) / loopSize);
                    int beginItemNumber = currentLocation.HighestItemNumberProcessed + 1;
                    int endItemNumber = currentLocation.HighestItemNumberProcessed + loopSize;
                    var querySuffix = $"E.Success is null and E.ExecutionID = @ExecutionID and E.ItemNumber between @beginItemNumber and @endItemNumber";

                    var sql = $@"
                                drop table if exists #child
                                create table #child (
	                                itemnumber int,
	                                assetID bigint,
	                                startDate date,
	                                endDate date
                                )

                                drop table if exists #parent
                                create table #parent (
	                                itemnumber int,
	                                assetID bigint,
	                                startDate date,
	                                endDate date
                                )

                                drop table if exists #deleteAssetDataProfile
                                create table #deleteAssetDataProfile (
	                                itemnumber int,
	                                assetID bigint,
	                                startDate date,
	                                endDate date
                                )

                                insert into #parent
                                select 
	                                ItemNumber,
	                                ID,
	                                startdate,
	                                enddate
                                from 
	                                Asset A
	                                inner join
	                                API.ExecutionDeleteAssetDataProfile E on A.uid = E.AssetUid and E.[Cascade] = 1
                                Where
                                    {querySuffix}	                                

                                insert into #deleteAssetDataProfile
                                select * from #parent

                                WHILE ((Select Count(*) from #parent) > 0)
                                BEGIN
	                                insert into #child
	                                select 
		                                ItemNumber,
		                                AAP.Assetid,
		                                p.startDate,
		                                p.endDate
	                                from 
		                                #parent P 
		                                inner join 
		                                [utility].[ArtifactAssetParent] AAP on P.assetID = AAP.ParentAssetID

	                                delete from #parent 
	
	                                insert into #parent
	                                select * from #child

	                                insert into #deleteAssetDataProfile
	                                select 
		                                c.* 
	                                from 
		                                #child c 
		                                left join 
		                                #deleteAssetDataProfile a on c.assetID=a.assetID and a.startdate =c.startdate and a.enddate=c.enddate
	                                where a.assetID is null

	                                delete from #child
                                END

                                insert into #deleteAssetDataProfile
                                select 
	                                ItemNumber,
	                                id,
	                                startdate,
	                                enddate
                                from 
	                                Asset A
	                                inner join
	                                API.ExecutionDeleteAssetDataProfile E on A.uid = E.AssetUid and E.[Cascade] = 0
                                where
                                    {querySuffix}	                                

                                drop table if exists #deletedResults
                                create table #deletedResults (
	                                itemnumber int,
	                                id bigint
                                )

                                merge AssetDataProfile as ADP
                                using (select * from #deleteAssetDataProfile) DADP
                                on DADP.assetID = ADP.AssetID and ADP.ProfileSetDate between DADP.startDate and DADP.endDate
                                when matched then
                                DELETE
                                OUTPUT DADP.itemNumber, DELETED.ID into #deletedResults;

                            
                                Delete from AssetDataProfileSample where AssetDataProfileID in( select ID from #deletedResults dr where dr.ItemNumber between @beginItemNumber and @endItemNumber )

                                Update E
                                set E.DeletedCount = DR.DeletedCount
                                from 
                                api.ExecutionDeleteAssetDataProfile E 
                                cross apply (select itemNumber, Count(ID) as DeletedCount from #deletedResults DR where DR.itemnumber = E.itemNumber group by itemNumber) DR
                                where 
                                {querySuffix}";

                    for (int currentLoop = 1; currentLoop <= numberOfLoops; currentLoop++)
                    {
                        bool runCompleted = false;
                        int retryCount = 0;

                        while (!runCompleted && retryCount <= API_V2_RETRY_LIMIT)
                        {
                            using (var trans = Connection.BeginTransaction())
                            {
                                #region Load valid items into table
                                try
                                {
                                    Connection.Query<KeyValuePair<long, long>>(sql, new { execution.ExecutionID, beginItemNumber, endItemNumber, CurrentResourceID }, transaction: trans, commandTimeout: timeout);

                                    #endregion

                                    // Update success flag.
                                    Connection.Execute(
                                        $@"update E 
                                            set Success = 1 
                                       From api.ExecutionDeleteAssetDataProfile E
                                       where {querySuffix};",
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
                                        sw.Restart();
                                        LogLoopExecutionError(execution.ExecutionID, beginItemNumber, endItemNumber, "api.ExecutionDeleteAssetDataProfile", ex.GetFullExceptionData(false), timeout);
                                        AddMeasurement(metrics, $"LogLoopExecutionError >> {currentLoop} >> {retryCount}", sw.ElapsedMilliseconds, ++step);
                                        sw.Restart();
                                    }
                                }
                            }
                        }

                        sw.Restart();
                        results.AddRange(
                            Query<DataProfileDeleteResponse>(
                                $"select [ItemNumber],[AssetUid] as uid,[ExecutionItemUid],[DeletedCount],[Message],[Success] from api.ExecutionDeleteAssetDataProfile where ExecutionID = @ExecutionID and ItemNumber between @beginItemNumber and @endItemNumber ",
                                new { execution.ExecutionID, beginItemNumber, endItemNumber }
                            )
                        );
                        AddMeasurement(metrics, $"results.AddRange >> DataProfileUpsertResponse>> {currentLoop}", sw.ElapsedMilliseconds, ++step);
                        sw.Restart();

                        beginItemNumber += loopSize;
                        endItemNumber += loopSize;
                    }
                }

                AddMeasurement(metrics, $"End of Method", swBegin.ElapsedMilliseconds, ++step);

                this.AITrackMetric(client, execution, METHOD_NAME, metrics, isLog);

                if (Database.Connection.State == ConnectionState.Open)
                {
                    Connection.Close();
                }

            }

            return results;
        }
    }
}
