using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers;
using Lucene.Net.Search;
using Lucene.Net.Store.Azure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Lucene.Net.Documents;
using d360.core.queue;
using d360.core;

namespace d360.extensions.search
{
    public class AzureSearchSource : ISearchSource //SearchSourceBase, 
    {
        Lucene.Net.Util.Version version = Lucene.Net.Util.Version.LUCENE_30;

        Document createDocument(IndexObjectModel item)
        {
            var doc = new Document();
            doc.Add(new Lucene.Net.Documents.Field("Group", item.Group, Lucene.Net.Documents.Field.Store.YES, Lucene.Net.Documents.Field.Index.ANALYZED));
            doc.Add(new Lucene.Net.Documents.Field("Type", item.Type, Lucene.Net.Documents.Field.Store.YES, Lucene.Net.Documents.Field.Index.ANALYZED));
            doc.Add(new Lucene.Net.Documents.Field("Url", item.RelativeUrl, Lucene.Net.Documents.Field.Store.YES, Lucene.Net.Documents.Field.Index.ANALYZED));
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

        void deleteDocument(IndexWriter writer, IndexObjectModel item)
        {
            writer.DeleteDocuments(
                new TermQuery(new Term("Group", item.Group)),
                new TermQuery(new Term("Type", item.Type)),
                new TermQuery(new Term("ID", item.ID.ToString()))
            );
        }

        AzureDirectory getDirectory(int companyID)
        {
            var acctName = constants.AZURE_STORAGE_NAME;
            var keyValue = constants.AZURE_STORAGE_KEY;
            return new AzureDirectory(
                new CloudStorageAccount(new StorageCredentials(acctName, keyValue), true), 
                string.Format("D3S-SearchIndex-{0}", companyID)
            );
        }

        IndexWriter getWriter(int companyID)
        {
            return new IndexWriter(
                    getDirectory(companyID),
                    new StandardAnalyzer(version),
                    IndexWriter.MaxFieldLength.UNLIMITED
                );
        }

        public void AddToIndex(AddToIndexModel item)
        {
            IndexWriter writer = null;
            Document doc = null;

            try
            {
                writer = getWriter(item.CompanyID);
                doc = createDocument(item);

                writer.AddDocument(doc);
                writer.Commit();
            }
            catch (Exception ex)
            {
                //throw new AddToIndexException(ex);
            }
            finally
            {
                doc = null;
                if (writer != null) writer.Dispose();
            }
        }

        public void AddToIndex(List<AddToIndexModel> items)
        {
            IndexWriter writer = null;
            Document doc = null;

            try
            {
                writer = getWriter(items[0].CompanyID);
                items.ForEach(item => {
                    doc = createDocument(item);
                    writer.AddDocument(doc);                
                });
                writer.Commit();
            }
            catch (Exception ex)
            {
                //throw new AddToIndexException(ex);
            }
            finally
            {
                doc = null;
                if (writer != null) writer.Dispose();
            }
        }

        public void ClearIndex(int companyID)
        {
            IndexWriter writer = null;

            try
            {
                writer = getWriter(companyID);
                writer.DeleteAll();
                writer.Commit();
            }
            catch { }
            finally
            {
                if (writer != null)
                    writer.Dispose();
            }
        }

        public void ClearIndex(int companyID, string group)
        {
            IndexWriter writer = null;

            try
            {
                writer = getWriter(companyID);
                writer.DeleteDocuments(new TermQuery(new Term("Group", group)));
                writer.Commit();
            }
            catch { }
            finally
            {
                if (writer != null)
                    writer.Dispose();
            }
        }

        private static readonly String[] kEnglishStopWords = {
            "a", "an", "and", "are", "as", "at", "be", "but", "by",
            "for", "i", "if", "in", "into", "is",
            "no", "not", "of", "on", "or", "s", "such",
            "t", "that", "the", "their", "then", "there", "these",
            "they", "this", "to", "was", "will", "with"
        };

        
        public IEnumerable<string> GetSearchPhrases(int companyID, string term, int maxResults)
        {
            AzureDirectory directory = null;
            IndexSearcher searcher = null;
            StandardAnalyzer analyzer = null;

            try
            {
                directory = getDirectory(companyID);

                searcher = new IndexSearcher(directory, true);
                analyzer = new StandardAnalyzer(Lucene.Net.Util.Version.LUCENE_30, Lucene.Net.Analysis.StopFilter.MakeStopSet(kEnglishStopWords));
                //Singe Field Search
                var queryParser = new QueryParser(Lucene.Net.Util.Version.LUCENE_30,
                                        "Name",
                                        analyzer);
                string strQuery = string.Format("{0}", term);
                var query = queryParser.Parse(QueryParser.Escape(strQuery));

                Sort sort = new Sort(new SortField("Score", SortField.SCORE));

                TopDocs docs = searcher.Search(query, null, maxResults, sort);
                string[] suggestions = docs.ScoreDocs.Select(doc =>
                    searcher.Doc(doc.Doc).Get("Name")).Distinct().ToArray();



                return suggestions;
            }
            catch (Exception ex)
            {
                throw new SearchResultsException(ex);
            }
            finally
            {
                if (directory != null)
                    directory.Dispose();
            }

            return null;
        }


        public IndexResults GetSearchResults(int companyID, int resourceID, string phrase, int size, int from, string group = "")
        {
            IndexResults result = new IndexResults();
            phrase = phrase.Replace("--", "");

            AzureDirectory directory = null;            
            IndexSearcher searcher = null;
            StandardAnalyzer analyzer = null;
            string[] fields = null;
            MultiFieldQueryParser parser = null;
            BooleanQuery query = null;
            TopDocs search = null;

            try
            {
                directory = getDirectory(companyID);
                //Trace.TraceInformation("GetSearchResults : Executing Search");

                searcher = new IndexSearcher(directory, true);
                analyzer = new StandardAnalyzer(Lucene.Net.Util.Version.LUCENE_29);

                fields = IndexReader.Open(directory, true).GetFieldNames(IndexReader.FieldOption.ALL).ToArray();

                query = new BooleanQuery();

                parser = new MultiFieldQueryParser(version, fields, analyzer);
                query.Add(parser.Parse(phrase), Occur.MUST);

                search = searcher.Search(query, null, searcher.MaxDoc);

                var maxScore = search.MaxScore;

                if (maxScore == System.Single.NaN) maxScore = 1;

                result.Results = search.ScoreDocs.Select(x =>
                {
                    var doc = searcher.Doc(x.Doc);
                    return new IndexResult
                    {
                        Group = doc.Get("Group") + "",
                        Name = doc.Get("Name") + "",
                        Type = doc.Get("Type"),
                        ID = doc.Get("ID"),
                        Description = doc.Get("Description") + "",
                        Url = doc.Get("Url") + "",
                        NormalizedScore = x.Score / maxScore,
                        Score = x.Score
                    };
                }
                ).ToList();


                //Trace.TraceInformation("GetSearchResults : Return Results");
                
                return result;
            }
            catch (Exception ex)
            {
                throw new SearchResultsException(ex);
            }
            finally
            {
                if (directory != null)
                    directory.Dispose();
            }
        }

        public void ReIndex(int companyID, List<AddToIndexModel> items)
        {
            IndexWriter writer = null;
            Document doc = null;

            try
            {
                ClearIndex(companyID);

                writer = getWriter(companyID);

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
                        }
                    });
                }

