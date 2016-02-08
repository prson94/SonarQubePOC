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
using System.Data.SqlClient;
using LuceneNetSqlDirectory;
using d360.utils.company;

namespace d360.extensions.search
{
    public class SqlServerSearchSource : ISearchSource
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

        SqlConnection getCompanyConnection(int companyID)
        {
            return CompanyConnectionUtils.GetCompanyConnection(companyID);
        }

        SqlServerDirectory getDirectory(SqlConnection cnn)
        {
            var schema = "[search]";
            try
            {
                return new SqlServerDirectory(cnn, new Options { SchemaName = schema });
            }
            catch
            {
                SqlServerDirectory.ProvisionDatabase(cnn, schemaName: schema, dropExisting: true);
                return new SqlServerDirectory(cnn, new Options { SchemaName = schema });
            }
        }

        public void AddToIndex(AddToIndexModel item)
        {
            SqlServerDirectory dir = null;

            try
            {
                var doc = createDocument(item);

                using (var cnn = getCompanyConnection(item.CompanyID))
                {
                    cnn.Open();
                    dir = getDirectory(cnn);
                    var exists = !IndexReader.IndexExists(dir);
                    var indexWriter = new IndexWriter(dir, new StandardAnalyzer(version), exists, new Lucene.Net.Index.IndexWriter.MaxFieldLength(IndexWriter.DEFAULT_MAX_FIELD_LENGTH));
                    indexWriter.AddDocument(doc);
                    indexWriter.Commit();

                    indexWriter.Dispose();
                    dir.Dispose();
                }
            }
            catch (Exception ex)
            {
                //throw new AddToIndexException(ex);
            }
            finally
            {
                if (dir != null)
                    dir.Dispose();
            }
        }

        public void AddToIndex(List<AddToIndexModel> items)
        {
            SqlServerDirectory dir = null;

            try
            {
                using (var cnn = getCompanyConnection(items[0].CompanyID))
                {
                    cnn.Open();
                    dir = getDirectory(cnn);
                    var exists = !IndexReader.IndexExists(dir);
                    var indexWriter = new IndexWriter(dir, new StandardAnalyzer(version), exists, new Lucene.Net.Index.IndexWriter.MaxFieldLength(IndexWriter.DEFAULT_MAX_FIELD_LENGTH));
                    //writer = getWriter(items[0].CompanyID);
                    items.ForEach(item => {
                        var doc = createDocument(item);
                        indexWriter.AddDocument(doc);
                    });
                    indexWriter.Commit();

                    indexWriter.Dispose();
                    dir.Dispose();
                }
            }
            catch (Exception ex)
            {
                //throw new AddToIndexException(ex);
            }
            finally
            {
                if (dir != null)
                    dir.Dispose();
            }
        }

        public void ClearIndex(int companyID)
        {
            SqlServerDirectory dir = null;
            try
            {
                using (var cnn = getCompanyConnection(companyID))
                {
                    cnn.Open();
                    dir = new SqlServerDirectory(cnn, new Options { SchemaName = "[search]" });
                    var exists = !IndexReader.IndexExists(dir);
                    var indexWriter = new IndexWriter(dir, new StandardAnalyzer(version), exists, new Lucene.Net.Index.IndexWriter.MaxFieldLength(IndexWriter.DEFAULT_MAX_FIELD_LENGTH));
                    indexWriter.DeleteAll();
                    indexWriter.Commit();

                    indexWriter.Dispose();
                    dir.Dispose();
                }
            }
            catch { }
            finally
            {
                if (dir != null)
                    dir.Dispose();
            }
        }

