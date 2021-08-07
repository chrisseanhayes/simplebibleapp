using System.Collections.Generic;

namespace simplebibleapp.xmldictionary
{
    public interface IHebrewDefinitionNode
    {
        string Text { get; }
        IEnumerable<HebrewDefinitionNodeBase> SubNodes { get; }
        HebrewDefinitionNodeBase Parent { get; }
        bool IsOpen { get; }
        void CloseNode();
        void AddNodeText(string readerValue);
        void AddSubNode(HebrewDefinitionNodeBase node);
    }
}