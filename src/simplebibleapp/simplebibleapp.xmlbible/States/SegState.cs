using System.Xml;

namespace simplebibleapp.xmlbible
{
    public class SegState : IState
    {
        public string TagName => "seg";
        public NodeBase GetNewNode(XmlReader reader, IChapterBuilder builder)
        {
            return new SegNode(builder.CurrentNode);
        }
    }
}