using System;
using System.Xml;

namespace simplebibleapp.xmldictionary
{
    public class MeaningDefinitionState : IHebrewDefinitionState
    {
        public Func<XmlReader, bool> Predicate => rdr => true;
        public string TagName => "meaning";
        public HebrewDefinitionNodeBase GetNewNode(XmlReader reader, HebrewDefinitionBuilder builder)
        {
            return new MeaningDefinitionNode(builder.CurrentNode);
        }
    }
}