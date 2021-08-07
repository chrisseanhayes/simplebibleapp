using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using simplebibleapp.xmldatacore;

namespace simplebibleapp.xmlbible
{
    public class BuilderState
    {
        public NodeBase CurrentNode { get; set; }
        public bool HasPrevChapter { get; set; }
        public string PrevBookAbbr { get; set; }
        public int PrevChapterNumber { get; set; }
        public bool HasNextChapter { get; set; }
        public string NextBookAbbr { get; set; }
        public int NextChapterNumber { get; set; }

        public BuilderState()
        {
            
        }

        internal BuilderState(
            NodeBase currentNode, 
            bool hasPrevChapter, 
            string prevBookAbbr, 
            int prevChapterNumber, 
            bool hasNextChapter, 
            string nextBookAbbr,
            int nextChapterNumber)
        {
            CurrentNode = currentNode;
            HasPrevChapter = hasPrevChapter;
            PrevBookAbbr = prevBookAbbr;
            PrevChapterNumber = prevChapterNumber;
            HasNextChapter = hasNextChapter;
            NextBookAbbr = nextBookAbbr;
            NextChapterNumber = nextChapterNumber;
        }

        internal BuilderState(ChapterBuilder builder):this(
            builder.CurrentNode,
            builder.HasPrevChapter,
            builder.PrevBookAbbr, 
            builder.PrevChapterNumber,
            builder.HasNextChapter, 
            builder.NextBookAbbr, 
            builder.NextChapterNumber)
        {
            
        }
    }
    public class ChapterBuilder : IChapterBuilder
    {
        #region private fields
        private readonly IEnumerable<IState> _states;
        private readonly IXmlPathResolver _pathResolver;
        private INodeState _nodeState;
        private NodeBase _currentNode;
        bool IsLive { get; set; }
        bool SearchComplete { get; set; }
        string SearchString { get; set; }
        #endregion

        #region public readonly properties
        public NodeBase CurrentNode => _currentNode;
        public bool HasPrevChapter { get; private set; }
        public string PrevBookAbbr { get; private set; }
        public int PrevChapterNumber { get; private set; }
        public bool HasNextChapter { get; private set; }
        public string NextBookAbbr { get; private set; }
        public int NextChapterNumber { get; private set; }
        #endregion

        public ChapterBuilder(IEnumerable<IState> states, IXmlPathResolver pathResolver)
        {
            _states = states;
            _pathResolver = pathResolver;
            ResetSearch();

        }

        public BuilderState GetChapter(string bookAbbreviation, int chapter)
        {
            ResetSearch();
            SearchString = GetSearchString(bookAbbreviation, chapter);
            var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Ignore };
            var filepath = Path.Combine(_pathResolver.GetPath(), "kjvfull.xml");
            using (var rdr =
                XmlReader.Create(filepath, settings))
            {
                rdr.MoveToContent();
                while (rdr.Read())
                {
                    SetNodeState(rdr);
                    _nodeState.HandleReader(rdr, this, _states);
                    if (SearchComplete) break;
                }
            }
            return new BuilderState(this);
        }

        private void ResetSearch()
        {
            IsLive = false;
            SearchComplete = false;
            HasPrevChapter = false;
            HasNextChapter = false;
            PrevBookAbbr = null;
            NextBookAbbr = null;
            PrevChapterNumber = 0;
            NextChapterNumber = 0;
            _nodeState = new NullState();
            _currentNode = new ChapterNode();
        }

        #region internal methods

        internal void CloseNode()
        {
            _currentNode.Parent.AddSubNode(_currentNode);
            if (!SearchComplete)
                _currentNode = _currentNode.Parent;
        }
        #endregion

        #region private methods
        private static string GetSearchString(string bookAbbr, int chapter)
        {
            return $"{bookAbbr}.{chapter}";
        }

        private bool IsChapterNode(XmlReader rdr)
        {
            return rdr.NodeType == XmlNodeType.Element &&
                rdr.Name == "chapter";
        }

        private void SetNodeState(XmlReader rdr)
        {
            if (!IsLive &&
                IsChapterNode(rdr) &&
                rdr.GetAttribute("osisID") == SearchString)
            {
                IsLive = true;
                return;
            }

            if (IsLive)
            {
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
            else
            {
                if (IsChapterNode(rdr))
                {
                    SetPreviousChapter(rdr);
                }
            }
        }

        private void SetPreviousChapter(XmlReader rdr)
        {
            HasPrevChapter = true;
            var osisId = rdr.GetAttribute("osisID");
            var osisIdParts = osisId.Split('.');
            PrevBookAbbr = osisIdParts.First();
            PrevChapterNumber = int.Parse(osisIdParts.Skip(1).First());
        }

        internal void SetNextChapter(string bookAbbr, int chapter)
        {
            HasNextChapter = true;
            NextBookAbbr = bookAbbr;
            NextChapterNumber = chapter;
        }

        private void AddNodeText(XmlReader reader)
        {
            _currentNode.AddSubNode(new TextNode(_currentNode, reader.Value));
        }

        private void SetCurrentNode(NodeBase newnode)
        {
            _currentNode = newnode;
        }
        #endregion

        #region node states
        interface INodeState
        {
            void HandleReader(XmlReader reader, ChapterBuilder builder, IEnumerable<IState> states);
        }

        class NullState : INodeState
        {
            public void HandleReader(XmlReader reader, ChapterBuilder builder, IEnumerable<IState> states)
            {
            }
        }

        class ElementState : INodeState
        {
            public void HandleReader(XmlReader reader, ChapterBuilder builder, IEnumerable<IState> states)
            {
                HandleLiveNode(reader, builder, states);
            }

            private static void HandleLiveNode(XmlReader reader, ChapterBuilder builder, IEnumerable<IState> states)
            {
                var newnode = states.First(s => s.TagName == reader.Name)?
                    .GetNewNode(reader, builder);
                if (newnode == null) throw new ApplicationException($"No state resolver found for node of type {reader.Name}");
                builder.SetCurrentNode(newnode);
                if (reader.IsEmptyElement)
                    builder.CloseNode();
            }
        }

        class TextState : INodeState
        {
            public void HandleReader(XmlReader reader, ChapterBuilder builder, IEnumerable<IState> states)
            {
                builder.AddNodeText(reader);
            }
        }

        class EndElementState : INodeState
        {
            public void HandleReader(XmlReader reader, ChapterBuilder builder, IEnumerable<IState> states)
            {
                if (reader.Name == "chapter")
                {
                    builder.SearchComplete = true;
                    while (reader.Read())
                    {
                        if (reader.IsStartElement() && reader.Name == "chapter")
                        {
                            var osisId = reader.GetAttribute("osisID");
                            var osisIdParts = osisId.Split('.');
                            var nextBookAbbr = osisIdParts.First();
                            var nextChapterNumber = int.Parse(osisIdParts.Skip(1).First());
                            builder.SetNextChapter(bookAbbr: nextBookAbbr, chapter: nextChapterNumber);
                            break;
                        }
                    }
                }

                builder.CloseNode();
            }
        }


        #endregion
    }
}
