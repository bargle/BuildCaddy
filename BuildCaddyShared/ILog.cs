namespace BuildCaddyShared
{
	public interface ILog
	{
		void Write( string msg );
		void WriteLine( string msg );
	}
}