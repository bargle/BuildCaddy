using System.Net.Mail;
using System.Net.Mime;

using BuildCaddyShared;


public class Plugin : IPlugin, IBuildMonitor
{
	string m_server;
	string m_u;
	string m_p;
	string m_from;
	string m_recipient;
	IBuilder m_builder;

#region IPlugin Interface
	public void Initialize( IBuilder builder )
	{
		m_builder = builder;

		if ( m_builder.GetConfigString( "emailalerts" ).ToLower().CompareTo( "true" ) != 0 )
		{
			return;
		}

		m_server		= m_builder.GetConfigString( "smtp_server" );
		m_u				= m_builder.GetConfigString( "smtp_user" );
		m_p				= m_builder.GetConfigString( "smtp_pass" );
		m_from			= m_builder.GetConfigString( "smtp_sender" );
		m_recipient		= m_builder.GetConfigString( "smtp_recipient" );

		m_builder.AddBuildMonitor( this );
	}

	public void Shutdown()
	{

	}

	public string GetName(){ return "MailNotifierPlugin"; }
#endregion

#region IBuildMonitor Interface
	public void OnRunning( string message )
	{

	}

	public void OnSuccess( string message )
	{

	}

	public void OnFailure( string message, string logFilename )
	{
		   if ( !ValidateMetaData() )
			{ 
				m_builder.GetLog().WriteLine( "Can not send mail, as all data is not properly set." );
				return;
			}

			MailMessage mail = new MailMessage();
			SmtpClient SmtpServer = new SmtpClient( m_server );

			mail.From = new MailAddress( m_from );
			mail.To.Add( m_recipient );
			mail.Subject = "Build Alert!";
			mail.Body = message;

			if ( logFilename != null && logFilename.Length > 0 )
			{
				Attachment data = new Attachment( logFilename );
				mail.Attachments.Add( data );
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
#endregion

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
