using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using TcpCommunications.Core.Messages;

namespace TcpCommunications.Core
{
	public class NetworkClient : INetworkClient
    {
		private INetworkStream NetworkStream { get; set; }
        public string ClientName { get; set; }
		public ConcurrentQueue<INetworkMessage> OutboundMessages { get; set; } = new ConcurrentQueue<INetworkMessage>();
		public ConcurrentQueue<INetworkMessage> InboundMessages { get; set; } = new ConcurrentQueue<INetworkMessage>();
		public CancellationTokenSource CancellationTokenSource { get; set; } = new CancellationTokenSource();
		public Task WatchForInboundNetworkTrafficTask { get; set; }
		public Task WatchInboundQueueTask { get; set; }
		public Task WatchForOutboundMessagesTask { get; set; }

		public NetworkClient(string name, TcpClient tcpClient)
		{
			ClientName = name;
			NetworkStream = new CommunicatorStream(tcpClient);
		}

		public INetworkStream GetCommunicationStream()
		{
			return NetworkStream;
		}

		public async Task Connect(string ipAddress, int port)
		{
			await NetworkStream.Connect(ipAddress, port);
		}
	}
}
