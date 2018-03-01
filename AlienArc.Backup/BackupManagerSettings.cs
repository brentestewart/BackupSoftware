using System.Collections.Generic;
using AlienArc.Backup.Common;

namespace AlienArc.Backup
{
	public class BackupManagerSettings : IBackupManagerSettings
	{
		public List<LocationInfo> Locations { get; set; } = new List<LocationInfo>();

		public void AddLocation(LocationInfo location)
		{
			Locations.Add(location);
		}
	}
}