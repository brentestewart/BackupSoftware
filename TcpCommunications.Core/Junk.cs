using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using TcpCommunications.Core.Messages;

namespace TcpCommunications.Core
{
	//public class ClientCommunicator
	//{
	//	public INetworkClient Client { get; set; }
	//	public string ClientName { get; set; }
	//	private Queue<INetworkMessage> InboundMessages { get; set; } = new Queue<INetworkMessage>();
	//	private Queue<INetworkMessage> OutboundMessages { get; set; } = new Queue<INetworkMessage>();
	//	private Task InboundQueueTask { get; set; }
	//	private Task InboundTrafficTask { get; set; }
	//	private Task OutboundQueueTask { get; set; }

	//	public ClientCommunicator(string clientName)
	//	{
	//		ClientName = clientName;
	//		Client = new NetworkClient("Junk", new TcpClient());
	//	}

	//	public async Task ConnectToServer(string ipAddress, int port)
	//	{
	//		Client.Connect(ipAddress, port);
	//		var stream = Client.GetStream();

	//		// Watch inbound queue
	//		InboundQueueTask = Task.Run(async () =>
	//		{
	//			while (true)
	//			{
	//				while (InboundMessages.Count == 0)
	//				{
	//					await Task.Delay(100);
	//				}

	//				ProcessMessage(InboundMessages.Dequeue());
	//			}
	//		});

	//		// Inbound network traffic
	//		InboundTrafficTask = Task.Run(async () =>
	//		{
	//			while (true)
	//			{
	//				while (!stream.DataAvailable)
	//				{
	//					await Task.Delay(100);
	//				}

	//				var message = MessageFactory.ReadMessage(stream);
	//				InboundMessages.Enqueue(message);
	//			}
	//		});

	//		OutboundQueueTask = Task.Run(async () =>
	//		{
	//			while (true)
	//			{
	//				while (OutboundMessages.Count == 0)
	//				{
	//					await Task.Delay(100);
	//				}

	//				var newMessage = OutboundMessages.Dequeue();
	//				var bytes = newMessage.GetMessageBytes();
	//				stream.Write(bytes, 0, bytes.Length);
	//			}
	//		});

	//		while ((InboundQueueTask.Status != TaskStatus.Running && InboundQueueTask.Status != TaskStatus.WaitingForActivation) ||
	//			(InboundTrafficTask.Status != TaskStatus.Running && InboundTrafficTask.Status != TaskStatus.WaitingForActivation) ||
	//			(OutboundQueueTask.Status != TaskStatus.Running && OutboundQueueTask.Status != TaskStatus.WaitingForActivation))
	//		{
	//			await Task.Delay(100);
	//		}

	//		var clientInfoMessage = new ClientInfoMessage(ClientName);
	//		OutboundMessages.Enqueue(clientInfoMessage);
	//	}

	//	private void ProcessMessage(INetworkMessage message)
	//	{
	//		switch (message.MessageType)
	//		{
	//			//case 1: //ConnectedMessage
	//			//	var clientInfoMessage = new ClientInfoMessage(ClientName);
	//			//	OutboundMessages.Enqueue(clientInfoMessage);
	//			//	break;
	//			case 20000:
	//				Console.WriteLine($"Message = {((MockMessage)message).Message}");
	//				break;
	//			default:
	//				break;
	//		}
	//	}

	//	public void SendMessage(string message)
	//	{
	//		var mockMessage = new MockMessage(message);
	//		OutboundMessages.Enqueue(mockMessage);
	//	}
	//}
}
