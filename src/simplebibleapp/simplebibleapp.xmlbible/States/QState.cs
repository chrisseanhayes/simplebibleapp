using System.Xml;

namespace simplebibleapp.xmlbible
{
    public class QState : IState
    {
        public string TagName => "q";
        public NodeBase GetNewNode(XmlReader reader, IChapterBuilder builder)
        {
            return new QNode(builder.CurrentNode, reader.GetAttribute("who"), reader.GetAttribute("marker"));
        }
    }
}