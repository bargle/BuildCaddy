using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

using BuildCaddyShared;

public class Plugin : IPlugin
{
    private IBuilder m_builder;
    private Dictionary<string, string> m_Config = new Dictionary< string, string >();
    private Thread m_thread;
    private bool m_bDone = false;
    private int m_delay = 0;

#region IPlugin Interface
	public void Initialize( IBuilder builder )
    {
        m_builder = builder;

        string cfg_filename = m_builder.GetConfigFilePath( "timer.cfg" );
        if ( !Config.ReadJSONConfig( cfg_filename, ref m_Config ) )
        {
            m_builder.GetLog().WriteLine( "Error loading svn.cfg! TimerPlugin disabled..." );
            return;
        }

        string enabled = GetConfigSetting( "enabled" );
        if ( enabled.ToLower().CompareTo( "true" ) != 0 )
        {
            m_builder.GetLog().WriteLine( "TimerPlugin disabled in config..." );
            return;
        }

        string _delay = GetConfigSetting( "delay" );
        if ( int.TryParse( _delay, out m_delay ) )
        {
		    ThreadStart threadStart = new ThreadStart( DoWork );
		    m_thread = new Thread( threadStart );
		    m_thread.Start();
        }
        else
        {
            m_builder.GetLog().WriteLine( "Error parsing delay! TimerPlugin disabled..." );
        }
    }

	public void Shutdown()
    {
        m_bDone = true;
    }

	public string GetName()
    {
        return "TimerPlugin";
    }
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
		while ( true )
		{
			if ( m_bDone )
			{
				break;
			}

            Thread.Sleep( m_delay );

            m_builder.QueueCommand( "build", new string[] { "0" } );
        }
    }
}