        public void ClearIndex(int companyID, string group)
        {
            SqlServerDirectory dir = null;

            try
            {
                using (var cnn = getCompanyConnection(companyID))
                {
                    cnn.Open();

                    dir = getDirectory(cnn);
                    var exists = !IndexReader.IndexExists(dir);
                    var indexWriter = new IndexWriter(dir, new StandardAnalyzer(version), exists, new Lucene.Net.Index.IndexWriter.MaxFieldLength(IndexWriter.DEFAULT_MAX_FIELD_LENGTH));
                    indexWriter.DeleteDocuments(new TermQuery(new Term("Group", group)));
                    indexWriter.Commit();

                    indexWriter.Dispose();
                    dir.Dispose();
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                if (dir != null)
                    dir.Dispose();
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
            string[] suggestions;
            SqlServerDirectory dir = null;

            try
            {
                using (var cnn = getCompanyConnection(companyID))
                {
                    cnn.Open();
                    dir = getDirectory(cnn);
                    var exists = !IndexReader.IndexExists(dir);

                    var searcher = new IndexSearcher(dir);
                    var analyzer = new StandardAnalyzer(version, Lucene.Net.Analysis.StopFilter.MakeStopSet(kEnglishStopWords));
                    //Singe Field Search
                    var queryParser = new QueryParser(version,
                                            "Name",
                                            analyzer);
                    string strQuery = string.Format("{0}", term);
                    var query = queryParser.Parse(QueryParser.Escape(strQuery));

                    Sort sort = new Sort(new SortField("Score", SortField.SCORE));

                    TopDocs docs = searcher.Search(query, null, maxResults, sort);
                    suggestions = docs.ScoreDocs.Select(doc =>
                        searcher.Doc(doc.Doc).Get("Name")).Distinct().ToArray();

                    analyzer.Dispose();
                    searcher.Dispose();
                    dir.Dispose();
                }

                return suggestions;
            }
            catch (Exception ex)
            {
                throw new SearchResultsException(ex);
            }
            finally
            {
                if (dir != null)
                    dir.Dispose();
            }
        }


        public List<IndexResult> GetSearchResults(int companyID, int resourceID, string phrase)
        {
            phrase = phrase.Replace("--", "");
            List<IndexResult> results = null;
            SqlServerDirectory dir = null;

            try
            {
                using (var cnn = getCompanyConnection(companyID))
                {
                    cnn.Open();
                    dir = getDirectory(cnn);
                    var exists = !IndexReader.IndexExists(dir);

                    var searcher = new IndexSearcher(dir);
                    var analyzer = new StandardAnalyzer(version, Lucene.Net.Analysis.StopFilter.MakeStopSet(kEnglishStopWords));
                    var fields = IndexReader.Open(dir, true).GetFieldNames(IndexReader.FieldOption.ALL).ToArray();
                    var query = new BooleanQuery();

                    var parser = new MultiFieldQueryParser(version, fields, analyzer);
                    query.Add(parser.Parse(phrase), Occur.MUST);

                    var search = searcher.Search(query, null, searcher.MaxDoc);

                    var maxScore = search.MaxScore;

                    if (maxScore == System.Single.NaN) maxScore = 1;

                    results = search.ScoreDocs.Select(x =>
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

                    analyzer.Dispose();
                    searcher.Dispose();
                    dir.Dispose();
                }

                return results;
            }
            catch (Exception ex)
            {
                throw new SearchResultsException(ex);
            }
            finally
            {
                if (dir != null)
                    dir.Dispose();
            }
        }

        public void ReIndex(int companyID, List<AddToIndexModel> items)
        {
            SqlServerDirectory dir = null;

            try
            {
                using (var cnn = getCompanyConnection(companyID))
                {
                    cnn.Open();
                    dir = getDirectory(cnn);
                    var exists = !IndexReader.IndexExists(dir);
                    var indexWriter = new IndexWriter(dir, new StandardAnalyzer(version), exists, new Lucene.Net.Index.IndexWriter.MaxFieldLength(IndexWriter.DEFAULT_MAX_FIELD_LENGTH));

                    if (items != null)
                    {
                        indexWriter.DeleteAll();
                        indexWriter.Commit();

                        items.ForEach(i =>
                        {
                            try
                            {
                                indexWriter.AddDocument(createDocument(i));
                            }
                            catch (Exception ex)
                            {
                            }
                        });

                        indexWriter.Commit();
                    }
                    indexWriter.Dispose();
                    dir.Dispose();
                }
            }
            catch (Exception ex)
            {
                throw new AddToIndexException(ex);
            }
            finally
            {
                if (dir != null)
                    dir.Dispose();
            }
        }

        public void RemoveFromIndex(RemoveFromIndexModel item)
        {
            SqlServerDirectory dir = null;

            try
            {
                using (var cnn = getCompanyConnection(item.CompanyID))
                {
                    cnn.Open();
                    dir = getDirectory(cnn);
                    var exists = !IndexReader.IndexExists(dir);
                    var indexWriter = new IndexWriter(dir, new StandardAnalyzer(version), exists, new Lucene.Net.Index.IndexWriter.MaxFieldLength(IndexWriter.DEFAULT_MAX_FIELD_LENGTH));

                    if (item != null)
                    {
                        deleteDocument(indexWriter, item);
                        indexWriter.Commit();
                    }
                    indexWriter.Dispose();
                    dir.Dispose();
                }
            }
            catch// (Exception ex)
            {
                throw;
            }
            finally
            {
                if (dir != null)
                    dir.Dispose();
            }
        }

        public void RemoveFromIndex(List<RemoveFromIndexModel> items)
        {
            SqlServerDirectory dir = null;

            try
            {
                using (var cnn = getCompanyConnection(items[0].CompanyID))
                {
                    cnn.Open();
                    dir = getDirectory(cnn);
                    var exists = !IndexReader.IndexExists(dir);
                    var indexWriter = new IndexWriter(dir, new StandardAnalyzer(version), exists, new Lucene.Net.Index.IndexWriter.MaxFieldLength(IndexWriter.DEFAULT_MAX_FIELD_LENGTH));

                    if (items != null)
                    {
                        items.ForEach(item => {
                            deleteDocument(indexWriter, item);
                        });
                        indexWriter.Commit();
                    }
                    indexWriter.Dispose();
                    dir.Dispose();
                }
            }
            catch { }
            finally
            {
                if (dir != null)
                    dir.Dispose();
            }
        }

        public void UpdateInIndex(UpdateInIndexModel item)
        {
            SqlServerDirectory dir = null;

            try
            {
                using (var cnn = getCompanyConnection(item.CompanyID))
                {
                    cnn.Open();
                    dir = getDirectory(cnn);
                    var exists = !IndexReader.IndexExists(dir);
                    var indexWriter = new IndexWriter(dir, new StandardAnalyzer(version), exists, new Lucene.Net.Index.IndexWriter.MaxFieldLength(IndexWriter.DEFAULT_MAX_FIELD_LENGTH));

                    if (item != null)
                    {
                        deleteDocument(indexWriter, item);
                        indexWriter.AddDocument(createDocument(item));
                        indexWriter.Commit();
                    }
                    indexWriter.Dispose();
                    dir.Dispose();
                }
            }
            catch { }
            finally
            {
                if (dir != null)
                {
                    dir.Dispose();
                }
            }
        }

        public void UpdateInIndex(List<UpdateInIndexModel> items)
        {
            SqlServerDirectory dir = null;
            IndexWriter indexWriter = null;
            SqlConnection cnn = null;

            try
            {
                cnn = getCompanyConnection(items[0].CompanyID);
                cnn.Open();
                dir = getDirectory(cnn);
                var exists = !IndexReader.IndexExists(dir);
                indexWriter = new IndexWriter(dir, new StandardAnalyzer(version), exists, new Lucene.Net.Index.IndexWriter.MaxFieldLength(IndexWriter.DEFAULT_MAX_FIELD_LENGTH));

                if (items != null)
                {
                    items.ForEach(item =>
                    {
                        deleteDocument(indexWriter, item);
                        indexWriter.AddDocument(createDocument(item));
                    });
                    //indexWriter.Commit();
                }
            }
            catch(Exception ex) { }
            finally
            {
                indexWriter.Dispose();

                cnn.Close();
                cnn.Dispose();
                dir.Dispose();
            }
        }
    }
}
