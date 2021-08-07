namespace simplebibleapp.xmldictionary
{
    public class PronunciationNode : GreekDefinitionNodeBase
    {
        public string Strongs { get; }

        public PronunciationNode(GreekDefinitionNodeBase parent, string strongs) : base(parent)
        {
            Strongs = strongs;
        }

        public override GreekDefinitionNodeType Type => GreekDefinitionNodeType.Pronunciation;
        public override string ToString()
        {
            return $"Type: {Type}, Strongs; {Strongs}, Text: {Text}";
        }
    }
}