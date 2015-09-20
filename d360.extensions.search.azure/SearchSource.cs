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

namespace d360.extensions.search.azure
{
    public class SearchSource : ISearchSource //SearchSourceBase, 
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
            var acctName = ConfigurationManager.AppSettings["AzureStorageAccountName"];
            //var keyName = ConfigurationManager.AppSettings["AzureStorageKeyName"];
            var keyValue = ConfigurationManager.AppSettings["AzureStorageKeyValue"];
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
                writer.Dispose();
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
                writer.Dispose();
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
                writer.Dispose();
            }
        }

        public List<IndexResult> GetSearchResults(int companyID, int resourceID, string phrase)
        {
            AzureDirectory directory = null;
            List<IndexResult> results = null;
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
                results = search.ScoreDocs.Select(x =>
                {
                    var doc = searcher.Doc(x.Doc);
                    return new IndexResult
                    {
                        Group = doc.Get("Group") + "",
                        Name = doc.Get("Name") + "",
                        Type = doc.Get("Type"),
                        Description = doc.Get("Description") + "",
                        Url = doc.Get("Url") + "",
                        Score = x.Score
                    };
                }
                ).ToList();

                //Trace.TraceInformation("GetSearchResults : Return Results");

                return results;
            }
            catch (Exception ex)
            {
                throw new SearchResultsException(ex);
            }
            finally
            {
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

            }
            finally
            {
                writer.Dispose();
                analyzer.Dispose();
                directory.Dispose();
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
                writer.Dispose();
                analyzer.Dispose();
                directory.Dispose();
            }
        }
    }
}
