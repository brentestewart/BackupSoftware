using System.Net.Sockets;
using AlienArc.Backup.Common;
using AlienArc.Backup.IO;
using TcpCommunications.Core;

namespace AlienArc.Backup
{
	public class StorageLocationFactory : IStorageLocationFactory
	{
		public IBackupIOFactory BackupIOFactory { get; }
		public ICommunicatorFactory CommunicatorFactory { get; }

		public StorageLocationFactory(IBackupIOFactory backupIOFactory, ICommunicatorFactory communicatorFactory)
		{
			BackupIOFactory = backupIOFactory;
			CommunicatorFactory = communicatorFactory;
		}

		public IStorageLocation GetStorageLocation(LocationInfo locationInfo)
		{
			switch (locationInfo.LocationType)
			{
				case StorageLocationType.Local:
					return new LocalStorageLocation(BackupIOFactory, locationInfo.Path, locationInfo.TempStoragePath);
				case StorageLocationType.Remote:
					return new RemoteStorageLocation(CommunicatorFactory.GetCommunicator(locationInfo.Port, locationInfo.Name), 
						locationInfo.Path, 
						locationInfo.TempStoragePath);
				default:
					return null;
			}
		}

	}


	public class CommunicatorFactory : ICommunicatorFactory
	{
		public ICommunicator GetCommunicator(int port, string serverName)
		{
			return new Communicator(port, new NetworkClient(serverName, new TcpClient()), new NetworkListener());
		}
	}
}