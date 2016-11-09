using System;
using System.IO;
using System.Threading;
using System.Reflection;
using System.Collections.Generic;
using System.Diagnostics;

using BuildCaddyShared;

namespace BuildCaddy
{
	class Builder : IBuilder
	{
        static int s_DefaultDelay = 60000;
		static string s_Invalid = "INVALID";
		Dictionary<string,string> s_Config = new Dictionary<string,string>();
		BuildCaddyShared.Task s_Task = new BuildCaddyShared.Task();
		BuildStatusMonitor m_buildStatusMonitor = new BuildStatusMonitor();
        string m_taskName = string.Empty;

		ILog m_log;

		//Temp
		string m_lastError = string.Empty;
		string m_lastLog = string.Empty;

		//FIXME: status should be componentized
		string currentStepTitle = string.Empty;

		#region IBuilder Interface
		
		public string GetConfigString( string key )
		{
			return GetConfigSetting( key );
		}

		public void AddBuildMonitor( IBuildMonitor monitor )
		{
			m_buildStatusMonitor.OnFailure += monitor.OnFailure;
			m_buildStatusMonitor.OnSuccess += monitor.OnSuccess;
			m_buildStatusMonitor.OnRunning += monitor.OnRunning;
            m_buildStatusMonitor.OnStep += monitor.OnStep;
        }

		public ILog GetLog()
		{
			return m_log;
		}

        public string GetConfigFilePath( string filename )
        {
            return Path.Combine( "Config", filename );
        }

        public string GetName()
        {
            return GetConfigString( "taskname" );
        }
        #endregion

        public void Initialize( string[] args )
		{
			m_log = new Log();

			//Read Builder Config
			Config.ReadJSONConfig( "builder.config", ref s_Config );
		}

		public void Run( string[] args )
		{
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
				m_log.WriteLine( "Usage: BuildCaddy.exe <task>" );
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

                m_taskName = GetConfigSetting( "taskname" );
                if ( m_taskName.Length == 0 )
                {
                    m_taskName = Path.GetFileNameWithoutExtension( taskFilename );
                }

				bool forceBuildOnLaunch = GetConfigSetting( "force_build_on_launch" ).ToLower().CompareTo( "true" ) == 0;
			
				RunBuilder( taskFilename, forceBuildOnLaunch );
			}
		}

		string GetConfigSetting( string key )
		{
			if ( !s_Config.ContainsKey( key ) )
			{ 
				return string.Empty;
			}

			return s_Config[ key ];
		}

		string GetAndUpdateRevisionNumber( string url )
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
		bool KickoffBuild( string taskName, string rev )
		{
			//Set Env vars: these will now be process-wide.
			foreach ( var pair in s_Config ) //TODO: this needs to honor the ENV tag
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
					m_log.WriteLine( "Starting step: " + step.m_Title );

                    //Inform the build monitor that we are starting this step
                    m_buildStatusMonitor.SetStep( step.m_Title );

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
									m_log.WriteLine( "ERROR on step: " + step.m_Title );

									m_lastLog = step.m_Log;

									//TODO: add full build information
									m_lastError =  "TASK: " + taskName + "\nERROR on build step: " + step.m_Title + " Revision: " + rev;
                                    return false;
								}

								using ( StreamReader reader = process.StandardOutput )
								{
									string result = reader.ReadToEnd();
									if ( result.Length > 0 )
									{ 
										m_log.WriteLine( result );
									}
								}
							}
						}
						catch ( System.Exception e )
						{ 
							m_lastError =   "Exception on: " + start.FileName + " " + start.Arguments + "\n" + e.ToString() ;
							m_log.WriteLine( m_lastError );
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
									m_log.WriteLine( "ERROR on step: " + step.m_Title );

									m_lastLog = step.m_Log;

									//TODO: add full build information
									m_lastError =   "TASK: " + taskName + "\nERROR on build step: " + step.m_Title + " Revision: " + rev;
                                    return false;
								}
							}
						}
						catch ( System.Exception e )
						{ 
							m_lastError =   "Exception on: " + start.FileName + " " + start.Arguments + "\n" + e.ToString() ;
							m_log.WriteLine( m_lastError );
                            return false;
						}
					}
				}
            }
            return true;
		}

		void RunBuilder( string taskName, bool forceBuild = false )
		{
			m_log.WriteLine( "Running " + taskName.Replace( ".task", "" ) + " builder..." );

			string rev = GetAndUpdateRevisionNumber( GetConfigSetting( "repo" ) );
			m_log.WriteLine( "Initial Revision Number is: " + rev );

			int delay = Util.ParseIntFromString( GetConfigSetting( "delay" ), s_DefaultDelay );

			while (true)
			{
				m_log.WriteLine( "Checking Source Control..." );
				string rev_current = GetAndUpdateRevisionNumber( GetConfigSetting( "repo" ) );

				if ( rev_current.CompareTo( s_Invalid ) == 0 )
				{ 
                    Thread.Sleep( delay );
					continue;
				}

				bool shouldBuild = ( rev.CompareTo(rev_current) != 0 ) || forceBuild;
				forceBuild = false;

				if ( shouldBuild )
				{
					m_buildStatusMonitor.SetRunning( "Starting " + taskName.Replace( ".task", "" ) + " build rev " + rev_current + "..." );

					m_log.WriteLine( "  Update Detected: " + rev + " != " + rev_current );

					rev = rev_current;
					m_log.WriteLine( "  Building Revision " + rev + "..." );
					if ( KickoffBuild( taskName, rev ) )
					{
						m_buildStatusMonitor.SetSuccess( "Finished " + taskName.Replace(".task", "") + " build rev " + rev_current + "..." );
					}
					else
					{ 
						m_buildStatusMonitor.SetFailure( m_lastError, m_lastLog );
					}
					m_log.WriteLine( "  Done..." );
				}
				else
				{ 
					m_log.WriteLine("  No Updates... (rev: " + rev_current + ")");
					m_buildStatusMonitor.SetIdle();
				}

                Thread.Sleep( delay );
			}

			//m_buildStatusMonitor.SetIdle();
		}
	}
}