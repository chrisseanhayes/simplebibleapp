using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using simplebibleapp.xmldatacore;

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
    }

    class TempPathResolver : IXmlPathResolver
    {
        public string GetPath()
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"data");
        }
    }
}
