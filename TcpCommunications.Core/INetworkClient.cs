using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using TcpCommunications.Core.Messages;

namespace TcpCommunications.Core
{
	public interface INetworkClient
	{
		string ClientName { get; set; }
		ConcurrentQueue<INetworkMessage> OutboundMessages { get; set; }
		ConcurrentQueue<INetworkMessage> InboundMessages { get; set; }
		CancellationTokenSource CancellationTokenSource { get; set; }
		Task WatchForInboundNetworkTrafficTask { get; set; }
		Task WatchInboundQueueTask { get; set; }
		Task WatchForOutboundMessagesTask { get; set; }
		INetworkStream GetCommunicationStream();
		Task Connect(string ipAddress, int port);
	}
}
