using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using d360.core;
using d360.core.entities;
using d360.core.enums;
using d360.core.queue;
using d360.core.resources;
using d360.extensions;
using d360.model.DataAccessLayer.repositories;
using d360.model.helpers;
using d360.model.helpers.filters;
using d360.utils.excel;

using Dapper;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using SpreadsheetLight;

namespace d360.model.DataAccessLayer
{
	public class RelationshipRepository : BaseRepository, IRelationshipRepository
	{
		private readonly ICompanyContext companyContext;
		private readonly IQueueSource QueueSource;
		private readonly IStorageProvider Storage;
		private readonly ICommunityContext communityContext;

		public RelationshipRepository(ICommunityContext communityContext, ICompanyContext companyContext, IQueueSource queueSource, IStorageProvider storageProvider)
			: base(companyContext)
		{
			this.companyContext = companyContext;
			QueueSource = queueSource;
			Storage = storageProvider;
			this.communityContext = communityContext;
		}

		public Intersect GetRelationshipByUID(Guid relationshipUid)
		{
			return companyContext.Filter<Intersect>(i => i.uid == relationshipUid).SingleOrDefault();
		}

		public IntersectType GetRelationshipTypeByUID(Guid relationshipTypUid)
		{
			return companyContext.Filter<IntersectType>(i => i.uid == relationshipTypUid).SingleOrDefault();
		}

		public async Task<IEnumerable<PredicateApiViewModel>> GetPredicates(Guid? PredicateUid = null, PredicateType? Type = null, string Name = null, string Inverse = null, bool? IsUsed = null)
		{
			string whereClause = string.Empty;
			List<string> whereConditions = new List<string>();
			var dbArgs = new DynamicParameters();

			if (PredicateUid.HasValue)
			{
				whereConditions.Add("P.Uid = @PredicateUid");
				dbArgs.Add("@PredicateUid", PredicateUid.Value);
			}

			if (Type.HasValue)
			{
				whereConditions.Add("P.Type = @Type");
				dbArgs.Add("@Type", Type.Value);
			}

			if (!string.IsNullOrEmpty(Name) && !string.IsNullOrWhiteSpace(Name))
			{
				Name = Name.Trim().ToLower();
				whereConditions.Add("P.Name = @Name");
				dbArgs.Add("@Name", Name);
			}

			if (!string.IsNullOrEmpty(Inverse) && !string.IsNullOrWhiteSpace(Inverse))
			{
				Inverse = Inverse.Trim().ToLower();
				dbArgs.Add("@Inverse", Inverse);
				whereConditions.Add("P.Inverse = @Inverse");
			}

			if (IsUsed.HasValue)
			{
				if (IsUsed.Value)
				{
					whereConditions.Add("Usage.Id is not null");
				}
				else
				{
					whereConditions.Add("Usage.Id is null");
				}
			}

			if (whereConditions.Count > 0)
			{
				whereClause = $"WHERE {string.Join(" AND ", whereConditions)}";
			}

			var allPredicates = await companyContext.QueryAsync<PredicateApiViewModel>($@"select 
																			 P.Uid,
																			 P.Name,
																			 P.Inverse,
																			 P.IsSystem,
																			 P.[Type],
																			 CASE
																			 WHEN Usage.Id is null then 0
																			 ELSE 1
																			 END AS IsInUse
																			from[Predicate] P
																			outer apply(select top 1 id from IntersectType where PredicateID = P.Id)Usage
																			{whereClause}          
																			order by[Type], Name", dbArgs, ApiTimeout);
			return allPredicates;
		}

		public async Task<JObject> GetRelationships(IEnumerable<KeyValuePair<string, string>> queryParams, string whereClause = "", bool isExport = false, CancellationToken? cancellationToken = null)
		{
			var dbArgs = new DynamicParameters();
			bool includeTotal = true;
			bool includeAssetPath = false;
			bool orderByAssetPath = false;
			bool listColorsAsJSON = false;
			bool includeLegacyData = false;

			string _orderBy = "I.IntersectTypeID,I.ID";
			string _orderDirection = "asc";
			string populateNoReadSQL = " ";

			List<string> TempTableScriptList = new List<string>();
			string TempTableScriptStr = " ";

			var fieldColumns = new DynamicQuerySelects();
			var fieldJoins = new DynamicQueryJoins();

			string filteredIntersectAssets = "";
			string prefilteredIntersectTypesTempTable = "";

			//if filtered by asset uid we will include relationship type name and asset name
			//both relationship type name and asset name depends on which side of relationship are we on
			//both fields are needed for filtering and ordering
			bool isFilteredByAssetUID = false;
			bool isFilteredByAssetTypeUID = false;

			Guid objectUid;
			Guid relationshipTypeUid;
			bool isSubject = false;

			var includedJoins = new DynamicQueryJoins();
			var filterJoins = new DynamicQueryJoins();
			List<string> whereStatements = new List<string>();


			List<string> simpleFilters = new List<string>();
			var simpleFilterTempTables = new StringBuilder();
			List<string> dynamicFilters = new List<string>();
			var dynamicFilterTempTables = new StringBuilder();
			var IntersectTypeTempTables = new StringBuilder();
			var IntersectTempTables = new StringBuilder();

			List<string> fiterIntersectType = new List<string>();
			var applyfilteredIntersectTypes = false;

			List<string> fiterIntersect = new List<string>();
			var AddInnerIntersectType = false;
			var AddInnerIntersect = false;

			List<string> fieldsUsedInMainQuery = new List<string>();
			List<string> filterfieldsUsedInMainQuery = new List<string>();

			//Helping Query Subjectuid/ObjectUid Paramer
			string AssetQuery = $@"Select A.Id as AssetId,A.AssetTypeId, 0 IsReference
									   From Asset A
									   WHERE  [Uid]= @AssetUid
									   union all
									   Select 0 as AssetId,A.ID AssetTypeId, 1 IsReference
									   From AssetType A
									   WHERE  [Uid]= @AssetUid";


			if (!string.IsNullOrEmpty(whereClause))
			{
				whereStatements.Add(whereClause);
			}

			//Check Record Exist for No Read Asset
			//1: Add Condition, 0: Not Add Condition
			var responsibilitySQL = @$"select count(1) from (select top 1 rd.RuleID from dbo.responsibilitydetail rd
										where ResourceID = @ResourceID and ((PermissionsBitMask & @permission) = 0)) a
										option(recompile)";

			var AddrightCondition = companyContext.Database.Connection.Query<int>(responsibilitySQL, new { ResourceID = companyContext.CurrentResourceID, permission = (int)Permission.ReadRelationships }).FirstOrDefault();
			//Create Temporary table to store No Read Asset/AssetTypeID
			if (AddrightCondition == 1)
			{
				populateNoReadSQL = $@"
					declare @ResourceID int = @CurrentResourceID,
							@permission int = @ReadPremission;

					drop table if exists #TempNoPermissionObjects;
					create table #TempNoPermissionObjects (
						AssetID bigint,
						AssetTypeID int
					);
					Create Clustered Index IX_TempNoPermissionObjects on #TempNoPermissionObjects(AssetID,AssetTypeID);

					Insert into #TempNoPermissionObjects
					select distinct AssetID,AssetTypeID
					from ResponsibilityDetail 
					where ResourceID = @ResourceID and ((PermissionsBitMask & @permission) = 0)
					option(recompile)";

				dbArgs.Add("@CurrentResourceID", companyContext.CurrentResourceID);
				dbArgs.Add("@ReadPremission", (int)Permission.ReadRelationships);
			}

			string baseTableSql(bool excludeFilterQueries = false)
			{
				return $@"
				{(excludeFilterQueries ? " from @tempintersect TempI " : " ")} 
				{(excludeFilterQueries ? " inner join [Intersect] I on I.ID = TempI.IntersectID " : " from[Intersect] I ")} 
				inner join IntersectType T on T.ID = I.IntersectTypeID 
				left join [Predicate] P on P.ID = T.PredicateID 
				left join Asset S on S.ID = I.SubjectAssetID 
				left join AssetType ST1 on S.ID is not null and ST1.ID = S.AssetTypeID
				left join AssetType ST2 on S.ID is null and ST2.ID = I.SubjectAssetTypeID and I.SubjectAssetID = 0
				left join Asset O on O.ID = I.ObjectAssetID 
				left join AssetType OT1 on O.ID is not null and OT1.ID = O.AssetTypeID
				left join AssetType OT2 on O.ID is null and OT2.ID = I.ObjectAssetTypeID and I.ObjectAssetID = 0";
			};

			whereStatements.Add(" coalesce(S.ID,ST2.ID) is not null and coalesce(O.ID,OT2.ID) is not null ");

			List<FieldType> fieldTypes = new List<FieldType>();
			bool filteringByFields = false;
			int pageNumber = 1;
			int pageSize = 250;

			//use ISNULL(S.ID,-1)
			//we need isnull as not in statment does not work well null values
			//we need -1 to not match results when asset type id is null (Reference Lists)
			if (AddrightCondition == 1)
			{
				whereStatements.Add(" not exists (select 1 from #TempNoPermissionObjects TNPO_A where TNPO_A.AssetID in (ISNULL(S.ID,-1),ISNULL(O.ID,-1)) and TNPO_A.AssetID > 0) ");
				whereStatements.Add(" not exists (select 1 from #TempNoPermissionObjects TNPO_AT where TNPO_AT.AssetID = 0 and TNPO_AT.AssetTypeID in (ISNULL(S.AssetTypeID,-1),ISNULL(O.AssetTypeID,-1))) ");
			}

