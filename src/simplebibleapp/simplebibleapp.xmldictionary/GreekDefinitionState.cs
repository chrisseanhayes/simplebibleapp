using System.Xml;

namespace simplebibleapp.xmldictionary
{
    public class GreekDefinitionState : IGreekDefinitionState
    {
        public string TagName => "greek";
        public GreekDefinitionNodeBase GetNewNode(XmlReader reader, GreekDefinitionBuilder builder)
        {
            return new GreekNode(
                parent: builder.CurrentNode,
                beta: reader.GetAttribute("BETA"),
                unicode: reader.GetAttribute("unicode"),
                translit: reader.GetAttribute("translit"));
        }
    }
}