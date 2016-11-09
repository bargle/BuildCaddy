namespace BuildCaddyShared
{
	public interface IMessage
	{
		string GetMessage();
        string GetOperation();
        string GetValue( string key );
	}
}