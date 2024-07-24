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
using System.IO;
using System.Collections.Generic;
using Lucene.Net.Search.Spell;

namespace MarketingCodingAssignment.Services
{
    public class SearchEngine
    {
        // The code below is roughly based on sample code from: https://lucenenet.apache.org/

        private const LuceneVersion AppLuceneVersion = LuceneVersion.LUCENE_48;
        private readonly string _indexPath;
        private SpellChecker _spellChecker;

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

            // Initialize the spell checker
            InitializeSpellChecker();
        }
        // Method to initialize the SpellChecker
        private void InitializeSpellChecker()
        {
            try
            {
                var spellCheckerDir = FSDirectory.Open(Path.Combine(_indexPath, "spellchecker"));
                _spellChecker = new SpellChecker(spellCheckerDir);

                // Populate the spell checker dictionary with terms from the index
                var indexDir = FSDirectory.Open(_indexPath);
                using var reader = DirectoryReader.Open(indexDir);
                var dictionary = new LuceneDictionary(reader, "CombinedText");
                _spellChecker.IndexDictionary(dictionary, new IndexWriterConfig(AppLuceneVersion, new StandardAnalyzer(AppLuceneVersion)), true);
            }
            catch (Exception ex)
            {
                // Log the exception (this is just an example, use your logging mechanism)
                Console.WriteLine($"An error occurred while initializing the SpellChecker: {ex.Message}");
                // Optionally, you can rethrow the exception if you want to handle it further up the call stack
                // throw;
            }
        }

        // Method to read films from CSV
        public List<FilmCsvRecord> ReadFilmsFromCsv()
        {
            try
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

                if (records == null || records.Count == 0)
                {
                    throw new InvalidOperationException("The CSV file is empty or could not be read.");
                }

                return records;
            }
            catch (FileNotFoundException fnfEx)
            {
                // Log the file not found exception
                Console.WriteLine($"File not found: {fnfEx.Message}");
                // Optionally, you can rethrow the exception if you want to handle it further up the call stack
                // throw;
            }
            catch (Exception ex)
            {
                // Log the exception (this is just an example, use your logging mechanism)
                Console.WriteLine($"An error occurred while reading films from CSV: {ex.Message}");
                // Optionally, you can rethrow the exception if you want to handle it further up the call stack
                // throw;
            }

