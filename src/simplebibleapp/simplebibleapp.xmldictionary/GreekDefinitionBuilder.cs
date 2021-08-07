using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using simplebibleapp.xmldatacore;

namespace simplebibleapp.xmldictionary
{
    public class GreekDefinitionBuilder
    {
        #region private fields
        private readonly IEnumerable<IGreekDefinitionState> _states;
        private readonly IXmlPathResolver _pathResolver;
        private IGreekDefinitionNodeState _nodeState;
        private GreekDefinitionNodeBase _currentNode;
        #endregion

        #region public readonly properties
        public GreekDefinitionNodeBase CurrentNode => _currentNode;
        public string SearchNumber { get; private set; }
        public bool IsLive { get; private set; }
        public bool DefinitionComplete { get; private set; }
        #endregion

        public GreekDefinitionBuilder(IEnumerable<IGreekDefinitionState> states, IXmlPathResolver pathResolver)
        {
            _states = states;
            _pathResolver = pathResolver;
            _nodeState = new NullState();
            _currentNode = new ElementNode();
        }

        public IGreekDefinitionNode GetDefinition(int number)
        {
            SearchNumber = GetSearchNumber(number);
            var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Parse };
            var filepath = Path.Combine(_pathResolver.GetPath(), "strongsgreek.xml");
            using (var rdr =
                XmlReader.Create(filepath, settings))
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

        #region internal methods

        internal void CloseNode()
        {
            _currentNode.Parent.AddSubNode(_currentNode);
            if (!DefinitionComplete)
                _currentNode = _currentNode.Parent;
        }
        #endregion

        #region private methods
        private static string GetSearchNumber(int number)
        {
            return (number + 100000).ToString().Substring(1, 5);
        }

        private void SetNodeState(XmlReader rdr)
        {
            if (!IsLive &&
                rdr.NodeType == XmlNodeType.Element &&
                rdr.Name == "entry" &&
                rdr.GetAttribute("strongs") == SearchNumber)
            {
                IsLive = true;
                return;
            }

            if (!IsLive) return;

            switch (rdr.NodeType)
            {
                case XmlNodeType.Element:
                    _nodeState = new ElementState();
                    break;
                case XmlNodeType.EndElement:
                    _nodeState = new EndElementState();
                    break;
                case XmlNodeType.Text:
                case XmlNodeType.Whitespace:
                    _nodeState = new TextState();
                    break;
            }
        }

        private void AddNodeText(XmlReader reader)
        {
            _currentNode.AddSubNode(new TextNode(_currentNode, reader.Value));
        }

        private void SetCurrentNode(GreekDefinitionNodeBase newnode)
        {
            _currentNode = newnode;
        }
        #endregion

        #region node states
        interface IGreekDefinitionNodeState
        {
            void HandleReader(XmlReader reader, GreekDefinitionBuilder builder, IEnumerable<IGreekDefinitionState> states);
        }

        class NullState : IGreekDefinitionNodeState
        {
            public void HandleReader(XmlReader reader, GreekDefinitionBuilder builder, IEnumerable<IGreekDefinitionState> states)
            {
            }
        }

        class ElementState : IGreekDefinitionNodeState
        {
            public void HandleReader(XmlReader reader, GreekDefinitionBuilder builder, IEnumerable<IGreekDefinitionState> states)
            {
                HandleLiveNode(reader, builder, states);
            }

            private static void HandleLiveNode(XmlReader reader, GreekDefinitionBuilder builder, IEnumerable<IGreekDefinitionState> states)
            {
                var newnode = states.First(s => s.TagName == reader.Name)?
                    .GetNewNode(reader, builder);
                if (newnode == null) throw new ApplicationException($"No state resolver found for node of type {reader.Name}");
                builder.SetCurrentNode(newnode);
                if (reader.IsEmptyElement)
                    builder.CloseNode();
            }
        }

        class TextState : IGreekDefinitionNodeState
        {
            public void HandleReader(XmlReader reader, GreekDefinitionBuilder builder, IEnumerable<IGreekDefinitionState> states)
            {
                builder.AddNodeText(reader);
            }
        }

        class EndElementState : IGreekDefinitionNodeState
        {
            public void HandleReader(XmlReader reader, GreekDefinitionBuilder builder, IEnumerable<IGreekDefinitionState> states)
            {
                if (reader.Name == "entry")
                    builder.DefinitionComplete = true;
                builder.CloseNode();
            }
        }
        #endregion
    }
}