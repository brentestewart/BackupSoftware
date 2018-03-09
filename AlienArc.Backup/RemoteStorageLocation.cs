using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Runtime;
using System.Threading.Tasks;
using AlienArc.Backup.Common;
using AlienArc.Backup.Common.Utilities;
using AlienArc.Backup.IO;
using TcpCommunications.Core;
using TcpCommunications.Core.Messages;

namespace AlienArc.Backup
{
	public interface IFileServer
	{
		Task StartServer();
		Task StopServer();
	}

	public class RemoteFileServer : FileMessageProcessor, IFileServer
	{
		protected const int CommunicationPort = 13001;
		protected string StoragePath { get; set; }
		protected List<string> ConnectedClients { get; set; } = new List<string>();

		public RemoteFileServer(ICommunicator communicator, string pathToStorage, string pathToTempStorage)
			: base(pathToTempStorage, communicator)
		{
			StoragePath = pathToStorage;
			TempStorageDirectory = pathToTempStorage;
			Communicator.OnMessageReceived += ProcessMessage;
			Communicator.OnClientConnected += ClientConnected;

			var storageDirectory = new DirectoryInfo(StoragePath);
			if(!storageDirectory.Exists) storageDirectory.Create();
		}

		private void ClientConnected(object sender, string e)
		{
			ConnectedClients.Add(e);
		}

		public async Task StartServer()
		{
			await Communicator.StartServer(1000);
		}

		public async Task StopServer()
		{
			await Communicator.ShutdownServer();
		}

		public async Task SendMessage(string clientName, INetworkMessage message)
		{
			await Communicator.SendMessage(clientName, message);

			//await Task.Delay(5000);
		}

		protected override void ReadFile(string clientName, byte[] fileHash, FileInfo file)
		{
			//var fileInfo = new FileInfo(Path.Combine(StoragePath, Hasher.GetDirectoryNameFromHash(fileHash), Hasher.GetFileNameFromHash(fileHash)));
			var directory = new DirectoryInfo(Path.Combine(StoragePath, Hasher.GetDirectoryNameFromHash(fileHash)));
			var fileInfo = new FileInfo(Path.Combine(directory.FullName, Hasher.GetFileNameFromHash(fileHash)));
			if (!directory.Exists)
			{
				directory.Create();
			}

			file.MoveTo(fileInfo.FullName);
			//using (var outStream = fileInfo.Create())
			//{
			//	fileStream.CopyTo(outStream);
			//}

			var verified = false;
			using (var verifyStream = fileInfo.OpenRead())
			{
				var verifyHash = Hasher.GetFileHash(verifyStream);
				if (verifyHash.SequenceEqual(fileHash))
				{
					verified = true;
				}
			}

			Communicator.SendMessage(clientName, new FileStoredVerificationMessage(fileHash, verified, fileInfo.FullName));
		}

		protected override async Task SendFile(string clientName, byte[] fileHash)
		{
			var fileInfo = new FileInfo(Path.Combine(StoragePath, Hasher.GetDirectoryNameFromHash(fileHash), Hasher.GetFileNameFromHash(fileHash)));
			var buffer = new byte[BufferSize];
			var totalBytes = fileInfo.Length;

			await Communicator.SendMessage(clientName, new FileStartMessage(fileHash));
			var chunkMessage = new FileChunckMessage {FileHash = fileHash};
			if (FileTransferResponse.ContainsKey(fileHash)) FileTransferResponse.Remove(fileHash);
			FileTransferResponse.Add(fileHash, false);
			using (var fileStream = fileInfo.OpenRead())
			{
				var bytesRemaining = totalBytes;
				while (bytesRemaining > 0)
				{
					var chunkSize =(int) ((bytesRemaining < BufferSize) ? bytesRemaining : BufferSize);
					if(chunkSize < BufferSize) buffer = new byte[chunkSize];

					var bytesRead = fileStream.Read(buffer, 0, chunkSize);
					chunkMessage.FileData = buffer;
					await Communicator.SendMessage(clientName, chunkMessage);
					bytesRemaining -= bytesRead;

					while (!FileTransferResponse[fileHash])
					{
						await Task.Delay(1);
					}

					FileTransferResponse[fileHash] = false;
				}
			}

			await Communicator.SendMessage(clientName, new FileEndMessage(fileHash));
		}
	}

