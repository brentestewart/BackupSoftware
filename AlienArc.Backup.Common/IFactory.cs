namespace AlienArc.Backup.Common
{
	public interface IFactory
	{
		IBackupContainer BackupContainer { get; }
	}
}