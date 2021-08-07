namespace simplebibleapp.xmlbible
{
    public class NoteNode : NodeBase
    {
        public override NodeType XmlNodeType { get; } = NodeType.Note;
        public string Type { get; }

        public NoteNode(NodeBase parent, string type) : base(parent)
        {
            Type = type;
        }
    }
}