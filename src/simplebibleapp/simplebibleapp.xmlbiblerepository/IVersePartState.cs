using System.Xml;

namespace simplebibleapp.xmlbiblerepository
{
    public interface IVersePartState
    {
        int Index { get; }
        void SetReaderProps(XmlReader reader);
        void AddText(string text);
        IVersePart GetPart();
        void StartSubPart(VerseWordSubpartType seg, XmlReader reader);
        void EndSubPart(VerseWordSubpartType seg);
    }
}