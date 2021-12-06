using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using d360.core.entities;
using d360.core.enums;
using Dapper;
using Newtonsoft.Json;
using d360.core;
using d360.core.queue;
using d360.extensions;
using System.Net;
using d360.core.helpers;
using d360.core.resources;
using System.Xml.Linq;
using Newtonsoft.Json.Linq;
using SpreadsheetLight;
using d360.model.DataAccessLayer.repositories;
using d360.model.helpers;
using d360.core.entities.Process;
using AngleSharp.Text;
using System.Drawing;
using System.Threading;
using d360.model.helpers.filters;
using d360.core.exceptions;

namespace d360.model.DataAccessLayer
{
    public class SemanticsRepository : BaseRepository, ISemanticsRepository
    {
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

        public async Task<GetSemantics> GetSemanticsAsync(IEnumerable<KeyValuePair<string, string>> queryParams, CancellationToken? cancellationToken = null)
        {
            if (cancellationToken == null)
            {
                cancellationToken = CancellationToken.None;
            }

            var dbArgs = new DynamicParameters();

            var pageNum = 1;
            var pageSize = 25;
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

                    dbArgs.Add("@simpleFilter", simpleFilter);

                    var simpleFilters = new List<string>();

                    simpleFilters.Add($"Name like @simpleFilter");
                    simpleFilters.Add($"Description like @simpleFilter");
                    simpleFilters.Add($"Qualifier like @simpleFilter");
                    simpleFilters.Add($"Status like @simpleFilter");
                    simpleFilters.Add($"[Source] like @simpleFilter");
                    simpleFilters.Add($"Threshold like @simpleFilter");
                    simpleFilters.Add($"Priority like @simpleFilter");
                    simpleFilters.Add($"BaseType like @simpleFilter");
                    simpleFilters.Add($"EffectiveDate like @simpleFilter");

                    whereStatements.Add($"({string.Join(" or ", simpleFilters)})");
                }
            }

            if (queryParams.ToList().Any(x => x.Key.ToLower() == "_pageNum"))
            {
                if (!int.TryParse(queryParams.FirstOrDefault(x => x.Key.ToLower() == "_pageNum").Value, out pageNum))
                {
                    pageNum = 1;
                }
            }

            if (queryParams.ToList().Any(x => x.Key.ToLower() == "_pageSize"))
            {
                if (!int.TryParse(queryParams.FirstOrDefault(x => x.Key.ToLower() == "_pageSize").Value, out pageSize))
                {
                    pageSize = 25;
                }
            }

            if (queryParams.ToList().Any(x => x.Key.ToLower() == "asOfEffectiveDate"))
            {
                if (!DateTime.TryParse(queryParams.FirstOrDefault(x => x.Key.ToLower() == "asOfEffectiveDate").Value, out asOfEffectiveDate))
                {
                    asOfEffectiveDate = DateTime.UtcNow;
                }
            }

            if (queryParams.ToList().Any(x => x.Key.ToLower() == "_order"))
            {
                order = queryParams.FirstOrDefault(x => x.Key.ToLower() == "_order").Value;
                var orderFields = new List<string> { "baseType", "description", "effectiveDate", "name", "priority", "qualifier", "source", "status", "threshold" };
                if (!orderFields.Contains(order))
                {
                    throw new GenericException(HttpStatusCode.BadRequest, "You have provided an invalid field as an order parameter.");
                }
            }

            if (queryParams.ToList().Any(x => x.Key.ToLower() == "_direction"))
            {
                direction = queryParams.FirstOrDefault(x => x.Key.ToLower() == "_direction").Value;
                if (direction != "asc" && direction != "desc")
                {
                    throw new GenericException(HttpStatusCode.BadRequest, "You have provided an invalid direction parameter.");
                }
            }

            var countSql = $"select count(1) as [Count] from Semantic where {string.Join(" and ", whereStatements)}";
            var sql = $"select * from Semantic where {string.Join(" and ", whereStatements)} order by {order} {direction} OFFSET {pageSize*(pageNum-1)} ROWS FETCH NEXT {pageSize} ROWS ONLY";

            var gridReader = await CompanyContext.Database.Connection.QueryMultipleAsync(
                  new CommandDefinition($"{countSql}; {sql}",
                  cancellationToken: cancellationToken.Value,
                  parameters: dbArgs,
                  commandTimeout: ApiTimeout
                ));

            var model = new GetSemantics { pageNum = pageNum, pageSize = pageSize };

            model.total = gridReader.Read<int>().FirstOrDefault();
            model.items = gridReader.Read<Semantic>().Select(o => o.ToGetModel()).ToList();
            
            return model;
        }

        public async Task<List<GetSemantic>> GetSemanticVersionsByQualifierAsync(string qualifier, IEnumerable<KeyValuePair<string, string>> queryParams, CancellationToken? cancellationToken = null)
        {
            if (cancellationToken == null)
            {
                cancellationToken = CancellationToken.None;
            }

            var dbArgs = new DynamicParameters();
            dbArgs.Add("@qualifier", qualifier);

            var order = "EffectiveDate";
            var direction = "desc";

            if (queryParams.ToList().Any(x => x.Key.ToLower() == "_order"))
            {
                order = queryParams.FirstOrDefault(x => x.Key.ToLower() == "_order").Value;
                var orderFields = new List<string> { "baseType", "description", "effectiveDate", "name", "priority", "qualifier", "source", "status", "threshold" };
                if (!orderFields.Contains(order))
                {
                    throw new GenericException(HttpStatusCode.BadRequest, "You have provided an invalid field as an order parameter.");
                }
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

            var items = await CompanyContext.Database.Connection.QueryAsync<Semantic>(
                  new CommandDefinition(sql,
                  cancellationToken: cancellationToken.Value,
                  parameters: dbArgs,
                  commandTimeout: ApiTimeout
                ));

            return items.Select(o => o.ToGetModel()).ToList();
        }

        public async Task<List<GetSemantic>> PatchSemanticsAsync(List<PatchSemantic> semantics)
        {
            try
            {
                return semantics;
            }
            catch (Exception ex)
            {
                string message = ex.GetFullExceptionData(false, constants.ERROR_MESSAGE_CHARACTER_LIMIT);
            }
        }

        public async Task<List<GetSemantic>> PostSemanticsAsync(List<PostSemantic> semantics)
        {
            try
            {
                return semantics;
            }
            catch (Exception ex)
            {
                string message = ex.GetFullExceptionData(false, constants.ERROR_MESSAGE_CHARACTER_LIMIT);
            }
        }

        public async Task<List<GetSemantic>> PutSemanticsAsync(List<PutSemantic> semantics)
        {
            try
            {
                return semantics;
            }
            catch (Exception ex)
            {
                string message = ex.GetFullExceptionData(false, constants.ERROR_MESSAGE_CHARACTER_LIMIT);
            }
        }

        public async void DeleteSemanticAsync(string qualifier)
        {
            try
            {

            }
            catch (Exception ex)
            {
                string message = ex.GetFullExceptionData(false, constants.ERROR_MESSAGE_CHARACTER_LIMIT);
            }
        }
    }
}
