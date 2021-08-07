namespace simplebibleapp.xmldictionary
{
    public class GreekNode : GreekDefinitionNodeBase
    {
        public string Beta { get; }
        public string Unicode { get; }
        public string Translit { get; }

        public GreekNode(GreekDefinitionNodeBase parent, string beta, string unicode, string translit) : base(parent)
        {
            Beta = beta;
            Unicode = unicode;
            Translit = translit;
        }

        public override GreekDefinitionNodeType Type => GreekDefinitionNodeType.Greek;

        public override string ToString()
        {
            return $"Type: {Type}, Beta: {Beta}, Unicode: {Unicode}, Translit: {Translit}";
        }
    }
}