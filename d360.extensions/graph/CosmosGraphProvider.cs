using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Graphs;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace d360.extensions.graph
{
    public class VertexModel
    {
        public string ID { get { return $"{ObjectType}|{ObjectID}"; } }
        public string Name { get; set; }
        public string ObjectType { get; set; }

        public string ObjectID { get; set; }
    }
    public class CosmosGraphProvider : IGraphProvider
    {
        public async void AddVertices<T>(int companyId, IEnumerable<T> items)
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

                
                //client.CreateDocumentAsync()
            }
        }

        public async Task AddVertex(int companyId, string id, string objectType, IDictionary<string, string> properties)
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
                                
                var gremlin = $"g.addV('{objectType}').property('id', '{id}')";

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

        public async void ClearData(int companyId)
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
