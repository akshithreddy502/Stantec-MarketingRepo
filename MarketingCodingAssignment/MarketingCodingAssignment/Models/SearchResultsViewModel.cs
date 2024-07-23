namespace MarketingCodingAssignment.Models
{
    public class SearchResultsViewModel
    {
        public int RecordsCount
        {
            get; set;
        }

        public IEnumerable<FilmLuceneRecord>? Films
        { 
            get; set; 
        }
        public List<string> Suggestions { get; set; }
        public SearchResultsViewModel()
        {
            Films = new List<FilmLuceneRecord>();
            Suggestions = new List<string>();
        }

    }
}
