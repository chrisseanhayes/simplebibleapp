using System;
using System.Collections.Generic;
using System.Xml;

namespace simplebibleapp.xmlbiblerepository
{
    public class BibleChapterBuilder
    {
        private int _currentVersePartIndex = 0;
        private readonly Chapter _chapter = new Chapter();
        private IVersePartState _versePartState;
        private static readonly Dictionary<VersePartStateType, Func<int, IVersePartState>> _versePartStateFactory =
            new Dictionary<VersePartStateType, Func<int, IVersePartState>>
            {
                {VersePartStateType.Word,
                    i=> new WordVersePartState(i) },
                {VersePartStateType.Whitespace,
                    i=> new TextVersePartState(i) },
                {VersePartStateType.Text,
                    i=> new TextVersePartState(i) },
                {VersePartStateType.TransChange,
                    i=> new TransChangePartState(i) },
                {VersePartStateType.Milestone,
                    i=> new MilestoneVersePartState(i) },
                {VersePartStateType.Note, i => new NotePartState(i) }
            };
        private Verse _currentVerse;
        private bool _partStateOpen;
        private bool _verseOpen;
        public bool ChapterOpen { get; private set; }
        public bool BuilderClosed { get; private set; }

        public BibleReading GetBibleReading()
        {
            if (BuilderClosed) return new BibleReading(new[] { _chapter });
            throw new ApplicationException("The builder must be closed before you can access the data");
        }

        public void AddOsisId(string readerValue)
        {
            _chapter.AddOsisId(readerValue);
        }

        public void AddChapterTitle(string readerValue)
        {
            _chapter.AddTitle(readerValue);
        }

        public void StartNewVerse(string sID, string osisID)
        {
            _verseOpen = true;
            _currentVersePartIndex = 0;
            _currentVerse = new Verse(sID, osisID);
        }

        public void CloseCurrentVerse()
        {
            _verseOpen = false;
            _chapter.AddVerse(_currentVerse);
        }

        public void OpenChapter()
        {
            ChapterOpen = true;
        }

        public void CloseBuilder()
        {
            BuilderClosed = true;
        }

        public void AddText(string text)
        {
            if (_verseOpen)
            {
                if (_partStateOpen)
                    _versePartState.AddText(text);
                else
                {
                    _currentVersePartIndex++;
                    _versePartState = _versePartStateFactory[VersePartStateType.Text](_currentVersePartIndex);
                    _versePartState.AddText(text);
                    _currentVerse.AddPart(_versePartState.GetPart());
                }
            }
            if (TitleOpen)
            {
                _chapter.AddTitle(text);
            }
        }

        public void StartNewPart(VersePartStateType state, XmlReader reader)
        {
            _partStateOpen = true;
            _currentVersePartIndex++;
            _versePartState = _versePartStateFactory[state](_currentVersePartIndex);
            _versePartState.SetReaderProps(reader);
        }

        public void ClosePart()
        {
            _partStateOpen = false;
            _currentVerse.AddPart(_versePartState.GetPart());
        }

        public void AddTitle(string readerValue)
        {
            _chapter.AddTitleAttribute(readerValue);
        }

        public void OpenTitle()
        {
            TitleOpen = true;
        }

        public bool TitleOpen { get; private set; }

        public void CloseTitle()
        {
            TitleOpen = false;
        }

        public void InsertSubPart(VerseWordSubpartType type, XmlReader reader)
        {
            _versePartState.StartSubPart(type, reader);
        }

        public void EndSubPart(VerseWordSubpartType type)
        {
            _versePartState.EndSubPart(type);
        }
    }
}