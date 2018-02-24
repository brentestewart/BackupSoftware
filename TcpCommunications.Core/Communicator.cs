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

			var task = Task.Run(async () =>
			{
				var numberOfAttachedClients = 0;
				ServerCancellationTokenSource.Token.ThrowIfCancellationRequested();
				while (true)
				{
					if (numberOfAttachedClients < maxClients)
					{
						var client = await Listener.AcceptNetworkClientAsync();
						numberOfAttachedClients++;
						StartSession(client);
					}
					await Task.Delay(1000);
				}
			}, ServerCancellationTokenSource.Token);

			while(task.Status != TaskStatus.WaitingForActivation &&
				task.Status != TaskStatus.Running)
			{
				await Task.Delay(10);
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

		public void ConnectToServer(string ipAddress, int port)
		{
			//Client.ClientName = serverName;
			Client.Connect(ipAddress, port);

			WatchInboundQueue(Client);
			WatchForInboundNetworkTraffic(Client);
			WatchForOutboundMessages(Client);

			var clientInfoMessage = new ClientInfoMessage(ServerName);
			clientInfoMessage.MessageId = GetNextMessageId();
			Client.OutboundMessages.Enqueue(clientInfoMessage);
		}

		public async Task DisconnectFromServer()
		{
			var message = DisconnectMessage.Insatnce;
			SendMessage(message);

			await StopSession(Client);
		}

		public void SendMessage(string clientName, INetworkMessage message)
		{
			if (!Clients.ContainsKey(clientName)) throw new Exception("Client not found");

			message.MessageId = GetNextMessageId();

			var client = Clients[clientName];
			client.OutboundMessages.Enqueue(message);
		}

		public void SendMessage(INetworkMessage message)
		{
			message.MessageId = GetNextMessageId();
			Client.OutboundMessages.Enqueue(message);
		}

		private void StartSession(INetworkClient currentClient)
		{
			Clients.Add(currentClient.ClientName, currentClient);
			WatchInboundQueue(currentClient);
			WatchForInboundNetworkTraffic(currentClient);
			WatchForOutboundMessages(currentClient);
		}

		private async Task StopSession(INetworkClient client)
		{
			client.CancellationTokenSource.Cancel();

			while((!client.WatchForInboundNetworkTrafficTask.IsFaulted && !client.WatchForInboundNetworkTrafficTask.IsCanceled) ||
				(!client.WatchInboundQueueTask.IsFaulted && !client.WatchInboundQueueTask.IsCanceled) ||
				(!client.WatchForOutboundMessagesTask.IsFaulted && !client.WatchForOutboundMessagesTask.IsCanceled))
			{
				await Task.Delay(10);
			}
		}

		private int GetNextMessageId()
		{
			lock(GetNextMessageIdLock)
			{
				return ++CurrentMessageId;
			}
		}

		private async void WatchForOutboundMessages(INetworkClient currentClient)
		{
			var token = currentClient.CancellationTokenSource.Token;
			var task = Task.Run(async () =>
			{
				token.ThrowIfCancellationRequested();
				var stream = currentClient.GetCommunicationStream();
				while (true)
				{
					while (currentClient.OutboundMessages.Count == 0)
					{
						if (token.IsCancellationRequested)
						{
							token.ThrowIfCancellationRequested();
						}

						await Task.Delay(100);
					}

					INetworkMessage newMessage;
					if(currentClient.OutboundMessages.TryDequeue(out newMessage))
					{ 
						var messageByte = newMessage.GetMessageBytes();
						stream.Write(messageByte, 0, messageByte.Length);
					}
				}
			}, currentClient.CancellationTokenSource.Token);

			currentClient.WatchForOutboundMessagesTask = task;

			while (task.Status != TaskStatus.WaitingForActivation 
				&& task.Status != TaskStatus.Running 
				&& task.Status != TaskStatus.Canceled)
			{
				await Task.Delay(10);
			}
		}

		private async void WatchForInboundNetworkTraffic(INetworkClient currentClient)
		{
			var token = currentClient.CancellationTokenSource.Token;
			var task = Task.Run(async () =>
			{
				token.ThrowIfCancellationRequested();
				var stream = currentClient.GetCommunicationStream();
				while (true)
				{
					while (!stream.DataAvailable)
					{
						if (token.IsCancellationRequested)
						{
							token.ThrowIfCancellationRequested();
						}

						await Task.Delay(100);
					}

					var message = MessageFactory.ReadMessage(stream);
					currentClient.InboundMessages.Enqueue(message);
				}
			}, currentClient.CancellationTokenSource.Token);

			currentClient.WatchForInboundNetworkTrafficTask = task;

			while (task.Status != TaskStatus.WaitingForActivation && task.Status != TaskStatus.Running)
			{
				await Task.Delay(10);
			}
		}

		private async void WatchInboundQueue(INetworkClient currentClient)
		{
			var token = currentClient.CancellationTokenSource.Token;
			var task = Task.Run(async () =>
			{
				try
				{
					token.ThrowIfCancellationRequested();

					while (true)
					{
						while (currentClient.InboundMessages.Count == 0)
						{
							if (token.IsCancellationRequested)
							{
								token.ThrowIfCancellationRequested();
							}

							await Task.Delay(100);
						}

						INetworkMessage message;
						if (currentClient.InboundMessages.TryDequeue(out message))
						{
							try
							{
								OnMessageReceived?.Invoke(this, new MessageReceivedArgs { Message = message, ClientName = currentClient.ClientName });
							}
							catch { }
						}
					}

				}
				catch (Exception ex)				
				{
				}
				finally
				{
				}
			}, currentClient.CancellationTokenSource.Token);

			currentClient.WatchInboundQueueTask = task;

			while (task.Status != TaskStatus.WaitingForActivation && task.Status != TaskStatus.Running)
			{
				await Task.Delay(10);
			}
		}
	}
}
