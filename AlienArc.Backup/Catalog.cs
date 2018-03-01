using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading;
using AlienArc.Backup.Common;
using AlienArc.Backup.IO;

namespace AlienArc.Backup
{
	[Serializable]
	public class Catalog : ICatalog
	{
		protected List<IBackupIndex> BackupIndexes { get; set; } = new List<IBackupIndex>();
		protected List<string> BackupDirectories { get; set; } = new List<string>();

		public void AddBackupDirectory(IBackupDirectory directory)
		{
			BackupDirectories.Add(directory.FullName);
		}

		public void AddBackup(IBackupIndex backup)
		{
			BackupIndexes.Add(backup);
		}

		public IEnumerable<IBackupIndex> GetBackups()
		{
			return BackupIndexes;
		}

		public IEnumerable<string> GetDirectories()
		{
			return BackupDirectories;
		}

		public IBackupIndex GetMostRecentBackupIndex(IStorageLocation location)
		{
			return BackupIndexes
				.Where(b => b.RootPath.Equals(location.RootPath, StringComparison.CurrentCultureIgnoreCase))
				.OrderByDescending(b => b.BackupDate).FirstOrDefault();
		}

		public IBackupSet GetBackupSet(IStorageLocation location, string backupSetPath)
		{
			var latestBackup = GetMostRecentBackupIndex(location);
			return latestBackup.BackupSets.FirstOrDefault(s => backupSetPath.Equals(s.BasePath, StringComparison.CurrentCultureIgnoreCase));
		}

		public byte[] GetFileHashFromPath(IStorageLocation storageLocation, string path)
		{
			var latestBackup = GetMostRecentBackupIndex(storageLocation);
			var matchingSet = latestBackup.BackupSets.FirstOrDefault(s => path.Contains(s.BasePath));

			var node = matchingSet?.FindNode(path);
			return node?.Hash;
		}
	}
}