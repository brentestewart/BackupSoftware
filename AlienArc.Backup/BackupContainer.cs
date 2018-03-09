using AlienArc.Backup.Common;
using Unity;

namespace AlienArc.Backup
{
	public class BackupContainer : IBackupContainer
	{
		protected IUnityContainer Container { get; }

		public BackupContainer(IUnityContainer container)
		{
			Container = container;
		}

		public T Resolve<T>()
		{
			return Container.Resolve<T>();
		}
	}
}