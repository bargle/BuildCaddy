namespace BuildCaddyShared
{

    public class NetworkMessage : IMessage
    {
        string m_message = string.Empty;
        string m_op = string.Empty;

	    public string GetMessage() { return m_message; }
        public string GetOperation() {  return m_op; }
    }

}