using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace igx.IntegrationTests.TestData
{
    public sealed class RelationshipTestData
    {
        public static string ExecutionUri { get; set; } = null;
        public static JArray PredicateTypes { get; set; } = null;
        public static JArray RelationshipTypes { get; set; } = null;
        public static JObject Relationships { get; set; } = null;
        public static JObject RelationshipItem { get; set; } = null;

        public static JArray GetRelationshipForDelete(List<string> uids)
        {
            var arr = new JArray();
            foreach(var uid in uids)
            {
                var jObject = new JObject();
                jObject.Add(new JProperty("Uid", uid));
                jObject.Add(new JProperty("Cascade", true));
                arr.Add(jObject);
            }

            return arr;
        }

        public static JArray GetRelationshipsForInsert(List<string> subjectUids, List<string> objectUids)
        {
            var arr = new JArray();
            if (subjectUids.Count != objectUids.Count)
                throw new Exception("Subject and Object count should be same!");

            for(int i = 0; i< subjectUids.Count; i++)
            {
                var jObject = new JObject();
                jObject.Add(new JProperty("SubjectAssetUid", subjectUids[i]));
                jObject.Add(new JProperty("ObjectAssetUid", objectUids[i]));
                arr.Add(jObject);
            }


            return arr;
        }

    }
}
