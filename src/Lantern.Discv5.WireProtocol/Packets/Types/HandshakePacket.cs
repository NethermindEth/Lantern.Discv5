using Lantern.Discv5.Rlp;
using Lantern.Discv5.WireProtocol.Packets.Constants;

namespace Lantern.Discv5.WireProtocol.Packets.Types;

public class HandshakePacket : Packet
{
    public HandshakePacket(byte[] idSignature, byte[] ephPubkey, byte[] srcId, byte[]? record = default) : base(PreparePacketBase(idSignature, ephPubkey, srcId, record))
    {
        IdSignature = idSignature;
        EphPubkey = ephPubkey;
        SrcId = srcId;
        Record = record;
    }
    
    public byte[] IdSignature { get; }
    
    public byte[] EphPubkey { get; }
    
    public byte[]? SrcId { get; }
    
    public byte[]? Record { get; }
    
    public int SigSize => IdSignature.Length;
    
    public int EphKeySize => EphPubkey.Length;

    private static byte[] PreparePacketBase(byte[] idSignature, byte[] ephPubkey, byte[] srcId, byte[]? record = default)
    {
        var authDataHead = ByteArrayUtils.Concatenate(srcId, ByteArrayUtils.ToBigEndianBytesTrimmed(idSignature.Length),
            ByteArrayUtils.ToBigEndianBytesTrimmed(ephPubkey.Length));
        return ByteArrayUtils.Concatenate(authDataHead, idSignature, ephPubkey, record);
    }
    
    public static HandshakePacket DecodeAuthData(byte[] authData)
    {
        var index = 0;
        var srcId = authData[..AuthDataSizes.NodeIdSize];
        index += AuthDataSizes.NodeIdSize;
        
        var sigSize = RlpExtensions.ByteArrayToInt32(authData[index..(index + AuthDataSizes.SigSize)]);
        index += AuthDataSizes.SigSize;
        
        var ephKeySize = RlpExtensions.ByteArrayToInt32(authData[index..(index + AuthDataSizes.EphemeralKeySize)]);
        index += AuthDataSizes.EphemeralKeySize;
        
        var idSignature = authData[index..(index + sigSize)];
        index += sigSize;
        
        var ephPubkey = authData[index..(index  + ephKeySize)];
        index += ephKeySize;
        
        var record = authData[index..];
        return new HandshakePacket(idSignature, ephPubkey, srcId, record);
    }
}