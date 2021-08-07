using System.Xml;

namespace simplebibleapp.xmldictionary
{
    public class KjvDefState : IGreekDefinitionState
    {
        public string TagName => "kjv_def";
        public GreekDefinitionNodeBase GetNewNode(XmlReader reader, GreekDefinitionBuilder builder)
        {
            return new KjvDefNode(parent: builder.CurrentNode);
        }
    }
}