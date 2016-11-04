namespace BuildCaddyShared
{
	public interface IBuilder
	{
		string GetConfigString( string key );
		void AddBuildMonitor( IBuildMonitor monitor );
		ILog GetLog();
	}
}