			if (queryParams != null)
			{
				var queryParamsList = queryParams.ToList();

				if (queryParamsList.Any(q => q.Key.ToLower() == "relationshiptypeuid"))
				{
					//if the search is by intersecttypeid we should change the default order by to I.ID for consistent results
					_orderBy = "I.ID";
					var relationshipTypeUidString = queryParamsList.FirstOrDefault(q => q.Key.ToLower() == "relationshiptypeuid").Value;
					if (Guid.TryParse(relationshipTypeUidString, out relationshipTypeUid))
					{
						dbArgs.Add("@relationshiptypeuid", relationshipTypeUid);
						fieldTypes = companyContext.Query<FieldType>("select F.* from FieldType F inner join IntersectType I on I.ID = F.IntersectTypeID and I.[Uid] = @relationshipTypeUid", new { relationshipTypeUid }, ApiTimeout).ToList();
						if (fieldTypes != null && fieldTypes.Count() > 0)
						{
							var IntersectTypeID = fieldTypes.FirstOrDefault().IntersectTypeID;
							dbArgs.Add("@IntersectTypeID", IntersectTypeID);
						}

						fiterIntersectType.Add($@"
												insert into @filteredIntersectTypes(ID)
												select IT.id 
												from intersecttype IT
												where IT.uid = cast(@relationshiptypeuid  as uniqueidentifier)
												option (recompile);

												set @AddInnerIntersectType = 1;");
						AddInnerIntersectType = true;

					}
				}
				if (queryParamsList.Any(q => q.Key.ToLower() == "predicateuid"))
				{
					Guid predicateUid;
					var predicateUidString = queryParamsList.FirstOrDefault(q => q.Key.ToLower() == "predicateuid").Value;
					if (Guid.TryParse(predicateUidString, out predicateUid))
					{
						dbArgs.Add("@predicateuid", predicateUid);
						whereStatements.Add($"(P.Uid = @predicateuid)");

						fiterIntersectType.Add(@$"
								if (@AddInnerIntersectType = 0)
									begin
										insert into @filteredIntersectTypes
										select IT.ID 
										from [IntersectType] IT
										inner join [Predicate] P on P.ID = IT.PredicateID and P.UID = @predicateuid
										option (recompile);
									end
								else
									begin
										delete fit
										from @filteredIntersectTypes fit
										where not exists (	select 1 
															from  [IntersectType] IT
															inner join [Predicate] P on P.ID = IT.PredicateID and P.UID = cast(@predicateuid  as uniqueidentifier)
															where it.id = fit.id)
										option (recompile);
									end
								set @AddInnerIntersectType = 1;
									");

						AddInnerIntersectType = true;

					}
				}
				if (queryParamsList.Any(k => k.Key.ToLower() == "_listcolorsasjson"))
				{
					bool.TryParse(queryParams.FirstOrDefault(k => k.Key.ToLower() == "_listcolorsasjson").Value, out listColorsAsJSON);
				}
				if (queryParamsList.Any(k => k.Key.ToLower() == "includelegacydata"))
				{
					bool.TryParse(queryParams.FirstOrDefault(k => k.Key.ToLower() == "includelegacydata").Value, out includeLegacyData);
				}
				if (queryParamsList.Any(q => q.Key.ToLower() == "state"))
				{
					State state;
					var stateString = queryParamsList.FirstOrDefault(q => q.Key.ToLower() == "state").Value;
					if (Enum.TryParse(stateString, out state))
					{
						dbArgs.Add("@state", state);
						whereClause += (string.IsNullOrEmpty(whereClause) ? " where" : " and") + $" I.[State] = @state";
					}
				}
				if (queryParamsList.Any(q => q.Key.ToLower() == "uid"))
				{
					Guid uid;
					var uidString = queryParamsList.FirstOrDefault(q => q.Key.ToLower() == "uid").Value;
					if (Guid.TryParse(uidString, out uid))
					{
						dbArgs.Add("@uid", uid);
						whereStatements.Add($" I.[Uid] = @uid");

						fiterIntersect.Add($@"
												insert into @filteredIntersect(ID)
												select I.ID 
												from [Intersect] I
												where fI.id is null and I.uid =cast(@uid as uniqueidentifier)
												option (recompile);

												set @AddInnerIntersect = 1;");
						AddInnerIntersect = true;

					}
				}

				if (queryParamsList.Any(q => q.Key.ToLower() == "subjectuid"))
				{
					Guid subjectUid;
					var subjectUidString = queryParamsList.FirstOrDefault(q => q.Key.ToLower() == "subjectuid").Value;
					if (Guid.TryParse(subjectUidString, out subjectUid))
					{
						long SubjAssetID = -1;
						int SubjAssetTypeID = 0;
						bool SubjIsReferenceList = false;

						var assetSubj = companyContext.Query<dynamic>(AssetQuery, new { AssetUid = subjectUid }, ApiTimeout).FirstOrDefault();

						if (assetSubj != null)
						{
							SubjAssetID = assetSubj?.AssetId;
							SubjAssetTypeID = assetSubj?.AssetTypeId;
							SubjIsReferenceList = (assetSubj?.IsReference == 1 ? true : false);
						}

						if (SubjIsReferenceList)
						{
							fiterIntersect.Add($@"
												if ( @AddInnerIntersect = 0 )
													begin
														insert into @filteredIntersect(ID)
														select I.ID 
														from [Intersect] I
														where I.subjectAssetTypeID = cast(@SubjAssetTypeID as int) and I.subjectAssetID = 0
														option (recompile);

														set @AddInnerIntersect = 1;
													end
												else
													begin
														delete fi
														from @filteredIntersect fi
														where not exists (select 1  
																		 from [Intersect] I
																		 where I.subjectAssetTypeID = cast(@SubjAssetTypeID as int) and I.subjectAssetID = 0
																		 and i.id = fi.id)
														option (recompile);
													end
												");
							dbArgs.Add("@SubjAssetTypeID", SubjAssetTypeID);
						}
						else
						{
							fiterIntersect.Add($@"
												if ( @AddInnerIntersect = 0 )
													begin
														insert into @filteredIntersect(ID)
														select I.ID 
														from [Intersect] I
														where I.SubjectAssetId = cast(@SubjAssetID as bigint)
														option (recompile);

														set @AddInnerIntersect = 1;
													end
												else
													begin
														delete fi
														from @filteredIntersect fi
														where not exists (select 1 
																		 from [Intersect] I
																		 where I.SubjectAssetId = cast(@SubjAssetID as bigint) and I.Id = fi.Id)
														option (recompile);
													end
												");
							dbArgs.Add("@SubjAssetID", SubjAssetID);
						}
						AddInnerIntersect = true;
					}
				}
				if (queryParamsList.Any(q => q.Key.ToLower() == "objectuid"))
				{
					var objectUidString = queryParamsList.FirstOrDefault(q => q.Key.ToLower() == "objectuid").Value;
					if (Guid.TryParse(objectUidString, out objectUid))
					{
						long ObjAssetID = -1;
						int ObjAssetTypeID = 0;
						bool ObjIsReferenceList = false;

						var assetObj = companyContext.Query<dynamic>(AssetQuery, new { AssetUid = objectUid }, ApiTimeout).FirstOrDefault();

						if (assetObj != null)
						{
							ObjAssetID = assetObj?.AssetId;
							ObjAssetTypeID = assetObj?.AssetTypeId;
							ObjIsReferenceList = (assetObj?.IsReference == 1 ? true : false);
						}

						if (ObjIsReferenceList)
						{
							fiterIntersect.Add($@"
												if ( @AddInnerIntersect = 0 )
													begin
														insert into @filteredIntersect(ID)
														select I.ID 
														from [Intersect] I
														where I.ObjectAssetTypeID = cast(@ObjAssetTypeID as int)  and I.ObjectAssetID = 0
														option (recompile);
														set @AddInnerIntersect = 1;
													end
												else
													begin
														delete fi
														from @filteredIntersect fi
														where not exists (select 1  
																		 from [Intersect] I
																		 where I.ObjectAssetTypeID = cast(@ObjAssetTypeID as int) and I.ObjectAssetID = 0
																		 and i.id = fi.id)
														option (recompile);
													end
												");
							dbArgs.Add("@ObjAssetTypeID", ObjAssetTypeID);
						}
						else
						{
							fiterIntersect.Add($@"
												if ( @AddInnerIntersect = 0 )
													begin
														insert into @filteredIntersect(ID)
														select I.ID 
														from [Intersect] I
														where I.ObjectAssetId = cast(@ObjAssetID as bigint)
														option (recompile);
														set @AddInnerIntersect = 1;
													end
												else
													begin
														delete fi
														from @filteredIntersect fi
														where not exists (select 1 
																		 from [Intersect] I
																		 where I.ObjectAssetId = cast(@ObjAssetID as bigint) and I.Id = fi.Id)
														option (recompile);
													end
												");
							dbArgs.Add("@ObjAssetID", ObjAssetID);
						}
						AddInnerIntersect = true;
					}
				}
				if (queryParamsList.Any(q => q.Key.ToLower() == "assetuid"))
				{
					Guid assetUid;
					var assetUidString = queryParamsList.FirstOrDefault(q => q.Key.ToLower() == "assetuid").Value;
					if (Guid.TryParse(assetUidString, out assetUid))
					{
						var asset = companyContext.Assets.Where(x => x.uid == assetUid).Select(x => new { x.ID }).FirstOrDefault();

						if (asset != null)
						{
							isFilteredByAssetUID = true;

							dbArgs.Add("@assetId", asset.ID);

							filteredIntersectAssets = @$"

							drop table if exists #tempassettypedata;

							with rsdata as
							(
							select distinct AssetTypeID
							from (
							select I.ObjectAssetTypeID AssetTypeID
							from [Intersect] I
							{(AddInnerIntersect ? "inner join @filteredIntersect fI on I.ID = fI.ID" : "")}
							{(AddInnerIntersectType ? "inner join @filteredIntersectTypes fit on fit.id = I.Intersecttypeid" : "")}
							where I.SubjectAssetID = cast(@assetId as bigint)
							union all
							select I.SubjectAssetTypeID AssetTypeID
							from [Intersect] I
							{(AddInnerIntersect ? "inner join @filteredIntersect fI on I.ID = fI.ID" : "")}
							{(AddInnerIntersectType ? "inner join @filteredIntersectTypes fit on fit.id = I.Intersecttypeid" : "")}
							where I.ObjectAssetID = cast(@assetId as bigint)
							) a
							)
							select rsdata.AssetTypeID,ATPath.[Path]
							into #tempassettypedata
							from rsdata
							cross apply dbo.GetAssetTypeTextPathById(rsdata.AssetTypeID, ' > ') ATPath
							option (recompile);

							create index ix_tempassettypedata on #tempassettypedata(AssetTypeID) include ([Path]);

							drop table if exists #filteredIntersectAssets;

							create table #filteredIntersectAssets (ID int,RelationshipTypeName Nvarchar(4000),AssetPath Nvarchar(2000));
							create clustered index ix_filteredIntersectAssets on #filteredIntersectAssets(ID);

							insert into #filteredIntersectAssets
							select  I.ID,
									P.Name + ' ' + isnull(ATPath.[Path],'---') RelationshipTypeName,
									ISNULL(AP.DisplayPath,OT2.Name) AssetPath
							from [Intersect] I
							{(AddInnerIntersect ? "inner join @filteredIntersect fI on I.ID = fI.ID" : "")}
							{(AddInnerIntersectType ? "inner join @filteredIntersectTypes fit on fit.id = I.IntersectTypeID" : "")}
							inner join [IntersectType] IT on IT.ID = I.IntersectTypeID
							inner join [Predicate] p on IT.PredicateID = P.ID
							left join AssetPath AP on AP.Id = I.ObjectAssetID
							left join AssetType OT2 on OT2.ID = I.ObjectAssetTypeID
							left join #tempassettypedata  ATPath on ATPath.AssetTypeID = I.ObjectAssetTypeID
							where I.SubjectAssetID = cast(@assetId as bigint)
							option(recompile);

							insert into #filteredIntersectAssets
							select  I.ID,
									P.Inverse + ' ' + isnull(ATPath.[Path],'---') RelationshipTypeName,
									ISNULL(AP.DisplayPath,ST2.Name) AssetPath
							from [Intersect] I
							{(AddInnerIntersect ? "inner join @filteredIntersect fI on I.ID = fI.ID" : "")}
							{(AddInnerIntersectType ? "inner join @filteredIntersectTypes fit on fit.id = I.IntersectTypeID" : "")}
							inner join [IntersectType] IT on IT.ID = I.IntersectTypeID
							inner join [Predicate] p on IT.PredicateID = P.ID
							left join AssetPath AP on AP.Id = I.SubjectAssetID
							left join AssetType ST2 on ST2.ID = I.SubjectAssetTypeID 
							Left outer join #filteredIntersectAssets fia on fia.id = I.ID
							left join #tempassettypedata  ATPath on ATPath.AssetTypeID = I.SubjectAssetTypeID
							where fia.id is null and I.ObjectAssetID = cast(@assetId as bigint)
							option(recompile);
							";
						}
						else
						{
							isFilteredByAssetTypeUID = true;
							var type = companyContext.AssetTypes.Where(x => x.uid == assetUid).Select(x => new { x.ID }).FirstOrDefault();
							dbArgs.Add("@assetTypeId", type.ID);
							filteredIntersectAssets = @$"

							drop table if exists #tempassettypedata;

							with rsdata as
							(
							select distinct AssetTypeID
							from (
							select I.ObjectAssetTypeID AssetTypeID
							from [Intersect] I
							{(AddInnerIntersect ? "inner join @filteredIntersect fI on I.ID = fI.ID" : "")}
							{(AddInnerIntersectType ? "inner join @filteredIntersectTypes fit on fit.id = I.Intersecttypeid" : "")}
							where I.SubjectAssetTypeID = cast(@assetTypeId as int) and I.SubjectAssetID = 0
							union all
							select I.SubjectAssetTypeID AssetTypeID
							from [Intersect] I
							{(AddInnerIntersect ? "inner join @filteredIntersect fI on I.ID = fI.ID" : "")}
							{(AddInnerIntersectType ? "inner join @filteredIntersectTypes fit on fit.id = I.Intersecttypeid" : "")}
							where I.ObjectAssetTypeID = cast(@assetTypeId as int) and I.ObjectAssetID = 0
							) a
							)
							select rsdata.AssetTypeID,ATPath.[Path]
							into #tempassettypedata
							from rsdata
							cross apply dbo.GetAssetTypeTextPathById(rsdata.AssetTypeID, ' > ') ATPath
							option (recompile);

							create index ix_tempassettypedata on #tempassettypedata(AssetTypeID) include ([Path]);

							drop table if exists #filteredIntersectAssets;

							create table #filteredIntersectAssets (ID int,RelationshipTypeName Nvarchar(max),AssetPath Nvarchar(max));
							create clustered index ix_filteredIntersectAssets on #filteredIntersectAssets(ID);

							insert into #filteredIntersectAssets
							select  I.ID,
									P.Name + ' ' + isnull(ATPath.[Path],'---') RelationshipTypeName,
									ISNULL(AP.DisplayPath,OT2.Name) AssetPath
							from [Intersect] I
							{(AddInnerIntersect ? "inner join @filteredIntersect fI on I.ID = fI.ID" : "")}
							{(AddInnerIntersectType ? "inner join @filteredIntersectTypes fit on fit.id = I.IntersectTypeID" : "")}
							inner join [IntersectType] IT on IT.ID = I.IntersectTypeID
							inner join [Predicate] p on IT.PredicateID = P.ID
							left join AssetPath AP on AP.Id = I.ObjectAssetID
							left join AssetType OT2 on OT2.ID = I.ObjectAssetTypeID 
							left join #tempassettypedata  ATPath on ATPath.AssetTypeID = I.ObjectAssetTypeID
							where I.SubjectAssetTypeID = cast(@assetTypeId as int) and I.SubjectAssetID = 0
							option(recompile);

							insert into #filteredIntersectAssets
							select  I.ID,
									P.Inverse + ' ' + isnull(ATPath.[Path],'---') RelationshipTypeName,
									ISNULL(AP.DisplayPath,ST2.Name) AssetPath
							from [Intersect] I
							{(AddInnerIntersect ? "inner join @filteredIntersect fI on I.ID = fI.ID" : "")}
							{(AddInnerIntersectType ? "inner join @filteredIntersectTypes fit on fit.id = I.IntersectTypeID" : "")}
							inner join [IntersectType] IT on IT.ID = I.IntersectTypeID
							inner join [Predicate] p on IT.PredicateID = P.ID
							left join AssetPath AP on AP.Id = I.SubjectAssetID
							left join AssetType ST2 on ST2.ID = I.SubjectAssetTypeID
							Left outer join #filteredIntersectAssets fia on fia.id = I.ID
							left join #tempassettypedata  ATPath on ATPath.AssetTypeID = I.SubjectAssetTypeID
							where fia.id is null and I.ObjectAssetTypeID = cast(@assetTypeId as int) and I.ObjectAssetID = 0
							option(recompile);
							";
						}

						//sort by relationship type then by asset
						_orderBy = "cast(RelationshipSideData.RelationshipTypeName as nvarchar(850)),cast(RelationshipSideData.AssetPath as nvarchar(850))";
						fieldsUsedInMainQuery.Add("RelationshipSideData");
					}
				}
				if (queryParamsList.Any(q => q.Key.ToLower() == "_pagenum"))
				{
					var pageNumberString = queryParamsList.FirstOrDefault(q => q.Key.ToLower() == "_pagenum").Value;
					if (!int.TryParse(pageNumberString, out pageNumber))
					{
						pageNumber = 1;
					}
				}
				if (queryParamsList.Any(q => q.Key.ToLower() == "_pagesize"))
				{
					var pageSizeString = queryParamsList.FirstOrDefault(q => q.Key.ToLower() == "_pagesize").Value;
					if (!int.TryParse(pageSizeString, out pageSize))
					{
						pageSize = 250;
					}
				}

				if (queryParamsList.Any(q => q.Key.ToLower() == "_includetotal"))
				{
					if (!bool.TryParse(queryParamsList.FirstOrDefault(q => q.Key.ToLower() == "_includetotal").Value, out includeTotal))
					{
						includeTotal = true;
					}
				}

				if (queryParamsList.Any(q => q.Key.ToLower() == "_includepath"))
				{
					if (!bool.TryParse(queryParamsList.FirstOrDefault(q => q.Key.ToLower() == "_includepath").Value, out includeAssetPath))
					{
						includeAssetPath = false;
					}
				}

				if (fieldTypes != null && fieldTypes.Count() > 0)
				{
					getFieldSql(fieldTypes, dbArgs, fieldJoins, fieldColumns, "i.Id", listColorsAsJSON, objectType: SystemObjects.Intersect, IsCreateTempTable: true, TempTableScriptList: TempTableScriptList);
					TempTableScriptStr = string.Join("\n ", TempTableScriptList);
				}

				// Now deal with dynamic field filters
				if (fieldTypes != null && fieldTypes.Count() > 0)
				{
					var avoidFields = new List<string> { "relationshiptypeuid", "subjectuid", "objectuid", "predicateuid", "_pagenum", "_pagesize", "state" };
					queryParamsList.ForEach(qp =>
					{
						if (!avoidFields.Contains(qp.Key.ToLower()))
						{
							var fieldType = fieldTypes.FirstOrDefault(i => i.Name.ToLower() == qp.Key.ToLower());
							if (fieldType != null)
							{

								bool isNumber = decimal.TryParse(qp.Value.Trim('%'), out _);
								bool isNumbericFieldType = fieldType.Type == DataType.Number.ToString() || fieldType.Type == DataType.Decimal.ToString();

								if (!(!isNumber && isNumbericFieldType))
								{
									var select = fieldColumns.Selects().FirstOrDefault(x => x.FieldIdentifier == fieldType.ID.ToString());
									var join = fieldJoins.Joins().FirstOrDefault(x => x.FieldIdentifier == fieldType.ID.ToString());

									if (select != null && join != null)
									{
										var selectField = select.StatementWithoutColumnName;
										var joinStatement = !string.IsNullOrEmpty(join.SimpleStatement) ? join.SimpleStatement : join.SQLStatement;

										if (join.FieldFilter != null)
										{
											dynamicFilterTempTables.AppendLine(join.FieldFilter.SimpleFilterTempTable);
											dynamicFilters.Add(join.FieldFilter.SimpleFilterStatement);
										}
										else
										{
											dynamicFilters.Add($@"
											select  I.ID
											from  [Intersect] I 
											{(AddInnerIntersect ? "inner join @filteredIntersect fI on I.ID = fI.ID" : "")}
											{(AddInnerIntersectType ? "inner join @filteredIntersectTypes fit on fit.id = I.IntersectTypeid" : "")}
											{joinStatement}
											left join #TempDynamicFilter tfa on tfa.IntersectId = I.ID
											where tfa.IntersectId is null and I.IntersectTypeID = cast( @IntersectTypeID as int) and {selectField} = @f{fieldType.ID}Value
											option(recompile)");
										}
									}
									dbArgs.Add($"@f{fieldType.ID}Value", qp.Value);
									filteringByFields = true;
								}
							}
						}
					});
				}
			}

			if (queryParams.Any(x => x.Key.ToLower() == "_order"))
			{
				var orderValue = queryParams.FirstOrDefault(x => x.Key.ToLower() == "_order").Value.ToLower(System.Globalization.CultureInfo.InvariantCulture);
				var joinColumn = fieldColumns.GetStatements().FirstOrDefault(x => x.ToLower().Contains($"[{orderValue}]"));
				if (!string.IsNullOrEmpty(joinColumn))
				{
					var select = fieldColumns.Selects().FirstOrDefault(x => x.Statement == joinColumn);
					if (select != null)
					{
						fieldsUsedInMainQuery.Add(select.FieldIdentifier);
					}
					_orderBy = joinColumn.Substring(0, joinColumn.IndexOf(" as ["));
				}
				else if (orderValue == "object.[path]")
				{
					_orderBy = "cast(ISNULL(ANDP_Object.DisplayPath,OT2.Name) as nvarchar(850))";
					isSubject = true;
					orderByAssetPath = true;
					fieldsUsedInMainQuery.Add("AssetPathObject");
				}
				else if (orderValue == "subject.[path]")
				{
					_orderBy = "cast(ISNULL(ANDP_Subject.DisplayPath,ST2.Name) as nvarchar(850))";
					orderByAssetPath = true;
					fieldsUsedInMainQuery.Add("AssetPathSubject");
				}
				else if (orderValue == "relationshiptypename")
				{
					_orderBy = "cast(RelationshipSideData.RelationshipTypeName as nvarchar(850))";
					orderByAssetPath = true;
				}
				else if (orderValue == "assetpath")
				{
					_orderBy = "cast(RelationshipSideData.AssetPath as nvarchar(850))";
					orderByAssetPath = true;
				}
			}
			if (queryParams.ToList().Any(x => x.Key.ToLower() == "_simplefilter"))
			{
				var simpleFilter = queryParams.FirstOrDefault(x => x.Key.ToLower() == "_simplefilter").Value.Trim();

				if (!string.IsNullOrEmpty(simpleFilter))
				{
					filteringByFields = true;
					bool isNumber = decimal.TryParse(simpleFilter.Trim('%'), out _);
					simpleFilter = companyContext.GetEscapedFilterString(simpleFilter);

					dbArgs.Add("@simpleFilter", simpleFilter);

					//There may be multiple OwnershipLookup fields, but they all look to the same table for filtering, so that will be dealt with below
					foreach (var ft in fieldTypes.Where(x => x.IsListable == true && x.Type != DataType.OwnershipLookup.ToString()))
					{
						bool isNumbericFieldType = ft.Type == DataType.Number.ToString() || ft.Type == DataType.Decimal.ToString();

						if (!isNumber && isNumbericFieldType)
						{
							//if search term is not a number, do not filter over numeric field types
							continue;
						}

						var select = fieldColumns.Selects().FirstOrDefault(x => x.FieldIdentifier == ft.ID.ToString());
						var join = fieldJoins.Joins().FirstOrDefault(x => x.FieldIdentifier == ft.ID.ToString());

						if (select != null && join != null)
						{
							var selectField = select.StatementWithoutColumnName;
							var joinStatement = !string.IsNullOrEmpty(join.SimpleStatement) ? join.SimpleStatement : join.SQLStatement;

							if (join.FieldFilter != null)
							{
								simpleFilterTempTables.AppendLine(join.FieldFilter.SimpleFilterTempTable);
								simpleFilters.Add(join.FieldFilter.SimpleFilterStatement);
							}
							else
							{
								simpleFilters.Add($@"
								select  I.ID
								from  [Intersect] I 
								{(AddInnerIntersect ? "inner join @filteredIntersect fI on I.ID = fI.ID" : "")}
								{(AddInnerIntersectType ? "inner join @filteredIntersectTypes fit on fit.id = I.IntersectTypeid" : "")}
								{joinStatement}
								left join #TempSimpleFilter tfa on tfa.IntersectId = I.ID
								where tfa.IntersectId is null and I.IntersectTypeID = @IntersectTypeID and {selectField} like @simpleFilter
								option(recompile)");
							}
						}
					}

					if (includeAssetPath)
					{
						if (isSubject)
						{
							simpleFilters.Add($@"
									select  I.ID
									from  [intersect] I
									{(AddInnerIntersect ? "inner join @filteredIntersect fI on I.ID = fI.ID" : "")}
									{(AddInnerIntersectType ? "inner join @filteredIntersectTypes fit on fit.id = I.IntersectTypeid" : "")}
									left join AssetPath ANDP_Object on ANDP_Object.Id = I.ObjectAssetID
									left join [AssetType] OT2 on I.ObjectAssetTypeID = OT2.ID 
									left join #TempSimpleFilter tfa on tfa.IntersectId = I.ID
									where tfa.IntersectId is null and ISNULL(ANDP_Object.DisplayPath,OT2.Name) like @simpleFilter
									option(recompile)");
						}
						else
						{
							simpleFilters.Add($@"
									select  I.ID
									from  [intersect] I
									{(AddInnerIntersect ? "inner join @filteredIntersect fI on I.ID = fI.ID" : "")}
									{(AddInnerIntersectType ? "inner join @filteredIntersectTypes fit on fit.id = I.IntersectTypeid" : "")}
									left join AssetPath ANDP_Subject on ANDP_Subject.Id = I.SubjectAssetID
									left join [AssetType] ST2 on ST2.ID = I.SubjectAssetTypeID
									left join #TempSimpleFilter tfa on tfa.IntersectId = I.ID
									where tfa.IntersectId is null and ISNULL(ANDP_Subject.DisplayPath,ST2.Name) like @simpleFilter
									option(recompile)");
						}
					}

					if (isFilteredByAssetUID || isFilteredByAssetTypeUID)
					{
						simpleFilters.Add($@"
							select  RelationshipSideData.ID
							from  #filteredIntersectAssets RelationshipSideData 
							left join #TempSimpleFilter tfa on tfa.IntersectId = RelationshipSideData.ID
							where tfa.IntersectId is null and RelationshipSideData.RelationshipTypeName like @simpleFilter
							option(recompile)");

						simpleFilters.Add($@"
							select  RelationshipSideData.ID
							from  #filteredIntersectAssets RelationshipSideData 
							left join #TempSimpleFilter tfa on tfa.IntersectId = RelationshipSideData.ID
							where tfa.IntersectId is null and RelationshipSideData.AssetPath like @simpleFilter
							option(recompile)");
					}
				}
			}

			if (queryParams.ToList().Any(x => x.Key.ToLower() == "_filter"))
			{
				var value = queryParams.FirstOrDefault(x => x.Key.ToLower() == "_filter").Value;

				if (!string.IsNullOrEmpty(value))
				{
					filteringByFields = true;

					var tempArgs = new DynamicParameters();
					var tempJoins = new DynamicQueryJoins();
					var tempFieldColumns = new DynamicQuerySelects();

					getFieldSql(fieldTypes, tempArgs, tempJoins, tempFieldColumns);

					var filterDataProvider = new FilterDataProvider(companyContext);

					var filterExpressionParser = new FilterExpressionParser(filterDataProvider, FilterExpressionParseType.RelationshipCustomFields);
					filterExpressionParser.LoadFieldTypes(fieldTypes, tempFieldColumns.GetStatements());
					var fieldsQuery = filterExpressionParser.Parse(value, out Dictionary<string, object> sqlParams, out List<int> filteredFields);

					foreach (var ff in filteredFields)
					{
						filterfieldsUsedInMainQuery.Add(ff.ToString());
					}

					whereStatements.Add(fieldsQuery);

					foreach (var item in sqlParams)
					{
						dbArgs.Add(item.Key, item.Value);
					}
				}
			}

			if (queryParams.Any(x => x.Key.ToLower() == "_direction"))
			{
				_orderDirection = queryParams.FirstOrDefault(x => x.Key.ToLower() == "_direction").Value.ToLower(System.Globalization.CultureInfo.InvariantCulture);
			}

			if (pageNumber < 0)
			{
				pageNumber = 1;
			}

			if (pageSize < 0 || pageSize > 5000)
			{
				pageSize = 5000;
			}

			dbArgs.Add("@pageNum", pageNumber);
			dbArgs.Add("@pageSize", pageSize);

			var stateSql = "case I.State ";
			State.Active.GetList().ForEach(s =>
			{
				stateSql += $"when {(int)s.ID} then '{s.ID.ToString()}' ";
			});
			stateSql += " end as State, ";

			var predicateTypeSql = "case P.Type ";
			PredicateType.DataLineage.GetAsList().ForEach(p =>
			{
				predicateTypeSql += $"when {(int)p.ID} then '{p.ID.ToString()}' ";
			});
			predicateTypeSql += " end as 'Predicate.Type', ";

			if (orderByAssetPath || includeAssetPath)
			{
				fieldJoins.Add(" left join AssetPath ANDP_Object on ANDP_Object.Id = O.Id ", "AssetPathObject");
				fieldJoins.Add(" left join AssetPath ANDP_Subject on ANDP_Subject.Id = S.Id ", "AssetPathSubject");
			}

			if (isExport)
			{
				fieldJoins.Add(" left join AssetDisplayValue ADVS on S.ID = ADVS.AssetID ", null);
				fieldJoins.Add(" left join AssetDisplayValue ADVO on O.ID = ADVO.AssetID ", null);
				fieldJoins.Add(" outer apply dbo.GetAssetTypeTextPathById(S.AssetTypeID, ' > ') PS ", null);
				fieldJoins.Add(" outer apply dbo.GetAssetTypeTextPathById(O.AssetTypeID, ' > ') PO ", null);
			}

			if (isFilteredByAssetUID || isFilteredByAssetTypeUID)
			{
				//apply data to check which side on relationship are we
				fieldJoins.Add(@"inner join  #filteredIntersectAssets RelationshipSideData on RelationshipSideData.ID = I.ID ", "RelationshipSideData");
			}

			var whereSql = "";
			if (whereStatements.Any())
			{
				whereSql = $"where {string.Join(" and ", whereStatements)}";
			}

			fieldsUsedInMainQuery.Distinct().ToList().ForEach(field =>
			{
				var joins = fieldJoins.Joins().Where(x => x.SQLStatement.ToLowerInvariant().Contains(field.ToLowerInvariant()));
				if (!(joins != null && joins.Count() > 0))
				{
					joins = fieldJoins.Joins().Where(x => x.FieldIdentifier.ToLowerInvariant().Contains(field.ToLowerInvariant()));
				}
				includedJoins.AddRange(joins);
			});

			filterfieldsUsedInMainQuery.Distinct().ToList().ForEach(field =>
			{
				var joins = fieldJoins.Joins().Where(x => x.SQLStatement.ToLowerInvariant().Contains(field.ToLowerInvariant()));
				if (!(joins != null && joins.Count() > 0))
				{
					joins = fieldJoins.Joins().Where(x => x.FieldIdentifier.ToLowerInvariant().Contains(field.ToLowerInvariant()));
				}
				filterJoins.AddRange(joins);
			});

			string simpleFiltersTempTablesQuery = "";
			if (dbArgs.ParameterNames.Contains("simpleFilter") && simpleFilters.Count()>0)
			{
				simpleFilterTempTables.AppendLine("drop table if exists #TempSimpleFilter");
				simpleFilterTempTables.AppendLine("create table #TempSimpleFilter(IntersectId int)");
				simpleFilterTempTables.AppendLine("create index ix_TempSimpleFilter on #TempSimpleFilter (IntersectId)");

				for (int i = 0; i < simpleFilters.Count; i++)
				{
					simpleFilterTempTables.AppendLine("insert into #TempSimpleFilter");
					simpleFilterTempTables.AppendLine(simpleFilters[i]);
				}

				simpleFilterTempTables.Remove(simpleFilterTempTables.Length - 1, 1);
				simpleFiltersTempTablesQuery = simpleFilterTempTables.ToString();
			}

			string dynamicFiltersTempTablesQuery = "";
			if (dynamicFilters != null && dynamicFilters.Count() > 0)
			{
				dynamicFilterTempTables.AppendLine("drop table if exists #TempDynamicFilter");
				dynamicFilterTempTables.AppendLine("create table #TempDynamicFilter(IntersectId int)");
				dynamicFilterTempTables.AppendLine("create index ix_TempDynamicFilter on #TempDynamicFilter (IntersectId)");

				for (int i = 0; i < dynamicFilters.Count; i++)
				{
					dynamicFilterTempTables.AppendLine("insert into #TempDynamicFilter");
					dynamicFilterTempTables.AppendLine(dynamicFilters[i]);
				}

				dynamicFilterTempTables.Remove(dynamicFilterTempTables.Length - 1, 1);
				dynamicFiltersTempTablesQuery = dynamicFilterTempTables.ToString();
			}

			string IntersectTypeTempTablesQuery = "";
			if (AddInnerIntersectType)
			{
				IntersectTypeTempTables.AppendLine("declare @AddInnerIntersectType bit = 0;");
				IntersectTypeTempTables.AppendLine("declare @filteredIntersectTypes table (Id int,index ix_filteredIntersectTypes (Id))");


				for (int i = 0; i < fiterIntersectType.Count; i++)
				{
					IntersectTypeTempTables.AppendLine(fiterIntersectType[i]);
				}

				IntersectTypeTempTables.Remove(IntersectTypeTempTables.Length - 1, 1);
				IntersectTypeTempTablesQuery = IntersectTypeTempTables.ToString();
			}

			string IntersectTempTablesQuery = "";
			if (AddInnerIntersect)
			{
				IntersectTempTables.AppendLine("declare @AddInnerIntersect bit = 0;");
				IntersectTempTables.AppendLine("declare @filteredIntersect table(Id int, index ix_filteredIntersect (Id))");

				for (int i = 0; i < fiterIntersect.Count; i++)
				{
					IntersectTempTables.AppendLine(fiterIntersect[i]);
				}

				IntersectTempTables.Remove(IntersectTempTables.Length - 1, 1);
				IntersectTempTablesQuery = IntersectTempTables.ToString();
			}

			bool useSimpleFilterTempTable = simpleFiltersTempTablesQuery.Length > 0;
			bool useDynamicTempTable = dynamicFiltersTempTablesQuery.Length > 0;

			bool containsAnyFilter = useSimpleFilterTempTable || useDynamicTempTable || AddInnerIntersect || AddInnerIntersectType;

			bool addWhereSql = true;

			string GetBaseQuery(bool excludeFilterQueries = false)
			{
				return $@"
				select  I.ID
				{baseTableSql()} 
				{(excludeFilterQueries && containsAnyFilter ? "inner join #filtered_results fr on fr.IntersectId = I.ID" : "")}
				{(!excludeFilterQueries && useSimpleFilterTempTable ? "inner join #TempSimpleFilter ta on ta.IntersectId = I.ID" : "")}
				{(!excludeFilterQueries && AddInnerIntersect ? "inner join @filteredIntersect fI on I.ID = fI.ID" : "")}
				{(!excludeFilterQueries && AddInnerIntersectType ? "inner join @filteredIntersectTypes fit on fit.id = T.Id" : "")}
				{(!excludeFilterQueries && useDynamicTempTable ? "inner join #TempDynamicFilter df on df.IntersectId = I.ID" : "")}

				{includedJoins.SQLSimpleStatement}
				{(addWhereSql ? filterJoins.SQLSimpleStatement : "")}
				{(addWhereSql ? whereSql : "")}
				";
			};

			var filteredResultsTempTable = "";

			if (containsAnyFilter)
			{
				filteredResultsTempTable = @$"
				{simpleFiltersTempTablesQuery}
				drop table if exists #filtered_results
				create table #filtered_results (IntersectId int)

				insert into #filtered_results
				{GetBaseQuery()}
				option (recompile);";

				addWhereSql = false;
			}

			string orderByClause = $"order by {_orderBy} {_orderDirection} {(!_orderBy.Contains("I.ID") ? ",I.ID " : "")}";

			var baseSQL = $@"
				{populateNoReadSQL}
				{IntersectTypeTempTablesQuery}
				{IntersectTempTablesQuery}
				{dynamicFiltersTempTablesQuery}
				{TempTableScriptStr}
				{filteredIntersectAssets}
				{filteredResultsTempTable}
				
				declare @tempintersect table (id int identity(1,1), IntersectId int,
				index ix_tempintersect_id clustered (Id)
				)

				insert into @tempintersect
				{GetBaseQuery(true)}
				{orderByClause}
				offset ((@pageNum-1) * @pageSize) rows fetch next @pageSize rows only
				option (recompile);";

			string countSQL = "";
			if (includeTotal)
			{
				if (containsAnyFilter)
				{
					countSQL = "select @total = count(1) from #filtered_results option(recompile);";
				}
				else
				{
					countSQL = $"select @total = count(1) from ({GetBaseQuery(true)}) a option(recompile);";
				}
			}

			string fieldColumnsSql = "";
			if (fieldColumns.Any())
				fieldColumnsSql = string.Join(",\n", fieldColumns.GetStatements()) + ",";

			var sql = $@"

declare @total int;

{countSQL}

select	
@pageSize as 'pageSize',
@pageNum as 'pageNum',
@total as 'total',
(
select	lower(I.Uid) as Uid,
		lower(T.Uid) as RelationshipTypeUid,
		{(isFilteredByAssetUID || isFilteredByAssetTypeUID ? @"RelationshipSideData.RelationshipTypeName,
		RelationshipSideData.AssetPath," : "")}
		{stateSql}
		{fieldColumnsSql}
		lower(P.UID) as 'Predicate.Uid',
		{predicateTypeSql}
		P.Name as 'Predicate.Name',
		P.Inverse as 'Predicate.Inverse',
		lower(S.Uid) as 'Subject.Uid',
		ISNULL(lower(ST1.Uid),lower(ST2.Uid)) as 'Subject.AssetTypeUid'
		{(includeAssetPath ? ",ISNULL(ANDP_Subject.DisplayPath,ST2.Name) as 'Subject.[Path]'" : "")}
		{(isExport ? ",PS.[Path] as 'Subject.AssetTypePath'" : "")}                
		{(isExport ? ",ADVS.DisplayValue as 'Subject.DisplayName'" : "")}
		,lower(O.Uid) as 'Object.Uid'
		,ISNULL(lower(OT1.Uid),lower(OT2.Uid)) as 'Object.AssetTypeUid'
		{(isExport ? ",ADVO.DisplayValue as 'Object.DisplayName'" : "")}
		{(isExport ? ",PO.[Path] as 'Object.AssetTypePath'" : "")}
		{(includeAssetPath ? ",ISNULL(ANDP_Object.DisplayPath,OT2.Name) as 'Object.[Path]'" : "")}
				
		{baseTableSql(true)}
		{string.Join("\n", fieldJoins.GetStatements())}
		order by TempI.ID
		for json path,INCLUDE_NULL_VALUES
) as 'items'
for json path, WITHOUT_ARRAY_WRAPPER
OPTION(RECOMPILE)";

			var getAllQuery = $"{baseSQL} {sql}";

			var models = await companyContext.ExecuteGetRelationshipQuery<JObject>(getAllQuery, cancellationToken.Value, dbArgs, ApiTimeout);

			return models;
		}

		public async Task<JObject> GetRelationship(Guid uid)
		{
			var dbArgs = new DynamicParameters();

			var baseTableSql = @"from [Intersect] I 
								inner join IntersectType T on T.ID = I.IntersectTypeID 
								left join [Predicate] P on P.ID = T.PredicateID 
								left join Asset S on S.ID = I.SubjectAssetID 
								left join AssetType ST1 on S.ID is not null and ST1.ID = S.AssetTypeID
								left join AssetType ST2 on S.ID is null and ST2.ID = I.SubjectAssetTypeID 
								left join dbo.AssetPath SKP on SKP.ID = S.ID
								left join Asset O on O.ID = I.ObjectAssetID 
								left join AssetType OT1 on O.ID is not null and OT1.ID = O.AssetTypeID
								left join AssetType OT2 on O.ID is null and OT2.ID = I.ObjectAssetTypeID 
								left join dbo.AssetPath OKP on OKP.ID = O.ID 
								";
			var whereClause = " WHERE I.[Uid] = @uid ";
			dbArgs.Add("@uid", uid);

			List<FieldType> fieldTypes = null;

			fieldTypes = companyContext.Query<FieldType>(
				$@"select F.* from FieldType F 
					inner join IntersectType IT on IT.ID = F.IntersectTypeID
					inner join [intersect] I on I.IntersectTypeID = IT.ID
					WHERE I.uid = @uid"
				, new { uid }, ApiTimeout).ToList();

			var fieldColumns = new DynamicQuerySelects();
			var fieldJoins = new DynamicQueryJoins();

			if (fieldTypes != null)
			{
				getFieldSql(fieldTypes, dbArgs, fieldJoins, fieldColumns, "i.Id", objectType: SystemObjects.Intersect);
			}

			var stateSql = "case I.State ";
			State.Active.GetList().ForEach(s =>
			{
				stateSql += $"when {(int)s.ID} then '{s.ID.ToString()}' ";
			});
			stateSql += " end as State, ";

			var predicateTypeSql = "case P.Type ";
			PredicateType.DataLineage.GetAsList().ForEach(p =>
			{
				predicateTypeSql += $"when {(int)p.ID} then '{p.ID.ToString()}' ";
			});
			predicateTypeSql += " end as 'Predicate.Type', ";

			string fieldColumnsSql = "";
			if (fieldColumns.Any())
			{
				fieldColumnsSql = string.Join(",\n", fieldColumns.GetStatements()) + ",";
			}

			var sql = $@"
						select	lower(I.Uid) as Uid,
								lower(T.Uid) as RelationshipTypeUid,
								{stateSql}
								I.[Owner],
								{fieldColumnsSql}
								lower(P.UID) as 'Predicate.Uid',
								{predicateTypeSql}
								P.Name as 'Predicate.Name',
								P.Inverse as 'Predicate.Inverse',
								lower(S.Uid) as 'Subject.Uid',
								SKP.KeyPath as 'Subject.Path',
								ISNULL(lower(ST1.Uid),lower(ST2.Uid)) as 'Subject.AssetTypeUid',
								lower(O.Uid) as 'Object.Uid',
								OKP.KeyPath as 'Object.Path',
								ISNULL(lower(OT1.Uid),lower(OT2.Uid)) as 'Object.AssetTypeUid'
						{baseTableSql}
						{fieldJoins.SQLJoinStatement}
						{whereClause} 
						for json path, INCLUDE_NULL_VALUES, WITHOUT_ARRAY_WRAPPER";

			var models = await companyContext.GetDatabaseJsonAsObjectAsync<JObject>(sql, dbArgs, ApiTimeout);

			return models;
		}

		public IQueryable<IntersectType> GetIntersectTypeById(int id)
		{
			return companyContext.Filter<IntersectType>(i => i.ID == id);
		}

		public IntersectType GetIntersectTypeByUid(Guid intersectTypeUid)
		{
			return companyContext.Filter<IntersectType>(i => i.uid == intersectTypeUid).SingleOrDefault();
		}

		public async Task<List<IntersectTypeApiViewModel>> GetRelationshipTypes(IEnumerable<KeyValuePair<string, string>> queryParams, string whereClause = "")
		{
			var dbArgs = new DynamicParameters();
			bool includeHasFieldTypes = false;
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
						bool IsReferenceType = false;
						Guid RefListUid = Guid.Empty;
						var assetType = companyContext.Filter<AssetType>(i => i.uid == assetTypeUid).FirstOrDefault();
						if (assetType != null)
						{
							if (assetType.Class == AssetTypeClass.Reference)
							{
								var assetTypeRef = companyContext.Filter<AssetType>(i => i.Class == AssetTypeClass.Reference && i.ObjectID == 0).FirstOrDefault();
								if (assetTypeRef != null)
								{
									RefListUid = assetTypeRef.uid;
								}
								IsReferenceType = true;
							}
						}
						dbArgs.Add("@assettypeuid", assetTypeUid);
						if (IsReferenceType)
						{
							dbArgs.Add("@RefListUid", RefListUid);
							whereClause += (string.IsNullOrEmpty(whereClause) ? " where" : " and") + $" (S.Uid = @assettypeuid OR O.Uid = @assettypeuid OR S9.Uid = @RefListUid OR O9.Uid = @RefListUid)";
						}
						else
						{
							whereClause += (string.IsNullOrEmpty(whereClause) ? " where" : " and") + $" (S.Uid = @assettypeuid OR O.Uid = @assettypeuid)";
						}
					}
				}				
				if (queryParams.ToList().Any(q => q.Key.ToLower() == "relationshiptypeuid"))
				{
					Guid relationshipTypeUid;
					var relationshipTypeUidString = queryParams.ToList().FirstOrDefault(q => q.Key.ToLower() == "relationshiptypeuid").Value;
					if (Guid.TryParse(relationshipTypeUidString, out relationshipTypeUid))
					{
						dbArgs.Add("@relationshiptypeuid", relationshipTypeUid);
						whereClause += (string.IsNullOrEmpty(whereClause) ? " where" : " and") + $" (I.Uid = @relationshiptypeuid)";
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
				if (queryParams.ToList().Any(q => q.Key.ToLower() == "includehasfieldtypes"))
				{
					var hasFieldTypesString = queryParams.ToList().FirstOrDefault(q => q.Key.ToLower() == "includehasfieldtypes").Value;
					bool.TryParse(hasFieldTypesString, out includeHasFieldTypes);
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
		coalesce(S.Uid,S9.Uid ) as 'Subject.Uid',		
		coalesce(SP.[Path], S.Name, S9.Name) as 'Subject.Name',
		coalesce(I.SubjectClass,0) as 'Subject.Class',
		I.SubjectCardinality as 'Subject.Cardinality',
		coalesce(O.Uid,O9.Uid) as 'Object.Uid',
		coalesce(OP.[Path], O.Name, O9.Name)  as 'Object.Name',
		coalesce(I.ObjectClass,0) as 'Object.Class',
		I.ObjectCardinality as 'Object.Cardinality'
		{(includeHasFieldTypes ? @",case 
								when exists (select top 1 1 from FieldType where IntersectTypeID = I.ID)
									then 1
									else 0
								end as 'HasFieldTypes'" : "")}
from	IntersectType I
		left join [Predicate] P on P.ID = I.PredicateID
		left join AssetType S on S.ID = I.SubjectAssetTypeID and I.SubjectAssetTypeID > 0
		left join AssetType S9 on S9.OBJECTID = 0 AND S9.CLASS = 9 AND S9.CLASS = I.SUBJECTCLASS and I.SubjectAssetTypeID = 0
		outer apply dbo.GetAssetTypeTextPathById(S.ID, '/') SP
		left join AssetType O on O.ID = I.ObjectAssetTypeID and I.ObjectAssetTypeID > 0
		left join AssetType O9 on O9.OBJECTID = 0 AND O9.CLASS = 9 AND O9.CLASS = I.OBJECTCLASS and I.ObjectAssetTypeID = 0
		outer apply dbo.GetAssetTypeTextPathById(O.ID, '/') OP
		{whereClause} for json path";

			var models = await companyContext.GetDatabaseJsonAsObjectAsync<List<IntersectTypeApiViewModel>>(sql, dbArgs, ApiTimeout);

			return models;
		}

		public async Task<ApiExecutionInfo> BulkPostRelationships(Guid intersectTypeUid, RelationshipInserts relationships, ApiExecution execution, bool triggerWorkflow = false)
		{
			var executionInfo = new ApiExecutionInfo
			{
				CompanyID = companyContext.CurrentCompanyID,
				ResourceID = companyContext.CurrentResourceID,
				CompanyDomainPrefix = companyContext.CurrentCompanyDomain,
				ExecutionID = Guid.NewGuid(),
				Action = ApiExecutionAction.PostRelationships,
				SendWorkflowEvents = triggerWorkflow
			};

			await Storage.CreateFolder(executionInfo.StorageFolder);
			await Storage.CreateFile(executionInfo.StorageFolder, executionInfo.RequestFileName, JsonConvert.SerializeObject(relationships));

			execution.ExecutionID = executionInfo.ExecutionID;
			companyContext.Add(execution);

			await QueueSource.CreateMessageAsync(Config.GetValue<string>("ApiExecutionQueue"), executionInfo);

			return executionInfo;
		}

		public async Task<ApiExecutionInfo> BulkPutRelationships(Guid intersectTypeUid, RelationshipUpdates relationships, ApiExecution execution, bool triggerWorkflow = false)
		{
			var executionInfo = new ApiExecutionInfo
			{
				CompanyID = companyContext.CurrentCompanyID,
				ResourceID = companyContext.CurrentResourceID,
				CompanyDomainPrefix = companyContext.CurrentCompanyDomain,
				ExecutionID = Guid.NewGuid(),
				Action = ApiExecutionAction.PutRelationships,
				SendWorkflowEvents = triggerWorkflow
			};

			await Storage.CreateFolder(executionInfo.StorageFolder);
			await Storage.CreateFile(executionInfo.StorageFolder, executionInfo.RequestFileName, JsonConvert.SerializeObject(relationships));

			execution.ExecutionID = executionInfo.ExecutionID;
			companyContext.Add(execution);

			await QueueSource.CreateMessageAsync(Config.GetValue<string>("ApiExecutionQueue"), executionInfo);

			return executionInfo;
		}

		public IEnumerable<dynamic> GetExportModel(int id)
		{
			return companyContext.Query<dynamic>(
				@"select 
					UID,
					ID,
					[Subject], 
					SubjectID, 
					SubjectUid,
					SubjectName, 
					SubjectTypeName, 
					[Object], 
					ObjectID, 
					ObjectUid,
					ObjectName, 
					ObjectTypeName, 
					PredicateName 
				from 
					intersectdetail 
				where intersecttypeid = @id", new { id }, ApiTimeout);
		}

		public IEnumerable<dynamic> GetExportModelWithCustomFields(int id, IEnumerable<string> customColumns)
		{
			var customColumnName = "[" + customColumns.Aggregate((x, y) => x + "],[" + y) + "]";
			var CteColumnName = "CTE.[" + customColumns.Aggregate((x, y) => x + "],CTE.[" + y) + "]";

			var sql = @"WITH CTE (ObjectID, " + customColumnName +
				") AS ( SELECT ObjectId, " + customColumnName +
				" FROM ( select f2.ObjectID, f.FriendlyName,FormattedValue from fieldtype f  " +
				"inner join field f2 on f2.fieldtypeid = f.id where f.IntersectTypeID = @id  ) as PivotData " +
				"PIVOT (max(FormattedValue) FOR FriendlyName IN (" + customColumnName + ") ) AS PivotResult) " +
				"select i.ID, i.[Subject],i.SubjectID, i.SubjectName, i.SubjectTypeName, i.[Object], " +
				"i.ObjectID, i.ObjectName, i.ObjectTypeName, i.PredicateName , i.SubjectUid, i.ObjectUid, " + CteColumnName +
				" from  intersectdetail as i left join CTE  on CTE.ObjectID =i.id where intersecttypeid=@id ";
			var models = companyContext.Query<dynamic>(sql, new { id }, ApiTimeout);

			return models;
		}

		public bool AnyExists(Guid uid)
		{
			return companyContext.Any<IntersectType>(i => i.uid == uid);
		}

		public bool AnyPredicateExists(Guid uid)
		{
			return companyContext.Any<Predicate>(i => i.UID == uid);
		}

		public async Task<List<DatabaseBulkAssetResult>> GetBulkResults(ApiExecutionInfo info)
		{
			List<DatabaseBulkAssetResult> results = null;
			try
			{
				results = await Storage.DeserializeJsonObjectFromBlobAsync<List<DatabaseBulkAssetResult>>(info.StorageFolder, info.ResponseFileName);
			}
			catch
			{
			}

			return results;
		}

		public List<DatabaseBulkRelationshipResult> DeleteRelationships(ApiExecution execution, IntersectType intersectType, RelationshipDeletes relationships, int timeout = 3600, bool triggerWorkflow = false)
		{
			return companyContext.DeleteRelationships(execution, intersectType, relationships, timeout, triggerWorkflow);
		}

		public async Task<ApiExecutionInfo> BulkDeleteRelationships(Guid intersectTypeUid, RelationshipDeletes relationships, ApiExecution execution, bool triggerWorkflow = false)
		{
			var executionInfo = new ApiExecutionInfo
			{
				CompanyID = companyContext.CurrentCompanyID,
				ResourceID = companyContext.CurrentResourceID,
				CompanyDomainPrefix = companyContext.CurrentCompanyDomain,
				ExecutionID = Guid.NewGuid(),
				Action = ApiExecutionAction.DeleteRelationships,
				SendWorkflowEvents = triggerWorkflow
			};

			await Storage.CreateFolder(executionInfo.StorageFolder);
			await Storage.CreateFile(executionInfo.StorageFolder, executionInfo.RequestFileName, JsonConvert.SerializeObject(relationships));

			execution.ExecutionID = executionInfo.ExecutionID;
			companyContext.Add(execution);

			await QueueSource.CreateMessageAsync(Config.GetValue<string>("ApiExecutionQueue"), executionInfo);

			return executionInfo;
		}

		public List<PredicateDeleteResult> DeletePredicates(PredicateDeletes predicates, ApiExecution execution)
		{
			companyContext.Add(execution);

			List<PredicateDeleteResult> results = null;
			try
			{
				results = companyContext.RemovePredicates(execution, predicates);

				// Close execution record.
				execution.Processed = results.Count;
				execution.Error = results.Count(i => !i.Success);
				execution.CompletedOn = DateTime.UtcNow;
				companyContext.Update(execution);
			}
			catch (Exception ex)
			{
				string message = ex.GetFullExceptionData(false, constants.ERROR_MESSAGE_CHARACTER_LIMIT);
				execution.ErrorMessage = message;
				execution.CompletedOn = DateTime.UtcNow;
				companyContext.Update(execution);
			}

			return results;
		}

		public List<PredicateUpsertResult> UpsertPredicates(PredicateUpserts predicates, ApiExecution execution)
		{
			companyContext.Add(execution);

			List<PredicateUpsertResult> results = null;
			try
			{
				results = companyContext.UpdatePredicates(execution, predicates);

				// Close execution record.
				execution.Processed = results.Count;
				execution.Error = results.Count(i => !i.Success);
				execution.CompletedOn = DateTime.UtcNow;
				companyContext.Update(execution);
			}
			catch (Exception ex)
			{
				string message = ex.GetFullExceptionData(false, constants.ERROR_MESSAGE_CHARACTER_LIMIT);
				execution.ErrorMessage = message;
				execution.CompletedOn = DateTime.UtcNow;
				companyContext.Update(execution);
			}

			return results;
		}

		public async Task<bool> IsTransformPredicateExists(int assetTypeId)
		{
			string sql = @"
Select	A.Name 
from	AssetType A
where	Id = @Id
		and (
			exists (select 1 from IntersectType I inner join [Predicate] P on P.Id = I.PredicateID and P.[Type]  = @type and I.SubjectAssetTypeID = A.ID )
			or exists (select 1 from IntersectType I inner join [Predicate] P on P.Id = I.PredicateID and P.[Type] = @type and I.ObjectAssetTypeID = A.ID )
		)";
			var result = await companyContext.QueryAsync<string>(sql, new { id = assetTypeId, type = (int)PredicateType.Transformation });
			
			return !string.IsNullOrEmpty(result.FirstOrDefault());
		}
		public List<RelationshipTypeResult> PostRelationshipTypes(List<RelationshipTypeInsert> relationshipTypes, ApiExecution execution)
		{
			companyContext.Add(execution);

			List<RelationshipTypeResult> results = null;
			try
			{
				results = companyContext.ImportRelationshipTypes(execution, relationshipTypes);

				// Close execution record.
				execution.Processed = results.Count;
				execution.Error = results.Count(i => !i.Success);
				execution.CompletedOn = DateTime.UtcNow;
				companyContext.Update(execution);
			}
			catch (Exception ex)
			{
				string message = ex.GetFullExceptionData(false, constants.ERROR_MESSAGE_CHARACTER_LIMIT);
				execution.ErrorMessage = message;
				execution.CompletedOn = DateTime.UtcNow;
				companyContext.Update(execution);
			}

			return results;
		}

		public List<RelationshipTypeResult> PutRelationshipTypes(List<RelationshipTypeUpdate> relationshipTypes, ApiExecution execution)
		{
			companyContext.Add(execution);

			List<RelationshipTypeResult> results = null;
			try
			{
				results = companyContext.ImportRelationshipTypes(execution, relationshipTypes);

				// Close execution record.
				execution.Processed = results.Count;
				execution.Error = results.Count(i => !i.Success);
				execution.CompletedOn = DateTime.UtcNow;
				companyContext.Update(execution);
			}
			catch (Exception ex)
			{
				string message = ex.GetFullExceptionData(false, constants.ERROR_MESSAGE_CHARACTER_LIMIT);
				execution.ErrorMessage = message;
				execution.CompletedOn = DateTime.UtcNow;
				companyContext.Update(execution);
			}

			return results;
		}

		public List<RelationshipTypeResult> DeleteRelationshipTypes(List<RelationshipTypeDelete> relationshipTypes, ApiExecution execution)
		{
			companyContext.Add(execution);

			List<RelationshipTypeResult> results = null;
			try
			{
				results = companyContext.DeleteRelationshipTypes(execution, relationshipTypes);

				// Close execution record.
				execution.Processed = results.Count;
				execution.Error = results.Count(i => !i.Success);
				execution.CompletedOn = DateTime.UtcNow;
				companyContext.Update(execution);
			}
			catch (Exception ex)
			{
				string message = ex.GetFullExceptionData(false, constants.ERROR_MESSAGE_CHARACTER_LIMIT);
				execution.ErrorMessage = message;
				execution.CompletedOn = DateTime.UtcNow;
				companyContext.Update(execution);
			}

			return results;
		}
		public async Task<SLDocument> GetRelationshipsExcel(IEnumerable<KeyValuePair<string, string>> queryParams, CancellationToken? cancellationToken = null)
		{
			var apiTimeout = ApiTimeout;
			JObject results = await GetRelationships(queryParams, isExport: true, cancellationToken: cancellationToken).ConfigureAwait(false);
			var includeTotal = true;
			var includeAssetPath = false;

			if (queryParams != null)
			{
				var queryParamsList = queryParams.ToList();

				if (queryParamsList.Any(q => q.Key.ToLower() == "_includetotal"))
				{
					if (!bool.TryParse(queryParamsList.FirstOrDefault(q => q.Key.ToLower() == "_includetotal").Value, out includeTotal))
					{
						includeTotal = true;
					}
				}

				if (queryParamsList.Any(q => q.Key.ToLower() == "_includepath"))
				{
					if (!bool.TryParse(queryParamsList.FirstOrDefault(q => q.Key.ToLower() == "_includepath").Value, out includeAssetPath))
					{
						includeAssetPath = false;
					}
				}
			}

			var apiInfo = results.Children().ToList();

			var excelDocument = new ExcelDocument(string.Format(ExcelExports.Relationships_DocumentName, DateTime.Now));

			var fields = new List<FieldType>();

			var headerRow = new ExcelRow();
			var itemsSheet = new ExcelSheet(ExcelExports.Relationships_SheetName);

			//add default fields
			fields.Add(new FieldType { Type = "string", Name = "Uid", FriendlyName = ExcelExports.Relationships_Relationship_UID });
			fields.Add(new FieldType { Type = "string", Name = "Subject|Uid", FriendlyName = ExcelExports.Relationships_Subject_UID });
			fields.Add(new FieldType { Type = "string", Name = "Subject|DisplayName", FriendlyName = ExcelExports.Relationships_Subject_Display_Name });
			
			if (includeAssetPath)
			{
				fields.Add(new FieldType { Type = "string", Name = "Subject|[Path]", FriendlyName = ExcelExports.Relationships_Subject_Asset_Path });
			}
			
			fields.Add(new FieldType { Type = "string", Name = "Subject|AssetTypePath", FriendlyName = ExcelExports.Relationships_Subject_Asset_Type_Path });
			fields.Add(new FieldType { Type = "string", Name = "Predicate|Name", FriendlyName = ExcelExports.Relationships_Predicate_Name });
			fields.Add(new FieldType { Type = "string", Name = "Object|Uid", FriendlyName = ExcelExports.Relationships_Object_UID });
			fields.Add(new FieldType { Type = "string", Name = "Object|DisplayName", FriendlyName = ExcelExports.Relationships_Object_Display_Name });
			
			if (includeAssetPath)
			{
				fields.Add(new FieldType { Type = "string", Name = "Object|[Path]", FriendlyName = ExcelExports.Relationships_Object_Asset_Path });
			}
			
			fields.Add(new FieldType { Type = "string", Name = "Object|AssetTypePath", FriendlyName = ExcelExports.Relationships_Object_Asset_Type_Path });
			fields.Add(new FieldType { Type = "string", Name = "RelationshipTypeUid", FriendlyName = ExcelExports.Relationships_Relationship_Type_UID });
			fields.Add(new FieldType { Type = "string", Name = "Subject|AssetTypeUid", FriendlyName = ExcelExports.Relationships_Subject_Asset_Type_UID });
			fields.Add(new FieldType { Type = "string", Name = "Object|AssetTypeUid", FriendlyName = ExcelExports.Relationships_Object_Asset_Type_UID });
			fields.Add(new FieldType { Type = "string", Name = "Predicate|Uid", FriendlyName = ExcelExports.Relationships_Predicate_UID });
			fields.Add(new FieldType { Type = "string", Name = "Predicate|Type", FriendlyName = ExcelExports.Relationships_Predicate_Type });
			fields.Add(new FieldType { Type = "string", Name = "Predicate|Inverse", FriendlyName = ExcelExports.Relationships_Predicate_Inverse });

			#region Populate Excel Document            

			#region API Info Sheet
			var apiInfoSheet = new ExcelSheet(ExcelExports.Common_ApiInfoSheetName);

			var pageSizeRow = new ExcelRow { ExcelExports.Common_PageSize, results.GetValue("pageSize").ToString() };
			var pageNumRow = new ExcelRow { ExcelExports.Common_PageNum, results.GetValue("pageNum").ToString() };
			apiInfoSheet.ValueRows.Add(pageSizeRow);
			apiInfoSheet.ValueRows.Add(pageNumRow);

			if (includeTotal)
			{
				var totalRow = new ExcelRow { ExcelExports.Common_Total, results.GetValue("total").ToString() };
				apiInfoSheet.ValueRows.Add(totalRow);
			}
			#endregion    

			var items = results.GetValue("items");
			var rowData = new List<JToken>();

			if (items != null)
			{
				#region Populate Items Sheet
				rowData = items.ToList();

				List<ExcelRow> rows = new List<ExcelRow>();

				var numberOfRelationshipTypes = rowData.Select(x => x["RelationshipTypeUid"]).Distinct().Count();
				foreach (var row in rowData)
				{
					var relationshipTypeUid = row["RelationshipTypeUid"];

					//only include custom fields if there is single relationship type present in results
					if (numberOfRelationshipTypes == 1)
					{
						var customColumns = GetCustomFieldsForExcel(relationshipTypeUid.ToString(), apiTimeout);

						if (customColumns.Count() > 0)
						{
							int customCount = 0;
							foreach (var cus in customColumns)
							{
								var name = cus.Name;
								var friendlyName = cus.FriendlyName;

								var exists = fields.Where(x => x.Name.Split('|')[0].ToLower() == name.ToLower()).FirstOrDefault();
								if (exists == null)
								{
									var cusField = new FieldType { Type = "string", Name = name, FriendlyName = friendlyName };
									fields.Insert((includeAssetPath ? 10 : 8) + customCount, cusField);
									customCount++;
								}
							}
						}
					}

					ExcelRow excelRow = new ExcelRow();
					foreach (var field in fields)
					{

						var token = row[field.Name];

						var fieldID = field.Name.Split('|');

						if (fieldID.Count() > 1)
						{
							token = row[fieldID[0]][fieldID[1]];
						}

						string value = "";
						if (token != null)
						{
							value = token.Value<string>();
						}
						excelRow.Add(value);
					}
					rows.Add(excelRow);
				}
				itemsSheet.ValueRows.AddRange(rows);
				#endregion
			}

			#endregion
			fields.ForEach((field) => headerRow.Add(field.FriendlyName));
			itemsSheet.HeaderRows.Add(headerRow);
			excelDocument.Add(itemsSheet);
			excelDocument.Add(apiInfoSheet);

			SLDocument document = excelDocument.ToSLDocument();
			document.SelectWorksheet(ExcelExports.Relationships_SheetName);
			return document;
		}

		public IEnumerable<dynamic> GetCustomFieldsForExcel(string intersectUid, int apiTimeout)
		{
			return companyContext.Query<dynamic>(
				@"select distinct  f.Name   as Name,f.FriendlyName as FriendlyName, f.ColumnOrder from fieldtype f  
				inner join IntersectType i on i.uid = @uid
				 where f.IntersectTypeID = i.ID and IsListable = 1
				 order by f.ColumnOrder", new { uid = intersectUid }, apiTimeout);
		}

		public async Task<RelationshipUidResult> GetRelationshipsUids(int intersectTypeID, int pageSize, int pageNum, bool includeTotal, string owner)
		{
			int? total = null;
			string whereFilter = string.IsNullOrEmpty(owner) ? " " : " and i.owner = @owner";

			if (includeTotal)
			{
				var cntsql = $@"
select	count(1)
from	[Intersect] i 
where	i.IntersectTypeID = @intersectTypeID {whereFilter}";

				total = await companyContext.QueryFirstOrDefaultAsync<int>(cntsql, new { intersectTypeID, owner }, ApiTimeout);
			}

			var sql = $@"
						begin                         
						 -- create temp table
						 drop table if exists #TempIntersectInfo
						 create table #TempIntersectInfo
						(
							IntersectUid UniqueIdentifier not null, 
							SubjectAssetID bigint,
							SubjectUid UniqueIdentifier null,
							ObjectAssetID bigint,
							ObjectUid UniqueIdentifier null,
						    [Owner] varchar(100) null
						)

						create nonclustered index temp_intersectInfo_idx on #TempIntersectInfo ([ObjectAssetID],[SubjectAssetID])

						 -- add intersect info into temp table

						 insert into #TempIntersectInfo
							(IntersectUid, [SubjectAssetID],[ObjectAssetID],[Owner])
						   select 
							I.[UID],
							I.[SubjectAssetID],
							I.[ObjectAssetID],
							I.[Owner]
							from	[intersect] I 
							where	I.IntersectTypeID = @intersectTypeID {whereFilter}
							order by I.ID OFFSET @offset ROWS 
							FETCH NEXT @rows ROWS ONLY

							UPDATE	#TempIntersectInfo
							SET		#TempIntersectInfo.SubjectUid =  a.[uid]
							FROM	Asset a
									INNER JOIN #TempIntersectInfo t ON a.ID = t.SubjectAssetID;

							UPDATE	#TempIntersectInfo
							SET		#TempIntersectInfo.ObjectUID =  a.[uid]
							FROM	asset a	
									INNER JOIN #TempIntersectInfo t ON a.ID = t.ObjectAssetID;

							select IntersectUid as RelationshipUid, ObjectUid, SubjectUid, Owner from #TempIntersectInfo 
						end";

			var results = await companyContext.QueryAsync<RelationshipUidResultItem>(sql, new { intersectTypeID, offset = ((pageNum - 1) * (pageSize)), rows = pageSize, owner }, ApiTimeout);

			return new RelationshipUidResult { Total = total, pageSize = pageSize, pageNum = pageNum, Results = results };
		}
	}
}
