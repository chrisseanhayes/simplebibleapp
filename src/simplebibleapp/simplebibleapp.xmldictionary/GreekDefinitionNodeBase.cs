using System.Collections.Generic;
using System.Linq;

namespace simplebibleapp.xmldictionary
{
    public abstract class GreekDefinitionNodeBase : IGreekDefinitionNode
    {
        private int _currentIndex;
        private int _sortOrder;
        public virtual int SortOrder => _sortOrder;
        public GreekDefinitionNodeBase Parent { get; }
        private readonly List<IGreekDefinitionNode> _subNodes = new List<IGreekDefinitionNode>();
        public IEnumerable<IGreekDefinitionNode> SubNodes => _subNodes;
        public abstract GreekDefinitionNodeType Type { get; }
        public virtual string Text => string.Join(string.Empty, SubNodes.Select(n => n.Text));


        public static GreekDefinitionNodeBase NullNode { get; } = new NullDefinitionNode(NullNode);

        class NullDefinitionNode : GreekDefinitionNodeBase
        {
            public NullDefinitionNode(GreekDefinitionNodeBase parent) : base(parent)
            {
            }

            public override GreekDefinitionNodeType Type => GreekDefinitionNodeType.Null;
            public override string Text => string.Empty;
            public override int SortOrder => 0;
            public override void AddSubNode(GreekDefinitionNodeBase node)
            {
            }
        }

        protected GreekDefinitionNodeBase(GreekDefinitionNodeBase parent)
        {
            Parent = parent;
        }

        public virtual void AddSubNode(GreekDefinitionNodeBase node)
        {
            node.AddIndex(_currentIndex++);
            _subNodes.Add(node);
        }

        public void AddIndex(int index)
        {
            _sortOrder = index;
        }
    }
}