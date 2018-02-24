using System;

namespace AlienArc.Backup.Common
{
	[Serializable]
	public class Node
	{
		public string Name { get; set; }
		public byte[] Hash { get; set; }

		public Node() { }
		public Node(string name, byte[] hash)
		{
			Name = name;
			Hash = hash;
		}
	}
}