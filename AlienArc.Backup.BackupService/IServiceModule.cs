using System.Threading.Tasks;

namespace AlienArc.Backup.BackupService
{
	public interface IServiceModule
	{
		Task Start();
		Task Stop();
	}
}