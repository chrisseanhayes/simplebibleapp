using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace simplebibleapp.xmlbible.search
{
    public class VerseInfo
    {
        public string ChapterAbbr { get; }
        public int ChapterNumber { get; }
        public int VerseNumber { get; }
        public string VerseXmlText { get; }
        public VerseInfo(string chapterAbbr, int chapterNumber, int verseNumber, string verseXmlText)
        {
            ChapterAbbr = chapterAbbr;
            ChapterNumber = chapterNumber;
            VerseNumber = verseNumber;
            VerseXmlText = verseXmlText;
        }
    }
    class XmlHelps
    {
        public static XmlDocument GetDocument(string xmlPath)
        {
            var doc = new XmlDocument();
            doc.Load(xmlPath);
            return doc;
        }

    }
    public class BookOccurrence
    {
        public string BookAbbr { get; }
        public int Count { get; }
        public BookOccurrence(string bookAbbr, int count)
        {
            BookAbbr = bookAbbr;
            Count = count;
        }
    }

    public interface IVerseSearch
    {
        IEnumerable<VerseInfo> GetGreekVersesByWordRef(int highlitedWordId, string bookAbbr = null);
        IEnumerable<VerseInfo> GetHebrewVersesByWordRef(int highlitedWordId, string bookAbbr = null);
        IEnumerable<BookOccurrence> GetWordBookAggregates(string id);
    }

    public static class SearchHelps
    {
        public static ConcurrentDictionary<string, Lazy<XmlDocument>> documentCache = new ConcurrentDictionary<string, Lazy<XmlDocument>>();
        public static IVerseSearch GetVerseSearch(string bibleXmlPath)
        {
            if (!documentCache.ContainsKey(bibleXmlPath)) documentCache.TryAdd(bibleXmlPath, new Lazy<XmlDocument>(() => XmlHelps.GetDocument(bibleXmlPath)));
            documentCache.TryGetValue(bibleXmlPath, out var bible);
            return new VerseSearch(bible);
        }
    }
    class VerseSearch : IVerseSearch
    {
        private readonly Lazy<XmlDocument> _bible;
        public VerseSearch(Lazy<XmlDocument> bible)
        {
            _bible = bible;
        }
        public IEnumerable<BookOccurrence> GetWordBookAggregates(string id)
        {
            return Array.Empty<BookOccurrence>();
        }
        public IEnumerable<VerseInfo> GetGreekVersesByWordRef(int highlitedWordId, string bookAbbr = null)
        {
            var doc = _bible.Value;
            var words = SearchGreekDictionary(doc, highlitedWordId);
            return words
                .Select(w => GetVerseNodeFromWord(w))
                .Where(v => v != null)
                .Select(v => GetVerseInfo(v, highlitedWordId));
        }

        public IEnumerable<VerseInfo> GetHebrewVersesByWordRef(int highlitedWordId, string bookAbbr = null)
        {
            var doc = _bible.Value;
            var words = SearchHebrewDictionary(doc, highlitedWordId);
            return words
                .Select(w => GetVerseNodeFromWord(w))
                .Where(v => v != null)
                .Select(v => GetVerseInfo(v, highlitedWordId));
        }



        VerseInfo GetVerseInfo(XmlNode verseNode, int highlitedWordId)
        {
            var refText = verseNode.Attributes.Cast<XmlAttribute>().FirstOrDefault(x => x.Name == "osisID")?.InnerText;
            var refTextParts = refText.Split('.');
            var verseText = GetHighlightedWordTextXml(GetWordVerse(verseNode), highlitedWordId);
            return new VerseInfo(refTextParts[0], int.Parse(refTextParts[1]), int.Parse(refTextParts[2]), verseText);
        }

        string GetHighlightedWordTextXml(IEnumerable<XmlNode> verseNodes, int highlitedWordId)
        {
            var verseText = new StringBuilder();
            Func<XmlNode, string> wordSpace = n => n.NodeType != XmlNodeType.Text && n.PreviousSibling?.Name == "w" ? " " : string.Empty;
            Func<XmlNode, string> getEmphText = n =>
            {
                if (n.NodeType == XmlNodeType.Text || n.Name != "w")
                    return n.InnerText;

                var lemmaAttr = n.Attributes?["lemma"];
                if (lemmaAttr == null)
                    return n.InnerText;

                var lemmas = lemmaAttr.InnerText.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                var searchStrGreek = $"strong:G{highlitedWordId}";
                var searchStrHebrew = $"strong:H0{highlitedWordId}";

                if (lemmas.Any(l => l == searchStrGreek || l == searchStrHebrew))
                    return $"<em>{n.InnerText}</em>";

                return n.InnerText;
            };
            verseNodes.ToList().ForEach(n =>
            {
                verseText.Append(wordSpace(n) + getEmphText(n));
            });
            return $"<verse>{verseText.ToString()}</verse>";
        }


        IEnumerable<XmlNode> SearchGreekDictionary(XmlDocument doc, int greekStrongsRef) =>
            doc.SelectNodes($"//*[name()='w' and contains(concat(' ', normalize-space(@lemma), ' '), ' strong:G{greekStrongsRef} ')]").Cast<XmlNode>();

        IEnumerable<XmlNode> SearchHebrewDictionary(XmlDocument doc, int hebrewStrongsRef) =>
            doc.SelectNodes($"//*[name()='w' and contains(concat(' ', normalize-space(@lemma), ' '), ' strong:H0{hebrewStrongsRef} ')]").Cast<XmlNode>();

        IEnumerable<XmlNode> GetWordVerse(XmlNode word)
        {
            var verseRef = GetVerseNodeFromWord(word);
            return GetVerseNodesFromTopVerseNode(verseRef);
        }

        XmlNode GetVerseNodeFromWord(XmlNode word)
        {
            var topVerse = word;
            while (topVerse != null && topVerse.Name != "verse")
            {
                if (topVerse.PreviousSibling == null)
                {
                    topVerse = topVerse.ParentNode;
                    if (topVerse == null)
                        break;
                }
                topVerse = topVerse.PreviousSibling;
            }
            return topVerse;
        }

        IEnumerable<XmlNode> GetVerseNodesFromTopVerseNode(XmlNode verseNode)
        {
            var output = new List<XmlNode>();
            if (verseNode == null) return output;
            output.Add(verseNode);
            var bottomVerse = verseNode.NextSibling;
            while (bottomVerse != null && bottomVerse.Name != "verse")
            {
                if (bottomVerse.Name == "q" && bottomVerse.HasChildNodes)
                {
                    bottomVerse = bottomVerse.ChildNodes[0];
                }
                if (bottomVerse.NextSibling == null)
                {
                    bottomVerse = bottomVerse.ParentNode?.NextSibling;
                }
                else
                {
                    bottomVerse = bottomVerse.NextSibling;
                }
                if (bottomVerse != null)
                {
                    output.Add(bottomVerse);
                }
            }
            return output;
        }
    }
}