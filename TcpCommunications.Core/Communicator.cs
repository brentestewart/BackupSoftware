using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TcpCommunications.Core.Messages;

namespace TcpCommunications.Core
{
	public class Communicator : ICommunicator
    {
		public event EventHandler<MessageReceivedArgs> OnMessageReceived;
	    public event EventHandler<string> OnClientConnected;

		#region members
		private string ServerName { get; set; }
		private INetworkListener Listener { get; set; }
		private int ServerPort { get; set; }
		private const int DefaultServerPort = 13001;
		private Dictionary<string, INetworkClient> Clients { get; set; } = new Dictionary<string, INetworkClient>();
		private INetworkClient Client { get; set; }
		private CancellationTokenSource ServerCancellationTokenSource { get; set; }
		private int CurrentMessageId { get; set; }
		private object GetNextMessageIdLock { get; set; } = new object();
	    private bool ConnectedToServer { get; set; } = false;
		#endregion

		public Communicator(int serverPort = DefaultServerPort)
			: this(serverPort, new NetworkClient(Environment.MachineName, new TcpClient()), new NetworkListener())
		{ }

		public Communicator(int serverPort, INetworkClient client, INetworkListener listener)
        {
			ServerName = Environment.MachineName;
            ServerPort = serverPort;
			Client = client;
			Listener = listener;
        }

        public async Task StartServer(int maxClients = 100)
        {
			ServerCancellationTokenSource = new CancellationTokenSource();
			Listener.Start(ServerPort);
	        var taskStarted = false;

			var task = Task.Run(async () =>
			{
				var numberOfAttachedClients = 0;
				ServerCancellationTokenSource.Token.ThrowIfCancellationRequested();
				taskStarted = true;
				while (true)
				{
					if (numberOfAttachedClients < maxClients)
					{
						var client = await Listener.AcceptNetworkClientAsync(ServerName);
						if (client != null)
						{
							numberOfAttachedClients++;
							OnClientConnected?.Invoke(this, client.ClientName);
							await StartSession(client);
						}
					}
					await Task.Delay(1);
				}
			}, ServerCancellationTokenSource.Token);

	        while (!taskStarted)
	        {
		        await Task.Delay(1);
	        }
        }

		public async Task ShutdownServer()
		{
			foreach (var client in Clients)
			{
				await StopSession(client.Value);
			}

			Listener.Stop();
			ServerCancellationTokenSource.Cancel();
		}

		public async Task ConnectToServer(string ipAddress, int port)
		{
			//Client.ClientName = serverName;
			await Client.Connect(ipAddress, port);

			await WatchInboundQueue(Client);
			await WatchForInboundNetworkTraffic(Client);
			await WatchForOutboundMessages(Client);

			var clientInfoMessage = new ClientInfoMessage(Client.ClientName) { MessageId = GetNextMessageId() };
			Client.OutboundMessages.Enqueue(clientInfoMessage);

			while (!ConnectedToServer)
			{
				await Task.Delay(1);
			}
		}

		public async Task DisconnectFromServer()
		{
			ConnectedToServer = false;
			var message = DisconnectMessage.Insatnce;
			SendMessage(message);

			await StopSession(Client);
		}

		public Task SendMessage(string clientName, INetworkMessage message)
		{
			if (!Clients.ContainsKey(clientName))
			{
				if (clientName == Client?.ClientName)
				{
					return SendMessage(message);
				}
				else
				{
					throw new Exception("Client not found");
				}
			}

			message.MessageId = GetNextMessageId();

			var client = Clients[clientName];
			client.OutboundMessages.Enqueue(message);

			return Task.CompletedTask;
		}

		public async Task SendMessage(INetworkMessage message)
		{
			if (!ConnectedToServer) throw new Exception("Not connected to a server");

			message.MessageId = GetNextMessageId();
			Client.OutboundMessages.Enqueue(message);
		}

		private async Task StartSession(INetworkClient currentClient)
		{
			Clients.Add(currentClient.ClientName, currentClient);
			await WatchInboundQueue(currentClient);
			await WatchForInboundNetworkTraffic(currentClient);
			await WatchForOutboundMessages(currentClient);
		}

