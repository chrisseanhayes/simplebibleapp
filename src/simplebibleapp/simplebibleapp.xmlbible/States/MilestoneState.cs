using System.Xml;

namespace simplebibleapp.xmlbible
{
    public class MilestoneState : IState
    {
        public string TagName => "milestone";
        public NodeBase GetNewNode(XmlReader reader, IChapterBuilder builder)
        {
            return new MilestoneNode(builder.CurrentNode,
                reader.GetAttribute("type"),
                reader.GetAttribute("marker"));
        }
    }
}