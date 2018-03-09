using System;
using System.Collections.Generic;
using System.Runtime.Remoting.Channels;
using System.Threading.Tasks;
using AlienArc.Backup.Common;

namespace AlienArc.Backup.BackupService
{
	public class WatchDog : IWatchDog
	{
		protected bool KeepRunning { get; set; } = true;
		protected List<IServiceModule> Modules { get; set; } = new List<IServiceModule>();

		public async Task Start()
		{
			foreach (var serviceModule in Modules)
			{
				await serviceModule.Start();
			}
		}

		public async Task Stop()
		{
			foreach (var serviceModule in Modules)
			{
				await serviceModule.Stop();
			}
		}

		public void RegisterModule(IServiceModule serviceModule)
		{
			Modules.Add(serviceModule);
		}

		public void UnregisterModule(IServiceModule serviceModule)
		{
			Modules.Remove(serviceModule);
		}
	}
}