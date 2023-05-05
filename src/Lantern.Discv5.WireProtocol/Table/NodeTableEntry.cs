using Lantern.Discv5.Enr;
using Lantern.Discv5.Enr.IdentityScheme.Interfaces;

namespace Lantern.Discv5.WireProtocol.Table;

public class NodeTableEntry
{
    public NodeTableEntry(EnrRecord record, IIdentitySchemeVerifier verifier)
    {
        Record = record;
        Id = verifier.GetNodeIdFromRecord(record);
        IsLive = false;
    }

    public byte[] Id { get; }
    
    public EnrRecord Record { get; }
    
    public bool IsLive { get; set; }

    public int LivenessCounter { get; set; }
}