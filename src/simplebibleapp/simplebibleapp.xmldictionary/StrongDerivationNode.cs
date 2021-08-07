namespace simplebibleapp.xmldictionary
{
    public class StrongDerivationNode : GreekDefinitionNodeBase
    {
        public override GreekDefinitionNodeType Type => GreekDefinitionNodeType.StrongDerivation;

        public StrongDerivationNode(GreekDefinitionNodeBase parent) : base(parent)
        {
        }

        public override string ToString()
        {
            return $"Type: {Type}, StrongsDerivation: {Text}";
        }
    }
}