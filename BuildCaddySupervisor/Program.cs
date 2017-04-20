using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net;

using BuildCaddyShared;

namespace BuildCaddySupervisor
{
	class MyApp
	{

        const int INVALID_INDEX = -1;
        struct RemoteBuilder
        {
            public RemoteBuilder( string name, IPEndPoint endPoint )
            {
                m_name = name;
                m_endPoint = endPoint;
            }

            public string m_name;
            public IPEndPoint m_endPoint;
        }

		NetworkService m_networkService;

		//List< IPEndPoint > m_endPoints;
        List< RemoteBuilder > m_remoteBuilders;

        bool m_verbose = false;

		public MyApp()
		{
            m_remoteBuilders = new List<RemoteBuilder>();
		}

		public void Initialize()
		{
			m_networkService = new EncryptedNetworkService();
			m_networkService.Initialize( 27000, OnReceiveData );

			while ( true )
			{ 
				Console.Write("> ");

				if ( !HandleInput( Console.ReadLine() ) )
                {
                    break;
                }
			}
		}

		bool HandleInput( string msg )
		{
				if ( msg.CompareTo( "red" ) == 0 )
				{
					Console.ForegroundColor = ConsoleColor.Red;
				}

				if ( msg.CompareTo( "resetcolor" ) == 0 )
				{
					Console.ResetColor();
				}

				if ( msg.CompareTo( "verbose" ) == 0 )
				{
					m_verbose = true;
				}

				if ( msg.CompareTo( "quiet" ) == 0 )
				{
					m_verbose = false;
				}

				if ( msg.CompareTo( "list" ) == 0 )
				{
					Console.WriteLine( "IPs: " + m_remoteBuilders.Count );
					for ( int i = 0; i < m_remoteBuilders.Count; i++ )
					{
						Console.WriteLine( " [" + i + "] " + m_remoteBuilders[i].m_name + " - " + m_remoteBuilders[i].m_endPoint.ToString() );
					}
				}

				if ( msg.CompareTo( "clearlist" ) == 0 )
				{
                    m_remoteBuilders.Clear();
				}

				if ( msg.StartsWith( "build" ) )
				{
                    string[] tokens = msg.Split( new char[]{ ' ' }, StringSplitOptions.RemoveEmptyEntries );
                    if ( tokens.Length < 3 )
					{
                        Console.WriteLine( "Usage: build <index> <rev number>" );
						return true;
                    }

                    int idx = 0;
					if ( int.TryParse(tokens[1], out idx ) )
					{
                        Message newMsg = new Message();
                        newMsg.Add( "OP", "BLD" );
                        newMsg.Add( "rev", tokens[2] );
						try
						{
							Console.WriteLine( "Sending BUILD command to: " + m_remoteBuilders[idx].m_endPoint.ToString() );
							m_networkService.Send( newMsg.GetSendable(), m_remoteBuilders[idx].m_endPoint );
						}
						catch ( System.Exception e ) 
						{
							Console.WriteLine( "Exception: " + e.ToString() );
						}
                    }
                    else
                    {
                        Console.WriteLine( "Couldn't parse:  " + tokens[1] );
                    }
                }

				if ( msg.StartsWith( "dumpbuild" ) )
				{
                    string[] tokens = msg.Split( new char[]{ ' ' }, StringSplitOptions.RemoveEmptyEntries );
                    if ( tokens.Length < 3 )
					{
                        Console.WriteLine( "Usage: dumpbuild <index> <rev number>" );
						return true;
                    }

                    int idx = 0;
					if ( int.TryParse(tokens[1], out idx ) )
					{
                        Message newMsg = new Message();
                        newMsg.Add( "OP", "DBLD" );
                        newMsg.Add( "rev", tokens[2] );
						try
						{
							Console.WriteLine( "Sending DUMPBUILD command to: " + m_remoteBuilders[idx].m_endPoint.ToString() );
							m_networkService.Send( newMsg.GetSendable(), m_remoteBuilders[idx].m_endPoint );
						}
						catch ( System.Exception e ) 
						{
							Console.WriteLine( "Exception: " + e.ToString() );
						}
                    }
                    else
                    {
                        Console.WriteLine( "Couldn't parse:  " + tokens[1] );
                    }
                }

				if ( msg.StartsWith( "ping" ) )
				{
					if ( m_remoteBuilders.Count == 0  )
					{
						Console.WriteLine( "No IPs in list..." );
						return true;
					}

					string[] tokens = msg.Split( new char[]{ ' ' }, StringSplitOptions.RemoveEmptyEntries );
					if ( tokens.Length < 2 )
					{
						
                        //ping them all
                        Message newMsg = new Message();
                        newMsg.Add( "OP", "PNG" );

                        for( int i = 0; i < m_remoteBuilders.Count; i++ )
                        {
                            m_networkService.Send( newMsg.GetSendable(), m_remoteBuilders[ i ].m_endPoint );
                        }

						return true;
					}

					int idx = 0;
					if ( int.TryParse(tokens[1], out idx ) )
					{
						if ( ( idx < 0 ) || ( idx > m_remoteBuilders.Count - 1 ) )
						{
							Console.WriteLine( "Index out of range..." );
							return true;
						}

                        Message newMsg = new Message();
                        newMsg.Add( "OP", "PNG" );

						try
						{
							Console.WriteLine( "Sending PING to: " + m_remoteBuilders[idx].m_endPoint.ToString() );
							m_networkService.Send( newMsg.GetSendable(), m_remoteBuilders[idx].m_endPoint );
						}
						catch ( System.Exception e ) 
						{
							Console.WriteLine( "Exception: " + e.ToString() );
						}
					}
					else
					{
						Console.WriteLine( "Usage: ping [index]" );
					}
					

				}

				if ( msg.CompareTo( "exit" ) == 0 )
				{
					return false;
				}

                return true;
		}

