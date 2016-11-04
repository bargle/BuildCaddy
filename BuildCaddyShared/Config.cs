using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace BuildCaddyShared
{
    public class Config
    {
        public static bool ReadJSONConfig( string configFile, ref Dictionary< string, string > result )
        {
            if ( result == null )
            { 
                return false;
            }

            try
            {
                using (StreamReader reader = new StreamReader(configFile))
                {
                    string content = reader.ReadToEnd();
                    JSONObject obj = new JSONObject(content);
                    if (obj.type == JSONObject.Type.OBJECT)
                    {
                        for (int i = 0; i < obj.list.Count; i++)
                        {
                            JSONObject entry = (JSONObject)obj.list[i];

                            string key = string.Empty;
                            string value = string.Empty;
							string env = string.Empty;

                            if (entry.HasField("KEY") && entry["KEY"].type == JSONObject.Type.STRING)
                            {
                                key = entry["KEY"].str;
                            }

                            if (entry.HasField("VALUE") && entry["VALUE"].type == JSONObject.Type.STRING)
                            {
                                value = entry["VALUE"].str;
                            }

                            if (key.Length > 0 && value.Length > 0)
                            {
								if (entry.HasField("ENV") && entry["ENV"].type == JSONObject.Type.STRING)
								{
									env = entry["ENV"].str;
									Environment.SetEnvironmentVariable( key, value );
								}

                                result.Add(key, value);
                            }
                        }
                    }
                }
            }
            catch (System.Exception e)
            {
                Console.WriteLine( e.ToString() );
                return false;
            }

            return true;
        }

        public static bool ReadJSONTask( string configFile, ref List< Step > result )
        {
            if ( result == null )
            { 
                return false;
            }

            result.Clear();

            try
            {
                using ( StreamReader reader = new StreamReader( configFile ) )
                {
                    string content = reader.ReadToEnd();
                    JSONObject obj = new JSONObject( content );
                    if ( obj.type == JSONObject.Type.OBJECT )
    	            {
    		            for ( int i = 0; i < obj.list.Count; i++ )
    		            {
    			            JSONObject entry = (JSONObject)obj.list[i];

                            Step step = new Step();
                            step.m_Title        = JSONUtil.GetString( entry, "TITLE" );
                            step.m_Command      = JSONUtil.GetString( entry, "COMMAND" );
                            step.m_Args         = JSONUtil.GetString( entry, "ARGS" );
                            step.m_Log          = JSONUtil.GetString( entry, "LOG" );
                            step.m_Batch        = JSONUtil.GetString( entry, "BATCH" ).CompareTo("true") == 0;
                            step.m_IgnoreErrors = JSONUtil.GetString( entry, "IGNOREERRORS" ).CompareTo("true") == 0;
                            result.Add( step );
                        }
                    }
                }
            }
            catch (System.Exception e)
            {
                Console.WriteLine( e.ToString() );
                return false;
            }

            return true;
        }

        public static void ResolveVariables( ref string obj, Dictionary<string, string> dict )
        {
            foreach ( string key in dict.Keys )
            { 
                string lookupKey = "${"+key+"}";
                if ( obj.Contains( lookupKey ) )
                { 
                    string value;
                    if ( dict.TryGetValue( key, out value ) )
                    {
                        obj = obj.Replace( lookupKey, value );
                    }
                }
            }
        }

        public static void ResolveVariables( Dictionary<string, string> obj, Dictionary<string, string> dict )
        {
            foreach ( string key in dict.Keys )
            { 
                string lookupKey = "${"+key+"}";
                foreach ( var pair in obj )
                {
                    if ( pair.Value.Contains( lookupKey ) )
                    { 
                        string value;
                        if ( dict.TryGetValue( key, out value ) )
                        {
                            obj[ key ] = pair.Value.Replace( lookupKey, value );
                        }
                    }
                }
            }
        }
    }
}
