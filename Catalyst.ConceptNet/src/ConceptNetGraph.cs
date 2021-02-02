using Catalyst.ConceptNet;
using Mosaik.Core;
using System;
using System.Collections.Generic;
using System.Linq;


namespace Catalyst
{
    public static class ConceptNetGraph
    {
        public static IEnumerable<(string Word, PartOfSpeech PartOfSpeech)> Get(string word, Language documentLanguage, Language targetLanguage, ConceptNetRelation relationType, PartOfSpeech partOfSpeech = PartOfSpeech.NOUN)
        {
            return Get(word.AsSpan(), documentLanguage, targetLanguage, relationType, partOfSpeech);
        }

        public static IEnumerable<(string Word, PartOfSpeech PartOfSpeech)> Get(ReadOnlySpan<char> word, Language documentLanguage, Language targetLanguage, ConceptNetRelation relationType, PartOfSpeech partOfSpeech = PartOfSpeech.NOUN)
        {
            if(Loader.TryGetWordsCache(targetLanguage, out var words))
            {
                if (Loader.TryGetEdgesData(documentLanguage, targetLanguage, out var edgesData))
                {
                    var wHash = Loader.HashWordUnderscoreIsSpace(word, partOfSpeech);
                    var xHash = Loader.HashWordUnderscoreIsSpace(word, PartOfSpeech.X);

                    var wEdges = edgesData.GetEdges(relationType, wHash);
                    var xEdges = edgesData.GetEdges(relationType, xHash);

                    if (wEdges.Length > 0 || xEdges.Length > 0)
                    {
                        var result = new List<(string Word, PartOfSpeech PartOfSpeech)>();

                        foreach (var edge in wEdges)
                        {
                            if (words.TryGetWord(edge.To, out var w, out var pos))
                            {
                                result.Add((w, pos));
                            }
                        }

                        foreach (var edge in xEdges)
                        {
                            if (words.TryGetWord(edge.To, out var w, out var pos))
                            {
                                result.Add((w, pos));
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
