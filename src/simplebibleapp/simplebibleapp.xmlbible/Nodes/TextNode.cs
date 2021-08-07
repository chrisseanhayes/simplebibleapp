namespace simplebibleapp.xmlbible
{
    public class TextNode : NodeBase
    {
        public override NodeType XmlNodeType { get; } = NodeType.Text;
        public override string Text { get; }

        public TextNode(NodeBase parent, string text) : base(parent)
        {
            Text = text;
        }
    }
}