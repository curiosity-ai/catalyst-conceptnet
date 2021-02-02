using Catalyst.ConceptNet;
using Mosaik.Core;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UID;


namespace Catalyst
{
    public static class ConceptNetExtensions
    {
        public static IEnumerable<(string Word, PartOfSpeech PartOfSpeech)> ConceptNet(this IToken token, Language language, ConceptNetRelation relationType)
        {
            return ConceptNet(token, language, language, relationType);
        }

        public static IEnumerable<(string Word, PartOfSpeech PartOfSpeech)> ConceptNet(this IToken token, Language documentLanguage, Language targetLanguage, ConceptNetRelation relationType)
        {
            return ConceptNetGraph.Get(token.ValueAsSpan, documentLanguage, targetLanguage, relationType, token.POS);
        }
    }
}
