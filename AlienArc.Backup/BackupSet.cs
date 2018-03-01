using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using AlienArc.Backup.Common;
using AlienArc.Backup.Common.Utilities;
using AlienArc.Backup.IO;

namespace AlienArc.Backup
{
	[Serializable]
	public class BackupSet : IBackupSet
	{
		public string BasePath { get; set; }
		public Branch Root { get; set; }

		public BackupSet(string basePath, string branchName)
		{
			BasePath = basePath;
			Root = new Branch() {Name = branchName};
		}

		public HashSet<byte[]> GetAllNodeHashes()
		{
			var hashes = new HashSet<byte[]>(HashComparer.Instance);
			GetNodeHashes(Root, hashes);
			return hashes;
		}

		public Node FindNode(string fullPath)
		{
			var pathParts = fullPath.GetPathParts();
			var branchesQueue = new Queue<string>(pathParts);

			var path = branchesQueue.Dequeue();
			while (!BasePath.Equals(path, StringComparison.CurrentCultureIgnoreCase) && branchesQueue.Count > 0)
			{
				path = Path.Combine(path, branchesQueue.Dequeue());
			}

			if (!BasePath.Equals(path, StringComparison.CurrentCultureIgnoreCase)) return null;

			return FindNode(Root, branchesQueue, Path.GetFileName(fullPath));
		}

		public void ResetBackupFlags()
		{
			Root.ResetBackupFlags();
		}

		private Node FindNode(Branch currentBranch, Queue<string> branchesQueue, string fileName)
		{
			if (branchesQueue.Count != 0)
			{
				var nextBranch = branchesQueue.Dequeue();
				var matchingBranch = currentBranch.Subtrees.FirstOrDefault(b => b.Name.Equals(nextBranch, StringComparison.CurrentCultureIgnoreCase));
				if (matchingBranch == null) return null;
				return FindNode(matchingBranch, branchesQueue, fileName);
			}

			return currentBranch.Nodes.FirstOrDefault(n => n.Name.Equals(fileName, StringComparison.CurrentCultureIgnoreCase));
		}

		private void GetNodeHashes(Branch root, HashSet<byte[]> hashes)
		{
			foreach (var currentSubtree in root.Subtrees)
			{
				GetNodeHashes(currentSubtree, hashes);
			}

			foreach (var node in root.Nodes)
			{
				if (!hashes.Contains(node.Hash) && node.BackedUp)
				{
					hashes.Add(node.Hash);
				}
			}
		}
	}
}