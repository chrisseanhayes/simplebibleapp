namespace simplebibleapp.xmlbible
{
    public class ForeignNode : NodeBase
    {
        public string N { get; }
        public override NodeType XmlNodeType { get; } = NodeType.Foreign;
        public ForeignNode(NodeBase parent, string n) : base(parent)
        {
            N = n;
        }
    }
}