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
	    void AddStorageLocation(LocationInfo locationInfo);
	    void RemoveStorageLocation(LocationInfo locationInfo);
	    List<IStorageLocation> GetLocations();
		bool RestoreFile(IStorageLocation location, string filePath, string destinationPath = null);
	    void RestoreBackupSet(IStorageLocation location, string backupSetPath, string destinationPath = null);
    }
}
