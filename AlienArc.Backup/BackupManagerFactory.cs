using AlienArc.Backup.Common;
using AlienArc.Backup.IO;

namespace AlienArc.Backup
{
	public class BackupManagerFactory : IBackupManagerFactory
	{
		public IStorageLocationFactory StorageLocationFactory { get; }
		public IBackupIOFactory BackupIOFactory { get; }
		protected ILogger Logger { get; set; }

		public BackupManagerFactory(IStorageLocationFactory storageLocationFactory, IBackupIOFactory backupIOFactory, ILogger logger)
		{
			StorageLocationFactory = storageLocationFactory;
			BackupIOFactory = backupIOFactory;
			Logger = logger;
		}

		public IBackupManager GetBackupManager(string path, IBackupManagerSettings settings)
		{
			return new BackupManager(StorageLocationFactory, BackupIOFactory, path, settings, Logger);
		}
	}
}