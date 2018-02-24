using System.Collections.Generic;
using AlienArc.Backup.IO;

namespace AlienArc.Backup.Common
{
    public interface IBackupManager
    {
	    void OpenCatalog(IBackupFile catalogFile);
	    void SaveCatalog();
	    void AddDirectoryToCatalog(IBackupDirectory directory);
	    void RunBackup();
	    IEnumerable<IBackupIndex> GetBackups();
	    void AddStorageLocation(string locationPath);
	    void RemoveStorageLocation(string locationPath);
	    bool RestoreFile(string filePath, string destinationPath = null);
	    void RestoreBackupSet(string backupSetPath, string destinationPath = null);

    }

	public interface IBackupManagerFactory
	{
		IBackupManager GetBackupManager(string path);
	}

	public interface IStorageLocationFactory
	{
		IStorageLocation GetStorageLocation(string path);
	}
}
