using System;
using System.Collections.Generic;
using System.Text;
using BuildCaddyShared;

public class Plugin : IPlugin, IBuildMonitor, IBuildQueueMonitor, IStringProvider
{
	IBuilder m_builder;
	private Dictionary<string, string> m_Config = new Dictionary< string, string >();
	HttpService m_httpService;

	enum Tag
	{
		Normal	= 0,
		Success = 1,
		Failure = 2
	}

	struct BuildLogEntry
	{
		public BuildLogEntry( string _message, Tag _tag, DateTime _timestamp )
		{
			message		= _message;
			tag			= _tag;
			timestamp	= _timestamp;
		}

		public string message;
		public Tag tag;
		public DateTime timestamp;
	}

	List<BuildLogEntry> m_logEntries = new List<BuildLogEntry>();
	List<string> m_buildQueue = new List<string>();
	StringBuilder m_html = new StringBuilder();
	string m_cachedHtml = string.Empty;
	object m_lock = new object();

	#region IPlugin
	public void Initialize( IBuilder builder )
    {
        m_builder = builder;
		m_builder.AddBuildMonitor( this );
		m_builder.AddBuildQueueMonitor( this );

        string cfg_filename = m_builder.GetConfigFilePath( "webstatus.cfg" );
        if ( !Config.ReadJSONConfig( cfg_filename, ref m_Config ) )
        {
            m_builder.GetLog().WriteLine( "Error loading webstatus.cfg! " + GetName() + " disabled..." );
            return;
        }

        string enabled = GetConfigSetting( "enabled" );
        if ( enabled.ToLower().CompareTo( "true" ) != 0 )
        {
            m_builder.GetLog().WriteLine( GetName() + " disabled in config..." );
            return;
        }

		string sPort = GetConfigSetting( "port" );
		int port;
		if ( !int.TryParse( sPort, out port  ) )
		{
			port = 8080;
		}


		//TEST
		/*
		AddLogEntry( new BuildLogEntry("Remove Thing", Tag.Normal, DateTime.Now ) );
		AddLogEntry( new BuildLogEntry("Parse It", Tag.Normal, DateTime.Now ) );
		AddLogEntry( new BuildLogEntry("Bad happenings", Tag.Failure, DateTime.Now ) );
		AddLogEntry( new BuildLogEntry("Add Thing", Tag.Normal, DateTime.Now ) );
		AddLogEntry( new BuildLogEntry("Build Complete", Tag.Success, DateTime.Now ) );
		AddLogEntry( new BuildLogEntry("<hr>", Tag.Normal, DateTime.Now ) );
		AddLogEntry( new BuildLogEntry("Started Build 8 from from fromfromf", Tag.Normal, DateTime.Now ) );

		m_buildQueue.Add("Build 1");
		m_buildQueue.Add("Build 2");
		m_buildQueue.Add("Build 3");
		m_buildQueue.Add("Build 4");
		*/
		GenerateHTML();
		//TEST


		m_builder.GetLog().WriteLine( GetName() + " starting HTTP service on port " + port + "..." );
		m_httpService = new HttpService( port, this );
		m_httpService.Start();
		m_builder.GetLog().WriteLine( GetName() + " HTTP service started..." );

    }

	public void Shutdown()
    {
		if ( m_httpService != null )
		{
			m_httpService.Stop();
		}
    }

