using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace simplebibleapp.xmlbible
{
    class MemCacheChapterBuilder : IChapterBuilder
    {
        private readonly Func<IChapterBuilder> _sourceFactory;
        private BuilderState _selectedNode;
        private static volatile bool _loaded;
        private static volatile bool _loading;
        private static ConcurrentDictionary<string, BuilderState> _cahcedNodes =
            new ConcurrentDictionary<string, BuilderState>();
        public NodeBase CurrentNode => _selectedNode.CurrentNode;
        public bool HasNextChapter => _selectedNode.HasNextChapter;
        public bool HasPrevChapter => _selectedNode.HasPrevChapter;
        public string NextBookAbbr => _selectedNode.NextBookAbbr;
        public int NextChapterNumber => _selectedNode.NextChapterNumber;
        public string PrevBookAbbr => _selectedNode.PrevBookAbbr;
        public int PrevChapterNumber => _selectedNode.PrevChapterNumber;

        public MemCacheChapterBuilder(Func<IChapterBuilder> sourceFactory)
        {
            _sourceFactory = sourceFactory;
        }
        public BuilderState GetChapter(string bookAbbreviation, int chapter)
        {
            var key = $"{bookAbbreviation}:{chapter}";
            var details = TryFromCache(bookAbbreviation, chapter, key, _sourceFactory());
            Console.WriteLine($"Getting {key} from cache");
            _selectedNode = details;
            return details;
        }

        private static BuilderState TryFromCache(string bookAbbreviation, int chapter, string key, IChapterBuilder source)
        {
            if (!_cahcedNodes.ContainsKey(key))
            {
                var result = source.GetChapter(bookAbbreviation, chapter);
                _cahcedNodes.TryAdd(key, result);
            }

            if (!_cahcedNodes.TryGetValue(key, out var details))
                throw new ApplicationException("unable to find chapter");
            return details;
        }

        public static void LoadBible(Func<IChapterBuilder> source)
        {
            if (_loaded || _loading) return;
            _loading = true;
            Task.Run(() =>
            {
                var details = MemCacheChapterBuilder.TryFromCache("Gen", 1, "Gen:1", source());
                while (details.HasNextChapter)
                {
                    var nextBook = details.NextBookAbbr;
                    var nextChapter = details.NextChapterNumber;
                    details = MemCacheChapterBuilder.TryFromCache(nextBook, nextChapter, $"{nextBook}:{nextChapter}",
                        source());
                }

            }).ContinueWith(rslt =>
            {
                _loading = false;
                _loaded = true;
            });
        }
    }
}