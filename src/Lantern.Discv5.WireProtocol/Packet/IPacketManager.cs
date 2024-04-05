using System.Net.Sockets;
using Lantern.Discv5.Enr;
using Lantern.Discv5.WireProtocol.Message;

namespace Lantern.Discv5.WireProtocol.Packet;

public interface IPacketManager
{
    Task<byte[]?> SendPacket(IEnr dest, MessageType messageType, params byte[][] args);
    
    Task HandleReceivedPacket(UdpReceiveResult returnedResult);
}