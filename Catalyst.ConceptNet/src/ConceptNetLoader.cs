using MessagePack;
using Mosaik.Core;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Catalyst.ConceptNet
{
    public static class Loader
    {
        public static readonly MessagePackSerializerOptions LZ4Standard = MessagePackSerializerOptions.Standard.WithCompression(MessagePackCompression.Lz4Block);

        private static Dictionary<Language, Assembly> _assemblies = new Dictionary<Language, Assembly>();


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

        private static bool TryReadEdges(Language fromLanguage, Language toLanguage, out ConceptNetEdgesData data)
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

        private static bool TryReadWords(Language language, out ConceptNetWords words)
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
