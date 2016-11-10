using System;
using System.Collections.Generic;
using System.Threading;
using System.Diagnostics;
using System.IO;

using BuildCaddyShared;

public class Plugin : IPlugin
{
    private IBuilder m_builder;
    private Dictionary<string, string> m_Config = new Dictionary< string, string >();
    private Thread m_thread;
    private bool m_bDone = false;
    private int m_delay = 0;
    private string m_repo;

#region IPlugin Interface
	public void Initialize( IBuilder builder )
    {
        m_builder = builder;

        string cfg_filename = m_builder.GetConfigFilePath( "svn.cfg" );
        if ( !Config.ReadJSONConfig( cfg_filename, ref m_Config ) )
        {
            m_builder.GetLog().WriteLine( "Error loading svn.cfg! SVNPlugin disabled..." );
            return;
        }

        string enabled = GetConfigSetting( "enabled" );
        if ( enabled.ToLower().CompareTo( "true" ) != 0 )
        {
            m_builder.GetLog().WriteLine( "SVNPlugin disabled in config..." );
            return;
        }

        m_repo = m_builder.GetConfigString( "repo" );
        Console.WriteLine( "Repo: " + m_repo );

        string _delay = GetConfigSetting( "delay" );
        if ( int.TryParse( _delay, out m_delay ) )
        {
		    ThreadStart threadStart = new ThreadStart( DoWork );
		    m_thread = new Thread( threadStart );
		    m_thread.Start();
        }
        else
        {
            m_builder.GetLog().WriteLine( "Error parsing delay! SVNPlugin disabled..." );
        }
    }

	public void Shutdown()
    {
        m_bDone = true;
    }

	public string GetName()
    {
        return "SVNPlugin";
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

	string GetAndUpdateRevisionNumber( string url )
	{
		ProcessStartInfo start = new ProcessStartInfo();
		start.FileName = GetConfigSetting( "SVN_BINARY" );
		start.Arguments = "info " + url;
		start.UseShellExecute = false;
		start.RedirectStandardOutput = true;

		using ( Process process = Process.Start( start ) )
		{
			using ( StreamReader reader = process.StandardOutput )
			{
				string result = reader.ReadToEnd();
				string[] tokens = result.Split( new char[]{ '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries );
				foreach( string token in tokens )
				{
					if ( token.Contains( "Last Changed Rev" ) )
					{
						string[] revisionTokens = token.Split( new char[]{ ':' }, StringSplitOptions.RemoveEmptyEntries );
						if ( revisionTokens.Length > 1 )
						{ 
							return revisionTokens[1].Replace( " ", "" );
						}
					}
				}
			}
		}

		return string.Empty;
	}

    void DoWork()
	{
        string current_rev = GetAndUpdateRevisionNumber( m_repo );

		while ( true )
		{
			if ( m_bDone )
			{
				break;
			}

            Thread.Sleep( m_delay );

            Console.WriteLine("SVN: checking...");

            string rev = GetAndUpdateRevisionNumber( m_repo );
            Console.WriteLine( "SVN - rev: " + rev );
            if ( rev.CompareTo( current_rev ) != 0 )
            {
                //Console.WriteLine("SVN: queue build command...");

                //Push a new command...
                m_builder.QueueCommand( "build", new string[] { rev } );

                // Update the revision to the current revsion.
                current_rev = rev;
            }
        }
    }

}

