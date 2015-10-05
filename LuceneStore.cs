﻿using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers;
using Lucene.Net.Search;
using Lucene.Net.Store;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CdrIndexer
{
    class LuceneStore
    {
        private static readonly Lucene.Net.Util.Version Version = Lucene.Net.Util.Version.LUCENE_30;
        private static LuceneStore current;
        private IndexWriter writer;
        private FSDirectory luceneIndexDirectory;
        private Analyzer analyzer;
        private IndexSearcher searcher;

        public static LuceneStore Current
        {
            get
            {
                if (current == null)
                {
                    current = new LuceneStore();
                }
                return current;
            }
        }

        private LuceneStore()
        {
            string indexPath = Path.Combine(
                Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                "LuceneIndex");
            this.luceneIndexDirectory = FSDirectory.Open(indexPath);
            this.analyzer = new StandardAnalyzer(Version);
            this.writer = new IndexWriter(
                luceneIndexDirectory,
                this.analyzer,
                !System.IO.Directory.Exists(indexPath),
                IndexWriter.MaxFieldLength.UNLIMITED);
            this.searcher = new IndexSearcher(this.luceneIndexDirectory);
        }

        public Entry Find(string path)
        {
            var query = new TermQuery(new Term("Path", path.ToLower()));
            TopDocs docs = this.searcher.Search(query, n: 1);
            return ToEntries(docs).FirstOrDefault();
        }

        public void Insert(Entry entry)
        {
            this.writer.AddDocument(entry.ToDocument());
            this.writer.Flush(triggerMerge: false, flushDocStores: true, flushDeletes: true);
        }

        public void Update(Entry entry)
        {
            var finder = new Term("Path", entry.Path);
            this.writer.UpdateDocument(finder, entry.ToDocument());
            this.writer.Flush(triggerMerge: false, flushDocStores: true, flushDeletes: true);
        }

        public void ReopenDirectory()
        {
            this.Close();
            LuceneStore.current = new LuceneStore();
        }

        public List<Entry> Search(string phrase)
        {
            var parser = new QueryParser(Version, "All", this.analyzer);
            Query query = parser.Parse(phrase);
            TopDocs result = this.searcher.Search(query, n: 1000);
            return ToEntries(result);
        }

        private List<Entry> ToEntries(TopDocs docs)
        {
            var entries = new List<Entry>(docs.ScoreDocs.Count());
            int index = 1;
            foreach (var doc in docs.ScoreDocs)
            {
                entries.Add(new Entry(this.searcher.Doc(doc.Doc), doc.Score, index++));
            }
            return entries;
        }

        public void DeleteAll()
        {
            this.writer.DeleteAll();
        }

        public void Close()
        {
            this.analyzer.Close();
            this.searcher.Dispose();
            this.writer.Dispose();
            this.luceneIndexDirectory.Dispose();
        }
    }
}