	public abstract class FileMessageProcessor
	{
		protected ICommunicator Communicator { get; }
		protected string TempStorageDirectory { get; set; }
		protected const int BufferSize = 524288;// 65536;
		protected Dictionary<byte[], Stream> FilesInProgress { get; set; } = new Dictionary<byte[], Stream>(HashComparer.Instance);
		protected Dictionary<byte[], INetworkMessage> VerificationMessages { get; set; } = new Dictionary<byte[], INetworkMessage>(HashComparer.Instance);
		protected Dictionary<byte[], bool> FileTransferResponse { get; set; } = new Dictionary<byte[], bool>(HashComparer.Instance);

		protected FileMessageProcessor(string pathToTempStorageDirectory, ICommunicator communicator)
		{
			Communicator = communicator;
			TempStorageDirectory = pathToTempStorageDirectory;
			VerifyTempStorageDirectoryExist();
		}

		protected void ProcessMessage(object sender, MessageReceivedArgs e)
		{
			switch (e.Message)
			{
				case FileStartMessage message:
					if (!FilesInProgress.ContainsKey(message.Payload))
					{
						var fileHash = message.Payload;
						var subDir = new DirectoryInfo(Path.Combine(TempStorageDirectory, Hasher.GetDirectoryNameFromHash(fileHash)));
						if(!subDir.Exists) subDir.Create();
						var fileName = Hasher.GetFileNameFromHash(fileHash);
						var filePath = Path.Combine(subDir.FullName, fileName);
						FilesInProgress.Add(message.Payload, File.OpenWrite(filePath));
					}
					break;
				case FileChunckMessage message:
					if (FilesInProgress.ContainsKey(message.FileHash))
					{
						var stream = FilesInProgress[message.FileHash];
						stream.Write(message.FileData, 0, message.FileData.Length);
						var receipt = new FileChunkReceivedMessage(message.FileHash, message.DataChecksum);
						Communicator.SendMessage(((INetworkClient) sender).ClientName, receipt);
					}
					break;
				case FileEndMessage message:
					if (FilesInProgress.ContainsKey(message.Payload))
					{
						var stream = FilesInProgress[message.Payload];
						stream.Flush();

						var fileHash = message.Payload;
						var subDir = Hasher.GetDirectoryNameFromHash(fileHash);
						var fileName = Hasher.GetFileNameFromHash(fileHash);
						var filePath = Path.Combine(TempStorageDirectory, subDir, fileName);
						stream.Dispose();
						ReadFile(((INetworkClient)sender).ClientName, message.Payload, new FileInfo(filePath));
						FilesInProgress.Remove(message.Payload);
					}
					break;
				case AbandonFileTransferMessage message:
					if (FilesInProgress.ContainsKey(message.Payload))
					{
						var stream = FilesInProgress[message.Payload];
						stream.Dispose();
						FilesInProgress.Remove(message.Payload);
					}
					break;
				case FileChunkReceivedMessage message:
					FileTransferResponse[message.FileHash] = true;
					break;
				case FileStoredVerificationMessage message:
					VerificationMessages.Add(message.FileHash, message);
					break;
				case GetFileMessage message:
					SendFile(((INetworkClient) sender).ClientName, message.Payload);
					break;
			}
		}

		protected void VerifyTempStorageDirectoryExist()
		{
			var tempStorageDirectory = new DirectoryInfo(TempStorageDirectory);
			if(!tempStorageDirectory.Exists) tempStorageDirectory.Create();
		}

		protected abstract void ReadFile(string clientName, byte[] fileHash, FileInfo file);

		protected abstract Task SendFile(string clientName, byte[] fileHash);
	}

	public class RemoteStorageLocation : FileMessageProcessor, IStorageLocation
	{
		protected const int CommunicationPort = 13001;
		public string RootPath { get; }
		public StorageLocationType LocationType => StorageLocationType.Remote;

		public RemoteStorageLocation(ICommunicator communicator, string path, string pathToTempStorage) 
			: base(pathToTempStorage, communicator)
		{
			RootPath = path;
			Communicator.OnMessageReceived += ProcessMessage;
		}

