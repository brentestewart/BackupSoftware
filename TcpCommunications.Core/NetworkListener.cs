using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using TcpCommunications.Core.Messages;

namespace TcpCommunications.Core
{
	public class NetworkListener : INetworkListener
	{
		private TcpListener Listener { get; set; }

		public void Start(int port)
		{
			var hostEntry = Dns.GetHostEntry(Environment.MachineName);
			IPAddress address = null;
			foreach (var ip in hostEntry.AddressList)
			{
				if(ip.AddressFamily == AddressFamily.InterNetwork)
				{
					address = ip;
					break;
				}
			}
			Listener = new TcpListener(address, port);
			Listener.Start();
		}

		public async Task<INetworkClient> AcceptNetworkClientAsync()
		{
			var tcpClient = await Listener.AcceptTcpClientAsync();
			var client = new NetworkClient("Temp", tcpClient);

			var stream = client.GetCommunicationStream();
			while(!stream.DataAvailable)
			{
				await Task.Delay(10);
			}

			var message = MessageFactory.ReadMessage(stream);

			if (!(message is ClientInfoMessage clientInfoMessage))
			{
				//Handle bad connection attempt
				return null;
			}

			client.ClientName = clientInfoMessage.ClientName;
			return client;
			//return new NetworkClient(clientInfoMessage.ClientName, tcpClient);
		}

		public void Stop()
		{
			Listener.Stop();
		}
	}
}
