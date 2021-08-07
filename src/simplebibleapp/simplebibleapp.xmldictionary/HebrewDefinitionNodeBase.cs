using System;
using System.Collections.Generic;
using System.Linq;

namespace simplebibleapp.xmldictionary
{
    public abstract class HebrewDefinitionNodeBase : IHebrewDefinitionNode
    {
        private readonly List<HebrewDefinitionNodeBase> _subnodes = new List<HebrewDefinitionNodeBase>();

        public virtual string Text =>
            _subnodes.Any() ? string.Join(string.Empty, _subnodes.Select(n => n.Text)) : string.Empty;
        public virtual IEnumerable<HebrewDefinitionNodeBase> SubNodes => _subnodes;
        public HebrewDefinitionNodeBase Parent { get; private set; }
        public bool IsOpen { get; private set; }
        public static HebrewDefinitionNodeBase NullNode { get; } = new NullHebrewDefinitionNode();

        class NullHebrewDefinitionNode : HebrewDefinitionNodeBase
        {
            public override string Text => String.Empty;

            public NullHebrewDefinitionNode() : base(null)
            {
                Parent = this;
            }

            public override void AddNodeText(string readerValue)
            {
            }

            public override void AddSubNode(HebrewDefinitionNodeBase node)
            {
            }
        }

        public void CloseNode()
        {
            IsOpen = false;
        }

        public virtual void AddNodeText(string readerValue)
        {
            _subnodes.Add(new TextDefinitionNode(this, readerValue));
        }

        public virtual void AddSubNode(HebrewDefinitionNodeBase node)
        {
            _subnodes.Add(node);
        }

        protected HebrewDefinitionNodeBase(HebrewDefinitionNodeBase parent)
        {
            IsOpen = true;
            Parent = parent;
        }
    }
}