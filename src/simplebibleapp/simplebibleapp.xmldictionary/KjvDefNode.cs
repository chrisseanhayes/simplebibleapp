namespace simplebibleapp.xmldictionary
{
    public class KjvDefNode : GreekDefinitionNodeBase
    {
        public override GreekDefinitionNodeType Type => GreekDefinitionNodeType.KjvDef;

        public KjvDefNode(GreekDefinitionNodeBase parent) : base(parent)
        {
        }

        public override string ToString()
        {
            return $"Type: {Type}, Text: {Text}";
        }
    }
}