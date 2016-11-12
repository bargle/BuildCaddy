using System;
using System.Text;
using System.IO;
using System.Security.Cryptography;

namespace BuildCaddyShared
{
    namespace Encryption
    {
        public class AES
        {
            //TODO: change this...
            static byte[] s_saltBytes = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };

            static public byte[] Encrypt( byte[] bytesToBeEncrypted, byte[] passwordBytes )
            {
                byte[] encryptedBytes = null;

                using ( MemoryStream ms = new MemoryStream() )
                {
                    using ( RijndaelManaged AES = new RijndaelManaged() )
                    {
                        AES.KeySize = 256;
                        AES.BlockSize = 128;
                        var key = new Rfc2898DeriveBytes( passwordBytes, s_saltBytes, 1000 );
                        AES.Key = key.GetBytes( AES.KeySize / 8 );
                        AES.IV = key.GetBytes( AES.BlockSize / 8 );
                        AES.Mode = CipherMode.CBC;

                        using ( var cs = new CryptoStream( ms, AES.CreateEncryptor(), CryptoStreamMode.Write ) )
                        {
                            cs.Write( bytesToBeEncrypted, 0, bytesToBeEncrypted.Length );
                            cs.Close();
                        }
                        encryptedBytes = ms.ToArray();
                    }
                }

                return encryptedBytes;
            }

            static public byte[] Decrypt( byte[] bytesToBeDecrypted, byte[] passwordBytes )
            {
                byte[] decryptedBytes = null;

                using ( MemoryStream ms = new MemoryStream() )
                {
                    using ( RijndaelManaged AES = new RijndaelManaged() )
                    {
                        AES.KeySize = 256;
                        AES.BlockSize = 128;
                        var key = new Rfc2898DeriveBytes( passwordBytes, s_saltBytes, 1000 );
                        AES.Key = key.GetBytes( AES.KeySize / 8 );
                        AES.IV = key.GetBytes( AES.BlockSize / 8 );
                        AES.Mode = CipherMode.CBC;

                        using ( var cs = new CryptoStream( ms, AES.CreateDecryptor(), CryptoStreamMode.Write ) )
                        {
                            try
                            {
                                cs.Write( bytesToBeDecrypted, 0, bytesToBeDecrypted.Length );
                                cs.Close();
                            } catch ( System.Exception ) { }
                        }
                        decryptedBytes = ms.ToArray();
                    }
                }
                return decryptedBytes;
            }

            static public string EncryptText( string input, string password )
            {
                // Get the bytes of the string
                byte[] bytesToBeEncrypted = Encoding.UTF8.GetBytes( input );
                byte[] passwordBytes = Encoding.UTF8.GetBytes( password );

                // Hash the password with SHA256
                passwordBytes = SHA256.Create().ComputeHash( passwordBytes );

                byte[] bytesEncrypted = Encrypt( bytesToBeEncrypted, passwordBytes );
                string result = Convert.ToBase64String( bytesEncrypted );
                return result;
            }

            static public byte[] EncryptTextToBytes( string input, string password )
            {
                // Get the bytes of the string
                byte[] bytesToBeEncrypted = Encoding.UTF8.GetBytes( input );
                byte[] passwordBytes = Encoding.UTF8.GetBytes( password );

                // Hash the password with SHA256
                passwordBytes = SHA256.Create().ComputeHash( passwordBytes );
                return Encrypt( bytesToBeEncrypted, passwordBytes );
            }

            static public string DecryptText( string input, string password )
            {
                try
                { 
                    byte[] bytesToBeDecrypted = Convert.FromBase64String( input );
                    byte[] passwordBytes = Encoding.UTF8.GetBytes( password );
                    passwordBytes = SHA256.Create().ComputeHash( passwordBytes );

                    byte[] bytesDecrypted = Decrypt( bytesToBeDecrypted, passwordBytes );
                    string result = Encoding.UTF8.GetString( bytesDecrypted );
                    return result;
                } catch ( System.Exception ) { }

                return string.Empty;
            }

            static public string DecryptText( byte[] bytesToBeDecrypted, string password )
            {
                try
                { 
                    byte[] passwordBytes = Encoding.UTF8.GetBytes( password );
                    passwordBytes = SHA256.Create().ComputeHash( passwordBytes );
                    byte[] bytesDecrypted = Decrypt( bytesToBeDecrypted, passwordBytes );
                    string result = Encoding.UTF8.GetString( bytesDecrypted );
                    return result;
                } catch ( System.Exception ) { }

                return string.Empty;
            }
        }
    }

}