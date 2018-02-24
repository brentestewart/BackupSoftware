namespace TcpCommunications.Core
{
	public interface INetworkStream
	{
		void Connect(string ipAddress, int port);
		bool DataAvailable { get; }
		byte[] Read(int offset, int size);
		void Write(byte[] buffer, int offset, int size);
	}
}
