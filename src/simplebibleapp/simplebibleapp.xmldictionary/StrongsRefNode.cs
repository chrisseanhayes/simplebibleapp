namespace simplebibleapp.xmldictionary
{
    public class StrongsRefNode : GreekDefinitionNodeBase
    {
        public string Language { get; }
        public string Strongs { get; }

        public StrongsRefNode(GreekDefinitionNodeBase parent, string language, string strongs) : base(parent)
        {
            Language = language;
            Strongs = strongs;
        }

        public override GreekDefinitionNodeType Type => GreekDefinitionNodeType.StrongsRef;

        public override string ToString()
        {
            return $"Type: {Type}, Language: {Language}, Strongs: {Strongs}";
        }
    }
}