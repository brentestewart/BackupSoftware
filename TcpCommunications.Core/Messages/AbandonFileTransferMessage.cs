namespace TcpCommunications.Core.Messages
{
	public class AbandonFileTransferMessage : NetworkMessageBase
	{
		public override int MessageType => 13;
		public override byte[] Payload { get; set; } = new byte[0];

		public AbandonFileTransferMessage(byte[] payload)
		{
			Payload = payload;
		}
	}
}