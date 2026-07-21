using System.Collections.Generic;
using simplebibleapp.xmlbible;
using simplebibleapp.xmldictionary;
using simplebibleapp.xmlbiblerepository;

namespace simplebibleapp.Models
{
    public class ReadViewModel
    {
        public string ChapterHeading { get; set; }
        public string BookAbbr { get; set; }
        public int Chapter { get; set; }
        public INode BibleNode { get; set; }
        public bool HasPreviousChapter { get; set; }
        public string PreviousChapterBookAbbr { get; set; }
        public int PreviousChapterNumber { get; set; }
        public bool HasNextChapter { get; set; }
        public string NextChapterBookAbbr { get; set; }
        public int NextChapterNumber { get; set; }
        public IEnumerable<WordInfo> WordInfos { get; set; }
        public IEnumerable<SelectableVerse> SelectableVerses { get; set; }
    }

    public class BibleChapterReading
    {
        public INode BibleNode { get; set; }
    }

    public class GreekDefViewModel
    {
        public IEnumerable<IGreekDefinitionNode> Nodes { get; set; }
        public string StrongNumber { get; set; }
        public bool IsTopElement { get; set; }
    }
}