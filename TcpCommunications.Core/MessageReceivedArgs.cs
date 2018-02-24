using System;
using TcpCommunications.Core.Messages;

namespace TcpCommunications.Core
{
	public class MessageReceivedArgs : EventArgs
	{
		public INetworkMessage Message { get; set; }
		public string ClientName { get; set; }
	}
}
