namespace simplebibleapp.xmldictionary
{
    public class LatinNode : GreekDefinitionNodeBase
    {
        public override GreekDefinitionNodeType Type => GreekDefinitionNodeType.Latin;

        public LatinNode(GreekDefinitionNodeBase parent) : base(parent)
        {
        }

        public override string ToString()
        {
            return $"Type: {Type}, Text: {Text}";
        }
    }
}