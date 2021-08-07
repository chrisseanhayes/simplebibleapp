using System.Xml;

namespace simplebibleapp.xmldictionary
{
    public class StrongsDerivationState : IGreekDefinitionState
    {
        public string TagName => "strongs_derivation";
        public GreekDefinitionNodeBase GetNewNode(XmlReader reader, GreekDefinitionBuilder builder)
        {
            return new StrongDerivationNode(parent: builder.CurrentNode);
        }
    }
}