using System;
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

	[Flags]
	public enum LoggingLevel
	{
		Error = 0,
		Warning = 1,
		Info = 2,
		Debug = 4,
	}

	public interface ILogger
	{
		LoggingLevel LogLevel { get; set; }
		void LogDebug(string message);
		void LogInfo(string message);
		void LogWarning(string message);
		void LogError(string message);
	}
}