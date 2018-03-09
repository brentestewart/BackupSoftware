using System;
using System.Collections.Generic;
using AlienArc.Backup.Common.Utilities;

namespace TcpCommunications.Core.Messages
{
	public sealed class FileChunckMessage : NetworkMessageBase
	{
		public byte[] FileHash { get; set; } = new byte[20];
		public override int MessageType => 11;
		public override byte[] Payload { get; set; } //Combo of filehash and filedata
		public override int PayloadLength => FileHash.Length + DataChecksum.Length + FileData.Length;
		public byte[] FileData { get; set; }
		public byte[] DataChecksum { get; set; } = new byte[20];

		public FileChunckMessage() { }

		public FileChunckMessage(byte[] payload)
		{
			Payload = payload;
			var dataLength = Payload.Length - 40;
			FileData = new byte[dataLength];
			Array.Copy(Payload, FileHash, 20);
			Array.Copy(Payload, 20, DataChecksum, 0, 20);
			Array.Copy(Payload, 40, FileData, 0, dataLength);
		}

		public override byte[] GetMessageBytes()
		{
			var byteList = new List<byte>();
			byteList.AddRange(BitConverter.GetBytes(MessageId));
			byteList.AddRange(BitConverter.GetBytes(PayloadLength));
			byteList.AddRange(BitConverter.GetBytes(MessageType));
			byteList.AddRange(FileHash);
			byteList.AddRange(Hasher.GetFileHash(FileData)); //Hash check for data
			byteList.AddRange(FileData);
			return byteList.ToArray();
		}
	}
}