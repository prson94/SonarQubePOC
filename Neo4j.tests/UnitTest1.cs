using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo4jClient;

namespace Neo4j.tests
{

    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            var client = new GraphClient(new Uri("http://hobby-noligehmoeaggbkeildcicol.dbs.graphenedb.com:24789/db/data/"));
            client.Connect();
            //var query = client.Cypher.Match("(person:Person)").Return(p => p.As<User>())

        }
    }
}
