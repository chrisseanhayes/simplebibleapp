using System.Xml;

namespace simplebibleapp.xmldictionary
{
    public class SeeState : IGreekDefinitionState
    {
        public string TagName => "see";
        public GreekDefinitionNodeBase GetNewNode(XmlReader reader, GreekDefinitionBuilder builder)
        {
            return new SeeNode(
                parent: builder.CurrentNode,
                language: reader.GetAttribute("language"), strongs: reader.GetAttribute("strongs"));
        }
    }
}