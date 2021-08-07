namespace simplebibleapp.xmlbible
{
    public class WordNode : NodeBase
    {
        public override NodeType XmlNodeType { get; } = NodeType.Word;
        public string Lemma { get; }
        public string Morph { get; }

        public WordNode(NodeBase parent, string lemma, string morph) : base(parent)
        {
            Lemma = lemma;
            Morph = morph;
        }
    }
}