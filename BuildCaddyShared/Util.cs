using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildCaddyShared
{
    public class Util
    {
        public static int ParseIntFromString( string value, int defaultValue )
        {
            int result = defaultValue;
            if ( int.TryParse( value, out result ) )
            {
                return result;
            }

            return defaultValue;
        }
    }
}
