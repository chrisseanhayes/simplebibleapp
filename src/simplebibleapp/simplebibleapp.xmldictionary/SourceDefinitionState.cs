using System;
using System.Xml;

namespace simplebibleapp.xmldictionary
{
    public class SourceDefinitionState : IHebrewDefinitionState
    {
        public Func<XmlReader, bool> Predicate => rdr => true;
        public string TagName => "source";
        public HebrewDefinitionNodeBase GetNewNode(XmlReader reader, HebrewDefinitionBuilder builder)
        {
            return new SourceHebrewDefinitionNode(builder.CurrentNode);
        }
    }
}