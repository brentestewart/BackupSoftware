namespace AlienArc.Backup.Common
{
	public interface IBackupManagerFactory
	{
		IBackupManager GetBackupManager(string path, IBackupManagerSettings settings);
	}
}