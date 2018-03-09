using System;
using System.IO;
using System.Threading.Tasks;
using AlienArc.Backup.IO;

namespace AlienArc.Backup.Common
{
	public interface IStorageLocation
	{
		string RootPath { get; }
		StorageLocationType LocationType { get; }
		Task<bool> StoreFile(IBackupFile file, byte[] hash);
		Task<bool> StoreFile(string filePath, byte[] hash);
		Task<string> GetFile(byte[] fileHash);
		Task Connect();
	}
}