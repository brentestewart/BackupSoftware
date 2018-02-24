using System;
using System.Threading.Tasks;
using TcpCommunications.Core.Messages;

namespace TcpCommunications.Core
{
	public interface ICommunicator
	{
		event EventHandler<MessageReceivedArgs> OnMessageReceived;
		Task StartServer(int maxClients);
		Task ShutdownServer();
		void ConnectToServer(string ipAddress, int port);
		void SendMessage(string clientName, INetworkMessage message);
		void SendMessage(INetworkMessage message);
	}
}
