using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using d360.core;
using d360.core.entities;
using d360.core.entities.Membership;
using d360.core.entities.Metric;
using d360.core.enums;
using d360.core.enums.Workflow;
using d360.core.exceptions;
using d360.core.helpers;
using d360.core.queue;
using d360.core.resources;

using Dapper;

using Microsoft.ApplicationInsights;

using Newtonsoft.Json;

namespace d360.model
{
    internal class CurrentExecutionLocationModel
    {
        public Guid ExecutionID { get; set; }

        public int HighestItemNumber { get; set; }

        public int HighestItemNumberProcessed { get; set; }
    }

	public partial interface ICompanyContext : IBaseContext
	{
		int ApiTimeout { get; }

		#region DbSets

		DbSet<ApiExecution> ApiExecutions { get; set; }

		DbSet<ApiExecutionsExternal> ApiExecutionsExternals { get; set; }
		
		#endregion
		

		#region Methods

		void SendBatchApiCompletedEvent(ApiExecution execution);

		#endregion
	}

	public partial class CompanyContext : BaseContext, ICompanyContext
	{
		#region Constants / Properties

		internal const int API_V2_RETRY_LIMIT = 10;
        internal const int API_V2_RETRY_INTERVAL = 100; // interval set in ms

        public string ApiExecutionFieldTable { get; set; } = "api.executionfield"; // table to use to load field values from

        public int SqlBulkBatchSize { get; set; } = 5000; // default size to use for sqlbulkcopy operations 0 means one batch

        public int SqlBulkBatchTimeout { get; set; } = 0; // timeout for sqlbulkcopy operations  0 means run until it happens

        public int SqlBulkAssetDeleteSize { get; set; } = 10000; // number of assets removed per transaction on type deletion

        public int SqlBulkIntersectFieldDeleteSize { get; set; } = 50000; //Number of fields, intersects in bulk delete sql

        public int WorkflowSendBatchSize { get; set; } = 50; // number of items to send at a time for a batch of service bus messages

		#endregion
		
		
		#region DbSets

		public DbSet<ApiExecution> ApiExecutions { get; set; }

        public DbSet<ApiExecutionsExternal> ApiExecutionsExternals { get; set; }

		#endregion


		#region Utility

		private void completeApiExecutionAndGetCounts(Guid executionId, string apiTableName)
		{
			Connection.Execute($@"
update	E 
set		E.CompletedOn = @dt,
		E.MarkedForProcessing = 0,
		E.[Total] = Tc.Cnt,
		E.Processed = Pc.Cnt,
		E.[Error] = Ec.Cnt
from	api.Execution E
		cross apply (
			select count(1) as Cnt from api.{apiTableName} where ExecutionID = E.ExecutionID and Success = 0 
		) Ec
		cross apply (
			select count(1) as Cnt from api.{apiTableName} where ExecutionID = E.ExecutionID and Success is not null
		) Pc
		cross apply (
			select count(1) as Cnt from api.{apiTableName} where ExecutionID = E.ExecutionID
		) Tc
where	E.ExecutionID = @executionId",
new { executionId, dt = DateTime.UtcNow }, commandTimeout: 540
);
		}

