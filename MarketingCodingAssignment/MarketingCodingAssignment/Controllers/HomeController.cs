using MarketingCodingAssignment.Models;
using MarketingCodingAssignment.Services;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace MarketingCodingAssignment.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly SearchEngine _searchEngine;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
            _searchEngine = new SearchEngine();
        }

        public IActionResult Index()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [HttpGet]
        public JsonResult Search(string searchString, int start, int rows, int? durationMinimum, int? durationMaximum, double? voteAverageMinimum, DateTime? releaseDateStart, DateTime? releaseDateEnd)
        {
            //SearchResultsViewModel searchResults = _searchEngine.Search(searchString, start, rows, durationMinimum, durationMaximum, voteAverageMinimum);
            var searchResults = _searchEngine.Search(searchString, start, rows, durationMinimum, durationMaximum, voteAverageMinimum,releaseDateStart,releaseDateEnd);
            return Json(new { searchResults });
           // return Json(new {searchResults});
        }

        public ActionResult ReloadIndex()
        {
            DeleteIndex();
            PopulateIndex();
            return RedirectToAction("Index", "Home");
        }

        // Delete the contents of the lucene index 
        public void DeleteIndex()
        {
            _searchEngine.DeleteIndex();
            return;
        }

        // Read the data from the csv and feed it into the lucene index
        public void PopulateIndex()
        {
            _searchEngine.PopulateIndexFromCsv();
            return;
        }
        [HttpGet]
        public JsonResult Autocomplete(string term, int maxSuggestions = 10)        {
            List<string> suggestions = _searchEngine.GetAutocompleteSuggestions(term, maxSuggestions);
            return Json(suggestions);
        }


    }
}

