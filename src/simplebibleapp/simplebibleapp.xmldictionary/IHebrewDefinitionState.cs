using System;
using System.Xml;

namespace simplebibleapp.xmldictionary
{
    public interface IHebrewDefinitionState
    {
        Func<XmlReader, bool> Predicate { get; }
        string TagName { get; }
        HebrewDefinitionNodeBase GetNewNode(
            XmlReader reader, HebrewDefinitionBuilder builder);
    }
}