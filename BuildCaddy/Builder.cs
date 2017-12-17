using System;
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
		List< IBuildQueueMonitor > m_buildQueueMonitors = new List<IBuildQueueMonitor>();
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

		public void AddBuildQueueMonitor( IBuildQueueMonitor monitor )
		{
			if ( !m_buildQueueMonitors.Contains( monitor ) )
			{
				m_buildQueueMonitors.Add( monitor );
			}
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
			NotifyBuildQueueMonitors();
            m_commandEvent.Set();
        }

        public void DequeueCommand( string command, string[] args )
        {
			Command[] commands = m_commands.ToArray();
			List<Command> command_list = new List<Command>( commands );
			int index = -1;
			for( int i = 0; i < command_list.Count; i++ )
			{
				if ( command_list[i].m_command.CompareTo( command ) != 0 )
				{
					continue;
				}

				if ( args == null || args.Length == 0 )
				{
					continue;
				}

				if ( command_list[i].m_args[0].CompareTo( args[0] ) != 0 )
				{
					continue;
				}

				index = i;
				break;
			}

			if ( index != -1 )
			{
				command_list.RemoveAt( index );
			}

			m_commands = new ConcurrentQueue<Command>( command_list );
			NotifyBuildQueueMonitors();
            m_commandEvent.Set();
        }

		public string[] GetCurrentBuildQueue()
		{
			if ( m_commands.Count == 0 )
			{
				return null;
			}

			Command[] queue = m_commands.ToArray();

			string[] commands = new string[ queue.Length ];

			for( int i = 0; i < commands.Length; i++ )
			{
				if ( queue[i].m_args.Length > 0 )
				{
					commands[i] = queue[i].m_command + " " + queue[i].m_args[0];
				}
				else
				{
					commands[i] = queue[i].m_command;
				}
			}

			return commands;
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
						NotifyBuildQueueMonitors();

                        //handle the command
                        switch( command.m_command )
                        {
                            case "build":
                                {
                                    //first arg is rev number
                                    if ( command.m_args.Length > 0 )
                                    {
                                        string rev = command.m_args[0];
                                        //if ( int.TryParse( command.m_args[0], out rev ) ) //let's make sure this is actually a number...
                                        {
					                        m_buildStatusMonitor.SetRunning( "Starting " + m_taskName.Replace( ".task", "" ) + " build rev " + rev.ToString() + "..." );

					                        m_log.WriteLine( "Building Revision " + rev + "..." );
											Stopwatch _stopWatch = new Stopwatch();
											_stopWatch.Start();
					                        if ( KickoffBuild( m_taskName, rev.ToString() ) )
					                        {
												_stopWatch.Stop();
												string timeElapsedString = GetElapsedTimeString( _stopWatch.ElapsedMilliseconds );
						                        m_buildStatusMonitor.SetSuccess( "Finished " + m_taskName.Replace(".task", "") + " build rev [" + rev.ToString() + "] in " + timeElapsedString );
                                                m_log.WriteLine( "Finished Revision [" + rev + "] in " + timeElapsedString );
					                        }
					                        else
					                        {
												_stopWatch.Stop();
												string timeElapsedString = GetElapsedTimeString( _stopWatch.ElapsedMilliseconds );
                                                m_buildStatusMonitor.SetFailure(m_lastError, m_lastLog);
                                                m_log.WriteLine( "Failed! Revision [" + rev + "] in " + timeElapsedString );
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
 
		static string GetElapsedTimeString( long milliseconds )
		{
			const long s_Second = 1000;
			const long s_Minute = s_Second * 60;
			const long s_Hour	= s_Minute * 60;

			long hours = milliseconds / s_Hour;

			if ( hours > 0 )
			{
				milliseconds -= ( hours * s_Hour );
			}

			long minutes = milliseconds / s_Minute;

			if ( minutes > 0 )
			{
				milliseconds -= ( minutes * s_Minute );
			}

			long seconds = milliseconds / s_Second;

			return ( ( hours > 0 ) ? hours + " hours, " : "" ) +
				( ( minutes > 0 ) ? minutes + ( ( minutes > 1 ) ? " minutes, " : " minute, "  ) : "" ) +
				(( seconds > 0 ) ? seconds + ( ( seconds > 1 ) ? " seconds" : " second"  ) : "" ) 
				;
		}

		void NotifyBuildQueueMonitors()
		{
			for( int i = 0; i < m_buildQueueMonitors.Count; i++ )
			{
				m_buildQueueMonitors[ i ].OnQueueChanged();
			}
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

                    if ( step.m_WorkingFolder.Length > 0 )
                    {
                        start.WorkingDirectory = step.m_WorkingFolder;
                    }

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
                            start.WindowStyle = ProcessWindowStyle.Minimized;

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