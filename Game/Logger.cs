using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using Game.Helpers;

namespace Game
{
	public static class Logger
	{
		public enum Level
		{
			Debug,
			Info,
			Error
		}

		private static string logFile;

		public static bool enableConsole;
		public static bool enableFile = true;
		public static Level minLevel = Level.Info;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsEnabled(Level level)
		{
			return level >= minLevel;
		}

		[Conditional("LOGGING")]
		public static void Log(Level level, string msg)
		{
			if (level < minLevel)
				return;
			if (enableConsole)
				Console.Error.WriteLine($"{level.ToString().ToUpper()} {msg}");
			if (enableFile)
			{
				if (logFile == null)
					logFile = Path.Combine(FileHelper.PatchDirectoryName("logs"), $"log{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.txt");
				using (var fileStream = File.Open(logFile, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
				using (var w = new StreamWriter(fileStream))
					w.WriteLine($"{DateTime.Now:s} {level.ToString().ToUpper()} {msg}");
			}
		}

		[Conditional("LOGGING")]
		public static void Debug(string msg)
		{
			Log(Level.Debug, msg);
		}

		[Conditional("LOGGING")]
		public static void Info(string msg)
		{
			Log(Level.Info, msg);
		}

		[Conditional("LOGGING")]
		public static void Error(string msg)
		{
			Log(Level.Error, msg);
		}

		[Conditional("LOGGING")]
		public static void Error(Exception exception, string msg)
		{
			Error($"{msg.TrimEnd('.')}. {exception}");
		}

		[Conditional("LOGGING")]
		public static void Error(Exception exception)
		{
			Error(exception.ToString());
		}
	}
}