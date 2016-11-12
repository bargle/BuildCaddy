using System;
using System.Text;

namespace BuildCaddyShared
{
    public class EncryptedNetworkService : NetworkService
    {
        //FIXME: this should be moved to load from a cfg file.
        static string s_password = "sdS46Gshsd#*2!";

	    protected override string Decode( byte[] bytes )
	    {
            return Encryption.AES.DecryptText( bytes, s_password );
	    }

	    protected override byte[] Encode( string msg )
	    {
            return Encryption.AES.EncryptTextToBytes( msg, s_password );
	    }
    }

}