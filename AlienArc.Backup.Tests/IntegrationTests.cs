using System.CodeDom;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;
using AlienArc.Backup.BackupService;
using AlienArc.Backup.Common;
using AlienArc.Backup.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using TcpCommunications.Core;
using Unity;

namespace AlienArc.Backup.Tests
{
	[TestClass]
	public class IntegrationTests
	{
		public const string CatalogPath = @"C:\zdir\backup\testcatalog.cat";
		public const string TestOnePath = @"C:\zdir\backup\Test1";
		public const string TestTwoPath = @"C:\zdir\backup\Test2";
		public const string FileToRestorePath = @"C:\zdir\backup\Test2\Test2Doc.txt";
		public const string PathToRestoreFile = @"C:\zdir\backup\restore\Test2\Test2Doc.txt";
		public const string StoragePath = @"C:\zdir\backup\storage";
		public const string RemoteStoragePath = @"C:\zdir\backup\remote\storage";
		public const string TemporaryStoragePath = @"C:\zdir\backup\tempstorage";
		public const string RemoteTemporaryStoragePath = @"C:\zdir\backup\remote\tempstorage";
		public const string RestorePath = @"C:\zdir\backup\restore";
		public const string FileToStorePath = @"C:\zdir\backup\Test2\Test2Doc.txt";

		public LocationInfo LocalLocationInfo { get; set; }
		public LocationInfo RemoteLocationInfo { get; set; }
		public byte[] FileToStoreHash { get; set; }
		public string FileToStoreDirectoryPath { get; set; }
		public string FileToStoreFileName { get; set; }
		public IBackupManagerFactory BackupManagerFactory { get; set; }

		public IStorageLocation StorageLocation { get; set; }
		public IBackupManager BackupManager { get; set; }
		public UnityContainer Container { get; set; }

		[TestInitialize]
		public void RunBeforeEachTest()
		{
			DeleteTestCatalog();
			CleanDirectory(StoragePath);
			CleanDirectory(TemporaryStoragePath);
			CleanDirectory(RestorePath);
			CleanDirectory(RemoteStoragePath);
			CleanDirectory(RemoteTemporaryStoragePath);

			Container = new UnityContainer();
			Container.RegisterType<IBackupIOFactory, BackupIOFactory>();
			Container.RegisterType<IStorageLocationFactory, StorageLocationFactory>();
			Container.RegisterType<IBackupManagerFactory, BackupManagerFactory>();
			Container.RegisterType<IBackupContainer, BackupContainer>();
			Container.RegisterType<ICommunicatorFactory, CommunicatorFactory>();
			var logger = new ConsoleLogger {LogLevel = LoggingLevel.Debug};
			Container.RegisterInstance<ILogger>(logger);
			//StorageLocation = new LocalStorageLocation(new BackupDirectory(StoragePath));
			BackupManagerFactory = Container.Resolve<IBackupManagerFactory>();
			LocalLocationInfo = new LocationInfo
			{
				Path = StoragePath,
				TempStoragePath = TemporaryStoragePath,
				LocationType = StorageLocationType.Local,
				IsDefault = true
			};
			RemoteLocationInfo = new LocationInfo
			{
				Name = "TestServer",
				Path = "192.168.1.6",
				Port = 13001,
				TempStoragePath = TemporaryStoragePath,
				LocationType = StorageLocationType.Remote,
				IsDefault = true
			};
		}

		[TestMethod]
		public void RunLocalBackupTest()
		{
			var settings = new BackupManagerSettings();
			settings.Locations.Add(LocalLocationInfo);
			BackupManager = BackupManagerFactory.GetBackupManager(CatalogPath, settings);
			BackupManager.AddDirectoryToCatalog(new BackupDirectory(TestTwoPath));
			BackupManager.AddDirectoryToCatalog(new BackupDirectory(TestOnePath));
			BackupManager.RunBackup();	
			BackupManager.SaveCatalog();

			var storageLocationFactory = Container.Resolve<IStorageLocationFactory>();
			var location = storageLocationFactory.GetStorageLocation(LocalLocationInfo);
			BackupManager.RestoreBackupSet(location, TestOnePath, RestorePath);
			BackupManager.RestoreBackupSet(location, TestTwoPath, RestorePath);

			var originalFile = new FileInfo(FileToRestorePath);
			var restoredFile = new FileInfo(PathToRestoreFile);

			Assert.AreEqual(originalFile.Length, restoredFile.Length);
			Assert.AreEqual(originalFile.Attributes, restoredFile.Attributes);
			Assert.AreEqual(originalFile.CreationTime, restoredFile.CreationTime);
			Assert.AreEqual(originalFile.LastWriteTime, restoredFile.LastWriteTime);
			Assert.AreEqual(originalFile.IsReadOnly, restoredFile.IsReadOnly);
		}

		[TestMethod]
		public async Task RunRemoteBackupTest()
		{
			var settings = new BackupManagerSettings();
			settings.Locations.Add(RemoteLocationInfo);
			BackupManager = BackupManagerFactory.GetBackupManager(CatalogPath, settings);
			BackupManager.AddDirectoryToCatalog(new BackupDirectory(TestTwoPath));
			//BackupManager.AddDirectoryToCatalog(new BackupDirectory(TestOnePath));

			var serverWatchDog = new WatchDog();
			var comminicator = new Communicator(13001, new NetworkClient("TestServer", new TcpClient()), new NetworkListener());
			var fileServer = new RemoteFileServer(comminicator, RemoteStoragePath, RemoteTemporaryStoragePath);
			var backupService = new BackupServiceModule(fileServer);
			serverWatchDog.RegisterModule(backupService);
			await serverWatchDog.Start();

			await BackupManager.RunBackup();
			BackupManager.SaveCatalog();

			//var storageLocationFactory = Container.Resolve<IStorageLocationFactory>();
			var location = BackupManager.GetLocations().First();
			//await BackupManager.RestoreBackupSet(location, TestOnePath, RestorePath);
			await BackupManager.RestoreBackupSet(location, TestTwoPath, RestorePath);

			var originalFile = new FileInfo(FileToRestorePath);
			var restoredFile = new FileInfo(PathToRestoreFile);

			Assert.AreEqual(originalFile.Length, restoredFile.Length);
			Assert.AreEqual(originalFile.Attributes, restoredFile.Attributes);
			Assert.AreEqual(originalFile.CreationTime, restoredFile.CreationTime);
			Assert.AreEqual(originalFile.LastWriteTime, restoredFile.LastWriteTime);
			Assert.AreEqual(originalFile.IsReadOnly, restoredFile.IsReadOnly);
		}

		private void DeleteTestCatalog()
		{
			File.Delete(CatalogPath);
		}

		private void CleanDirectory(string path)
		{
			if (Directory.Exists(path))
			{
				Directory.Delete(path, true);
			}
		}
	}
}
