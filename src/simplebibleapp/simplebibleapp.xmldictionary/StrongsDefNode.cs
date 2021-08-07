namespace simplebibleapp.xmldictionary
{
    public class StrongsDefNode : GreekDefinitionNodeBase
    {
        public override GreekDefinitionNodeType Type => GreekDefinitionNodeType.StrongsDef;

        public StrongsDefNode(GreekDefinitionNodeBase parent) : base(parent)
        {
        }

        public override string ToString()
        {
            return $"Type: {Type}, GreekDefinition: {Text}";
        }
    }
}