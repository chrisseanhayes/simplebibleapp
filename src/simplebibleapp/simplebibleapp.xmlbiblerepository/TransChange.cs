namespace simplebibleapp.xmlbiblerepository
{
    public class TransChange : VersePartBase
    {
        private string _text;
        public override string Text => _text;
        public override VersePartType Type => VersePartType.TransChange;

        public TransChange(int sortOrder) : base(sortOrder)
        {
        }

        public void SetText(string text)
        {
            _text = text;
        }
    }
}