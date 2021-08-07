using System.Xml;

namespace simplebibleapp.xmlbiblerepository
{
    public class NotePartState : VersePartStateBase
    {
        private readonly Note _note;
        public NotePartState(int i) : base(i)
        {
            _note = new Note(i);
        }

        public override void SetReaderProps(XmlReader reader)
        {
        }

        public override void AddText(string text)
        {
            _note.SetText(text);
        }

        public override IVersePart GetPart()
        {
            return _note;
        }
    }
}