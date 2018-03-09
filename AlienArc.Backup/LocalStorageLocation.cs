using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Principal;
using System.Threading.Tasks;
using AlienArc.Backup.Common;
using AlienArc.Backup.Common.Utilities;
using AlienArc.Backup.IO;
using TcpCommunications.Core.Messages;

namespace AlienArc.Backup
{
	public class LocalStorageLocation : IStorageLocation
	{
		public IBackupIOFactory BackupIOFactory { get; }
		public IBackupDirectory StorageDirectory { get; }
		public string RootPath { get; }
		public StorageLocationType LocationType => StorageLocationType.Local;
		protected string TempStoragePath { get; set; }

		public LocalStorageLocation(IBackupIOFactory backupIOFactory, string storageDirectoryPath, string tempStoragePath)
		{
			RootPath = storageDirectoryPath;
			BackupIOFactory = backupIOFactory;
			TempStoragePath = tempStoragePath;
			StorageDirectory = BackupIOFactory.GetBackupDirectory(storageDirectoryPath);
		}

		public async Task<bool> StoreFile(IBackupFile file, byte[] hash)
		{
			if (file == null || !file.Exists) throw new ArgumentException();

			using (var inStream = file.OpenRead())
			{
				return await StoreFile(inStream, hash);
			}
		}

		public async Task<bool> StoreFile(string filePath, byte[] hash)
		{
			if (!File.Exists(filePath)) throw new ArgumentException();

			using (var inStream = File.OpenRead(filePath))
			{
				return await StoreFile(inStream, hash);
			}
		}

		private async Task<bool> StoreFile(Stream fileStream, byte[] hash)
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

			return await Task.FromResult(true);
		}

		public Task<string> GetFile(byte[] fileHash)
		{
			var subdir = Hasher.GetDirectoryNameFromHash(fileHash);
			var fileName = Hasher.GetFileNameFromHash(fileHash);
			var path = Path.Combine(StorageDirectory.FullName, subdir, fileName);

			if(!File.Exists(path)) throw new FileNotFoundException("Could not locate file.");

			var fileStream = File.OpenRead(path);
			var tempDirectory = Path.Combine(TempStoragePath, subdir);
			if (!Directory.Exists(tempDirectory)) Directory.CreateDirectory(tempDirectory);
			var filePath = Path.Combine(tempDirectory, fileName);
			using (var outStream = File.OpenWrite(filePath))
			using (var inflatedStream = new DeflateStream(fileStream, CompressionMode.Decompress))
			{
				inflatedStream.CopyTo(outStream);				
			}

			return Task.FromResult(filePath);
		}

		public Task Connect()
		{
			return Task.CompletedTask;
		}

		public override bool Equals(object obj)
		{
			if (obj == null || GetType() != obj.GetType()) return false;

			var compareTo = (LocalStorageLocation) obj;
			return RootPath.Equals(compareTo.RootPath, StringComparison.CurrentCultureIgnoreCase);
		}

		protected bool Equals(LocalStorageLocation other)
		{
			return string.Equals(RootPath, other.RootPath);
		}

		public override int GetHashCode()
		{
			return (RootPath != null ? RootPath.GetHashCode() : 0);
		}
	}
}