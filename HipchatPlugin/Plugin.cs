using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

using BuildCaddyShared;

public class Plugin : IPlugin, IBuildMonitor
{
	IBuilder m_builder;
    private Dictionary<string, string> m_Config = new Dictionary< string, string >();

#region IPlugin Interface
	public void Initialize( IBuilder builder )
	{
		m_builder = builder;

        string cfg_filename = m_builder.GetConfigFilePath( "hipchat.cfg" );
        if ( !Config.ReadJSONConfig( cfg_filename, ref m_Config ) )
        {
            m_builder.GetLog().WriteLine( "Error loading hipchat.cfg! HipchatPlugin disabled..." );
            return;
        }

		string enabled = GetConfigSetting( "enabled" );
		if ( enabled.Length == 0 || enabled.ToLower().CompareTo( "true" ) != 0 )
		{
            m_builder.GetLog().WriteLine( "HipchatPlugin disabled in config..." );
			return;
		}

		m_builder.AddBuildMonitor( this );
	}

	public void Shutdown()
	{

	}

	public string GetName(){ return "HipchatPlugin"; }
#endregion

#region IBuildMonitor Interface
	public void OnRunning( string message )
	{
		ProcessStartInfo start = CreateProcessStartInfo();
		start.Arguments = "-s -d \"&message="+ message +"\" " + GetConfigSetting( "room_url" );
		DoWork( start );
	}

    public void OnStep( string message )
    {
    }

    public void OnSuccess( string message )
	{
		ProcessStartInfo start = CreateProcessStartInfo();
		start.Arguments = "-s -d \"&message=" + message + "&color=green\" " + GetConfigSetting( "room_url" );
		DoWork( start );
	}

	public void OnFailure( string message, string logFilename )
	{
		ProcessStartInfo start = CreateProcessStartInfo();
		start.Arguments = "-s -d \"&message=" + message + "&color=red\" " + GetConfigSetting( "room_url" );
		DoWork( start );
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

	private ProcessStartInfo CreateProcessStartInfo()
	{
		ProcessStartInfo start = new ProcessStartInfo();
		start.FileName = GetConfigSetting( "cURL_BINARY" );
		start.UseShellExecute = false;
		start.RedirectStandardOutput = true;

		return start;
	}

	private void DoWork( ProcessStartInfo startInfo )
	{
		try
		{
			using ( Process process = Process.Start( startInfo ) )
			{
			}
		}
		catch ( System.Exception e )
		{ 
			m_builder.GetLog().WriteLine( e.ToString() );
		}
	}
}