using System;
using System.Collections.Generic;

namespace d360.core.search
{
	public class IndexResult : TypeaheadResult
    {
        public IndexResult()
        {
            Scores = new List<IndexAssetScore>();
        }
        
        public string ID { get; set; }
        
        public string Description { get; set; }
        
        public string AbsoluteUrl { get; set; }
        
        public float Score { get; set; }
        
        /// <summary>
        /// score ranging between 1 and 0 adjusted based on max value.
        /// </summary>
        public float NormalizedScore { get; set; }
        
        public string Explanation { get; set; }
        
        public List<IndexFieldDisplay> Fields { get; set; }
        
        public string Status { get; set; }
        
        public string Object { get; set; }
        
        public long ObjectId { get; set; }
        
        public bool HasProfiling { get; set; }
        
        public List<IndexAssetScore> Scores { get; set; }

		public string SemanticName { get; set; }

		public string SemanticQualifier { get; set; }

		public Guid? SemanticUid { get; set; }
	}
}
