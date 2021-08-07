using System.Xml;

namespace simplebibleapp.xmlbiblerepository
{
    public class MilestoneVersePartState : VersePartStateBase
    {
        private Milestone _milestone;

        public MilestoneVersePartState(int index) : base(index)
        {
            _milestone = new Milestone(index);
        }

        public override void SetReaderProps(XmlReader reader)
        {
            if (reader.MoveToAttribute("type"))
            {
                _milestone.AddType(reader.Value);
                reader.MoveToElement();
            }
            if (reader.MoveToAttribute("marker"))
            {
                _milestone.AddMarker(reader.Value);
                reader.MoveToElement();
            }
        }

        public override void AddText(string text)
        {
        }

        public override IVersePart GetPart()
        {
            return _milestone;
        }
    }
}