using AlienArc.Backup.Common;
using AlienArc.Backup.IO;

namespace AlienArc.Backup
{
	public class StorageLocationFactory : IStorageLocationFactory
	{
		public IBackupIOFactory BackupIOFactory { get; }

		public StorageLocationFactory(IBackupIOFactory backupIOFactory)
		{
			BackupIOFactory = backupIOFactory;
		}

		public IStorageLocation GetStorageLocation(string path)
		{
			return new LocalStorageLocation(BackupIOFactory, path);
		}
	}
}