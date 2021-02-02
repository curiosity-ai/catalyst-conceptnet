using MessagePack;
using Mosaik.Core;
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
    }

}
