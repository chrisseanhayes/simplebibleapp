using System;
using System.Xml;

namespace simplebibleapp.xmldictionary
{
    public class NoteDefiniionState : IHebrewDefinitionState
    {
        public Func<XmlReader, bool> Predicate => rdr => true;
        public string TagName => "note";
        public HebrewDefinitionNodeBase GetNewNode(XmlReader reader, HebrewDefinitionBuilder builder)
        {
            return new NoteDefinitionNode(builder.CurrentNode);
        }
    }
}