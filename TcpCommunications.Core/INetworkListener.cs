using System.Threading.Tasks;

namespace TcpCommunications.Core
{
	public interface INetworkListener
	{
		void Start(int port);
		void Stop();
		Task<INetworkClient> AcceptNetworkClientAsync();
	}
}
