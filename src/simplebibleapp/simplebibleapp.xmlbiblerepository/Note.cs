namespace simplebibleapp.xmlbiblerepository
{
    public class Note : VersePartBase
    {
        public Note(int sortOrder) : base(sortOrder)
        {
        }

        private string _text;
        public override string Text => _text;
        public override VersePartType Type => VersePartType.Note;
        public void SetText(string text)
        {
            _text = text;
        }

    }
}