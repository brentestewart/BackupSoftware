using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using AlienArc.Backup.Common;
using AlienArc.Backup.Common.Utilities;
using AlienArc.Backup.IO;

namespace AlienArc.Backup
{
	[Serializable]
	public class LocalStorageLocation : IStorageLocation
	{
		public IBackupIOFactory BackupIOFactory { get; }
		public IBackupDirectory StorageDirectory { get; }
		public string RootPath { get; }

		public LocalStorageLocation(IBackupIOFactory backupIOFactory, string storageDirectoryPath)
		{
			RootPath = storageDirectoryPath;
			BackupIOFactory = backupIOFactory;
			StorageDirectory = BackupIOFactory.GetBackupDirectory(storageDirectoryPath);
		}

		public bool StoreFile(IBackupFile file, byte[] hash)
		{
			if (file == null || !file.Exists) throw new ArgumentException();

			using (var inStream = file.OpenRead())
			{
				return StoreFile(inStream, hash);
			}
		}

		public bool StoreFile(string filePath, byte[] hash)
		{
			if (!File.Exists(filePath)) throw new ArgumentException();

			using (var inStream = File.OpenRead(filePath))
			{
				return StoreFile(inStream, hash);
			}
		}

		public bool StoreFile(Stream fileStream, byte[] hash)
		{
			if (fileStream == null || hash == null || hash.Length != 20) throw new ArgumentException();

			var subdir = Hasher.GetDirectoryNameFromHash(hash);
			var storageLocation = new BackupDirectory(Path.Combine(StorageDirectory.FullName, subdir));

			if (!storageLocation.Exists)
			{
				storageLocation.Create();
			}

			var fileName = Hasher.GetFileNameFromHash(hash);
			var outFilePath = Path.Combine(storageLocation.FullName, fileName);

			using (var outStream = File.OpenWrite(outFilePath))
			using (var deflateStream = new DeflateStream(outStream, CompressionMode.Compress))
			{
				fileStream.CopyTo(deflateStream);
			}

			//Validate the file??
			//var hashCheck = Hasher.GetFileHash(file);

			return true;
		}

		public Stream GetFile(byte[] fileHash)
		{
			var subdir = Hasher.GetDirectoryNameFromHash(fileHash);
			var fileName = Hasher.GetFileNameFromHash(fileHash);
			var path = Path.Combine(StorageDirectory.FullName, subdir, fileName);

			if(!File.Exists(path)) throw new FileNotFoundException("Could not locate file.");

			var fileStream = File.OpenRead(path);
			return new DeflateStream(fileStream, CompressionMode.Decompress);
		}
	}
}