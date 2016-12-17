using System.Net.Mail;
using System.Net.Mime;
using System.Collections.Generic;
using System.IO;

using BuildCaddyShared;


public class Plugin : IPlugin, IBuildMonitor
{
	string m_server;
	string m_u;
	string m_p;
	string m_from;
	string m_recipient;
	IBuilder m_builder;
    private Dictionary<string, string> m_Config = new Dictionary< string, string >();

#region IPlugin Interface
	public void Initialize( IBuilder builder )
	{
		m_builder = builder;

        string cfg_filename = m_builder.GetConfigFilePath( "mailnotifier.cfg" );
        if ( !Config.ReadJSONConfig( cfg_filename, ref m_Config ) )
        {
            m_builder.GetLog().WriteLine( "Error loading mailnotifier.cfg! MailNotifierPlugin disabled..." );
            return;
        }

        string enabled = GetConfigSetting( "enabled" );
        if ( enabled.ToLower().CompareTo( "true" ) != 0 )
		{
            m_builder.GetLog().WriteLine( "MailNotifierPlugin disabled in config..." );
			return;
		}

		m_server		= GetConfigSetting( "smtp_server" );
		m_u				= GetConfigSetting( "smtp_user" );
		m_p				= GetConfigSetting( "smtp_pass" );
		m_from			= GetConfigSetting( "smtp_sender" );
		m_recipient		= GetConfigSetting( "smtp_recipient" );

		m_builder.AddBuildMonitor( this );
	}

	public void Shutdown()
	{

	}

	public string GetName(){ return "MailNotifierPlugin"; }
#endregion

#region IBuildMonitor Interface
	public void OnRunning( string message ){}
    public void OnStep( string message ){}
    public void OnSuccess( string message ){}

	public void OnFailure( string message, string logFilename )
	{
        SendMessage( message, logFilename );
	}
#endregion

    void SendMessage( string message, string logFilename = "" )
    {
		   if ( !ValidateMetaData() )
			{ 
				m_builder.GetLog().WriteLine( "Can not send mail, as all data is not properly set." );
				return;
			}

			MailMessage mail = new MailMessage();
			SmtpClient SmtpServer = new SmtpClient( m_server );

            m_builder.GetLog().WriteLine( "Sending mail to: " + m_recipient + " via " + m_server );

			mail.From = new MailAddress( m_from );
			mail.To.Add( m_recipient );
			mail.Subject = "Build Alert!";
			mail.Body = message;

			if ( logFilename != null && logFilename.Length > 0 )
			{
				if ( File.Exists ( logFilename ) )
				{
					Attachment data = new Attachment( logFilename );
					mail.Attachments.Add( data );
				}
			}

			SmtpServer.Port = 587;
			SmtpServer.Credentials = new System.Net.NetworkCredential( m_u, m_p );
			SmtpServer.EnableSsl = true;
			SmtpServer.Timeout = 30000;
			SmtpServer.DeliveryMethod = SmtpDeliveryMethod.Network;

			try
			{
				SmtpServer.Send( mail );
			}
			catch ( System.Exception e )
			{ 
				m_builder.GetLog().WriteLine( "Failed to send mail!" );
				m_builder.GetLog().WriteLine( e.ToString() );
			}
    }

    string GetConfigSetting( string key )
    {
        if ( !m_Config.ContainsKey( key ) )
        {
            return string.Empty;
        }

        return m_Config[ key ];
    }

	bool ValidateMetaData()
	{
		if (
			( m_server == null || m_server.Length < 1 ) ||
			( m_u == null || m_u.Length < 1 ) ||
			( m_p == null || m_p.Length < 1 ) ||
			( m_from == null || m_from.Length < 1 ) ||
			( m_recipient == null || m_recipient.Length < 1 )
			)
		{ 
			return false;
		}

		return true;
	}
}
