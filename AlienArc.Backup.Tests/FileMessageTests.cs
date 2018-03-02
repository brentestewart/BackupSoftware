using Microsoft.VisualStudio.TestTools.UnitTesting;
using TcpCommunications.Core;

namespace AlienArc.Backup.Tests
{
	[TestClass]
	public class FileMessageTests
	{
		[TestInitialize]
		public static void RunBeforeEachTest()
		{

		}

		[TestMethod]
		public void TransferFileTest()
		{
			var serverCommunicator = new Communicator();
			var server = new RemoteFileServer(serverCommunicator);

			var clientCommunicator = new Communicator();
			var client = new RemoteStorageLocation(clientCommunicator, "127.0.0.1");


		}
	}
}