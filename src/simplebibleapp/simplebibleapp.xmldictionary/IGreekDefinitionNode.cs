using System.Collections.Generic;

namespace simplebibleapp.xmldictionary
{
    public interface IGreekDefinitionNode
    {
        int SortOrder { get; }
        GreekDefinitionNodeBase Parent { get; }
        IEnumerable<IGreekDefinitionNode> SubNodes { get; }
        GreekDefinitionNodeType Type { get; }
        string Text { get; }
        void AddSubNode(GreekDefinitionNodeBase node);
    }
}