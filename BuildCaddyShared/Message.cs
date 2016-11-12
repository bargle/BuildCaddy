namespace BuildCaddyShared
{
    public class Message : IMessage
    {
        private int m_version = 2;
        private string m_message = string.Empty;
        private string m_op = string.Empty;
        private bool m_valid = false;
        private JSONObject m_jsonObject;

        #region IMessage Interface
        public string GetMessage() { return m_message; }
        public string GetOperation() {  return m_op; }
        public string GetSendable() {  return m_jsonObject.Print(); }
        public string GetValue( string key )
        {
            return JSONUtil.GetString( m_jsonObject, key );
        }
        #endregion

        public bool IsValid
        {
            get
            {
                return m_valid;
            }
        }

        public Message()
        {
            m_jsonObject = new JSONObject();
            m_jsonObject.AddField( "version", m_version.ToString() );
            m_valid = true;
        }

        public Message( JSONObject obj )
        {
            m_jsonObject = obj.Copy();
            m_op = JSONUtil.GetString( m_jsonObject, "OP", string.Empty );
            m_message = JSONUtil.GetString( m_jsonObject, "message", string.Empty );

			string versionString = JSONUtil.GetString( m_jsonObject, "version" );
			int version = Util.ParseIntFromString( versionString, -1 );

            if ( version == m_version )
            {
                m_valid = true;
            }
        }

        public void Add( string key, string msg )
        {
            m_jsonObject.AddField( key, msg );
        }
    }

}