using System.ComponentModel;
using System.Xml;

namespace simplebibleapp.xmlbible
{
    public class VerseState : IState
    {
        public string TagName => "verse";
        public NodeBase GetNewNode(XmlReader reader, IChapterBuilder builder)
        {
            if(reader.GetAttribute("sID")!= null)
                return new BeginVerseNode(
                    parent: builder.CurrentNode, 
                    osisId: reader.GetAttribute("osisID"), 
                    sId: reader.GetAttribute("sID"));
            else
                return new EndVerseNode(
                    parent: builder.CurrentNode,
                    eId: reader.GetAttribute(("eID")));
        }
    }
}