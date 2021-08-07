namespace simplebibleapp.xmlbible
{
    public class InscriptionNode : NodeBase
    {
        public override NodeType XmlNodeType { get; } = NodeType.Inscription;

        public InscriptionNode(NodeBase parent) : base(parent)
        {
        }
    }
}