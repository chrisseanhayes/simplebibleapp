using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using simplebibleapp.Models;
using simplebibleapp.xmlbible;
using simplebibleapp.xmlbible.search;
using simplebibleapp.xmlbiblerepository;
using simplebibleapp.xmldictionary;
using StackExchange.Redis;
using LogLevel = NLog.LogLevel;

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

        public HomeController(
            IChapterBuilderFactory factory,
            IBookNameRepository bookNameRepository,
            IDictionaryRepository dictionaryRepository,
            IWordCountBuilderFactory wordCountBuilderFactory,
            IVerseSearch verseSearch)
        {
            _builder = factory.GetBuilder();
            _bookNameRepository = bookNameRepository;
            _dictionaryRepository = dictionaryRepository;
            _wordCountBuilder = wordCountBuilderFactory.GetBuilder();
            _verseSearch = verseSearch;
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
            var booknames = _bookNameRepository.GetBooks();
            return View(booknames);
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
                SelectableVerses = _bookNameRepository.GetSelectableVerses(bookAbbr).ToArray()
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

            return defaultAction();//PartialView("Error");
        }

        [Authorize]
        [HttpGet]
        public ActionResult Login()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync("Cookies");
            await HttpContext.SignOutAsync("oidc");
            return View();
        }

    }
}
