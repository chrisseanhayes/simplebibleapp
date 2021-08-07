using System.Collections.Generic;
using simplebibleapp.xmldatacore;

namespace simplebibleapp.xmldictionary
{
    public class DictionaryRepository : IDictionaryRepository
    {
        private readonly IEnumerable<IGreekDefinitionState> _greekStates;
        private readonly IEnumerable<IHebrewDefinitionState> _hebrewStates;
        private readonly IXmlPathResolver _pathResolver;

        public DictionaryRepository(
            IEnumerable<IGreekDefinitionState> states,
            IEnumerable<IHebrewDefinitionState> hebrewStates,
            IXmlPathResolver pathResolver)
        {
            _greekStates = states;
            _hebrewStates = hebrewStates;
            _pathResolver = pathResolver;
        }
        public IGreekDefinitionNode GetGreekDefinition(int number)
        {
            var builder = new GreekDefinitionBuilder(_greekStates, _pathResolver);
            return builder.GetDefinition(number);
        }

        public IHebrewDefinitionNode GetHebrewDefinition(int number)
        {
            var builder = new HebrewDefinitionBuilder(_hebrewStates, _pathResolver);
            return builder.GetDefinition(number);
        }
    }
}
