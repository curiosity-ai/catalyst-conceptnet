using Catalyst.ConceptNet;
using Mosaik.Core;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
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
            if(Loader.TryGetWordsCache(targetLanguage, out var words))
            {
                if (Loader.TryGetEdgesData(documentLanguage, targetLanguage, out var edgesData))
                {
                    var wHash = Loader.HashWordUnderscoreIsSpace(token.ValueAsSpan, token.POS);
                    var xHash = Loader.HashWordUnderscoreIsSpace(token.ValueAsSpan, PartOfSpeech.X);

                    var wEdges = edgesData.GetEdges(relationType, wHash);
                    var xEdges = edgesData.GetEdges(relationType, xHash);

                    if (wEdges.Length > 0 || xEdges.Length > 0)
                    {
                        var result = new List<(string Word, PartOfSpeech PartOfSpeech)>();

                        foreach (var edge in wEdges)
                        {
                            if (words.TryGetWord(edge.To, out var word, out var pos))
                            {
                                result.Add((word, pos));
                            }
                        }

                        foreach (var edge in xEdges)
                        {
                            if (words.TryGetWord(edge.To, out var word, out var pos))
                            {
                                result.Add((word, pos));
                            }
                        }
                        return result;
                    }
                    return Enumerable.Empty<(string, PartOfSpeech)>();
                }
                else
                {
                    throw new Exception($"The data package for the language {documentLanguage} was not found. Did you install the correct NuGet Package: (https://www.nuget.org/packages/Catalyst.ConceptNet.{documentLanguage}) ? If the package is installed and loaded, then the language pair might not exist in it.");
                }
            }
            else
            {
                throw new Exception($"The data package for the language {targetLanguage} was not found. Did you install the correct NuGet Package: (https://www.nuget.org/packages/Catalyst.ConceptNet.{targetLanguage}) ?");
            }
        }
    }
}
