using System;
using System.Collections.Generic;

namespace TcpCommunications.Core.Messages
{
	public abstract class NetworkMessageBase : INetworkMessage
	{
		public int MessageId { get; set; }

		public abstract int MessageType { get; }

		public int PayloadLength => Payload.Length;

		public abstract byte[] Payload { get; set; }

		public virtual byte[] GetMessageBytes()
		{
			var byteList = new List<byte>();
			byteList.AddRange(BitConverter.GetBytes(MessageId));
			byteList.AddRange(BitConverter.GetBytes(PayloadLength));
			byteList.AddRange(BitConverter.GetBytes(MessageType));
			byteList.AddRange(Payload);
			return byteList.ToArray();
		}
	}
}
