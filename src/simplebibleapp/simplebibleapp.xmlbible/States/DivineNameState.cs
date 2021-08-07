using System.Xml;

namespace simplebibleapp.xmlbible
{
    public class DivineNameState : IState
    {
        public string TagName => "divineName";
        public NodeBase GetNewNode(XmlReader reader, IChapterBuilder builder)
        {
            return new DivineNameNode(builder.CurrentNode);
        }
    }
}