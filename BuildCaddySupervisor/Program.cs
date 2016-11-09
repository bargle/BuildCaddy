using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;

using BuildCaddyShared;

namespace BuildCaddySupervisor
{
	class MyApp
	{
		NetworkService m_networkService;

		List< IPEndPoint > m_endPoints;

		public MyApp()
		{
			m_endPoints = new List< IPEndPoint >();
		}

		public void Initialize()
		{
			m_networkService = new NetworkService();
			m_networkService.Initialize( 25000, OnReceiveData );

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

				if ( msg.CompareTo( "reset" ) == 0 )
				{
					Console.ResetColor();
				}

				if ( msg.CompareTo( "list" ) == 0 )
				{
					Console.WriteLine( "IPs: " + m_endPoints.Count );
					for ( int i = 0; i < m_endPoints.Count; i++ )
					{
						Console.WriteLine( " [" + i + "] " + m_endPoints[i].ToString() );
					}
				}

				if ( msg.StartsWith( "ping" ) )
				{
					if ( m_endPoints.Count == 0  )
					{
						Console.WriteLine( "No IPs in list..." );
						return true;
					}

					string[] tokens = msg.Split( new char[]{ ' ' }, StringSplitOptions.RemoveEmptyEntries );
					if ( tokens.Length < 2 )
					{
						Console.WriteLine( "Usage: ping [index]" );
						return true;
					}

					int idx = 0;
					if ( int.TryParse(tokens[1], out idx ) )
					{
						if ( ( idx < 0 ) || ( idx > m_endPoints.Count - 1 ) )
						{
							Console.WriteLine( "Index out of range..." );
							return true;
						}

                        Message newMsg = new Message();
                        newMsg.Add( "OP", "PNG" );

						try
						{
							Console.WriteLine( "Sending PING to: " + m_endPoints[idx].ToString() );
							m_networkService.Send( newMsg.GetSendable(), m_endPoints[idx] );
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

		void OnReceiveData( IMessage msg, IPEndPoint endPoint )
		{
            string op = msg.GetOperation();
            //Console.WriteLine("OP: " + op + " from " + endPoint.ToString() );

            if ( op.CompareTo( "HLO" ) == 0 )
            {
                Message outMessage = new Message();
                outMessage.Add( "OP", "PNG" );

                bool found = false;
                for ( int i = 0; i < m_endPoints.Count; i++ )
                {
                    if ( m_endPoints[i].Equals( endPoint ) )
                    {
                        found = true;
                        break;
                    }
                }

                if ( !found )
                {
                    m_endPoints.Add( endPoint );
                }

                try
                {
                    Console.WriteLine("Sending PING to: " + endPoint );
                    m_networkService.Send( outMessage.GetSendable(), endPoint );
                }
                catch (System.Exception e)
                {
                    Console.WriteLine( "Exception: " + e.ToString() );
                }
            }

                if ( op.CompareTo( "STATUS" ) == 0  )
            {
                string message = msg.GetMessage();
                Console.WriteLine( "STATUS: " + msg.GetMessage() );
            }

            if ( op.CompareTo( "STEP" ) == 0 )
            {
                string message = msg.GetMessage();
                Console.WriteLine( "BUILD STEP: " + message );
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