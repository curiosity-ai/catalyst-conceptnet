using MessagePack;
using Mosaik.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UID;

namespace Catalyst.ConceptNet.Prepare
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.InputEncoding = Encoding.UTF8;
            System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
            System.Globalization.CultureInfo.DefaultThreadCurrentCulture = System.Globalization.CultureInfo.InvariantCulture;

            var source = args[0].Trim('"');
            var resourcesPath = args[1].Trim('"');

            var words     = new Dictionary<Language, Dictionary<ulong, (int start, byte length, byte pos)>>();
            var wordCache = new Dictionary<Language, StringBuilder>();

            var relationships = new Dictionary<(Language from, Language to, ConceptNetRelation relationType), Dictionary<ulong, ConceptNetEdge[]>>();
            
            var totalLines = 34074917;
            var readLines = 0;

            (ulong hash, int start, byte length) AddToCache(ReadOnlySpan<char> word, PartOfSpeech pos, Language language)
            {
                var hash = Loader.HashWordUnderscoreIsSpace(word, pos);

                if (!words.TryGetValue(language, out var wordsForLang))
                {
                    wordsForLang = new Dictionary<ulong, (int start, byte length, byte pos)>();
                    words[language] = wordsForLang;
                }

                if (wordsForLang.TryGetValue(hash, out var info))
                {
                    return (hash, info.start, info.length);
                }
                else
                {
                    if (!wordCache.TryGetValue(language, out var cacheForLang))
                    {
                        cacheForLang = new StringBuilder();
                        wordCache[language] = cacheForLang;
                    }
                    var start = cacheForLang.Length;
                    foreach(var c in word)
                    {
                        if (c == '_')
                        {
                            cacheForLang.Append(' ');
                        }
                        else
                        {
                            cacheForLang.Append(c);
                        }
                    }
                    var len = (byte)(cacheForLang.Length - start);
                    wordsForLang[hash] = (start, len, (byte)pos);
                    return (hash, start, len);
                }
            }

            using(var f = File.OpenRead(source))
            using(var sr = new StreamReader(f))
            {
                while (!sr.EndOfStream)
                {
                    /*
                     *  /a/[/r/Antonym/,/c/ang/niht/n/,/c/ang/dæg/]     /r/Antonym      /c/ang/niht/n   /c/ang/dæg      {"dataset": "/d/wiktionary/en", "license": "cc:by-sa/4.0", "sources": [{"contributor": "/s/resource/wiktionary/en", "process": "/s/process/wikiparsec/2"}], "weight": 1.0}
                     *  /a/[/r/Antonym/,/c/pt/desrespeitar/v/,/c/pt/acatar/]    /r/Antonym      /c/pt/desrespeitar/v    /c/pt/acatar    {"dataset": "/d/wiktionary/en", "license": "cc:by-sa/4.0", "sources": [{"contributor": "/s/resource/wiktionary/en", "process": "/s/process/wikiparsec/2"}], "weight": 1.0}
                     *  /a/[/r/ExternalURL/,/c/da/synonym/,/http://da.dbpedia.org/resource/Synonym/]    /r/ExternalURL  /c/da/synonym   http://da.dbpedia.org/resource/Synonym  {"dataset": "/d/dbpedia/en", "license": "cc:by-sa/4.0", "sources": [{"contributor": "/s/resource/dbpedia/2015/en"}], "weight": 1.0}
                     *  /a/[/r/Synonym/,/c/aa/alle/v/,/c/fr/ale/]       /r/Synonym      /c/aa/alle/v    /c/fr/ale       {"dataset": "/d/wiktionary/fr", "license": "cc:by-sa/4.0", "sources": [{"contributor": "/s/resource/wiktionary/fr", "process": "/s/process/wikiparsec/2"}], "weight": 1.0}
                     *  /a/[/r/DerivedFrom/,/c/en/2,2,2_trifluoroethyl_vinyl/,/c/en/vinyl/n/]	/r/DerivedFrom	/c/en/2,2,2_trifluoroethyl_vinyl	/c/en/vinyl/n	{"dataset": "/d/wiktionary/en", "license": "cc:by-sa/4.0", "sources": [{"contributor": "/s/resource/wiktionary/en", "process": "/s/process/wikiparsec/2"}], "weight": 1.0}
                     *  The five fields of each line are:
                     * - The URI of the whole edge
                     *  - The relation expressed by the edge
                     *  - The node at the start of the edge
                     *  - The node at the end of the edge
                     *  - A JSON structure of additional information about the edge, such as its weight
                     * 
                     */

                    var lineSpan = sr.ReadLine().AsSpan();


                    readLines++;

                    var parts = lineSpan.Split('\t');
                    parts.MoveNext();
                    parts.MoveNext(); var edgeType = lineSpan.Slice(parts.Current.Start.Value + 3, parts.Current.End.Value - parts.Current.Start.Value - 3);
                    parts.MoveNext(); var from     = lineSpan.Slice(parts.Current.Start.Value, parts.Current.End.Value - parts.Current.Start.Value);
                    parts.MoveNext(); var to       = lineSpan.Slice(parts.Current.Start.Value, parts.Current.End.Value - parts.Current.Start.Value);

                    if (from[1] == 'c' && to[1] == 'c' &&  TryGetLang(from.Slice(3, 3), out var fromLangLen, out var fromLang) && TryGetLang(to.Slice(3, 3), out var toLangLen, out var toLang))
                    {
                        var fromWord = from.Slice(4 + fromLangLen);
                        var ixf      = fromWord.IndexOf('/');

                        var dataSource = GetSource(lineSpan.Slice(lineSpan.IndexOf('{')));

                        //  /c/en/web/n/wn/artifact

                        var fromPOS = PartOfSpeech.X;

                        if (ixf > 0)
                        {
                            fromPOS = GetPOS(fromWord.Slice(ixf));
                            fromWord = fromWord.Slice(0, ixf);
                        }

                        var toWord = to.Slice(4 + toLangLen);
                        var ixt = toWord.IndexOf('/');

                        var toPOS = PartOfSpeech.X;

                        if (ixt > 0)
                        {
                            toPOS   = GetPOS(toWord.Slice(ixt));
                            toWord = toWord.Slice(0, ixt);
                        }
                        ConceptNetRelation relation;

                        if (edgeType[0] == 'd' && edgeType[1] == 'b')
                        {
                            relation = Enum.Parse<ConceptNetRelation>(new string(edgeType).Replace("/", ""), true);
                        }
                        else
                        {
                            relation = Enum.Parse<ConceptNetRelation>(new string(edgeType));
                        }

                        var key = (fromLang, toLang, relation);

                        if (!relationships.TryGetValue(key, out var edges))
                        {
                            edges = new Dictionary<ulong, ConceptNetEdge[]>();
                            relationships[key] = edges;
                        }

                        var fromHash = AddToCache(fromWord, fromPOS, fromLang);
                        var toHash   = AddToCache(toWord, toPOS, toLang);

                        float weightFloat = 0f;

                        var weightIndex = lineSpan.LastIndexOf(':') - "weight\":".Length;

                        if (lineSpan[weightIndex + 1] == 'w' && lineSpan[weightIndex + 2] == 'e' && lineSpan[weightIndex + 3] == 'i')
                        {
                            weightFloat = float.Parse(lineSpan.Slice(weightIndex + "\"weight\": ".Length, lineSpan.Length - weightIndex - "\"weight\": ".Length - 1));
                        }

                        byte weight = (byte)(weightFloat * 100);


                        if (edges.TryGetValue(fromHash.hash, out var existingEdges))
                        {
                            Array.Resize(ref existingEdges, existingEdges.Length + 1);
                            existingEdges[existingEdges.Length - 1] = new  ConceptNetEdge(toHash.hash, weight, dataSource);
                            edges[fromHash.hash] = existingEdges;
                        }
                        else
                        {
                            edges[fromHash.hash] = new ConceptNetEdge[] { new ConceptNetEdge(toHash.hash, weight, dataSource) };
                        }

                        if (readLines % 500 == 0)
                        {
                            Console.WriteLine($"[{(100f * readLines / totalLines):n1}%] {new string(fromWord)}\t{relation}\t{new string(toWord)}");
                        }
                    }
                }
            }

            Console.WriteLine("Done Reading");

            var languagesToKeep = new HashSet<Language>(new[] { Language.English, Language.French, Language.Italian, Language.German, Language.Spanish, Language.Russian, Language.Portuguese, Language.Japanese, Language.Dutch, Language.Chinese, Language.Bulgarian, Language.Finnish, Language.Norwegian, Language.Swedish });

            foreach(var (lang, sb) in wordCache)
            {
                if (!languagesToKeep.Contains(lang)) continue;

                var wordsCache = new ConceptNetWords(lang, sb.ToString(), words[lang]);

                var langPath = resourcesPath.Replace(".Language", $".{lang}");
                
                if(!Directory.Exists(langPath))
                {
                    Directory.CreateDirectory(langPath);
                }

                foreach (var toLang in relationships.Keys.Where(k => k.from == lang).Select(k => k.to).Distinct())
                {
                    if (!languagesToKeep.Contains(toLang)) continue;

                    Console.WriteLine($"Processing pair {lang} -> {toLang}");

                    var edgesMap = new Dictionary<ConceptNetRelation, Dictionary<ulong, (int from, ushort length)>>();
                    
                    var allEdges = new List<ConceptNetEdge>();

                    foreach (var rel in relationships.Keys.Where(k => k.from == lang && k.to == toLang).Select(k => k.relationType))
                    {
                        var edges = new Dictionary<ulong, (int from, ushort length)>();
                        edgesMap.Add(rel, edges);

                        foreach(var kv in relationships[(lang, toLang, rel)])
                        {
                            var from = allEdges.Count;
                            allEdges.AddRange(kv.Value);
                            var len = allEdges.Count - from;
                            if(len < ushort.MaxValue)
                            {
                                edges[kv.Key] = (from, (ushort)len);
                            }
                            else
                            {
                                throw new Exception("Error");
                            }
                        }
                    }

                    var edgesData = new ConceptNetEdgesData(lang, toLang, allEdges.ToArray(), edgesMap);

                    using (var f = File.OpenWrite(Path.Combine(langPath, $"edges-{Languages.EnumToCode(lang)}-{Languages.EnumToCode(toLang)}.msgpack")))
                    {
                        MessagePackSerializer.Serialize(f, edgesData, Loader.LZ4Standard);
                        f.Flush();
                    }
                }


                using(var f = File.OpenWrite(Path.Combine(langPath, $"words-{Languages.EnumToCode(lang)}.msgpack")))
                {
                    MessagePackSerializer.Serialize(f, wordsCache, Loader.LZ4Standard);
                    f.Flush();
                }
            }

            
            Console.WriteLine("Done Writing");

            PartOfSpeech GetPOS(ReadOnlySpan<char> concept)
            {
                if(concept.Length == 0)
                {
                    return PartOfSpeech.X;
                }

                var nextSlash = concept.Slice(1).IndexOf('/');

                if (nextSlash < 0 || nextSlash == 1)
                {
                    var pos = concept.Slice(1, 1)[0];

                    switch (pos)
                    {
                        case 'n': return PartOfSpeech.NOUN;
                        case 'v': return PartOfSpeech.VERB;
                        case 'a': return PartOfSpeech.ADJ;
                        case 's': return PartOfSpeech.ADJ;
                        case 'r': return PartOfSpeech.ADV;
                    }
                }

                return PartOfSpeech.X;
            }

            ConceptNetSource GetSource(ReadOnlySpan<char> jsonPart)
            {
                var ix = jsonPart.IndexOf("/d/", StringComparison.InvariantCultureIgnoreCase);
                
                if (ix < 0)
                {
                    throw new Exception("Missing Source: " + new string(jsonPart));
                }

                jsonPart = jsonPart.Slice(ix + 3);
                
                if (jsonPart.Contains("conceptnet/", StringComparison.InvariantCultureIgnoreCase))
                {
                    if (jsonPart.Contains("conceptnet/4/en", StringComparison.InvariantCultureIgnoreCase)) return ConceptNetSource.ConceptNetEnglishV4;
                    if (jsonPart.Contains("conceptnet/4/pt", StringComparison.InvariantCultureIgnoreCase)) return ConceptNetSource.ConceptNetPortugueseV4;
                    if (jsonPart.Contains("conceptnet/4/es", StringComparison.InvariantCultureIgnoreCase)) return ConceptNetSource.ConceptNetSpanishV4;
                    if (jsonPart.Contains("conceptnet/4/fr", StringComparison.InvariantCultureIgnoreCase)) return ConceptNetSource.ConceptNetFrenchV4;
                    if (jsonPart.Contains("conceptnet/4/it", StringComparison.InvariantCultureIgnoreCase)) return ConceptNetSource.ConceptNetItalianV4;
                    if (jsonPart.Contains("conceptnet/4/hu", StringComparison.InvariantCultureIgnoreCase)) return ConceptNetSource.ConceptNetHungarianV4;
                    if (jsonPart.Contains("conceptnet/4/ko", StringComparison.InvariantCultureIgnoreCase)) return ConceptNetSource.ConceptNetKoreanV4;
                    if (jsonPart.Contains("conceptnet/4/ja", StringComparison.InvariantCultureIgnoreCase)) return ConceptNetSource.ConceptNetJapaneseV4;
                    if (jsonPart.Contains("conceptnet/4/zh", StringComparison.InvariantCultureIgnoreCase)) return ConceptNetSource.ConceptNetChineseV4;
                    if (jsonPart.Contains("conceptnet/4/nl", StringComparison.InvariantCultureIgnoreCase)) return ConceptNetSource.ConceptNetDutchV4;
                }
                else if (jsonPart.Contains("wiktionary/", StringComparison.InvariantCultureIgnoreCase))
                {
                    if (jsonPart.Contains("wiktionary/en", StringComparison.InvariantCultureIgnoreCase)) return ConceptNetSource.WiktionaryEnglish;
                    if (jsonPart.Contains("wiktionary/fr", StringComparison.InvariantCultureIgnoreCase)) return ConceptNetSource.WiktionaryFrench;
                    if (jsonPart.Contains("wiktionary/de", StringComparison.InvariantCultureIgnoreCase)) return ConceptNetSource.WiktionaryGerman;
                }
                else
                {
                    if (jsonPart.Contains("dbpedia", StringComparison.InvariantCultureIgnoreCase))       return ConceptNetSource.DBpedia;
                    if (jsonPart.Contains("jmdict", StringComparison.InvariantCultureIgnoreCase))        return ConceptNetSource.JMDict;
                    if (jsonPart.Contains("opencyc", StringComparison.InvariantCultureIgnoreCase))       return ConceptNetSource.OpenCyc;
                    if (jsonPart.Contains("verbosity", StringComparison.InvariantCultureIgnoreCase))     return ConceptNetSource.Verbosity;
                    if (jsonPart.Contains("wordnet", StringComparison.InvariantCultureIgnoreCase))       return ConceptNetSource.WordNet;
                    if (jsonPart.Contains("kyoto_yahoo", StringComparison.InvariantCultureIgnoreCase))   return ConceptNetSource.KyotoYahoo;
                    if (jsonPart.Contains("cc_cedict", StringComparison.InvariantCultureIgnoreCase))     return ConceptNetSource.CCCedict;
                }
                throw new Exception("Missing Source: " + new string(jsonPart));
            }


            static bool TryGetLang(ReadOnlySpan<char> langSpan, out int length, out Language language)
            {
                if (langSpan[2] == '/')
                {
                    if((langSpan[0] == 'a' && langSpan[1] == 'v')
                        || (langSpan[0] == 'b' && langSpan[1] == 'm')
                        || (langSpan[0] == 'e' && langSpan[1] == 'e')
                        || (langSpan[0] == 's' && langSpan[1] == 'h')
                        || (langSpan[0] == 'o' && langSpan[1] == 'j')
                        || (langSpan[0] == 'f' && langSpan[1] == 'f')
                        )
                    {
                        language = Language.Unknown;
                        length = -1;
                        return false;
                    }

                    language = Languages.CodeToEnum(new string(langSpan.Slice(0, 2)));
                    length = 2;
                    return true;
                }
                else
                {
                    var tmp = new string(langSpan);
                    if(Languages.IsValid3LetterCode(tmp))
                    {
                        language = Languages.ThreeLetterCodeToEnum(tmp);
                        length = 3;
                        return true;
                    }
                }

                language = Language.Unknown;
                length = -1;
                return false;
            }
        }
    }

}
