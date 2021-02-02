using MessagePack;
using Mosaik.Core;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using UID;

namespace Catalyst.ConceptNet
{
    public static class Loader
    {
        public static readonly MessagePackSerializerOptions LZ4Standard = MessagePackSerializerOptions.Standard.WithCompression(MessagePackCompression.Lz4Block);
        private static Dictionary<Language, Assembly> _assemblies = new Dictionary<Language, Assembly>();
        private static ConcurrentDictionary<Language, ConceptNetWords> _wordsCache = new ConcurrentDictionary<Language, ConceptNetWords>();
        private static ConcurrentDictionary<(Language from, Language to), ConceptNetEdgesData> _edges  = new ConcurrentDictionary<(Language from, Language to), ConceptNetEdgesData>();
        private static Dictionary<PartOfSpeech, ulong> _posHashes = Enum.GetValues(typeof(PartOfSpeech)).Cast<PartOfSpeech>().ToDictionary(pos => pos, pos => pos.ToString().Hash64());


        public static void RegisterFromAssembly(Assembly assembly, Language language)
        {
            lock (_assemblies)
            {
                _assemblies[language] = assembly;
            }
        }

        internal static TType GetResource<TType>(Assembly assembly, Language language, string fileName)
        {
            using var stream = assembly.GetManifestResourceStream($"Catalyst.ConceptNet.{language}.Resources." + fileName);
            return MessagePackSerializer.Deserialize<TType>(stream);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ulong HashWordUnderscoreIsSpace(ReadOnlySpan<char> word, PartOfSpeech pos)
        {
            unchecked
            {
                //Implementation of Fowler/Noll/Vo (https://en.wikipedia.org/wiki/Fowler%E2%80%93Noll%E2%80%93Vo_hash_function), also used by the Roslyn compiler: https://github.com/dotnet/roslyn/blob/master/src/Compilers/Core/Portable/InternalUtilities/Hash.cs
                const ulong fnv_prime = 1099511628211LU;
                const ulong fnv_offset_basis = 0xcbf29ce484222325;
                ulong hash = fnv_offset_basis;
                for (int i = 0; i < word.Length; i++)
                {
                    var c = word[i];

                    if (c == '_') c = ' ';

                    hash = (hash ^ c) * fnv_prime;
                }

                return Hashes.Combine(hash, _posHashes[pos]);
            }
        }

        internal static bool TryGetEdgesData(Language fromLanguage, Language toLanguage, out ConceptNetEdgesData data)
        {
            data = _edges.GetOrAdd((fromLanguage, toLanguage), k => TryLoadEdges(k.from, k.to, out var loadedData) ? loadedData : null);
            return data is object;
        }

        internal static bool TryGetWordsCache(Language language, out ConceptNetWords data)
        {
            data = _wordsCache.GetOrAdd(language, k => TryLoadWords(k, out var loadedData) ? loadedData : null);
            return data is object;
        }

        private static bool TryLoadEdges(Language fromLanguage, Language toLanguage, out ConceptNetEdgesData data)
        {
            var fromAssembly = GetAssemblyFor(fromLanguage);

            if (fromAssembly is object)
            {
                data = GetResource<ConceptNetEdgesData>(fromAssembly, fromLanguage, $"edges-{Languages.EnumToCode(fromLanguage)}-{Languages.EnumToCode(toLanguage)}.msgpack");
                return true;
            }
            else
            {
                data = null;
                return false;
            }
        }

        private static bool TryLoadWords(Language language, out ConceptNetWords words)
        {
            var fromAssembly = GetAssemblyFor(language);

            if (fromAssembly is object)
            {
                words = GetResource<ConceptNetWords>(fromAssembly, language, $"words-{Languages.EnumToCode(language)}.msgpack");
                return true;
            }
            else
            {
                words = null;
                return false;
            }
        }

        private static Assembly GetAssemblyFor(Language fromLanguage)
        {
            Assembly fromAssembly = null;

            lock (_assemblies)
            {
                _assemblies.TryGetValue(fromLanguage, out fromAssembly);
            }

            return fromAssembly;
        }
    }
}
