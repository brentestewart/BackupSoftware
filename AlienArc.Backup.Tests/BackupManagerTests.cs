using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Text;
using AlienArc.Backup.Common;
using AlienArc.Backup.Common.Utilities;
using AlienArc.Backup.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Unity;


namespace AlienArc.Backup.Tests
{
	public class MockStream : MemoryStream
	{
		protected MemoryStream InnerStream { get; set; }
		public byte[] Contents { get; set; } = new byte[0];
		public string TextContents => Encoding.ASCII.GetString(Contents);
		public MockStream(MemoryStream stream)
		{
			InnerStream = stream;
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			return InnerStream.Read(buffer, offset, count);
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			var newArray = new byte[Contents.Length + (count - offset)];
			Array.Copy(Contents, newArray, Contents.Length);
			Array.Copy(buffer, offset, newArray, Contents.Length, count);
			Contents = newArray;
			//Contents += Encoding.ASCII.GetString(buffer, offset, count);
			InnerStream.Write(buffer, offset, count);
		}
	}

	public class MockFile : IBackupFile
	{
		private byte[] contents;
		public string FullName { get; set; }
		public string Name { get; set; }
		public IBackupDirectory Directory { get; set; }
		public bool Exists { get; set; }
		public Stream ReadStream { get; set; }
		public Stream WriteStream { get; set; }
		public byte[] Hash { get; set; }
		public string OutputFile { get; set; }

		public byte[] Contents
		{
			get => contents;
			set
			{
				contents = value;
				using (var stream = new MemoryStream(contents))
				{
					Hash = Hasher.GetFileHash(stream);
				}
			}
		}

		public void SetTextContents(string newContents)
		{
			Contents = Encoding.ASCII.GetBytes(newContents);
		}

		public void SetBinaryContents(byte[] newContents)
		{
			Contents = newContents;
		}

		public Stream OpenRead()
		{
			ReadStream = new MemoryStream(Contents);
			return ReadStream;
		}

		public Stream Create()
		{
			if (string.IsNullOrWhiteSpace(OutputFile)) return CreateMockStream();

			return CreateFile();
		}

		public Stream CreateMockStream()
		{
			var innerStream = new MemoryStream();
			WriteStream = new MockStream(innerStream);
			return WriteStream;
		}

		public Stream CreateFile()
		{
			return File.OpenWrite(OutputFile);
		}

		public byte[] GetCreateContents()
		{
			return ((MockStream) WriteStream).Contents;
		}

		public string GetCreateContentsAsText()
		{
			return ((MockStream) WriteStream).TextContents;
		}
	}

	public class MockDirectory : IBackupDirectory
	{
		public Mock<IStorageLocation> StorageLocation { get; }
		public Mock<IBackupIOFactory> IOFactory { get; }
		public string FullName { get; set; }
		public string Name { get; set; }
		public IBackupDirectory Parent { get; set; }
		public bool Exists { get; set; }
		public List<IBackupDirectory> Subdirectories { get; set; } = new List<IBackupDirectory>();
		public List<IBackupFile> Files { get; set; } = new List<IBackupFile>();

		public MockDirectory(Mock<IStorageLocation> storageLocation, Mock<IBackupIOFactory> ioFactory, string fullName, string name, IBackupDirectory parent)
		{
			StorageLocation = storageLocation;
			IOFactory = ioFactory;
			FullName = fullName;
			Name = name;
			Parent = parent;
			Exists = true;
		}

		public IEnumerable<IBackupDirectory> GetDirectories()
		{
			return Subdirectories;
		}

		public IEnumerable<IBackupFile> GetFiles()
		{
			return Files;
		}

		public void Create()
		{			
		}

		public IEnumerable<string> GetPathParts()
		{
			return FullName.GetPathParts();
		}

		public MockFile AddMockFile(string fileName, string contents)
		{
			var newFile = new MockFile
			{
				Name = fileName,
				FullName = Path.Combine(FullName, fileName),
				Directory = this,
				Exists = true,
			};
			newFile.SetTextContents(contents);
			Files.Add(newFile);
			StorageLocation.Setup(s => s.GetFile(It.Is<byte[]>(b => b.SequenceEqual(newFile.Hash)))).Returns(newFile.OpenRead);
			IOFactory.Setup(f => f.GetBackupFile(newFile.FullName)).Returns(newFile);
			return newFile;
		}

		public MockDirectory AddMockDirectory(string directoryName)
		{
			var fullName = Path.Combine(FullName, directoryName);
			var subDir = new MockDirectory(StorageLocation, IOFactory, fullName, directoryName, this);
			Subdirectories.Add(subDir);
			IOFactory.Setup(f => f.GetBackupDirectory(subDir.FullName)).Returns(subDir);

			return subDir;
		}
	}

	[TestClass]
	public class BackupManagerTests
	{
		public const string CatalogPath = @"X:\fake\testcatalog.cat";
		public const string DirectoryToBackupPath = @"X:\fake\backup";

		public LocationInfo PrimaryLocationInfo =
			new LocationInfo {Path = @"X:\fake\storage", LocationType = StorageLocationType.Local, IsDefault = true};

		public Mock<IStorageLocation> MockStorageLocation { get; set; }
		public Mock<IBackupIOFactory> MockIOFactory { get; set; }
		public IBackupManager BackupManager { get; set; }
		public UnityContainer Container { get; set; }

		public MockDirectory DirectoryToBackup { get; set; }
		public MockFile CatalogFile { get; set; }
		public MockFile Sub1File1 { get; set; }
		public MockFile Sub1File2 { get; set; }
		public MockFile Sub1File3 { get; set; }
		public MockFile Sub2File1 { get; set; }
		public MockFile RestoreFile { get; set; }

