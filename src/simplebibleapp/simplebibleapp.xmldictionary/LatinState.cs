using System.Xml;

namespace simplebibleapp.xmldictionary
{
    public class LatinState : IGreekDefinitionState
    {
        public string TagName => "latin";
        public GreekDefinitionNodeBase GetNewNode(XmlReader reader, GreekDefinitionBuilder builder)
        {
            return new LatinNode(parent: builder.CurrentNode);
        }
    }
}