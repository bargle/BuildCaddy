using System;
using BuildCaddyShared;

namespace BuildCaddy
{
	public class Log : ILog
	{
		public void Write( string message )
		{
			Console.Write( message );
		}

		public void WriteLine( string message )
		{
			Console.WriteLine( message );
		}
	}
}