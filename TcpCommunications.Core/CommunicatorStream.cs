using System.Net;
using System.Net.Sockets;

namespace TcpCommunications.Core
{
	public class CommunicatorStream : INetworkStream
	{
		public TcpClient TcpClient { get; }
		public bool DataAvailable => Stream.DataAvailable;
		private NetworkStream stream;
		private NetworkStream Stream => stream == null ? (stream = TcpClient.GetStream()) : stream;
		public CommunicatorStream(TcpClient tcpClient)
		{
			TcpClient = tcpClient;
		}

		public void Connect(string ipAddress, int port)
		{
			TcpClient.Connect(IPAddress.Parse(ipAddress), port);
		}

		public byte[] Read(int offset, int size)
		{
			if (size == 0) return new byte[0];

			var bytes = new byte[size];
			Stream.Read(bytes, offset, size);
			return bytes;
		}

		public void Write(byte[] buffer, int offset, int size)
		{
			Stream.Write(buffer, offset, size);
		}
	}
}
