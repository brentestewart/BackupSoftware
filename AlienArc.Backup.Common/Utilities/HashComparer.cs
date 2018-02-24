using System;
using System.Collections.Generic;
using System.Linq;

namespace AlienArc.Backup.Common.Utilities
{
	public class HashComparer : IEqualityComparer<byte[]>
	{
		private static HashComparer instance;
		public static HashComparer Instance => (instance == null) ? (instance = new HashComparer()) : instance;
		public bool Equals(byte[] x, byte[] y)
		{
			return x.SequenceEqual(y);
		}

		public int GetHashCode(byte[] obj)
		{
			return BitConverter.ToInt32(obj, 0);
		}
	}
}