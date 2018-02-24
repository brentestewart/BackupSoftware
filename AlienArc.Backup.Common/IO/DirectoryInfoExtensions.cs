﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace AlienArc.Backup.IO
{
	public interface IBackupFile
	{
		string FullName { get; }
		string Name { get; }
		IBackupDirectory Directory { get; }
		bool Exists { get; }
		Stream OpenRead();
		Stream Create();
	}

	public interface IBackupDirectory
	{
		string FullName { get; }
		string Name { get; }
		IBackupDirectory Parent { get; }
		bool Exists { get; }
		IEnumerable<IBackupDirectory> GetDirectories();
		IEnumerable<IBackupFile> GetFiles();
		void Create();
		IEnumerable<string> GetPathParts();
	}

	public interface IBackupIOFactory
	{
		IBackupDirectory GetBackupDirectory(string path);
		IBackupFile GetBackupFile(string path);
	}
}