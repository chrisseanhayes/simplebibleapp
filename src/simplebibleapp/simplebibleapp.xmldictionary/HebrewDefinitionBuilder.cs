using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using simplebibleapp.xmldatacore;

namespace simplebibleapp.xmldictionary
{
    public class HebrewDefinitionBuilder
    {
        #region private fields
        private readonly IEnumerable<IHebrewDefinitionState> _states;
        private readonly IXmlPathResolver _pathResolver;
        private IHebrewDefinitionNodeState _nodeState;
        private HebrewDefinitionNodeBase _currentNode;
        internal HebrewDefinitionNodeBase CurrentNode => _currentNode;
        #endregion

        #region public readonly properties
        public string SearchNumber { get; private set; }
        public bool IsLive { get; private set; }
        public bool DefinitionComplete { get; private set; }
        #endregion

        public HebrewDefinitionBuilder(IEnumerable<IHebrewDefinitionState> states, IXmlPathResolver pathResolver)
        {
            _states = states;
            _pathResolver = pathResolver;
            _currentNode = new ElementHebrewDefinitionNode();
            _nodeState = _nodestates["null"];
        }

        public HebrewDefinitionNodeBase GetDefinition(int number)
        {
            SearchNumber = GetSearchNumber(number);
            using (var rdr = XmlReader.Create(Path.Combine(_pathResolver.GetPath(), "HebrewStrong.xml")))
            {
                rdr.MoveToContent();
                while (rdr.Read())
                {
                    SetNodeState(rdr);
                    _nodeState.HandleReader(rdr, this, _states);
                    if (DefinitionComplete) break;
                }
            }
            return _currentNode;
        }

        #region private methods
        private static string GetSearchNumber(int number)
        {
            return "H" + number;
        }

        private void SetNodeState(XmlReader rdr)
        {
            if (!IsLive &&
                rdr.NodeType == XmlNodeType.Element &&
                rdr.Name == "entry" &&
                rdr.GetAttribute("id") == SearchNumber)
            {
                IsLive = true;
                return;
            }

            if (!IsLive) return;

            switch (rdr.NodeType)
            {
                case XmlNodeType.Element:
                    _nodeState = _nodestates["element"];
                    break;
                case XmlNodeType.EndElement:
                    _nodeState = _nodestates["end"];
                    break;
                case XmlNodeType.Whitespace:
                case XmlNodeType.Text:
                    _nodeState = _nodestates["text"];
                    break;
            }
        }

        private static readonly Dictionary<string, IHebrewDefinitionNodeState> _nodestates = new Dictionary<string, IHebrewDefinitionNodeState>
        {
            {"null",new NullState() },
            {"element",new ElementState() },
            {"text",new TextState() },
            {"end",new EndElementState()},
        };

        private void SetCurrentNode(HebrewDefinitionNodeBase newnode)
        {
            _currentNode = newnode;
        }
        #endregion

        #region node states
        interface IHebrewDefinitionNodeState
        {
            void HandleReader(XmlReader reader, HebrewDefinitionBuilder builder, IEnumerable<IHebrewDefinitionState> states);
        }

        class NullState : IHebrewDefinitionNodeState
        {
            public void HandleReader(XmlReader reader, HebrewDefinitionBuilder builder, IEnumerable<IHebrewDefinitionState> states)
            {
            }
        }

        class ElementState : IHebrewDefinitionNodeState
        {
            public void HandleReader(XmlReader reader, HebrewDefinitionBuilder builder,
                IEnumerable<IHebrewDefinitionState> states)
            {
                var node = states.FirstOrDefault(
                        s => s.TagName == reader.Name &&
                             s.Predicate(reader))?
                    .GetNewNode(reader, builder);
                if (node == null) throw new ApplicationException($"No state processor found for hebrew definitions {reader.Name} with matching predicate.");
                if (builder.CurrentNode.IsOpen)
                {
                    builder.CurrentNode.AddSubNode(node);
                    if (!reader.IsEmptyElement)
                        builder.SetCurrentNode(node);
                }
                else
                {
                    builder.SetCurrentNode(node);
                }
            }

        }

        class TextState : IHebrewDefinitionNodeState
        {
            public void HandleReader(XmlReader reader, HebrewDefinitionBuilder builder, IEnumerable<IHebrewDefinitionState> states)
            {
                builder.CurrentNode.AddNodeText(reader.Value);
            }
        }

        class EndElementState : IHebrewDefinitionNodeState
        {
            public void HandleReader(XmlReader reader, HebrewDefinitionBuilder builder, IEnumerable<IHebrewDefinitionState> states)
            {
                builder.CurrentNode.CloseNode();
                if (reader.Name == "entry")
                {
                    builder.DefinitionComplete = true;
                    return;
                }
                builder.SetCurrentNode(builder.CurrentNode.Parent);
            }
        }
        #endregion
    }
}