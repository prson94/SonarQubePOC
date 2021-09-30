using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using d360.core;
using d360.core.queue;

namespace d360.extensions
{
    #region Extensions Models

    public class AggregationFilter
    {
        public string Field { get; set; }
        public string[] Values { get; set; }
    }

    public class FieldFilter
    {
        public string Field { get; set; }
        public string[] Values { get; set; }
        public SearchConnector Connector { get; set; } = SearchConnector.Or;
        public SearchOperator Operator { get; set; } = SearchOperator.Contains;
        public bool MatchWords { get; set; } = false;
    }

    public class FieldBoost
    {
        public string Field { get; set; }
        public float Boost { get; set; }
    }

    public class IndexableType
    {
        public string Name { get; set; }
        public int Class { get; set; }
        public string ClassName { get; set; }
        public Guid AssetTypeUid { get; set; }
    }

    public class IndexableCount
    {
        public string ClassName { get; set; }
        public int Class { get; set; }
        public Guid AssetTypeUid { get; set; }
        public int CurrentCount { get; set; }
    }

    public class IndexableStatus : IndexableCount
    {
        public int Status { get; set; }
        public int TargetCount { get; set; }
        public DateTime Start { get; set; }
        public DateTime LastUpdate { get; set; }
    }

    public class QueryRequest
    {
        public const int SEARCH_TERM_MAX_LENGTH = 255;
        public QueryRequest()
        {
            AggregationFilters = new List<AggregationFilter>();
            FieldFilters = new List<FieldFilter>();
            Aggregations = new List<string>();
            FieldBoosters = new List<FieldBoost>();
        }
        private string _term;
        public string Term {
            get
            {
                return _term;
            }
            set
            {
                if (value != null && value.Length > SEARCH_TERM_MAX_LENGTH)
                {
                    _term = value.Substring(0, SEARCH_TERM_MAX_LENGTH);
                }
                else
                {
                    _term = value;
                }
            }
        }
        public int Size { get; set; } = 100;
        public int From { get; set; } = 0;
        public List<AggregationFilter> AggregationFilters { get; set; }
        public List<FieldFilter> FieldFilters { get; set; }
        public List<string> Aggregations { get; set; }
        public SearchConnector SearchConnector { get; set; } = SearchConnector.And;
        public bool Explain { get; set; } = false;
        public List<FieldBoost> FieldBoosters { get; set; }
    }

    public class QueryLimitation
    {
        public QueryLimitation()
        {
            AggregationFilters = new List<AggregationFilter>();
            ResourceGroupIDs = new List<int>();
            ResourceOrgIDs = new List<int>();
        }
        public List<AggregationFilter> AggregationFilters { get; set; }
        public bool HideData3SixtyUsers { get; set; } = false;
        public int ResourceID { get; set; }
        public List<int> ResourceGroupIDs { get; set; }
        public List<int> ResourceOrgIDs { get; set; }
    }

    public class IndexTypeList
    {

        public IndexTypeList()
        {
            Categories = new List<IndexCategory>();
        }

        public string Name { get; set; }
        public string DisplayName { get; set; }
        public int ResultCount { get; set; }

        public List<IndexCategory> Categories { get; set; }
    }

    public class IndexCategory
    {
        public string Name { get; set; }
        public int ResultCount { get; set; }
    }

    public class IndexResults
    {
        public IndexResults()
        {
            Results = new List<IndexResult>();
        }

        public List<IndexResult> Results { get; set; }
        public int Matches { get; set; }
        public int ElapsedMS { get; set; }
    }

    public class IndexTag : IEqualityComparer<IndexTag>, IEquatable<IndexTag>
    {
        public Guid? Uid { get; set; }
        public string Value { get; set; }
        private string _highlight = null;
        public string Highlight
        {
            get
            {
                return _highlight ?? Value;
            }
            set
            {
                _highlight = value;
            }
        }

        public bool Equals(IndexTag other)
        {
            return other?.Uid == Uid;
        }

        public bool Equals(IndexTag x, IndexTag y)
        {
            return x.Equals(y);
        }

        public int GetHashCode(IndexTag obj)
        {
            return obj.Uid.GetHashCode();
        }
        public override bool Equals(object obj) => Equals(obj as IndexTag);
        public override int GetHashCode()
        {
            return GetHashCode(this);
        }
    }

    public class TypeaheadResult
    {
        public TypeaheadResult()
        {
            Tags = new List<IndexTag>();
        }
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public string Group { get; set; }
        public string Type { get; set; }
        public string Url { get; set; }

        public string Icon { get; set; }
        public string ImageUrl { get; set; }
        public List<PathComponent> AssetPath { get; set; }
        public Guid? Uid { get; set; }
        public Guid? AssetTypeUid { get; set; }
        public List<IndexTag> Tags { get; set; }

