using System;
using System.Xml;

namespace simplebibleapp.xmldictionary
{
    public class RootWHebrewDefinitionState : IHebrewDefinitionState
    {
        public Func<XmlReader, bool> Predicate => rdr => string.IsNullOrWhiteSpace(rdr.GetAttribute("src"));
        public string TagName => "w";
        public HebrewDefinitionNodeBase GetNewNode(XmlReader reader, HebrewDefinitionBuilder builder)
        {
            return new RootWDefinitionNode(
                parent: builder.CurrentNode,
                pos: reader.GetAttribute("pos"),
                pron: reader.GetAttribute("pron"),
                xlit: reader.GetAttribute("xlit"),
                xmllang: reader.GetAttribute("xml:lang"));
        }
    }
}