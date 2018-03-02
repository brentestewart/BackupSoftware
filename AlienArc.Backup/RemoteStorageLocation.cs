using System;
using System.Collections.Generic;
using System.IO;
using AlienArc.Backup.Common;
using AlienArc.Backup.IO;
using TcpCommunications.Core;
using TcpCommunications.Core.Messages;

namespace AlienArc.Backup
{
	public class RemoteFileServer : FileMessageProcessor
	{
		protected const int CommunicationPort = 55444;
		protected ICommunicator Communicator { get; }

		public RemoteFileServer(ICommunicator communicator)
		{
			Communicator = communicator;
			Communicator.OnMessageReceived += ProcessMessage;
		}

		public void StartServer()
		{
			Communicator.StartServer(1000);
		}

		public void ShutdownServer()
		{
			Communicator.ShutdownServer();
		}

	}

	public abstract class FileMessageProcessor
	{
		protected Dictionary<byte[], Stream> FilesInProgress { get; set; } = new Dictionary<byte[], Stream>();
		protected void ProcessMessage(object sender, MessageReceivedArgs e)
		{
			switch (e.Message)
			{
				case FileStartMessage message:
					FilesInProgress.Add(message.FileHash, new MemoryStream());
					break;
				case FileChunckMessage message:
					if (FilesInProgress.ContainsKey(message.FileHash))
					{
						var stream = FilesInProgress[message.FileHash];
						stream.Write(message.Payload, 0, message.PayloadLength);
					}
					break;
				case FileEndMessage message:
					if (FilesInProgress.ContainsKey(message.FileHash))
					{
						var stream = FilesInProgress[message.FileHash];
						stream.Flush();
						stream.Dispose();
					}
					break;
				case AbandonFileTransferMessage message:
					if (FilesInProgress.ContainsKey(message.FileHash))
					{
						var stream = FilesInProgress[message.FileHash];
						stream.Dispose();
						FilesInProgress.Remove(message.FileHash);
					}
					break;
			}

		}
	}

	public class RemoteStorageLocation : FileMessageProcessor, IStorageLocation
	{
		protected ICommunicator Communicator { get; }
		protected const int CommunicationPort = 55444;
		public string RootPath { get; }
		public StorageLocationType LocationType => StorageLocationType.Remote;
		protected const int BufferSize = 65536;

		public RemoteStorageLocation(ICommunicator communicator, string path)
		{
			Communicator = communicator;
			RootPath = path;
			Communicator.OnMessageReceived += ProcessMessage;
			Communicator.ConnectToServer(path, CommunicationPort);
		}

		public bool StoreFile(IBackupFile file, byte[] hash)
		{
			Communicator.SendMessage(new FileStartMessage(hash));
			var totalBytes = file.Length;
			var buffer = new byte[BufferSize];

			var position = 0L;
			var remainingBytes = totalBytes;
			var message = new FileChunckMessage(hash);
			while (position < totalBytes)
			{
				using (var fileStream = file.OpenRead())
				{
					var bytesToRead = (int) ((remainingBytes > BufferSize) ? BufferSize : remainingBytes);
					if(bytesToRead < BufferSize) buffer = new byte[bytesToRead]; 
					position += fileStream.Read(buffer, 0, bytesToRead);
					message.Payload = buffer;
					Communicator.SendMessage(message);
				}
			}

			// Verify file
			return true;
		}

		public bool StoreFile(string filePath, byte[] hash)
		{
			var file = new BackupFile(filePath);
			return StoreFile(file, hash);
		}

		public Stream GetFile(byte[] fileHash)
		{
			var requestMessage = new GetFileMessage(fileHash);

			var outStream = new MemoryStream();
			FilesInProgress.Add(fileHash, outStream);
			Communicator.SendMessage(requestMessage);

			return outStream;
		}
	}
}