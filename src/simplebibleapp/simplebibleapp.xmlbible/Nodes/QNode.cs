namespace simplebibleapp.xmlbible
{
    public class QNode : NodeBase
    {
        public string Who { get; }
        public string Marker { get; }
        public override NodeType XmlNodeType { get; } = NodeType.Q;
        public QNode(NodeBase parent, string who, string marker) : base(parent)
        {
            Who = who;
            Marker = marker;
        }
    }
}