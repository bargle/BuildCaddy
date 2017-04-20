namespace BuildCaddyShared
{
	public interface IBuilder
	{
		string GetConfigString( string key );
        string GetName();
		void AddBuildMonitor( IBuildMonitor monitor );
		void AddBuildQueueMonitor( IBuildQueueMonitor monitor );
		ILog GetLog();
        string GetConfigFilePath(string filename);
        void QueueCommand( string command, string[] args );
        void DequeueCommand( string command, string[] args );
		string[] GetCurrentBuildQueue();
	}
}