using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace BuildCaddyShared
{
	public class NetworkSerialize
	{
		public static byte[] SerializeToBytes<T>( T source )
		{
			using (var stream = new MemoryStream())
			{
			try
			{
				var formatter = new BinaryFormatter();
				formatter.Serialize(stream, source);
				return stream.ToArray();
			}
			catch( System.Exception) { }

			return null;
			}
		}

		// Deerialize from bytes (BinaryFormatter)
		public static T DeserializeFromBytes<T>( byte[] source ) where T : Packet
		{
			using (var stream = new MemoryStream(source))
			{
				try
				{
					var formatter = new BinaryFormatter();
					stream.Seek(0, SeekOrigin.Begin);
					return (T)formatter.Deserialize(stream);
				}
				catch( System.Exception )
				{

				}
				return null;
			}
		}

	}
}