namespace simplebibleapp.xmlbible
{
    public class SegNode : NodeBase
    {
        public override NodeType XmlNodeType { get; } = NodeType.Seg;
        public SegNode(NodeBase parent) : base(parent)
        {
        }
    }
}