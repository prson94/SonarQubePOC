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

    public class TypeaheadResult
    {
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public string Type { get; set; }
        public string Url { get; set; }

        public string Desc { get; set; }
    }

    public class IndexResult
    {
        public string Name { get; set; }
        public string Group { get; set; }
        public string Type { get; set; }
        public string ID { get; set; }
        public string Description { get; set; }
        public string AbsoluteUrl { get; set; }
        public string Url { get; set; }
        public float Score { get; set; }
        /// <summary>
        /// score ranging between 1 and 0 adjusted based on max value.
        /// </summary>
        public float NormalizedScore { get; set; }
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
        void AddToIndex(AddToIndexModel item);

        void AddToIndex(IEnumerable<AddToIndexModel> items);

        /// <summary>
        /// Clears out all entries from a company's index.
        /// </summary>
        /// <param name="companyID">The current company ID</param>
        void ClearIndex(int companyID);

        /// <summary>
        /// Clears out all group entries from a company's index.
        /// </summary>
        /// <param name="companyID">The current company ID</param>
        /// <param name="group">The current group</param>
        void ClearIndex(int companyID, string group);

        /// <summary>
        /// Gets search results for the specified phrase.
        /// </summary>
        /// <param name="companyID">The current company ID</param>
        /// <param name="resourceID">The current user ID</param>
        /// <param name="phrase">The search phrase to get results for</param>
        /// <param name="size">Page Size</param>
        /// <param name="from">Start at result</param>
        /// <returns>A list of search results.</returns>
        /// <exception cref="SearchResultsException"></exception>
        IndexResults GetSearchResultsWithCategory(int companyID, int resourceID, string phrase, int size, int from, List<IndexTypeList> categories, string group = "", string type = "", string advancedFilterJSON = "");

        IEnumerable<TypeaheadResult> GetTypeaheadResults(int companyID, int resourceID, string phrase, int size = 10, string type = "");
        
        IndexResults GetSearchResults(int companyID, int resourceID, string phrase, int size, int from, string group = "");


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
        void ReIndex(int companyID, IEnumerable<AddToIndexModel> items);

        /// <summary>
        /// Adds the item to the remove queue for removal from the index.
        /// </summary>
        /// <param name="item">The item to remove from the index.</param>
        void RemoveFromIndex(RemoveFromIndexModel item);
        void RemoveFromIndex(IEnumerable<RemoveFromIndexModel> items);

        void UpdateInIndex(UpdateInIndexModel item);
        void UpdateInIndex(IEnumerable<UpdateInIndexModel> items);
    }
}