		public void CopyFieldLookupValuesAsIs(Guid executionID, int timeout = 3600, string fieldTable = "api.ExecutionField", SqlTransaction trans = null)
        {
            Connection.Execute($@"
									update	T
									set		T.LookupValue = T.[FieldValue]
									from	{fieldTable} T
									inner join FieldType ST on ST.ID = T.FieldTypeID and ST.[Type] = 'Lookup' and T.ExecutionID = @executionID",
                                    new { executionID }, commandTimeout: timeout, transaction: trans);
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

		private void DeleteEmptyAssetListFieldByApiExecutionUid(Guid executionUid, SqlTransaction trans, int beginItemNumber, int endItemNumber, int timeout = 3600)
        {
            Connection.Execute(@"delete F from Field F
									inner join api.ExecutionAsset EA on EA.ExecutionID = @executionUid
									inner join FieldType FT on F.FieldTypeID = FT.ID
									where 
										FT.[Type] = 'Lookup'
									  and F.AssetId = EA.AssetID
									  and EA.ItemNumber between @beginItemNumber and @endItemNumber
									  and F.Value = ''", new { executionUid, beginItemNumber, endItemNumber }, transaction: trans, commandTimeout: timeout);
        }
                
		private PredicateType? DeterminePredicateType(string obj)
        {
            PredicateType? predicateType = null;
            switch (obj)
            {
                case "ArtifactType":
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
		
		private static bool FilterIntersectTypeApiViewModel(IntersectTypeApiViewModel item, string keyword, int? id, string subject, string predicate, string @object)
        {
            if (item == null)
            {
                return false;
            }

            if (string.IsNullOrEmpty(keyword)
                && !id.HasValue
                && string.IsNullOrEmpty(subject)
                && string.IsNullOrEmpty(predicate)
                && string.IsNullOrEmpty(@object))
            {
                return true;
            }

            bool checkKeyword(IntersectTypeApiViewModel item, string keyword)
            {
                return checkObject(item, keyword)
                   || checkPredicate(item, keyword)
                   || checkSubject(item, keyword);
            }

            bool checkSubject(IntersectTypeApiViewModel item, string subject)
            {
                return string.IsNullOrEmpty(subject)
                        ? true
                        : (item.Subject?.Name ?? string.Empty).IndexOf(subject, StringComparison.OrdinalIgnoreCase) > -1;
            }

            bool checkPredicate(IntersectTypeApiViewModel item, string predicate)
            {
                return string.IsNullOrEmpty(predicate)
                        ? true
                        : (item.Predicate?.Name ?? string.Empty).IndexOf(predicate, StringComparison.OrdinalIgnoreCase) > -1
                            || (item.Predicate?.Inverse.ToString() ?? string.Empty).IndexOf(predicate, StringComparison.OrdinalIgnoreCase) > -1;
            }

            bool checkObject(IntersectTypeApiViewModel item, string @object)
            {
                return string.IsNullOrEmpty(@object)
                        ? true
                        : (item.Object?.Name ?? string.Empty).IndexOf(@object, StringComparison.OrdinalIgnoreCase) > -1;
            }

            bool checkId(IntersectTypeApiViewModel item, int? id)
            {
                if (!id.HasValue)
                {
                    return true;
                }
                return item.Id.ToString().IndexOf(id.ToString(), StringComparison.OrdinalIgnoreCase) > -1;
            }

            return (string.IsNullOrEmpty(keyword) ? true : checkKeyword(item, keyword))
                        && checkId(item, id)
                        && checkSubject(item, subject)
                        && checkPredicate(item, predicate)
                        && checkObject(item, @object);
        }
		
		private CurrentExecutionLocationModel GetCurrentExecutionLocation(Guid executionID, string targetTable)
        {
            return Connection
                .Query<CurrentExecutionLocationModel>($@"
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
										inner join [Intersect] I on T.ExecutionID = @executionID and I.IntersectTypeID = T.IntersectTypeID and I.ObjectAssetID = T.AssetID and T.ParentUid is null
										inner join Asset S on S.ID = I.SubjectAssetID;",
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
										A.Id as AssetId,
										1 [Level]
									from #tempdistparent P
									inner join Asset A
									on A.uid = p.parentuid
									union all
								select  H.parentuid,
										I.SubjectAssetId as AssetId,
										H.[Level] + 1 [Level]
								from H
								inner join [Intersect] I
									on I.[ObjectAssetID] = h.AssetId
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
											A.Id as AssetId,
											1 [Level]
										from #tempdistchild C
										inner join Asset A
										on a.uid = c.uid
										union all
									select H.uid,
											I.ObjectAssetID as AssetId,
											H.[Level] + 1 [Level]
									from H
									inner join [Intersect] I
										on I.[SubjectAssetID] = h.AssetId
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
								inner join AssetType AST on AST.object = A.objecttype and AST.ObjectID = A.objecttypeid
								inner join dbo.FieldType FT on FT.AssetTypeID = AST.ID and FT.IsRequired = 1 and FT.DefaultValue is null
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
                PermissionInfo permission = GetTypePermissions(at.Object, at.ObjectID).Where(x => (x.ID & Permission.AddAsset) != 0).SingleOrDefault();

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
										and usrper.PermissionsBitMask & @p = @p
										group by usrper.AssetID;

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
								inner join {ApiExecutionFieldTable} EF on EA.ItemNumber = EF.ItemNumber AND EF.ExecutionID = EA.ExecutionId
								inner join FieldType FT on FT.Id = EF.FieldTypeId and FT.[Type] = 'Counter'
								inner join FieldCounterValue FCV on FCV.FieldTypeId = FT.ID and FCV.Value = TRY_CAST(LEFT(EF.FieldValue, 50) AS INT) and a.id <> fcv.assetid
								where EA.ExecutionID = @executionId and EA.Success is null;

								update EA
								set EA.Success = 0,
									EA.[Message] = 'Asset with same counter value already exists. (' + FT.Name + ' = ' + cast(fcv.value as nvarchar(50)) + ')'
								from api.ExecutionAsset EA
								left join asset a on a.uid = ea.[uid]
								inner join api.Execution EX on ex.executionid = @executionId
								inner join {ApiExecutionFieldTable} EF on EA.ItemNumber = EF.ItemNumber AND EF.ExecutionID = EA.ExecutionId
								inner join FieldType FT on FT.Id = EF.FieldTypeId and FT.[Type] = 'Counter'
								inner join FieldCounterValue FCV on FCV.FieldTypeId = FT.ID and FCV.Value = TRY_CAST(LEFT(EF.FieldValue, 50) AS INT)
								where EA.ExecutionID = @executionId and EA.Success is null and a.uid is null;
								", new { executionId }, commandTimeout: timeout);
        }

        private void LogFieldLookupErrors(Guid executionID, string obj, int objID, string errorPrefix, bool lookupFieldsPassedByValue, int timeout = 3600)
        {
            string targetTable = "api.ExecutionRelationship";
			bool isIntersect = true;
			int assetTypeId = -1;

            if (obj != "IntersectType")
            {
                targetTable = "api.ExecutionAsset";
				assetTypeId = Connection.QueryFirst<int>("Select ID from AssetType where Object = @obj and ObjectID = @objID", new { obj = new DbString { Value = obj, Length = 50, IsAnsi = true }, objID });
				isIntersect = false;
			}

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
														where 
															{(isIntersect ? $"FT.IntersectTypeID = @objID" : "FT.AssetTypeID= @assetTypeId")}															
															and 
															[Type] = 'Lookup' 
															and 
															F.FieldValue is not null 
															and 
															(A.Id is null and reftype.id is null and ModelType.id is null) 
															and 
															(try_cast(CONVERT(NVARCHAR(20), val.Value) as int) <> 0 or try_cast(CONVERT(NVARCHAR(20), val.Value) as int) IS NULL)
														) 
														S on S.ExecutionID = T.ExecutionID and S.ItemNumber = T.ItemNumber;
									", new { executionID, objID, assetTypeId }, commandTimeout: timeout);
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
																	inner join FieldType FT on {(isIntersect ? $"FT.IntersectTypeID = @objID" : "FT.AssetTypeID= @assetTypeId")}
																								and FT.[Type] = 'Lookup'
																	inner join {ApiExecutionFieldTable} F on F.ExecutionID = A.ExecutionID and F.ItemNumber = A.ItemNumber and F.FieldTypeID = FT.ID and F.LookupValue is null and (F.FieldValue != '' or FT.IsRequired = 1)
														where       A.ExecutionID = @executionID
														group by	A.ExecutionID, A.ItemNumber
														) S on S.ExecutionID = T.ExecutionID and S.ItemNumber = T.ItemNumber;
									", new { executionID, objID, assetTypeId }, commandTimeout: timeout);
            }
        }

        private void LogRelationshipErrors(Guid executionID, string obj, int objID, string errorPrefix, int timeout = 3600, bool lookupFieldsPassedByValue = false)
        {
            string targetTable = (obj != "IntersectType") ? "api.ExecutionAsset" : "api.ExecutionRelationship";
            string assetJoin = lookupFieldsPassedByValue ? "AD.ObjectID = try_cast(V.[value] as int)" : "Cast(AD.DisplayValue as nvarchar(4000)) = V.[value]";
			string assetrefJoin = lookupFieldsPassedByValue ? "att.ObjectID = try_cast(V.[value] as int)" : "att.Name = V.[value]";

			string sql = $@"

						drop table if exists #tempdata;
						drop table if exists #tempfinaldata;

						select  A.ExecutionID,               
						A.ItemNumber,               
						FT.NAME FTNAME,
						F.FieldValue,
						F.FieldTypeID,
						v.value [Value],
						IT.ObjectAssetTypeID,
						IT.SubjectAssetTypeID,
						IT.ObjectClass,
						IT.SubjectClass,
						0 ISFound
						into #tempdata
						from  {targetTable} A               
						inner join FieldType FT on FT.IntersectTypeID = @objID and FT.[Type] = 'Relationship'              
						inner join {ApiExecutionFieldTable} F on F.ExecutionID = A.ExecutionID and F.ItemNumber = A.ItemNumber and F.FieldTypeID = FT.ID and F.LookupValue is null 
									and (F.FieldValue != '' or FT.IsRequired = 1)               
						cross apply string_split(F.FieldValue, ',') V                                           
						inner join IntersectType IT on IT.ID = FT.LookupObjectID
						where A.ExecutionID = @executionID;
					
						update V
						set isfound = 1
						from #tempdata V
						inner join AssetDetail ad on AD.[AssetTypeID] = V.ObjectAssetTypeID and {assetJoin} 
						where isfound = 0;

						update V
						set isfound = 2
						from #tempdata V
						inner join AssetDetail ad on AD.[AssetTypeID] = V.SubjectAssetTypeID and {assetJoin} 
						where isfound = 0;

						if exists(Select 1 from #tempdata t where t.ObjectClass = {(int)AssetTypeClass.Reference}  and isfound = 0)
						begin
							update V
							set isfound = 3
							from #tempdata V
							inner join AssetType att on att.[Class] = {(int)AssetTypeClass.Reference} and att.[ObjectID] <> 0 and {assetrefJoin}
							where isfound = 0 and V.ObjectAssetTypeID = 0 and V.ObjectClass = {(int)AssetTypeClass.Reference};
						end

						if exists(Select 1 from #tempdata t where t.SubjectClass = {(int)AssetTypeClass.Reference} and isfound = 0)
						begin
							update V
							set isfound = 4
							from #tempdata V
							inner join AssetType att on att.[Class] = {(int)AssetTypeClass.Reference} and att.[ObjectID] <> 0 and {assetrefJoin}
							where isfound = 0 and V.SubjectAssetTypeID = 0 and V.SubjectClass = {(int)AssetTypeClass.Reference};
						end;

						WITH RS_DATA AS(SELECT ExecutionID,ItemNumber,FTName,MAX(FieldValue) FieldValue,MIN(isfound) isfound 
										FROM #tempdata GROUP BY ExecutionID,ItemNumber,FTName)
						select  S.ExecutionID,
						S.ItemNumber,
						MIN(S.isfound) isfound,
						STRING_AGG(S.FTName+'='+left(S.FieldValue,250), ', ') as Names    
						into #tempfinaldata        
						from  RS_DATA S 
						group by S.ExecutionID,S.ItemNumber;

						delete from #tempfinaldata where isfound > 0

						create index ix_tempfinaldata on #tempfinaldata (ExecutionID,ItemNumber);

						update	T
						set		T.Success = 0,
								T.[Message] = coalesce(T.[Message] + '; ', '') + '{errorPrefix} contains one or more fields with invalid relationship values: [' + S.Names + ']'
						from	{targetTable} T
								inner join	#tempfinaldata S on S.ExecutionID = T.ExecutionID and S.ItemNumber = T.ItemNumber;
					    ";

            Connection.Execute(sql, new { executionID, obj = new DbString { Value = obj, Length = 50, IsAnsi = true }, objID }, commandTimeout: timeout);
        }

        private void LogLoopExecutionError(Guid executionID, int beginItemNumber, int endItemNumber, string targetTable, string msg, int timeout = 3600)
        {
            int characterLimit = constants.ERROR_MESSAGE_CHARACTER_LIMIT;
            Connection.Execute($@"
								update	api.Execution
								set		[ErrorMessage] = LEFT(coalesce([ErrorMessage],'') + @msg,@characterLimit)
								where	ExecutionID = @executionID; 

								update	{targetTable} 
								set		Success = 0,
										[Message] = @msg
								where	ExecutionID = @executionID 
										 and ItemNumber between @beginItemNumber and @endItemNumber;",
         new { executionID, msg, beginItemNumber, endItemNumber, characterLimit }, commandTimeout: timeout);
        }
        
        private void MergeJsonFieldProperties(Guid executionID, SqlTransaction trans, List<FieldTypeCore> jsonFieldTypes, SystemObjects objectType, string tableName, string IdSqlSyntax, int beginItemNumber, int endItemNumber, int timeout = 3600, Dictionary<string, double> metrics = null, int step = 0, bool isInsert = false)
        {
			var fieldIdSQL = $" and F.AssetID = {IdSqlSyntax}";

			if (objectType == SystemObjects.Intersect)
			{
				fieldIdSQL = $" and F.IntersectID = {IdSqlSyntax}";
			}

			if (objectType == SystemObjects.Issue)
			{
				fieldIdSQL = $" and F.IssueID = {IdSqlSyntax}";
			}

			Stopwatch sw = Stopwatch.StartNew();
            string jsonFieldTypeIDs = string.Join(",", jsonFieldTypes.Select(i => i.ID));
            IEnumerable<dynamic> fields = Connection.Query<dynamic>($@"
					select  F.ID, 
							F.FormattedValue 
					from    Field F 
							inner join {ApiExecutionFieldTable} E on E.ExecutionID = @executionID and E.ItemNumber between @beginItemNumber and @endItemNumber and E.FieldTypeID = F.FieldTypeID and E.FieldTypeID in ({jsonFieldTypeIDs})
							inner join {tableName} A on A.ExecutionID = E.ExecutionID and A.ItemNumber = E.ItemNumber {fieldIdSQL}",
                            new { executionID, beginItemNumber, endItemNumber }, transaction: trans, commandTimeout: timeout);

            if (metrics != null)
            {
                addMeasurement(metrics, $"MergeJsonFieldProperties >> loadfields", sw.ElapsedMilliseconds, ++step);
            }

            sw.Restart();

            //check for 0 fields to update case which often happens when editing from ui since you cant edit json fields.
            if (!fields.Any())
            {
                return;
            }

            List<FieldJsonProperty> collectionFieldProperties = new List<FieldJsonProperty>();

            foreach (dynamic f in fields)
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

            if (metrics != null)
            {
                addMeasurement(metrics, $"MergeJsonFieldProperties >> iterate properties", sw.ElapsedMilliseconds, ++step);
            }

            sw.Restart();

            //delete old json field values if this is not a POST
            if (!isInsert)
            {
                Connection.Execute($@"
					delete from FieldJsonProperty where fieldid in(
					select  F.ID
					from    Field F 
							inner join {ApiExecutionFieldTable} E on E.ExecutionID = @executionID and E.ItemNumber between @beginItemNumber and @endItemNumber and E.FieldTypeID = F.FieldTypeID and E.FieldTypeID in ({jsonFieldTypeIDs})
							inner join {tableName} A on A.ExecutionID = E.ExecutionID and A.ItemNumber = E.ItemNumber {fieldIdSQL})",
                            new { executionID, beginItemNumber, endItemNumber }, transaction: trans, commandTimeout: timeout);
            }

            if (metrics != null)
            {
                addMeasurement(metrics, $"MergeJsonFieldProperties >> delete old values", sw.ElapsedMilliseconds, ++step);
            }

            sw.Restart();

            #region Build data tables for bulk load.

            DataTable table = new DataTable();
            table.Columns.Add("FieldID", typeof(long));
            table.Columns.Add("Name", typeof(string));
            table.Columns.Add("Parent", typeof(string));
            table.Columns.Add("Path", typeof(string));
            table.Columns.Add("Position", typeof(int));
            table.Columns.Add("IsArray", typeof(bool));
            table.Columns.Add("Value", typeof(string));
            table.Columns.Add("CreatedBy", typeof(int));
            table.Columns.Add("UpdatedBy", typeof(int));

            foreach (FieldJsonProperty f in collectionFieldProperties)
            {
                DataRow row = table.NewRow();

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

            using (SqlBulkCopy bulkCopy = new SqlBulkCopy((SqlConnection)Database.Connection, SqlBulkCopyOptions.TableLock, trans)
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

            if (metrics != null)
            {
                addMeasurement(metrics, $"MergeJsonFieldProperties >> bulk load values", sw.ElapsedMilliseconds, ++step);
            }

            sw.Restart();

            #endregion
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
		
		private bool TypeHasProcessRelationshipTypes(AssetType at)
        {
            return Database.Connection.QuerySingle<bool>(@"select iif(count(*) = 0, 0, 1) from IntersectTypeDetail where SubjectAssetTypeID = @ID or ObjectAssetTypeID = @ID and PredicateType = 15", new { at.ID });
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
            {
                return Database.Connection.QuerySingle<int>("SELECT ISNULL((select count(1) from workflow.EventRegistration where [object] = @obj and [objectid] = @objId and [state] = 1 and [changetype] = @change), 0)", new { obj = new DbString { Value = @object, IsFixedLength = true, Length = 50, IsAnsi = true }, objId = objectID, change = changeType.Value }) > 0;
            }

            return Database.Connection.QuerySingle<int>("SELECT ISNULL((select count(1) from workflow.EventRegistration where [object] = @obj and [objectid] = @objId and [state] = 1 ), 0)", new { obj = new DbString { Value = @object, IsFixedLength = true, Length = 50, IsAnsi = true }, objId = objectID }) > 0;
        }

		private bool TypeHasWorkflows(int? AssetTypeID, int? IntersectTypeID, int? IssueTypeID, ChangeType? changeType)
		{
			if (changeType.HasValue)
			{
				if (AssetTypeID.HasValue)
				{
					return Database.Connection.QuerySingle<int>("SELECT ISNULL((select count(1) from workflow.EventRegistration where [AssetTypeId] = @AssetTypeID and [state] = 1 and [changetype] = @change), 0)", new { AssetTypeID = AssetTypeID, change = changeType.Value }) > 0;
				}
				else if (IntersectTypeID.HasValue)
				{
					return Database.Connection.QuerySingle<int>("SELECT ISNULL((select count(1) from workflow.EventRegistration where [IntersectTypeID] = @IntersectTypeID and [state] = 1 and [changetype] = @change), 0)", new { IntersectTypeID = IntersectTypeID, change = changeType.Value }) > 0;
				}
				else if (IssueTypeID.HasValue)
				{
					return Database.Connection.QuerySingle<int>("SELECT ISNULL((select count(1) from workflow.EventRegistration where [IssueTypeID] = @IssueTypeID and [state] = 1 and [changetype] = @change), 0)", new { IssueTypeID = IssueTypeID, change = changeType.Value }) > 0;
				}
				else
				{
					return false;
				}
			}

			if (AssetTypeID.HasValue)
			{
				return Database.Connection.QuerySingle<int>("SELECT ISNULL((select count(1) from workflow.EventRegistration where [AssetTypeId] = @AssetTypeID and [state] = 1 ), 0)", new { AssetTypeID = AssetTypeID }) > 0;
			}
			else if (IntersectTypeID.HasValue)
			{
				return Database.Connection.QuerySingle<int>("SELECT ISNULL((select count(1) from workflow.EventRegistration where [IntersectTypeID] = @IntersectTypeID and [state] = 1), 0)", new { IntersectTypeID = IntersectTypeID }) > 0;
			}
			else if (IssueTypeID.HasValue)
			{
				return Database.Connection.QuerySingle<int>("SELECT ISNULL((select count(1) from workflow.EventRegistration where [IssueTypeID] = @IssueTypeID and [state] = 1), 0)", new { IssueTypeID = IssueTypeID }) > 0;
			}
			else
			{
				return false;
			}
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
								set     T.ParentAssetID = S.ID
								from    api.ExecutionAsset T
										inner join Asset S on T.ExecutionID = @executionID and S.Uid = T.ParentUid and T.ParentUid is not null
										inner join AssetType ST on ST.ID = S.AssetTypeID and ST.ID = T.ParentAssetTypeID;",
            new { executionID, assetTypeID }, commandTimeout: timeout);
        }

		private void ValidateDeleteRelationshipTypes(ApiExecution execution, int timeout = 3600)
		{
			List<PredicateTypeInfo> predicateTypeInfo = new PredicateType().GetAsList();
			List<int> disallowEditIds = predicateTypeInfo.Where(p => p.AllowEditFromRelationshipEditor == false).Select(p => (int)p.ID).ToList();

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
							inner join IntersectTypeDetail it on it.uid = ER.Uid
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
			List<PredicateTypeInfo> predicateTypeInfo = new PredicateType().GetAsList();
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
													(I.SubjectCardinality = 1 and ER.SubjectCardinality <> 1 and I.ID in (select LookupObjectID from FieldType where LookupObjectType = 'IntersectType' and AssetTypeID = I.ObjectAssetTypeID and [Type] = 'FieldFromRelationship')) 
													or (I.ObjectCardinality = 1 and ER.ObjectCardinality <> 1  and I.ID in (select LookupObjectID from FieldType where LookupObjectType = 'IntersectType' and AssetTypeID = I.SubjectAssetTypeID and [Type] = 'FieldFromRelationship')) 
													)
												and  ER.ExecutionID = @ExecutionID 
												and ER.Success is null;

									Update  T
									set     SubjectClass = SA.[Class], 
											SubjectAssetTypeID = CASE WHEN SA.Class = 9 and SA.ObjectID = 0 then 0 else SA.ID end
									from    [api].[ExecutionRelationshipType] T
											inner join IntersectType S on S.Uid = T.Uid
											inner join AssetType SA on SA.Uid = T.SubjectUid
									where   T.ExecutionID = @ExecutionID and T.Success is null;

									Update  T
									set     ObjectClass = OA.[Class], 
											ObjectAssetTypeID = CASE WHEN OA.Class = 9 and OA.ObjectID = 0 then 0 else OA.ID end
									from    [api].[ExecutionRelationshipType] T
											inner join IntersectType S on S.Uid = T.Uid
											inner join AssetType OA on OA.Uid = T.ObjectUid
									where   T.ExecutionID = @ExecutionID and T.Success is null;

									update  ER 
									set     Success = 0,
											Message = 'Cannot change SubjectUid or ObjectUid for a relationship type that already has relationships.' 
									from    [api].[ExecutionRelationshipType] ER 
											inner join IntersectType T on T.Uid = ER.Uid
									where   ER.ExecutionID = @ExecutionID 
											and ER.Success is null 
											and (ER.SubjectUid is not null or ER.ObjectUid is not null)
											and exists (select 1 from [Intersect] where IntersectTypeId = T.ID);",
				new { execution.ExecutionID, emptyUid }, commandTimeout: timeout);
			}

			#region Insert/Update

			string predicateCheckSql = "";
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

			Connection.Execute($@"
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
								set     ER.SubjectAssetTypeID = CASE WHEN AST.Class = 9 and AST.ObjectID = 0 then 0 else AST.ID end,
										ER.SubjectClass = AST.Class
								from    [api].[ExecutionRelationshipType] ER 
										inner join AssetType AST on AST.UID = ER.SubjectUID 
								where   ER.ExecutionID = @ExecutionID and ER.Success is null;

								Update  ER 
								set     ER.ObjectAssetTypeID = CASE WHEN AST.Class = 9 and AST.ObjectID = 0 then 0 else AST.ID end,
										ER.ObjectClass = AST.Class 
								from    [api].[ExecutionRelationshipType] ER 
										inner join AssetType AST on AST.UID = ER.ObjectUID 
								where   ER.ExecutionID = @ExecutionID and ER.Success is null;

								Update  ER 
								set     Success = 0, 
										Message = 'Relationship Type not allowed because SubjectUid and ObjectUid both are same and associated with Reference List.' 
								from    [api].[ExecutionRelationshipType] ER 
										inner join AssetType AST on AST.UID = ER.ObjectUID and AST.Class = 9
										inner join AssetType ASTS on ASTS.UID = ER.SubjectUID and ASTS.Class = 9
								where   ER.ExecutionID = @ExecutionID and ER.Success is null and ER.ObjectUID = ER.SubjectUID;

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
											and (SubjectUid is not null or SubjectUid <> @emptyUid)
											and SubjectAssetTypeID is null;

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
											and (ObjectUid is not null or ObjectUid <> @emptyUid)
											and ObjectAssetTypeID is null;

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
												from    IntersectTypeDetail 
												where   SubjectUid = ER.SubjectUid 
														and ObjectUid = ER.ObjectUid 
														and PredicateID = ER.PredicateID
											);", new { execution.ExecutionID, emptyUid }, commandTimeout: timeout);
			}
			else
			{
				Connection.Execute(@"
update  ER 
set     Success = 0, 
		Message = 'Relationship type with the specified predicate already exists.' 
from    [api].[ExecutionRelationshipType] ER 
		inner join IntersectType IT on IT.UID = ER.UID 
where   ER.ExecutionID = @ExecutionID 
		and ER.Success is null 
		and exists (
			select  1 
			from    IntersectType I 
					inner join Predicate P on P.ID = I.PredicateID 
			where   P.Uid = ER.PredicateUid 
					and I.SubjectAssetTypeID = IT.SubjectAssetTypeID 
					and I.ObjectAssetTypeID = IT.ObjectAssetTypeID 
					and I.Uid != IT.Uid
		);",
					new { execution.ExecutionID }, commandTimeout: timeout);
			}
		}

		#endregion


		#region Methods

		public void CalculateProposedKeyHashesBulkLoad(AssetType at, Guid executionID, int timeout = 3600, int? parentIntersectTypeId = null, SqlTransaction trans = null, string assetTable = "api.ExecutionAsset", string fieldTable = "api.ExecutionField")
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

			string shouldCheckHashStatement = $@"
						declare @hasUpdatedKeyFields bit = 0
						if exists (
							select a.AssetID, f.FieldValue, fl.FormattedValue from {assetTable} A
							 inner join {fieldTable} F on F.ExecutionID = A.ExecutionID and F.ItemNumber = A.ItemNumber
							 inner join FieldType FT on FT.AssetTypeID = @ID and FT.ID = F.FieldTypeID and FT.IsPartOfKey = 1
							 left join Field fl on fl.AssetId = a.assetid and fl.fieldtypeid = ft.id
							where A.executionid = @executionid and (f.fieldvalue <> fl.formattedvalue or fl.id is null)
						)
						set @hasUpdatedKeyFields = 1 
						else
						set @hasUpdatedKeyFields = 0";

			if (at.Object == "ReferenceItemType")
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
				if (parentIntersectTypeId.HasValue)
				{
					string CreateFieldTempData = $@"
													drop table if exists #Keys;
													CREATE TABLE #Keys (AssetID bigint, ParentAssetUID uniqueidentifier null, 
																		KeyValue nvarchar(max) null, ActiveKey varchar(32) null);

													{shouldCheckHashStatement}

													insert into #Keys WITH(TABLOCK)
													select		A.ID, P.UID as ParentAssetUID, Null, Null
													from		Asset A 
															left join [Intersect] I on I.IntersectTypeID = @intersectTypeID and I.ObjectAssetId = A.Id
															left join Asset P on P.Id = I.SubjectAssetId
													where		A.AssetTypeID = @ID and @hasUpdatedKeyFields = 1;

													create clustered index idx_key_assetid on #keys(AssetID);

													if (select count(1) from fieldtype ft 
														inner join assettype att on att.id = ft.AssetTypeID 
														where ft.IsPartOfKey = 1 and ft.AssetTypeID = @ID and ft.DefaultValue is null 
														and replace(replace(att.DisplayFormat,'}}',''),'{{','') = ft.Name) = 1
														and (select count(1) from fieldtype ft 
														inner join assettype att on att.id = ft.AssetTypeID 
														where ft.IsPartOfKey = 1 and ft.AssetTypeID = @ID) = 1

														begin
															-- display value is the key field and its the only key field and required...	
															update T
															set T.KeyValue = cast(@ID as nvarchar(50)) + '|' + COALESCE(cast(T.ParentAssetUid as nvarchar(50))+'|', '') + ADV.DisplayValue
															from 
																#Keys T		
																inner join AssetDisplayValue ADV on ADV.AssetID = T.AssetID
														end
													else if (select count(1) from fieldtype where IsPartOfKey = 1 and Assettypeid = @ID) = 1
														begin
															--only key field and required
			
															select @fieldtypeid = id,
																   @DefaultValue = DefaultValue
															from fieldtype
															where assettypeid = @id and IsPartOfKey = 1;
			
															update T
															set T.KeyValue = cast(@ID as nvarchar(50)) + '|' + COALESCE(cast(T.ParentAssetUid as nvarchar(50))+'|', '') + coalesce(F.Value, F.FormattedValue, @DefaultValue)
															from #Keys T
															left join Field F on F.AssetID = T.AssetID and F.FieldTypeID = @fieldtypeid

														end
													else
														begin
															-- multiple key fields need to agg all the values
															drop table if exists #KeysField;
															CREATE TABLE #KeysField (AssetID bigint,FormattedValue nvarchar(max));
			
															insert into #KeysField WITH(TABLOCK)
															select A.AssetID,STRING_AGG(coalesce(F.Value, F.FormattedValue, FT.DefaultValue), '|') within group (order by FT.ColumnOrder asc, FT.Name asc) FormattedValue
															from #Keys A
															inner join FieldType FT on FT.AssetTypeID = @ID and FT.IsPartOfKey = 1
															left join Field F on FT.ID = F.FieldTypeID and A.AssetID = F.AssetID  
															group by A.AssetID;

															CREATE NONCLUSTERED INDEX CIX_KeysFieldKeys ON #KeysField ( AssetID ASC );
			
															update T
															set T.KeyValue = cast(@ID as nvarchar(50)) + '|' + COALESCE(cast(T.ParentAssetUid as nvarchar(50))+'|', '') + KF.FormattedValue
															from #Keys T
															inner join #KeysField KF on T.AssetID = KF.AssetID;

															drop table if exists #KeysField;
														end

														update #Keys set ActiveKey = utility.GetHash(KeyValue);

														CREATE NONCLUSTERED INDEX CIX_TempApiExecutionKeys ON #Keys ( ActiveKey ASC ); ";


					string keyHashCalulationScript = $@"
										Declare @fieldtypeid int =-1;
										declare @DefaultValue nvarchar(max);

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
														) S on T.ExecutionID = @ExecutionID and S.ItemNumber = T.ItemNumber;";

					if (at.Class == AssetTypeClass.Model)
					{
						//models with more levels comes with multiple name fields with same FieldTypeId
						//we are using BulkExecutionFieldUnique temp table with unique field values, where only Name of asset is used to build hash values
						keyHashCalulationScript = $@"
										drop table if exists #BulkExecutionFieldUnique

										;with unique_item_field as (
											select distinct ItemNumber, FieldTypeID 
											from #BulkExecutionField)
										select Field.* 
										into #BulkExecutionFieldUnique
										from unique_item_field BEF
										outer apply (
											select top 1 * from {fieldTable} BEF2 
											where FieldValue is not null and BEF2.ItemNumber = BEF.ItemNumber and BEF2.FieldTypeID = BEF.FieldTypeID
											order by ColumnIndex desc
											)Field

										Declare @fieldtypeid int =-1;
										declare @DefaultValue nvarchar(max);

										update  T
										set     T.ProposedKey = utility.GetHash(cast(@ID as nvarchar) + '|' + S.ProposedKey) 
										from    {assetTable} T
											inner join	(
														select		A.ItemNumber,
																	COALESCE(cast(A.ParentUid as nvarchar(50))+'|', '') + STRING_AGG(coalesce(F.LookupValue, F.FieldValue), '|') within group (order by FT.ColumnOrder asc, FT.Name asc) as ProposedKey
														from		{assetTable} A
																	inner join #BulkExecutionFieldUnique F on F.ExecutionID = A.ExecutionID and F.ItemNumber = A.ItemNumber
																	inner join FieldType FT on FT.AssetTypeID = @ID and FT.ID = F.FieldTypeID and FT.IsPartOfKey = 1
														where		A.ExecutionID = @ExecutionID
														group by	A.ItemNumber, A.ParentUid
														) S on T.ExecutionID = @ExecutionID and S.ItemNumber = T.ItemNumber;";
					}

					string SqlStmt = $@"
										{keyHashCalulationScript}

										{CreateFieldTempData}

										{keyComparisonUpdateStatement}";
					Connection.Execute(SqlStmt, new { executionID, at.ID, intersectTypeID = parentIntersectTypeId ?? 0 }, commandTimeout: timeout, transaction: trans);
				}
				else
				{
					string activeKeySql = $@"
											select		A.ID,
													utility.GetHash(cast(@ID as nvarchar) + '|' + STRING_AGG(coalesce((case when ft.type <> 'Counter' then F.Value else isnull(cast(FCV.Value as nvarchar(50)),newid()) end), F.FormattedValue, FT.DefaultValue), '|') within group (order by FT.ColumnOrder asc, FT.Name asc)) as ActiveKey 
											from		Asset A 
													inner join FieldType FT on FT.AssetTypeID = A.AssetTypeID and FT.IsPartOfKey = 1
													left join Field F on FT.ID = F.FieldTypeID and F.AssetID = A.ID
													left join FieldCounterValue FCV on FT.Type = 'Counter' and FCV.FieldTypeId = FT.ID and FCV.AssetId = F.AssetId
											where	    A.AssetTypeID = @ID and @hasUpdatedKeyFields = 1
											group by    A.ID;";

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

											{shouldCheckHashStatement}

											insert into #Keys WITH(TABLOCK)
											{activeKeySql} 

											{keyComparisonUpdateStatement}",
					new { executionID, at.ID, intersectTypeID = parentIntersectTypeId ?? 0 }, commandTimeout: timeout, transaction: trans);
				}
			}
		}

