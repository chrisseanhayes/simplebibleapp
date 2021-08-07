namespace simplebibleapp.xmldictionary
{
    internal class TextNode : GreekDefinitionNodeBase
    {
        public override GreekDefinitionNodeType Type => GreekDefinitionNodeType.Text;
        public override string Text { get; }

        public TextNode(GreekDefinitionNodeBase parent, string readerValue) : base(parent)
        {
            Text = readerValue;
        }

        public override string ToString()
        {
            return $"Type: {Type}, Text{Text}";
        }
    }
}