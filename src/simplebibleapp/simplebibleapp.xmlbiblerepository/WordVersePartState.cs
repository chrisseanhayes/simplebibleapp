using System.Xml;

namespace simplebibleapp.xmlbiblerepository
{
    public class WordVersePartState : VersePartStateBase
    {
        private readonly Word _word;

        public WordVersePartState(int index) : base(index)
        {
            _word = new Word(index);
        }

        public override void SetReaderProps(XmlReader reader)
        {
            if (reader.MoveToAttribute("lemma"))
            {
                _word.AddLemma(reader.Value);
                reader.MoveToElement();
            }
            if (reader.MoveToAttribute("morph"))
            {
                _word.AddMorph(reader.Value);
                reader.MoveToElement();
            }
        }

        public override void AddText(string text)
        {
            _word.AddText(text);
        }

        public override IVersePart GetPart()
        {
            return _word;
        }

        public override void StartSubPart(VerseWordSubpartType type, XmlReader reader)
        {
            if (type == VerseWordSubpartType.Seg)
                _word.StartSeg();
            if (type == VerseWordSubpartType.DivineName)
                _word.StartDivineName();
        }

        public override void EndSubPart(VerseWordSubpartType type)
        {
            if (type == VerseWordSubpartType.Seg)
                _word.EndSeg();
            if (type == VerseWordSubpartType.DivineName)
                _word.EndDivineName();
        }
    }
}