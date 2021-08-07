using System.Xml;

namespace simplebibleapp.xmlbiblerepository
{
    public abstract class VersePartStateBase : IVersePartState
    {
        public int Index { get; }

        protected VersePartStateBase(int index)
        {
            Index = index;
        }

        public abstract void SetReaderProps(XmlReader reader);

        public abstract void AddText(string text);

        public abstract IVersePart GetPart();
        public virtual void StartSubPart(VerseWordSubpartType seg, XmlReader reader)
        {
        }
        public virtual void EndSubPart(VerseWordSubpartType seg)
        {
        }
    }
}