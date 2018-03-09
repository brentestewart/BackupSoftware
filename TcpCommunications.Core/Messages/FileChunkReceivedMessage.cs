using System;
using System.Collections.Generic;

namespace TcpCommunications.Core.Messages
{
	public sealed class FileChunkReceivedMessage : NetworkMessageBase
	{
		public override int MessageType => 15;
		public override byte[] Payload { get; set; }
		public override int PayloadLength => 40;
		public byte[] FileHash { get; set; } = new byte[20];
		public byte[] DataChecksum { get; set; } = new byte[20];

		public FileChunkReceivedMessage(byte[] payload)
		{
			Payload = payload;
			Array.Copy(Payload, FileHash, 20);
			Array.Copy(Payload, 20, DataChecksum, 0, 20);
		}

		public FileChunkReceivedMessage(byte[] fileHash, byte[] dataChecksum)
		{
			FileHash = fileHash;
			DataChecksum = dataChecksum;
		}

		public override byte[] GetMessageBytes()
		{
			var byteList = new List<byte>();
			byteList.AddRange(BitConverter.GetBytes(MessageId));
			byteList.AddRange(BitConverter.GetBytes(PayloadLength));
			byteList.AddRange(BitConverter.GetBytes(MessageType));
			byteList.AddRange(FileHash);
			byteList.AddRange(DataChecksum);
			return byteList.ToArray();
		}
	}
}