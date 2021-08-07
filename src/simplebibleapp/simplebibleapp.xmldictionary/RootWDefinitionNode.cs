namespace simplebibleapp.xmldictionary
{
    public class RootWDefinitionNode : HebrewDefinitionNodeBase
    {
        public string Pos { get; }
        public string Pron { get; }
        public string Xlit { get; }
        public string Xmllang { get; }

        public RootWDefinitionNode(HebrewDefinitionNodeBase parent, string pos, string pron, string xlit, string xmllang) : base(parent)
        {
            Pos = pos;
            Pron = pron;
            Xlit = xlit;
            Xmllang = xmllang;
        }
    }
}