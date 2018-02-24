namespace TcpCommunications.Core.Messages
{
	public interface INetworkMessage
    {
        int MessageId { get; set; }
        int MessageType { get; }
        int PayloadLength { get; }
        byte[] Payload { get; }
        byte[] GetMessageBytes();
    }
}
