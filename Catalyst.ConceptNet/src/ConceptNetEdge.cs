using MessagePack;

namespace Catalyst.ConceptNet
{
    [MessagePackObject]
    public struct ConceptNetEdge
    {
        [Key(0)] public ulong To { get; }
        [Key(1)] public byte Weight { get; }

        public ConceptNetEdge(ulong to, byte weight)
        {
            To = to;
            Weight = weight;
        }
    }

}
