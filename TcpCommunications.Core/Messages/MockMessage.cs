using System.Text;

namespace TcpCommunications.Core.Messages
{
	public class MockMessage : NetworkMessageBase
	{
		public override int MessageType => 20000;

		public override byte[] Payload { get; set; }
		public string Message => Encoding.ASCII.GetString(Payload);

		public MockMessage(string message)
		{
			Payload = Encoding.ASCII.GetBytes(message);
		}

		public MockMessage(byte[] payload)
		{
			Payload = payload;
		}
	}
}