		[TestInitialize]
		public void RunBeforeEachTest()
		{
			MockStorageLocation = new Mock<IStorageLocation>();
			MockStorageLocation.Setup(s => s.RootPath).Returns(PrimaryLocationInfo.Path);
			MockStorageLocation.Setup(s => s.LocationType).Returns(StorageLocationType.Local);

			MockIOFactory = new Mock<IBackupIOFactory>();

			SetupMockFiles();


			var mockStorageLocationFactory = new Mock<IStorageLocationFactory>();
			mockStorageLocationFactory.Setup(f => f.GetStorageLocation(PrimaryLocationInfo)).Returns(MockStorageLocation.Object);

			MockIOFactory.Setup(f => f.GetBackupFile(CatalogFile.FullName)).Returns(CatalogFile);

			Container = new UnityContainer();
			Container.RegisterInstance(MockIOFactory.Object);
			Container.RegisterType<IBackupManagerFactory, BackupManagerFactory>();
			Container.RegisterInstance(mockStorageLocationFactory.Object);

			var managerFactory = Container.Resolve<IBackupManagerFactory>();
			var settings = new BackupManagerSettings();
			settings.Locations.Add(PrimaryLocationInfo);
			BackupManager = managerFactory.GetBackupManager(CatalogFile.FullName, settings);
			BackupManager.AddDirectoryToCatalog(DirectoryToBackup);
		}

		[TestMethod]
		public void RunBackupTest()
		{
			BackupManager.RunBackup();

			MockStorageLocation.Verify(sl => sl.StoreFile(Sub1File1.FullName, Sub1File1.Hash), Times.Once);
		}

		[TestMethod]
		public void RestoreFileTest()
		{
			var contents = "Hello there";
			Sub1File1.SetTextContents(contents);
			BackupManager.RunBackup();
			BackupManager.RestoreFile(MockStorageLocation.Object, Sub1File1.FullName, RestoreFile.FullName);

			var output = RestoreFile.GetCreateContentsAsText();

			Assert.AreEqual(contents, output);
		}

		[TestMethod]
		public void SaveCatalogTest()
		{
			BackupManager.RunBackup();
			BackupManager.RemoveStorageLocation(PrimaryLocationInfo);
			BackupManager.SaveCatalog();

			var catalogContents = CatalogFile.GetCreateContents();
			Assert.IsTrue(catalogContents.Length > 1000);
		}

		[TestMethod]
		public void LoadCatalogTest()
		{
			BackupManager.RunBackup();
			BackupManager.RemoveStorageLocation(PrimaryLocationInfo);
			CatalogFile.OutputFile = @"C:\zdir\backup\mock.bin";
			BackupManager.SaveCatalog();

			byte[] catalogContents;
			using (var inStream = File.OpenRead(@"C:\zdir\backup\mock.bin"))
			{
				var len = (int)inStream.Length;
				var results = new byte[len];
				inStream.Read(results, 0, len);
				catalogContents = results;
			}

			var newCatalogFile = new MockFile() {FullName = @"X:\cat.file"};
			newCatalogFile.SetBinaryContents(catalogContents);

			BackupManager.OpenCatalog(newCatalogFile);
			var allBackups = BackupManager.GetBackups().ToList();

			Assert.AreEqual(1, allBackups.Count());
			Assert.AreEqual(1, allBackups.First().BackupSets.Count);
		}

		[TestMethod]
		public void AddRemoveLocationTest()
		{
			IBackupIOFactory ioFactory = new BackupIOFactory();
			IStorageLocationFactory storageLocationFactory = new StorageLocationFactory(ioFactory);
			IBackupManagerSettings settings = new BackupManagerSettings();
			IBackupManager manager = new BackupManager(storageLocationFactory, ioFactory, @"X:\fake\cat.bin", settings);
			var location = new LocationInfo {Path = @"C:\backup", LocationType = StorageLocationType.Local, IsDefault = true};

			manager.AddStorageLocation(location);
			Assert.IsTrue(manager.GetLocations().Count == 1);

			manager.RemoveStorageLocation(location);
			Assert.IsTrue(manager.GetLocations().Count == 0);
		}

		private void SetupMockFiles()
		{
			CatalogFile = new MockFile
			{
				FullName = CatalogPath,
				Name = @"testcatalog.cat",
				Exists = false
			};

			var root = new MockDirectory(MockStorageLocation, MockIOFactory, @"X:\", @"X:\", null);

			var fakeDir = root.AddMockDirectory("Fake");
			DirectoryToBackup = fakeDir.AddMockDirectory("backup");

			var sub1 = DirectoryToBackup.AddMockDirectory("sub1");
			var sub2 = DirectoryToBackup.AddMockDirectory("sub2");

			Sub1File1 = sub1.AddMockFile("Sub1File1.txt", "This is the contents of Sub1File1.txt");
			Sub1File2 = sub1.AddMockFile("Sub1File2.txt", "This is the contents of Sub1File2.txt");
			Sub1File3 = sub1.AddMockFile("Sub1File3.txt", "This is the contents of Sub1File3.txt");
			Sub2File1 = sub2.AddMockFile("Sub2File1.txt", "This is the contents of Sub2File1.txt");

			var restoreDirectory = new MockDirectory(MockStorageLocation, MockIOFactory, @"X:\fake\restore", "restore", fakeDir);
			RestoreFile = restoreDirectory.AddMockFile(@"mockFakeOne.txt", "Restore Contents");
			RestoreFile.Exists = false;
		}
	}
}
