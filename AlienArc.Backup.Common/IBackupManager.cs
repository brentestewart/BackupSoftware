using System.Collections.Generic;
using System.Threading.Tasks;
using AlienArc.Backup.IO;

namespace AlienArc.Backup.Common
{
    public interface IBackupManager
    {
	    void OpenCatalog(IBackupFile catalogFile);
	    void SaveCatalog();
	    void AddDirectoryToCatalog(IBackupDirectory directory);
	    Task<bool> RunBackup();
	    IEnumerable<IBackupIndex> GetBackups();
	    void AddStorageLocation(LocationInfo locationInfo);
	    void RemoveStorageLocation(LocationInfo locationInfo);
	    List<IStorageLocation> GetLocations();
		Task<bool> RestoreFile(IStorageLocation location, string filePath, string destinationPath = null);
	    Task<bool> RestoreBackupSet(IStorageLocation location, string backupSetPath, string destinationPath = null);
    }
}
