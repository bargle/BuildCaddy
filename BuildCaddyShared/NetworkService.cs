using System;
using System.IO;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;

namespace BuildCaddyShared
{
    public delegate void OnReceiveData( IMessage msg, IPEndPoint endPoint );
    public class NetworkService
    {
	    UdpClient m_Peer;
	    OnReceiveData m_onReceiveData;

	    private bool CreateSocket( int port )
	    {
		    int orgPort = port;
		    m_Peer = new UdpClient();
		    m_Peer.Client.SetSocketOption( SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true );

		    try
		    {
			    IPEndPoint localpt = new IPEndPoint( IPAddress.Any, port );
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
		    if ( CreateSocket( port ) )
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
		    return ASCIIEncoding.ASCII.GetString( bytes );
	    }

	    protected virtual byte[] Encode( string msg )
	    {
		    return Encoding.ASCII.GetBytes( msg );
	    }

        private void PrintBytes( byte[] bytes )
        {
            for( int i = 0 ; i < bytes.Length; i++ )
            {
                Console.Write( bytes[i] );
            }

            Console.Write( "\n" );
        }

      // Serialize to bytes (BinaryFormatter)
       public static byte[] SerializeToBytes<T>( T source )
       {
          using (var stream = new MemoryStream())
          {
            try
            {
                var formatter = new BinaryFormatter();
                formatter.Serialize(stream, source);
                return stream.ToArray();
            }
            catch( System.Exception) { }

            return null;
          }
       }

       // Deerialize from bytes (BinaryFormatter)
       public static T DeserializeFromBytes<T>( byte[] source ) where T :Packet
       {
          using (var stream = new MemoryStream(source))
          {
            try
            {
                var formatter = new BinaryFormatter();
                stream.Seek(0, SeekOrigin.Begin);
                return (T)formatter.Deserialize(stream);
            }
            catch( System.Exception )
            {

            }
                return null;
          }
       }

	    void recv( IAsyncResult res )
	    {
		    try
		    {
		        IPEndPoint remote = new IPEndPoint( IPAddress.Any, 0 );
		        byte[] bytes = m_Peer.EndReceive( res, ref remote );

                Packet packet = DeserializeFromBytes<Packet>( bytes );
               
                if ( packet != null && packet.IsValid )
                {
                    //Console.WriteLine( "Received " + bytes.Length + " bytes from " + remote.ToString() );
                    //PrintBytes( bytes );

		            string returnData = Decode( packet.m_bytes );

		            if ( m_onReceiveData != null )
		            {
			            JSONObject obj = new JSONObject( returnData );
                        Message msg = new Message( obj );
                        if ( msg.IsValid )
                        {
			                m_onReceiveData( msg, remote );
                        }
		            }
                }

		        // Wait for the next packet
		        m_Peer.BeginReceive( recv, null );
            }
            catch( System.Exception e )
            {
                Console.WriteLine( e.ToString() );
            }
	    }

	    public bool Send( string str, IPEndPoint endPoint )
	    {
           // Console.WriteLine( "Sending " + str );

		    try
		    {
			    byte[] bytes = Encode( str );

                //Console.WriteLine( "Sending " + bytes.Length + " bytes to " + endPoint.ToString() );
                //PrintBytes( bytes );

                Packet packet = new Packet( bytes );

                byte[] packetBytes = SerializeToBytes<Packet>( packet );

                if ( packetBytes == null )
                {
                    return false;
                }
                
			    m_Peer.Send( packetBytes, packetBytes.Length, endPoint );
			    return true;
		    }
            catch( System.Exception e )
            {
                Console.WriteLine( e.ToString() );
            }

		    return false;
	    }
    }
}