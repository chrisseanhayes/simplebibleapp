using System.Xml;

namespace simplebibleapp.xmlbiblerepository
{
    public class TextVersePartState : VersePartStateBase
    {
        private VerseText _textPart;

        public TextVersePartState(int index) : base(index)
        {

        }

        public override void SetReaderProps(XmlReader reader)
        {
        }

        public override void AddText(string text)
        {
            _textPart = new VerseText(Index, text);
        }

        public override IVersePart GetPart()
        {
            return _textPart;
        }
    }
}