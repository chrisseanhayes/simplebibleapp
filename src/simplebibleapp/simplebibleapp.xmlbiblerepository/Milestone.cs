namespace simplebibleapp.xmlbiblerepository
{
    public class Milestone : VersePartBase
    {
        private string _type;
        private string _marker;
        public override VersePartType Type => VersePartType.Milestone;
        public override string Text => _marker;

        public Milestone(int sortOrder) : base(sortOrder)
        {
        }

        public void AddType(string readerValue)
        {
            _type = readerValue;
        }

        public void AddMarker(string readerValue)
        {
            _marker = readerValue;
        }
    }
}