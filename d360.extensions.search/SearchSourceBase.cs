using System;
using System.Collections.Generic;
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

namespace d360.extensions.search
{
    public class SearchSourceBase
    {
        Lucene.Net.Util.Version version = Lucene.Net.Util.Version.LUCENE_30;

        Document createDocument(AddToIndexItem item)
        {
            var doc = new Document();
            doc.Add(new Lucene.Net.Documents.Field("Type", item.Type, Lucene.Net.Documents.Field.Store.YES, Lucene.Net.Documents.Field.Index.ANALYZED));
            doc.Add(new Lucene.Net.Documents.Field("Url", item.Url, Lucene.Net.Documents.Field.Store.YES, Lucene.Net.Documents.Field.Index.ANALYZED));
            doc.Add(new Lucene.Net.Documents.Field("ID", item.ID.ToString(), Lucene.Net.Documents.Field.Store.YES, Lucene.Net.Documents.Field.Index.ANALYZED));
            if (item.Fields != null)
            {
                foreach (var key in item.Fields.Keys)
                {
                    if (item.Fields[key] != null)
                        doc.Add(new Lucene.Net.Documents.Field(key, item.Fields[key].ToString(), Lucene.Net.Documents.Field.Store.YES, Lucene.Net.Documents.Field.Index.ANALYZED));
                }
            }
            return doc;
        }

        public void AddToIndex(Directory directory, AddToIndexItem item)
        {
            StandardAnalyzer analyzer = null;
            IndexWriter writer = null;
            Document doc = null;

            try
            {
                analyzer = new StandardAnalyzer(version);
                writer = new IndexWriter(directory, analyzer, IndexWriter.MaxFieldLength.UNLIMITED);

                doc = createDocument(item);

                try
                {
                    writer.UpdateDocument(new Term("ID", item.ID.ToString()), doc);
                    writer.Commit();
                }
                catch (OutOfMemoryException)
                {
                    writer.Dispose();
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.Message + "; " + ((ex.InnerException != null) ? ex.InnerException.Message : ""));
                throw new AddToIndexException(ex);
            }
            finally
            {
                doc = null;
                writer.Dispose();
                analyzer.Dispose();
            }
        }

        public void ClearIndex(Directory directory, int companyID)
        {
            StandardAnalyzer analyzer = null;
            IndexWriter writer = null;

            try
            {
                analyzer = new StandardAnalyzer(version);
                writer = new IndexWriter(directory, analyzer, IndexWriter.MaxFieldLength.UNLIMITED);

                writer.DeleteAll();
                writer.Commit();
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.Message + "; " + ((ex.InnerException != null) ? ex.InnerException.Message : ""));
            }
            finally 
            {
                writer.Dispose();
                analyzer.Dispose();
            }
        }

        public List<IndexResult> GetSearchResults(Directory directory, int companyID, int resourceID, string phrase)
        {
            List<IndexResult> results = null;
            IndexSearcher searcher = null;
            StandardAnalyzer analyzer = null;
            string[] fields = null;
            MultiFieldQueryParser parser = null;
            BooleanQuery query = null;
            TopDocs search = null;

            try
            {
                Trace.TraceInformation("GetSearchResults : Executing Search");
                
                searcher = new IndexSearcher(directory, true);
                analyzer = new StandardAnalyzer(Lucene.Net.Util.Version.LUCENE_29);

                fields = IndexReader.Open(directory, true).GetFieldNames(IndexReader.FieldOption.ALL).ToArray();

                query = new BooleanQuery();

                parser = new MultiFieldQueryParser(version, fields, analyzer);
                query.Add(parser.Parse(phrase), Occur.MUST);

                search = searcher.Search(query, null, searcher.MaxDoc);
                results = search.ScoreDocs.Select(x =>
                {
                    var doc = searcher.Doc(x.Doc);
                    return new IndexResult
                    {
                        Name = doc.Get("Name") + "",
                        Type = doc.Get("Type"),
                        Description = doc.Get("Description") + "",
                        Url = doc.Get("Url") + "",
                        Score = x.Score
                    };
                }
                ).ToList();

                Trace.TraceInformation("GetSearchResults : Return Results");

                return results;
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.Message + "; " + ((ex.InnerException != null) ? ex.InnerException.Message : ""));
                throw new SearchResultsException(ex);
            }
            finally
            {
                #region Destroy objects

                search = null;
                query = null;
                parser = null;
                fields = null;
                analyzer.Dispose();
                searcher.Dispose();

                #endregion
            }
        }

        public void ReIndex(Directory directory, int companyID, string type, List<AddToIndexItem> items)
        {
            StandardAnalyzer analyzer = null;
            IndexWriter writer = null;
            Document doc = null;
            string itemsNotindexed = "";

            try
            {
                analyzer = new StandardAnalyzer(version);
                writer = new IndexWriter(directory, analyzer, IndexWriter.MaxFieldLength.UNLIMITED);
                var query = new TermQuery(new Term("Type", type));
                writer.DeleteDocuments(query);

                if (items != null)
                {

                    items.ForEach(i =>
                    {
                        try
                        {
                            doc = createDocument(i);
                            writer.AddDocument(doc);
                        }
                        catch (Exception ex)
                        {
                            itemsNotindexed += string.Format("{0}, {1}, {2}; ", i.Type, i.ID, ex.Message);
                        }
                    });
                }

                if (!string.IsNullOrEmpty(itemsNotindexed)) Trace.TraceWarning(itemsNotindexed);

                writer.Commit();
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.Message + "; " + ((ex.InnerException != null) ? ex.InnerException.Message : ""));
                throw new AddToIndexException(ex);
            }
            finally
            {
                writer.Dispose();
                analyzer.Dispose();
            }
        }

        public void RemoveFromIndex(Directory directory, RemoveFromIndexItem item)
        {
            StandardAnalyzer analyzer = null;
            IndexWriter writer = null;

            try
            {
                analyzer = new StandardAnalyzer(version);
                writer = new IndexWriter(directory, analyzer, IndexWriter.MaxFieldLength.UNLIMITED);
                var typeQuery = new TermQuery(new Term("Type", item.Type.ToString()));
                var idQuery = new TermQuery(new Term("ID", item.ID.ToString()));
                writer.DeleteDocuments(typeQuery, idQuery);

                writer.Commit();
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.Message + "; " + ((ex.InnerException != null) ? ex.InnerException.Message : ""));
            }
            finally
            {
                writer.Dispose();
                analyzer.Dispose();
            }
        }
    }
}
