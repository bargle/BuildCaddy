using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

using BuildCaddyShared;

namespace TestSuite
{
	class Program
	{
		public class ServerListener : INetworkServerListener
		{
			public void OnPeerConnect()
			{
			}
			public void OnPeerDisconnect()
			{
			}
			public void OnPeerReceive( Message message )
			{
				Console.WriteLine( "[RECV] " + message.GetOperation() + " " + message.GetMessage() );
			}
		}

		static void Main(string[] args)
		{
			if ( args.Length == 0 )
			{
				return;
			}

			if ( args[0].CompareTo( "server" ) == 0 )
			{
				NetworkServer server = new NetworkServer();
				server.Start( 25001, 2, "BuildCaddyTest");
				if ( server.Active == false )
				{
					Console.WriteLine("Server failed to start...");
				}

				Console.WriteLine( "Press any key to stop..." );

				while( !Console.KeyAvailable )
				{
					server.Update();
					Thread.Sleep( 100 );
				}


				server.Stop();
				return;
			} 

			if ( args[0].CompareTo( "client" ) == 0 )
			{
				NetworkClient client = new NetworkClient();
				client.Connect( "127.0.0.1", 25001, "BuildCaddyTest" );

				Console.WriteLine( "Press any key to quit..." );

				while( !Console.KeyAvailable )
				{
					client.Update();
					Thread.Sleep( 100 );
				}

				client.Stop();
				return;
			} 

			if ( args[0].CompareTo( "supervisor" ) == 0 )
			{
				NetworkServer server = new NetworkServer();
				server.Start( 25001, 2, "BuildCaddyTest");
				if ( server.Active == false )
				{
					Console.WriteLine("Server failed to start...");
				}

				ServerListener listener = new ServerListener();
				server.SetListener( listener );

				Console.WriteLine( "Press any key to stop..." );

				while( !Console.KeyAvailable )
				{
					server.Update();
					Thread.Sleep( 100 );
				}


				server.Stop();
				return;
			} 

		}
	}
}
