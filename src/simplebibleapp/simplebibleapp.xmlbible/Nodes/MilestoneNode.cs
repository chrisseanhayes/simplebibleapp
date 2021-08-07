namespace simplebibleapp.xmlbible
{
    public class MilestoneNode : NodeBase
    {
        public override NodeType XmlNodeType { get; } = NodeType.Milestone;
        public string Type { get; }
        public string Marker { get; }

        public MilestoneNode(NodeBase parent, string type, string marker) : base(parent)
        {
            Type = type;
            Marker = marker;
        }
    }
}