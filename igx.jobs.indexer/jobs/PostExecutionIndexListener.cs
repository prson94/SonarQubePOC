using d360.core.entities;
using d360.core.queue;
using d360.extensions.search;
using d360.utils.company;
using Dapper;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using repositories;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace igx.jobs.indexer
{
	public class PostExecutionIndexListener : BaseWebJob
	{        
        const string FUNCTION_NAME = "PostExecution_IndexListener";

		readonly ElasticSearchSource Search;

		public PostExecutionIndexListener(IConfiguration config, ICommunity community, ElasticSearchSource search) : base(community, config)
		{
			Search = search;
		}

		[FunctionName(FUNCTION_NAME)]
		public async Task RunViaQueue([QueueTrigger(constants.Queue.PostExecutionIndex, Connection = constants.Setting.Storage)] string myQueueItem, ILogger log)
        {
            var message = JsonConvert.DeserializeObject<PostExecutionQueueMessage>(myQueueItem);

			var logProperties = new Dictionary<string, object> {
				{ "Function", FUNCTION_NAME },
				{ "CompanyID", message.CompanyID }
			};

			using (log.BeginScope(logProperties))
			{
				try
				{
					var list = new List<IndexObjectModel>();
					var searchUpserts = new List<Guid>();
					var searchDeletes = new List<Guid>();

					ApiExecutionAction action = ApiExecutionAction.Miscellaneous;

					var connectionString = await Community.GetConnectionStringForTenantAsync(message.CompanyID);
					using (var company = new SqlConnection(connectionString))
					{
						await company.OpenAsync();

						var execution = await company.QueryFirstOrDefaultAsync<ApiExecution>("select * from api.Execution where Id = @id", new { id = message.ExecutionId });

						if (execution != null)
						{
							action = execution.Action;
							string sql = "";
							IEnumerable<Guid> guidresults = null;
							IEnumerable<dynamic> results = null;
							switch (execution.Action)
							{
								case ApiExecutionAction.PatchCatalog:
									sql = @"
select distinct
		a.Uid
from	api.ExecutionCatalogItem l 
		inner join Asset a on a.Id = l.Id and l.[Type] = 'A' and l.Success = 1 and l.IsDelete = 0 and l.ExecutionId = @id";
									guidresults = await company.QueryAsync<Guid>(sql, new { id = message.ExecutionId });
									searchUpserts.AddRange(guidresults);
									break;
								case ApiExecutionAction.PostAssets:
								case ApiExecutionAction.PutAssets:
								case ApiExecutionAction.PostGroups:
								case ApiExecutionAction.PutGroups:
								case ApiExecutionAction.UpsertUsers:
									sql = @"
select distinct
		a.Uid
from	api.ExecutionLog l 
inner join api.Execution e on e.Id = l.ExecutionId and l.ExecutionId = @id 
cross apply openjson(l.Payload) with (AssetId int) p 
inner join Asset a on a.ID = p.AssetId ";
									guidresults = await company.QueryAsync<Guid>(sql, new { id = message.ExecutionId });
									searchUpserts.AddRange(guidresults);
									break;
								case ApiExecutionAction.DeleteAssets:
									sql = @"
select	p.Object, 
		p.ObjectId,
		p.AssetId
from	api.ExecutionLog l
		inner join api.Execution e on e.Id = l.ExecutionId 
		cross apply openjson(l.Payload) with (AssetId int, Object varchar(50), ObjectId int, ObjectName nvarchar(250), TypeName nvarchar(250)) p
where l.ExecutionId = @Id;";
									results = await company.QueryAsync(sql, new { id = message.ExecutionId });
									foreach (var result in results)
									{
										var indexObject = new IndexObjectModel
										{
											CompanyID = message.CompanyID,
											Category = SearchIndexer.GetCategoryFromObject(result.Object),
											ID = result.ObjectId,
											To = QueueAction.RemoveFromIndex,
											RelativeUrl = "#",
											AssetID = result.AssetId,
											ItemUniqueID = result.AssetId.ToString()
										};

										//Set uniqueID for index object
										if (result.Object == "Synonym")
										{
											indexObject.ItemUniqueID = $"custom|{result.ObjectId}";
										}
										//Intersects have two search documents, se we need to delete both
										else if (result.Object == "Intersect")
										{
											indexObject.Category = "Synonym";
											indexObject.AssetType = "Synonym";

											IndexObjectModel reciprocal = indexObject.ShallowCopy();
											reciprocal.ItemUniqueID = $"intersect|{result.ObjectId}|O";
											indexObject.ItemUniqueID = $"intersect|{result.ObjectId}|S";
											list.Add(reciprocal);
										}
										
										list.Add(indexObject);
									}
									break;
								case ApiExecutionAction.DeleteGroups:
									sql = @"
select	p.Object, 
		p.ID as ObjectId,
		p.AssetId
from	api.ExecutionLog l
		inner join api.Execution e on e.Id = l.ExecutionId 
		cross apply openjson(l.Payload) with (AssetId int, Object varchar(50), ID int, ObjectName nvarchar(250), TypeName nvarchar(250)) p
where l.ExecutionId = @Id;";
									results = await company.QueryAsync(sql, new { id = message.ExecutionId });
									foreach (var result in results)
									{
										var indexObject = new IndexObjectModel
										{
											CompanyID = message.CompanyID,
											Category = SearchIndexer.GetCategoryFromObject(result.Object),
											ID = result.ObjectId,
											To = QueueAction.RemoveFromIndex,
											RelativeUrl = "#",
											AssetID = result.AssetId,
											ItemUniqueID = result.AssetId.ToString()
										};

										list.Add(indexObject);
									}
									break;
								case ApiExecutionAction.Miscellaneous:
									if (execution.Route.StartsWith("/api/v2/process/"))
									{
										sql = @"
select distinct
		eda.Uid
from	api.ExecutionDiagramAsset eda 
inner join api.Execution e on eda.ExecutionID = e.ExecutionID and e.id = @id
where	eda.Action in ('Insert', 'Update') ";
										guidresults = await company.QueryAsync<Guid>(sql, new { id = message.ExecutionId });
										searchUpserts.AddRange(guidresults);
										sql = @"
select distinct
		eda.Uid
from	api.ExecutionDiagramAsset eda 
inner join api.Execution e on eda.ExecutionID = e.ExecutionID and e.id = @id
where	eda.Action = 'Delete' ";
										guidresults = await company.QueryAsync<Guid>(sql, new { id = message.ExecutionId });
										searchDeletes.AddRange(guidresults);
									}
									break;
								default:
									//do nothing
									break;
							}
						}

						if (searchUpserts.Any() || searchDeletes.Any())
						{
							var indexer = new SearchIndexer(company, message.CompanyID, Search);

							if (searchUpserts.Any())
							{
								indexer.IndexAssets(searchUpserts);
							}

							if (searchDeletes.Any())
							{
								indexer.RemoveAssets(searchDeletes);
							}
						}
					}

					if (list.Count > 0)
					{
						switch (action)
						{
							case ApiExecutionAction.DeleteAssets:
								Search.RemoveFromIndex(list);
								break;
							case ApiExecutionAction.DeleteGroups:
								Search.RemoveFromIndex(list);
								break;
							default:
								//do nothing
								break;
						}
					}
				}
				catch (Exception ex)
				{
					log.LogCritical(ex, "Critical error on PostExecutionIndexListener web job.");
				}			
			}
		}
    }
}