            return new List<FilmCsvRecord>(); // Return an empty list if an exception occurs
        }


        // Read the data from the csv and feed it into the lucene index
        public void PopulateIndexFromCsv()
        {
            try
            {
                // Get the list of films from the csv file
                var csvFilms = ReadFilmsFromCsv();

                if (csvFilms == null || csvFilms.Count == 0)
                {
                    throw new InvalidOperationException("The CSV file is empty or could not be read.");
                }

                // Convert to Lucene format
                List<FilmLuceneRecord> luceneFilms = csvFilms.Select(x => new FilmLuceneRecord
                {
                    Id = x?.Id ?? string.Empty,
                    Title = x?.Title ?? string.Empty,
                    Overview = x?.Overview ?? string.Empty,
                    Runtime = int.TryParse(x?.Runtime, out int parsedRuntime) ? parsedRuntime : 0,
                    Tagline = x?.Tagline ?? string.Empty,
                    Revenue = long.TryParse(x?.Revenue, out long parsedRevenue) ? parsedRevenue : 0,
                    VoteAverage = double.TryParse(x?.VoteAverage, out double parsedVoteAverage) ? parsedVoteAverage : 0,
                    ReleaseDate = DateTime.TryParse(x?.ReleaseDate, out DateTime parsedReleaseDate) ? parsedReleaseDate : (DateTime?)null  // Added release date
                }).ToList();

                // Write the records to the lucene index
                PopulateIndex(luceneFilms);
            }
            catch (Exception ex)
            {              
                Console.WriteLine($"An error occurred while populating the index from CSV: {ex.Message}");
              
            }
        }


        public void PopulateIndex(List<FilmLuceneRecord> films)
        {
            try
            {
                if (films == null || films.Count == 0)
                {
                    throw new ArgumentNullException(nameof(films), "The film list cannot be null or empty.");
                }

                using FSDirectory dir = FSDirectory.Open(_indexPath);

                // Create an analyzer to process the text
                Analyzer analyzer = new CustomAnalyzer();

                // Create an index writer
                IndexWriterConfig indexConfig = new IndexWriterConfig(AppLuceneVersion, analyzer);
                using IndexWriter writer = new IndexWriter(dir, indexConfig);

                // Add to the index
                foreach (var film in films)
                {
                    if (film == null)
                    {
                        continue; // Skip null entries in the list
                    }

                    Document doc = new()
            {
                new StringField("Id", film.Id ?? string.Empty, Field.Store.YES),
                new TextField("Title", film.Title ?? string.Empty, Field.Store.YES),
                new TextField("Overview", film.Overview ?? string.Empty, Field.Store.YES),
                new Int32Field("Runtime", film.Runtime, Field.Store.YES),
                new TextField("Tagline", film.Tagline ?? string.Empty, Field.Store.YES),
                new Int64Field("Revenue", film.Revenue ?? 0, Field.Store.YES),
                new DoubleField("VoteAverage", film.VoteAverage ?? 0.0, Field.Store.YES),
                new TextField("CombinedText", (film.Title + " " + film.Tagline + " " + film.Overview) ?? string.Empty, Field.Store.NO)
            };
                    if (film.ReleaseDate.HasValue)
                    {
                        doc.Add(new StringField("ReleaseDate", film.ReleaseDate.Value.ToString("yyyyMMdd"), Field.Store.YES));  // Added release date
                    }
                    writer.AddDocument(doc);
                }

                writer.Flush(triggerMerge: false, applyAllDeletes: false);
                writer.Commit();
            }
            catch (Exception ex)
            {              
                Console.WriteLine($"An error occurred during index population: {ex.Message}");             
            }
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
            SearchResultsViewModel searchResults = new();

            try
            {
                if (!IndexExists())
                {
                    PopulateIndexFromCsv();
                }

                using FSDirectory dir = FSDirectory.Open(_indexPath);
                using DirectoryReader reader = DirectoryReader.Open(dir);
                IndexSearcher searcher = new(reader);

                int hitsLimit = 1000;
                TopScoreDocCollector collector = TopScoreDocCollector.Create(hitsLimit, true);

                var query = this.GetLuceneQuery(searchString, durationMinimum, durationMaximum, voteAverageMinimum, releaseDateStart, releaseDateEnd);

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
                        Id = foundDoc.Get("Id")?.ToString(),
                        Title = foundDoc.Get("Title")?.ToString(),
                        Overview = foundDoc.Get("Overview")?.ToString(),
                        Runtime = int.TryParse(foundDoc.Get("Runtime"), out int parsedRuntime) ? parsedRuntime : 0,
                        Tagline = foundDoc.Get("Tagline")?.ToString(),
                        Revenue = long.TryParse(foundDoc.Get("Revenue"), out long parsedRevenue) ? parsedRevenue : 0,
                        VoteAverage = double.TryParse(foundDoc.Get("VoteAverage"), out double parsedVoteAverage) ? parsedVoteAverage : 0.0,
                        ReleaseDate = DateTime.TryParseExact(foundDoc.Get("ReleaseDate"), "yyyyMMdd", null, DateTimeStyles.None, out DateTime parsedReleaseDate) ? parsedReleaseDate : (DateTime?)null,
                        Score = hit.Score
                    };
                    films.Add(film);
                }

                searchResults = new SearchResultsViewModel
                {
                    RecordsCount = hits.TotalHits,
                    Films = films.ToList()
                };

                // Spell checking if no results found
                if (searchResults.RecordsCount == 0)
                {
                    var suggestions = GetSpellCheckSuggestions(searchString);
                    searchResults.Suggestions = suggestions;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred during search: {ex.Message}");
               
            }

            return searchResults;
        }

        private Query GetLuceneQuery(string searchString, int? durationMinimum, int? durationMaximum, double? voteAverageMinimum, DateTime? releaseDateStart, DateTime? releaseDateEnd)
        {
            BooleanQuery bq = new BooleanQuery();

            try
            {
                if (!string.IsNullOrWhiteSpace(searchString))
                {
                    var analyzer = new CustomAnalyzer(); // Use CustomAnalyzer here
                    using var tokenStream = analyzer.GetTokenStream("CombinedText", new System.IO.StringReader(searchString));
                    var termAttr = tokenStream.AddAttribute<Lucene.Net.Analysis.TokenAttributes.ICharTermAttribute>();
                    tokenStream.Reset();

                    while (tokenStream.IncrementToken())
                    {
                        var term = termAttr.ToString();
                        if (!string.IsNullOrWhiteSpace(term))
                        {
                            var termQuery = new TermQuery(new Term("CombinedText", term));
                            bq.Add(termQuery, Occur.MUST);
                        }
                    }

                    tokenStream.End();
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
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred while creating the Lucene query: {ex.Message}");
                
            }

            return bq;
        }


        public List<string> GetAutocompleteSuggestions(string prefix, int maxSuggestions)
        {
            List<string> suggestions = new();

            // Null check for the prefix
            if (string.IsNullOrWhiteSpace(prefix))
            {
                return suggestions;
            }

            try
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
                    // Null or empty word check
                    if (string.IsNullOrWhiteSpace(word))
                    {
                        continue;
                    }

                    var termQuery = new PrefixQuery(new Term("CombinedText", word));
                    booleanQuery.Add(termQuery, Occur.MUST);
                }

                // Create a collector to gather the top scoring documents
                var collector = TopScoreDocCollector.Create(1000, true); // Increase the initial search limit
                searcher.Search(booleanQuery, collector);

                TopDocs topDocs = collector.GetTopDocs(0, 1000); // Fetch more results initially
                ScoreDoc[] scoreDocs = topDocs.ScoreDocs;

                foreach (var scoreDoc in scoreDocs)
                {
                    Document doc = searcher.Doc(scoreDoc.Doc);
                    string title = doc.Get("Title");
                    if (title != null && title.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) && !suggestions.Contains(title))
                    {
                        suggestions.Add(title);
                        if (suggestions.Count >= maxSuggestions)
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred while getting autocomplete suggestions: {ex.Message}");
            }

            return suggestions;
        }

        // Method to get spell check suggestions
        public List<string> GetSpellCheckSuggestions(string searchString, int maxSuggestions = 5)
        {
            List<string> suggestions = new();

            // Null check for the searchString
            if (string.IsNullOrWhiteSpace(searchString))
            {
                return suggestions;
            }

            try
            {
                var words = searchString.ToLowerInvariant().Split(' ');

                foreach (var word in words)
                {
                    // Null or empty word check
                    if (string.IsNullOrWhiteSpace(word))
                    {
                        continue;
                    }

                    string[] suggestWords = _spellChecker.SuggestSimilar(word, maxSuggestions);
                    if (suggestWords != null && suggestWords.Length > 0)
                    {
                        suggestions.AddRange(suggestWords);
                    }
                    else
                    {
                        suggestions.Add(word); // Add the original word if no suggestions are found
                    }
                }
            }
            catch (Exception ex)
            {               
                Console.WriteLine($"An error occurred while getting spell check suggestions: {ex.Message}");
               
            }

            return suggestions.Distinct().ToList();
        }







    }
}

