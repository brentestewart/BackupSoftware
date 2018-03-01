using System;
using System.Collections.Generic;

namespace AlienArc.Backup.Common
{
	public interface IBackupIndex
	{
		string RootPath { get; set; }
		DateTime BackupDate { get; set; }
		IList<IBackupSet> BackupSets { get; set; }
		HashSet<byte[]> GetAllNodeHashes();
		IBackupSet AddBackupSet(string basePath, string name);
		void ResetBackupFlags();
	}
}