using System.Collections.Generic;

namespace simplebibleapp.xmlbiblerepository
{
    public class BibleReading
    {
        private readonly List<Chapter> _chapters = new List<Chapter>();
        public IEnumerable<Chapter> Chapters => _chapters;

        public BibleReading(IEnumerable<Chapter> chapters)
        {
            _chapters.AddRange(chapters);
        }
    }
}