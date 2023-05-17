using System.Net;
using System.Net.Sockets;
using Lantern.Discv5.Enr;
using Lantern.Discv5.Enr.EnrContent;
using Lantern.Discv5.Enr.EnrContent.Entries;
using Lantern.Discv5.Rlp;
using Lantern.Discv5.WireProtocol.Connection;
using Lantern.Discv5.WireProtocol.Identity;
using Lantern.Discv5.WireProtocol.Message;
using Lantern.Discv5.WireProtocol.Packet.Handlers;
using Lantern.Discv5.WireProtocol.Packet.Types;
using Lantern.Discv5.WireProtocol.Session;
using Lantern.Discv5.WireProtocol.Table;
using Lantern.Discv5.WireProtocol.Utility;
using Microsoft.Extensions.Logging;

namespace Lantern.Discv5.WireProtocol.Packet;

public class PacketService : IPacketService
{
    private readonly IPacketHandlerFactory _packetHandlerFactory;
    private readonly IIdentityManager _identityManager;
    private readonly ISessionManager _sessionManager;
    private readonly IRoutingTable _routingTable;
    private readonly IMessageRequester _messageRequester;
    private readonly IPendingRequests _pendingRequests;
    private readonly IUdpConnection _udpConnection;
    private readonly ILookupManager _lookupManager;
    private readonly IAesUtility _aesUtility;
    private readonly IPacketBuilder _packetBuilder;
    private readonly ILogger<PacketService> _logger;

    public PacketService(IPacketHandlerFactory packetHandlerFactory, IIdentityManager identityManager,
        ISessionManager sessionManager, IRoutingTable routingTable, IMessageRequester messageRequester,
        IUdpConnection udpConnection, ILookupManager lookupManager, IAesUtility aesUtility, IPacketBuilder packetBuilder, 
        ILoggerFactory loggerFactory, IPendingRequests pendingRequests)
    {
        _packetHandlerFactory = packetHandlerFactory;
        _identityManager = identityManager;
        _sessionManager = sessionManager;
        _routingTable = routingTable;
        _messageRequester = messageRequester;
        _udpConnection = udpConnection;
        _lookupManager = lookupManager;
        _aesUtility = aesUtility;
        _packetBuilder = packetBuilder;
        _pendingRequests = pendingRequests;
        _logger = loggerFactory.CreateLogger<PacketService>();
    }

    public async Task InitialiseDiscoveryAsync()
    {
        if (_routingTable.GetTotalEntriesCount() == 0)
        {
            _logger.LogInformation("Initialising from bootstrap ENRs");
            var bootstrapEnrs = _routingTable.GetBootstrapEnrs();

            foreach (var bootstrapEnr in bootstrapEnrs)
            {
                try
                {
                    await SendPacket(MessageType.Ping, bootstrapEnr);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error sending packet to bootstrap ENR: {BootstrapEnr}", bootstrapEnr);
                }
            }
        }
    }

    public async Task PingNodeAsync()
    {
        if (_routingTable.GetTotalEntriesCount() > 0)
        {
            _logger.LogInformation("Pinging node for checking liveness...");
            var targetNodeId = RandomUtility.GenerateNodeId(PacketConstants.NodeIdSize);
            var nodeEntry = _routingTable.GetInitialNodesForLookup(targetNodeId).First();
            await SendPacket(MessageType.Ping, nodeEntry.Record);
        }
    }
    
    public async Task PerformLookup(byte[] targetNodeId)
    {
        _logger.LogInformation("Performing lookup...");
        var closestNodes = await _lookupManager.PerformLookup(targetNodeId);
        _logger.LogInformation("Lookup completed. Closest nodes found:");
        
        foreach (var node in closestNodes)
        {
            _logger.LogInformation("Node ID: {NodeId}", Convert.ToHexString(node.Id));
        }
    }

