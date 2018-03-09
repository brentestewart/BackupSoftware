using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using AlienArc.Backup.Common;
using TcpCommunications.Core;
using Unity;

namespace AlienArc.Backup.BackupService
{
	static class Program
	{
		public static UnityContainer Container { get; set; }
		public static IWatchDog WatchDog { get; set; }

		static void Main(string[] args)
		{
			ConfigureContainer();
			var pathToStorage = @"C:\zdir\backup\remote\storage";
			var pathToTempStorage = @"C:\zdir\backup\remote\tempstorage";

			WatchDog = Container.Resolve<IWatchDog>();
			var comminicator = new Communicator(13001, new NetworkClient("TestServer", new TcpClient()), new NetworkListener());
			var fileServer = new RemoteFileServer(comminicator, pathToStorage, pathToTempStorage);
			var backupService = new BackupServiceModule(fileServer);
			WatchDog.RegisterModule(backupService);

			WatchDog.Start();
			Console.WriteLine("Press any key to stop backup server");
			Console.ReadKey();
			WatchDog.Stop();
		}

		private static void ConfigureContainer()
		{
			Container = new UnityContainer();
			Container.RegisterType<IBackupManager, BackupManager>();
			Container.RegisterType<IBackupManagerFactory, BackupManagerFactory>();
			Container.RegisterType<ICommunicator, Communicator>();
			Container.RegisterType<IFileServer, RemoteFileServer>();
			Container.RegisterType<IWatchDog, WatchDog>();
		}
	}
}
