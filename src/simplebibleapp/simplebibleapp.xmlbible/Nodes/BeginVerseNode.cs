using System.Linq;

namespace simplebibleapp.xmlbible
{
    public abstract class VerseBaseNode : NodeBase
    {
        public abstract override NodeType XmlNodeType { get; }

        public string SId { get; }
        public string BookAbbr { get; private set; }
        public int Chapter { get; private set; }
        public int Verse { get; private set; }

        protected VerseBaseNode(NodeBase parent, string sId) : base(parent)
        {
            SId = sId;
            SetParts();
        }

        private void SetParts()
        {
            var sidparts = SId.Split('.');
            BookAbbr = sidparts.First();
            if (int.TryParse(sidparts.Skip(1).First(), out var chapter))
                Chapter = chapter;
            if (int.TryParse(sidparts.Skip(2).First(), out var verse))
                Verse = verse;

        }
    }
    public class BeginVerseNode : VerseBaseNode
    {
        public override NodeType XmlNodeType { get; } = NodeType.BeginVerse;
        public string OsisId { get; }

        public BeginVerseNode(NodeBase parent, string osisId, string sId) : base(parent, sId)
        {
            OsisId = osisId;
        }
    }

    public class EndVerseNode : VerseBaseNode
    {
        public EndVerseNode(NodeBase parent, string eId) : base(parent, eId)
        {
        }
        public override NodeType XmlNodeType => NodeType.EndVerse;
    }
}