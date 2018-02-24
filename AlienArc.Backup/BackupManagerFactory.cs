using AlienArc.Backup.Common;
using AlienArc.Backup.IO;

namespace AlienArc.Backup
{
	public class BackupManagerFactory : IBackupManagerFactory
	{
		public IStorageLocationFactory StorageLocationFactory { get; }
		public IBackupIOFactory BackupIOFactory { get; }

		public BackupManagerFactory(IStorageLocationFactory storageLocationFactory, IBackupIOFactory backupIOFactory)
		{
			StorageLocationFactory = storageLocationFactory;
			BackupIOFactory = backupIOFactory;
		}

		public IBackupManager GetBackupManager(string path)
		{
			return new BackupManager(StorageLocationFactory, BackupIOFactory, path);
		}
	}
}