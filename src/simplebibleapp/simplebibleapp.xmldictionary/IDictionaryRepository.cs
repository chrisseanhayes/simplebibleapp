namespace simplebibleapp.xmldictionary
{
    public interface IDictionaryRepository
    {
        IGreekDefinitionNode GetGreekDefinition(int number);
        IHebrewDefinitionNode GetHebrewDefinition(int number);
    }
}