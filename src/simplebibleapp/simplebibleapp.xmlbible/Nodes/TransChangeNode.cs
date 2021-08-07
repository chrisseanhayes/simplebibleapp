namespace simplebibleapp.xmlbible
{
    public class TransChangeNode : NodeBase
    {
        public override NodeType XmlNodeType { get; } = NodeType.TransChange;
        public string Type { get; }

        public TransChangeNode(NodeBase parent, string type) : base(parent)
        {
            Type = type;
        }
    }
}