namespace simplebibleapp.xmldictionary
{
    public class ElementNode : GreekDefinitionNodeBase
    {
        public override GreekDefinitionNodeType Type =>
            GreekDefinitionNodeType.Element;
        public ElementNode() : base(NullNode)
        {

        }

        public override string ToString()
        {
            return $"Type: {Type}, Element: {Text}";
        }
    }
}