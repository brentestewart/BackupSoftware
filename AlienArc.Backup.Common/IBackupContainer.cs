namespace AlienArc.Backup.Common
{
	public interface IBackupContainer
	{
		T Resolve<T>();
	}
}