using System.Collections.Generic;
using System.Linq;

namespace simplebibleapp.xmlbiblerepository
{
    public class Chapter
    {
        private readonly List<Verse> _verses = new List<Verse>();
        public IEnumerable<Verse> Verses => _verses;
        private string _osisId;
        private string _title;
        public string Title => _title;
        private string _titleAttribute;
        public string TitleAttribute => _titleAttribute;

        public string BookAbbreviation => _osisId.Split('.').First();
        public int ChapterNumber => int.Parse(_osisId.Split('.').Skip(1).First() ?? "-1");

        public void AddVerse(Verse verse)
        {
            _verses.Add(verse);
        }

        public void AddOsisId(string id)
        {
            _osisId = id;
        }

        public void AddTitle(string title)
        {
            _title = title;
        }

        public void AddTitleAttribute(string readerValue)
        {
            _titleAttribute = readerValue;
        }

        public override string ToString()
        {
            return $"Book: {BookAbbreviation}; Chapter: {ChapterNumber}; Title: {Title}";
        }
    }
}