using d360.core.entities;
using d360.core.queue;
using d360.extensions.search;
using d360.utils.company;
using Dapper;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace igx.jobs.indexer
{
	public class PostExecutionIndexListener : BaseWebJob
	{        
        const string FUNCTION_NAME = "PostExecution_IndexListener";

		readonly ElasticSearchSource Search;

		public PostExecutionIndexListener(IConfiguration config, ElasticSearchSource search) : base(config)
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
					ApiExecutionAction action = ApiExecutionAction.Miscellaneous;
					
					using (var company = CompanyConnectionUtils.GetCompanyConnection(message.CompanyID, ConnString))
					{
						var execution = await company.QueryFirstOrDefaultAsync<ApiExecution>("select * from api.Execution where Id = @id", new { id = message.ExecutionId });

						if (execution != null)
						{
							action = execution.Action;
							string sql = "";
							IEnumerable<dynamic> results = null;
							switch (execution.Action)
							{
								case ApiExecutionAction.PatchCatalog:
									sql = @"
select	a.Id, 
		a.Uid, 
		a.Object, 
		a.ObjectId,
		adv.DisplayValue, 
		ap.[Segments],
		cast(t.DefaultPermissions as bit) as DefaultPermissions,
		t.Uid as AssetTypeUid,
		t.Name as AssetType 
from	api.ExecutionCatalogItem l 
		inner join Asset a on a.Id = l.Id and l.ExecutionId = @Id and l.[Type] = 'A' and l.Success = 1 and l.IsDelete = 0 and l.ExecutionId = @id 
		inner join AssetType t on t.Id = a.AssetTypeId 
		inner join AssetDisplayValue adv on adv.AssetId = a.Id 
		left join AssetPath ap on ap.Id = a.Id ";
									results = await company.QueryAsync(sql, new { id = message.ExecutionId });
									foreach (var result in results)
									{
										var indexObject = new IndexObjectModel
										{
											CompanyID = message.CompanyID,
											Category = SearchIndexer.GetCategoryFromObject(result.Object),
											ID = result.ObjectId,
											To = (action == ApiExecutionAction.PostAssets ? QueueAction.AddToIndex : QueueAction.UpdateInIndex),
											RelativeUrl = $"/asset/{result.Uid.ToString().ToLower()}",
											AssetID = result.Id,
											ItemUniqueID = result.Id.ToString(),
											Uid = result.Uid,
											AssetType = result.AssetType,
											AssetTypeUid = result.AssetTypeUid,
											DefaultPermissions = result.DefaultPermissions,
											Fields = new Dictionary<string, string> { { "Name", result.DisplayValue } }
										};
										list.Add(indexObject);
									}
									break;
								case ApiExecutionAction.PostAssets:
								case ApiExecutionAction.PutAssets:
									sql = @"
select	a.Id, 
		a.Uid, 
		a.Object, 
		a.ObjectId,
		adv.DisplayValue, 
		ap.[Segments],
		cast(t.DefaultPermissions as bit) as DefaultPermissions,
		t.Uid as AssetTypeUid,
		t.Name as AssetType 
from	api.ExecutionLog l 
inner join api.Execution e on e.Id = l.ExecutionId and l.ExecutionId = @id 
cross apply openjson(l.Payload) with (AssetId int) p 
inner join Asset a on a.ID = p.AssetId 
inner join AssetType t on t.Id = a.AssetTypeId 
inner join AssetDisplayValue adv on adv.AssetId = a.Id 
left join AssetPath ap on ap.Id = a.Id ";
									results = await company.QueryAsync(sql, new { id = message.ExecutionId });
									foreach (var result in results)
									{
										var indexObject = new IndexObjectModel
										{
											CompanyID = message.CompanyID,
											Category = SearchIndexer.GetCategoryFromObject(result.Object),
											ID = result.ObjectId,
											To = (action == ApiExecutionAction.PostAssets ? QueueAction.AddToIndex : QueueAction.UpdateInIndex),
											RelativeUrl = $"/asset/{result.Uid.ToString().ToLower()}",
											AssetID = result.Id,
											ItemUniqueID = result.Id.ToString(),
											Uid = result.Uid,
											AssetType = result.AssetType,
											AssetTypeUid = result.AssetTypeUid,
											DefaultPermissions = result.DefaultPermissions,
											Fields = new Dictionary<string, string> { { "Name", result.DisplayValue } }
										};
										list.Add(indexObject);
									}
									break;
								case ApiExecutionAction.PostGroups:
								case ApiExecutionAction.PutGroups:
									sql = @"
select	a.Id, 
		a.Uid, 
		a.Name,
		p.AssetId
from	api.ExecutionLog l 
inner join api.Execution e on e.Id = l.ExecutionId and l.ExecutionId = @id 
cross apply openjson(l.Payload) with (ID int, AssetId bigint) p 
inner join [Group] a on a.ID = p.ID";
									results = await company.QueryAsync(sql, new { id = message.ExecutionId });
									foreach (var result in results)
									{
										var indexObject = new IndexObjectModel
										{
											CompanyID = message.CompanyID,
											Category = SearchIndexer.GetCategoryFromObject("Group"),
											ID = result.Id,
											To = (action == ApiExecutionAction.PostGroups ? QueueAction.AddToIndex : QueueAction.UpdateInIndex),
											RelativeUrl = $"/group/{result.Id}",
											AssetID = result.AssetId,
											ItemUniqueID = result.Id.ToString(),
											Uid = result.Uid,
											AssetType = "Group",
											DefaultPermissions = true,
											Fields = new Dictionary<string, string> { { "Name", result.Name } }
										};
										list.Add(indexObject);
									}
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

										list.Add(indexObject);
									}
									break;
								default:
									//do nothing
									break;
							}
						}
					}

					if (list.Count > 0)
					{
						switch (action)
						{
							case ApiExecutionAction.PatchCatalog:
								Search.AddToIndex(list);
								break;
							case ApiExecutionAction.PostAssets:
								Search.AddToIndex(list);
								break;
							case ApiExecutionAction.PutAssets:
								Search.UpdateInIndex(list);
								break;
							case ApiExecutionAction.PostGroups:
								Search.AddToIndex(list);
								break;
							case ApiExecutionAction.PutGroups:
								Search.UpdateInIndex(list);
								break;
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
