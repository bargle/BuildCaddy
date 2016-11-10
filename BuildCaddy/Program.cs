using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using BuildCaddyShared;


namespace BuildCaddy
{
	class Program
	{
		static string s_PluginsPath = "Plugins";
		static List<IPlugin> s_Plugins = new List<IPlugin>();

		static void LoadPlugins( IBuilder builder )
		{
			string[] files = Directory.GetFiles( s_PluginsPath, "*.dll" );

			foreach( string file in files )
			{
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

		static void Main(string[] args)
		{
			Builder builder = new Builder();
			builder.Initialize( args );

			LoadPlugins( builder );

			builder.Run( );
		}
	}
}
