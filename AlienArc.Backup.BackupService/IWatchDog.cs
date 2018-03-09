using System.Threading.Tasks;

namespace AlienArc.Backup.BackupService
{
	public interface IWatchDog
	{
		Task Start();
		Task Stop();
		void RegisterModule(IServiceModule serviceModule);
		void UnregisterModule(IServiceModule serviceModule);
	}
}