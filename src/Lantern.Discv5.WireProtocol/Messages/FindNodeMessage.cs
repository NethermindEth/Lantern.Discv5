using Lantern.Discv5.Rlp;

namespace Lantern.Discv5.WireProtocol.Messages;

public class FindNodeMessage : Message
{
    public FindNodeMessage(int[] distances) : base(Messages.MessageType.FindNode)
    {
        Distances = distances;
    }

    public int[] Distances { get; }

    public override byte[] EncodeMessage()
    {
        var messageId = new[] { MessageType };
        var encodedRequestId = RlpEncoder.EncodeBytes(RequestId);

        using var stream = new MemoryStream();
        foreach (var t in Distances) stream.Write(RlpEncoder.EncodeInteger(t));

        var encodedDistances = RlpEncoder.EncodeCollectionOfBytes(stream.ToArray());
        var encodedMessage =
            RlpEncoder.EncodeCollectionOfBytes(ByteArrayUtils.Concatenate(encodedRequestId, encodedDistances));
        return ByteArrayUtils.Concatenate(messageId, encodedMessage);
    }

    public static FindNodeMessage DecodeMessage(byte[] message)
    {
        var decodedMessage = RlpDecoder.Decode(message[1..]);
        var distances = new int[decodedMessage.Count - 1];

        for (var i = 1; i < decodedMessage.Count; i++)
            distances[i - 1] = RlpExtensions.ByteArrayToInt32(decodedMessage[i]);

        return new FindNodeMessage(distances)
        {
            RequestId = decodedMessage[0]
        };
    }
}