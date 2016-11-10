﻿using System;
using System.IO;
using System.Threading;
using System.Reflection;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Diagnostics;

using BuildCaddyShared;

namespace BuildCaddy
{
	class Builder : IBuilder
	{
        struct Command
        {
            public Command( string command, string[] args )
            {
                m_command = command;
                m_args = args;
            }

            public string m_command;
            public string[] m_args;
        }

        ConcurrentQueue< Command > m_commands = new ConcurrentQueue<Command>();
        AutoResetEvent m_commandEvent = new AutoResetEvent(false);

		Dictionary<string,string> s_Config = new Dictionary<string,string>();
		BuildCaddyShared.Task s_Task = new BuildCaddyShared.Task();
		BuildStatusMonitor m_buildStatusMonitor = new BuildStatusMonitor();
        string m_taskName = string.Empty;
        string m_taskFilename = string.Empty;

		ILog m_log;

		//Temp
		string m_lastError = string.Empty;
		string m_lastLog = string.Empty;

		//FIXME: status should be componentized
		string currentStepTitle = string.Empty;

		#region IBuilder Interface
        //TODO: make this threadsafe
		
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

        public void QueueCommand( string command, string[] args )
        {
            m_commands.Enqueue( new Command( command, args ) );
            m_commandEvent.Set();
        }
        #endregion

        public void Initialize( string[] args )
		{
			m_log = new Log();

			//Read Builder Config
			Config.ReadJSONConfig( "builder.config", ref s_Config );

			//Usage: BuildCaddy.exe <MyTask.task>
			if ( args.Length > 0 )
			{
				//make sure this is a task...
				if ( Path.GetExtension( args[0].ToLower() ).CompareTo( ".task" ) == 0 )
				{
					m_taskFilename = args[0];
				}
			}
			else
			{
				m_log.WriteLine( "Usage: BuildCaddy.exe <task>" );
				return;
			}

			//Read task meta config
			string taskMeta = m_taskFilename.Replace( ".task", ".meta" );
			Config.ReadJSONConfig( taskMeta, ref s_Config );
			
			//Resolve all current outstanding variables ( config -> meta )
			Config.ResolveVariables( s_Config, s_Config );
		}

        void RunQueue()
        {
            bool done = false;
            while( !done )
            {
                while ( m_commands.Count > 0 )
                {
                    Command command;
                    if ( m_commands.TryDequeue( out command ) )
                    {
                        //handle the command
                        switch( command.m_command )
                        {
                            case "build":
                                {
                                    //first arg is rev number
                                    if ( command.m_args.Length > 0 )
                                    {
                                        int rev = 0;
                                        if ( int.TryParse( command.m_args[0], out rev ) ) //let's make sure this is actually a number...
                                        {
					                        m_buildStatusMonitor.SetRunning( "Starting " + m_taskName.Replace( ".task", "" ) + " build rev " + rev.ToString() + "..." );

					                        m_log.WriteLine( "Building Revision " + rev + "..." );
					                        if ( KickoffBuild( m_taskName, rev.ToString() ) )
					                        {
						                        m_buildStatusMonitor.SetSuccess( "Finished " + m_taskName.Replace(".task", "") + " build rev " + rev.ToString() + "..." );
                                                m_log.WriteLine( "Finished Revision " + rev + "..." );
					                        }
					                        else
					                        {
                                                m_buildStatusMonitor.SetFailure(m_lastError, m_lastLog);
                                                m_log.WriteLine( "Failed! Revision " + rev + "..." );
					                        }
                                        }
                                    }
                                } break;
                            default:
                                {
                                    GetLog().WriteLine( "Unrecognized command: " + command.m_command );
                                }
                                break;
                        }
                    }
                }
                //WAIT FOR SIGNAL
                m_commandEvent.WaitOne();

                GetLog().WriteLine( "RunQueue awoken..." );
            }
        }

		public void Run( )
		{
			//Read task config
			if ( s_Task.Initialize( m_taskFilename ) )
			{ 
				s_Task.ResolveVariables( s_Config );

                m_taskName = GetConfigSetting( "taskname" );
                if ( m_taskName.Length == 0 )
                {
                    m_taskName = Path.GetFileNameWithoutExtension( m_taskFilename );
                }

                RunQueue();
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

	}
}