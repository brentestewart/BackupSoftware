using System.Collections.Generic;
using System.IO;

namespace AlienArc.Backup.Common
{
	public interface IBackupSet
	{
		string BasePath { get; set; }
		Branch Root { get; set; }
		HashSet<byte[]> GetAllNodeHashes();
		Node FindNode(string fullPath);
		void ResetBackupFlags();
	}
}