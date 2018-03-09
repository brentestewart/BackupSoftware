namespace TcpCommunications.Core.Messages
{
	public sealed class FileEndMessage : NetworkMessageBase
	{
		public override int MessageType => 12;
		public override byte[] Payload { get; set; }

		public FileEndMessage(byte[] fileHash)
		{
			Payload = fileHash;
		}
	}
}