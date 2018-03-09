using System;
using System.IO;
using AlienArc.Backup.IO;

namespace AlienArc.Backup.Common
{
	[Serializable]
	public class Node
	{
		public string Name { get; set; }
		public byte[] Hash { get; set; }
		public bool BackedUp { get; set; }
		public DateTime CreationTime { get; set; }
		public DateTime ModifiedTime { get; set; }
		public bool ReadOnly { get; set; }
		public FileAttributes FileAttributes { get; set; }

		public Node() { }
		public Node(IBackupFile file, byte[] hash)
		{
			Name = file.Name;
			CreationTime = file.CreationTime;
			ModifiedTime = file.ModifiedTime;
			ReadOnly = file.ReadOnly;
			FileAttributes = file.Attributes;
			Hash = hash;
		}
	}
}