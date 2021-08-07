using System.Xml;

namespace simplebibleapp.xmldictionary
{
    public class StrongsState : IGreekDefinitionState
    {
        public string TagName => "strongs";

        public GreekDefinitionNodeBase GetNewNode(XmlReader reader, GreekDefinitionBuilder builder)
        {
            return new StrongsNode(parent: builder.CurrentNode);
        }
    }
}