public class JSONUtil
{
	public static int GetInt( JSONObject obj, string key, int defaultValue = 0 )
	{
		if ( !obj.HasField( key ) || obj[ key ].type != JSONObject.Type.NUMBER )
		{
			return defaultValue;
		}

		return (int)( obj[ key ].n );
	}

	public static float GetFloat( JSONObject obj, string key, float defaultValue = 0.0f )
	{
		if ( !obj.HasField( key ) || obj[ key ].type != JSONObject.Type.NUMBER )
		{
			return defaultValue;
		}

		return (float)( obj[ key ].n );
	}

	public static string GetString( JSONObject obj, string key, string defaultValue = "" )
	{
		if ( !obj.HasField( key ) || obj[ key ].type != JSONObject.Type.STRING )
		{
			return defaultValue;
		}

		return obj[ key ].str;
	}

	public static bool GetBool( JSONObject obj, string key, bool defaultValue = false )
	{
		if ( !obj.HasField( key ) || obj[ key ].type != JSONObject.Type.BOOL )
		{
			return defaultValue;
		}

		return obj[ key ].b;
	}
	
	public static bool HasArray( JSONObject obj, string key )
	{
		if ( !obj.HasField( key ) )
		{
			return false;
		}

		if ( obj[ key ].type == JSONObject.Type.ARRAY )
		{
			return true;
		}

		return false;
	}
};
