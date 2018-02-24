using System.Collections.Generic;
using System.IO;
using AlienArc.Backup.IO;

namespace AlienArc.Backup.Common
{
	public interface ICatalog
	{
		void AddStorageLocation(string location);
		void RemoveStorageLocation(string locationPath);
		void AddBackupDirectory(IBackupDirectory directory);
		IEnumerable<string> GetStorageLocations();
		void AddBackup(IBackupIndex backup);
		IEnumerable<IBackupIndex> GetBackups();
		IEnumerable<string> GetDirectories();
		IBackupIndex GetMostRecentBackupIndex();

		byte[] GetFileHashFromPath(string path);
		//Stream GetFile(string filePath);
		//Stream GetFile(byte[] fileHash);
		IBackupSet GetBackupSet(string backupSetPath);
	}
}