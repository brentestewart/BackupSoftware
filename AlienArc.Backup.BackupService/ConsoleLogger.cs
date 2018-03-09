using System;
using AlienArc.Backup.Common;

namespace AlienArc.Backup.BackupService
{
	public class ConsoleLogger : ILogger
	{
		public LoggingLevel LogLevel { get; set; }

		public void LogError(string message)
		{
			LogMessage(message);
		}

		public void LogWarning(string message)
		{
			if (LogLevel >= LoggingLevel.Warning)
			{
				LogMessage(message);
			}
		}

		public void LogInfo(string message)
		{
			if (LogLevel >= LoggingLevel.Info)
			{
				LogMessage(message);
			}
		}

		public void LogDebug(string message)
		{
			if (LogLevel >= LoggingLevel.Debug)
			{
				LogMessage(message);
			}
		}

		private void LogMessage(string message)
		{
			Console.WriteLine(message);
		}
	}
}