        bool IsConnectedEndpoint( IPEndPoint endPoint )
        {
            for ( int i = 0; i < m_remoteBuilders.Count; i++ )
            {
                if ( m_remoteBuilders[i].m_endPoint.Equals( endPoint ) )
                {
                    return true;
                }
            }

            return false;
        }

        int GetConnectedEndpointIndex( IPEndPoint endPoint )
        {
            for ( int i = 0; i < m_remoteBuilders.Count; i++ )
            {
                if ( m_remoteBuilders[i].m_endPoint.Equals( endPoint ) )
                {
                    return i;
                }
            }

            return INVALID_INDEX;
        }

		void OnReceiveData( IMessage msg, IPEndPoint endPoint )
		{
            string op = msg.GetOperation();
            //Console.WriteLine("OP: " + op + " from " + endPoint.ToString() );

            if ( op.CompareTo( "PONG" ) == 0 )
            {
                string name = msg.GetValue( "name" );
                Console.WriteLine( "Received PONG from: " + name );
            }

            if ( op.CompareTo( "HLO" ) == 0 )
            {
                string name = msg.GetValue( "name" );

                Message outMessage = new Message();
                outMessage.Add( "OP", "SRV" );

                bool found = false;
                for ( int i = 0; i < m_remoteBuilders.Count; i++ )
                {
                    if ( m_remoteBuilders[i].m_endPoint.Equals( endPoint ) )
                    {
                        found = true;
                        break;
                    }
                }

                if ( !found )
                {
                    m_remoteBuilders.Add( new RemoteBuilder( name, endPoint ) );
                }

                try
                {
                    //Console.WriteLine("Sending PING to: " + endPoint );
                    m_networkService.Send( outMessage.GetSendable(), endPoint );
                }
                catch ( System.Exception e )
                {
                    Console.WriteLine( "Exception: " + e.ToString() );
                }
            }
            else
            {
                int index = GetConnectedEndpointIndex( endPoint );
                if ( index == INVALID_INDEX )
                {
                    return;
                }

                if ( m_verbose )
                {
                    if ( op.CompareTo( "STATUS" ) == 0  )
                    {
                        string message = msg.GetMessage();
                        Console.WriteLine( m_remoteBuilders[index].m_name + ": " + "[STATUS] " + msg.GetMessage() );
                    }

                    if ( op.CompareTo( "STEP" ) == 0 )
                    {
                        string message = msg.GetMessage();
                        Console.WriteLine(  m_remoteBuilders[index].m_name + ": " + "[BUILD STEP] " + message );
                    }
                }
            }
		}
	}

	class Program
	{
		static void Main( string[] args )
		{
			MyApp app = new MyApp();
			app.Initialize();
		}
	}
}