using System.Xml;

namespace simplebibleapp.xmldictionary
{
    public class StrongsDefState : IGreekDefinitionState
    {
        public string TagName => "strongs_def";
        public GreekDefinitionNodeBase GetNewNode(XmlReader reader, GreekDefinitionBuilder builder)
        {
            return new StrongsDefNode(parent: builder.CurrentNode);
        }
    }
}