namespace simplebibleapp.xmldictionary
{
    public class TextDefinitionNode : HebrewDefinitionNodeBase
    {
        public TextDefinitionNode(HebrewDefinitionNodeBase parent, string text) : base(parent)
        {
            Text = text;
        }

        public override string Text { get; }

        public override void AddNodeText(string readerValue)
        {
        }
    }
}