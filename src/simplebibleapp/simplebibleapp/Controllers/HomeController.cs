using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using simplebibleapp.LinguisticEngine.Services;
using simplebibleapp.Models;
using simplebibleapp.xmlbible;
using simplebibleapp.xmlbible.search;
using simplebibleapp.xmlbiblerepository;
using simplebibleapp.xmldictionary;
using StackExchange.Redis;
using LogLevel = NLog.LogLevel;
using Microsoft.AspNetCore.SignalR;
using simplebibleapp.Hubs;
using Microsoft.Extensions.DependencyInjection;

namespace simplebibleapp.Controllers
{
    public class HomeController : Controller
    {
        private readonly IChapterBuilder _builder;
        private readonly IBookNameRepository _bookNameRepository;
        private readonly IDictionaryRepository _dictionaryRepository;
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly IWordCountBuilder _wordCountBuilder;
        private readonly IVerseSearch _verseSearch;
        private readonly IAgyLinguisticService _linguisticService;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public HomeController(
            IChapterBuilderFactory factory,
            IBookNameRepository bookNameRepository,
            IDictionaryRepository dictionaryRepository,
            IWordCountBuilderFactory wordCountBuilderFactory,
            IVerseSearch verseSearch,
            IAgyLinguisticService linguisticService,
            IServiceScopeFactory serviceScopeFactory)
        {
            _builder = factory.GetBuilder();
            _bookNameRepository = bookNameRepository;
            _dictionaryRepository = dictionaryRepository;
            _wordCountBuilder = wordCountBuilderFactory.GetBuilder();
            _verseSearch = verseSearch;
            _linguisticService = linguisticService;
            _serviceScopeFactory = serviceScopeFactory;
        }

        [HttpGet]
        public IActionResult About()
        {
            ViewData["Message"] = "Just a simple bible app.";

            return View();
        }

        [HttpGet]
        public IActionResult Contact()
        {
            ViewData["Message"] = "Your contact page.";

            return View();
        }

        [HttpGet]
        public IActionResult ContactMe()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [HttpGet]
        public ActionResult Index()
        {
            return Redirect($"{Url.Action("Read", new { bookAbbr = "John", chapter = 1 })}#vs-1");
        }

        [HttpGet]
        public ActionResult Book(string id)
        {
            var model = _bookNameRepository.GetBooks()
                .Where(n => n.SearchAbbr == id)
                .Select(n => new BookLanding(n.BookName, _bookNameRepository.GetSelectableVerses(id).ToArray()))
                .FirstOrDefault();
            return View(model);
        }

        [HttpGet]
        public IActionResult GetBookVerses(string id)
        {
            var verses = _bookNameRepository.GetSelectableVerses(id).ToArray();
            return Json(verses);
        }

        [HttpGet]
        public ActionResult Read(string bookAbbr, int chapter)
        {
            var biblechapter = _builder.GetChapter(bookAbbr, chapter);
            var chapterName = _bookNameRepository.GetBooks().First(b => b.SearchAbbr == bookAbbr).BookName;
            //var wordInfos = _wordCountBuilder.GetWords().Where(w => w.BookAbbr == bookAbbr && w.Chapter == chapter)
            //    .ToArray();
            var vm = new ReadViewModel
            {
                ChapterHeading = $"{chapterName} {chapter}",
                BookAbbr = bookAbbr,
                Chapter = chapter,
                BibleNode = biblechapter.CurrentNode,
                HasPreviousChapter = _builder.HasPrevChapter,
                PreviousChapterBookAbbr = _builder.PrevBookAbbr,
                PreviousChapterNumber = _builder.PrevChapterNumber,
                HasNextChapter = _builder.HasNextChapter,
                NextChapterBookAbbr = _builder.NextBookAbbr,
                NextChapterNumber = _builder.NextChapterNumber,
                WordInfos = new WordInfo[0],
                SelectableVerses = _bookNameRepository.GetSelectableVerses(bookAbbr).ToArray(),
                Books = _bookNameRepository.GetBooks()
            };
            return View(vm);
        }

