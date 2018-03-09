using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AlienArc.Backup.Common;
using AlienArc.Backup.Common.Utilities;
using AlienArc.Backup.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace AlienArc.Backup.Tests
{
	[TestClass]
	public class LocalStorageLocationTests
	{
		public const string StoragePath = @"C:\zdir\backup\storage";
		public const string TempStoragePath = @"C:\zdir\backup\tempstorage";
		public const string FileToStorePath = @"C:\zdir\backup\Test2\Test2Doc.txt";

		public IStorageLocation StorageLocation { get; set; }
		public byte[] FileToStoreHash { get; set; }
		public string FileToStoreDirectoryPath { get; set; }
		public string FileToStoreFileName { get; set; }
		public IBackupIOFactory IOFactory { get; set; }

		[TestInitialize]
		public void RunBeforeEachTest()
		{
			CleanupStorageDirectory();
			IOFactory = new BackupIOFactory();

			StorageLocation = new LocalStorageLocation(IOFactory, StoragePath, TempStoragePath);
			FileToStoreHash = Hasher.GetFileHash(new BackupFile(FileToStorePath));
			FileToStoreDirectoryPath = Hasher.GetDirectoryNameFromHash(FileToStoreHash);
			FileToStoreFileName = Hasher.GetFileNameFromHash(FileToStoreHash);
		}

		[TestMethod]
		public void StoreFileFromPathTest()
		{
			Assert.IsFalse(new DirectoryInfo(Path.Combine(StoragePath, FileToStoreDirectoryPath)).Exists);
			Assert.IsFalse(File.Exists(Path.Combine(StoragePath, FileToStoreDirectoryPath, FileToStoreFileName)));

			StorageLocation.StoreFile(FileToStorePath, FileToStoreHash);

			Assert.IsTrue(new DirectoryInfo(Path.Combine(StoragePath, FileToStoreDirectoryPath)).Exists);
			Assert.IsTrue(File.Exists(Path.Combine(StoragePath, FileToStoreDirectoryPath, FileToStoreFileName)));
		}

		[TestMethod]
		public void StoreFileFromFileInfoTest()
		{
			Assert.IsFalse(new DirectoryInfo(Path.Combine(StoragePath, FileToStoreDirectoryPath)).Exists);
			Assert.IsFalse(File.Exists(Path.Combine(StoragePath, FileToStoreDirectoryPath, FileToStoreFileName)));

			StorageLocation.StoreFile(new BackupFile(FileToStorePath), FileToStoreHash);

			Assert.IsTrue(new DirectoryInfo(Path.Combine(StoragePath, FileToStoreDirectoryPath)).Exists);
			Assert.IsTrue(File.Exists(Path.Combine(StoragePath, FileToStoreDirectoryPath, FileToStoreFileName)));
		}

		[TestMethod]
		public async Task GetFileTest()
		{
			await StorageLocation.StoreFile(FileToStorePath, FileToStoreHash);

			var tempFilePath = await StorageLocation.GetFile(FileToStoreHash);
			var resultHash = Hasher.GetFileHash(File.OpenRead(tempFilePath));

			Assert.IsTrue(FileToStoreHash.SequenceEqual(resultHash));
		}

		private void CleanupStorageDirectory()
		{
			Directory.Delete(StoragePath, true);
			Directory.Delete(TempStoragePath, true);
		}
	}
}