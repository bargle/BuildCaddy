using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using BuildCaddyShared;

namespace BuildCaddy
{
    public class BuildStatusMonitor
    {
        private BuildStatus.Status m_status;

        public delegate void OnTrigger();
        public delegate void OnTriggerWithMessage( string message );
        public delegate void OnTriggerWithMessageAndLog( string message, string logFilename );

        public OnTrigger OnIdle;
        public OnTriggerWithMessage OnRunning;
        public OnTriggerWithMessage OnStep;
        public OnTriggerWithMessageAndLog OnFailure;
        public OnTriggerWithMessage OnSuccess;

		public BuildStatus.Status Status
		{
			get
			{
				return m_status;
			}
		}

        public void SetIdle()
        {
            m_status = BuildStatus.Status.Idle;
            if ( OnIdle != null )
            { 
                OnIdle();
            }
        }

        public void SetRunning( string message )
        {
            m_status = BuildStatus.Status.Running;
            if ( OnRunning != null )
            { 
                OnRunning( message );
            }
        }

        public void SetStep(string message)
        {
            if ( OnStep != null )
            {
                OnStep( message );
            }
        }

        public void SetFailure( string message, string logFilename )
        {
            m_status = BuildStatus.Status.Failure;
            if ( OnFailure != null )
            { 
                OnFailure( message, logFilename );
            }
        }

        public void SetSuccess( string message )
        {
            m_status = BuildStatus.Status.Success;
            if ( OnSuccess != null )
            { 
                OnSuccess( message );
            }
        }
    }
}