        public bool MissingIcon()
        {
            return string.IsNullOrEmpty(Icon) && string.IsNullOrEmpty(ImageUrl);
        }
    }

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
    }

    public class IndexAssetScore
    {
        public Guid AssetUid { get; set; }
        public string EffectiveDate { get; set; }
        public string EndDate { get; set; }
        public string Rundate { get; set; }
        public string ScoreType { get; set; }
        public decimal Value { get; set; }
        public int LowerThreshold { get; set; }
        public int UpperThreshold { get; set; }
    }

    public class IndexFieldDisplay
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string Label { get; set; }
        public string Prefix { get; set; }
        public string Suffix { get; set; }
        public string Value { get; set; }
        public bool Empty { get; set; }
    }

    public enum SearchOperator
    {
        Contains,
        NotContains
    }

    public enum SearchConnector
    {
        And,
        Or
    }

    public class AdvancedSearchParameters
    {
        public string field { get; set; }
        public string value { get; set; }
        public bool exact { get; set; }
        public SearchConnector connector { get; set; }
    }

    public class PathComponent
    {
        public string[] Key { get; set; }
        public string AssetType { get; set; }
    }

    #endregion

    #region Extensions Exceptions

    public class TooManyTypesException : Exception 
    {
        protected TooManyTypesException(): base("More than one type found in the list of items to index.  Please ensure that items contains only one type.")
        {
        }
    }

    public class AddToIndexException : Exception
    {
        public AddToIndexException(Exception ex)
            : base("An error occured while trying to add item to index.", ex)
        {
        }
    }

    public class SearchResultsException : Exception
    {
        public SearchResultsException(Exception ex)
            : base("An error occured while trying to get search results.", ex)
        {
        }
    }

    #endregion

    public interface ISearchSource
    {
        /// <summary>
        /// Add the indexable item to the index queue for indexing processing.
        /// </summary>
        /// <param name="item">The item to index, containing all relevant information to add to the index.</param>
        void AddToIndex(IndexObjectModel item);

        void AddToIndex(IEnumerable<IndexObjectModel> items);

        /// <summary>
        /// Clears out all entries from a company's index.
        /// </summary>
        /// <param name="companyID">The current company ID</param>
        void ClearIndex(int companyID);

        /// <summary>
        /// Clears out all entries from a company's index based on category/class and optionally asset type.
        /// </summary>
        /// <param name="companyID">The current company ID</param>
        /// <param name="category">The current categroy/class</param>
        /// <param name="assetType">Optional asset type</param>
        void ClearIndex(int companyID, string category, string assetType = null);

        /// <summary>
        /// Clears out all entries from a company's index based on asset type UID.
        /// </summary>
        /// <param name="companyID">The current company ID</param>
        /// <param name="assetTypeUid">UID of Asset Type to remove/param>
        void ClearIndex(int companyID, Guid assetTypeUid);

        IndexResults GetSearchResultsWithAggregation(int companyID, int resourceID, QueryRequest queryRequest, List<IndexTypeList> categories, QueryLimitation queryLimit);

        IEnumerable<TypeaheadResult> GetTypeaheadResults(int companyID, int resourceID, string phrase, QueryLimitation queryLimit, int size = 10, string type = "");
        
        IndexResults GetSearchResults(int companyID, int resourceID, string phrase, int size, int from, string group = "");

        IndexResults GetStatusSearch(int companyID, List<IndexTypeList> categories, bool withTypes = false);

        List<IndexableCount> GetStatusList(int companyID);

        /// <summary>
        /// Get list of phrases that match the starting term used by autocomplete to suggest matches
        /// </summary>
        /// <param name="startsWith"></param>
        /// <returns></returns>
        IEnumerable<string> GetSearchPhrases(int companyID, string term, int maxResults);
        
        /// <summary>
        /// Removes and adds all entries of a certain type.
        /// </summary>
        /// <param name="companyID">The current company ID</param>
        /// <param name="items">The current company ID</param>
        /// <exception cref="TooManyTypesException">Method will throw an exception if more than one type is detected in the list of items.</exception>
        void ReIndex(int companyID, IEnumerable<IndexObjectModel> items);

        /// <summary>
        /// Adds the item to the remove queue for removal from the index.
        /// </summary>
        /// <param name="item">The item to remove from the index.</param>
        void RemoveFromIndex(IndexObjectModel item);
        void RemoveFromIndex(IEnumerable<IndexObjectModel> items);

        void UpdateInIndex(IndexObjectModel item);
        void UpdateInIndex(IEnumerable<IndexObjectModel> items);
    }
}
