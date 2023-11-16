using d360.core.queue;
using d360.core.search;
using System;
using System.Collections.Generic;

namespace d360.extensions
{
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

        /// <summary>
        /// Removes search documents by UID
        /// </summary>
        /// <param name="companyID">The current company ID</param>
        /// <param name="assetUids">IEnumerable of UIDs to remove. UIDs can be Asset UID or Semantic UID.</param>
        void RemoveByUids(int companyID, IEnumerable<Guid> assetUids);

        IndexResults GetSearchResultsWithAggregation(int companyID, QueryRequest queryRequest, QueryLimitation queryLimit);

        IEnumerable<TypeaheadResult> GetTypeaheadResults(int companyID, string phrase, QueryLimitation queryLimit, int size = 10, string type = "");
        
        IndexResults GetSearchResults(int companyID, string phrase, int size, int from, QueryLimitation queryLimit, string group = "");

        IndexResults GetStatusSearch(int companyID, bool withTypes = false);

        List<IndexableCount> GetStatusList(int companyID);

        /// <summary>
        /// Removes and adds all entries of a certain type.
        /// </summary>
        /// <param name="companyID">The current company ID</param>
        /// <param name="items">The current company ID</param>
        void ReIndex(int companyID, IEnumerable<IndexObjectModel> items);

        /// <summary>
        /// Adds the item to the remove queue for removal from the index.
        /// </summary>
        /// <param name="item">The item to remove from the index.</param>
        void RemoveFromIndex(IndexObjectModel item);
        
        void RemoveFromIndex(IEnumerable<IndexObjectModel> items);

        void UpdateInIndex(IndexObjectModel item, bool withUpsert = false);
        
        void UpdateInIndex(IEnumerable<IndexObjectModel> items, bool withUpsert = false);
    }
}
