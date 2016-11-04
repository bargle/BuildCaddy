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
			m_endPoints = new List<IPEndPoint>();
		}

		public void Initialize()
		{
			m_networkService = new NetworkService();
			m_networkService.Initialize( 20000, OnReceiveData );

			while ( true )
			{ 
				Console.Write("> ");

				HandleInput( Console.ReadLine() );

			}
		}

		void HandleInput( string msg )
		{
				if ( msg.CompareTo("red") == 0 )
				{
					Console.ForegroundColor = ConsoleColor.Red;
				}

				if ( msg.CompareTo("reset") == 0 )
				{
					Console.ResetColor();
				}

				if ( msg.CompareTo("list") == 0 )
				{
					Console.WriteLine( "IPs: " + m_endPoints.Count );
					for (int i = 0; i < m_endPoints.Count; i++)
					{
						Console.WriteLine( " [" + i + "] " + m_endPoints[i].ToString() );
					}
				}

				if ( msg.StartsWith( "ping" ) )
				{
					if ( m_endPoints.Count == 0  )
					{
						Console.WriteLine( "No IPs in list..." );
						return;
					}

					string[] tokens = msg.Split( new char[]{ ' ' }, StringSplitOptions.RemoveEmptyEntries );
					if (tokens.Length < 2)
					{
						Console.WriteLine( "Usage: ping [index]" );
						return;
					}

					int idx = 0;
					if ( int.TryParse(tokens[1], out idx ) )
					{
						if ( ( idx < 0 ) || ( idx > m_endPoints.Count - 1) )
						{
							Console.WriteLine("Index out of range...");
							return;
						}

						JSONObject json = new JSONObject();
						json.AddField( "OP", "PNG" );
						json.AddField( "version", "1" );

						try
						{
							Console.WriteLine("Sending PING to: " + m_endPoints[idx].ToString() );
							m_networkService.Send( json.Print(), m_endPoints[idx] );
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
					return;
				}
		}

		void OnReceiveData( ref JSONObject obj, IPEndPoint endPoint )
		{
			string op = JSONUtil.GetString( obj, "OP", string.Empty );
			//Console.WriteLine("OP: " + op + " from " + endPoint.ToString() );

			bool found = false;
			for (int i = 0; i < m_endPoints.Count; i++)
			{
				if ( m_endPoints[i].Equals(endPoint) )
				{
					found = true;
					break;
				}
			}

			if (!found)
			{
				m_endPoints.Add( endPoint );
			}
		}
	}

	class Program
	{
		static void Main(string[] args)
		{
			MyApp app = new MyApp();
			app.Initialize();
		}
	}
}