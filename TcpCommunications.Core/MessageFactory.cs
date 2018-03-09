using System;
using System.IO;
using TcpCommunications.Core.Messages;

namespace TcpCommunications.Core
{
	public static class MessageFactory
    {
        public static INetworkMessage ReadMessage(INetworkStream stream)
        {
            var intBytes = stream.Read(0, 4);
            var messageId = BitConverter.ToInt32(intBytes, 0);

			intBytes = stream.Read(0, 4);
            var payloadLength = BitConverter.ToInt32(intBytes, 0);

			intBytes = stream.Read(0, 4);
            var messageType = BitConverter.ToInt32(intBytes, 0);

            var payloadBytes = stream.Read(0, payloadLength);

			return BuildMessage(messageId, messageType, payloadBytes);
        }

        public static INetworkMessage BuildMessage(int messageId, int messageType, byte[] payload)
        {
            switch (messageType)
            {
                //case 1:
                //    return ConnectedMessage.Instance;
                case 2:
                    return new GetFileMessage(payload);
				case 10:
					return new FileStartMessage(payload);
				case 11:
					return new FileChunckMessage(payload);
				case 12:
					return new FileEndMessage(payload);
				case 13:
					return new AbandonFileTransferMessage(payload);
				case 14:
					return new FileStoredVerificationMessage(payload);
				case 15:
					return new FileChunkReceivedMessage(payload);
				case 1000:
					return new ClientInfoMessage(payload);
				case 1001:
					return new ClientConnectedMessage(payload);
				case 20000:
					return new MockMessage(payload);
                default:
                    return null;
            }
        }
    }
}
