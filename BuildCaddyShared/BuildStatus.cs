using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildCaddyShared
{
    public class BuildStatus
    {
        public enum Status
        {
            Idle = 0,
            Running = 1,
            Failure = 2,
            Success = 3
        }

        private Status m_status;

        public Status CurrentStatus
        {
            get
            {
                return m_status;
            }

            set
            {
                m_status = value;

            }
        }

        public BuildStatus()
        {
            m_status = Status.Idle;
        }
    }
}
