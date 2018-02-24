namespace TcpCommunications.Core.Messages
{
	public class GetFileMessage : NetworkMessageBase
	{
		public override int MessageType => 2;

		public override byte[] Payload { get; set; }

		public byte[] FileHash => Payload;

		public GetFileMessage(byte[] fileHash)
		{
			Payload = fileHash;
		}
	}
}
