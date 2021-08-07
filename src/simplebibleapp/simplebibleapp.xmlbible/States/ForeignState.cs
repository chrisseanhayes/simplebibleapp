using System.Xml;

namespace simplebibleapp.xmlbible
{
    public class ForeignState : IState
    {
        public string TagName => "foreign";
        public NodeBase GetNewNode(XmlReader reader, IChapterBuilder builder)
        {
            return new ForeignNode(builder.CurrentNode, reader.GetAttribute("n"));
        }
    }
}