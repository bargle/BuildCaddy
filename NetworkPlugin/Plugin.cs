using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using BuildCaddyShared;

public class Plugin : IPlugin
{
	private IBuilder m_builder;
	private NetworkService m_networkService;

	Thread m_thread;
	bool m_bDone = false;
	IPEndPoint m_server = null;

#region IPlugin Interface
	public void Initialize( IBuilder builder )
	{
		m_builder = builder;

		//TODO:
		// m_builder.GetConfig( "networkplugin.cfg" ) <- or similar
		m_networkService = new NetworkService();
		m_networkService.Initialize( 0, OnReceiveData );

		ThreadStart threadStart = new ThreadStart( DoWork );

		m_thread = new Thread( threadStart );
		m_thread.Start();
	}

	public void Shutdown()
	{
		m_bDone = true;
	}

	public string GetName(){ return "NetworkPlugin"; }
#endregion

	void DoWork()
	{
		while ( true )
		{
			if ( m_bDone )
			{
				break;
			}

			if ( m_server == null )
			{
				JSONObject json = new JSONObject();
				json.AddField( "OP", "HLO" );
				json.AddField( "version", "1" );

				try
				{
					m_networkService.Send( json.Print(), new IPEndPoint( IPAddress.Broadcast, 20000 ) ); //FIXME: This port needs to be config-driven
				}
				catch ( System.Exception ) 
				{
				}
			}

			Thread.Sleep( 1000 ); //1 second delay.. For now...
		}
	}

	void OnReceiveData( ref JSONObject obj, IPEndPoint endPoint )
	{
		//...
		Console.WriteLine( GetName() + "received a message from: " +endPoint.ToString() );
		Console.WriteLine( " MSG: " + obj.Print() );
	}
}
