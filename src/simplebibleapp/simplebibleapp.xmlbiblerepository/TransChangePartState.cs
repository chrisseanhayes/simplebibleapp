using System.Xml;

namespace simplebibleapp.xmlbiblerepository
{
    public class TransChangePartState : VersePartStateBase
    {
        private readonly TransChange _transChange;

        public TransChangePartState(int index) : base(index)
        {
            _transChange = new TransChange(index);
        }

        public override void SetReaderProps(XmlReader reader)
        {
        }

        public override void AddText(string text)
        {
            _transChange.SetText(text);
        }

        public override IVersePart GetPart()
        {
            return _transChange;
        }
    }
}