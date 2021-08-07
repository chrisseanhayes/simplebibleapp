using System.Xml;

namespace simplebibleapp.xmlbible
{
    public class NoteState : IState
    {
        public string TagName => "note";
        public NodeBase GetNewNode(XmlReader reader, IChapterBuilder builder)
        {
            return new NoteNode(builder.CurrentNode, reader.GetAttribute("type"));
        }
    }
}