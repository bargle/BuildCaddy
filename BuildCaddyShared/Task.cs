using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildCaddyShared
{
        public class Task
        {
            List<Step> m_steps = new List<Step>();

            public List<Step> Steps
            {
                get
                {
                    return m_steps;
                }
            }

            public Task()
            {

            }

            public bool Initialize( string filename )
            {
                return Config.ReadJSONTask( filename, ref m_steps );
            }

            public void ResolveVariables( Dictionary<string, string> dict )
            {
                for ( int i = 0; i < m_steps.Count; i++ )
                { 
                    m_steps[ i ].ResolveVariables( dict );
                }
            }
        }
}
