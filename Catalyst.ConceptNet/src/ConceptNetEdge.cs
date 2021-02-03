using MessagePack;

namespace Catalyst.ConceptNet
{
    [MessagePackObject]
    public struct ConceptNetEdge
    {
        [Key(0)] public ulong To { get; }
        [Key(1)] public byte Weight { get; }
        [Key(2)] public ConceptNetSource Source { get; }

        public ConceptNetEdge(ulong to, byte weight, ConceptNetSource source)
        {
            To = to;
            Weight = weight;
            Source = source;
        }
    }
}