		public List<DatabaseBulkRelationshipResult> DeleteRelationships(ApiExecution execution, IntersectType it, RelationshipDeletes import, int timeout = 3600, bool sendWorkflowEvents = false, bool sendGraphEvents = true)
		{
			List<DatabaseBulkRelationshipResult> results = new List<DatabaseBulkRelationshipResult>();
			bool generalChecksCompleted = false;
			CurrentExecutionLocationModel currentLocation = null;

			SetApiExecutionProcessingStartTime(execution.ExecutionID);

			//check if trigger workflows is set to true and there are actually no workflows in which case shut off triggering of workflows
			sendWorkflowEvents = sendWorkflowEvents && TypeHasWorkflows(null, it.ID, null, ChangeType.Delete);

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

				DataTable table = new DataTable();
				table.Columns.Add("ExecutionID", typeof(Guid));
				table.Columns.Add("ItemNumber", typeof(int));
				table.Columns.Add("ExecutionItemUid", typeof(Guid));
				table.Columns.Add("Uid", typeof(Guid));
				table.Columns.Add("Cascade", typeof(bool));

				#endregion

				#region Generate data sets

				for (int i = currentLocation.HighestItemNumber + 1; i <= import.Count; i++)
				{
					RelationshipDelete model = import[i - 1];

					DataRow row = table.NewRow();

					row["ExecutionID"] = execution.ExecutionID;
					row["ItemNumber"] = i;
					row["Uid"] = model.Uid;
					row["Cascade"] = model.Cascade;
					if (model.ExecutionItemUid.HasValue)
					{
						row["ExecutionItemUid"] = model.ExecutionItemUid.Value;
					}

					table.Rows.Add(row);
				}

				#endregion

				if (Database.Connection.State != ConnectionState.Open)
				{
					Connection.Open();
				}

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
									set		T.SubjectID = I.SubjectAssetID,
											T.ObjectID = I.ObjectAssetID
									from	api.ExecutionDeletedRelationship T
											inner join [Intersect] I on I.ID = T.IntersectID
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
															inner join Asset A on A.ID = I.SubjectAssetID 
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
															inner join Asset A on A.ID = I.ObjectAssetID 
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

				generalChecksCompleted = true;
			}
			catch (Exception generalEx)
			{
				generalChecksCompleted = false;
				string msg = generalEx.GetFullExceptionData(false, constants.ERROR_MESSAGE_CHARACTER_LIMIT);
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
						using (SqlTransaction trans = Connection.BeginTransaction())
						{
							try
							{
								#region Field table delete

								Connection.Execute($@"
													delete  T
													from    [Field] T
														inner join api.ExecutionDeletedRelationship S on T.IntersectId = S.IntersectID
															and S.ExecutionID = @ExecutionID 
															and S.ItemNumber between @beginItemNumber and @endItemNumber
															and S.Success is null
														where T.IntersectID is not null;",
															new { execution.ExecutionID, beginItemNumber, endItemNumber }, transaction: trans, commandTimeout: timeout);

								#endregion

								#region Audit

								string auditSql = @"
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
																'Relationship', 
																'This relationship has been removed.' 
														from	[Intersect] I
																inner join AssetDetail A on {0}
																cross apply dbo.getIntersectTypeNames(I.IntersectTypeID) TName
																inner join api.ExecutionDeletedRelationship S on S.IntersectID = I.ID 
																	and S.ExecutionID = @executionID 
																	and S.ItemNumber between @beginItemNumber and @endItemNumber 
																	and S.Success is null;";

								Connection.Execute(string.Format(auditSql, "A.ID = I.SubjectAssetID"), new { execution.ExecutionID, r = CurrentResourceID, dt = DateTime.UtcNow, beginItemNumber, endItemNumber }, transaction: trans, commandTimeout: timeout);
								Connection.Execute(string.Format(auditSql, "A.ID = I.ObjectAssetID"), new { execution.ExecutionID, r = CurrentResourceID, dt = DateTime.UtcNow, beginItemNumber, endItemNumber }, transaction: trans, commandTimeout: timeout);

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

					beginItemNumber += loopSize;
					endItemNumber += loopSize;
				}

				completeApiExecutionAndGetCounts(execution.ExecutionID, "ExecutionDeletedRelationship");
				Connection.Close();

				if (sendWorkflowEvents)
				{
					SendWorkflowEvents("IntersectType", it.ID, results, ChangeType.Delete);
				}
				CreateDeleteRelationshipsExecution(execution.ExecutionID, it.ID);
			}

			return results;
		}

