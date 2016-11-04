using System;
using System.Net.Sockets;
using System.Net;
using System.Text;

public delegate void OnReceiveData(ref JSONObject obj, IPEndPoint endPoint);
public class NetworkService
{
	public static int protocolVersion = 1;
	UdpClient m_Peer;
	OnReceiveData m_onReceiveData;

	private bool CreateSocket( int port )
	{
		int orgPort = port;
		m_Peer = new UdpClient();
		m_Peer.Client.SetSocketOption( SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true );

		try
		{
			IPEndPoint localpt = new IPEndPoint(IPAddress.Any, port);
			m_Peer.Client.Bind( localpt );
		}
		catch( System.Exception )
		{
			return false;
		}

		m_Peer.EnableBroadcast = true;
		m_Peer.BeginReceive( new AsyncCallback( recv ), null );
		return true;
	}

	public void Initialize( int port, OnReceiveData recvFn )
	{
		if (CreateSocket(port))
		{
			m_onReceiveData += recvFn;
		}
		else
		{
			Console.WriteLine( "FAILED TO CREATE PORT" );
		}
	}

	protected virtual string Decode( byte[] bytes )
	{
		return ASCIIEncoding.ASCII.GetString(bytes);
	}

	protected virtual byte[] Encode( string msg )
	{
		return Encoding.ASCII.GetBytes( msg );
	}

	void recv( IAsyncResult res )
	{
		//Console.WriteLine("recv");

		IPEndPoint remote = new IPEndPoint( IPAddress.Any, 0 );
		byte[] bytes = m_Peer.EndReceive( res, ref remote );
		string returnData = Decode( bytes );

		if ( m_onReceiveData != null )
		{
			JSONObject obj = new JSONObject( returnData );
			if ( obj.HasField( "version" ) )
			{
				string versionString = JSONUtil.GetString( obj, "version" );
				int version = BuildCaddyShared.Util.ParseIntFromString( versionString, -1 );

				if ( version == protocolVersion )
				{
					m_onReceiveData( ref obj, remote );
				}
				else 
				{
					//ignore this old version...
				}
			} // else ignore this packet...  
		}

		// get next packet
		m_Peer.BeginReceive( recv, null );

	}

	public bool Send( string str, IPEndPoint endPoint )
	{
		try
		{
			byte[] bytes = Encode( str );
			m_Peer.Send( bytes, bytes.Length, endPoint );
			return true;
		}
		catch (System.Exception)
		{
		}

		return false;
	}

}