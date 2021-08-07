using System.Xml;

namespace simplebibleapp.xmldictionary
{
    public class StrongsRefState : IGreekDefinitionState
    {
        public string TagName => "strongsref";
        public GreekDefinitionNodeBase GetNewNode(XmlReader reader, GreekDefinitionBuilder builder)
        {
            return new StrongsRefNode(
                parent: builder.CurrentNode,
                language: reader.GetAttribute("language"),
                strongs: reader.GetAttribute("strongs"));
        }
    }
}