		private async Task StopSession(INetworkClient client)
		{
			client.CancellationTokenSource.Cancel();

			while((!client.WatchForInboundNetworkTrafficTask.IsFaulted && !client.WatchForInboundNetworkTrafficTask.IsCanceled) ||
				(!client.WatchInboundQueueTask.IsFaulted && !client.WatchInboundQueueTask.IsCanceled) ||
				(!client.WatchForOutboundMessagesTask.IsFaulted && !client.WatchForOutboundMessagesTask.IsCanceled))
			{
				await Task.Delay(1);
			}
		}

		private int GetNextMessageId()
		{
			lock(GetNextMessageIdLock)
			{
				return ++CurrentMessageId;
			}
		}

		private async Task WatchForOutboundMessages(INetworkClient currentClient)
		{
			var token = currentClient.CancellationTokenSource.Token;
			var taskStarted = false;
			var task = Task.Run(async () =>
			{
				try
				{
					token.ThrowIfCancellationRequested();
					var stream = currentClient.GetCommunicationStream();
					taskStarted = true;
					while (true)
					{
						while (currentClient.OutboundMessages.Count == 0)
						{
							if (token.IsCancellationRequested)
							{
								token.ThrowIfCancellationRequested();
							}

							await Task.Delay(1, token);
						}

						INetworkMessage newMessage;
						if (currentClient.OutboundMessages.TryDequeue(out newMessage))
						{
							var messageBytes = newMessage.GetMessageBytes();
							stream.Write(messageBytes, 0, messageBytes.Length);
						}
					}

				}
				catch (Exception ex)
				{
					throw;
				}
			}, currentClient.CancellationTokenSource.Token);

			currentClient.WatchForOutboundMessagesTask = task;

			while (!taskStarted)
			{
				await Task.Delay(1, token);
			}
		}

		private async Task WatchForInboundNetworkTraffic(INetworkClient currentClient)
		{
			var token = currentClient.CancellationTokenSource.Token;
			var taskStarted = false;
			var task = Task.Run(async () =>
			{
				try
				{
					token.ThrowIfCancellationRequested();
					var stream = currentClient.GetCommunicationStream();
					taskStarted = true;
					while (true)
					{
						while (!stream.DataAvailable)
						{
							if (token.IsCancellationRequested)
							{
								token.ThrowIfCancellationRequested();
							}

							await Task.Delay(1, token);
						}

						var message = MessageFactory.ReadMessage(stream);
						currentClient.InboundMessages.Enqueue(message);
					}

				}
				catch (Exception ex)
				{
					throw;
				}
			}, currentClient.CancellationTokenSource.Token);

			currentClient.WatchForInboundNetworkTrafficTask = task;

			while (!taskStarted)
			{
				await Task.Delay(1, token);
			}
		}

		private async Task WatchInboundQueue(INetworkClient currentClient)
		{
			var token = currentClient.CancellationTokenSource.Token;
			var taskStarted = false;
			var task = Task.Run(async () =>
			{
				try
				{
					token.ThrowIfCancellationRequested();
					taskStarted = true;
					while (true)
					{
						while (currentClient.InboundMessages.Count == 0)
						{
							if (token.IsCancellationRequested)
							{
								token.ThrowIfCancellationRequested();
							}

							await Task.Delay(1, token);
						}

						INetworkMessage message;
						if (currentClient.InboundMessages.TryDequeue(out message))
						{
							switch (message)
							{
								case ClientConnectedMessage connectedMessage:
									ConnectedToServer = true;
									break;
								default:
									OnMessageReceived?.Invoke(currentClient,
										new MessageReceivedArgs {Message = message, ClientName = currentClient.ClientName});
									break;
							}
						}
					}

				}
				catch (Exception ex)				
				{
				}
			}, currentClient.CancellationTokenSource.Token);

			currentClient.WatchInboundQueueTask = task;

			while (!taskStarted)
			{
				await Task.Delay(1, token);
			}
		}
	}
}
