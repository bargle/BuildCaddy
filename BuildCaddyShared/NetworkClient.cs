using System;
using System.IO;
using System.Text;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;

using LiteNetLib;
using LiteNetLib.Utils;

namespace BuildCaddyShared
{
	public interface INetworkClientListener
	{
		void OnConnect();
		void OnDisconnect();
		void OnReceive( byte[] bytes );
	}

	public class NetworkClient : INetEventListener
	{

		private INetworkClientListener m_listener;
		private NetClient m_LiteNetClient;
		private bool m_Active = false;

		private string m_ip;
		private int m_port;

		public bool Active
		{
			get { return m_Active; }
		}

		public void SetListener( INetworkClientListener listener )
		{
			m_listener = listener;
		}

		public void Connect( string ip, int port, string key )
		{
			m_ip = ip;
			m_port = port;

			m_LiteNetClient = new NetClient( this, key );
			m_LiteNetClient.MaxConnectAttempts = 0;
			if ( m_LiteNetClient.Start() )
			{
				m_Active = true;
				m_LiteNetClient.Connect( ip, port );
			}
		}

		public void Stop()
		{
			if ( m_LiteNetClient == null )
			{
				return;
			}

			m_Active = false;
			m_LiteNetClient.Stop();
			m_LiteNetClient = null;
		}

		public void Update()
		{
			if ( m_LiteNetClient == null )
			{
				return;
			}

			m_LiteNetClient.PollEvents();
		}

		public void Send( string message )
		{
			//encode it...
			byte[] bytes = Encryption.AES.EncryptTextToBytes( message, "2E4s6j$#6!" );
			Packet packet = new Packet( bytes );
			byte[] packetBytes = NetworkSerialize.SerializeToBytes<Packet>( packet );
			m_LiteNetClient.Peer.Send( packetBytes, SendOptions.ReliableOrdered );
		}

		#region INetEventListener
		public void OnNetworkError(NetEndPoint endPoint, int socketErrorCode)
		{
			//Console.WriteLine("[Client] error! " + socketErrorCode);
		}
		public void OnNetworkLatencyUpdate(NetPeer peer, int latency)
		{
		}
		public void OnNetworkReceive(NetPeer peer, NetDataReader reader)
		{
			if ( m_listener != null )
			{
				m_listener.OnReceive( reader.Data );
			}
		}
		public void OnNetworkReceiveUnconnected(NetEndPoint remoteEndPoint, NetDataReader reader, UnconnectedMessageType messageType)
		{
		}
		public void OnPeerConnected(NetPeer peer)
		{
			//Console.WriteLine("[Client] connected to: {0}:{1}", peer.EndPoint.Host, peer.EndPoint.Port);
			if ( m_listener != null )
			{
				m_listener.OnConnect();
			}
		}
		public void OnPeerDisconnected(NetPeer peer, DisconnectReason disconnectReason, int socketErrorCode)
		{
			if ( m_listener != null )
			{
				m_listener.OnDisconnect();
			}

			//Console.WriteLine("[Client] disconnected: " + disconnectReason);

			//if ( disconnectReason == DisconnectReason.Timeout )
			{
				//reconnect...
				if ( m_ip != null && m_ip.Length != 0 && m_port != 0 )
				{
					//Console.WriteLine("[Client] attempting reconnect to: {0}:{1}", m_ip, m_port);
					m_LiteNetClient.Connect(m_ip, m_port);
				}
			}
		}
		#endregion

	}
}