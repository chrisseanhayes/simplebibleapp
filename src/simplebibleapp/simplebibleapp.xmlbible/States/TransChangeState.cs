using System.Xml;

namespace simplebibleapp.xmlbible
{
    public class TransChangeState : IState
    {
        public string TagName => "transChange";
        public NodeBase GetNewNode(XmlReader reader, IChapterBuilder builder)
        {
            return new TransChangeNode(builder.CurrentNode, reader.GetAttribute("type"));
        }
    }
}