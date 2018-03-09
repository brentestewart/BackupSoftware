using System;
using System.IO;
using System.Net.Sockets;
using System.Runtime.Remoting;
using System.Threading.Tasks;
using AlienArc.Backup.Common.Utilities;
using AlienArc.Backup.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TcpCommunications.Core;
using TcpCommunications.Core.Messages;

namespace AlienArc.Backup.Tests
{
	[TestClass]
	public class FileMessageTests
	{
		public const string FileToStorePath = @"C:\zdir\Backup\video1.mkv";
		public const string PathToStorage = @"C:\zdir\Backup\Storage";
		public const string PathToServerTempStorage = @"C:\zdir\Backup\ServerTempStorage";
		public const string PathToClientTempStorage = @"C:\zdir\Backup\ClientTempStorage";

		[TestInitialize]
		public void RunBeforeEachTest()
		{
			CleanUpStorage();
		}

		private void CleanUpStorage()
		{
			RemoveDirectory(PathToStorage);
			RemoveDirectory(PathToServerTempStorage);
			RemoveDirectory(PathToClientTempStorage);
		}

		private void RemoveDirectory(string path)
		{
			if (Directory.Exists(path))
			{
				Directory.Delete(path, true);
			}
		}

		[TestMethod]
		public async Task CommunicatorTests()
		{
			var server = new Communicator();
			await server.StartServer();

			var client = new Communicator(13001, new NetworkClient("Test", new TcpClient()), new NetworkListener());
			await client.ConnectToServer("192.168.1.6", 13001);

			//await Task.Delay(1000);
			await client.SendMessage(new FileStartMessage(new byte[] { 1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1}));

			await Task.Delay(5000);
		}

		[TestMethod]
		public async Task StoreRemoteFileTest()
		{
			var serverCommunicator = new Communicator();
			var server = new RemoteFileServer(serverCommunicator, PathToStorage, PathToServerTempStorage); // Path.GetTempPath());
			server.StartServer();

			var clientCommunicator = new Communicator(13001, new NetworkClient("Test", new TcpClient()), new NetworkListener());
			var client = new RemoteStorageLocation(clientCommunicator, "192.168.1.6", PathToClientTempStorage);
			await client.Connect();


			var file = new BackupFile(FileToStorePath);
			var hash = Hasher.GetFileHash(file);

			var storeReslts = await client.StoreFile(file, hash);

			Assert.IsTrue(storeReslts);
		}

		[TestMethod]
		public async Task GetRemoteFileTest()
		{
			var serverCommunicator = new Communicator();
			var server = new RemoteFileServer(serverCommunicator, PathToStorage, PathToServerTempStorage); // Path.GetTempPath());
			server.StartServer();

			var clientCommunicator = new Communicator();
			var client = new RemoteStorageLocation(clientCommunicator, "192.168.1.6", PathToClientTempStorage);
			await client.Connect();

			var file = new BackupFile(FileToStorePath);
			var hash = Hasher.GetFileHash(file);
			await client.StoreFile(file, hash);

			var filePath = await client.GetFile(hash);

		}
	}
}