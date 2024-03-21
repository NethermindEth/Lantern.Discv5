using Lantern.Discv5.Enr;
using Lantern.Discv5.Enr.Identity.V4;
using Lantern.Discv5.WireProtocol.Connection;
using Lantern.Discv5.WireProtocol.Identity;
using Lantern.Discv5.WireProtocol.Logging;
using Lantern.Discv5.WireProtocol.Message;
using Lantern.Discv5.WireProtocol.Message.Requests;
using Lantern.Discv5.WireProtocol.Message.Responses;
using Lantern.Discv5.WireProtocol.Session;
using Lantern.Discv5.WireProtocol.Table;
using Lantern.Discv5.WireProtocol.Utility;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace Lantern.Discv5.WireProtocol.Tests;

[TestFixture]
public class MessageRequesterTests
{
    private Mock<IIdentityManager> mockIdentityManager = null!;
    private Mock<IRequestManager> mockRequestManager = null!;
    private Mock<ILoggerFactory> mockLoggerFactory;
    private Mock<ILogger<MessageRequester>> logger; 
    private static IIdentityManager _identityManager = null!;
    private static IEnrFactory _enrFactory = null!;
    private static IMessageRequester _messageRequester = null!;

    [SetUp]
    public void Setup()
    {
        var connectionOptions = new ConnectionOptions();
        var sessionOptions = SessionOptions.Default;
        var tableOptions = TableOptions.Default;
        var loggerFactory = LoggingOptions.Default; 
        var enrEntryRegistry = new EnrEntryRegistry();
        var serviceProvider =
            Discv5ServiceConfiguration.ConfigureServices(loggerFactory, connectionOptions, sessionOptions, enrEntryRegistry,Discv5ProtocolBuilder.CreateNewRecord(connectionOptions, sessionOptions.Verifier, sessionOptions.Signer), tableOptions).BuildServiceProvider();
        
        _identityManager = serviceProvider.GetRequiredService<IIdentityManager>();
        _enrFactory = serviceProvider.GetRequiredService<IEnrFactory>();
        _messageRequester = serviceProvider.GetRequiredService<IMessageRequester>();
        
        mockIdentityManager = new Mock<IIdentityManager>();
        mockRequestManager = new Mock<IRequestManager>();
        mockLoggerFactory = new Mock<ILoggerFactory>();
        logger = new Mock<ILogger<MessageRequester>>();
        logger.Setup(x => x.Log(It.IsAny<LogLevel>(), It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(), (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()));
        mockLoggerFactory
            .Setup(x => x.CreateLogger(It.IsAny<string>()))
            .Returns(logger.Object);
    }

    [Test]
    public void Test_MessageRequester_ShouldGeneratePingMessageCorrectly()
    {
        var destNodeId = RandomUtility.GenerateRandomData(32);
        var pingMessage = _messageRequester.ConstructPingMessage(destNodeId)!;
        var cachedPingMessage = _messageRequester.ConstructCachedPingMessage(destNodeId)!;
        var decodedPingMessage = (PingMessage)new MessageDecoder(_identityManager, _enrFactory).DecodeMessage(pingMessage);
        var decodedCachedPingMessage = (PingMessage)new MessageDecoder(_identityManager, _enrFactory).DecodeMessage(cachedPingMessage);

        Assert.AreEqual(MessageType.Ping, decodedPingMessage.MessageType);
        Assert.AreEqual(_identityManager.Record.SequenceNumber, decodedPingMessage.EnrSeq);
        Assert.AreEqual(MessageType.Ping, decodedCachedPingMessage.MessageType);
        Assert.AreEqual(_identityManager.Record.SequenceNumber, decodedCachedPingMessage.EnrSeq);
    }

    [Test]
    public void Test_MessageRequester_ShouldGenerateFindNodeMessageCorrectly()
    {
        var destNodeId = RandomUtility.GenerateRandomData(32);
        var targetNodeId = RandomUtility.GenerateRandomData(32);
        var findNodeMessage = _messageRequester.ConstructFindNodeMessage(destNodeId, targetNodeId)!;
        var cachedFindNodeMessage = _messageRequester.ConstructCachedFindNodeMessage(destNodeId, targetNodeId)!;
        var decodedFindNodeMessage = (FindNodeMessage)new MessageDecoder(_identityManager, _enrFactory).DecodeMessage(findNodeMessage);
        var decodedCachedFindNodeMessage = (FindNodeMessage)new MessageDecoder(_identityManager, _enrFactory).DecodeMessage(cachedFindNodeMessage);

        Assert.AreEqual(MessageType.FindNode, decodedFindNodeMessage.MessageType);
        Assert.AreEqual(TableUtility.Log2Distance(targetNodeId, destNodeId), decodedFindNodeMessage.Distances.First());
        Assert.AreEqual(MessageType.FindNode, decodedCachedFindNodeMessage.MessageType);
        Assert.AreEqual(TableUtility.Log2Distance(targetNodeId, destNodeId), decodedCachedFindNodeMessage.Distances.First());
    }
    
    [Test]
    public void Test_MessageRequester_ShouldGenerateTalkRequestMessageCorrectly()
    {
        var destNodeId = RandomUtility.GenerateRandomData(32);
        var protocol = "discv5"u8.ToArray();
        var request = "ping"u8.ToArray();
        var talkRequestMessage = _messageRequester.ConstructTalkReqMessage(destNodeId, protocol, request)!;
        var cachedTalkRequestMessage = _messageRequester.ConstructCachedTalkReqMessage(destNodeId, protocol, request)!;
        var decodedTalkRequestMessage = (TalkReqMessage)new MessageDecoder(_identityManager, _enrFactory).DecodeMessage(talkRequestMessage);
        var cachedDecodedTalkRequestMessage = (TalkReqMessage)new MessageDecoder(_identityManager, _enrFactory).DecodeMessage(cachedTalkRequestMessage);

        Assert.AreEqual(MessageType.TalkReq, decodedTalkRequestMessage.MessageType);
        Assert.AreEqual(protocol, decodedTalkRequestMessage.Protocol);
        Assert.AreEqual(request, decodedTalkRequestMessage.Request);
        Assert.AreEqual(MessageType.TalkReq, cachedDecodedTalkRequestMessage.MessageType);
        Assert.AreEqual(protocol, cachedDecodedTalkRequestMessage.Protocol);
        Assert.AreEqual(request, cachedDecodedTalkRequestMessage.Request);
    }

    [Test]
    public void Test_MessageRequester_ShouldGenerateTalkResponseMessageCorrectly()
    {
        var destNodeId = RandomUtility.GenerateRandomData(32);
        var response = "response"u8.ToArray();
        var talkResponseMessage = _messageRequester.ConstructTalkRespMessage(destNodeId, response)!;
        var cachedTalkResponseMessage = _messageRequester.ConstructCachedTalkRespMessage(destNodeId, response)!;
        var decodedTalkResponseMessage = (TalkRespMessage)new MessageDecoder(_identityManager, _enrFactory).DecodeMessage(talkResponseMessage);
        var cachedDecodedTalkResponseMessage = (TalkRespMessage)new MessageDecoder(_identityManager, _enrFactory).DecodeMessage(cachedTalkResponseMessage);

        Assert.AreEqual(MessageType.TalkResp, decodedTalkResponseMessage.MessageType);
        Assert.AreEqual(response, decodedTalkResponseMessage.Response);
        Assert.AreEqual(MessageType.TalkResp, cachedDecodedTalkResponseMessage.MessageType);
        Assert.AreEqual(response, cachedDecodedTalkResponseMessage.Response);
    }
    
    [Test]
    public void Test_MessageRequester_ShouldReturnWhenUnableToAddRequests()
    {
        var enrEntryRegistry = new EnrEntryRegistry();
        var enrRecord = new EnrFactory(enrEntryRegistry).CreateFromString("enr:-IS4QHCYrYZbAKWCBRlAy5zzaDZXJBGkcnh4MHcBFZntXNFrdvJjX04jRzjzCBOonrkTfj499SZuOh8R33Ls8RRcy5wBgmlkgnY0gmlwhH8AAAGJc2VjcDI1NmsxoQPKY0yuDUmstAHYpMa2_oxVtw0RW_QAdpzBQA8yWM0xOIN1ZHCCdl8", new IdentityVerifierV4());
        
        mockIdentityManager
            .Setup(x => x.Record)
            .Returns(enrRecord);
        mockRequestManager
            .Setup(x => x.AddPendingRequest(It.IsAny<byte[]>(), It.IsAny<PendingRequest>()))
            .Returns(false);
        mockRequestManager
            .Setup(x => x.AddCachedRequest(It.IsAny<byte[]>(), It.IsAny<CachedRequest>()))
            .Returns(false);
        
        var messageRequester = new MessageRequester(mockIdentityManager.Object, mockRequestManager.Object, mockLoggerFactory.Object); 
        var destNodeId = RandomUtility.GenerateRandomData(32);
        var pingResult = messageRequester.ConstructPingMessage(destNodeId);
        var cachedPingResult = messageRequester.ConstructCachedPingMessage(destNodeId);
        var findNodeResult = messageRequester.ConstructFindNodeMessage(destNodeId, destNodeId);
        var cachedFindNodeResult = messageRequester.ConstructCachedFindNodeMessage(destNodeId, destNodeId);
        var talkRequestResult = messageRequester.ConstructTalkReqMessage(destNodeId, "discv5"u8.ToArray(), "ping"u8.ToArray());
        var cachedTalkRequestResult = messageRequester.ConstructCachedTalkReqMessage(destNodeId, "discv5"u8.ToArray(), "ping"u8.ToArray());
        var talkResponseResult = messageRequester.ConstructTalkRespMessage(destNodeId, "response"u8.ToArray());
        var cachedTalkResponseResult = messageRequester.ConstructCachedTalkRespMessage(destNodeId, "response"u8.ToArray());
        
        Assert.IsNull(pingResult);
        Assert.IsNull(cachedPingResult);
        Assert.IsNull(findNodeResult);
        Assert.IsNull(cachedFindNodeResult);
        Assert.IsNull(talkRequestResult);
        Assert.IsNull(cachedTalkRequestResult);
        Assert.IsNull(talkResponseResult);
        Assert.IsNull(cachedTalkResponseResult);
    }
}