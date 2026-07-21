using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using simplebibleapp.xmldatacore;
using simplebibleapp.xmlbible.search;

namespace simplebibleapp.xmlbible.tests
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            var states = new IState[]
            {
                new DivineNameState(), 
                new ForeignState(), 
                new InscriptionState(), 
                new MilestoneState(), 
                new NoteState(), 
                new QState(), 
                new SegState(), 
                new TitleState(), 
                new TransChangeState(), 
                new VerseState(), 
                new WordState(), 
            };
            var pathresolver = new TempPathResolver();
            var builder = new ChapterBuilder(states,pathresolver);
            var tries = new ConcurrentBag<string>();
            Parallel.ForEach(Enumerable.Range(1, 1000), i =>
            {
                var job6 = builder.GetChapter("Job", 6);
                tries.Add(JsonConvert.SerializeObject(job6));
            });

            Assert.IsTrue(tries.Distinct().Count() == 1);
        }

        [TestMethod]
        public void TestHebrewSearchDoesNotThrowOnTitleNodes()
        {
            var pathresolver = new TempPathResolver();
            var biblePath = Path.Combine(pathresolver.GetPath(), "Bible", "kjvfull.xml");
            if (!File.Exists(biblePath))
            {
                biblePath = Path.Combine(pathresolver.GetPath(), "kjvfull.xml");
            }
            var search = SearchHelps.GetVerseSearch(biblePath);
            var verses = search.GetHebrewVersesByWordRef(3068).ToList();
            Assert.IsTrue(verses.Count > 0);
        }

        [TestMethod]
        public void TestSqliteChapterBuilder()
        {
            try
            {
                var resolver = new DirectPathResolver();
                var builder = new SqliteChapterBuilder(resolver);
                var result = builder.GetChapter("Gen", 1);
                Assert.IsNotNull(result);
                Assert.IsNotNull(result.CurrentNode);
                Console.WriteLine("Gen 1 node type: " + result.CurrentNode.XmlNodeType);
            }
            catch (Exception ex)
            {
                Console.WriteLine("EXCEPTION: " + ex);
                throw;
            }
        }

        [TestMethod]
        public void TestSqliteVerseSearch()
        {
            try
            {
                var resolver = new DirectPathResolver();
                var search = new SqliteVerseSearch(resolver);
                var result = search.GetGreekVersesByWordRef(2424).ToList();
                Assert.IsTrue(result.Count > 0);
                Console.WriteLine("Found Greek verses count: " + result.Count);
            }
            catch (Exception ex)
            {
                Console.WriteLine("EXCEPTION: " + ex);
                throw;
            }
        }

        [TestMethod]
        public void TestSqliteVerseSearchWithBook()
        {
            try
            {
                var resolver = new DirectPathResolver();
                var search = new SqliteVerseSearch(resolver);
                var resultAll = search.GetGreekVersesByWordRef(2424).ToList();
                var resultMattOnly = search.GetGreekVersesByWordRef(2424, "Matt").ToList();
                Assert.IsTrue(resultAll.Count > resultMattOnly.Count);
                Assert.IsTrue(resultMattOnly.Count > 0);
                Assert.IsTrue(resultMattOnly.All(v => v.ChapterAbbr == "Matt"));
                Console.WriteLine($"Found global Greek verses count: {resultAll.Count}, Matt only: {resultMattOnly.Count}");
            }
            catch (Exception ex)
            {
                Console.WriteLine("EXCEPTION: " + ex);
                throw;
            }
        }

        [TestMethod]
        public void TestSqliteVerseSearchBlacklist()
        {
            try
            {
                var resolver = new DirectPathResolver();
                var search = new SqliteVerseSearch(resolver);
                // G2532 (kai - conjunction) is excluded
                var resultGreek = search.GetGreekVersesByWordRef(2532).ToList();
                Assert.AreEqual(0, resultGreek.Count);

                // H853 (eth - direct object sign) is H0853, which is excluded
                var resultHebrew = search.GetHebrewVersesByWordRef(853).ToList();
                Assert.AreEqual(0, resultHebrew.Count);
            }
            catch (Exception ex)
            {
                Console.WriteLine("EXCEPTION: " + ex);
                throw;
            }
        }

        [TestMethod]
        public void TestSqliteVerseSearchAggregates()
        {
            try
            {
                var resolver = new DirectPathResolver();
                var search = new SqliteVerseSearch(resolver);
                var result = search.GetWordBookAggregates("G2424").ToList();
                Assert.IsTrue(result.Count > 0);
                Assert.IsTrue(result.Any(r => r.BookAbbr == "Matt" && r.Count > 0));
                Console.WriteLine($"G2424 aggregate books count: {result.Count}");
                foreach (var agg in result.Take(5))
                {
                    Console.WriteLine($"Book: {agg.BookAbbr}, Count: {agg.Count}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("EXCEPTION: " + ex);
                throw;
            }
        }
    }

    class TempPathResolver : IXmlPathResolver
    {
        public string GetPath()
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"data");
        }
    }
}
