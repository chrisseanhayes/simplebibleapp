using System.Xml;

namespace simplebibleapp.xmlbible
{
    public class InscriptionState : IState
    {
        public string TagName => "inscription";
        public NodeBase GetNewNode(XmlReader reader, IChapterBuilder builder)
        {
            return new InscriptionNode(builder.CurrentNode);
        }
    }
}