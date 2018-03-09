using System;
using System.Threading.Tasks;
using TcpCommunications.Core.Messages;

namespace TcpCommunications.Core
{
	public interface ICommunicator
	{
		event EventHandler<MessageReceivedArgs> OnMessageReceived;
		event EventHandler<string> OnClientConnected;
		Task StartServer(int maxClients);
		Task ShutdownServer();
		Task ConnectToServer(string ipAddress, int port);
		Task SendMessage(string clientName, INetworkMessage message);
		Task SendMessage(INetworkMessage message);
	}
}
