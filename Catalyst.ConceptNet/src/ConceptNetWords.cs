using MessagePack;
using Mosaik.Core;
using System;
using System.Collections.Generic;

namespace Catalyst.ConceptNet
{
    [MessagePackObject]
    public class ConceptNetWords
    {
        public ConceptNetWords(Language language, string cache, Dictionary<ulong, (int start, byte length, byte pos)> hashesMap)
        {
            Language = language;
            Cache = cache;
            HashesMap = hashesMap;
        }

        [Key(0)] public Language Language { get; }
        [Key(1)] public string Cache { get; }
        [Key(2)] public Dictionary<ulong, (int start, byte length, byte pos)> HashesMap { get; }

        public (string Word, PartOfSpeech PartOfSpeech) GetWord(ulong hash)
        {
            if (TryGetWord(hash, out var word, out var pos))
            {
                return (word, pos);
            }
            else
            {
                return (null, PartOfSpeech.NONE);
            }
        }

        public bool TryGetWord(ulong hash, out string word, out PartOfSpeech partOfSpeech)
        {
            if (HashesMap.TryGetValue(hash, out var map))
            {
                word = new string(Cache.AsSpan().Slice(map.start, map.length));
                partOfSpeech = (PartOfSpeech)map.pos;
                return true;
            }
            else
            {
                word = null;
                partOfSpeech = PartOfSpeech.NONE;
                return false;
            }
        }
    }

}
