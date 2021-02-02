using Catalyst.ConceptNet;
using Mosaik.Core;
using System;
using System.Collections.Generic;
using System.Linq;


namespace Catalyst
{
    public static class ConceptNetGraph
    {
        public static IEnumerable<(string Word, PartOfSpeech PartOfSpeech, float Weight)> Get(string word, Language documentLanguage, Language targetLanguage, ConceptNetRelation relationType, PartOfSpeech partOfSpeech = PartOfSpeech.NOUN, bool includeMissingPartOfSpeech = false, bool doNotThrow = false)
        {
            return Get(word.AsSpan(), documentLanguage, targetLanguage, relationType, partOfSpeech, includeMissingPartOfSpeech, doNotThrow);
        }

        public static IEnumerable<(string Word, PartOfSpeech PartOfSpeech, float Weight)> Get(ReadOnlySpan<char> word, Language documentLanguage, Language targetLanguage, ConceptNetRelation relationType, PartOfSpeech partOfSpeech = PartOfSpeech.NOUN, bool includeMissingPartOfSpeech = false, bool doNotThrow = false)
        {
            if (Loader.TryGetWordsCache(targetLanguage, out var words))
            {
                if (Loader.TryGetEdgesData(documentLanguage, targetLanguage, out var edgesData))
                {
                    var wHash = Loader.HashWordUnderscoreIsSpace(word, partOfSpeech);
                    var wEdges = edgesData.GetEdges(relationType, wHash);

                    ReadOnlySpan<ConceptNetEdge> xEdges;

                    if (includeMissingPartOfSpeech)
                    {
                        var xHash = Loader.HashWordUnderscoreIsSpace(word, PartOfSpeech.X);
                        xEdges = edgesData.GetEdges(relationType, xHash);
                    }
                    else
                    {
                        xEdges = ReadOnlySpan<ConceptNetEdge>.Empty;
                    }

                    if (wEdges.Length > 0 || xEdges.Length > 0)
                    {
                        var result = new List<(string Word, PartOfSpeech PartOfSpeech, float Weight)>();

                        foreach (var edge in wEdges)
                        {
                            if (words.TryGetWord(edge.To, out var w, out var pos))
                            {
                                result.Add((w, pos, edge.Weight / 100f));
                            }
                        }

                        foreach (var edge in xEdges)
                        {
                            if (words.TryGetWord(edge.To, out var w, out var pos))
                            {
                                result.Add((w, pos, edge.Weight / 100f));
                            }
                        }

                        return result;
                    }
                    return Enumerable.Empty<(string, PartOfSpeech, float)>();
                }
                else
                {
                    if (!doNotThrow) return Enumerable.Empty<(string, PartOfSpeech, float)>();

                    throw new Exception($"The data package for the language {documentLanguage} was not found. Did you install the correct NuGet Package: (https://www.nuget.org/packages/Catalyst.ConceptNet.{documentLanguage}) ? If the package is installed and loaded, then the language pair might not exist in it.");
                }
            }
            else
            {
                if (!doNotThrow) return Enumerable.Empty<(string, PartOfSpeech, float)>();

                throw new Exception($"The data package for the language {targetLanguage} was not found. Did you install the correct NuGet Package: (https://www.nuget.org/packages/Catalyst.ConceptNet.{targetLanguage}) ?");
            }
        }
    }
}
