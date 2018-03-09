namespace TcpCommunications.Core
{
	public interface ICommunicatorFactory
	{
		ICommunicator GetCommunicator(int port, string serverName);
	}
}