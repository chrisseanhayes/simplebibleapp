using System;
using System.Xml;

namespace simplebibleapp.xmldictionary
{
    public class UsageDefinitionState : IHebrewDefinitionState
    {
        public Func<XmlReader, bool> Predicate => rdr => true;
        public string TagName => "usage";
        public HebrewDefinitionNodeBase GetNewNode(XmlReader reader, HebrewDefinitionBuilder builder)
        {
            return new UsageDefinitionNode(builder.CurrentNode);
        }
    }
}