using d360.core.entities;
using d360.core.enums;
using d360.core.exceptions;
using d360.extensions;
using d360.model.DataAccessLayer.repositories;
using d360.model.helpers;
using d360.model.helpers.filters;
using Dapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace d360.model.DataAccessLayer
{
    public class SemanticsRepository : BaseRepository, ISemanticsRepository
    {
        #region DI

        internal ICompanyContext CompanyContext;
        internal IQueueSource QueueSource;
        internal IStorageProvider StorageProvider;
        internal ICommunityContext Community;

        public SemanticsRepository(ICompanyContext companyContext, IQueueSource queueSource, IStorageProvider storageProvider, ICommunityContext community)
            : base(companyContext)
        {
            this.CompanyContext = companyContext;
            this.QueueSource = queueSource;
            this.StorageProvider = storageProvider;
            this.Community = community;
        }

        #endregion

        #region Private

        readonly List<string> complexNonSortableFields = new List<string> 
        {
            "headerRegExps", 
            "invalidList", 
            "advanced", 
            "regExReturned", 
            "validLocales", 
            "validList"
        };

        readonly Dictionary<string, string> orderFields = new Dictionary<string, string> 
        {
            { "baseType", "BaseType" },
            { "description", "Description" },
            { "effectiveDate", "" },
            { "headerRegExpConfidence", "HeaderFilterConfidence" },
            { "matchType", "MatchType" },
            { "maximum", "Maximum" },
            { "minimum", "Minimum" },
            { "minSamples", "MinimumSamples" },
            { "minMaxPresent", "MinMaxPresent" },
            { "name", "Name" },
            { "priority", "Priority" },
            { "qualifier", "Qualifier" },
            { "status", "Status" },
            { "threshold", "Threshold" }
        };

        void addToChangeLog(string transactionId, string action)
        {
            CompanyContext.Connection.Execute(@"
declare @items table (
	Qualifier nvarchar(200),
	Object varchar(50),
	ObjectID bigint,
	ResourceID int,
	[Date] datetime,
	[Action] varchar(15),
	AuditId bigint null
)

declare @fields table (
	Qualifier nvarchar(200),
	EffectiveDate datetime,
	FieldName varchar(250),
	[Version] int,
	[Value] nvarchar(max),
	[PreviousValue] nvarchar(max),
	AuditId bigint null
)

insert into @items
	select	Qualifier,
			'Semantic',
			ID,
			UpdatedBy,
			UpdatedOn,
			case @action
				when 'C' then 'Created'
				when 'U' then 'Updated'
				else 'Removed'
			end,
			null
	from	Semantic 
	where	TransactionId = @transactionId

insert into @fields (Qualifier, EffectiveDate, FieldName, [Value])
	select	S.Qualifier, S.EffectiveDate, F.FieldName, F.FieldValue
	from	Semantic S 
			cross apply (
				select 'Name', cast([Name] as nvarchar(max)) 
				union all select 'Description', cast([Description] as nvarchar(max))
				union all select 'Threshold', cast([Threshold] as nvarchar(max))
				union all select 'Status', cast([Status] as nvarchar(max))
				union all select 'Priority', cast([Priority] as nvarchar(max))
				union all select 'Source', cast([Source] as nvarchar(max))
				union all select 'MatchType', cast(MatchType as nvarchar(max))
				union all select 'BaseType', cast(BaseType as nvarchar(max))
				union all select 'RegularExpression', cast(RegularExpression as nvarchar(max))
				union all select 'ValidValues', cast(ValidValues as nvarchar(max))
				union all select 'InvalidValues', cast(InvalidValues as nvarchar(max))
				union all select 'ValidLocales', cast(ValidLocales as nvarchar(max))
				union all select 'MinimumSamples', cast(MinimumSamples as nvarchar(max))
				union all select 'HeaderFilter', cast(HeaderFilter as nvarchar(max))
				union all select 'HeaderFilterConfidence', cast(HeaderFilterConfidence as nvarchar(max))
				union all select 'Minimum', cast(Minimum as nvarchar(max))
				union all select 'Maximum', cast(Maximum as nvarchar(max))
				union all select 'MinMaxPresent', cast(MinMaxPresent as nvarchar(max))
				union all select 'JsonPayload', cast(JsonPayload as nvarchar(max)) 
			) F (FieldName, FieldValue)
	where	TransactionId = @transactionId

if @action = 'U'
begin
	update	T
	set		T.PreviousValue = FS.FieldValue
	from	@fields T
			cross apply (
						select	S.Qualifier, F.FieldName, F.FieldValue
						from	(
								select	Qualifier,
										max(EffectiveDate) as EffectiveDate
								from	Semantic
								where	Qualifier = T.Qualifier
										and EffectiveDate < T.EffectiveDate
								group by Qualifier
								) MS
								inner join Semantic S on S.Qualifier = MS.Qualifier and S.EffectiveDate = MS.EffectiveDate
								cross apply (
									select 'Name', cast(S.[Name] as nvarchar(max)) 
									union all select 'Description', cast(S.[Description] as nvarchar(max))
									union all select 'Threshold', cast(S.[Threshold] as nvarchar(max))
									union all select 'Status', cast(S.[Status] as nvarchar(max))
									union all select 'Priority', cast(S.[Priority] as nvarchar(max))
									union all select 'Source', cast(S.[Source] as nvarchar(max))
									union all select 'MatchType', cast(S.MatchType as nvarchar(max))
									union all select 'BaseType', cast(S.BaseType as nvarchar(max))
									union all select 'RegularExpression', cast(S.RegularExpression as nvarchar(max))
									union all select 'ValidValues', cast(S.ValidValues as nvarchar(max))
									union all select 'InvalidValues', cast(S.InvalidValues as nvarchar(max))
									union all select 'ValidLocales', cast(S.ValidLocales as nvarchar(max))
									union all select 'MinimumSamples', cast(S.MinimumSamples as nvarchar(max))
									union all select 'HeaderFilter', cast(S.HeaderFilter as nvarchar(max))
									union all select 'HeaderFilterConfidence', cast(S.HeaderFilterConfidence as nvarchar(max))
									union all select 'Minimum', cast(S.Minimum as nvarchar(max))
									union all select 'Maximum', cast(S.Maximum as nvarchar(max))
									union all select 'MinMaxPresent', cast(S.MinMaxPresent as nvarchar(max))
									union all select 'JsonPayload', cast(S.JsonPayload as nvarchar(max)) 
								) F (FieldName, FieldValue)
						where	S.Qualifier = T.Qualifier
								and F.FieldName = T.FieldName
						) FS

	delete @fields where [Value] = PreviousValue
end

delete @fields where [Value] is null and PreviousValue is null

declare @ids table (AuditId bigint, Qualifier nvarchar(200))

insert into [reporting].[Global_Audit] (
	Object, ObjectID, ObjectName, 
	ResourceID, Date, Action, 
	ActionObject, ActionObjectID, ActionObjectTypeName, ActionObjectName, 
	ActionDescription)
output inserted.ID, inserted.ObjectName into @ids
select	Object, ObjectID, Qualifier, 
		ResourceID, Date, Action,
		Object, ObjectID, 'Semantic', Qualifier,
		'Semantic ' + case @action
				when 'C' then 'created'
				when 'U' then 'updated'
				else 'removed'
			end + '.'
from @items

update	T
set		T.AuditId = S.AuditId
from	@items T
		inner join @ids S on S.Qualifier = T.Qualifier

update	T
set		T.AuditId = S.AuditId
from	@fields T
		inner join @items S on S.Qualifier = T.Qualifier

update	T
set		T.Version = S.[Count] + 1
from	@fields T
		cross apply (
			select	count(1) as [Count]
			from	[reporting].[Global_FieldAudit] F
					inner join [reporting].[Global_Audit] A on A.ID = F.AuditID and A.Object = 'Semantic' 
					and A.ObjectName = T.Qualifier 
					and F.FieldName = T.FieldName
		) S

insert into [reporting].[Global_FieldAudit] (AuditID, FieldTypeID, FieldName, Version, Value, PreviousValue)
	select AuditID, 0, FieldName, coalesce(Version, 1), Value, PreviousValue from @fields", new { transactionId, action });
        }

        List<Semantic> findLatestExistingSemantics(List<string> qualifiers, int expectedCount)
        {
            var existingSemantics = CompanyContext.Query<Semantic>(
        "select * " +
        "from Semantic S " +
        "inner join (" +
        " select Qualifier, max(EffectiveDate) as EffectiveDate " +
        " from Semantic " +
        " where Qualifier in (@qualifiers) " +
        "group by Qualifier) M on M.Qualifier = S.Qualifier and M.EffectiveDate = S.EffectiveDate", new { qualifiers }).ToList();

            if (existingSemantics.Count != expectedCount)
            {
                var missing = qualifiers.Where(q => !existingSemantics.Any(e => e.Qualifier == q)).ToList();
                throw new GenericException(
                    HttpStatusCode.NotFound,
                    "Some semantics were not found.",
                    $"The following semantics could not be located: {string.Join("; ", missing)}."
                    );
            }

            return existingSemantics;
        }

        string generateTransactionId()
        {
            return Community.GenerateOpenIdRequestValue(10);
        }

        #endregion
        
        public async Task<HttpStatusCode> DeleteSemanticAsync(string qualifier)
        {
            try
            {
                var qualifiers = new List<string> { qualifier };
                findLatestExistingSemantics(qualifiers, 1);

                var anyProfilesQuery = await CompanyContext.QueryAsync<int>(@"
select  count(1)  
from    AssetDataProfile P 
        cross apply (
            select  max(EffectiveDate) as EffectiveDate
            from    Semantic 
            where   Qualifier = @qualifier 
                    and EffectiveDate <= P.ProfileSetDate
        ) S
where   P.TypeQualifier = @qualifier", new { qualifier });

                var profileRecordCount = anyProfilesQuery.Single();

                if (profileRecordCount > 0)
                {
                    throw new GenericException(
                        HttpStatusCode.Conflict, 
                        "Profiles match the semantic.",
                        "You may not remove this semantic since one or more asset data profiles match this semantic.");
                }

                CompanyContext.Connection.Execute("delete Semantic where Qualifier = @qualifier", new { qualifier });

                return HttpStatusCode.OK;
            }
            catch (GenericException ex)
            {
                throw ex;
            }
            catch (Exception ex)
            {
                throw ex;
                // TODO: Should we do something else here?
            }
        }
        
        public async Task<GetSemantics> GetSemanticsAsync(IEnumerable<KeyValuePair<string, string>> queryParams, CancellationToken? cancellationToken = null)
        {
            if (cancellationToken == null)
            {
                cancellationToken = CancellationToken.None;
            }

            var dbArgs = new DynamicParameters();

            var pageNum = 1;
            var pageSize = 200;
            var order = "Qualifier";
            var direction = "asc";
            DateTime asOfEffectiveDate = DateTime.UtcNow;
            var whereStatements = new List<string>();

            if (queryParams.ToList().Any(x => x.Key.ToLower() == "_filter"))
            {
                var value = queryParams.FirstOrDefault(x => x.Key.ToLower() == "_filter").Value;
                if (!string.IsNullOrEmpty(value))
                {
                    var filterDataProvider = new FilterDataProvider(CompanyContext);
                    var filterExpressionParser = new FilterExpressionParser(filterDataProvider, FilterExpressionParseType.Semantics, false, false, false);
                    var sqlParams = new Dictionary<string, object>();
                    whereStatements.Add("(" + filterExpressionParser.Parse(value, out sqlParams, out _) + ")");
                    foreach (var item in sqlParams)
                    {
                        dbArgs.Add(item.Key, item.Value);
                    }
                }
            }

            if (queryParams.ToList().Any(x => x.Key.ToLower() == "_simplefilter"))
            {
                var simpleFilter = queryParams.FirstOrDefault(x => x.Key.ToLower() == "_simplefilter").Value.Trim();
                if (!string.IsNullOrEmpty(simpleFilter))
                {
                    simpleFilter = CompanyContext.GetEscapedFilterString(simpleFilter);
                    var possibleEnumStringValue = simpleFilter.Replace("%", "");

                    dbArgs.Add("@simpleFilter", simpleFilter);

                    var simpleFilters = new List<string>();

                    simpleFilters.Add($"Name like @simpleFilter");
                    simpleFilters.Add($"Description like @simpleFilter");
                    simpleFilters.Add($"Qualifier like @simpleFilter");
                    SemanticSource source;
                    if (Enum.TryParse(possibleEnumStringValue, out source))
                    { 
                        simpleFilters.Add($"[Source] = {(int)source}");
                    }
                    SemanticStatus status;
                    if (Enum.TryParse(possibleEnumStringValue, out status))
                    {
                        simpleFilters.Add($"Status = {(int)status}");
                    }
                    simpleFilters.Add($"Threshold like @simpleFilter");
                    simpleFilters.Add($"Priority like @simpleFilter");
                    SemanticBaseType baseType;
                    if (Enum.TryParse(possibleEnumStringValue, out baseType))
                    {
                        simpleFilters.Add($"BaseType = {(int)baseType}");
                    }
                    simpleFilters.Add($"EffectiveDate like @simpleFilter");

                    whereStatements.Add($"({string.Join(" or ", simpleFilters)})");
                }
            }

            if (queryParams.ToList().Any(x => x.Key.ToLower() == "_pagenum"))
            {
                if (!int.TryParse(queryParams.FirstOrDefault(x => x.Key.ToLower() == "_pagenum").Value, out pageNum))
                {
                    pageNum = 1;
                }
            }

            if (queryParams.ToList().Any(x => x.Key.ToLower() == "_pagesize"))
            {
                if (!int.TryParse(queryParams.FirstOrDefault(x => x.Key.ToLower() == "_pagesize").Value, out pageSize))
                {
                    pageSize = 25;
                }
            }

            if (queryParams.ToList().Any(x => x.Key.ToLower() == "asofeffectivedate"))
            {
                if (!DateTime.TryParse(queryParams.FirstOrDefault(x => x.Key.ToLower() == "asofeffectivedate").Value, out asOfEffectiveDate))
                {
                    asOfEffectiveDate = DateTime.UtcNow;
                }
            }
            else
            {
                asOfEffectiveDate = DateTime.UtcNow;
            }
            dbArgs.Add("@asOfEffectiveDate", asOfEffectiveDate);

            if (queryParams.ToList().Any(x => x.Key.ToLower() == "_order"))
            {
                order = queryParams.FirstOrDefault(x => x.Key.ToLower() == "_order").Value;

                if (complexNonSortableFields.Contains(order))
                {
                    throw new GenericException(HttpStatusCode.BadRequest, "Invalid sort configuration", "You have provided a complex non-sortable field as an order parameter.");
                }
                if (!orderFields.ContainsKey(order))
                {
                    throw new GenericException(HttpStatusCode.BadRequest, "Invalid sort configuration", "You have provided an invalid field as an order parameter.");
                }

                order = orderFields[order]; // Get the appropriate column name.
            }
            
            if (queryParams.ToList().Any(x => x.Key.ToLower() == "_direction"))
            {
                direction = queryParams.FirstOrDefault(x => x.Key.ToLower() == "_direction").Value;
                if (direction != "asc" && direction != "desc")
                {
                    throw new GenericException(HttpStatusCode.BadRequest, "You have provided an invalid direction parameter.");
                }
            }

            var tableQuery = @"(select	ROW_NUMBER() OVER(PARTITION BY Qualifier ORDER BY EffectiveDate desc ) AS RowNum, * from Semantic where EffectiveDate <= @asOfEffectiveDate) S where S.RowNum = 1";
            var whereConjunction = whereStatements.Count > 0 ? "and" : "";

            var countSql = $"select count(1) as [Count] from {tableQuery} {whereConjunction} {string.Join(" and ", whereStatements)}";
            var sql = $"select * from {tableQuery} {whereConjunction} {string.Join(" and ", whereStatements)} order by {order} {direction} OFFSET {pageSize*(pageNum-1)} ROWS FETCH NEXT {pageSize} ROWS ONLY";

            var gridReader = await CompanyContext.Database.Connection.QueryMultipleAsync(
                  new CommandDefinition($"{countSql}; {sql}",
                  cancellationToken: cancellationToken.Value,
                  parameters: dbArgs,
                  commandTimeout: ApiTimeout
                ));

            var model = new GetSemantics { pageNum = pageNum, pageSize = pageSize };

            model.total = gridReader.Read<int>().FirstOrDefault();
            var repoModels = gridReader.Read<Semantic>().ToList();

            model.items = (
                from s in repoModels
                join c in CompanyContext.GlobalReportingResources on s.CreatedBy equals c.ResourceID
                join u in CompanyContext.GlobalReportingResources on s.UpdatedBy equals u.ResourceID
                select s.ToGetModel(c, u)
                ).ToList();

            return model;
        }

        public async Task<List<GetSemantic>> GetSemanticVersionsByQualifierAsync(string qualifier, IEnumerable<KeyValuePair<string, string>> queryParams, CancellationToken? cancellationToken = null)
        {
            if (cancellationToken == null)
            {
                cancellationToken = CancellationToken.None;
            }

            var qualifiers = new List<string> { qualifier };
            findLatestExistingSemantics(qualifiers, 1);

            var dbArgs = new DynamicParameters();
            dbArgs.Add("@qualifier", qualifier);

            var order = "EffectiveDate";
            var direction = "desc";

            if (queryParams.ToList().Any(x => x.Key.ToLower() == "_order"))
            {
                order = queryParams.FirstOrDefault(x => x.Key.ToLower() == "_order").Value;

                if (complexNonSortableFields.Contains(order))
                {
                    throw new GenericException(HttpStatusCode.BadRequest, "You have provided a complex non-sortable field as an order parameter.");
                }
                if (!orderFields.ContainsKey(order))
                {
                    throw new GenericException(HttpStatusCode.BadRequest, "You have provided an invalid field as an order parameter.");
                }

                order = orderFields[order]; // Get the appropriate column name.
            }

            if (queryParams.ToList().Any(x => x.Key.ToLower() == "_direction"))
            {
                direction = queryParams.FirstOrDefault(x => x.Key.ToLower() == "_direction").Value;
                if (direction != "asc" && direction != "desc")
                {
                    throw new GenericException(HttpStatusCode.BadRequest, "You have provided an invalid direction parameter.");
                }
            }

            var sql = $"select * from Semantic where Qualifier = @qualifier order by {order} {direction}";

            var repoModels = await CompanyContext.Database.Connection.QueryAsync<Semantic>(
                  new CommandDefinition(sql,
                  cancellationToken: cancellationToken.Value,
                  parameters: dbArgs,
                  commandTimeout: ApiTimeout
                ));

            var getModels = (
                            from s in repoModels
                            join c in CompanyContext.GlobalReportingResources on s.CreatedBy equals c.ResourceID
                            join u in CompanyContext.GlobalReportingResources on s.UpdatedBy equals u.ResourceID
                            select s.ToGetModel(c, u)
                            ).ToList();

            return getModels;
        }

        public async Task<List<GetSemantic>> PatchSemanticsAsync(List<PatchSemantic> semantics)
        {
            try
            {
                var qualifiers = semantics.Select(o => o.Qualifier).ToList();
                var existingSemantics = findLatestExistingSemantics(qualifiers, semantics.Count);

                #region Built-in semantic checking

                var nonUpdatedableSemantics = new List<string>();
                existingSemantics.ForEach(e =>
                {
                    if (e.Source == core.enums.SemanticSource.BuiltIn)
                    {
                        var patchModel = semantics.SingleOrDefault(o => o.Qualifier == e.Qualifier);
                        if (patchModel != null)
                        { 
                            if (patchModel.BaseType.HasValue
                                || patchModel.HeaderFilterStructured != null
                                || patchModel.HeaderFilterConfidence.HasValue
                                || patchModel.InvalidValuesStructured != null
                                || patchModel.JsonPayloadStructured != null
                                || patchModel.MatchType.HasValue
                                || patchModel.Maximum.HasValue
                                || patchModel.Minimum.HasValue
                                || patchModel.MinMaxPresent.HasValue
                                || patchModel.Priority.HasValue
                                || !string.IsNullOrEmpty(patchModel.RegularExpression)
                                || patchModel.Status.HasValue
                                || patchModel.Threshold.HasValue
                                || patchModel.ValidLocalesStructured != null
                                || patchModel.ValidValuesStructured != null)
                            {
                                nonUpdatedableSemantics.Add(e.Qualifier);
                            }
                        }
                    }
                });
                if (nonUpdatedableSemantics.Count > 0)
                {
                    throw new GenericException(
                        HttpStatusCode.Conflict,
                        "Semantics may not be updated.",
                        $"The following semantics are Built-in and may not have the specified properties updated: {string.Join("; ", nonUpdatedableSemantics)}."
                        );
                }

                #endregion

                var repoModels = (
                    from u in semantics
                    join e in existingSemantics on u.Qualifier equals e.Qualifier
                    select u.ToRepositoryModel(e, CompanyContext.CurrentResourceID)
                    ).ToList();

                var transactionId = generateTransactionId();
                repoModels.ForEach(s =>
                {
                    s.Validate();
                    s.TransactionId = transactionId;
                });

                CompanyContext.Semantics.AddRange(repoModels);
                await CompanyContext.SaveChangesAsync();
                addToChangeLog(transactionId, "U");

                var getModels = (
                                from s in repoModels
                                join c in CompanyContext.GlobalReportingResources on s.CreatedBy equals c.ResourceID
                                join u in CompanyContext.GlobalReportingResources on s.UpdatedBy equals u.ResourceID
                                select s.ToGetModel(c, u)
                                ).ToList();

                return getModels;
            }
            catch (GenericException ex)
            {
                throw ex;
            }
            catch (Exception ex)
            {
                throw ex;
                // TODO: Should we do something else here?
            }
        }

        public async Task<List<GetSemantic>> PostSemanticsAsync(List<PostSemantic> semantics)
        {
            try
            {
                var repoModels = semantics.Select(s => s.ToRepositoryModel(CompanyContext.CurrentResourceID)).ToList();
                var transactionId = generateTransactionId();
                repoModels.ForEach(s =>
                {
                    s.Validate();
                    s.TransactionId = transactionId;
                });
                var qualifiers = repoModels.Select(m => m.Qualifier).ToList();
                var matches = CompanyContext.Filter<Semantic>(s => qualifiers.Contains(s.Qualifier)).Select(s => s.Qualifier).Distinct().ToList();
                if (matches.Count > 0)
                {
                    throw new GenericException(HttpStatusCode.Conflict, 
                        "Potential conflicting semantics.", 
                        $"The following qualifiers already exist in your environment: {string.Join("; ", matches)}.");
                }
                CompanyContext.Semantics.AddRange(repoModels);
                await CompanyContext.SaveChangesAsync();
                addToChangeLog(transactionId, "C");

                var getModels = (
                              from s in repoModels
                              join c in CompanyContext.GlobalReportingResources on s.CreatedBy equals c.ResourceID
                              join u in CompanyContext.GlobalReportingResources on s.UpdatedBy equals u.ResourceID
                              select s.ToGetModel(c, u)
                              ).ToList();

                return getModels;
            }
            catch (GenericException ex)
            {
                throw ex;
            }
            catch (Exception ex)
            {
                throw ex;
                // TODO: Should we do something else here?
            }
        }

        public async Task<List<GetSemantic>> PutSemanticsAsync(List<PutSemantic> semantics)
        {
            try
            {
                var qualifiers = semantics.Select(o => o.Qualifier).ToList();
                var existingSemantics = findLatestExistingSemantics(qualifiers, semantics.Count);

                #region Built-in semantic checking

                var nonUpdatedableSemantics = new List<string>();
                existingSemantics.ForEach(e =>
                {
                    if (e.Source == core.enums.SemanticSource.BuiltIn)
                    {
                        nonUpdatedableSemantics.Add(e.Qualifier);
                    }
                });
                if (nonUpdatedableSemantics.Count > 0)
                {
                    throw new GenericException(
                        HttpStatusCode.Conflict,
                        "Semantics may not be updated.",
                        $"The following semantics are Built-in and may not be updated: {string.Join("; ", nonUpdatedableSemantics)}."
                        );
                }

                #endregion

                var repoModels = (
                    from u in semantics
                    join e in existingSemantics on u.Qualifier equals e.Qualifier
                    select u.ToRepositoryModel(e, CompanyContext.CurrentResourceID)
                    ).ToList();

                var transactionId = generateTransactionId();
                repoModels.ForEach(s =>
                {
                    s.Validate();
                    s.TransactionId = transactionId;
                });

                CompanyContext.Semantics.AddRange(repoModels);
                await CompanyContext.SaveChangesAsync();
                addToChangeLog(transactionId, "U");

                var getModels = (
                                from s in repoModels
                                join c in CompanyContext.GlobalReportingResources on s.CreatedBy equals c.ResourceID
                                join u in CompanyContext.GlobalReportingResources on s.UpdatedBy equals u.ResourceID
                                select s.ToGetModel(c, u)
                                ).ToList();

                return getModels;
            }
            catch (GenericException ex)
            {
                throw ex;
            }
            catch (Exception ex)
            {
                throw ex;
                // TODO: Should we do something else here?
            }
        }
    }
}
