using System;
using System.Collections.Generic;
using AlienArc.Backup.Common;
using AlienArc.Backup.Common.Utilities;

namespace AlienArc.Backup
{
	[Serializable]
	public class BackupIndex : IBackupIndex
	{
		public DateTime BackupDate { get; set; } = DateTime.Now;
		public IList<IBackupSet> BackupSets { get; set; } = new List<IBackupSet>();

		public IBackupSet AddBackupSet(string basePath, string name)
		{
			var newBackupSet = new BackupSet(basePath, name);
			BackupSets.Add(newBackupSet);
			return newBackupSet;
		}

		public HashSet<byte[]> GetAllNodeHashes()
		{
			var results = new HashSet<byte[]>(HashComparer.Instance);
			foreach (var backupSet in BackupSets)
			{
				results.UnionWith(backupSet.GetAllNodeHashes());
			}

			return results;
		}
	}
}