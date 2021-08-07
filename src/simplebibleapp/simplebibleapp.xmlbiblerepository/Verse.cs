using System.Collections.Generic;
using System.Linq;

namespace simplebibleapp.xmlbiblerepository
{
    public class Verse
    {
        public string SID { get; }
        public string OsisID { get; }

        public string BookAbbreviation => OsisID.Split('.').First();
        public int Chapter => int.Parse(OsisID.Split('.').Skip(1).First() ?? "-1");
        public int VerseNumber => int.Parse(OsisID.Split('.').Skip(2).First() ?? "-1");

        private readonly List<IVersePart> _words = new List<IVersePart>();
        public IEnumerable<IVersePart> Parts => _words;

        public Verse(string sid, string osisId)
        {
            SID = sid;
            OsisID = osisId;
        }

        public void AddPart(IVersePart word)
        {
            _words.Add(word);
        }

        public override string ToString()
        {
            return $"Book: {BookAbbreviation}; Chapter: {Chapter}; Verse: {VerseNumber}; Text: {string.Join("", _words.Select(w => w.Text))}";
        }
    }
}