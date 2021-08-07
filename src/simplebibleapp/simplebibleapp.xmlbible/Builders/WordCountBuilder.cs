using System.Collections.Generic;
using System.Linq;

namespace simplebibleapp.xmlbible
{
    public interface IWordCountBuilder
    {
        IEnumerable<WordInfo> GetWords();
    }

    public interface IWordCountBuilderFactory
    {
        IWordCountBuilder GetBuilder();
    }

    public class WordCountBuilderFactory : IWordCountBuilderFactory
    {
        private readonly IChapterBuilderFactory _chapterBuilderFactory;

        public WordCountBuilderFactory(IChapterBuilderFactory chapterBuilderFactory)
        {
            _chapterBuilderFactory = chapterBuilderFactory;
        }
        public IWordCountBuilder GetBuilder()
        {
            return new MemCacheCountBuilder(new WordCountBuilder(_chapterBuilderFactory));
        }
    }

    public class MemCacheCountBuilder : IWordCountBuilder
    {
        private readonly IWordCountBuilder _source;
        private IEnumerable<WordInfo> _wordInfos;

        public MemCacheCountBuilder(IWordCountBuilder source)
        {
            _source = source;
        }
        public IEnumerable<WordInfo> GetWords()
        {
            return _wordInfos ?? (_wordInfos = _source.GetWords());
        }
    }

    public class WordCountBuilder : IWordCountBuilder
    {
        private readonly IChapterBuilderFactory _builderFactory;
        private static readonly WordInfo[] _emptyList = new WordInfo[0];

        public WordCountBuilder(IChapterBuilderFactory chapterBuilderFactory)
        {
            _builderFactory = chapterBuilderFactory;
        }

        public IEnumerable<WordInfo> GetWords()
        {
            var wordList = new List<WordInfo>();
            var builder = _builderFactory.GetBuilder();
            builder.GetChapter("Gen", 1);
            do
            {
                wordList.AddRange(GetWordsFromNode(builder.CurrentNode, "Gen", 1, 1));
                builder = _builderFactory.GetBuilder();
                builder.GetChapter(builder.NextBookAbbr, builder.NextChapterNumber);
            } while (builder.HasNextChapter);

            return wordList;
        }

        private IEnumerable<WordInfo> GetWordsFromNode(NodeBase currentNode,string bookAbbr, int chapter, int verse) =>
            GetWordsFromNode(currentNode, _emptyList, bookAbbr,chapter,verse);

        private IEnumerable<WordInfo> GetWordsFromNode(NodeBase currentNode,
            IEnumerable<WordInfo> rootCollection, string currentBook, int chapter, int verse)
        {
            IEnumerable<WordInfo> output = rootCollection.ToArray();
            if (currentNode.XmlNodeType == NodeType.BeginVerse)
            {
                var verseInfo = currentNode as BeginVerseNode;
                currentBook = verseInfo.BookAbbr;
                chapter = verseInfo.Chapter;
                verse = verseInfo.Verse;
            }

            if (currentNode.XmlNodeType == NodeType.Word)
                output = output.Union(new[] { WordInfo.FromNode(currentNode as WordNode,currentBook,chapter,verse ) });

            currentNode.SubNodes.ToList().ForEach(n => output = GetWordsFromNode(n, output, currentBook,chapter,verse));
            return output;
        }
    }

    public class WordInfo
    {
        public string StrongNumber { get; private set; }

        public int Verse { get; private set; }

        public int Chapter { get; private set; }

        public string BookAbbr { get; private set; }

        public static WordInfo FromNode(WordNode wordNode, string currentBook, int chapter, int verse)
        {
            return new WordInfo
            {
                StrongNumber = wordNode.Lemma,
                BookAbbr = currentBook,
                Chapter = chapter,
                Verse = verse
            };
        }
    }
}