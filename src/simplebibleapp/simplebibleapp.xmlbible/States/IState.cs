using System.Xml;

namespace simplebibleapp.xmlbible
{
    public interface IState
    {
        string TagName { get; }
        NodeBase GetNewNode(XmlReader reader, IChapterBuilder builder);
    }
}