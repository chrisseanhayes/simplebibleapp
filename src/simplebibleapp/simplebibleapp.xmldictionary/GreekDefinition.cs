using System.Collections.Generic;

namespace simplebibleapp.xmldictionary
{
    public class GreekDefinition
    {
        private List<IGreekDefinitionNode> _nodes = new List<IGreekDefinitionNode>();
        public IEnumerable<IGreekDefinitionNode> Nodes => _nodes;

        public void AddNode(IGreekDefinitionNode node)
        {
            _nodes.Add(node);
        }
    }
}