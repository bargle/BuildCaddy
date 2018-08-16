using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Net;

using BuildCaddyShared;

public class Plugin : IPlugin, IBuildMonitor
{
	IBuilder m_builder;
    private Dictionary<string, string> m_Config = new Dictionary< string, string >();

#region IPlugin Interface
	public void Initialize( IBuilder builder )
	{
		m_builder = builder;

        string cfg_filename = m_builder.GetConfigFilePath( "discord.cfg" );
        if ( !Config.ReadJSONConfig( cfg_filename, ref m_Config ) )
        {
            m_builder.GetLog().WriteLine( "Error loading discord.cfg! DiscordPlugin disabled..." );
            return;
        }

		string enabled = GetConfigSetting( "enabled" );
		if ( enabled.Length == 0 || enabled.ToLower().CompareTo( "true" ) != 0 )
		{
            m_builder.GetLog().WriteLine( "DiscordPlugin disabled in config..." );
			return;
		}

		m_builder.AddBuildMonitor( this );
	}

	public void Shutdown()
	{

	}

	public string GetName(){ return "DiscordPlugin"; }
#endregion

#region IBuildMonitor Interface
	public void OnRunning( string message )
	{
		string jsonMessage = "{ \"content\":\"" + message + "\" }";
		PostMessage(jsonMessage);
	}

    public void OnStep( string message )
    {
    }

    public void OnSuccess( string message )
	{
		string jsonMessage = "{ \"content\":\"" + message + "\" }";
		PostMessage(jsonMessage);
	}

	public void OnFailure( string message, string logFilename )
	{
		string jsonMessage = "{ \"content\":\"" + message + "\" }";
		PostMessage(jsonMessage);
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

	private void PostMessage( string message )
	{
		HttpWebRequest req = null;
		HttpWebResponse res = null;

		try
		{
			string webhookURL = GetConfigSetting("webhook_url");
			req = (HttpWebRequest)WebRequest.Create( webhookURL );
			req.Method = "POST";
			req.ContentType = "application/json";

			req.ContentLength = message.Length;
			var sw = new StreamWriter( req.GetRequestStream() );
			sw.Write(message);
			sw.Close();

			res = (HttpWebResponse)req.GetResponse();
			Stream responseStream = res.GetResponseStream();
			var streamReader = new StreamReader(responseStream);
			m_builder.GetLog().WriteLine( streamReader.ReadToEnd() );
		}
		catch ( Exception e )
		{
			m_builder.GetLog().WriteLine( e.ToString() );
		}
	}
}