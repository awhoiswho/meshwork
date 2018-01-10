// 
// LoggingService.cs
// 
// Author:
//   Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
// 
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections.Generic;
using Meshwork.Backend.Core.Logging;

namespace Meshwork.Backend.Core
{
	public static class LoggingService
	{
		static List<ILogger> loggers = new List<ILogger> ();
		
		static LoggingService ()
		{
			var consoleLogger = new ConsoleLogger ();
			loggers.Add (consoleLogger);
			
			var consoleLogLevelEnv = Environment.GetEnvironmentVariable ("MESHWORK_CONSOLE_LOG_LEVEL");
			if (!string.IsNullOrEmpty (consoleLogLevelEnv)) {
				try {
					consoleLogger.EnabledLevel = (EnabledLoggingLevel) Enum.Parse (typeof (EnabledLoggingLevel), consoleLogLevelEnv, true);
				} catch {}
			}
			
			var consoleLogUseColourEnv = Environment.GetEnvironmentVariable ("MESHWORK_CONSOLE_LOG_USE_COLOUR");
			if (!string.IsNullOrEmpty (consoleLogUseColourEnv) && consoleLogUseColourEnv.ToLower () == "false") {
				consoleLogger.UseColour = false;
			} else {
				consoleLogger.UseColour = true;
			}
			
			var logFileEnv = Environment.GetEnvironmentVariable ("MESHWORK_LOG_FILE");
			if (!string.IsNullOrEmpty (logFileEnv)) {
				try {
					var fileLogger = new FileLogger (logFileEnv);
					loggers.Add (fileLogger);
					var logFileLevelEnv = Environment.GetEnvironmentVariable ("MESHWORK_FILE_LOG_LEVEL");
					fileLogger.EnabledLevel = (EnabledLoggingLevel) Enum.Parse (typeof (EnabledLoggingLevel), logFileLevelEnv, true);
				} catch (Exception e) {
					LogError (e.ToString ());
				}
			}
		}
		
#region the core service
		
		public static bool IsLevelEnabled (LogLevel level)
		{
			var l = (EnabledLoggingLevel) level;
			foreach (var logger in loggers)
				if ((logger.EnabledLevel & l) == l)
					return true;
			return false;
		}
		
		public static void Log (LogLevel level, string message)
		{
			var l = (EnabledLoggingLevel) level;
			foreach (var logger in loggers)
				if ((logger.EnabledLevel & l) == l)
					logger.Log (level, message);
		}
		
#endregion
		
#region methods to access/add/remove loggers -- this service is essentially a log message broadcaster
		
		public static ILogger GetLogger (string name)
		{
			foreach (var logger in loggers)
				if (logger.Name == name)
					return logger;
			return null;
		}
		
		public static void AddLogger (ILogger logger)
		{
			if (GetLogger (logger.Name) != null)
				throw new Exception ("There is already a logger with the name '" + logger.Name + "'");
			loggers.Add (logger);
		}
		
		public static void RemoveLogger (string name)
		{
			var logger = GetLogger (name);
			if (logger == null)
				throw new Exception ("There is no logger registered with the name '" + name + "'");
			loggers.Remove (logger);
		}
		
#endregion
		
#region convenience methods (string message)
		
		public static void LogDebug (string message)
		{
			Log (LogLevel.Debug, message);
		}
		
		public static void LogInfo (string message)
		{
			Log (LogLevel.Info, message);
		}
		
		public static void LogWarning (string message)
		{
			Log (LogLevel.Warn, message);
		}
		
		public static void LogError (string message)
		{
			Log (LogLevel.Error, message);
		}
		
		public static void LogError (Exception ex)
		{
			Log (LogLevel.Error, ex.ToString());
		}
		
		public static void LogFatalError (string message)
		{
			Log (LogLevel.Fatal, message);
		}
		
#endregion
		
#region convenience methods (string messageFormat, params object[] args)
		
		public static void LogDebug (string messageFormat, params object[] args)
		{
			Log (LogLevel.Debug, string.Format (messageFormat, args));
		}
		
		public static void LogInfo (string messageFormat, params object[] args)
		{
			Log (LogLevel.Info, string.Format (messageFormat, args));
		}
		
		public static void LogWarning (string messageFormat, params object[] args)
		{
			Log (LogLevel.Warn, string.Format (messageFormat, args));
		}
		
		public static void LogError (string messageFormat, params object[] args)
		{
			Log (LogLevel.Error, string.Format (messageFormat, args));
		}
		
		public static void LogFatalError (string messageFormat, params object[] args)
		{
			Log (LogLevel.Fatal, string.Format (messageFormat, args));
		}
		
#endregion
		
#region convenience methods (string message, Exception ex)
		
		public static void LogDebug (string message, Exception ex)
		{
			Log (LogLevel.Debug, message + Environment.NewLine + (ex != null? ex.ToString () : string.Empty));
		}
		
		public static void LogInfo (string message, Exception ex)
		{
			Log (LogLevel.Info, message + Environment.NewLine + (ex != null? ex.ToString () : string.Empty));
		}
		
		public static void LogWarning (string message, Exception ex)
		{
			Log (LogLevel.Warn, message + Environment.NewLine + (ex != null? ex.ToString () : string.Empty));
		}
		
		public static void LogError (string message, Exception ex)
		{
			Log (LogLevel.Error, message + Environment.NewLine + (ex != null? ex.ToString () : string.Empty));
		}
		
		public static void LogFatalError (string message, Exception ex)
		{
			Log (LogLevel.Fatal, message + Environment.NewLine + (ex != null? ex.ToString () : string.Empty));
		}
		
#endregion		
		
		
	}
}