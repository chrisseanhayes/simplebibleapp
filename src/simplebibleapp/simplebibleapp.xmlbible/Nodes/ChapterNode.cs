namespace simplebibleapp.xmlbible
{
    public class ChapterNode : NodeBase
    {
        public override NodeType XmlNodeType { get; } = NodeType.Chapter;

        public ChapterNode() : base(NullNode)
        {
        }
    }
}