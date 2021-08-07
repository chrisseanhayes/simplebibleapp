namespace simplebibleapp.xmlbible
{
    public class TitleNode : NodeBase
    {
        public override NodeType XmlNodeType { get; } = NodeType.Title;
        public string Type { get; }

        public TitleNode(NodeBase parent, string type) : base(parent)
        {
            Type = type;
        }
    }
}