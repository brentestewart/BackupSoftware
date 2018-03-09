using System.Threading.Tasks;

namespace AlienArc.Backup.BackupService
{
	public class BackupServiceModule : IServiceModule
	{
		protected IFileServer Server { get; set; }
		protected bool KeepRunning { get; set; } = true;

		public BackupServiceModule(IFileServer server)
		{
			Server = server;
		}

		public async Task Start()
		{
			await Server.StartServer();
		}

		public async Task Stop()
		{
			await Server.StopServer();
		}
	}
}