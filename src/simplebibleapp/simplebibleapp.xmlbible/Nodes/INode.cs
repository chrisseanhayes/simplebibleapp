using System.Collections.Generic;

namespace simplebibleapp.xmlbible
{
    public interface INode
    {
        NodeType XmlNodeType { get; }
        string Text { get; }
        NodeBase Parent { get; }
        IEnumerable<NodeBase> SubNodes { get; }
        void AddSubNode(NodeBase node);
    }
}