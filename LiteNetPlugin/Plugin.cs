using System;
using System.Collections.Generic;
using System.Threading;

using BuildCaddyShared;

public class Plugin : IPlugin, INetworkClientListener, IBuildMonitor
{
	private IBuilder m_builder;
    private Dictionary<string, string> m_Config = new Dictionary< string, string >();

    Thread m_thread;
	bool m_bDone = false;

	NetworkClient m_client;
	string m_ip;
	int m_port;
	string m_sharedKey;

	#region IPlugin Interface
	public void Initialize( IBuilder builder )
	{
		m_builder = builder;

        string cfg_filename = m_builder.GetConfigFilePath( "litenet.cfg" );
        if ( !Config.ReadJSONConfig( cfg_filename, ref m_Config ) )
        {
            m_builder.GetLog().WriteLine( "Error loading litenet.cfg! LiteNetPlugin disabled..." );
            return;
        }

        string enabled = GetConfigSetting( "enabled" );
        if ( enabled.ToLower().CompareTo("true") != 0 )
        {
            m_builder.GetLog().WriteLine( "Networkplugin disabled in config..." );
            return;
        }

		m_sharedKey = m_builder.GetConfigString( "sharedkey" );
		m_ip = GetConfigSetting( "host" );
		string _port = GetConfigSetting( "port" );
		if ( int.TryParse( _port, out m_port ) )
		{
			//it worked...
		}

        m_builder.AddBuildMonitor(this);
		/*

		m_networkService = new EncryptedNetworkService();
		m_networkService.Initialize( 0, OnReceiveData );
		*/
		ThreadStart threadStart = new ThreadStart( DoWork );
		m_thread = new Thread( threadStart );
		m_thread.Start();
	}

	public void Shutdown()
	{
		m_bDone = true;
	}

	public string GetName(){ return "LiteNetPlugin"; }
    #endregion

    string GetConfigSetting( string key )
    {
        if ( !m_Config.ContainsKey( key ) )
        {
            return string.Empty;
        }

        return m_Config[ key ];
    }

    void DoWork()
	{
		m_client = new NetworkClient();
		m_client.SetListener( this );
		m_client.Connect( m_ip, m_port, m_sharedKey );

		while ( true )
		{
			if ( m_bDone )
			{
				m_client.Stop();
				break;
			}

			m_client.Update();

			Thread.Sleep( 1000 ); //1 second delay.. For now...
		}
	}

	#region INetworkClientListener
	public void OnConnect()
	{
		Console.WriteLine("[INetworkClientListener] OnConnect...");
	}
	public void OnDisconnect()
	{
		Console.WriteLine("[INetworkClientListener] OnDisconnect...");
	}
	public void OnReceive( byte[] bytes )
	{
		Console.WriteLine("[INetworkClientListener] OnReceive: " + bytes.Length + " received...");
	}
	#endregion

#region IBuildMonitor Interface
    public void OnRunning( string message )
    {
        if ( !m_client.Active )
        {
            return;
        }

        Message newMsg = new Message();
        newMsg.Add( "OP", "STATUS" );
        newMsg.Add( "message", message );
        //m_networkService.Send( newMsg.GetSendable(), m_server );

		m_client.Send( newMsg.GetSendable() );
    }

    public void OnStep( string message )
    {
        if ( !m_client.Active )
        {
            return;
        }

        Message newMsg = new Message();
        newMsg.Add( "OP", "STEP" );
        newMsg.Add( "message", message );
        //m_networkService.Send( newMsg.GetSendable(), m_server );

		m_client.Send( newMsg.GetSendable() );
    }

    public void OnSuccess( string message )
    {
        if ( !m_client.Active )
        {
            return;
        }

        Message newMsg = new Message();
        newMsg.Add( "OP", "STATUS" );
        newMsg.Add( "message", message );
		//m_networkService.Send( newMsg.GetSendable(), m_server )
		m_client.Send( newMsg.GetSendable() );
    }

    public void OnFailure( string message, string logFilename )
    {
        if ( !m_client.Active )
        {
            return;
        }

        Message newMsg = new Message();
        newMsg.Add( "OP", "STATUS" );
        newMsg.Add( "message", message );
        //m_networkService.Send( newMsg.GetSendable(), m_server );

		m_client.Send( newMsg.GetSendable() );
    }
#endregion

}

