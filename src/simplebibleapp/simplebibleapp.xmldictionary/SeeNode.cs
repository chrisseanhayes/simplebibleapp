namespace simplebibleapp.xmldictionary
{
    public class SeeNode : GreekDefinitionNodeBase
    {
        public string Language { get; }
        public string Strongs { get; }

        public SeeNode(GreekDefinitionNodeBase parent, string language, string strongs) : base(parent)
        {
            Language = language;
            Strongs = strongs;
        }

        public override GreekDefinitionNodeType Type => GreekDefinitionNodeType.See;

        public override string ToString()
        {
            return $"Type: {Type}, Language: {Language}, Strongs: {Strongs}";
        }
    }
}