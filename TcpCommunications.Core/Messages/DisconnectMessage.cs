namespace TcpCommunications.Core.Messages
{
	public class DisconnectMessage : NetworkMessageBase
	{
		private static DisconnectMessage instance;
		public static DisconnectMessage Insatnce => (instance == null) ? (instance = new DisconnectMessage()) : instance;

		public override int MessageType => 9999;

		public override byte[] Payload { get; set; } = new byte[0];		
	}
}
