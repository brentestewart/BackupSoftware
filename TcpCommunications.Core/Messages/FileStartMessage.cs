namespace TcpCommunications.Core.Messages
{
	public sealed class FileStartMessage : NetworkMessageBase
	{
		public override int MessageType => 10;
		public override byte[] Payload { get; set; }

		public FileStartMessage(byte[] fileHash)
		{
			Payload = fileHash;
		}
	}
}