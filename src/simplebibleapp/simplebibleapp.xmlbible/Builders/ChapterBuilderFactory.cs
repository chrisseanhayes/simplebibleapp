using System;
using simplebibleapp.xmldatacore;

namespace simplebibleapp.xmlbible
{
    public interface IChapterBuilderFactory
    {
        IChapterBuilder GetBuilder();
    }

    public class ChapterBuilderFactory : IChapterBuilderFactory
    {
        private readonly IXmlPathResolver _pathResolver;

        public ChapterBuilderFactory(IXmlPathResolver pathResolver)
        {
            _pathResolver = pathResolver;
        }

        public IChapterBuilder GetBuilder()
        {
            return new SqliteChapterBuilder(_pathResolver);
        }
    }
}