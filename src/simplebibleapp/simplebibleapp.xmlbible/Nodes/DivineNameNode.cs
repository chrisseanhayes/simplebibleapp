namespace simplebibleapp.xmlbible
{
    public class DivineNameNode : NodeBase
    {
        public override NodeType XmlNodeType { get; } = NodeType.DivineName;
        public DivineNameNode(NodeBase parent) : base(parent)
        {
        }
    }
}