using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Queries;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using MarketingCodingAssignment.Models;
using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using Lucene.Net.Analysis.En;
using Directory = System.IO.Directory;

namespace MarketingCodingAssignment.Services
{
    public class SearchEngine
    {
        // The code below is roughly based on sample code from: https://lucenenet.apache.org/

        private const LuceneVersion AppLuceneVersion = LuceneVersion.LUCENE_48;
        private readonly string _indexPath;

        public SearchEngine()
        {
            // Define the index path within the CommonApplicationData folder
            // creates index path if deosnt exist
            string basePath = Directory.GetCurrentDirectory();
            _indexPath = Path.Combine(basePath, "LuceneIndex");

            // Ensure the directory exists
            if (!Directory.Exists(_indexPath))
            {
                Directory.CreateDirectory(_indexPath);
            }
        }

        public List<FilmCsvRecord> ReadFilmsFromCsv()
        {
            List<FilmCsvRecord> records = new();
            string filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "csv", "FilmsInfo.csv");
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("CSV file not found", filePath);
            }
            using (StreamReader reader = new(filePath))
            using (CsvReader csv = new(reader, new CsvConfiguration(CultureInfo.InvariantCulture)))
            {
                records = csv.GetRecords<FilmCsvRecord>().ToList();

            }
            using (StreamReader r = new(filePath))
            {
                string csvFileText = r.ReadToEnd();
            }
            return records;
        }

        // Read the data from the csv and feed it into the lucene index
        public void PopulateIndexFromCsv()
        {
            // Get the list of films from the csv file
            var csvFilms = ReadFilmsFromCsv();

            // Convert to Lucene format
            List<FilmLuceneRecord> luceneFilms = csvFilms.Select(x => new FilmLuceneRecord
            {
                Id = x.Id,
                Title = x.Title,
                Overview = x.Overview,
                Runtime = int.TryParse(x.Runtime, out int parsedRuntime) ? parsedRuntime : 0,
                Tagline = x.Tagline,
                Revenue = long.TryParse(x.Revenue, out long parsedRevenue) ? parsedRevenue : 0,
                VoteAverage = double.TryParse(x.VoteAverage, out double parsedVoteAverage) ? parsedVoteAverage : 0,
                ReleaseDate= DateTime.TryParse(x.ReleaseDate, out DateTime parsedReleaseDate) ? parsedReleaseDate : (DateTime?)null  // Added release date
            }).ToList();

            // Write the records to the lucene index
            PopulateIndex(luceneFilms);

            return;
        }

        public void PopulateIndex(List<FilmLuceneRecord> films)
        {
            using FSDirectory dir = FSDirectory.Open(_indexPath);

            // Create an analyzer to process the text
            Analyzer analyzer = new CustomAnalyzer();

            // Create an index writer
            IndexWriterConfig indexConfig = new IndexWriterConfig(AppLuceneVersion, analyzer);
            using IndexWriter writer = new IndexWriter(dir, indexConfig);

            //Add to the index
            foreach (var film in films)
            {
                Document doc = new()
                {
                    new StringField("Id", film.Id, Field.Store.YES),
                    new TextField("Title", film.Title, Field.Store.YES),
                    new TextField("Overview", film.Overview, Field.Store.YES),
                    new Int32Field("Runtime", film.Runtime, Field.Store.YES),
                    new TextField("Tagline", film.Tagline, Field.Store.YES),
                    new Int64Field("Revenue", film.Revenue ?? 0, Field.Store.YES),
                    new DoubleField("VoteAverage", film.VoteAverage ?? 0.0, Field.Store.YES),
                    new TextField("CombinedText", film.Title + " " + film.Tagline + " " + film.Overview, Field.Store.NO)
                };
                if (film.ReleaseDate.HasValue)
                {
                    doc.Add(new StringField("ReleaseDate", film.ReleaseDate.Value.ToString("yyyyMMdd"), Field.Store.YES));  // Added release date
                }
                writer.AddDocument(doc);
            }

            writer.Flush(triggerMerge: false, applyAllDeletes: false);
            writer.Commit();

           return;
        }

        public void DeleteIndex()
        {
            using FSDirectory dir = FSDirectory.Open(_indexPath);
            StandardAnalyzer analyzer = new(AppLuceneVersion);
            IndexWriterConfig indexConfig = new(AppLuceneVersion, analyzer);
            using IndexWriter writer = new(dir, indexConfig);
            writer.DeleteAll();
            writer.Commit();
            return;
        }
        // Check if the index exists
        private bool IndexExists()
        {
            using FSDirectory dir = FSDirectory.Open(_indexPath);
            string[] files = dir.ListAll();
            return files != null && files.Length > 0;
        }
        public SearchResultsViewModel Search(string searchString, int startPage, int rowsPerPage, int? durationMinimum, int? durationMaximum, double? voteAverageMinimum, DateTime? releaseDateStart, DateTime? releaseDateEnd)
        {
            if (!IndexExists()) {
                PopulateIndexFromCsv();
                    }
            using FSDirectory dir = FSDirectory.Open(_indexPath);
            using DirectoryReader reader = DirectoryReader.Open(dir);
            IndexSearcher searcher = new(reader);

            int hitsLimit = 1000;
            TopScoreDocCollector collector = TopScoreDocCollector.Create(hitsLimit, true);

            var query = this.GetLuceneQuery(searchString, durationMinimum, durationMaximum, voteAverageMinimum, releaseDateStart,  releaseDateEnd);

            searcher.Search(query, collector);

            int startIndex = (startPage) * rowsPerPage;
            TopDocs hits = collector.GetTopDocs(startIndex, rowsPerPage);
            ScoreDoc[] scoreDocs = hits.ScoreDocs;

            List<FilmLuceneRecord> films = new();
            foreach (ScoreDoc? hit in scoreDocs)
            {
                Document foundDoc = searcher.Doc(hit.Doc);
                FilmLuceneRecord film = new()
                {
                    Id = foundDoc.Get("Id").ToString(),
                    Title = foundDoc.Get("Title").ToString(),
                    Overview = foundDoc.Get("Overview").ToString(),
                    Runtime = int.TryParse(foundDoc.Get("Runtime"), out int parsedRuntime) ? parsedRuntime : 0,
                    Tagline = foundDoc.Get("Tagline").ToString(),
                    Revenue = long.TryParse(foundDoc.Get("Revenue"), out long parsedRevenue) ? parsedRevenue : 0,
                    VoteAverage =  double.TryParse(foundDoc.Get("VoteAverage"), out double parsedVoteAverage) ? parsedVoteAverage : 0.0,
                    ReleaseDate = DateTime.TryParseExact(foundDoc.Get("ReleaseDate"), "yyyyMMdd", null, DateTimeStyles.None, out DateTime parsedReleaseDate) ? parsedReleaseDate : (DateTime?)null,  // Added release date
                    Score = hit.Score
                };
                films.Add(film);
            }

            SearchResultsViewModel searchResults = new()
            {
                RecordsCount = hits.TotalHits,
                Films = films.ToList()
            };

            return searchResults;
        }
        private Query GetLuceneQuery(string searchString, int? durationMinimum, int? durationMaximum, double? voteAverageMinimum, DateTime? releaseDateStart, DateTime? releaseDateEnd)
        {
            BooleanQuery bq = new BooleanQuery();

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                var analyzer = new CustomAnalyzer(); // Use CustomAnalyzer here
                var tokenStream = analyzer.GetTokenStream("CombinedText", new System.IO.StringReader(searchString));
                var termAttr = tokenStream.AddAttribute<Lucene.Net.Analysis.TokenAttributes.ICharTermAttribute>();
                tokenStream.Reset();

                while (tokenStream.IncrementToken())
                {
                    var term = termAttr.ToString();
                    var termQuery = new TermQuery(new Term("CombinedText", term));
                    bq.Add(termQuery, Occur.MUST);
                }
                tokenStream.End();
                tokenStream.Dispose();
            }

            if (durationMaximum.HasValue || durationMinimum.HasValue)
            {
                Query rq = NumericRangeQuery.NewInt32Range("Runtime", durationMinimum, durationMaximum, true, true);
                bq.Add(rq, Occur.MUST);
            }
            if (voteAverageMinimum.HasValue)
            {
                Query vaq = NumericRangeQuery.NewDoubleRange("VoteAverage", voteAverageMinimum, 10.0, true, true);
                bq.Add(vaq, Occur.MUST);
            }
            if (releaseDateStart.HasValue || releaseDateEnd.HasValue)
            {
                string start = releaseDateStart.HasValue ? releaseDateStart.Value.ToString("yyyyMMdd") : null;
                string end = releaseDateEnd.HasValue ? releaseDateEnd.Value.ToString("yyyyMMdd") : null;
                Query rdq = TermRangeQuery.NewStringRange("ReleaseDate", start, end, true, true);
                bq.Add(rdq, Occur.MUST);
            }
            if (string.IsNullOrWhiteSpace(searchString) && bq.Clauses.Count == 0)
            {
                // If there's no search string, just return everything.
                return new MatchAllDocsQuery();
            }


            return bq;
        }

        public List<string> GetAutocompleteSuggestions(string prefix, int maxSuggestions)
        {
            using FSDirectory dir = FSDirectory.Open(_indexPath);
            using DirectoryReader reader = DirectoryReader.Open(dir);
            IndexSearcher searcher = new(reader);

            // Split the prefix into separate words
            var words = prefix.ToLowerInvariant().Split(' ');

            // Create a BooleanQuery to combine multiple PrefixQuery terms
            BooleanQuery booleanQuery = new BooleanQuery();
            foreach (var word in words)
            {
                var termQuery = new PrefixQuery(new Term("CombinedText", word));
                booleanQuery.Add(termQuery, Occur.MUST);
            }

            // Create a collector to gather the top scoring documents
            var collector = TopScoreDocCollector.Create(1000, true); // Increase the initial search limit
            searcher.Search(booleanQuery, collector);

            TopDocs topDocs = collector.GetTopDocs(0, 1000); // Fetch more results initially
            ScoreDoc[] scoreDocs = topDocs.ScoreDocs;

            List<string> suggestions = new();
            foreach (var scoreDoc in scoreDocs)
            {
                Document doc = searcher.Doc(scoreDoc.Doc);
                string title = doc.Get("Title");
                if (title.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) && !suggestions.Contains(title))
                {
                    suggestions.Add(title);
                    if (suggestions.Count >= maxSuggestions)
                        break;
                }
            }

            return suggestions;
        }






    }
}

