using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Net.Mail;
using System.Net.Mime;

namespace BuildCaddy
{
    public class EmailNotifier
    {
        string m_server;
        string m_u;
        string m_p;
        string m_from;
        string m_recipient;

        public EmailNotifier( string server )
        {
            m_server = server;
        }

        public void SetCredentials( string u, string p )
        {
            m_u = u;
            m_p = p;
        }

        public void SetSenderAddress( string from )
        {
            m_from = from;
        }

        public void SetRecipient( string recipient )
        {
            m_recipient = recipient;
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

        public void OnFailure( string message, string logFilename )
        {
            if ( !ValidateMetaData() )
            { 
                Console.WriteLine("Can not send mail, as all data is not properly set.");
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
                Console.WriteLine( "Failed to send mail!" );
                Console.WriteLine( e.ToString() );
            }
        }
    }
}