    private async Task PerformDiscovery()
    {
        _logger.LogInformation("Performing discovery...");
        var targetNodeId = RandomUtility.GenerateNodeId(PacketConstants.NodeIdSize);
        var initialNodesForLookup = _routingTable.GetInitialNodesForLookup(targetNodeId);
        
        // Establish sessions with initial nodes
        foreach (var nodeEntry in initialNodesForLookup)
        {
            if (!nodeEntry.IsQueried)
            {
                await SendPacket(MessageType.FindNode, nodeEntry.Record);
            }
        }
    }

    public async Task SendPacket(MessageType messageType, EnrRecord record)
    {
        var destNodeId = _identityManager.Verifier.GetNodeIdFromRecord(record);
        var destEndPoint = new IPEndPoint(record.GetEntry<EntryIp>(EnrContentKey.Ip).Value, record.GetEntry<EntryUdp>(EnrContentKey.Udp).Value);
        var cryptoSession = _sessionManager.GetSession(destNodeId, destEndPoint);
        
        if (cryptoSession is { IsEstablished: true })
        {
            await SendOrdinaryPacketAsync(messageType, cryptoSession, destEndPoint, destNodeId);
            return;
        }

        await SendRandomOrdinaryPacketAsync(destEndPoint, destNodeId);
    }
    
    public async Task HandleReceivedPacket(UdpReceiveResult returnedResult)
    {
        var packet = new PacketProcessor(_identityManager, _aesUtility, returnedResult.Buffer);
        var packetHandler = _packetHandlerFactory.GetPacketHandler((PacketType)packet.StaticHeader.Flag);
        await packetHandler.HandlePacket(_udpConnection, returnedResult);
    }

    private async Task CheckPendingRequests()
    {
        // asynchronously check pending requests
        var currentRequests = _pendingRequests.GetPendingRequests();
    
        foreach (var request in currentRequests)
        {
            if(request.CreatedAt + TimeSpan.FromMilliseconds(500) < DateTime.UtcNow)
            {
                _logger.LogInformation("Request timed out. Removing from pending requests");
                _pendingRequests.RemovePendingRequest(request.Message.RequestId);
                
                // What else should be done??
            }
        }

    }

    private async Task SendOrdinaryPacketAsync(MessageType messageType, SessionMain sessionMain, IPEndPoint destEndPoint, byte[] destNodeId)
    {
        var maskingIv = RandomUtility.GenerateMaskingIv(PacketConstants.MaskingIvSize);
        var ordinaryPacket = _packetBuilder.BuildOrdinaryPacket(destNodeId, maskingIv, sessionMain.MessageCount);
        var message = _messageRequester.ConstructMessage(messageType, destNodeId);

        if (message == null)
        {
            _logger.LogWarning("Unable to construct {MessageType} message. Cannot send packet", messageType);
            return;
        }
        
        var encryptedMessage = sessionMain.EncryptMessage(ordinaryPacket.Item2, maskingIv, message);
        var finalPacket = ByteArrayUtils.JoinByteArrays(ordinaryPacket.Item1, encryptedMessage);
        
        await _udpConnection.SendAsync(finalPacket, destEndPoint);
        _logger.LogInformation("Sent {MessageType} request to {Destination}", messageType, destEndPoint);
    }

    private async Task SendRandomOrdinaryPacketAsync(IPEndPoint destEndPoint, byte[] destNodeId)
    {
        var maskingIv = RandomUtility.GenerateMaskingIv(PacketConstants.MaskingIvSize);
        var packetNonce = RandomUtility.GenerateNonce(PacketConstants.NonceSize);
            
        _sessionManager.SaveHandshakeInteraction(packetNonce, destNodeId);
            
        var constructedOrdinaryPacket = _packetBuilder.BuildRandomOrdinaryPacket(destNodeId, packetNonce, maskingIv);
        await _udpConnection.SendAsync(constructedOrdinaryPacket.Item1, destEndPoint);
        _logger.LogInformation("Sent RANDOM packet to initiate handshake with {Destination}", destEndPoint);
    }
}