using System.Collections.Generic;
using System.Net;
using System.Threading;

using BuildCaddyShared;

public class Plugin : IPlugin, IBuildMonitor, IBuildQueueMonitor
{
	private IBuilder m_builder;
	private Dictionary<string, string> m_Config = new Dictionary< string, string >();

	bool m_bRunning = false;

	bool m_mainWorker = true;

	public void Initialize( IBuilder builder )
	{
		m_builder = builder;
		m_builder.AddBuildMonitor( this );
		m_builder.AddBuildQueueMonitor( this );

        string cfg_filename = m_builder.GetConfigFilePath( "coworker.cfg" );
        if ( !Config.ReadJSONConfig( cfg_filename, ref m_Config ) )
        {
            m_builder.GetLog().WriteLine( "Error loading coworker.cfg! " + GetName() + " disabled..." );
            return;
        }

        string enabled = GetConfigSetting( "enabled" );
        if ( enabled.ToLower().CompareTo("true") != 0 )
        {
            m_builder.GetLog().WriteLine( GetName() + " disabled in config..." );
            return;
        }

	}

	public void Shutdown()
	{

	}

	public string GetName(){ return "CoWorkerPlugin"; }


	string GetConfigSetting( string key )
    {
        if ( !m_Config.ContainsKey( key ) )
        {
            return string.Empty;
        }

        return m_Config[ key ];
    }

#region IBuildMonitor Interface
    public void OnRunning( string message )
    {
		m_bRunning = true;
    }

    public void OnStep( string message )
    {
		//don't care...
    }

    public void OnSuccess( string message )
    {
		m_bRunning = false;
    }

    public void OnFailure( string message, string logFilename )
    {
		m_bRunning = false;
    }
#endregion

	#region IBuildQueueMonitor
	public void OnQueueChanged()
	{
		string[] buildQueue = m_builder.GetCurrentBuildQueue();
		if ( buildQueue == null )
		{
			return;
		}

		if ( m_mainWorker )
		{
			//Tell our CoWorkers..
		}

	}
	#endregion

	void OnReceiveData( IMessage msg, IPEndPoint endPoint )
	{
        string op = msg.GetOperation();
	}

}
