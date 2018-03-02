using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

namespace AlienArc.Backup.IO
{
	public static class PathStringExtensions
	{
		public static IEnumerable<string> GetPathParts(this string fullPath)
		{
			return Split(Path.GetDirectoryName(fullPath));
		}

		private static IEnumerable<string> Split(string path)
		{
			if (string.IsNullOrWhiteSpace(path)) throw new ArgumentNullException("path");

			var parent = Path.GetDirectoryName(path);
			if (!string.IsNullOrWhiteSpace(parent))
			{
				foreach (var d in Split(parent))
				{
					yield return d;
				}
			}

			var name = Path.GetFileName(path);
			if (string.IsNullOrWhiteSpace(name))
			{
				yield return path;
			}
			else
			{
				yield return Path.GetFileName(path);
			}
		}
	}

	[Serializable]
    public class BackupFile : IBackupFile
    {
	    protected FileInfo FileInfo { get; set; }
	    public string FullName => FileInfo.FullName;
	    public string Name => FileInfo.Name;
	    private IBackupDirectory directory;
	    public IBackupDirectory Directory => directory ?? (directory = new BackupDirectory(FileInfo.Directory));
	    public bool Exists => FileInfo.Exists;
	    public long Length => FileInfo.Length;

	    public BackupFile(string path) : this(new FileInfo(path)) { }
	    public BackupFile(FileInfo fileInfo)
	    {
		    FileInfo = fileInfo;
	    }

	    public Stream OpenRead()
	    {
		    return FileInfo.OpenRead();
	    }

	    public Stream Create()
	    {
		    return FileInfo.Create();
	    }
    }

	[Serializable]
	public class BackupDirectory : IBackupDirectory
	{
		private readonly string directoryFullPath;
		private DirectoryInfo directoryInfo;
		protected DirectoryInfo DirectoryInfo => directoryInfo ?? (directoryInfo = new DirectoryInfo(directoryFullPath));

		public string FullName => DirectoryInfo.FullName;
		public bool Exists => DirectoryInfo.Exists;

		public BackupDirectory(string path) : this(new DirectoryInfo(path)) { }
		public BackupDirectory(DirectoryInfo directoryInfo)
		{
			directoryFullPath = directoryInfo.FullName;
		}

		public string Name => DirectoryInfo.Name;
		private IBackupDirectory parent;
		public IBackupDirectory Parent => parent ?? ((DirectoryInfo.Parent == null) ? null : (parent = new BackupDirectory(DirectoryInfo.Parent)));

		public IEnumerable<IBackupDirectory> GetDirectories()
		{
			return DirectoryInfo.GetDirectories().Select(d => new BackupDirectory(d));
		}

		public IEnumerable<IBackupFile> GetFiles()
		{
			return DirectoryInfo.GetFiles().Select(f => new BackupFile(f));
		}

		public void Create()
		{
			DirectoryInfo.Create();
		}

		public IEnumerable<string> GetPathParts()
		{
			return Split(this);
		}

		private IEnumerable<string> Split(IBackupDirectory path)
		{
			if (path == null)
				throw new ArgumentNullException("path");
			if (path.Parent != null)
				foreach (var d in Split(path.Parent))
					yield return d;
			yield return path.Name;
		}
	}

	[Serializable]
	public class BackupIOFactory : IBackupIOFactory
	{
		public IBackupDirectory GetBackupDirectory(string path)
		{
			return new BackupDirectory(path);
		}

		public IBackupFile GetBackupFile(string path)
		{
			return new BackupFile(path);
		}
	}
}
