using System.CodeDom;
using System.IO;
using AlienArc.Backup.Common;
using AlienArc.Backup.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
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
		public const string PathToRestoreFile = @"C:\zdir\backup\restore\Test2Doc.txt";
		public const string StoragePath = @"C:\zdir\backup\storage";
		public const string RestorePath = @"C:\zdir\backup\restore";
		public const string FileToStorePath = @"C:\zdir\backup\Test2\Test2Doc.txt";
		public LocationInfo PrimaryLocationInfo { get; set; }
		public byte[] FileToStoreHash { get; set; }
		public string FileToStoreDirectoryPath { get; set; }
		public string FileToStoreFileName { get; set; }

		public IStorageLocation StorageLocation { get; set; }
		public IBackupManager BackupManager { get; set; }
		public UnityContainer Container { get; set; }

		[TestInitialize]
		public void RunBeforeEachTest()
		{
			DeleteTestCatalog();
			CleanDirectory(StoragePath);
			CleanDirectory(RestorePath);

			Container = new UnityContainer();
			Container.RegisterType<IBackupIOFactory, BackupIOFactory>();
			Container.RegisterType<IStorageLocationFactory, StorageLocationFactory>();
			Container.RegisterType<IBackupManagerFactory, BackupManagerFactory>();
			//StorageLocation = new LocalStorageLocation(new BackupDirectory(StoragePath));
			var managerFactory = Container.Resolve<IBackupManagerFactory>();
			PrimaryLocationInfo = new LocationInfo
			{
				Path = StoragePath,
				LocationType = StorageLocationType.Local,
				IsDefault = true
			};
			var settings = new BackupManagerSettings();
			settings.Locations.Add(PrimaryLocationInfo);
			BackupManager = managerFactory.GetBackupManager(CatalogPath, settings);
			BackupManager.AddDirectoryToCatalog(new BackupDirectory(TestTwoPath));
		}

		[TestMethod]
		public void RunBackupTest()
		{
			BackupManager.AddDirectoryToCatalog(new BackupDirectory(TestOnePath));
			BackupManager.RunBackup();	
			BackupManager.SaveCatalog();

			var storageLocationFactory = Container.Resolve<IStorageLocationFactory>();
			var location = storageLocationFactory.GetStorageLocation(PrimaryLocationInfo);
			BackupManager.RestoreBackupSet(location, TestOnePath, RestorePath);
			BackupManager.RestoreBackupSet(location, TestTwoPath, RestorePath);
		}

		private void DeleteTestCatalog()
		{
			File.Delete(CatalogPath);
		}

		private void CleanDirectory(string path)
		{
			var directory = new DirectoryInfo(path);
			foreach (var file in directory.GetFiles())
			{
				file.Delete();
			}

			foreach (var dir in directory.GetDirectories())
			{
				dir.Delete(true);
			}
		}
	}
}
