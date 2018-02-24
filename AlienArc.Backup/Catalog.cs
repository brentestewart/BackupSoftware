using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using AlienArc.Backup.Common;
using AlienArc.Backup.IO;

namespace AlienArc.Backup
{
	[Serializable]
	public class Catalog : ICatalog
	{
		protected List<IBackupIndex> BackupIndexes { get; set; } = new List<IBackupIndex>();
		protected List<string> StorageLocations { get; set; } = new List<string>();
		protected List<string> BackupDirectories { get; set; } = new List<string>();

		public void AddStorageLocation(string locationPath)
		{
			StorageLocations.Add(locationPath);
		}

		public void RemoveStorageLocation(string locationPath)
		{
			var location = StorageLocations.FirstOrDefault(l => locationPath.Equals(l, StringComparison.CurrentCultureIgnoreCase));

			if(location == null) return;

			StorageLocations.Remove(location);
		}

		public void AddBackupDirectory(IBackupDirectory directory)
		{
			BackupDirectories.Add(directory.FullName);
		}

		public IEnumerable<string> GetStorageLocations()
		{
			return StorageLocations;
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

		public IBackupIndex GetMostRecentBackupIndex()
		{
			return BackupIndexes.OrderByDescending(b => b.BackupDate).FirstOrDefault();
		}

		//public Stream GetFile(string filePath)
		//{
		//	var latestBackup = GetMostRecentBackupIndex();
		//	var matchingSet = latestBackup.BackupSets.FirstOrDefault(s => filePath.Contains(s.BasePath));

		//	if (matchingSet == null) return null;

		//	var node = matchingSet.FindNode(filePath);
		//	return GetFile(node.Hash);
		//}

		//public Stream GetFile(byte[] fileHash)
		//{
		//	var location = StorageLocations.FirstOrDefault();

		//	return location?.GetFile(fileHash);
		//}

		public IBackupSet GetBackupSet(string backupSetPath)
		{
			var latestBackup = GetMostRecentBackupIndex();
			return latestBackup.BackupSets.FirstOrDefault(s => backupSetPath.Equals(s.BasePath, StringComparison.CurrentCultureIgnoreCase));
		}

		public byte[] GetFileHashFromPath(string path)
		{
			var latestBackup = GetMostRecentBackupIndex();
			var matchingSet = latestBackup.BackupSets.FirstOrDefault(s => path.Contains(s.BasePath));

			var node = matchingSet?.FindNode(path);
			return node?.Hash;
		}
	}
}