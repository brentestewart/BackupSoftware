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

		public IStorageLocation GetStorageLocation(LocationInfo locationInfo)
		{
			switch (locationInfo.LocationType)
			{
				case StorageLocationType.Local:
					return new LocalStorageLocation(BackupIOFactory, locationInfo.Path);
				case StorageLocationType.Remote:
					return null;
				default:
					return null;
			}
		}
	}
}