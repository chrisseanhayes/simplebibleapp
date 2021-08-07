using System.Xml;

namespace simplebibleapp.xmldictionary
{
    public interface IGreekDefinitionState
    {
        string TagName { get; }
        GreekDefinitionNodeBase GetNewNode(XmlReader reader, GreekDefinitionBuilder builder);
    }
}