namespace BuildCaddyShared
{
	public interface IBuilder
	{
		string GetConfigString( string key );
        string GetName();
		void AddBuildMonitor( IBuildMonitor monitor );
		ILog GetLog();
        string GetConfigFilePath(string filename);
	}
}