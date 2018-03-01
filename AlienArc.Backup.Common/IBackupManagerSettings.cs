using System.Collections.Generic;

namespace AlienArc.Backup.Common
{
	public interface IBackupManagerSettings
	{
		List<LocationInfo> Locations { get; set; }
		void AddLocation(LocationInfo location);
	}
}