using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Graphs;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace d360.extensions.graph
{
    public class CosmosJsonConverter : JsonConverter
    {
    
    public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(VertexModel) || objectType == typeof(EdgeModel);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var obj = value as VertexModel;

            if (obj != null)
            {
                JObject o = new JObject();
                o.Add(new JProperty("label", obj.Label));
                o.Add(new JProperty("id", obj.ID));

                foreach (var item in obj.Properties)
                {
                    var ar = new JArray();

                    JObject props = new JObject();
                    props.Add(new JProperty("_value", item.Value));
                    props.Add(new JProperty("id", Guid.NewGuid()));

                    ar.Add(props);

                    o.Add(new JProperty(item.Key, ar));
                }

                o.WriteTo(writer);

                return;
            }

            var edgeObj = value as EdgeModel;

            if (edgeObj != null)
            {
                JObject o = new JObject();
                o.Add(new JProperty("label", edgeObj.RelationshipType));
                o.Add(new JProperty("id", Guid.NewGuid()));
                o.Add(new JProperty("_isEdge", true));
                o.Add(new JProperty("_sink", edgeObj.EndID));
                o.Add(new JProperty("_sinkLabel", edgeObj.EndLabel));
                o.Add(new JProperty("_vertexId", edgeObj.StartID));
                o.Add(new JProperty("_vertexLabel", edgeObj.StartLabel));

                o.WriteTo(writer);

                return;
            }
        }
    }

    public class CosmosGraphProvider : IGraphProvider
    {
        public async Task AddObjects<T>(int companyId, IEnumerable<T> items)
        {
            using (DocumentClient client = new DocumentClient(
                new Uri(d360.core.constants.COSMOS_ENDPOINT),
                d360.core.constants.COSMOS_AUTH_KEY,
                new ConnectionPolicy { ConnectionMode = ConnectionMode.Direct, ConnectionProtocol = Protocol.Tcp }))
            {
                Database database = await client.CreateDatabaseIfNotExistsAsync(new Database { Id = "graphdb" });

                DocumentCollection graph = await client.CreateDocumentCollectionIfNotExistsAsync(
                    UriFactory.CreateDatabaseUri("graphdb"),
                    new DocumentCollection { Id = $"D3S{companyId}" });

                try
                {
                    int currentCount = 0;
                    int documentCount = items.Count();
                    int itemsPerInsert = 200;
                    var delay = TimeSpan.Zero;

                    while (currentCount < documentCount)
                    {
                        await Task.Delay(delay);
                        delay = TimeSpan.Zero;

                        string argsJson = JsonConvert.SerializeObject(items.Skip(currentCount).Take(itemsPerInsert).ToArray(), Formatting.Indented, new CosmosJsonConverter());

                        var args = new dynamic[] { JsonConvert.DeserializeObject<dynamic[]>(argsJson) };

                        try
                        {
                            var sprocResponse = await client.ExecuteStoredProcedureAsync<dynamic>($"/dbs/graphdb/colls/D3S{companyId}/sprocs/bulkImport/", args);

                            int insertCount = (int)sprocResponse.Response;

                            currentCount += itemsPerInsert;
                        }
                        catch(DocumentClientException e)
                        {
                            var statusCode = (int)e.StatusCode;
                            if (statusCode == 429)
                            {
                                delay = e.RetryAfter;
                            }
                            else
                                throw;
                        }
                    }
                }
                catch (Exception e)
                {
                    System.Diagnostics.Debug.WriteLine("Error: {0}", e.Message);
                }
            }
        }

        
        public async Task AddVertex(int companyId, string id, string label, IDictionary<string, string> properties)
        {
            using (DocumentClient client = new DocumentClient(
                new Uri(d360.core.constants.COSMOS_ENDPOINT),
                d360.core.constants.COSMOS_AUTH_KEY,
                new ConnectionPolicy { ConnectionMode = ConnectionMode.Direct, ConnectionProtocol = Protocol.Tcp }))
            {
                Database database = await client.CreateDatabaseIfNotExistsAsync(new Database { Id = "graphdb" });

                DocumentCollection graph = await client.CreateDocumentCollectionIfNotExistsAsync(
                    UriFactory.CreateDatabaseUri("graphdb"),
                    new DocumentCollection { Id = $"D3S{companyId}" });
                                
                var gremlin = $"g.addV('{label}').property('id', '{id}')";

                foreach (var item in properties)
                {
                    gremlin += $".property('{item.Key}','{item.Value}')";
                }

                var query = client.CreateGremlinQuery<dynamic>(graph, gremlin);

                while (query.HasMoreResults)
                {
                    foreach (dynamic result in await query.ExecuteNextAsync())
                    {
                        System.Diagnostics.Debug.WriteLine($"\t {JsonConvert.SerializeObject(result)}");
                    }
                }
            }
        }

        public async Task AddEdge(int companyId, string startId, string endId, string relationshipName, IDictionary<string, string> properties)
        {
            using (DocumentClient client = new DocumentClient(
                new Uri(d360.core.constants.COSMOS_ENDPOINT),
                d360.core.constants.COSMOS_AUTH_KEY,
                new ConnectionPolicy { ConnectionMode = ConnectionMode.Direct, ConnectionProtocol = Protocol.Tcp }))
            {
                Database database = await client.CreateDatabaseIfNotExistsAsync(new Database { Id = "graphdb" });

                DocumentCollection graph = await client.CreateDocumentCollectionIfNotExistsAsync(
                    UriFactory.CreateDatabaseUri("graphdb"),
                    new DocumentCollection { Id = $"D3S{companyId}" });

                var gremlin = $"g.V('{startId.Trim()}').addE('{relationshipName}')";

                foreach (var item in properties)
                {
                    gremlin += $".property('{item.Key}','{item.Value}')";
                }

                gremlin += $".to(g.V('{endId.Trim()}'))";


                var query = client.CreateGremlinQuery<dynamic>(graph, gremlin);

                while (query.HasMoreResults)
                {
                    foreach (dynamic result in await query.ExecuteNextAsync())
                    {
                        System.Diagnostics.Debug.WriteLine($"\t {JsonConvert.SerializeObject(result)}");
                    }
                }
            }
        }

        public async Task DeleteEdge(int companyId, string field, string value)
        {
            using (DocumentClient client = new DocumentClient(
                new Uri(d360.core.constants.COSMOS_ENDPOINT),
                d360.core.constants.COSMOS_AUTH_KEY,
                new ConnectionPolicy { ConnectionMode = ConnectionMode.Direct, ConnectionProtocol = Protocol.Tcp }))
            {
                Database database = await client.CreateDatabaseIfNotExistsAsync(new Database { Id = "graphdb" });

                DocumentCollection graph = await client.CreateDocumentCollectionIfNotExistsAsync(
                    UriFactory.CreateDatabaseUri("graphdb"),
                    new DocumentCollection { Id = $"D3S{companyId}" });

                var gremlin = $"g.E().has('{field}', '{value}').drop()";

                var query = client.CreateGremlinQuery<dynamic>(graph, gremlin);

                while (query.HasMoreResults)
                {
                    foreach (dynamic result in await query.ExecuteNextAsync())
                    {
                        System.Diagnostics.Debug.WriteLine($"\t {JsonConvert.SerializeObject(result)}");
                    }
                }
            }
        }

        public async Task ClearData(int companyId)
        {
            using (DocumentClient client = new DocumentClient(
                new Uri(d360.core.constants.COSMOS_ENDPOINT),
                d360.core.constants.COSMOS_AUTH_KEY,
                new ConnectionPolicy { ConnectionMode = ConnectionMode.Direct, ConnectionProtocol = Protocol.Tcp }))
            {
                Database database = await client.CreateDatabaseIfNotExistsAsync(new Database { Id = "graphdb" });

                DocumentCollection graph = await client.CreateDocumentCollectionIfNotExistsAsync(
                    UriFactory.CreateDatabaseUri("graphdb"),
                    new DocumentCollection { Id = $"D3S{companyId}" });

                var query = client.CreateGremlinQuery<dynamic>(graph, "g.V().drop()");

                while (query.HasMoreResults)
                {
                    foreach (dynamic result in await query.ExecuteNextAsync())
                    {
                        //Console.WriteLine($"\t {JsonConvert.SerializeObject(result)}");
                    }
                }
            }
        }
    }
}
