using System;
using System.Xml;

namespace simplebibleapp.xmldictionary
{
    public class RefWDefinitionState : IHebrewDefinitionState
    {
        public Func<XmlReader, bool> Predicate => rdr => !string.IsNullOrWhiteSpace(rdr.GetAttribute("src"));
        public string TagName => "w";
        public HebrewDefinitionNodeBase GetNewNode(XmlReader reader, HebrewDefinitionBuilder builder)
        {
            return new RefWDefinitionNode(
                parent: builder.CurrentNode,
                src: reader.GetAttribute("src"));
        }
    }
}