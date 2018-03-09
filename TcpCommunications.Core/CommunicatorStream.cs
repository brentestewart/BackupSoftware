using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace TcpCommunications.Core
{
	public class CommunicatorStream : INetworkStream
	{
		public TcpClient TcpClient { get; }
		public bool DataAvailable => Stream.DataAvailable;
		private NetworkStream stream;
		private NetworkStream Stream => stream == null ? (stream = TcpClient.GetStream()) : stream;
		private byte[] ReadBuffer { get; set; } = new byte[0];
		public CommunicatorStream(TcpClient tcpClient)
		{
			TcpClient = tcpClient;
		}

		public async Task Connect(string ipAddress, int port)
		{
			TcpClient.Connect(IPAddress.Parse(ipAddress), port);
		}

		public byte[] Read(int offset, int size)
		{
			if (size == 0) return new byte[0];

			if (ReadBuffer.Length != size)
			{
				ReadBuffer = new byte[size];
			}

			Stream.Read(ReadBuffer, offset, size);
			return ReadBuffer;
		}

		public void Write(byte[] buffer, int offset, int size)
		{
			Stream.Write(buffer, offset, size);
		}
	}
}