	public string GetName()
    {
        return "WebStatusPlugin";
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

	#region IBuildStatusMonitor
	public void OnRunning( string message )
	{
		AddLogEntry( new BuildLogEntry( message, Tag.Normal, DateTime.Now ) );
		GenerateHTML();
	}
    public void OnStep(string message)
	{
		AddLogEntry( new BuildLogEntry( message, Tag.Normal, DateTime.Now ) );
		GenerateHTML();
	}
    public void OnSuccess( string message )
	{
		AddLogEntry( new BuildLogEntry( message, Tag.Success, DateTime.Now ) );
		AddLogEntry( new BuildLogEntry( "<hr>", Tag.Normal, DateTime.Now ) );
		GenerateHTML();
	}
	public void OnFailure( string message, string logFilename )
	{
		AddLogEntry( new BuildLogEntry( message, Tag.Failure, DateTime.Now ) );
		AddLogEntry( new BuildLogEntry( "<hr>", Tag.Normal, DateTime.Now ) );
		GenerateHTML();
	}
	#endregion

	void AddLogEntry( BuildLogEntry entry )
	{
		m_logEntries.Add( entry );

		if ( m_logEntries.Count > 200 )
		{
			m_logEntries.RemoveAt( 0 );
		}
	}

	#region IBuildQueueMonitor
	public void OnQueueChanged()
	{
		string[] buildQueue = m_builder.GetCurrentBuildQueue();
		if ( buildQueue == null )
		{
			m_buildQueue.Clear();
			GenerateHTML();
			return;
		}

		m_buildQueue.Clear();
		for( int i = 0; i < buildQueue.Length; i++ )
		{
			m_buildQueue.Add( buildQueue[ i ] );
		}

		GenerateHTML();
	}
	#endregion

	#region IStringProvider
	public string GetString()
	{
		string result;
		lock( m_lock )
		{
			result = m_cachedHtml;
		}

		return result;
	}
	#endregion

	void GenerateHTML()
	{
		m_html.Clear();

		string builderName = m_builder.GetName();

		m_html.AppendLine("<table border=1 width=800>");
			m_html.AppendLine("<tr>");
				m_html.AppendLine("<td align=center colspan=3 bgcolor=#dddddd>");
				m_html.AppendLine("<center><h1>"+ builderName +"<h1></center>");
				m_html.AppendLine("</td>");
			m_html.AppendLine("</tr>");

			m_html.AppendLine("<tr>");
				m_html.AppendLine("<td align=center>");
				m_html.AppendLine("<center><h2>Build Log<h2></center>");
				m_html.AppendLine("</td>");

				m_html.AppendLine("<td align=center>");
				m_html.AppendLine("<center><h2>Queue<h2></center>");
				m_html.AppendLine("</td>");
			m_html.AppendLine("</tr>");

			m_html.AppendLine("<tr>");
				m_html.AppendLine("<td>");
					m_html.AppendLine("<table>");

					int count = m_logEntries.Count;
					for( int i = 0; i < count; i++ )
					{
						int index = (count - 1) - i;

						m_html.AppendLine("<tr>");

						if ( m_logEntries[ index ].tag == Tag.Success )
						{
							m_html.AppendLine("<td bgcolor=#55FF55>");
						}
						else if ( m_logEntries[ index ].tag == Tag.Failure )
						{
							m_html.AppendLine("<td bgcolor=#FF5555>");
						}
						else
						{
							m_html.AppendLine("<td>");
						}

						m_html.AppendLine( m_logEntries[ index ].message );
						m_html.AppendLine("</td>");
						m_html.AppendLine("<td>");
						m_html.AppendLine( String.Format("{0:G}", m_logEntries[ index ].timestamp) );
						m_html.AppendLine("</td>");
						m_html.AppendLine("</tr>");
					}

					m_html.AppendLine("</table>");
				m_html.AppendLine("</td>");

				m_html.AppendLine("<td valign=top>");
					m_html.AppendLine("<table>");
					int queue_count = m_buildQueue.Count;

					for( int i = 0; i < queue_count; i++ )
					{
						m_html.AppendLine("<tr><td valign=top>");
						m_html.AppendLine( m_buildQueue[ i ] );
						m_html.AppendLine("</td></tr>");
					}
					m_html.AppendLine("</table>");
				m_html.AppendLine("</td>");
			m_html.AppendLine("</tr>");

		m_html.AppendLine("</table>");

		lock( m_lock )
		{
			m_cachedHtml = m_html.ToString();
		}
	}
}

