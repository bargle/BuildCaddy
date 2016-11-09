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

#region IPlugin Interface
	public void Initialize( IBuilder builder )
	{
		m_builder = builder;

		string hipchatEnabled = m_builder.GetConfigString( "hipchat" );
		if ( hipchatEnabled.Length == 0 || hipchatEnabled.CompareTo( "enable" ) != 0 )
		{
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
		start.Arguments = "-d \"&message="+ message +"\" " + m_builder.GetConfigString( "hipchaturl" );
		DoWork( start );
	}

    public void OnStep( string message )
    {
    }

    public void OnSuccess( string message )
	{
		ProcessStartInfo start = CreateProcessStartInfo();
		start.Arguments = "-d \"&message=" + message + "&color=green\" " + m_builder.GetConfigString( "hipchaturl" );
		DoWork( start );
	}

	public void OnFailure( string message, string logFilename )
	{
		ProcessStartInfo start = CreateProcessStartInfo();
		start.Arguments = "-d \"&message=" + message + "&color=red\" " + m_builder.GetConfigString( "hipchaturl" );
		DoWork( start );
	}
#endregion

	private ProcessStartInfo CreateProcessStartInfo()
	{
		ProcessStartInfo start = new ProcessStartInfo();
		start.FileName = m_builder.GetConfigString( "curlbinary" );
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