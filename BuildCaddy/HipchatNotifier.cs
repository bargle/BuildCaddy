using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace BuildCaddy
{
    public class HipchatNotifier
    {
        string m_cUrl = string.Empty;
        string m_url = string.Empty;

        public HipchatNotifier( string cUrl, string url )
        {
            m_cUrl = cUrl;
            m_url = url;
        }

        private ProcessStartInfo CreateProcessStartInfo()
        {
	        ProcessStartInfo start = new ProcessStartInfo();
	        start.FileName = m_cUrl;
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
                Console.WriteLine( e.ToString() );
            }
        }

        public void OnRunning( string message )
        {
            ProcessStartInfo start = CreateProcessStartInfo();
            start.Arguments = "-d \"&message="+ message +"\" " + m_url;
            DoWork( start );
        }

        public void OnFailure( string message, string logFilename )
        {
            ProcessStartInfo start = CreateProcessStartInfo();
            start.Arguments = "-d \"&message=" + message + "&color=red\" " + m_url;
            DoWork( start );
        }

        public void OnSuccess( string message )
        {
            ProcessStartInfo start = CreateProcessStartInfo();
            start.Arguments = "-d \"&message=" + message + "&color=green\" " + m_url;
            DoWork( start );
        }
    }
}
