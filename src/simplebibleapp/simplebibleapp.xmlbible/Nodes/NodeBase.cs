using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace simplebibleapp.xmlbible
{
    public abstract class NodeBase : INode
    {
        public abstract NodeType XmlNodeType { get; }
        [JsonIgnore]
        public NodeBase Parent { get; }
        private readonly List<NodeBase> _subNodes = new List<NodeBase>();
        public IEnumerable<NodeBase> SubNodes => _subNodes;

        public virtual string Text => string.Join(string.Empty, SubNodes.Select(n => n.Text));

        public static NodeBase NullNode { get; } = new NullTerminatedNode(NullNode);

        class NullTerminatedNode : NodeBase
        {
            public override NodeType XmlNodeType { get; } = NodeType.Null;
            public NullTerminatedNode(NodeBase parent) : base(parent)
            {

            }

            public override void AddSubNode(NodeBase node)
            {
                _subNodes.Add(node);
            }
        }

        protected NodeBase(NodeBase parent)
        {
            Parent = parent;
        }

        public virtual void AddSubNode(NodeBase node)
        {
            _subNodes.Add(node);
        }
    }
}