                writer.Commit();
            }
            catch(Exception ex)
            {
                throw new AddToIndexException(ex);
            }
            finally
            {
                if (writer != null)
                    writer.Dispose();
            }
        }

        public void RemoveFromIndex(RemoveFromIndexModel item)
        {
            AzureDirectory directory = null;
            StandardAnalyzer analyzer = null;
            IndexWriter writer = null;

            try
            {
                directory = getDirectory(item.CompanyID);
                analyzer = new StandardAnalyzer(version);
                writer = new IndexWriter(directory, analyzer, IndexWriter.MaxFieldLength.UNLIMITED);

                deleteDocument(writer, item);

                writer.Commit();
            }
            catch// (Exception ex)
            {
                throw;
            }
            finally
            {
                if (writer != null) writer.Dispose();
                if (analyzer != null) analyzer.Dispose();
                if (directory != null) directory.Dispose();
            }
        }

        public void RemoveFromIndex(List<RemoveFromIndexModel> items)
        {
            AzureDirectory directory = null;
            StandardAnalyzer analyzer = null;
            IndexWriter writer = null;

            try
            {
                directory = getDirectory(items[0].CompanyID);
                analyzer = new StandardAnalyzer(version);
                writer = new IndexWriter(directory, analyzer, IndexWriter.MaxFieldLength.UNLIMITED);

                items.ForEach(item => {
                    deleteDocument(writer, item);
                });

                writer.Commit();
            }
            catch { }
            finally
            {
                if (writer != null) writer.Dispose();
                if (analyzer != null) analyzer.Dispose();
                if (directory != null) directory.Dispose();
            }
        }

        public void UpdateInIndex(UpdateInIndexModel item)
        {
            AzureDirectory directory = null;
            StandardAnalyzer analyzer = null;
            IndexWriter writer = null;
            Document doc = null;

            try
            {
                directory = getDirectory(item.CompanyID);
                analyzer = new StandardAnalyzer(version);
                writer = new IndexWriter(directory, analyzer, IndexWriter.MaxFieldLength.UNLIMITED);

                deleteDocument(writer, item);
                doc = createDocument(item);
                writer.AddDocument(doc);
                writer.Commit();
            }
            catch { }
            finally
            {
                doc = null;
                if (writer != null) writer.Dispose();
                if (analyzer != null) analyzer.Dispose();
                if (directory != null) directory.Dispose();
            }
        }

        public void UpdateInIndex(List<UpdateInIndexModel> items)
        {
            AzureDirectory directory = null;
            StandardAnalyzer analyzer = null;
            IndexWriter writer = null;
            Document doc = null;

            try
            {
                directory = getDirectory(items[0].CompanyID);
                analyzer = new StandardAnalyzer(version);
                writer = new IndexWriter(directory, analyzer, IndexWriter.MaxFieldLength.UNLIMITED);

                items.ForEach(item => {
                    deleteDocument(writer, item);
                    doc = createDocument(item);
                    writer.AddDocument(doc);                
                });

                writer.Commit();
            }
            catch { }
            finally
            {
                doc = null;
                if (writer != null) writer.Dispose();
                if (analyzer != null) analyzer.Dispose();
                if (directory != null) directory.Dispose();
            }
        }
    }
}