		public List<RelationshipTypeResult> DeleteRelationshipTypes(ApiExecution execution, IEnumerable<RelationshipTypeDelete> import, int timeout = 3600)
        {
            List<RelationshipTypeResult> results = new List<RelationshipTypeResult>();

            SetApiExecutionProcessingStartTime(execution.ExecutionID);

            var dupes = import.Where(i => i.ExecutionItemUid.HasValue && i.ExecutionItemUid.Value != Guid.Empty).GroupBy(i => i.ExecutionItemUid).Where(i => i.Count() > 1).Select(i => new { ExecutionItemUid = i.Key, Count = i.Count() }).ToList();
            if (dupes.Any())
            {
                string message = $"Duplicate execution item identifiers: {string.Join(", ", dupes.Select(i => i.ExecutionItemUid.ToString()))}. Identifiers must be unique within a batch.";
                execution.ErrorMessage = message.Substring(0, Math.Min(constants.ERROR_MESSAGE_CHARACTER_LIMIT, message.Length));
                results.AddRange(import.Select(i => new RelationshipTypeResult { ExecutionItemUid = i.ExecutionItemUid, Message = execution.ErrorMessage, Success = false }));
            }
            else
            {
                #region Build data tables for bulk load.

                DataTable table = new DataTable();
                table.Columns.Add("ExecutionID", typeof(Guid));
                table.Columns.Add("ExecutionItemUid", typeof(Guid));
                table.Columns.Add("ItemNumber", typeof(int));
                table.Columns.Add("uid", typeof(Guid));
                table.Columns.Add("Cascade", typeof(bool));
                table.Columns.Add("Message", typeof(string));
                table.Columns.Add("Success", typeof(bool));

                int i = 0;
                foreach (RelationshipTypeDelete item in import)
                {
                    DataRow row = table.NewRow();

                    row["ExecutionID"] = execution.ExecutionID;
                    row["ItemNumber"] = i++;
                    if (item.ExecutionItemUid.HasValue)
                    {
                        row["ExecutionItemUid"] = item.ExecutionItemUid.Value;
                    }

                    row["uid"] = item.Uid;
                    row["Cascade"] = item.Cascade;
                    table.Rows.Add(row);
                }

                #endregion

                try
                {
                    if (Database.Connection.State != ConnectionState.Open)
                    {
                        Connection.Open();
                    }

                    #region Bulk Copy

                    using (SqlBulkCopy bulkCopy = new SqlBulkCopy(Connection)
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

                    ValidateDeleteRelationshipTypes(execution, timeout);

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

                    List<Guid> impactedMeasureVersions = new List<Guid>();
                    List<int> intersectTypeIds = Query<int>("select ID from IntersectType where Uid in @Uids", new { Uids = import.Select(imp => imp.Uid) }).ToList();
                    intersectTypeIds.ForEach(it =>
                    {
                        List<Guid> impacted = GetImpactedMeasureVersionsBy(MetricGovernanceCheckType.Relation, it);
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
									and ER.Success is null) S on S.ID = T.IntersectID ;

							delete FT
							from    [FieldType] FT
									inner join (Select I.ID from [intersecttype] I
									inner join api.ExecutionDeletedRelationshipType ER on ER.UID = I.UID 
									where ER.ExecutionID = @ExecutionID 
									and ER.Success is null) S on S.ID = FT.IntersectTypeID ;

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
                    {
                        Connection.Close();
                    }
                }
            }

            return results;
        }

		public List<FieldTypeCore> GetAssetTypeFieldTypesCore(string obj, int objectID)
		{
			var joinCondition = @"Inner Join 
								  AssetType AST on FT.AssetTypeID = AST.ID";

			var whereCondition = "where AST.[Object] = @Obj and AST.[ObjectID] = @ObjectID";

			if (obj == SystemObjects.IssueType.ToString() || obj == SystemObjects.Issue.ToString())
			{
				if (obj == SystemObjects.Issue.ToString())
				{
					joinCondition = @"Inner Join 
										  Issue I on I.IssueTypeID = FT.IssueTypeID";

					whereCondition = "where I.ID = @ObjectID";
				}

				if (obj == SystemObjects.IssueType.ToString())
				{
					joinCondition = @"Inner Join 
										  IssueType IT on IT.ID = FT.IssueTypeID";

					whereCondition = "where IT.ID = @ObjectID";
				}
			}
			else if (obj == SystemObjects.Intersect.ToString() || obj == SystemObjects.IntersectType.ToString())
			{
				if (obj == SystemObjects.Intersect.ToString())
				{
					joinCondition = @"Inner Join 
										  Intersect I on I.IntersectTypeID = FT.IntersectTypeID";

					whereCondition = "where I.ID = @ObjectID";
				}

				if (obj == SystemObjects.IntersectType.ToString())
				{
					joinCondition = @"Inner Join 
										  IntersectType IT on IT.ID = FT.IntersectTypeID";

					whereCondition = "where IT.ID = @ObjectID";
				}
			}

			string fieldTypeSql = @$"
									SELECT [Type]
										  ,[IsRequired]
										  ,CASE WHEN ([DefaultValue] IS NULL or [DefaultValue] = '') THEN 0 ELSE 1 END as [HasDefaultValue]
										  ,FT.[Name]
										  ,[FriendlyName]
										  ,FT.[ID]
										  ,[AllowMultipleValues]
										  ,[Pattern]
										  ,[Length]
										  ,[MinimumLength]
										  ,[MaximumLength]
									FROM [dbo].[FieldType] FT
										{joinCondition}
										{whereCondition}";

			return Query<FieldTypeCore>(fieldTypeSql, new { @Obj = new DbString { Value = obj, IsFixedLength = true, Length = 50, IsAnsi = true }, objectID }).ToList();
		}

		public async Task<List<IntersectTypeApiViewModel>> GetRelationshipTypes(IEnumerable<KeyValuePair<string, string>> queryParams, string whereClause = "", string keyword = null, int? id = null, string subject = null, string predicate = null, string @object = null)
        {
            DynamicParameters dbArgs = new DynamicParameters();

            if (queryParams != null)
            {
                if (queryParams.ToList().Any(q => q.Key.ToLower() == "predicateuid"))
                {
                    string predicateUidString = queryParams.ToList().FirstOrDefault(q => q.Key.ToLower() == "predicateuid").Value;

                    if (Guid.TryParse(predicateUidString, out Guid predicateUid))
                    {
                        dbArgs.Add("@predicateUid", predicateUid);
                        whereClause += (string.IsNullOrEmpty(whereClause) ? " where" : " and") + $" PredicateUid = @predicateUid";
                    }
                }

                if (queryParams.ToList().Any(q => q.Key.ToLower() == "assettypeuid"))
                {
                    string assetTypeUidString = queryParams.ToList().FirstOrDefault(q => q.Key.ToLower() == "assettypeuid").Value;

                    if (Guid.TryParse(assetTypeUidString, out Guid assetTypeUid))
                    {
                        dbArgs.Add("@assettypeuid", assetTypeUid);
                        whereClause += (string.IsNullOrEmpty(whereClause) ? " where" : " and") + $" (SubjectUid = @assettypeuid OR ObjectUid = @assettypeuid)";
                    }
                }

                if (queryParams.ToList().Any(q => q.Key.ToLower() == "state"))
                {
                    string stateString = queryParams.ToList().FirstOrDefault(q => q.Key.ToLower() == "state").Value;

                    if (Enum.TryParse(stateString, out State state))
                    {
                        dbArgs.Add("@state", state);
                        whereClause += (string.IsNullOrEmpty(whereClause) ? " where" : " and") + $" State = @state";
                    }
                }
            }

            string sql = $@"
							select	Id,
									SourceID,
									Uid,
									State,
									coalesce(IsSystem, 0) as IsSystem,
									PredicateUid as 'Predicate.Uid',
									coalesce(PredicateType,0) as 'Predicate.Type',
									coalesce(PredicateName,'') as 'Predicate.Name',
									coalesce(PredicateInverse,'') as 'Predicate.Inverse',
									SubjectUid as 'Subject.Uid',
									SubjectAssetTypePath as 'Subject.Name',
									SubjectClass as 'Subject.Class',
									SubjectCardinality as 'Subject.Cardinality',
									ObjectUid as 'Object.Uid',
									ObjectAssetTypePath  as 'Object.Name',
									ObjectClass as 'Object.Class',
									ObjectCardinality as 'Object.Cardinality'
							from	IntersectTypeDetail
							{whereClause} for json path";

            List<IntersectTypeApiViewModel> models = await GetDatabaseJsonAsObjectAsync<List<IntersectTypeApiViewModel>>(sql, dbArgs);
            if (models != null)
            {
                // in-memory filter
                if (string.IsNullOrEmpty(keyword) == false
                    || id.HasValue
                    || string.IsNullOrEmpty(subject)
                    || string.IsNullOrEmpty(predicate)
                    || string.IsNullOrEmpty(@object))
                {
                    models = models.Where(x => FilterIntersectTypeApiViewModel(x, keyword, id, subject, predicate, @object)).ToList();
                }
            }

            return models;
        }

		public void ImportRelationships(Guid executionID, SqlTransaction trans, string tableName, string objectSqlSyntax, string objectIdSqlSyntax, int beginItemNumber, int endItemNumber, int timeout = 3600, bool resolveRelationshipOnObjectId = false, bool sendGraphEvents = true)
        {

            string assetJoin = resolveRelationshipOnObjectId ? "AD.ObjectID = try_cast(V.[value] as int)" : "AD.DisplayValue = V.[value]";
			string assetrefJoin = resolveRelationshipOnObjectId ? "att.ObjectID = try_cast(V.[value] as int)" : "att.Name = V.[value]";

			string sql = $@"
				drop table if exists #Relationships;
				create table #Relationships
				(
					ID int,
					[uid] uniqueidentifier,
					IntersectTypeID int,
					SubjectAssetID bigint,
					SubjectAssetTypeID int,
					ObjectAssetID bigint,
					ObjectAssetTypeID int,
					SwitchObject bit
				)

				create index idx_Relationships_id on #Relationships(id);

				drop table if exists #DeletedRelationships;
				create table #DeletedRelationships
				(
					[uid] uniqueidentifier
				)

				create index idx_DeletedRelationships_uid on #DeletedRelationships(uid);

				drop table if exists #tempdata;


				select  distinct 
						A.AssetID as ObjectAssetID,
						OT.ID as ObjectAssetTypeID,
						FT.LookupObjectId as IntersectTypeID,
						Cast(0 as int) as SubjectAssetTypeID,
						Cast(0 as bigint) as SubjectAssetID,
						Cast(0 as bit) as switchObject,
						V.value [Value],
						0 IsFound,
						IT.[ObjectClass] as OBJECTCLASS,
						IT.ObjectAssetTypeID as ITOBJECTASSETTYPEID,
						IT.[SubjectClass] as SUBJECTCLASS,
						IT.SubjectAssetTypeID as ITSUBJECTASSETTYPEID,
						FT.IsSubject
				into #tempdata
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
				where   A.ExecutionID = @executionID
						and A.ItemNumber between @beginItemNumber and @endItemNumber 
						and (F.Ignore = 0 or F.Ignore is null)
						and FT.Type = 'Relationship';

				update V
				set [SubjectAssetID] = AD.[ID],
					SubjectAssetTypeID = AD.AssetTypeID,
					switchObject = 1,
					isfound = 1
				from #tempdata V
				inner join AssetDetail ad on AD.AssetTypeID = V.ITOBJECTASSETTYPEID and {assetJoin} 
				where isfound = 0 and V.ObjectAssetTypeID = V.ITSUBJECTASSETTYPEID and (V.ITSUBJECTASSETTYPEID != V.ITOBJECTASSETTYPEID or (V.ITSUBJECTASSETTYPEID = V.ITOBJECTASSETTYPEID and V.IsSubject=1));

				update V
				set [SubjectAssetID] = AD.[ID],
					SubjectAssetTypeID = AD.AssetTypeID,
					switchObject = 0,
					isfound = 2
				from #tempdata V
				inner join AssetDetail ad on AD.AssetTypeID = V.ITSUBJECTASSETTYPEID and {assetJoin} 
				where isfound = 0 and V.ObjectAssetTypeID = V.ITOBJECTASSETTYPEID and (V.ITSUBJECTASSETTYPEID != V.ITOBJECTASSETTYPEID or (V.ITSUBJECTASSETTYPEID = V.ITOBJECTASSETTYPEID and V.IsSubject=0));

				if exists(Select 1 from #tempdata t where T.OBJECTCLASS = {(int)AssetTypeClass.Reference}  and isfound = 0)
				begin
					update V
					set [SubjectAssetID] = 0,
						SubjectAssetTypeID = att.ID,
						switchObject = 1,
						isfound = 3
					from #tempdata V
					inner join AssetType att on att.[Class] = {(int)AssetTypeClass.Reference} and att.[ObjectID] <> 0 and {assetrefJoin} 
					where isfound = 0 and V.ITOBJECTASSETTYPEID = 0 and V.OBJECTCLASS = {(int)AssetTypeClass.Reference} ;
				end

				if exists(Select 1 from #tempdata t where T.SUBJECTCLASS = {(int)AssetTypeClass.Reference}  and isfound = 0)
				begin
					update V
					set [SubjectAssetID] = 0,
						SubjectAssetTypeID = att.ID,
						switchObject = 0,
						isfound = 4
					from #tempdata V
					inner join AssetType att on att.[Class] = {(int)AssetTypeClass.Reference} and att.[ObjectID] <> 0 and {assetrefJoin} 
					where isfound = 0 and V.ITSUBJECTASSETTYPEID = 0 and V.SUBJECTCLASS = {(int)AssetTypeClass.Reference} ;
				end

				insert into #Relationships WITH(TABLOCK) (ID, [uid], IntersectTypeID, SubjectAssetID, SubjectAssetTypeID, ObjectAssetID, ObjectAssetTypeID, SwitchObject)
				select
					null as ID,
					null as [uid],
					IntersectTypeId, 
					CASE 
						when switchObject = 0 then SubjectAssetID
						else ObjectAssetID
					END AS SubjectAssetID, 
					CASE 
						when switchObject = 0 then SubjectAssetTypeID
						else ObjectAssetTypeID
					END AS SubjectAssetTypeID, 
					CASE 
						when switchObject = 0 then ObjectAssetID
						else SubjectAssetID
					END AS ObjectAssetID, 
					CASE 
						when switchObject = 0 then ObjectAssetTypeID
						else SubjectAssetTypeID
					END AS ObjectAssetTypeID, 
					SwitchObject
				from #tempdata
				where isfound <> 0;

				update	R
				set		R.ID = I.ID,
						R.[uid] = I.[uid]
				from	#Relationships R
						inner join [Intersect] I on I.IntersectTypeID = R.IntersectTypeID 
							and I.SubjectAssetID = R.SubjectAssetID 
							and I.ObjectAssetID = R.ObjectAssetID
				where R.ObjectAssetID <> 0 and R.SubjectAssetID <> 0;

				if exists(Select 1 from #tempdata t where T.OBJECTCLASS = {(int)AssetTypeClass.Reference}  and isfound = 0)
				begin
					update	R
					set		R.ID = I.ID,
							R.[uid] = I.[uid]
					from	#Relationships R
							inner join [Intersect] I on I.IntersectTypeID = R.IntersectTypeID 
								and I.SubjectAssetID = R.SubjectAssetID 
								and I.ObjectAssetTypeID = R.ObjectAssetTypeID and I.ObjectAssetID = 0;
				end

				if exists(Select 1 from #tempdata t where T.SubjectAssetID = {(int)AssetTypeClass.Reference}  and isfound = 0)
				begin
					update	R
					set		R.ID = I.ID,
							R.[uid] = I.[uid]
					from	#Relationships R
							inner join [Intersect] I on I.IntersectTypeID = R.IntersectTypeID 
							and I.SubjectAssetTypeID = R.SubjectAssetTypeID 
							and I.ObjectAssetID = R.ObjectAssetID and I.SubjectAssetID = 0;
				end				

				drop table if exists #tempdatasmy;

				select distinct IntersectTypeID, ObjectAssetID
				into #tempdatasmy
				from #tempdata;

				create index idx_tempdatasmy on #tempdatasmy(IntersectTypeID, ObjectAssetID);

				With IIDs as
				(
				select distinct ID,Uid from
				(
				select I.ID,I.Uid
				from #tempdatasmy A
                inner join [Intersect] I on I.IntersectTypeID = A.IntersectTypeID and I.ObjectAssetID = A.ObjectAssetID
				union all
				select I.ID,I.Uid
				from #tempdatasmy A
                inner join [Intersect] I on I.IntersectTypeID = A.IntersectTypeID and I.SubjectAssetID = A.ObjectAssetID
				) a
				)
				insert into #DeletedRelationships WITH(TABLOCK)
				select I.[uid]
				from IIDs I
				left join #Relationships R on R.ID = I.Id
				where R.ID is null ;	

				delete	i
				from	[Intersect] I 
				where	exists (select 1 from #DeletedRelationships d where d.uid = I.[uid]);

				insert into [Intersect] (IntersectTypeID, 
										SubjectAssetID, SubjectAssetTypeID, 
										ObjectAssetID, ObjectAssetTypeID, 
										CreatedBy, UpdatedBy)
				select  IntersectTypeID,
						SubjectAssetID, SubjectAssetTypeID, 
						ObjectAssetID, ObjectAssetTypeID, 
						{CurrentResourceID}, {CurrentResourceID}
					from   #Relationships
					where  ID is null

					update	R
					set		R.ID = I.ID,
							R.[uid] = I.[uid]
					from	#Relationships R
							inner join [Intersect] I on I.SubjectAssetID = R.SubjectAssetID and I.ObjectAssetID = R.ObjectAssetID and I.IntersectTypeID = R.IntersectTypeID
					where	R.ID is null;

					select [uid], 1 as Success, 'Intersect' as [Object] from #Relationships
					union all
					select [uid], 1 as Success, 'Intersect' as [Object] from #DeletedRelationships
";

            IEnumerable<DatabaseBulkRelationshipResult> events = Connection.Query<DatabaseBulkRelationshipResult>(sql,
            new { executionID, beginItemNumber, endItemNumber }, transaction: trans, commandTimeout: timeout);


			// TODO: Add event grid calls here.
        }

		public List<DatabaseBulkRelationshipResult> ImportRelationships(ApiExecution execution, IntersectType rt, RelationshipInserts import, int timeout = 3600, bool sendWorkflowEvents = false, bool lookupFieldsPassedByValue = false, bool sendGraphEvents = true)
		{
			Stopwatch swBegin = Stopwatch.StartNew();
			const string METHOD_NAME = "ImportRelationships";
			bool isLog = import.Count() > 1;
			List<DatabaseBulkRelationshipResult> results = new List<DatabaseBulkRelationshipResult>();
			bool generalChecksCompleted = false;
			CurrentExecutionLocationModel currentLocation = null;
			bool checkCircularRelationships = false;
			bool checkSemanticRelation = false;
			bool relationshipTypeHasFieldTypes = false;
			bool relationshipTypeHasLookupFieldTypes = false;
			bool IsUidPassed = false;
			Dictionary<string, double> metrics = new Dictionary<string, double>();
			int step = 0;

			if ((rt.Predicate != null) && rt.Predicate.Type == PredicateType.Transformation)
			{
				checkCircularRelationships = true;
			}

			if ((rt.Predicate != null) && rt.Predicate.Type.AsInfoModel().SingleRelationshipByFunctionalType)
			{
				checkSemanticRelation = true;
			}

			import.ForEach(rel =>
			{
				if (!string.IsNullOrEmpty(rel.Owner))
				{
					rel.Owner = rel.Owner.Trim();
				}
			});

			SetApiExecutionProcessingStartTime(execution.ExecutionID);

			//check if trigger workflows is set to true and there are actually no workflows
			sendWorkflowEvents = sendWorkflowEvents && TypeHasWorkflows(null, rt.ID, null, null);

			var executionItemDupes = import.Where(i => i.ExecutionItemUid.HasValue).GroupBy(i => i.ExecutionItemUid).Where(i => i.Count() > 1).Select(i => new { ExecutionItemUid = i.Key, Count = i.Count() }).ToList();
			List<RelationshipInsert> tooLongOwners = import.Where(x => !string.IsNullOrEmpty(x.Owner) && x.Owner.Length > 100).ToList();
			var uidDupes = import.Where(i => i.Uid != Guid.Empty && i.Uid != null).GroupBy(i => i.Uid).Where(i => i.Count() > 1).Select(i => new { Uid = i.Key, Count = i.Count() }).ToList();

			if (executionItemDupes.Any())
			{
				string message = $"Duplicate execution item identifiers: {string.Join(", ", executionItemDupes.Select(i => i.ExecutionItemUid.ToString()))}. Identifiers must be unique within a batch.";
				execution.ErrorMessage = message.Substring(0, Math.Min(constants.ERROR_MESSAGE_CHARACTER_LIMIT, message.Length));
				results.AddRange(import.Select(i => new DatabaseBulkRelationshipResult { ExecutionItemUid = i.ExecutionItemUid, Message = execution.ErrorMessage, Success = false }));
			}
			else if (tooLongOwners.Any())
			{
				string message = $"Owner value max length exceeded : {string.Join(", ", tooLongOwners.Select(i => i.Owner))}. Max length of Owner field is 100 characters.";
				execution.ErrorMessage = message.Substring(0, Math.Min(constants.ERROR_MESSAGE_CHARACTER_LIMIT, message.Length));
				results.AddRange(import.Select(i => new DatabaseBulkRelationshipResult { ExecutionItemUid = i.ExecutionItemUid, Message = execution.ErrorMessage, Success = false }));
			}
			else if (uidDupes.Any())
			{
				string message = string.Format(Messages.Error_Duplicate_Relationship_Uid, string.Join(", ", uidDupes.Select(i => i.Uid)));
				execution.ErrorMessage = message.Substring(0, Math.Min(constants.ERROR_MESSAGE_CHARACTER_LIMIT, message.Length));
				results.AddRange(import.Select(i => new DatabaseBulkRelationshipResult { ExecutionItemUid = i.ExecutionItemUid, Message = execution.ErrorMessage, Success = false }));
			}
			else if (!executionItemDupes.Any() && !tooLongOwners.Any() && !uidDupes.Any())
			{
				Stopwatch sw = Stopwatch.StartNew();
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

					DataTable table = new DataTable();
					table.Columns.Add("ExecutionID", typeof(Guid));
					table.Columns.Add("ItemNumber", typeof(int));
					table.Columns.Add("Message", typeof(string));
					table.Columns.Add("Success", typeof(bool));
					table.Columns.Add("SubjectUid", typeof(Guid));
					table.Columns.Add("ObjectUid", typeof(Guid));
					table.Columns.Add("ExecutionItemUid", typeof(Guid));
					table.Columns.Add("Owner", typeof(string));
					table.Columns.Add("uid", typeof(Guid));

					DataTable errorTable = new DataTable();
					errorTable.Columns.Add("ExecutionID", typeof(Guid));
					errorTable.Columns.Add("ItemNumber", typeof(int));
					errorTable.Columns.Add("Message", typeof(string));
					errorTable.Columns.Add("ExecutionItemUid", typeof(Guid));

					DataTable fieldTable = new DataTable();
					fieldTable.Columns.Add("ExecutionID", typeof(Guid));
					fieldTable.Columns.Add("ItemNumber", typeof(int));
					fieldTable.Columns.Add("FieldName", typeof(string));
					fieldTable.Columns.Add("FieldValue", typeof(string));
					fieldTable.Columns.Add("FieldTypeID", typeof(int));

					#endregion

					// Get field types.
					sw.Restart();

					List<FieldTypeCore> fieldTypes = GetAssetTypeFieldTypesCore("IntersectType", rt.ID);
					addMeasurement(metrics, "Get field types", sw.ElapsedMilliseconds, ++step);
					List<string> requiredFieldTypeNames = fieldTypes.Where(f => f.IsRequired && !f.HasDefaultValue && f.Type != DataType.Counter.ToString()).Select(f => f.Name).ToList();
					relationshipTypeHasFieldTypes = fieldTypes.Any();
					relationshipTypeHasLookupFieldTypes = fieldTypes.Any(f => f.Type == DataType.Lookup.ToString());

					#region Generate data sets

					sw.Restart();
					for (int i = 1; i <= import.Count; i++)
					{
						if (i > currentLocation.HighestItemNumber)
						{
							RelationshipInsert model = import[i - 1];

							List<DataRow> fieldRows = ValidateFields("IntersectType", rt.ID, true, fieldTypes, requiredFieldTypeNames, model.Fields, execution.ExecutionID, i, fieldTable, out bool success, out string errorMessage, jsonElementsEnabled: false, IslookupFieldsPassedByValue: lookupFieldsPassedByValue);

							if (success)
							{
								fieldRows.ForEach(fr => { fieldTable.Rows.Add(fr); });

								DataRow row = table.NewRow();

								row["ExecutionID"] = execution.ExecutionID;
								row["ItemNumber"] = i;
								row["SubjectUid"] = model.SubjectAssetUid;
								row["ObjectUid"] = model.ObjectAssetUid;
								row["Owner"] = model.Owner;

								if (model.ExecutionItemUid.HasValue)
								{
									row["ExecutionItemUid"] = model.ExecutionItemUid.Value;
								}

								if (model.Uid != Guid.Empty)
								{
									row["uid"] = model.Uid;
									IsUidPassed = true;
								}

								table.Rows.Add(row);
							}
							else
							{
								DataRow row = errorTable.NewRow();
								row["ExecutionID"] = execution.ExecutionID;
								if (model.ExecutionItemUid.HasValue)
								{
									row["ExecutionItemUid"] = model.ExecutionItemUid.Value;
								}

								row["ItemNumber"] = i;
								row["Message"] = errorMessage;

								errorTable.Rows.Add(row);

								results.Add(new DatabaseBulkRelationshipResult { IntersectID = 0, ExecutionItemUid = model.ExecutionItemUid, IsNew = false, ItemNumber = i, Message = errorMessage, Success = false });
							}
						}
					}
					addMeasurement(metrics, "Generate data sets", sw.ElapsedMilliseconds, ++step);

					#endregion

					if (Database.Connection.State != ConnectionState.Open)
					{
						Connection.Open();
					}

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
						bulkCopy.ColumnMappings.Add("uid", "uid");

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

					addMeasurement(metrics, "Bulk Copy", sw.ElapsedMilliseconds, ++step);

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

						addMeasurement(metrics, "ResolveFieldLookupValues", sw.ElapsedMilliseconds, ++step);
						sw.Restart();
						LogFieldLookupErrors(execution.ExecutionID, "IntersectType", rt.ID, "Relationship", lookupFieldsPassedByValue, timeout);
						addMeasurement(metrics, "LogFieldLookupErrors", sw.ElapsedMilliseconds, ++step);
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
													and T.SubjectUid = D.SubjectUid and T.ObjectUid = D.ObjectUid",
						new { execution.ExecutionID }, commandTimeout: timeout);
						addMeasurement(metrics, "Invalidate duplicates", sw.ElapsedMilliseconds, ++step);
					}

					#endregion

					if (IsUidPassed)
					{
						#region Validate Relationship Uid

						Connection.Execute(@"
											declare @it int;

											select	@it = ID
											from	IntersectType
											where	[uid] = @uid

											drop table if exists #tempdupuid;

											select I.IntersectTypeID,
											T.SubjectUid Ex_SubjectUid,T.ObjectUid Ex_ObjectUid, S.Uid Int_SubjectUid,
											O.Uid Int_ObjectUid
											into #tempdupuid
											from api.ExecutionRelationship T
											inner join [Intersect] I on I.Uid = T.Uid
											left join Asset S on S.ID = I.SubjectAssetID 
											left join Asset O on O.ID = I.ObjectAssetID 
											where   T.ExecutionId = @ExecutionID and T.Uid is not null;

											create index idx_tempdupuid on #tempdupuid(IntersectTypeID);
							
											if exists (select 1 from #tempdupuid where IntersectTypeID != @it)
											   begin
													update	T
													set		T.Message = coalesce(T.Message + '; ', '') + 'Batch failed due to relationship uid is already specified with different relationship type.',
															T.Success = 0
													from	api.ExecutionRelationship T
													where   T.ExecutionId = @ExecutionID
											end

											if exists (select 1 from #tempdupuid where IntersectTypeID = @it and (Ex_SubjectUid != Int_SubjectUid or Ex_ObjectUid != Int_ObjectUid))
											   begin
													update	T
													set		T.Message = coalesce(T.Message + '; ', '') + 'Batch failed due to passed relationship uid match. But SubjectUid and ObjectUid not match.',
															T.Success = 0
													from	api.ExecutionRelationship T
													where   T.ExecutionId = @ExecutionID
											end
										",
										new { execution.ExecutionID, rt.uid }, commandTimeout: timeout);
						addMeasurement(metrics, "Log Validate Relationship Uid", sw.ElapsedMilliseconds, ++step);

						#endregion

						#region Validate Relationship Uid - Add

						Connection.Execute(@"
											declare @sc int,
												@stid int,
												@oc int,
												@otid int,
												@it int


											select	@sc = SubjectClass,
													@stid = SubjectAssetTypeID,
													@oc = ObjectClass,
													@otid = ObjectAssetTypeID,
													@it = ID
											from	IntersectType
											where	[uid] = @uid

											drop table if exists #tempNewuid;
							
											select T.uid
											into #tempNewuid
											from api.ExecutionRelationship T
											left outer join [Intersect] I on T.uid = I.uid
											where  T.ExecutionId = @ExecutionID and T.Uid is not null and I.Id is null;

											create index idx_tempNewuid on #tempNewuid(uid);

											if exists ( select 1
														from	api.ExecutionRelationship T
														inner join #tempNewuid N on T.uid = N.uid
														left join AssetWithType S on S.AssetTypeID = @stid and S.[uid] = T.SubjectUid
														left join AssetWithType O on O.AssetTypeID = @otid and O.[uid] = T.ObjectUid
														left join IntersectType IT on IT.uid = @uid
														left join [Intersect] I on IT.Id = I.IntersectTypeId and I.SubjectAssetId= S.Id and I.ObjectAssetId = O.Id 
														where   T.ExecutionId = @ExecutionID and I.id is not null
													  )
											   begin
													update	T
													set		T.Message = coalesce(T.Message + '; ', '') + 'Batch failed due to unique relationship uid passed. But subjectuid and objectuid exist for relationship type.',
															T.Success = 0
													from	api.ExecutionRelationship T
													where   T.ExecutionId = @ExecutionID
											end
										",
										new { execution.ExecutionID, rt.uid }, commandTimeout: timeout);
						addMeasurement(metrics, "Log Validate Relationship Uid - Add", sw.ElapsedMilliseconds, ++step);

						#endregion
					}

					#region Validate subjects/objects

					string intersectTempTableQuery = string.Empty;
					string intersectCheckJoin = "left join [Intersect] I on IT.Id = I.IntersectTypeId and I.SubjectAssetId= S.ID and I.ObjectAssetId = O.ID ";

					bool useTempTablesForIntersects = import.Count() > 500;

					if (useTempTablesForIntersects)
					{
						intersectTempTableQuery = @$"drop table if exists #tempIntersects
										select I.Id, I.SubjectAssetID, I.ObjectAssetID
										into #tempIntersects
										from [IntersectType] IT 
										inner join [Intersect] I on I.IntersectTypeID = IT.ID
										where IT.uid = @uid";

						intersectCheckJoin = "left join #tempIntersects I on I.SubjectAssetId= S.ID and I.ObjectAssetId = O.ID ";
					}

					sw.Restart();
					Connection.Execute($@"
										declare @sc int,
												@stid int,
												@oc int,
												@otid int,
												@it int

										select	@sc = SubjectClass,
												@stid = SubjectAssetTypeID,
												@oc = ObjectClass,
												@otid = ObjectAssetTypeID,
												@it = ID
										from	IntersectType
										where	[uid] = @uid

										update	T
										set		T.SubjectAssetID = coalesce(I.SubjectAssetID, 0),
												T.SubjectAssetTypeID = coalesce(I.SubjectAssetTypeID, 0),
												T.ObjectAssetID = coalesce(I.ObjectAssetID, 0),
												T.ObjectAssetTypeID = coalesce(I.ObjectAssetTypeID, 0),
												T.IsNew = iif(I.Id is null, 1, 0)
										from	api.ExecutionRelationship T
												inner join [Intersect] I on abs(I.IntersectTypeId) = @it and I.Uid = T.Uid
										where	T.ExecutionID = @ExecutionID and T.Uid Is not null;
										
										{intersectTempTableQuery}

										update	T
										set		T.SubjectAssetID = coalesce(S.ID, 0),
												T.SubjectAssetTypeID = coalesce(S.AssetTypeID, 0),
												T.ObjectAssetID = coalesce(O.ID, 0),
												T.ObjectAssetTypeID = coalesce(O.AssetTypeID, 0),
												T.IsNew = iif(I.Id is null, 1, 0)
										from	api.ExecutionRelationship T
												left join Asset S on S.AssetTypeID = @stid and S.[uid] = T.SubjectUid
												left join Asset O on O.AssetTypeID = @otid and O.[uid] = T.ObjectUid
												left join IntersectType IT on IT.uid = @uid
												{intersectCheckJoin}
											where T.ExecutionID = @ExecutionID and (T.IsNew is null OR T.IsNew = 1);

										if @sc = 9 and @stid = 0
										begin
										update	T
										set		T.SubjectAssetID = 0,
												T.SubjectAssetTypeID = S.ID
										from	api.ExecutionRelationship T
												inner join AssetType S on S.[uid] = T.SubjectUid and S.[Class] = @sc and T.SubjectAssetTypeID = 0 
												where T.ExecutionID = @ExecutionID;
										end

										if @oc = 9 and @otid = 0 
										begin
										update	T
										set		T.ObjectAssetID = 0,
												T.ObjectAssetTypeID = O.ID
										from	api.ExecutionRelationship T
												inner join AssetType O on O.[uid] = T.ObjectUid and O.[Class] = @oc  and T.ObjectAssetTypeID = 0
												where T.ExecutionID = @ExecutionID;
										end

										if ((@sc = 9 and @stid = 0) or (@oc = 9 and @otid = 0))
										begin
										update	T
										set		T.IsNew = 0
										from	api.ExecutionRelationship T
												inner join [Intersect] I on  I.IntersectTypeId = @it 
												and I.SubjectAssetId= T.SubjectAssetID and I.SubjectAssetTypeId= T.SubjectAssetTypeID 
												and I.ObjectAssetId = T.ObjectAssetId and I.ObjectAssetTypeID = T.ObjectAssetTypeID
										where T.ExecutionID = @ExecutionID and T.IsNew = 1;
										end
											",
										new { execution.ExecutionID, rt.uid }, commandTimeout: timeout);
					addMeasurement(metrics, "Validate subjects/objects", sw.ElapsedMilliseconds, ++step);

					#endregion

					#region Log subject/object resolution errors

					sw.Restart();
					Connection.Execute(@"
										update	api.ExecutionRelationship
										set		Success = 0,
											[Message] = coalesce([Message] + '; ', '') + 'Not able to resolve subject of this relationship to a valid asset.'
										where	ExecutionID = @ExecutionID and (SubjectAssetID = 0 and SubjectAssetTypeID = 0);
	
										update	api.ExecutionRelationship
										set		Success = 0,
											[Message] = coalesce([Message] + '; ', '') + 'Not able to resolve object of this relationship to a valid asset.'
										where	ExecutionID = @ExecutionID and (ObjectAssetID = 0 and ObjectAssetTypeID = 0);

										update	api.ExecutionRelationship
										set		Success = 0,
											[Message] = coalesce([Message] + '; ', '') + 'Subject and Object cannot be same Asset.'
										where	ExecutionID = @ExecutionID and SubjectUid = ObjectUid;
										",
										new { execution.ExecutionID }, commandTimeout: timeout);
					addMeasurement(metrics, "Log subject/object resolution errors", sw.ElapsedMilliseconds, ++step);

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
																	inner join [Intersect] I on I.IntersectTypeID = @IntersectTypeID and I.ObjectAssetID = O.ID
																	inner join Asset S on S.Uid <> ER.SubjectUid and S.ID = I.SubjectAssetID 
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
						addMeasurement(metrics, "SubjectCardinality == Cardinality.One", sw.ElapsedMilliseconds, ++step);
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
																	inner join [Intersect] I on I.IntersectTypeID = @IntersectTypeID and I.SubjectAssetID = S.ID 
																	inner join Asset O on O.Uid <> ER.ObjectUid and O.ID = I.ObjectAssetID 
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
						addMeasurement(metrics, "ObjectCardinality == Cardinality.One", sw.ElapsedMilliseconds, ++step);
					}

					#endregion

					#region Permissions Validation

					sw.Restart();
					Connection.Execute($@"
										declare @IsAdministrator bit = 0
										select	@IsAdministrator = IsAdministrator
										from	reporting.Global_Resource
										where	ResourceID = @ResourceID

										if @IsAdministrator = 0
										begin
										update	T
										set		T.Message = coalesce(T.Message + '; ', '') + '{CompanyContextApiError.NotPermissionModifyRelationSubjectAsset}',
												T.Success = 0
										from	
												api.ExecutionRelationship T
										where 
												T.ExecutionID = @ExecutionID and not exists (
															select 1
															from	Asset A
																	outer apply dbo.UserAssetPermissions(@ResourceID, A.AssetTypeID) P
																	where	
																	A.Uid = T.SubjectUid 
																	and
																	(															
																		(
																			P.AssetID = A.ID
																			or 
																			P.AssetTypeID is null
																		)
																		OR
																		(																	
																			P.AssetID=0 
																			and 
																			P.AssetTypeID=A.AssetTypeID
																		)
																	)
																	and 
																	P.PermissionsBitMask is not null and P.PermissionsBitMask & @p = @p	
															)  ;

										update	T
										set		T.Message = coalesce(T.Message + '; ', '') + '{CompanyContextApiError.NotPermissionModifyRelationobjectAsset}',
												T.Success = 0
										from	
												api.ExecutionRelationship T
										where 
												T.ExecutionID = @ExecutionID and not exists (
															select 1
															from	Asset A
																	outer apply dbo.UserAssetPermissions(@ResourceID, A.AssetTypeID) P
																	where	
																	A.Uid = T.ObjectUid 
																	and
																	(															
																		(
																			P.AssetID = A.ID
																			or 
																			P.AssetTypeID is null
																		)
																		OR
																		(																	
																			P.AssetID=0 
																			and 
																			P.AssetTypeID=A.AssetTypeID
																		)
																	)
																	and 
																	P.PermissionsBitMask is not null and P.PermissionsBitMask & @p = @p	
															);
										end",
										new { execution.ExecutionID, execution.ResourceID, p = (int)Permission.EditRelationships }, commandTimeout: timeout);
					addMeasurement(metrics, "Permissions Validation", sw.ElapsedMilliseconds, ++step);

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
						addMeasurement(metrics, "Circular Relationships Validation", sw.ElapsedMilliseconds, ++step);
					}

					if (checkSemanticRelation)
					{
						sw.Restart();
						Connection.Execute(@"
											update	T
											set		T.Message = coalesce(T.Message + '; ', '') + 'Not able to create this relationship because a relationship for this functional type already exists.',
													T.Success = 0
											from	api.ExecutionRelationship T
													inner join [Intersect] I on ( 
														(I.SubjectAssetID = T.SubjectAssetID and I.ObjectAssetID = T.ObjectAssetID) 
														or (I.ObjectAssetID = T.SubjectAssetID and I.SubjectAssetID = T.ObjectAssetID)
													)
													inner join [IntersectType] IT on IT.ID = I.IntersectTypeID
													inner join [Predicate] P on P.ID = IT.PredicateID and P.[Type] = @predicateType  
													where IT.ID <> @intersectTypeID and T.ExecutionId = @ExecutionID 
													and T.IsNew = 1 
											", new { execution.ExecutionID, predicateType = (int)PredicateType.SemanticRelation, intersectTypeID = rt.ID }, commandTimeout: timeout);
						addMeasurement(metrics, "Semantic Relationships Validation", sw.ElapsedMilliseconds, ++step);
					}

					generalChecksCompleted = true;
				}
				catch (Exception generalEx)
				{
					generalChecksCompleted = false;
					string msg = generalEx.GetFullExceptionData(false, constants.ERROR_MESSAGE_CHARACTER_LIMIT);
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
							using (SqlTransaction trans = Connection.BeginTransaction())
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
														on      ( T.IntersectTypeID = @rtID and T.SubjectAssetID = S.SubjectAssetID and T.ObjectAssetID = S.ObjectAssetID and T.SubjectAssetTypeID = S.SubjectAssetTypeID and T.ObjectAssetTypeID = S.ObjectAssetTypeID)
														when matched then
															update set
																	T.UpdatedBy = @CurrentResourceID,
																	T.UpdatedOn = getutcdate(),
																	T.Owner = coalesce(S.Owner,T.Owner)
														when not matched by target then
															insert  (uid,IntersectTypeID, SubjectAssetID, SubjectAssetTypeID, ObjectAssetID, ObjectAssetTypeID, [State], CreatedBy, CreatedOn, UpdatedBy, UpdatedOn, [Owner])
															values  (isnull(S.Uid,newid()),@rtID, S.SubjectAssetID, S.SubjectAssetTypeID, S.ObjectAssetID, S.ObjectAssetTypeID, 1, @CurrentResourceID, getutcdate(), @CurrentResourceID, getutcdate(), coalesce(S.Owner,'BULK_API'))
														output inserted.ID, S.ItemNumber, $action into #ObjectMergeTableResult;

														update	T
														set		T.IntersectID = S.ID,
																T.uid = IT.uid
														from	api.ExecutionRelationship T
																inner join #ObjectMergeTableResult S on T.ExecutionID = @ExecutionID and S.ItemNumber = T.ItemNumber
																inner join [Intersect] IT on IT.ID = S.ID
														where   T.ItemNumber between @beginItemNumber and @endItemNumber;",
														new { execution.ExecutionID, beginItemNumber, endItemNumber, CurrentResourceID, rtID = rt.ID }, transaction: trans, commandTimeout: timeout);
									addMeasurement(metrics, "Intersect table merge", sw.ElapsedMilliseconds, ++step);

									#endregion

									fieldTypeUpdates.Clear();

									if (relationshipTypeHasFieldTypes)
									{
										sw.Restart();
										fieldTypeUpdates = MergeFields(execution.ExecutionID, trans, "api.ExecutionRelationship", SystemObjects.Intersect, "A.IntersectID", beginItemNumber, endItemNumber, sendWorkflowEvents, timeout);
										addMeasurement(metrics, "MergeFields", sw.ElapsedMilliseconds, ++step);
									}

									// Update success flag
									sw.Restart();
									Connection.Execute(
										$"update api.ExecutionRelationship set Success = 1 where Success is null and ExecutionID = @ExecutionID and ItemNumber between @beginItemNumber and @endItemNumber and IntersectID is not null;",
										new { execution.ExecutionID, beginItemNumber, endItemNumber }, transaction: trans, commandTimeout: timeout);
									addMeasurement(metrics, "Update success flag", sw.ElapsedMilliseconds, ++step);

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
										addMeasurement(metrics, "LogLoop Execution Error In Rollback", sw.ElapsedMilliseconds, ++step);
									}

									retryCount++;

									if (retryCount > API_V2_RETRY_LIMIT)
									{
										sw.Restart();
										LogLoopExecutionError(execution.ExecutionID, beginItemNumber, endItemNumber, "api.ExecutionRelationship", ex.GetFullExceptionData(false), timeout);
										addMeasurement(metrics, "LogLoopExecutionError", sw.ElapsedMilliseconds, ++step);
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

						beginItemNumber += loopSize;
						endItemNumber += loopSize;
					}

					completeApiExecutionAndGetCounts(execution.ExecutionID, "ExecutionRelationship");
					Connection.Close();
					sw.Restart();

					if (sendWorkflowEvents)
					{
						SendWorkflowEvents("IntersectType", rt.ID, results, null, fieldTypeUpdates);
					}

					addMeasurement(metrics, "SendWorkflowEvents", sw.ElapsedMilliseconds, ++step);

					// Send score recalculation notifications.
					sw.Restart();
					CreateImportRelationshipsExecution(execution.ExecutionID, rt.ID, timeout);
					addMeasurement(metrics, $"SendScoreEventWithPayload", sw.ElapsedMilliseconds, ++step);
				}
			}
			addMeasurement(metrics, "End Method", swBegin.ElapsedMilliseconds, ++step);
			addMetric(TelemetryClient, execution, METHOD_NAME, metrics, isLog);

			return results;
		}

		public List<RelationshipTypeResult> ImportRelationshipTypes(ApiExecution execution, IEnumerable<RelationshipTypeInsert> import, int timeout = 3600)
        {
            List<RelationshipTypeResult> results = new List<RelationshipTypeResult>();

            SetApiExecutionProcessingStartTime(execution.ExecutionID);

            var dupes = import.Where(i => i.ExecutionItemUid.HasValue && i.ExecutionItemUid.Value != Guid.Empty).GroupBy(i => i.ExecutionItemUid).Where(i => i.Count() > 1).Select(i => new { ExecutionItemUid = i.Key, Count = i.Count() }).ToList();
            if (dupes.Any())
            {
                string message = $"Duplicate execution item identifiers: {string.Join(", ", dupes.Select(i => i.ExecutionItemUid.ToString()))}. Identifiers must be unique within a batch.";
                execution.ErrorMessage = message.Substring(0, Math.Min(constants.ERROR_MESSAGE_CHARACTER_LIMIT, message.Length));
                results.AddRange(import.Select(i => new RelationshipTypeResult { ExecutionItemUid = i.ExecutionItemUid, Message = execution.ErrorMessage, Success = false }));
            }
            else
            {
                #region Build data tables for bulk load.

                DataTable table = new DataTable();
                table.Columns.Add("ExecutionID", typeof(Guid));
                table.Columns.Add("ExecutionItemUid", typeof(Guid));
                table.Columns.Add("ItemNumber", typeof(int));
				table.Columns.Add("SourceID", typeof(string));
				table.Columns.Add("SubjectUid", typeof(Guid));
                table.Columns.Add("SubjectCardinality", typeof(int));
                table.Columns.Add("ObjectUid", typeof(Guid));
                table.Columns.Add("ObjectCardinality", typeof(int));
                table.Columns.Add("PredicateUid", typeof(Guid));
                table.Columns.Add("IsNew", typeof(bool));
                table.Columns.Add("uid", typeof(Guid));

                int i = 0;
                foreach (RelationshipTypeInsert item in import)
                {
                    DataRow row = table.NewRow();

                    row["ExecutionID"] = execution.ExecutionID;
                    row["ItemNumber"] = i++;
					if (!string.IsNullOrEmpty(item.SourceID) && !string.IsNullOrWhiteSpace(item.SourceID))
					{
						row["SourceID"] = item.SourceID;
					}
					if (item.ExecutionItemUid.HasValue)
                    {
                        row["ExecutionItemUid"] = item.ExecutionItemUid.Value;
                    }

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
                    {
                        Connection.Open();
                    }

                    #region Bulk Copy

                    using (SqlBulkCopy bulkCopy = new SqlBulkCopy(Connection)
                    {
                        BatchSize = SqlBulkBatchSize,
                        DestinationTableName = "api.ExecutionRelationshipType",
                        BulkCopyTimeout = SqlBulkBatchTimeout
                    })
                    {

                        bulkCopy.ColumnMappings.Add("ExecutionID", "ExecutionID");
                        bulkCopy.ColumnMappings.Add("ExecutionItemUid", "ExecutionItemUid");
                        bulkCopy.ColumnMappings.Add("ItemNumber", "ItemNumber");
						bulkCopy.ColumnMappings.Add("SourceID", "SourceID");

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

                    ValidateRelationshipTypes(true, execution, timeout);

                    Connection.Execute(@"
										update  api.ExecutionRelationshipType
										set     [Uid] = Newid()
										where   ExecutionID = @ExecutionID 
												and Success is null
												and ([Uid] is null or [Uid] = @emptyUid);

										insert into [IntersectType] 
												(SubjectClass, SubjectAssetTypeID, ObjectClass, ObjectAssetTypeID, PredicateID, SubjectCardinality, ObjectCardinality, CreatedBy, CreatedOn, UpdatedBy, UpdatedOn, [Uid], SourceID)
										select  SubjectClass, SubjectAssetTypeID, 
												ObjectClass, ObjectAssetTypeID, 
												PredicateID, SubjectCardinality, ObjectCardinality,
												@resourceId, @utcNow, @resourceId, @utcNow, [Uid], SourceID 
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
                                        $"select ExecutionItemUid,Uid,SourceID,Message,Success from api.ExecutionRelationshipType where ExecutionID = @ExecutionID",
                                        new { execution.ExecutionID }
                                        ).ToList();

                    CreateRollupPathChangedExecution(null, null, execution.ExecutionID);
                }
                finally
                {
                    if (Database.Connection.State == ConnectionState.Open)
                    {
                        Connection.Close();
                    }
                }

            }
            return results;
        }
        
		public List<RelationshipTypeResult> ImportRelationshipTypes(ApiExecution execution, IEnumerable<RelationshipTypeUpdate> import, int timeout = 3600)
        {
            List<RelationshipTypeResult> results = new List<RelationshipTypeResult>();

            SetApiExecutionProcessingStartTime(execution.ExecutionID);

            var dupes = import.Where(i => i.ExecutionItemUid.HasValue && i.ExecutionItemUid.Value != Guid.Empty).GroupBy(i => i.ExecutionItemUid).Where(i => i.Count() > 1).Select(i => new { ExecutionItemUid = i.Key, Count = i.Count() }).ToList();

            if (dupes.Any())
            {
                string message = $"Duplicate execution item identifiers: {string.Join(", ", dupes.Select(i => i.ExecutionItemUid.ToString()))}. Identifiers must be unique within a batch.";
                execution.ErrorMessage = message.Substring(0, Math.Min(constants.ERROR_MESSAGE_CHARACTER_LIMIT, message.Length));
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

                    DataTable table = new DataTable();
                    table.Columns.Add("ExecutionID", typeof(Guid));
                    table.Columns.Add("ExecutionItemUid", typeof(Guid));
                    table.Columns.Add("ItemNumber", typeof(int));
					table.Columns.Add("SourceID", typeof(string));
					table.Columns.Add("SubjectUid", typeof(Guid));
                    table.Columns.Add("SubjectCardinality", typeof(int));
                    table.Columns.Add("ObjectUid", typeof(Guid));
                    table.Columns.Add("ObjectCardinality", typeof(int));
                    table.Columns.Add("PredicateUid", typeof(Guid));
                    table.Columns.Add("PredicateID", typeof(int));
                    table.Columns.Add("Message", typeof(string));
                    table.Columns.Add("Success", typeof(bool));
                    table.Columns.Add("IsNew", typeof(bool));
                    table.Columns.Add("uid", typeof(Guid));

                    int i = 0;
                    foreach (RelationshipTypeUpdate item in import)
                    {
                        DataRow row = table.NewRow();

                        row["ExecutionID"] = execution.ExecutionID;
                        row["ItemNumber"] = i++;
						if (!string.IsNullOrEmpty(item.SourceID) && !string.IsNullOrWhiteSpace(item.SourceID))
						{
							row["SourceID"] = item.SourceID;
						}
						if (item.ExecutionItemUid.HasValue)
                        {
                            row["ExecutionItemUid"] = item.ExecutionItemUid.Value;
                        }

                        row["SubjectCardinality"] = (int)item.SubjectCardinality;
                        row["ObjectCardinality"] = (int)item.ObjectCardinality;
                        row["SubjectUid"] = item.SubjectUid.HasValue ? item.SubjectUid : DBNull.Value;
                        row["ObjectUid"] = item.ObjectUid.HasValue ? item.ObjectUid : DBNull.Value;
                        row["PredicateUid"] = item.PredicateUid;
                        row["uid"] = item.Uid;
                        row["IsNew"] = false;

                        table.Rows.Add(row);
                    }

                    #endregion
                    try
                    {
                        if (Database.Connection.State != ConnectionState.Open)
                        {
                            Connection.Open();
                        }

                        #region Bulk Copy

                        using (SqlBulkCopy bulkCopy = new SqlBulkCopy(Connection)
                        {
                            BatchSize = SqlBulkBatchSize,
                            DestinationTableName = "api.ExecutionRelationshipType",
                            BulkCopyTimeout = SqlBulkBatchTimeout
                        })
                        {

                            bulkCopy.ColumnMappings.Add("ExecutionID", "ExecutionID");
                            bulkCopy.ColumnMappings.Add("ExecutionItemUid", "ExecutionItemUid");
                            bulkCopy.ColumnMappings.Add("ItemNumber", "ItemNumber");
							bulkCopy.ColumnMappings.Add("SourceID", "SourceID");

							bulkCopy.ColumnMappings.Add("SubjectCardinality", "SubjectCardinality");
                            bulkCopy.ColumnMappings.Add("ObjectCardinality", "ObjectCardinality");
                            bulkCopy.ColumnMappings.Add("SubjectUid", "SubjectUid");
                            bulkCopy.ColumnMappings.Add("ObjectUid", "ObjectUid");
                            bulkCopy.ColumnMappings.Add("PredicateUid", "PredicateUid");
                            bulkCopy.ColumnMappings.Add("IsNew", "IsNew");
                            bulkCopy.ColumnMappings.Add("uid", "uid");

                            bulkCopy.WriteToServer(table);
                        }

                        #endregion

                        ValidateRelationshipTypes(false, execution, timeout);

                        Connection.Execute(@"
							Update	IT
							Set		PredicateID = ER.PredicateID,
									SubjectCardinality = ER.SubjectCardinality, 
									ObjectCardinality = ER.ObjectCardinality,
									SubjectClass = coalesce(ER.SubjectClass, IT.SubjectClass),
									SubjectAssetTypeID = coalesce(ER.SubjectAssetTypeID, IT.SubjectAssetTypeID),
									ObjectClass = coalesce(ER.ObjectClass, IT.ObjectClass),
									ObjectAssetTypeID = coalesce(ER.ObjectAssetTypeID, IT.ObjectAssetTypeID),
									IT.SourceID = coalesce(ER.SourceID, IT.SourceID),
									UpdatedBy = @resourceId,
									UpdatedOn = @utcNow
							from [intersecttype] IT
							inner join [api].[ExecutionRelationshipType] ER on IT.UID = ER.UID
							where  ER.ExecutionID=@executionID and
							ER.Success is null
						   
							Update	api.ExecutionRelationshipType
							Set		Success =1,
									Message ='Updated Successfully'
							Where	ExecutionID=@executionID and Success is null; ",
                                new { executionID = execution.ExecutionID, resourceId = CurrentResourceID, utcNow = DateTime.UtcNow }, commandTimeout: timeout);

                        results = Query<RelationshipTypeResult>(
                                            $"select ExecutionItemUid,Uid,SourceID,Message,Success from api.ExecutionRelationshipType where ExecutionID = @ExecutionID",
                                            new { execution.ExecutionID }).ToList();
                    }
                    finally
                    {
                        if (Database.Connection.State == ConnectionState.Open)
                        {
                            Connection.Close();
                        }
                    }

                }
            }

            return results;
        }

        public List<AssetFieldTypeUpdate> MergeFields(Guid executionID, SqlTransaction trans, string tableName, SystemObjects objectType, string IdSqlSyntax, int beginItemNumber, int endItemNumber, bool sendWorkflowEvents, int timeout = 3600, bool isInsert = false, bool hasLookupFieldTypes = true)
        {
            List<AssetFieldTypeUpdate> res = new List<AssetFieldTypeUpdate>();

            if (sendWorkflowEvents)
            {
                string changedFieldsSql = $@"select  
							{(tableName.Equals("api.ExecutionRelationship", StringComparison.InvariantCultureIgnoreCase) ? "A.Object" : "EA.Object")}, 
							{(tableName.Equals("api.ExecutionRelationship", StringComparison.InvariantCultureIgnoreCase) ? "A.ObjectID" : "EA.ObjectID")}, 
							EF.FieldTypeID AS Id 
					from    {tableName} EA 
							{(tableName.Equals("api.ExecutionRelationship", StringComparison.InvariantCultureIgnoreCase) ? "inner join Asset a on a.id = EA.ObjectAssetID" : " ")}
							inner join {ApiExecutionFieldTable} EF on EF.ExecutionID = EA.ExecutionID 
											and EF.ItemNumber = EA.ItemNumber 
											and EF.FieldTypeID is not null
							inner join Field F on F.FieldTypeId = EF.FieldTypeID 
											and F.AssetID = {(tableName.Equals("api.ExecutionRelationship", StringComparison.InvariantCultureIgnoreCase) ? "EA.ObjectAssetID" : "EA.AssetID")} 
							inner join FieldType FT on F.FieldTypeID=FT.ID
					where   EA.ExecutionID = @executionID 
							and EA.IsNew <> 1 and FT.Type != 'Lookup'
							{(!isInsert ? "and F.FormattedValue <> EF.FieldValue" : "")} 
							and EA.ItemNumber between @beginItemNumber and @endItemNumber

					union all

					select  
							{(tableName.Equals("api.ExecutionRelationship", StringComparison.InvariantCultureIgnoreCase) ? "A.Object" : "EA.Object")}, 
							{(tableName.Equals("api.ExecutionRelationship", StringComparison.InvariantCultureIgnoreCase) ? "A.ObjectID" : "EA.ObjectID")}, 
							EF.FieldTypeID AS Id 
					from    {tableName} EA 
							{(tableName.Equals("api.ExecutionRelationship", StringComparison.InvariantCultureIgnoreCase) ? "inner join Asset a on a.id = EA.ObjectAssetID" : " ")}
							inner join {ApiExecutionFieldTable} EF on EF.ExecutionID = EA.ExecutionID 
											and EF.ItemNumber = EA.ItemNumber 
											and EF.FieldTypeID is not null
					where   EA.ExecutionID = @executionID 
							and EA.IsNew <> 1 
							and EA.ItemNumber between @beginItemNumber and @endItemNumber
							{(!isInsert ? "and coalesce(EF.FieldValue, '') <> ''" : "")} 
							and not exists (select 1 from Field F where FieldTypeID = EF.FieldTypeID 
								and F.AssetID = {(tableName.Equals("api.ExecutionRelationship", StringComparison.InvariantCultureIgnoreCase) ? "EA.ObjectAssetID" : "EA.AssetID")} )
					UNION ALL
					select  
							{(tableName.Equals("api.ExecutionRelationship", StringComparison.InvariantCultureIgnoreCase) ? "A.Object" : "EA.Object")}, 
							{(tableName.Equals("api.ExecutionRelationship", StringComparison.InvariantCultureIgnoreCase) ? "A.ObjectID" : "EA.ObjectID")}, 
							EF.FieldTypeID AS Id 
					from    {tableName} EA 
							{(tableName.Equals("api.ExecutionRelationship", StringComparison.InvariantCultureIgnoreCase) ? "inner join Asset a on a.id = EA.ObjectAssetID" : " ")}
							inner join {ApiExecutionFieldTable} EF on EF.ExecutionID = EA.ExecutionID 
											and EF.ItemNumber = EA.ItemNumber 
											and EF.FieldTypeID is not null
							inner join Field F on F.FieldTypeId = EF.FieldTypeID 
											and F.AssetID = {(tableName.Equals("api.ExecutionRelationship", StringComparison.InvariantCultureIgnoreCase) ? "EA.ObjectAssetID" : "EA.AssetID")} 
							inner join FieldType FT on F.FieldTypeID=FT.ID
					where   EA.ExecutionID = @executionID 
							and EA.IsNew <> 1 and FT.Type = 'Lookup'
							{(!isInsert ? "and F.Value <> EF.FieldValue" : "")}
							and EA.ItemNumber between @beginItemNumber and @endItemNumber";

                if (!isInsert)
                {
                    changedFieldsSql += $@"
					union all

					select  A.[Object], 
							A.ObjectID, 
							F.FieldTypeID as Id
					from    Field F
							inner join {tableName} E on E.ExecutionID = @executionID 
							inner join {ApiExecutionFieldTable} EF on EF.ExecutionId = E.ExecutionId and EF.ItemNumber = E.ItemNumber
							inner join Asset A on A.uid = E.Uid                  
					where   E.ExecutionID = @executionID
							and EF.ItemNumber between @beginItemNumber and @endItemNumber
							and EF.Ignore is null
							and EF.FieldTypeID is not null
							and F.AssetID = A.Id
							and F.FieldTypeID = EF.FieldTypeID
							and EF.FieldValue is null 
							and EF.LookupValue is null";
                }

                res = Connection.Query<AssetFieldTypeUpdate>(changedFieldsSql, new { executionID, beginItemNumber, endItemNumber }, transaction: trans, commandTimeout: timeout).ToList();
            }

            // if we already have the asset id then insert it
            bool hasAssetID = ((tableName ?? "").ToUpper() == "API.EXECUTIONASSET" || (tableName ?? "").ToUpper() ==  "API.EXECUTIONUSER");
			bool hasIntersectID = ((tableName ?? "").ToUpper() == "API.EXECUTIONRELATIONSHIP");

			string fieldValuesSql = $@"
								select 
										F.FieldTypeID as [FieldTypeID]                                        
										,case 
											when FT.Type = 'Link' then F.FieldValue
											else F.LookupValue
										end as [Value]
										,F.FieldValue as [FormattedValue]
										,getutcdate() as [UpdatedOn]
										,@resourceId as [UpdatedBy]
										{(hasAssetID ? ",A.AssetID" : ",null as AssetID")}                                          
										{(hasIntersectID ? ",A.IntersectID" : ",null as IntersectID")}  
								from    {tableName} A
										inner join {ApiExecutionFieldTable} F on F.ExecutionID = A.ExecutionID
											and F.ItemNumber = A.ItemNumber 
											and F.FieldTypeID is not null
											and A.Success is null
										inner join FieldType FT on FT.Id = F.FieldTypeID
								where   A.ExecutionID = @executionID
										and A.ItemNumber between @beginItemNumber and @endItemNumber 
										and (F.Ignore = 0 or F.Ignore is null)
										and FT.Type != 'Relationship'
										and FT.Type != 'Counter'
										and FieldValue is not null";

            string lookupFieldValuesSql = $@"
								select 
										F.FieldTypeID as [FieldTypeID]                                        
										,F.LookupValue as [Value]
										,F.FieldValue as [FormattedValue]
										,getutcdate() as [UpdatedOn]
										,@resourceId as [UpdatedBy]
										{(hasAssetID ? ",A.AssetID as AssetID" : ",null as AssetID")}
										{(hasIntersectID ? ",A.IntersectID as IntersectID" : ",null as IntersectID")}  
								from    {tableName} A
										inner join {ApiExecutionFieldTable} F on F.ExecutionID = A.ExecutionID
											and F.ItemNumber = A.ItemNumber 
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
						dbo.[Field] ([FieldTypeID],[Value],[FormattedValue],[UpdatedOn],[UpdatedBy],[AssetID],[IntersectID])                         
						{fieldValuesSql}
					"
                    , new { executionID, beginItemNumber, endItemNumber, resourceId = CurrentResourceID }, transaction: trans, commandTimeout: timeout);
            }
            else
            {
				var fieldIdSQL = $" and F.AssetID = {IdSqlSyntax}";

				if(objectType == SystemObjects.Intersect)
				{
					fieldIdSQL = $" and F.IntersectID = {IdSqlSyntax}";
				}

				if (objectType == SystemObjects.Issue)
				{
					fieldIdSQL = $" and F.IssueID = {IdSqlSyntax}";
				}

				Connection.Execute($@"
					DELETE Field
					FROM Field F
						inner join {tableName} A on A.ExecutionID = @executionID 
						inner join {ApiExecutionFieldTable} EF on EF.ExecutionId = A.ExecutionId and EF.ItemNumber = A.ItemNumber
					WHERE EF.ItemNumber between @beginItemNumber and @endItemNumber
					 and EF.Ignore is null
					 and EF.FieldTypeID is not null
					 {fieldIdSQL}
					 and F.FieldTypeID = EF.FieldTypeID
					 and EF.FieldValue is null 
					 and EF.LookupValue is null;",
                new { executionID, beginItemNumber, endItemNumber, resourceId = CurrentResourceID }, transaction: trans, commandTimeout: timeout);

				// Merge Field Filter field
				var mergefieldSQL = $" T.AssetID = S.AssetID";
				if (hasIntersectID)
				{
					mergefieldSQL = $" T.IntersectID = S.IntersectID";
				}

				// update non-lookup fields
				Connection.Execute($@"
					merge       Field as T
					using       (
									{fieldValuesSql} and FT.Type != 'Lookup'
								) as S 
					on          ( T.FieldTypeID = S.FieldTypeID and ({mergefieldSQL}) )
					when matched and T.Value <> S.Value COLLATE SQL_Latin1_General_CP1_CS_AS OR T.FormattedValue <> S.FormattedValue COLLATE SQL_Latin1_General_CP1_CS_AS then
					update set T.Value = S.Value,T.FormattedValue = S.FormattedValue, T.UpdatedBy = @resourceId, T.UpdatedOn = getutcdate()                     
					when		not matched by target then
					insert		(FieldTypeID, Value, FormattedValue, UpdatedBy, UpdatedOn, AssetID, IntersectID)
					values		(S.FieldTypeID, S.Value, S.FormattedValue, @resourceId, getutcdate(), S.AssetID, S.IntersectID);",
                                new { executionID, sendWorkflowEvents, beginItemNumber, endItemNumber, resourceId = CurrentResourceID }, transaction: trans, commandTimeout: timeout);

                if (hasLookupFieldTypes)
                {
                    // update lookup fields, DO NOT SET THE FORMATTED VALUE to the ID only compare on the id since you dont have the formatted value...
                    Connection.Execute($@"
					merge       Field as T
					using       (
									{lookupFieldValuesSql}
								) as S 
					on          ( T.FieldTypeID = S.FieldTypeID and ({mergefieldSQL}) )
					when matched and T.Value <> S.Value COLLATE SQL_Latin1_General_CP1_CS_AS or T.Value is null then
					update set T.Value = S.Value, T.UpdatedBy = @resourceId, T.UpdatedOn = getutcdate()                     
					when		not matched by target then
					insert		(FieldTypeID, Value, FormattedValue, UpdatedBy, UpdatedOn, AssetID, IntersectID)
					values		(S.FieldTypeID, S.Value, S.FormattedValue, @resourceId, getutcdate(), S.AssetID, s.IntersectID);",
                                    new { executionID, sendWorkflowEvents, beginItemNumber, endItemNumber, resourceId = CurrentResourceID }, transaction: trans, commandTimeout: timeout);
                }
            }

            return res;
        }

		public List<DatabaseBulkRelationshipUpdateResult> PutRelationships(ApiExecution execution, IntersectType rt, RelationshipUpdates import, int timeout = 3600, bool sendWorkflowEvents = false, bool lookupFieldsPassedByValue = false, bool sendGraphEvents = true)
		{
			Stopwatch swBegin = Stopwatch.StartNew();
			const string METHOD_NAME = "PutRelationships";
			bool isLog = import.Count() > 1;
			List<DatabaseBulkRelationshipUpdateResult> results = new List<DatabaseBulkRelationshipUpdateResult>();
			bool generalChecksCompleted = false;
			CurrentExecutionLocationModel currentLocation = null;
			bool relationshipTypeHasFieldTypes = false;
			bool relationshipTypeHasLookupFieldTypes = false;
			bool IsUidPassed = false;
			Dictionary<string, double> metrics = new Dictionary<string, double>();
			int step = 0;

			import.ForEach(rel =>
			{
				if (!string.IsNullOrEmpty(rel.Owner))
				{
					rel.Owner = rel.Owner.Trim();
				}
			});

			SetApiExecutionProcessingStartTime(execution.ExecutionID);

			//check if trigger workflows is set to true and there are actually no workflows
			sendWorkflowEvents = sendWorkflowEvents && TypeHasWorkflows(null, rt.ID, null, null);

			var executionItemDupes = import.Where(i => i.ExecutionItemUid.HasValue).GroupBy(i => i.ExecutionItemUid).Where(i => i.Count() > 1).Select(i => new { ExecutionItemUid = i.Key, Count = i.Count() }).ToList();
			List<RelationshipUpdate> tooLongOwners = import.Where(x => !string.IsNullOrEmpty(x.Owner) && x.Owner.Length > 100).ToList();

			if (executionItemDupes.Any())
			{
				string message = string.Format(CompanyContextApiError.DuplicateExecutionItem, string.Join(", ", executionItemDupes.Select(i => i.ExecutionItemUid.ToString())));

				execution.ErrorMessage = message.Substring(0, Math.Min(constants.ERROR_MESSAGE_CHARACTER_LIMIT, message.Length));
				results.AddRange(import.Select(i => new DatabaseBulkRelationshipUpdateResult { ExecutionItemUid = i.ExecutionItemUid, Message = execution.ErrorMessage, Success = false }));
			}
			else if (tooLongOwners.Any())
			{
				string message = string.Format(CompanyContextApiError.OwnerValueMaxLength, string.Join(", ", tooLongOwners.Select(i => i.Owner)));
				execution.ErrorMessage = message.Substring(0, Math.Min(constants.ERROR_MESSAGE_CHARACTER_LIMIT, message.Length));
				results.AddRange(import.Select(i => new DatabaseBulkRelationshipUpdateResult { ExecutionItemUid = i.ExecutionItemUid, Message = execution.ErrorMessage, Success = false }));
			}
			else if (!executionItemDupes.Any() && !tooLongOwners.Any())
			{
				Stopwatch sw = Stopwatch.StartNew();
				try
				{
					currentLocation = GetCurrentExecutionLocation(execution.ExecutionID, "api.ExecutionRelationship");

					if (currentLocation.HighestItemNumberProcessed > 0)
					{
						results.AddRange(
							Query<DatabaseBulkRelationshipUpdateResult>(
								$"select * from api.ExecutionRelationship where ExecutionID = @ExecutionID and ItemNumber <= {currentLocation.HighestItemNumberProcessed}",
								new { execution.ExecutionID }
							)
						);
					}

					#region Build data tables for bulk load.

					DataTable table = new DataTable();
					table.Columns.Add("ExecutionID", typeof(Guid));
					table.Columns.Add("ItemNumber", typeof(int));
					table.Columns.Add("Message", typeof(string));
					table.Columns.Add("Success", typeof(bool));
					table.Columns.Add("ExecutionItemUid", typeof(Guid));
					table.Columns.Add("Owner", typeof(string));
					table.Columns.Add("uid", typeof(Guid));

					DataTable errorTable = new DataTable();
					errorTable.Columns.Add("ExecutionID", typeof(Guid));
					errorTable.Columns.Add("ItemNumber", typeof(int));
					errorTable.Columns.Add("Message", typeof(string));
					errorTable.Columns.Add("ExecutionItemUid", typeof(Guid));

					DataTable fieldTable = new DataTable();
					fieldTable.Columns.Add("ExecutionID", typeof(Guid));
					fieldTable.Columns.Add("ItemNumber", typeof(int));
					fieldTable.Columns.Add("FieldName", typeof(string));
					fieldTable.Columns.Add("FieldValue", typeof(string));
					fieldTable.Columns.Add("FieldTypeID", typeof(int));

					#endregion

					// Get field types.
					sw.Restart();

					List<FieldTypeCore> fieldTypes = GetAssetTypeFieldTypesCore("IntersectType", rt.ID);

					addMeasurement(metrics, "Get field types", sw.ElapsedMilliseconds, ++step);
					List<string> requiredFieldTypeNames = fieldTypes.Where(f => f.IsRequired && !f.HasDefaultValue && f.Type != DataType.Counter.ToString()).Select(f => f.Name).ToList();
					relationshipTypeHasFieldTypes = fieldTypes.Any();
					relationshipTypeHasLookupFieldTypes = fieldTypes.Any(f => f.Type == DataType.Lookup.ToString());

					#region Generate data sets

					sw.Restart();
					for (int i = 1; i <= import.Count; i++)
					{
						if (i > currentLocation.HighestItemNumber)
						{

							RelationshipUpdate model = import[i - 1];

							List<DataRow> fieldRows = ValidateFields("IntersectType", rt.ID, true, fieldTypes, requiredFieldTypeNames, model.Fields, execution.ExecutionID, i, fieldTable, out bool success, out string errorMessage, jsonElementsEnabled: false, IslookupFieldsPassedByValue: lookupFieldsPassedByValue);

							if (success)
							{
								fieldRows.ForEach(fr => { fieldTable.Rows.Add(fr); });

								DataRow row = table.NewRow();

								row["ExecutionID"] = execution.ExecutionID;
								row["ItemNumber"] = i;
								row["Owner"] = model.Owner;
								if (model.ExecutionItemUid.HasValue)
								{
									row["ExecutionItemUid"] = model.ExecutionItemUid.Value;
								}

								if (model.Uid != Guid.Empty)
								{
									row["uid"] = model.Uid;
									IsUidPassed = true;
								}
								table.Rows.Add(row);
							}
							else
							{
								DataRow row = errorTable.NewRow();
								row["ExecutionID"] = execution.ExecutionID;
								if (model.ExecutionItemUid.HasValue)
								{
									row["ExecutionItemUid"] = model.ExecutionItemUid.Value;
								}

								row["ItemNumber"] = i;
								row["Message"] = errorMessage;

								errorTable.Rows.Add(row);

								results.Add(new DatabaseBulkRelationshipUpdateResult { IntersectID = 0, ExecutionItemUid = model.ExecutionItemUid, IsNew = false, ItemNumber = i, Message = errorMessage, Success = false });

							}
						}
					}
					addMeasurement(metrics, "Generate data sets", sw.ElapsedMilliseconds, ++step);

					#endregion

					if (Database.Connection.State != ConnectionState.Open)
					{
						Connection.Open();
					}

					#region Bulk Copy

					sw.Restart();
					using (SqlBulkCopy bulkCopy = new SqlBulkCopy(Connection))
					{

						bulkCopy.BatchSize = SqlBulkBatchSize;
						bulkCopy.DestinationTableName = "api.ExecutionRelationship";
						bulkCopy.BulkCopyTimeout = SqlBulkBatchTimeout;

						bulkCopy.ColumnMappings.Add("ExecutionID", "ExecutionID");
						bulkCopy.ColumnMappings.Add("ItemNumber", "ItemNumber");
						bulkCopy.ColumnMappings.Add("ExecutionItemUid", "ExecutionItemUid");
						bulkCopy.ColumnMappings.Add("Owner", "Owner");
						bulkCopy.ColumnMappings.Add("uid", "uid");

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

					addMeasurement(metrics, "Bulk Copy", sw.ElapsedMilliseconds, ++step);

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

						addMeasurement(metrics, "ResolveFieldLookupValues", sw.ElapsedMilliseconds, ++step);
						sw.Restart();
						LogFieldLookupErrors(execution.ExecutionID, "IntersectType", rt.ID, "Relationship", lookupFieldsPassedByValue, timeout);
						addMeasurement(metrics, "LogFieldLookupErrors", sw.ElapsedMilliseconds, ++step);
					}

					#region Validate Uid

					sw.Restart();

					Connection.Execute($@"
										update	T
										set		T.Message = coalesce(T.Message + '; ', '') + '{CompanyContextApiError.RelationshipInvalidUid}',
												T.Success = 0
										from	api.ExecutionRelationship T
										where   T.ExecutionID = @ExecutionID and T.uid is null


										update	T
										set		T.Message = coalesce(T.Message + '; ', '') + '{CompanyContextApiError.RelationshipUidNotFound}',
												T.Success = 0
										from	api.ExecutionRelationship T
										where T.ExecutionID = @ExecutionID and T.Uid Is not null 
										and not exists (select 1 
														from [Intersect] I 
														where I.Uid = T.Uid
														);
										",
										new { execution.ExecutionID }, commandTimeout: timeout);
					addMeasurement(metrics, "Validate Uid", sw.ElapsedMilliseconds, ++step);

					#endregion

					#region Invalidate duplicates

					sw.Restart();

					if (execution.Total > 1)
					{
						Connection.Execute($@"
											update	T
											set		T.Message = coalesce(T.Message + '; ', '') + '{CompanyContextApiError.RelatioshipSpecifiedMoreThanOnce}',
													T.Success = 0
											from	api.ExecutionRelationship T
											cross apply (
												select      Uid
												from        api.ExecutionRelationship
												where       ExecutionID = @ExecutionID
												group by    Uid
												having      count(*) > 1
											) D
											where T.ExecutionId = @ExecutionID
											And T.uid is not null 
											And T.Uid= D.Uid
									",
						new { execution.ExecutionID }, commandTimeout: timeout);
						addMeasurement(metrics, "Invalidate duplicates", sw.ElapsedMilliseconds, ++step);
					}

					#endregion

					if (IsUidPassed)
					{
						#region Validate Relationship Uid

						Connection.Execute($@"
							declare @it int;

							select	@it = ID
							from	IntersectType
							where	[uid] = @uid

							drop table if exists #tempdupuid;

							select I.IntersectTypeID,
							T.Uid
							into #tempdupuid
							from api.ExecutionRelationship T
							inner join [Intersect] I on I.Uid = T.Uid
							where T.ExecutionId = @ExecutionID 
							and T.Uid is not null;

							create index idx_tempdupuid on #tempdupuid(Uid);

							
							if exists (select 1 from #tempdupuid where IntersectTypeID != @it)
							   begin
									update	T
									set		T.Message = coalesce(T.Message + '; ', '') + '{CompanyContextApiError.RelatioshipUidExistWithDifferentType}',
											T.Success = 0
									from	api.ExecutionRelationship T
									inner join #tempdupuid temp on T.uid = temp.Uid 
									where   T.ExecutionId = @ExecutionID and temp.IntersectTypeID != @it
							   end

						",
						new { execution.ExecutionID, rt.uid }, commandTimeout: timeout);
						addMeasurement(metrics, "Log Validate Relationship Uid", sw.ElapsedMilliseconds, ++step);

						#endregion
					}

					#region Validate subjects/objects

					sw.Restart();
					Connection.Execute(@"
										declare @it int

										select	@it = ID
										from	IntersectType
										where	[uid] = @uid

										update	T
										set		T.IntersectID = I.ID,
												T.SubjectAssetID = I.SubjectAssetID,
												T.SubjectAssetTypeID = I.SubjectAssetTypeID,
												T.ObjectAssetID = I.ObjectAssetID,
												T.ObjectAssetTypeID = I.ObjectAssetTypeID,
												T.IsNew = 0
										from	api.ExecutionRelationship T
												inner join [Intersect] I on abs(I.IntersectTypeId) =  @it and I.Uid = T.Uid
										where	T.ExecutionID = @ExecutionID and T.Uid Is not null;

										update	T
										set		T.SubjectAssetID = A.ID,
												T.SubjectAssetTypeID = A.AssetTypeID
										from	api.ExecutionRelationship T
												inner join [Asset] A on A.Uid = T.SubjectUid 
										where	T.ExecutionID = @ExecutionID;

										update	T
										set		T.ObjectAssetID = A.ID,
												T.ObjectAssetTypeID = A.AssetTypeID
										from	api.ExecutionRelationship T
												inner join [Asset] A on A.Uid = T.ObjectUid 
										where	T.ExecutionID = @ExecutionID;
										",
										new { execution.ExecutionID, rt.uid }, commandTimeout: timeout);
					addMeasurement(metrics, "Validate subjects/objects", sw.ElapsedMilliseconds, ++step);

					#endregion

					#region Permissions Validation

					sw.Restart();
					Connection.Execute($@"
										declare @IsAdministrator bit = 0
										select	@IsAdministrator = IsAdministrator
										from	reporting.Global_Resource
										where	ResourceID = @ResourceID

										if @IsAdministrator = 0
										begin

										drop table if exists #temppremissionSub;

										select	R.ExecutionID, R.ItemNumber
										into #temppremissionSub
										from	api.ExecutionRelationship R
												inner join Asset A on A.ID = R.SubjectAssetID and R.ExecutionID = @ExecutionID
												outer apply dbo.UserAssetPermissions(@ResourceID, A.AssetTypeID) P
										where	(
												(P.AssetID = A.ID) 
												or P.AssetTypeID is null
												)
												and (
													(P.PermissionsBitMask is not null and P.PermissionsBitMask & @p <> @p) 
													or 
													P.PermissionsBitMask is null
													)
										group by R.ExecutionID, R.ItemNumber;

										create index IX_temppremissionSubitem on #temppremissionSub(ExecutionID,ItemNumber)

										update	T
										set		T.Message = coalesce(T.Message + '; ', '') + '{CompanyContextApiError.NotPermissionModifyRelationSubjectAsset}',
												T.Success = 0
										from	api.ExecutionRelationship T
												inner join #temppremissionSub S on S.ExecutionID = T.ExecutionID and S.ItemNumber = T.ItemNumber
										where  T.ExecutionID = @ExecutionID;

										drop table if exists #temppremissionObj;


										select	R.ExecutionID, R.ItemNumber
										into #temppremissionObj
										from	api.ExecutionRelationship R
												inner join Asset A on A.ID = R.ObjectAssetID and R.ExecutionID = @ExecutionID
												outer apply dbo.UserAssetPermissions(@ResourceID, A.AssetTypeID) P
										where	(
												(P.AssetID = A.ID) 
												or P.AssetTypeID is null
												)
												and (
													(P.PermissionsBitMask is not null and P.PermissionsBitMask & @p <> @p) 
													or 
													P.PermissionsBitMask is null
													)
										group by R.ExecutionID, R.ItemNumber

										create index IX_temppremissionObjitem on #temppremissionObj(ExecutionID,ItemNumber)

										update	T
										set		T.Message = coalesce(T.Message + '; ', '') + '{CompanyContextApiError.NotPermissionModifyRelationobjectAsset}',
												T.Success = 0
										from	api.ExecutionRelationship T
												inner join	#temppremissionObj S on S.ExecutionID = T.ExecutionID and S.ItemNumber = T.ItemNumber
										where T.ExecutionID= @ExecutionID;
										end",
					new { execution.ExecutionID, execution.ResourceID, p = (int)Permission.AddRelationships }, commandTimeout: timeout);
					addMeasurement(metrics, "Permissions Validation", sw.ElapsedMilliseconds, ++step);

					#endregion

					generalChecksCompleted = true;
				}
				catch (Exception generalEx)
				{
					generalChecksCompleted = false;
					string msg = generalEx.GetFullExceptionData(false, constants.ERROR_MESSAGE_CHARACTER_LIMIT);
					execution.ErrorMessage = msg;
					execution.Processed = 0;
					execution.Error = import.Count();

					results = new List<DatabaseBulkRelationshipUpdateResult>();
					results.AddRange(import.Select(i => new DatabaseBulkRelationshipUpdateResult { ExecutionItemUid = i.ExecutionItemUid, Message = msg, Success = false }));
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
							using (SqlTransaction trans = Connection.BeginTransaction())
							{
								try
								{
									#region Intersect table merge

									sw.Restart();
									Connection.Execute($@"
														drop table if exists #TempExecRelOwner;

														select      uid, owner
														into        #TempExecRelOwner
														from        api.ExecutionRelationship 
														where		ExecutionID = @ExecutionID
																	and ItemNumber between @beginItemNumber and @endItemNumber
																	and Success is null;

														CREATE NONCLUSTERED INDEX IX_TempExecRelOwnerUid ON #TempExecRelOwner (Uid);

														update	T
														set		T.UpdatedBy = @CurrentResourceID,
																T.UpdatedOn = getutcdate(),
																T.Owner = coalesce(S.Owner, T.Owner)
														from	[Intersect] T
																inner join #TempExecRelOwner S on S.Uid = T.Uid
														where	T.IntersectTypeID = @rtID;

													", new { execution.ExecutionID, beginItemNumber, endItemNumber, CurrentResourceID, rtID = rt.ID }, transaction: trans, commandTimeout: timeout);
									addMeasurement(metrics, "Intersect table Update", sw.ElapsedMilliseconds, ++step);

									#endregion

									fieldTypeUpdates.Clear();

									if (relationshipTypeHasFieldTypes)
									{
										sw.Restart();
										fieldTypeUpdates = MergeFields(execution.ExecutionID, trans, "api.ExecutionRelationship", SystemObjects.Intersect, "A.IntersectID", beginItemNumber, endItemNumber, sendWorkflowEvents, timeout);
										addMeasurement(metrics, "MergeFields", sw.ElapsedMilliseconds, ++step);
									}

									// Update success flag
									sw.Restart();
									Connection.Execute(
										$"update api.ExecutionRelationship set Success = 1 where Success is null and ExecutionID = @ExecutionID and ItemNumber between @beginItemNumber and @endItemNumber and IntersectID is not null;",
										new { execution.ExecutionID, beginItemNumber, endItemNumber }, transaction: trans, commandTimeout: timeout);
									addMeasurement(metrics, "Update success flag", sw.ElapsedMilliseconds, ++step);

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
										addMeasurement(metrics, "LogLoop Execution Error In Rollback", sw.ElapsedMilliseconds, ++step);
									}

									retryCount++;

									if (retryCount > API_V2_RETRY_LIMIT)
									{
										sw.Restart();
										LogLoopExecutionError(execution.ExecutionID, beginItemNumber, endItemNumber, "api.ExecutionRelationship", ex.GetFullExceptionData(false), timeout);
										addMeasurement(metrics, "LogLoopExecutionError", sw.ElapsedMilliseconds, ++step);
									}
									else
									{
										Thread.Sleep(API_V2_RETRY_INTERVAL);
									}
								}
							}
						}
						beginItemNumber += loopSize;
						endItemNumber += loopSize;
					}

					completeApiExecutionAndGetCounts(execution.ExecutionID, "ExecutionRelationship");
					Connection.Close();
					sw.Restart();

					if (sendWorkflowEvents)
					{
						SendWorkflowEvents("IntersectType", rt.ID, results, null, fieldTypeUpdates);
					}

					addMeasurement(metrics, "SendWorkflowEvents", sw.ElapsedMilliseconds, ++step);

					// Send score recalculation notifications.
					sw.Restart();
					CreateImportRelationshipsExecution(execution.ExecutionID, rt.ID, timeout);
					addMeasurement(metrics, $"SendScoreEventWithPayload", sw.ElapsedMilliseconds, ++step);
				}
			}

			addMeasurement(metrics, "End Method", swBegin.ElapsedMilliseconds, ++step);
			addMetric(TelemetryClient, execution, METHOD_NAME, metrics, isLog);

			return results;
		}

