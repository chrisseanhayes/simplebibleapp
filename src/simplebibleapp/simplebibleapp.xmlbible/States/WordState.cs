using System.Xml;

namespace simplebibleapp.xmlbible
{
    public class WordState : IState
    {
        public string TagName => "w";
        public NodeBase GetNewNode(XmlReader reader, IChapterBuilder builder)
        {
            return new WordNode(builder.CurrentNode, reader.GetAttribute("lemma"), reader.GetAttribute("morph"));
        }
    }
}