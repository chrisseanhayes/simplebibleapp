namespace simplebibleapp.xmldictionary
{
    public class RefWDefinitionNode : HebrewDefinitionNodeBase
    {
        public string Src { get; }

        public RefWDefinitionNode(HebrewDefinitionNodeBase parent, string src) : base(parent)
        {
            Src = src;
        }
    }
}