		public List<PredicateDeleteResult> RemovePredicates(ApiExecution execution, PredicateDeletes import, int timeout = 3600)
		{
			List<PredicateDeleteResult> results = new List<PredicateDeleteResult>();
			bool generalChecksCompleted = false;
			CurrentExecutionLocationModel currentLocation = null;

			SetApiExecutionProcessingStartTime(execution.ExecutionID);

			var executionItemDupes = import.Where(i => i.ExecutionItemUid.HasValue).GroupBy(i => i.ExecutionItemUid).Where(i => i.Count() > 1).Select(i => new { ExecutionItemUid = i.Key, Count = i.Count() }).ToList();
			if (executionItemDupes.Any())
			{
				string message = $"Duplicate execution item identifiers: {string.Join(", ", executionItemDupes.Select(i => i.ExecutionItemUid.ToString()))}. Identifiers must be unique within a batch.";
				execution.ErrorMessage = message.Substring(0, Math.Min(constants.ERROR_MESSAGE_CHARACTER_LIMIT, message.Length));
				results.AddRange(import.Select(i => new PredicateDeleteResult { ExecutionItemUid = i.ExecutionItemUid, Uid = i.Uid, Message = execution.ErrorMessage, Success = false }));
			}
			else
			{
				var uidDupes = import.GroupBy(i => i.Uid).Where(i => i.Count() > 1).Select(i => new { Uid = i.Key, Count = i.Count() }).ToList();
				if (uidDupes.Any())
				{
					string message = $"Duplicate predicate Uids: {string.Join(", ", uidDupes.Select(i => i.Uid.ToString()))}. Identifiers must be unique within a batch.";
					execution.ErrorMessage = message.Substring(0, Math.Min(constants.ERROR_MESSAGE_CHARACTER_LIMIT, message.Length));
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

						DataTable table = new DataTable();
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
								PredicateDelete model = import[i - 1];

								DataRow row = table.NewRow();

								row["ExecutionID"] = execution.ExecutionID;
								row["ItemNumber"] = i;
								if (model.ExecutionItemUid.HasValue)
								{
									row["ExecutionItemUid"] = model.ExecutionItemUid.Value;
								}
								else
								{
									row["ExecutionItemUid"] = Guid.NewGuid();
								}

								row["Uid"] = model.Uid;

								table.Rows.Add(row);
							}
						}

						#endregion

						if (Database.Connection.State != ConnectionState.Open)
						{
							Connection.Open();
						}

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
						string msg = generalEx.GetFullExceptionData(false, constants.ERROR_MESSAGE_CHARACTER_LIMIT);
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
								string querySuffix = $"P.Success is null and P.ExecutionID = @ExecutionID and P.ItemNumber between @beginItemNumber and @endItemNumber";
								using (SqlTransaction trans = Connection.BeginTransaction())
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

		public void ResolveFieldLookupValues(Guid executionID, string fieldTable = "api.ExecutionField", int timeout = 3600, SqlTransaction trans = null)
        {
            Connection.Execute($@"
								drop table if exists #RelevantLookupValues;
								create table #RelevantLookupValues (FieldTypeID int not null, [Text] nvarchar(max), [Value] nvarchar(max));

								drop table if exists #temp_field_type_ids
								select distinct F.Id 
								into #temp_field_type_ids
								from {fieldTable} T
								inner join FieldType F on F.ID = T.FieldTypeID and F.[Type] = 'Lookup' and T.ExecutionID = @executionID


								declare @fieldTypeId int = (select top 1 Id from #temp_field_type_ids)

								while @fieldTypeId is not null
								begin
									insert into #RelevantLookupValues WITH(TABLOCK)
									select FieldTypeId,[Text],[Value] from FieldLookupValue FLV where FLV.FieldTypeID = @fieldTypeId

									delete top (1) from #temp_field_type_ids
									set @fieldTypeId = (select top 1 Id from #temp_field_type_ids)
								end

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
								",
                                new { executionID }, commandTimeout: timeout, transaction: trans);
        }
				
		public void SendBatchApiCompletedEvent(ApiExecution execution)
		{
			QueueSource.CreateFilteredTopicMessageAsync(Config.GetValue<string>("EventBusTopicName"), new BatchApiEvent()
			{
				CompanyID = CurrentCompanyID,
				CompanyDomainPrefix = CurrentCompanyDomain,
				Action = BatchApiEventAction.Completed,
				ExecutionID = execution.ExecutionID
			})
				.GetAwaiter()
				.GetResult();
		}

		public void SetApiExecutionProcessingStartTime(Guid ExecutionId)
		{
			Query<int>("update api.Execution set ProcessingStartedOn = @startedOn where ExecutionId = @ExecutionId and ProcessingStartedOn is null",
				new { startedOn = DateTime.UtcNow, ExecutionId }).FirstOrDefault();
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
								and ((ex.Method = 'PUT' and ef.FieldValue is not null and cast(ef.FieldValue as int) <> isnull(FCV.Value,0)) or ex.Method = 'POST' or (ex.Method = 'BULK' and ea.IsNew = 1));"
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

        public void UpdateGroupCounterFields(Guid executionID, SqlTransaction trans, int beginItemNumber, int endItemNumber, int timeout = 3600)
        {
            Connection.Execute(
                      $@"insert into FieldCounterValue (AssetId, AssetTypeId, FieldTypeId, [Value])
						select distinct a.id as AssetId, ft.assettypeid, ft.id, ef.FieldValue 
							from api.executiongroup ea
						inner join [asset] a on a.Object = 'Group' and a.uid = ea.groupuid
						inner join FieldType ft on ft.assetTypeID = a.assetTypeID and ft.Type = @dataType
						inner join api.execution ex on ex.executionid = @executionid
						inner join [group] g on g.id = a.objectid						
						left join {ApiExecutionFieldTable} ef on ef.executionid = @executionid and ef.itemnumber = ea.itemnumber and ft.id = ef.fieldtypeid
						left join dbo.FieldCounterValue FCV on FCV.AssetId = a.id and FCV.FieldTypeId = ft.id
						where ea.ExecutionID = @executionID 
								and ea.Success is null 
								and a.id is not null
								and ea.ItemNumber between @beginItemNumber and @endItemNumber
								and ((ex.Method = 'PUT' and ef.FieldValue is not null and cast(ef.FieldValue as int) <> isnull(FCV.Value,0)) or ex.Method = 'POST' or ex.Method = 'BULK');"
                      , new { executionID, beginItemNumber, endItemNumber, resourceId = CurrentResourceID, dataType = DataType.Counter.ToString() }, transaction: trans, commandTimeout: timeout);
        }

		public List<PredicateUpsertResult> UpdatePredicates(ApiExecution execution, PredicateUpserts import, int timeout = 3600)
		{
			List<PredicateUpsertResult> results = new List<PredicateUpsertResult>();
			bool generalChecksCompleted = false;
			CurrentExecutionLocationModel currentLocation = null;

			SetApiExecutionProcessingStartTime(execution.ExecutionID);

			var executionItemDupes = import.Where(i => i.ExecutionItemUid.HasValue).GroupBy(i => i.ExecutionItemUid).Where(i => i.Count() > 1).Select(i => new { ExecutionItemUid = i.Key, Count = i.Count() }).ToList();
			var predDupes = import.GroupBy(x => x.Name + x.Type).Where(x => x.Count() > 1).Select(x => new { x.Key, Items = x.ToList() }).ToList();

			var predInverseDupes = import.GroupBy(x => x.Inverse + x.Type).Where(x => x.Count() > 1).Select(x => new { x.Key, Items = x.ToList() }).ToList();

			if (executionItemDupes.Any())
			{
				string message = $"Duplicate execution item identifiers: {string.Join(", ", executionItemDupes.Select(i => i.ExecutionItemUid.ToString()))}. Identifiers must be unique within a batch.";
				execution.ErrorMessage = message.Substring(0, Math.Min(constants.ERROR_MESSAGE_CHARACTER_LIMIT, message.Length));
				results.AddRange(import.Select(i => new PredicateUpsertResult { ExecutionItemUid = i.ExecutionItemUid, Message = execution.ErrorMessage, Success = false }));
			}
			else if (predDupes.Any())
			{
				string message = $"Duplicate predicate items: {string.Join(", ", predDupes.Select(i => i.Items.First().Name + "|" + i.Items.First().Type.ToString()))}. Name and type must be unique within a batch.";
				execution.ErrorMessage = message.Substring(0, Math.Min(constants.ERROR_MESSAGE_CHARACTER_LIMIT, message.Length));
				results.AddRange(import.Select(i => new PredicateUpsertResult { ExecutionItemUid = i.ExecutionItemUid, Message = execution.ErrorMessage, Success = false }));
			}
			else if (predInverseDupes.Any())
			{
				string message = $"Duplicate predicate items: {string.Join(", ", predInverseDupes.Select(i => i.Items.First().Inverse + "|" + i.Items.First().Type.ToString()))}. Inverse and type must be unique within a batch.";
				execution.ErrorMessage = message.Substring(0, Math.Min(constants.ERROR_MESSAGE_CHARACTER_LIMIT, message.Length));
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

					DataTable table = new DataTable();
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
							PredicateUpsert model = import[i - 1];

							DataRow row = table.NewRow();

							row["ExecutionID"] = execution.ExecutionID;
							row["ItemNumber"] = i;

							if (model.ExecutionItemUid.HasValue)
							{
								row["ExecutionItemUid"] = model.ExecutionItemUid.Value;
							}
							else
							{
								row["ExecutionItemUid"] = Guid.NewGuid();
							}

							row["Type"] = (int)model.Type;
							row["Name"] = model.Name;
							row["Inverse"] = model.Inverse;

							if (model.Uid.HasValue)
							{
								row["uid"] = model.Uid;
							}

							table.Rows.Add(row);
						}
					}

					#endregion

					if (Database.Connection.State != ConnectionState.Open)
					{
						Connection.Open();
					}

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

					List<PredicateTypeInfo> allowedFunctionalTypes = PredicateType.DataLineage.GetAsList()
						.Where(p =>
							p.AllowEditFromPredicateEditor
							).ToList();
					string allowedTypeIdList = string.Join(", ", allowedFunctionalTypes.Select(p => (int)p.ID));
					string allowedTypeNameList = string.Join(", ", allowedFunctionalTypes.Select(p => p.ID.ToString().Replace("'", "''")));

					string checkSQL = $@"
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
										where	ExecutionID = @ExecutionID and [Type] not in ({allowedTypeIdList});";

					Connection.Execute(checkSQL, new { execution.ExecutionID }, commandTimeout: timeout);

					#endregion

					generalChecksCompleted = true;
				}
				catch (Exception generalEx)
				{
					generalChecksCompleted = false;
					string msg = generalEx.GetFullExceptionData(false);
					execution.ErrorMessage = msg.Substring(0, Math.Min(constants.ERROR_MESSAGE_CHARACTER_LIMIT, msg.Length));
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
					List<PredicateType> predicateTypes = Enum.GetValues(typeof(PredicateType)).Cast<PredicateType>().ToList();

					for (int currentLoop = 1; currentLoop <= numberOfLoops; currentLoop++)
					{
						bool runCompleted = false;
						int retryCount = 0;

						while (!runCompleted && retryCount <= API_V2_RETRY_LIMIT)
						{
							string querySuffix = $"P.Success is null and P.ExecutionID = @ExecutionID and P.ItemNumber between @beginItemNumber and @endItemNumber";
							using (SqlTransaction trans = Connection.BeginTransaction())
							{
								try
								{

									string insertSQL = $@"
										drop table if exists #mergeResultTable
										create table #mergeResultTable (PredicateId int, PredicateUid uniqueidentifier, ExecutionItemUid uniqueidentifier) 

										update	api.ExecutionPredicate 
										set		Success = 0,
												[Message] = coalesce([Message] + '; ', '') + 'Predicate with same Name and Type already exists'
										from api.ExecutionPredicate EP
										inner join [Predicate] P WITH (NOLOCK) on P.Name = EP.Name and P.Type = EP.Type
										where	ExecutionID = @ExecutionID and EP.uid is null

										update	api.ExecutionPredicate 
										set		Success = 0,
												[Message] = coalesce([Message] + '; ', '') + 'Predicate with same Inverse and Type already exists'
										from api.ExecutionPredicate EP
										inner join [Predicate] P WITH (NOLOCK) on P.Inverse = EP.Inverse and P.Type = EP.Type
										where	ExecutionID = @ExecutionID and EP.uid is null

										update	api.ExecutionPredicate 
										set		Success = 0,
												[Message] = coalesce([Message] + '; ', '') + 'Predicate with same Inverse and Type already exists'
										from api.ExecutionPredicate EP
										inner join [Predicate] P WITH (NOLOCK) on P.Inverse = EP.Inverse and P.Type = EP.Type and P.uid != EP.uid
										where	ExecutionID = @ExecutionID and EP.uid is not null

										update	api.ExecutionPredicate
										set		Success = 0,
												[Message] = coalesce([Message] + '; ', '') + 'Predicate with same Name and Type already exists'
										from api.ExecutionPredicate EP
										inner join [Predicate] P WITH (NOLOCK) on P.Name = EP.Name and P.Type = EP.Type and P.uid != EP.uid
										where	ExecutionID = @ExecutionID and EP.uid is not null
											
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
											P.Type = S.Type,
											P.UpdatedBy = {CurrentResourceID}
										when not matched then
											insert (Uid, Name, Inverse, Type, IsSystem,CreatedBy,UpdatedBy)
											values (S.Uid, S.Name,S.Inverse, S.Type, 0, {CurrentResourceID},{CurrentResourceID})
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
										$"update P set P.Success = 1 from api.ExecutionPredicate P where {querySuffix} and P.PredicateID is not null;",
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

		public List<DataRow> ValidateFields(
            string ot, int otid, bool isInsert,
            List<FieldTypeCore> fieldTypes, List<string> requiredFieldTypeNames,
            Dictionary<string, string> fields, Guid executionID, int itemNumber,
            DataTable fieldTable, out bool success, out string errorMessage,
            bool useFriendlyNames = false,
            bool allowTagFields = false,
            FieldValidationFieldProperties validationFieldProperties = null,
            bool jsonElementsEnabled = true,
            bool IslookupFieldsPassedByValue = false
            )
        {
            List<DataRow> fieldRows = new List<DataRow>();
            List<string> errorMessages = new List<string>();
            string errorDelimiter = ". ";
            success = true;
            errorMessage = string.Empty;
            FieldTypeCore fieldType = null;

            // Contains all required fields?
            IEnumerable<string> missingFields = requiredFieldTypeNames.Except(fields.Select(f => f.Key));

            if (missingFields.Any() && isInsert) // Only check for required fields on insert.
            {
                success = false;
                bool isSinglar = (missingFields.Count() == 1);
                errorMessages.Add($"{string.Join(",", missingFields)} {(isSinglar ? "is a" : "are")} required field{(isSinglar ? "" : "s")}");
            }

            List<string> restrictedFieldTypes = DataType.Text.GetNotAllowedToUpdateViaAssetApi();
            if (allowTagFields)
            {
                restrictedFieldTypes = restrictedFieldTypes.Where(x => x != "Tag").ToList();
            }

            foreach (KeyValuePair<string, string> k in fields)
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
                            errorMessages.Add(CompanyContextApiError.ValidateColorField);
                            success = false;
                        }

                        if (validationFieldProperties != null)
                        {
                            validationFieldProperties.ContainsColorField = true;
                        }
                    }
                    else if (ot == "ReferenceItemType")
                    {
                        switch (fieldName.ToLower())
                        {
                            case "code":

                                if ((fieldValue ?? "").Length > 250)
                                {
                                    errorMessages.Add(CompanyContextApiError.ReferenceListCodeFieldMaxLengthCheck);
                                    success = false;
                                }

                                break;
                            case "icon":

                                if ((fieldValue ?? "").Length > 50 || !fieldValue.StartsWith("fa-"))
                                {
                                    errorMessages.Add(CompanyContextApiError.IconFieldValidation);
                                    success = false;
                                }

                                break;
                            case "referenceitemtypeid":
                                break;
                            default:
                                success = false;
                                errorMessages.Add(string.Format(CompanyContextApiError.ValidFieldCheck, fieldName));
                                break;
                        }
                    }
                    else
                    {
                        success = false;
                        errorMessages.Add(string.Format(CompanyContextApiError.ValidFieldCheck, fieldName));
                    }
                }
                else
                {
                    fieldTypeId = fieldType.ID;

                    if (restrictedFieldTypes.Contains(fieldType.Type))
                    {
                        success = false;
                        errorMessages.Add(string.Format(CompanyContextApiError.RestrictFieldTypeUpdate, fieldName, fieldType.Type));
                    }
                    else
                    {
                        if (string.IsNullOrEmpty(fieldValue))
                        {
                            if (fieldType.IsRequired)
                            {
                                success = false;
                                errorMessages.Add(string.Format(CompanyContextApiError.FieldValueIsRequired, fieldName));
                            }

                            if (isValueEmptyString)
                            {
                                switch (fieldType.Type)
                                {
                                    case "Boolean":
                                        errorMessages.Add(string.Format(CompanyContextApiError.ValidateBoolValue, fieldName));
                                        success = false;
                                        break;
                                    case "Date":
                                        errorMessages.Add(string.Format(CompanyContextApiError.FieldNameValidate, fieldName, "date"));
                                        success = false;
                                        break;
                                    case "DateTime":
                                        errorMessages.Add(string.Format(CompanyContextApiError.FieldNameValidate, fieldName, "datetime value"));
                                        success = false;
                                        break;
                                    case "Decimal":
                                        errorMessages.Add(string.Format(CompanyContextApiError.FieldNameValidate, fieldName, "decimal"));
                                        success = false;
                                        break;
                                    case "Number":
                                        errorMessages.Add(string.Format(CompanyContextApiError.FieldNameValidate, fieldName, "number"));
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
                                        errorMessages.Add(string.Format(CompanyContextApiError.ValidateBoolValue, fieldName));
                                    }

                                    break;
                                case "Date":
                                    DateTime dTest;

                                    if (!DateTime.TryParse(fieldValue, out dTest) && !string.IsNullOrEmpty(fieldValue))
                                    {
                                        success = false;
                                        errorMessages.Add(string.Format(CompanyContextApiError.FieldNameValidate, fieldName, "date"));
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
                                        errorMessages.Add(string.Format(CompanyContextApiError.FieldNameValidate, fieldName, "datetime value"));
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
                                        errorMessages.Add(string.Format(CompanyContextApiError.FieldNameValidate, fieldName, "decimal"));
                                    }

                                    break;
                                case "Link":

                                    if (fieldValue.Count(c => c == '|') != 1 && !string.IsNullOrEmpty(fieldValue) && !fieldValue.Equals('|'))
                                    {
                                        success = false;
                                        errorMessages.Add(string.Format(CompanyContextApiError.ValidateLinkValue, fieldName));
                                    }

                                    if (success)
                                    {
                                        //Remove 'inner' trailing/leading spaces in link value
                                        fieldValue = Regex.Replace(fieldValue, "(\\s*\\|\\s*)", "|");
                                    }

                                    break;
                                case "Lookup":

                                    if (fieldType.AllowMultipleValues == false && fieldValue.Split(',').Length > 1 && IslookupFieldsPassedByValue)
                                    {
                                        success = false;
                                        errorMessages.Add(string.Format(CompanyContextApiError.FieldNotAllowedMultipleValies, fieldName));
                                    }

                                    break;
                                case "Number":

                                    if (!long.TryParse(fieldValue, out _) && !string.IsNullOrEmpty(fieldValue))
                                    {
                                        success = false;
                                        errorMessages.Add(string.Format(CompanyContextApiError.ValidateNumberFieldRange, fieldName, -9223372036854775808, 9223372036854775807));
                                    }

                                    break;
                                case "Percentage":
                                    decimal pctTest;

                                    if (!decimal.TryParse(fieldValue, out pctTest) && !string.IsNullOrEmpty(fieldValue))
                                    {
                                        success = false;
                                        errorMessages.Add(string.Format(CompanyContextApiError.FieldNameValidate, fieldName, "percentage"));
                                    }

                                    break;
                                case "JSON":

                                    if (jsonElementsEnabled && (fieldValue.Length > 2500))
                                    {
                                        success = false;
                                        errorMessages.Add(string.Format(CompanyContextApiError.ExceedsMaximumLength, fieldName, 2500));
                                    }

                                    validationFieldProperties.JsonFieldCount++;
                                    break;
                                case "Counter":
                                    int counterValue = 0;

                                    if (!int.TryParse(fieldValue, out counterValue) || counterValue <= 0)
                                    {
                                        success = false;
                                        errorMessages.Add(string.Format(CompanyContextApiError.ValidateNumberFieldRange, fieldName, 0, 2147483647));
                                    }

                                    break;
                                case "System":
                                    if (ot == "ReferenceItemType" && fieldName.ToLower() == "code" && (fieldValue ?? "").Length > 250)
                                    {
                                        success = false;
                                        errorMessages.Add(CompanyContextApiError.ReferenceListCodeFieldMaxLengthCheck);
                                    }

                                    break;
                                default: // Html, Text

                                    if (!string.IsNullOrEmpty(fieldType.Pattern) && !string.IsNullOrEmpty(fieldValue))
                                    {
                                        if (!Regex.IsMatch(fieldValue, fieldType.Pattern))
                                        {
                                            success = false;
                                            errorMessages.Add(string.Format(CompanyContextApiError.RegularExpressionPatternMatch, fieldName));
                                        }
                                    }

                                    break;
                            }

                            if (fieldType.Length.HasValue)
                            {
                                if (fieldValue.Length < fieldType.Length.Value)
                                {
                                    success = false;
                                    errorMessages.Add(string.Format(CompanyContextApiError.CheckExactLength, fieldName, fieldType.Length.Value));
                                }
                            }

                            if (fieldType.MinimumLength.HasValue)
                            {
                                if (fieldType.Type == "Decimal" || fieldType.Type == "Number")
                                {
                                    if (decimal.TryParse(fieldValue, out decimal fieldDecimalValue) && fieldDecimalValue < fieldType.MinimumLength.Value)
                                    {
                                        success = false;
                                        errorMessages.Add(string.Format(CompanyContextApiError.NumericMinimumValueCheck, fieldName, fieldType.MinimumLength.Value.ToString(decimalFormatString)));
                                    }
                                }
                                else
                                {
                                    if (fieldValue.Length < fieldType.MinimumLength.Value)
                                    {
                                        success = false;
                                        errorMessages.Add(string.Format(CompanyContextApiError.NumericMinimumLengthCheck, fieldName, fieldType.MinimumLength.Value.ToString(decimalFormatString)));
                                    }
                                }

                            }

                            if (fieldType.MaximumLength.HasValue)
                            {
                                if (fieldType.Type == "Decimal" || fieldType.Type == "Number")
                                {
                                    if (decimal.TryParse(fieldValue, out decimal fieldDecimalValue) && fieldDecimalValue > fieldType.MaximumLength.Value)
                                    {
                                        success = false;
                                        errorMessages.Add(string.Format(CompanyContextApiError.NumericMaximumValueCheck, fieldName, fieldType.MaximumLength.Value.ToString(decimalFormatString)));
                                    }
                                }
                                else
                                {
                                    if (fieldValue.Length > fieldType.MaximumLength.Value)
                                    {
                                        success = false;
                                        errorMessages.Add(string.Format(CompanyContextApiError.NumericMaxmiumLengthCheck, fieldName, fieldType.MaximumLength.Value.ToString(decimalFormatString)));
                                    }
                                }
                            }
                        }
                    }
                }

                if (fieldTable != null)
                {
                    DataRow fieldRow = fieldTable.NewRow();

                    fieldRow["ExecutionID"] = executionID;
                    fieldRow["ItemNumber"] = itemNumber;
                    fieldRow["FieldName"] = fieldName;

                    if (k.Value == null)
                    {
                        fieldRow["FieldValue"] = DBNull.Value;
                    }
                    else
                    {
                        fieldRow["FieldValue"] = fieldValue;
                    }

                    if (fieldTypeId.HasValue)
                    {
                        fieldRow["FieldTypeID"] = fieldTypeId.Value;
                    }

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

		#endregion
    }
}
