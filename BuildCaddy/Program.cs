using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using BuildCaddyShared;
using System.Runtime.InteropServices; //Making this Windows-specific so we can catch app exit. Ugly..

namespace BuildCaddy
{
	class Program
	{
    private delegate bool ConsoleEventDelegate(int eventType);
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetConsoleCtrlHandler(ConsoleEventDelegate callback, bool add);
    static ConsoleEventDelegate handler;

    static string s_PluginsPath = "Plugins";
		static List<IPlugin> s_Plugins = new List<IPlugin>();

		static void LoadPlugins( IBuilder builder )
		{
			string[] files = Directory.GetFiles( s_PluginsPath, "*.dll" );

			foreach( string file in files )
			{
				Console.WriteLine( $"Loading plugin: {file}" );
				var asm = Assembly.LoadFile( Path.Combine( Directory.GetCurrentDirectory(), file ) );
				var type = asm.GetType("Plugin");
				IPlugin plugin = Activator.CreateInstance(type) as IPlugin;
				if ( plugin != null )
				{
					plugin.Initialize( builder );
					s_Plugins.Add( plugin );
				}
			}
		}

    static bool ConsoleEventCallback(int eventType)
    {
      switch (eventType)
      {
				case 0:
				case 1:
				case 2:
				{
					foreach (var plugin in s_Plugins)
					{
						plugin.Shutdown();
					}
				} break;
      }
      return false;
    }

    static void Main(string[] args)
		{
			Builder builder = new Builder();
			builder.Initialize( args );

			LoadPlugins( builder );

      Console.CancelKeyPress += delegate {
        // call methods to clean up
				foreach( var plugin in s_Plugins )
				{
					plugin.Shutdown();
				}
				return;
      };

      handler = new ConsoleEventDelegate(ConsoleEventCallback);
      SetConsoleCtrlHandler(handler, true);

      builder.Run( );
		}
	}
}
