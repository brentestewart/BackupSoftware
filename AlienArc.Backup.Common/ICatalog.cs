using System.Collections.Generic;
using System.IO;
using AlienArc.Backup.IO;

namespace AlienArc.Backup.Common
{
	public interface ICatalog
	{
		void AddBackupDirectory(IBackupDirectory directory);
		void AddBackup(IBackupIndex backup);
		IEnumerable<IBackupIndex> GetBackups();
		IEnumerable<string> GetDirectories();
		IBackupIndex GetMostRecentBackupIndex(IStorageLocation location);
		byte[] GetFileHashFromPath(IStorageLocation location, string path);
		IBackupSet GetBackupSet(IStorageLocation location, string backupSetPath);
	}
}