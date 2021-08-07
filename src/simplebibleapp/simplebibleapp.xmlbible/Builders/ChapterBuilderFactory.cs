using System;
using System.Collections.Generic;
using simplebibleapp.xmldatacore;

namespace simplebibleapp.xmlbible
{
    public interface IChapterBuilderFactory
    {
        IChapterBuilder GetBuilder();
    }

    public class ChapterBuilderFactory : IChapterBuilderFactory
    {
        private readonly IEnumerable<IState> _states;
        private readonly IXmlPathResolver _pathResolver;

        public ChapterBuilderFactory(IEnumerable<IState> states, IXmlPathResolver pathResolver)
        {
            _states = states;
            _pathResolver = pathResolver;
        }
        public IChapterBuilder GetBuilder()
        {
            Func<IChapterBuilder> source = () => new ChapterBuilder(_states, _pathResolver);
            var builder = new MemCacheChapterBuilder(source);

            MemCacheChapterBuilder.LoadBible(source);

            return builder;
        }
    }
}