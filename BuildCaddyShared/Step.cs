using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildCaddyShared
{
    public class Step
    {
        //TODO: provide better access structure to these variables
        public string m_Title;
        public string m_Command;
        public string m_Args;
        public string m_WorkingFolder;
        public string m_Log;
        public bool m_Batch;
        public bool m_IgnoreErrors;

        public void Copy( Step rhs )
        {
            m_Title         = rhs.m_Title;
            m_Command       = rhs.m_Command;
            m_Args          = rhs.m_Args;
            m_WorkingFolder = rhs.m_WorkingFolder;
            m_Log           = rhs.m_Log;
            m_Batch         = rhs.m_Batch;
            m_IgnoreErrors  = rhs.m_IgnoreErrors;
        }

        public void ResolveVariables( Dictionary<string, string> dict )
        {
            Config.ResolveVariables( ref m_Title,           dict );
            Config.ResolveVariables( ref m_Command,         dict );
            Config.ResolveVariables( ref m_Args,            dict );
            Config.ResolveVariables( ref m_WorkingFolder,   dict );
            Config.ResolveVariables( ref m_Log,             dict );
        }
    }
}
