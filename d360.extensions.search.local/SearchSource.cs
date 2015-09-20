using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using d360.core;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers;
using Lucene.Net.Search;
using Lucene.Net.Store;
using System.Diagnostics;

namespace d360.extensions.search.local
{
    public class SearchSource: SearchSourceBase, ISearchSource
    {
        string ConfigurationDirectory { get { return ConfigurationManager.AppSettings["d360.extensions.search.local.IndexDirectory"]; } }

        MMapDirectory getDirectory(int companyID)
        {
            Trace.TraceInformation("Getting index directory path from configuration.");
            var pathString = string.Format(ConfigurationDirectory, companyID);
            Trace.TraceInformation("Getting/Creating index directory {0}.", pathString);
            var path = new DirectoryInfo(pathString);
            if (!path.Exists) 
            {
                Trace.TraceInformation("Creating index path {0}.", pathString);
                path.Create(); 
                path.Refresh(); 
            }
            //var lockFactory = new SimpleFSLockFactory();
            return new MMapDirectory(path); //NIOFSDirectory(path, lockFactory);
        }

        public void AddToIndex(AddToIndexItem item)
        {
            MMapDirectory directory = null;

            try
            {
                directory = getDirectory(item.CompanyID);
                AddToIndex(directory, item);
            }
            catch (Exception ex)
            {
                Trace.TraceError("ClearIndex: Error occurred: {0}, {1}", ex.Message, ex.InnerException != null ? ex.InnerException.Message : "");
                throw;
            }
            finally 
            {
                directory.Dispose();
            }
        }

        public void ClearIndex(int companyID)
        {
            MMapDirectory directory = null;

            try
            {
                directory = getDirectory(companyID);
                Trace.TraceInformation("ClearIndex: Clearing index.");
                ClearIndex(directory, companyID);
            }
            catch(Exception ex)
            {
                Trace.TraceError("ClearIndex: Error occurred: {0}, {1}", ex.Message, ex.InnerException != null ? ex.InnerException.Message : "");
                throw;
            }
            finally
            {
                directory.Dispose();
            }
        }

        public List<IndexResult> GetSearchResults(int companyID, int resourceID, string phrase)
        {
            MMapDirectory directory = null;

            try
            {
                directory = getDirectory(companyID);
                Trace.TraceInformation("ClearIndex: Getting search results.");
                return GetSearchResults(directory, companyID, resourceID, phrase);
            }
            catch (Exception ex)
            {
                Trace.TraceError("GetSearchResults: Error occurred: {0}, {1}", ex.Message, ex.InnerException != null ? ex.InnerException.Message : "");
                throw;
            }
            finally
            {
                directory.Dispose();
            }
        }

        public void ReIndex(int companyID, string type, List<AddToIndexItem> items)
        {
            MMapDirectory directory = null;

            try
            {
                directory = getDirectory(companyID);
                Trace.TraceInformation("ReIndex: Re-indexing data.");
                ReIndex(directory, companyID, type, items);
            }
            catch (Exception ex)
            {
                Trace.TraceError("ReIndex: Error occurred: {0}, {1}", ex.Message, ex.InnerException != null ? ex.InnerException.Message : "");
                throw;
            }
            finally
            {
                directory.Dispose();
            }
        }

        public void RemoveFromIndex(RemoveFromIndexItem item)
        {
            MMapDirectory directory = null;

            try
            {
                directory = getDirectory(item.CompanyID);
                RemoveFromIndex(directory, item);
            }
            catch (Exception ex)
            {
                Trace.TraceError("RemoveFromIndex: Error occurred: {0}, {1}", ex.Message, ex.InnerException != null ? ex.InnerException.Message : "");
                throw;
            }
            finally
            {
                directory.Dispose();
            }
        }
    }
}
