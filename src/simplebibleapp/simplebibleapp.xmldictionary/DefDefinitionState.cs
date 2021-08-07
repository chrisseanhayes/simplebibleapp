using System;
using System.Xml;

namespace simplebibleapp.xmldictionary
{
    public class DefDefinitionState : IHebrewDefinitionState
    {
        public Func<XmlReader, bool> Predicate => rdr => true;
        public string TagName => "def";
        public HebrewDefinitionNodeBase GetNewNode(XmlReader reader, HebrewDefinitionBuilder builder)
        {
            return new DefDefinitionNode(builder.CurrentNode);
        }
    }
}