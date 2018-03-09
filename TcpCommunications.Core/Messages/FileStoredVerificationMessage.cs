using System;
using System.Collections.Generic;
using System.Text;

namespace TcpCommunications.Core.Messages
{
	public sealed class FileStoredVerificationMessage : NetworkMessageBase
	{
		public byte[] FileHash { get; set; }
		public bool Success { get; set; }
		public override int PayloadLength => 20 + 1 + Encoding.ASCII.GetBytes(FilePath).Length;
		public override int MessageType => 14;
		public override byte[] Payload { get; set; }
		public string FilePath { get; set; }

		public FileStoredVerificationMessage(byte[] fileHash, bool success, string filePath)
		{
			FileHash = fileHash;
			Success = success;
			FilePath = filePath;
		}

		public FileStoredVerificationMessage(byte[] payload)
		{
			Payload = payload;
			var dataLength = Payload.Length - 20 - 1;
			FileHash = new byte[20];
			var filePathArray = new byte[dataLength];
			Array.Copy(Payload, FileHash, 20);
			Success = BitConverter.ToBoolean(Payload, 20);
			Array.Copy(Payload, 21, filePathArray, 0, dataLength);
			FilePath = Encoding.ASCII.GetString(filePathArray);
		}

		public override byte[] GetMessageBytes()
		{
			var byteList = new List<byte>();
			byteList.AddRange(BitConverter.GetBytes(MessageId));
			byteList.AddRange(BitConverter.GetBytes(PayloadLength));
			byteList.AddRange(BitConverter.GetBytes(MessageType));
			byteList.AddRange(FileHash);
			byteList.AddRange(BitConverter.GetBytes(Success));
			byteList.AddRange(Encoding.ASCII.GetBytes(FilePath));
			return byteList.ToArray();
		}
	}
}