		public async Task Connect()
		{
			await TryConnectToServer();
		}

		private async Task TryConnectToServer()
		{
			while (!(await ConnectToServer()))
			{
				await Task.Delay(1);
			}
		}

		private async Task<bool> ConnectToServer()
		{
			try
			{
				await Communicator.ConnectToServer(RootPath, CommunicationPort);
			}
			catch (Exception e)
			{
				return false;
			}

			return true;
		}

		public async Task<bool> StoreFile(IBackupFile file, byte[] hash)
		{
			var success = false;

			await Communicator.SendMessage(new FileStartMessage(hash));
			var totalBytes = file.Length;
			var buffer = new byte[BufferSize];

			var position = 0L;
			var remainingBytes = totalBytes;
			var message = new FileChunckMessage() { FileHash = hash };
			if (FileTransferResponse.ContainsKey(hash)) FileTransferResponse.Remove(hash);
			FileTransferResponse.Add(hash, false);
			using (var fileStream = file.OpenRead())
			{
				while (position < totalBytes)
				{
					var bytesToRead = (int) ((remainingBytes > BufferSize) ? BufferSize : remainingBytes);
					if (bytesToRead < BufferSize) buffer = new byte[bytesToRead];
					position += fileStream.Read(buffer, 0, bytesToRead);
					remainingBytes = totalBytes - position;
					message.FileData = buffer;
					await Communicator.SendMessage(message);

					while (!FileTransferResponse[hash])
					{
						await Task.Delay(1);
					}

					FileTransferResponse[hash] = false;
				}
			}

			await Communicator.SendMessage(new FileEndMessage(hash));

			// Watch the inbound messages for a verification message
			var gotVerificationMessage = false;
			while (!gotVerificationMessage)
			{
				if (VerificationMessages.ContainsKey(hash))
				{
					var verifyMessage = VerificationMessages[hash] as FileStoredVerificationMessage;
					success = verifyMessage?.Success ?? false;
					VerificationMessages.Remove(hash);
					gotVerificationMessage = true;
				}

				await Task.Delay(1);
			}

			return success;
		}

		public async Task<bool> StoreFile(string filePath, byte[] hash)
		{
			var file = new BackupFile(filePath);
			return await StoreFile(file, hash);
		}

		public async Task<string> GetFile(byte[] fileHash)
		{
			var requestMessage = new GetFileMessage(fileHash);

			var subDir = new DirectoryInfo(Path.Combine(TempStorageDirectory, Hasher.GetDirectoryNameFromHash(fileHash)));
			if (!subDir.Exists) subDir.Create();
			var fileName = Hasher.GetFileNameFromHash(fileHash);
			var filePath = Path.Combine(subDir.FullName, fileName);
			FilesInProgress.Add(fileHash, File.OpenWrite(filePath));

			await Communicator.SendMessage(requestMessage);

			// wait for transfer
			var gotVerificationMessage = false;
			string resultsPath = null;
			while (!gotVerificationMessage)
			{
				if (VerificationMessages.ContainsKey(fileHash))
				{
					var verifyMessage = VerificationMessages[fileHash] as FileStoredVerificationMessage;
					resultsPath = (verifyMessage?.Success ?? false) ? verifyMessage.FilePath : null;
					VerificationMessages.Remove(fileHash);
					gotVerificationMessage = true;
				}

				await Task.Delay(1);
			}

			return resultsPath;
		}

		protected override void ReadFile(string clientName, byte[] fileHash, FileInfo file)
		{
			
			//var verified = false;
			//using (var verifyStream = fileInfo.OpenRead())
			//{
			//	var verifyHash = Hasher.GetFileHash(verifyStream);
			//	if (verifyHash.SequenceEqual(fileHash))
			//	{
			//		verified = true;
			//	}
			//}

			VerificationMessages[fileHash] = new FileStoredVerificationMessage(fileHash, true, file.FullName);
			//Communicator.SendMessage(clientName, new FileStoredVerificationMessage(fileHash, true, file.FullName));
		}

		protected override async Task SendFile(string clientName, byte[] fileHash)
		{
			throw new NotImplementedException();
		}
	}
}