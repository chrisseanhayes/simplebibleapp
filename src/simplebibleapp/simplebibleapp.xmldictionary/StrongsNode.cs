namespace simplebibleapp.xmldictionary
{
    public class StrongsNode : GreekDefinitionNodeBase
    {
        public int StrongsNumber { get; private set; }
        public override GreekDefinitionNodeType Type => GreekDefinitionNodeType.Strongs;
        public override string Text => StrongsNumber.ToString();

        public StrongsNode(GreekDefinitionNodeBase parent) : base(parent)
        {
        }

        public override void AddSubNode(GreekDefinitionNodeBase node)
        {
            if (node.Type == GreekDefinitionNodeType.Text &&
                int.TryParse(node.Text, out var number))
            {
                StrongsNumber = number;
            }
        }

        public override string ToString()
        {
            return $"Type: {Type}, StrongsNumber: {StrongsNumber}, Text: {Text}";
        }
    }
}