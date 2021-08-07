using System.Xml;

namespace simplebibleapp.xmlbible
{
    public class TitleState : IState
    {
        public string TagName => "title";
        public NodeBase GetNewNode(XmlReader reader, IChapterBuilder builder)
        {
            return new TitleNode(builder.CurrentNode, reader.GetAttribute("type"));
        }
    }
}