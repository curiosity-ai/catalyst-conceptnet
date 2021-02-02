using MessagePack;
using Mosaik.Core;
using System;
using System.Collections.Generic;

namespace Catalyst.ConceptNet
{
    [MessagePackObject]
    public class ConceptNetEdgesData
    {
        public ConceptNetEdgesData(Language from, Language to, ConceptNetEdge[] edges, Dictionary<ConceptNetRelation, Dictionary<ulong, (int from, ushort length)>> edgesMap)
        {
            From = from;
            To = to;
            Edges = edges;
            EdgesMap = edgesMap;
        }

        [Key(0)] public Language From { get; }
        [Key(1)] public Language To { get; }
        [Key(2)] public ConceptNetEdge[] Edges { get; }
        [Key(3)] public Dictionary<ConceptNetRelation, Dictionary<ulong, (int from, ushort length)>> EdgesMap { get; }

        public ReadOnlySpan<ConceptNetEdge> GetEdges(ConceptNetRelation relationType, ulong hash)
        {
            if(EdgesMap.TryGetValue(relationType, out var relationsMap) && relationsMap.TryGetValue(hash, out var edgeInfo))
            {
                return Edges.AsSpan().Slice(edgeInfo.from, edgeInfo.length);
            }
            else
            {
                return ReadOnlySpan<ConceptNetEdge>.Empty;
            }
        }
    }

}
