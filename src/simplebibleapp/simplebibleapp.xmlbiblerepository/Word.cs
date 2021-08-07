using System;
using System.Collections.Generic;
using System.Linq;

namespace simplebibleapp.xmlbiblerepository
{
    public class Word : VersePartBase
    {
        private string _lemma;
        public string Lemma => _lemma;
        private string _morph;
        public string Morph => _morph;
        public bool InSeg { get; private set; }
        public override VersePartType Type => VersePartType.Word;
        private readonly List<WordTextPart> _textParts = new List<WordTextPart>();
        public IEnumerable<WordTextPart> Parts => _textParts;
        public bool HasLemma => _lemma != null;
        public bool HasMorph => _morph != null;
        public bool HasText => _textParts.Any();
        public override string Text => String.Join(string.Empty, _textParts.Select(t => t.Value));

        public void AddLemma(string readerValue)
        {
            _lemma = readerValue;
        }

        public void AddText(string readerValue)
        {
            if (InSeg && InDivineName)
                _textParts.Add(new WordTextPart(VerseWordSubpartType.DivineName, readerValue));
            else
                _textParts.Add(new WordTextPart(VerseWordSubpartType.Text, readerValue));
        }

        public string DivineName { get; private set; }

        public bool InDivineName { get; private set; }

        public void AddMorph(string readerValue)
        {
            _morph = readerValue;
        }

        public Word(int sortOrder) : base(sortOrder)
        {
        }

        public void StartSeg()
        {
            InSeg = true;
        }

        public void EndSeg()
        {
            InSeg = false;
        }

        public void StartDivineName()
        {
            InDivineName = true;
        }

        public void EndDivineName()
        {
            InDivineName = false;
        }
    }
}