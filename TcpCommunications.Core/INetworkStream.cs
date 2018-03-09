using System.Threading.Tasks;

namespace TcpCommunications.Core
{
	public interface INetworkStream
	{
		Task Connect(string ipAddress, int port);
		bool DataAvailable { get; }
		byte[] Read(int offset, int size);
		void Write(byte[] buffer, int offset, int size);
	}
}
