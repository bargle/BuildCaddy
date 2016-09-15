using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net;

using System.Diagnostics;
using System.IO;

using System.Net.Mail;

using BuildCaddyShared;

namespace BuildCaddy
{
    class Program
    {
        static string s_Invalid = "INVALID";
        static Dictionary<string,string> s_Config = new Dictionary<string,string>();
        static BuildCaddyShared.Task s_Task = new BuildCaddyShared.Task();
        static BuildStatusMonitor m_buildStatusMonitor = new BuildStatusMonitor();

        //Temp
        static string m_lastError = string.Empty;
        static string m_lastLog = string.Empty;

        //FIXME: status should be componentized
        static string currentStepTitle = string.Empty;

        static EmailNotifier m_emailNotifier;
        static HipchatNotifier m_hipchatNotifier;

        static string GetConfigSetting( string key )
        {
            if ( !s_Config.ContainsKey( key ) )
            { 
                return string.Empty;
            }

            return s_Config[key];
        }

        static string GetAndUpdateRevisionNumber( string url )
        {
	        ProcessStartInfo start = new ProcessStartInfo();
	        start.FileName = GetConfigSetting( "SVN_BINARY" );
            start.Arguments = "info " + url;
	        start.UseShellExecute = false;
	        start.RedirectStandardOutput = true;

	        using ( Process process = Process.Start( start ) )
	        {
	            using ( StreamReader reader = process.StandardOutput )
	            {
		            string result = reader.ReadToEnd();
                    string[] tokens = result.Split( new char[]{ '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries );
                    foreach( string token in tokens )
                    {
                        if ( token.Contains( "Last Changed Rev" ) )
                        {
                            string[] revisionTokens = token.Split( new char[]{ ':' }, StringSplitOptions.RemoveEmptyEntries );
                            if ( revisionTokens.Length > 1 )
                            { 
                                return revisionTokens[1].Replace( " ", "" );
                            }
                        }
                    }
	            }
	        }

            //FIXME: Use a common static string, or similar...
            return s_Invalid;
        }

        //TODO: this process needs to be done in a thread...
        static bool KickoffBuild( string taskName, string rev )
        {
            //Set Env vars: these will now be process-wide.
            foreach ( var pair in s_Config )
            { 
                Environment.SetEnvironmentVariable( pair.Key, pair.Value );
            }

            //Build Just-in-time build session variables. (Such as revision number)
            Dictionary< string, string > dict = new Dictionary<string,string>();
            dict.Add( "REVISION", rev );
 
            for ( int i = 0; i < s_Task.Steps.Count; i++ )
            {
                m_lastLog = string.Empty;

                Step step = new Step();

                //Copy the step, so we can modify the strings at build time
                step.Copy( s_Task.Steps[ i ] );

                //resolve variables against current context (rev number etc...)
                step.ResolveVariables( dict );

                //Grab the current step title
                currentStepTitle = step.m_Title;

                //Run the step...
                if ( step.m_Command.Length > 0 )
                { 
                    Console.WriteLine( "Starting step: " + step.m_Title );

                    ProcessStartInfo start = new ProcessStartInfo();
	                start.FileName = step.m_Command;
                    start.Arguments = step.m_Args;

                    if ( step.m_Batch == false )
                    {
                        start.UseShellExecute = false;
                        start.RedirectStandardOutput = true;

                        try
                        {
                            using ( Process process = Process.Start( start ) )
                            {
                                process.WaitForExit();
                                if ( process.ExitCode != 0 && step.m_IgnoreErrors == false )                                
                                {
                                    Console.WriteLine( "ERROR on step: " + step.m_Title );

                                    m_lastLog = step.m_Log;

                                    //TODO: add full build information
                                    m_lastError =  "TASK: " + taskName + "\nERROR on build step: " + step.m_Title ;
                                    return false;
                                }

                                using ( StreamReader reader = process.StandardOutput )
                                {
                                    string result = reader.ReadToEnd();
                                    if ( result.Length > 0 )
                                    { 
                                        Console.WriteLine( result );
                                    }
                                }
                            }
                        }
                        catch ( System.Exception e )
                        { 
                            m_lastError =   "Exception on: " + start.FileName + " " + start.Arguments + "\n" + e.ToString() ;
                            Console.WriteLine( m_lastError );
                            return false;
                        }
                    }
                    else
                    { 
                        try 
                        {
                            using ( Process process = Process.Start( start ) )
                            {
                                process.WaitForExit();
                                if ( process.ExitCode != 0 && step.m_IgnoreErrors == false )                                
                                {
                                    Console.WriteLine( "ERROR on step: " + step.m_Title );

                                    m_lastLog = step.m_Log;

                                    //TODO: add full build information
                                    m_lastError =   "TASK: " + taskName + "\nERROR on build step: " + step.m_Title;
                                    return false;
                                }
                            }
                        }
                        catch ( System.Exception e )
                        { 
                            m_lastError =   "Exception on: " + start.FileName + " " + start.Arguments + "\n" + e.ToString() ;
                            Console.WriteLine( m_lastError );
                            return false;
                        }
                    }
                }   
            }
            return true;
        }

        static void RunBuilder( string taskName, bool forceBuild = false )
        {
            Console.WriteLine( "Running " + taskName.Replace( ".task", "" ) + " builder..." );

            string rev = GetAndUpdateRevisionNumber( GetConfigSetting( "repo" ) );
            Console.WriteLine( "Initial Revision Number is: " + rev );

            int delay = Util.ParseIntFromString( GetConfigSetting( "delay" ), 60000 );

            while (true)
            {
                Thread.Sleep( delay );
                Console.WriteLine( "Checking Source Control..." );
                string rev_current = GetAndUpdateRevisionNumber( GetConfigSetting( "repo" ) );

                if ( rev_current.CompareTo( s_Invalid ) == 0 )
                { 
                    continue;
                }

                bool shouldBuild = ( rev.CompareTo(rev_current) != 0 ) || forceBuild;
                forceBuild = false;

                if ( shouldBuild )
                {
                    m_buildStatusMonitor.SetRunning( "Starting " + taskName.Replace( ".task", "" ) + " build rev " + rev_current + "..." );

                    Console.WriteLine( "  Update Detected: " + rev + " != " + rev_current );

                    rev = rev_current;
                    Console.WriteLine( "  Building Revision " + rev + "..." );
                    if ( KickoffBuild( taskName, rev ) )
                    {
                        m_buildStatusMonitor.SetSuccess( "Finished " + taskName.Replace(".task", "") + " build rev " + rev_current + "..." );
                    }
                    else
                    { 
                        m_buildStatusMonitor.SetFailure( m_lastError, m_lastLog );
                    }
                    Console.WriteLine( "  Done..." );
                }
                else
                { 
                    Console.WriteLine("  No Updates... (rev: " + rev_current + ")");
                }
            }

            m_buildStatusMonitor.SetIdle();
        }

        static void SetupNotifiers()
        {
            if ( GetConfigSetting("emailalerts").ToLower().CompareTo("true") == 0 )
            {
                m_emailNotifier = new EmailNotifier( GetConfigSetting("smtp_server") );
                m_emailNotifier.SetCredentials( GetConfigSetting("smtp_user"), GetConfigSetting("smtp_pass") );
                m_emailNotifier.SetSenderAddress( GetConfigSetting("smtp_sender") );
                m_emailNotifier.SetRecipient( GetConfigSetting("smtp_recipient") );
                m_buildStatusMonitor.OnFailure += m_emailNotifier.OnFailure;
            }

            string hipchatEnabled = GetConfigSetting( "hipchat" );
            if ( hipchatEnabled.Length != 0 && hipchatEnabled.CompareTo( "enable" ) == 0 )
            {
                m_hipchatNotifier = new HipchatNotifier( GetConfigSetting( "curlbinary" ), GetConfigSetting("hipchaturl") );
                m_buildStatusMonitor.OnFailure += m_hipchatNotifier.OnFailure;
                m_buildStatusMonitor.OnSuccess += m_hipchatNotifier.OnSuccess;
                m_buildStatusMonitor.OnRunning += m_hipchatNotifier.OnRunning;
            }

        }

        static void Main(string[] args)
        {
            //Read Builder Config
            Config.ReadJSONConfig( "build.config", ref s_Config );

            //Set initial task
            string taskFilename = string.Empty;

            //Usage: BuildCaddy.exe <MyTask.task>
            if ( args.Length > 0 )
            {
                //make sure this is a task...
                if ( Path.GetExtension( args[0].ToLower() ).CompareTo( ".task" ) == 0 )
                {
                    taskFilename = args[0];
                }
            }
            else
            {
                Console.WriteLine( "Usage: BuildCaddy.exe <task>" );
                return;
            }

            //Read task meta config
            string taskMeta = taskFilename.Replace( ".task", ".meta" );
            Config.ReadJSONConfig( taskMeta, ref s_Config );
            
            //Resolve all current outstanding variables ( config -> meta )
            Config.ResolveVariables( s_Config, s_Config );

            //Read task config
            if ( s_Task.Initialize( taskFilename ) )
            { 
                s_Task.ResolveVariables( s_Config );

                SetupNotifiers();

                bool forceBuildOnLaunch = GetConfigSetting( "force_build_on_launch" ).ToLower().CompareTo( "true" ) == 0;
            
                RunBuilder( taskFilename, forceBuildOnLaunch );
            }
        }
    }
}
