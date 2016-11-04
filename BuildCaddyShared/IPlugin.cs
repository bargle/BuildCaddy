namespace BuildCaddyShared
{
	public interface IPlugin
	{
		void Initialize( IBuilder builder );
		void Shutdown();
		string GetName();
	}
}