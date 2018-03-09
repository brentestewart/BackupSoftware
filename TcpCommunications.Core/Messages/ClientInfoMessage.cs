using System.Text;

namespace TcpCommunications.Core.Messages
{
	public class ClientInfoMessage : NetworkMessageBase
	{
		public override int MessageType => 1000;

		public override byte[] Payload { get; set; }

		public string ClientName => Encoding.ASCII.GetString(Payload);

		public ClientInfoMessage(byte[] payload)
		{
			Payload = payload;
		}

		public ClientInfoMessage(string clientName)
		{
			Payload = Encoding.ASCII.GetBytes(clientName);
		}
	}

	public sealed class ClientConnectedMessage : NetworkMessageBase
	{
		public override int MessageType => 1001;
		public override byte[] Payload { get; set; }

		public ClientConnectedMessage(string serverName)
		{
			Payload = Encoding.ASCII.GetBytes(serverName);
		}

		public ClientConnectedMessage(byte[] payload)
		{
			Payload = payload;
		}
	}
}
