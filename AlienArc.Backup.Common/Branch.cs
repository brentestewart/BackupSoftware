using System;
using System.Collections.Generic;
using System.Text;

namespace AlienArc.Backup.Common
{
	[Serializable]
	public class Branch
	{
		public string Name { get; set; }
		public List<Branch> Subtrees { get; set; } = new List<Branch>();
		public List<Node> Nodes { get; set; } = new List<Node>();

		public Branch() { }

		public Branch(string name)
		{
			Name = name;
		}

		public Branch AddSubtree(string name)
		{
			var newBranch = new Branch(name);
			Subtrees.Add(newBranch);
			return newBranch;
		}

		public void DeleteSubtree(Branch branch)
		{
			Subtrees.Remove(branch);
		}
	}
}
