using System;
using System.IO;
using AlienArc.Backup.IO;

namespace AlienArc.Backup.Common
{
	public interface IStorageLocation
	{
		string RootPath { get; }
		StorageLocationType LocationType { get; }
		bool StoreFile(IBackupFile file, byte[] hash);
		bool StoreFile(string filePath, byte[] hash);
		bool StoreFile(Stream fileStream, byte[] hash);
		Stream GetFile(byte[] fileHash);
	}
}