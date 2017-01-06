using System;
using System.IO;
using System.Text;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;

using LiteNetLib;
using LiteNetLib.Utils;

namespace BuildCaddyShared
{
	public interface INetworkServerListener
	{
		void OnPeerConnect();
		void OnPeerDisconnect();
		void OnPeerReceive( Message message );
	}

	public class NetworkServer : INetEventListener
	{
		private INetworkServerListener m_listener;
		private NetServer m_LiteNetServer;
		private bool m_Active = false;

		//struct
		// remote builder reference
		// netpeer

		public bool Active
		{
			get { return m_Active; }
		}

		public void SetListener( INetworkServerListener listener )
		{
			m_listener = listener;
		}

		public void Start( int port, int maxConnections, string key )
		{
			m_LiteNetServer = new NetServer( this, maxConnections, key );

			if ( m_LiteNetServer.Start( port ) )
			{
				m_Active = true;
			}
		}

		public void Stop()
		{
			if ( m_LiteNetServer == null )
			{
				return;
			}

			m_LiteNetServer.Stop();
			m_LiteNetServer = null;
		}

		public void Update()
		{
			if ( m_LiteNetServer == null )
			{
				return;
			}

			m_LiteNetServer.PollEvents();
		}

		#region INetEventListener
		public void OnNetworkError(NetEndPoint endPoint, int socketErrorCode)
		{
			Console.WriteLine("[Server] error: " + socketErrorCode);
		}
		public void OnNetworkLatencyUpdate(NetPeer peer, int latency)
		{
		}
		public void OnNetworkReceive(NetPeer peer, NetDataReader reader)
		{
			if ( m_listener != null )
			{
				//decode it...
				//reader.Data
				Packet packet = NetworkSerialize.DeserializeFromBytes<Packet>( reader.Data );
				string text = Encryption.AES.DecryptText( packet.m_bytes, "2E4s6j$#6!" );
				JSONObject obj = new JSONObject( text );
                Message msg = new Message( obj );
				m_listener.OnPeerReceive( msg );
			}
		}
		public void OnNetworkReceiveUnconnected(NetEndPoint remoteEndPoint, NetDataReader reader, UnconnectedMessageType messageType)
		{
		}
		public void OnPeerConnected(NetPeer peer)
		{
			Console.WriteLine("[Server] Peer connected: " + peer.EndPoint);

			if ( m_listener != null )
			{
				m_listener.OnPeerConnect();
			}
		}
		public void OnPeerDisconnected(NetPeer peer, DisconnectReason disconnectReason, int socketErrorCode)
		{
			Console.WriteLine("[Server] Peer disconnected: " + peer.EndPoint + ", reason: " + disconnectReason);

			if ( m_listener != null )
			{
				m_listener.OnPeerDisconnect();
			}
		}
		#endregion

	}
}