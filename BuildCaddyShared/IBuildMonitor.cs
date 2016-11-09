﻿namespace BuildCaddyShared
{
	public interface IBuildMonitor
	{
		void OnRunning( string message );
        void OnStep(string message);
        void OnSuccess( string message );
		void OnFailure( string message, string logFilename );
	}
}