        [HttpGet]
        public PartialViewResult GetStrongRef(string id)
        {
            return ParseByDictionary(dictRef => {
                    var def = _dictionaryRepository.GetGreekDefinition(dictRef);
                    return PartialView("GreekDefinition", new GreekDefViewModel { Nodes = def.SubNodes, StrongNumber = id, IsTopElement = true });

            }, dictRef => {
                    var def = _dictionaryRepository.GetHebrewDefinition(dictRef);
                    return PartialView("HebrewDefinition", def);

            }, () => { return PartialView("Error"); }, id);

        }

        [HttpGet]
        public IEnumerable<VerseInfo> GetWordRefs(string id, string bookAbbr = null)
        {
            return ParseByDictionary(dictRef => {
                return _verseSearch.GetGreekVersesByWordRef(dictRef, bookAbbr);
            }, dictRef => {
                return _verseSearch.GetHebrewVersesByWordRef(dictRef, bookAbbr);
            }, () => { return new VerseInfo[] { }; }, id);
        }

        [HttpGet]
        public IEnumerable<BookOccurrence> GetWordAggregates(string id)
        {
            return _verseSearch.GetWordBookAggregates(id);
        }

        /// <summary>
        /// Returns a synonym / semantic-network analysis for an original-language token.
        /// Calls the Gemini CLI via <see cref="IAgyLinguisticService"/> (with in-memory caching).
        /// </summary>
        /// <param name="reference">Full verse reference, e.g. "John 3:16"</param>
        /// <param name="strongs">Strong's number, e.g. "G3056"</param>
        /// <param name="lemma">Lemma text, e.g. "λόγος"</param>
        /// <param name="language">"Greek" or "Hebrew"</param>
        [HttpGet]
        public IActionResult GetSynonyms(
            string reference,
            string strongs,
            string lemma,
            string language,
            string connectionId)
        {
            if (string.IsNullOrWhiteSpace(strongs))
                return BadRequest(new { error = "'strongs' parameter is required." });
            if (string.IsNullOrWhiteSpace(connectionId))
                return BadRequest(new { error = "'connectionId' parameter is required." });

            // Run in background and return immediately
            Task.Run(async () =>
            {
                try
                {
                    using var scope = _serviceScopeFactory.CreateScope();
                    var linguisticService = scope.ServiceProvider.GetRequiredService<IAgyLinguisticService>();
                    var hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<LinguisticHub>>();

                    var result = await linguisticService.AnalyzeTokenAsync(
                        strongs, lemma ?? string.Empty, language ?? "Greek", CancellationToken.None);

                    if (result is null)
                    {
                        await hubContext.Clients.Client(connectionId).SendAsync("ReceiveSynonymsError", new { error = "Gemini CLI analysis returned no result." });
                    }
                    else
                    {
                        await hubContext.Clients.Client(connectionId).SendAsync("ReceiveSynonyms", result);
                    }
                }
                catch (OperationCanceledException)
                {
                    // Ignore cancelled in background
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "GetSynonyms background failed for {Strongs}", strongs);
                    try
                    {
                        using var scope = _serviceScopeFactory.CreateScope();
                        var hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<LinguisticHub>>();
                        await hubContext.Clients.Client(connectionId).SendAsync("ReceiveSynonymsError", new { error = "Internal error during synonym analysis." });
                    }
                    catch { }
                }
            });

            return Json(new { status = "processing" });
        }

        [HttpGet]
        public async Task<IActionResult> CheckSynonymsCache(
            string reference,
            string strongs)
        {
            if (string.IsNullOrWhiteSpace(strongs))
                return BadRequest();

            using var scope = _serviceScopeFactory.CreateScope();
            var linguisticService = scope.ServiceProvider.GetRequiredService<IAgyLinguisticService>();
            var cached = await linguisticService.GetCachedTokenAsync(strongs);
            
            if (cached != null)
                return Json(new { cached = true, data = cached });
            else
                return Json(new { cached = false });
        }

        private T ParseByDictionary<T>(Func<int,T> forGreekDefinition, Func<int,T> forHebrewDefinition, Func<T> defaultAction, string id){
            if (id.Substring(0, 1) == "G")
                if (int.TryParse(id.Substring(1),
                    out var strongNumber))
                {
                    return forGreekDefinition(strongNumber);
                    }

            if (id.Substring(0, 1) == "H")
                if (int.TryParse(id.Substring(1),
                    out var strongNumber))
                {
                    return forHebrewDefinition(strongNumber);
                }

            return defaultAction();
        }
    }
}
