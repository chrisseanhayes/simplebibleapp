using System.Xml;

namespace simplebibleapp.xmldictionary
{
    public class PronunciationState : IGreekDefinitionState
    {
        public string TagName => "pronunciation";
        public GreekDefinitionNodeBase GetNewNode(XmlReader reader, GreekDefinitionBuilder builder)
        {
            return new PronunciationNode(
                parent: builder.CurrentNode,
                strongs: reader.GetAttribute("strongs"));
        }
    }
}